using Aerochat.Enums;
using Aerochat.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.ShlwApi;

namespace Aerochat.ViewModels
{
    public class SettingViewModel : ViewModelBase
    {
        private string _type;
        private string _name;
        private string _defaultValue;

        public ObservableCollection<string> EnumValues { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> TimeFormatOptions { get; set; }
        public TimeFormat SelectedTimeFormat { get; set; }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        private string _selectedEnumValue;
        public string SelectedEnumValue
        {
            get => _selectedEnumValue;
            set
            {
                _selectedEnumValue = value;
                OnPropertyChanged();
            }
        }
    }

    public class SettingsCategory : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }

    public class SettingsViewModel : ViewModelBase
    {
        public ObservableCollection<SettingsCategory> Categories { get; } = new();

        private SettingsCategory _selectedCategory;
        public SettingsCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ObservableCollection<SettingViewModel> SettingsItems { get; } = new();
    }
}
