using Aerochat.Pages.Wizard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.DwmApi;
using Vanara.PInvoke;
using MouseButtonState = System.Windows.Input.MouseButtonState;

namespace Aerochat.Windows
{
    public partial class CategoryWizard : Window
    {
        public CategoryWizard()
        {
            InitializeComponent();
            SourceInitialized += DebugWindow_SourceInitialized;
            try
            {
                RenderOptions.ClearTypeHintProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata { DefaultValue = ClearTypeHint.Enabled });
                TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata { DefaultValue = TextFormattingMode.Display });
            } catch (Exception) { }
            WizardFrame.Content = new WizardHomePage();
            WizardFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        }

        public void NavigateTo(Page page)
        {
            WizardFrame.Content = page;
        }

        private void DebugWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hWnd = new WindowInteropHelper(this).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(hWnd);
            mainWindowSrc.AddHook(WndProc);
            mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
            var desktop = Graphics.FromHwnd(hWnd);
            int DpiMargin(int px)
            {
                return px * (int)(desktop.DpiX / 96);
            }
            int borderSize = 8;
            MARGINS margins = new(DpiMargin(borderSize), DpiMargin(borderSize), DpiMargin(64), DpiMargin(borderSize));
            DwmExtendFrameIntoClientArea(hWnd, in margins);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // try and convert msg to a uint
            uint msgUint = (uint)msg;
            var message = (WindowMessage)msg;
            if (message.ToString().StartsWith("WM_NC"))
            {
                if (handled = DwmDefWindowProc(hWnd, msgUint, wParam, lParam, out var res))
                    return res;
            }
            switch (message)
            {
                case WindowMessage.WM_NCCALCSIZE:
                    handled = true;
                    return IntPtr.Zero;
                case WindowMessage.WM_CREATE:
                    GetWindowRect(hWnd, out var rcClient);
                    SetWindowPos(hWnd, HWND.NULL, rcClient.Left, rcClient.Top, rcClient.Width, rcClient.Height, SetWindowPosFlags.SWP_FRAMECHANGED);
                    handled = true;
                    return 0;
            }
            handled = false;
            return IntPtr.Zero;
        }

        private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
