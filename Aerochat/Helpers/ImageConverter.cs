using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Aerochat.Helpers
{
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return DependencyProperty.UnsetValue;
            else if (value is string)
            {
                var uri = value as string;
                if (uri is null || string.IsNullOrWhiteSpace(uri)) return DependencyProperty.UnsetValue;
                return new BitmapImage(new Uri(uri));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // According to https://msdn.microsoft.com/en-us/library/system.windows.data.ivalueconverter.convertback(v=vs.110).aspx#Anchor_1
            // (kudos Scott Chamberlain), if you do not support a conversion 
            // back you should return a Binding.DoNothing or a 
            // DependencyProperty.UnsetValue
            return Binding.DoNothing;
        }
    }
}