using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    /// <summary>
    /// Interaction logic for ImagePreviewer.xaml
    /// </summary>
    public partial class ImagePreviewer : Window
    {
        public ImagePreviewerViewModel ViewModel { get; private set; }

        public ImagePreviewer(string source, string fileName)
        {
            ViewModel = new ImagePreviewerViewModel
            {
                FileName = fileName,
                SourceUri = source,
            };

            DataContext = ViewModel;
            InitializeComponent();
        }

        private void OpenImage(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ViewModel.SourceUri,
                UseShellExecute = true
            });
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;

            Close();
        }

        private void OnDeactivated(object sender, EventArgs e) => Close();
    }
}
