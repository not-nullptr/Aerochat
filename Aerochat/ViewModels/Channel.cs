using Aerochat.Hoarder;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class ChannelViewModel : ViewModelBase
    {
        private string _name;
        private string _topic;
        private ulong _id;
        private bool _canTalk;

        public required string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public required string Topic
        {
            get => _topic;
            set => SetProperty(ref _topic, value);
        }

        public required ulong Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public bool CanTalk
        {
            get => _canTalk;
            set => SetProperty(ref _canTalk, value);
        }

        public static ChannelViewModel FromChannel(DiscordChannel channel)
        {
            return new ChannelViewModel
            {
                Name = channel is DiscordDmChannel ? channel.Name ?? string.Join(", ", ((DiscordDmChannel)channel).Recipients.Select(x => x.DisplayName)) : channel.Name,
                Topic = channel.Topic ?? "",
                Id = channel.Id,
                CanTalk = channel.Guild == null || (channel.PermissionsFor(channel.Guild.CurrentMember) & Permissions.SendMessages) == Permissions.SendMessages
            };
        }
    }
}
