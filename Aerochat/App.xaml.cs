using Aerochat.Hoarder;
using Aerochat.ViewModels;
using Aerochat.Windows;
using Aerochat.Settings;
using DSharpPlus.Entities;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static Vanara.PInvoke.User32;
using Vanara.PInvoke;
using Timer = System.Timers.Timer;
using System.Drawing;
using System.Runtime.InteropServices;
using Aerochat.Theme;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using DSharpPlus;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using Aerochat.Settings;
using System.Windows.Shell;
using System.Windows.Media.Imaging;
using DSharpPlus.Enums;
using Aerovoice.Clients;
using DiscordProtos.DiscordUsers.V1;
using System.Buffers.Text;
using Google.Protobuf;
using DSharpPlus.EventArgs;
using Aerochat.Helpers;
using System.Threading.Channels;
using static ICSharpCode.AvalonEdit.Document.TextDocumentWeakEventManager;
using Markdig.Extensions.Footnotes;
using System.IO.Pipes;
using System.Windows.Media.Animation;
using Aerochat.Enums;
using DSharpPlus.Exceptions;

namespace Aerochat
{
    public partial class App : Application
    {
        public static Guid _appGuid;
        private static Mutex? _appInstanceMutex = null;
        private MessageWindow _messageWindow;

        private Timer fullscreenInterval = new(500);
        private MediaPlayer mediaPlayer = new();

        public Login? LoginWindow;

        public bool LoggingOut = false;
        private Dictionary<UserStatus, ImageSource> _taskbarPresences = new();
        private VoiceSocket voiceSocket;

        private UserStatus? _initialUserStatus = null;

        public static async Task SetStatus(UserStatus status, bool updateUserSettingsProto = true)
        {
            App? instance = (App)Application.Current;

            if (instance == null)
            {
                return;
            }

            await Discord.Client.UpdateStatusAsync(userStatus: status);



            if (updateUserSettingsProto && DiscordUserSettingsManager.Instance.UserSettingsProto != null && DiscordUserSettingsManager.Instance.UserSettingsProto.Status != null)
            {
                DiscordUserSettingsManager.Instance.UserSettingsProto.Status.Status = status.ToDiscordString();
                _ = DiscordUserSettingsManager.Instance.UpdateRemote();
            } // NullReferenceException crash fixed: UserSettingsProto was not null but the Status field was. - OmegaAOL


            foreach (Window wnd in Current.Windows)
            {
                if (wnd is Chat chat)
                {
                    if (chat.ViewModel.IsGroupChat)
                    {
                        var cat = chat.ViewModel.Categories[0];
                        var item = cat.Items.FirstOrDefault(x => x.Id == Discord.Client.CurrentUser.Id);
                        if (item is null) return;
                        item.Presence.Status = status.ToString();
                    }
                    else
                    {
                        if (chat.ViewModel.CurrentUser?.Presence != null)
                            chat.ViewModel.CurrentUser.Presence.Status = status.ToString();
                    }
                }
                else if (wnd is Home home)
                {
                    if (home.ViewModel.CurrentUser.Presence != null)
                        home.ViewModel.CurrentUser.Presence.Status = status.ToString();

                    home.TaskbarInfo.Overlay = ((App)(Current))._taskbarPresences[status];
                }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool openingCrashLog = false;
            string? crashReport = null;

            // Only initialise the mutex if we're not opening the crash log.
            if (e.Args.ElementAtOrDefault(0) != "/OpenCrashLog")
            {
                _appGuid = Guid.Parse(((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0]).Value);

                _appInstanceMutex = new Mutex(true, _appGuid.ToString(), out bool isFirstAppInstance);

                if (!isFirstAppInstance)
                {
                    // We're already running, so send our arguments to the current instance and prevent
                    // further startup:
                    try
                    {
                        using (NamedPipeClientStream client = new(_appGuid.ToString()))
                        using (StreamWriter writer = new(client))
                        {
                            client.Connect(200);
                            foreach (string arg in e.Args)
                                writer.WriteLine(arg);
                        }
                    }
                    catch (TimeoutException)
                    { }
                    catch (IOException)
                    { }

                    Application.Current.Shutdown();
                    return;
                }
                else
                {
                    HandleArguments(e.Args, true);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ListenForArgumentsMessage));
                }
            }
            else
            {
                // Parse crash log arguments:
                byte[] data = Convert.FromBase64String(e.Args.ElementAtOrDefault(1) ?? "Failed to parse crash information. Sorry :(");
                crashReport = Encoding.UTF8.GetString(data);
                openingCrashLog = true;
            }

            base.OnStartup(e);

            if (openingCrashLog)
            {
                CrashReport crashReportWindow = new();
                crashReportWindow.SetCrashReport(crashReport ?? "Failed to get crash information. Sorry :(");
                crashReportWindow.Show();
                return;
            }

            // DO NOT RUN THIS BEFORE THE CRASH REPORT WINDOW IS DEALT WITH -- IT WILL FORK BOMB
            // AND RENDER YOUR SYSTEM UNUSABLE!
            SetupUncaughtExceptionHandlers();

            StartAerochatMain();
        }

        private void OnReceiveArgumentsMessage(object? sender, ArgumentsMessageReceivedEventArgs e)
        {
            HandleArguments(e.Arguments, false);
        }

        private void HandleArguments(string[] args, bool firstTime = false)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/opendm")
                {
                    // Shift the pointer to the server name
                    i++;

                    if (ulong.TryParse(args[i], out ulong channelId))
                    {
                        OpenChatQueue.Instance.AddEntry(OpenChatQueue.EntryType.Dm, channelId);
                    }
                    else
                    {
                        // Invalidly-formed arguments.
                        break;
                    }
                }
                else if (args[i] == "/openguild")
                {
                    // Shift the pointer to the server name
                    i++;

                    if (ulong.TryParse(args[i], out ulong guildId))
                    {
                        OpenChatQueue.Instance.AddEntry(OpenChatQueue.EntryType.Guild, guildId);
                    }
                    else
                    {
                        // Invalidly-formed arguments.
                        break;
                    }
                }
            }
        }

        public App()
        {
            FixMicrosoftBadCodeMakingShitCrash.InstallHooks();
            InitializeComponent();

            ArgumentsMessageReceived += OnReceiveArgumentsMessage;
        }

        private void StartAerochatMain()
        {
            SettingsManager.Load();
            if (SettingsManager.Instance.ReadRecieptReference == DateTime.MinValue)
            {
                SettingsManager.Instance.ReadRecieptReference = DateTime.Now;
                SettingsManager.Save();
            }

            _taskbarPresences = new()
                {
                { UserStatus.Online,        new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Tray/Active.ico")) },
                { UserStatus.Idle,          new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Tray/Idle.ico")) },
                { UserStatus.DoNotDisturb,  new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Tray/Dnd.ico")) },
                { UserStatus.Invisible,     new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Tray/Offline.ico")) },
                { UserStatus.Offline,       new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Tray/Offline.ico")) },
            };

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Aerochat.Scenes.Scenes.xml";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            XDocument doc = XDocument.Parse(result);

            foreach (XElement sceneXml in doc.Root?.Elements() ?? [])
            {
                SceneViewModel scene = SceneViewModel.FromScene(sceneXml);
                var existing = ThemeService.Instance.Scenes.FirstOrDefault(x => x.Color == scene.Color);
                if (existing != null)
                {
                    throw new Exception($"Duplicate scene color {scene.Color}");
                }
                ThemeService.Instance.Scenes.Add(scene);
            }
            // see if we can get the token from the config
            byte[]? encryptedToken = null;
            try
            {
                string b64 = SettingsManager.Instance.Token;
                encryptedToken = string.IsNullOrEmpty(b64) ? null : Convert.FromBase64String(b64);
            }
            catch (Exception)
            {
                // no token saved - that's fine, continue. we'll catch this case later
            }
            bool tokenFound = encryptedToken != null && encryptedToken.Length > 0;
            string token = "";
            if (tokenFound)
            {
                try
                {
                    token = Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedToken, null, DataProtectionScope.CurrentUser));
                }
                catch (CryptographicException)
                {
                    // This is a natural error state that can occur due to invalid user configuration.
                    // If it occurs, we'll just bring the user through to the login screen where they
                    // can reauthenticate and regenerate the encrypted token (this time, hopefully
                    // in a way that can be decrypted by the current user).

                    token = "";
                    tokenFound = false;

                    // We set the shutdown mode to prevent the application from automatically closing when
                    // this dialog is closed, which would otherwise happen due to the default OnLastWindowClose.
                    ShutdownMode origShutdownMode = this.ShutdownMode;
                    this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    Dialog dialog = new(
                        "Warning",
                        "Your session token could not be decrypted. This may occur due to your configuration being " +
                        "transferred from another user. You will be logged out.",
                        SystemIcons.Warning
                    );
                    dialog.ShowDialog();

                    this.ShutdownMode = origShutdownMode;

                    // [fallthrough]
                }
                catch (NotSupportedException)
                {
                    // This is not a natural position to end up in, but it's a nice warning to leave
                    // for anyone who wants to try and get Aerochat running on non-Windows operating
                    // systems. It might also show up on ancient versions of Windows that are modified
                    // enough to run Aerochat but not enough to support the data encryption API.

                    Dialog dialog = new(
                        "Error",
                        "Your operating system does not support the encryption API we use for token encryption. " +
                        "For the safety of your Discord account, you may not continue.",
                        SystemIcons.Error
                    );
                    dialog.ShowDialog();

                    Application.Current.Shutdown();
                }
            }

            try
            {
                Discord.Client = new(new()
                {
                    TokenType = TokenType.User,
                    Token = tokenFound ? token : "",
                });
            }
            catch (CryptographicException)
            {
                Discord.Client = new(new()
                {
                    TokenType = TokenType.User,
                });
                tokenFound = false;
            }

            mediaPlayer.MediaOpened += (sender, args) =>
            {
                mediaPlayer.Play();
            };

            // Now create the message window for messages from outside processes:
            _messageWindow = new();

            if (tokenFound)
            {
                Task.Run(async () =>
                {
                    AerochatLoginStatus success = await BeginLogin(token);
                    if (success != AerochatLoginStatus.Success)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoginWindow = new(true, success);
                            LoginWindow.Show();
                        });
                    }
                });
            }
            else
            {
                // token doesn't exist - user hasn't saved it. show the login window
                LoginWindow = new();
                LoginWindow.Show();
            }
        }

        private async Task OnInitialClientReady(DiscordClient discordClient, ReadyEventArgs readyEventArgs)
        {
            Discord.Ready = true;

            UserStatus? status = _initialUserStatus;

            DiscordUserSettingsManager.Instance.LoadInitialSettingsFromDiscordClient();
            if (DiscordUserSettingsManager.Instance.UserSettingsProto?.Status?.Status != null)
            {
                // Set the status from the protobuf settings.
                status = DiscordUserSettingsManager.Instance.UserSettingsProto.Status.Status.ToUserStatus();
            }

            await Dispatcher.InvokeAsync(() => SetStatus(status ?? UserStatus.Online));

            // Prevent the client from initialised multiple times (i.e. in case of connection
            // loss):
            Discord.Client.Ready -= OnInitialClientReady;

            Dispatcher.Invoke(() =>
            {
                Login? loginWindow = Windows.OfType<Login>().FirstOrDefault();
                loginWindow?.Dispatcher.BeginInvoke(() => loginWindow.Close());
                new Home().Show();
            });
        }

        public async Task<AerochatLoginStatus> BeginLogin(string givenToken, bool save = false, UserStatus? status = null)
        {
            Discord.Client = new(new()
            {
                Token = givenToken,
                TokenType = TokenType.User,
            });

            _initialUserStatus = status;

            Discord.Client.Ready += OnInitialClientReady;

            try
            {
                await Discord.Client.ConnectAsync(status: status ?? UserStatus.Online);
            }
            catch (UnauthorizedException)
            {
                return AerochatLoginStatus.Unauthorized;
            }
            catch (BadRequestException)
            {
                return AerochatLoginStatus.BadRequest;
            }
            catch (ServerErrorException)
            {
                return AerochatLoginStatus.ServerError;
            }

            // use ProtectedData to encrypt the token
            if (save)
            {
                byte[] encryptedToken = ProtectedData.Protect(Encoding.UTF8.GetBytes(givenToken), null, DataProtectionScope.CurrentUser);
                string b64T = Convert.ToBase64String(encryptedToken);
                SettingsManager.Instance.Token = b64T;
                SettingsManager.Instance.HasUserLoggedInBefore = true;
                SettingsManager.Save();
            }
            SceneViewModel scene = SceneViewModel.FromUser(Discord.Client.CurrentUser);
            // clone the scene into ThemeService.Instance.Scene, so that its not by reference
            if (scene != null)
            {
                ThemeService.Instance.Scene = new SceneViewModel
                {
                    Id = scene.Id,
                    File = scene.File,
                    DisplayName = scene.DisplayName,
                    Color = scene.Color,
                    Default = scene.Default,
                    TextColor = scene.TextColor,
                    ShadowColor = scene.ShadowColor,
                };
            }
            Dispatcher.Invoke(() =>
            {
                Timer timer = new(5000);
                timer.Elapsed += GCRelease;
                timer.AutoReset = false;
                timer.Start();

                HWND desktopHandle = GetDesktopWindow();
                HWND shellHandle = GetShellWindow();

                bool isFullscreen = false;
                UserStatus lastStatus = Discord.Client.CurrentUser.Presence.Status;

                fullscreenInterval.Elapsed += async (sender, args) =>
                {
                    if (!SettingsManager.Instance.GoIdleWithFullscreenProgram)
                    {
                        return;
                    }

                    bool fullscreen = false;
                    RECT appBounds;
                    RECT screenBounds;
                    HWND hWnd;

                    hWnd = GetForegroundWindow();
                    if (!hWnd.Equals(IntPtr.Zero))
                    {
                        if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                        {
                            GetWindowRect(hWnd, out appBounds);
                            // get screen bounds via win32 api
                            HMONITOR monitor = MonitorFromWindow(hWnd, MonitorFlags.MONITOR_DEFAULTTONEAREST);
                            MONITORINFO monitorInfo = new();
                            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
                            GetMonitorInfo(monitor, ref monitorInfo);
                            screenBounds = monitorInfo.rcMonitor;

                            if ((appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width)
                            {
                                fullscreen = true;
                            }
                        }
                    }

                    if (fullscreen == isFullscreen) return;
                    isFullscreen = fullscreen;
                    if (Discord.Client.CurrentUser is null) return;
                    if (fullscreen)
                    {
                        lastStatus = Discord.Client.CurrentUser.Presence.Status;
                        await Dispatcher.InvokeAsync(() => SetStatus(UserStatus.DoNotDisturb));
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(() => SetStatus(lastStatus));
                    }
                };

                fullscreenInterval.Start();

                Discord.Client.PresenceUpdated += async (s, e) =>
                {
                    // search through existing windows datacontexts and if it .User.Id is e.User.Id set success to false
                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (Window wnd in Current.Windows)
                        {
                            if (wnd is Notification notification)
                            {
                                if (notification.DataContext is NotificationWindowViewModel vm)
                                {
                                    if (vm.User.Id == e.User.Id)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    });

                    if (e.PresenceBefore == null || e.PresenceBefore.Status != UserStatus.Offline || e.PresenceAfter.Status == UserStatus.Offline)
                    {
                        return;
                    }

                    await Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (Discord.Client.CurrentUser.Presence?.Status == UserStatus.DoNotDisturb)
                            return;
                        if (!SettingsManager.Instance.NotifyFriendOnline)
                            return;

                        // if the user isn't on our friends list return
                        var relationship = Discord.Client.Relationships.Values.FirstOrDefault(x => x.UserId == e.User.Id);
                        if (relationship == null || relationship.RelationshipType != DiscordRelationshipType.Friend) return;
                        var noti = new Notification(NotificationType.SignOn, new
                        {
                            e.User,
                            Presence = e.PresenceAfter
                        });
                        noti.Show();

                        mediaPlayer.Open(new Uri("Resources/Sounds/online.wav", UriKind.Relative));
                    });

                };

                DiscordUserSettingsManager.Instance.Startup();

                Discord.Client.CaptchaRequested += Client_CaptchaRequested;

                Discord.Client.MessageCreated += async (s, e) =>
                {
                    bool isDM = e.Message.Channel is DiscordDmChannel;
                    bool roleMention = e.Message.MentionedRoles.Any(r => (e.Guild?.GetMemberAsync(Discord.Client.CurrentUser.Id).Result?.Roles.Contains(r)).GetValueOrDefault());
                    bool isMention = e.Message.MentionedUsers.Contains(Discord.Client.CurrentUser) || e.Message.MentionEveryone || roleMention;
                    bool isSelf = e.Author.Id == Discord.Client.CurrentUser.Id;

                    if (isSelf) return;
                    if (!isDM && !isMention)
                        return;

                    if (Discord.Client.CurrentUser.Presence?.Status == UserStatus.DoNotDisturb)
                        return;

                    if (isDM && !SettingsManager.Instance.NotifyDm)
                        return;

                    if (isMention && !SettingsManager.Instance.NotifyMention)
                        return;

                    await Current.Dispatcher.InvokeAsync(() =>
                    {
                        Home? homeWindow = null;

                        foreach (Window wnd in Current.Windows)
                        {
                            if (wnd is Chat chat)
                            {
                                if (e.Channel?.Id == chat.Channel?.Id)
                                {
                                    if (SettingsManager.Instance.AutomaticallyOpenNotification)
                                        return;

                                    if (chat.IsActive)
                                        return;

                                    break;
                                }
                            }
                            else if (wnd is Home foundHomeWindow)
                            {
                                homeWindow = foundHomeWindow;
                            }
                        }

                        if (SettingsManager.Instance.AutomaticallyOpenNotification)
                        {
                            PresenceViewModel? presenceVm = null;

                            if (e.Channel?.Id != null)
                            {
                                if (homeWindow != null)
                                {
                                    // Try to get the presence so we can display the correct status when the window
                                    // opens.
                                    presenceVm = homeWindow.FindPresenceForUserId(e.Channel.Id);
                                }

                                new Chat(e.Channel.Id, false, presenceVm);
                            }
                        }

                        Notification notification = new(NotificationType.Message, e.Message);
                        notification.Show();
                        bool isNudge = e.Message.Content == "[nudge]";

                        if (isNudge)
                        {
                            if (SettingsManager.Instance.PlayNotificationSounds) //toggle sound
                                mediaPlayer.Open(new Uri("Resources/Sounds/nudge.wav", UriKind.Relative));
                        }
                        else
                        {
                            if (SettingsManager.Instance.PlayNotificationSounds)
                                mediaPlayer.Open(new Uri("Resources/Sounds/type.wav", UriKind.Relative));
                        }
                    });
                };
            });

            return AerochatLoginStatus.Success;
        }

        public async Task SignOut()
        {
            LoggingOut = true;
            Discord.Ready = false;
            // close all windows
            LoginWindow = new();
            LoginWindow.Show();
            LoginWindow.Focus();

            foreach (Window wnd in Current.Windows)
            {
                if (wnd is Login) continue;
                wnd.Close();
            }
            if (!string.IsNullOrEmpty(SettingsManager.Instance.Token))
            {
                SettingsManager.Instance.Token = "";
            }

            SettingsManager.Save();

            await Discord.Client.DisconnectAsync();
            Discord.Client.Dispose();
            Discord.Client = null;
            Discord.Client = new(new()
            {
                TokenType = TokenType.User,
            });
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            JumpList.SetJumpList(this, null);

            LoggingOut = false;
        }

        public async void RebuildJumpLists()
        {
            JumpList jumpList = new();

            jumpList.JumpItemsRemovedByUser += (s, e) =>
            {
                foreach (JumpItem item in e.RemovedItems)
                {
                    if (item is JumpTask task)
                    {
                        string[] tokens = task.Arguments.Split(" ");

                        if (tokens.Length < 2)
                            continue;

                        if (ulong.TryParse(tokens[1], out ulong uid))
                        {
                            if (tokens[0] == "/opendm")
                            {
                                SettingsManager.Instance.RecentDMChats.Remove(uid);
                            }
                            else if (tokens[0] == "/openguild")
                            {
                                SettingsManager.Instance.RecentServerChats.Remove(uid);
                            }
                        }
                    }
                }

                SettingsManager.Save();
            };

            List<ulong> recentChats;
            try
            {
                recentChats = SettingsManager.Instance.RecentDMChats.TakeLast(5).Reverse().ToList();
            }
            catch (ArgumentNullException)
            {
                recentChats = new();
            }

            foreach (ulong channelId in recentChats)
            {
                Discord.Client.TryGetCachedChannel(channelId, out DiscordChannel newChannel);
                if (newChannel is null)
                {
                    try
                    {
                        newChannel = await Discord.Client.GetChannelAsync(channelId);
                    }
                    catch (Exception)
                    {
                        // Ignore any network exception; it simply does not matter.
                        return;
                    }
                }
                if (newChannel == null) continue;

                string? recipientName = null;
                if (newChannel is DiscordDmChannel)
                {
                    try
                    {
                        recipientName = newChannel.Name ?? string.Join(", ", ((DiscordDmChannel)newChannel).Recipients.Select(x => x.DisplayName));
                    }
                    catch (ArgumentNullException)
                    {
                        // Bad recipient name.
                        continue;
                    }
                }

                JumpTask item = new()
                {
                    ApplicationPath = System.Environment.ProcessPath,
                    Arguments = $"/opendm {channelId}",
                    Title = recipientName,
                    Description = $"Open your chat with {recipientName}",
                    IconResourcePath = System.Environment.ProcessPath,
                    IconResourceIndex = 0,
                    CustomCategory = "Recent Chats",
                };

                jumpList.JumpItems.Add(item);
            }

            List<ulong> recentServers;
            try
            {
                recentServers = SettingsManager.Instance.RecentServerChats.TakeLast(5).Reverse().ToList();
            }
            catch (ArgumentNullException)
            {
                recentServers = new();
            }

            foreach (ulong guildId in recentServers)
            {
                Discord.Client.TryGetCachedGuild(guildId, out var guild);
                if (guild == null) continue;

                JumpTask item = new()
                {
                    ApplicationPath = System.Environment.ProcessPath,
                    Arguments = $"/openguild {guildId}",
                    Title = guild.Name,
                    Description = $"Open your chat in {guild.Name}",
                    IconResourcePath = System.Environment.ProcessPath,
                    IconResourceIndex = 0,
                    CustomCategory = "Recent Servers",
                };

                jumpList.JumpItems.Add(item);
            }

            _ = Dispatcher.BeginInvoke(() =>
            {
                jumpList.Apply();
                JumpList.SetJumpList(Application.Current, jumpList);
            });
        }

        private async Task Client_CaptchaRequested(BaseDiscordClient sender, DSharpPlus.EventArgs.CaptchaRequestEventArgs args)
        {
            var tcs = new TaskCompletionSource<DiscordCaptchaResponse>();

            await Dispatcher.InvokeAsync(async () =>
            {
                var dialog = new WebView2Frame(args.Request);
                dialog.ShowDialog();
                tcs.SetResult(dialog.CaptchaResponse);
            });

            args.SetResponse(await tcs.Task);
        }

        private void GCRelease(object? sender, System.Timers.ElapsedEventArgs e)
        {
            ((System.Timers.Timer?)(sender))?.Stop();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        private void ListenForArgumentsMessage(object? state)
        {
            try
            {
                using (NamedPipeServerStream server = new(_appGuid.ToString()))
                using (StreamReader reader = new(server))
                {
                    server.WaitForConnection();

                    List<string> arguments = new();
                    while (server.IsConnected)
                    {
                        arguments.Add(reader.ReadLine() ?? "");
                    }

                    ThreadPool.QueueUserWorkItem(new WaitCallback(OnArgumentsMessageReceived), arguments.ToArray());
                }
            }
            catch (IOException)
            {
                // Ignore.
            }
            finally
            {
                ListenForArgumentsMessage(null);
            }
        }

        public class ArgumentsMessageReceivedEventArgs : EventArgs
        {
            public string[] Arguments
            {
                get;
                private set;
            }

            public ArgumentsMessageReceivedEventArgs(string[] arguments)
            {
                Arguments = arguments;
            }
        }

        public event EventHandler<ArgumentsMessageReceivedEventArgs> ArgumentsMessageReceived;

        private void OnArgumentsMessageReceived(object? state)
        {
            string[] arguments = (string[])state;
            ArgumentsMessageReceived?.Invoke(this, new(arguments));
        }

        public void SetupUncaughtExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                OnAnyUncaughtException((Exception)e.ExceptionObject);
            };
        }

        static bool s_hasShownUncaughtExceptionBefore = false;

        public void OnAnyUncaughtException(Exception exception)
        {
            if (s_hasShownUncaughtExceptionBefore)
                return;

            byte[] exceptionUtf8 = Encoding.UTF8.GetBytes(exception.ToString());
            string exceptionBase64 = Convert.ToBase64String(exceptionUtf8);

            Shell32.ShellExecute(HWND.NULL,
                "open",
                System.Environment.ProcessPath,
                $"/OpenCrashLog {exceptionBase64}",
                null,
                ShowWindowCommand.SW_SHOWNORMAL
            );

            s_hasShownUncaughtExceptionBefore = true;
        }
    }
}
