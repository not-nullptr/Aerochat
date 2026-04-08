using System;
using System.Globalization;
using System.Windows.Data;
using DSharpPlus.Entities;

namespace Aerochat.Helpers
{
    /// <summary>
    /// Converts presence status string (e.g. "Online", "Idle") to DSharpPlus UserStatus enum for ProfilePictureFrame binding.
    /// Returns Offline when value is null or invalid.
    /// </summary>
    public class StatusStringToUserStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not string s)
                return UserStatus.Offline;
            return s switch
            {
                "Online" => UserStatus.Online,
                "Idle" => UserStatus.Idle,
                "DoNotDisturb" => UserStatus.DoNotDisturb,
                "Invisible" => UserStatus.Invisible,
                "Offline" => UserStatus.Offline,
                _ => UserStatus.Offline
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
