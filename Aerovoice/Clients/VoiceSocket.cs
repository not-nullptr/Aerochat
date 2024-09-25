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
using Aerovoice.Decryptors;
using Aerovoice.Logging;

namespace Aerovoice.Clients
{
    public class VoiceSocket(DiscordClient client)
    {
        private WebsocketClient _socket;
        private UDPClient UdpClient;
        private DiscordClient _client = client;
        private JObject _ready;
        private IPlayer _player = new NAudioPlayer();
        private IDecoder _decoder = new OpusDotNetDecoder();
        private BaseDecryptor _decryptor;
        private byte[] _secretKey;
        private int _sequence;
        private string _sessionId;
        private string _voiceToken;
        private Uri _endpoint;
        private DiscordChannel _channel;
        private bool _connected = false;
        private List<string> _availableEncryptionModes;

        public string? ForceEncryptionName;

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
                    var ssrc = _ready["ssrc"]!.Value<uint>();
                    var modes = _ready["modes"]!.ToArray().Select(x => x.Value<string>()!);
                    _availableEncryptionModes = modes.ToList();
                    Logger.Log($"Attempting to open UDP connection to {ip}:{port}.");
                    Logger.Log($"Your SSRC is {ssrc}.");
                    UdpClient = new(ip, port);
                    UdpClient.MessageReceived += (s, e) => Task.Run(() => UdpClient_MessageReceived(s, e));

                    var discoveryPacket = ConstructPortScanPacket(ssrc, ip, port);

                    UdpClient.SendMessage(discoveryPacket);
                    break;
                }
                case 4: // session description
                {
                    // secret_key is a number array
                    var secretKey = message["d"]!["secret_key"]!.Value<JArray>()!.Select(x => (byte)x.Value<int>()).ToArray();
                    _secretKey = secretKey;
                    if (_decryptor is null)
                    {
                        _decryptor = GetPreferredEncryption();
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
                    Logger.Log($"IP discovery was successful, your info is {address}:{port}.");
                    if (_decryptor is null)
                    {
                        _decryptor = GetPreferredEncryption();
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
                                mode = _decryptor.PName
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
                    break;
                }
                case 0x78: // voice data
                {
                    // TODO: thread manager where each user gets one thread
                    await Task.Run(() =>
                    {
                        if (_decryptor is null) return;
                        byte[] decryptedData = [];
                        decryptedData = _decryptor.Decrypt(e, _secretKey);
                        if (decryptedData.Length == 0) return;
                        // ssrc is at offset 4 and is a 4 byte (32 bit) unsigned big-endian integer
                        var ssrc = BinaryPrimitives.ReadUInt32BigEndian(e.AsSpan(8));
                        var decoded = _decoder.Decode(decryptedData, decryptedData.Length, out int decodedLength);
                        _player.AddSamples(decoded, decodedLength, ssrc);
                    });
                    break;
                }
            }
        }

        public BaseDecryptor GetPreferredEncryption()
        {
            var decryptors = typeof(BaseDecryptor).Assembly.GetTypes().Where(x => x.Namespace == "Aerovoice.Decryptors" && x.IsSubclassOf(typeof(BaseDecryptor)) && _availableEncryptionModes.Contains((string)x.GetProperty("Name")!.GetValue(null)!));
            var priority = new[] { "aead_aes256_gcm_rtpsize", "aead_xchacha20_poly1305_rtpsize" };
            BaseDecryptor? decryptor = null;
            if (ForceEncryptionName != null)
            {
                var forced = decryptors.FirstOrDefault(x => x.GetProperty("Name")!.GetValue(null)!.Equals(ForceEncryptionName));
                if (forced != null)
                {
                    decryptor = (BaseDecryptor)Activator.CreateInstance(forced)!;
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
                        decryptor = (BaseDecryptor)Activator.CreateInstance(d)!;
                        break;
                    }
                }
            }

            decryptor = decryptor ?? (BaseDecryptor)Activator.CreateInstance(decryptors.FirstOrDefault(x => _availableEncryptionModes.Contains(x.GetProperty("Name")!.GetValue(null))!)!)!;
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
            _channel = channel;
            await _client.UpdateVoiceStateAsync(_channel.Guild?.Id ?? _channel.Id, _channel.Id, false, false);
            _client.VoiceStateUpdated += _client_VoiceStateUpdated;
            _client.VoiceServerUpdated += _client_VoiceServerUpdated;
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
                    server_id = _channel.Guild?.Id ?? _channel.Id,
                    user_id = _client.CurrentUser.Id,
                    session_id = _sessionId,
                    token = _voiceToken
                }
            }));
        }
    }
}
