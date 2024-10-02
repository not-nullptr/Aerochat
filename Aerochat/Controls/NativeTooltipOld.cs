using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.Ole32;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;

namespace Aerochat.Controls
{
    public class NativeToolTipControl : ToolTip
    {
        #region NativeMethods

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(WindowStylesEx dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const int CW_USEDEFAULT = unchecked((int)0x80000000);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const int WM_USER = 0x0400;

        private struct TOOLINFO
        {
            public int cbSize;
            public int uFlags;
            public IntPtr hwnd;
            public IntPtr uId;
            public RECT rect;
            public IntPtr hinst;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszText;
            public IntPtr lParam;
        }

        [Flags]
        private enum WindowStylesEx : uint
        {
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_NOACTIVATE = 0x08000000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
            WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_WINDOWEDGE = 0x00000100
        }

        [Flags]
        private enum WindowStyles : uint
        {
            WS_BORDER = 0x800000,
            WS_CAPTION = 0xc00000,
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x2000000,
            WS_CLIPSIBLINGS = 0x4000000,
            WS_DISABLED = 0x8000000,
            WS_DLGFRAME = 0x400000,
            WS_GROUP = 0x20000,
            WS_HSCROLL = 0x100000,
            WS_MAXIMIZE = 0x1000000,
            WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x20000,
            WS_OVERLAPPED = 0x0,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000u,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SIZEFRAME = 0x40000,
            WS_SYSMENU = 0x80000,
            WS_TABSTOP = 0x10000,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x200000
        }

        [Flags]
        private enum ToolTipStyles : uint
        {
            TTS_ALWAYSTIP = 0x01,
            TTS_NOPREFIX = 0x02,
            TTS_NOANIMATE = 0x10,
            TTS_NOFADE = 0x20,
            TTS_BALLOON = 0x40,
            TTS_CLOSE = 0x80,
            TTS_USEVISUALSTYLE = 0x100  // Use themed hyperlinks, Vista+
        }

        [Flags]
        private enum ToolTipFlags : uint
        {
            TTF_IDISHWND = 0x0001,
            TTF_CENTERTIP = 0x0002,
            TTF_RTLREADING = 0x0004,
            TTF_SUBCLASS = 0x0010,
            TTF_TRACK = 0x0020,
            TTF_ABSOLUTE = 0x0080,
            TTF_TRANSPARENT = 0x0100,
            TTF_PARSELINKS = 0x1000,
            TTF_DI_SETITEM = 0x8000
        }

        private enum ToolTipMessages : uint
        {
            TTM_ACTIVATE = WM_USER + 1,
            TTM_SETDELAYTIME = WM_USER + 3,
            TTM_ADDTOOLA = WM_USER + 4,
            TTM_ADDTOOLW = WM_USER + 50,
            TTM_DELTOOLA = WM_USER + 5,
            TTM_DELTOOLW = WM_USER + 51,
            TTM_NEWTOOLRECTA = WM_USER + 6,
            TTM_NEWTOOLRECTW = WM_USER + 52,
            TTM_RELAYEVENT = WM_USER + 7, // Win7: wParam = GetMessageExtraInfo when relaying WM_MOUSEMOVE
            TTM_GETTOOLINFOA = WM_USER + 8,
            TTM_GETTOOLINFOW = WM_USER + 53,
            TTM_SETTOOLINFOA = WM_USER + 9,
            TTM_SETTOOLINFOW = WM_USER + 54,
            TTM_HITTESTA = WM_USER + 10,
            TTM_HITTESTW = WM_USER + 55,
            TTM_GETTEXTA = WM_USER + 11,
            TTM_GETTEXTW = WM_USER + 56,
            TTM_UPDATETIPTEXTA = WM_USER + 12,
            TTM_UPDATETIPTEXTW = WM_USER + 57,
            TTM_GETTOOLCOUNT = WM_USER + 13,
            TTM_ENUMTOOLSA = WM_USER + 14,
            TTM_ENUMTOOLSW = WM_USER + 58,
            TTM_GETCURRENTTOOLA = WM_USER + 15,
            TTM_GETCURRENTTOOLW = WM_USER + 59,
            TTM_WINDOWFROMPOINT = WM_USER + 16,
            TTM_TRACKACTIVATE = WM_USER + 17,  // wParam = TRUE/FALSE start end  lparam = LPTOOLINFO
            TTM_TRACKPOSITION = WM_USER + 18,  // lParam = dwPos
            TTM_SETTIPBKCOLOR = WM_USER + 19,
            TTM_SETTIPTEXTCOLOR = WM_USER + 20,
            TTM_GETDELAYTIME = WM_USER + 21,
            TTM_GETTIPBKCOLOR = WM_USER + 22,
            TTM_GETTIPTEXTCOLOR = WM_USER + 23,
            TTM_SETMAXTIPWIDTH = WM_USER + 24,
            TTM_GETMAXTIPWIDTH = WM_USER + 25,
            TTM_SETMARGIN = WM_USER + 26,  // lParam = lprc
            TTM_GETMARGIN = WM_USER + 27,  // lParam = lprc
            TTM_POP = WM_USER + 28,
            TTM_UPDATE = WM_USER + 29,
            TTM_GETBUBBLESIZE = WM_USER + 30,
            TTM_ADJUSTRECT = WM_USER + 31,
            TTM_SETTITLEA = WM_USER + 32,  // wParam = TTI_*, lParam = char* szTitle
            TTM_SETTITLEW = WM_USER + 33,  // wParam = TTI_*, lParam = wchar* szTitle
            TTM_POPUP = WM_USER + 34,
            TTM_GETTITLE = WM_USER + 35 // wParam = 0, lParam = TTGETTITLE*
        }

        [Flags]
        private enum SetWindowPosFlags : uint
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOREDRAW = 0x0008,
            SWP_NOACTIVATE = 0x0010,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_DEFERERASE = 0x2000,
            SWP_ASYNCWINDOWPOS = 0x4000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        #endregion

        // hwnd of the tooltip window
        private static IntPtr _tooltipWindow;

        // TOOLINFO structure (in managed memory)
        private static TOOLINFO _ti;

        // pointer to the TOOLINFO structure in unmanaged memory
        private static IntPtr _pti;

        // keeps track of whether the tooltip has been created
        private static bool added;

        // the hwnd of the last window that displayed a tooltip
        private static IntPtr lastwindow;

        private static bool _tooltipVisible = false;

        static NativeToolTipControl()
        {
            ToolTipProperty = DependencyProperty.RegisterAttached("ToolTip", typeof(string), typeof(NativeToolTipControl), new PropertyMetadata(null, ToolTipPropertyChangedCallback));
            IsOpenProperty.OverrideMetadata(typeof(NativeToolTipControl), new SingleFrameworkPropertyMetadata(OnIsOpenedChanged));
            ContentProperty.OverrideMetadata(typeof(NativeToolTipControl), new FrameworkPropertyMetadata(OnContentChanged));
        }

        public static void Destroy()
        {
            // free up unmanaged memory
            if (_pti != IntPtr.Zero) Marshal.FreeHGlobal(_pti);
            if (_tooltipWindow != IntPtr.Zero) DestroyWindow(_tooltipWindow);
        }

        public static readonly DependencyProperty ToolTipProperty;

        public static string GetToolTip(DependencyObject obj)
        {
            return (string)obj.GetValue(ToolTipProperty);
        }

        public static void SetToolTip(DependencyObject obj, string value)
        {
            obj.SetValue(ToolTipProperty, value);
        }

        private static void ToolTipPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            FrameworkElement element = dependencyObject as FrameworkElement;

            string text = dependencyPropertyChangedEventArgs.NewValue as string;

            if (element == null || string.IsNullOrWhiteSpace(text)) return;

            NativeToolTipControl existingtt = element.ToolTip as NativeToolTipControl;

            if (existingtt != null) existingtt.Content = text;
            else element.ToolTip = new NativeToolTipControl() { Content = text };
        }

        private static void Add(IntPtr handle)
        {
            _tooltipWindow = CreateWindowEx(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_TRANSPARENT, "tooltips_class32", null,
                (uint)WindowStyles.WS_POPUP | (uint)ToolTipStyles.TTS_NOPREFIX | (uint)ToolTipStyles.TTS_ALWAYSTIP,
                CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            SetWindowPos(_tooltipWindow, HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE);

            _ti = new TOOLINFO();
            _ti.cbSize = Marshal.SizeOf(_ti);
            _ti.uFlags = (int)ToolTipFlags.TTF_SUBCLASS;
            _ti.hwnd = handle;
            // we need to set the rect field, otherwise TTM_POPUP doesn't do anything
            _ti.rect = new RECT() { top = 0, left = 0, bottom = int.MaxValue, right = int.MaxValue };

            // copy the TOOLINFO struct into unmanaged memory (remember to free the memory when exiting the application)
            _pti = Marshal.AllocHGlobal(Marshal.SizeOf(_ti));

            Marshal.StructureToPtr(_ti, _pti, false);

            SendToolTipMessage(ToolTipMessages.TTM_ADDTOOLW, IntPtr.Zero, _pti);

            // effectively disable the maximum width bound - line breaking can behave oddly otherwise
            SendToolTipMessage(ToolTipMessages.TTM_SETMAXTIPWIDTH, IntPtr.Zero, new IntPtr(short.MaxValue));

            added = true;
        }

        /// <summary>
        /// Call this method to enable the tooltip for a different window.
        /// </summary>
        /// <param name="handle">Window handle.</param>
        private static void Update(IntPtr handle)
        {
            SendToolTipMessage(ToolTipMessages.TTM_DELTOOLW, IntPtr.Zero, _pti);

            _ti.hwnd = handle;
            Marshal.StructureToPtr(_ti, _pti, true);
            SendToolTipMessage(ToolTipMessages.TTM_ADDTOOLW, IntPtr.Zero, _pti);
        }

        private static void SendToolTipMessage(ToolTipMessages message, IntPtr wParam, IntPtr lParam)
        {
            SendMessage(_tooltipWindow, (uint)message, wParam, lParam);
        }

        private static void OnContentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            NativeToolTipControl tooltip = dependencyObject as NativeToolTipControl;

            if (tooltip == null || tooltip.PlacementTarget == null || !tooltip.IsOpen) return;

            string text = tooltip.Content as string;

            if (string.IsNullOrWhiteSpace(text)) return;

            _ti.lpszText = text;

            Marshal.StructureToPtr(_ti, _pti, true);
            SendToolTipMessage(ToolTipMessages.TTM_UPDATETIPTEXTW, IntPtr.Zero, _pti);
        }

        private static void OnIsOpenedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            NativeToolTipControl tooltip = dependencyObject as NativeToolTipControl;

            Debug.WriteLine(DateTime.Now + ": OnIsOpenedChange");
            //Debug.WriteLine(DateTime.Now + ": " + new System.Diagnostics.StackTrace().ToString());

            if (tooltip == null) return;

            string text = tooltip.Content as string;

            Debug.WriteLine(DateTime.Now + ": " + text);

            Window parentwindow = FindParent(tooltip.PlacementTarget, typeof(Window)) as Window;
            IntPtr handle = new WindowInteropHelper(parentwindow).Handle;

            if (!added)
            {
                Add(handle);
            }
            else if (lastwindow != handle)
            {
                Update(handle);
            }

            lastwindow = handle;

            if ((bool)e.NewValue && !_tooltipVisible && !string.IsNullOrWhiteSpace(text))
            {
                if (tooltip.Placement == PlacementMode.Mouse)
                {
                    if (_ti.lpszText != text)
                    {
                        _ti.lpszText = text;

                        Marshal.StructureToPtr(_ti, _pti, true);
                        SendToolTipMessage(ToolTipMessages.TTM_UPDATETIPTEXTW, IntPtr.Zero, _pti);
                    }

                    SendToolTipMessage(ToolTipMessages.TTM_ACTIVATE, new IntPtr(1), IntPtr.Zero);
                    SendToolTipMessage(ToolTipMessages.TTM_POPUP, IntPtr.Zero, IntPtr.Zero);

                    if (!_tooltipVisible)
                    {
                        tooltip.OnOpened(new RoutedEventArgs(OpenedEvent, tooltip));
                        _tooltipVisible = true;
                    }
                }
                else
                {
                    throw new NotImplementedException("Placement mode must be 'Mouse'. Was: " + tooltip.Placement);
                }
            }
            else if (_tooltipVisible)
            {
                // TODO(VERY IMPORTANT): Tooltips freeze GUI when switching from one to the other because of closing.
                // To avoid this, detect this state(how ?)

                SendToolTipMessage(ToolTipMessages.TTM_POP, IntPtr.Zero, IntPtr.Zero);
                SendToolTipMessage(ToolTipMessages.TTM_ACTIVATE, IntPtr.Zero, IntPtr.Zero);

                if (_tooltipVisible)
                {
                    tooltip.OnClosed(new RoutedEventArgs(ClosedEvent, tooltip));
                    _tooltipVisible = false;
                }
            }
        }

        ///// <summary>
        ///// Gets the list of routed event handlers subscribed to the specified routed event.
        ///// </summary>
        ///// <param name="element">The UI element on which the event is defined.</param>
        ///// <param name="routedEvent">The routed event for which to retrieve the event handlers.</param>
        ///// <returns>The list of subscribed routed event handlers.</returns>
        //public static RoutedEventHandlerInfo[] GetRoutedEventHandlers(UIElement element, RoutedEvent routedEvent)
        //{
        //    // Get the EventHandlersStore instance which holds event handlers for the specified element.
        //    // The EventHandlersStore class is declared as internal.
        //    var eventHandlersStoreProperty = typeof(UIElement).GetProperty(
        //        "EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
        //    object eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

        //    // Invoke the GetRoutedEventHandlers method on the EventHandlersStore instance 
        //    // for getting an array of the subscribed event handlers.
        //    var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod(
        //        "GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //    var routedEventHandlers = (RoutedEventHandlerInfo[])getRoutedEventHandlers.Invoke(
        //        eventHandlersStore, new object[] { routedEvent });

        //    return routedEventHandlers;
        //}

        public static UIElement FindParent(UIElement uieSearchStart, Type t)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(uieSearchStart);

            DependencyObject lastparent = parent;

            while (parent != null && (parent.GetType() != t && parent.GetType().BaseType != t))
            {
                lastparent = parent;
                parent = VisualTreeHelper.GetParent(parent);
            }

            DependencyObject logicalparent = lastparent;

            while (logicalparent != null && (logicalparent.GetType() != t && logicalparent.GetType().BaseType != t))
            {
                logicalparent = LogicalTreeHelper.GetParent(logicalparent);
            }

            if (parent != null) return (UIElement)parent;
            if (logicalparent != null) return (UIElement)logicalparent;
            return null;
        }
    }

    public class SingleFrameworkPropertyMetadata : FrameworkPropertyMetadata
    {
        public SingleFrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback)
            : base(propertyChangedCallback)
        {
        }

        protected override void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
        {
            base.Merge(baseMetadata, dp);

            PropertyChangedCallback = GetLast(PropertyChangedCallback);
        }

        private static PropertyChangedCallback GetLast(PropertyChangedCallback callback)
        {
            Delegate[] delegates = callback.GetInvocationList();
            return (PropertyChangedCallback)delegates[delegates.Length - 1];
        }
    }
}
