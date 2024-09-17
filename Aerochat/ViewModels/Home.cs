using Aerochat.Theme;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class HomeWindowViewModel : ViewModelBase
    {
        private UserViewModel _currentUser = new()
        {
            Avatar = "/Resources/Frames/PlaceholderPfp.png",
            Id = 0,
            Name = "nullptr",
            Username = "notnullptr"
        };

        public UserViewModel CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public ObservableCollection<HomeListViewCategory> Categories { get; } = new();

        private AdViewModel _ad = new();
        public AdViewModel Ad
        {
            get => _ad;
            set => SetProperty(ref _ad, value);
        }

        public ObservableCollection<HomeButtonViewModel> Buttons { get; } = new();

        public ThemeService Theme { get; } = ThemeService.Instance;
    }
}
