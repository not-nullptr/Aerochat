using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Aerochat.Windows
{
    public class MessageWindow : IDisposable
    {
        // Public message -- keep stable API.
        public const uint WM_AEROCHAT_CLOSE_FOR_REINSTALLATION = WM_APP + 26;

        // This name should never change, because rudimentary IPC methods (i.e. the installer) are
        // dependent on it.
        private static readonly string s_windowClass = "Aerochat_MessageWindow_" + 
            Guid.Parse(((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0]).Value).ToString().ToUpper();

        private static bool s_windowClassRegistered = false;

        private HWND _hWnd;

        WindowProc wndProcDelegate;

        public void Dispose()
        {
            DestroyWindow(_hWnd);
        }

        public MessageWindow()
        {
            wndProcDelegate = WndProc;

            if (!s_windowClassRegistered)
            {
                WNDCLASS cls = new()
                {
                    lpszClassName = s_windowClass,
                    lpfnWndProc = wndProcDelegate
                };

                if (RegisterClass(cls).IsInvalid)
                {
                    MessageBox(HWND.NULL, "Fuck", "Fawk", MB_FLAGS.MB_OK);
                }
            }

            _hWnd = CreateWindowEx(
                0,
                s_windowClass,
                String.Empty,
                0,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );
        }

        private nint WndProc(HWND hWnd, uint uMsg, nint wParam, nint lParam)
        {
            switch (uMsg)
            {
                case WM_AEROCHAT_CLOSE_FOR_REINSTALLATION:
                {
                    Application.Current.Shutdown();
                    return 0;
                }
            }

            return DefWindowProc(hWnd, uMsg, wParam, lParam);
        }
    }
}
