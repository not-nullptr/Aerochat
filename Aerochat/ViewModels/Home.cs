using Aerochat.Theme;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using static Aerochat.ViewModels.HomeListViewCategory;

namespace Aerochat.ViewModels
{
    public class HomeWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _searchDebounceTimer;
        private const int DEBOUNCE_INTERVAL_MS = 150;

        public HomeWindowViewModel()
        {
            Notices.CollectionChanged += (_, _) => InvokePropertyChanged(nameof(CurrentNotice));
            // Initialize filtered categories
            FilteredCategories = new ObservableCollection<HomeListViewCategory>();
            Categories.CollectionChanged += (s, e) => UpdateFilteredCategories();

            // Initialize debounce timer
            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DEBOUNCE_INTERVAL_MS)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                UpdateFilteredCategories();
            };
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Reset and restart the debounce timer
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Start();
                }
            }
        }

        public ObservableCollection<HomeListViewCategory> FilteredCategories { get; }

        private void UpdateFilteredCategories()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Fast path for empty search - just reference existing categories
                FilteredCategories.Clear();
                foreach (var category in Categories)
                {
                    FilteredCategories.Add(category);
                }
                return;
            }

            var searchTerms = SearchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (searchTerms.Length == 0)
            {
                return;
            }

            FilteredCategories.Clear();
            var reusableCategory = new HomeListViewCategory();

            foreach (var category in Categories)
            {
                // Reuse the same category object and just update its properties
                reusableCategory.Name = category.Name;
                reusableCategory.IsVisibleProperty = category.IsVisibleProperty;
                reusableCategory.Collapsed = category.Collapsed;
                reusableCategory.IsSelected = category.IsSelected;
                reusableCategory.Items.Clear();

                bool hasMatchingItems = false;
                foreach (var item in category.Items)
                {
                    if (MatchesSearchTerms(item, searchTerms))
                    {
                        reusableCategory.Items.Add(item);
                        hasMatchingItems = true;
                    }
                }

                if (hasMatchingItems)
                {
                    // Only create a new category if matches are found
                    var newCategory = new HomeListViewCategory
                    {
                        Name = reusableCategory.Name,
                        IsVisibleProperty = reusableCategory.IsVisibleProperty,
                        Collapsed = reusableCategory.Collapsed,
                        IsSelected = reusableCategory.IsSelected
                    };
                    foreach (var item in reusableCategory.Items)
                    {
                        newCategory.Items.Add(item);
                    }
                    FilteredCategories.Add(newCategory);
                }
            }
        }

        private static bool MatchesSearchTerms(HomeListItemViewModel item, string[] searchTerms)
        {
            var name = item.Name?.ToLower() ?? "";
            var status = item.Presence?.Status?.ToLower() ?? "";
            var presence = item.Presence?.Presence?.ToLower() ?? "";

            return searchTerms.All(term =>
                name.Contains(term) ||
                status.Contains(term) ||
                presence.Contains(term));
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
