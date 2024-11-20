// Enable this define to visualise the movement optimisation. 
//#define VISUALIZE_MOVEMENT_OPTIMIZATION

using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32.WindowMessage;
using static Vanara.PInvoke.User32.SetWindowPosFlags;
using System.Windows.Media.Animation;

namespace Aerochat.Controls.AttachmentsEditor
{
    /// <summary>
    /// Manages popup behaviours.
    /// </summary>
    public static class PopupBehavior
    {
        public static ContentControl GetPopupContainer(DependencyObject obj)
        {
            return (ContentControl)obj.GetValue(PopupContainerProperty);
        }

        public static void SetPopupContainer(DependencyObject obj, ContentControl value)
        {
            obj.SetValue(PopupContainerProperty, value);
        }

        public static readonly DependencyProperty PopupContainerProperty =
            DependencyProperty.RegisterAttached(
                "PopupContainer",
                typeof(ContentControl),
                typeof(PopupBehavior),
                new PropertyMetadata(OnPopupContainerChanged)
            );

        private static PopupBehaviorInstance GetBehaviorInstance(DependencyObject obj)
        {
            return (PopupBehaviorInstance)obj.GetValue(BehaviorInstanceProperty);
        }

        private static void SetBehaviorInstance(DependencyObject obj, PopupBehaviorInstance? value)
        {
            obj.SetValue(BehaviorInstanceProperty, value);
        }

        private static readonly DependencyProperty BehaviorInstanceProperty =
            DependencyProperty.RegisterAttached(
                "BehaviorInstance",
                typeof(PopupBehaviorInstance),
                typeof(PopupBehavior),
                new PropertyMetadata(null)
            );

        private static void OnPopupContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup)d;
            ContentControl contentControl = e.NewValue as ContentControl;

            if (contentControl == null)
            {
                if (e.OldValue is PopupBehaviorInstance oldInstance)
                {
                    oldInstance.UnregisterHandlers();
                }

                SetBehaviorInstance(d, null);
                return;
            }

            Window window = Window.GetWindow(contentControl);

            PopupBehaviorInstance instance = new(popup, contentControl, window);

            SetBehaviorInstance(d, instance);

            instance.RegisterHandlers();
        }

        /// <summary>
        /// Manages associating window event managers with the actual windows.
        /// </summary>
        internal static class WindowEventAssociationManager
        {
            private static Dictionary<WeakReference<Window>, WindowEventManager> _windows = new();

            public static WindowEventManager GetWindowManager(Window window)
            {
                foreach (KeyValuePair<WeakReference<Window>, WindowEventManager> kvp in _windows)
                {
                    if (kvp.Key.TryGetTarget(out Window targetWindow) && targetWindow == window)
                    {
                        return kvp.Value;
                    }
                    else
                    {
                        _windows.Remove(kvp.Key);
                    }
                }

                // Otherwise, if a window manager doesn't exist, then try to make one.
                WindowEventManager newWindowManager = new(window);

                _windows.Add(new WeakReference<Window>(window), newWindowManager);

                return newWindowManager;
            }
        }

        /// <summary>
        /// Manages events registered on a window that can contain popups.
        /// </summary>
        internal class WindowEventManager
        {
            private Window _window;

            private bool _isInitialized = false;

            private bool _isPreviewMouseUpSubscribed = false;

            public PopupBehaviorInstance? CurrentPopup { get; set; } = null;

            internal WindowEventManager(Window window)
            {
                _window = window;
            }

            public void EnsureInitialized()
            {
                if (!_isInitialized)
                {
                    Initialize();
                }
            }

            public void Initialize()
            {
                RegisterHandlers();

                _isInitialized = true;
            }

            public void RegisterHandlers()
            {
                _window.Activated += OnWindowActivated;
                _window.Deactivated += OnWindowDeactivated;
                _window.LocationChanged += OnWindowPositionChanged;
            }

            public void UnregisterHandlers()
            {
                _window.Activated -= OnWindowActivated;
                _window.Deactivated -= OnWindowDeactivated;
                _window.LocationChanged -= OnWindowPositionChanged;

                UnsubscribePreviewMouseUp();
            }

            public void OnAnyPopupOpened(object? sender, EventArgs args)
            {
                SubscribePreviewMouseUp();
            }

            public void OnAnyPopupClosed(object? sender, EventArgs args)
            {
                UnsubscribePreviewMouseUp();
            }

            private void OnWindowPositionChanged(object? sender, EventArgs e)
            {
                if (CurrentPopup == null)
                {
                    Debug.WriteLine("PopupBehavior WindowManager OnWindowPositionChanged: CurrentPopup is null");
                    return;
                }

                // Force WPF to refresh the position of the popup.
                var offset = CurrentPopup.Popup.HorizontalOffset;
                CurrentPopup.Popup.HorizontalOffset = offset + 1;
                CurrentPopup.Popup.HorizontalOffset = offset;
            }

            public void OnWindowActivated(object? sender, EventArgs args)
            {
                if (CurrentPopup == null)
                {
                    Debug.WriteLine("PopupBehavior WindowManager OnWindowActivated: CurrentPopup is null");
                    return;
                }

                CurrentPopup.SetPopupTopmost(true);

                // We have to wait just a second before reregistering the event so that the click doesn't
                // register anyway. It seems that the event is sent multiple times.
                Task.Run(async () =>
                {
                    await Task.Delay(200);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!_isPreviewMouseUpSubscribed)
                        {
                            SubscribePreviewMouseUp();
                        }
                    });
                });
            }

            public void OnWindowDeactivated(object? sender, EventArgs args)
            {
                if (CurrentPopup == null)
                {
                    Debug.WriteLine("PopupBehavior WindowManager OnWindowDeactivated: CurrentPopup is null");
                    return;
                }

                CurrentPopup.SetPopupTopmost(false);
                UnsubscribePreviewMouseUp();
            }

            public void SubscribePreviewMouseUp()
            {
                if (!_isPreviewMouseUpSubscribed)
                {
                    _window.PreviewMouseUp += OnWindowClicked;
                    _isPreviewMouseUpSubscribed = true;
                }
            }

            public void UnsubscribePreviewMouseUp()
            {
                _window.PreviewMouseUp -= OnWindowClicked;
                _isPreviewMouseUpSubscribed = false;
            }

            public void OnWindowClicked(object? sender, MouseButtonEventArgs args)
            {
                if (CurrentPopup == null)
                {
                    Debug.WriteLine("PopupBehavior WindowManager OnWindowClicked: CurrentPopup is null");
                    return;
                }

                CurrentPopup.OnWindowClicked(sender, args);
            }
        }

        /// <summary>
        /// Manages popup per-instance behaviours.
        /// </summary>
        internal class PopupBehaviorInstance
        {
            private Popup _popup;
            private ContentControl _openingButton;
            private Window _window;
            private WindowEventManager _windowEventManager;

            private HwndSource _hWndSource;

            private bool _dragFullWindows = false;
            private bool _isOptimizingMovement = false;
            private int _minOptimizationWidth = 0;
            private int _minOptimizationHeight = 0;
            private bool _isWindowMoving = false;
            private MoveImageAdorner? _moveImageAdorner = null;
            private RenderTargetBitmap? _moveBitmap = null;

            public Popup Popup
            {
                get => _popup;
                private set { }
            }

            internal PopupBehaviorInstance(Popup popup, ContentControl openingButton, Window window)
            {
                _popup = popup;
                _openingButton = openingButton;
                _window = window;

                _windowEventManager = WindowEventAssociationManager.GetWindowManager(window);
            }

            ~PopupBehaviorInstance()
            {
                UnregisterHandlers();

                // idk if this is good practice in C# or not but I'll keep it here just in case...
                //if (_windowManager.CurrentPopup == this)
                //{
                //    _windowManager.CurrentPopup = null;
                //}
            }

            public void RegisterHandlers()
            {
                _popup.Opened += OnPopupOpened;
                _popup.Closed += OnPopupClosed;
                _popup.PreviewMouseUp += OnPopupClicked;

                _windowEventManager.EnsureInitialized();

                _hWndSource = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
                _hWndSource.AddHook(WndProc);

                RefreshSystemParametersInfo();
            }

            public void UnregisterHandlers()
            {
                _hWndSource.RemoveHook(WndProc);

                _popup.Opened -= OnPopupOpened;
                _popup.Closed -= OnPopupClosed;
                _popup.PreviewMouseUp -= OnPopupClicked;
            }

            public void OnPopupOpened(object? sender, EventArgs args)
            {
                _windowEventManager.CurrentPopup = this;
                _windowEventManager.OnAnyPopupOpened(sender, args);
            }

            public void OnPopupClosed(object? sender, EventArgs args)
            {
                if (_windowEventManager.CurrentPopup == this)
                {
                    _windowEventManager.CurrentPopup = null;
                }

                _windowEventManager.OnAnyPopupClosed(sender, args);
            }

            private void OnPopupClicked(object sender, MouseButtonEventArgs e)
            {
                Debug.WriteLine("popup clicked");
                _window.Activate();
            }

            public void OnWindowClicked(object? sender, MouseButtonEventArgs args)
            {
                Point position = args.GetPosition(_popup.Child);
                Point hitTestButton = args.GetPosition(_openingButton);

                Debug.WriteLine(position.ToString());
                Debug.WriteLine($"{_popup.ActualWidth}, {_popup.ActualHeight}");

                // Don't close the menu if we're hitting the opening button, since we'll change the state
                // of the button and make it instantly open again.
                if (hitTestButton.X > 0 && hitTestButton.X <= _openingButton.ActualWidth && hitTestButton.Y > 0 && hitTestButton.Y <= _openingButton.ActualHeight)
                {
                    return;
                }

                // Don't close the menu if we're clicking within the menu.
                if (position.X > 0 && position.X <= _popup.Child.RenderSize.Width && position.Y > 0 && position.Y <= _popup.Child.RenderSize.Height)
                {
                    return;
                }

                _popup.IsOpen = false;
            }

            private IntPtr WndProc(nint hWnd, int uMsg, nint wParam, nint lParam, ref bool handled)
            {
                //Debug.WriteLine("Window procedure!!!");

                switch ((User32.WindowMessage)uMsg)
                {
                    case WM_ENTERSIZEMOVE:
                    {
                        Debug.WriteLine("WM_ENTERSIZEMOVE");

                        if (!_isWindowMoving)
                        {
                            _isWindowMoving = true;

                            TryEnableMovementOptimizations();
                            //_isOptimizingMovement = true;

                            //EnsureMoveImage(hWnd);
                        }

                        break;
                    }

                    case WM_EXITSIZEMOVE:
                    {
                        Debug.WriteLine("WM_EXITSIZEMOVE");

                        if (_isWindowMoving)
                        {
                            _isWindowMoving = false;
                            _isOptimizingMovement = false;

                            // Disable movement optimisations since we're exiting them.
                            // They will be re-enabled next time we call.
                            DisableMovementOptimizations();
                        }

                        break;
                    }

                    case WM_MOVE:
                    {
                        //Debug.WriteLine("WM_MOVE");

                        if (!_isOptimizingMovement)
                        {
                            // If we're not optimising movement (i.e. complex shape), then move the real popup
                            // window alongside the owner. This will stutter more than optimised movement, but
                            // it will look better than keeping the popup window in place for the duration of
                            // window movement.
                            //Debug.WriteLine("unoptimised!!!");
                            var offset = _popup.HorizontalOffset;
                            _popup.HorizontalOffset = offset + 1;
                            _popup.HorizontalOffset = offset;
                        }

                        break;
                    }

                    case WM_SIZE:
                    {
                        int width = lParam.ToInt32() & 0xFFFF;
                        int height = lParam.ToInt32() >> 16 & 0xFFFF;

                        if (_isOptimizingMovement)
                        {
                            if (width < _minOptimizationWidth || height < _minOptimizationHeight)
                            {
                                DisableMovementOptimizations();
                            }
                        }
                        else
                        {
                            if ((width >= _minOptimizationWidth || height >= _minOptimizationHeight) && (width != 0 && height != 0))
                            {
                                TryEnableMovementOptimizations();
                            }
                        }

                        break;
                    }

                    case WM_SETTINGCHANGE:
                    {
                        RefreshSystemParametersInfo();
                        break;
                    }
                }

                return IntPtr.Zero;
            }

            private void RefreshSystemParametersInfo()
            {
                User32.SystemParametersInfo(User32.SPI.SPI_GETDRAGFULLWINDOWS, out bool dragFullWindows);
                _dragFullWindows = dragFullWindows;
            }

            private async void EnsureMoveImage(nint hWndOwner)
            {
                if (_popup.Child == null)
                {
                    return;
                }

                // must be > 0 or crash
                if (_popup.Child.RenderSize.Width > 0 && _popup.Child.RenderSize.Height > 0)
                {
                    if (_moveBitmap == null)
                    {
                        _moveBitmap = new((int)_popup.Child.RenderSize.Width, (int)_popup.Child.RenderSize.Height, 96, 96, PixelFormats.Default);
                    }

                    _moveBitmap.Render(_popup.Child);

                    // At this point, we're going to wait, which means that state
                    // can change between here and now.
                    nint hWndPopup = await EnsurePopupWindowExists();

                    if (hWndPopup == 0)
                    {
                        // If we failed to ensure the existence of the popup window, then we will simply
                        // give up, since there's no way that we could run.
                        _isOptimizingMovement = false;
                        return;
                    }

                    // Since the user could have stopped moving the window during
                    // the interval it took to ensure that the hWnd for the popup
                    // window exists, we need to check again to avoid creating an
                    // extraneous element.
                    if (!_isWindowMoving)
                    {
                        _isOptimizingMovement = false;
                        return;
                    }

                    User32.GetWindowRect(hWndPopup, out RECT rect);
                    User32.MapWindowRect(0 /* HWND_DESKTOP */, hWndOwner, ref rect);

                    // We can't optimise movement if the popup window is larger than the owner,
                    // so we just rely on moving the window directly.
                    if (rect.right > _window.ActualWidth || rect.bottom > _window.ActualHeight)
                    {
                        _isOptimizingMovement = false;
                        return;
                    }

                    _minOptimizationWidth = rect.right;
                    _minOptimizationHeight = rect.bottom;

                    Rect renderRect = new Rect
                    {
                        X = rect.X,
                        Y = rect.Y,
                        Width = _popup.Child.RenderSize.Width,
                        Height = _popup.Child.RenderSize.Height
                    };

                    // If someone else already created the adorner, then we don't want to
                    // create a new adorner, because we'll be stuck with the existing one.
                    // This system is only designed with having one single adorner in mind.
                    if (_moveImageAdorner != null)
                    {
                        Debug.WriteLine("C");
                        _isOptimizingMovement = false;
                        return;
                    }

                    _moveImageAdorner = new(_moveBitmap, renderRect, (UIElement)_window.Content);
                    AdornerLayer.GetAdornerLayer((Visual)_window.Content).Add(_moveImageAdorner);
                    _isOptimizingMovement = true;

                    _popup.Child.Visibility = Visibility.Hidden;
                }
            }

            private void TryEnableMovementOptimizations()
            {
                if (!_dragFullWindows)
                {
                    // If we aren't dragging full windows, then there's no point in using
                    // this optimisation.
                    _isOptimizingMovement = false;
                    return;
                }

                _isOptimizingMovement = true;
                EnsureMoveImage(new WindowInteropHelper(_window).Handle);
            }

            private void DisableMovementOptimizations()
            {
                _isOptimizingMovement = false;

                _popup.Child.Visibility = Visibility.Visible;

                if (_moveImageAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer((Visual)_window.Content).Remove(_moveImageAdorner);
                    _moveImageAdorner = null;
                    //_moveBitmap = null;
                }
            }

            private async Task<nint> EnsurePopupWindowExists()
            {
                PresentationSource? presentationSource = null;
                nint hWnd = 0;
                int iterations = 0;

                do
                {
                    if (_popup.Child is Visual visual)
                    {
                        presentationSource = PresentationSource.FromVisual(visual);

                        if (presentationSource is HwndSource hwndSource)
                        {
                            hWnd = hwndSource.Handle;
                            return hWnd;
                        }
                    }

                    Debug.WriteLine("Waiting for the popup window to exist.");
                    iterations++;
                    await Task.Delay(1);
                }
                while (presentationSource == null && hWnd == 0 && iterations < 10);

                return 0;
            }

            internal void SetPopupTopmost(bool isTopmost)
            {
                if (_popup.Child == null)
                {
                    return;
                }

                HwndSource? hwndSource = PresentationSource.FromVisual(_popup.Child) as HwndSource;

                if (hwndSource == null)
                {
                    return;
                }

                nint hWnd = hwndSource.Handle;

                RECT rect;

                if (!User32.GetWindowRect(hWnd, out rect))
                {
                    return;
                }

                const User32.SetWindowPosFlags COMMON_SWP_FLAGS = SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING;

                if (isTopmost)
                {
                    User32.SetWindowPos(
                        hWnd,
                        HWND.HWND_TOPMOST,
                        rect.Left,
                        rect.Top,
                        (int)_popup.Child.RenderSize.Width,
                        (int)_popup.Child.RenderSize.Height,
                        COMMON_SWP_FLAGS
                    );
                }
                else
                {
                    User32.SetWindowPos(
                        hWnd,
                        HWND.HWND_BOTTOM,
                        rect.Left,
                        rect.Top,
                        (int)_popup.Child.RenderSize.Width,
                        (int)_popup.Child.RenderSize.Height,
                        COMMON_SWP_FLAGS
                    );

                    User32.SetWindowPos(
                        hWnd,
                        HWND.HWND_TOP,
                        rect.Left,
                        rect.Top,
                        (int)_popup.Child.RenderSize.Width,
                        (int)_popup.Child.RenderSize.Height,
                        COMMON_SWP_FLAGS
                    );

                    User32.SetWindowPos(
                        hWnd,
                        HWND.HWND_NOTOPMOST,
                        rect.Left,
                        rect.Top,
                        (int)_popup.Child.RenderSize.Width,
                        (int)_popup.Child.RenderSize.Height,
                        COMMON_SWP_FLAGS
                    );
                }
            }

            private class MoveImageAdorner : Adorner
            {
                private ImageSource _bitmap;
                private Rect _renderRect;

                public MoveImageAdorner(ImageSource bitmap, Rect renderRect, UIElement adornedElement)
                    : base(adornedElement)
                {
                    _bitmap = bitmap;
                    _renderRect = renderRect;
                }

                protected override void OnRender(DrawingContext drawingContext)
                {
#if VISUALIZE_MOVEMENT_OPTIMIZATION
                    drawingContext.DrawRectangle(new SolidColorBrush(Colors.Green), null, _renderRect);
#else
                    drawingContext.DrawImage(_bitmap, _renderRect);
#endif
                }
            }
        }
    }
}
