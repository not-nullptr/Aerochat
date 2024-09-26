using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class HomeListItem : ViewModelBase
    {
        private string _name;
        public required string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public class HomeListCategory : ViewModelBase
        {
            private string _name;
            public required string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }
            public ObservableCollection<HomeListItem> Items { get; } = new();
        }
    }
}
