using Aerochat.Helpers;
using Aerochat.Hoarder;
using Aerochat.Services;
using Aerochat.Settings;
using Aerochat.Theme;
using Aerochat.ViewModels;
using Aerochat.Voice;
using Aerovoice.Clients;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vanara.PInvoke;
using static Aerochat.ViewModels.HomeListViewCategory;
using static Aerochat.Windows.ToolbarItem;
using static System.Windows.Forms.AxHost;
using static Vanara.PInvoke.DwmApi;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace Aerochat.Windows
{
    public class ToolbarItem(string text, ToolbarItemAction action, bool isEyecandy = false, string hint = "Action unimplemented")
    {
        public string Text { get; set; } = text;
        public string ToolTip { get; set; } = hint;

        public delegate void ToolbarItemAction(FrameworkElement itemElement);

        public ToolbarItemAction Action { get; set; } = action;
    }

    public partial class Chat : Window
    {
        private MediaPlayer chatSoundPlayer = new();
        public DiscordChannel Channel;
        public ulong ChannelId;
        bool isDraggingTopSeparator = false;
        bool isDraggingBottomSeparator = false;
        int initialPos = 0;
        private Dictionary<ulong, Timer> timers = new();
        private VoiceSocket voiceSocket;
        private bool sizeTainted = false;
        private PresenceViewModel? _initialPresence = null;
        private HomeListItemViewModel? _openingItem = null;
        private readonly ChatService _chatService;

        public ObservableCollection<DiscordUser> TypingUsers { get; } = new();
        public ChatWindowViewModel ViewModel { get; set; } = new ChatWindowViewModel();
        public Chat(ulong id, bool allowDefault = false, PresenceViewModel? initialPresence = null, HomeListItemViewModel? openingItem = null, DiscordClient discordClient = null)
        {
            typingTimer.Elapsed += TypingTimer_Elapsed;
            typingTimer.AutoReset = false;
            Hide();
            _initialPresence = initialPresence;
            _openingItem = openingItem;
            _chatService = new ChatService(Discord.Client, new DSharpPlusDiscordApi(Discord.Client));
            if (allowDefault)
            {
                SettingsManager.Instance.SelectedChannels.TryGetValue(id, out ulong channelId);
                if (_chatService.TryGetCachedChannel(channelId, out DiscordChannel channel))
                {
                    ChannelId = id;
                }
                else
                {
                    // get the key of `id` in the dictionary
                    var key = SettingsManager.Instance.SelectedChannels.FirstOrDefault(x => x.Value == id).Key;
                    if (_chatService.TryGetCachedGuild(key, out DiscordGuild guild))
                    {
                        // get the first channel in the guild
                        var firstChannel = guild.Channels.Values.FirstOrDefault(x => x.Type == ChannelType.Text && x.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels));
                        if (firstChannel is not null)
                        {
                            ChannelId = firstChannel.Id;
                        }
                        else
                        {
                            UnavailableDialog();
                            return;
                        }
                    }
                }
            }

            if (ChannelId == 0)
            {
                ChannelId = id;
            }

           
            InitializeComponent();
            DataContext = ViewModel;

            // Ensure that visual elements that aren't supposed to be initially
            // displayed are not initially displayed:
            HideReplyView();
            HideAttachmentsEditor(true);

            Task.Run(BeginDiscordLoop);
            chatSoundPlayer.MediaOpened += (sender, args) =>
            {
                chatSoundPlayer.Play();
            };
            ViewModel.Messages.CollectionChanged += UpdateHiddenInfo;
            TypingUsers.CollectionChanged += TypingUsers_CollectionChanged;

            // (iL - 20.12.2024) Subscribe to settings changes for live update
            SettingsManager.Instance.PropertyChanged += OnSettingsChanged;

            Closing += Chat_Closing;
            Loaded += Chat_Loaded;
            _chatService.TypingStarted += OnType;
            _chatService.MessageCreated += OnMessageCreation;
            _chatService.MessageDeleted += OnMessageDeleted;
            _chatService.MessageUpdated += OnMessageUpdated;
            _chatService.ChannelCreated += OnChannelCreated;
            _chatService.ChannelDeleted += OnChannelDeleted;
            _chatService.ChannelUpdated += OnChannelUpdated;
            _chatService.PresenceUpdated += OnPresenceUpdated;
            _chatService.VoiceStateUpdated += OnVoiceStateUpdated;
            ViewModel.UndoEnabled = false;
            ViewModel.RedoEnabled = false;

            CommandManager.AddPreviewCanExecuteHandler(MessageTextBox, MessageTextBox_OnPreviewCanExecute);
            CommandManager.AddPreviewExecutedHandler(MessageTextBox, MessageTextBox_OnPreviewExecuted);

            PreviewKeyDown += Chat_PreviewKeyDown;
            KeyDown += Chat_KeyDown;

            PART_AttachmentsEditor.ViewModel.Attachments.CollectionChanged
                += OnAttachmentsEditorAttachmentsUpdated;

            RefreshAerochatVersionLinkVisibility();
            STTButton.SetToggle(true);
        }
        public async Task ExecuteNudgePrettyPlease(double initialLeft, double initialTop, double duration = 2, double intensity = 10, bool forceFocus = false)
        {
            double GetRandomNumber(double minimum, double maximum)
            {
                Random random = new Random();
                return random.NextDouble() * (maximum - minimum) + minimum;
            }

            double frequency = 16;
            double steps = duration * 1000 / frequency;
            int stepSize = (int)Math.Floor(frequency);

            for (int i = 0; i < steps; i++)
            {
                double newLeft = initialLeft + GetRandomNumber(-intensity, intensity);
                double newTop = initialTop + GetRandomNumber(-intensity, intensity);

                await Dispatcher.InvokeAsync(() =>
                {
                    Left = newLeft;
                    Top = newTop;
                    WindowState = WindowState.Normal;
                    if (forceFocus)
                    {
                        Activate();
                        Show();
                    }
                });

                await Task.Delay(stepSize);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Left = initialLeft;
                Top = initialTop;
            });
        }

        private void Chat_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
#if !AEROCHAT_RC
                AerochatVersionLink.Text = "Aerochat v" + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3) + " by nullptr";
#else
                AerochatVersionLink.Text = "Aerochat v" + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3) +
                    " " + AssemblyInfo.RC_REVISION + " by nullptr";
#endif

                MouseEnter += (s, e) => SetVisibleProperty(true);
                MouseLeave += (s, e) =>
                {
                    if (!IsActive) SetVisibleProperty(false);
                };
                Activated += (s, e) => SetVisibleProperty(true);
                Deactivated += (s, e) => SetVisibleProperty(false);
            }
            catch (Exception)
            {

            }
        }

        public void SetVisibleProperty(bool prop)
        {
            if (ViewModel?.Categories is null) return;
            foreach (var item in ViewModel.Categories)
            {
                item.IsVisibleProperty = prop;
            }
        }

        public async Task OnChannelChange(bool initial = false)
        {
            if (ViewModel is null || ViewModel?.Messages is null) return;

            // We cannot edit messages or upload images across channels, so close the respective UIs.
            // In the future, this design should possibly be reconsidered: the official Discord client
            // persists such state between channels, and it only applies to the currently-active channel.
            await Dispatcher.BeginInvoke(() =>
            {
                ClearReplyTarget();
                LeaveEditingMessage();
                CloseAttachmentsEditor();
            });

            if (ViewModel.Messages.Count > 0) ViewModel.Messages.Clear();
            ViewModel.Loading = true;

            var newChannel = await _chatService.GetChannelAsync(ChannelId);

            await Dispatcher.BeginInvoke(delegate
            {
                if (ViewModel is null || ViewModel?.Messages is null) return;
                Channel = newChannel;
                ViewModel.Channel = ChannelViewModel.FromChannel(newChannel);
            });

            bool isDM = newChannel is DiscordDmChannel;
            bool isGroupChat = isDM && ((DiscordDmChannel)newChannel).Type == ChannelType.Group;

            // if typing users is not empty clear it
            if (TypingUsers.Count > 0)
            {
                TypingUsers.Clear();
            }

            ViewModel.IsDM = isDM;
            ViewModel.IsGroupChat = isGroupChat;
            var currentUser = await _chatService.GetCurrentUser();
            ViewModel.CurrentUser = UserViewModel.FromUser(currentUser);

            await Dispatcher.BeginInvoke(() =>
            {
                SetIconGCDM(isDM, isGroupChat);
            });

            DiscordUser? recipient = null;
            if (isDM && !isGroupChat)
            {
                recipient = ((DiscordDmChannel)newChannel).Recipients.FirstOrDefault(x => x.Id != currentUser.Id);
                if (!_chatService.TryGetCachedUser(recipient?.Id ?? 0, out recipient) || recipient?.BannerColor == null)
                {
                    // GetUserProfileAsync can fail if the recipient is not friends with you and shares no
                    // mutual servers. We need to fail gracefully in this case.
                    try
                    {
                        DiscordProfile userProfile = await _chatService.GetUserProfileAsync(recipient.Id, true);
                        recipient = userProfile.User;
                    }
                    catch (NotFoundException)
                    {
                        // Ignore.
                    }
                }
            }
            else
            {
                ViewModel.Recipient = new()
                {
                    Avatar = "",
                    Id = 0,
                    Name = "",
                    Username = ""
                };
                ViewModel.Recipient.Scene = ThemeService.Instance.Scene;
            }

            if (isGroupChat)
            {
                await Dispatcher.BeginInvoke(RefreshGroupChat);
            }

            if (recipient is not null) ViewModel.Recipient = UserViewModel.FromUser(recipient);

            if (isDM && !isGroupChat)
            {
                // If we're a one-on-one DM, then we must display the initial presence (as provided from whoever
                // opened this chat window). The Discord API does not report this information to us, so this hack
                // is necessary:
                ViewModel.Recipient.Presence = _initialPresence;
            }

            var messages = await _chatService.GetMessagesAsync(newChannel);
            List<MessageViewModel> messageViewModels = new();

            foreach (var msg in messages)
            {
                var member = msg.Channel.Guild?.Members.FirstOrDefault(x => x.Key == msg.Author.Id).Value;
                MessageViewModel message = MessageViewModel.FromMessage(msg, member);
                messageViewModels.Add(message);
            }

            messageViewModels.Reverse();

            await Dispatcher.BeginInvoke(() =>
            {
                foreach (var msg in messageViewModels)
                {
                    if (ViewModel is null || ViewModel?.Messages is null) return;
                    ViewModel.Messages.Add(msg);
                }
            });


            await Dispatcher.BeginInvoke(delegate
            {
                if (ViewModel is null || ViewModel?.Messages is null) return;
                if (initial)
                {
                    Show();
                }
                MessageTextBox.Focus();
            });

            ViewModel.Loading = false;

            RunGCRelease();
            ProcessLastRead();

            if (!isDM)
            {
                if (SettingsManager.Instance.LastReadMessages.ContainsKey(ChannelId) && Channel.LastMessageId is not null)
                {
                    SettingsManager.Instance.LastReadMessages[ChannelId] = (ulong)Channel.LastMessageId;
                }
                SettingsManager.Save();
            }

            await Dispatcher.BeginInvoke(() =>
            {
                foreach (var window in Application.Current.Windows)
                {
                    if (window is Home home)
                    {
                        home.UpdateUnreadMessages();
                        break;
                    }
                }
            });

            await Dispatcher.BeginInvoke(UpdateChannelListerReadReciepts);
        }

        public void UpdateChannelListerReadReciepts()
        {
            var categories = ViewModel.Categories.ToList();
            Task.Run(() =>
            {
                bool isDM = Channel is DiscordDmChannel;
                if (isDM) return;

                foreach (var category in categories)
                {
                    foreach (var item in category.Items)
                    {
                        _chatService.TryGetCachedChannel(item.Id, out var discordChannel);
                        if (discordChannel == null) continue;

                        bool found = SettingsManager.Instance.LastReadMessages.TryGetValue(discordChannel.Id, out var lastReadMessageId);
                        DateTime lastReadMessageTime;

                        if (found)
                        {
                            lastReadMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)(lastReadMessageId >> 22) + 1420070400000)).DateTime;
                        }
                        else
                        {
                            lastReadMessageTime = SettingsManager.Instance.ReadRecieptReference;
                        }

                        bool isCurrentChannel = discordChannel.Id == ChannelId;
                        var channel = discordChannel;
                        var lastMessageId = channel.LastMessageId;

                        if (channel.Type == ChannelType.Voice)
                        {
                            item.Image = "unread";
                            continue;
                        }
                        if (lastMessageId is null)
                        {
                            item.Image = "read";
                            continue;
                        }

                        var lastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)(lastMessageId >> 22) + 1420070400000)).DateTime;

                        if (lastMessageTime > lastReadMessageTime && !isCurrentChannel)
                        {
                            item.Image = "unread";
                        }
                        else
                        {
                            item.Image = "read";
                            if (isCurrentChannel)
                            {
                                // update the last read message
                                SettingsManager.Instance.LastReadMessages[ChannelId] = lastMessageId ?? 0;
                            }
                        }
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var items = ViewModel.Categories.ToList();
                    ViewModel.Categories.Clear();
                    foreach (var item in items)
                    {
                        ViewModel.Categories.Add(item);
                    }
                });
            });
        }


        public async Task RefreshGroupChat()
        {
            if (ViewModel.Categories.Count > 0) ViewModel.Categories.Clear();
            ViewModel.Categories.Add(new()
            {
                Collapsed = false,
                IsSelected = false,
                IsVisibleProperty = true,
                Name = ""
            });
            var currentUser = await _chatService.GetCurrentUser();
            ViewModel.Categories[0].Items.Add(new()
            {
                Name = currentUser.DisplayName,
                Id = currentUser.Id,
                Image = currentUser.AvatarUrl,
                Presence = currentUser.Presence == null ? null : PresenceViewModel.FromPresence(currentUser.Presence)
            });

            foreach (var rec in ((DiscordDmChannel)Channel).Recipients)
            {
                ViewModel.Categories[0].Items.Add(new()
                {
                    Name = rec.DisplayName,
                    Id = rec.Id,
                    Image = rec.AvatarUrl,
                    Presence = rec.Presence == null ? null : PresenceViewModel.FromPresence(rec.Presence)
                });
            }
        }

        public async Task BeginDiscordLoop()
        {
            try
            {
                await OnChannelChange();
                _chatService.TryGetCachedChannel(ChannelId, out DiscordChannel currentChannel);
                if (currentChannel is null)
                {
                    currentChannel = await _chatService.GetChannelAsync(ChannelId);
                }

                bool isDM = currentChannel is DiscordDmChannel;
                var guild = await _chatService.GetGuild(currentChannel.GuildId ?? 0, isDM, currentChannel);

                await Dispatcher.BeginInvoke(() =>
                {
                    if (!isDM)
                    {
                        ViewModel.Guild = GuildViewModel.FromGuild(guild);
                        RefreshChannelList();
                    }
                });
                await Dispatcher.BeginInvoke(Show);
                if (!isDM) await _chatService.SyncGuildsAsync(guild).ConfigureAwait(false);

                if (isDM)
                {
                    SettingsManager.Instance.RecentDMChats.Remove(ChannelId);
                    SettingsManager.Instance.RecentDMChats.Add(ChannelId);
                }
                else if (ViewModel.Guild is not null)
                {
                    SettingsManager.Instance.RecentServerChats.Remove(ViewModel.Guild.Id);
                    SettingsManager.Instance.RecentServerChats.Add(ViewModel.Guild.Id);
                }

                if (!isDM)
                {
                    SettingsManager.Instance.SelectedChannels[guild.Id] = ChannelId;
                }

                SettingsManager.Save();

                App app = (App)Application.Current;
                try
                {
                    app.RebuildJumpLists();
                }
                catch (Exception)
                {
                    // Ignore. TODO: Logging for meaningless exceptions like this.
                }
            }
            catch (UnauthorizedException e)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(() => ShowErrorDialog("Unauthorized request.\n\nTechnical details: " + e.WebResponse.Response));
            }
            catch (Exception e)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(() => ShowErrorDialog("An unknown error occurred.\n\nTechnical details: " + e.ToString()));
            }
        }

        public void RefreshChannelList()
        {
            if (ViewModel.Categories.Count > 0) ViewModel.Categories.Clear();
            var guild = Channel.Guild;
            if (guild is null) return;
            var currentChannel = Channel;

            List<ChannelType> AllowedChannelTypes = new()
            {
                ChannelType.Text,
                ChannelType.Announcement,
                ChannelType.Voice
            };

            // firstly, get all uncategorized channels
            var uncategorized = guild.Channels.Values
                .Where(x => x.ParentId == null && AllowedChannelTypes.Contains(x.Type) && x.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels))
                .OrderBy(x => x.Position);

            if (uncategorized.Count() > 0)
            {
                ViewModel.Categories.Add(new()
                {
                    Name = "",
                    Collapsed = false,
                    IsSelected = false,
                    IsVisibleProperty = true,
                });

                foreach (var channel in uncategorized)
                {
                    if ((channel.PermissionsFor(guild.CurrentMember) & Permissions.AccessChannels) != Permissions.AccessChannels) continue;
                    ViewModel.Categories[^1].Items.Add(new()
                    {
                        Name = channel.Name,
                        Id = channel.Id,
                        IsSelected = currentChannel.Id == channel.Id
                    });
                    foreach (var voiceState in channel.Guild.VoiceStates.Where(x => x.Value.Channel.Id == channel.Id).ToList().OrderBy(x => x.Value.User.Username))
                    {
                        ViewModel.Categories[^1].Items[^1].ConnectedUsers.Add(UserViewModel.FromUser(voiceState.Value.User));
                    }
                }
            }

            var categories = guild.Channels.Values
                .Where(x => x.Type == ChannelType.Category
                       && (x.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels)
                       && guild.Channels.Values.Where(c =>
                            c.ParentId == x.Id
                            && c.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels)
                            && AllowedChannelTypes.Contains(c.Type)
                       ).Count() > 0 || guild.Channels.Values.Where(c =>
                            c.ParentId == x.Id
                            && c.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels)
                            && AllowedChannelTypes.Contains(c.Type)
                       ).Count() > 0))
                .OrderBy(x => x.Position);

            foreach (var category in categories)
            {
                ViewModel.Categories.Add(new()
                {
                    Name = category.Name,
                    Collapsed = false,
                    IsSelected = false,
                    IsVisibleProperty = true,
                });

                var channels = guild.Channels.Values
                    .Where(x => x.ParentId == category.Id && AllowedChannelTypes.Contains(x.Type))
                    .OrderBy(x => x.Position);

                foreach (var channel in channels)
                {
                    if ((channel.PermissionsFor(guild.CurrentMember) & Permissions.AccessChannels) != Permissions.AccessChannels) continue;
                    ViewModel.Categories[^1].Items.Add(new()
                    {
                        Name = channel.Name,
                        Id = channel.Id,
                        IsSelected = currentChannel.Id == channel.Id
                    });
                    foreach (var voiceState in channel.Guild.VoiceStates.Where(x => x.Value.Channel.Id == channel.Id).ToList().OrderBy(x => x.Value.User.Username))
                    {
                        ViewModel.Categories[^1].Items[^1].ConnectedUsers.Add(UserViewModel.FromUser(voiceState.Value.User));
                    }
                }
            }

            Dispatcher.BeginInvoke(UpdateChannelListerReadReciepts);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                if (Channel?.Guild?.Channels?.Select(x => x.Key).ToList().Contains(VoiceManager.Instance.Channel?.Id ?? 0) ?? false)
                {
                    Task.Run(VoiceManager.Instance.LeaveVoiceChannel);
                }
            }
            catch (Exception) { }
            base.OnClosing(e);
            _chatService.TypingStarted -= OnType;
            _chatService.MessageCreated -= OnMessageCreation;
            // dispose of the chat
            ViewModel.Messages.Clear();
            TypingUsers.Clear();
            foreach (var t in timers)
            {
                t.Value.Stop();
                t.Value.Dispose();
            }
            timers.Clear();
            chatSoundPlayer.Stop();
            chatSoundPlayer.Close();
            System.Timers.Timer timer = new(2000);
            timer.Elapsed += GCRelease;
            timer.Start();
        }

        private async Task OnMessageCreation(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
        {
            if (args.Channel.Id != ChannelId) return;
            args.Channel.GetType().GetProperty("LastMessageId")?.SetValue(args.Channel, args.Message.Id);
            //Dispatcher.BeginInvoke(UpdateChannelListerReadReciepts);
            bool isNudge = args.Message.Content == "[nudge]";
            DiscordUser user = args.Author;
            if (user is null) return;

            var member = args.Guild?.Members.FirstOrDefault(x => x.Key == args.Author.Id).Value;

            MessageViewModel message = MessageViewModel.FromMessage(args.Message, member);

            MessageViewModel? eph = ViewModel.Messages.FirstOrDefault(x => x.Ephemeral && x.Message == message.Message);

            int messageIndex = -1;

            if (eph != null)
            {
                messageIndex = ViewModel.Messages.IndexOf(eph);
            }

            await Dispatcher.BeginInvoke(() =>
            {
                if (messageIndex > -1)
                {
                    try
                    {
                        ViewModel.Messages.RemoveAt(messageIndex);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // We literally couldn't care less. This case should never crash the program.
                    }
                }

                ViewModel.LastReceivedMessage = message;
                ViewModel.Messages.Add(message);

                // Limit messages to 50
                while (ViewModel.Messages.Count > 50)
                {
                    ViewModel.Messages.RemoveAt(0);
                }
            });

            if (TypingUsers.Contains(args.Author))
            {
                TypingUsers.Remove(args.Author);
            }

            await Dispatcher.BeginInvoke(() =>
            {
                if (_chatService.GetCurrentUser().Result.Presence.Status == UserStatus.DoNotDisturb)
                {
                    return;
                }

                if (isNudge)
                {
                    chatSoundPlayer.Open(new Uri("Resources/Sounds/nudge.wav", UriKind.Relative));
                }
                else
                {
                    if (IsActive && message.Author?.Id != _chatService.GetCurrentUser().Result.Id)
                    {
                        if (SettingsManager.Instance.NotifyChat || message.MessageEntity.MentionedUsers.Contains(_chatService.GetCurrentUser().Result)) // IDK
                            chatSoundPlayer.Open(new Uri("Resources/Sounds/type.wav", UriKind.Relative));
                    }
                }
            });
        }

        private void ProcessLastRead()
        {
            Dispatcher.BeginInvoke(() =>
            {
                var message = ViewModel.Messages.LastOrDefault();
                // if its ephemeral return
                if (message == null || message.Ephemeral) return;
                // if if VisualChildrenCount returns zero don't
                if (VisualTreeHelper.GetChildrenCount(MessagesListItemsControl) == 0) return;
                ScrollViewer scrollViewer = VisualTreeHelper.GetChild(MessagesListItemsControl, 0) as ScrollViewer;
                if (scrollViewer != null && IsActive)
                {
                    if (!SettingsManager.Instance.LastReadMessages.TryGetValue(ChannelId, out var msgId))
                    {
                        SettingsManager.Instance.LastReadMessages[ChannelId] = message.Id ?? 0;
                    }
                    else
                    {
                        long prevTimestamp = ((long)(msgId >> 22)) + 1420070400000;
                        DateTime prevLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(prevTimestamp).DateTime;

                        long nextTimestamp = ((long)(message.Id! >> 22)) + 1420070400000;
                        DateTime nextLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(nextTimestamp).DateTime;

                        if (prevLastMessageTime < nextLastMessageTime)
                        {
                            SettingsManager.Instance.LastReadMessages[ChannelId] = message.Id ?? 0;
                        }
                    }

                    SettingsManager.Save();

                    foreach (var window in Application.Current.Windows)
                    {
                        if (window is Home home)
                        {
                            home.UpdateUnreadMessages();
                            break;
                        }
                    }

                }
            });
            Dispatcher.BeginInvoke(UpdateChannelListerReadReciepts);
        }

        private async Task OnType(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.TypingStartEventArgs args)
        {
            if (args.Channel.Id != ChannelId) return;
            if (args.User.Id == _chatService.GetCurrentUser().Result.Id) return;
            await Dispatcher.BeginInvoke(() =>
            {
                if (timers.TryGetValue(args.User.Id, out System.Timers.Timer? timer))
                {
                    timer?.Stop();
                    timer?.Start();
                }
                else
                {
                    System.Timers.Timer newTimer = new(10000);
                    newTimer.Elapsed += (s, e) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            TypingUsers.Remove(args.User);
                            newTimer.Stop();
                            newTimer.Dispose();
                        });
                        timers.Remove(args.User.Id);
                    };
                    newTimer.AutoReset = false;
                    newTimer.Start();
                    timers.Add(args.User.Id, newTimer);
                }
                // if typingusers includes this user don't add
                if (!TypingUsers.Contains(args.User))
                    TypingUsers.Add(args.User);
            });

        }

        public void UnavailableDialog()
        {
            ShowErrorDialog("This server is unavailable right now. Please try again later.", "Server unavailable");
        }

        public void ShowErrorDialog(string message, string title = "Error", Icon? icon = null)
        {
            if (icon == null)
            {
                icon = SystemIcons.Error;
            }

            Show();
            Visibility = Visibility.Hidden;
            var dialog = new Dialog(title, message, icon);
            dialog.Owner = this;
            dialog.ShowDialog();
            Close();
        }



        private void Chat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V) // Ctrl+V
                {
                    if (Clipboard.ContainsImage())
                    {
                        e.Handled = true;
                        AddImageAttachmentFromClipboard();
                    }
                    else
                    {
                        // Forward the input into the chat box:
                        e.Handled = true;
                        MessageTextBox.RaiseEvent(new KeyEventArgs(
                            e.KeyboardDevice,
                            PresentationSource.FromVisual(MessageTextBox),
                            0,
                            e.Key
                        )
                        { RoutedEvent = Keyboard.KeyDownEvent });
                        MessageTextBox.Focus();
                    }
                }
                else if (e.Key == Key.A) // Ctrl+A
                {
                    e.Handled = true;
                    MessageTextBox.Focus();
                    MessageTextBox.SelectAll();
                }
            }
        }

        private void AddImageAttachmentFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource image = Clipboard.GetImage();
                InsertAttachmentsFromAnyThreadFromBitmapSourceArray([image]);
            }
        }

        private void Chat_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ViewModel.MessageTargetMode == TargetMessageMode.Edit)
                {
                    // Leave without submission:
                    LeaveEditingMessage();
                }
                else if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
                {
                    // Unselect the current message:
                    ClearMessageSelection();
                }
                else if (ViewModel.IsShowingAttachmentEditor)
                {
                    bool isAnyPopupVisible = false;
                    foreach (var attachment in PART_AttachmentsEditor.ViewModel.Attachments)
                    {
                        isAnyPopupVisible |= attachment.Selected;
                    }

                    // Only close the attachments editor if edit or reply have
                    // already been left.
                    if (!isAnyPopupVisible)
                        CloseAttachmentsEditor();
                }
            }
            else if (e.Key == Key.Enter && (!e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.LeftShift)) && !e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.RightShift))))
            {
                // Send the current message from the chatbox:
                SendMessageFromChatBox();
            }
            else if (
                MessageTextBox.IsEnabled &&
                !MessageTextBox.IsKeyboardFocused &&
                FocusManager.GetFocusedElement(this) is not TextBox &&
                (e.KeyboardDevice.Modifiers == ModifierKeys.Shift ||
                e.KeyboardDevice.Modifiers == ModifierKeys.None)
            )
            {
                // If we're some generally typable character, then focus the chat box
                // and let the key event bubble to the text box:
                MessageTextBox.Focus();
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsManager.Instance.SelectedTimeFormat))
            {
                Dispatcher.BeginInvoke(() =>
                {
                    foreach (var message in ViewModel.Messages)
                    {
                        // Update each message
                        var temp = message.TimestampString;
                        message.TimestampString = null;
                        message.TimestampString = temp;
                        message.RaisePropertyChanged(nameof(message.TimestampString));
                    }

                    if (ViewModel.LastReceivedMessage is not null)
                    {
                        var lastMessage = ViewModel.LastReceivedMessage;
                        lastMessage.LastMessageReceivedString = "(Ignored)"; // Setter creates the value
                        lastMessage.RaisePropertyChanged(nameof(lastMessage.LastMessageReceivedString));
                    }

                    RefreshAerochatVersionLinkVisibility();
                });
            }
        }

        private void RefreshAerochatVersionLinkVisibility()
        {
            AerochatVersionLink.Visibility = SettingsManager.Instance.DisplayAerochatAttribution
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async Task OnVoiceStateUpdated(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            if (args.Guild.Id != Channel.Guild?.Id) return;
            Dispatcher.BeginInvoke(RefreshChannelList);
        }

        private async Task OnPresenceUpdated(DiscordClient sender, DSharpPlus.EventArgs.PresenceUpdateEventArgs args)
        {
            if (ViewModel.IsGroupChat)
            {
                var gChannel = Channel as DiscordDmChannel;
                var recipient = gChannel?.Recipients.FirstOrDefault(x => x.Id == args.User.Id);
                if (recipient is null) return;
                var cat = ViewModel.Categories[0];
                var item = cat.Items.FirstOrDefault(x => x.Id == recipient.Id);
                if (item is null) return;
                item.Presence = PresenceViewModel.FromPresence(args.PresenceAfter);
            }
            else
            {
                if (args.User.Id != ViewModel.Recipient?.Id) return;
                ViewModel.Recipient.Presence = PresenceViewModel.FromPresence(args.PresenceAfter);
            }
        }

        private async Task OnChannelUpdated(DiscordClient sender, DSharpPlus.EventArgs.ChannelUpdateEventArgs args)
        {
            if (args.ChannelAfter.Guild?.Id != Channel.Guild?.Id || Channel.Guild == null) return;
            Dispatcher.BeginInvoke(RefreshChannelList);
        }

        private async Task OnChannelDeleted(DiscordClient sender, DSharpPlus.EventArgs.ChannelDeleteEventArgs args)
        {
            if (args.Channel.Guild?.Id != Channel.Guild?.Id || Channel.Guild == null) return;
            Dispatcher.BeginInvoke(RefreshChannelList);
            if (args.Channel.Id == Channel.Id)
            {
                var newChannel = ViewModel.Categories.ElementAt(0).Items.ElementAt(0);
                ChannelId = newChannel.Id;
                newChannel.IsSelected = true;
                Dispatcher.BeginInvoke(() => OnChannelChange());
            }
        }

        private async Task OnChannelCreated(DiscordClient sender, DSharpPlus.EventArgs.ChannelCreateEventArgs args)
        {
            if (args.Channel.Guild?.Id != Channel.Guild?.Id || Channel.Guild == null) return;
            Dispatcher.BeginInvoke(RefreshChannelList);
        }

        private async Task OnMessageUpdated(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs args)
        {
            // get the message from the collection
            _ = Dispatcher.BeginInvoke(() =>
            {
                if (ViewModel is null) return;
                var message = ViewModel.Messages.FirstOrDefault(x => x.Id == args.Message.Id);
                if (message is not null)
                {
                    message.Message = args.Message.Content;

                    // The body of the message entity changes, but this is ordinarily invisible to
                    // WPF. RaisePropertyChanged is useless, so I just opted for swapping the value
                    // and relying on the assignment operator overload or whatever.
                    var temp = message.MessageEntity;
                    message.MessageEntity = null!;
                    message.MessageEntity = temp;

                    message.RaisePropertyChanged(nameof(message.MessageEntity));
                    message.Embeds.Clear();
                    foreach (var embed in args.Message.Embeds)
                    {
                        message.Embeds.Add(EmbedViewModel.FromEmbed(embed));
                    }
                }
            });
        }

        private async Task OnMessageDeleted(DiscordClient sender, DSharpPlus.EventArgs.MessageDeleteEventArgs args)
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                if (ViewModel is null) return;
                var message = ViewModel.Messages.FirstOrDefault(x => x.Id == args.Message.Id);
                if (message is not null)
                {
                    // this is such a terrible solution i'm so sorry
                    ViewModel.Messages.Remove(message);
                    foreach (MessageViewModel item in ViewModel.Messages)
                    {
                        int index = ViewModel.Messages.IndexOf(item);
                        if (index == -1) return;
                        if (index == 0)
                        {
                            item.HiddenInfo = false;
                            continue;
                        }
                        MessageViewModel previous = ViewModel.Messages[index - 1];
                        if (item.Special)
                        {
                            item.HiddenInfo = previous.Special;
                            continue;
                        }

                        item.HiddenInfo = previous.Author?.Id == item.Author?.Id && !previous.Special;

                    }

                    // Reset the "Last message received ..." string, as the 12/24 hour
                    // clock setting may have changed
                    if (ViewModel.LastReceivedMessage is not null)
                    {
                        ViewModel.LastReceivedMessage.LastMessageReceivedString = "(Ignored)";
                    }
                }
            });
        }

        private void Chat_Closing(object? sender, CancelEventArgs e)
        {
            // clear everything up
            ViewModel.Messages.CollectionChanged -= UpdateHiddenInfo;
            TypingUsers.CollectionChanged -= TypingUsers_CollectionChanged;
            foreach (var t in timers)
            {
                t.Value.Stop();
                t.Value.Dispose();
            }
            timers.Clear();

            chatSoundPlayer.Stop();
            chatSoundPlayer.Close();

            _chatService.TypingStarted -= OnType;
            _chatService.MessageCreated -= OnMessageCreation;
            _chatService.MessageDeleted -= OnMessageDeleted;
            _chatService.MessageUpdated -= OnMessageUpdated;
            _chatService.ChannelCreated -= OnChannelCreated;
            _chatService.ChannelDeleted -= OnChannelDeleted;
            _chatService.ChannelUpdated -= OnChannelUpdated;
            _chatService.PresenceUpdated -= OnPresenceUpdated;
            _chatService.VoiceStateUpdated -= OnVoiceStateUpdated;

            ViewModel.Messages.Clear();
            TypingUsers.Clear();

            System.Timers.Timer timer = new(2000);
            timer.Elapsed += GCRelease;
            timer.Start();
        }

        private void GCRelease(object sender, ElapsedEventArgs e)
        {
            RunGCRelease();
            ((System.Timers.Timer)sender).Stop();
            ((System.Timers.Timer)sender).Dispose();
        }

        private void RunGCRelease()
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private async void TypingUsers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            List<DiscordUser> tempUsers = new();
            foreach (var user in TypingUsers.ToList())
            {
                if (!_chatService.TryGetCachedUser(user.Id, out DiscordUser discordUser))
                {
                    // I believe this is fully safe since it'll only occur in a server context,
                    // where all typing users' profiles should be fully accessible.
                    discordUser = (await _chatService.GetUserProfileAsync(user.Id, true)).User;
                }
                tempUsers.Add(discordUser);
            }
            ViewModel.TypingString = tempUsers.Count switch
            {
                0 => "",
                1 => $"{tempUsers[0].DisplayName} is writing...",
                2 => $"{tempUsers[0].DisplayName} and {tempUsers[1].DisplayName} are writing...",
                _ => $"{tempUsers[0].DisplayName} and {tempUsers.Count - 1} others are writing..."
            };
        }

        private void UpdateHiddenInfo(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (MessageViewModel item in e.NewItems)
                {
                    // get the index of the item through the id
                    int index = ViewModel.Messages.IndexOf(item);
                    if (index == -1) return;
                    if (index == 0)
                    {
                        item.HiddenInfo = false;
                        continue;
                    }
                    MessageViewModel previous = ViewModel.Messages[index - 1];
                    if (item.Special)
                    {
                        item.HiddenInfo = previous.Special;
                        continue;
                    }

                    item.HiddenInfo = previous.Author?.Id == item.Author?.Id && !previous.Special;

                }
            }
            // set ViewModel.LastMessage to the last message in the collection
            if (ViewModel.Messages.Count > 0) ViewModel.LastReceivedMessage = ViewModel.Messages[^1];
        }


        void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
            Graphics desktop = Graphics.FromHwnd(hwnd);
            float DesktopDpiX = desktop.DpiX;
            float DesktopDpiY = desktop.DpiY;
            MARGINS margin = new(2, 2, 0, 56);
            margin.cxLeftWidth = (int)(margin.cxLeftWidth * DesktopDpiX / 96);
            margin.cxRightWidth = (int)(margin.cxRightWidth * DesktopDpiX / 96);
            margin.cyTopHeight = (int)(margin.cyTopHeight * DesktopDpiY / 96);
            margin.cyBottomHeight = (int)(margin.cyBottomHeight * DesktopDpiY / 96);
            DwmExtendFrameIntoClientArea(hwnd, in margin);

        }

        private async Task SendMessage(
    string value,
    Stream? drawingAttachment = null,
    int attachmentWidth = 0,
    int attachmentHeight = 0)
        {
            // 1) Resolve guild & current user via the service
            bool isDM = Channel is DiscordDmChannel;

            DiscordGuild? guild = null;
            if (!isDM)
            {
                var gid = Channel.GuildId ?? 0;
                if (!_chatService.TryGetCachedGuild(gid, out guild))
                {
                    guild = await _chatService.GetGuild(gid, isDM: false, Channel);
                }
            }

            var me = await _chatService.GetCurrentUser();
            // 2) UX: scroll to bottom before adding the local "pending" message
            if (VisualTreeHelper.GetChildrenCount(MessagesListItemsControl) > 0)
            {
                if (VisualTreeHelper.GetChild(MessagesListItemsControl, 0) is ScrollViewer sv)
                {
                    sv.ScrollToBottom();
                }
            }

            // 3) Build a local ephemeral/pending message (reflection hack kept)
            var msgType = typeof(DiscordMessage);
            var ctor = msgType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: System.Type.EmptyTypes,
                modifiers: null);

            if (ctor is null) throw new Exception("DiscordMessage private constructor not found.");

            var fakeMsg = (DiscordMessage)ctor.Invoke(null);
            fakeMsg.GetType().GetProperty("Content")?.SetValue(fakeMsg, value);
            fakeMsg.GetType().GetProperty("Author")?.SetValue(fakeMsg, me);
            fakeMsg.GetType().GetProperty("Channel")?.SetValue(fakeMsg, Channel);
            if (guild is not null)
                fakeMsg.GetType().GetProperty("Guild")?.SetValue(fakeMsg, guild);
            fakeMsg.GetType().GetProperty("_timestampRaw")?.SetValue(fakeMsg, DateTime.Now);
            fakeMsg.GetType().GetProperty("Id")?.SetValue(fakeMsg, (ulong)0);
            fakeMsg.GetType().GetProperty("Type")?.SetValue(fakeMsg, MessageType.Default);
            fakeMsg.GetType().GetProperty("_mentionedUsers")?.SetValue(fakeMsg, new List<DiscordUser>());
            fakeMsg.GetType().GetProperty("_mentionedRoles")?.SetValue(fakeMsg, new List<DiscordRole>());
            fakeMsg.GetType().GetProperty("_mentionedChannels")?.SetValue(fakeMsg, new List<DiscordChannel>());

            var pendingVm = new MessageViewModel
            {
                Author = isDM ? UserViewModel.FromUser(me) : UserViewModel.FromMember(guild!.CurrentMember),
                Message = value == "[nudge]" ? "You have just sent a nudge." : value,
                Timestamp = DateTime.Now,
                Id = 0,
                Ephemeral = true,
                Special = value == "[nudge]",
                MessageEntity = fakeMsg,
            };

            ViewModel.Messages.Add(pendingVm);

            // Show placeholder attachments on the pending bubble
            if (drawingAttachment != null)
            {
                ViewModel.Messages[^1].Attachments.Add(new AttachmentViewModel
                {
                    Id = 0,
                    Width = attachmentWidth,
                    Height = attachmentHeight,
                    MediaType = Aerochat.Enums.MediaType.Image,
                    Url = "",
                    Name = "attachment.png",
                    Size = "Uploading..."
                });
            }
            else if (ViewModel.IsShowingAttachmentEditor)
            {
                for (int i = 0; i < Math.Min(10, PART_AttachmentsEditor.ViewModel.Attachments.Count); i++)
                {
                    var attachment = PART_AttachmentsEditor.ViewModel.Attachments[i];
                    ViewModel.Messages[^1].Attachments.Add(new AttachmentViewModel
                    {
                        Id = 0,
                        Width = 0,
                        Height = 0,
                        MediaType = Aerochat.Enums.MediaType.Unknown,
                        Url = "",
                        Name = attachment.FileName,
                        Size = "Uploading..."
                    });
                }
            }

            // 4) Build the outbound Discord message
            try
            {
                var builder = new DiscordMessageBuilder().WithContent(value);

                if (ViewModel.MessageTargetMode == TargetMessageMode.Reply && ViewModel.TargetMessage != null)
                {
                    builder.WithReply(
                        ViewModel.TargetMessage.Id,
                        PART_ReplyContainerMention.IsChecked ?? false
                    );
                }

                if (drawingAttachment != null)
                {
                    if (drawingAttachment.CanSeek) drawingAttachment.Position = 0;
                    builder.AddFile("attachment.png", drawingAttachment);
                }
                else if (ViewModel.IsShowingAttachmentEditor)
                {
                    for (int i = 0; i < Math.Min(10, PART_AttachmentsEditor.ViewModel.Attachments.Count); i++)
                    {
                        var a = PART_AttachmentsEditor.ViewModel.Attachments[i];
                        try
                        {
                            var stream = a.GetStream();
                            if (stream.CanSeek) stream.Position = 0;

                            builder.AddFile(
                                (a.MarkAsSpoiler ? "SPOILER_" : "") + a.FileName,
                                stream
                            );
                        }
                        catch
                        {
                            var errorDialog = new Dialog(
                                "Error",
                                $"Failed to upload attachment #{i} \"{a.FileName}\".",
                                SystemIcons.Warning
                            )
                            { Owner = this };
                            errorDialog.ShowDialog();
                            return;
                        }
                    }
                }

                if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
                {
                    ClearReplyTarget();
                }

                CloseAttachmentsEditor();

                // 5) Send via the service (do NOT recreate ChatService)
                var result = await _chatService.SendAsync(Channel.Id, builder);

                if (!result.Success)
                {
                    // Drop pending message on error
                    var idx = ViewModel.Messages.IndexOf(pendingVm);
                    if (idx >= 0) ViewModel.Messages.RemoveAt(idx);

                    var msg = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Unknown error."
                        : result.ErrorMessage;

                    var errorDialog = new Dialog(
                        "Failed to send message",
                        $"{result.ErrorCode ?? "Error"}: {msg}",
                        SystemIcons.Error
                    )
                    { Owner = this };
                    errorDialog.ShowDialog();
                }
                // else: the real message will appear via MessageCreated; we keep it simple.
            }
            catch
            {
                // Drop the pending bubble if anything threw
                var idx = ViewModel.Messages.IndexOf(pendingVm);
                if (idx >= 0) ViewModel.Messages.RemoveAt(idx);
            }
        }


        private async void SendMessageFromChatBox()
        {
            if (Channel is null)
                return;

            string value = GetMessageBoxText();

            if (value.Trim() == string.Empty && (!ViewModel.IsShowingAttachmentEditor
                || PART_AttachmentsEditor.ViewModel.Attachments.Count == 0))
            {
                return;
            }

            MessageTextBox.Document.Blocks.Clear();
            ViewModel.BottomHeight = 64;

            if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
            {
                // Since the editor UI is still open when we send the message,
                // we need to account for its added height, or we subtract the
                // wrong value when closing the UI.
                ViewModel.BottomHeight += 25;
            }

            sizeTainted = false;

            if (ViewModel.MessageTargetMode == TargetMessageMode.Edit)
            {
                await ViewModel.TargetMessage.MessageEntity.ModifyAsync(value);
                LeaveEditingMessage();
            }
            else
            {
                await SendMessage(value);
            }
        }

        private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                SendMessageFromChatBox();
            }
        }

        private void MessageTextBox_OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (Clipboard.ContainsImage())
                {
                    AddImageAttachmentFromClipboard();
                    e.Handled = true;
                }
            }
        }

        private void MessageTextBox_OnPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (Clipboard.ContainsImage())
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Unselect all the text in the message box. This ensures that the caret ends up
            // at the end of the previous selection rather than the beginning, which is the
            // more expected behaviour of a chatbox.
            TextPointer end = MessageTextBox.Selection.End;
            MessageTextBox.CaretPosition = end;
            MessageTextBox.Selection.Select(end, end);
        }

        private void ToolbarClick(object sender, MouseButtonEventArgs e)
        {
            Grid grid = (Grid)sender;
            if (grid.DataContext is ToolbarItem toolbarItem)
            {
                toolbarItem.Action((FrameworkElement)sender);
            }
        }

        private void HiddenItemsClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContextMenu contextMenu = (ContextMenu)FindName("HiddenItemsContextMenu");
            ItemsControl itemsControl = (ItemsControl)FindName("ToolbarOverflowPanel");
            bool hiddenItems = false;
            foreach (ToolbarItem item in itemsControl.Items)
            {
                var child = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (child.Visibility == Visibility.Hidden)
                {
                    hiddenItems = true;
                }
            }
            // get ExpandBtn
            Grid expandBtn = (Grid)FindName("ExpandBtn");
            if (hiddenItems)
            {
                expandBtn.Visibility = Visibility.Visible;
            }
            else
            {
                expandBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void Separator_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingTopSeparator = true;
            initialPos = (int)e.GetPosition(this).Y;
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingTopSeparator = false;
            isDraggingBottomSeparator = false;
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // drag the ViewModel.TopHeight
            var pos = (int)e.GetPosition(this).Y;
            if (isDraggingTopSeparator)
            {
                ViewModel.TopHeight += pos - initialPos;
                ViewModel.TopHeightMinus10 = ViewModel.TopHeight - 10;
                initialPos = pos;
            }

            if (isDraggingBottomSeparator)
            {
                ViewModel.BottomHeight -= pos - initialPos;
                int min = 64;

                if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
                {
                    min += 25;
                }

                int max = 200;
                ViewModel.BottomHeight = Math.Clamp(ViewModel.BottomHeight, min, max);
                if (ViewModel.BottomHeight != min && ViewModel.BottomHeight != max) initialPos = pos;
                sizeTainted = true;
            }
        }

        private bool AutoScroll = true;

        private void MessagesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = VisualTreeHelper.GetChild(MessagesListItemsControl, 0) as ScrollViewer;
            if (scrollViewer is null) return;
            if (e.ExtentHeightChange == 0)
            {
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {
                    AutoScroll = true;
                }
                else
                {
                    AutoScroll = false;
                }
            }

            if (AutoScroll && e.ExtentHeightChange != 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }

        private void BottomSeparator_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingBottomSeparator = true;
            initialPos = (int)e.GetPosition(this).Y;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = sender as ScrollViewer;
            if (scv == null) return;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ItemToggleCollapse(object sender, MouseButtonEventArgs e)
        {
            var item = (HomeListViewCategory)((Image)sender).DataContext;
            item.Collapsed = !item.Collapsed;
        }

        private async void ItemClick(object sender, MouseButtonEventArgs e)
        {
            HomeListItemViewModel? prev = null;
            // set all items to not selected
            foreach (var i in ViewModel.Categories)
            {
                i.IsSelected = false;
                foreach (var x in i.Items)
                {
                    if (x.IsSelected) prev = x;
                    x.IsSelected = false;
                }
            }
            // get the data context of the clicked item
            var item = (dynamic)((Grid)sender).DataContext;
            if (item is HomeListViewCategory)
            {
                item.IsSelected = true;
            }
            else if (item is HomeListItemViewModel)
            {
                // get the channel
                if (!_chatService.TryGetCachedChannel(item.Id, out DiscordChannel channel)) return;
                switch (channel.Type)
                {
                    case ChannelType.Voice:
                        if (prev is not null) prev.IsSelected = true;
                        if (!SettingsManager.Instance.HasWarnedAboutVoiceChat)
                        {
                            SettingsManager.Instance.HasWarnedAboutVoiceChat = true;
                            SettingsManager.Save();
                            var dialog = new Dialog("Call warning", "Calling is currently in beta and WILL PROBABLY CRASH YOUR CLIENT. It uses your default microphone and speakers in the Windows settings, so please make sure those are properly configured. This warning will not be shown again; click the call again to join.", SystemIcons.Warning);
                            dialog.Owner = this;
                            dialog.ShowDialog();
                            return;
                        }
                        try
                        {
                            await VoiceManager.Instance.JoinVoiceChannel(channel);
                        }
                        catch (Exception ex)
                        {
                            Dialog errorDialog = new(
                                "Error",
                                "Failed to open voice channel.\n\nTechnical details: " + ex.ToString(),
                                SystemIcons.Error
                            );
                            errorDialog.Owner = this;
                            errorDialog.ShowDialog();
                            return;
                        }
                        break;
                    default:
                        item.IsSelected = true;
                        ChannelId = item.Id;
                        await OnChannelChange();
                        break;
                }
            }
        }

        private async void RunNudge(object sender, MouseButtonEventArgs e)
        {
            await SendMessage("[nudge]");
            ExecuteNudgePrettyPlease(Left, Top, SettingsManager.Instance.NudgeLength, SettingsManager.Instance.NudgeIntensity).ConfigureAwait(false);
        }

        private void OpenEmojiFlyout(object sender, MouseButtonEventArgs e)
        {
            EmojiFlyout.IsOpen = true;
            var imageFiles = EmojiDictionary.Map.Values.Distinct().ToList();
            int index = 0;

            foreach (var imageName in imageFiles)
            {
                if (index >= PinnedEmojiGrid.Children.Count) break;

                var border = PinnedEmojiGrid.Children[index] as Border;
                Image img = new Image();

                if (border != null)
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/Emoji/{imageName}"));
                    img.Stretch = Stretch.Uniform;
                    img.Margin = new Thickness(3);
                    img.Tag = EmojiDictionary.Map.FirstOrDefault(kvp => kvp.Value == imageName).Key;
                    border.Child = img;
                }

                index++;
            }
        }

        private void EmojiBox_Click(object sender, MouseButtonEventArgs e)
        {
            Image imgInside = ((Border)sender).Child as Image;
            Image newImg = new Image
            {
                Source = imgInside.Source,
                Width = 16,
                Height = 16
            };

            InlineUIContainer container = new InlineUIContainer(newImg);
            container.Tag = imgInside.Tag;

            Paragraph paragraph = MessageTextBox.Document.Blocks.LastBlock as Paragraph;
            if (paragraph == null)
            {
                paragraph = new Paragraph();
                MessageTextBox.Document.Blocks.Add(paragraph);
            }
            paragraph.Inlines.Add(container);

            MessageTextBox.CaretPosition = paragraph.ContentEnd;
            MessageTextBox.Focus();
        }

        private void JumpToReply(object sender, MouseButtonEventArgs e)
        {
            var messageVm = (sender as Panel)?.DataContext as MessageViewModel;
            if (messageVm is null || !messageVm.IsReply || messageVm.ReplyMessage is null) return;

            var replyId = messageVm.ReplyMessage.Id;
            for (var i = 0; i < MessagesListItemsControl.Items.Count; i++)
            {
                var item = MessagesListItemsControl.Items[i] as MessageViewModel;
                if (item is null) return;

                if (item.Id != replyId) continue;

                var container = MessagesListItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container is null) return;

                container.BringIntoView();
                return;
            }
        }

        /// <summary>
        /// Opens an external URL with the default protocol handler on the user's operating system.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>True if succeeded, false if failed.</returns>
        private bool OpenExternalUrl(string uri)
        {
            // Make sure we're trying to open an actual URL and not any arbitrary program.
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;

            Shell32.ShellExecute(HWND.NULL, "open", uri, null, null, ShowWindowCommand.SW_SHOWNORMAL);
            return true;
        }

        private void EmojiFlyout_Closed(object sender, EventArgs e)
        {
            EmojiButtonGrid.SetToggle(false);
        }

        private async void MessageParser_HyperlinkClicked(object sender, Controls.HyperlinkClickedEventArgs e)
        {
            switch (e.Type)
            {
                case Controls.HyperlinkType.WebLink:
                    {
                        string? uri = (string)e.AssociatedObject;

                        if (uri is null)
                            return;

                        OpenExternalUrl(uri);
                        break;
                    }

                case Controls.HyperlinkType.Channel:
                    {
                        var channel = (DiscordChannel)e.AssociatedObject;
                        ChannelId = channel.Id;
                        foreach (var category in ViewModel.Categories)
                        {
                            foreach (var item in category.Items)
                            {
                                if (item.Id == channel.Id)
                                {
                                    item.IsSelected = true;
                                }
                                else
                                {
                                    item.IsSelected = false;
                                }
                            }
                        }
                        await OnChannelChange().ConfigureAwait(false);
                        // find the item in the list
                        break;
                    }
            }
        }

        private void OpenMedia(object sender, MouseButtonEventArgs e)
        {
            var media = sender as FrameworkElement;
            if (media is null) return;

            var imageProviderVm = media.DataContext as AttachmentViewModel;
            if (imageProviderVm is null) return;

            var imagePreviewer = new ImagePreviewer(imageProviderVm);
            OnImagePreviewerOpened(imagePreviewer);
        }

        private void OpenMediaEmbed(object sender, MouseButtonEventArgs e)
        {
            var media = sender as FrameworkElement;
            if (media is null) return;

            //EmbedViewModel? embedVm = media.DataContext as EmbedViewModel;
            //if (embedVm is null) return;
            //var imageProviderVm = embedVm.Thumbnail;
            var imageProviderVm = media.Tag as EmbedImageViewModel;
            if (imageProviderVm is null) return;

            var imagePreviewer = new ImagePreviewer(imageProviderVm);
            OnImagePreviewerOpened(imagePreviewer);
        }

        private void OnImagePreviewerOpened(ImagePreviewer imagePreviewer)
        {
            // set its position to the center of this window
            imagePreviewer.Left = Left + (Width - imagePreviewer.Width) / 2;
            imagePreviewer.Top = Top + (Height - imagePreviewer.Height) / 2;
            imagePreviewer.Show();
        }

        private async void LeaveCallButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await VoiceManager.Instance.LeaveVoiceChannel();
        }

        private string GetMessageBoxText() // returns full text & converts all emoticon images to Unicode emojis
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block block in MessageTextBox.Document.Blocks)
            {
                foreach (Inline inline in ((Paragraph)block).Inlines)
                {
                    switch (inline)
                    {
                        case LineBreak: // newline
                            sb.AppendLine();
                            break;
                        case Run: // character
                            sb.Append(((Run)inline).Text);
                            break;
                        case InlineUIContainer: // emoticon
                            string EmojiNameDiscord = ":" + ((InlineUIContainer)inline).Tag + ":";
                            DiscordEmoji emoji = DiscordEmoji.FromName(Discord.Client, EmojiNameDiscord);
                            TextPointer caret = MessageTextBox.CaretPosition;
                            sb.Append(emoji.ToString());
                            break;
                    }
                }
            }
            return sb.ToString();
        }

        private void SetMessageBoxText(string text) // TODO: make it set emoticons instead of Unicode
        {
            new TextRange(MessageTextBox.Document.ContentStart, MessageTextBox.Document.ContentEnd).Text = text;
        }

        private void MessageTextBox_SizeChanged(object sender, ScrollChangedEventArgs e)
        {
            if (isDraggingBottomSeparator) return;
            var textBox = (RichTextBox)sender;
            var newHeight = (int)textBox.ExtentHeight + 40;
            if ((ViewModel.BottomHeight > newHeight && sizeTainted) || newHeight > 200) return;

            int min = 64;

            if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
            {
                min += 25;
            }

            ViewModel.BottomHeight = Math.Max(newHeight, min);
        }


        private async void CanvasButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = DrawingCanvas;
            var width = (int)canvas.ActualWidth;
            var height = (int)canvas.ActualHeight;

            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(canvas);

            var writeableBitmap = new WriteableBitmap(renderTarget);
            var stride = writeableBitmap.PixelWidth * (writeableBitmap.Format.BitsPerPixel / 8);
            var pixelData = new byte[stride * writeableBitmap.PixelHeight];
            writeableBitmap.CopyPixels(pixelData, stride, 0);

            int minX = width, minY = height, maxX = 0, maxY = 0;
            bool foundNonTransparentPixel = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * stride) + (x * 4);
                    byte alpha = pixelData[index + 3];

                    if (alpha != 0)
                    {
                        foundNonTransparentPixel = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!foundNonTransparentPixel)
            {
                return;
            }

            int croppedWidth = maxX - minX + 1;
            int croppedHeight = maxY - minY + 1;

            var croppedBitmap = new CroppedBitmap(writeableBitmap, new Int32Rect(minX, minY, croppedWidth, croppedHeight));

            int padding = 10;
            int paddedWidth = croppedWidth + (2 * padding);
            int paddedHeight = croppedHeight + (2 * padding);

            var whiteBackgroundBitmap = new RenderTargetBitmap(paddedWidth, paddedHeight, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, paddedWidth, paddedHeight));
                drawingContext.DrawImage(croppedBitmap, new Rect(padding, padding, croppedWidth, croppedHeight));
            }

            whiteBackgroundBitmap.Render(drawingVisual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(whiteBackgroundBitmap));

            canvas.Strokes.Clear();

            // write to tmp.png next to the .exe
            // XXX(isabella): Why?
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                await SendMessage("", ms, encoder.Frames[0].PixelWidth, encoder.Frames[0].PixelHeight);
            }
        }


        private int _drawingHeight = 120;
        private int _writingHeight = 64;

        private Stack<Stroke> _undoStack = new();
        private Stack<Stroke> _redoStack = new();

        private void Update_Stroke_Buttons()
        {
            ViewModel.UndoEnabled = DrawingCanvas.Strokes.Count > 0;
            ViewModel.RedoEnabled = _redoStack.Count > 0;
        }

        private void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Added.Count > 0)
            {
                _undoStack.Push(e.Added[0]);
                _redoStack.Clear();
            }
            Update_Stroke_Buttons();
        }

        public void Undo()
        {
            if (DrawingCanvas.Strokes.Count == 0) return;
            var stroke = DrawingCanvas.Strokes.Last();
            _redoStack.Push(stroke);
            _undoStack.Pop();
            DrawingCanvas.Strokes.Remove(stroke);
            Update_Stroke_Buttons();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var stroke = _redoStack.Pop();
            DrawingCanvas.Strokes.StrokesChanged -= Strokes_StrokesChanged; // Disable the event
            DrawingCanvas.Strokes.Add(stroke);
            DrawingCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged; // Re-enable the event
            _undoStack.Push(stroke);
            Update_Stroke_Buttons();
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // handle (ctrl) + z | y
            if (e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.LeftCtrl)) && !e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.RightCtrl)))
            {
                if (e.Key == Key.Z)
                {
                    Undo();
                }
                else if (e.Key == Key.Y)
                {
                    Redo();
                }
            }
        }

        private void SwitchToText_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ShowEditorTextTab();
        }

        private void SwitchToDraw_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ShowEditorDrawTab();
        }

        private void ShowEditorTextTab()
        {
            STDButton.SetToggle(false);
            if (MessageTextBox.Visibility == Visibility.Visible) return;
            _drawingHeight = ViewModel.BottomHeight;
            ViewModel.BottomHeight = _writingHeight;
            MessageTextBox.Visibility = Visibility.Visible;
            DrawingContainer.Visibility = Visibility.Collapsed;
        }

        private void ShowEditorDrawTab()
        {
            STTButton.SetToggle(false);
            if (MessageTextBox.Visibility == Visibility.Collapsed) return;
            _writingHeight = ViewModel.BottomHeight;
            ViewModel.BottomHeight = _drawingHeight;
            MessageTextBox.Visibility = Visibility.Collapsed;
            DrawingContainer.Visibility = Visibility.Visible;
        }

        private void DrawOnClickUndo(object sender, MouseButtonEventArgs e)
        {
            Undo();
        }

        private void DrawOnClickRedo(object sender, MouseButtonEventArgs e)
        {
            Redo();
        }

        private void DrawOnClickTrash(object sender, MouseButtonEventArgs e)
        {
            DrawingCanvas.Strokes.Clear();
            _redoStack.Clear();
            _undoStack.Clear();
            Update_Stroke_Buttons();
        }

        private void DrawOnClickPen(object sender, MouseButtonEventArgs e)
        {
            DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
            ViewModel.DrawingTool = DrawingTool.Pen;
        }

        private void DrawOnClickKesigomu(object sender, MouseButtonEventArgs e)
        {
            DrawingCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            ViewModel.DrawingTool = DrawingTool.Kesigomu;
        }

        private void ShowColorMenu(object sender, MouseButtonEventArgs e)
        {
            var picker = new ColorPicker();
            // set the position of the picker to be below the button
            var button = (FrameworkElement)sender;
            var point = button.PointToScreen(new Point(0, button.ActualHeight));
            picker.Left = point.X;
            picker.Top = point.Y;
            picker.Show();
            picker.Closing += (s, e) =>
            {
                var brush = picker.SelectedColor;
                if (brush is null) return;
                DrawingCanvas.DefaultDrawingAttributes.Color = brush.Color;
            };
        }

        private bool _isTyping = false;
        private string _lastValue = "";

        Timer typingTimer = new(1000)
        {
            AutoReset = false
        };

        private void TypingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!_isTyping) return;

                string currentText = GetMessageBoxText();

                if (currentText == _lastValue || string.IsNullOrWhiteSpace(currentText))
                {
                    _isTyping = false;
                    return;
                }

                _lastValue = currentText;

                Task.Run(async () =>
                {
                    await Channel.TriggerTypingAsync();
                });

                typingTimer.Start();
            });
        }

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isTyping)
            {
                _isTyping = true;
                TypingTimer_Elapsed(null, null!);
                typingTimer.Start();
            }

            /*if (GetMessageBoxText().EndsWith(':')) TODO: Emoji suggestions popup
            {
                
            }*/            
        }

        private void OnMessageContextMenuOpening(object senderRaw, ContextMenuEventArgs e)
        {
            FrameworkElement? sender = senderRaw as FrameworkElement;

            if (sender == null)
                return;

            Grid? grid = FindParent.Execute<Grid>(sender, "PART_MessageGrid");

            if (grid == null)
                return;

            MessageViewModel? vm = grid.DataContext as MessageViewModel;

            ContextMenu contextMenu = grid.ContextMenu;
            contextMenu.DataContext = vm;

            // TODO(isabella): Implement reactions.
            bool isReactionAllowed = false && ViewModel.Channel.CanAddReactions && !vm.Ephemeral;

            MenuItem addReactionsButton = (MenuItem)FindContextMenuItemName(contextMenu, "AddReactionButton");
            addReactionsButton.Visibility = isReactionAllowed
                ? Visibility.Visible
                : Visibility.Collapsed;

            Separator addReactionsSeparator = (Separator)FindContextMenuItemName(contextMenu, "ReactionsSeparator");
            addReactionsSeparator.Visibility = isReactionAllowed
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (isReactionAllowed)
            {
                // Build reactions context menu:
            }

            bool isOwnMessage = vm.Author.Id == _chatService.GetCurrentUser().Result.Id;

            MenuItem editButton = (MenuItem)FindContextMenuItemName(contextMenu, "EditButton");
            editButton.Visibility = isOwnMessage
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool isChattingAllowed = ViewModel.Channel.CanTalk && !vm.Ephemeral;

            MenuItem replyButton = (MenuItem)FindContextMenuItemName(contextMenu, "ReplyButton");
            replyButton.Visibility = isChattingAllowed
                ? Visibility.Visible
                : Visibility.Collapsed;

            //MenuItem forwardButton = (MenuItem)FindContextMenuItemName(contextMenu, "ForwardButton");
            //forwardButton.Visibility = isChattingAllowed
            //    ? Visibility.Visible
            //    : Visibility.Collapsed;

            //MenuItem copyMessageLinkButton = (MenuItem)FindContextMenuItemName(contextMenu, "CopyMessageLinkButton");
            //copyMessageLinkButton.Visibility = !vm.Ephemeral
            //    ? Visibility.Visible
            //    : Visibility.Collapsed;

            //MenuItem markUnreadButton = (MenuItem)FindContextMenuItemName(contextMenu, "MarkUnreadButton");
            //markUnreadButton.Visibility = !vm.Ephemeral
            //    ? Visibility.Visible
            //    : Visibility.Collapsed;

            bool isDeveloperModeEnabled = SettingsManager.Instance.DiscordDeveloperMode && !vm.Ephemeral;

            Separator developerActionsSeparator = (Separator)FindContextMenuItemName(contextMenu, "DeveloperActionsSeparator");
            developerActionsSeparator.Visibility = isDeveloperModeEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;

            MenuItem copyAuthorIdButton = (MenuItem)FindContextMenuItemName(contextMenu, "CopyAuthorIdButton");
            copyAuthorIdButton.Visibility = isDeveloperModeEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;

            MenuItem copyMessageIdButton = (MenuItem)FindContextMenuItemName(contextMenu, "CopyMessageIdButton");
            copyMessageIdButton.Visibility = isDeveloperModeEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool isDeleteAllowed = isOwnMessage || ViewModel.Channel.CanManageMessages;

            Separator deleteSeparator = (Separator)FindContextMenuItemName(contextMenu, "DeleteButtonSeparator");
            deleteSeparator.Visibility = isDeleteAllowed
                ? Visibility.Visible
                : Visibility.Collapsed;

            MenuItem deleteButton = (MenuItem)FindContextMenuItemName(contextMenu, "DeleteButton");
            deleteButton.Visibility = isDeleteAllowed
                ? Visibility.Visible
                : Visibility.Collapsed;

        }

        private FrameworkElement? FindContextMenuItemName(ContextMenu contextMenu, string itemName)
        {
            foreach (FrameworkElement itemIt in contextMenu.Items)
            {
                if (itemIt.Name == itemName)
                {
                    return itemIt;
                }
            }

            return null;
        }

        private MessageViewModel? GetMessageViewModelForContextMenu(object sender)
        {
            MenuItem? menuItem = sender as MenuItem;

            if (menuItem == null)
                return null;

            ContextMenu? contextMenu = menuItem.Parent as ContextMenu;

            if (contextMenu == null)
                return null;

            return contextMenu.DataContext as MessageViewModel;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            Channel.DeleteMessageAsync(messageVm.MessageEntity);
        }

        private void CopyMessageIdButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            Clipboard.SetText(messageVm.Id.ToString());
        }

        private void CopyAuthorIdButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            Clipboard.SetText(messageVm.Author?.Id.ToString() ?? "0");
        }

        private void CopyMessageLinkButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            Clipboard.SetText(messageVm.MessageEntity.JumpLink.ToString());
        }

        private void CopyMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            Clipboard.SetText(messageVm.Message.ToString());
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            StartEditingMessage(messageVm);
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            MessageViewModel? messageVm = GetMessageViewModelForContextMenu(sender);

            SetReplyTargetAndEnterReplyMode(messageVm);
        }


        private void ClearMessageSelection()
        {
            // If we came in from a reply, then hide the reply container:
            // Anti-MVVM workaround:
            if (ViewModel.MessageTargetMode == TargetMessageMode.Reply)
            {
                HideReplyView();
            }

            if (ViewModel.TargetMessage != null)
            {
                ViewModel.TargetMessage.IsSelectedForUiAction = false;
                ViewModel.TargetMessage = null;
                ViewModel.MessageTargetMode = TargetMessageMode.None;
            }

            RevalidatePushToDrawVisibility();
        }

        private void StartEditingMessage(MessageViewModel message)
        {
            // Leave other selection modes so no conflicts occur:
            ClearMessageSelection();


            Paragraph paragraph = MessageTextBox.Document.Blocks.LastBlock as Paragraph;
            if (paragraph == null)
            {
                paragraph = new Paragraph();
                MessageTextBox.Document.Blocks.Add(paragraph);
            }

            MessageTextBox.CaretPosition = paragraph.ContentEnd;



            message.IsSelectedForUiAction = true;
            ViewModel.TargetMessage = message;
            ViewModel.MessageTargetMode = TargetMessageMode.Edit;

            SetMessageBoxText(message.Message);

            RevalidatePushToDrawVisibility();

            // We can't use the push to draw feature when editing a message, so we
            // set the tab now:
            ShowEditorTextTab();

            MessageTextBox.Focus();
        }

        private void LeaveEditingMessage()
        {
            if (ViewModel.TargetMessage != null)
            {
                // Only if we're clearing the message being edited should we
                // wipe the contents of the input box.
                MessageTextBox.Document.Blocks.Clear();
            }

            ClearMessageSelection();

            RevalidatePushToDrawVisibility();
        }

        private void SetReplyTargetAndEnterReplyMode(MessageViewModel message)
        {
            // Leave other selection modes so no conflicts occur:
            ClearMessageSelection();

            message.IsSelectedForUiAction = true;
            ViewModel.TargetMessage = message;
            ViewModel.MessageTargetMode = TargetMessageMode.Reply;

            ShowReplyView();
        }

        private void SetIconGCDM(bool isDm, bool isGc)
        {
            string uri = null;
            if (isDm && !isGc) // 1-on-1 dm
            {
                uri = "pack://application:,,,/Icons/DirectMessage.ico";
            }
            else { uri = "pack://application:,,,/Icons/GroupServer.ico"; }
            this.Icon = new BitmapImage(new Uri(uri));
        }

        private void ClearReplyTarget()
        {
            ClearMessageSelection();
        }

        private void ShowReplyView()
        {
            PART_ReplyTargetRowDefinition.Height = new GridLength(25);
            PART_ReplyTargetContainer.Visibility = Visibility.Visible;
            ViewModel.BottomHeight += 25;
        }

        private void HideReplyView()
        {
            PART_ReplyTargetRowDefinition.Height = new GridLength(0);
            PART_ReplyTargetContainer.Visibility = Visibility.Collapsed;

            ViewModel.BottomHeight -= 25;
            if (MessageTextBox.Height > 25)
            {
                ViewModel.BottomHeight = 25;
            }
        }

        private void RevalidatePushToDrawVisibility()
        {
            if (ViewModel.MessageTargetMode == TargetMessageMode.Edit ||
                ViewModel.IsShowingAttachmentEditor)
            {
                SwitchToDraw.Visibility = Visibility.Collapsed;
            }
            else
            {
                SwitchToDraw.Visibility = Visibility.Visible;
            }
        }

        private bool VerifyAttachmentPermissions()
        {
            if (!ViewModel.Channel.CanAttachFiles)
            {
                Dialog errorDialog = new("Error",
                    "You do not have permission to perform this action.",
                    SystemIcons.Error);
                errorDialog.Owner = this;
                errorDialog.ShowDialog();

                return false;
            }

            return true;
        }

        internal void OpenAttachmentsFilePicker()
        {
            OpenFileDialog dialog = new();
            dialog.Multiselect = true;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                if (dialog.FileNames.Length > 10)
                {
                    Dialog errorDialog = new("Error",
                        "You may only attach at most 10 files in the same message. " +
                        "The selection will be clipped to first 10 items.",
                        SystemIcons.Error);
                    errorDialog.Owner = this;
                    errorDialog.ShowDialog();
                }

                InsertAttachmentsFromAnyThreadFromStringArray(dialog.FileNames);
            }
        }

        private void InsertAttachmentsFromAnyThreadFromStringArray(string[] fileNames)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                bool succeeded = false;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    succeeded = InsertAttachment(fileNames[i]);
                });

                if (!succeeded)
                {
                    break;
                }
            }
        }

        private void InsertAttachmentsFromAnyThreadFromBitmapSourceArray(BitmapSource[] bitmapSources)
        {
            for (int i = 0; i < bitmapSources.Length; i++)
            {
                bool succeeded = false;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    succeeded = InsertAttachment(bitmapSources[i]);
                });

                if (!succeeded)
                {
                    break;
                }
            }
        }

        private bool ValidateInsertAttachmentConditions()
        {
            if (!VerifyAttachmentPermissions())
            {
                return false;
            }

            if (PART_AttachmentsEditor.ViewModel.Attachments.Count >= 10)
            {
                Dialog errorDialog = new("Error",
                    "You may only attach at most 10 files in the same message.",
                    SystemIcons.Error);
                errorDialog.Owner = this;
                errorDialog.ShowDialog();

                return false;
            }

            return true;
        }

        internal bool InsertAttachment(string fileName)
        {
            if (!ValidateInsertAttachmentConditions())
                return false;

            PART_AttachmentsEditor.ViewModel.AddItem(fileName);
            ShowAttachmentsEditor();

            return true;
        }

        internal bool InsertAttachment(Stream dataStream, string extension)
        {
            if (!ValidateInsertAttachmentConditions())
                return false;

            PART_AttachmentsEditor.ViewModel.AddVirtualItem(dataStream, extension);
            ShowAttachmentsEditor();

            return true;
        }

        internal bool InsertAttachment(BitmapSource bitmapSource)
        {
            Stream stream = new MemoryStream();

            PngBitmapEncoder pngEncoder = new();
            pngEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            pngEncoder.Save(stream);

            return InsertAttachment(stream, "png");
        }

        private void OnAttachmentsEditorAttachmentsUpdated(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<AttachmentsEditorItem>;

            if (collection == null)
            {
                return;
            }

            if (collection.Count == 0)
            {
                CloseAttachmentsEditor(true);
            }
        }

        internal void CloseAttachmentsEditor(bool noClear = false)
        {
            // This condition will avoid firing the InvalidOperationException in the first
            // place in well-formed code. If you forget to pass the argument, oh well, no
            // big deal.
            if (!noClear)
            {
                try
                {
                    PART_AttachmentsEditor.ViewModel.Attachments.Clear();
                }
                catch (InvalidOperationException)
                {
                    // Ignore. This is usually because of a rather bogus error
                    // "Cannot change ObservableCollection during a CollectionChanged event."
                    // which only occurs if we're being sent from the count update code in
                    // OnAttachmentsEditorAttachmentsUpdated.
                    Debug.Assert(PART_AttachmentsEditor.ViewModel.Attachments.Count == 0);
                }
            }

            HideAttachmentsEditor();
        }

        private void ShowAttachmentsEditor()
        {
            if (ViewModel.IsShowingAttachmentEditor)
            {
                return;
            }

            PART_AttachmentEditorRowDefinition.Height = new GridLength(64);
            PART_AttachmentEditorGrid.Visibility = Visibility.Visible;

            // We can't use the push to draw feature when uploading another attachment, so we
            // set the tab now:
            ShowEditorTextTab();

            ViewModel.IsShowingAttachmentEditor = true;
            RevalidatePushToDrawVisibility();
        }

        private void HideAttachmentsEditor(bool noShowCheck = false)
        {
            if (!noShowCheck && !ViewModel.IsShowingAttachmentEditor)
            {
                return;
            }

            PART_AttachmentEditorRowDefinition.Height = new GridLength(0);
            PART_AttachmentEditorGrid.Visibility = Visibility.Collapsed;

            ViewModel.IsShowingAttachmentEditor = false;
            RevalidatePushToDrawVisibility();
        }

        private void OnDropFileIntoChatWindow(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                InsertAttachmentsFromAnyThreadFromStringArray(files);
            }
        }

        private void OnEmbedTitleHyperlinkClicked(object sender, RoutedEventArgs e)
        {
            EmbedViewModel? embed = (sender as Hyperlink)?.DataContext as EmbedViewModel;

            if (embed == null)
                return;

            string? uri = embed?.Url;

            if (uri is null)
                return;

            OpenExternalUrl(uri);
        }

        private void OnEmbedProviderHyperlinkClicked(object sender, RoutedEventArgs e)
        {
            EmbedViewModel? embed = (sender as Hyperlink)?.DataContext as EmbedViewModel;

            if (embed == null)
                return;

            string? uri = embed?.Provider?.Url;

            if (uri is null)
                return;

            OpenExternalUrl(uri);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            EmbedViewModel? embed = (sender as Hyperlink)?.DataContext as EmbedViewModel;

            if (embed == null)
                return;

            string? uri = embed?.Author?.Url;

            if (uri is null)
                return;

            OpenExternalUrl(uri);
        }
    }

    public struct DoStroke
    {
        public string ActionFlag { get; set; }
        public System.Windows.Ink.Stroke Stroke { get; set; }
    }
}
