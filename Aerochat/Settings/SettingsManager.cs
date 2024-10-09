using Aerochat.Pages.Wizard;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Aerochat.Settings
{
    public enum CategoryLambdaType
    {
        DM,
        Guild
    }

    public class CategoryLambda
    {
        private void Recompile()
        {
            if (_lambda == null) return;
            var config = new ParsingConfig
            {
                CustomTypeProvider = new ProvideAllTypesProvider()
            };
            if (_type == CategoryLambdaType.DM)
            {
                ParameterExpression channelExpression = Expression.Parameter(typeof(DiscordChannel), "channel");
                LambdaExpression lambda = DynamicExpressionParser.ParseLambda(config, [channelExpression], typeof(bool), Lambda);
                var compiled = lambda.Compile();
                _compiledLambda = compiled;
            } else
            {
                ParameterExpression guildExpression = Expression.Parameter(typeof(DiscordGuild), "guild");
                LambdaExpression lambda = DynamicExpressionParser.ParseLambda(config, [guildExpression], typeof(bool), Lambda);
                var compiled = lambda.Compile();
                _compiledLambda = compiled;
            }
        }

        public Delegate? GetOrCompile()
        {
            if (_compiledLambda is null)
            {
                if (_lambda == null) return null;
                Recompile();
            }
            return _compiledLambda;
        }

        private Delegate? _compiledLambda;
        private CategoryLambdaType _type { get; set; }
        public CategoryLambdaType Type
        {
            get => _type;
            set
            {
                _type = value;
                Recompile();
            }
        }
        private string _lambda { get; set; }
        public string Lambda
        {
            get => _lambda;
            set
            {
                _lambda = value;
                Recompile();
            }
        }
        public string Name { get; set; }
    }

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
        public bool HasWarnedAboutVoiceChat { get; set; } = false;
        public List<ulong> ViewedNotices { get; set; } = [];
        public List<CategoryLambda> CategoryLambdas { get; set; } = new()
        {
            new()
            {
                Name = "Conversations",
                Type = CategoryLambdaType.DM,
                Lambda = "true"
            }
        };

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
