using Aerochat.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls
{
    /// <summary>
    /// Manages the titlebar theme (Vista or XP)
    /// </summary>
    class TitlebarThemeManager : DependencyObject
    {
        public static TitlebarThemeManager Instance { get; private set; }

        static TitlebarThemeManager()
        {
            Instance = new();
        }

        public TitlebarThemeManager()
        {
            ReloadTheme();
        }

        public void ReloadTheme()
        {
            if (SettingsManager.Instance.XPCaptionButtons)
            {
                LoadXPTheme();
            }
            else
            {
                LoadVistaTheme();
            }
        }

        private void LoadXPTheme()
        {
            CloseImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/Close.png", UriKind.Relative));
            CloseHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/CloseHover.png", UriKind.Relative));
            CloseActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/CloseActive.png", UriKind.Relative));
            CloseInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/CloseInactive.png", UriKind.Relative));

            MaximizeImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/Maximize.png", UriKind.Relative));
            MaximizeHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MaximizeHover.png", UriKind.Relative));
            MaximizeActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MaximizeActive.png", UriKind.Relative));
            MaximizeInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MaximizeInactive.png", UriKind.Relative));

            MinimizeImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/Minimize.png", UriKind.Relative));
            MinimizeHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MinimizeHover.png", UriKind.Relative));
            MinimizeActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MinimizeActive.png", UriKind.Relative));
            MinimizeInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/XP/MinimizeInactive.png", UriKind.Relative));
        }

        private void LoadVistaTheme()
        {
            CloseImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/Close.png", UriKind.Relative));
            CloseHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/CloseHover.png", UriKind.Relative));
            CloseActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/CloseActive.png", UriKind.Relative));
            CloseInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/CloseInactive.png", UriKind.Relative));

            MaximizeImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/Maximize.png", UriKind.Relative));
            MaximizeHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MaximizeHover.png", UriKind.Relative));
            MaximizeActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MaximizeActive.png", UriKind.Relative));
            MaximizeInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MaximizeInactive.png", UriKind.Relative));

            MinimizeImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/Minimize.png", UriKind.Relative));
            MinimizeHoverImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MinimizeHover.png", UriKind.Relative));
            MinimizeActiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MinimizeActive.png", UriKind.Relative));
            MinimizeInactiveImagePath = new BitmapImage(new Uri("/Resources/Titlebar/Vista/MinimizeInactive.png", UriKind.Relative));
        }

        #region Property boilerplate

        public BitmapImage CloseImagePath
        {
            get => (BitmapImage)GetValue(CloseImagePathProperty);
            set => SetValue(CloseImagePathProperty, value);
        }

        public static readonly DependencyProperty CloseImagePathProperty =
            DependencyProperty.Register(
                "CloseImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage CloseHoverImagePath
        {
            get => (BitmapImage)GetValue(CloseHoverImagePathProperty);
            set => SetValue(CloseHoverImagePathProperty, value);
        }

        public static readonly DependencyProperty CloseHoverImagePathProperty =
            DependencyProperty.Register(
                "CloseHoverImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage CloseActiveImagePath
        {
            get => (BitmapImage)GetValue(CloseActiveImagePathProperty);
            set => SetValue(CloseActiveImagePathProperty, value);
        }

        public static readonly DependencyProperty CloseActiveImagePathProperty =
            DependencyProperty.Register(
                "CloseActiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage CloseInactiveImagePath
        {
            get => (BitmapImage)GetValue(CloseInactiveImagePathProperty);
            set => SetValue(CloseInactiveImagePathProperty, value);
        }

        public static readonly DependencyProperty CloseInactiveImagePathProperty =
            DependencyProperty.Register(
                "CloseInactiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MaximizeImagePath
        {
            get => (BitmapImage)GetValue(MaximizeImagePathProperty);
            set => SetValue(MaximizeImagePathProperty, value);
        }

        public static readonly DependencyProperty MaximizeImagePathProperty =
            DependencyProperty.Register(
                "MaximizeImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MaximizeHoverImagePath
        {
            get => (BitmapImage)GetValue(MaximizeHoverImagePathProperty);
            set => SetValue(MaximizeHoverImagePathProperty, value);
        }

        public static readonly DependencyProperty MaximizeHoverImagePathProperty =
            DependencyProperty.Register(
                "MaximizeHoverImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MaximizeActiveImagePath
        {
            get => (BitmapImage)GetValue(MaximizeActiveImagePathProperty);
            set => SetValue(MaximizeActiveImagePathProperty, value);
        }

        public static readonly DependencyProperty MaximizeActiveImagePathProperty =
            DependencyProperty.Register(
                "MaximizeActiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MaximizeInactiveImagePath
        {
            get => (BitmapImage)GetValue(MaximizeInactiveImagePathProperty);
            set => SetValue(MaximizeInactiveImagePathProperty, value);
        }

        public static readonly DependencyProperty MaximizeInactiveImagePathProperty =
            DependencyProperty.Register(
                "MaximizeInactiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MinimizeImagePath
        {
            get => (BitmapImage)GetValue(MinimizeImagePathProperty);
            set => SetValue(MinimizeImagePathProperty, value);
        }

        public static readonly DependencyProperty MinimizeImagePathProperty =
            DependencyProperty.Register(
                "MinimizeImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MinimizeHoverImagePath
        {
            get => (BitmapImage)GetValue(MinimizeHoverImagePathProperty);
            set => SetValue(MinimizeHoverImagePathProperty, value);
        }

        public static readonly DependencyProperty MinimizeHoverImagePathProperty =
            DependencyProperty.Register(
                "MinimizeHoverImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MinimizeActiveImagePath
        {
            get => (BitmapImage)GetValue(MinimizeActiveImagePathProperty);
            set => SetValue(MinimizeActiveImagePathProperty, value);
        }

        public static readonly DependencyProperty MinimizeActiveImagePathProperty =
            DependencyProperty.Register(
                "MinimizeActiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        public BitmapImage MinimizeInactiveImagePath
        {
            get => (BitmapImage)GetValue(MinimizeInactiveImagePathProperty);
            set => SetValue(MinimizeInactiveImagePathProperty, value);
        }

        public static readonly DependencyProperty MinimizeInactiveImagePathProperty =
            DependencyProperty.Register(
                "MinimizeInactiveImagePath",
                typeof(BitmapImage),
                typeof(TitlebarThemeManager),
                new FrameworkPropertyMetadata(null)
            );

        #endregion
    }
}
