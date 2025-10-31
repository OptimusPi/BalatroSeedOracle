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
    }

    public partial class SearchModalViewModel
        : ObservableObject,
            IDisposable,
            BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly SearchManager _searchManager;
        private readonly CircularConsoleBuffer _consoleBuffer;

        private SearchInstance? _searchInstance;
        private string _currentSearchId = string.Empty;

        public Views.BalatroMainMenu? MainMenu { get; set; }

        // Callback for CREATE NEW FILTER button (set by View)
        private Action? _newFilterRequestedAction;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartSearchCommand))]
        private bool _isSearching = false;

        [ObservableProperty]
        private Motely.Filters.MotelyJsonConfig? _loadedConfig;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private SearchProgress? _latestProgress;

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

        // Search Mode Properties
        [ObservableProperty]
        private SearchMode _selectedSearchMode = SearchMode.AllSeeds;

        public string[] SearchModeDisplayValues { get; } =
            new[] { "All Seeds", "Single Seed", "Word List" };

        [ObservableProperty]
        private string _seedInput = string.Empty;

        [ObservableProperty]
        private bool _continueFromLast = false;

        // Visibility properties for mode-specific controls
        [ObservableProperty]
        private bool _isThreadsVisible = true;

        [ObservableProperty]
        private bool _isBatchSizeVisible = true;

        [ObservableProperty]
        private bool _isContinueVisible = true;

        [ObservableProperty]
        private bool _isSeedInputVisible = false;

        [ObservableProperty]
        private bool _isWordListVisible = false;

        // WordList index properties for SpinnerControl binding
        [ObservableProperty]
        private int _selectedWordListIndex = 0;

        public int WordListMaxIndex => Math.Max(0, AvailableWordLists.Count - 1);

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
        private long _seedsProcessed = 0;

        [ObservableProperty]
        private string _timeElapsed = "00:00:00";

        [ObservableProperty]
        private string _estimatedTimeRemaining = "--:--:--";

        [ObservableProperty]
        private string _findRate = "0.00%";

        [ObservableProperty]
        private string _rarity = "--";

        // Search button dynamic properties
        public string CookButtonText => IsSearching ? "STOP SEARCH" : "START SEARCH";

        // Results filtering
        [ObservableProperty]
        private string _resultsFilterText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SearchResult> _filteredResults = new();

        public SearchModalViewModel(SearchManager searchManager)
        {
            _searchManager = searchManager;
            _consoleBuffer = new CircularConsoleBuffer(1000);

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<string>();

            // Set default values
            ThreadCount = Environment.ProcessorCount / 2;
            BatchSize = 1; // Default batch size (35^2)

            // Initialize dynamic tabs
            InitializeSearchTabs();

            // Load available wordlists
            LoadAvailableWordLists();

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
        public int BatchSize { get; set; } = 3;

        public string SearchStatus => IsSearching ? "Searching..." : "Ready";
        public double SearchProgress => LatestProgress?.PercentComplete ?? 0.0;
        public string ProgressText => LatestProgress?.ToString() ?? "No search active";
        public int ResultsCount => _searchInstance?.ResultCount ?? SearchResults.Count;

        public string CurrentSearchId => _currentSearchId;

        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();
        public ObservableCollection<Models.SearchResult> SearchResults { get; }
        public ObservableCollection<string> ConsoleOutput { get; }

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

                IsSearching = true;

                ClearResults();
                AddConsoleMessage(
                    $"Starting search in {SearchModeDisplayValues[(int)SelectedSearchMode]} mode..."
                );
                PanelText = $"Searching with '{LoadedConfig.Name}'...";

                AddConsoleMessage($"Building search criteria...");
                var searchCriteria = BuildSearchCriteria();
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

                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Search started with ID: {_currentSearchId}"
                );
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
                    _searchInstance.StopSearch();
                    AddConsoleMessage("Search stopped by user.");
                    DebugLogger.Log("SearchModalViewModel", "Search stopped by user");
                }

                IsSearching = false;
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

            // Force thread/batch values for Single Seed mode
            if (value == SearchMode.SingleSeed)
            {
                ThreadCount = 1;
                BatchSize = 1;
            }
        }

        partial void OnSelectedWordListIndexChanged(int value)
        {
            if (value >= 0 && value < AvailableWordLists.Count)
            {
                SelectedWordList = AvailableWordLists[value];
            }
        }

        private void UpdateControlVisibility()
        {
            switch (SelectedSearchMode)
            {
                case SearchMode.AllSeeds:
                    IsThreadsVisible = true;
                    IsBatchSizeVisible = true;
                    IsContinueVisible = true;
                    IsSeedInputVisible = false;
                    IsWordListVisible = false;
                    break;

                case SearchMode.SingleSeed:
                    IsThreadsVisible = false;
                    IsBatchSizeVisible = false;
                    IsContinueVisible = false;
                    IsSeedInputVisible = true;
                    IsWordListVisible = false;
                    break;

                case SearchMode.WordList:
                    IsThreadsVisible = true;
                    IsBatchSizeVisible = true;
                    IsContinueVisible = false; // Wordlists don't support continue
                    IsSeedInputVisible = false;
                    IsWordListVisible = true;
                    break;
            }

            // Notify property changed for WordListMaxIndex
            OnPropertyChanged(nameof(WordListMaxIndex));
        }

        public async Task LoadFilterAsync(string configPath)
        {
            try
            {
                LoadConfigFromPath(configPath);
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
            _newFilterRequestedAction = callback;
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
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsSearching = false;
                AddConsoleMessage($"Search completed. Found {SearchResults.Count} results.");
                PanelText = $"Search complete: {SearchResults.Count} seeds";
                DebugLogger.Log(
                    "SearchModalViewModel",
                    $"Search completed with {SearchResults.Count} results"
                );
            });
        }

        #endregion

        #region Helper Methods

        private SearchCriteria BuildSearchCriteria()
        {
            if (string.IsNullOrEmpty(CurrentFilterPath))
            {
                throw new InvalidOperationException(
                    "No filter path available - filter must be loaded first!"
                );
            }

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
                        // CRITICAL FIX: Convert JSON filter path to database path
                        // CurrentFilterPath is like: "JsonItemFilters/MyFilter.json"
                        // Database path should be: "SearchResults/MyFilter.db"
                        var filterName = System.IO.Path.GetFileNameWithoutExtension(
                            CurrentFilterPath
                        );
                        var searchResultsDir = System.IO.Path.Combine(
                            System.IO.Directory.GetCurrentDirectory(),
                            "SearchResults"
                        );
                        var dbPath = System.IO.Path.Combine(searchResultsDir, $"{filterName}.db");

                        AddConsoleMessage($"Checking for saved state at: {dbPath}");
                        var savedState = Services.SearchStateManager.LoadSearchState(dbPath);
                        if (savedState != null)
                        {
                            int resumeBatch = savedState.LastCompletedBatch;

                            // If user changed batch size, convert the batch number
                            if (savedState.BatchSize != BatchSize)
                            {
                                resumeBatch = Services.SearchStateManager.ConvertBatchNumber(
                                    savedState.LastCompletedBatch,
                                    savedState.BatchSize,
                                    BatchSize
                                );

                                DebugLogger.Log(
                                    "SearchModalViewModel",
                                    $"Converted batch {savedState.LastCompletedBatch} (size {savedState.BatchSize}) "
                                        + $"to batch {resumeBatch} (size {BatchSize})"
                                );
                                AddConsoleMessage(
                                    $"Batch size changed - converted to batch {resumeBatch}"
                                );
                            }

                            criteria.StartBatch = (ulong)(resumeBatch + 1); // +1 to start AFTER last completed
                            AddConsoleMessage($"Resuming from batch {resumeBatch + 1}");
                        }
                        else
                        {
                            AddConsoleMessage($"No saved state found - starting from batch 0");
                        }
                    }
                    break;

                case SearchMode.SingleSeed:
                    // Single seed debug mode
                    criteria.DebugSeed = SeedInput;
                    criteria.ThreadCount = 1;
                    criteria.BatchSize = 1;
                    criteria.EnableDebugOutput = true;
                    break;

                case SearchMode.WordList:
                    // Wordlist filtering mode
                    // Remove .txt extension for CLI
                    var listName = Path.GetFileNameWithoutExtension(SelectedWordList);
                    criteria.WordList = listName;
                    criteria.ThreadCount = ThreadCount;
                    criteria.BatchSize = BatchSize;
                    break;
            }

            return criteria;
        }

        private void AddConsoleMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";

            _consoleBuffer.AddLine(formattedMessage);
            ConsoleOutput.Add(formattedMessage);

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

                    // CRITICAL: Update UI state from existing search
                    IsSearching = _searchInstance?.IsRunning ?? false;

                    // CRITICAL: Load existing results to show current progress
                    await LoadExistingResults();

                    // CRITICAL: Get current search progress/stats
                    RefreshSearchStats();

                    // Switch to Results tab to show the reconnected search
                    SelectedTabIndex = 3; // Results tab

                    DebugLogger.Log(
                        "SearchModalViewModel",
                        $"Successfully reconnected to search: {searchId}, Running: {_searchInstance?.IsRunning ?? false}, Results: {SearchResults.Count}"
                    );
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
        public void LoadConfigFromPath(string configPath)
        {
            try
            {
                DebugLogger.Log("SearchModalViewModel", $"Loading config from: {configPath}");

                if (!System.IO.File.Exists(configPath))
                {
                    DebugLogger.LogError(
                        "SearchModalViewModel",
                        $"Filter file not found: {configPath}"
                    );
                    return;
                }

                // Read and parse the filter configuration
                var json = System.IO.File.ReadAllText(configPath);
                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        json
                    );

                if (config != null)
                {
                    LoadedConfig = config;
                    CurrentFilterPath = configPath; // CRITICAL: Store the path for the search!

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

        private void OnProgressUpdated(object? sender, SearchProgress e)
        {
            // Store immutable data on background thread (safe)
            LatestProgress = e;
            LastKnownResultCount = e.ResultsFound;

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
                    // CRITICAL FIX: Convert JSON filter path to database path for saving state
                    var filterName = System.IO.Path.GetFileNameWithoutExtension(CurrentFilterPath);
                    var searchResultsDir = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "SearchResults"
                    );
                    var dbPath = System.IO.Path.Combine(searchResultsDir, $"{filterName}.db");

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
                // Update all stats properties
                ProgressPercent = e.PercentComplete;

                // Calculate seeds per second from SeedsPerMillisecond
                double seedsPerSecond = e.SeedsPerMillisecond * 1000.0;
                SearchSpeed = $"{seedsPerSecond:N0} seeds/s";

                // Use the search instance for additional stats if available
                if (_searchInstance != null)
                {
                    SeedsProcessed = (long)e.SeedsSearched;
                    TimeElapsed = _searchInstance.SearchDuration.ToString(@"hh\:mm\:ss");

                    // Estimate time remaining based on progress
                    if (e.PercentComplete > 0 && e.PercentComplete < 100)
                    {
                        var elapsed = _searchInstance.SearchDuration;
                        var totalEstimated = TimeSpan.FromSeconds(
                            elapsed.TotalSeconds / (e.PercentComplete / 100.0)
                        );
                        var remaining = totalEstimated - elapsed;
                        EstimatedTimeRemaining = remaining.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        EstimatedTimeRemaining = "--:--:--";
                    }
                }
                else
                {
                    SeedsProcessed = (long)e.SeedsSearched;
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
                var wordListDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WordLists");
                if (Directory.Exists(wordListDir))
                {
                    var files = Directory
                        .GetFiles(wordListDir, "*.txt")
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

        #endregion
    }
}
