using Aerochat.Enums;
using Aerochat.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Aerochat.Windows
{
    public class TimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // Retrieve SelectedTimeFormat from the SettingsManager
                var format = SettingsManager.Instance.SelectedTimeFormat;
                string formatString = format == TimeFormat.TwentyFourHour ? "HH:mm:ss" : "h:mm tt";

                return dateTime.ToString(formatString);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
