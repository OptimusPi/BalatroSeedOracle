using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views.Modals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely.Filters;

namespace BalatroSeedOracle.ViewModels
{
    // Search mode enum
    public enum SearchMode
    {
        AllSeeds = 0,
        SingleSeed = 1,
        WordList = 2,
        DbList = 3,
    }

    public partial class SearchModalViewModel
        : ObservableObject,
            IDisposable,
            BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly SearchManager _searchManager;
        private readonly CircularConsoleBuffer _consoleBuffer;
        private readonly UserProfileService _userProfileService;
        private readonly BalatroSeedOracle.Services.Storage.IAppDataStore _appDataStore;

        private SearchInstance? _searchInstance;
        private string _currentSearchId = string.Empty;

        public Views.BalatroMainMenu? MainMenu { get; set; }

        // Callback for CREATE NEW FILTER button (set by View)
        private Action? _newFilterRequestedAction;

        // Callback for EDIT FILTER button (set by View) - takes filter path
        private Action<string?>? _editFilterRequestedAction;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
        private bool _isSearching = false;

        [ObservableProperty]
        private Motely.Filters.MotelyJsonConfig? _loadedConfig;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private SearchProgress? _latestProgress;

        /// <summary>
        /// Optional: Active shader transition driven by search progress.
        /// When set, search progress (0-100%) will drive shader LERP between Start/End parameters.
        /// User can design custom transitions (e.g., dark red → bright blue as search progresses).
        /// </summary>
        public Models.VisualizerPresetTransition? ActiveSearchTransition { get; set; }

        // PROPER MVVM: Tab visibility controlled by ViewModel, not code-behind
        [ObservableProperty]
        private bool _isSettingsTabVisible = false;

        [ObservableProperty]
        private bool _isSearchTabVisible = false;

        [ObservableProperty]
        private bool _isResultsTabVisible = false;

        [ObservableProperty]
        private int _deckIndex = 0;

        [ObservableProperty]
        private int _stakeIndex = 0;

        [ObservableProperty]
        private int _lastKnownResultCount = 0;

        // UX: generic Balatro-styled info text for the Results tab panel
        [ObservableProperty]
        private string _panelText = "Tip: Results appear below. Use Export to save seeds.";

        [ObservableProperty]
        private string? _currentFilterPath; // CRITICAL: Store the path to the loaded filter!

        [ObservableProperty]
        private int _maxResults = 1000;

        [ObservableProperty]
        private int _timeoutSeconds = 300;

        [ObservableProperty]
        private string _deckSelection = "Red Deck";

        [ObservableProperty]
        private string _stakeSelection = "White";

        [ObservableProperty]
        private int _selectedDeckIndex = 0;

        [ObservableProperty]
        private int _selectedStakeIndex = 0;

        // Generate deck display values from BalatroData to ensure order matches
        public string[] DeckDisplayValues { get; } = BalatroData.Decks.Values.ToArray();

        // Generate stake display values from BalatroData (strip " Stake" suffix for display)
        public string[] StakeDisplayValues { get; } =
            BalatroData.Stakes.Values.Select(v => v.Replace(" Stake", "")).ToArray();

        [ObservableProperty]
        private string _selectedWordList = "None";

        [ObservableProperty]
        private ObservableCollection<string> _availableWordLists = new();

        [ObservableProperty]
        private string _selectedDbList = "None";

        [ObservableProperty]
        private ObservableCollection<string> _availableDbLists = new();

        // Search Mode Properties
        [ObservableProperty]
        private SearchMode _selectedSearchMode = SearchMode.AllSeeds;

        public string[] SearchModeDisplayValues { get; } =
            new[] { "All Seeds", "Single Seed", "Word List", "DB List" };

        [ObservableProperty]
        private string _seedInput = string.Empty;

        [ObservableProperty]
        private bool _continueFromLast = false;

        // Visibility properties for mode-specific controls
        [ObservableProperty]
        private bool _isSmartAutoMode = true;

        [ObservableProperty]
        private bool _isThreadsVisible = true;

        [ObservableProperty]
        private bool _isContinueVisible = true;

        [ObservableProperty]
        private bool _isSeedInputVisible = false;

        [ObservableProperty]
        private bool _isWordListVisible = false;

        [ObservableProperty]
        private bool _isDbListVisible = false;

        public bool CanMinimizeToDesktopVisible => _searchInstance != null && !string.IsNullOrEmpty(_currentSearchId);

        // WordList index properties for SpinnerControl binding
        [ObservableProperty]
        private int _selectedWordListIndex = 0;

        public int WordListMaxIndex => Math.Max(0, AvailableWordLists.Count - 1);

        // DBList index properties for SpinnerControl binding
        [ObservableProperty]
        private int _selectedDbListIndex = 0;

        public int DbListMaxIndex => Math.Max(0, AvailableDbLists.Count - 1);

        // Search parameters
        [ObservableProperty]
        private int _minScore = 0;

        [ObservableProperty]
        private bool _isDebugMode = false;

        [ObservableProperty]
        private string _debugSeed = string.Empty;

        // Console
        [ObservableProperty]
        private string _consoleText = "> Motely Search Console\n> Ready to search...\n";

        [ObservableProperty]
        private string _jsonValidationStatus = "JSON: Valid ✓";

        [ObservableProperty]
        private string _jsonValidationColor = "Green";

        // Stats properties
        [ObservableProperty]
        private double _progressPercent = 0.0;

        [ObservableProperty]
        private string _searchSpeed = "0 seeds/s";

        [ObservableProperty]
        private int _currentBatch = 0;

        [ObservableProperty]
        private int _maxBatch = 0;

        [ObservableProperty]
        private string _seedsProcessed = "0";

        [ObservableProperty]
        private string _timeElapsed = "00:00:00";

        [ObservableProperty]
        private string _estimatedTimeRemaining = "--:--:--";

        [ObservableProperty]
        private string _findRate = "0.00%";

        [ObservableProperty]
        private string _rarity = "--";

        // Search button dynamic properties - CRITICAL: State machine for PAUSE vs STOP
        public string CookButtonText
        {
            get
            {
                if (!IsSearching)
                    return ContinueFromLast ? "Resume Search" : "Start Search";

                // If Continue is enabled, show PAUSE (saves state)
                // If Continue is disabled, show STOP (doesn't save state)
                return ContinueFromLast ? "Pause Search" : "Stop Search";
            }
        }

        // Button color class - Blue when stopped, Yellow-Orange when running
        public string CookButtonClass => IsSearching ? "btn-warning" : "btn-blue";

        // Results filtering
        [ObservableProperty]
        private string _resultsFilterText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SearchResult> _filteredResults = new();

        public SearchModalViewModel(
            SearchManager searchManager,
            UserProfileService userProfileService,
            BalatroSeedOracle.Services.Storage.IAppDataStore appDataStore
        )
        {
            _searchManager = searchManager;
            _userProfileService = userProfileService;
            _appDataStore = appDataStore;
            _consoleBuffer = new CircularConsoleBuffer(1000);

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<Models.ConsoleMessage>();

            // Set default values
            ThreadCount = Environment.ProcessorCount / 2;
            // BatchSize is now hardcoded to 3 for optimal performance/responsiveness balance

            // Initialize dynamic tabs
            InitializeSearchTabs();

            // Load available wordlists
            LoadAvailableWordLists();

            // Load available DB lists
            LoadAvailableDbLists();

            // Initialize control visibility
            UpdateControlVisibility();

            // Events will be subscribed to individual SearchInstance when created
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentTabContent));
        }

        partial void OnIsSearchingChanged(bool value)
        {
            StopSearchCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CookButtonText));
            OnPropertyChanged(nameof(CookButtonClass));
        }

        partial void OnContinueFromLastChanged(bool value)
        {
            // Update button text when Continue checkbox changes
            OnPropertyChanged(nameof(CookButtonText));

            // If user just enabled Continue while search is NOT running, load saved progress
            if (value && !IsSearching)
            {
                LoadSavedProgressAsync().ConfigureAwait(false);
            }
        }

        partial void OnResultsFilterTextChanged(string value)
        {
            FilterResults();
        }

        #region Properties

        public object? CurrentTabContent =>
            SelectedTabIndex >= 0 && SelectedTabIndex < TabItems.Count
                ? TabItems[SelectedTabIndex].Content
                : null;

        public int ThreadCount { get; set; } = Environment.ProcessorCount;
        public int MaxThreadCount { get; } = Environment.ProcessorCount; // Auto-detect CPU cores

        // BatchSize set to 2 for better API responsiveness (35^2 = 1,225 seeds per batch)
        public int BatchSize => 2;

        public string SearchStatus => IsSearching ? "Searching..." : "Ready";
        public double SearchProgress => LatestProgress?.PercentComplete ?? 0.0;
        public string ProgressText => LatestProgress?.ToString() ?? "No search active";
        public int ResultsCount => _searchInstance?.ResultCount ?? SearchResults.Count;

        public string CurrentSearchId => _currentSearchId;

        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();
        public ObservableCollection<Models.SearchResult> SearchResults { get; }
        public ObservableCollection<Models.ConsoleMessage> ConsoleOutput { get; }

        #endregion

        #region Events

        public event EventHandler<string>? CreateShortcutRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? MaximizeToggleRequested;
        public event EventHandler<(
            string searchId,
            string? configPath,
            string filterName
        )>? MinimizeToDesktopRequested;

        #endregion

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanStartSearch))]
        private async Task StartSearchAsync()
        {
            try
            {
                AddConsoleMessage("Starting search...");

                if (LoadedConfig == null)
                {
                    AddConsoleMessage(
                        "No filter configuration loaded. Please load a filter first."
                    );
                    return;
                }

                AddConsoleMessage($"Filter loaded: {LoadedConfig.Name}");
                AddConsoleMessage(
                    $"Search mode: {SearchModeDisplayValues[(int)SelectedSearchMode]}"
                );

                // Validate mode-specific requirements
                if (
                    SelectedSearchMode == SearchMode.SingleSeed
                    && string.IsNullOrWhiteSpace(SeedInput)
                )
                {
                    AddConsoleMessage("Please enter a seed name for Single Seed mode.");
                    return;
                }

                if (
                    SelectedSearchMode == SearchMode.WordList
                    && (string.IsNullOrEmpty(SelectedWordList) || SelectedWordList == "None")
                )
                {
                    AddConsoleMessage("Please select a wordlist for Word List mode.");
                    return;
                }

                if (
                    SelectedSearchMode == SearchMode.DbList
                    && (string.IsNullOrEmpty(SelectedDbList) || SelectedDbList == "None")
                )
                {
                    AddConsoleMessage("Please select a DB list for DB List mode.");
                    return;
                }

                IsSearching = true;

                ClearResults();
                AddConsoleMessage(
                    $"Starting search in {SearchModeDisplayValues[(int)SelectedSearchMode]} mode..."
                );
                PanelText = $"Searching with '{LoadedConfig.Name}'...";

                AddConsoleMessage($"Building search criteria...");
                var searchCriteria = BuildSearchCriteria();

                // Apply SMART Auto Mode overrides
                if (IsSmartAutoMode && (SelectedSearchMode == SearchMode.AllSeeds || SelectedSearchMode == SearchMode.WordList))
                {
                    searchCriteria.ThreadCount = MaxThreadCount; // Use all cores
                    AddConsoleMessage($"SMART Auto Mode: Optimized for {MaxThreadCount} threads");
                }

                AddConsoleMessage($"Filter path: {searchCriteria.ConfigPath}");
                AddConsoleMessage($"Thread count: {searchCriteria.ThreadCount}");
                AddConsoleMessage($"Batch size: {searchCriteria.BatchSize}");

                AddConsoleMessage($"Creating search instance...");
                _searchInstance = await _searchManager.StartSearchAsync(
                    searchCriteria,
                    LoadedConfig
                );
                AddConsoleMessage($"Search instance created successfully!");

                // CRITICAL: Get the ACTUAL search ID from the SearchInstance, not a random GUID!
                _currentSearchId = _searchInstance.SearchId;

                // Subscribe to SearchInstance events directly
                _searchInstance.SearchCompleted += OnSearchCompleted;
                _searchInstance.ProgressUpdated += OnProgressUpdated;

                // CRITICAL FIX: Add immediate feedback that search is starting
                AddConsoleMessage($"Search started with ID: {_currentSearchId}");
                AddConsoleMessage($"Monitoring progress updates...");

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Search started with ID: {_currentSearchId}"
                );

                // Configure search transition (if enabled by user)
                ConfigureSearchTransition();
            }
            catch (Exception ex)
            {
                IsSearching = false;
                AddConsoleMessage($"Error starting search: {ex.Message}");
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Error starting search: {ex.Message}"
                );
            }
        }

        private bool CanStartSearch()
        {
            return !IsSearching && LoadedConfig != null;
        }

        [RelayCommand(CanExecute = nameof(CanStopSearch))]
        private void StopSearch()
        {
            try
            {
                if (_searchInstance != null)
                {
                    // CRITICAL: Different behavior based on Continue checkbox state
                    bool shouldSaveState = ContinueFromLast;

                    if (shouldSaveState)
                    {
                        // PAUSE mode: Save current batch position to DuckDB
                        AddConsoleMessage("Pausing search and saving progress...");
                        DebugLogger.Log("SearchModalViewModel", "Pausing search (saving state)");

                        // The SearchInstance already saves state periodically in OnProgressUpdated
                        // We just need to stop gracefully without clearing the database
                        _searchInstance.StopSearch();
                    }
                    else
                    {
                        // STOP mode: Don't save state, just stop
                        AddConsoleMessage("Stopping search (progress will NOT be saved)...");
                        DebugLogger.Log(
                            "SearchModalViewModel",
                            "Stopping search (NOT saving state)"
                        );

                        // Stop without saving
                        _searchInstance.StopSearch();
                    }

                    // Unsubscribe from events
                    _searchInstance.SearchStarted -= OnSearchStarted;
                    _searchInstance.SearchCompleted -= OnSearchCompleted;
                    _searchInstance.ProgressUpdated -= OnProgressUpdated;

                    // Dispose and clear the instance
                    _searchInstance.Dispose();
                    _searchInstance = null;

                    var actionText = shouldSaveState ? "paused" : "stopped";
                    AddConsoleMessage($"Search {actionText} by user.");
                }

                IsSearching = false;

                // Clear search transition when search stops
                ActiveSearchTransition = null;
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"Error stopping search: {ex.Message}");
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Error stopping search: {ex.Message}"
                );
            }
        }

        private bool CanStopSearch()
        {
            return IsSearching;
        }

        [RelayCommand(CanExecute = nameof(CanMinimizeToDesktop))]
        private void MinimizeToDesktop()
        {
            try
            {
                if (_searchInstance == null || string.IsNullOrEmpty(_currentSearchId))
                {
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        "Cannot minimize - no active search instance"
                    );
                    return;
                }

                var filterName = LoadedConfig?.Name ?? "Unknown Filter";

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Minimizing search to desktop: SearchID={_currentSearchId}, Filter={filterName}, ConfigPath={CurrentFilterPath}"
                );

                // Raise event for View to handle (creates SearchDesktopIcon and closes modal)
                MinimizeToDesktopRequested?.Invoke(
                    this,
                    (_currentSearchId, CurrentFilterPath, filterName)
                );

                AddConsoleMessage($"Search '{filterName}' minimized to desktop widget");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Error minimizing search: {ex.Message}"
                );
                AddConsoleMessage($"Error minimizing search: {ex.Message}");
            }
        }

        private bool CanMinimizeToDesktop()
        {
            // Can minimize if a search is running or paused
            return _searchInstance != null && !string.IsNullOrEmpty(_currentSearchId);
        }

        [RelayCommand]
        private void ClearResults()
        {
            SearchResults.Clear();
            ConsoleOutput.Clear();
            _consoleBuffer.Clear();
            LastKnownResultCount = 0;
            LatestProgress = null;
            PanelText = "Tip: Results appear below. Use Export to save seeds.";
            DebugLogger.Log("SearchModalViewModel", "Results cleared");
        }

        [RelayCommand]
        private Task LoadFilterAsync()
        {
            try
            {
                // This would typically show a file dialog
                // For now, we'll load from the temp location
                AddConsoleMessage("Filter loading functionality needs UI implementation");
                DebugLogger.Log("SearchModalViewModel", "Load filter requested");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"Error loading filter: {ex.Message}");
                DebugLogger.LogError("SearchModalViewModel", $"Error loading filter: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        partial void OnSelectedDeckIndexChanged(int value)
        {
            if (value >= 0 && value < DeckDisplayValues.Length)
            {
                DeckSelection = DeckDisplayValues[value].Replace(" Deck", "");
            }
        }

        partial void OnSelectedStakeIndexChanged(int value)
        {
            if (value >= 0 && value < StakeDisplayValues.Length)
            {
                StakeSelection = StakeDisplayValues[value];
            }
        }

        partial void OnSelectedSearchModeChanged(SearchMode value)
        {
            UpdateControlVisibility();

            // Force thread count for Single Seed mode (BatchSize is hardcoded to 3)
            if (value == SearchMode.SingleSeed)
            {
                ThreadCount = 1;
            }
        }

        partial void OnSelectedWordListIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableWordLists.Count)
            {
                SelectedWordList = AvailableWordLists[value];
            }
        }

        partial void OnSelectedDbListIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableDbLists.Count)
            {
                SelectedDbList = AvailableDbLists[value];
            }
        }

        private void UpdateControlVisibility()
        {
            switch (SelectedSearchMode)
            {
                case SearchMode.AllSeeds:
                    IsThreadsVisible = true;
                    IsContinueVisible = true;
                    IsSeedInputVisible = false;
                    IsWordListVisible = false;
                    IsDbListVisible = false;
                    break;

                case SearchMode.SingleSeed:
                    IsThreadsVisible = false;
                    IsContinueVisible = false;
                    IsSeedInputVisible = true;
                    IsWordListVisible = false;
                    IsDbListVisible = false;
                    break;

                case SearchMode.WordList:
                    IsThreadsVisible = true;
                    IsContinueVisible = false; // Wordlists don't support continue
                    IsSeedInputVisible = false;
                    IsWordListVisible = true;
                    IsDbListVisible = false;
                    break;

                case SearchMode.DbList:
                    IsThreadsVisible = false; // DB queries don't need threads
                    IsContinueVisible = false; // DB queries don't support continue
                    IsSeedInputVisible = false;
                    IsWordListVisible = false;
                    IsDbListVisible = true;
                    break;
            }

            // Notify property changed for WordListMaxIndex and DbListMaxIndex
            OnPropertyChanged(nameof(WordListMaxIndex));
            OnPropertyChanged(nameof(DbListMaxIndex));
        }

        public async Task LoadFilterAsync(string configPath)
        {
            try
            {
                await LoadConfigFromPathAsync(configPath);
                PanelText =
                    $"Filter loaded: {LoadedConfig?.Name ?? System.IO.Path.GetFileNameWithoutExtension(configPath)}";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Error loading filter from path: {ex.Message}"
                );
            }
        }

        private void SwitchTab(string? tabName)
        {
            if (!string.IsNullOrEmpty(tabName))
            {
                // CurrentActiveTab removed - using proper TabControl SelectedIndex binding
                DebugLogger.Log("SearchModalViewModel", $"Switched to tab: {tabName}");
            }
        }

        [RelayCommand]
        private void CreateShortcut(string? searchId)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                CreateShortcutRequested?.Invoke(this, searchId);
            }
        }

        [RelayCommand]
        private void Close()
        {
            DebugLogger.Log("SearchModalViewModel", "Closing modal");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Maximize()
        {
            // Request maximize toggle - the View will handle finding the window
            MaximizeToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SelectTab(object? parameter)
        {
            if (parameter is int tabIndex)
            {
                SelectedTabIndex = tabIndex;
                UpdateTabVisibility(tabIndex);
            }
            else if (
                parameter is string tabIndexStr
                && int.TryParse(tabIndexStr, out int parsedIndex)
            )
            {
                SelectedTabIndex = parsedIndex;
                UpdateTabVisibility(parsedIndex);
            }
        }

        /// <summary>
        /// Set callback for CREATE NEW FILTER button (called from View)
        /// </summary>
        public void SetNewFilterRequestedCallback(Action callback)
        {
            DebugLogger.Log("SearchModalViewModel", "SetNewFilterRequestedCallback called");
            _newFilterRequestedAction = callback;
        }

        /// <summary>
        /// Set callback for EDIT FILTER button (called from View)
        /// </summary>
        public void SetEditFilterRequestedCallback(Action<string?> callback)
        {
            _editFilterRequestedAction = callback;
        }

        /// <summary>
        /// Handle CREATE NEW FILTER request from FilterSelector
        /// </summary>
        public void OnNewFilterRequested()
        {
            DebugLogger.Log("SearchModalViewModel", "OnNewFilterRequested called");
            if (_newFilterRequestedAction != null)
            {
                DebugLogger.Log("SearchModalViewModel", "Invoking new filter requested callback");
                _newFilterRequestedAction.Invoke();
            }
            else
            {
                DebugLogger.LogError("SearchModalViewModel", "New filter requested action is null!");
            }
        }

        /// <summary>
        /// PROPER MVVM: Update tab visibility when tab selection changes
        /// Ensures only ONE tab is visible at a time
        /// </summary>
        public void UpdateTabVisibility(int tabIndex)
        {
            // Hide ALL tabs first
            IsSettingsTabVisible = false;
            IsSearchTabVisible = false;
            IsResultsTabVisible = false;

            // Show only the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsSettingsTabVisible = true;
                    break;
                case 1:
                    IsSearchTabVisible = true;
                    break;
                case 2:
                    IsResultsTabVisible = true;
                    break;
            }

            DebugLogger.Log("SearchModalViewModel", $"Switched to tab {tabIndex}");
        }

        /// <summary>
        /// Implements in-modal back navigation for progressive/tabbed flow.
        /// Returns true if navigation occurred; false to signal modal should close.
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

        [RelayCommand]
        private void EnterVibeOutMode()
        {
            // Feature removed
        }

        [RelayCommand]
        private async Task ToggleSearchAsync()
        {
            if (IsSearching)
            {
                StopSearch();
            }
            else
            {
                await StartSearchAsync();
            }
        }

        [RelayCommand]
        private void ClearConsole()
        {
            ConsoleText = "> Motely Search Console\n> Ready to search...\n";
            ConsoleOutput.Clear();
            _consoleBuffer.Clear();
            DebugLogger.Log("SearchModalViewModel", "Console cleared");
        }

        [RelayCommand]
        private void RefreshResults()
        {
            FilterResults();
            DebugLogger.Log("SearchModalViewModel", "Results refreshed");
        }

        [RelayCommand]
        private void SortBySeed()
        {
            var sorted = FilteredResults.OrderBy(r => r.Seed).ToList();
            FilteredResults.Clear();
            foreach (var result in sorted)
            {
                FilteredResults.Add(result);
            }
            DebugLogger.Log("SearchModalViewModel", "Results sorted by seed");
        }

        [RelayCommand]
        private void SortByScore()
        {
            var sorted = FilteredResults.OrderByDescending(r => r.TotalScore).ToList();
            FilteredResults.Clear();
            foreach (var result in sorted)
            {
                FilteredResults.Add(result);
            }
            DebugLogger.Log("SearchModalViewModel", "Results sorted by score");
        }

        [RelayCommand]
        private async Task CopySeedsAsync()
        {
            try
            {
                var seeds = string.Join("\n", FilteredResults.Select(r => r.Seed));
                if (!string.IsNullOrEmpty(seeds) && MainMenu != null)
                {
                    var clipboard = TopLevel.GetTopLevel(MainMenu)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(seeds);
                        AddConsoleMessage($"Copied {FilteredResults.Count} seeds to clipboard");
                        DebugLogger.Log(
                            "SearchModalViewModel",
                            $"Copied {FilteredResults.Count} seeds to clipboard"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to copy seeds: {ex.Message}");
            }
        }

        private void FilterResults()
        {
            FilteredResults.Clear();

            if (string.IsNullOrWhiteSpace(ResultsFilterText))
            {
                // No filter - show all results
                foreach (var result in SearchResults)
                {
                    FilteredResults.Add(result);
                }
            }
            else
            {
                // Filter by seed name
                var filter = ResultsFilterText.ToLowerInvariant();
                foreach (
                    var result in SearchResults.Where(r =>
                        r.Seed.ToLowerInvariant().Contains(filter)
                    )
                )
                {
                    FilteredResults.Add(result);
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                IsSearching = false;

                // CRITICAL FIX: Load results from DuckDB into ObservableCollection
                await LoadExistingResults();

                AddConsoleMessage($"Search completed. Found {ResultsCount} results.");
                PanelText = $"Search complete: {ResultsCount} seeds";
                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Search completed with {ResultsCount} results"
                );
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the unique search ID that includes filter name, deck, and stake.
        /// This ensures different deck/stake combinations have separate result databases.
        /// </summary>
        private string GetSearchId()
        {
            if (string.IsNullOrEmpty(CurrentFilterPath))
                return string.Empty;

            var filterName = System.IO.Path.GetFileNameWithoutExtension(CurrentFilterPath);
            var normalizedFilterName = filterName?.Replace(" ", "_") ?? "unknown";

            // Use the current deck/stake selection, defaulting to "Red" and "White"
            var deck = string.IsNullOrEmpty(DeckSelection) || DeckSelection == "All Decks"
                ? "Red"
                : DeckSelection.Replace(" Deck", "");
            var stake = string.IsNullOrEmpty(StakeSelection) || StakeSelection == "All Stakes"
                ? "White"
                : StakeSelection;

            return $"{normalizedFilterName}_{deck}_{stake}";
        }

        /// <summary>
        /// Gets the database path for the current filter/deck/stake combination.
        /// </summary>
        private string GetDatabasePath()
        {
            var searchId = GetSearchId();
            if (string.IsNullOrEmpty(searchId))
                return string.Empty;

            var searchResultsDir = AppPaths.SearchResultsDir;
            return System.IO.Path.Combine(searchResultsDir, $"{searchId}.db");
        }

        private SearchCriteria BuildSearchCriteria()
        {
            DebugLogger.LogImportant("SearchModalViewModel", $"🔍 BuildSearchCriteria - CurrentFilterPath value: '{CurrentFilterPath}'");
            DebugLogger.LogImportant("SearchModalViewModel", $"🔍 BuildSearchCriteria - LoadedConfig: {(LoadedConfig != null ? LoadedConfig.Name : "NULL")}");

            if (string.IsNullOrEmpty(CurrentFilterPath))
            {
                DebugLogger.LogError("SearchModalViewModel", "❌ CurrentFilterPath is NULL or EMPTY in BuildSearchCriteria!");
                throw new InvalidOperationException(
                    "No filter path available - filter must be loaded first!"
                );
            }

            DebugLogger.Log("SearchModalViewModel", $"✅ Using CurrentFilterPath: {CurrentFilterPath}");
            var criteria = new SearchCriteria
            {
                ConfigPath = CurrentFilterPath,
                Deck = DeckSelection == "All Decks" ? null : DeckSelection,
                Stake = StakeSelection == "All Stakes" ? null : StakeSelection,
                MinScore = MinScore,
            };

            // Build CLI arguments based on search mode
            switch (SelectedSearchMode)
            {
                case SearchMode.AllSeeds:
                    // Normal sequential search
                    criteria.ThreadCount = ThreadCount;
                    criteria.BatchSize = BatchSize;

                    // Handle Continue feature
                    if (ContinueFromLast && !string.IsNullOrEmpty(CurrentFilterPath))
                    {
                        // Use helper method that includes deck/stake in the database path
                        var dbPath = GetDatabasePath();

                        AddConsoleMessage($"Checking for saved state at: {dbPath}");
                        var savedState = Services.SearchStateManager.LoadSearchState(dbPath);
                        if (savedState != null)
                        {
                            ulong resumeBatch = (ulong)savedState.LastCompletedBatch;

                            // INTENTIONALLY REMOVED: Batch size conversion logic
                            // The batch size is now HARDCODED to 3 for optimal performance
                            // This eliminates the pitfall where startBatch calculation breaks if batch size changes
                            // If the user somehow has a saved state with a different batch size, we ignore it
                            // and start fresh (better than corrupting the search state)
                            if (savedState.BatchSize != BatchSize)
                            {
                                DebugLogger.LogError(
                                    "SearchModalViewModel",
                                    $"Saved state has different batch size ({savedState.BatchSize} vs {BatchSize}). "
                                        + "Batch size is now hardcoded to 3. Ignoring saved state and starting fresh."
                                );
                                AddConsoleMessage(
                                    $"WARNING: Saved state has incompatible batch size. Starting fresh search."
                                );
                                criteria.StartBatch = 0;
                            }
                            else
                            {
                                criteria.StartBatch = (ulong)(resumeBatch + 1); // +1 to start AFTER last completed
                                AddConsoleMessage($"Resuming from batch {resumeBatch + 1}");
                            }
                        }
                        else
                        {
                            AddConsoleMessage($"No saved state found - starting from batch 0");
                        }
                    }
                    break;

                case SearchMode.SingleSeed:
                    // Single seed mode - use direct Motely C# API
                    criteria.DebugSeed = SeedInput;
                    criteria.ThreadCount = 1;
                    criteria.BatchSize = 1;
                    break;

                case SearchMode.WordList:
                    // Wordlist filtering mode
                    // Remove .txt extension for CLI
                    var listName = Path.GetFileNameWithoutExtension(SelectedWordList);
                    criteria.WordList = listName;
                    criteria.ThreadCount = ThreadCount;
                    criteria.BatchSize = BatchSize;
                    break;

                case SearchMode.DbList:
                    // DB List mode - query pre-computed DuckDB databases
                    var dbFileName = SelectedDbList;
                    criteria.DbList = dbFileName;
                    criteria.ThreadCount = 1; // DB queries don't use threads
                    criteria.BatchSize = 1;
                    break;
            }

            return criteria;
        }

        private void AddConsoleMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";

            _consoleBuffer.AddLine(formattedMessage);

            var consoleMessage = new Models.ConsoleMessage
            {
                Text = formattedMessage,
                CopyableText = null, // No copy button for regular messages
            };
            ConsoleOutput.Add(consoleMessage);

            // Update the ConsoleText binding
            ConsoleText += formattedMessage + "\n";

            // Keep console output manageable
            while (ConsoleOutput.Count > 1000)
            {
                ConsoleOutput.RemoveAt(0);
            }
        }

        /// <summary>
        /// Adds a seed found message to the console with a copy button
        /// </summary>
        private void AddSeedFoundMessage(string seed, int score)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] Found seed: {seed} (Score: {score})";

            _consoleBuffer.AddLine(formattedMessage);

            var consoleMessage = new Models.ConsoleMessage
            {
                Text = formattedMessage,
                CopyableText = seed, // Copy button will copy just the seed name
                CopyCommand = new RelayCommand(async () =>
                {
                    // Copy seed to clipboard using ClipboardService
                    await Services.ClipboardService.CopyToClipboardAsync(seed);
                    AddConsoleMessage($"Copied '{seed}' to clipboard");
                }),
            };
            ConsoleOutput.Add(consoleMessage);

            // Update the ConsoleText binding
            ConsoleText += formattedMessage + "\n";

            // Keep console output manageable
            while (ConsoleOutput.Count > 1000)
            {
                ConsoleOutput.RemoveAt(0);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_searchInstance != null)
            {
                _searchInstance.SearchStarted -= OnSearchStarted;
                _searchInstance.SearchCompleted -= OnSearchCompleted;
                _searchInstance.ProgressUpdated -= OnProgressUpdated;
                _searchInstance.Dispose();
            }
            // CircularConsoleBuffer doesn't need disposal
        }

        [RelayCommand(CanExecute = nameof(CanPauseSearch))]
        private void PauseSearch()
        {
            if (_searchInstance != null && IsSearching)
            {
                _searchInstance.PauseSearch();
            }
        }

        private bool CanPauseSearch() => IsSearching;

        [RelayCommand(CanExecute = nameof(CanExportResults))]
        private async Task ExportResults()
        {
            try
            {
                if (SearchResults.Count == 0)
                {
                    DebugLogger.Log("SearchModalViewModel", "No results to export");
                    return;
                }

                // Create export text
                var exportText = $"Balatro Seed Search Results\n";
                exportText += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                exportText += $"Filter: {LoadedConfig?.Name ?? "Unknown"}\n";
                exportText += $"Total Results: {SearchResults.Count}\n";
                exportText += new string('=', 50) + "\n\n";

                foreach (var result in SearchResults)
                {
                    exportText += $"Seed: {result.Seed}\n";
                    exportText += $"Score: {result.TotalScore}\n";
                    if (result.Scores != null && result.Scores.Length > 0)
                    {
                        exportText += $"Scores: {string.Join(", ", result.Scores)}\n";
                    }
                    exportText += "\n";
                }

                // Save to file
                var fileName = $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    fileName
                );

                await System.IO.File.WriteAllTextAsync(filePath, exportText);
                DebugLogger.Log("SearchModalViewModel", $"Results exported to: {filePath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Export failed: {ex.Message}");
            }
        }

        private bool CanExportResults() => SearchResults.Count > 0;

        /// <summary>
        /// Connect to an existing search instance (for resuming searches)
        /// FIXES SEARCH RESUME BUG: Properly reconnect to running search with stats
        /// </summary>
        public async Task ConnectToExistingSearch(string searchId)
        {
            try
            {
                _currentSearchId = searchId;
                _searchInstance = _searchManager.GetSearch(searchId);

                if (_searchInstance != null)
                {
                    // Subscribe to search events for live updates
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.ProgressUpdated += OnProgressUpdated;

                    // CRITICAL FIX: Set CurrentFilterPath from search instance's ConfigPath
                    // This ensures state save/resume features work after reconnecting
                    if (!string.IsNullOrEmpty(_searchInstance.ConfigPath))
                    {
                        CurrentFilterPath = _searchInstance.ConfigPath;
                        DebugLogger.Log(
                            "SearchModalViewModel",
                            $"✅ CurrentFilterPath restored from search instance: {CurrentFilterPath}"
                        );

                        // Also load the config to restore LoadedConfig, deck/stake, etc.
                        await LoadConfigFromPathAsync(_searchInstance.ConfigPath);
                    }

                    // CRITICAL: Update UI state from existing search
                    IsSearching = _searchInstance?.IsRunning ?? false;

                    // CRITICAL: Load existing results to show current progress
                    await LoadExistingResults();

                    // CRITICAL: Get current search progress/stats
                    RefreshSearchStats();

                    // Switch to Results tab to show the reconnected search
                    SelectedTabIndex = 1; // Results tab (0=Search, 1=Results)

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Successfully reconnected to search: {searchId}, Running: {_searchInstance?.IsRunning ?? false}, Results: {SearchResults.Count}"
                    );
                    
                    OnPropertyChanged(nameof(CanMinimizeToDesktopVisible));
                }
                else
                {
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        $"Search instance not found: {searchId}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to connect to existing search: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Load existing results from the search instance
        /// </summary>
        private async Task LoadExistingResults()
        {
            if (_searchInstance == null)
                return;

            try
            {
                SearchResults.Clear();

                // Load results from the search instance using async API
                var existingResults = await _searchInstance.GetResultsPageAsync(0, 1000);
                if (existingResults != null)
                {
                    // Inject tally labels from SearchInstance column names (seed, score, then tallies)
                    var labels =
                        _searchInstance.ColumnNames.Count > 2
                            ? _searchInstance.ColumnNames.Skip(2).ToArray()
                            : Array.Empty<string>();

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Loading {existingResults.Count} results with {labels.Length} tally columns"
                    );

                    int idx = 0;
                    foreach (var result in existingResults)
                    {
                        // Set labels only on the first result to drive grid headers
                        if (idx == 0 && labels.Length > 0)
                        {
                            result.Labels = labels;
                        }
                        SearchResults.Add(result);
                        idx++;
                    }

                    // CRITICAL FIX: Force grid to reinitialize columns after loading results
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            var resultsTab = TabItems.FirstOrDefault(t => t.Header == "RESULTS");
                            if (resultsTab?.Content is Views.SearchModalTabs.ResultsTab tab)
                            {
                                var grid =
                                    tab.FindControl<BalatroSeedOracle.Controls.SortableResultsGrid>(
                                        "ResultsGrid"
                                    );
                                if (grid != null)
                                {
                                    // Force column reinitialization
                                    grid.ClearResults();
                                    grid.AddResults(SearchResults);
                                    DebugLogger.Log(
                                        "SearchModalViewModel",
                                        "Forced grid refresh after loading results"
                                    );
                                }
                            }
                        }
                        catch (Exception gridEx)
                        {
                            DebugLogger.LogError(
                                "SearchModalViewModel",
                                $"Failed to refresh grid: {gridEx.Message}"
                            );
                        }
                    });
                }

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Loaded {SearchResults.Count} existing results"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load existing results: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Refresh search statistics from the running instance
        /// CRITICAL: This connects the UI to live search data
        /// </summary>
        private void RefreshSearchStats()
        {
            if (_searchInstance == null)
                return;

            try
            {
                // Get LIVE stats from the running SearchInstance
                LastKnownResultCount = _searchInstance.ResultCount;

                // Update search state
                IsSearching = _searchInstance.IsRunning;

                // Trigger ALL UI property updates for live stats
                OnPropertyChanged(nameof(SearchStatus));
                OnPropertyChanged(nameof(SearchProgress));
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ResultsCount));

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"🔄 RECONNECTED to search - Running: {IsSearching}, Results: {LastKnownResultCount}"
                );

                // Start a timer to periodically refresh stats for live updates
                StartStatsRefreshTimer();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to refresh search stats: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Start periodic stats refresh for live updates while search is running
        /// </summary>
        private void StartStatsRefreshTimer()
        {
            if (_searchInstance == null || !IsSearching)
                return;

            // Use a simple timer to refresh stats every 500ms while search is active
            Task.Run(async () =>
            {
                while (_searchInstance?.IsRunning == true && IsSearching)
                {
                    try
                    {
                        // Update result count from live search
                        var liveResultCount = _searchInstance.ResultCount;
                        if (liveResultCount != LastKnownResultCount)
                        {
                            LastKnownResultCount = liveResultCount;
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                OnPropertyChanged(nameof(ResultsCount));
                            });
                        }

                        await Task.Delay(500); // Refresh every 500ms
                    }
                    catch
                    {
                        break; // Exit on any error
                    }
                }
            });
        }

        /// <summary>
        /// Load configuration from file path
        /// </summary>
        public async Task LoadConfigFromPathAsync(string configPath)
        {
            try
            {
                DebugLogger.LogImportant("SearchModalViewModel", $"🔍 LoadConfigFromPathAsync called with: {configPath}");

                string json;
#if BROWSER
                // Normalize path for browser storage
                var storeKey = configPath.Replace('\\', '/');
                if (storeKey.StartsWith("/data/"))
                    storeKey = storeKey.Substring(6);
                else if (storeKey.StartsWith("data/"))
                    storeKey = storeKey.Substring(5);

                DebugLogger.Log("SearchModalViewModel", $"🔍 Checking store key: {storeKey}");
                
                if (!await _appDataStore.ExistsAsync(storeKey))
                {
                     DebugLogger.LogError("SearchModalViewModel", $"❌ Filter file not found in store: {storeKey}");
                     return;
                }
                json = await _appDataStore.ReadTextAsync(storeKey) ?? string.Empty;
#else
                DebugLogger.Log("SearchModalViewModel", $"🔍 File.Exists check: {System.IO.File.Exists(configPath)}");

                if (!System.IO.File.Exists(configPath))
                {
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        $"❌ Filter file not found: {configPath}"
                    );
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        $"❌ Current directory: {System.IO.Directory.GetCurrentDirectory()}"
                    );
                    return;
                }

                // Read and parse the filter configuration
                json = await System.IO.File.ReadAllTextAsync(configPath);
#endif

                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        json
                    );

                if (config != null)
                {
                    LoadedConfig = config;
                    CurrentFilterPath = configPath; // CRITICAL: Store the path for the search!
                    DebugLogger.LogImportant("SearchModalViewModel", $"✅ CurrentFilterPath SET TO: {CurrentFilterPath}");

                    // Update deck and stake from the loaded config
                    if (!string.IsNullOrEmpty(config.Deck))
                    {
                        DeckSelection = config.Deck;
                        SelectedDeckIndex = Array.FindIndex(
                            DeckDisplayValues,
                            d => d.Contains(config.Deck.Replace(" Deck", ""))
                        );
                    }

                    if (!string.IsNullOrEmpty(config.Stake))
                    {
                        StakeSelection = config.Stake;
                        SelectedStakeIndex = Array.FindIndex(
                            StakeDisplayValues,
                            s => s == config.Stake
                        );
                    }

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Successfully loaded filter: {config.Name} (Deck: {config.Deck}, Stake: {config.Stake})"
                    );

                    // Load saved progress if Continue is enabled
                    if (ContinueFromLast)
                    {
                        await LoadSavedProgressAsync();
                    }

                    // Switch to the Search tab so user can start searching
                    SelectedTabIndex = 1; // Search tab (Deck/Stake removed)
                }
                else
                {
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        "Failed to deserialize filter config"
                    );
                }
            }
            catch (Exception ex)
            {
                var filename = configPath != null ? Path.GetFileName(configPath) : "unknown";
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load config from '{filename}': {ex.Message}"
                );
            }
        }

        // Missing event handlers for search events
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsSearching = true;
                DebugLogger.Log("SearchModalViewModel", "Search started");
                // TODO AFTER pifreak configures the visualizer THEN we can make the search mode audio!
            });
        }

        // Track last console log time to avoid spamming
        private DateTime _lastConsoleLog = DateTime.MinValue;
        private DateTime _lastResultsLoad = DateTime.MinValue;
        private volatile bool _isLoadingResults = false; // Prevent concurrent queries

        private DateTime _lastProgressLog = DateTime.MinValue;
        
        private void OnProgressUpdated(object? sender, SearchProgress e)
        {
            // Store immutable data on background thread (safe)
            LatestProgress = e;
            LastKnownResultCount = e.ResultsFound;
            
            // Log progress to console every 2 seconds so user knows search is working
            var now = DateTime.Now;
            if ((now - _lastProgressLog).TotalSeconds >= 2.0)
            {
                _lastProgressLog = now;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    AddConsoleMessage($"Progress: {e.SeedsSearched:N0} seeds | {e.SeedsPerMillisecond:F1} seeds/ms | {e.ResultsFound} found");
                });
            }

            // OPTIONAL: Apply search transition if configured (progress-driven shader effects)
            if (ActiveSearchTransition != null && MainMenu != null)
            {
                // Update transition progress (0-100% → 0.0-1.0)
                ActiveSearchTransition.CurrentProgress = (float)(e.PercentComplete / 100.0);

                // Apply interpolated shader parameters to background
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var interpolatedParams = ActiveSearchTransition.GetInterpolatedParameters();
                        ApplyShaderParametersToMainMenu(MainMenu, interpolatedParams);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "SearchModalViewModel",
                            $"Failed to apply search transition: {ex.Message}"
                        );
                    }
                });
            }

            // OPTIMIZED: Only query DuckDB when invalidation flag indicates new results exist
            // This eliminates 95%+ of wasteful queries during search
            now = DateTime.Now;
            var canCheckResults = (now - _lastResultsLoad).TotalSeconds >= 0.5; // Reduced from 1.0s for snappier updates

            if (
                canCheckResults
                && _searchInstance != null
                && _searchInstance.HasNewResultsSinceLastQuery
                && !_isLoadingResults
            )
            {
                _lastResultsLoad = now;
                _isLoadingResults = true;

                // PERFORMANCE: Run query on background thread to avoid UI lag
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_searchInstance == null)
                            return;

                        var existingCount = SearchResults.Count;

                        // Query DuckDB for new results (runs on background thread)
                        var newResults = await _searchInstance
                            .GetResultsPageAsync(existingCount, 100)
                            .ConfigureAwait(false);

                        // Acknowledge that we've queried - resets invalidation flag
                        _searchInstance.AcknowledgeResultsQueried();

                        if (newResults != null && newResults.Count > 0)
                        {
                            // Inject tally labels from SearchInstance column names (seed, score, then tallies)
                            var labels =
                                _searchInstance.ColumnNames.Count > 2
                                    ? _searchInstance.ColumnNames.Skip(2).ToArray()
                                    : Array.Empty<string>();

                            // Add results on UI thread
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                foreach (var result in newResults)
                                {
                                    // Set labels only on the first result to drive grid headers
                                    if (SearchResults.Count == 0 && labels.Length > 0)
                                    {
                                        result.Labels = labels;
                                    }
                                    SearchResults.Add(result);

                                    // Add seed found message to console with copy button
                                    AddSeedFoundMessage(result.Seed, result.TotalScore);
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "SearchModalViewModel",
                            $"Failed to load live results: {ex.Message}"
                        );
                    }
                    finally
                    {
                        _isLoadingResults = false;
                    }
                });
            }

            // Save state periodically (only for AllSeeds mode)
            // Calculate current batch from seeds searched
            if (SelectedSearchMode == SearchMode.AllSeeds && e.SeedsSearched > 0)
            {
                long batchSizeInSeeds = (long)Math.Pow(35, BatchSize + 1);
                int currentBatch = (int)(e.SeedsSearched / (ulong)batchSizeInSeeds);

                // Save state every 10 batches
                if (
                    currentBatch > 0
                    && currentBatch % 10 == 0
                    && !string.IsNullOrEmpty(CurrentFilterPath)
                )
                {
                    // Use helper method that includes deck/stake in the database path
                    var dbPath = GetDatabasePath();

                    var state = new SearchState
                    {
                        Id = 1,
                        DeckIndex = SelectedDeckIndex,
                        StakeIndex = SelectedStakeIndex,
                        BatchSize = BatchSize,
                        LastCompletedBatch = currentBatch,
                        SearchMode = (int)SelectedSearchMode,
                        WordListName = null,
                        UpdatedAt = DateTime.Now,
                    };
                    Services.SearchStateManager.SaveSearchState(dbPath, state);
                }
            }

            // Marshal ALL property updates to UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Calculate seeds per second once for use in multiple places
                double seedsPerSecond = e.SeedsPerMillisecond * 1000.0;

                // CRITICAL FIX: Add console progress logging every 5 seconds
                var now = DateTime.Now;
                if ((now - _lastConsoleLog).TotalSeconds >= 5)
                {
                    AddConsoleMessage(
                        $"Progress: {e.PercentComplete:0.00}% (~{seedsPerSecond:N0} seeds/s) {e.ResultsFound} results"
                    );
                    _lastConsoleLog = now;
                }

                // Log first result found as immediate feedback
                if (e.ResultsFound == 1 && LastKnownResultCount == 0)
                {
                    AddConsoleMessage($"First result found!");
                }

                // Update all stats properties
                ProgressPercent = e.PercentComplete;
                SearchSpeed = FormatSeedSpeed(seedsPerSecond);

                // Use the search instance for additional stats if available
                if (_searchInstance != null)
                {
                    SeedsProcessed = FormatSeedsCount((long)e.SeedsSearched);
                    TimeElapsed = _searchInstance.SearchDuration.ToString(@"hh\:mm\:ss");

                    // CRITICAL FIX: Use EstimatedTimeRemaining from SearchProgress (calculated in SearchInstance)
                    // This ensures consistent ETA calculation across all layers
                    if (e.EstimatedTimeRemaining.HasValue)
                    {
                        var remaining = e.EstimatedTimeRemaining.Value;
                        EstimatedTimeRemaining = remaining.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        EstimatedTimeRemaining = "--:--:--";
                    }
                }
                else
                {
                    SeedsProcessed = FormatSeedsCount((long)e.SeedsSearched);
                    TimeElapsed = "00:00:00";
                    EstimatedTimeRemaining = "--:--:--";
                }

                // Batch info - we'll need to calculate these or leave as placeholders
                // These aren't in the SearchProgress model currently
                CurrentBatch = 0;
                MaxBatch = 0;

                // Smart Rate Formatting - Adaptive precision based on rarity tier
                if (e.SeedsSearched > 0 && e.ResultsFound > 0)
                {
                    double rate = (double)e.ResultsFound / e.SeedsSearched * 100.0;
                    if (rate >= 1.0)
                        FindRate = $"{rate:0.00}%"; // Common: 5.67%
                    else if (rate >= 0.01)
                        FindRate = $"{rate:0.000}%"; // Uncommon: 0.234%
                    else if (rate >= 0.0001)
                        FindRate = $"{rate:0.0000}%"; // Rare: 0.0123%
                    else if (rate > 0)
                        FindRate = $"{rate:0.00000}%"; // Mythical: 0.00023%
                    else
                        FindRate = "0.00%";
                }
                else
                {
                    FindRate = "0.00%";
                }

                // Smart Rarity Formatting with K/M/B/T suffixes (NO SPM!)
                if (e.ResultsFound > 0 && e.SeedsSearched > 0)
                {
                    ulong rarity = e.SeedsSearched / (ulong)e.ResultsFound;
                    if (rarity >= 1_000_000_000_000)
                        Rarity = $"1 in {rarity / 1_000_000_000_000.0:0.00}T"; // 1 in 2.67T
                    else if (rarity >= 1_000_000_000)
                        Rarity = $"1 in {rarity / 1_000_000_000.0:0.00}B"; // 1 in 42.67B
                    else if (rarity >= 1_000_000)
                        Rarity = $"1 in {rarity / 1_000_000.0:0.00}M"; // 1 in 42.67M
                    else if (rarity >= 10_000)
                        Rarity = $"1 in {rarity / 1_000.0:0.0}K"; // 1 in 42.7K
                    else
                        Rarity = $"1 in {rarity:N0}"; // 1 in 427
                }
                else
                {
                    Rarity = "--"; // Show placeholder until first result
                }

                OnPropertyChanged(nameof(SearchProgress));
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ResultsCount));
                PanelText = $"{e.ResultsFound} seeds | {e.PercentComplete:0}%";

                // If results increased since last update, log the new seeds found
                if (e.ResultsFound > LastKnownResultCount)
                {
                    var newSeedsCount = e.ResultsFound - LastKnownResultCount;
                    if (newSeedsCount == 1)
                    {
                        AddConsoleMessage($"Found new seed! Total: {e.ResultsFound}");
                    }
                    else
                    {
                        AddConsoleMessage(
                            $"Found {newSeedsCount} new seeds! Total: {e.ResultsFound}"
                        );
                    }
                    LastKnownResultCount = e.ResultsFound;
                }
            });
        }

        /// <summary>
        /// Initialize dynamic tabs for consistent Balatro styling
        /// </summary>
        public object? SettingsTabContent { get; private set; }
        public object? SearchTabContent { get; private set; }
        public object? ResultsTabContent { get; private set; }

        private void InitializeSearchTabs()
        {
            TabItems.Clear();

            // PROPER MVVM: Use XAML UserControls
            SettingsTabContent = new Views.SearchModalTabs.SettingsTab { DataContext = this };
            SearchTabContent = new Views.SearchModalTabs.SearchTab { DataContext = this };
            ResultsTabContent = new Views.SearchModalTabs.ResultsTab { DataContext = this };

            // Remove the built-in "Select Filter" tab; the new `FilterSelectionModal` will be used instead
            // Preferred Deck tab removed - users already see deck/stake info in filter selection modal
            // TabItems.Add(new TabItemViewModel("Preferred Deck", SettingsTabContent));
            TabItems.Add(new TabItemViewModel("Search", SearchTabContent));
            TabItems.Add(new TabItemViewModel("Results", ResultsTabContent));
        }

        private void LoadAvailableWordLists()
        {
            try
            {
                var wordListDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "WordLists");
                if (Directory.Exists(wordListDir))
                {
                    var files = Directory
                        .GetFiles(wordListDir, "*.db")
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>()
                        .OrderBy(f => f);

                    AvailableWordLists.Clear();
                    foreach (var file in files)
                    {
                        AvailableWordLists.Add(file);
                    }

                    // Select first one by default
                    if (AvailableWordLists.Count > 0)
                    {
                        SelectedWordList = AvailableWordLists[0];
                        SelectedWordListIndex = 0;
                    }

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Loaded {AvailableWordLists.Count} word lists"
                    );
                }
                else
                {
                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"WordLists directory not found: {wordListDir}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load wordlists: {ex.Message}"
                );
            }
        }

        private void LoadAvailableDbLists()
        {
            try
            {
                var searchResultsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SearchResults");
                if (Directory.Exists(searchResultsDir))
                {
                    var files = Directory
                        .GetFiles(searchResultsDir, "*.db")
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>()
                        .OrderBy(f => f);

                    AvailableDbLists.Clear();
                    foreach (var file in files)
                    {
                        AvailableDbLists.Add(file);
                    }

                    // Select first one by default
                    if (AvailableDbLists.Count > 0)
                    {
                        SelectedDbList = AvailableDbLists[0];
                        SelectedDbListIndex = 0;
                    }

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Loaded {AvailableDbLists.Count} DB lists"
                    );
                }
                else
                {
                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"SearchResults directory not found: {searchResultsDir}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load DB lists: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Format seed speed with K/M abbreviations (no decimals)
        /// </summary>
        private static string FormatSeedSpeed(double seedsPerSecond)
        {
            if (seedsPerSecond >= 1_000_000)
            {
                return $"{seedsPerSecond / 1_000_000:0}M/s";
            }
            else if (seedsPerSecond >= 1_000)
            {
                return $"{seedsPerSecond / 1_000:0}K/s";
            }
            else
            {
                return $"{seedsPerSecond:0}/s";
            }
        }

        private static string FormatSeedsCount(long count)
        {
            if (count >= 1_000_000)
            {
                return $"{count / 1_000_000.0:0}M";
            }
            else if (count >= 1_000)
            {
                return $"{count / 1_000.0:0}K";
            }
            else
            {
                return $"{count:N0}";
            }
        }

        /// <summary>
        /// Load saved search progress from DuckDB when "Continue from last position" is enabled
        /// </summary>
        private async Task LoadSavedProgressAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilterPath))
                {
                    DebugLogger.Log(
                        "SearchModalViewModel",
                        "Cannot load saved progress: No filter path"
                    );
                    return;
                }

                // Use helper method that includes deck/stake in the database path
                var dbPath = GetDatabasePath();

                if (!System.IO.File.Exists(dbPath))
                {
                    DebugLogger.Log("SearchModalViewModel", $"No saved state found at: {dbPath}");
                    ProgressPercent = 0.0;
                    return;
                }

                var savedState = Services.SearchStateManager.LoadSearchState(dbPath);
                if (savedState != null)
                {
                    // Calculate progress percentage from saved batch
                    // BatchSize is hardcoded to 3, so total batches = 35^4 = 1,500,625
                    long totalBatches = (long)Math.Pow(35, BatchSize + 1);
                    double progress =
                        ((double)savedState.LastCompletedBatch / totalBatches) * 100.0;

                    ProgressPercent = progress;
                    AddConsoleMessage(
                        $"Loaded saved state: Batch {savedState.LastCompletedBatch:N0} ({progress:0.00}%)"
                    );

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Loaded saved progress: {progress:0.00}% from batch {savedState.LastCompletedBatch}"
                    );
                }
                else
                {
                    ProgressPercent = 0.0;
                    AddConsoleMessage("No saved progress found for this filter");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load saved progress: {ex.Message}"
                );
                ProgressPercent = 0.0;
            }
        }

        #endregion

        #region Shader Transition Helpers

        /// <summary>
        /// Configures the search transition based on user profile settings.
        /// Called when a search starts. If enabled, creates a transition from configured start/end presets.
        /// </summary>
        private void ConfigureSearchTransition()
        {
            try
            {
                var settings = _userProfileService.GetProfile().VisualizerSettings;

                if (!settings.EnableSearchTransition)
                {
                    ActiveSearchTransition = null;
                    DebugLogger.Log("SearchModalViewModel", "Search transitions disabled by user");
                    return;
                }

                // Load start and end presets (or use defaults)
                var startParams = LoadPresetParameters(
                    settings.SearchTransitionStartPresetName ?? "Default Balatro",
                    true
                );
                var endParams = LoadPresetParameters(settings.SearchTransitionEndPresetName ?? "Default Balatro", false);

                // Create transition
                ActiveSearchTransition = new Models.VisualizerPresetTransition
                {
                    StartParameters = startParams,
                    EndParameters = endParams,
                    CurrentProgress = 0f,
                };

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Search transition configured: Start='{settings.SearchTransitionStartPresetName ?? "Default Balatro"}', End='{settings.SearchTransitionEndPresetName ?? "Default Balatro"}'"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to configure search transition: {ex.Message}"
                );
                ActiveSearchTransition = null;
            }
        }

        /// <summary>
        /// Loads shader parameters from a preset name, or returns defaults if not found.
        /// </summary>
        private Models.ShaderParameters LoadPresetParameters(string? presetName, bool isDarkPreset)
        {
            // If no preset name specified or it's a default preset, use built-in defaults
            if (
                string.IsNullOrWhiteSpace(presetName)
                || presetName == "Default Balatro"
            )
            {
                return isDarkPreset
                    ? Extensions.VisualizerPresetExtensions.CreateDefaultIntroParameters()
                    : Extensions.VisualizerPresetExtensions.CreateDefaultNormalParameters();
            }

            // Try to load custom preset from disk
            try
            {
                var presets = Helpers.PresetHelper.LoadAllPresets();
                var preset = presets.FirstOrDefault(p => p.Name == presetName);

                if (preset != null)
                {
                    return preset.ToShaderParameters();
                }
                else
                {
                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Preset '{presetName}' not found, using defaults"
                    );
                    return isDarkPreset
                        ? Extensions.VisualizerPresetExtensions.CreateDefaultIntroParameters()
                        : Extensions.VisualizerPresetExtensions.CreateDefaultNormalParameters();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to load preset '{presetName}': {ex.Message}"
                );
                return isDarkPreset
                    ? Extensions.VisualizerPresetExtensions.CreateDefaultIntroParameters()
                    : Extensions.VisualizerPresetExtensions.CreateDefaultNormalParameters();
            }
        }

        /// <summary>
        /// Applies shader parameters to BalatroMainMenu's shader background.
        /// Uses reflection to access private _shaderBackground field.
        /// Called when ActiveSearchTransition is set and search progress updates.
        /// </summary>
        private void ApplyShaderParametersToMainMenu(
            Views.BalatroMainMenu mainMenu,
            Models.ShaderParameters parameters
        )
        {
            try
            {
                // Access private _shaderBackground field via reflection
                var shaderBackgroundField = typeof(Views.BalatroMainMenu).GetField(
                    "_shaderBackground",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );

                if (
                    shaderBackgroundField?.GetValue(mainMenu)
                    is BalatroSeedOracle.Controls.BalatroShaderBackground shaderBackground
                )
                {
                    // Apply all shader parameters
                    shaderBackground.SetTime(parameters.TimeSpeed);
                    shaderBackground.SetSpinTime(parameters.SpinTimeSpeed);
                    shaderBackground.SetMainColor(parameters.MainColor);
                    shaderBackground.SetAccentColor(parameters.AccentColor);
                    shaderBackground.SetBackgroundColor(parameters.BackgroundColor);
                    shaderBackground.SetContrast(parameters.Contrast);
                    shaderBackground.SetSpinAmount(parameters.SpinAmount);
                    shaderBackground.SetParallax(parameters.ParallaxX, parameters.ParallaxY);
                    shaderBackground.SetZoomScale(parameters.ZoomScale);
                    shaderBackground.SetSaturationAmount(parameters.SaturationAmount);
                    shaderBackground.SetSaturationAmount2(parameters.SaturationAmount2);
                    shaderBackground.SetPixelSize(parameters.PixelSize);
                    shaderBackground.SetSpinEase(parameters.SpinEase);
                    shaderBackground.SetLoopCount(parameters.LoopCount);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchModalViewModel",
                    $"Failed to apply shader parameters: {ex.Message}"
                );
            }
        }

        #endregion
    }
}
