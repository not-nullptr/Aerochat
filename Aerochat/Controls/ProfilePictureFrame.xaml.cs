using Aerochat.Hoarder;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;

namespace Aerochat.Controls
{
    public enum ProfileFrameSize
    {
        ExtraSmall,
        Small,
        Medium,
        Large,
        ExtraLarge,
        Unknown
    }
    public partial class ProfilePictureFrame : UserControl
    {
        public static readonly DependencyProperty FrameSizeProperty = DependencyProperty.Register("FrameSize", typeof(ProfileFrameSize), typeof(ProfilePictureFrame), new PropertyMetadata(ProfileFrameSize.Unknown, OnFrameSizeChange));
        public static readonly DependencyProperty UserStatusProperty = DependencyProperty.Register("UserStatus", typeof(UserStatus), typeof(ProfilePictureFrame), new PropertyMetadata(UserStatus.Offline, OnStatusChange));
        public static readonly DependencyProperty ProfilePictureProperty = DependencyProperty.Register("ProfilePicture", typeof(BitmapSource), typeof(ProfilePictureFrame), new PropertyMetadata(null, OnProfilePictureChange));

        public ProfileFrameSize FrameSize
        {
            get => (ProfileFrameSize)GetValue(FrameSizeProperty);
            set => SetValue(FrameSizeProperty, value);
        }

        public UserStatus UserStatus
        {
            get => (UserStatus)GetValue(UserStatusProperty);
            set => SetValue(UserStatusProperty, value);
        }

        public BitmapSource ProfilePicture
        {
            get => (BitmapSource)GetValue(ProfilePictureProperty);
            set => SetValue(ProfilePictureProperty, value);
        }

        private bool _initial = true;

        public ProfilePictureFrame()
        {
            InitializeComponent();
            var size = SizeToPixels(FrameSize);
            if (size == -1) return;
            ForegroundTileImage.FrameWidth = size;
            ForegroundTileImage.FrameHeight = size;
            BackgroundTileImage.FrameWidth = size;
            BackgroundTileImage.FrameHeight = size;
        }

        private static void OnStatusChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProfilePictureFrame frame)
            {
                frame.UpdateStatus(e);
            }
        }

        private void UpdateStatus(DependencyPropertyChangedEventArgs e)
        {
            if (_initial)
            {
                var status = (UserStatus)e.NewValue;
                var source = FrameToSource(status, FrameSize);
                if (source is null) return;
                ForegroundTileImage.Image = source;
                ForegroundTileImage.Reset();
                ForegroundTileImage.Pause();
                BackgroundTileImage.Image = source;
                BackgroundTileImage.Reset();
                BackgroundTileImage.Pause();
                ForegroundTileImage.Opacity = 0;
                BackgroundTileImage.Opacity = 1;
                _initial = false;
                return;
            }
            var oldStatus = (UserStatus)e.OldValue;
            var newStatus = (UserStatus)e.NewValue;
            if (oldStatus == newStatus) return;
            var oldSource = FrameToSource(oldStatus, FrameSize);
            var newSource = FrameToSource(newStatus, FrameSize);
            if (oldSource is null || newSource is null) return;
            ForegroundTileImage.Image = oldSource;
            BackgroundTileImage.Image = newSource;
            ForegroundTileImage.Reset();
            BackgroundTileImage.Reset();

            // set the foreground opacity to 1 and the background opacity to 0
            ForegroundTileImage.Opacity = 1;
            BackgroundTileImage.Opacity = 0;

            // cancel any existing animations
            ForegroundTileImage.BeginAnimation(UIElement.OpacityProperty, null);
            BackgroundTileImage.BeginAnimation(UIElement.OpacityProperty, null);

            var totalFrames = newSource.Width / BackgroundTileImage.FrameWidth;
            var halfTime = totalFrames * ForegroundTileImage.FrameDuration / 2;

            double timerDuration = oldStatus == UserStatus.Invisible || oldStatus == UserStatus.Offline ? halfTime / 2 : halfTime;

            // in halfTime milliseconds, fade out the old frame and fade in the new frame
            var timer = new Timer(timerDuration);
            timer.Elapsed += (s, e) =>
            {
                // fade out the old frame using wpf's animation system
                Dispatcher.Invoke(() =>
                {
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(halfTime / 2));
                    ForegroundTileImage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(halfTime / 2));
                    BackgroundTileImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                });
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private static void OnFrameSizeChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProfilePictureFrame frame)
            {
                frame.UpdateFrameSize(e);
            }
        }

        private bool _frameSizeInitial = true;

        private void UpdateFrameSize(DependencyPropertyChangedEventArgs e)
        {
            var oldSize = (ProfileFrameSize)e.OldValue;
            var newSize = (ProfileFrameSize)e.NewValue;
            if (oldSize == newSize) return;
            var size = SizeToPixels(newSize);
            if (size == -1) throw new ArgumentException("Invalid frame size.");
            ForegroundTileImage.FrameWidth = size;
            ForegroundTileImage.FrameHeight = size;
            BackgroundTileImage.FrameWidth = size;
            BackgroundTileImage.FrameHeight = size;
            BackgroundTileImage.Image = FrameToSource(UserStatus, newSize);
            ForegroundTileImage.Image = BackgroundTileImage.Image;
            var pfpSize = FrameSizeToProfilePictureSize(newSize);
            var pfpMargin = FrameSizeToProfilePictureMargin(newSize);
            ProfilePictureControl.Width = pfpSize;
            ProfilePictureControl.Height = pfpSize;
            ProfilePictureControl.Margin = pfpMargin;
        }

        private static void OnProfilePictureChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProfilePictureFrame frame)
            {
                frame.UpdateProfilePicture(e);
            }
        }

        private void UpdateProfilePicture(DependencyPropertyChangedEventArgs e)
        {
            ProfilePictureControl.Source = ProfilePicture;
        }

        private int SizeToPixels(ProfileFrameSize size)
        {
            return size switch
            {
                ProfileFrameSize.ExtraSmall => 16,
                ProfileFrameSize.Small => 45,
                ProfileFrameSize.Medium => 59,
                ProfileFrameSize.Large => 79,
                ProfileFrameSize.ExtraLarge => 139,
                _ => -1, // why does c# force you to have a default case, even when all cases are covered?
            };
        }

        private BitmapImage? FrameToSource(UserStatus status, ProfileFrameSize size)
        {
            string sizeString = size switch
            {
                ProfileFrameSize.ExtraSmall => "XS",
                ProfileFrameSize.ExtraLarge => "XL",
                _ => size.ToString(),
            };

            string statusString = status switch
            {
                UserStatus.Online => "Active",
                UserStatus.Idle => "Idle",
                UserStatus.DoNotDisturb => "Dnd",
                _ => "Offline",
            };

            string path = $"pack://application:,,,/Aerochat;component/Resources/Frames/{sizeString}Frame{statusString}{(statusString == "Offline" || sizeString == "XS" ? "" : "Animation")}.png";
            var source = Discord.ProfileFrames.FirstOrDefault(x => x.UriSource.AbsoluteUri == path);

            var targetOpacity = statusString == "Offline" ? 0.5 : 1;
            if (_initial)
            {
                Opacity = targetOpacity;
            }
            else
            {
                var opacityAnimation = new DoubleAnimation
                {
                    From = Opacity,
                    To = targetOpacity,
                    Duration = TimeSpan.FromSeconds(1)
                }; BeginAnimation(ProfilePictureFrame.OpacityProperty, opacityAnimation);
            }

            if (source is null) throw new ArgumentException("Invalid frame size or status.");
            return source;
        }

        private int FrameSizeToProfilePictureSize(ProfileFrameSize size)
        {
            return size switch
            {
                ProfileFrameSize.ExtraLarge => 96,
                ProfileFrameSize.Large => 48,
                ProfileFrameSize.Medium => 32,
                ProfileFrameSize.Small => 24,
                ProfileFrameSize.ExtraSmall => 0,
                _ => -1,
            };
        }

        private Thickness FrameSizeToProfilePictureMargin(ProfileFrameSize size)
        {
            return size switch
            {
                ProfileFrameSize.ExtraLarge => new(24, 19, 0, 0),
                ProfileFrameSize.Large => new(18, 14, 0, 0),
                ProfileFrameSize.Medium => new(15, 12, 0, 0),
                ProfileFrameSize.Small => new(11, 10, 0, 0),
                ProfileFrameSize.ExtraSmall => new(0, 0, 0, 0),
                _ => new(),
            };
        }
    }
}
