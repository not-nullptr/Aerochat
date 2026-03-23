using Aerochat.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Aerochat.Converter
{
    /// <summary>
    /// Converts (AuthorName, RawMessage) into the localized notification header string.
    /// </summary>
    public class NotificationTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var name = values.Length > 0 ? values[0] as string ?? "" : "";
            var raw  = values.Length > 1 ? values[1] as string ?? "" : "";
            var loc  = LocalizationManager.Instance;

            if (raw == "[nudge]")
                return string.Format(loc["NotificationNudgeFormat"], name);

            return string.Format(loc["NotificationSaysFormat"], name);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
