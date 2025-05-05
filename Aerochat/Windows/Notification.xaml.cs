using Aerochat.ViewModels;
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
using System.Speech.Synthesis;
using Aerochat.Settings;

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
        private SpeechSynthesizer synth = new();
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

            synth.Dispose();
            Close();
        }

        public DiscordMessage? MessageEntity;

        public Notification(NotificationType type, dynamic RelevantThing)
        {
            InitializeComponent();
            synth.SetOutputToDefaultAudioDevice();
            DataContext = ViewModel;
            //if (Message is not null)
            //{
            //    ViewModel.Message = MessageViewModel.FromMessage(Message);
            //    MessageEntity = Message;
            //};

            // switch statement for the class of RelevantThing
            ViewModel.Type = type;
            switch (type)
            {
                case NotificationType.Message:
                    DiscordMessage message = (DiscordMessage)RelevantThing;
                    ViewModel.Message = MessageViewModel.FromMessage(message);
                    MessageEntity = message;

                    if (SettingsManager.Instance.ReadMessageNotifications)
                    {
                        //Filters @mentions, links, channels, and emojis out of the TTS message so it doesn't ramble on forever.
                        var FilteredMessage = "";
                        var SplitFilteredMessage = ViewModel.Message.Message.Split(' ');
                        foreach (var split in SplitFilteredMessage)
                        {
                            if (split.StartsWith('<') && split.EndsWith('>'))
                            {
                                string id = split.Replace("<", "").Replace(">", "");

                                if (id.ElementAt(0) == '@')
                                {
                                    if (id.ElementAt(1) == '&')
                                        FilteredMessage += " at role ";
                                    else
                                        FilteredMessage += " at user ";
                                }
                                else if (id.ElementAt(0) == '#')
                                {
                                    FilteredMessage += " a channel ";
                                }
                                else if (id.ElementAt(0) == ':')
                                {
                                    FilteredMessage += " (an emoji) ";
                                }

                            }
                            else if (split.StartsWith("http://") || split.StartsWith("https://"))
                            {
                                FilteredMessage += " a link ";
                            }
                            else
                            {
                                FilteredMessage += " " + split + " ";
                            }
                        }
                        synth.SpeakAsync($"{ViewModel.Message.Author.Name.ToLower()} said {FilteredMessage}.");
                    }
                    break;
                case NotificationType.SignOn:
                    UserViewModel user = UserViewModel.FromUser(RelevantThing.User);
                    PresenceViewModel presence = PresenceViewModel.FromPresence(RelevantThing.Presence);
                    ViewModel.User = user;
                    ViewModel.Presence = presence;

                    if (SettingsManager.Instance.ReadOnlineNotifications)
                        synth.SpeakAsync($"{ViewModel.User.Name.ToLower()} has just signed in.");
                    break;
                default:
                    break;
            }

            Left = ScreenWidth - Width - 10;
            Opacity = 0;
            RunOpenAnimation();
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