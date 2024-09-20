using Aerochat.Hoarder;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private UserViewModel? _author;
        private string _message;
        private string _rawMessage;
        private DateTime _timestamp;
        private ulong? _id;
        private bool _ephemeral = false;
        private bool _special = false;
        private bool _hiddenInfo = false;
        private string _type;
        private bool _isReply = false;
        private MessageViewModel? _replyMessage;
        private DiscordMessage _messageEntity;

        public UserViewModel? Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string RawMessage
        {
            get => _rawMessage;
            set => SetProperty(ref _rawMessage, value);
        }
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }
        public ulong? Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public bool Ephemeral
        {
            get => _ephemeral;
            set => SetProperty(ref _ephemeral, value);
        }

        public bool Special
        {
            get => _special;
            set => SetProperty(ref _special, value);
        }

        public bool HiddenInfo
        {
            get => _hiddenInfo;
            set => SetProperty(ref _hiddenInfo, value);
        }
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public bool IsReply
        {
            get => _isReply;
            set => SetProperty(ref _isReply, value);
        }

        public MessageViewModel? ReplyMessage
        {
            get => _replyMessage;
            set => SetProperty(ref _replyMessage, value);
        }

        public DiscordMessage MessageEntity
        {
            get => _messageEntity;
            set => SetProperty(ref _messageEntity, value);
        }

        public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();
        public ObservableCollection<EmbedViewModel> Embeds { get; } = new();

        public static MessageViewModel FromMessage(DiscordMessage message, DiscordMember? member = null, bool isReply = false)
        {
            if (message == null) return new();
            var user = member == null ? UserViewModel.FromUser(message.Author) : UserViewModel.FromMember(member);
            var vm = new MessageViewModel
            {
                Author = user,
                Id = message.Id,
                Timestamp = message.Timestamp.DateTime,
                RawMessage = message.Content,
                Type = message.MessageType?.ToString() ?? "Unknown",
                IsReply = message.MessageType == MessageType.Reply && !isReply,
                MessageEntity = message
            };
            foreach (var embed in message.Embeds)
            {
                vm.Embeds.Add(EmbedViewModel.FromEmbed(embed));
            }

            // TO ANY DEVELOPER LOOKING AT THIS, THINKING "WHERE IS THIS CRASHING??"
            // IT'S PROBABLY THE TIERONE, TIERTWO, OR TIERTHREE USERPREMIUMGUILDSUBSCRIPTIONS!!!!
            var specialMsg = message.MessageType switch
            {
                MessageType.GuildMemberJoin => $"{user.Name} has entered the conversation.",
                MessageType.UserPremiumGuildSubscription => $"{user.Name} has subscribed to {message.Channel.Guild.Name}!",
                MessageType.TierOneUserPremiumGuildSubscription => $"{user.Name} has subscribed to the server, bringing {message.Channel.Guild.Name} to level one!",
                MessageType.TierTwoUserPremiumGuildSubscription => $"{user.Name} has subscribed to the server, bringing {message.Channel.Guild.Name} to level two!",
                MessageType.TierThreeUserPremiumGuildSubscription => $"{user.Name} has subscribed to the server, bringing {message.Channel.Guild.Name} to level three!",
                MessageType.RecipientAdd => $"{user.Name} has been added to the group.",
                MessageType.RecipientRemove => $"{user.Name} has been removed from the group.",
                MessageType.Call => $"{user.Name} has started a call.",
                MessageType.ChannelFollowAdd => $"{user.Name} has followed the channel.",
                MessageType.GuildDiscoveryDisqualified => $"{message.Channel.Guild.Name} has been disqualified from guild discovery.",
                MessageType.GuildDiscoveryRequalified => $"{message.Channel.Guild.Name} has been requalified for guild discovery.",
                MessageType.GuildDiscoveryGracePeriodInitialWarning => $"{message.Channel.Guild.Name} has failed to meet the guild discovery requirements for a week.",
                MessageType.GuildDiscoveryGracePeriodFinalWarning => $"{message.Channel.Guild.Name} has failed to meet the guild discovery requirements for 3 weeks.",
                _ => null
            };
            switch (message.Content)
            {
                case "[nudge]":
                    //return new MessageViewModel
                    //{
                    //    Author = user,
                    //    Id = message.Id,
                    //    Message = $"{(user.Id == Discord.Client.CurrentUser.Id ? "You have" : $"{user.Name} has")} just sent a nudge.",
                    //    Timestamp = message.Timestamp.DateTime,
                    //    Special = true,
                    //    RawMessage = message.Content
                    //};
                    vm.Message = $"{(user.Id == Discord.Client.CurrentUser.Id ? "You have" : $"{user.Name} has")} just sent a nudge.";
                    vm.Special = true;
                    break;
                default:
                    vm.Message = message.Content;
                    break;
            }

            if (!string.IsNullOrEmpty(specialMsg))
            {
                vm.Message = specialMsg;
                vm.Special = true;
            }

            foreach (var attachment in message.Attachments)
            {
                vm.Attachments.Add(AttachmentViewModel.FromAttachment(attachment));
            }

            if (vm.IsReply)
            {
                vm.ReplyMessage = FromMessage(message.ReferencedMessage, null, true);
            }

            return vm;
        }
    }
}
