using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Aerochat.Converter
{
    /// <summary>
    /// Converts a set of booleans to a single boolean result. This will output true if all inputs are
    /// true, or false otherwise.
    /// </summary>
    internal class MultiBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return !values.Contains(false);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { false };
        }
    }
}
