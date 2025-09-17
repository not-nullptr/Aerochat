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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.Kernel32;

namespace Aerovoice.Clients
{
    public class VoiceStateChanged
    {
        public uint SSRC;
        public bool Speaking;

        public VoiceStateChanged(uint SSRC, bool Speaking)
        {
            this.SSRC = SSRC;
            this.Speaking = Speaking;
        }
    }

    struct IPInfo
    {
        public string Address;
        public ushort Port;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VoiceUserCallback(uint ssrc, bool speaking);

    partial class VoiceSession : IDisposable
    {
        unsafe struct RawIPInfo
        {
            public fixed byte IP[64];
            public ushort Port;
        }

        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr voice_session_new(uint ssrc, ulong channelId, [MarshalAs(UnmanagedType.LPUTF8Str)] string ip, ushort port, VoiceUserCallback onSpeaking);
        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void voice_session_free(IntPtr sessionHandle);
        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void voice_session_init_poll_thread(IntPtr sessionHandle);
        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr voice_session_discover_ip(IntPtr sessionHandle);
        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private unsafe static partial IntPtr voice_session_set_secret(IntPtr sessionHandle, byte* secret, uint secretLen);
        [LibraryImport("AerovoiceNative.dll")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.LPUTF8Str)]
        private unsafe static partial string voice_session_select_cryptor(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPUTF8Str, SizeParamIndex = 2)] string[] availableMethods, uint availableMethodsLen);

        private IntPtr _sessionHandle;

        public VoiceSession(uint ssrc, ulong channelId, string ip, ushort port, VoiceUserCallback onSpeaking)
        {
            AllocConsole();
            _sessionHandle = voice_session_new(ssrc, channelId, ip, port, onSpeaking);
            if (_sessionHandle == IntPtr.Zero)
            {
                throw new Exception("Failed to create voice session.");
            }
        }

        public void BeginPolling()
        {
            voice_session_init_poll_thread(_sessionHandle);
        }

        public IPInfo DiscoverIP()
        {
            unsafe
            {
                var result = *(RawIPInfo*)voice_session_discover_ip(_sessionHandle).ToPointer();
                byte[] managedIP = new byte[64];
                for (int i = 0; i < 64; i++)
                {
                    managedIP[i] = result.IP[i];
                }

                string ip = Encoding.UTF8.GetString(managedIP).Trim((char)0);
                ushort port = result.Port;
                return new IPInfo { Address = ip, Port = port };
            }
        }

        public void SetSecret(byte[] secret)
        {
            var len = (uint)secret.Length;
            unsafe
            {
                fixed (byte* secretPtr = &secret[0])
                {
                    voice_session_set_secret(_sessionHandle, secretPtr, len);
                }
            }
        }

        public string SelectCryptor(string[] availableCryptors)
        {
            return voice_session_select_cryptor(_sessionHandle, availableCryptors, (uint)availableCryptors.Length);
        }

        public void Dispose()
        {
            FreeConsole();
            if (_sessionHandle != IntPtr.Zero)
            {
                voice_session_free(_sessionHandle);
                _sessionHandle = IntPtr.Zero;
            }
        }
    }


    public class VoiceSocket
    {
        private WebsocketClient _socket;
        private DiscordClient _client;
        private JObject _ready;
        private bool _disposed = false;
        private VoiceSession? _session;

        //private BaseCrypt cryptor;
        private string _serverSdp;
        private int _sequence;
        private string _sessionId;
        private string _voiceToken;
        private Uri _endpoint;
        public DiscordChannel Channel { get; private set; }
        private bool _connected = false;
        private RTPTimestamp _timestamp = new(3840);
        private uint _ssrc = 0;
        private VoiceUserCallback _cb;
        private Dictionary<uint, ulong> _userSsrcMap = [];
        public Dictionary<uint, ulong> UserSSRCMap { get { return _userSsrcMap; } }

        public bool Speaking { get; private set; } = false;

        System.Timers.Timer _timer;

        Action<VoiceStateChanged> _onStateChange;

        public VoiceSocket(DiscordClient client, Action<VoiceStateChanged> onStateChange)
        {
            _client = client;
            // every 1/60 seconds, on another thread, incrementTimestamp using 3840
            _timer = new();
            _timer.Interval = 16.666666666666668;
            _timer.AutoReset = true;
            _timer.Elapsed += (s, e) => _timestamp.Increment(3840);
            _timer.Start();
            _onStateChange = onStateChange;
            _cb = new VoiceUserCallback(InternalVoiceCallback);
        }

        private void InternalVoiceCallback(uint ssrc, bool speaking)
        {
            _onStateChange(new VoiceStateChanged(ssrc, speaking));
            if (ssrc != _ssrc || _socket == null)
            {
                return;
            }

            Debug.WriteLine(speaking);

            Task.Run(async () =>
            {
                var msg = new
                {
                    op = 5,
                    d = new
                    {
                        speaking = speaking ? 1 : 0,
                        delay = 0,
                        ssrc
                    }
                };
                await SendMessage(JObject.FromObject(msg));
            });
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

        private void OnSpeaking(uint ssrc, bool speaking)
        { }

        public async Task OnMessageReceived(JObject message)
        {
            Debug.WriteLine(message);
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
                        _userSsrcMap.Add(_ssrc, _client.CurrentUser.Id);
                        var modes = _ready["modes"]!.ToArray().Select(x => x.Value<string>()!);
                        Logger.Log($"Attempting to open UDP connection to {ip}:{port}.");
                        Logger.Log($"Your SSRC is {_ssrc}.");
                        _session = new VoiceSession(_ssrc, this.Channel.Id, ip, port, _cb);
                        var discovered = _session.DiscoverIP();
                        Debug.WriteLine($"IP: {discovered.Address}");
                        Debug.WriteLine($"Port: {discovered.Port}");
                        var cryptor = _session.SelectCryptor([.. modes]);
                        if (cryptor == null) return;
                        var msg = new
                        {
                            op = 1,
                            d = new
                            {
                                protocol = "udp",
                                data = new
                                {
                                    address = discovered.Address,
                                    port = discovered.Port,
                                    mode = cryptor
                                },
                                codecs = new[]
                                {
                                    new
                                    {
                                        name = "opus",
                                        type = "audio",
                                        priority = 1000,
                                        payload_type = 109,
                                    }
                                }
                            }
                        };

                        await SendMessage(JObject.FromObject(msg));
                        break;
                    }
                case 4: // session description
                    {
                        //var serverSdp = message["d"]!["sdp"]!.Value<string>()!;
                        //_serverSdp = serverSdp;
                        //_session.SetServerSdp(_serverSdp);
                        var secret = message["d"]!["secret_key"]!.Value<JArray>()!.Select(x => (byte)x.Value<int>()).ToArray();
                        _session?.SetSecret(secret);
                        _session?.BeginPolling();
                        var msg = new
                        {
                            op = 5,
                            d = new
                            {
                                speaking = 0,
                                delay = 0,
                                ssrc = _ssrc
                            }
                        };
                        await SendMessage(JObject.FromObject(msg));
                        break;
                    }
                case 5: // speaking
                    {
                        var userId = ulong.Parse(message["d"]!["user_id"]!.Value<string>()!);
                        var ssrc = message["d"]!["ssrc"]!.Value<uint>()!;

                        _userSsrcMap.Remove(ssrc);
                        _userSsrcMap.Add(ssrc, userId);

                        break;
                    }
            }
        }

        public async Task ConnectAsync(DiscordChannel channel)
        {
            if (_disposed) throw new InvalidOperationException("This voice socket has been disposed!");
            Channel = channel;
            await _client.UpdateVoiceStateAsync(Channel.Guild?.Id ?? Channel.Id, Channel.Id, false, false);
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
                    server_id = Channel.Guild?.Id.ToString() ?? "",
                    user_id = _client.CurrentUser.Id.ToString(),
                    session_id = _sessionId,
                    token = _voiceToken,
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
            _timer.Dispose();
            _session?.Dispose();
        }
    }
}
