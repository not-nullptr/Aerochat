using Aerochat.Enums;
using Aerochat.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Aerochat.Attributes;

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
                // Use user path in DEBUG too so that settings (e.g. favorites) persist across
                // rebuilds and runs; the bin folder can be cleaned or vary by configuration.
                _settingsFilePath = userPath;
#else
                // Otherwise, we only set the active path to the application-local path
                // if the file already exists. This will not affect migration as the
                // Aerochat 0.2 installer manages migration of 0.1-era configuration.
                if (Path.Exists(applicationLocalPath))
                {
                    _settingsFilePath = applicationLocalPath;
                }
                else
                {
                    _settingsFilePath = userPath;
                }
#endif

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

            // Ensure that the directory exists:
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);

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
            var path = SettingsFilePath;
#if DEBUG
            // Migrate from legacy bin folder config so favorites and other settings are not lost
            if (!File.Exists(path))
            {
                var applicationLocalPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                    $"{Assembly.GetEntryAssembly()!.GetName().Name}.json");
                if (File.Exists(applicationLocalPath))
                {
                    var migrationJson = File.ReadAllText(applicationLocalPath);
                    if (TryLoadFromJson(migrationJson))
                        Save(); // write to new user path
                    return;
                }
            }
#endif
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            TryLoadFromJson(json);
        }

        private static bool TryLoadFromJson(string json)
        {
            Dictionary<string, JsonElement>? settings = null;
            try
            {
                settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            }
            catch (Exception)
            {
                return false;
            }

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
                catch (JsonException)
                {
                    // skip property
                }
            }
            return true;
        }
#endregion

        #region Private Settings
        public string Token { get; set; } = "";
        public Dictionary<ulong, ulong> LastReadMessages { get; set; } = new();
        public Dictionary<ulong, ulong> SelectedChannels { get; set; } = new();
        public List<ulong> RecentDMChats { get ; set; } = new();
        public List<ulong> RecentServerChats { get; set; } = new();
        public DateTime ReadRecieptReference { get; set; } = DateTime.MinValue;
        public bool HasUserLoggedInBefore { get; set; } = false;
        public bool HasWarnedAboutVoiceChat { get; set; } = false;
        public List<ulong> ViewedNotices { get; set; } = [];
        public List<ulong> FavoriteConversationIds { get; set; } = [];
        public List<ulong> FavoriteGuildIds { get; set; } = [];

        /// <summary>
        /// BCP-47 language code for the UI locale (e.g. "en", "fr").
        /// Defaults to English. Locale files must be present in the Locales/ folder.
        /// </summary>
        public string Language { get; set; } = "en";

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

        [Settings("Alerts", "Read message notifications with text to speech")]
        public bool ReadMessageNotifications { get; set; } = false;

        [Settings("Alerts", "Read online notifications with text to speech")]
        public bool ReadOnlineNotifications { get; set; } = false;

        [Settings("Alerts", "Allow text-to-speech messages to be read aloud")]
        public bool EnableMessageTts { get; set; } = true;

        [Settings("Alerts", "Nudge intensity")]
        public int NudgeIntensity { get; set; } = 10;

        [Settings("Alerts", "Nudge length")]
        public int NudgeLength { get; set; } = 2;

        [Settings("Activity", "Make my status go \"away\" when I open a fullscreen application")]
        public bool GoIdleWithFullscreenProgram  { get; set; } = true;

        [Settings("Appearance", "Display unimplemented buttons for eyecandy")]
        public bool DisplayUnimplementedButtons { get; set; } = false;

        [Settings("Appearance", "Configure when to use the non-native basic titlebar fallback")]
        public BasicTitlebarSetting BasicTitlebar { get; set; } = BasicTitlebarSetting.Automatic;

        [Settings("Appearance", "Use the Windows XP caption button theme in non-native titlebar")]
        public bool XPCaptionButtons { get; set; } = false;

        [Settings("Appearance", "Highlight messages that have mentioned you")]
        public bool HighlightMentions { get; set; } = true;

        [Settings("Appearance", "Show news on the home page")]

        public bool DisplayHomeNews { get; set; } = true;


        [Settings("Appearance", "Show community-submitted ads on the home page")]

        public bool DisplayAds { get; set; } = true;

        [Settings("Appearance", "Show the Aerochat link on the chat window")]

        public bool DisplayAerochatAttribution { get; set; } = true;

        [Settings("Appearance", "Show link previews in chat")]

        public bool DisplayLinkPreviews { get; set; } = true;

        [Settings("Appearance", "Time format")]
        public TimeFormat SelectedTimeFormat { get; set; } = TimeFormat.TwentyFourHour;

        [Settings("Appearance", "Enable developer commands in context menus")]

        public bool DiscordDeveloperMode { get; set; } = false;
        
        [Settings("Audio", "Input device [Requires rejoining if changed mid-call]"), MultiStringToIntAction("FetchInputDevices")]
        
        public int InputDeviceIndex { get; set; } = 0;

        #endregion
    }
}
