using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// Base class for all filter tab ViewModels.
    /// Provides shared functionality for data loading, filtering, grouping, and auto-save.
    /// Eliminates ~1,200 lines of duplicate code across 3 ViewModels.
    /// </summary>
    public abstract partial class FilterTabViewModelBase : ObservableObject
    {
        protected readonly FiltersModalViewModel? _parentViewModel;
        protected readonly IFilterItemDataService _dataService;
        protected readonly IFilterItemFilterService _filterService;

        // Auto-save debouncing
        protected CancellationTokenSource? _autoSaveCts;
        protected const int AutoSaveDebounceMs = 500;

        #region Shared Observable Properties

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private string _selectedMainCategory = "Joker";

        [ObservableProperty]
        private bool _isFavoritesCategorySelected = false;

        [ObservableProperty]
        private bool _isJokerCategorySelected = true;

        [ObservableProperty]
        private bool _isConsumableCategorySelected = false;

        [ObservableProperty]
        private bool _isSkipTagCategorySelected = false;

        [ObservableProperty]
        private bool _isBossCategorySelected = false;

        [ObservableProperty]
        private bool _isVoucherCategorySelected = false;

        [ObservableProperty]
        private bool _isStandardCardCategorySelected = false;

        [ObservableProperty]
        private ObservableCollection<FilterItemGroup> _groupedItems = new();

        #endregion

        #region Shared Collections (All + Filtered)

        public ObservableCollection<FilterItem> AllJokers { get; }
        public ObservableCollection<FilterItem> AllTags { get; }
        public ObservableCollection<FilterItem> AllVouchers { get; }
        public ObservableCollection<FilterItem> AllTarots { get; }
        public ObservableCollection<FilterItem> AllPlanets { get; }
        public ObservableCollection<FilterItem> AllSpectrals { get; }
        public ObservableCollection<FilterItem> AllBosses { get; }
        public ObservableCollection<FilterItem> AllWildcards { get; }
        public ObservableCollection<FilterItem> AllStandardCards { get; }

        public ObservableCollection<FilterItem> FilteredJokers { get; }
        public ObservableCollection<FilterItem> FilteredTags { get; }
        public ObservableCollection<FilterItem> FilteredVouchers { get; }
        public ObservableCollection<FilterItem> FilteredTarots { get; }
        public ObservableCollection<FilterItem> FilteredPlanets { get; }
        public ObservableCollection<FilterItem> FilteredSpectrals { get; }
        public ObservableCollection<FilterItem> FilteredBosses { get; }
        public ObservableCollection<FilterItem> FilteredWildcards { get; }
        public ObservableCollection<FilterItem> FilteredStandardCards { get; }

        public ObservableCollection<FilterItem> FilteredItems { get; }

        #endregion

        #region Parent Integration

        /// <summary>
        /// Expose parent's FilterName for display
        /// </summary>
        public string FilterName => _parentViewModel?.FilterName ?? "New Filter";

        #endregion

        #region Constructor

        protected FilterTabViewModelBase(
            FiltersModalViewModel? parentViewModel,
            IFilterItemDataService dataService,
            IFilterItemFilterService filterService)
        {
            _parentViewModel = parentViewModel;
            _dataService = dataService;
            _filterService = filterService;

            // Subscribe to parent's property changes
            if (_parentViewModel != null)
            {
                _parentViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FiltersModalViewModel.FilterName))
                    {
                        OnPropertyChanged(nameof(FilterName));
                    }
                };
            }

            // Initialize All* collections
            AllJokers = new ObservableCollection<FilterItem>();
            AllTags = new ObservableCollection<FilterItem>();
            AllVouchers = new ObservableCollection<FilterItem>();
            AllTarots = new ObservableCollection<FilterItem>();
            AllPlanets = new ObservableCollection<FilterItem>();
            AllSpectrals = new ObservableCollection<FilterItem>();
            AllBosses = new ObservableCollection<FilterItem>();
            AllWildcards = new ObservableCollection<FilterItem>();
            AllStandardCards = new ObservableCollection<FilterItem>();

            // Initialize Filtered* collections
            FilteredJokers = new ObservableCollection<FilterItem>();
            FilteredTags = new ObservableCollection<FilterItem>();
            FilteredVouchers = new ObservableCollection<FilterItem>();
            FilteredTarots = new ObservableCollection<FilterItem>();
            FilteredPlanets = new ObservableCollection<FilterItem>();
            FilteredSpectrals = new ObservableCollection<FilterItem>();
            FilteredBosses = new ObservableCollection<FilterItem>();
            FilteredWildcards = new ObservableCollection<FilterItem>();
            FilteredStandardCards = new ObservableCollection<FilterItem>();

            FilteredItems = new ObservableCollection<FilterItem>();
            GroupedItems = new ObservableCollection<FilterItemGroup>();

            // Subscribe to SearchFilter property changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchFilter))
                {
                    ApplyFilter();
                }
            };

            // Initialize data asynchronously
            _ = Task.Run(LoadGameDataAsync);
        }

        #endregion

        #region Data Loading (virtual for override)

        /// <summary>
        /// Loads game data asynchronously and updates UI on completion
        /// </summary>
        protected virtual async Task LoadGameDataAsync()
        {
            try
            {
                await Task.Run(() => LoadGameData());

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ApplyFilter();
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterTabBase", $"Error loading data: {ex.Message}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        /// <summary>
        /// Loads game data using the FilterItemDataService
        /// </summary>
        protected virtual void LoadGameData()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(LoadGameData);
                return;
            }

            try
            {
                var collections = new Services.FilterItemCollections
                {
                    AllJokers = AllJokers,
                    AllTags = AllTags,
                    AllVouchers = AllVouchers,
                    AllTarots = AllTarots,
                    AllPlanets = AllPlanets,
                    AllSpectrals = AllSpectrals,
                    AllBosses = AllBosses,
                    AllWildcards = AllWildcards,
                    AllStandardCards = AllStandardCards
                };

                // Use service to load all game data
                _dataService.LoadGameData(collections);

                DebugLogger.Log("FilterTabBase",
                    $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterTabBase", $"Error loading game data: {ex.Message}");
            }

            ApplyFilter();
        }

        #endregion

        #region Filtering (virtual for override)

        /// <summary>
        /// Applies search filter to all collections using FilterItemFilterService
        /// </summary>
        protected virtual void ApplyFilter()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(ApplyFilter);
                return;
            }

            var allCollections = new Services.FilterItemCollections
            {
                AllJokers = AllJokers,
                AllTags = AllTags,
                AllVouchers = AllVouchers,
                AllTarots = AllTarots,
                AllPlanets = AllPlanets,
                AllSpectrals = AllSpectrals,
                AllBosses = AllBosses,
                AllWildcards = AllWildcards,
                AllStandardCards = AllStandardCards
            };

            var filteredCollections = new Services.FilterItemCollections
            {
                AllJokers = FilteredJokers,
                AllTags = FilteredTags,
                AllVouchers = FilteredVouchers,
                AllTarots = FilteredTarots,
                AllPlanets = FilteredPlanets,
                AllSpectrals = FilteredSpectrals,
                AllBosses = FilteredBosses,
                AllWildcards = FilteredWildcards,
                AllStandardCards = FilteredStandardCards
            };

            // Use service to apply filter
            _filterService.ApplyFilterToAll(SearchFilter, allCollections, filteredCollections);

            // Rebuild grouped items after filtering
            RebuildGroupedItems();
        }

        #endregion

        #region Grouping (virtual for override)

        /// <summary>
        /// Sets the main category and updates visibility flags
        /// </summary>
        public virtual void SetCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return;
            }

            SearchFilter = "";
            SelectedMainCategory = category;

            IsFavoritesCategorySelected = category == "Favorites";
            IsJokerCategorySelected = category == "Joker";
            IsConsumableCategorySelected = category == "Consumable";
            IsSkipTagCategorySelected = category == "SkipTag";
            IsBossCategorySelected = category == "Boss";
            IsVoucherCategorySelected = category == "Voucher";
            IsStandardCardCategorySelected = category == "StandardCard";

            RebuildGroupedItems();

            if (FilteredJokers.Count == 0)
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// Rebuilds grouped items based on selected category
        /// Virtual to allow tab-specific customization
        /// </summary>
        protected virtual void RebuildGroupedItems()
        {
            GroupedItems.Clear();

            switch (SelectedMainCategory)
            {
                case "Favorites":
                    var favoriteItems = AllJokers.Where(j => j.IsFavorite == true).ToList();
                    AddGroup("Favorite Items", favoriteItems);
                    AddGroup("Wildcards", FilteredWildcards);
                    break;

                case "Joker":
                    AddGroup("Legendary Jokers", FilteredJokers.Where(j => j.Type == "SoulJoker"));
                    AddGroup("Rare Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare"));
                    AddGroup("Uncommon Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon"));
                    AddGroup("Common Jokers", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common"));
                    break;

                case "Consumable":
                    AddGroup("Tarot Cards", FilteredTarots);
                    AddGroup("Planet Cards", FilteredPlanets);
                    AddGroup("Spectral Cards", FilteredSpectrals);
                    break;

                case "SkipTag":
                    AddGroup("Skip Tags - Any Ante", FilteredTags);
                    break;

                case "Boss":
                    AddGroup("Boss Blinds", FilteredBosses);
                    break;

                case "Voucher":
                    var voucherPairs = VoucherHelper.GetVoucherPairs();
                    var organizedVouchers = new List<FilterItem>();

                    var firstSet = voucherPairs.Take(8).ToList();
                    foreach (var (baseName, _) in firstSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null) organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in firstSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null) organizedVouchers.Add(upgradeVoucher);
                    }

                    var secondSet = voucherPairs.Skip(8).Take(8).ToList();
                    foreach (var (baseName, _) in secondSet)
                    {
                        var baseVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        if (baseVoucher != null) organizedVouchers.Add(baseVoucher);
                    }
                    foreach (var (_, upgradeName) in secondSet)
                    {
                        var upgradeVoucher = FilteredVouchers.FirstOrDefault(v => v.Name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase));
                        if (upgradeVoucher != null) organizedVouchers.Add(upgradeVoucher);
                    }

                    var remainingVouchers = FilteredVouchers.Except(organizedVouchers);
                    organizedVouchers.AddRange(remainingVouchers);
                    AddGroup("Vouchers", organizedVouchers);
                    break;

                case "StandardCard":
                    AddGroup("Hearts", FilteredStandardCards.Where(c => c.Category == "Hearts"));
                    AddGroup("Spades", FilteredStandardCards.Where(c => c.Category == "Spades"));
                    AddGroup("Diamonds", FilteredStandardCards.Where(c => c.Category == "Diamonds"));
                    AddGroup("Clubs", FilteredStandardCards.Where(c => c.Category == "Clubs"));
                    AddGroup("Mult Cards", FilteredStandardCards.Where(c => c.Category == "Mult"));
                    AddGroup("Bonus Cards", FilteredStandardCards.Where(c => c.Category == "Bonus"));
                    AddGroup("Glass Cards", FilteredStandardCards.Where(c => c.Category == "Glass"));
                    AddGroup("Gold Cards", FilteredStandardCards.Where(c => c.Category == "Gold"));
                    AddGroup("Steel Cards", FilteredStandardCards.Where(c => c.Category == "Steel"));
                    AddGroup("Stone Card", FilteredStandardCards.Where(c => c.Category == "Stone"));
                    break;
            }
        }

        /// <summary>
        /// Helper to add a group to GroupedItems
        /// </summary>
        protected void AddGroup(string groupName, IEnumerable<FilterItem> items)
        {
            var group = new FilterItemGroup
            {
                GroupName = groupName,
                Items = new ObservableCollection<FilterItem>(items)
            };
            GroupedItems.Add(group);
        }

        #endregion

        #region Auto-Save (virtual for override)

        /// <summary>
        /// Triggers debounced auto-save
        /// </summary>
        public virtual void TriggerAutoSave()
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = new CancellationTokenSource();

            var token = _autoSaveCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AutoSaveDebounceMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        await PerformAutoSave();
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FilterTabBase", $"Auto-save error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Performs auto-save operation
        /// Virtual to allow tab-specific customization
        /// </summary>
        protected virtual async Task PerformAutoSave()
        {
            if (_parentViewModel == null) return;

            try
            {
                var filterName = _parentViewModel.FilterName;
                if (string.IsNullOrWhiteSpace(filterName)) return;

                var config = _parentViewModel.BuildConfigFromCurrentState();
                var configService = ServiceHelper.GetService<IConfigurationService>();
                if (configService == null) return;

                var filePath = System.IO.Path.Combine(configService.GetFiltersDirectory(), $"{filterName.Replace(" ", "_")}.json");
                await configService.SaveFilterAsync(filePath, config);

                DebugLogger.Log("FilterTabBase", $"Auto-saved filter: {filterName}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterTabBase", $"Auto-save exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles collection changes and triggers auto-save
        /// </summary>
        protected virtual void OnZoneCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            TriggerAutoSave();
        }

        #endregion

        [RelayCommand]
        private void SelectCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return;
            }

            SetCategory(category);
        }
    }
}
