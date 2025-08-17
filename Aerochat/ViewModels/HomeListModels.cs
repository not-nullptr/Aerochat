using System.Collections.ObjectModel;

namespace Aerochat.ViewModels
{
    public class HomeListItem : ViewModelBase
    {
        private string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }

    public class HomeListCategory : ViewModelBase
    {
        private string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<HomeListItem> Items { get; } = new();
    }
}
