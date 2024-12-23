using Aerochat.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Aerochat.Windows
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;  // Default empty string for no selection

            if (value is Enum enumValue)
            {
                var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
                var displayAttribute = fieldInfo?.GetCustomAttribute<DisplayAttribute>();
                return displayAttribute?.Name ?? enumValue.ToString();
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle conversion back from the display name to enum
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return null;

            try
            {
                var enumType = targetType;
                var enumValue = Enum.GetValues(enumType)
                                    .Cast<Enum>()
                                    .FirstOrDefault(e => e.GetType()
                                                          .GetField(e.ToString())
                                                          ?.GetCustomAttribute<DisplayAttribute>()?.Name == value.ToString());

                return enumValue;
            }
            catch
            {
                return null;
            }
        }
    }
}
