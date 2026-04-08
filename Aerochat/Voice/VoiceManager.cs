using Aerochat.Hoarder;
using Aerochat.ViewModels;
using Aerochat.Windows;
using Aerovoice.Clients;
using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aerochat.Voice
{
    public class VoiceManager : ViewModelBase
    {
        public static VoiceManager Instance = new();
        private VoiceSocket? voiceSocket;
        public DiscordChannel? Channel => voiceSocket?.Channel;

        private ChannelViewModel? _channelVM;
        public ChannelViewModel? ChannelVM
        {
            get => _channelVM;
            set => SetProperty(ref _channelVM, value);
        }

        public event EventHandler<(ulong UserId, bool IsSpeaking)> UserSpeakingChanged;
        public event EventHandler<bool> ClientSpeakingChanged;

        private readonly ConcurrentDictionary<ulong, float> _userVolumes = new();
        private readonly ConcurrentDictionary<ulong, bool> _userMuted = new();

        public bool SelfMuted
        {
            get => voiceSocket?.SelfMuted ?? false;
            set { if (voiceSocket != null) voiceSocket.SelfMuted = value; }
        }

        public bool SelfDeafened
        {
            get => voiceSocket?.SelfDeafened ?? false;
            set { if (voiceSocket != null) voiceSocket.SelfDeafened = value; }
        }

        public float ClientTransmitVolume
        {
            get => voiceSocket?.Player.ClientTransmitVolume ?? 1.0f;
            set { if (voiceSocket != null) voiceSocket.Player.ClientTransmitVolume = value; }
        }

        public void SetUserVolume(ulong userId, float volume)
        {
            _userVolumes[userId] = volume;
            ApplyVolumeForUser(userId);
        }

        public float GetUserVolume(ulong userId)
        {
            return _userVolumes.TryGetValue(userId, out var vol) ? vol : 1.0f;
        }

        public void SetUserMuted(ulong userId, bool muted)
        {
            _userMuted[userId] = muted;
            ApplyMuteForUser(userId);
        }

        public bool IsUserMuted(ulong userId)
        {
            return _userMuted.TryGetValue(userId, out var m) && m;
        }

        private IEnumerable<uint> GetSsrcsForUser(ulong userId)
        {
            if (voiceSocket == null) return Array.Empty<uint>();
            return voiceSocket.SsrcToUserId
                .Where(kvp => kvp.Value == userId)
                .Select(kvp => kvp.Key);
        }

        private void ApplyVolumeForUser(ulong userId)
        {
            if (voiceSocket == null) return;
            float vol = _userVolumes.TryGetValue(userId, out var v) ? v : 1.0f;
            foreach (var ssrc in GetSsrcsForUser(userId))
                voiceSocket.Player.SetSsrcVolume(ssrc, vol);
        }

        private void ApplyMuteForUser(ulong userId)
        {
            if (voiceSocket == null) return;
            bool muted = _userMuted.TryGetValue(userId, out var m) && m;
            foreach (var ssrc in GetSsrcsForUser(userId))
                voiceSocket.Player.SetSsrcMuted(ssrc, muted);
        }

        public async Task LeaveVoiceChannel()
        {
            if (voiceSocket is null)
                return;
            UnsubscribeEvents();
            await voiceSocket.DisconnectAndDispose();
            voiceSocket = null;
            ChannelVM = null;
        }

        public async Task JoinVoiceChannel(DiscordChannel channel)
        {
            await LeaveVoiceChannel();
            voiceSocket = new(Discord.Client);
            SubscribeEvents();
            await voiceSocket.ConnectAsync(channel);
            voiceSocket.Recorder.SetInputDevice(Settings.SettingsManager.Instance.InputDeviceIndex);
            ChannelVM = ChannelViewModel.FromChannel(channel);
        }

        private void SubscribeEvents()
        {
            if (voiceSocket == null) return;
            voiceSocket.UserSpeakingChanged += OnUserSpeakingChanged;
            voiceSocket.ClientSpeakingChanged += OnClientSpeakingChanged;
        }

        private void UnsubscribeEvents()
        {
            if (voiceSocket == null) return;
            voiceSocket.UserSpeakingChanged -= OnUserSpeakingChanged;
            voiceSocket.ClientSpeakingChanged -= OnClientSpeakingChanged;
        }

        private void OnUserSpeakingChanged(object? sender, (ulong UserId, bool IsSpeaking) e)
        {
            if (voiceSocket == null) return;
            if (e.IsSpeaking)
            {
                ApplyMuteForUser(e.UserId);
                ApplyVolumeForUser(e.UserId);
            }
            UserSpeakingChanged?.Invoke(this, e);
        }

        private void OnClientSpeakingChanged(object? sender, bool isSpeaking)
        {
            ClientSpeakingChanged?.Invoke(this, isSpeaking);
        }
    }
}
