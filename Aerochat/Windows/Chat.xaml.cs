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
using System.Windows.Media.Imaging;
using XamlAnimatedGif;
using Aerovoice.Clients;
using System.Collections.Concurrent;
using Aerochat.Voice;
using Brushes = System.Windows.Media.Brushes;
using System.Globalization;
using Size = System.Windows.Size;
using Microsoft.Win32;
using System.IO;
using System.Windows.Ink;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;
using DSharpPlus.Exceptions;
using static Aerochat.Windows.ToolbarItem;
using Aerochat.Enums;

namespace Aerochat.Windows
{
    public class ToolbarItem(string text, ToolbarItemAction action)
    {
        public string Text { get; set; } = text;

        public delegate void ToolbarItemAction(FrameworkElement itemElement);

        public ToolbarItemAction Action { get; set; } = action;
    }

    public partial class Chat : Window
    {
        private MediaPlayer chatSoundPlayer = new();
        public DiscordChannel Channel;
        public ulong ChannelId;
        bool isDraggingTopSeperator = false;
        bool isDraggingBottomSeperator = false;
        int initialPos = 0;
        private Dictionary<ulong, Timer> timers = new();
        private VoiceSocket voiceSocket;
        private bool sizeTainted = false;

        public ObservableCollection<DiscordUser> TypingUsers { get; } = new();
        public ChatWindowViewModel ViewModel { get; set; } = new ChatWindowViewModel();

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

            Random random = new();

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
            Discord.Client.TryGetCachedChannel(ChannelId, out DiscordChannel currentChannel);
            if (currentChannel is null)
            {
                currentChannel = await Discord.Client.GetChannelAsync(ChannelId);
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
                    DiscordProfile userProfile = await Discord.Client.GetUserProfileAsync(recipient.Id, true);
                    recipient = userProfile.User;
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
                Dispatcher.Invoke(RefreshGroupChat);
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
                if (SettingsManager.Instance.LastReadMessages.ContainsKey(ChannelId) && Channel.LastMessageId is not null)
                {
                    SettingsManager.Instance.LastReadMessages[ChannelId] = (ulong)Channel.LastMessageId;
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
                SettingsManager.Instance.SelectedChannels[guild.Id] = ChannelId;
                SettingsManager.Save();
            }

            Dispatcher.Invoke(UpdateChannelListerReadReciepts);
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
                        Discord.Client.TryGetCachedChannel(item.Id, out var c);
                        if (c == null) continue;

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

                        bool isCurrentChannel = c.Id == ChannelId;
                        var channel = c;
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

                Application.Current.Dispatcher.Invoke(() =>
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


        public void RefreshGroupChat()
        {
            if (ViewModel.Categories.Count > 0) ViewModel.Categories.Clear();
            ViewModel.Categories.Add(new()
            {
                Collapsed = false,
                IsSelected = false,
                IsVisibleProperty = true,
                Name = ""
            });

            ViewModel.Categories[0].Items.Add(new()
            {
                Name = Discord.Client.CurrentUser.DisplayName,
                Id = Discord.Client.CurrentUser.Id,
                Image = Discord.Client.CurrentUser.AvatarUrl,
                Presence = Discord.Client.CurrentUser.Presence == null ? null : PresenceViewModel.FromPresence(Discord.Client.CurrentUser.Presence)
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
                Discord.Client.TryGetCachedChannel(ChannelId, out DiscordChannel currentChannel);
                if (currentChannel is null)
                {
                    currentChannel = await Discord.Client.GetChannelAsync(ChannelId);
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
                        RefreshChannelList();
                    }
                });
                Dispatcher.Invoke(Show);
                if (!isDM) await Discord.Client.SyncGuildsAsync(guild).ConfigureAwait(false);
            }
            catch (UnauthorizedException e)
            {
                Application.Current.Dispatcher.Invoke(() => ShowErrorDialog("Unauthorized request.\n\nTechnical details: " + e.WebResponse.Response));
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() => ShowErrorDialog("An unknown error occurred.\n\nTechnical details: " + e.Message));
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
                       && x.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels)
                       && guild.Channels.Values.Where(c =>
                            c.ParentId == x.Id
                            && c.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels)
                            && AllowedChannelTypes.Contains(c.Type)
                       ).Count() > 0)
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

            Dispatcher.Invoke(UpdateChannelListerReadReciepts);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                if (Channel.Guild?.Channels?.Select(x => x.Key).ToList().Contains(VoiceManager.Instance.Channel?.Id ?? 0) ?? false)
                {
                    Task.Run(VoiceManager.Instance.LeaveVoiceChannel);
                }
            }
            catch (Exception) { }
            base.OnClosing(e);
            Discord.Client.TypingStarted -= OnType;
            Discord.Client.MessageCreated -= OnMessageCreation;
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
            //Dispatcher.Invoke(UpdateChannelListerReadReciepts);
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

            Dispatcher.Invoke(() =>
            {
                if (messageIndex != -1) ViewModel.Messages.RemoveAt(messageIndex);

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

            Dispatcher.Invoke(() =>
            {
                if (isNudge)
                {
                    chatSoundPlayer.Open(new Uri("Resources/Sounds/nudge.wav", UriKind.Relative));
                }
                else
                {
                    if (IsActive && message.Author?.Id != Discord.Client.CurrentUser.Id) chatSoundPlayer.Open(new Uri("Resources/Sounds/type.wav", UriKind.Relative));
                }
            });

            //Dispatcher.Invoke(ProcessLastRead);

            //if (isNudge)
            //{
            //    Application.Current.Dispatcher.Invoke(() => ExecuteNudgePrettyPlease(Left, Top, SettingsManager.Instance.NudgeLength, SettingsManager.Instance.NudgeIntensity));
            //}
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
                    if (!SettingsManager.Instance.LastReadMessages.TryGetValue(ChannelId, out var msgId))
                    {
                        SettingsManager.Instance.LastReadMessages[ChannelId] = message.Id ?? 0;
                    } else
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
            Dispatcher.Invoke(UpdateChannelListerReadReciepts);
        }

        private async Task OnType(DSharpPlus.DiscordClient sender, DSharpPlus.EventArgs.TypingStartEventArgs args)
        {
            if (args.Channel.Id != ChannelId) return;
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

        public Chat(ulong id, bool allowDefault = false)
        {
            typingTimer.Elapsed += TypingTimer_Elapsed;
            typingTimer.AutoReset = false;
            Hide();

            if (allowDefault)
            {
                SettingsManager.Instance.SelectedChannels.TryGetValue(id, out ulong channelId);
                if (Discord.Client.TryGetCachedChannel(channelId, out DiscordChannel channel))
                {
                    ChannelId = id;
                }
                else
                {
                    // get the key of `id` in the dictionary
                    var key = SettingsManager.Instance.SelectedChannels.FirstOrDefault(x => x.Value == id).Key;
                    if (Discord.Client.TryGetCachedGuild(key, out DiscordGuild guild))
                    {
                        // get the first channel in the guild
                        var firstChannel = guild.Channels.Values.FirstOrDefault(x => x.Type == ChannelType.Text && x.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels));
                        if (firstChannel is not null)
                        {
                            ChannelId = firstChannel.Id;
                        } else
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
            Task.Run(BeginDiscordLoop);
            DataContext = ViewModel;
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
            Discord.Client.TypingStarted += OnType;
            Discord.Client.MessageCreated += OnMessageCreation;
            Discord.Client.MessageDeleted += OnMessageDeleted;
            Discord.Client.MessageUpdated += OnMessageUpdated;
            Discord.Client.ChannelCreated += OnChannelCreated;
            Discord.Client.ChannelDeleted += OnChannelDeleted;
            Discord.Client.ChannelUpdated += OnChannelUpdated;
            Discord.Client.PresenceUpdated += OnPresenceUpdated;
            Discord.Client.VoiceStateUpdated += OnVoiceStateUpdated;
            DrawingCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsManager.Instance.SelectedTimeFormat))
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var message in ViewModel.Messages)
                    {
                        // Update each message
                        message.RaisePropertyChanged(nameof(MessageViewModel.TimestampString));
                    }

                    // Force the collection to refresh
                    // (iL - 21.12.2024) I know that this is a really shitty way to force the UI to update,
                    // but I wasn't able to implement the live updating any other way after 
                    // fooling around with it for an hour.
                    // Maybe you have a better idea? :-)
                    var tempMessages = ViewModel.Messages.ToList();
                    ViewModel.Messages.Clear();
                    foreach (var msg in tempMessages)
                    {
                        ViewModel.Messages.Add(msg);
                    }
                });
            }
        }

        private async Task OnVoiceStateUpdated(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            if (args.Guild.Id != Channel.Guild?.Id) return;
            Dispatcher.Invoke(RefreshChannelList);
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
            Dispatcher.Invoke(RefreshChannelList);
        }

        private async Task OnChannelDeleted(DiscordClient sender, DSharpPlus.EventArgs.ChannelDeleteEventArgs args)
        {
            if (args.Channel.Guild?.Id != Channel.Guild?.Id || Channel.Guild == null) return;
            Dispatcher.Invoke(RefreshChannelList);
            if (args.Channel.Id == Channel.Id)
            {
                var newChannel = ViewModel.Categories.ElementAt(0).Items.ElementAt(0);
                ChannelId = newChannel.Id;
                newChannel.IsSelected = true;
                Dispatcher.Invoke(() => OnChannelChange());
            }
        }

        private async Task OnChannelCreated(DiscordClient sender, DSharpPlus.EventArgs.ChannelCreateEventArgs args)
        {
            if (args.Channel.Guild?.Id != Channel.Guild?.Id || Channel.Guild == null) return;
            Dispatcher.Invoke(RefreshChannelList);
        }

        private async Task OnMessageUpdated(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs args)
        {
            // get the message from the collection
            Dispatcher.Invoke(() =>
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
            Dispatcher.Invoke(() =>
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
            Discord.Client.MessageDeleted -= OnMessageDeleted;
            Discord.Client.MessageUpdated -= OnMessageUpdated;
            Discord.Client.ChannelCreated -= OnChannelCreated;
            Discord.Client.ChannelDeleted -= OnChannelDeleted;
            Discord.Client.ChannelUpdated -= OnChannelUpdated;
            Discord.Client.PresenceUpdated -= OnPresenceUpdated;
            Discord.Client.VoiceStateUpdated -= OnVoiceStateUpdated;

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

        private async Task SendMessage(string value, Stream? attachment = null, int attachmentWidth = 0, int attachmentHeight = 0)
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
                MessageEntity = fakeMsg,
            });

            if (attachment != null)
            {
                // XXX kawapure: DISABLED TEMPORARILY FOR AEROCHAT USER DISCORD ACCOUNT SAFETY
                // Re-enable in public builds only when confirmed to have no negative impact.
                // Attachments currently cause account to be marked as spam.

#if DEBUG // kawapure: See above comment.
                ViewModel.Messages[^1].Attachments.Add(new()
                {
                    Id = 0,
                    Width = attachmentWidth,
                    Height = attachmentHeight,
                    MediaType = Enums.MediaType.Image,
                    Url = "",
                    Name = "attachment.png",
                    Size = "Uploading..."
                });
#endif
            }
            try
            {

                //await new DiscordMessageBuilder()
                //    .WithContent(value)
                //    .AddFile("attachment.png", attachment!)
                //    .SendAsync(Channel);

                var builder = new DiscordMessageBuilder()
                    .WithContent(value);

#if DEBUG // kawapure: See above comment.
                if (attachment != null)
                {
                    builder.AddFile("attachment.png", attachment);
                }
#endif

                await builder.SendAsync(Channel);
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
                ViewModel.BottomHeight = 64;
                sizeTainted = false;
                await SendMessage(value);
            }
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
                int min = 64;
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
                if (!Discord.Client.TryGetCachedChannel(item.Id, out DiscordChannel channel)) return;
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
                        await VoiceManager.Instance.JoinVoiceChannel(channel);
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

            // get the rect of the image
            var imgRect = media.TransformToAncestor(this).TransformBounds(new Rect(0, 0, media.ActualWidth, media.ActualHeight));
            var wndRect = new Rect(Left, Top, Width, Height);

            // add wndRect to imgRect so its offset properly
            imgRect.Offset(wndRect.Left, wndRect.Top);

            var attachmentVm = media.DataContext as AttachmentViewModel;
            if (attachmentVm is null) return;

            var imagePreviewer = new ImagePreviewer(attachmentVm, imgRect, wndRect);
            // set its position to the center of this window
            imagePreviewer.Left = Left + (Width - imagePreviewer.Width) / 2;
            imagePreviewer.Top = Top + (Height - imagePreviewer.Height) / 2;
            imagePreviewer.Show();
        }

        private async void LeaveCallButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await VoiceManager.Instance.LeaveVoiceChannel();
        }

        private void MessageTextBox_SizeChanged(object sender, ScrollChangedEventArgs e)
        {
            if (isDraggingBottomSeperator) return;
            var textBox = (TextBox)sender;
            var newHeight = (int)textBox.ExtentHeight + 40;
            if ((ViewModel.BottomHeight > newHeight && sizeTainted) || newHeight > 200) return;
            ViewModel.BottomHeight = Math.Max(newHeight, 64);
        }

        private async void CanvasButton_Click(object sender, RoutedEventArgs e)
        {
            Dialog dialog = new(
                "Error",
                "Due to Discord flagging accounts using this feature as spammers, access to the " +
                "drawing feature has been temporarily disabled.",
                SystemIcons.Error
            );
            dialog.ShowDialog();
            return;

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

            //using (MemoryStream ms = new())
            //{
            //    encoder.Save(ms);
            //    await SendMessage("", ms);
            //}

            // write to tmp.png next to the .exe
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

        private void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Added.Count > 0)
            {
                _undoStack.Push(e.Added[0]);
                _redoStack.Clear();
            }
        }

        public void Undo()
        {
            if (DrawingCanvas.Strokes.Count == 0) return;
            var stroke = DrawingCanvas.Strokes.Last();
            _redoStack.Push(stroke);
            _undoStack.Pop();
            DrawingCanvas.Strokes.Remove(stroke);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var stroke = _redoStack.Pop();
            DrawingCanvas.Strokes.StrokesChanged -= Strokes_StrokesChanged; // Disable the event
            DrawingCanvas.Strokes.Add(stroke);
            DrawingCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged; // Re-enable the event
            _undoStack.Push(stroke);
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // handle (ctrl) + z | v
            if (!e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.LeftCtrl)) && !e.KeyStates.HasFlag(Keyboard.GetKeyStates(Key.RightCtrl))) return;
            if (e.Key == Key.Z)
            {
                Undo();
            }
            else if (e.Key == Key.Y)
            {
                Redo();
            }
        }

        private void SwitchToText_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MessageTextBox.Visibility == Visibility.Visible) return;
            _drawingHeight = ViewModel.BottomHeight;
            ViewModel.BottomHeight = _writingHeight;
            MessageTextBox.Visibility = Visibility.Visible;
            DrawingContainer.Visibility = Visibility.Collapsed;
        }

        private void SwitchToDraw_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Dialog dialog = new(
                "Error",
                "Due to Discord flagging accounts using this feature as spammers, access to the " +
                "drawing feature has been temporarily disabled.",
                SystemIcons.Error
            );
            dialog.ShowDialog();
            return;

            if (MessageTextBox.Visibility == Visibility.Collapsed) return;
            _writingHeight = ViewModel.BottomHeight;
            ViewModel.BottomHeight = _drawingHeight;
            MessageTextBox.Visibility = Visibility.Collapsed;
            DrawingContainer.Visibility = Visibility.Visible;
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
            Dispatcher.Invoke(() =>
            {
                if (!_isTyping) return;
                if (MessageTextBox.Text == _lastValue || MessageTextBox.Text == string.Empty)
                {
                    _isTyping = false;
                    return;
                }
                _lastValue = MessageTextBox.Text;
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
            };
        }
    }

    public struct DoStroke
    {
        public string ActionFlag { get; set; }
        public System.Windows.Ink.Stroke Stroke { get; set; }
    }
}