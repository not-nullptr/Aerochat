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
        public HomeWindowViewModel()
        {
            Notices.CollectionChanged += (_, _) => InvokePropertyChanged(nameof(CurrentNotice));
        }

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

        public ObservableCollection<NoticeViewModel> Notices { get; } = new();

        public NoticeViewModel? CurrentNotice => Notices.FirstOrDefault();

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ObservableCollection<NewsViewModel> News { get; } = new();
        private NewsViewModel? _currentNews;
        public NewsViewModel? CurrentNews
        {
            get => _currentNews;
            set => SetProperty(ref _currentNews, value);
        }
    }
}
