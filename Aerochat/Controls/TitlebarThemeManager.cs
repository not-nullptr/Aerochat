using Aerochat.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            SettingsManager.Instance.PropertyChanged += OnSettingsChange;
        }

        private void OnSettingsChange(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "XPCaptionButtons")
            {
                // This needs to run on the UI thread or it will throw an exception
                // and silently break the program otherwise.
                Application.Current.Dispatcher.BeginInvoke(ReloadTheme);
            }
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

            FreezeThemeResources();
        }

        /// <summary>
        /// Freezes theme resources for optimisation.
        /// </summary>
        private void FreezeThemeResources()
        {
            CloseImagePath.Freeze();
            CloseHoverImagePath.Freeze();
            CloseActiveImagePath.Freeze();
            CloseInactiveImagePath.Freeze();

            MaximizeImagePath.Freeze();
            MaximizeHoverImagePath.Freeze();
            MaximizeActiveImagePath.Freeze();
            MaximizeInactiveImagePath.Freeze();

            MinimizeImagePath.Freeze();
            MinimizeHoverImagePath.Freeze();
            MinimizeActiveImagePath.Freeze();
            MinimizeInactiveImagePath.Freeze();
        }

        private void LoadXPTheme()
        {
            CloseImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/Close.png"));
            CloseHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/CloseHover.png"));
            CloseActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/CloseActive.png"));
            CloseInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/CloseInactive.png"));

            MaximizeImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/Maximize.png"));
            MaximizeHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MaximizeHover.png"));
            MaximizeActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MaximizeActive.png"));
            MaximizeInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MaximizeInactive.png"));

            MinimizeImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/Minimize.png"));
            MinimizeHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MinimizeHover.png"));
            MinimizeActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MinimizeActive.png"));
            MinimizeInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/XP/MinimizeInactive.png"));
        }

        private void LoadVistaTheme()
        {
            CloseImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/Close.png"));
            CloseHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/CloseHover.png"));
            CloseActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/CloseActive.png"));
            CloseInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/CloseInactive.png"));

            MaximizeImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/Maximize.png"));
            MaximizeHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MaximizeHover.png"));
            MaximizeActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MaximizeActive.png"));
            MaximizeInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MaximizeInactive.png"));

            MinimizeImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/Minimize.png"));
            MinimizeHoverImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MinimizeHover.png"));
            MinimizeActiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MinimizeActive.png"));
            MinimizeInactiveImagePath = new BitmapImage(new Uri("pack://application:,,,/Aerochat;component/Resources/Titlebar/Vista/MinimizeInactive.png"));
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
