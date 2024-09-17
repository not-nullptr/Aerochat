using Aerochat.Hoarder;
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

        public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();
        public ObservableCollection<EmbedViewModel> Embeds { get; } = new();

        public static MessageViewModel FromMessage(DiscordMessage message, DiscordMember? member = null)
        {
            var user = UserViewModel.FromUser(message.Author);
            var vm = new MessageViewModel
            {
                Author = user,
                Id = message.Id,
                Timestamp = message.Timestamp.DateTime,
                RawMessage = message.Content,
            };
            foreach (var embed in message.Embeds)
            {
                vm.Embeds.Add(EmbedViewModel.FromEmbed(embed));
            }
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

            foreach (var attachment in message.Attachments)
            {
                vm.Attachments.Add(AttachmentViewModel.FromAttachment(attachment));
            }

            return vm;
        }
    }
}
