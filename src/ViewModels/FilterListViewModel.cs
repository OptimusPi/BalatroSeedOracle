using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FilterListViewModel : ObservableObject
    {
        private const int DEFAULT_FILTERS_PER_PAGE = 10;
        private const double ITEM_HEIGHT = 32.0; // 28px button height + 4px margin (2px top + 2px bottom)

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
        private double _triangleOffset = 0;

        [ObservableProperty]
        private bool _isTriangleVisible = false;

        [ObservableProperty]
        private bool _hasFilterItems = false;

        [ObservableProperty]
        private bool _showSelectButton = false;

        [ObservableProperty]
        private bool _showActionButtons = true;

        private List<FilterListItem> _allFilters = new();

        public FilterListViewModel()
        {
            LoadFilters();
        }

        /// <summary>
        /// Sets the context mode for the control (SearchModal vs FiltersModal)
        /// </summary>
        public void SetSearchModalMode(bool isInSearchModal)
        {
            ShowSelectButton = isInSearchModal;
            ShowActionButtons = !isInSearchModal;
            DebugLogger.Log("FilterListViewModel", $"Mode changed: ShowSelectButton={ShowSelectButton}, ShowActionButtons={ShowActionButtons}");
        }

        public void LoadFilters()
        {
            try
            {
                // FIXED: Use the actual Motely filter directory
                var filtersDir = Path.Combine(Directory.GetCurrentDirectory(), "external", "Motely", "Motely", "JsonItemFilters");
                if (!Directory.Exists(filtersDir))
                {
                    DebugLogger.LogError("FilterListViewModel", $"Filters directory not found: {filtersDir}");
                    return;
                }

                var filterFiles = Directory.GetFiles(filtersDir, "*.json")
                    .OrderBy(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();

                _allFilters.Clear();
                for (int i = 0; i < filterFiles.Count; i++)
                {
                    var filterPath = filterFiles[i];
                    var filterName = Path.GetFileNameWithoutExtension(filterPath);
                    var author = GetFilterAuthor(filterPath);

                    _allFilters.Add(new FilterListItem
                    {
                        Number = i + 1,
                        Name = filterName,
                        Author = author,
                        FilePath = filterPath
                    });
                }

                CurrentPage = 0;
                UpdatePage();

                DebugLogger.Log("FilterListViewModel", $"Loaded {_allFilters.Count} filters");
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

            LoadFilterStats(filter.FilePath);
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

            PageIndicator = $"{CurrentPage + 1} / {TotalPages}";
            CanGoToPreviousPage = CurrentPage > 0;
            CanGoToNextPage = CurrentPage < TotalPages - 1;
        }

        private void LoadFilterStats(string filterPath)
        {
            try
            {
                var json = File.ReadAllText(filterPath);
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                using var doc = JsonDocument.Parse(json, options);
                var root = doc.RootElement;

                SelectedFilterStats.Clear();

                if (root.TryGetProperty("description", out var descProp))
                    SelectedFilterStats.Add(new FilterStat { Label = "Description", Value = descProp.GetString() ?? "N/A", Color = "#FFFFFF" });

                if (root.TryGetProperty("must_have", out var mustProp) && mustProp.ValueKind == JsonValueKind.Array)
                    SelectedFilterStats.Add(new FilterStat { Label = "Must Have", Value = $"{mustProp.GetArrayLength()} items", Color = "#ff4c40" });

                if (root.TryGetProperty("should_have", out var shouldProp) && shouldProp.ValueKind == JsonValueKind.Array)
                    SelectedFilterStats.Add(new FilterStat { Label = "Should Have", Value = $"{shouldProp.GetArrayLength()} items", Color = "#0093ff" });

                if (root.TryGetProperty("must_not_have", out var mustNotProp) && mustNotProp.ValueKind == JsonValueKind.Array)
                    SelectedFilterStats.Add(new FilterStat { Label = "Must Not Have", Value = $"{mustNotProp.GetArrayLength()} items", Color = "#ff9800" });

                if (root.TryGetProperty("seed_count", out var seedCountProp))
                    SelectedFilterStats.Add(new FilterStat { Label = "Target Seeds", Value = seedCountProp.GetInt32().ToString(), Color = "#ffd700" });
            }
            catch (Exception ex)
            {
                var filename = Path.GetFileName(filterPath);
                DebugLogger.LogError("FilterListViewModel", $"Error loading filter stats from '{filename}': {ex.Message}");
            }
        }

        private string GetFilterAuthor(string filterPath)
        {
            try
            {
                var json = File.ReadAllText(filterPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("author", out var authorProp))
                {
                    return authorProp.GetString() ?? "Unknown";
                }
            }
            catch { }
            return "Unknown";
        }

        public string? GetSelectedFilterPath() => SelectedFilter?.FilePath;

        /// <summary>
        /// Recalculates the number of items per page based on available container height
        /// </summary>
        public void UpdateItemsPerPage(double availableHeight)
        {
            if (availableHeight <= 0)
            {
                FiltersPerPage = DEFAULT_FILTERS_PER_PAGE;
                return;
            }

            // Account for Border padding (4px total: 2px top + 2px bottom)
            const double borderPadding = 4.0;
            var usableHeight = availableHeight - borderPadding;

            // Calculate how many items can fit (minimum 3, maximum 20)
            var calculatedItems = Math.Floor(usableHeight / ITEM_HEIGHT);
            var newPageSize = Math.Clamp((int)calculatedItems, 3, 20);

            // PREVENT INFINITE LOOP: Only update if page size actually changed!
            if (newPageSize == FiltersPerPage)
                return;

            FiltersPerPage = newPageSize;
            DebugLogger.Log("FilterListViewModel", $"Updated FiltersPerPage to {FiltersPerPage} (container: {availableHeight:F0}px, usable: {usableHeight:F0}px)");

            // Refresh the current page with new page size
            UpdatePage();
        }

        [RelayCommand]
        public void SelectTab(string tabType)
        {
            SelectedTabType = tabType;
            LoadFilterItemsForTab(tabType);
            UpdateTrianglePosition(tabType);
        }

        private void UpdateTrianglePosition(string tabType)
        {
            IsTriangleVisible = !string.IsNullOrEmpty(tabType);

            // Calculate offset based on tab type
            // These values match the original hardcoded positions
            TriangleOffset = tabType switch
            {
                "must_have" => -113,     // Left tab
                "should_have" => 0,      // Middle tab (centered)
                "must_not_have" => 105,  // Right tab
                _ => 0
            };
        }

        private void LoadFilterItemsForTab(string tabType)
        {
            var filterPath = GetSelectedFilterPath(); // Declare outside try for catch block access
            try
            {
                if (string.IsNullOrEmpty(filterPath) || !File.Exists(filterPath))
                {
                    DebugLogger.Log("FilterListViewModel", "No filter selected or filter file not found");
                    FilterItems.Clear();
                    HasFilterItems = false;
                    return;
                }

                var json = File.ReadAllText(filterPath);
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                using var doc = JsonDocument.Parse(json, options);
                var root = doc.RootElement;

                FilterItems.Clear();

                // Get the items array for the selected tab
                if (root.TryGetProperty(tabType, out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<string>();
                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            items.Add(item.GetString() ?? "");
                        }
                        else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("item", out var itemProp))
                        {
                            items.Add(itemProp.GetString() ?? "");
                        }
                    }

                    // Create FilterItemViewModels with loaded images
                    if (items.Count > 0)
                    {
                        var spriteService = ServiceHelper.GetService<SpriteService>();
                        if (spriteService != null)
                        {
                            foreach (var itemName in items)
                            {
                                var sprite = GetItemSprite(itemName, spriteService);
                                if (sprite != null)
                                {
                                    FilterItems.Add(new FilterItemViewModel
                                    {
                                        ItemName = itemName,
                                        ItemImage = sprite
                                    });
                                }
                            }
                        }

                        HasFilterItems = FilterItems.Count > 0;
                    }
                    else
                    {
                        HasFilterItems = false;
                    }
                }
                else
                {
                    HasFilterItems = false;
                }
            }
            catch (Exception ex)
            {
                var filename = filterPath != null ? Path.GetFileName(filterPath) : "unknown";
                DebugLogger.LogError("FilterListViewModel", $"Error loading filter items from '{filename}': {ex.Message}");
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
            if (sprite == null) sprite = spriteService.GetTarotImage(spriteName);
            if (sprite == null) sprite = spriteService.GetItemImage(spriteName, "Planet");
            if (sprite == null) sprite = spriteService.GetSpectralImage(spriteName);
            if (sprite == null) sprite = spriteService.GetVoucherImage(spriteName);
            if (sprite == null) sprite = spriteService.GetTagImage(spriteName);

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
