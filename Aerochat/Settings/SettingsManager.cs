using Aerochat.Enums;
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

        private static string? _settingsFilePath = null;

        private static string SettingsFilePath
        {
            get
            {
                if (_settingsFilePath != null)
                {
                    return _settingsFilePath;
                }

                // Application-local configuration; stored per application instance.
                // Useful for development purposes only. For general users, we prefer
                // loading the configuration from the Application Data folder.
                string applicationLocalPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, // <installation dir>
                    $"{Assembly.GetEntryAssembly()!.GetName().Name}.json");        // \Aerochat.json

                // User-local configuration. Since Aerochat since version 0.2 can be
                // installed for all users (i.e. in Program Files), we want to use the
                // user-specific Application Data folder to store configuration.
                string userPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // %AppData%
                    Assembly.GetEntryAssembly()!.GetName().Name,                          // \Aerochat
                    "config.json");                                                       // \config.json

#if DEBUG
                // For debug builds, we always want to supply an application-specific
                // path for developer convenience. It's not a problem since these builds
                // should stay local to the developer's system.
                if (true)
#else
                // Otherwise, we only set the active path to the application-local path
                // if the file already exists. This will not affect migration as the
                // Aerochat 0.2 installer manages migration of 0.1-era configuration.
                if (Path.Exists(applicationLocalPath))
#endif
                {
                    _settingsFilePath = applicationLocalPath;
                }
                else
                {
                    _settingsFilePath = userPath;
                }

                return _settingsFilePath;
            }

            set {}
        }

        public static void Save()
        {
            // Serialize non-static properties
            var properties = Instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && !prop.GetMethod.IsStatic)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(Instance));

            var json = JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);

            // Call OnPropertyChanged for all properties
            foreach (var property in properties.Keys)
            {
                Instance.InvokePropertyChanged(property);
            }
        }

        public static void Load()
        {
            if (!File.Exists(SettingsFilePath)) return;

            var json = File.ReadAllText(SettingsFilePath);
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
        public List<ulong> RecentDMChats { get ; set; } = new();
        public List<ulong> RecentServerChats { get; set; } = new();
        public DateTime ReadRecieptReference { get; set; } = DateTime.MinValue;
        public bool WarningShown { get; set; } = false;
        public bool HasWarnedAboutVoiceChat { get; set; } = false;
        public List<ulong> ViewedNotices { get; set; } = [];

        #endregion

        #region Public Settings
        // Volatile setting, will be removed when the beta warning is removed.
        [Settings("Alerts", "Show beta warning on startup")]
        public bool ShowBetaWarning { get; set; } = true;

        [Settings("Alerts", "Notify me when my friends come online")]
        public bool NotifyFriendOnline { get; set; } = true;

        [Settings("Alerts", "Notify me when I receive a direct message")]
        public bool NotifyDm { get; set; } = true;

        [Settings("Alerts", "Notify me when I am mentioned in a group chat or server")]
        public bool NotifyMention { get; set; } = true;

        [Settings("Alerts", "Play a notification when any new message has been sent in chat")]
        public bool NotifyChat { get; set; } = true;

        [Settings("Alerts", "Open a new chat window whenever I get a DM")]
        public bool AutomaticallyOpenNotification { get; set; } = false;


        [Settings("Alerts", "Play notification sounds")]
        public bool PlayNotificationSounds { get; set; } = true;

        [Settings("Alerts", "Nudge intensity")]
        public int NudgeIntensity { get; set; } = 10;

        [Settings("Alerts", "Nudge length")]
        public int NudgeLength { get; set; } = 2;

        [Settings("Activity", "Make my status go \"away\" when I open a fullscreen application")]
        public bool GoIdleWithFullscreenProgram  { get; set; } = true;

        [Settings("Appearance", "Display unimplemented buttons for eyecandy")]
        public bool DisplayUnimplementedButtons { get; set; } = false;

        [Settings("Appearance", "Use the Windows XP caption button theme in non-native titlebar")]
        public bool XPCaptionButtons { get; set; } = false;

        [Settings("Appearance", "Show the Aerochat Discord server link button on the home page")]

        public bool DisplayDiscordServerLink { get; set; } = true;


        [Settings("Appearance", "Show news on the home page")]

        public bool DisplayHomeNews { get; set; } = true;


        [Settings("Appearance", "Show community-submitted ads on the home page")]

        public bool DisplayAds { get; set; } = true;

        [Settings("Appearance", "Show the Aerochat link on the chat window")]

        public bool DisplayAerochatAttribution { get; set; } = true;

        [Settings("Appearance", "Time format")]
        public TimeFormat SelectedTimeFormat { get; set; } = TimeFormat.TwentyFourHour;

        [Settings("Appearance", "Enable developer commands in context menus")]

        public bool DiscordDeveloperMode { get; set; } = false;
        #endregion
    }
}
