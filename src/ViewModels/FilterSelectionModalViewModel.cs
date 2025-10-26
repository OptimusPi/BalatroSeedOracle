using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterSelectionModalViewModel : ObservableObject
    {
        // Configuration passed in constructor
        public bool EnableSearch { get; }
        public bool EnableEdit { get; }
        public bool EnableCopy { get; }
        public bool EnableDelete { get; }
        public bool EnableAnalyze { get; }

        // Child ViewModel for paginated filter list
        public PaginatedFilterBrowserViewModel FilterList { get; }

        // Tab navigation for details panel
        public BalatroTabControlViewModel TabControl { get; }

        // Selected filter details
        [ObservableProperty]
        private FilterBrowserItem? _selectedFilter;

        // Active tab index
        [ObservableProperty]
        private int _activeTabIndex = 0;

        // Details panel visibility
        public bool ShowDetailsPanel => SelectedFilter != null;
        public bool ShowPlaceholder => SelectedFilter == null;

        // Tab content visibility
        public bool ShowFilterTab => ActiveTabIndex == 0 && ShowDetailsPanel;
        public bool ShowScoreTab => ActiveTabIndex == 1 && ShowDetailsPanel;
        public bool ShowDeckTab => ActiveTabIndex == 2 && ShowDetailsPanel;

        // Filter details for display
        public string FilterName => SelectedFilter?.Name ?? "";
        public string FilterAuthor => SelectedFilter?.Author ?? "";
        public string FilterDescription => SelectedFilter?.Description ?? "";
        public string CreatedDate => SelectedFilter?.DateCreated.ToString("MMM yyyy") ?? "";

        // Item counts for preview
        public int MustHaveCount => SelectedFilter?.MustCount ?? 0;
        public int ShouldHaveCount => SelectedFilter?.ShouldCount ?? 0;
        public int MustNotCount => SelectedFilter?.MustNotCount ?? 0;

        // Placeholder text when nothing selected
        public string PlaceholderText => EnableEdit || EnableCopy
            ? "Please select a filter or CREATE NEW"
            : "Please select a filter";

        // Result to return when modal closes
        public FilterSelectionResult Result { get; private set; } = new() { Cancelled = true };

        // Events
        public event EventHandler? ModalCloseRequested;

        public FilterSelectionModalViewModel(
            bool enableSearch = false,
            bool enableEdit = false,
            bool enableCopy = false,
            bool enableDelete = false,
            bool enableAnalyze = false)
        {
            EnableSearch = enableSearch;
            EnableEdit = enableEdit;
            EnableCopy = enableCopy;
            EnableDelete = enableDelete;
            EnableAnalyze = enableAnalyze;

            // Create child ViewModel for filter list
            FilterList = new PaginatedFilterBrowserViewModel();

            // Subscribe to filter selection changes
            FilterList.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterList.SelectedFilter))
                {
                    SelectedFilter = FilterList.SelectedFilter;
                }
            };

            // Initialize tab control with three tabs
            TabControl = new BalatroTabControlViewModel
            {
                Tabs = new ObservableCollection<BalatroTabItem>
                {
                    new BalatroTabItem { Title = "Filter Info", Index = 0, IsActive = true },
                    new BalatroTabItem { Title = "Score Setup", Index = 1 },
                    new BalatroTabItem { Title = "Preferred Deck", Index = 2 }
                }
            };

            // Subscribe to tab changes
            TabControl.TabChanged += (s, tabIndex) =>
            {
                ActiveTabIndex = tabIndex;
            };
        }

        partial void OnSelectedFilterChanged(FilterBrowserItem? value)
        {
            OnPropertyChanged(nameof(ShowDetailsPanel));
            OnPropertyChanged(nameof(ShowPlaceholder));
            OnPropertyChanged(nameof(FilterName));
            OnPropertyChanged(nameof(FilterAuthor));
            OnPropertyChanged(nameof(FilterDescription));
            OnPropertyChanged(nameof(CreatedDate));
            OnPropertyChanged(nameof(MustHaveCount));
            OnPropertyChanged(nameof(ShouldHaveCount));
            OnPropertyChanged(nameof(MustNotCount));
            OnPropertyChanged(nameof(ShowFilterTab));
            OnPropertyChanged(nameof(ShowScoreTab));
            OnPropertyChanged(nameof(ShowDeckTab));
        }

        partial void OnActiveTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(ShowFilterTab));
            OnPropertyChanged(nameof(ShowScoreTab));
            OnPropertyChanged(nameof(ShowDeckTab));
        }

        [RelayCommand]
        private void Search()
        {
            if (SelectedFilter == null) return;

            if (SelectedFilter.IsCreateNew)
            {
                // Cannot search with blank filter
                DebugLogger.Log("FilterSelectionModal", "Cannot search with CREATE NEW filter");
                return;
            }

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Search,
                FilterId = SelectedFilter.FilterId
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Edit()
        {
            if (SelectedFilter == null) return;

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = SelectedFilter.IsCreateNew ? FilterAction.CreateNew : FilterAction.Edit,
                FilterId = SelectedFilter.IsCreateNew ? null : SelectedFilter.FilterId
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void CreateNew()
        {
            // Create new filter directly (called from placeholder button)
            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.CreateNew,
                FilterId = null
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Copy()
        {
            if (SelectedFilter == null || SelectedFilter.IsCreateNew) return;

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Copy,
                FilterId = SelectedFilter.FilterId
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedFilter == null || SelectedFilter.IsCreateNew) return;

            // TODO: Show confirmation dialog
            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Delete,
                FilterId = SelectedFilter.FilterId
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Analyze()
        {
            if (SelectedFilter == null) return;

            if (SelectedFilter.IsCreateNew)
            {
                // Cannot analyze blank filter
                DebugLogger.Log("FilterSelectionModal", "Cannot analyze CREATE NEW filter");
                return;
            }

            Result = new FilterSelectionResult
            {
                Cancelled = false,
                Action = FilterAction.Analyze,
                FilterId = SelectedFilter.FilterId
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Back()
        {
            Result = new FilterSelectionResult
            {
                Cancelled = true,
                Action = FilterAction.Cancelled
            };

            ModalCloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
