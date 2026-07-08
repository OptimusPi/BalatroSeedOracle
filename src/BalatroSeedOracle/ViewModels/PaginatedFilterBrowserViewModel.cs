using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels
{
    public partial class PaginatedFilterBrowserViewModel : ObservableObject
    {
        private const int ITEMS_PER_PAGE = 10;

        private readonly IFilterCacheService _filterCacheService;
        private readonly UserProfileService? _userProfileService;
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

        /// <summary>
        /// Y position for the selection triangle (Canvas.Top) in the filter list.
        /// </summary>
        [ObservableProperty]
        private double _selectedItemTriangleY;

        // Properties
        public ObservableCollection<FilterBrowserItemViewModel> CurrentPageFilters { get; } = new();

        public bool HasSelectedFilter => SelectedFilter is not null;

        public string PageIndicatorText => $"Page {_currentPage + 1}/{TotalPages}";
        public string StatusText => $"Total {_allFilters.Count} filters";

        private int TotalPages => (int)Math.Ceiling((double)_allFilters.Count / ITEMS_PER_PAGE);

        // Commands that parent ViewModels will set (using ICommand for Avalonia property compatibility)
        public ICommand MainButtonCommand { get; set; } = null!;
        public ICommand SecondaryButtonCommand { get; set; } = null!;
        public ICommand DeleteCommand { get; set; } = null!;

        // Events
        public event EventHandler<FilterBrowserItem>? FilterSelected;

        public PaginatedFilterBrowserViewModel(
            IFilterCacheService filterCacheService,
            UserProfileService? userProfileService = null
        )
        {
            _filterCacheService = filterCacheService;
            _userProfileService = userProfileService;
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
            if (filterViewModel?.FilterBrowserItem is not null)
            {
                // Handle CREATE NEW FILTER specially
                if (filterViewModel.FilterBrowserItem.IsCreateNew)
                {
                    try
                    {
                        var tempPath = await CreateTempFilter();
                        var tempFilter = LoadFilterBrowserItem(tempPath);
                        if (tempFilter is not null)
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

            var tempPath = Path.Combine(filtersDir, "_UNSAVED_CREATION.json");

            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.Jaml.JamlConfig
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author = _userProfileService?.GetAuthorName() ?? "Unknown",
                Deck = Motely.Enums.MotelyDeck.Red,
                Stake = Motely.Enums.MotelyStake.White,
                Must = new System.Collections.Generic.List<Motely.Filters.Jaml.IJamlClause>(),
                Should = new System.Collections.Generic.List<Motely.Filters.Jaml.IJamlClause>(),
                MustNot = new System.Collections.Generic.List<Motely.Filters.Jaml.IJamlClause>(),
            };

            var yaml = Motely.Filters.Jaml.JamlConfigLoader.ToYaml(emptyFilter);
            await File.WriteAllTextAsync(tempPath, yaml);

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

                DebugLogger.Log(
                    "PaginatedFilterBrowserViewModel",
                    $"Loading filters from cache ({_filterCacheService.Count} cached)"
                );

                foreach (var cached in _filterCacheService.GetAllFilters())
                {
                    var filterItem = ConvertCachedFilterToBrowserItem(cached);
                    if (filterItem is not null)
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
                if (config is null || string.IsNullOrEmpty(config.Name))
                    return null;

                var item = new FilterBrowserItem
                {
                    Name = config.Name,
                    Description = config.Description ?? "",
                    Author = config.Author ?? "Unknown",
                    DateCreated = cachedFilter.LastModified,
                    FilePath = cachedFilter.FilePath,
                    MustCount = config.Must?.Count ?? 0,
                    ShouldCount = config.Should?.Count ?? 0,
                    MustNotCount = config.MustNot?.Count ?? 0,
                    DeckName = config.Deck.ToString(),
                    StakeName = config.Stake.ToString(),
                };

                // Parse Must items
                if (config.Must is not null)
                {
                    item.Must = ParseItemCollections(config.Must);
                }

                // Parse Should items
                if (config.Should is not null)
                {
                    item.Should = ParseItemCollections(config.Should);
                }

                // Parse MustNot items
                if (config.MustNot is not null)
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

                Motely.Filters.Jaml.JamlConfig? config = null;
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".jaml")
                {
                    // Load JAML file
                    if (
                        !Motely.Filters.Jaml.JamlConfigLoader.TryLoad(
                            content,
                            out config,
                            out var jamlError
                        )
                    )
                    {
                        DebugLogger.LogError(
                            "PaginatedFilterBrowserViewModel",
                            $"Failed to parse JAML {filePath}: {jamlError}"
                        );
                        return null;
                    }
                }
                else
                {
                    if (!Motely.Filters.Jaml.JamlConfigLoader.TryLoad(content, out config, out var loadError))
                    {
                        DebugLogger.LogError(
                            "PaginatedFilterBrowserViewModel",
                            $"Failed to load filter {filePath}: {loadError}"
                        );
                        return null;
                    }
                }

                if (config is null || string.IsNullOrEmpty(config.Name))
                    return null;

                var item = new FilterBrowserItem
                {
                    Name = config.Name,
                    Description = config.Description ?? "",
                    Author = config.Author ?? "Unknown",
                    DateCreated = File.GetLastWriteTime(filePath),
                    FilePath = filePath,
                    MustCount = config.Must?.Count ?? 0,
                    ShouldCount = config.Should?.Count ?? 0,
                    MustNotCount = config.MustNot?.Count ?? 0,
                    DeckName = config.Deck.ToString(),
                    StakeName = config.Stake.ToString(),
                };

                // Parse Must items
                if (config.Must is not null)
                {
                    item.Must = ParseItemCollections(config.Must);
                }

                // Parse Should items
                if (config.Should is not null)
                {
                    item.Should = ParseItemCollections(config.Should);
                }

                // Parse MustNot items
                if (config.MustNot is not null)
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
            System.Collections.Generic.IList<Motely.Filters.Jaml.IJamlClause> clauses,
            int? scoreOverride = null
        )
        {
            var collections = new FilterItemCollections();

            foreach (var clause in clauses)
            {
                var itemValue = clause.GetValueName();

                if (!string.IsNullOrEmpty(itemValue))
                {
                    AddItemToCollection(collections, clause, itemValue, scoreOverride);
                }

                var childClauses = clause.GetClauses();
                if (childClauses is not null && childClauses.Length > 0)
                {
                    var nestedCollections = ParseItemCollections(
                        childClauses,
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
            Motely.Filters.Jaml.IJamlClause clause,
            string itemValue,
            int? scoreOverride = null
        )
        {
            var typeName = clause.GetTypeName() ?? "";
            var itemType = typeName.ToLowerInvariant();

            var itemConfig = new ItemConfig
            {
                ItemKey = itemValue,
                ItemType = typeName,
                ItemName = itemValue,
                Antes = clause.GetAntes().ToList(),
                Edition = clause.GetEditionString() ?? "none",
                Seal = clause.GetSealString() ?? "None",
                Enhancement = clause.GetEnhancementString() ?? "None",
                Rank = clause is StandardCardClause sc ? sc.Rank?.ToString() : clause is StartingDrawClause sd ? sd.Rank?.ToString() : null,
                Suit = clause is StandardCardClause sc2 ? sc2.Suit?.ToString() : clause is StartingDrawClause sd2 ? sd2.Suit?.ToString() : null,
                Score = scoreOverride ?? (clause.Score > 0 ? clause.Score : 1),
                Label = clause.Label,
                Stickers = clause.GetStickerStrings()?.ToList(),
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
            // Rescan filesystem first so deleted filters drop out of the cache,
            // then reload our list from the refreshed cache.
            _filterCacheService.RefreshCache();
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

        public string FilterId => Path.GetFileNameWithoutExtension(FilePath);
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
