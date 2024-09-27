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
            RefreshTitlebarState();
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            // Install the initial window procedure hook:
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

        private void RefreshTitlebarState()
        {
            if (IsDwmEnabled)
            {
                return;
            }

            if (Titlebar == null || Window == null) return;

            // WindowChrome adds a new window procedure hook which takes precedent
            // over our own and breaks our custom WM_NCHITTEST handler. As such,
            // we need to uninstall and reinstall our main hook to maintain
            // precedence.
            IntPtr hWnd = new WindowInteropHelper(Window).Handle;
            HwndSource.FromHwnd(hWnd).RemoveHook(WndProc);

            if (Window.WindowState == WindowState.Maximized)
            {
                HideCustomTitlebar();
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

            // Restore our primary window procedure hook:
            HwndSource.FromHwnd(hWnd).AddHook(WndProc);
        }

        private void HideCustomTitlebar()
        {
            Titlebar.Visibility = Visibility.Collapsed;
            WindowChrome.SetWindowChrome(Window, null);
            FirstBorder.BorderThickness = new Thickness(0);
            SecondBorder.BorderThickness = new Thickness(0);
            // set the grid's first row to 0
            Container.RowDefinitions[0].Height = new GridLength(0);
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

            if (IsDwmEnabled)
            {
                HideCustomTitlebar();
            }
            else
            {
                RefreshTitlebarState();
            }
        }

        private System.Windows.Point PointFromNcHit(IntPtr lParam)
        {
            int bounds = lParam.ToInt32();

            int screenX = bounds << 16 >> 16;
            int screenY = bounds >> 16;

            return PointFromScreen(new(screenX, screenY));
        }

        private bool _sysMenuRightClickHack = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x84: // WM_NCHITTEST
                {
                    System.Windows.Point point = PointFromNcHit(lParam);

                    if (Titlebar != null && Titlebar.SystemMenuButton != null && Titlebar.SystemMenuButton.Visibility == Visibility.Visible)
                    {
                        HitTestResult? hitResult = VisualTreeHelper.HitTest(Window, point);

                        if (hitResult != null &&
                            (hitResult.VisualHit == Titlebar.SystemMenuImage ||
                            hitResult.VisualHit == Titlebar.SystemMenuButton)
                        )
                        {
                            handled = true;

                            if (_sysMenuRightClickHack)
                            {
                                // For some reason, the OS refuses to open the menu when right
                                // clicking if we return HTCAPTION. I do not know why this is;
                                // from what I've analysed of DefWindowProc, this very case
                                // should always work and yet somehow it doesn't. This hack is
                                // a workaround so the icon behaves like native windows do.

                                return 2; // HTCAPTION
                            }

                            return 3; // HTSYSMENU
                        }
                    }

                    break;
                }

                case 0xA4: // WM_NCRBUTTONDOWN
                {
                    System.Windows.Point point = PointFromNcHit(lParam);

                    if (Titlebar != null && Titlebar.SystemMenuButton != null && Titlebar.SystemMenuButton.Visibility == Visibility.Visible)
                    {
                        HitTestResult? hitResult = VisualTreeHelper.HitTest(Window, point);

                        if (hitResult != null &&
                            (hitResult.VisualHit == Titlebar.SystemMenuImage ||
                            hitResult.VisualHit == Titlebar.SystemMenuButton)
                        )
                        {
                            // Enable the hack to fix right clicking the system menu button:
                            _sysMenuRightClickHack = true;

                            handled = true;
                            return 0;
                        }
                    }

                    break;
                }

                case 0xA5: // WM_NCRBUTTONUP
                {
                    System.Windows.Point point = PointFromNcHit(lParam);

                    if (Titlebar != null && Titlebar.SystemMenuButton != null && Titlebar.SystemMenuButton.Visibility == Visibility.Visible)
                    {
                        HitTestResult? hitResult = VisualTreeHelper.HitTest(Window, point);

                        if (hitResult != null &&
                            (hitResult.VisualHit == Titlebar.SystemMenuImage ||
                            hitResult.VisualHit == Titlebar.SystemMenuButton)
                        )
                        {
                            // On right click, we need to also open the system menu.
                            // WPF does not handle this correctly, but oddly enough
                            // we need to.
                            //s_sysMenuRightClickHack = false;
                            DefWindowProc(hwnd, msg, wParam, lParam);

                            // Now that we've handled the case, we want to disable the
                            // system menu hack so that left clicking the menu works.
                            _sysMenuRightClickHack = false;

                            return 0;
                        }
                    }

                    // Disable the system menu hack; no matter what, if we released the
                    // right mouse button, then this needs to be reset.
                    _sysMenuRightClickHack = false;

                    break;
                }

                case 0x31E: // WM_DWMCOMPOSITIONCHANGED
                {
                    DwmIsCompositionEnabled(out bool enabled);
                    if (enabled != IsDwmEnabled)
                    {
                        IsDwmEnabled = enabled;
                        OnDwmChanged();
                    }
                    OnDwmChanged();
                    break;
                }
                case 0x000C: // WM_SETTEXT
                {
                    string? newText = Marshal.PtrToStringAuto(wParam);
                    if (newText != null)
                    {
                        Titlebar.ViewModel.Title = newText;
                    }
                    break;
                }
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, IntPtr uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool enabled);
    }
}
