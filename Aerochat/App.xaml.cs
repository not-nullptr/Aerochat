using Aerochat.Hoarder;
using Aerochat.ViewModels;
using Aerochat.Windows;
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

namespace Aerochat
{
    public partial class App : Application
    {
        private Timer fullscreenInterval = new(500);
        private MediaPlayer mediaPlayer = new();

        public Login? LoginWindow;

        public bool LoggingOut = false;
        private Dictionary<UserStatus, ImageSource> _taskbarPresences = new();
        public static async Task SetStatus(UserStatus status)
        {
            await Discord.Client.UpdateStatusAsync(userStatus:status);
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
        public App()
        {
            SettingsManager.Load();
            if (SettingsManager.Instance.ReadRecieptReference == DateTime.MinValue)
            {
                SettingsManager.Instance.ReadRecieptReference = DateTime.Now;
                SettingsManager.Save();
            }
            InitializeComponent();
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
                token = Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedToken, null, DataProtectionScope.CurrentUser));
            }
            try
            {
                Discord.Client = new(new()
                {
                    TokenType = TokenType.User,
                    Token = tokenFound ? token : "",
                });
            } catch (CryptographicException)
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

            if (tokenFound)
            {
                Task.Run(async () =>
                {
                    bool success = await BeginLogin(token);
                    if (!success)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoginWindow = new(true);
                            LoginWindow.Show();
                        });
                    }
                });
            } else
            {
                // token doesn't exist - user hasn't saved it. show the login window
                LoginWindow = new();
                LoginWindow.Show();
            }
        }
        public async Task<bool> BeginLogin(string givenToken, bool save = false, UserStatus status = UserStatus.Online)
        {
            Discord.Client = new(new()
            {
                Token = givenToken,
                TokenType = TokenType.User,
            });
            try
            {
                await Discord.Client.ConnectAsync(status: status);
            } catch (Exception)
            {
                return false;
            }
            Discord.Client.Ready += async (_, __) =>
            {
                Discord.Ready = true;
                Dispatcher.Invoke(() => { 
                    new Home().Show();
                    Login? loginWindow = Windows.OfType<Login>().FirstOrDefault();
                    loginWindow?.Dispatcher.Invoke(() => loginWindow.Close());
                });
                Dispatcher.Invoke(() => SetStatus(status));
            };
            // use ProtectedData to encrypt the token
            if (save)
            {
                byte[] encryptedToken = ProtectedData.Protect(Encoding.UTF8.GetBytes(givenToken), null, DataProtectionScope.CurrentUser);
                string b64T = Convert.ToBase64String(encryptedToken);
                SettingsManager.Instance.Token = b64T;
                SettingsManager.Save();
            }
            DiscordColor? colour = Discord.Client.CurrentUser.BannerColor;
            if (colour != null)
            {
                string hex = $"#{colour.Value.R:X2}{colour.Value.G:X2}{colour.Value.B:X2}";
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
                    ThemeService.Instance.Scene.Color = hex;
                }
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
                        if (Discord.Client.CurrentUser.Presence?.Status == UserStatus.DoNotDisturb) return;
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

                Discord.Client.CaptchaRequested += Client_CaptchaRequested;

                Discord.Client.MessageCreated += async (s, e) =>
                {
                    bool isDM = e.Message.Channel is DiscordDmChannel;
                    bool isMention = e.Message.MentionedUsers.Contains(Discord.Client.CurrentUser);
                    bool isSelf = e.Author.Id == Discord.Client.CurrentUser.Id;

                    if (isSelf) return;
                    if (!isDM && !isMention)
                        return;

                    Current.Dispatcher.Invoke(() =>
                    {
                        if (Discord.Client.CurrentUser.Presence?.Status == UserStatus.DoNotDisturb) return;

                        foreach (Window wnd in Current.Windows)
                        {
                            if (wnd is Chat chat)
                            {
                                if (e.Channel?.Id == chat.Channel?.Id)
                                {
                                    if (chat.IsActive) return;
                                    break;
                                }
                            }
                        }

                        Notification notification = new(NotificationType.Message, e.Message);
                        notification.Show();
                        bool isNudge = e.Message.Content == "[nudge]";

                        if (isNudge)
                        {
                            mediaPlayer.Open(new Uri("Resources/Sounds/nudge.wav", UriKind.Relative));
                        }
                        else
                        {
                            mediaPlayer.Open(new Uri("Resources/Sounds/type.wav", UriKind.Relative));
                        }
                    });
                };
            });
            return true;
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

            LoggingOut = false;
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
    }
}