using Aerochat.Controls;
using Aerochat.Settings;
using Aerochat.Theme;
using Aerochat.Voice;
using Aerochat.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Aerochat.ViewModels
{
    public enum TargetMessageMode
    {
        None,
        Edit,
        Reply,
    }

    public enum DrawingTool
    {
        Pen,      // ペン
        Kesigomu, // 消しゴム
    }

    public class ChatWindowViewModel : ViewModelBase
    {
        public ChatWindowViewModel()
        {
            SettingsManager.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsManager.Instance.DisplayUnimplementedButtons))
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    ShowEyecandy = SettingsManager.Instance.DisplayUnimplementedButtons;
                });
            }
        }

        public List<ToolbarItem> ToolbarItems { get; set; } = new()
        {
            new("Photos", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Photos clicked");

                Chat? chat = Window.GetWindow(itemElement) as Chat;
                if (chat != null)
                {
                    chat.OpenAttachmentsFilePicker();
                }
            }, false, "Share photos"), // add the tooltips when you implement buttons!
            new("Files", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Files clicked");

                Chat? chat = Window.GetWindow(itemElement) as Chat;
                if (chat != null)
                {
                    chat.OpenAttachmentsFilePicker();
                }
            }, false, "Share files"),
            new("Video", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Video clicked");
                OnUmimplementedAction(itemElement);
            }, true),
            new("Call", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Call clicked");
               OnUmimplementedAction(itemElement);
            }, true, "Call a contact"),
            new("Games", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Games clicked");
                OnUmimplementedAction(itemElement);
            }, true),
            new("Activities", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Activities clicked");
                OnUmimplementedAction(itemElement);
            }, true),
            new("Invite", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Invite clicked");
                OnUmimplementedAction(itemElement);
            }, true),
            new("Block", (FrameworkElement itemElement) =>
            {
                Debug.WriteLine("Block clicked");

                var menu = new ContextMenu();
                var menuItem1 = new MenuItem { Header = "Block permanently" };
                menuItem1.Click += (_, __) => OnUmimplementedAction(itemElement);
                var menuItem2 = new MenuItem { Header = "Block and report abuse" };
                menuItem2.Click += (_, __) => OnUmimplementedAction(itemElement);
                menu.Items.Add(menuItem1);
                menu.Items.Add(menuItem2);

                itemElement.ContextMenu = menu;
                menu.PlacementTarget = itemElement;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;

            }, true, "Block this contact from seeing or contacting you")
        };

        /// <summary>
        /// Shows a dialog stating that the toolbar action is unimplemented.
        /// </summary>
        private static void OnUmimplementedAction(FrameworkElement itemElement)
        {
            Dialog dialog = new(
                "Error",
                "This action is currently unimplemented.",
                SystemIcons.Error
            );
            dialog.Owner = Window.GetWindow(itemElement);
            dialog.ShowDialog();
        }

        public ObservableCollection<MessageViewModel> Messages { get; set; } = new();

        private int _topHeight = 80;

        private bool _showEyecandy = SettingsManager.Instance.DisplayUnimplementedButtons;

        public bool ShowEyecandy
        {
            get => _showEyecandy;
            set => SetProperty(ref _showEyecandy, value);
        }

        public int TopHeight
        {
            get => _topHeight;
            set => SetProperty(ref _topHeight, value);
        }

        private int _bottomHeight = 64;

        public int BottomHeight
        {
            get => _bottomHeight;
            set => SetProperty(ref _bottomHeight, value);
        }

        private string _adText = "AIM access for Escargot users only $5!";

        public string AdText
        {
            get => _adText;
            set => SetProperty(ref _adText, value);
        }

        private ChannelViewModel _channel = new()
        {
            Name = "Loading channel...",
            Id = 0,
            Topic = "This channel is loading. Please wait...",
        };
        public ChannelViewModel Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        private bool _isDM = false;
        public bool IsDM
        {
            get => _isDM;
            set => SetProperty(ref _isDM, value);
        }

        private UserViewModel? _recipient;

        public UserViewModel? Recipient
        {
            get => _recipient;
            set => SetProperty(ref _recipient, value);
        }

        private MessageViewModel? _lastReceivedMessage;

        public MessageViewModel? LastReceivedMessage
        {
            get => _lastReceivedMessage;
            set => SetProperty(ref _lastReceivedMessage, value);
        }

        private UserViewModel? _currentUser;

        public UserViewModel? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        private string _typingString = "";

        public string TypingString
        {
            get => _typingString;
            set => SetProperty(ref _typingString, value);
        }

        private int _topHeightMinus10 = 70;

        public int TopHeightMinus10
        {
            get => _topHeightMinus10;
            set => SetProperty(ref _topHeightMinus10, value);
        }

        public ThemeService Theme { get; } = ThemeService.Instance;

        public ObservableCollection<HomeListViewCategory> Categories { get; } = [];

        private bool _loading = false;

        public bool Loading
        {
            get => _loading;
            set => SetProperty(ref _loading, value);
        }

        private GuildViewModel? _guild;

        public GuildViewModel? Guild
        {
            get => _guild;
            set => SetProperty(ref _guild, value);
        }

        private bool _isGroupChat;
        public bool IsGroupChat
        {
            get => _isGroupChat;
            set => SetProperty(ref _isGroupChat, value);
        }

        public VoiceManager VoiceManager { get; } = VoiceManager.Instance;

        private MessageViewModel? _editingMessage;

        public MessageViewModel? TargetMessage
        {
            get => _editingMessage;
            set => SetProperty(ref _editingMessage, value);
        }

        private TargetMessageMode _messageTargetMode;

        public TargetMessageMode MessageTargetMode
        {
            get => _messageTargetMode;
            set => SetProperty(ref _messageTargetMode, value);
        }

        private bool _isShowingAttachmentEditor = false;
        public bool IsShowingAttachmentEditor
        {
            get => _isShowingAttachmentEditor;
            set => SetProperty(ref _isShowingAttachmentEditor, value);
        }

        private DrawingTool _drawingTool = DrawingTool.Pen;

        public DrawingTool DrawingTool
        {
            get => _drawingTool;
            set => SetProperty(ref _drawingTool, value);
        }


        private bool _undoEnabled = false;
        public bool UndoEnabled
        {
            get => _undoEnabled;
            set => SetProperty(ref _undoEnabled, value);
        }

        private bool _redoEnabled = false;
        public bool RedoEnabled
        {
            get => _redoEnabled;
            set => SetProperty(ref _redoEnabled, value);
        }

    }
}
