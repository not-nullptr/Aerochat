using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
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

namespace Aerochat.Windows
{
    public partial class Dialog : Window
    {
        public DialogViewModel ViewModel { get; set; } = new DialogViewModel();
        public Dialog(string title, string description, Icon icon)
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Title = title;
            ViewModel.Description = description;
            ViewModel.Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            Loaded += Dialog_Loaded;
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (Owner == null)
                throw new InvalidOperationException("Dialog must have an owner window.");
            double dialogWidth = ActualWidth;
            double dialogHeight = ActualHeight;
            double ownerWidth = Owner.ActualWidth;
            double ownerHeight = Owner.ActualHeight;

            Left = Owner.Left + (ownerWidth - dialogWidth) / 2;
            Top = Owner.Top + (ownerHeight - dialogHeight) / 2;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
