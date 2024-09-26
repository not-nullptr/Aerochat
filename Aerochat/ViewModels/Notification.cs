using Aerochat.Windows;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class NotificationWindowViewModel : ViewModelBase
    {
        private MessageViewModel _message = new()
        {
            Author = new()
            {
                Avatar = "/Resources/Frames/PlaceholderPfp.png",
                Id = 0,
                Name = "Some person's incredibly, stupidly long name",
                Username = "SomePerson"
            },
            Id = 0,
            Message = "blah blah blah",
            Timestamp = DateTime.Now,
            RawMessage = "[nudge]",
        };
        public MessageViewModel Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private NotificationType _type = NotificationType.SignOn;
        public NotificationType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private UserViewModel _user = new()
        {
            Avatar = "/Resources/Frames/PlaceholderPfp.png",
            Id = 0,
            Name = "nullptr",
            Username = "notnullptr"
        };

        public UserViewModel User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        private PresenceViewModel _presence;

        public PresenceViewModel Presence
        {
            get => _presence;
            set => SetProperty(ref _presence, value);
        }
    }
}
