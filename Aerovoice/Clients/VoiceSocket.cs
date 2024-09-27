using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Websocket.Client;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json.Linq;
using System.Net;
using Sodium;
using Microsoft.VisualBasic;
using NAudio.Wave;
using System.Timers;
using DSharpPlus.Entities;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using OpusDotNet;
using Aerovoice.Players;
using Aerovoice.Decoders;
using Aerovoice.Crypts;
using Aerovoice.Logging;
using Aerovoice.Recorders;
using Aerovoice.Encoders;
using Aerovoice.Timestamp;

namespace Aerovoice.Clients
{
    public class VoiceSocket
    {
        private WebsocketClient _socket;
        private UDPClient UdpClient;
        private DiscordClient _client;
        private JObject _ready;
        private bool _disposed = false;


        public IPlayer Player = new NAudioPlayer();
        public IDecoder Decoder = new OpusDotNetDecoder();
        public IEncoder Encoder = new ConcentusEncoder();
        public BaseRecorder Recorder = new NAudioRecorder();
        public string? ForceEncryptionName;


        private BaseCrypt cryptor;
        private byte[] _secretKey;
        private int _sequence;
        private string _sessionId;
        private string _voiceToken;
        private Uri _endpoint;
        public DiscordChannel Channel { get; private set; }
        private bool _connected = false;
        private List<string> _availableEncryptionModes;
        private RTPTimestamp _timestamp = new(3840);
        private uint _ssrc = 0;

        public bool Speaking { get; private set; } = false;

        System.Timers.Timer _timer;

        public VoiceSocket(DiscordClient client)
        {
            _client = client;
            // every 1/60 seconds, on another thread, incrementTimestamp using 3840
            _timer = new();
            _timer.Interval = 16.666666666666668;
            _timer.AutoReset = true;
            _timer.Elapsed += (s, e) => _timestamp.Increment(3840);
            _timer.Start();
        }

        public async Task SendMessage(JObject message)
        {
            _socket.Send(message.ToString());
        }

        public byte[] ConstructPortScanPacket(uint ssrc, string ip, ushort port)
        {
            // Allocate buffers for each part of the packet
            byte[] packetType = new byte[2];
            BitConverter.GetBytes((ushort)1).CopyTo(packetType, 0);
            Array.Reverse(packetType); // Ensure big-endian order

            byte[] packetLength = new byte[2];
            BitConverter.GetBytes((ushort)70).CopyTo(packetLength, 0);
            Array.Reverse(packetLength);    

            byte[] ssrcBuf = new byte[4];
            BitConverter.GetBytes(ssrc).CopyTo(ssrcBuf, 0);
            Array.Reverse(ssrcBuf);

            byte[] address = new byte[64];
            byte[] ipBytes = Encoding.UTF8.GetBytes(ip);
            Array.Copy(ipBytes, address, ipBytes.Length);

            byte[] portBuffer = new byte[2];
            BitConverter.GetBytes(port).CopyTo(portBuffer, 0);
            Array.Reverse(portBuffer);

            byte[] packet = new byte[2 + 2 + 4 + 64 + 2];
            Array.Copy(packetType, 0, packet, 0, 2);
            Array.Copy(packetLength, 0, packet, 2, 2);
            Array.Copy(ssrcBuf, 0, packet, 4, 4);
            Array.Copy(address, 0, packet, 8, 64);
            Array.Copy(portBuffer, 0, packet, 72, 2);

            return packet;
        }

        public async Task OnMessageReceived(JObject message)
        {
            // if message["seq"] exists, set _sequence to it
            if (message["seq"] != null)
            {
                _sequence = message["seq"]!.Value<int>();
            }
            int op = message["op"]!.Value<int>();
            switch (op)
            {
                case 2: // ready
                {
                    _ready = message["d"]!.Value<JObject>()!;
                    var ip = _ready["ip"]!.Value<string>()!;
                    var port = _ready["port"]!.Value<ushort>();
                    _ssrc = _ready["ssrc"]!.Value<uint>();
                    var modes = _ready["modes"]!.ToArray().Select(x => x.Value<string>()!);
                    _availableEncryptionModes = modes.ToList();
                    Logger.Log($"Attempting to open UDP connection to {ip}:{port}.");
                    Logger.Log($"Your SSRC is {_ssrc}.");
                    UdpClient = new(ip, port);
                    UdpClient.MessageReceived += (s, e) => Task.Run(() => UdpClient_MessageReceived(s, e));

                    var discoveryPacket = ConstructPortScanPacket(_ssrc, ip, port);

                    UdpClient.SendMessage(discoveryPacket);
                    break;
                }
                case 4: // session description
                {
                    // secret_key is a number array
                    var secretKey = message["d"]!["secret_key"]!.Value<JArray>()!.Select(x => (byte)x.Value<int>()).ToArray();
                    _secretKey = secretKey;
                    if (cryptor is null)
                    {
                        cryptor = GetPreferredEncryption();
                    }
                    break;
                }
            }
        }

        private async Task UdpClient_MessageReceived(object? sender, byte[] e)
        {
            byte packetType = e[1];
            switch (packetType)
            {
                case 0x2: // ip discovery
                {
                    var address = Encoding.UTF8.GetString(e, 8, 64).TrimEnd('\0');
                    var port = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(e, 72));
                    Logger.Log($"IP discovery was successful, your port is {port}.");
                    if (cryptor is null)
                    {
                        cryptor = GetPreferredEncryption();
                    }
                    await SendMessage(JObject.FromObject(new
                    {
                        op = 1,
                        d = new
                        {
                            protocol = "udp",
                            data = new
                            {
                                address,
                                port,
                                mode = cryptor.PName
                            },
                            codecs = new[]
        {
                                new
                                {
                                    name = "opus",
                                    type = "audio",
                                    priority = 1000,
                                    payload_type = 120
                                }
                            }
                        }
                    }));
                    Recorder.Start();
                    break;
                }
                case 0x78: // voice data
                {
                    // TODO: thread manager where each user gets one thread
                    await Task.Run(() =>
                    {
                        if (cryptor is null || _secretKey is null) return;
                        byte[] decryptedData = [];
                        decryptedData = cryptor.Decrypt(e, _secretKey);
                        if (decryptedData.Length == 0) return;
                        // ssrc is at offset 4 and is a 4 byte (32 bit) unsigned big-endian integer
                        var ssrc = BinaryPrimitives.ReadUInt32BigEndian(e.AsSpan(8));
                        var decoded = Decoder.Decode(decryptedData, decryptedData.Length, out int decodedLength, ssrc);
                        Player.AddSamples(decoded, decodedLength, ssrc);
                    });
                    break;
                }
            }
        }

        public BaseCrypt GetPreferredEncryption()
        {
            var decryptors = typeof(BaseCrypt).Assembly.GetTypes().Where(x => x.Namespace == "Aerovoice.Crypts" && x.IsSubclassOf(typeof(BaseCrypt)) && _availableEncryptionModes.Contains((string)x.GetProperty("Name")!.GetValue(null)!));
            var priority = new[] { "aead_aes256_gcm_rtpsize", "aead_xchacha20_poly1305_rtpsize" };
            BaseCrypt? decryptor = null;
            if (ForceEncryptionName != null)
            {
                var forced = decryptors.FirstOrDefault(x => x.GetProperty("Name")!.GetValue(null)!.Equals(ForceEncryptionName));
                if (forced != null)
                {
                    decryptor = (BaseCrypt)Activator.CreateInstance(forced)!;
                } else
                {
                    Logger.Log($"\"{ForceEncryptionName}\" is not supported, falling back to default.");
                }
            }
            if (decryptor == null)
            {
                foreach (var p in priority)
                {
                    var d = decryptors.FirstOrDefault(x => x.GetProperty("Name")!.GetValue(null)!.Equals(p));
                    if (d != null && _availableEncryptionModes.Contains(p))
                    {
                        decryptor = (BaseCrypt)Activator.CreateInstance(d)!;
                        break;
                    }
                }
            }

            decryptor = decryptor ?? (BaseCrypt)Activator.CreateInstance(decryptors.FirstOrDefault(x => _availableEncryptionModes.Contains(x.GetProperty("Name")!.GetValue(null))!)!)!;
            // log all encryption modes but make the preferred one bold
            Logger.Log($"Encryption mode selected:");
            var names = decryptors.Select(x => (string)x.GetProperty("Name")!.GetValue(null)!);
            // sort the modes such that the preferred one is first, then the supported ones, then the unsupported ones
            // unsupported modes are modes where Name isn't in names
            _availableEncryptionModes = _availableEncryptionModes.OrderBy(x => x != decryptor.PName).ThenBy(x => !names.Contains(x)).ThenBy(x => x).ToList();
            foreach (var mode in _availableEncryptionModes)
            {
                if (mode == decryptor.PName)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                } 
                else if (!names.Contains(mode))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine($"- {mode}");
                Console.ResetColor();
            }
            return decryptor;
        }

        public async Task ConnectAsync(DiscordChannel channel)
        {
            if (_disposed) throw new InvalidOperationException("This voice socket has been disposed!");
            Channel = channel;
            await _client.UpdateVoiceStateAsync(Channel.Guild?.Id ?? Channel.Id, Channel.Id, false, false);
            _client.VoiceStateUpdated += _client_VoiceStateUpdated;
            _client.VoiceServerUpdated += _client_VoiceServerUpdated;
            Recorder.DataAvailable += Recorder_DataAvailable;
        }

        private short _udpSequence = (short)new Random().Next(0, short.MaxValue);

        private const int BufferDurationMs = 200;
        private const int ChunkDurationMs = 20;
        private const int SampleRate = 48000; // 48kHz
        private const int BytesPerSample = 2; // 16-bit PCM
        private const int Channels = 2; // Stereo
        private const int BufferSizeBytes = (SampleRate * Channels * BytesPerSample * BufferDurationMs) / 1000; // 38400 bytes
        private byte[] _circularBuffer = new byte[BufferSizeBytes];
        private int _bufferOffset = 0;
        private bool _bufferFilled = false;

        private async void Recorder_DataAvailable(object? sender, byte[] e)
        {
            await Task.Run(() =>
            {
                // Append incoming 20ms audio to the circular buffer
                AddToCircularBuffer(e);

                // Check if the user is speaking using the circular buffer
                var sampleIsSpeaking = IsSpeaking(_circularBuffer, _bufferFilled ? BufferSizeBytes : _bufferOffset);

                if (sampleIsSpeaking)
                {
                    if (Speaking)
                    {
                        _ = SendMessage(JObject.FromObject(new
                        {
                            op = 5,
                            d = new
                            {
                                speaking = 0, // not speaking
                                delay = 0,
                                ssrc = _ssrc
                            }
                        }));
                    }
                    Speaking = false;
                    return;
                }

                if (!Speaking)
                {
                    _ = SendMessage(JObject.FromObject(new
                    {
                        op = 5,
                        d = new
                        {
                            speaking = 1 << 0, // VOICE
                            delay = 0,
                            ssrc = _ssrc
                        }
                    }));
                    Speaking = true;
                }

                if (cryptor is null) return;
                var opus = Encoder.Encode(e);
                var header = new byte[12];
                header[0] = 0x80; // Version + Flags
                header[1] = 0x78; // Payload Type
                BinaryPrimitives.WriteInt16BigEndian(header.AsSpan(2), _udpSequence++); // Sequence
                BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), _timestamp.GetCurrentTimestamp()); // Timestamp
                BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), _ssrc); // SSRC
                var packet = new byte[header.Length + opus.Length];
                Array.Copy(header, 0, packet, 0, header.Length);
                Array.Copy(opus, 0, packet, header.Length, opus.Length);
                var encrypted = cryptor.Encrypt(packet, _secretKey);
                UdpClient.SendMessage(encrypted);
            });
        }

        private void AddToCircularBuffer(byte[] data)
        {
            int dataLength = data.Length;

            if (_bufferOffset + dataLength > BufferSizeBytes)
            {
                int overflow = _bufferOffset + dataLength - BufferSizeBytes;
                Array.Copy(data, 0, _circularBuffer, _bufferOffset, dataLength - overflow);
                Array.Copy(data, dataLength - overflow, _circularBuffer, 0, overflow);
                _bufferOffset = overflow;
                _bufferFilled = true;
            }
            else
            {
                Array.Copy(data, 0, _circularBuffer, _bufferOffset, dataLength);
                _bufferOffset += dataLength;
            }
        }

        private bool IsSpeaking(byte[] buffer, int length)
        {
            int samples = length / BytesPerSample; // Convert byte length to number of samples
            double sum = 0;

            for (int i = 0; i < length; i += 2)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]); // Convert to 16-bit sample
                sum += sample * sample;
            }

            // Calculate RMS
            double rms = Math.Sqrt(sum / samples);

            // Threshold for speaking detection
            const double threshold = 300; // Adjust this value based on the microphone and environment
            return rms < threshold;
        }
        private async Task _client_VoiceStateUpdated(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            _sessionId = args.SessionId;
            if (_voiceToken != null && _endpoint != null)
            {
                await BeginVoiceConnection();
            }
        }

        private async Task _client_VoiceServerUpdated(DiscordClient sender, DSharpPlus.EventArgs.VoiceServerUpdateEventArgs args)
        {
            _voiceToken = args.VoiceToken;
            _endpoint = new Uri($"wss://{args.Endpoint}/?v=8");
            if (_sessionId != null)
            {
                await BeginVoiceConnection();
            }
        }

        private async Task BeginVoiceConnection()
        {
            if (_connected) return;
            _connected = true;
            _socket = new WebsocketClient(_endpoint);
            await _socket.Start();
            System.Timers.Timer timer = new();
            timer.Interval = 13750;
            timer.AutoReset = true;
            timer.Elapsed += async (s, e) => await SendMessage(JObject.FromObject(new
            {
                op = 3,
                d = new
                {
                    // t should be the current unix time
                    t = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    seq = _sequence,
                }
            }));
            timer.Start();
            Logger.Log("Connected to the voice gateway, attempting to connect to UDP server!");
            _socket.MessageReceived.Subscribe(x => OnMessageReceived(JObject.Parse(x.Text)));
            await SendMessage(JObject.FromObject(new
            {
                op = 0,
                d = new
                {
                    server_id = Channel.Guild?.Id ?? Channel.Id,
                    user_id = _client.CurrentUser.Id,
                    session_id = _sessionId,
                    token = _voiceToken
                }
            }));
        }

        public async Task DisconnectAndDispose()
        {
            // if the socket isn't connected, return
            if (!_connected) return;
            _connected = false;
            _disposed = true;
            await _client.UpdateVoiceStateAsync(Channel.GuildId, null, false, false);
            _socket?.Dispose();
            UdpClient?.Dispose();
            Recorder?.Dispose();
            _timer.Dispose();
        }
    }
}
