using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels
{
    public partial class PaginatedFilterBrowserViewModel : ObservableObject
    {
        // Fixed pagination size for stability (matches visible filter list height)
        private const int ITEMS_PER_PAGE = 10;

        private List<FilterBrowserItem> _allFilters = new();

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
                        DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Failed to create temp filter: {ex.Message}");
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
            var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.AppDomain.CurrentDomain.BaseDirectory;
            var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
            System.IO.Directory.CreateDirectory(filtersDir);
            
            var tempPath = System.IO.Path.Combine(filtersDir, "_UNSAVED_CREATION.json");
            
            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.MotelyJsonConfig
            {
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author = ServiceHelper.GetService<Services.UserProfileService>()?.GetAuthorName() ?? "Unknown",
                DateCreated = System.DateTime.UtcNow,
                Must = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>()
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(emptyFilter, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
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

                var filtersDir = Path.Combine(Directory.GetCurrentDirectory(), "JsonItemFilters");
                if (!Directory.Exists(filtersDir))
                {
                    Directory.CreateDirectory(filtersDir);
                    UpdateCurrentPage();
                    return;
                }

                var filterFiles = Directory.GetFiles(filtersDir, "*.json")
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
                DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Error loading filters: {ex.Message}");
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
                    AllowTrailingCommas = true
                };

                var config = JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(content, options);
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
                    MustNotCount = config.MustNot?.Count ?? 0
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
                DebugLogger.LogError("PaginatedFilterBrowserViewModel", $"Error parsing filter {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts item names from filter clauses and groups them by category
        /// </summary>
        private FilterItemCollections ParseItemCollections(List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause> clauses)
        {
            var collections = new FilterItemCollections();

            foreach (var clause in clauses)
            {
                var itemType = clause.Type?.ToLowerInvariant() ?? "";
                var itemValue = clause.Value;

                // Handle single value
                if (!string.IsNullOrEmpty(itemValue))
                {
                    AddItemToCollection(collections, itemType, itemValue);
                }

                // Handle multiple values
                if (clause.Values != null)
                {
                    foreach (var value in clause.Values)
                    {
                        AddItemToCollection(collections, itemType, value);
                    }
                }

                // Recursively handle nested And/Or clauses
                if (clause.Clauses != null && clause.Clauses.Count > 0)
                {
                    var nestedCollections = ParseItemCollections(clause.Clauses);
                    collections.Jokers.AddRange(nestedCollections.Jokers);
                    collections.Consumables.AddRange(nestedCollections.Consumables);
                    collections.Vouchers.AddRange(nestedCollections.Vouchers);
                    collections.Tags.AddRange(nestedCollections.Tags);
                    collections.Bosses.AddRange(nestedCollections.Bosses);
                }
            }

            // Remove duplicates while preserving order
            collections.Jokers = collections.Jokers.Distinct().ToList();
            collections.Consumables = collections.Consumables.Distinct().ToList();
            collections.Vouchers = collections.Vouchers.Distinct().ToList();
            collections.Tags = collections.Tags.Distinct().ToList();
            collections.Bosses = collections.Bosses.Distinct().ToList();

            return collections;
        }

        /// <summary>
        /// Adds an item to the appropriate collection based on its type
        /// </summary>
        private void AddItemToCollection(FilterItemCollections collections, string itemType, string itemValue)
        {
            switch (itemType)
            {
                case "joker":
                case "souljoker":
                    collections.Jokers.Add(itemValue);
                    break;

                case "tarotcard":
                case "planetcard":
                case "spectralcard":
                    collections.Consumables.Add(itemValue);
                    break;

                case "voucher":
                    collections.Vouchers.Add(itemValue);
                    break;

                case "tag":
                case "smallblindtag":
                case "bigblindtag":
                    collections.Tags.Add(itemValue);
                    break;

                case "boss":
                    collections.Bosses.Add(itemValue);
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
                string displayText;
                
                // Match Balatro Challenges style: no in-button number, just the name
                displayText = filter.Name;
                
                var itemViewModel = new FilterBrowserItemViewModel
                {
                    FilterBrowserItem = filter,
                    DisplayText = displayText,
                    IsSelected = SelectedFilter?.FilePath == filter.FilePath
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
        public string StatsText => IsCreateNew ? "Start with a blank filter" : $"Must: {MustCount}, Should: {ShouldCount}, Must Not: {MustNotCount}";
    }

    /// <summary>
    /// Collections of items grouped by category for sprite display
    /// </summary>
    public class FilterItemCollections
    {
        public List<string> Jokers { get; set; } = new();
        public List<string> Consumables { get; set; } = new(); // Tarots + Planets + Spectrals
        public List<string> Vouchers { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<string> Bosses { get; set; } = new();
    }

    public partial class FilterBrowserItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public FilterBrowserItem FilterBrowserItem { get; set; } = null!;
        public string DisplayText { get; set; } = "";

        public string ItemClasses => FilterBrowserItem.IsCreateNew ? "filter-list-item create-new-item" : "filter-list-item";
    }
}