using Aerochat.Localization;
using Aerochat.Settings;
using Aerochat.Theme;
using Aerochat.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vanara.PInvoke;
using Aerochat.Attributes;
using System.Collections.ObjectModel;
using HarmonyLib;

namespace Aerochat.Windows
{
    public partial class Settings : Window
    {
        public SettingsViewModel ViewModel { get; } = new();
        private const string GeneralCategoryKey = "General";

        public Settings()
        {
            InitializeComponent();
            DataContext = ViewModel;

            // Inject the "General" category first (contains language selector).
            ViewModel.Categories.Add(new SettingsCategory
            {
                Key = GeneralCategoryKey,
                Name = LocalizationManager.Instance["SettingsCategoryGeneral"]
            });

            // Populate remaining categories from reflection on SettingsManager.
            var properties = SettingsManager.Instance.GetType()
                .GetProperties()
                .Where(prop => prop.GetCustomAttribute<SettingsAttribute>() != null);

            foreach (var englishCategory in properties
                .Select(prop => prop.GetCustomAttribute<SettingsAttribute>()!.Category)
                .Distinct())
            {
                var locKey = "SettingsCategory" + englishCategory;
                var translated = LocalizationManager.Instance.Get(locKey);
                ViewModel.Categories.Add(new SettingsCategory
                {
                    Key = englishCategory,
                    Name = translated != locKey ? translated : englishCategory
                });
            }

            ViewModel.SelectedCategory = ViewModel.Categories.First();
            CategoriesListBox.SelectedItem = ViewModel.SelectedCategory;
            var items = GetSettingsFromCategory(ViewModel.SelectedCategory.Key);
            foreach (var item in items)
            {
                ViewModel.SettingsItems.Add(item);
            }
        }

        private static string GetEnumDisplayName(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));
            var englishName = attribute?.Name ?? enumValue.ToString();
            // Translate via reverse-lookup in the English baseline
            return LocalizationManager.Instance.GetByEnglishValue(englishName);
        }

        /// <summary>
        /// Returns the translated display name for a settings property.
        /// The locale key is constructed as "Setting" + the C# property name (e.g. "SettingShowBetaWarning").
        /// Falls back to the English DisplayName from [SettingsAttribute] when no translation is available.
        /// </summary>
        private static string TranslateSettingName(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<SettingsAttribute>()!;
            var locKey = "Setting" + prop.Name;
            var translated = LocalizationManager.Instance.Get(locKey);
            return translated != locKey ? translated : attr.DisplayName;
        }

        public List<SettingViewModel> GetSettingsFromCategory(string category)
        {
            // The "General" category is handled separately since its items are not
            // backed by [Settings]-decorated properties on SettingsManager.
            // category is always the English Key, so comparing to GeneralCategoryKey is sufficient.
            if (category == GeneralCategoryKey)
                return GetGeneralSettings();

            var properties = SettingsManager.Instance.GetType()
                .GetProperties()
                .Where(prop => prop.GetCustomAttribute<SettingsAttribute>()?.Category == category);

            var settings = new List<SettingViewModel>();

            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsEnum)
                {
                    // Get the enum values with their display names
                    var enumValues = Enum.GetValues(prop.PropertyType)
                        .Cast<Enum>()
                        .Select(e => new KeyValuePair<string, string>(e.ToString(), GetEnumDisplayName(e)))
                        .ToList();

                    settings.Add(new SettingViewModel
                    {
                        Key = prop.GetCustomAttribute<SettingsAttribute>()!.DisplayName,
                        Name = TranslateSettingName(prop),
                        Type = "Enum",
                        DefaultValue = prop.GetValue(SettingsManager.Instance)?.ToString(),
                        EnumValues = new ObservableCollection<string>(enumValues.Select(ev => ev.Value)),
                        SelectedEnumValue = GetEnumDisplayName((Enum)prop.GetValue(SettingsManager.Instance))
                    });
                }
                else
                {
                    var attribute = prop.GetCustomAttribute<MultiStringToIntAction>();
                    if (attribute != null)
                    {
                        var displayNames = attribute.GetDisplayNames();
                        
                        settings.Add(new SettingViewModel
                        {
                            Key = prop.GetCustomAttribute<SettingsAttribute>()!.DisplayName,
                            Name = TranslateSettingName(prop),
                            Type = prop.PropertyType.Name,
                            DefaultValue = displayNames.ElementAtOrDefault(SettingsManager.Instance.InputDeviceIndex) ?? LocalizationManager.Instance["SettingsUnknownDevice"],
                            StringValues = new ObservableCollection<string>(displayNames),
                        });
                    }
                    else
                    {
                        settings.Add(new SettingViewModel
                        {
                            Key = prop.GetCustomAttribute<SettingsAttribute>()!.DisplayName,
                            Name = TranslateSettingName(prop),
                            Type = prop.PropertyType.Name,
                            DefaultValue = prop.GetValue(SettingsManager.Instance)?.ToString()
                        });
                    }
                }
            }

            return settings;
        }

        /// <summary>
        /// Builds the settings items for the "General" category, which contains the language selector.
        /// The available languages are discovered dynamically by scanning the Locales folder, so
        /// contributors can add new translations without recompiling the application.
        /// </summary>
        private List<SettingViewModel> GetGeneralSettings()
        {
            var loc = LocalizationManager.Instance;
            var languages = loc.GetAvailableLanguages();
            var currentName = languages.FirstOrDefault(l => l.Code == SettingsManager.Instance.Language).Name
                              ?? SettingsManager.Instance.Language;

            return new List<SettingViewModel>
            {
                new SettingViewModel
                {
                    Key = "Language",
                    Name = loc["SettingLanguage"],
                    Note = loc["SettingLanguageRestartNote"],
                    Type = "StringList",
                    DefaultValue = currentName,
                    StringValues = new ObservableCollection<string>(languages.Select(l => l.Name))
                }
            };
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get the selected category
            var category = (SettingsCategory)CategoriesListBox.SelectedItem;
            ViewModel.SelectedCategory = category;
            // clear the settings list
            ViewModel.SettingsItems.Clear();
            // add the settings from the selected category
            var items = GetSettingsFromCategory(category.Key);
            foreach (var item in items)
            {
                ViewModel.SettingsItems.Add(item);
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // if it's not a number, don't allow it
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Tag is not string name) return;
            if (!int.TryParse(textBox.Text, out var result)) return;
            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);
            if (property is null || property.PropertyType != typeof(int)) return;
            property.SetValue(SettingsManager.Instance, result);
            SettingsManager.Save();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if (checkBox.Tag is not string name) return;
            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);
            if (property is null || property.PropertyType != typeof(bool)) return;
            property.SetValue(SettingsManager.Instance, checkBox.IsChecked);
            SettingsManager.Save();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            if (comboBox.Tag is not string name) return;
            if (comboBox.SelectedItem is not string selectedValue) return;

            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);

            if (property is null) return;

            if (property.PropertyType.IsEnum)
            {
                var enumValue = Enum.GetValues(property.PropertyType)
                    .Cast<Enum>()
                    .FirstOrDefault(v => GetEnumDisplayName(v) == selectedValue);

                if (enumValue != null)
                    property.SetValue(SettingsManager.Instance, enumValue);
            }
            else if (property.PropertyType.IsInteger())
            {
                int index = 0;
                foreach (string comboBoxItem in comboBox.Items)
                {
                    if (comboBoxItem == selectedValue) break;
                    index++;
                }
                property.SetValue(SettingsManager.Instance, index);
            }

            SettingsManager.Save();
        }

        /// <summary>
        /// Handles language selection changes from the StringList combobox in the General category.
        /// Finds the language code that corresponds to the selected display name and reloads the locale.
        /// </summary>
        private void StringList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem is not string selectedName)
                return;

            var languages = LocalizationManager.Instance.GetAvailableLanguages();
            var match = languages.FirstOrDefault(l => l.Name == selectedName);
            if (match == default)
                return;

            // Do nothing if the language hasn't actually changed (e.g. ComboBox init fires SelectionChanged).
            if (match.Code == SettingsManager.Instance.Language)
                return;

            // Persist the new language choice before restarting.
            SettingsManager.Instance.Language = match.Code;
            SettingsManager.Save();

            // Restart the application so every already-open window picks up the new language.
            System.Diagnostics.Process.Start(Environment.ProcessPath!);
            Application.Current.Shutdown();
        }
    }
}
