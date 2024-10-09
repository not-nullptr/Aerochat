using Aerochat.Hoarder;
using Aerochat.HwndHosts;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
using static Vanara.PInvoke.DwmApi;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Macros;
using System.Windows.Media.Animation;
using Vanara.PInvoke;
using System.Windows.Interop;
using System.Drawing;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using Aerochat.Pages.Wizard;
using Aerochat.Controls;

namespace Aerochat.Windows
{
    public partial class DebugWindow : Window
    {
        public DebugWindowViewModel ViewModel { get; } = new DebugWindowViewModel();
        public DebugWindow()
        {
            new CategoryWizard().Show();
            InitializeComponent();
            Loaded += DebugWindow_Loaded;
        }

        private void DebugWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
