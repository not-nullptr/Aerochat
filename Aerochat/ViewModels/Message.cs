using Aerochat.Enums;
using Aerochat.Hoarder;
using Aerochat.Settings;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

        public string TimestampString
        {
            get
            {
                var format = SettingsManager.Instance.SelectedTimeFormat == TimeFormat.TwentyFourHour ? "HH:mm" : "h:mm tt";
                return Timestamp.ToString(format, CultureInfo.InvariantCulture);  // Ensure InvariantCulture to control formatting: Otherwise we will lose the AM/PM at the end!
            }
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    RaisePropertyChanged(nameof(Timestamp));
                    RaisePropertyChanged(nameof(TimestampString));
                }
            }
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                MessageType.GuildDiscoveryDisqualified => $"{message.Channel.Guild.Name} has been disqualified from server discovery.",
                MessageType.GuildDiscoveryRequalified => $"{message.Channel.Guild.Name} has been requalified for server discovery.",
                MessageType.GuildDiscoveryGracePeriodInitialWarning => $"{message.Channel.Guild.Name} has failed to meet the server discovery requirements for a week.",
                MessageType.GuildDiscoveryGracePeriodFinalWarning => $"{message.Channel.Guild.Name} has failed to meet the server discovery requirements for three weeks.",
                MessageType.ChannelPinnedMessage => $"{user.Name} pinned a message to this channel.",
                MessageType.ChannelNameChange => $"{user.Name} has changed the group name to {message.Content}.",
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
