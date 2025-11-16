using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterListViewModel : ObservableObject
    {
        // Fixed page size for stability in the UI.
        // 10 items requested; we will render exactly 10 per page.
        private const int DEFAULT_FILTERS_PER_PAGE = 10;
        private const double ITEM_HEIGHT = 23.0; // default fallback: 22px button + 1px safety

        private readonly IFilterCacheService _filterCacheService;

        [ObservableProperty]
        private int _filtersPerPage = DEFAULT_FILTERS_PER_PAGE;

        [ObservableProperty]
        private ObservableCollection<FilterListItem> _displayedFilters = new();

        [ObservableProperty]
        private FilterListItem? _selectedFilter;

        [ObservableProperty]
        private string _selectedFilterName = "";

        [ObservableProperty]
        private string _selectedFilterAuthor = "";

        [ObservableProperty]
        private string _selectedFilterDescription = "";

        [ObservableProperty]
        private ObservableCollection<FilterStat> _selectedFilterStats = new();

        [ObservableProperty]
        private bool _isFilterSelected = false;

        [ObservableProperty]
        private int _currentPage = 0;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private string _pageIndicator = "1 / 1";

        [ObservableProperty]
        private bool _canGoToPreviousPage = false;

        [ObservableProperty]
        private bool _canGoToNextPage = false;

        [ObservableProperty]
        private string _selectedTabType = "";

        [ObservableProperty]
        private ObservableCollection<FilterItemViewModel> _filterItems = new();

        [ObservableProperty]
        private ObservableCollection<FilterItemViewModel> _mustHaveItems = new();

        [ObservableProperty]
        private ObservableCollection<FilterItemViewModel> _shouldHaveItems = new();

        [ObservableProperty]
        private ObservableCollection<FilterItemViewModel> _mustNotHaveItems = new();

        [ObservableProperty]
        private bool _hasMustHaveItems = false;

        [ObservableProperty]
        private bool _hasShouldHaveItems = false;

        [ObservableProperty]
        private bool _hasMustNotHaveItems = false;

        [ObservableProperty]
        private bool _hasFilterItems = false;

        [ObservableProperty]
        private bool _showSelectButton = false;

        [ObservableProperty]
        private bool _showActionButtons = true;

        // Dynamic text for the SELECT button in SearchModal
        [ObservableProperty]
        private string _selectButtonText = "Search Seeds with this Filter";

        private List<FilterListItem> _allFilters = new();

        public FilterListViewModel()
        {
            _filterCacheService = ServiceHelper.GetRequiredService<IFilterCacheService>();
            LoadFilters();
        }

        /// <summary>
        /// Sets the context mode for the control (SearchModal vs FiltersModal)
        /// </summary>
        public void SetSearchModalMode(bool isInSearchModal)
        {
            ShowSelectButton = isInSearchModal;
            ShowActionButtons = !isInSearchModal;
            UpdateSelectButtonText();
            DebugLogger.Log(
                "FilterListViewModel",
                $"Mode changed: ShowSelectButton={ShowSelectButton}, ShowActionButtons={ShowActionButtons}"
            );
        }

        public void LoadFilters()
        {
            try
            {
                // Use the FilterCacheService instead of reading files manually
                var allCachedFilters = _filterCacheService.GetAllFilters();

                _allFilters.Clear();
                for (int i = 0; i < allCachedFilters.Count; i++)
                {
                    var cached = allCachedFilters[i];
                    _allFilters.Add(
                        new FilterListItem
                        {
                            Number = i + 1,
                            Name = cached.Name,
                            Author = cached.Author,
                            Description = cached.Description,
                            FilePath = cached.FilePath,
                        }
                    );
                }

                CurrentPage = 0;
                UpdatePage();

                DebugLogger.Log(
                    "FilterListViewModel",
                    $"Loaded {_allFilters.Count} filters from cache"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterListViewModel", $"Error loading filters: {ex.Message}");
            }
        }

        [RelayCommand]
        public void SelectFilter(FilterListItem filter)
        {
            // Clear previous selection
            if (SelectedFilter != null)
            {
                SelectedFilter.IsSelected = false;
            }

            // Set new selection
            SelectedFilter = filter;
            filter.IsSelected = true;
            SelectedFilterName = filter.Name;
            SelectedFilterAuthor = $"by {filter.Author}";
            IsFilterSelected = true;

            // Update select button text to reflect current context
            UpdateSelectButtonText();

            LoadFilterStats(filter.FilePath);

            // Auto-select first non-empty tab to show sprites immediately
            AutoSelectFirstNonEmptyTab(filter.FilePath);
        }

        /// <summary>
        /// Updates the select button text based on context and selection state.
        /// </summary>
        private void UpdateSelectButtonText()
        {
            if (ShowSelectButton)
            {
                SelectButtonText =
                    SelectedFilter != null ? "USE THIS FILTER" : "SEARCH WITH THIS FILTER";
            }
            else
            {
                // Not visible in FiltersModal, but keep value sensible
                SelectButtonText = "LOAD THIS FILTER";
            }
        }

        /// <summary>
        /// Auto-selects the first non-empty tab when a filter is selected
        /// Priority: must_have → should_have → must_not_have
        /// </summary>
        private void AutoSelectFirstNonEmptyTab(string filterPath)
        {
            try
            {
                var config = _filterCacheService.GetFilterByPath(filterPath);
                if (config == null)
                {
                    DebugLogger.Log(
                        "FilterListViewModel",
                        "Cannot auto-select tab - filter not found in cache"
                    );
                    return;
                }

                if (config.Must != null && config.Must.Count > 0)
                {
                    DebugLogger.Log("FilterListViewModel", "Auto-selecting 'must_have' tab");
                    SelectTab("must_have");
                    return;
                }

                if (config.Should != null && config.Should.Count > 0)
                {
                    DebugLogger.Log("FilterListViewModel", "Auto-selecting 'should_have' tab");
                    SelectTab("should_have");
                    return;
                }

                if (config.MustNot != null && config.MustNot.Count > 0)
                {
                    DebugLogger.Log("FilterListViewModel", "Auto-selecting 'must_not_have' tab");
                    SelectTab("must_not_have");
                    return;
                }

                // If all tabs are empty, clear the display
                DebugLogger.Log("FilterListViewModel", "All tabs empty - clearing display");
                SelectedTabType = "";
                FilterItems.Clear();
                HasFilterItems = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterListViewModel",
                    $"Error auto-selecting tab: {ex.Message}"
                );
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages - 1)
            {
                CurrentPage++;
                UpdatePage();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
                UpdatePage();
            }
        }

        private void UpdatePage()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(_allFilters.Count / (double)FiltersPerPage));
            CurrentPage = Math.Clamp(CurrentPage, 0, TotalPages - 1);

            var startIndex = CurrentPage * FiltersPerPage;
            var pageItems = _allFilters.Skip(startIndex).Take(FiltersPerPage).ToList();

            DisplayedFilters.Clear();
            foreach (var item in pageItems)
            {
                DisplayedFilters.Add(item);
            }

            PageIndicator = $"Page {CurrentPage + 1}/{TotalPages}";
            CanGoToPreviousPage = CurrentPage > 0;
            CanGoToNextPage = CurrentPage < TotalPages - 1;
        }

        private void LoadFilterStats(string filterPath)
        {
            try
            {
                SelectedFilterDescription = "";
                var config = _filterCacheService.GetFilterByPath(filterPath);
                if (config == null)
                {
                    DebugLogger.LogError("FilterListViewModel", "Filter not found in cache");
                    return;
                }

                SelectedFilterStats.Clear();

                // Clear all item collections
                MustHaveItems.Clear();
                ShouldHaveItems.Clear();
                MustNotHaveItems.Clear();

                // Description
                if (!string.IsNullOrEmpty(config.Description))
                {
                    SelectedFilterStats.Add(
                        new FilterStat
                        {
                            Label = "Description",
                            Value = config.Description,
                            Color = UIColors.White,
                        }
                    );
                    SelectedFilterDescription = config.Description;
                }

                var spriteService = ServiceHelper.GetService<SpriteService>();

                // Load Must Have items
                if (config.Must != null && config.Must.Count > 0)
                {
                    LoadItemsFromConfig(config.Must, MustHaveItems, spriteService);
                    HasMustHaveItems = MustHaveItems.Count > 0;
                    SelectedFilterStats.Add(
                        new FilterStat
                        {
                            Label = "Must Have",
                            Value = $"{config.Must.Count} items",
                            Color = UIColors.MustHaveColor,
                        }
                    );
                }
                else
                {
                    HasMustHaveItems = false;
                }

                // Load Should Have items
                if (config.Should != null && config.Should.Count > 0)
                {
                    LoadItemsFromConfig(config.Should, ShouldHaveItems, spriteService);
                    HasShouldHaveItems = ShouldHaveItems.Count > 0;
                    SelectedFilterStats.Add(
                        new FilterStat
                        {
                            Label = "Should Have",
                            Value = $"{config.Should.Count} items",
                            Color = UIColors.ShouldHaveColor,
                        }
                    );
                }
                else
                {
                    HasShouldHaveItems = false;
                }

                // Load Must Not Have items
                if (config.MustNot != null && config.MustNot.Count > 0)
                {
                    LoadItemsFromConfig(config.MustNot, MustNotHaveItems, spriteService);
                    HasMustNotHaveItems = MustNotHaveItems.Count > 0;
                    SelectedFilterStats.Add(
                        new FilterStat
                        {
                            Label = "Must Not Have",
                            Value = $"{config.MustNot.Count} items",
                            Color = UIColors.BannedColor,
                        }
                    );
                }
                else
                {
                    HasMustNotHaveItems = false;
                }
            }
            catch (Exception ex)
            {
                var filename = Path.GetFileName(filterPath);
                DebugLogger.LogError(
                    "FilterListViewModel",
                    $"Error loading filter stats from '{filename}': {ex.Message}"
                );
                SelectedFilterDescription = "";
                HasMustHaveItems = false;
                HasShouldHaveItems = false;
                HasMustNotHaveItems = false;
            }
        }

        private void LoadItemsFromConfig(
            List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause> items,
            ObservableCollection<FilterItemViewModel> collection,
            SpriteService? spriteService
        )
        {
            if (items == null || items.Count == 0 || spriteService == null)
                return;

            foreach (var item in items)
            {
                var itemName = item.Value ?? "";
                if (string.IsNullOrEmpty(itemName))
                    continue;

                var sprite = GetItemSprite(itemName, spriteService);
                if (sprite != null)
                {
                    collection.Add(
                        new FilterItemViewModel { ItemName = itemName, ItemImage = sprite }
                    );
                }
            }
        }

        public string? GetSelectedFilterPath() => SelectedFilter?.FilePath;

        /// <summary>
        /// Dynamically compute the number of items per page from available height.
        /// Ensures the last item never gets clipped by using measured row height.
        /// </summary>
        public void UpdateItemsPerPage(double availableHeight, double? measuredItemHeight = null)
        {
            // Determine the visual height of a single row (button + margins + spacing + safety)
            var rowHeight = Math.Max(ITEM_HEIGHT, measuredItemHeight ?? ITEM_HEIGHT);

            if (availableHeight <= 0 || rowHeight <= 0)
                return;

            // No bottom safety needed; container row isolates pagination below
            var effectiveHeight = Math.Max(0, availableHeight);

            var computed = Math.Max(1, (int)Math.Floor(effectiveHeight / rowHeight));

            // Avoid unnecessary updates
            if (computed == FiltersPerPage)
                return;

            // PRESERVE selected filter position when resizing
            var selectedIndex = _allFilters.FindIndex(f => f.IsSelected);
            var oldFiltersPerPage = FiltersPerPage;

            FiltersPerPage = computed;
            DebugLogger.Log(
                "FilterListViewModel",
                $"Dynamic FiltersPerPage set to {FiltersPerPage} (rowHeight={rowHeight:F1}, available={availableHeight:F1})"
            );

            // If a filter was selected, jump to the page containing it
            if (selectedIndex >= 0)
            {
                CurrentPage = selectedIndex / FiltersPerPage;
                DebugLogger.Log(
                    "FilterListViewModel",
                    $"Preserved selected filter at index {selectedIndex}, jumping to page {CurrentPage + 1}"
                );
            }

            UpdatePage();
        }

        [RelayCommand]
        public void SelectTab(string tabType)
        {
            SelectedTabType = tabType;
            LoadFilterItemsForTab(tabType);
        }

        private void LoadFilterItemsForTab(string tabType)
        {
            var filterPath = GetSelectedFilterPath();
            try
            {
                if (string.IsNullOrEmpty(filterPath))
                {
                    DebugLogger.Log("FilterListViewModel", "No filter selected");
                    FilterItems.Clear();
                    HasFilterItems = false;
                    return;
                }

                var config = _filterCacheService.GetFilterByPath(filterPath);
                if (config == null)
                {
                    DebugLogger.Log("FilterListViewModel", "Filter not found in cache");
                    FilterItems.Clear();
                    HasFilterItems = false;
                    return;
                }

                FilterItems.Clear();

                // Get the items list for the selected tab
                List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>? items = tabType switch
                {
                    "must_have" => config.Must,
                    "should_have" => config.Should,
                    "must_not_have" => config.MustNot,
                    _ => null,
                };

                if (items != null && items.Count > 0)
                {
                    var spriteService = ServiceHelper.GetService<SpriteService>();
                    LoadItemsFromConfig(items, FilterItems, spriteService);
                    HasFilterItems = FilterItems.Count > 0;
                }
                else
                {
                    HasFilterItems = false;
                }
            }
            catch (Exception ex)
            {
                var filename = filterPath != null ? Path.GetFileName(filterPath) : "unknown";
                DebugLogger.LogError(
                    "FilterListViewModel",
                    $"Error loading filter items from '{filename}': {ex.Message}"
                );
                FilterItems.Clear();
                HasFilterItems = false;
            }
        }

        private IImage? GetItemSprite(string itemName, SpriteService spriteService)
        {
            // Convert underscores to match sprite names
            var spriteName = itemName.Replace("_", " ");

            // Try different item types
            IImage? sprite = null;

            sprite = spriteService.GetJokerImage(spriteName);
            if (sprite == null)
                sprite = spriteService.GetTarotImage(spriteName);
            if (sprite == null)
                sprite = spriteService.GetItemImage(spriteName, "Planet");
            if (sprite == null)
                sprite = spriteService.GetSpectralImage(spriteName);
            if (sprite == null)
                sprite = spriteService.GetVoucherImage(spriteName);
            if (sprite == null)
                sprite = spriteService.GetTagImage(spriteName);

            return sprite;
        }
    }

    public class FilterItemViewModel
    {
        public string ItemName { get; set; } = "";
        public IImage? ItemImage { get; set; }
    }

    public class FilterStat
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Color { get; set; } = "#FFFFFF";
    }
}
