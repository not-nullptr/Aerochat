using System.Runtime.InteropServices;
using System.Windows.Interop;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.ComCtl32;
using Vanara.PInvoke;
using System.Windows;
using System.Diagnostics;
using System.Configuration;
using System.Timers;
using System.Text;
using System.Linq.Dynamic.Core;

#pragma warning disable CA1416 

namespace Aerochat.HwndHosts
{
    public class ControlHost : HwndHost
    {
        public event EventHandler? WindowCreated;

        public event EventHandler? Click;

        internal const int HOST_ID = 0x00000002;
        private SafeHWND? hwndHost;
        private SafeHWND? hwndControl;
        private int hostHeight, hostWidth = 0;
        HwndSource? source;
        Window? parentWnd;
        private WindowProc hostWndProcDelegate;

        public SafeHWND? ControlHwnd
        {
            get => hwndControl;
        }

        public ControlHost(double height, double width)
        {
            hostHeight = (int)height;
            hostWidth = (int)width;
            SizeChanged += ControlHost_SizeChanged;
            Loaded += ControlHost_Loaded;
        }

        private static long MakeLong(int uis, int uisf)
        {
            return (uisf << 16) | uis;
        }

        private void ControlHost_Loaded(object sender, RoutedEventArgs e)
        {
            var wnd = Window.GetWindow(this);
            if (wnd is null) return;
            parentWnd = wnd;
            var interopHelper = new WindowInteropHelper(wnd);
            source = HwndSource.FromHwnd(interopHelper.Handle);
            if (source is null) return;
            SendMessage(source.Handle, WindowMessage.WM_UPDATEUISTATE, MakeLong((int)UIS.UIS_SET, (int)UISF.UISF_HIDEFOCUS), IntPtr.Zero);
            source.AddHook(MainWindowWndProc);
        }

        private IntPtr MainWindowWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var message = (WindowMessage)msg;
            handled = false;
            return IntPtr.Zero;
        }

        private void ControlHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (hwndControl is null || hwndHost is null) return;
            SetWindowPos(hwndHost, IntPtr.Zero, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOMOVE);
            SetWindowPos(hwndControl, IntPtr.Zero, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOMOVE);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            parentWnd = Window.GetWindow(this);
            Point controlPosition = TransformToAncestor(parentWnd).Transform(new Point(0, 0));
            hostWndProcDelegate = HostWndProc;
            var uuid = Guid.NewGuid().ToString();
            var @class = new WNDCLASS()
            {
                lpfnWndProc = hostWndProcDelegate,
                hInstance = Process.GetCurrentProcess().Handle,
                lpszClassName = uuid,
                hbrBackground = (IntPtr)(StockObjectType.WHITE_BRUSH + 1),
            };
            RegisterClass(in @class);
            hwndHost = CreateWindowEx(0, @class.lpszClassName, "",
                                      WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD,
                                      (int)controlPosition.X, (int)controlPosition.Y,
                                      0, 0,
                                      hwndParent.Handle,
                                      (IntPtr)HOST_ID,
                                      IntPtr.Zero,
                                      0);
            hwndControl = CreateWindowEx(0, "BUTTON", "",
                                          WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | (WindowStyles)(ButtonStyle.BS_COMMANDLINK | ButtonStyle.BS_MULTILINE),
                                          0, 0,
                                          0, 0,
                                          hwndHost.DangerousGetHandle(),
                                          IntPtr.Zero,
                                          IntPtr.Zero,
                                          0);

            WindowCreated?.Invoke(this, EventArgs.Empty);
            SetWindowPos(hwndHost, IntPtr.Zero, 0, 0, (int)ActualWidth, (int)ActualHeight, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOMOVE);
            SetWindowPos(hwndControl, IntPtr.Zero, 0, 0, (int)ActualWidth, (int)ActualHeight, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOMOVE);
            SendMessage(hwndControl, WindowMessage.WM_UPDATEUISTATE, MakeLong((int)UIS.UIS_SET, (int)UISF.UISF_HIDEFOCUS), IntPtr.Zero);
            SendMessage(hwndHost, WindowMessage.WM_UPDATEUISTATE, MakeLong((int)UIS.UIS_SET, (int)UISF.UISF_HIDEFOCUS), IntPtr.Zero);
            return new HandleRef(this, hwndHost.DangerousGetHandle());
        }

        private IntPtr HostWndProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            var message = (WindowMessage)uMsg;
            switch (message)
            {
                case WindowMessage.WM_CTLCOLORBTN:
                case WindowMessage.WM_CTLCOLORSTATIC:
                    SetBkColor(wParam, 0xFFFFFF);
                    return (nint)GetStockObject(StockObjectType.WHITE_BRUSH);
                case WindowMessage.WM_COMMAND:
                    Click?.Invoke(this, EventArgs.Empty);
                    break;
                case WindowMessage.WM_SETFOCUS:
                    SetFocus();
                    break;
            }
            return DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var message = (WindowMessage)msg;
            if (message == WindowMessage.WM_SETFOCUS)
            {
                SetFocus();
                handled = true;
                return 0;
            }
            handled = false;
            return IntPtr.Zero;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
            source?.RemoveHook(MainWindowWndProc);
        }
    }
}