using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class NonNativeItem : ViewModelBase
    {
        private string _name;
        public required string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _key;
        public required string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }
    }

    public class NonNativeTooltipViewModel : ViewModelBase
    {
        public ObservableCollection<NonNativeItem> Items { get; } = new();
    }
}
