using Aerochat.Windows;
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
    public class HomeListViewCategory : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _isVisibleProperty = false;
        public bool IsVisibleProperty
        {
            get => _isVisibleProperty;
            set => SetProperty(ref _isVisibleProperty, value);
        }

        private bool _collapsed = false;
        public bool Collapsed
        {
            get => _collapsed;
            set => SetProperty(ref _collapsed, value);
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ObservableCollection<HomeListItemViewModel> Items { get; } = new();

        public class HomeListItemViewModel : ViewModelBase
        {
            private string _name;
            private string _image;
            private PresenceViewModel _presence;
            private Action _doubleClick;
            private bool _isSelected;
            private ulong _lastMsgId;
            private ulong _id;
            private bool _isGroupChat;
            private int _recipientCount;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            public string Image
            {
                get => _image;
                set => SetProperty(ref _image, value);
            }

            public PresenceViewModel Presence
            {
                get => _presence;
                set => SetProperty(ref _presence, value);
            }

            public Action DoubleClick
            {
                get => _doubleClick;
                set => SetProperty(ref _doubleClick, value);
            }

            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }

            public ulong LastMsgId
            {
                get => _lastMsgId;
                set => SetProperty(ref _lastMsgId, value);
            }

            public ulong Id
            {
                get => _id;
                set => SetProperty(ref _id, value);
            }

            public bool IsGroupChat
            {
                get => _isGroupChat;
                set => SetProperty(ref _isGroupChat, value);
            }

            public int RecipientCount
            {
                get => _recipientCount;
                set => SetProperty(ref _recipientCount, value);
            }

            public ObservableCollection<DiscordUser> Recipients { get; } = new();

            public ObservableCollection<UserViewModel> ConnectedUsers { get; } = new();
        }
    }
}