using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aerochat.Attributes;

namespace Aerochat.Helpers
{
    public static class EnumHelper
    {
        public static string GetDisplayName(Enum enumValue)
        {
            var fieldInfo = enumValue.GetType()
                .GetField(enumValue.ToString(), BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null)
            {
                var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();

                if (displayAttribute != null)
                {
                    return displayAttribute.Name;
                }
            }

            return enumValue.ToString();
        }

        public static List<(string DisplayName, T EnumValue)> GetEnumDisplayList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => (GetDisplayName(e), e))  // Create tuple of DisplayName and EnumValue
                .ToList();
        }
    }
}
