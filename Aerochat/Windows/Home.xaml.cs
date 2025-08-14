﻿using Aerochat.Helpers;
using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DiscordProtos.DiscordUsers.V1;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Vanara.PInvoke;
using static Aerochat.ViewModels.HomeListViewCategory;
using Timer = System.Timers.Timer;

namespace Aerochat.Windows
{
    public partial class Home : Window
    {
        private Dictionary<ulong, Timer> _typingTimers = new();
        public HomeWindowViewModel ViewModel { get; } = new HomeWindowViewModel();
        private Timer _hoverTimer = new(50);
        private Timer _adTimer = new(20000);

        private static List<AdViewModel> _ads = new();

        public int AdIndex { get; set; } = 0;

        /// <summary>
        /// The base URL used for dynamic notices and news content.
        /// </summary>
        const string DYNAMIC_BASE_URL = "https://raw.githubusercontent.com/not-nullptr/Aerochat/refs/heads/main/Dynamic/";

        /// <summary>
        /// The URL for remote dynamic news content shown along the bottom of the client.
        /// </summary>
        const string DYNAMIC_NEWS_URL    = DYNAMIC_BASE_URL + "news.json";

        /// <summary>
        /// The URL for remote notices shown along the top of the client until dismissal.
        /// </summary>
        const string DYNAMIC_NOTICES_URL = DYNAMIC_BASE_URL + "notices.json";

        public PresenceViewModel? FindPresenceForUserId(ulong userId)
        {
            foreach (var category in ViewModel.Categories)
            {
                foreach (var item in category.Items)
                {
                    if (item.Id == userId)
                    {
                        return item.Presence;
                    }
                }
            }

            return null;
        }

        public Home()
        {
            InitializeComponent();

            // Invoke used to prevent the constructor from returning before our work is done.
            Dispatcher.Invoke(async () =>
            {
                ViewModel.CurrentUser = UserViewModel.FromUser(Discord.Client.CurrentUser);

                // Load initial presence:
                PreloadedUserSettings? userSettings = DiscordUserSettingsManager.Instance.UserSettingsProto;
                if (userSettings is not null)
                    ViewModel.CurrentUser.Presence = PresenceViewModel.GetPresenceForCurrentUser(userSettings);

                ViewModel.Categories.Clear();
                DataContext = ViewModel;
                Loaded += HomeListView_Loaded;

                // Set default visibilities of optional homepage elements:
                UpdateAdVisibility();
                UpdateNewsVisibility();
                UpdateDiscordServerLinkVisibility();

                await Client_Ready(Discord.Client, null);

                // Subscribe to changes in the DisplayAds property
                SettingsManager.Instance.PropertyChanged += OnSettingsChange;
            });
        }

        private void UpdateAdVisibility()
        {
            AdContainer.Visibility = SettingsManager.Instance.DisplayAds ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateNewsVisibility() =>
            NewsContainer.Visibility = SettingsManager.Instance.DisplayHomeNews ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateDiscordServerLinkVisibility() =>
            DiscordServerLinkContainer.Visibility = SettingsManager.Instance.DisplayDiscordServerLink ? Visibility.Visible : Visibility.Collapsed;

        private void OnSettingsChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsManager.Instance.DisplayAds))
            {
                Dispatcher.BeginInvoke(UpdateAdVisibility);
            }
            else if (e.PropertyName == nameof(SettingsManager.Instance.DisplayHomeNews))
            {
                Dispatcher.BeginInvoke(UpdateNewsVisibility);
            }
            else if (e.PropertyName == nameof(SettingsManager.Instance.DisplayDiscordServerLink))
            {
                Dispatcher.BeginInvoke(UpdateDiscordServerLinkVisibility);
            }
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
                            item.Image="/Aerochat;component/Resources/Frames/XSFrameActiveM.png";
                            break;
                        }
                        else
                        {
                            item.Image="/Aerochat;component/Resources/Frames/XSFrameIdleM.png";
                        }
                    }
                }
            }
        }

        private Random _random = new();
        Dictionary<int, int> adWeights = new();

        private int GetNextAdIndex()
        {
            int totalWeight = adWeights.Values.Sum();

            int randomWeight = _random.Next(totalWeight);

            int cumulativeWeight = 0;
            for (int i = 0; i < _ads.Count; i++)
            {
                cumulativeWeight += adWeights[i];
                if (randomWeight < cumulativeWeight)
                {
                    return i;
                }
            }
            return 0;
        }

        private async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            await Dispatcher.BeginInvoke(() =>
            {
                newsTimer.Elapsed += NewsTimer_Elapsed;
                newsTimer.Start();
                ViewModel.Buttons.Add(new()
                {
                    Image="/Aerochat;component/Resources/Icons/DiscordIcon.png",
                    Click = () =>
                    {
                        Process.Start(new ProcessStartInfo("https://discord.gg/Jcg84hmSqM") { UseShellExecute = true });
                    }
                });
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Aerochat.Ads.Ads.xml";
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                using StreamReader reader = new(stream);
                string result = reader.ReadToEnd();
                XDocument doc = XDocument.Parse(result);
                foreach (XElement adXml in doc.Root?.Elements() ?? [])
                {
                    AdViewModel ad = AdViewModel.FromAd(adXml);
                    _ads.Add(ad);
                }
                Random random = new();
                AdIndex = random.Next(_ads.Count);

                for (int i = 0; i < _ads.Count; i++)
                {
                    adWeights[i] = 1;
                }

                _adTimer.Elapsed += (s, e) =>
                {
                    AdIndex = GetNextAdIndex();

                    for (int i = 0; i < _ads.Count; i++)
                    {
                        if (i == AdIndex)
                        {
                            adWeights[i] = 1;
                        }
                        else
                        {
                            adWeights[i]++;
                        }
                    }

                    AdViewModel adVm = _ads[AdIndex];
                    ViewModel.Ad = adVm;
                };

                ViewModel.Ad = _ads[AdIndex];
                _adTimer.Start();

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
                Discord.Client.VoiceStateUpdated += VoiceStateUpdatedEvent;
                DiscordUserSettingsManager.Instance.UserSettingsUpdated += OnUserSettingsUpdated;

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

                Task.Run(async () =>
                {
                    while (true)
                    {
                        _ = CheckForUpdates();
                        _ = GetNewNews();
                        _ = GetNewNotices();
                        await Task.Delay(60 * 5 * 1000);
                    }
                });

#if AEROCHAT_RC && !DEVELOPER_PRERELEASE
                Dialog betaNoticeDlg = new(
                    "Notice",
                    "This is a work-in-progress beta copy of Aerochat. Please stay updated with the GitHub " +
                    "page for the full release. You will be prompted to install the full release when it " +
                    "comes out.",
                    SystemIcons.Information
                );
                betaNoticeDlg.Owner = null;
                betaNoticeDlg.ShowDialog();
#endif

#if RELEASE && !AEROCHAT_RC
                if (SettingsManager.Instance.ShowBetaWarning)
                {
                    Dialog betaNoticeDlg = new(
                        "Notice",
                        "Aerochat is currently early in development. Many features are currently unimplemented. " +
                        "You will probably not be able to daily drive it. \n\n" +
                        "Please keep this in mind when reporting bugs.",
                        SystemIcons.Information
                    );
                    betaNoticeDlg.Owner = this;
                    betaNoticeDlg.ShowDialog();
                }
#endif

                OpenChatQueue.Instance.ExecuteQueue();
                OpenChatQueue.Instance.ExecuteOnAdd = true;
            });
        }

        private void OnUserSettingsUpdated(object? sender, DiscordUserSettingsUpdateEventArgs e)
        {
            ViewModel.CurrentUser.Presence = PresenceViewModel.GetPresenceForCurrentUser(e.NewSettings);
        }

        private void Image_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            AdIndex = GetNextAdIndex();

            for (int i = 0; i < _ads.Count; i++)
            {
                if (i == AdIndex)
                {
                    adWeights[i] = 1;
                }
                else
                {
                    adWeights[i]++;
                }
            }

            // Reset the ad switch timer so it isn't desynchronised by this forceful
            // change. This is particularly important for some animated ads, which
            // may not get to play their full contents if the timer length is severely
            // desynchronised from continous skipping.
            _adTimer.Stop();
            _adTimer.Start();

            ViewModel.Ad = _ads[AdIndex];
        }

        private void NewsTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // go to the next news item
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var index = ViewModel.News.IndexOf(ViewModel.CurrentNews);
                    if (index == ViewModel.News.Count - 1)
                    {
                        ViewModel.CurrentNews = ViewModel.News[0];
                    }
                    else
                    {
                        ViewModel.CurrentNews = ViewModel.News[index + 1];
                    }
                }
                catch
                {
                    ViewModel.CurrentNews ??= new NewsViewModel();
                    ViewModel.CurrentNews.Body = "Failed to fetch news.";
                }
            });
        }

        private Timer newsTimer = new(20000);

        public void SetNews(NewsViewModel news)
        {
            ViewModel.CurrentNews = news;
            var animation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            NewsText.BeginAnimation(OpacityProperty, animation);
            // reset the timer
            newsTimer.Stop();
            newsTimer.Start();
        }

        /// <summary>
        /// Gets the user agent we report when making a request to remote servers.
        /// </summary>
        private static string GetAerochatUserAgent()
        {
            return "Aerochat/" + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3);
        }

        /// <summary>
        /// Retrieves new news headlines from the remote server. This is shown along the bottom of the home window
        /// at all times unless disabled in the user's appearance settings.
        /// </summary>
        public async Task GetNewNews()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", GetAerochatUserAgent());
            try
            {
                var response = await httpClient.GetAsync(DYNAMIC_NEWS_URL + "?breaker=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var news = JsonDocument.Parse(await response.Content.ReadAsStringAsync(), new JsonDocumentOptions()
                        {
                            AllowTrailingCommas = true,
                            CommentHandling = JsonCommentHandling.Skip,
                        });
                        var newsList = new List<NewsViewModel>();
                        foreach (var n in news.RootElement.EnumerateArray())
                        {
                            newsList.Add(NewsViewModel.FromNews(n));
                        }

                        _ = Dispatcher.BeginInvoke(() =>
                        {
                            ViewModel.News.Clear();
                            foreach (var n in newsList)
                            {
                                ViewModel.News.Add(n);
                            }

                            ViewModel.CurrentNews = ViewModel.News.FirstOrDefault(x => x.Date == ViewModel.CurrentNews?.Date) ?? ViewModel.News.FirstOrDefault();
                        });
                    }
                    catch (JsonException)
                    {
                        // The content is not valid JSON. Ignore.
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Doesn't matter.
            }
        }

        /// <summary>
        /// Retrieves new notices which are shown along the top of the server list periodically as they update.
        /// </summary>
        public async Task GetNewNotices()
        {
            var noticesList = new List<NoticeViewModel>();
            
            // Get the latest notices from the GitHub repo.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", GetAerochatUserAgent());
            try
            {
                var response = await httpClient.GetAsync(DYNAMIC_NOTICES_URL + "?breaker=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var notices = JsonDocument.Parse(await response.Content.ReadAsStringAsync(), new JsonDocumentOptions()
                        {
                            AllowTrailingCommas = true,
                            CommentHandling = JsonCommentHandling.Skip,
                        });
                        // this is an array so iterate through it
                        foreach (var notice in notices.RootElement.EnumerateArray())
                        {
                            var noticeViewModel = NoticeViewModel.FromNotice(notice);
                            if (!noticeViewModel.IsTargeted || SettingsManager.Instance.ViewedNotices.Contains(noticeViewModel.Date)) continue;
                            noticesList.Add(noticeViewModel);
                        }

                        _ = Dispatcher.BeginInvoke(() =>
                        {
                            ViewModel.Notices.Clear();
                            foreach (var n in noticesList)
                            {
                                ViewModel.Notices.Add(n);
                            }
                        });
                    }
                    catch (JsonException)
                    {
                        // The content is not valid JSON. Ignore.
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Doesn't matter.
            }
        }
        private void CloseNoticeButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var notice = ViewModel.Notices[0];
            SettingsManager.Instance.ViewedNotices.Add(notice.Date);
            ViewModel.Notices.Remove(notice);
            SettingsManager.Save();
        }

        private bool showingUpdate = false;

        public async Task CheckForUpdates()
        {
#if DEBUG || DEVELOPER_PRERELEASE
            return;
#endif
            if (showingUpdate)
                return;

            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("User-Agent", GetAerochatUserAgent());

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync("https://api.github.com/repos/not-nullptr/Aerochat/tags");
            }
            catch (Exception)
            {
                // Ignore networking exception.
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Ignore unsuccessful requests.
                return;
            }

            JsonDocument tags = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            string? latestTag;
            try
            {
                latestTag = tags.RootElement[0].GetProperty("name").GetString();
            }
            catch (Exception)
            {
                return;
            }

            if (latestTag == null)
                return;

#if !AEROCHAT_RC
            var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
#else
            var localVersion = Version.Parse(AssemblyInfo.RC_LAST_VERSION);
#endif
            if (localVersion == null)
                return;

            string latestTagVersion = latestTag.Split('-')[0];
            
            if (latestTagVersion.StartsWith('v'))
            {
                latestTagVersion = latestTagVersion.Remove(0, 1);
            }

            Version remoteVersion = new(latestTagVersion);
            if (localVersion.CompareTo(remoteVersion) < 0)
            {
                _ = Dispatcher.BeginInvoke(async () =>
                {
                    showingUpdate = true;
                    Dialog dialog = new(
                        "A new version is available", 
                        $"Version {remoteVersion} has been released, but you currently have {localVersion.ToString()}. Press Continue to update.", 
                        SystemIcons.Information
                    );
                    dialog.Owner = this;
                    dialog.ShowDialog();
                    Hide();

                    //close all windows other than this one
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != this)
                            window.Close();
                    }
                    _ = Task.Run(async () =>
                    {
                        HttpResponseMessage releaseResponse;
                        try
                        {
                            releaseResponse = await httpClient.GetAsync($"https://api.github.com/repos/not-nullptr/Aerochat/releases/tags/{latestTag}");
                        }
                        catch (Exception)
                        {
                            await ShowAutomaticUpdateDownloadFailureDialog(latestTag);
                            Dispatcher.BeginInvoke(Close);
                            return;
                        }

                        JsonDocument release = JsonDocument.Parse(await releaseResponse.Content.ReadAsStringAsync());
                        string? assetUrl = null;
                        try
                        {
                            var assets = release.RootElement.GetProperty("assets");

                            if (assets.GetArrayLength() > 0)
                            {
                                assetUrl = assets[0].GetProperty("browser_download_url").GetString();
                            }
                        }
                        catch (Exception)
                        {
                            await ShowAutomaticUpdateDownloadFailureDialog(latestTag);
                            Dispatcher.BeginInvoke(Close);
                            return;
                        }

                        if (assetUrl == null)
                        {
                            await ShowAutomaticUpdateDownloadFailureDialog(latestTag);
                            Dispatcher.BeginInvoke(Close);
                            return;
                        }

                        // get a temp folder
                        string tempFolder = Path.GetTempPath();
                        string tempSetupExePath = Path.Combine(tempFolder, "aerochat-setup.exe");

                        // download the asset to the temp folder
                        var asset = await httpClient.GetAsync(assetUrl);
                        byte[] assetBytes = await asset.Content.ReadAsByteArrayAsync();

                        try
                        {
                            File.WriteAllBytes(tempSetupExePath, assetBytes);

                            // ShellExecute will open the UAC prompt, rather than trying to open the application with the same
                            // permissions as the current process and potentially failing.
                            Shell32.ShellExecute(HWND.NULL, "open", tempSetupExePath, null, null, ShowWindowCommand.SW_SHOWNORMAL);
                        }
                        catch (Exception)
                        {
                            await ShowAutomaticUpdateDownloadFailureDialog(latestTag);
                            Dispatcher.BeginInvoke(Close);
                            return;
                        }

                        Dispatcher.BeginInvoke(Close);
                    });
                });
            }
            else
            {
                httpClient.Dispose();
                tags.Dispose();
            }
        }

        private async Task ShowAutomaticUpdateDownloadFailureDialog(string latestTag)
        {
            // We couldn't fetch the release for whatever reason, so inform the user of the error and
            // open the release's link in the user's browser:
            await Dispatcher.InvokeAsync(() =>
            {
                Dialog failureDialog = new(
                    "Failed to download update",
                    "We failed to automatically download this update. We will open the download " +
                    "link in your browser instead.",
                    SystemIcons.Warning
                );
                failureDialog.Owner = this;
                failureDialog.ShowDialog();

                Shell32.ShellExecute(HWND.NULL, "open",
                    $"https://github.com/not-nullptr/Aerochat/releases/tag/{latestTag}", null, null,
                    ShowWindowCommand.SW_SHOWNORMAL
                );
            });
        }

        private async Task VoiceStateUpdatedEvent(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
        {
            if (args.Guild is null) return;
            var voiceStates = args.Guild.GetType().GetField("_voiceStates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(args.Guild) as ConcurrentDictionary<ulong, DiscordVoiceState>;
            if (voiceStates is null) return;
            if (args.Channel is null)
            {
                voiceStates.TryRemove(args.User.Id, out _);
            }
            else
            {
                voiceStates[args.User.Id] = args.After;
            }
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

            item.Image="/Aerochat;component/Resources/Frames/XSFrameActiveM.png";

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
                    Dispatcher.BeginInvoke(() =>
                    {
                        item.Image="/Aerochat;component/Resources/Frames/XSFrameIdleM.png";
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
            try
            {
                await Dispatcher.InvokeAsync(() => UpdateStatuses());
            }
            catch (TaskCanceledException)
            {
                // Ignore.
            }
        }

        private void AddGuilds()
        {
            // get all guilds which aren't sorted (ie not in a folder)
            List<ulong> processedGuilds = new();

            if (DiscordUserSettingsManager.Instance.UserSettingsProto?.GuildFolders?.Folders is not null)
            {
                foreach (PreloadedUserSettings.Types.GuildFolder folder in DiscordUserSettingsManager.Instance.UserSettingsProto!.GuildFolders.Folders)
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

                        if (channelsList.Count == 0) continue;

                        CreateAndInsertGuild(guild.Name, channelsList[0].Id, index);
                        processedGuilds.Add(guildId);
                    }
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

                CreateAndInsertGuild(guild.Name, channelsList.ElementAtOrDefault(0)?.Id ?? 0, 1);
            }
            UpdateUnreadMessages();
        }

        private void CreateAndInsertGuild(string name, ulong guildId, int categoryIndex)
        {
            var guildItem = new HomeListItemViewModel
            {
                Name = name,
                Image="/Aerochat;component/Resources/Frames/XSFrameIdleM.png",
                Presence = new PresenceViewModel
                {
                    Presence = "",
                    Status = "",
                    Type = "",
                },
                IsSelected = false,
                LastMsgId = 0,
                Id = guildId
            };

            ViewModel.Categories[categoryIndex].Items.Add(guildItem);
        }

        private void UpdateStatuses()
        {
            // Update the UI with the sorted list
            _ = Dispatcher.BeginInvoke(() =>
            {
                var oldList = ViewModel.Categories[0].Items;
                var newList = new List<HomeListItemViewModel>();

                // Build the new list from the current private channels
                foreach (var c in Discord.Client.PrivateChannels)
                {
                    var dm = c.Value;
                    bool isGroupChat = dm?.Recipients?.Count > 1;
                    var recipient = dm?.Recipients?.FirstOrDefault();
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

                    if (dm?.Recipients is not null) foreach (DiscordUser user in dm.Recipients)
                        newItem.Recipients.Add(user);

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
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab)
                e.Handled = true;
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (SearchInput.IsFocused)
                {
                    SearchInput.Text = "";
                    SearchInput.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
                else if (PART_StatusInputBox.IsFocused)
                {
                    PART_StatusInputBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (PART_StatusInputBox.IsFocused)
                {
                    PART_StatusInputBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            }
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

            ViewModel.IsVisible = prop;
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
            var item = (HomeListViewCategory)((FrameworkElement)sender).DataContext;
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

        private async void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (HomeListItemViewModel)((Button)sender).DataContext;
            // is a window already open for this item?
            Chat? chat = Application.Current.Windows.OfType<Chat>().FirstOrDefault(x => 
                x?.ViewModel?.Recipient?.Id == item.Id || 
                x?.Channel?.Id == item.Id || 
                (x?.Channel?.Guild?.Channels.Values?.Select(x => x.Id)?.Contains(item.Id) ?? false));
            if (chat is null)
            {
                // We send over the presence of the item in case this is a one-on-one DM, where the Discord
                // API doesn't initially report this state.
                new Chat(item.Id, true, item.Presence, item);
            }
            else
            {
                // move the chat to the center of this window
                var rect = chat.RestoreBounds;

                // Avoid infinity values to avoid an ArgumentException.
                if (rect.Width == double.NegativeInfinity ||
                    rect.Width == double.PositiveInfinity)
                {
                    rect = new Rect(0, 0, 100, 100);
                }

                if (rect.Height == double.NegativeInfinity ||
                    rect.Height == double.PositiveInfinity)
                {
                    rect = new Rect(0, 0, 100, 100);
                }

                chat.Left = Left + (Width - rect.Width) / 2;
                chat.Top = Top + (Height - rect.Height) / 2;
                await chat.ExecuteNudgePrettyPlease(chat.Left, chat.Top, 0.5, 15);
            }
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

            // Entire menu here is completely unimplemented, so we don't bother with it.
            if (SettingsManager.Instance.DisplayUnimplementedButtons)
            {
                _hoverTimer.Stop();
                _hoverTimer.Start();
                tooltip?.StopKillTimer();
            }
        }

        private void MouseExitedUser(object sender, MouseEventArgs e)
        {
            // grab the control which the user has exited
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement?.DataContext is HomeListItemViewModel item)
            {
                if (SettingsManager.Instance.DisplayUnimplementedButtons)
                {
                    _hoverTimer.Stop();
                    tooltip?.StartKillTimer();
                }
            }
        }

        private void OnTimerEnd(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
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
                    var pos = _lastHoveredControl.PointToScreen(new System.Windows.Point(0, 0));
                    tooltip.Left = pos.X - tooltip.Width - 56;
                    tooltip.Top = pos.Y;
                };

                tooltip.Show();
            });
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UriBuilder builder = new(ViewModel.Ad.Url);
            var segments = builder.Path.Split('/');
            if (builder.Host == "web.archive.org" && segments.Length > 2 && !segments[2].EndsWith("if_"))
            {
                segments[2] += "if_";
            }
            builder.Path = string.Join("/", segments);
            var uri = builder.Uri;
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
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
            settings.ShowDialog();
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
            SceneTileImage.Image = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Home/PageOpen.png"));
            SceneTileImage.Reset();
            Debug.WriteLine("Enter");
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            SceneTileImage.Image = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Home/PageClose.png"));
            SceneTileImage.Reset();
            Debug.WriteLine("Exit");
        }

        private void SceneTileImage_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            new ChangeScene().ShowDialog();
        }

        private void CreditsBtn_Click(object sender, RoutedEventArgs e)
        {
            new About().ShowDialog();
        }

        private void DebugBtn_Click(object sender, RoutedEventArgs e)
        {
            new DebugWindow().Show();
        }

        private void PreviousNewsItem_Click(object sender, RoutedEventArgs e)
        {
            // get the current index
            if (ViewModel.CurrentNews is null) return;
            var index = ViewModel.News.IndexOf(ViewModel.CurrentNews);
            SetNews(ViewModel.News[(index - 1 + ViewModel.News.Count) % ViewModel.News.Count]);
        }

        private void NextNewsItem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentNews is null) return;
            var index = ViewModel.News.IndexOf(ViewModel.CurrentNews);
            SetNews(ViewModel.News[(index + 1) % ViewModel.News.Count]);
        }

        private void StatusDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (DiscordUserSettingsManager.Instance.UserSettingsProto?.Status.CustomStatus == null)
            {
                PART_StatusInputBox.Text = "";
            }
            else
            {
                PART_StatusInputBox.Text = PART_StatusStaticView.Text;
            }

            ViewModel.IsEditingStatus = true;

            PART_StatusInputBox.Focus();
        }

        private void PART_StatusInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            OnStatusInputBoxLostFocus();
        }

        private void OnStatusInputBoxLostFocus()
        {
            ViewModel.IsEditingStatus = false;

            if (PART_StatusInputBox.Text != PART_StatusStaticView.Text && DiscordUserSettingsManager.Instance.UserSettingsProto != null &&
                ViewModel.CurrentUser.Presence != null && !(ViewModel.CurrentUser.Presence.CustomStatus == null && PART_StatusInputBox.Text == ""))
            {
                // Update the text for the brief period before the remote text is updated.
                ViewModel.CurrentUser.Presence!.CustomStatus = PART_StatusInputBox.Text;

                if (PART_StatusInputBox.Text == "")
                {
                    // Revert to the placeholder text:
                    ViewModel.CurrentUser.Presence.CustomStatus = null;

                    DiscordUserSettingsManager.Instance.UserSettingsProto.Status.CustomStatus = null;
                }
                else
                {
                    string croppedText = PART_StatusInputBox.Text;

                    // Ensure that the text fits into the limit given by Discord.
                    if (croppedText.Length > 128)
                    {
                        croppedText = croppedText.Substring(0, 128);
                    }

                    if (DiscordUserSettingsManager.Instance.UserSettingsProto.Status.CustomStatus == null)
                    {
                        DiscordUserSettingsManager.Instance.UserSettingsProto.Status.CustomStatus = new();
                    }

                    DiscordUserSettingsManager.Instance.UserSettingsProto.Status.CustomStatus.Text = PART_StatusInputBox.Text;
                }

                _ = DiscordUserSettingsManager.Instance.UpdateRemote();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement? focusedElement = Keyboard.FocusedElement;
            Keyboard.ClearFocus();

            if (focusedElement != null)
                focusedElement.RaiseEvent(new RoutedEventArgs(LostFocusEvent));
        }


        private void OnDoubleClickTreeViewExpander(object sender, MouseButtonEventArgs e)
        {
            // In order to avoid double actions from occurring when the expander button is clicked (which
            // takes action after a single input), we ignore clicks going to that area. As an easy hack,
            // we just hit test and ignore the action if the mouse is not over the expander button.
            FrameworkElement? expanderButton = ((FrameworkElement)sender).FindName("PART_ExpanderButton") as FrameworkElement;

            bool isInExpanderButton = false;

            if (expanderButton != null)
            {
                HitTestResult? hitTest = VisualTreeHelper.HitTest(this, e.GetPosition(this));

                if (hitTest?.VisualHit == expanderButton)
                {
                    isInExpanderButton = true;
                }
            }

            if (e.ClickCount == 2 && !e.Handled && !isInExpanderButton)
            {
                ItemToggleCollapse(sender, e);
            }
        }
    }
}
