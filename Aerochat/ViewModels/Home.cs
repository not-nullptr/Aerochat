using Aerochat.Theme;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Aerochat.ViewModels.HomeListViewCategory;

namespace Aerochat.ViewModels
{
    public class HomeWindowViewModel : ViewModelBase
    {
        public HomeWindowViewModel()
        {
            Notices.CollectionChanged += (_, _) => InvokePropertyChanged(nameof(CurrentNotice));
            // Initialize filtered categories
            FilteredCategories = new ObservableCollection<HomeListViewCategory>();
            Categories.CollectionChanged += (s, e) => UpdateFilteredCategories();
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateFilteredCategories();
                }
            }
        }

        public ObservableCollection<HomeListViewCategory> FilteredCategories { get; }

        private void UpdateFilteredCategories()
        {
            FilteredCategories.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var category in Categories)
                {
                    FilteredCategories.Add(category);
                }
                return;
            }

            var searchTerms = SearchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var category in Categories)
            {
                var filteredItems = category.Items.Where(item =>
                    searchTerms.All(term =>
                        item.Name.ToLower().Contains(term) ||
                        (item.Presence?.Status?.ToLower().Contains(term) ?? false) ||
                        (item.Presence?.Presence?.ToLower().Contains(term) ?? false)
                    )).ToList();

                if (filteredItems.Any())
                {
                    var filteredCategory = new HomeListViewCategory
                    {
                        Name = category.Name,
                        IsVisibleProperty = category.IsVisibleProperty,
                        Collapsed = category.Collapsed,
                        IsSelected = category.IsSelected
                    };
                    foreach (var item in filteredItems)
                    {
                        filteredCategory.Items.Add(item);
                    }
                    FilteredCategories.Add(filteredCategory);
                }
            }
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
