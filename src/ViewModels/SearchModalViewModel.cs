using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using BalatroSeedOracle.Helpers;
using Motely.Filters;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class SearchModalViewModel : ObservableObject, IDisposable, BalatroSeedOracle.Helpers.IModalBackNavigable
    {
        private readonly SearchManager _searchManager;
        private readonly CircularConsoleBuffer _consoleBuffer;

        private SearchInstance? _searchInstance;
        private string _currentSearchId = string.Empty;

        // Reference to main menu for VibeOut mode
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
        private bool _isSelectFilterTabVisible = true;

        [ObservableProperty]
        private bool _isSettingsTabVisible = false;

        [ObservableProperty]
        private bool _isSearchTabVisible = false;

        [ObservableProperty]
        private bool _isResultsTabVisible = false;

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
        private string _deckSelection = "All Decks";

        [ObservableProperty]
        private string _stakeSelection = "All Stakes";

        [ObservableProperty]
        private string _selectedWordList = "None";

        [ObservableProperty]
        private ObservableCollection<string> _availableWordLists = new();

        public SearchModalViewModel(SearchManager searchManager)
        {
            _searchManager = searchManager;
            _consoleBuffer = new CircularConsoleBuffer(1000);

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<string>();

            // Initialize dynamic tabs
            InitializeSearchTabs();

            // Load available wordlists
            LoadAvailableWordLists();

            // Events will be subscribed to individual SearchInstance when created
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentTabContent));
        }

        partial void OnIsSearchingChanged(bool value)
        {
            StopSearchCommand.NotifyCanExecuteChanged();
        }

        #region Properties

        public object? CurrentTabContent => SelectedTabIndex >= 0 && SelectedTabIndex < TabItems.Count
            ? TabItems[SelectedTabIndex].Content
            : null;

        public int ThreadCount { get; set; } = Environment.ProcessorCount;
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

        #endregion

        #region Command Implementations

        [RelayCommand(CanExecute = nameof(CanStartSearch))]
        private async Task StartSearchAsync()
        {
            try
            {
                if (LoadedConfig == null)
                {
                    AddConsoleMessage("No filter configuration loaded. Please load a filter first.");
                    return;
                }

                IsSearching = true;
                _currentSearchId = Guid.NewGuid().ToString();
                
                ClearResults();
                AddConsoleMessage("Starting search...");
                PanelText = $"Searching with '{LoadedConfig.Name}'...";

                var searchCriteria = BuildSearchCriteria();
                _searchInstance = await _searchManager.StartSearchAsync(searchCriteria, LoadedConfig);

                // Subscribe to SearchInstance events directly
                _searchInstance.SearchCompleted += OnSearchCompleted;
                _searchInstance.ProgressUpdated += OnProgressUpdated;

                DebugLogger.Log("SearchModalViewModel", $"Search started with ID: {_currentSearchId}");
            }
            catch (Exception ex)
            {
                IsSearching = false;
                AddConsoleMessage($"Error starting search: {ex.Message}");
                DebugLogger.LogError("SearchModalViewModel", $"Error starting search: {ex.Message}");
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
                DebugLogger.LogError("SearchModalViewModel", $"Error stopping search: {ex.Message}");
            }
        }

        private bool CanStopSearch()
        {
            return IsSearching;
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

        public async Task LoadFilterAsync(string configPath)
        {
            try
            {
                LoadConfigFromPath(configPath);
                PanelText = $"Filter loaded: {LoadedConfig?.Name ?? System.IO.Path.GetFileNameWithoutExtension(configPath)}";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Error loading filter from path: {ex.Message}");
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
            else if (parameter is string tabIndexStr && int.TryParse(tabIndexStr, out int parsedIndex))
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
            IsSelectFilterTabVisible = false;
            IsSettingsTabVisible = false;
            IsSearchTabVisible = false;
            IsResultsTabVisible = false;

            // Show only the selected tab
            switch (tabIndex)
            {
                case 0:
                    IsSelectFilterTabVisible = true;
                    break;
                case 1:
                    IsSettingsTabVisible = true;
                    break;
                case 2:
                    IsSearchTabVisible = true;
                    break;
                case 3:
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
            try
            {
                if (MainMenu != null)
                {
                    MainMenu.EnterVibeOutMode();
                    DebugLogger.Log("SearchModalViewModel", "🎵 VibeOut mode activated!");
                }
                else
                {
                    DebugLogger.LogError("SearchModalViewModel", "MainMenu reference not set - cannot enter VibeOut mode");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to start VibeOut: {ex.Message}");
            }
        }
        

        #endregion

        #region Event Handlers

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            IsSearching = false;
            AddConsoleMessage($"Search completed. Found {SearchResults.Count} results.");
            PanelText = $"Search complete: {SearchResults.Count} seeds";
            DebugLogger.Log("SearchModalViewModel", $"Search completed with {SearchResults.Count} results");
        }

        #endregion

        #region Helper Methods

        private SearchCriteria BuildSearchCriteria()
        {
            if (string.IsNullOrEmpty(CurrentFilterPath))
            {
                throw new InvalidOperationException("No filter path available - filter must be loaded first!");
            }

            return new SearchCriteria
            {
                ConfigPath = CurrentFilterPath, // CRITICAL: Pass the filter path!
                ThreadCount = Environment.ProcessorCount,
                Deck = DeckSelection == "All Decks" ? null : DeckSelection,
                Stake = StakeSelection == "All Stakes" ? null : StakeSelection,
                WordList = SelectedWordList == "None" ? null : SelectedWordList // Pass wordlist if selected
            };
        }

        private void AddConsoleMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";
            
            _consoleBuffer.AddLine(formattedMessage);
            ConsoleOutput.Add(formattedMessage);
            
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
                    
                    DebugLogger.Log("SearchModalViewModel", $"Successfully reconnected to search: {searchId}, Running: {_searchInstance?.IsRunning ?? false}, Results: {SearchResults.Count}");
                }
                else
                {
                    DebugLogger.LogError("SearchModalViewModel", $"Search instance not found: {searchId}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to connect to existing search: {ex.Message}");
            }
        }

        /// <summary>
        /// Load existing results from the search instance
        /// </summary>
        private async Task LoadExistingResults()
        {
            if (_searchInstance == null) return;

            try
            {
                SearchResults.Clear();
                
                // Load results from the search instance using async API
                var existingResults = await _searchInstance.GetResultsPageAsync(0, 1000);
                if (existingResults != null)
                {
                    // Inject tally labels from SearchInstance column names (seed, score, then tallies)
                    var labels = _searchInstance.ColumnNames.Count > 2
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
                
                DebugLogger.Log("SearchModalViewModel", $"Loaded {SearchResults.Count} existing results");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to load existing results: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh search statistics from the running instance
        /// CRITICAL: This connects the UI to live search data
        /// </summary>
        private void RefreshSearchStats()
        {
            if (_searchInstance == null) return;

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

                DebugLogger.Log("SearchModalViewModel", $"🔄 RECONNECTED to search - Running: {IsSearching}, Results: {LastKnownResultCount}");
                
                // Start a timer to periodically refresh stats for live updates
                StartStatsRefreshTimer();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to refresh search stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Start periodic stats refresh for live updates while search is running
        /// </summary>
        private void StartStatsRefreshTimer()
        {
            if (_searchInstance == null || !IsSearching) return;

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
                    DebugLogger.LogError("SearchModalViewModel", $"Filter file not found: {configPath}");
                    return;
                }
                
                // Read and parse the filter configuration
                var json = System.IO.File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);

                if (config != null)
                {
                    LoadedConfig = config;
                    CurrentFilterPath = configPath; // CRITICAL: Store the path for the search!
                    
                    // Update deck and stake from the loaded config
                    if (!string.IsNullOrEmpty(config.Deck))
                    {
                        DeckSelection = config.Deck;
                    }
                    
                    if (!string.IsNullOrEmpty(config.Stake))
                    {
                        StakeSelection = config.Stake;
                    }
                    
                    DebugLogger.Log("SearchModalViewModel", $"Successfully loaded filter: {config.Name} (Deck: {config.Deck}, Stake: {config.Stake})");
                    
                    // Switch to the Search tab so user can start searching
                    SelectedTabIndex = 2; // Search tab
                }
                else
                {
                    DebugLogger.LogError("SearchModalViewModel", "Failed to deserialize filter config");
                }
            }
            catch (Exception ex)
            {
                var filename = configPath != null ? Path.GetFileName(configPath) : "unknown";
                DebugLogger.LogError("SearchModalViewModel", $"Failed to load config from '{filename}': {ex.Message}");
            }
        }

        // Missing event handlers for search events
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            IsSearching = true;
            DebugLogger.Log("SearchModalViewModel", "Search started");
            // TODO AFTER pifreak configures the visualizer THEN we can make the search mode audio!
        }

        private void OnProgressUpdated(object? sender, SearchProgress e)
        {
            LatestProgress = e;
            LastKnownResultCount = e.ResultsFound;
            OnPropertyChanged(nameof(SearchProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ResultsCount));
            PanelText = $"{e.ResultsFound} seeds | {e.PercentComplete:0}%";
        }


        /// <summary>
        /// Initialize dynamic tabs for consistent Balatro styling
        /// </summary>
        public object? FilterTabContent { get; private set; }
        public object? SettingsTabContent { get; private set; }
        public object? SearchTabContent { get; private set; }
        public object? ResultsTabContent { get; private set; }

        private void InitializeSearchTabs()
        {
            TabItems.Clear();

            // PROPER MVVM: Use XAML UserControls, not UI-in-code garbage
            FilterTabContent = CreateFilterTabContent(); // Still needs programmatic setup for FilterSelector events
            SettingsTabContent = new Views.SearchModalTabs.SettingsTab { DataContext = this };
            SearchTabContent = new Views.SearchModalTabs.SearchTab { DataContext = this };
            ResultsTabContent = new Views.SearchModalTabs.ResultsTab { DataContext = this };

            TabItems.Add(new TabItemViewModel("Select Filter", FilterTabContent));
            TabItems.Add(new TabItemViewModel("Deck/Stake", SettingsTabContent));
            TabItems.Add(new TabItemViewModel("Search", SearchTabContent));
            TabItems.Add(new TabItemViewModel("Results", ResultsTabContent));
        }

        private UserControl CreateFilterTabContent()
        {
            // Use the challenges-inspired FilterSelectorControl for the filter chooser
            var filterSelector = new Components.FilterSelectorControl
            {
                Name = "FilterSelector",
                IsInSearchModal = true
            };

            // Wire events directly so Select button advances to Search tab
            filterSelector.FilterSelected += async (s, path) =>
            {
                DebugLogger.Log("SearchModalViewModel", $"Filter clicked in list! Path: {path}");
                await LoadFilterAsync(path);
                DebugLogger.Log("SearchModalViewModel", "Filter loaded for preview - staying on Select Filter tab");
            };

            filterSelector.FilterConfirmed += async (s, path) =>
            {
                DebugLogger.Log("SearchModalViewModel", $"SELECT THIS FILTER button clicked! Path: {path}");
                await LoadFilterAsync(path);
                DebugLogger.Log("SearchModalViewModel", "Filter confirmed, auto-advancing to Search tab");

                // Advance to Search tab (index 2)
                SelectedTabIndex = 2;
                UpdateTabVisibility(2);
            };

            filterSelector.NewFilterRequested += (s, e) =>
            {
                DebugLogger.Log("SearchModalViewModel", "CREATE NEW FILTER button clicked! Opening FiltersModal...");
                _newFilterRequestedAction?.Invoke();
            };

            // Layout: simple container hosting the selector
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // FilterSelector

            Grid.SetRow(filterSelector, 0);
            mainGrid.Children.Add(filterSelector);

            // Return selector directly inside a simple container without hardcoded background
            return new UserControl
            {
                Content = mainGrid
            };
        }

        // DELETED: CreateSettingsTabContent() - replaced with XAML UserControl (SettingsTab.axaml)
        // DELETED: CreateSearchTabContent() - replaced with XAML UserControl (SearchTab.axaml)
        // DELETED: CreateResultsTabContent() - replaced with XAML UserControl (ResultsTab.axaml)
        // These methods were 235 lines of anti-MVVM UI-in-code garbage!

        // CreateShortcutRequested already exists - removed duplicate
        
        /// <summary>
        /// Create simple ante selector
        /// </summary>

        private void LoadAvailableWordLists()
        {
            try
            {
                var wordListsPath = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    "WordLists"
                );

                AvailableWordLists.Clear();
                AvailableWordLists.Add("None"); // Default option

                if (System.IO.Directory.Exists(wordListsPath))
                {
                    var files = System.IO.Directory.GetFiles(wordListsPath, "*.txt")
                        .Select(f => System.IO.Path.GetFileNameWithoutExtension(f))
                        .OrderBy(f => f);

                    foreach (var file in files)
                    {
                        AvailableWordLists.Add(file);
                    }
                }

                SelectedWordList = "None";
                DebugLogger.Log("SearchModalViewModel", $"Loaded {AvailableWordLists.Count - 1} word lists");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModalViewModel", $"Failed to load word lists: {ex.Message}");
            }
        }

        #endregion
    }
}