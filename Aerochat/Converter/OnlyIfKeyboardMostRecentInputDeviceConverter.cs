using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Aerochat.Converter
{
    /// <summary>
    /// Returns true only if the keyboard was the most recent input device.
    /// </summary>
    /// <remarks>
    /// Hack for certain button styles.
    /// </remarks>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class OnlyIfKeyboardMostRecentInputDeviceConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(InputManager.Current.MostRecentInputDevice is KeyboardDevice))
            {
                return false;
            }

            return (bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
