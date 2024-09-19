using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Vanara.PInvoke.DwmApi;
using System.Windows.Interop;
using System.Drawing;
using System.Timers;
using DSharpPlus.Entities;
using Aerochat.ViewModels;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;
using System.Reactive.Linq;
using Aerochat.Hoarder;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Aerochat.Theme;
using Image = System.Windows.Controls.Image;
using static Aerochat.ViewModels.HomeListViewCategory;
using DSharpPlus;
using System.Windows.Threading;
using Aerochat.Settings;
using System.Reflection;

namespace Aerochat.Windows
{
    public class ToolbarItem(string text, Action action)
    {
        public string Text { get; set; } = text;
        public Action Action { get; set; } = action;
    }

    public partial class Chat : Window
    {
        private MediaPlayer chatSoundPlayer = new();
        public DiscordChannel Channel;
        private ulong channelId;
        bool isDraggingTopSeperator = false;
        bool isDraggingBottomSeperator = false;
        int initialPos = 0;
        private Dictionary<ulong, System.Timers.Timer> timers = new();

        public ObservableCollection<DiscordUser> TypingUsers { get; } = new();
        public ChatWindowViewModel ViewModel { get; set; } = new ChatWindowViewModel();

        public async Task ExecuteNudgePrettyPlease(double initialLeft, double initialTop)
        {
            int steps = 50;
            int stepSize = 25;

            Random random = new();

            for (int i = 0; i < steps; i++)
            {
                double newLeft = initialLeft + random.Next(-10, 10);
                double newTop = initialTop + random.Next(-10, 10);

                await Dispatcher.InvokeAsync(() =>
                {
                    Left = newLeft;
                    Top = newTop;
                });

                await Task.Delay(stepSize);
            }
        }

        private void Chat_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
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
            if (ViewModel.Messages.Count > 0) ViewModel.Messages.Clear();
            ViewModel.Loading = true;
            Discord.Client.TryGetCachedChannel(channelId, out DiscordChannel currentChannel);
            if (currentChannel is null)
            {
                currentChannel = await Discord.Client.GetChannelAsync(channelId);
            }

            Application.Current.Dispatcher.Invoke(delegate
            {
                if (ViewModel is null || ViewModel?.Messages is null) return;
                Channel = currentChannel;
                ViewModel.Channel = ChannelViewModel.FromChannel(currentChannel);
            });

            bool isDM = currentChannel is DiscordDmChannel;
            bool isGroupChat = isDM && ((DiscordDmChannel)currentChannel).Type == ChannelType.Group;

            // if typing users is not empty clear it
            if (TypingUsers.Count > 0)
            {
                TypingUsers.Clear();
            }

            ViewModel.IsDM = isDM;
            ViewModel.IsGroupChat = isGroupChat;
            ViewModel.CurrentUser = UserViewModel.FromUser(Discord.Client.CurrentUser);

            DiscordUser? recipient = null;
            if (isDM && !isGroupChat)
            {
                recipient = ((DiscordDmChannel)currentChannel).Recipients.FirstOrDefault(x => x.Id != Discord.Client.CurrentUser.Id);
                if (!Discord.Client.TryGetCachedUser(recipient?.Id ?? 0, out recipient) || recipient?.BannerColor == null)
                {
                    recipient = await Discord.Client.GetUserAsync(recipient.Id, true);
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
                ViewModel.Categories.Add(new()
                {
                    Collapsed = false,
                    IsSelected = false,
                    IsVisibleProperty = true,
                    Name = "Test"
                });

                ViewModel.Categories[0].Items.Add(new()
                {
                    Name = Discord.Client.CurrentUser.DisplayName,
                    Id = Discord.Client.CurrentUser.Id,
                    Image = Discord.Client.CurrentUser.AvatarUrl,
                    Presence = Discord.Client.CurrentUser.Presence == null ? null : PresenceViewModel.FromPresence(Discord.Client.CurrentUser.Presence)
                });

                foreach (var rec in ((DiscordDmChannel)currentChannel).Recipients)
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

            if (recipient is not null) ViewModel.Recipient = UserViewModel.FromUser(recipient);

            var messages = await currentChannel.GetMessagesAsync(50);
            List<MessageViewModel> messageViewModels = new();

            if (!Discord.Client.TryGetCachedGuild(currentChannel.GuildId ?? 0, out DiscordGuild guild) && !isDM)
            {
                guild = await Discord.Client.GetGuildAsync(currentChannel.GuildId ?? 0);
            }

            foreach (var msg in messages)
            {
                var member = msg.Channel.Guild?.Members.FirstOrDefault(x => x.Key == msg.Author.Id).Value;
                MessageViewModel message = MessageViewModel.FromMessage(msg, member);
                messageViewModels.Add(message);
            }

            messageViewModels.Reverse();

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var msg in messageViewModels)
                {
                    if (ViewModel is null || ViewModel?.Messages is null) return;
                    ViewModel.Messages.Add(msg);
                }
            });


            Application.Current.Dispatcher.Invoke(delegate
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
                if (SettingsManager.Instance.LastReadMessages.ContainsKey(channelId) && Channel.LastMessageId is not null)
                {
                    SettingsManager.Instance.LastReadMessages[channelId] = (ulong)Channel.LastMessageId;
                }
                SettingsManager.Save();
            }

            Dispatcher.Invoke(() =>
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

            if (!isDM)
            {
                SettingsManager.Instance.SelectedChannels[guild.Id] = channelId;
                SettingsManager.Save();
            }

            Dispatcher.Invoke(UpdateChannelListerReadReciepts);
        }

        public void UpdateChannelListerReadReciepts()
        {
            bool isDM = Channel is DiscordDmChannel;
            if (isDM) return;
            foreach (var category in ViewModel.Categories)
            {
                foreach (var item in category.Items)
                {
                    Discord.Client.TryGetCachedChannel(item.Id, out var c);
                    bool found = SettingsManager.Instance.LastReadMessages.TryGetValue(c.Id, out var lastReadMessageId);
                    DateTime lastReadMessageTime;
                    if (found)
                    {
                        lastReadMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)(lastReadMessageId >> 22) + 1420070400000)).DateTime;
                    }
                    else
                    {
                        lastReadMessageTime = SettingsManager.Instance.ReadRecieptReference;
                    }
                    bool isCurrentChannel = c.Id == channelId;
                    var channel = c;
                    var lastMessageId = channel.LastMessageId;
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
                            SettingsManager.Instance.LastReadMessages[channelId] = lastMessageId ?? 0;
                        }
                    }
                }
            }

            var items = ViewModel.Categories.ToList();
            ViewModel.Categories.Clear();
            foreach (var item in items)
            {
                ViewModel.Categories.Add(item);
            }
        }

        public async Task BeginDiscordLoop()
        {
            await OnChannelChange();
            Discord.Client.TryGetCachedChannel(channelId, out DiscordChannel currentChannel);
            if (currentChannel is null)
            {
                currentChannel = await Discord.Client.GetChannelAsync(channelId);
            }

            bool isDM = currentChannel is DiscordDmChannel;
            Discord.Client.TryGetCachedGuild(currentChannel.GuildId ?? 0, out DiscordGuild guild);
            if (guild is null && !isDM)
            {
                guild = await Discord.Client.GetGuildAsync(currentChannel.GuildId ?? 0);
            }

            Dispatcher.Invoke(() =>
            {
                if (!isDM)
                {
                    ViewModel.Guild = GuildViewModel.FromGuild(guild);
                    List<ChannelType> AllowedChannelTypes = new()
                {
                    ChannelType.Text,
                    ChannelType.Announcement
                };

                    // firstly, get all uncategorized channels
                    var uncategorized = guild.Channels.Values
                        .Where(x => x.ParentId == null && AllowedChannelTypes.Contains(x.Type))
                        .OrderBy(x => x.Position);

                    if (uncategorized.Count() > 0)
                    {
                        ViewModel.Categories.Add(new()
                        {
                            Name = "Uncategorized",
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
                        }
                    }

                    var categories = guild.Channels.Values
                        .Where(x => x.Type == ChannelType.Category)
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
                        }
                    }

                    Dispatcher.Invoke(UpdateChannelListerReadReciepts);
                }
            });
            Dispatcher.Invoke(Show);
            if (!isDM) await Discord.Client.SyncGuildsAsync(guild).ConfigureAwait(false);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Discord.Client.TypingStarted -= OnType;
            Discord.Client.MessageCreated -= OnMessageCreation;
        }

        private async Task OnMessageCreation(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
        {
            args.Channel.GetType().GetProperty("LastMessageId")?.SetValue(args.Channel, args.Message.Id);
            Dispatcher.Invoke(UpdateChannelListerReadReciepts);

            if (args.Channel.Id != channelId) return;
            bool isNudge = args.Message.Content == "[nudge]";
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (!Discord.Client.TryGetCachedUser(args.Author.Id, out DiscordUser user))
                {
                    user = Discord.Client.GetUserAsync(args.Author.Id).Result;
                };

                var member = args.Guild?.Members.FirstOrDefault(x => x.Key == args.Author.Id).Value;

                MessageViewModel message = MessageViewModel.FromMessage(args.Message, member);

                foreach (var attachment in args.Message.Attachments)
                {
                    message.Attachments.Add(AttachmentViewModel.FromAttachment(attachment));
                }

                MessageViewModel? eph = ViewModel.Messages.FirstOrDefault(x => x.Ephemeral && x.Message == message.Message);
                if (eph != null)
                {
                    int messageIndex = ViewModel.Messages.IndexOf(eph);
                    if (messageIndex != -1)
                    {
                        ViewModel.Messages.RemoveAt(messageIndex);
                    }
                }

                ViewModel.LastReceivedMessage = message;

                ViewModel.Messages.Add(message);

                // if the user is in the typing users list, remove them
                if (TypingUsers.Contains(args.Author))
                {
                    TypingUsers.Remove(args.Author);
                }

                if (isNudge)
                {
                    chatSoundPlayer.Open(new Uri("Resources/Sounds/nudge.wav", UriKind.Relative));
                }
                else
                {
                    if (IsActive && message.Author.Id != Discord.Client.CurrentUser.Id) chatSoundPlayer.Open(new Uri("Resources/Sounds/type.wav", UriKind.Relative));
                }
                ProcessLastRead();
            });

            if (isNudge)
            {
                Application.Current.Dispatcher.Invoke(() => ExecuteNudgePrettyPlease(Left, Top));
            }
        }

        private void ProcessLastRead()
        {
            Dispatcher.Invoke(() =>
            {
                var message = ViewModel.Messages.LastOrDefault();
                // if its ephemeral return
                if (message == null || message.Ephemeral) return;
                // if if VisualChildrenCount returns zero don't
                if (VisualTreeHelper.GetChildrenCount(MessagesListItemsControl) == 0) return;
                ScrollViewer scrollViewer = VisualTreeHelper.GetChild(MessagesListItemsControl, 0) as ScrollViewer;
                if (scrollViewer != null && IsActive)
                {
                    if (!SettingsManager.Instance.LastReadMessages.TryGetValue(channelId, out var msgId))
                    {
                        SettingsManager.Instance.LastReadMessages[channelId] = message.Id ?? 0;
                    } else
                    {
                        long prevTimestamp = ((long)(msgId >> 22)) + 1420070400000;
                        DateTime prevLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(prevTimestamp).DateTime;

                        long nextTimestamp = ((long)(message.Id! >> 22)) + 1420070400000;
                        DateTime nextLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(nextTimestamp).DateTime;

                        if (prevLastMessageTime < nextLastMessageTime)
                        {
                            SettingsManager.Instance.LastReadMessages[channelId] = message.Id ?? 0;
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
            Dispatcher.Invoke(UpdateChannelListerReadReciepts);
        }

        private async Task OnType(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.TypingStartEventArgs args)
        {
            if (args.Channel.Id != channelId) return;
            if (args.User.Id == Discord.Client.CurrentUser.Id) return;
            Application.Current.Dispatcher.Invoke(() =>
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
                        Application.Current.Dispatcher.Invoke(() =>
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

        public Chat(ulong id, bool allowDefault = false)
        {
            Hide();

            if (allowDefault)
            {
                Discord.Client.TryGetCachedChannel(id, out DiscordChannel channel);
                if (channel is null || channel.GuildId is null)
                {
                    channelId = id;
                } else
                if (SettingsManager.Instance.SelectedChannels.TryGetValue(channel.GuildId ?? 0, out ulong selectedChannel))
                {
                    channelId = selectedChannel;
                } else
                {
                    channelId = id;
                }
            } else
            {
                channelId = id;
            }
            InitializeComponent();
            Task.Run(BeginDiscordLoop);
            DataContext = ViewModel;
            chatSoundPlayer.MediaOpened += (sender, args) =>
            {
                chatSoundPlayer.Play();
            };
            ViewModel.Messages.CollectionChanged += UpdateHiddenInfo;
            TypingUsers.CollectionChanged += TypingUsers_CollectionChanged;
            Closing += Chat_Closing;
            Loaded += Chat_Loaded;
            Discord.Client.TypingStarted += OnType;
            Discord.Client.MessageCreated += OnMessageCreation;
            Discord.Client.MessageDeleted += OnMessageDeleted;
            Discord.Client.MessageUpdated += OnMessageUpdated;
        }

        private async Task OnMessageUpdated(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs args)
        {
            // get the message from the collection
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ViewModel is null) return;
                var message = ViewModel.Messages.FirstOrDefault(x => x.Id == args.Message.Id);
                if (message is not null)
                {
                    message.Message = args.Message.Content;
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ViewModel is null) return;
                var message = ViewModel.Messages.FirstOrDefault(x => x.Id == args.Message.Id);
                if (message is not null)
                {
                    ViewModel.Messages.Remove(message);
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

            Discord.Client.TypingStarted -= OnType;
            Discord.Client.MessageCreated -= OnMessageCreation;

            ViewModel.Messages.Clear();
            TypingUsers.Clear();

            System.Timers.Timer timer = new(100);
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
            foreach (var user in TypingUsers.ToList()) {
                if (!Discord.Client.TryGetCachedUser(user.Id, out DiscordUser discordUser))
                {
                    discordUser = await Discord.Client.GetUserAsync(user.Id, true);
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

        private async Task SendMessage(string value)
        {
            bool IsDM = Channel is DiscordDmChannel;
            if (!Discord.Client.TryGetCachedGuild(Channel.GuildId ?? 0, out DiscordGuild guild) && !IsDM)
            {
                guild = await Discord.Client.GetGuildAsync(Channel.GuildId ?? 0);
            }
            // scroll to bottom
            ScrollViewer scrollViewer = VisualTreeHelper.GetChild(MessagesListItemsControl, 0) as ScrollViewer;
            scrollViewer.ScrollToBottom();
            // initialize a new DiscordMessage using reflection, because the constructor is internal
            Type myClassType = typeof(DiscordMessage);

            // Get the private constructor
            ConstructorInfo? constructor = myClassType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (constructor is null)
            {
                throw new Exception("Constructor not found!");
            }

            DiscordMessage fakeMsg = (DiscordMessage)constructor.Invoke(null);
            fakeMsg.GetType().GetProperty("Content")?.SetValue(fakeMsg, value);
            fakeMsg.GetType().GetProperty("Author")?.SetValue(fakeMsg, Discord.Client.CurrentUser);
            fakeMsg.GetType().GetProperty("Channel")?.SetValue(fakeMsg, Channel);
            fakeMsg.GetType().GetProperty("Guild")?.SetValue(fakeMsg, guild);
            fakeMsg.GetType().GetProperty("_timestampRaw")?.SetValue(fakeMsg, DateTime.Now);
            fakeMsg.GetType().GetProperty("Id")?.SetValue(fakeMsg, (ulong)0);
            fakeMsg.GetType().GetProperty("Type")?.SetValue(fakeMsg, MessageType.Default);
            // try to populate mentions
            fakeMsg.GetType().GetProperty("_mentionedUsers")?.SetValue(fakeMsg, new List<DiscordUser>());
            fakeMsg.GetType().GetProperty("_mentionedRoles")?.SetValue(fakeMsg, new List<DiscordRole>());
            fakeMsg.GetType().GetProperty("_mentionedChannels")?.SetValue(fakeMsg, new List<DiscordChannel>());
            ViewModel.Messages.Add(new()
            {
                Author = IsDM ? UserViewModel.FromUser(Discord.Client.CurrentUser) : UserViewModel.FromMember(guild.CurrentMember),
                Message = value == "[nudge]" ? "You have just sent a nudge." : value,
                Timestamp = DateTime.Now,
                Id = 0,
                Ephemeral = true,
                Special = value == "[nudge]",
                MessageEntity = fakeMsg
            });
            try
            {
                await Channel.SendMessageAsync(value);
            }
            catch (Exception)
            {
                // remove the last ephemeral message
                int index = ViewModel.Messages.IndexOf(ViewModel.Messages.Last(x => x.Ephemeral));
                ViewModel.Messages.RemoveAt(index);
            }
        }

        private async void Message_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                if (Channel is null) return;
                e.Handled = true;
                TextBox text = (TextBox)sender;
                string value = new(text.Text);
                if (value.Trim() == string.Empty) return;
                text.Text = "";
                await SendMessage(value);
            } else
            {
                await Channel.TriggerTypingAsync().ConfigureAwait(false);
            }
        }

        private void ToolbarClick(object sender, MouseButtonEventArgs e)
        {
            Grid grid = (Grid)sender;
            if (grid.DataContext is ToolbarItem toolbarItem)
            {
                toolbarItem.Action();
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
            } else
            {
                expandBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void Seperator_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingTopSeperator = true;
            initialPos = (int)e.GetPosition(this).Y;
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingTopSeperator = false;
            isDraggingBottomSeperator = false;
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // drag the ViewModel.TopHeight
            var pos = (int)e.GetPosition(this).Y;
            if (isDraggingTopSeperator)
            {
                ViewModel.TopHeight += pos - initialPos;
                ViewModel.TopHeightMinus10 = ViewModel.TopHeight - 10;
                initialPos = pos;
            }

            if (isDraggingBottomSeperator)
            {
                ViewModel.BottomHeight -= pos - initialPos;
                int min = 70;
                int max = 200;
                ViewModel.BottomHeight = Math.Clamp(ViewModel.BottomHeight, min, max);
                if (ViewModel.BottomHeight != min && ViewModel.BottomHeight != max) initialPos = pos;
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

        private void BottomSeperator_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingBottomSeperator = true;
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
            // set all items to not selected
            foreach (var i in ViewModel.Categories)
            {
                i.IsSelected = false;
                foreach (var x in i.Items)
                {
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
                item.IsSelected = true;
                channelId = item.Id;
                await OnChannelChange();
            }
        }

        private async void RunNudge(object sender, MouseButtonEventArgs e)
        {
            await SendMessage("[nudge]");
            ExecuteNudgePrettyPlease(Left, Top).ConfigureAwait(false);
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

        private async void MessageParser_HyperlinkClicked(object sender, Controls.HyperlinkClickedEventArgs e)
        {
            switch (e.Type)
            {
                case Controls.HyperlinkType.Channel:
                    {
                        var channel = (DiscordChannel)e.AssociatedObject;
                        channelId = channel.Id;
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

        private void OpenImage(object sender, MouseButtonEventArgs e)
        {
            var image = (sender as Image);
            if (image is null) return;

            // get the rect of the image
            var imgRect = image.TransformToAncestor(this).TransformBounds(new Rect(0, 0, image.ActualWidth, image.ActualHeight));
            var wndRect = new Rect(Left, Top, Width, Height);

            // add wndRect to imgRect so its offset properly
            imgRect.Offset(wndRect.Left, wndRect.Top);

            var attachmentVm = image.DataContext as AttachmentViewModel;
            if (attachmentVm is null) return;

            var imagePreviewer = new ImagePreviewer(image.Source.ToString(), attachmentVm.Name, imgRect, wndRect)
            {
                Owner = this,
            };
            imagePreviewer.Show();
        }
    }
}