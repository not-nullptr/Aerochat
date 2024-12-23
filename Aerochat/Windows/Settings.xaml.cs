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

namespace Aerochat.Windows
{
    public partial class Settings : Window
    {
        public SettingsViewModel ViewModel { get; } = new();
        public Settings()
        {
            InitializeComponent();
            DataContext = ViewModel;
            // using reflection, get all properties using the SettingsAttribute decorator off SettingsManager.Instance
            var properties = SettingsManager.Instance.GetType()
                .GetProperties()
                .Where(prop => prop.GetCustomAttribute<SettingsAttribute>() != null);
            // add all categories
            foreach (var category in properties.Select(prop => prop.GetCustomAttribute<SettingsAttribute>()!.Category).Distinct())
            {
                ViewModel.Categories.Add(new SettingsCategory { Name = category });
            }

            ViewModel.SelectedCategory = ViewModel.Categories.First();
            CategoriesListBox.SelectedItem = ViewModel.SelectedCategory;
            var items = GetSettingsFromCategory(ViewModel.SelectedCategory.Name);
            foreach (var item in items)
            {
                ViewModel.SettingsItems.Add(item);
            }
        }

        private string GetEnumDisplayName(Enum enumValue)
        {
            // Get the field corresponding to the enum value
            var field = enumValue.GetType().GetField(enumValue.ToString());
            // Get the DisplayAttribute from the field
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));

            return attribute?.Name ?? enumValue.ToString(); // Return the display name or the enum value if no display name is found
        }

        public List<SettingViewModel> GetSettingsFromCategory(string category)
        {
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
                        Name = prop.GetCustomAttribute<SettingsAttribute>()!.DisplayName,
                        Type = "Enum",
                        DefaultValue = prop.GetValue(SettingsManager.Instance)?.ToString(),
                        EnumValues = new ObservableCollection<string>(enumValues.Select(ev => ev.Value)),
                        SelectedEnumValue = GetEnumDisplayName((Enum)prop.GetValue(SettingsManager.Instance))
                    });
                }
                else
                {
                    settings.Add(new SettingViewModel
                    {
                        Name = prop.GetCustomAttribute<SettingsAttribute>()!.DisplayName,
                        Type = prop.PropertyType.Name,
                        DefaultValue = prop.GetValue(SettingsManager.Instance)?.ToString()
                    });
                }
            }

            return settings;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get the selected category
            var category = (SettingsCategory)CategoriesListBox.SelectedItem;
            ViewModel.SelectedCategory = category;
            // clear the settings list
            ViewModel.SettingsItems.Clear();
            // add the settings from the selected category
            var items = GetSettingsFromCategory(category.Name);
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
            int.TryParse(textBox.Text, out var result);
            // get the property name
            var name = textBox.Tag.ToString();
            // get all properties with their decorators and find the one where name == the name on the decorator)
            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);
            // if the types don't match, return
            if (property!.PropertyType != typeof(int)) return;
            property!.SetValue(SettingsManager.Instance, result);
            // save the settings
            SettingsManager.Save();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var name = checkBox.Tag.ToString();
            // get all properties with their decorators and find the one where name == the name on the decorator)
            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);
            // set the value of the property to the value of the textbox
            if (property!.PropertyType != typeof(bool)) return;
            property!.SetValue(SettingsManager.Instance, checkBox.IsChecked);
            // save the settings
            SettingsManager.Save();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var name = comboBox.Tag.ToString();
            var selectedValue = comboBox.SelectedItem.ToString();

            // Find the corresponding property in SettingsManager
            var property = SettingsManager.Instance.GetType()
                .GetProperties()
                .FirstOrDefault(prop => prop.GetCustomAttribute<SettingsAttribute>()?.DisplayName == name);

            // If it's an Enum, map display name to enum value
            if (property!.PropertyType.IsEnum)
            {
                var enumValue = Enum.GetValues(property.PropertyType)
                    .Cast<Enum>()
                    .FirstOrDefault(e => GetEnumDisplayName(e) == selectedValue);

                if (enumValue != null)
                {
                    property.SetValue(SettingsManager.Instance, enumValue);
                }
            }

            // Save the settings
            SettingsManager.Save();
        }
    }
}
