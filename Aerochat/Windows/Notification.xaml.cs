using Aerochat.ViewModels;
using Aerochat.Helpers;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Aerochat.Settings;
using Aerochat.Hoarder;

namespace Aerochat.Windows
{
    public enum NotificationType
    {
        Message,
        SignOn
    }

    public enum NotificationState
    {
        Opening,
        Open,
        Closing,
    }

    public partial class Notification : Window
    {
        public NotificationState State = NotificationState.Opening;
        public int ScreenWidth => (int)SystemParameters.WorkArea.Width;
        public int ScreenHeight => (int)SystemParameters.WorkArea.Height;
        public int ScreenX => (int)SystemParameters.WorkArea.X;
        public int ScreenY => (int)SystemParameters.WorkArea.Y;
        public NotificationWindowViewModel ViewModel = new();

        public async void RunOpenAnimation()
        {
            var windows = Application.Current.Windows;
            int offset = 0;
            foreach (var window in windows)
            {
                if (window is Notification notification)
                {
                    if (notification == this) continue;
                    offset += (int)notification.ActualHeight + 18;
                    notification.Closing += Notification_Closing;
                }
            }
            double startTop = ScreenHeight;
            double endTop = ScreenHeight - Height - 10 - offset;

            // Animate via an interval
            int animationTime = 500;
            int steps = 10;
            double stepSize = (startTop - endTop) / steps;

            // Fade in setup
            double startOpacity = 0;
            double endOpacity = 1;
            double opacityStepSize = (endOpacity - startOpacity) / steps;

            // Perform both top position and opacity animations
            for (int i = 0; i < steps; i++)
            {
                double top = startTop - stepSize * i;
                double opacity = startOpacity + opacityStepSize * i;

                // Asynchronously set the top and opacity properties
                await Dispatcher.InvokeAsync(() =>
                {
                    Top = top;
                    Opacity = opacity;
                });
                await Task.Delay(animationTime / steps);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Top = endTop;
                Opacity = endOpacity;
                State = NotificationState.Open;
            });

            // In 5 seconds, run the close animation
            await Task.Delay(5000);
            RunCloseAnimation();
        }

        private void Notification_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (State != NotificationState.Open) return;
            Top += ((Notification)sender).ActualHeight + 18;
        }

        public async void RunCloseAnimation()
        {
            State = NotificationState.Closing;
            double startTop = Top;
            double endTop = ScreenHeight;

            int steps = 10;
            double stepSize = (startTop - endTop) / steps;

            // Fade out setup
            double startOpacity = 1;
            double endOpacity = 0;
            double opacityStepSize = (startOpacity - endOpacity) / steps;

            // Perform both top position and opacity animations
            for (int i = 0; i < steps; i++)
            {
                double top = startTop - stepSize * i;
                double opacity = startOpacity - opacityStepSize * i;

                await Dispatcher.InvokeAsync(() =>
                {
                    Top = top;
                    Opacity = opacity;
                });
                await Task.Delay(500 / steps);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Top = endTop;
                Opacity = endOpacity;
            });

            Close();
        }

        public DiscordMessage? MessageEntity;

        public Notification(NotificationType type, dynamic RelevantThing)
        {
            InitializeComponent();
            DataContext = ViewModel;

            // switch statement for the class of RelevantThing
            ViewModel.Type = type;
            switch (type)
            {
                case NotificationType.Message:
                    DiscordMessage message = (DiscordMessage)RelevantThing;
                    ViewModel.Message = MessageViewModel.FromMessage(message);
                    ViewModel.Message.Message = ParseNotificationContent(message);
                    MessageEntity = message;

                    if (SettingsManager.Instance.ReadMessageNotifications && TextToSpeech.Instance.Available)
                    {
                        //Remove links from the TTS message so it doesn't ramble on forever.
                        //also replaces a few characters with different pronounciations

                        var FilteredMessage = "";
                        var SplitFilteredMessage = ViewModel.Message.Message.Split(' ');
                        foreach (var split in SplitFilteredMessage)
                        {
                            if (split.StartsWith('#')) //Pronounced as "number sign", replaced with "hashtag"
                            {
                                string part = split.Replace("#", " hashtag");
                                FilteredMessage += part;
                            }
                            else if (split.StartsWith('^')) //Pronounced as "circumflex accent", replaced with "caret"
                            {
                                string part = split.Replace("^", " caret");
                                FilteredMessage += part;
                            }
                            else if (split.StartsWith("http://") || split.StartsWith("https://")) //Replace all links with "a link"
                            {
                                FilteredMessage += " (a link)";
                            }
                            else
                            {
                                FilteredMessage += " " + split;
                            }
                        }

                        TextToSpeech.Instance.ReadOutMessage($"{ViewModel.Message.Author.Name.ToLower()} said {FilteredMessage}.");
                    }
                    break;
                case NotificationType.SignOn:
                    UserViewModel user = UserViewModel.FromUser(RelevantThing.User);
                    PresenceViewModel presence = PresenceViewModel.FromPresence(RelevantThing.Presence);
                    ViewModel.User = user;
                    ViewModel.Presence = presence;

                    if (SettingsManager.Instance.ReadOnlineNotifications && TextToSpeech.Instance.Available)
                        TextToSpeech.Instance.ReadOutMessage($"{ViewModel.User.Name.ToLower()} has just signed in.");
                    break;
                default:
                    break;
            }

            Left = ScreenWidth - Width - 10;
            Opacity = 0;
            RunOpenAnimation();
        }

        private string ParseNotificationContent(DiscordMessage message)
        {
            if (message.Attachments.Count > 0 && message.Content == String.Empty)
            {
                return $"Uploaded {message.Attachments.Count} attachment{(message.Attachments.Count == 1 ? '\0' : 's')}.";
            }
            else
            {
                string FilteredMessage = "";

                var SplitFilteredMessage = message.Content.Split(' ');

                foreach (var split in SplitFilteredMessage)
                {
                    if (split.StartsWith('<') && split.EndsWith('>'))
                    {
                        string id = split.Replace("<", "").Replace(">", "");

                        if (id.ElementAt(0) == '@')
                        {
                            id = id.Replace("@", "");
                            if (id.ElementAt(0) == '&')
                            {
                                id = id.Replace("&", "");
                                ulong.TryParse(id, out ulong parsedId);
                                var role = message.MentionedRoles?.FirstOrDefault(x => x?.Id == parsedId);
                                if (role == null)
                                    FilteredMessage += " @unknown-role";
                                else
                                    FilteredMessage += $" @{role.Name}";
                            }
                            else
                            {
                                ulong.TryParse(id, out ulong parsedId);
                                var user = Discord.Client.GetUserProfileAsync(parsedId).GetAwaiter().GetResult().User;

                                if (user == null)
                                    FilteredMessage += " @unknown-user";
                                else
                                    FilteredMessage += $" @{user.DisplayName}";
                            }
                        }
                        else if (id.ElementAt(0) == '#')
                        {
                            id = id.Replace("#", "");

                            ulong.TryParse(id, out ulong parsedId);
                            var channel = message.MentionedChannels?.FirstOrDefault(x => x?.Id == parsedId);
                            if (channel == null)
                                FilteredMessage += " #unknown-channel";
                            else
                                FilteredMessage += $" #{channel.Name}";
                        }
                        else if (id.ElementAt(0) == ':')
                        {
                            string emojiName = id.Split(':')[1];
                            FilteredMessage += $" :{emojiName}:";
                        }
                        else if (id.ElementAt(0) == '/')
                        {
                            string emojiName = id.Split(':')[0];
                            FilteredMessage += $" {emojiName}";
                        }
                        else if (id.ElementAt(0) == 't')
                        {
                            FilteredMessage += " (timestamp)";
                        }
                        else
                        {
                            FilteredMessage += $" {split}";
                        }
                    }
                    else
                    {
                        FilteredMessage += " " + split;
                    }
                }
                return FilteredMessage;
            }
        }

        private void CloseButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void StackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (Window wnd in Application.Current.Windows)
            {
                if (wnd is Chat chat)
                {
                    if (MessageEntity is not null && MessageEntity.ChannelId == chat.Channel.Id)
                    {
                        wnd.Focus();
                        return;
                    }
                }
            }

            if (MessageEntity is not null)
            {
                Chat newChatWnd = new(MessageEntity.ChannelId);
                newChatWnd.Show();
            }
        }
    }
}