using Aerochat.Enums;
using Aerochat.Hoarder;
using Aerochat.Localization;
using Aerochat.Settings;
using Aerochat.Helpers;
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
        private bool _isSelectedForUiAction = false;
        private bool _isTTS = false;
        private bool _isAuthorCurrentUser = false;
        private MessageViewModel? _replyMessage;
        private DiscordMessage _messageEntity;

        /// <summary>True when the message author is the current client user (for bolding in UI).</summary>
        public bool IsAuthorCurrentUser
        {
            get => _isAuthorCurrentUser;
            set => SetProperty(ref _isAuthorCurrentUser, value);
        }

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

        public bool IsTTS
        {
            get => _isTTS;
            set => SetProperty(ref _isTTS, value);
        }

        public bool IsSelectedForUiAction
        {
            get => _isSelectedForUiAction;
            set => SetProperty(ref _isSelectedForUiAction, value);
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

        private string? _timestampString = null;
        private string? _lastMessageReceivedString = null;

        public string TimestampString
        {
            get
            {
                if (_timestampString == null)
                    _timestampString = GetTimestampString();
                return _timestampString;
            }

            set => SetProperty(ref _timestampString, GetTimestampString());
        }

        public string LastMessageReceivedString
        {
            get
            {
                if (_lastMessageReceivedString == null)
                    _lastMessageReceivedString = GetLastMessageReceivedString();
                return _lastMessageReceivedString;
            }

            set => SetProperty(ref _lastMessageReceivedString, GetLastMessageReceivedString());
        }

        private string GetTimestampString()
        {
            var format = SettingsManager.Instance.SelectedTimeFormat == TimeFormat.TwentyFourHour ? "HH:mm" : "h:mm tt";
            return Timestamp.ToString(format, CultureInfo.InvariantCulture);  // Ensure InvariantCulture to control formatting: Otherwise we will lose the AM/PM at the end!
        }

        private string GetLastMessageReceivedString()
        {
            var format = SettingsManager.Instance.SelectedTimeFormat == TimeFormat.TwentyFourHour ? "HH:mm" : "h:mm tt";
            var time = Timestamp.ToString(format, CultureInfo.InvariantCulture);
            return string.Format(Localization.LocalizationManager.Instance["MessageLastReceived"], time, Timestamp.ToString("dd/MM/yyyy"));
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
                MessageEntity = message,
                IsTTS = message.IsTTS,
                IsAuthorCurrentUser = user.Id == Discord.Client.CurrentUser.Id
            };

            if (SettingsManager.Instance.DisplayLinkPreviews)
            {
                foreach (var embed in message.Embeds)
                {
                    vm.Embeds.Add(EmbedViewModel.FromEmbed(embed));
                }
            }

            // TO ANY DEVELOPER LOOKING AT THIS, THINKING "WHERE IS THIS CRASHING??"
            // IT'S PROBABLY THE TIERONE, TIERTWO, OR TIERTHREE USERPREMIUMGUILDSUBSCRIPTIONS!!!!
            var loc = LocalizationManager.Instance;
            var specialMsg = message.MessageType switch
            {
                MessageType.GuildMemberJoin => string.Format(loc["MessageSystemJoined"], user.Name),
                MessageType.UserPremiumGuildSubscription => string.Format(loc["MessageSystemNitroUpgrade"], user.Name),
                MessageType.TierOneUserPremiumGuildSubscription => string.Format(loc["MessageSystemNitroUpgradeTier1"], user.Name),
                MessageType.TierTwoUserPremiumGuildSubscription => string.Format(loc["MessageSystemNitroUpgradeTier2"], user.Name),
                MessageType.TierThreeUserPremiumGuildSubscription => string.Format(loc["MessageSystemNitroUpgradeTier3"], user.Name),
                MessageType.RecipientAdd => string.Format(loc["MessageSystemGroupAdd"], user.Name, message.MentionedUsers.FirstOrDefault()?.Username ?? "?"),
                MessageType.RecipientRemove => string.Format(loc["MessageSystemGroupRemove"], user.Name, message.MentionedUsers.FirstOrDefault()?.Username ?? "?"),
                MessageType.Call => string.Format(loc["MessageSystemGroupCall"], user.Name),
                MessageType.ChannelFollowAdd => string.Format(loc["MessageSystemFollowedChannel"], user.Name, message.Content),
                MessageType.GuildDiscoveryDisqualified => loc["MessageSystemGuildDiscovery"],
                MessageType.GuildDiscoveryRequalified => loc["MessageSystemGuildDiscovery"],
                MessageType.GuildDiscoveryGracePeriodInitialWarning => loc["MessageSystemGuildDiscovery"],
                MessageType.GuildDiscoveryGracePeriodFinalWarning => loc["MessageSystemGuildDiscovery"],
                MessageType.ChannelPinnedMessage => string.Format(loc["MessageSystemPinnedMessage"], user.Name),
                MessageType.ChannelNameChange => string.Format(loc["MessageSystemGroupRename"], user.Name, message.Content),
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
                    vm.Message = user.Id == Discord.Client.CurrentUser.Id
                        ? loc["MessageNudgeSentYou"]
                        : string.Format(loc["MessageNudgeSentOther"], user.Name);
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

            if (vm.IsTTS && SettingsManager.Instance.EnableMessageTts && Discord.Client.CurrentUser.Presence?.Status != UserStatus.DoNotDisturb)
            {
                TextToSpeech.Instance.ReadOutMessage($"{vm.Author.Name} said {message.Content}");
            }

            return vm;
        }
    }
}
