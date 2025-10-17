using Aerochat.Settings;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Vanara.PInvoke;
using static Vanara.PInvoke.DwmApi;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.User32.WindowMessage;
using Color = System.Windows.Media.Color;

namespace Aerochat.Controls
{
    public class BaseTitlebar : ContentControl
    {
        public enum TitlebarStyle
        {
            Default,
            Custom,
        }

        #region Custom tilebar variables
        /// <summary>
        /// The height of the custom titlebar. This is a constant which does not follow system parameters, so
        /// it will always be 28 pixels (or a factor thereof, on high DPI systems).
        /// </summary>
        private const int TITLEBAR_HEIGHT = 28;

        /// <summary>
        /// The radius, in pixels, of the rounded corners at the top of the window.
        /// </summary>
        private const int ROUNDED_CORNER_RADIUS = 6;

        /// <summary>
        /// Although we use a 6 pixel compromise on 96 DPI, this looks better at 200%. This value is 8, which is
        /// congruent with the definitions in NoDwmTitlebar.xaml on the topmost Border element.
        /// </summary>
        private const int HIDPI_ROUNDED_CORNER_RADIUS = 8;

        /// <summary>
        /// <see cref="ROUNDED_CORNER_RADIUS">ROUNDED_CORNER_RADIUS</see> or <see cref="HIDPI_ROUNDED_CORNER_RADIUS">HIDPI_ROUNDED_CORNER_RADIUS</see>
        /// scaled to the user's DPI.
        /// </summary>
        private int _scaledRoundedCornerRadius = ROUNDED_CORNER_RADIUS;
        #endregion

        ContentPresenter AddedContent;
        NoDwmTitlebar Titlebar;
        Window Window;
        Border FirstBorder;
        Border SecondBorder;
        Grid Container;
        TitlebarStyle _titlebarStyle = TitlebarStyle.Default;
        public bool IsDwmEnabled { get; private set; }

        public BaseTitlebar()
        {
            DetermineDwmCompositionState(out bool isEnabled);
            IsDwmEnabled = isEnabled;

            SettingsManager.Instance.PropertyChanged += OnSettingsChange;
        }

        /// <summary>
        /// Determines a virtualised DWM composition state which will not
        /// </summary>
        private HRESULT DetermineDwmCompositionState(out bool fEnabled)
        {
            if (ShouldForceNonNative())
            {
                fEnabled = false;
                return HRESULT.S_OK;
            }
            else if (ShouldForceNative())
            {
                fEnabled = true;
                return HRESULT.S_OK;
            }

            return DwmIsCompositionEnabled(out fEnabled);
        }

        private void OnSettingsChange(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "BasicTitlebar")
            {
                Application.Current.Dispatcher.BeginInvoke(UpdateBasicTitlebarSetting);
            }
        }

        public void UpdateBasicTitlebarSetting()
        {
            DetermineDwmCompositionState(out bool isEnabled);
            IsDwmEnabled = isEnabled;
            OnDwmChanged();
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
            whiteBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255));
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

                DpiScale dpiScale = VisualTreeHelper.GetDpi(Window);
                double fDpiScale = (dpiScale.DpiScaleX + dpiScale.DpiScaleY) / 2;

                // To avoid white pixels along the corners at higher DPIs, we settle for a slightly higher
                // border radius.
                double baseCornerRadius = fDpiScale == 1.0 ? ROUNDED_CORNER_RADIUS : HIDPI_ROUNDED_CORNER_RADIUS;

                _scaledRoundedCornerRadius = (int)Math.Ceiling(baseCornerRadius * fDpiScale);
            }
        }

        private bool ShouldForceNonNative()
        {
            return SettingsManager.Instance.BasicTitlebar == Enums.BasicTitlebarSetting.AlwaysNonNative;
        }

        private bool ShouldForceNative()
        {
            return SettingsManager.Instance.BasicTitlebar == Enums.BasicTitlebarSetting.AlwaysNative;
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
                // If DWM is enabled, then we always opt for using the default titlebar style.
                return;
            }

            if (Titlebar == null || Window == null) return;

            IntPtr hWnd = new WindowInteropHelper(Window).Handle;

            if (Window.WindowState == WindowState.Maximized)
            {
                HideCustomTitlebar();
            }
            else
            {
                Titlebar.Visibility = Visibility.Visible;
                _titlebarStyle = TitlebarStyle.Custom;
                FirstBorder.BorderThickness = new Thickness(1);
                SecondBorder.BorderThickness = new Thickness(1);
                Container.RowDefinitions[0].Height = new GridLength(28);

                // Since we're adding the custom window frame, we need the window region.
                // Not only does this compliment our visual style, but it also is a helpful
                // trick to evict UxTheme which is rather pesky and can interfere with our
                // custom titlebar drawing code in some way.
                RecreateWindowRegion(hWnd);
                SignalFrameUpdate(hWnd);
            }
        }

        private void HideCustomTitlebar()
        {
            Titlebar.Visibility = Visibility.Collapsed;
            _titlebarStyle = TitlebarStyle.Default;
            FirstBorder.BorderThickness = new Thickness(0);
            SecondBorder.BorderThickness = new Thickness(0);
            // set the grid's first row to 0
            Container.RowDefinitions[0].Height = new GridLength(0);

            HWND hWnd = new WindowInteropHelper(Window).Handle;
            SetWindowRgn(hWnd, HRGN.NULL, true);
            SignalFrameUpdate(hWnd);
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

        private System.Windows.Point PointFromLParam(IntPtr lParam)
        {
            int bounds = lParam.ToInt32();

            int screenX = bounds << 16 >> 16;
            int screenY = bounds >> 16;

            return new(screenX, screenY);
        }

        private System.Windows.Point PointFromNcHit(IntPtr lParam)
        {
            return PointFromScreen(PointFromLParam(lParam));
        }

        private static readonly HitTestValues[,] HITTEST_BORDERS = new[,]
        {
            { HitTestValues.HTTOPLEFT,    HitTestValues.HTTOP,    HitTestValues.HTTOPRIGHT    },
            { HitTestValues.HTLEFT,       HitTestValues.HTCLIENT, HitTestValues.HTRIGHT       },
            { HitTestValues.HTBOTTOMLEFT, HitTestValues.HTBOTTOM, HitTestValues.HTBOTTOMRIGHT },
        };

        private HitTestValues HitTestBorder(System.Windows.Point point)
        {
            // 0 = top
            // 1 = middle (client)
            // 2 = bottom
            int row = 1;

            // 0 = left
            // 1 = middle
            // 2=  right
            int col = 1;

            // Are we on a resize border?
            bool onResizeBorder = false;

            if (point.Y <= TITLEBAR_HEIGHT)
            {
                onResizeBorder = point.Y < SystemParameters.WindowResizeBorderThickness.Top;
                row = 0; // Top
            }
            else if (point.Y >= Window.Height - SystemParameters.WindowResizeBorderThickness.Bottom)
            {
                row = 2; // Bottom
            }

            if (point.X < SystemParameters.WindowResizeBorderThickness.Left)
            {
                col = 0; // Left
            }
            else if (point.X >= Window.Width - SystemParameters.WindowResizeBorderThickness.Right)
            {
                col = 2; // Right
            }

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (row == 0 && col != 1 && !onResizeBorder)
            {
                row = 1;
            }

            HitTestValues ht = HITTEST_BORDERS[row, col];

            if (ht == HitTestValues.HTTOP && !onResizeBorder)
            {
                ht = HitTestValues.HTCAPTION;
            }

            return ht;
        }

        /// <summary>Add and remove a native WindowStyle from the HWND. Copied and modified from WPF source code.</summary>
        /// <param name="hWnd">Handle to the window to modify.</param>
        /// <param name="removeStyle">The styles to be removed.  These can be bitwise combined.</param>
        /// <param name="addStyle">The styles to be added.  These can be bitwise combined.</param>
        /// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        private bool _ModifyStyle(HWND hWnd, WindowStyles removeStyle, WindowStyles addStyle)
        {
            var dwStyle = (WindowStyle)GetWindowLongPtr(hWnd, WindowLongFlags.GWL_STYLE).ToInt32();
            var dwNewStyle = ((int)dwStyle & ~(int)removeStyle) | (int)addStyle;
            if ((int)dwStyle == dwNewStyle)
            {
                return false;
            }

            SetWindowLong(hWnd, WindowLongFlags.GWL_STYLE, new IntPtr(dwNewStyle));
            return true;
        }

        /// <summary>
        /// Handles a window text or icon change from the window procedure.
        /// </summary>
        private nint OnTextOrIconChange(HWND hWnd, WindowMessage uMsg, nint wParam, nint lParam, ref bool handled)
        {
            // In order to prevent the OS from redrawing its own non-client area upon a title or
            // text change, we will remove the visibility style from the window temporarily to
            // suppress this default behaviour.
            // This conditional body runs if the style was modified. Since we don't want to make
            // the window visible if it's already invisible, we only run the following code if
            // the window was previously visible.
            if (_ModifyStyle(hWnd, WindowStyles.WS_VISIBLE, 0))
            {
                IntPtr lRes = DefWindowProc(hWnd, (uint)uMsg, wParam, lParam);
                handled = true;
                _ModifyStyle(hWnd, 0, WindowStyles.WS_VISIBLE);
                return lRes;
            }

            return nint.Zero;
        }

        #region DLL imports
        /*
         * We have a couple custom definitions here instead of using Vanara directly because
         * Vanara's wrappers may only return a SafeHRGN object, and I do not like the implications
         * of using a class object.
         * 
         * In order to avoid allocating heap memory, these custom definitions are preferred.
         */

        [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
        [PInvokeData("wingdi.h", MSDNShortId = "17456440-c655-48ab-8d1e-ee770330f164")]
        public static extern HRGN CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
        [PInvokeData("wingdi.h", MSDNShortId = "16f387e1-b00c-4755-8b21-1ee0f25bc46b")]
        public static extern HRGN CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
        #endregion

        /// <summary>
        /// Recreates the mask region that appears around the window.
        /// </summary>
        private void RecreateWindowRegion(HWND hWnd)
        {
            // The top corners of the window are rounded, but the bottom corners are not,
            // similarly to UxTheme windows. Unlike WPF's native WindowChrome implementation,
            // which seeks to allow customisation of all four corners, we only have to allocate
            // and combine two regions to achieve this effect, rather than WindowChrome's four.

            // Using properties on the Window object will get us out-of-date values, which we do not want.
            GetWindowRect(hWnd, out RECT rcWindow);

            HRGN hrgnTop = CreateRoundRectRgn(
                0, 0,
                // Rounded rect regions require an extra pixel of padding on the right or they'll get cut off.
                rcWindow.Width + 1, (rcWindow.Height / 2) + _scaledRoundedCornerRadius + 1,
                _scaledRoundedCornerRadius, _scaledRoundedCornerRadius
            );

            HRGN hrgnTarget = CreateRectRgn(
                0, (rcWindow.Height / 2) - _scaledRoundedCornerRadius,
                rcWindow.Width, rcWindow.Height
            );

            if (CombineRgn(hrgnTarget, hrgnTop, hrgnTarget, RGN_COMB.RGN_OR) != RGN_TYPE.ERROR)
            {
                _ = SetWindowRgn(hWnd, hrgnTarget, true);
            }
            else
            {
                Debug.WriteLine("Failed to create a valid window region, so we'll just evict it instead.");
                _ = SetWindowRgn(hWnd, HRGN.NULL, true);
            }

            DeleteObject(hrgnTop);
        }

        private void SignalFrameUpdate(HWND hWnd)
        {
            SetWindowPos(hWnd, HWND.NULL, 0, 0, 0, 0, SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOMOVE |
                SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
        }
        
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WindowMessage)msg)
            {
                case WM_NCCALCSIZE:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        handled = true;

                        IntPtr retVal = IntPtr.Zero;

                        if (wParam.ToInt32() != 0)
                        {
                            retVal = new IntPtr(0x0300 /* WVR_REDRAW */);
                        }

                        return retVal;
                    }

                    // Otherwise, we'll just fallthrough and handle this as usual.
                    break;
                }

                case WM_NCHITTEST:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        System.Windows.Point point = PointFromNcHit(lParam);
                        
                        // The titlebar has complex inner geometry, so we only check hittesting for titlebar children if the
                        // mouse is over the titlebar in the first place to be a little more efficient.
                        if (point.Y <= TITLEBAR_HEIGHT)
                        {
                            HitTestResult? hitResult = VisualTreeHelper.HitTest(Window, point);
                            if (Titlebar is not null && 
                                Titlebar.SystemMenuButton?.Visibility == Visibility.Visible &&
                                (
                                    hitResult?.VisualHit == Titlebar!.SystemMenuImage ||
                                    hitResult?.VisualHit == Titlebar!.SystemMenuButton
                                )
                            )
                            {
                                handled = true;
                                return (nint)HitTestValues.HTSYSMENU;
                            }
                            // XXX(isabella): Windows 11 has a snap layout menu which appears when the user hovers over
                            // the maximise button. We may want to consider supporting it, however, the custom titlebar
                            // is primarily used when DWM composition is disabled, i.e. via a mod. I am not sure if the
                            // menu still works with basic theme windows on Windows 11. If the feature can be disabled,
                            // though, then it wouldn't hurt to support. You can also interpret this comment as a TODO.
                            else if (hitResult?.VisualHit is IInputElement elm && WindowChrome.GetIsHitTestVisibleInChrome(elm))
                            {
                                // Even though we stopped using WindowChrome, we continue to use its attached property
                                // here in order to determine if the content should be hittested as client content.
                                handled = true;
                                return (nint)HitTestValues.HTCLIENT;
                            }
                        }

                        handled = true;
                        return (nint)HitTestBorder(point);
                    }

                    break;
                }

                case WM_NCACTIVATE:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        // Per WPF WindowChrome documentation:
                        // Despite MSDN's documentation of lParam not being used,
                        // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.
                        //
                        // Additionally, there is a problem with UxTheme non-client area flickering on activation
                        // change if the user's system is themed and we don't have a window region. Fortunately for
                        // us, we always set a custom window region, which evicts the UxTheme borders.
                        handled = true;
                        return DefWindowProc(hwnd, (uint)WM_NCACTIVATE, wParam, new IntPtr(-1));
                    }
                    break;
                }

                case WM_NCPAINT:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        handled = true;
                        return 0;
                    }
                    break;
                }

                case WM_WINDOWPOSCHANGED:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        WINDOWPOS wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);

                        if ((wp.flags & SetWindowPosFlags.SWP_NOSIZE) == 0)
                        {
                            // Since the size of our window changed, we need to recreate the window's region.
                            RecreateWindowRegion(hwnd);
                        }
                    }
                    break;
                }

                case WM_NCRBUTTONDOWN:
                {
                    System.Windows.Point point = PointFromNcHit(lParam);
                    System.Windows.Point screenPoint = PointFromLParam(lParam);

                    if (wParam.ToInt32() == (int)HitTestValues.HTCAPTION ||
                        wParam.ToInt32() == (int)HitTestValues.HTSYSMENU)
                    {
                        SystemCommands.ShowSystemMenu(Window, screenPoint);
                    }

                    handled = false;
                    return IntPtr.Zero;
                }

                case WM_DWMCOMPOSITIONCHANGED:
                {
                    if (ShouldForceNative() || ShouldForceNonNative())
                    {
                        break;
                    }

                    DwmIsCompositionEnabled(out bool enabled);
                    if (enabled != IsDwmEnabled)
                    {
                        IsDwmEnabled = enabled;
                        OnDwmChanged();
                    }
                    OnDwmChanged();
                    break;
                }

                case WM_SETICON:
                {
                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        return OnTextOrIconChange(hwnd, (WindowMessage)msg, wParam, lParam, ref handled);
                    }

                    break;
                }

                case WM_SETTEXT:
                {
                    string? newText = Marshal.PtrToStringAuto(lParam);
                    if (newText != null)
                    {
                        Titlebar.ViewModel.Title = newText;
                    }
                    else
                    {
                        // Even if we failed to get the text for some reason, we want
                        // to update the titlebar. In this case, we'll just copy it
                        // back from the window:
                        Titlebar.ViewModel.Title = Window.Title;
                    }

                    if (_titlebarStyle == TitlebarStyle.Custom)
                    {
                        return OnTextOrIconChange(hwnd, (WindowMessage)msg, wParam, lParam, ref handled);
                    }

                    break;
                }
            }
            return IntPtr.Zero;
        }
    }
}
