using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Controls;
using Motely.Filters;
using BalatroSeedOracle.Helpers;
using Avalonia.Controls;
using Avalonia.Layout;

namespace BalatroSeedOracle.ViewModels
{
    public class FiltersModalViewModel : BaseViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;

        // Core properties  
        private string _currentCategory = "Jokers";
        private string _searchFilter = "";
        private int _selectedTabIndex = 0;
        private string? _currentFilterPath;
        private Motely.Filters.MotelyJsonConfig? _loadedConfig;
        private string _filterName = "";
        private string _filterDescription = "";
        private string _selectedDeck = "Red";
        private int _selectedStake = 0;

        // Collections - Observable for data binding
        private readonly Dictionary<string, List<string>> _itemCategories;
        private readonly ObservableCollection<string> _selectedMust = new();
        private readonly ObservableCollection<string> _selectedShould = new();
        private readonly ObservableCollection<string> _selectedMustNot = new();
        private readonly Dictionary<string, ItemConfig> _itemConfigs = new();

        // Counters
        private int _itemKeyCounter = 0;
        private int _instanceCounter = 0;

        public FiltersModalViewModel(IConfigurationService configurationService, IFilterService filterService)
        {
            _configurationService = configurationService;
            _filterService = filterService;
            
            _itemCategories = InitializeItemCategories();
            // DON'T call InitializeTabs() here - it creates UI controls on a background thread!
            
            // Initialize commands
            SaveCommand = new AsyncRelayCommand(SaveCurrentFilterAsync);
            LoadCommand = new AsyncRelayCommand(LoadFilterAsync);
            NewCommand = new AsyncRelayCommand(CreateNewFilterAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteCurrentFilterAsync);
            RefreshCommand = new RelayCommand(RefreshFromConfig);
            ReloadVisualCommand = new AsyncRelayCommand(ReloadVisualFromSavedFileAsync);
        }

        #region Properties

        public string CurrentCategory
        {
            get => _currentCategory;
            set => SetProperty(ref _currentCategory, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set => SetProperty(ref _searchFilter, value);
        }


        public string? CurrentFilterPath
        {
            get => _currentFilterPath;
            set => SetProperty(ref _currentFilterPath, value);
        }

        public Motely.Filters.MotelyJsonConfig? LoadedConfig
        {
            get => _loadedConfig;
            set => SetProperty(ref _loadedConfig, value);
        }

        public string FilterName
        {
            get => _filterName;
            set => SetProperty(ref _filterName, value);
        }

        public string FilterDescription
        {
            get => _filterDescription;
            set => SetProperty(ref _filterDescription, value);
        }

        public string SelectedDeck
        {
            get => _selectedDeck;
            set => SetProperty(ref _selectedDeck, value);
        }

        public int SelectedStake
        {
            get => _selectedStake;
            set => SetProperty(ref _selectedStake, value);
        }
        
        public int SelectedDeckIndex
        {
            get
            {
                var decks = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost", 
                                   "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                return Array.IndexOf(decks, _selectedDeck);
            }
            set
            {
                var decks = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost", 
                                   "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                if (value >= 0 && value < decks.Length)
                {
                    SelectedDeck = decks[value];
                    OnPropertyChanged();
                }
            }
        }
        
        public int SelectedStakeIndex
        {
            get => _selectedStake;
            set
            {
                SelectedStake = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, List<string>> ItemCategories => _itemCategories;
        public ObservableCollection<string> SelectedMust => _selectedMust;
        public ObservableCollection<string> SelectedShould => _selectedShould;
        public ObservableCollection<string> SelectedMustNot => _selectedMustNot;
        public Dictionary<string, ItemConfig> ItemConfigs => _itemConfigs;

        // Tab visibility properties
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();
        
        // Tab ViewModels for cross-tab communication
        public object? VisualBuilderTab { get; set; }
        public object? JsonEditorTab { get; set; }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ReloadVisualCommand { get; }
        // SwitchTabCommand removed - using proper TabControl SelectedIndex binding

        #endregion

        #region Command Implementations

        private async Task SaveCurrentFilterAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilterPath))
                {
                    CurrentFilterPath = _configurationService.GetTempFilterPath();
                }

                // CRITICAL: Clean up databases BEFORE saving new filter
                await CleanupFilterDatabases();

                var config = BuildConfigFromCurrentState();
                var success = await _configurationService.SaveFilterAsync(CurrentFilterPath, config);
                
                if (success)
                {
                    LoadedConfig = config;
                    DebugLogger.Log("FiltersModalViewModel", $"✅ Filter saved with database cleanup: {CurrentFilterPath}");
                }
                else
                {
                    DebugLogger.LogError("FiltersModalViewModel", "Failed to save filter");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error saving filter: {ex.Message}");
            }
        }

        /// <summary>
        /// CRITICAL: Clean up all DuckDB files and running searches for this filter
        /// Must be called before saving an edited filter to prevent stale data
        /// </summary>
        private async Task CleanupFilterDatabases()
        {
            if (string.IsNullOrEmpty(CurrentFilterPath)) return;

            try
            {
                // Get filter name from path for database cleanup
                var filterName = Path.GetFileNameWithoutExtension(CurrentFilterPath);
                var searchResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
                
                DebugLogger.Log("FiltersModalViewModel", $"🧹 Starting database cleanup for filter: {filterName}");

                // STEP 1: Stop ALL running searches for this filter across all deck/stake combinations
                var searchManager = ServiceHelper.GetService<SearchManager>();
                if (searchManager != null)
                {
                    var stoppedSearches = searchManager.StopSearchesForFilter(filterName);
                    DebugLogger.Log("FiltersModalViewModel", $"🛑 Stopped {stoppedSearches} running searches for filter");
                }

                // STEP 2: Find and delete ALL DuckDB files for this filter
                if (Directory.Exists(searchResultsDir))
                {
                    var dbFiles = Directory.GetFiles(searchResultsDir, $"{filterName}_*.duckdb");
                    var walFiles = Directory.GetFiles(searchResultsDir, $"{filterName}_*.duckdb.wal");
                    
                    var deletedCount = 0;
                    
                    // Delete main database files
                    foreach (var dbFile in dbFiles)
                    {
                        try
                        {
                            File.Delete(dbFile);
                            deletedCount++;
                            DebugLogger.Log("FiltersModalViewModel", $"🗑️ Deleted: {Path.GetFileName(dbFile)}");
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("FiltersModalViewModel", $"Failed to delete {dbFile}: {ex.Message}");
                        }
                    }
                    
                    // Delete WAL files (write-ahead log)
                    foreach (var walFile in walFiles)
                    {
                        try
                        {
                            File.Delete(walFile);
                            deletedCount++;
                            DebugLogger.Log("FiltersModalViewModel", $"🗑️ Deleted: {Path.GetFileName(walFile)}");
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("FiltersModalViewModel", $"Failed to delete {walFile}: {ex.Message}");
                        }
                    }
                    
                    DebugLogger.Log("FiltersModalViewModel", $"🧹 Database cleanup complete - {deletedCount} files deleted");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Database cleanup failed: {ex.Message}");
            }
        }

        public async Task LoadFilterAsync()
        {
            try
            {
                var filters = await _filterService.GetAvailableFiltersAsync();
                // This would typically open a file dialog or selection UI
                // For now, we'll need UI interaction to select which filter to load
                DebugLogger.Log("FiltersModalViewModel", $"Found {filters.Count} available filters");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter: {ex.Message}");
            }
        }

        private Task CreateNewFilterAsync()
        {
            try
            {
                ClearAllSelections();
                CurrentFilterPath = null;
                LoadedConfig = null;
                DebugLogger.Log("FiltersModalViewModel", "Created new filter");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error creating new filter: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        private async Task DeleteCurrentFilterAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentFilterPath))
                {
                    var success = await _filterService.DeleteFilterAsync(CurrentFilterPath);
                    if (success)
                    {
                        await CreateNewFilterAsync();
                        DebugLogger.Log("FiltersModalViewModel", $"Deleted filter: {CurrentFilterPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error deleting filter: {ex.Message}");
            }
        }

        private void RefreshFromConfig()
        {
            try
            {
                if (LoadedConfig != null)
                {
                    LoadConfigIntoState(LoadedConfig);
                    DebugLogger.Log("FiltersModalViewModel", "Refreshed from config");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error refreshing from config: {ex.Message}");
            }
        }

        private async Task ReloadVisualFromSavedFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilterPath) || !_configurationService.FileExists(CurrentFilterPath))
                {
                    DebugLogger.Log("FiltersModalViewModel", "No saved file to reload visual from");
                    return;
                }

                DebugLogger.Log("FiltersModalViewModel", $"Reloading visual from file: {CurrentFilterPath}");

                var config = await _configurationService.LoadFilterAsync<Motely.Filters.MotelyJsonConfig>(CurrentFilterPath);
                if (config != null)
                {
                    LoadConfigIntoState(config);
                    LoadedConfig = config;
                    DebugLogger.Log("FiltersModalViewModel", "Visual reloaded from saved file");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error reloading visual: {ex.Message}");
            }
        }


        #endregion

        #region Helper Methods

        private Dictionary<string, List<string>> InitializeItemCategories()
        {
            // Initialize from BalatroData like the original implementation
            return new Dictionary<string, List<string>>
            {
                ["Favorites"] = new List<string>(), // TODO: Load from FavoritesService
                ["Jokers"] = new List<string>(), // TODO: Load from BalatroData.Jokers.Keys
                ["Tarots"] = new List<string>(), // TODO: Load from BalatroData.TarotCards.Keys
                ["Planets"] = new List<string>(), // TODO: Load from BalatroData.PlanetCards.Keys
                ["Spectrals"] = new List<string>(), // TODO: Load from BalatroData.SpectralCards.Keys
                ["PlayingCards"] = new List<string>(), // TODO: Generate playing cards list
                ["Vouchers"] = new List<string>(), // TODO: Load from BalatroData.Vouchers.Keys
                ["Tags"] = new List<string>(), // TODO: Load from BalatroData.Tags.Keys
                ["Bosses"] = new List<string>() // TODO: Load from BalatroData.BossBlinds.Keys
            };
        }

        private Motely.Filters.MotelyJsonConfig BuildConfigFromCurrentState()
        {
            var config = new Motely.Filters.MotelyJsonConfig
            {
                Name = FilterName,
                Description = FilterDescription,
                DateCreated = DateTime.Now,
                Author = "BalatroSeedOracle",
                Deck = GetDeckName(SelectedDeckIndex),
                Stake = GetStakeName(SelectedStakeIndex),
                Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>()
            };
            
            // Build Must clauses
            foreach (var itemKey in SelectedMust)
            {
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    var clause = ConvertItemConfigToClause(itemConfig);
                    if (clause != null)
                        config.Must.Add(clause);
                }
            }
            
            // Build Should clauses
            foreach (var itemKey in SelectedShould)
            {
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    var clause = ConvertItemConfigToClause(itemConfig);
                    if (clause != null)
                        config.Should.Add(clause);
                }
            }
            
            // Build MustNot clauses
            foreach (var itemKey in SelectedMustNot)
            {
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    var clause = ConvertItemConfigToClause(itemConfig);
                    if (clause != null)
                        config.MustNot.Add(clause);
                }
            }
            
            return config;
        }
        
        private MotelyJsonConfig.MotleyJsonFilterClause? ConvertItemConfigToClause(ItemConfig itemConfig)
        {
            var clause = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = itemConfig.IsSoulJoker ? "SoulJoker" : itemConfig.ItemType,
                Value = itemConfig.IsMultiValue ? null : itemConfig.ItemName,
                Values = itemConfig.IsMultiValue ? itemConfig.Values?.ToArray() : null,
                Score = itemConfig.Score,
                Label = itemConfig.Label,
                Min = itemConfig.Min
            };
            
            // Add antes if configured
            if (itemConfig.Antes?.Any() == true)
            {
                clause.Antes = itemConfig.Antes.ToArray();
            }
            
            // Add edition if not default
            if (!string.IsNullOrEmpty(itemConfig.Edition) && itemConfig.Edition != "none")
            {
                clause.Edition = itemConfig.Edition;
            }
            
            // Add stickers if configured
            if (itemConfig.Stickers?.Any() == true)
            {
                clause.Stickers = itemConfig.Stickers;
            }
            
            // Handle playing card specific properties
            if (itemConfig.ItemType == "PlayingCard")
            {
                if (itemConfig.Seal != "None")
                    clause.Seal = itemConfig.Seal;
                if (itemConfig.Enhancement != "None")  
                    clause.Enhancement = itemConfig.Enhancement;
            }
            
            // Handle tag type
            if (!string.IsNullOrEmpty(itemConfig.TagType))
            {
                clause.Type = itemConfig.TagType; // Override with specific tag type
            }
            
            // Handle sources if configured
            if (itemConfig.ShopSlots?.Any() == true || 
                itemConfig.PackSlots?.Any() == true ||
                itemConfig.SkipBlindTags ||
                itemConfig.IsMegaArcana)
            {
                clause.Sources = new MotelyJsonConfig.SourcesConfig
                {
                    ShopSlots = itemConfig.ShopSlots?.ToArray(),
                    PackSlots = itemConfig.PackSlots?.ToArray(),
                    Tags = itemConfig.SkipBlindTags ? true : null,
                    RequireMega = itemConfig.IsMegaArcana ? true : null
                };
            }
            else if (itemConfig.Sources != null)
            {
                // Fallback to direct sources object if set
                clause.Sources = itemConfig.Sources as MotelyJsonConfig.SourcesConfig;
            }
            
            return clause;
        }

        private void LoadConfigIntoState(Motely.Filters.MotelyJsonConfig config)
        {
            // Clear current state
            ClearAllSelections();
            
            // Load basic properties
            FilterName = config.Name ?? "Untitled";
            FilterDescription = config.Description ?? "";
            
            // Load deck and stake
            if (!string.IsNullOrEmpty(config.Deck))
                SelectedDeck = config.Deck;
            if (!string.IsNullOrEmpty(config.Stake))
            {
                var stakes = new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" };
                SelectedStake = Array.IndexOf(stakes, config.Stake.ToLower());
                if (SelectedStake < 0) SelectedStake = 0;
            }
            
            // Load Must clauses
            if (config.Must != null)
            {
                foreach (var clause in config.Must)
                {
                    var itemKey = GenerateNextItemKey();
                    var itemConfig = ConvertClauseToItemConfig(clause, itemKey);
                    ItemConfigs[itemKey] = itemConfig;
                    SelectedMust.Add(itemKey);
                }
            }
            
            // Load Should clauses
            if (config.Should != null)
            {
                foreach (var clause in config.Should)
                {
                    var itemKey = GenerateNextItemKey();
                    var itemConfig = ConvertClauseToItemConfig(clause, itemKey);
                    ItemConfigs[itemKey] = itemConfig;
                    SelectedShould.Add(itemKey);
                }
            }
            
            // Load MustNot clauses
            if (config.MustNot != null)
            {
                foreach (var clause in config.MustNot)
                {
                    var itemKey = GenerateNextItemKey();
                    var itemConfig = ConvertClauseToItemConfig(clause, itemKey);
                    ItemConfigs[itemKey] = itemConfig;
                    SelectedMustNot.Add(itemKey);
                }
            }
            
            LoadedConfig = config;
        }
        
        private ItemConfig ConvertClauseToItemConfig(MotelyJsonConfig.MotleyJsonFilterClause clause, string itemKey)
        {
            var itemConfig = new ItemConfig
            {
                ItemKey = itemKey,
                ItemType = clause.Type ?? "Unknown",
                ItemName = clause.Value ?? clause.Values?.FirstOrDefault() ?? "",
                Score = clause.Score,
                Label = clause.Label,
                Min = clause.Min,
                Edition = clause.Edition ?? "none",
                Antes = clause.Antes?.ToList(),
                Sources = clause.Sources,
                Stickers = clause.Stickers?.ToList()
            };
            
            // Handle special types
            if (clause.Type == "SoulJoker")
            {
                itemConfig.ItemType = "Joker"; // Display as joker in UI
                itemConfig.IsSoulJoker = true; // Mark as soul joker
            }
            
            // Handle multi-value clauses (values array)
            if (clause.Values != null && clause.Values.Length > 1)
            {
                itemConfig.IsMultiValue = true;
                itemConfig.Values = clause.Values.ToList();
            }
            
            return itemConfig;
        }

        private void ClearAllSelections()
        {
            _selectedMust.Clear();
            _selectedShould.Clear();
            _selectedMustNot.Clear();
            _itemConfigs.Clear();
            _itemKeyCounter = 0;
            _instanceCounter = 0;
        }

        public string GenerateNextItemKey()
        {
            return $"item_{++_itemKeyCounter}";
        }

        public int GenerateNextInstance()
        {
            return ++_instanceCounter;
        }

        public void InitializeTabs()
        {
            TabItems.Clear();
            
            // Create child ViewModels with parent reference
            var visualBuilderViewModel = new FilterTabs.VisualBuilderTabViewModel(this);
            var visualBuilderTab = new Components.FilterTabs.VisualBuilderTab
            {
                DataContext = visualBuilderViewModel
            };
            VisualBuilderTab = visualBuilderViewModel; // Store reference
            
            var jsonEditorViewModel = new FilterTabs.JsonEditorTabViewModel(this);
            var jsonEditorTab = new Components.FilterTabs.JsonEditorTab
            {
                DataContext = jsonEditorViewModel
            };
            JsonEditorTab = jsonEditorViewModel; // Store reference
            
            var saveFilterTab = new Components.FilterTabs.SaveFilterTab();
            
            TabItems.Add(new TabItemViewModel("VISUAL BUILDER", visualBuilderTab));
            TabItems.Add(new TabItemViewModel("JSON EDITOR", jsonEditorTab));
            TabItems.Add(new TabItemViewModel("SAVE & TEST", saveFilterTab));
            TabItems.Add(new TabItemViewModel("LOAD", CreateLoadTabContent()));
        }

        private object CreateLoadTabContent()
        {
            // Create a FilterSelector for loading filters
            var filterSelector = new Components.FilterSelector
            {
                Title = "Select Filter to Load",
                ShowSelectButton = true,
                ShowActionButtons = true,
                AutoLoadEnabled = false  // Don't auto-load on selection, wait for button click
            };
            
            // Wire up the FilterLoaded event to actually load the filter
            filterSelector.FilterLoaded += async (sender, filterPath) =>
            {
                try
                {
                    DebugLogger.Log("FiltersModalViewModel", $"Loading filter from: {filterPath}");
                    
                    // Read and parse the filter
                    var json = await System.IO.File.ReadAllTextAsync(filterPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);
                    
                    if (config != null)
                    {
                        LoadedConfig = config;
                        CurrentFilterPath = filterPath;
                        
                        // Update deck and stake
                        if (!string.IsNullOrEmpty(config.Deck))
                        {
                            var deckIndex = Array.IndexOf(new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", 
                                "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" }, 
                                config.Deck);
                            if (deckIndex >= 0) SelectedDeckIndex = deckIndex;
                        }
                        
                        if (!string.IsNullOrEmpty(config.Stake))
                        {
                            var stakeIndex = Array.IndexOf(new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" }, 
                                config.Stake.ToLower());
                            if (stakeIndex >= 0) SelectedStakeIndex = stakeIndex;
                        }
                        
                        // Switch to Visual Builder tab to show loaded filter
                        SelectedTabIndex = 0;
                        
                        DebugLogger.Log("FiltersModalViewModel", $"Filter loaded successfully: {config.Name}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter: {ex.Message}");
                }
            };
            
            // Handle filter editing
            filterSelector.FilterEditRequested += async (sender, filterPath) =>
            {
                // Trigger the same load logic for editing
                try
                {
                    DebugLogger.Log("FiltersModalViewModel", $"Editing filter from: {filterPath}");
                    
                    var json = await System.IO.File.ReadAllTextAsync(filterPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);
                    
                    if (config != null)
                    {
                        LoadedConfig = config;
                        CurrentFilterPath = filterPath;
                        
                        // Update deck and stake
                        if (!string.IsNullOrEmpty(config.Deck))
                        {
                            var deckIndex = Array.IndexOf(new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", 
                                "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" }, 
                                config.Deck);
                            if (deckIndex >= 0) SelectedDeckIndex = deckIndex;
                        }
                        
                        if (!string.IsNullOrEmpty(config.Stake))
                        {
                            var stakeIndex = Array.IndexOf(new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" }, 
                                config.Stake.ToLower());
                            if (stakeIndex >= 0) SelectedStakeIndex = stakeIndex;
                        }
                        
                        // Switch to Visual Builder tab for editing
                        SelectedTabIndex = 0;
                        
                        DebugLogger.Log("FiltersModalViewModel", $"Filter loaded for editing: {config.Name}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter for editing: {ex.Message}");
                }
            };
            
            // Handle filter deletion
            filterSelector.FilterDeleteRequested += (sender, filterPath) =>
            {
                try
                {
                    if (System.IO.File.Exists(filterPath))
                    {
                        System.IO.File.Delete(filterPath);
                        filterSelector.RefreshFilters();
                        DebugLogger.Log("FiltersModalViewModel", $"Deleted filter: {filterPath}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error deleting filter: {ex.Message}");
                }
            };
            
            return new Border
            {
                Background = Avalonia.Media.Brush.Parse("#2a2a2a"),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Child = filterSelector
            };
        }
        
        private string GetDeckName(int index)
        {
            var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost", 
                                    "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : "Red";
        }
        
        private string GetStakeName(int index)
        {
            var stakeNames = new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : "white";
        }

        #endregion
    }
}