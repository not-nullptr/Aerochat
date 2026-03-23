using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Aerochat.Localization
{
    /// <summary>
    /// Singleton that manages application translations.
    /// Locale JSON files live in a "Locales" folder next to the executable.
    /// Each file is named by its BCP-47 language code (e.g. "en.json", "fr.json").
    /// </summary>
    public class LocalizationManager : INotifyPropertyChanged
    {
        public static readonly LocalizationManager Instance = new();

        private Dictionary<string, string> _current = new();
        private Dictionary<string, string> _fallback = new();

        private string _languageCode = "en";
        public string LanguageCode => _languageCode;

        /// <summary>
        /// Path to the folder that contains locale JSON files at runtime.
        /// </summary>
        public static string LocalesDirectory =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                "Locales");

        /// <summary>
        /// Indexer used by WPF bindings: {Binding [KeyName], Source={x:Static loc:LocalizationManager.Instance}}
        /// Also used by the <see cref="LocExtension"/> markup extension.
        /// </summary>
        public string this[string key] => Get(key);

        /// <summary>
        /// Returns the translated string for <paramref name="key"/>.
        /// Falls back to English and then to the key itself if no translation is found.
        /// </summary>
        public string Get(string key)
        {
            if (_current.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;

            if (_fallback.TryGetValue(key, out var fallback) && !string.IsNullOrEmpty(fallback))
                return fallback;

            return key;
        }

        /// <summary>
        /// Loads a locale by its BCP-47 code (e.g. "en", "fr").
        /// English is always loaded first as the fallback dictionary.
        /// </summary>
        public void LoadLanguage(string code)
        {
            // Always (re)load the English baseline so untranslated keys degrade gracefully.
            _fallback = LoadFile("en") ?? new Dictionary<string, string>();

            _languageCode = code;
            _current = (code == "en") ? _fallback : (LoadFile(code) ?? _fallback);

            // Notify all WPF bindings that use the indexer.
            OnPropertyChanged("Item[]");
        }

        /// <summary>
        /// Returns every language available in the Locales directory as (code, display name) pairs.
        /// The display name is read from the "_meta_language" key inside the file.
        /// </summary>
        public List<(string Code, string Name)> GetAvailableLanguages()
        {
            var languages = new List<(string Code, string Name)>();

            if (!Directory.Exists(LocalesDirectory))
                return languages;

            foreach (var file in Directory.GetFiles(LocalesDirectory, "*.json"))
            {
                var code = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(code))
                    continue;

                try
                {
                    var json = File.ReadAllText(file);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    var name = dict?.GetValueOrDefault("_meta_language") ?? code;
                    languages.Add((code, name));
                }
                catch
                {
                    languages.Add((code, code));
                }
            }

            // Sort so English always comes first, then alphabetically.
            languages.Sort((a, b) =>
            {
                if (a.Code == "en") return -1;
                if (b.Code == "en") return 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            return languages;
        }

        /// <summary>
        /// Reverse-looks up a key by its English value and returns the current-locale translation.
        /// Useful for translating strings that are stored as English text (e.g. enum display names).
        /// Falls back to <paramref name="englishValue"/> itself when no matching key is found.
        /// </summary>
        public string GetByEnglishValue(string englishValue)
        {
            foreach (var kv in _fallback)
            {
                if (kv.Value == englishValue)
                    return Get(kv.Key);
            }
            return englishValue;
        }

        private static Dictionary<string, string>? LoadFile(string code)
        {
            var path = Path.Combine(LocalesDirectory, $"{code}.json");
            if (!File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch
            {
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
