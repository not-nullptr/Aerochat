using Aerochat.Theme;
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
using System.Windows.Shapes;

namespace Aerochat.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            //Color currentColor = (Color)ColorConverter.ConvertFromString(ThemeService.Instance.Color);
            //Red.Value = currentColor.R;
            //Green.Value = currentColor.G;
            //Blue.Value = currentColor.B;
        }

        private void RefreshColor(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte red = (byte)Red.Value;
            byte green = (byte)Green.Value;
            byte blue = (byte)Blue.Value;

            //ThemeService.Instance.Color = $"#{red:X2}{green:X2}{blue:X2}";
        }
    }
}
