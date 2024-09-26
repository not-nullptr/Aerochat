using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aerochat.Controls
{
    public partial class NoDwmTitlebar : UserControl
    {
        public BasicTitlebarViewModel ViewModel = new();
        public NoDwmTitlebar()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            // maximize the window
            if (Window.GetWindow(this) is not Window window) return;
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            // minimize the window
            if (Window.GetWindow(this) is not Window window) return;
            window.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // close the window
            if (Window.GetWindow(this) is not Window window) return;
            window.Close();
        }

        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is not Window window) return;
            window.Close();
        }
    }
}
