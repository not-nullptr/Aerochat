using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using static Vanara.PInvoke.User32;
using Color = System.Windows.Media.Color;

namespace Aerochat.Controls
{
    public class BaseTitlebar : ContentControl
    {
        ContentPresenter AddedContent;
        NoDwmTitlebar Titlebar;
        Window Window;
        Border FirstBorder;
        Border SecondBorder;
        Grid Container;
        public bool IsDwmEnabled { get; private set; }
        public BaseTitlebar() 
        {
            DwmIsCompositionEnabled(out bool isEnabled);
            IsDwmEnabled = isEnabled;
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(BaseTitlebar),
            new PropertyMetadata(Colors.Transparent, OnColorChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty BlackTextProperty = DependencyProperty.Register(
            nameof(BlackText),
            typeof(Color),
            typeof(BaseTitlebar),
            new PropertyMetadata(Colors.Transparent, OnBlackTextChanged));

        public Color BlackText
        {
            get => (Color)GetValue(BlackTextProperty);
            set => SetValue(BlackTextProperty, value);
        }

        private static void OnBlackTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BaseTitlebar)d;
            control.OnBlackTextChanged((Color)e.NewValue);
        }

        private void OnBlackTextChanged(Color isBlackText)
        {
            if (Titlebar is not null)
            {
                Titlebar.ViewModel.TextColor = new SolidColorBrush(isBlackText);
            }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BaseTitlebar)d;
            control.OnColorChanged((Color)e.NewValue);
        }

        private void OnColorChanged(Color newColor)
        {
            if (Titlebar is not null)
            {
                Titlebar.ViewModel.Color = new SolidColorBrush(newColor);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Border containerBorder = new();

            var grid = new Grid();
            Container = grid;
            // first row should be Auto, second should be *
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // create a content presenter for the Content property
            var contentPresenter = new ContentPresenter
            {
                Content = Content,
                ContentTemplate = ContentTemplate,
                ContentTemplateSelector = ContentTemplateSelector,
                ContentStringFormat = ContentStringFormat
            };
            
            var titlebar = new NoDwmTitlebar();
            titlebar.ViewModel.TextColor = new SolidColorBrush(BlackText);
            titlebar.ViewModel.Color = new SolidColorBrush(Color);
            Grid.SetRow(titlebar, 0);
            Grid.SetRow(contentPresenter, 1);

            grid.Children.Add(titlebar);
            grid.Children.Add(contentPresenter);

            Grid parentBorder = new Grid();
            parentBorder.Children.Add(grid);

            Border border = new();
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68));
            border.BorderThickness = new Thickness(1);
            parentBorder.Children.Add(border);

            Border whiteBorder = new();
            // set to white, opacity 32, thickness 1
            whiteBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(127, 255,255,255));
            whiteBorder.BorderThickness = new Thickness(1);
            // set it so its 1px offset in all directions (top left bottom right)
            whiteBorder.Margin = new Thickness(1);
            parentBorder.Children.Add(whiteBorder);

            // round both borders to 6px
            border.CornerRadius = new CornerRadius(4, 4, 0, 0);
            whiteBorder.CornerRadius = new CornerRadius(4, 4, 0, 0);

            // round the content presenter
            contentPresenter.ClipToBounds = true;

            containerBorder.Child = parentBorder;

            Content = containerBorder;

            FirstBorder = border;
            SecondBorder = whiteBorder;

            Titlebar = titlebar;
            AddedContent = contentPresenter;

            // get the parent window
            Window = Window.GetWindow(this);
            if (Window != null)
            {
                Window.SourceInitialized += Window_SourceInitialized;
                Window.StateChanged += Window_StateChanged;
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (IsDwmEnabled) return;
            if (Window.WindowState == WindowState.Maximized)
            {
                Titlebar.Visibility = Visibility.Collapsed;
                WindowChrome.SetWindowChrome(Window, null);
                FirstBorder.BorderThickness = new Thickness(0);
                SecondBorder.BorderThickness = new Thickness(0);
                // set the grid's first row to 0
                Container.RowDefinitions[0].Height = new GridLength(0);
            }
            else
            {
                Titlebar.Visibility = Visibility.Visible;
                WindowChrome chrome = new WindowChrome();
                chrome.CaptionHeight = 28;
                chrome.CornerRadius = new CornerRadius(6, 6, 0, 0);
                chrome.GlassFrameThickness = new Thickness(0);
                WindowChrome.SetWindowChrome(Window, chrome);
                FirstBorder.BorderThickness = new Thickness(1);
                SecondBorder.BorderThickness = new Thickness(1);
                Container.RowDefinitions[0].Height = new GridLength(28);
            }
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(Window).Handle;
            HwndSource.FromHwnd(handle).AddHook(WndProc);
            OnDwmChanged();

            Window.Activated += Window_Activated;
            Window.Deactivated += Window_Deactivated;

            Titlebar.ViewModel.Title = Window.Title;
            //Titlebar.ViewModel.Icon = Window.Icon;
            // if window.icon is null, use the default icon
            if (Window.Icon == null)
            {
                // load from relative path Icons/MainWnd.ico, relative to the executing assembly
                string iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Icons/MainWnd.ico");
                Icon icon = new(iconPath, new System.Drawing.Size(16, 16));
                Titlebar.ViewModel.Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            else
            {
                Titlebar.ViewModel.Icon = Window.Icon;
            }
            Titlebar.ViewModel.Activated = true;
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            Titlebar.ViewModel.Activated = false;
        }

        private void Window_Activated(object? sender, EventArgs e)
        {
            Titlebar.ViewModel.Activated = true;
        }

        public void OnDwmChanged()
        {
            if (Titlebar == null || Window == null) return;
            // add window chrome if DWM is enabled
            if (IsDwmEnabled)
            {
                Titlebar.Visibility = Visibility.Collapsed;
                WindowChrome.SetWindowChrome(Window, null);
                FirstBorder.BorderThickness = new Thickness(0);
                SecondBorder.BorderThickness = new Thickness(0);
                // set the grid's first row to 0
                Container.RowDefinitions[0].Height = new GridLength(0);
            }
            else
            {
                Titlebar.Visibility = Visibility.Visible;
                WindowChrome chrome = new WindowChrome();
                chrome.CaptionHeight = 28;
                chrome.CornerRadius = new CornerRadius(6, 6, 0, 0);
                chrome.GlassFrameThickness = new Thickness(0);
                WindowChrome.SetWindowChrome(Window, chrome);
                FirstBorder.BorderThickness = new Thickness(1);
                SecondBorder.BorderThickness = new Thickness(1);
                Container.RowDefinitions[0].Height = new GridLength(28);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg) {
                case 0x31E: // WM_DWMCOMPOSITIONCHANGED
                    DwmIsCompositionEnabled(out bool enabled);
                    if (enabled != IsDwmEnabled)
                    {
                        IsDwmEnabled = enabled;
                        OnDwmChanged();
                    }
                    OnDwmChanged();
                    break;
                case 0x000C: // WM_SETTEXT
                    string? newText = Marshal.PtrToStringAuto(wParam);
                    if (newText != null)
                    {
                        Titlebar.ViewModel.Title = newText;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool enabled);
    }
}
