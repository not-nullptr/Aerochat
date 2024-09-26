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

namespace Aerochat.Windows
{
    public enum NotificationType
    {
        Message,
        SignOn
    }
    public partial class Notification : Window
    {
        public int ScreenWidth => (int)SystemParameters.WorkArea.Width;
        public int ScreenHeight => (int)SystemParameters.WorkArea.Height;
        public int ScreenX => (int)SystemParameters.WorkArea.X;
        public int ScreenY => (int)SystemParameters.WorkArea.Y;
        public NotificationWindowViewModel ViewModel = new();

        public async void RunOpenAnimation()
        {
            double startTop = ScreenHeight;
            double endTop = ScreenHeight - Height - 10;

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
            });

            // In 5 seconds, run the close animation
            await Task.Delay(5000);
            RunCloseAnimation();
        }

        public async void RunCloseAnimation()
        {
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
                    break;
                case NotificationType.SignOn:
                    UserViewModel user = UserViewModel.FromUser(RelevantThing.User);
                    PresenceViewModel presence = PresenceViewModel.FromPresence(RelevantThing.Presence);
                    ViewModel.User = user;
                    ViewModel.Presence = presence;
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
