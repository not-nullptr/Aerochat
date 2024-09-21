using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Aerochat.Controls.ScrollBar
{
    /// <summary>
    /// Hack to not display the scrollbar gripper when the thumb is too small.
    /// </summary>
    [ValueConversion(typeof(Double), typeof(Boolean))]
    internal class ShowThumbGripperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            // This will be height for vertical scrollbars, and width for
            // horizontal scrollbars.
            double scrollbarSize = (double)value;

            return scrollbarSize > 15;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Unhandled:
            return Binding.DoNothing;
        }
    }
}
