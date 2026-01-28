using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class PaginatedFilterBrowserViewModel : ObservableObject
    {
        private const int ITEMS_PER_PAGE = 10;

        private readonly List<FilterBrowserItem> _allFilters = [];

        [ObservableProperty]
        private FilterBrowserItem? _selectedFilter;

        private int _currentPage = 0;

        [ObservableProperty]
        private string _mainButtonText = "Select";

        [ObservableProperty]
        private string _secondaryButtonText = "View";

        [ObservableProperty]
        private bool _showSecondaryButton = true;

        [ObservableProperty]
        private bool _showDeleteButton = true;

        // Properties
        public ObservableCollection<FilterBrowserItemViewModel> CurrentPageFilters { get; } = new();

        public bool HasSelectedFilter => SelectedFilter != null;

        public string PageIndicatorText => $"Page {_currentPage + 1}/{TotalPages}";
        public string StatusText => $"Total {_allFilters.Count} filters";

        private int TotalPages => (int)Math.Ceiling((double)_allFilters.Count / ITEMS_PER_PAGE);

        // Commands that parent ViewModels will set (using ICommand for Avalonia property compatibility)
        public ICommand MainButtonCommand { get; set; } = null!;
        public ICommand SecondaryButtonCommand { get; set; } = null!;
        public ICommand DeleteCommand { get; set; } = null!;

        // Events
        public event EventHandler<FilterBrowserItem>? FilterSelected;

        public PaginatedFilterBrowserViewModel()
        {
            LoadFilters();
        }

        partial void OnSelectedFilterChanged(FilterBrowserItem? value)
        {
            OnPropertyChanged(nameof(HasSelectedFilter));
            UpdateCurrentPageSelection();
        }

        [RelayCommand]
        private async Task SelectFilter(FilterBrowserItemViewModel? filterViewModel)
        {
            if (filterViewModel?.FilterBrowserItem != null)
            {
                // Handle CREATE NEW FILTER specially
                if (filterViewModel.FilterBrowserItem.IsCreateNew)
                {
                    try
                    {
                        var tempPath = await CreateTempFilter();
                        var tempFilter = LoadFilterBrowserItem(tempPath);
                        if (tempFilter != null)
                        {
                            SelectedFilter = tempFilter;
                            FilterSelected?.Invoke(this, tempFilter);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "PaginatedFilterBrowserViewModel",
                            $"Failed to create temp filter: {ex.Message}"
                        );
                    }
                }
                else
                {
                    SelectedFilter = filterViewModel.FilterBrowserItem;
                    FilterSelected?.Invoke(this, filterViewModel.FilterBrowserItem);
                }
            }
        }

        private async System.Threading.Tasks.Task<string> CreateTempFilter()
        {
            var filtersDir = AppPaths.FiltersDir;

            var tempPath = System.IO.Path.Combine(filtersDir, "_UNSAVED_CREATION.json");

            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.MotelyJsonConfig
            {
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author =
                    ServiceHelper.GetService<Services.UserProfileService>()?.GetAuthorName()
                    ?? "Unknown",
                DateCreated = System.DateTime.UtcNow,
                Must =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotelyJsonFilterClause>(),
                Should =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotelyJsonFilterClause>(),
                MustNot =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotelyJsonFilterClause>(),
            };

            var json = System.Text.Json.JsonSerializer.Serialize(
                emptyFilter,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
            await System.IO.File.WriteAllTextAsync(tempPath, json);

            return tempPath;
        }

        [RelayCommand(CanExecute = nameof(CanPreviousPage))]
        private void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdateCurrentPage();
            }
        }

        private bool CanPreviousPage() => _currentPage > 0;

        [RelayCommand(CanExecute = nameof(CanNextPage))]
        private void NextPage()
        {
            if (_currentPage < TotalPages - 1)
            {
                _currentPage++;
                UpdateCurrentPage();
            }
        }

        private bool CanNextPage() => _currentPage < TotalPages - 1;

        private void LoadFilters()
        {
            try
            {
                _allFilters.Clear();

                // Try to use the cache service first for performance
                var filterCache = ServiceHelper.GetService<Services.IFilterCacheService>();
                if (filterCache != null)
                {
                    DebugLogger.Log(
                        "PaginatedFilterBrowserViewModel",
                        $"Loading filters from cache ({filterCache.Count} cached)"
                    );

                    var cachedFilters = filterCache.GetAllFilters();
                    foreach (var cached in cachedFilters)
                    {
                        var filterItem = ConvertCachedFilterToBrowserItem(cached);
                        if (filterItem != null)
                        {
                            _allFilters.Add(filterItem);
                        }
                    }

                    UpdateCurrentPage();
                    return;
                }

                // Fallback to disk loading if cache not available
                DebugLogger.Log(
                    "PaginatedFilterBrowserViewModel",
                    "Cache service not available, loading from disk"
                );

                var filtersDir = AppPaths.FiltersDir;
                if (!Directory.Exists(filtersDir))
                {
                    UpdateCurrentPage();
                    return;
                }

                var filterFiles = Directory
                    .GetFiles(filtersDir, "*.json")
                    .Where(f => Path.GetFileName(f) != "_UNSAVED_CREATION.json") // Skip temp files
                    .OrderByDescending(File.GetLastWriteTime)
                    .ToList();

                foreach (var filePath in filterFiles)
                {
                    var filterItem = LoadFilterBrowserItem(filePath);
                    if (filterItem != null)
                    {
                        _allFilters.Add(filterItem);
                    }
                }

                UpdateCurrentPage();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "PaginatedFilterBrowserViewModel",
                    $"Error loading filters: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Converts a cached filter to a FilterBrowserItem for display.
        /// This is much faster than parsing from disk since the config is already in memory.
        /// </summary>
        private FilterBrowserItem? ConvertCachedFilterToBrowserItem(
            Services.CachedFilter cachedFilter
        )
        {
            try
            {
                var config = cachedFilter.Config;
                if (config == null || string.IsNullOrEmpty(config.Name))
                    return null;

                var item = new FilterBrowserItem
                {
                    Name = config.Name,
                    Description = config.Description ?? "",
                    Author = config.Author ?? "Unknown",
                    DateCreated = config.DateCreated ?? cachedFilter.LastModified,
                    FilePath = cachedFilter.FilePath,
                    MustCount = config.Must?.Count ?? 0,
                    ShouldCount = config.Should?.Count ?? 0,
                    MustNotCount = config.MustNot?.Count ?? 0,
                    DeckName = config.Deck ?? "Red",
                    StakeName = config.Stake ?? "White",
                };

                // Parse Must items
                if (config.Must != null)
                {
                    item.Must = ParseItemCollections(config.Must);
                }

                // Parse Should items
                if (config.Should != null)
                {
                    item.Should = ParseItemCollections(config.Should);
                }

                // Parse MustNot items
                if (config.MustNot != null)
                {
                    item.MustNot = ParseItemCollections(config.MustNot);
                }

                return item;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "PaginatedFilterBrowserViewModel",
                    $"Error converting cached filter {cachedFilter.FilterId}: {ex.Message}"
                );
                return null;
            }
        }

        private FilterBrowserItem? LoadFilterBrowserItem(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content))
                    return null;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };

                var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                    content,
                    options
                );
                if (config == null || string.IsNullOrEmpty(config.Name))
                    return null;

                var item = new FilterBrowserItem
                {
                    Name = config.Name,
                    Description = config.Description ?? "",
                    Author = config.Author ?? "Unknown",
                    DateCreated = config.DateCreated ?? File.GetLastWriteTime(filePath),
                    FilePath = filePath,
                    MustCount = config.Must?.Count ?? 0,
                    ShouldCount = config.Should?.Count ?? 0,
                    MustNotCount = config.MustNot?.Count ?? 0,
                    DeckName = config.Deck ?? "Red",
                    StakeName = config.Stake ?? "White",
                };

                // Parse Must items
                if (config.Must != null)
                {
                    item.Must = ParseItemCollections(config.Must);
                }

                // Parse Should items
                if (config.Should != null)
                {
                    item.Should = ParseItemCollections(config.Should);
                }

                // Parse MustNot items
                if (config.MustNot != null)
                {
                    item.MustNot = ParseItemCollections(config.MustNot);
                }

                return item;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "PaginatedFilterBrowserViewModel",
                    $"Error parsing filter {filePath}: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Extracts item names from filter clauses and groups them by category
        /// </summary>
        private FilterItemCollections ParseItemCollections(
            List<Motely.Filters.MotelyJsonConfig.MotelyJsonFilterClause> clauses,
            int? scoreOverride = null
        )
        {
            var collections = new FilterItemCollections();

            foreach (var clause in clauses)
            {
                var itemType = clause.Type?.ToLowerInvariant() ?? "";
                var itemValue = clause.Value;

                // Handle single value
                if (!string.IsNullOrEmpty(itemValue))
                {
                    AddItemToCollection(collections, clause, itemValue, scoreOverride);
                }

                // Handle multiple values
                if (clause.Values != null)
                {
                    foreach (var value in clause.Values)
                    {
                        AddItemToCollection(collections, clause, value, scoreOverride);
                    }
                }

                // Recursively handle nested And/Or clauses
                if (clause.Clauses != null && clause.Clauses.Count > 0)
                {
                    var nestedCollections = ParseItemCollections(
                        clause.Clauses,
                        scoreOverride ?? clause.Score
                    );
                    collections.Jokers.AddRange(nestedCollections.Jokers);
                    collections.Consumables.AddRange(nestedCollections.Consumables);
                    collections.Vouchers.AddRange(nestedCollections.Vouchers);
                    collections.Tags.AddRange(nestedCollections.Tags);
                    collections.Bosses.AddRange(nestedCollections.Bosses);
                    collections.StandardCards.AddRange(nestedCollections.StandardCards);
                }
            }

            // Remove duplicates by ItemName while preserving first occurrence's data
            collections.Jokers = collections
                .Jokers.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();
            collections.Consumables = collections
                .Consumables.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();
            collections.Vouchers = collections
                .Vouchers.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();
            collections.Tags = collections
                .Tags.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();
            collections.Bosses = collections
                .Bosses.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();
            collections.StandardCards = collections
                .StandardCards.GroupBy(x => x.ItemName)
                .Select(g => g.First())
                .ToList();

            return collections;
        }

        /// <summary>
        /// Adds an item to the appropriate collection based on its type
        /// Creates a full ItemConfig from the clause data
        /// </summary>
        private void AddItemToCollection(
            FilterItemCollections collections,
            Motely.Filters.MotelyJsonConfig.MotelyJsonFilterClause clause,
            string itemValue,
            int? scoreOverride = null
        )
        {
            var itemType = clause.Type?.ToLowerInvariant() ?? "";

            // Create ItemConfig from clause data
            var itemConfig = new ItemConfig
            {
                ItemKey = itemValue,
                ItemType = clause.Type ?? "",
                ItemName = itemValue,
                Antes = clause.Antes?.ToList(),
                Edition = clause.Edition ?? "none",
                Seal = clause.Seal ?? "None",
                Enhancement = clause.Enhancement ?? "None",
                Rank = clause.Rank,
                Suit = clause.Suit,
                Score = scoreOverride ?? clause.Score,
                Label = clause.Label,
                Stickers = clause.Stickers != null ? new List<string>(clause.Stickers) : null,
            };

            switch (itemType)
            {
                case "joker":
                case "souljoker":
                    collections.Jokers.Add(itemConfig);
                    break;

                case "tarotcard":
                case "planetcard":
                case "spectralcard":
                    collections.Consumables.Add(itemConfig);
                    break;

                case "voucher":
                    collections.Vouchers.Add(itemConfig);
                    break;

                case "tag":
                case "smallblindtag":
                case "bigblindtag":
                    collections.Tags.Add(itemConfig);
                    break;

                case "boss":
                    collections.Bosses.Add(itemConfig);
                    break;

                case "standardcard":
                    collections.StandardCards.Add(itemConfig);
                    break;
            }
        }

        private List<FilterBrowserItem> GetCurrentPageItems()
        {
            var startIndex = _currentPage * ITEMS_PER_PAGE;
            return _allFilters.Skip(startIndex).Take(ITEMS_PER_PAGE).ToList();
        }

        private void UpdateCurrentPage()
        {
            CurrentPageFilters.Clear();

            var pageItems = GetCurrentPageItems();
            for (int i = 0; i < pageItems.Count; i++)
            {
                var filter = pageItems[i];

                var itemViewModel = new FilterBrowserItemViewModel
                {
                    FilterBrowserItem = filter,
                    DisplayText = filter.Name,
                    IsSelected = SelectedFilter?.FilePath == filter.FilePath,
                };
                CurrentPageFilters.Add(itemViewModel);
            }

            OnPropertyChanged(nameof(PageIndicatorText));
            OnPropertyChanged(nameof(StatusText));

            // Update command states
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        private void UpdateCurrentPageSelection()
        {
            foreach (var item in CurrentPageFilters)
            {
                item.IsSelected = SelectedFilter?.FilePath == item.FilterBrowserItem.FilePath;
            }
        }

        public void RefreshFilters()
        {
            // CRITICAL FIX: Rescan filesystem FIRST to remove deleted filters from cache
            var filterCache = Helpers.ServiceHelper.GetService<IFilterCacheService>();
            filterCache?.RefreshCache();

            // THEN reload from refreshed cache
            LoadFilters();
        }
    }

    // Data models
    public class FilterBrowserItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public string FilePath { get; set; } = "";
        public int MustCount { get; set; }
        public int ShouldCount { get; set; }
        public int MustNotCount { get; set; }
        public bool IsCreateNew { get; set; } = false;
        public string DeckName { get; set; } = "Red";
        public string StakeName { get; set; } = "White";

        // Item collections for sprite display
        public FilterItemCollections Must { get; set; } = new();
        public FilterItemCollections Should { get; set; } = new();
        public FilterItemCollections MustNot { get; set; } = new();

        // Visibility helpers
        public bool HasJokers => Must.Jokers.Count > 0 || MustNot.Jokers.Count > 0;
        public bool HasConsumables => Must.Consumables.Count > 0 || MustNot.Consumables.Count > 0;
        public bool HasVouchers => Must.Vouchers.Count > 0 || MustNot.Vouchers.Count > 0;

        public string FilterId => System.IO.Path.GetFileNameWithoutExtension(FilePath);
        public string AuthorText => $"by {Author}";
        public string DateText => DateCreated.ToString("MMM dd, yyyy");
        public string StatsText =>
            IsCreateNew
                ? "Start with a blank filter"
                : $"Must: {MustCount}, Should: {ShouldCount}, Must Not: {MustNotCount}";
    }

    /// <summary>
    /// Collections of items grouped by category for sprite display
    /// </summary>
    public class FilterItemCollections
    {
        public List<ItemConfig> Jokers { get; set; } = new();
        public List<ItemConfig> Consumables { get; set; } = new();
        public List<ItemConfig> Vouchers { get; set; } = new();
        public List<ItemConfig> Tags { get; set; } = new();
        public List<ItemConfig> Bosses { get; set; } = new();
        public List<ItemConfig> StandardCards { get; set; } = new();

        /// <summary>
        /// All items combined for fanned card hand display
        /// </summary>
        public List<ItemConfig> AllItems =>
            Jokers
                .Concat(Vouchers)
                .Concat(Consumables)
                .Concat(Tags)
                .Concat(Bosses)
                .Concat(StandardCards)
                .ToList();
    }

    public partial class FilterBrowserItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public FilterBrowserItem FilterBrowserItem { get; set; } = null!;
        public string DisplayText { get; set; } = "";

        public string ItemClasses =>
            FilterBrowserItem.IsCreateNew ? "filter-list-item create-new-item" : "filter-list-item";
    }
}
