using Aerochat.Hoarder;
using Aerochat.ViewModels;
using Aerochat.Windows;
using Aerovoice.Clients;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aerochat.Voice
{
    public class VoiceManager : ViewModelBase
    {
        public static VoiceManager Instance = new();
        private VoiceSocket? voiceSocket;
        public VoiceSocket? VoiceSocket
        {
            get => voiceSocket;
        }

        public DiscordChannel? Channel => voiceSocket?.Channel;

        private ChannelViewModel? _channelVM;
        public ChannelViewModel? ChannelVM
        {
            get => _channelVM;
            set => SetProperty(ref _channelVM, value);
        }

        public async Task LeaveVoiceChannel()
        {
            if (voiceSocket is null)
                return;
            await voiceSocket.DisconnectAndDispose();
            voiceSocket = null;
            ChannelVM = null;
        }

        public async Task JoinVoiceChannel(DiscordChannel channel, Action<VoiceStateChanged> onStateChange)
        {
            await LeaveVoiceChannel();
            voiceSocket = new(Discord.Client, onStateChange);
            await voiceSocket.ConnectAsync(channel);
            ChannelVM = ChannelViewModel.FromChannel(channel);
        }
    }
}
