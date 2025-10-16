using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.ViewModels.FilterTabs;
using Motely.Filters;
using BalatroSeedOracle.Helpers;
using Avalonia.Controls;
using Avalonia.Layout;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Main ViewModel for FiltersModal - proper MVVM pattern
    /// Replaces 8,583-line god class in FiltersModal.axaml.cs
    /// </summary>
    public partial class FiltersModalViewModel : ObservableObject, BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly IConfigurationService _configurationService;
        private readonly IFilterService _filterService;

        // ===== CORE STATE (using [ObservableProperty] for automatic INotifyPropertyChanged) =====
        [ObservableProperty]
        private string _currentCategory = "Jokers";

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadedFilter))]
        [NotifyCanExecuteChangedFor(nameof(DeleteFilterCommand))]
        private string? _currentFilterPath;

        [ObservableProperty]
        private MotelyJsonConfig? _loadedConfig;

        [ObservableProperty]
        private string _filterName = "";

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        private string _selectedDeck = "Red";

        [ObservableProperty]
        private int _selectedStake = 0;

        // Tab visibility properties - proper MVVM pattern
        [ObservableProperty]
        private bool _isLoadSaveTabVisible = true;

        [ObservableProperty]
        private bool _isVisualTabVisible = false;

        [ObservableProperty]
        private bool _isJsonTabVisible = false;

        [ObservableProperty]
        private bool _isTestTabVisible = false;

        [ObservableProperty]
        private bool _isSaveTabVisible = false;

        // Collections - Observable for data binding
        private readonly Dictionary<string, List<string>> _itemCategories;
        public ObservableCollection<string> SelectedMust { get; } = new();
        public ObservableCollection<string> SelectedShould { get; } = new();
        public ObservableCollection<string> SelectedMustNot { get; } = new();
        public Dictionary<string, ItemConfig> ItemConfigs { get; } = new();

        // Counters
        private int _itemKeyCounter = 0;
        private int _instanceCounter = 0;

        // Computed properties
        public bool HasLoadedFilter => !string.IsNullOrEmpty(CurrentFilterPath);

        public ObservableCollection<BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel> FilterTabs { get; }

        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel MustHaveItems { get; }
        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel ShouldHaveItems { get; }
        public BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel MustNotHaveItems { get; }

        public FiltersModalViewModel(IConfigurationService configurationService, IFilterService filterService)
        {
            _configurationService = configurationService;
            _filterService = filterService;
            
            _itemCategories = InitializeItemCategories();

            MustHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel { Header = "Must Have" };
            ShouldHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel { Header = "Should Have" };
            MustNotHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel { Header = "Must Not Have" };

            FilterTabs = new ObservableCollection<BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel>
            {
                MustHaveItems, 
                ShouldHaveItems, 
                MustNotHaveItems 
            };
        }
        
        // Deck/Stake index helpers
        public int SelectedDeckIndex
        {
            get
            {
                var decks = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                   "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                return Array.IndexOf(decks, SelectedDeck);
            }
            set
            {
                var decks = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                   "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                if (value >= 0 && value < decks.Length)
                {
                    SelectedDeck = decks[value];
                }
            }
        }

        public int SelectedStakeIndex
        {
            get => SelectedStake;
            set => SelectedStake = value;
        }

        public Dictionary<string, List<string>> ItemCategories => _itemCategories;
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();

        // Tab ViewModels for cross-tab communication
        public object? VisualBuilderTab { get; set; }
        public object? JsonEditorTab { get; set; }

        [ObservableProperty]
        private object? _currentPopup;

        // Expose the currently selected tab’s content to the view
        public object? CurrentTabContent
        {
            get
            {
                var index = SelectedTabIndex;
                return (index >= 0 && index < TabItems.Count)
                    ? TabItems[index].Content
                    : null;
            }
        }

        // ===== COMMANDS (using [RelayCommand] source generator) =====
        [RelayCommand]
        private void OpenItemConfigPopup(ItemConfig itemConfig)
        {
            CurrentPopup = new ItemConfigPopupViewModel(itemConfig);
        }

        [RelayCommand]
        private async Task SaveCurrentFilter()
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

        [RelayCommand]
        public async Task LoadFilter()
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

        [RelayCommand]
        private void CreateNewFilter()
        {
            try
            {
                ClearAllSelections();
                CurrentFilterPath = null;
                LoadedConfig = null;
                FilterName = "";
                FilterDescription = "";
                DebugLogger.Log("FiltersModalViewModel", "Created new filter");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error creating new filter: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasLoadedFilter))]
        private async Task DeleteFilter()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentFilterPath))
                {
                    var success = await _filterService.DeleteFilterAsync(CurrentFilterPath);
                    if (success)
                    {
                        CreateNewFilter();
                        DebugLogger.Log("FiltersModalViewModel", $"Deleted filter: {CurrentFilterPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error deleting filter: {ex.Message}");
            }
        }

        [RelayCommand]
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

        private void PopulateFilterTabs(Motely.Filters.MotelyJsonConfig config)
        {
            MustHaveItems.Items.Clear();
            ShouldHaveItems.Items.Clear();
            MustNotHaveItems.Items.Clear();

            if (config.Must != null)
            {
                foreach (var item in config.Must)
                {
                    MustHaveItems.Items.Add(new FilterItem { Name = item.Value ?? "", Status = FilterItemStatus.MustHave });
                }
            }

            if (config.Should != null)
            {
                foreach (var item in config.Should)
                {
                    ShouldHaveItems.Items.Add(new FilterItem { Name = item.Value ?? "", Status = FilterItemStatus.ShouldHave });
                }
            }

            if (config.MustNot != null)
            {
                foreach (var item in config.MustNot)
                {
                    MustNotHaveItems.Items.Add(new FilterItem { Name = item.Value ?? "", Status = FilterItemStatus.MustNotHave });
                }
            }
        }

        [RelayCommand]
        private async Task ReloadVisualFromSavedFile()
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
                    PopulateFilterTabs(config);
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

        // ===== TAB VISIBILITY MANAGEMENT (MVVM) =====

        /// <summary>
        /// Updates tab visibility based on the selected tab index
        /// Follows proper MVVM pattern - no direct UI manipulation
        /// </summary>
        /// <param name="tabIndex">0=LoadSave, 1=Visual, 2=JSON, 3=Test, 4=Save</param>
        public void UpdateTabVisibility(int tabIndex)
        {
            DebugLogger.Log("FiltersModalViewModel", $"UpdateTabVisibility called with tabIndex={tabIndex}");

            // Hide all tabs first
            IsLoadSaveTabVisible = false;
            IsVisualTabVisible = false;
            IsJsonTabVisible = false;
            IsTestTabVisible = false;
            IsSaveTabVisible = false;

            // Show the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsLoadSaveTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "LoadSave tab visible, all others hidden");
                    break;
                case 1:
                    IsVisualTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "Visual tab visible, all others hidden");
                    break;
                case 2:
                    IsJsonTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "JSON tab visible, all others hidden");
                    break;
                case 3:
                    IsTestTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "TEST tab visible, all others hidden");
                    break;
                case 4:
                    IsSaveTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "Save tab visible, all others hidden");
                    break;
            }

            // Log final state
            DebugLogger.Log("FiltersModalViewModel",
                $"Final visibility state - LoadSave:{IsLoadSaveTabVisible} Visual:{IsVisualTabVisible} JSON:{IsJsonTabVisible} Test:{IsTestTabVisible} Save:{IsSaveTabVisible}");
        }

        /// <summary>
        /// Implements in-modal back navigation for tabbed filters workflow.
        /// Returns true if navigation occurred; false indicates caller should close modal.
        /// </summary>
        public bool TryGoBack()
        {
            if (SelectedTabIndex > 0)
            {
                var newIndex = SelectedTabIndex - 1;
                SelectedTabIndex = newIndex;
                UpdateTabVisibility(newIndex);
                return true;
            }
            return false;
        }

        // Automatically update tab visibility and content when header selection changes
        partial void OnSelectedTabIndexChanged(int value)
        {
            UpdateTabVisibility(value);
            OnPropertyChanged(nameof(CurrentTabContent));
        }

        // ===== HELPER METHODS =====

        private Dictionary<string, List<string>> InitializeItemCategories()
        {
            // Initialize from BalatroData
            return new Dictionary<string, List<string>>
            {
                ["Favorites"] = new List<string>(), // TODO: Load from FavoritesService
                ["Jokers"] = new List<string>(BalatroData.Jokers.Keys),
                ["Tarots"] = new List<string>(BalatroData.TarotCards.Keys),
                ["Planets"] = new List<string>(BalatroData.PlanetCards.Keys),
                ["Spectrals"] = new List<string>(BalatroData.SpectralCards.Keys),
                ["PlayingCards"] = GeneratePlayingCardsList(),
                ["Vouchers"] = new List<string>(BalatroData.Vouchers.Keys),
                ["Tags"] = new List<string>(BalatroData.Tags.Keys),
                ["Bosses"] = new List<string>(BalatroData.BossBlinds.Keys)
            };
        }

        private List<string> GeneratePlayingCardsList()
        {
            var cards = new List<string>();
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            var suits = new[] { "♠", "♥", "♦", "♣" };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    cards.Add($"{rank}{suit}");
                }
            }

            return cards;
        }

        /// <summary>
        /// Builds MotelyJsonConfig from current ViewModel state
        /// Called by code-behind and ViewModel methods
        /// </summary>
        public Motely.Filters.MotelyJsonConfig BuildConfigFromCurrentState()
        {
            // Get author from UserProfileService
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            var author = userProfileService?.GetAuthorName() ?? "Unknown";

            var config = new Motely.Filters.MotelyJsonConfig
            {
                Name = string.IsNullOrWhiteSpace(FilterName) ? "Untitled Filter" : FilterName,
                Description = FilterDescription,
                DateCreated = DateTime.Now,
                Author = author,
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
            SelectedMust.Clear();
            SelectedShould.Clear();
            SelectedMustNot.Clear();
            ItemConfigs.Clear();
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

            // Create SaveFilterTab with parent reference so it can access selected items
            var configService = ServiceHelper.GetService<IConfigurationService>() ?? new ConfigurationService();
            var filterService = ServiceHelper.GetService<IFilterService>() ?? new FilterService(configService);
            var userProfileService = ServiceHelper.GetService<UserProfileService>() ?? throw new InvalidOperationException("UserProfileService not available");
            var filterConfigService = ServiceHelper.GetService<IFilterConfigurationService>() ?? new FilterConfigurationService(userProfileService);
            var saveFilterViewModel = new FilterTabs.SaveFilterTabViewModel(this, configService, filterService, filterConfigService);
            var saveFilterTab = new Components.FilterTabs.SaveFilterTab
            {
                DataContext = saveFilterViewModel
            };

            // Order must match UpdateTabVisibility mapping:
            // 0=LoadSave, 1=Visual, 2=JSON, 3=Test, 4=Save
            TabItems.Add(new TabItemViewModel("LOAD", CreateLoadTabContent()));
            TabItems.Add(new TabItemViewModel("VISUAL BUILDER", visualBuilderTab));
            TabItems.Add(new TabItemViewModel("JSON EDITOR", jsonEditorTab));
            // Separate TEST header; content is shown via bound TestPanel
            TabItems.Add(new TabItemViewModel("TEST", new Grid()));
            // Separate SAVE header with SaveFilterTab content
            TabItems.Add(new TabItemViewModel("SAVE", saveFilterTab));

            // Ensure initial tab content and visibility are set
            UpdateTabVisibility(SelectedTabIndex);
            OnPropertyChanged(nameof(CurrentTabContent));
        }

        private object CreateLoadTabContent()
        {
            // Use the new FilterSelectorControl (unified component for both modals)
            var filterSelector = new Components.FilterSelectorControl
            {
                // In FiltersModal, we are NOT in the Search context
                IsInSearchModal = false
            };

            // Handle EDIT action: load config and switch to Visual Builder
            filterSelector.FilterEditRequested += async (sender, filterPath) =>
            {
                try
                {
                    DebugLogger.Log("FiltersModalViewModel", $"Editing filter from: {filterPath}");

                    var json = await System.IO.File.ReadAllTextAsync(filterPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);

                    if (config != null)
                    {
                        LoadedConfig = config;
                        CurrentFilterPath = filterPath;

                        // Update deck and stake selection indices
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

                        // Switch to Visual Builder tab
                        SelectedTabIndex = 1;
                        DebugLogger.Log("FiltersModalViewModel", $"Filter loaded for editing: {config.Name}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter for editing: {ex.Message}");
                }
            };

            // Handle COPY action: duplicate file, resolve name collisions, load copy for editing
            filterSelector.FilterCopyRequested += async (sender, originalPath) =>
            {
                try
                {
                    var directory = System.IO.Path.GetDirectoryName(originalPath) ?? Directory.GetCurrentDirectory();
                    var baseName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
                    var extension = System.IO.Path.GetExtension(originalPath);

                    // Prefer "(copy)" suffix and append counter if needed
                    string candidateName = $"{baseName} (copy)";
                    string newPath = System.IO.Path.Combine(directory, candidateName + extension);
                    int counter = 2;
                    while (System.IO.File.Exists(newPath))
                    {
                        candidateName = $"{baseName} (copy {counter})";
                        newPath = System.IO.Path.Combine(directory, candidateName + extension);
                        counter++;
                    }

                    // Read original config; if parse fails, still copy raw file
                    Motely.Filters.MotelyJsonConfig? config = null;
                    try
                    {
                        var json = await System.IO.File.ReadAllTextAsync(originalPath);
                        config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);
                    }
                    catch { }

                    if (config != null)
                    {
                        // Update the config name to reflect copy and write new file
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        config.Name = string.IsNullOrWhiteSpace(config.Name) ? candidateName : $"{config.Name} (copy)";
                        var newJson = System.Text.Json.JsonSerializer.Serialize(config, options);
                        await System.IO.File.WriteAllTextAsync(newPath, newJson);
                    }
                    else
                    {
                        // Fallback: simple file copy
                        System.IO.File.Copy(originalPath, newPath, overwrite: false);
                    }

                    // Refresh list and load the new copy for editing
                    filterSelector.RefreshFilters();

                    try
                    {
                        var newJson = await System.IO.File.ReadAllTextAsync(newPath);
                        var newConfig = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(newJson);
                        if (newConfig != null)
                        {
                            LoadedConfig = newConfig;
                            CurrentFilterPath = newPath;

                            // Preserve deck/stake selections from the copy
                            if (!string.IsNullOrEmpty(newConfig.Deck))
                            {
                                var deckIndex = Array.IndexOf(new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic",
                                    "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" },
                                    newConfig.Deck);
                                if (deckIndex >= 0) SelectedDeckIndex = deckIndex;
                            }

                            if (!string.IsNullOrEmpty(newConfig.Stake))
                            {
                                var stakeIndex = Array.IndexOf(new[] { "white", "red", "green", "black", "blue", "purple", "orange", "gold" },
                                    newConfig.Stake.ToLower());
                                if (stakeIndex >= 0) SelectedStakeIndex = stakeIndex;
                            }

                            SelectedTabIndex = 1;
                            DebugLogger.Log("FiltersModalViewModel", $"Created and loaded copy: {candidateName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("FiltersModalViewModel", $"Copy created but failed to load: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error copying filter: {ex.Message}");
                }
            };

            // Handle DELETE action
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

            // Handle CREATE NEW FILTER
            filterSelector.NewFilterRequested += (sender, e) =>
            {
                try
                {
                    DebugLogger.Log("FiltersModalViewModel", "Create New Filter requested from Load tab");
                    CreateNewFilter();
                    SelectedTabIndex = 1;
                    DebugLogger.Log("FiltersModalViewModel", "Switched to Visual Builder for new filter");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("FiltersModalViewModel", $"Error handling Create New Filter: {ex.Message}");
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
    }
}