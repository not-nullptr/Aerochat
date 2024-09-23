using Aerochat.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Aerochat.Settings
{
    // decorator for settings with the category name and settings display name
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SettingsAttribute : Attribute
    {
        public string Category { get; }
        public string DisplayName { get; }

        public SettingsAttribute(string category, string displayName)
        {
            Category = category;
            DisplayName = displayName;
        }
    }
    public class SettingsManager : ViewModelBase
    {
        #region Boilerplate
        public static SettingsManager Instance = new();

        private static readonly string _settingsFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
            $"{Assembly.GetEntryAssembly()!.GetName().Name}.json");

        public static void Save()
        {
            // Serialize non-static properties
            var properties = Instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && !prop.GetMethod.IsStatic)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(Instance));

            var json = JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);

            // Call OnPropertyChanged for all properties
            foreach (var property in properties.Keys)
            {
                Instance.InvokePropertyChanged(property);
            }
        }

        public static void Load()
        {
            if (!File.Exists(_settingsFilePath)) return;

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            var properties = Instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && !prop.GetMethod.IsStatic);

            foreach (var property in properties)
            {
                if (settings == null || !settings.ContainsKey(property.Name)) continue;

                try
                {
                    var jsonElement = settings[property.Name];
                    var value = JsonSerializer.Deserialize(jsonElement.GetRawText(), property.PropertyType);
                    property.SetValue(Instance, value);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Error deserializing property '{property.Name}'", ex);
                }
            }
        }
        #endregion

        #region Private Settings
        public string Token { get; set; } = "";
        public Dictionary<ulong, ulong> LastReadMessages { get; set; } = new();
        public Dictionary<ulong, ulong> SelectedChannels { get; set; } = new();
        public DateTime ReadRecieptReference { get; set; } = DateTime.MinValue;
        public bool WarningShown { get; set; } = false;

        #endregion

        #region Public Settings
        [Settings("Alerts", "Nudge intensity")]
        public int NudgeIntensity { get; set; } = 10;

        [Settings("Alerts", "Nudge length")]
        public int NudgeLength { get; set; } = 2;

        [Settings("Appearance", "Use the Windows XP caption button theme in non-native titlebar")]
        public bool XPCaptionButtons { get; set; } = false;
        #endregion
    }
}
