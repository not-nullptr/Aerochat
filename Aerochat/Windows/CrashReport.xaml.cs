using Aerochat.Theme;
using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Vanara.PInvoke;

namespace Aerochat.Windows
{
    public partial class CrashReport : Window
    {
        public CrashReport()
        {
            InitializeComponent();
        }

        public void SetCrashReport(string text)
        {
            PART_LogTextbox.Text = "Aerochat version " + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)
#if AEROCHAT_RC
                + " " + AssemblyInfo.RC_REVISION
#endif
                + "\n";
            PART_LogTextbox.Text += text;
        }

        private void OnClickDismissButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClickRestartAerochatButton(object sender, RoutedEventArgs e)
        {
            Shell32.ShellExecute(HWND.NULL, "open", System.Environment.ProcessPath, null, null, ShowWindowCommand.SW_SHOWNORMAL);
            Close();
        }
    }
}
