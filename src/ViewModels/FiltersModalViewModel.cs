using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels.FilterTabs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Main ViewModel for FiltersModal - proper MVVM pattern
    /// Replaces 8,583-line god class in FiltersModal.axaml.cs
    /// </summary>
    public partial class FiltersModalViewModel
        : ObservableObject,
            BalatroSeedOracle.Helpers.IModalBackNavigable
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

        // Track original filter criteria hash to detect MUST/SHOULD/MUSTNOT changes
        private string? _originalCriteriaHash;

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

        public FiltersModalViewModel(
            IConfigurationService configurationService,
            IFilterService filterService
        )
        {
            _configurationService = configurationService;
            _filterService = filterService;

            _itemCategories = InitializeItemCategories();

            MustHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Must Have",
            };
            ShouldHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Should Have",
            };
            MustNotHaveItems = new BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel
            {
                Header = "Must Not Have",
            };

            FilterTabs =
                new ObservableCollection<BalatroSeedOracle.ViewModels.FilterTabs.FilterTabViewModel>
                {
                    MustHaveItems,
                    ShouldHaveItems,
                    MustNotHaveItems,
                };
        }

        // Deck/Stake display values for spinners
        public string[] DeckDisplayValues { get; } = BalatroData.Decks.Values.ToArray();

        // Generate stake display values from BalatroData (strip " Stake" suffix for display)
        public string[] StakeDisplayValues { get; } =
            BalatroData.Stakes.Values.Select(v => v.Replace(" Stake", "")).ToArray();

        // Deck/Stake index helpers
        public int SelectedDeckIndex
        {
            get
            {
                var decks = new[]
                {
                    "Red",
                    "Blue",
                    "Yellow",
                    "Green",
                    "Black",
                    "Magic",
                    "Nebula",
                    "Ghost",
                    "Abandoned",
                    "Checkered",
                    "Zodiac",
                    "Painted",
                    "Anaglyph",
                    "Plasma",
                    "Erratic",
                };
                return Array.IndexOf(decks, SelectedDeck);
            }
            set
            {
                var decks = new[]
                {
                    "Red",
                    "Blue",
                    "Yellow",
                    "Green",
                    "Black",
                    "Magic",
                    "Nebula",
                    "Ghost",
                    "Abandoned",
                    "Checkered",
                    "Zodiac",
                    "Painted",
                    "Anaglyph",
                    "Plasma",
                    "Erratic",
                };
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
                return (index >= 0 && index < TabItems.Count) ? TabItems[index].Content : null;
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

                // Check if MUST/SHOULD/MUSTNOT criteria changed (not just metadata like name/description)
                var currentHash = ComputeCriteriaHash();
                var criteriaChanged = _originalCriteriaHash == null || currentHash != _originalCriteriaHash;

                if (criteriaChanged)
                {
                    DebugLogger.LogImportant(
                        "FiltersModalViewModel",
                        $"🔄 Filter criteria changed - invalidating databases and dumping to fertilizer.txt"
                    );
                    // CRITICAL: Clean up databases BEFORE saving filter with changed criteria
                    await CleanupFilterDatabases();
                }
                else
                {
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        "📝 Only metadata changed (name/description/notes) - keeping databases intact"
                    );
                }

                var config = BuildConfigFromCurrentState();
                var success = await _configurationService.SaveFilterAsync(
                    CurrentFilterPath,
                    config
                );

                if (success)
                {
                    LoadedConfig = config;
                    _originalCriteriaHash = currentHash; // Update hash for next save
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        $"✅ Filter saved: {CurrentFilterPath}"
                    );
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
            if (string.IsNullOrEmpty(CurrentFilterPath))
                return;

            try
            {
                // Get filter name from path for database cleanup
                var filterName = Path.GetFileNameWithoutExtension(CurrentFilterPath);
                var searchResultsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "SearchResults"
                );

                DebugLogger.Log(
                    "FiltersModalViewModel",
                    $"🧹 Starting database cleanup for filter: {filterName}"
                );

                // STEP 1: Stop ALL running searches for this filter across all deck/stake combinations
                var searchManager = ServiceHelper.GetService<SearchManager>();
                if (searchManager != null)
                {
                    var stoppedSearches = searchManager.StopSearchesForFilter(filterName);
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        $"🛑 Stopped {stoppedSearches} running searches for filter"
                    );
                }

                // STEP 2: Dump seeds to fertilizer.txt BEFORE deleting databases
                if (Directory.Exists(searchResultsDir))
                {
                    var dbFiles = Directory.GetFiles(searchResultsDir, $"{filterName}_*.duckdb");

                    // Dump all seeds from all database files to fertilizer.txt
                    await DumpDatabasesToFertilizerAsync(dbFiles);

                    var walFiles = Directory.GetFiles(
                        searchResultsDir,
                        $"{filterName}_*.duckdb.wal"
                    );

                    var deletedCount = 0;

                    // Delete main database files
                    foreach (var dbFile in dbFiles)
                    {
                        try
                        {
                            File.Delete(dbFile);
                            deletedCount++;
                            DebugLogger.Log(
                                "FiltersModalViewModel",
                                $"🗑️ Deleted: {Path.GetFileName(dbFile)}"
                            );
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "FiltersModalViewModel",
                                $"Failed to delete {dbFile}: {ex.Message}"
                            );
                        }
                    }

                    // Delete WAL files (write-ahead log)
                    foreach (var walFile in walFiles)
                    {
                        try
                        {
                            File.Delete(walFile);
                            deletedCount++;
                            DebugLogger.Log(
                                "FiltersModalViewModel",
                                $"🗑️ Deleted: {Path.GetFileName(walFile)}"
                            );
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "FiltersModalViewModel",
                                $"Failed to delete {walFile}: {ex.Message}"
                            );
                        }
                    }

                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        $"🧹 Database cleanup complete - {deletedCount} files deleted"
                    );
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Database cleanup failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Dumps all seeds from multiple database files to WordLists/fertilizer.txt.
        /// "Fertilizer" helps new "seeds" grow faster by providing a head start wordlist.
        /// </summary>
        private async Task DumpDatabasesToFertilizerAsync(string[] dbFiles)
        {
            if (dbFiles == null || dbFiles.Length == 0)
            {
                DebugLogger.Log("FiltersModalViewModel", "No database files to dump");
                return;
            }

            try
            {
                // Ensure WordLists directory exists
                var wordListsDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "WordLists"
                );
                Directory.CreateDirectory(wordListsDir);

                var fertilizerPath = Path.Combine(wordListsDir, "fertilizer.txt");
                var allSeeds = new List<string>();

                // Collect seeds from all database files
                foreach (var dbFile in dbFiles)
                {
                    if (!File.Exists(dbFile))
                        continue;

                    try
                    {
                        using var connection = new DuckDB.NET.Data.DuckDBConnection($"Data Source={dbFile}");
                        connection.Open();

                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = "SELECT seed FROM results ORDER BY seed";

                        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var seed = reader.GetString(0);
                            if (!string.IsNullOrWhiteSpace(seed))
                            {
                                allSeeds.Add(seed);
                            }
                        }

                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"🌱 Collected seeds from: {Path.GetFileName(dbFile)}"
                        );
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "FiltersModalViewModel",
                            $"Failed to dump seeds from {Path.GetFileName(dbFile)}: {ex.Message}"
                        );
                    }
                }

                if (allSeeds.Count == 0)
                {
                    DebugLogger.Log("FiltersModalViewModel", "No seeds found in databases");
                    return;
                }

                // Append all seeds to fertilizer.txt
                await File.AppendAllLinesAsync(fertilizerPath, allSeeds).ConfigureAwait(false);

                DebugLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"🌱 Dumped {allSeeds.Count} seeds to fertilizer.txt (total file size: {new FileInfo(fertilizerPath).Length} bytes)"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Failed to dump databases to fertilizer.txt: {ex.Message}"
                );
                // Don't throw - fertilizer dump is a nice-to-have, not critical
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
                DebugLogger.Log(
                    "FiltersModalViewModel",
                    $"Found {filters.Count} available filters"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error loading filter: {ex.Message}"
                );
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
                _originalCriteriaHash = null; // New filter has no original criteria
                DebugLogger.Log("FiltersModalViewModel", "Created new filter");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error creating new filter: {ex.Message}"
                );
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
                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"Deleted filter: {CurrentFilterPath}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error deleting filter: {ex.Message}"
                );
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
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error refreshing from config: {ex.Message}"
                );
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
                    MustHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.Value ?? "",
                            Status = FilterItemStatus.MustHave,
                        }
                    );
                }
            }

            if (config.Should != null)
            {
                foreach (var item in config.Should)
                {
                    ShouldHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.Value ?? "",
                            Status = FilterItemStatus.ShouldHave,
                        }
                    );
                }
            }

            if (config.MustNot != null)
            {
                foreach (var item in config.MustNot)
                {
                    MustNotHaveItems.Items.Add(
                        new FilterItem
                        {
                            Name = item.Value ?? "",
                            Status = FilterItemStatus.MustNotHave,
                        }
                    );
                }
            }
        }

        [RelayCommand]
        private async Task ReloadVisualFromSavedFile()
        {
            try
            {
                if (
                    string.IsNullOrEmpty(CurrentFilterPath)
                    || !_configurationService.FileExists(CurrentFilterPath)
                )
                {
                    DebugLogger.Log("FiltersModalViewModel", "No saved file to reload visual from");
                    return;
                }

                DebugLogger.Log(
                    "FiltersModalViewModel",
                    $"Reloading visual from file: {CurrentFilterPath}"
                );

                var config =
                    await _configurationService.LoadFilterAsync<Motely.Filters.MotelyJsonConfig>(
                        CurrentFilterPath
                    );
                if (config != null)
                {
                    PopulateFilterTabs(config);
                    LoadConfigIntoState(config);
                    LoadedConfig = config;

                    // Update JSON editor with loaded content
                    UpdateJsonEditorFromConfig(config);

                    // Update Visual Builder with FilterItem objects
                    await UpdateVisualBuilderFromItemConfigs();

                    // CRITICAL: Expand drop zones that have items so they render
                    ExpandDropZonesWithItems();

                    DebugLogger.Log("FiltersModalViewModel", "Visual reloaded from saved file");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error reloading visual: {ex.Message}"
                );
            }
        }

        // ===== TAB VISIBILITY MANAGEMENT (MVVM) =====

        /// <summary>
        /// Updates tab visibility based on the selected tab index
        /// Follows proper MVVM pattern - no direct UI manipulation
        /// </summary>
        /// <param name="tabIndex">0=Configure Filter, 1=Configure Score, 2=JSON Editor, 3=Save</param>
        public void UpdateTabVisibility(int tabIndex)
        {
            DebugLogger.Log(
                "FiltersModalViewModel",
                $"UpdateTabVisibility called with tabIndex={tabIndex}"
            );

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
                    IsVisualTabVisible = true;
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        "Configure Filter tab visible, all others hidden"
                    );
                    break;
                case 1:
                    IsVisualTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "Configure Score tab visible, all others hidden");
                    break;
                case 2:
                    IsJsonTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "JSON Editor tab visible, all others hidden");
                    break;
                case 3:
                    IsSaveTabVisible = true;
                    // Refresh Save tab data when it becomes visible
                    RefreshSaveTabData();
                    DebugLogger.Log("FiltersModalViewModel", "Save tab visible, all others hidden");
                    break;
            }

            // Log final state
            DebugLogger.Log(
                "FiltersModalViewModel",
                $"Final visibility state - Visual:{IsVisualTabVisible} JSON:{IsJsonTabVisible} Save:{IsSaveTabVisible}"
            );
        }

        /// <summary>
        /// Refresh Save tab data when switching to it
        /// </summary>
        private void RefreshSaveTabData()
        {
            try
            {
                // Find the Save tab and refresh its data
                var saveTab = TabItems.FirstOrDefault(t => t.Content is Components.FilterTabs.SaveFilterTab);
                if (saveTab?.Content is Components.FilterTabs.SaveFilterTab saveFilterTab &&
                    saveFilterTab.DataContext is FilterTabs.SaveFilterTabViewModel saveVm)
                {
                    saveVm.PreFillFilterData();
                    DebugLogger.Log("FiltersModalViewModel", "Refreshed Save tab data");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error refreshing Save tab: {ex.Message}");
            }
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
                ["Favorites"] = Services.FavoritesService.Instance.GetFavoriteItems(),
                ["Jokers"] = new List<string>(BalatroData.Jokers.Keys),
                ["Tarots"] = new List<string>(BalatroData.TarotCards.Keys),
                ["Planets"] = new List<string>(BalatroData.PlanetCards.Keys),
                ["Spectrals"] = new List<string>(BalatroData.SpectralCards.Keys),
                ["PlayingCards"] = GeneratePlayingCardsList(),
                ["Vouchers"] = new List<string>(BalatroData.Vouchers.Keys),
                ["Tags"] = new List<string>(BalatroData.Tags.Keys),
                ["Bosses"] = new List<string>(BalatroData.BossBlinds.Keys),
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
                MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
            };

            // CRITICAL FIX: Read from VisualBuilderTab's collections if available
            // The VisualBuilderTab has its own SelectedMust/Should/MustNot collections (FilterItem objects)
            IEnumerable<string> mustKeys;
            IEnumerable<string> shouldKeys;
            IEnumerable<string> mustNotKeys;

            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                // Convert FilterItem objects to their keys (ItemKey property)
                mustKeys = visualVm.SelectedMust.Select(item => item.ItemKey);
                shouldKeys = visualVm.SelectedShould.Select(item => item.ItemKey);
                mustNotKeys = visualVm.SelectedMustNot.Select(item => item.ItemKey);
                DebugLogger.Log("FilterConfigurationService",
                    $"Building config from VisualBuilderTab: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should, {visualVm.SelectedMustNot.Count} mustNot");
            }
            else
            {
                // Fallback to parent's collections
                mustKeys = SelectedMust;
                shouldKeys = SelectedShould;
                mustNotKeys = SelectedMustNot;
            }

            // Build Must clauses
            foreach (var itemKey in mustKeys)
            {
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    var clause = ConvertItemConfigToClause(itemConfig);
                    if (clause != null)
                        config.Must.Add(clause);
                }
            }

            // Build Should clauses
            foreach (var itemKey in shouldKeys)
            {
                if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                {
                    var clause = ConvertItemConfigToClause(itemConfig);
                    if (clause != null)
                        config.Should.Add(clause);
                }
            }

            // Build MustNot clauses
            foreach (var itemKey in mustNotKeys)
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

        private MotelyJsonConfig.MotleyJsonFilterClause? ConvertItemConfigToClause(
            ItemConfig itemConfig
        )
        {
            var clause = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = itemConfig.IsSoulJoker ? "SoulJoker" : itemConfig.ItemType,
                Value = itemConfig.IsMultiValue ? null : itemConfig.ItemName,
                Values = itemConfig.IsMultiValue ? itemConfig.Values?.ToArray() : null,
                Score = itemConfig.Score,
                Label = itemConfig.Label,
                Min = itemConfig.Min,
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
            if (
                itemConfig.ShopSlots?.Any() == true
                || itemConfig.PackSlots?.Any() == true
                || itemConfig.SkipBlindTags
                || itemConfig.IsMegaArcana
            )
            {
                clause.Sources = new MotelyJsonConfig.SourcesConfig
                {
                    ShopSlots = itemConfig.ShopSlots?.ToArray(),
                    PackSlots = itemConfig.PackSlots?.ToArray(),
                    Tags = itemConfig.SkipBlindTags ? true : null,
                    RequireMega = itemConfig.IsMegaArcana ? true : null,
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
                var stakes = new[]
                {
                    "white",
                    "red",
                    "green",
                    "black",
                    "blue",
                    "purple",
                    "orange",
                    "gold",
                };
                SelectedStake = Array.IndexOf(stakes, config.Stake.ToLower());
                if (SelectedStake < 0)
                    SelectedStake = 0;
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

            // Capture criteria hash after loading
            _originalCriteriaHash = ComputeCriteriaHash();
        }

        /// <summary>
        /// Computes SHA256 hash of current MUST/SHOULD/MUSTNOT criteria.
        /// Only considers filter logic, NOT metadata like name/description/notes.
        /// Used to detect when filter criteria actually changed vs metadata-only edits.
        /// </summary>
        private string ComputeCriteriaHash()
        {
            var sb = new StringBuilder();

            // MUST clauses
            sb.Append("MUST:");
            foreach (var itemKey in SelectedMust.OrderBy(k => k))
            {
                if (ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    sb.Append($"{config.ItemType}|{config.ItemName}|{config.Score}|{config.Min}|");
                    sb.Append($"{config.Edition}|{config.Label}|");
                    if (config.Antes != null)
                        sb.Append(string.Join(",", config.Antes));
                    sb.Append("|");
                }
            }

            // SHOULD clauses
            sb.Append("SHOULD:");
            foreach (var itemKey in SelectedShould.OrderBy(k => k))
            {
                if (ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    sb.Append($"{config.ItemType}|{config.ItemName}|{config.Score}|{config.Min}|");
                    sb.Append($"{config.Edition}|{config.Label}|");
                    if (config.Antes != null)
                        sb.Append(string.Join(",", config.Antes));
                    sb.Append("|");
                }
            }

            // MUSTNOT clauses
            sb.Append("MUSTNOT:");
            foreach (var itemKey in SelectedMustNot.OrderBy(k => k))
            {
                if (ItemConfigs.TryGetValue(itemKey, out var config))
                {
                    sb.Append($"{config.ItemType}|{config.ItemName}|{config.Score}|{config.Min}|");
                    sb.Append($"{config.Edition}|{config.Label}|");
                    if (config.Antes != null)
                        sb.Append(string.Join(",", config.Antes));
                    sb.Append("|");
                }
            }

            // Compute SHA256 hash
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private ItemConfig ConvertClauseToItemConfig(
            MotelyJsonConfig.MotleyJsonFilterClause clause,
            string itemKey
        )
        {
            var normalizedType = NormalizeItemType(clause.Type);

            var itemConfig = new ItemConfig
            {
                ItemKey = itemKey,
                ItemType = normalizedType,
                ItemName = clause.Value ?? clause.Values?.FirstOrDefault() ?? "",
                Score = clause.Score,
                Label = clause.Label,
                Min = clause.Min,
                Edition = clause.Edition ?? "none",
                Antes = clause.Antes?.ToList(),
                Sources = clause.Sources,
                Stickers = clause.Stickers?.ToList(),
            };

            // Handle special types
            if (normalizedType == "SoulJoker")
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

            // Create a single VisualBuilderTabViewModel instance to share between the two new tabs
            var visualBuilderViewModel = new FilterTabs.VisualBuilderTabViewModel(this);
            VisualBuilderTab = visualBuilderViewModel; // Store reference for other components

            // Tab 1: Configure Filter (MUST and MUST NOT zones only)
            var configureFilterTab = new Components.FilterTabs.ConfigureFilterTab
            {
                DataContext = visualBuilderViewModel, // Share the same ViewModel!
            };
            TabItems.Add(new TabItemViewModel("CONFIGURE FILTER", configureFilterTab));

            // Tab 2: Configure Score (SHOULD items in a row-based UI)
            var configureScoreTab = new Components.FilterTabs.ConfigureScoreTab
            {
                DataContext = visualBuilderViewModel, // Share the same ViewModel!
            };
            TabItems.Add(new TabItemViewModel("CONFIGURE SCORE", configureScoreTab));

            // JSON Editor tab
            var jsonEditorViewModel = new FilterTabs.JsonEditorTabViewModel(this);
            var jsonEditorTab = new Components.FilterTabs.JsonEditorTab
            {
                DataContext = jsonEditorViewModel,
            };
            JsonEditorTab = jsonEditorViewModel; // Store reference

            TabItems.Add(new TabItemViewModel("JSON EDITOR", jsonEditorTab));

            // Save Filter tab
            var configService =
                ServiceHelper.GetService<IConfigurationService>() ?? new ConfigurationService();
            var filterService =
                ServiceHelper.GetService<IFilterService>() ?? new FilterService(configService);
            var userProfileService =
                ServiceHelper.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");
            var filterConfigService =
                ServiceHelper.GetService<IFilterConfigurationService>()
                ?? new FilterConfigurationService(userProfileService);
            var saveFilterViewModel = new FilterTabs.SaveFilterTabViewModel(
                this,
                configService,
                filterService,
                filterConfigService
            );
            var saveFilterTab = new Components.FilterTabs.SaveFilterTab
            {
                DataContext = saveFilterViewModel,
            };

            TabItems.Add(new TabItemViewModel("SAVE", saveFilterTab));

            // Ensure initial tab content and visibility are set
            // Order now: 0=Configure Filter, 1=Configure Score, 2=JSON Editor, 3=Save
            UpdateTabVisibility(SelectedTabIndex);
            OnPropertyChanged(nameof(CurrentTabContent));
        }

        private object CreateLoadTabContent()
        {
            // Use the new FilterSelectorControl (unified component for both modals)
            var filterSelector = new Components.FilterSelectorControl
            {
                // In FiltersModal, we are NOT in the Search context
                IsInSearchModal = false,
            };

            // Handle EDIT action: load config and switch to Visual Builder
            filterSelector.FilterEditRequested += async (sender, filterPath) =>
            {
                try
                {
                    DebugLogger.Log("FiltersModalViewModel", $"Editing filter from: {filterPath}");

                    var json = await System.IO.File.ReadAllTextAsync(filterPath);
                    var config =
                        System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                            json
                        );

                    if (config != null)
                    {
                        CurrentFilterPath = filterPath;

                        // CRITICAL: Load the config into state (populates SelectedMust/Should/MustNot)
                        LoadConfigIntoState(config);

                        // Update Visual Builder with FilterItem objects and expand zones
                        await UpdateVisualBuilderFromItemConfigs();
                        ExpandDropZonesWithItems();

                        // Update deck and stake selection indices
                        if (!string.IsNullOrEmpty(config.Deck))
                        {
                            var deckIndex = Array.IndexOf(
                                new[]
                                {
                                    "Red",
                                    "Blue",
                                    "Yellow",
                                    "Green",
                                    "Black",
                                    "Magic",
                                    "Nebula",
                                    "Ghost",
                                    "Abandoned",
                                    "Checkered",
                                    "Zodiac",
                                    "Painted",
                                    "Anaglyph",
                                    "Plasma",
                                    "Erratic",
                                },
                                config.Deck
                            );
                            if (deckIndex >= 0)
                                SelectedDeckIndex = deckIndex;
                        }

                        if (!string.IsNullOrEmpty(config.Stake))
                        {
                            var stakeIndex = Array.IndexOf(
                                new[]
                                {
                                    "white",
                                    "red",
                                    "green",
                                    "black",
                                    "blue",
                                    "purple",
                                    "orange",
                                    "gold",
                                },
                                config.Stake.ToLower()
                            );
                            if (stakeIndex >= 0)
                                SelectedStakeIndex = stakeIndex;
                        }

                        // Switch to Visual Builder tab
                        SelectedTabIndex = 1;
                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"Filter loaded for editing: {config.Name}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error loading filter for editing: {ex.Message}"
                    );
                }
            };

            // Handle COPY action: duplicate file, resolve name collisions, load copy for editing
            filterSelector.FilterCopyRequested += async (sender, originalPath) =>
            {
                try
                {
                    var directory =
                        System.IO.Path.GetDirectoryName(originalPath)
                        ?? Directory.GetCurrentDirectory();
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
                        config =
                            System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                                json
                            );
                    }
                    catch
                    {
                        // Failed to load/parse filter - will remain null and be skipped below
                    }

                    if (config != null)
                    {
                        // Update the config name to reflect copy and write new file
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                        };
                        config.Name = string.IsNullOrWhiteSpace(config.Name)
                            ? candidateName
                            : $"{config.Name} (copy)";
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
                        var newConfig =
                            System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                                newJson
                            );
                        if (newConfig != null)
                        {
                            LoadedConfig = newConfig;
                            CurrentFilterPath = newPath;

                            // Load config and update Visual Builder
                            LoadConfigIntoState(newConfig);
                            await UpdateVisualBuilderFromItemConfigs();
                            ExpandDropZonesWithItems();

                            // Preserve deck/stake selections from the copy
                            if (!string.IsNullOrEmpty(newConfig.Deck))
                            {
                                var deckIndex = Array.IndexOf(
                                    new[]
                                    {
                                        "Red",
                                        "Blue",
                                        "Yellow",
                                        "Green",
                                        "Black",
                                        "Magic",
                                        "Nebula",
                                        "Ghost",
                                        "Abandoned",
                                        "Checkered",
                                        "Zodiac",
                                        "Painted",
                                        "Anaglyph",
                                        "Plasma",
                                        "Erratic",
                                    },
                                    newConfig.Deck
                                );
                                if (deckIndex >= 0)
                                    SelectedDeckIndex = deckIndex;
                            }

                            if (!string.IsNullOrEmpty(newConfig.Stake))
                            {
                                var stakeIndex = Array.IndexOf(
                                    new[]
                                    {
                                        "white",
                                        "red",
                                        "green",
                                        "black",
                                        "blue",
                                        "purple",
                                        "orange",
                                        "gold",
                                    },
                                    newConfig.Stake.ToLower()
                                );
                                if (stakeIndex >= 0)
                                    SelectedStakeIndex = stakeIndex;
                            }

                            SelectedTabIndex = 1;
                            DebugLogger.Log(
                                "FiltersModalViewModel",
                                $"Created and loaded copy: {candidateName}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "FiltersModalViewModel",
                            $"Copy created but failed to load: {ex.Message}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error copying filter: {ex.Message}"
                    );
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
                    DebugLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error deleting filter: {ex.Message}"
                    );
                }
            };

            // Handle CREATE NEW FILTER
            filterSelector.NewFilterRequested += (sender, e) =>
            {
                try
                {
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        "Create New Filter requested from Load tab"
                    );
                    CreateNewFilter();
                    SelectedTabIndex = 1;
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        "Switched to Visual Builder for new filter"
                    );
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "FiltersModalViewModel",
                        $"Error handling Create New Filter: {ex.Message}"
                    );
                }
            };

            return filterSelector;
        }

        private string GetDeckName(int index)
        {
            var deckNames = new[]
            {
                "Red",
                "Blue",
                "Yellow",
                "Green",
                "Black",
                "Magic",
                "Nebula",
                "Ghost",
                "Abandoned",
                "Checkered",
                "Zodiac",
                "Painted",
                "Anaglyph",
                "Plasma",
                "Erratic",
            };
            return index >= 0 && index < deckNames.Length ? deckNames[index] : "Red";
        }

        private string GetStakeName(int index)
        {
            var stakeNames = new[]
            {
                "white",
                "red",
                "green",
                "black",
                "blue",
                "purple",
                "orange",
                "gold",
            };
            return index >= 0 && index < stakeNames.Length ? stakeNames[index] : "white";
        }

        /// <summary>
        /// Update JSON editor content from loaded config
        /// </summary>
        private void UpdateJsonEditorFromConfig(Motely.Filters.MotelyJsonConfig config)
        {
            try
            {
                // Find JSON Editor tab by checking Content property (which is the view, with DataContext as ViewModel)
                var jsonEditorTab = TabItems.FirstOrDefault(t =>
                    t.Content is Components.FilterTabs.JsonEditorTab);

                if (jsonEditorTab?.Content is Components.FilterTabs.JsonEditorTab jsonEditorView &&
                    jsonEditorView.DataContext is FilterTabs.JsonEditorTabViewModel jsonEditorVm)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                    jsonEditorVm.JsonContent = json;
                    DebugLogger.Log("FiltersModalViewModel", "Updated JSON editor from config");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error updating JSON editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Visual Builder FilterItem collections from ItemConfigs
        /// </summary>
        private async Task UpdateVisualBuilderFromItemConfigs()
        {
            try
            {
                if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
                {
                    // Clear existing items
                    visualVm.SelectedMust.Clear();
                    visualVm.SelectedShould.Clear();
                    visualVm.SelectedMustNot.Clear();

                    // Convert Must items
                    foreach (var itemKey in SelectedMust)
                    {
                        if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                        {
                            var filterItem = await ConvertItemConfigToFilterItem(itemConfig);
                            if (filterItem != null)
                            {
                                visualVm.SelectedMust.Add(filterItem);
                            }
                        }
                    }

                    // Convert Should items
                    foreach (var itemKey in SelectedShould)
                    {
                        if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                        {
                            var filterItem = await ConvertItemConfigToFilterItem(itemConfig);
                            if (filterItem != null)
                            {
                                visualVm.SelectedShould.Add(filterItem);
                            }
                        }
                    }

                    // Convert MustNot items
                    foreach (var itemKey in SelectedMustNot)
                    {
                        if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                        {
                            var filterItem = await ConvertItemConfigToFilterItem(itemConfig);
                            if (filterItem != null)
                            {
                                visualVm.SelectedMustNot.Add(filterItem);
                            }
                        }
                    }

                    DebugLogger.Log("FiltersModalViewModel", $"Updated Visual Builder: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should, {visualVm.SelectedMustNot.Count} mustNot");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error updating Visual Builder: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert ItemConfig to FilterItem for Visual Builder
        /// </summary>
        private Task<Models.FilterItem?> ConvertItemConfigToFilterItem(ItemConfig itemConfig)
        {
            try
            {
                var spriteService = SpriteService.Instance;

                var normalizedType = NormalizeItemType(itemConfig.ItemType);
                var effectiveType = itemConfig.IsSoulJoker ? "SoulJoker" : normalizedType;

                // Determine category for layout behavior
                var category = DetermineCategoryFromType(effectiveType);

                // Create FilterItem - ItemImage and SoulFaceImage are computed properties based on Type and Name
                var filterItem = new Models.FilterItem
                {
                    Name = itemConfig.ItemName,
                    Type = effectiveType,
                    Category = category,
                    ItemKey = itemConfig.ItemKey,
                    DisplayName = BalatroData.GetDisplayNameFromSprite(itemConfig.ItemName),
                    // Get appropriate sprite image based on type
                    ItemImage = effectiveType switch
                    {
                        "Joker" or "SoulJoker" => spriteService.GetJokerImage(itemConfig.ItemName),
                        "SmallBlindTag" or "BigBlindTag" => spriteService.GetTagImage(itemConfig.ItemName),
                        "Voucher" => spriteService.GetVoucherImage(itemConfig.ItemName),
                        "Tarot" => spriteService.GetTarotImage(itemConfig.ItemName),
                        "Planet" => spriteService.GetPlanetCardImage(itemConfig.ItemName),
                        "Spectral" => spriteService.GetSpectralImage(itemConfig.ItemName),
                        "Boss" => spriteService.GetBossImage(itemConfig.ItemName),
                        _ => null
                    }
                };

                return Task.FromResult<Models.FilterItem?>(filterItem);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error converting ItemConfig to FilterItem: {ex.Message}");
                return Task.FromResult<Models.FilterItem?>(null);
            }
        }

        /// <summary>
        /// Maps item type to category for CategoryGroupedLayoutBehavior
        /// </summary>
        private string DetermineCategoryFromType(string itemType)
        {
            var normalizedType = NormalizeItemType(itemType);

            return normalizedType switch
            {
                "Joker" or "SoulJoker" => "Jokers",
                "SmallBlindTag" or "BigBlindTag" => "Tags",
                "Boss" => "Bosses",
                "Voucher" => "Vouchers",
                "Tarot" or "Planet" or "Spectral" => "Consumables",
                _ => "Other"
            };
        }

        private static string NormalizeItemType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "Unknown";
            }

            var cleaned = new string(type.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                return "Unknown";
            }

            var lower = cleaned.ToLowerInvariant();

            return lower switch
            {
                "joker" => "Joker",
                "souljoker" => "SoulJoker",
                "smallblindtag" => "SmallBlindTag",
                "bigblindtag" => "BigBlindTag",
                "tag" => "SmallBlindTag",
                "boss" => "Boss",
                "voucher" => "Voucher",
                "tarot" => "Tarot",
                "planet" => "Planet",
                "spectral" => "Spectral",
                "operator" => "Operator",
                "playingcard" => "PlayingCard",
                "standardcard" => "StandardCard",
                _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower),
            };
        }

        /// <summary>
        /// CRITICAL: Expand drop zones that contain items after loading a filter.
        /// Without this, loaded items won't render because the ItemsControl IsVisible=false when collapsed.
        /// </summary>
        private void ExpandDropZonesWithItems()
        {
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                // Expand ALL zones that have items
                visualVm.IsMustExpanded = visualVm.SelectedMust.Count > 0;
                visualVm.IsShouldExpanded = visualVm.SelectedShould.Count > 0;
                visualVm.IsCantExpanded = visualVm.SelectedMustNot.Count > 0;
            }
        }
    }
}
