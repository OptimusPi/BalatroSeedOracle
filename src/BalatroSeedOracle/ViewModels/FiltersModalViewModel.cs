using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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

        // Track previous tab index to properly save data when switching tabs
        private int _previousTabIndex = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoadedFilter))]
        [NotifyCanExecuteChangedFor(nameof(DeleteFilterCommand))]
        private string? _currentFilterPath;

        [ObservableProperty]
        private MotelyJsonConfig? _loadedConfig;

        // Track original filter criteria hash to detect MUST/SHOULD/MUSTNOT changes
        private string? _originalCriteriaHash;

        // Track original metadata to preserve on save (prevent overwriting author/date)
        private DateTime? _originalDateCreated;
        private string? _originalAuthor;

        [ObservableProperty]
        private string _filterName = "";

        [ObservableProperty]
        private bool _filterNameEditMode = false;

        [ObservableProperty]
        private bool _filterNameDisplayMode = true;

        [ObservableProperty]
        private string _filterDescription = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedDeckIndex))]
        private Motely.MotelyDeck _selectedDeck = Motely.MotelyDeck.Red;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedStakeIndex))]
        private Motely.MotelyStake _selectedStake = Motely.MotelyStake.White;

        // Tab visibility properties - proper MVVM pattern
        [ObservableProperty]
        private bool _isLoadSaveTabVisible = true;

        [ObservableProperty]
        private bool _isVisualTabVisible = false;

        [ObservableProperty]
        private bool _isDeckStakeTabVisible = false;

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

        // Callbacks for child view models
        public Action<string>? RequestNavigateToSearch { get; set; }
        public Action? RequestClose { get; set; }

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
            get => (int)SelectedDeck;
            set
            {
                if (value >= 0 && value <= 14) // 0-14 for 15 decks
                {
                    SelectedDeck = (Motely.MotelyDeck)value;
                }
            }
        }

        public int SelectedStakeIndex
        {
            get
            {
                // Map enum to UI index (0-7)
                return SelectedStake switch
                {
                    Motely.MotelyStake.White => 0,
                    Motely.MotelyStake.Red => 1,
                    Motely.MotelyStake.Green => 2,
                    Motely.MotelyStake.Black => 3,
                    Motely.MotelyStake.Blue => 4,
                    Motely.MotelyStake.Purple => 5,
                    Motely.MotelyStake.Orange => 6,
                    Motely.MotelyStake.Gold => 7,
                    _ => 0,
                };
            }
            set
            {
                // Map UI index (0-7) to enum
                SelectedStake = value switch
                {
                    0 => Motely.MotelyStake.White,
                    1 => Motely.MotelyStake.Red,
                    2 => Motely.MotelyStake.Green,
                    3 => Motely.MotelyStake.Black,
                    4 => Motely.MotelyStake.Blue,
                    5 => Motely.MotelyStake.Purple,
                    6 => Motely.MotelyStake.Orange,
                    7 => Motely.MotelyStake.Gold,
                    _ => Motely.MotelyStake.White,
                };
            }
        }

        public Dictionary<string, List<string>> ItemCategories => _itemCategories;
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();

        // Tab ViewModels for cross-tab communication
        public object? VisualBuilderTab { get; set; }
        public object? DeckStakeTab { get; set; }
        public object? JamlEditorTab { get; set; }

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

        // ===== PARTIAL METHODS (Property change handlers) =====
        partial void OnFilterNameEditModeChanged(bool value)
        {
            FilterNameDisplayMode = !value;
        }

        partial void OnSelectedDeckChanged(Motely.MotelyDeck value)
        {
            DebugLogger.Log("FiltersModalViewModel", $"Deck changed to: {value}");
            if (JamlEditorTab is FilterTabs.JamlEditorTabViewModel jamlVm)
            {
                jamlVm.AutoGenerateFromVisual();
            }
        }

        partial void OnSelectedStakeChanged(Motely.MotelyStake value)
        {
            DebugLogger.Log("FiltersModalViewModel", $"Stake changed to: {value}");
            if (JamlEditorTab is FilterTabs.JamlEditorTabViewModel jamlVm)
            {
                jamlVm.AutoGenerateFromVisual();
            }
        }

        // ===== EVENTS =====
        /// <summary>
        /// Raised when filter name edit mode is activated (for focus request)
        /// </summary>
        public event EventHandler? OnFilterNameEditActivated;

        // ===== COMMANDS (using [RelayCommand] source generator) =====

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
                var criteriaChanged =
                    _originalCriteriaHash == null || currentHash != _originalCriteriaHash;

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
                var searchResultsDir = AppPaths.SearchResultsDir;

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
#if BROWSER
            // DuckDB not available in browser
            await Task.CompletedTask;
            return;
#else
            if (dbFiles == null || dbFiles.Length == 0)
            {
                DebugLogger.Log("FiltersModalViewModel", "No database files to dump");
                return;
            }

            try
            {
                // Ensure WordLists directory exists
                var wordListsDir = AppPaths.WordListsDir;

                var fertilizerPath = Path.Combine(wordListsDir, "fertilizer.txt");
                var allSeeds = new List<string>();

                // Collect seeds from all database files
                foreach (var dbFile in dbFiles)
                {
                    if (!File.Exists(dbFile))
                        continue;

                    try
                    {
                        using var connection = new DuckDB.NET.Data.DuckDBConnection(
                            $"Data Source={dbFile}"
                        );
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
#endif
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

        /// <summary>
        /// Loads a specific filter config for editing (called from other modals)
        /// </summary>
        /// <param name="config">The filter config to load</param>
        public async Task LoadFilterForEditing(MotelyJsonConfig config)
        {
            try
            {
                LoadedConfig = config;
                FilterName = config.Name ?? "Unnamed Filter";
                FilterDescription = config.Description ?? "";
                // Convert string deck/stake to enum values
                if (!string.IsNullOrEmpty(config.Deck) && Enum.TryParse<Motely.MotelyDeck>(config.Deck, true, out var deck))
                {
                    SelectedDeck = deck;
                }
                if (!string.IsNullOrEmpty(config.Stake) && Enum.TryParse<Motely.MotelyStake>(config.Stake, true, out var stake))
                {
                    SelectedStake = stake;
                }
                
                // Store original metadata
                _originalAuthor = config.Author;
                _originalDateCreated = config.DateCreated;
                
                // TODO: Calculate criteria hash for change detection if method exists
                // _originalCriteriaHash = CalculateCriteriaHash(config);
                
                // TODO: Load clauses into tabs when proper ViewModels are available
                // For now, just load basic info
                
                // Don't set CurrentFilterPath since this is editing, not loading from file
                CurrentFilterPath = null;
                
                DebugLogger.Log("FiltersModalViewModel", $"Loaded filter for editing: {config.Name}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModalViewModel", $"Error loading filter for editing: {ex.Message}");
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

        [RelayCommand]
        private void FilterNameClick()
        {
            FilterNameEditMode = true;
            OnFilterNameEditActivated?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SaveFilterName()
        {
            var newName = FilterName?.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                FilterName = newName;
                DebugLogger.Log("FiltersModalViewModel", $"Filter name updated to: {newName}");
            }
            FilterNameEditMode = false;
        }

        [RelayCommand]
        private void CancelFilterNameEdit()
        {
            // Restore original value from loaded config if available
            if (LoadedConfig != null && !string.IsNullOrEmpty(LoadedConfig.Name))
            {
                FilterName = LoadedConfig.Name;
            }
            FilterNameEditMode = false;
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
        public async Task ReloadVisualFromSavedFile()
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

                    // Update JAML editor with loaded content
                    if (JamlEditorTab is FilterTabs.JamlEditorTabViewModel jamlVm)
                    {
                        jamlVm.AutoGenerateFromVisual();
                    }

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
        /// <param name="tabIndex">0=Build Filter, 1=Deck/Stake, 2=JSON Editor, 3=JAML Editor, 4=Validate Filter</param>
        public void UpdateTabVisibility(int tabIndex)
        {
            DebugLogger.Log(
                "FiltersModalViewModel",
                $"UpdateTabVisibility called with tabIndex={tabIndex}"
            );

            // Hide all tabs first
            IsLoadSaveTabVisible = false;
            IsVisualTabVisible = false;
            IsDeckStakeTabVisible = false;
            IsJsonTabVisible = false;
            IsTestTabVisible = false;
            IsSaveTabVisible = false;

            // Show the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsVisualTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "Build Filter tab visible");
                    break;
                case 1:
                    IsDeckStakeTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "Deck/Stake tab visible");
                    break;
                case 2:
                    IsJsonTabVisible = true;
                    DebugLogger.Log("FiltersModalViewModel", "JSON Editor tab visible");
                    break;
                case 3:
                    DebugLogger.Log("FiltersModalViewModel", "JAML Editor tab visible");
                    break;
                case 4:
                    IsSaveTabVisible = true;
                    _ = RefreshSaveTabData(); // Fire-and-forget when switching tabs
                    DebugLogger.Log("FiltersModalViewModel", "Validate Filter tab visible");
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
        private async Task RefreshSaveTabData()
        {
            try
            {
                // Find the Validate Filter tab and refresh its data
                var validateTab = TabItems.FirstOrDefault(t =>
                    t.Content is Components.FilterTabs.ValidateFilterTab
                );
                if (
                    validateTab?.Content
                        is Components.FilterTabs.ValidateFilterTab validateFilterTab
                    && validateFilterTab.DataContext
                        is FilterTabs.ValidateFilterTabViewModel validateVm
                )
                {
                    validateVm.PreFillFilterData();
                    await validateVm.RefreshClauseDisplay();
                    DebugLogger.Log("FiltersModalViewModel", "Refreshed Validate Filter tab data");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error refreshing Save tab: {ex.Message}"
                );
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

        // Capture the OLD tab index BEFORE the property changes
        partial void OnSelectedTabIndexChanging(int value)
        {
            _previousTabIndex = SelectedTabIndex;

            // Tab order: 0=Build Filter, 1=Deck/Stake, 2=JAML Editor, 3=Validate Filter
            switch (_previousTabIndex)
            {
                case 2: // Leaving JAML Editor - parse JAML and update state
                    if (JamlEditorTab is FilterTabs.JamlEditorTabViewModel jamlVm)
                    {
                        SaveJamlEditorToState(jamlVm);
                    }
                    break;
            }
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            DebugLogger.Log("FiltersModalViewModel", $"Tab switch to {value}");

            UpdateTabVisibility(value);
            OnPropertyChanged(nameof(CurrentTabContent));

            // Tab order: 0=Build Filter, 1=Deck/Stake, 2=JAML Editor, 3=Validate Filter
            switch (value)
            {
                case 0: // Entering Visual Builder - refresh from state
                    if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
                    {
                        visualVm.LoadFromParentCollections();
                        ExpandDropZonesWithItems();
                    }
                    break;
                case 2: // Entering JAML Editor - regenerate from state
                    if (JamlEditorTab is FilterTabs.JamlEditorTabViewModel jamlVm)
                    {
                        jamlVm.AutoGenerateFromVisual();
                    }
                    break;
                case 3: // Entering Validate Filter - refresh clause display
                    _ = RefreshSaveTabData();
                    break;
            }
        }

        /// <summary>
        /// Parse JAML from JAML Editor and update parent ViewModel's state
        /// JAML (Joker Ante Markup Language) is a YAML-based format
        /// </summary>
        private void SaveJamlEditorToState(FilterTabs.JamlEditorTabViewModel jamlVm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jamlVm.JamlContent))
                {
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        "⚠️ JAML Editor is empty - skipping save"
                    );
                    return;
                }

                // Parse JAML to config using YamlDotNet (JAML is YAML-based)
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(
                        YamlDotNet
                            .Serialization
                            .NamingConventions
                            .CamelCaseNamingConvention
                            .Instance
                    )
                    .Build();

                var config = deserializer.Deserialize<MotelyJsonConfig>(jamlVm.JamlContent);

                if (config == null)
                {
                    DebugLogger.LogError(
                        "FiltersModalViewModel",
                        "❌ Failed to parse JAML - skipping save"
                    );
                    return;
                }

                // Load config into parent state (updates SelectedMust/Should/MustNot, deck, stake, etc.)
                LoadConfigIntoState(config);

                DebugLogger.Log(
                    "FiltersModalViewModel",
                    $"✅ JAML Editor saved to state: {config.Must?.Count ?? 0} must, {config.Should?.Count ?? 0} should, Deck={config.Deck}, Stake={config.Stake}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error parsing JAML: {ex.Message}"
                );
            }
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
                // BUG FIX: Preserve original DateCreated and Author when re-saving an existing filter
                DateCreated = _originalDateCreated ?? DateTime.Now,
                Author = _originalAuthor ?? author,
                // Use enum ToString() for JSON serialization
                Deck = SelectedDeck.ToString(),
                Stake = SelectedStake.ToString().ToLower(),
                Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
            };

            // CRITICAL FIX: Read from VisualBuilderTab's collections if available
            // The VisualBuilderTab has its own SelectedMust/Should/MustNot collections (FilterItem objects)
            DebugLogger.LogImportant(
                "FiltersModalViewModel",
                $"🔍 BuildConfig: VisualBuilderTab={VisualBuilderTab?.GetType().Name ?? "NULL"}"
            );

            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                DebugLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"✅ USING VisualBuilder PATH: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should"
                );

                // Build Must clauses directly from FilterItem objects (including FilterOperatorItems)
                foreach (var filterItem in visualVm.SelectedMust)
                {
                    DebugLogger.LogImportant(
                        "FiltersModalViewModel",
                        $"Processing MUST item: Name={filterItem.Name}, Type={filterItem.Type}, ActualType={filterItem.GetType().Name}"
                    );

                    var clause = ConvertFilterItemToClause(filterItem);
                    if (clause != null)
                    {
                        config.Must.Add(clause);
                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"Added clause: Type={clause.Type}, HasClauses={clause.Clauses != null}, ClausesCount={clause.Clauses?.Count ?? 0}"
                        );
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "FiltersModalViewModel",
                            $"Failed to convert {filterItem.Name} to clause!"
                        );
                    }
                }
            }
            else
            {
                // Fallback to parent's key-based collections
                foreach (var itemKey in SelectedMust)
                {
                    if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    {
                        var clause = ConvertItemConfigToClause(itemConfig);
                        if (clause != null)
                            config.Must.Add(clause);
                    }
                }
            }

            // Build Should clauses
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm2)
            {
                foreach (var filterItem in visualVm2.SelectedShould)
                {
                    var clause = ConvertFilterItemToClause(filterItem);
                    if (clause != null)
                        config.Should.Add(clause);
                }
            }
            else
            {
                foreach (var itemKey in SelectedShould)
                {
                    if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    {
                        var clause = ConvertItemConfigToClause(itemConfig);
                        if (clause != null)
                            config.Should.Add(clause);
                    }
                }
            }

            // Build MustNot clauses - handle both BannedItems operator and direct MustNot
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm3)
            {
                // Check for BannedItems operator in Must collection
                foreach (var item in visualVm3.SelectedMust)
                {
                    if (item is Models.FilterOperatorItem op && op.OperatorType == "BannedItems")
                    {
                        foreach (var child in op.Children)
                        {
                            var clause = ConvertFilterItemToClause(child);
                            if (clause != null)
                                config.MustNot.Add(clause);
                        }
                    }
                }
            }
            else
            {
                foreach (var itemKey in SelectedMustNot)
                {
                    if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                    {
                        var clause = ConvertItemConfigToClause(itemConfig);
                        if (clause != null)
                            config.MustNot.Add(clause);
                    }
                }
            }

            return config;
        }

        private MotelyJsonConfig.MotleyJsonFilterClause? ConvertItemConfigToClause(
            ItemConfig itemConfig
        )
        {
            // Handle AND/OR clause types with Children
            if (itemConfig.ItemType == "Operator" && !string.IsNullOrEmpty(itemConfig.OperatorType))
            {
                var operatorClause = new MotelyJsonConfig.MotleyJsonFilterClause
                {
                    Type = itemConfig.OperatorType.ToLowerInvariant(), // "or" or "and"
                    Score = itemConfig.Score,
                    Label = itemConfig.Label,
                    Clauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Add antes if configured
                if (itemConfig.Antes?.Any() == true)
                {
                    operatorClause.Antes = itemConfig.Antes.ToArray();
                }

                // Add Mode for OR clauses (Max) or AND clauses (Sum/default)
                if (!string.IsNullOrEmpty(itemConfig.Mode))
                {
                    operatorClause.Mode = itemConfig.Mode;
                }

                // Recursively convert child items to clauses
                if (itemConfig.Children?.Any() == true)
                {
                    foreach (var child in itemConfig.Children)
                    {
                        var childClause = ConvertItemConfigToClause(child);
                        if (childClause != null)
                        {
                            operatorClause.Clauses.Add(childClause);
                        }
                    }
                }

                return operatorClause;
            }

            // Regular item (not a clause operator)
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

        private MotelyJsonConfig.MotleyJsonFilterClause? ConvertFilterItemToClause(
            Models.FilterItem filterItem
        )
        {
            // Handle FilterOperatorItem (OR/AND clauses)
            if (filterItem is Models.FilterOperatorItem operatorItem)
            {
                // Skip BannedItems operator - it's handled separately
                if (operatorItem.OperatorType == "BannedItems")
                    return null;

                DebugLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"Converting FilterOperatorItem: Type={operatorItem.OperatorType}, Children={operatorItem.Children.Count}"
                );

                var operatorClause = new MotelyJsonConfig.MotleyJsonFilterClause
                {
                    Type = operatorItem.OperatorType.ToLowerInvariant(), // "or" or "and"
                    Label = operatorItem.DisplayName,
                    Clauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Recursively convert children
                foreach (var child in operatorItem.Children)
                {
                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        $"  Converting child: {child.Name} (Type={child.Type})"
                    );
                    var childClause = ConvertFilterItemToClause(child);
                    if (childClause != null)
                    {
                        operatorClause.Clauses.Add(childClause);
                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"  ✓ Added child clause: Type={childClause.Type}, Value={childClause.Value}"
                        );
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "FiltersModalViewModel",
                            $"  ✗ Failed to convert child: {child.Name}"
                        );
                    }
                }

                DebugLogger.LogImportant(
                    "FiltersModalViewModel",
                    $"✓ Created {operatorItem.OperatorType} operator with {operatorClause.Clauses.Count} clauses"
                );

                return operatorClause;
            }

            // Regular FilterItem
            // Convert wildcard names to Motely-compatible values
            string clauseValue = filterItem.Name;
            if (filterItem.Name.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                // SoulJoker wildcards ALWAYS use "Any" regardless of rarity
                if (filterItem.Type == "SoulJoker")
                {
                    clauseValue = "Any";
                }
                else
                {
                    // Regular joker wildcards: Wildcard_JokerLegendary → anylegendary
                    clauseValue = filterItem
                        .Name.Replace("Wildcard_Joker", "any", StringComparison.OrdinalIgnoreCase)
                        .ToLowerInvariant();
                }
            }

            var clause = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = filterItem.Type,
                Value = clauseValue,
            };

            // Add antes if configured
            if (filterItem.Antes?.Any() == true)
            {
                clause.Antes = filterItem.Antes.ToArray();
            }

            // Add edition if not default
            if (
                !string.IsNullOrEmpty(filterItem.Edition)
                && filterItem.Edition != "none"
                && filterItem.Edition != "None"
            )
            {
                clause.Edition = filterItem.Edition;
            }

            // Add stickers if configured
            if (filterItem.Stickers?.Any() == true)
            {
                clause.Stickers = filterItem.Stickers;
            }

            // Handle playing card specific properties
            if (filterItem.Type == "PlayingCard" || filterItem.Category == "PlayingCards")
            {
                if (!string.IsNullOrEmpty(filterItem.Seal) && filterItem.Seal != "None")
                    clause.Seal = filterItem.Seal;
                if (
                    !string.IsNullOrEmpty(filterItem.Enhancement)
                    && filterItem.Enhancement != "None"
                )
                    clause.Enhancement = filterItem.Enhancement;
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

            // BUG FIX: Store original metadata to preserve on save
            _originalDateCreated = config.DateCreated;
            _originalAuthor = config.Author;

            // Load deck and stake from JSON strings → parse to enums
            if (
                !string.IsNullOrEmpty(config.Deck)
                && Enum.TryParse<Motely.MotelyDeck>(config.Deck, true, out var deck)
            )
            {
                SelectedDeck = deck;
            }
            if (
                !string.IsNullOrEmpty(config.Stake)
                && Enum.TryParse<Motely.MotelyStake>(config.Stake, true, out var stake)
            )
            {
                SelectedStake = stake;
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

            // CRITICAL FIX: Sync Visual Builder tab collections from loaded data!
            // This populates the SelectedMust/Should/MustNot FilterItem collections
            if (VisualBuilderTab is FilterTabs.VisualBuilderTabViewModel visualVm)
            {
                visualVm.LoadFromParentCollections();
                DebugLogger.Log(
                    "FiltersModalViewModel",
                    "Synced Visual Builder tab from loaded config"
                );
            }
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

            // Handle AND/OR clause operators with Children
            if (
                (
                    normalizedType.Equals("and", StringComparison.OrdinalIgnoreCase)
                    || normalizedType.Equals("or", StringComparison.OrdinalIgnoreCase)
                )
                && clause.Clauses?.Count > 0
            )
            {
                var operatorConfig = new ItemConfig
                {
                    ItemKey = itemKey,
                    ItemType = "Operator", // CRITICAL FIX: Was "Clause", must be "Operator"!
                    ItemName = $"{normalizedType.ToUpper()} ({clause.Clauses.Count} items)",
                    OperatorType =
                        normalizedType.Substring(0, 1).ToUpper()
                        + normalizedType.Substring(1).ToLower(), // "And" or "Or"
                    Mode = clause.Mode,
                    Score = clause.Score,
                    Label = clause.Label,
                    Antes = clause.Antes?.ToList(),
                    Children = new List<ItemConfig>(),
                };

                // Recursively convert child clauses
                int childIndex = 0;
                foreach (var childClause in clause.Clauses)
                {
                    var childKey = $"{itemKey}_child_{++childIndex}";
                    var childConfig = ConvertClauseToItemConfig(childClause, childKey);
                    operatorConfig.Children.Add(childConfig);
                }

                return operatorConfig;
            }

            // Regular item (not a clause operator)
            // CRITICAL: Convert "Any" back to wildcard name for round-trip compatibility
            string itemName = clause.Value ?? clause.Values?.FirstOrDefault() ?? "";
            if (
                itemName.Equals("Any", StringComparison.OrdinalIgnoreCase)
                && normalizedType == "SoulJoker"
            )
            {
                // Reconstruct wildcard name based on edition
                if (!string.IsNullOrEmpty(clause.Edition) && clause.Edition != "none")
                {
                    // Any Negative/Polychrome/etc SoulJoker → Wildcard_JokerLegendary (assumed Legendary if has edition)
                    itemName = "Wildcard_JokerLegendary";
                }
                else
                {
                    // Any SoulJoker with no edition → generic wildcard
                    itemName = "Wildcard_JokerLegendary"; // Default to legendary wildcard
                }
            }

            var itemConfig = new ItemConfig
            {
                ItemKey = itemKey,
                ItemType = normalizedType,
                ItemName = itemName,
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

            // BUG FIX: Clear original metadata when starting a new filter
            _originalDateCreated = null;
            _originalAuthor = null;
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

            // Create the proper VisualBuilderTabViewModel for the visual builder
            var visualBuilderViewModel = new FilterTabs.VisualBuilderTabViewModel(this);
            VisualBuilderTab = visualBuilderViewModel; // Store reference for other components

            // Create the VISUAL BUILDER tab with the actual visual item shelf!
            var visualBuilderTab = new Components.FilterTabs.VisualBuilderTab
            {
                DataContext = visualBuilderViewModel,
            };

            // Wire up filter name edit activation event
            OnFilterNameEditActivated += (s, e) =>
            {
                var filterNameEdit = visualBuilderTab.FindControl<Avalonia.Controls.TextBox>(
                    "FilterNameEdit"
                );
                if (filterNameEdit != null)
                {
                    filterNameEdit.Focus();
                    filterNameEdit.SelectAll();
                }
            };

            TabItems.Add(new TabItemViewModel("BUILD FILTER", visualBuilderTab));

            // Deck/Stake tab (NEW - between Build Filter and JSON Editor)
            var deckStakeViewModel = new FilterTabs.DeckStakeTabViewModel(this);
            var deckStakeTab = new Components.FilterTabs.DeckStakeTab
            {
                DataContext = deckStakeViewModel,
            };
            DeckStakeTab = deckStakeViewModel;
            TabItems.Add(new TabItemViewModel("DECK/STAKE", deckStakeTab));

            // JAML Editor tab (JAML = Joker Ante Markup Language, a YAML-based format)
            var jamlEditorViewModel = new FilterTabs.JamlEditorTabViewModel(this);
            var jamlEditorTab = new Components.FilterTabs.JamlEditorTab
            {
                DataContext = jamlEditorViewModel,
            };
            JamlEditorTab = jamlEditorViewModel; // Store reference
            TabItems.Add(new TabItemViewModel("JAML EDITOR", jamlEditorTab));

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
            var validateFilterViewModel = new FilterTabs.ValidateFilterTabViewModel(
                this,
                configService,
                filterService,
                filterConfigService
            );
            var validateFilterTab = new Components.FilterTabs.ValidateFilterTab
            {
                DataContext = validateFilterViewModel,
            };

            // Initialize the ValidateFilterTab with current filter data
            validateFilterViewModel.PreFillFilterData();
            _ = validateFilterViewModel.RefreshClauseDisplay();

            TabItems.Add(new TabItemViewModel("VALIDATE FILTER", validateFilterTab));

            // Ensure initial tab content and visibility are set
            // Order: 0=Build Filter, 1=Deck/Stake, 2=JAML Editor, 3=Validate Filter
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
                        System.IO.Path.GetDirectoryName(originalPath) ?? AppPaths.FiltersDir;
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
                            DefaultIgnoreCondition = System
                                .Text
                                .Json
                                .Serialization
                                .JsonIgnoreCondition
                                .WhenWritingNull,
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

        // Convert index to deck name via enum
        private string GetDeckName(int index)
        {
            if (index >= 0 && index <= 14)
                return ((Motely.MotelyDeck)index).ToString();
            return "Red";
        }

        // Convert index to stake name via enum (handles gaps in enum values)
        private string GetStakeName(int index)
        {
            var stake = index switch
            {
                0 => Motely.MotelyStake.White,
                1 => Motely.MotelyStake.Red,
                2 => Motely.MotelyStake.Green,
                3 => Motely.MotelyStake.Black,
                4 => Motely.MotelyStake.Blue,
                5 => Motely.MotelyStake.Purple,
                6 => Motely.MotelyStake.Orange,
                7 => Motely.MotelyStake.Gold,
                _ => Motely.MotelyStake.White,
            };
            return stake.ToString().ToLower();
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
                    // Disable JSON auto-generation during filter load
                    visualVm.BeginFilterLoad();

                    // Clear existing items
                    visualVm.SelectedMust.Clear();
                    visualVm.SelectedShould.Clear();

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

                    // Convert MustNot items → BannedItems tray
                    if (SelectedMustNot != null && SelectedMustNot.Any())
                    {
                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"Creating BannedItems tray with {SelectedMustNot.Count} items"
                        );

                        var bannedItemsTray = new Models.FilterOperatorItem("BannedItems");

                        foreach (var itemKey in SelectedMustNot)
                        {
                            if (ItemConfigs.TryGetValue(itemKey, out var itemConfig))
                            {
                                var filterItem = await ConvertItemConfigToFilterItem(itemConfig);
                                if (filterItem != null)
                                {
                                    // Set IsInBannedItemsTray flag for debuffed overlay
                                    filterItem.IsInBannedItemsTray = true;
                                    bannedItemsTray.Children.Add(filterItem);
                                }
                            }
                        }

                        // Add BannedItems tray to MUST zone
                        visualVm.SelectedMust.Add(bannedItemsTray);

                        DebugLogger.Log(
                            "FiltersModalViewModel",
                            $"Added BannedItems tray with {bannedItemsTray.Children.Count} children to MUST zone"
                        );
                    }

                    DebugLogger.Log(
                        "FiltersModalViewModel",
                        $"Updated Visual Builder: {visualVm.SelectedMust.Count} must, {visualVm.SelectedShould.Count} should"
                    );

                    // Re-enable JSON auto-generation after filter load
                    visualVm.EndFilterLoad();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error updating Visual Builder: {ex.Message}"
                );
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
                        "Joker" or "SoulJoker" => spriteService.GetJokerImage(
                            itemConfig.ItemName,
                            itemConfig.Edition
                        ),
                        "SmallBlindTag" or "BigBlindTag" => spriteService.GetTagImage(
                            itemConfig.ItemName
                        ),
                        "Voucher" => spriteService.GetVoucherImage(itemConfig.ItemName),
                        "Tarot" => spriteService.GetTarotImage(itemConfig.ItemName),
                        "Planet" => spriteService.GetPlanetCardImage(itemConfig.ItemName),
                        "Spectral" => spriteService.GetSpectralImage(itemConfig.ItemName),
                        "Boss" => spriteService.GetBossImage(itemConfig.ItemName),
                        _ => null,
                    },
                };

                return Task.FromResult<Models.FilterItem?>(filterItem);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FiltersModalViewModel",
                    $"Error converting ItemConfig to FilterItem: {ex.Message}"
                );
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
                "StandardCard" or "PlayingCard" => "PlayingCards",
                _ => "Other",
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
                // MUST-NOT zone removed - no IsCantExpanded property
            }
        }
    }
}
