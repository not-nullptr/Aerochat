using Aerochat.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Aerochat.Converter
{
    /// <summary>
    /// Converts a numeric member count to a localized "{N} members" string.
    /// </summary>
    public class MembersCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(LocalizationManager.Instance["HomeMembersCount"], value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
