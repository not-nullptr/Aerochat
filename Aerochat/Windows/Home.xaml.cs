using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Aerochat.ViewModels.HomeListViewCategory;
using static Vanara.PInvoke.User32;
using Timer = System.Timers.Timer;

namespace Aerochat.Windows
{
    public partial class Home : Window
    {
        private Dictionary<ulong, Timer> _typingTimers = new();
        public HomeWindowViewModel ViewModel { get; } = new HomeWindowViewModel();
        private Timer _hoverTimer = new(50);

        private static List<AdViewModel> _ads = new()
        {
            new()
            {
                Image = "/Resources/Ads/Aerochat.png",
                Url = "https://aerochat.live"
            },
            new()
            {
                Image = "/Resources/Ads/N09.png",
                Url = "https://discord.gg/pU4get53Zp"
            },
            new()
            {
                Image = "/Resources/Ads/Liam.png",
                Url = "https://x.com/z2rmc"
            },
            new()
            {
                Image = "/Resources/Ads/Slobbery.png",
                Url = "https://slobbery.wang"
            },
        };

        public int AdIndex { get; set; } = 0;

        public Home()
        {
            InitializeComponent();
            Dispatcher.Invoke(() =>
            {
                ViewModel.CurrentUser = UserViewModel.FromUser(Discord.Client.CurrentUser);
                ViewModel.Categories.Clear();
                DataContext = ViewModel;
                Loaded += HomeListView_Loaded;
                //c heck if we're already ready
                if (Discord.Ready)
                {
                    Client_Ready(Discord.Client, null);
                } else Discord.Client.Ready += Client_Ready;
            });
        }

        public void UpdateUnreadMessages()
        {
            foreach (var category in ViewModel.Categories)
            {
                foreach (var item in category.Items)
                {
                    Discord.Client.TryGetCachedChannel(item.Id, out var c);
                    if (c is null || c is DiscordDmChannel) continue;
                    Discord.Client.TryGetCachedGuild(c.GuildId ?? 0, out var guild);
                    if (guild is null) return;
                    foreach (var channelId in guild.Channels)
                    {
                        bool found = SettingsManager.Instance.LastReadMessages.TryGetValue(channelId.Key, out var lastReadMessageId);
                        DateTime lastReadMessageTime;
                        if (found)
                        {
                            lastReadMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)(lastReadMessageId >> 22) + 1420070400000)).DateTime;
                        }
                        else
                        {
                            lastReadMessageTime = SettingsManager.Instance.ReadRecieptReference;
                        }
                        var channel = guild.Channels[channelId.Key];
                        var lastMessageId = channel.LastMessageId;
                        if ((channel.Type != ChannelType.Text && channel.Type != ChannelType.Announcement) || lastMessageId == null) continue;
                        var lastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)(lastMessageId >> 22) + 1420070400000)).DateTime;
                        if (lastMessageTime > lastReadMessageTime)
                        {
                            item.Image = "/Resources/Frames/XSFrameActiveM.png";
                            break;
                        }
                        else
                        {
                            item.Image = "/Resources/Frames/XSFrameIdleM.png";
                        }
                    }
                }
            }
        }

        private async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                ViewModel.Buttons.Add(new()
                {
                    Image = "/Resources/Icons/DiscordIcon.png",
                    Click = () =>
                    {
                        Process.Start(new ProcessStartInfo("https://discord.gg/Jcg84hmSqM") { UseShellExecute = true });
                    }
                });
                Timer adTimer = new(60000);
                adTimer.Elapsed += (s, e) =>
                {
                    AdIndex++;
                    if (AdIndex >= _ads.Count)
                        AdIndex = 0;
                    ViewModel.Ad = _ads[AdIndex];
                };
                ViewModel.Ad = _ads[AdIndex];

                adTimer.Start();

                ViewModel.Categories.Add(new HomeListViewCategory
                {
                    Name = "Conversations",
                });

                ViewModel.Categories.Add(new HomeListViewCategory
                {
                    Name = "Servers",
                });

                Discord.Client.PresenceUpdated += InvokeUpdateStatuses;
                Discord.Client.ChannelCreated += ChannelCreatedEvent;
                Discord.Client.ChannelDeleted += ChannelDeletedEvent;

                UpdateStatuses();
                AddGuilds();

                Discord.Client.MessageCreated += async (s, e) =>
                {
                    if (e.Channel is DiscordDmChannel)
                    {
                        // find the private channel in Discord.Client.PrivateChannels
                        var dm = Discord.Client.PrivateChannels[e.Channel.Id];
                        // using reflection, set the last message id to the new message id
                        dm.GetType().GetProperty("LastMessageId").SetValue(dm, e.Message.Id);
                        UpdateStatuses();
                    } else
                    {
                        if (!Discord.Client.TryGetCachedGuild(e.Channel.GuildId ?? 0, out var guild)) return;
                        if (!Discord.Client.TryGetCachedChannel(e.Channel.Id, out var channel)) return;
                        if (channel.Type != ChannelType.Text && channel.Type != ChannelType.Announcement) return;

                        guild.Channels[channel.Id].GetType().GetProperty("LastMessageId").SetValue(guild.Channels[channel.Id], e.Message.Id);
                    }

                    UpdateUnreadMessages();
                };

                _hoverTimer.Elapsed += OnTimerEnd;
                _hoverTimer.AutoReset = false;

                Show();
                Focus();
            });
        }

        private async Task OnTyping(DiscordClient sender, DSharpPlus.EventArgs.TypingStartEventArgs args)
        {
            var guildId = args.Channel.GuildId;
            if (guildId is null) return;
            var guild = Discord.Client.TryGetCachedGuild(guildId.Value, out var g) ? g : await Discord.Client.GetGuildAsync(guildId.Value);
            if (guild is null) return;
            HomeListItemViewModel? item = null;
            foreach (var category in ViewModel.Categories)
            {
                item = category.Items.FirstOrDefault(x => guild.Channels.ContainsKey(x.Id));
                if (item != null) break;
            }
            if (item is null) return;

            item.Image = "/Resources/Frames/XSFrameActiveM.png";

            if (_typingTimers.ContainsKey(guildId.Value))
            {
                _typingTimers[guildId.Value].Stop();
                _typingTimers[guildId.Value].Start();
            }
            else
            {
                _typingTimers[guildId.Value] = new(15000);
                _typingTimers[guildId.Value].Elapsed += (s, e) =>
                {
                    _typingTimers[guildId.Value].Stop();
                    Dispatcher.Invoke(() =>
                    {
                        item.Image = "/Resources/Frames/XSFrameIdleM.png";
                    });
                };
                _typingTimers[guildId.Value].Start();
            }
        }

        private async Task ChannelDeletedEvent(DiscordClient sender, DSharpPlus.EventArgs.ChannelDeleteEventArgs args)
        {
            if (args.Channel.GuildId is null) await Dispatcher.InvokeAsync(() => UpdateStatuses());
        }

        private async Task ChannelCreatedEvent(DiscordClient sender, DSharpPlus.EventArgs.ChannelCreateEventArgs args)
        {
            if (args.Channel.GuildId is null) await Dispatcher.InvokeAsync(() => UpdateStatuses());
        }

        private async Task InvokeUpdateStatuses(DiscordClient sender, DSharpPlus.EventArgs.PresenceUpdateEventArgs args)
        {
            await Dispatcher.InvokeAsync(() => UpdateStatuses());
        }

        private void AddGuilds()
        {
            // get all guilds which aren't sorted (ie not in a folder)
            List<ulong> processedGuilds = new();
            foreach (var folder in Discord.Client.UserSettings.GuildFolders)
            {
                var index = 1;
                if (!string.IsNullOrEmpty(folder.Name))
                {
                    var category = new HomeListViewCategory
                    {
                        Name = folder.Name,
                    };
                    ViewModel.Categories.Add(category);
                    index = ViewModel.Categories.IndexOf(category);
                }
                foreach (var guildId in folder.GuildIds)
                {
                    Discord.Client.TryGetCachedGuild(guildId, out var guild);
                    if (guild == null) continue;
                    var channels = guild.Channels.Values;
                    List<DiscordChannel> channelsList = new();
                    foreach (var c in channels)
                    {
                        if ((c.PermissionsFor(guild.CurrentMember) & Permissions.AccessChannels) == Permissions.AccessChannels && c.Type == ChannelType.Text)
                        {
                            channelsList.Add(c);
                        }
                    }

                    channelsList.Sort((x, y) => x.Position.CompareTo(y.Position));


                    var guildItem = new HomeListItemViewModel
                    {
                        Name = guild.Name,
                        Image = "/Resources/Frames/XSFrameIdleM.png",
                        Presence = new PresenceViewModel
                        {
                            Presence = "",
                            Status = "",
                            Type = "",
                        },
                        IsSelected = false,
                        LastMsgId = 0,
                        Id = channelsList[0].Id
                    };
                    guildItem.DoubleClick = () =>
                    {
                        new Chat(guild.Id).Show();
                    };
                    ViewModel.Categories[index].Items.Add(guildItem);

                    processedGuilds.Add(guildId);
                }
            }
            // for each item in uncategorizedGuilds, add it to the Servers folder [1]
            // sorted by join date
            var uncategorizedGuilds = Discord.Client.Guilds
                .Where(x => !processedGuilds.Contains(x.Key))
                .OrderBy(x => x.Value.JoinedAt)
                .Select(x => x.Value);
            foreach (var guild in uncategorizedGuilds)
            {
                var channels = guild.Channels.Values;
                List<DiscordChannel> channelsList = new();
                foreach (var c in channels)
                {
                    if ((c.PermissionsFor(guild.CurrentMember) & Permissions.AccessChannels) == Permissions.AccessChannels && c.Type == ChannelType.Text)
                    {
                        channelsList.Add(c);
                    }
                }

                channelsList.Sort((x, y) => x.Position.CompareTo(y.Position));

                var guildItem = new HomeListItemViewModel
                {
                    Name = guild.Name,
                    Image = "/Resources/Frames/XSFrameIdleM.png",
                    Presence = new PresenceViewModel
                    {
                        Presence = "",
                        Status = "",
                        Type = "",
                    },
                    IsSelected = false,
                    LastMsgId = 0,
                    Id = channelsList[0].Id
                };
                guildItem.DoubleClick = () =>
                {
                    new Chat(guild.Id).Show();
                };
                //ViewModel.Categories[1].Items.Add(guildItem);
                // add to start:
                ViewModel.Categories[1].Items.Insert(0, guildItem);
            }
            UpdateUnreadMessages();
        }

        private async void UpdateStatuses()
        {
            await Task.Run(() =>
            {
                var oldList = ViewModel.Categories[0].Items;
                var newList = new List<HomeListItemViewModel>();

                // Build the new list from the current private channels
                foreach (var c in Discord.Client.PrivateChannels)
                {
                    var dm = c.Value;
                    bool isGroupChat = dm.Recipients.Count > 1;
                    var recipient = dm.Recipients.FirstOrDefault();
                    if (recipient is null) continue;

                    // Create new item or reuse existing item's selection state
                    var existingItem = oldList.ToList().FirstOrDefault(v => v?.Id == dm?.Id);
                    var newItem = new HomeListItemViewModel
                    {
                        Name = isGroupChat ? string.IsNullOrEmpty(dm.Name) ? String.Join(", ", dm.Recipients.Select(r => r.DisplayName)) : dm.Name : recipient.DisplayName,
                        Presence = recipient.Presence == null ? new PresenceViewModel()
                        {
                            Presence = "",
                            Status = recipient.Presence?.Status.ToString() ?? "Offline",
                            Type = "",
                        } : PresenceViewModel.FromPresence(recipient.Presence),
                        LastMsgId = dm.LastMessageId ?? dm.Id,
                        Id = dm.Id,
                        IsSelected = existingItem?.IsSelected ?? false,
                        IsGroupChat = isGroupChat,
                        RecipientCount = dm.Recipients.Count + 1, // to account for ourselves, i think?
                    };

                    newList.Add(newItem);
                }

                // Sort the new list based on the last message time
                newList.Sort((x, y) =>
                {
                    long prevTimestamp = ((long)(x.LastMsgId >> 22)) + 1420070400000;
                    DateTime prevLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(prevTimestamp).DateTime;

                    long nextTimestamp = ((long)(y.LastMsgId >> 22)) + 1420070400000;
                    DateTime nextLastMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(nextTimestamp).DateTime;

                    return nextLastMessageTime.CompareTo(prevLastMessageTime);
                });

                // Update the UI with the sorted list
                Dispatcher.Invoke(() =>
                {
                    var itemsToRemove = oldList.Where(oldItem => !newList.Any(newItem => newItem.Id == oldItem.Id)).ToList();
                    foreach (var itemToRemove in itemsToRemove)
                    {
                        oldList.Remove(itemToRemove); // Remove items that are no longer in the new list
                    }

                    // Update or add new items, maintaining the sorted order
                    foreach (var newItem in newList)
                    {
                        var existingItem = oldList.FirstOrDefault(v => v.Id == newItem.Id);
                        if (existingItem != null)
                        {
                            existingItem.Name = newItem.Name;
                            existingItem.LastMsgId = newItem.LastMsgId;
                            existingItem.IsSelected = newItem.IsSelected;
                            existingItem.Presence = newItem.Presence;
                        }
                        else
                        {
                            // Add new item to the old list in the correct sorted order
                            oldList.Add(newItem);
                        }
                    }

                    // if the resulting lists r the same, return to prevent flickers

                    bool isSame = false;
                    if (oldList.Count == newList.Count)
                    {
                        isSame = true;
                        for (int i = 0; i < oldList.Count; i++)
                        {
                            if (oldList[i].Id != newList[i].Id)
                            {
                                isSame = false;
                                break;
                            }
                        }
                    }
                    if (isSame) return;
                    ViewModel.Categories[0].Items.Clear();
                    foreach (var item in newList)
                    {
                        ViewModel.Categories[0].Items.Add(item);
                    }
                });
            });
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab)
                e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var app = (App)Application.Current;
            if (app.LoggingOut) return;
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this)
                    window.Close();
            }
        }

        public void SetVisibleProperty(bool prop)
        {
            foreach (var item in ViewModel.Categories)
            {
                item.IsVisibleProperty = prop;
            }
        }

        private void HomeListView_Loaded(object sender, RoutedEventArgs e)
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

        private void ItemToggleCollapse(object sender, MouseButtonEventArgs e)
        {
            var item = (HomeListViewCategory)((Image)sender).DataContext;
            item.Collapsed = !item.Collapsed;
        }

        private void ItemClick(object sender, MouseButtonEventArgs e)
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
            }
        }

        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // get the data context of the clicked item
            var item = (HomeListItemViewModel)((Button)sender).DataContext;
            // open a new chat window
            new Chat(item.Id, true);
        }

        private NonNativeTooltip? tooltip;
        private HomeListItemViewModel? _lastHoveredItem;
        private Button _lastHoveredControl;

        private void MouseEnteredUser(object sender, MouseEventArgs e)
        {
            var item = (HomeListItemViewModel)((FrameworkElement)sender).DataContext;
            if (!item.IsSelected) return;
            if (_lastHoveredItem == item) return;
            _lastHoveredItem = item;
            // traverse the parents till we find a Button
            var frameworkElement = sender as FrameworkElement;
            while (frameworkElement != null && !(frameworkElement is Button))
                frameworkElement = VisualTreeHelper.GetParent(frameworkElement) as FrameworkElement;
            _lastHoveredControl = (Button)frameworkElement;
            _hoverTimer.Stop();
            _hoverTimer.Start();
            tooltip?.StopKillTimer();
        }

        private void MouseExitedUser(object sender, MouseEventArgs e)
        {
            // grab the control which the user has exited
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement?.DataContext is HomeListItemViewModel item)
            {
                _hoverTimer.Stop();
                tooltip?.StartKillTimer();
            }
        }

        private void OnTimerEnd(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // if there's a tooltip already open, close it
                tooltip?.Close();
                tooltip = new(new()
                {
                    new()
                    {
                        Name = "Block this user",
                        Key = "block"
                    },

                    new()
                    {
                        Name = "Send an instant message",
                        Key = "msg"
                    }
                });

                tooltip.ItemClicked += (s, e) =>
                {
                    Debug.WriteLine(e.Item.Key);
                };

                tooltip.Closed += (s, e) =>
                {
                    tooltip = null;
                };

                // get the position of the 

                tooltip.Loaded += (s, e) =>
                {
                    var pos = _lastHoveredControl.PointToScreen(new Point(0, 0));
                    tooltip.Left = pos.X - tooltip.Width - 56;
                    tooltip.Top = pos.Y;
                };

                tooltip.Show();
            });
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // open ViewModel.Ad.Url in the user's default browser
            Process.Start(new ProcessStartInfo(ViewModel.Ad.Url) { UseShellExecute = true });
        }

        private void NameDropdown_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = ((Button)sender).ContextMenu;
            contextMenu.PlacementTarget = (Button)sender;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private async void Available_Click(object sender, RoutedEventArgs e)
        {
            await App.SetStatus(UserStatus.Online);
        }

        private async void Busy_Click(object sender, RoutedEventArgs e)
        {
            await App.SetStatus(UserStatus.DoNotDisturb);
        }

        private async void Away_Click(object sender, RoutedEventArgs e)
        {
            await App.SetStatus(UserStatus.Idle);
        }

        private async void AppearOffline_Click(object sender, RoutedEventArgs e)
        {
            await App.SetStatus(UserStatus.Invisible);
        }

        private void OptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new();
            settings.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // get the data context of the clicked item
            var item = (HomeButtonViewModel)((Button)sender).DataContext;
            // get the one in ViewModel.Buttons
            var button = ViewModel.Buttons.FirstOrDefault(x => x == item);
            // run the click action
            button?.Click?.Invoke();
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            await app.SignOut();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            SceneTileImage.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/Home/PageOpen.png"));
            SceneTileImage.Reset();
            Debug.WriteLine("Enter");
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            SceneTileImage.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/Home/PageClose.png"));
            SceneTileImage.Reset();
            Debug.WriteLine("Exit");
        }

        private void SceneTileImage_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            new ChangeScene().ShowDialog();
        }
    }
}
