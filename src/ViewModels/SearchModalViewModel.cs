using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
    public class SearchModalViewModel : BaseViewModel, IDisposable
    {
        private readonly SearchManager _searchManager;
        private readonly CircularConsoleBuffer _consoleBuffer;

        private SearchInstance? _searchInstance;
        private string _currentSearchId = string.Empty;
        private bool _isSearching = false;

        // Reference to main menu for VibeOut mode
        public Views.BalatroMainMenu? MainMenu { get; set; }
        private Motely.Filters.MotelyJsonConfig? _loadedConfig;
        private int _selectedTabIndex = 0;
        private SearchProgress? _latestProgress;
        private int _lastKnownResultCount = 0;
        private string? _currentFilterPath; // CRITICAL: Store the path to the loaded filter!

        // Search parameters
        private int _maxResults = 1000;
        private int _timeoutSeconds = 300;
        private string _deckSelection = "All Decks";
        private string _stakeSelection = "All Stakes";
        private string _selectedWordList = "None";
        private ObservableCollection<string> _availableWordLists = new();

        public SearchModalViewModel(SearchManager searchManager)
        {
            _searchManager = searchManager;
            _consoleBuffer = new CircularConsoleBuffer(1000);

            SearchResults = new ObservableCollection<Models.SearchResult>();
            ConsoleOutput = new ObservableCollection<string>();

            // Initialize commands
            StartSearchCommand = new AsyncRelayCommand(StartSearchAsync, CanStartSearch);
            StopSearchCommand = new RelayCommand(StopSearch, CanStopSearch);
            PauseSearchCommand = new RelayCommand(PauseSearch, CanPauseSearch);
            ExportResultsCommand = new RelayCommand(ExportResults, CanExportResults);
            ClearResultsCommand = new RelayCommand(ClearResults);
            LoadFilterCommand = new AsyncRelayCommand(LoadFilterAsync);
            CreateShortcutCommand = new RelayCommand<string>(CreateShortcut);
            EnterVibeOutModeCommand = new RelayCommand(EnterVibeOutMode);
            CloseCommand = new RelayCommand(CloseModal);
            SelectTabCommand = new RelayCommand<object>(SelectTab);

            // Initialize dynamic tabs
            InitializeSearchTabs();

            // Load available wordlists
            LoadAvailableWordLists();

            // Events will be subscribed to individual SearchInstance when created
        }

        #region Properties

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnPropertyChanged(nameof(CurrentTabContent));
                }
            }
        }

        public object? CurrentTabContent => _selectedTabIndex >= 0 && _selectedTabIndex < TabItems.Count
            ? TabItems[_selectedTabIndex].Content
            : null;

        public int ThreadCount { get; set; } = Environment.ProcessorCount;
        public int BatchSize { get; set; } = 3;
        
        public string SearchStatus => _isSearching ? "Searching..." : "Ready";
        public double SearchProgress => _latestProgress?.PercentComplete ?? 0.0;
        public string ProgressText => _latestProgress?.ToString() ?? "No search active";
        public int ResultsCount => _searchInstance?.ResultCount ?? SearchResults.Count;
        
        public string CurrentSearchId => _currentSearchId;
        
        public ObservableCollection<TabItemViewModel> TabItems { get; } = new();

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (SetProperty(ref _isSearching, value))
                {
                    ((AsyncRelayCommand)StartSearchCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)StopSearchCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public Motely.Filters.MotelyJsonConfig? LoadedConfig
        {
            get => _loadedConfig;
            set => SetProperty(ref _loadedConfig, value);
        }

        // CurrentActiveTab removed - using proper TabControl SelectedIndex binding

        public int MaxResults
        {
            get => _maxResults;
            set => SetProperty(ref _maxResults, value);
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => SetProperty(ref _timeoutSeconds, value);
        }

        public string DeckSelection
        {
            get => _deckSelection;
            set => SetProperty(ref _deckSelection, value);
        }

        public string StakeSelection
        {
            get => _stakeSelection;
            set => SetProperty(ref _stakeSelection, value);
        }

        public string SelectedWordList
        {
            get => _selectedWordList;
            set => SetProperty(ref _selectedWordList, value);
        }

        public ObservableCollection<string> AvailableWordLists
        {
            get => _availableWordLists;
            set => SetProperty(ref _availableWordLists, value);
        }

        public SearchProgress? LatestProgress
        {
            get => _latestProgress;
            set => SetProperty(ref _latestProgress, value);
        }

        public int LastKnownResultCount
        {
            get => _lastKnownResultCount;
            set => SetProperty(ref _lastKnownResultCount, value);
        }

        public ObservableCollection<Models.SearchResult> SearchResults { get; }
        public ObservableCollection<string> ConsoleOutput { get; }

        #endregion

        #region Commands

        public ICommand StartSearchCommand { get; }
        public ICommand StopSearchCommand { get; }
        public ICommand PauseSearchCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand LoadFilterCommand { get; }
        public ICommand CreateShortcutCommand { get; }
        public ICommand EnterVibeOutModeCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SelectTabCommand { get; }

        #endregion

        #region Events

        public event EventHandler<string>? CreateShortcutRequested;
        public event EventHandler? CloseRequested;

        #endregion

        #region Command Implementations

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

        private void ClearResults()
        {
            SearchResults.Clear();
            ConsoleOutput.Clear();
            _consoleBuffer.Clear();
            LastKnownResultCount = 0;
            LatestProgress = null;
            DebugLogger.Log("SearchModalViewModel", "Results cleared");
        }

        public Task LoadFilterAsync()
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

        private void CreateShortcut(string? searchId)
        {
            if (!string.IsNullOrEmpty(searchId))
            {
                CreateShortcutRequested?.Invoke(this, searchId);
            }
        }

        
        private void CloseModal()
        {
            DebugLogger.Log("SearchModalViewModel", "Closing modal");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SelectTab(object? parameter)
        {
            if (parameter is int tabIndex)
            {
                SelectedTabIndex = tabIndex;
            }
            else if (parameter is string tabIndexStr && int.TryParse(tabIndexStr, out int parsedIndex))
            {
                SelectedTabIndex = parsedIndex;
            }
        }
        
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
            DebugLogger.Log("SearchModalViewModel", $"Search completed with {SearchResults.Count} results");
        }

        #endregion

        #region Helper Methods

        private SearchCriteria BuildSearchCriteria()
        {
            if (string.IsNullOrEmpty(_currentFilterPath))
            {
                throw new InvalidOperationException("No filter path available - filter must be loaded first!");
            }
            
            return new SearchCriteria
            {
                ConfigPath = _currentFilterPath, // CRITICAL: Pass the filter path!
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

        private void PauseSearch()
        {
            if (_searchInstance != null && _isSearching)
            {
                _searchInstance.PauseSearch();
            }
        }

        private bool CanPauseSearch() => _isSearching;

        private async void ExportResults()
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
                exportText += $"Filter: {_loadedConfig?.Name ?? "Unknown"}\n";
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
                    foreach (var result in existingResults)
                    {
                        SearchResults.Add(result);
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
                _lastKnownResultCount = _searchInstance.ResultCount;
                
                // Update search state
                _isSearching = _searchInstance.IsRunning;
                
                // Trigger ALL UI property updates for live stats
                OnPropertyChanged(nameof(SearchStatus));
                OnPropertyChanged(nameof(SearchProgress));
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ResultsCount));
                OnPropertyChanged(nameof(IsSearching));
                
                DebugLogger.Log("SearchModalViewModel", $"🔄 RECONNECTED to search - Running: {_isSearching}, Results: {_lastKnownResultCount}");
                
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
            if (_searchInstance == null || !_isSearching) return;
            
            // Use a simple timer to refresh stats every 500ms while search is active
            Task.Run(async () =>
            {
                while (_searchInstance?.IsRunning == true && _isSearching)
                {
                    try
                    {
                        // Update result count from live search
                        var liveResultCount = _searchInstance.ResultCount;
                        if (liveResultCount != _lastKnownResultCount)
                        {
                            _lastKnownResultCount = liveResultCount;
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
                    _currentFilterPath = configPath; // CRITICAL: Store the path for the search!
                    
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
                DebugLogger.LogError("SearchModalViewModel", $"Failed to load config: {ex.Message}");
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
            _lastKnownResultCount = e.ResultsFound;
            OnPropertyChanged(nameof(SearchProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ResultsCount));
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

            // Create tab content as UserControls and expose as properties
            FilterTabContent = CreateFilterTabContent();
            SettingsTabContent = CreateSettingsTabContent();
            SearchTabContent = CreateSearchTabContent();
            ResultsTabContent = CreateResultsTabContent();

            TabItems.Add(new TabItemViewModel("🔍 FILTER", FilterTabContent));
            TabItems.Add(new TabItemViewModel("⚙️ SETTINGS", SettingsTabContent));
            TabItems.Add(new TabItemViewModel("🚀 SEARCH", SearchTabContent));
            TabItems.Add(new TabItemViewModel("📊 RESULTS", ResultsTabContent));
        }

        private UserControl CreateFilterTabContent()
        {
            // FIXED: Use the clean FilterSelector component with PanelSpinner (card-based UI)
            var filterSelector = new Components.FilterSelector
            {
                ShowSelectButton = true,
                ShowActionButtons = false, // Hide edit/copy/delete for now (just select to search)
                AutoLoadEnabled = true,
                Title = "SELECT FILTER FOR SEARCH"
            };

            // Wire up the FilterLoaded event - fires when user selects a filter
            filterSelector.FilterLoaded += async (sender, filterPath) =>
            {
                DebugLogger.Log("SearchModalViewModel", $"Filter selected: {filterPath}");
                _currentFilterPath = filterPath;
                await LoadFilterAsync(filterPath);

                // Auto-switch to SEARCH tab after loading filter
                SelectedTabIndex = 2;
            };

            // Create the deck and stake selector
            var deckStakeSelector = new Components.DeckAndStakeSelector();

            // Wire up the deck/stake selection events
            deckStakeSelector.SelectionChanged += (sender, selection) =>
            {
                var deckNames = new[] { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                                        "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
                var stakeNames = new[] { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };

                if (selection.deckIndex >= 0 && selection.deckIndex < deckNames.Length)
                    DeckSelection = deckNames[selection.deckIndex];

                if (selection.stakeIndex >= 0 && selection.stakeIndex < stakeNames.Length)
                    StakeSelection = stakeNames[selection.stakeIndex];

                DebugLogger.Log("SearchModalViewModel", $"Deck/Stake changed to {DeckSelection}/{StakeSelection}");
            };

            // KISS: Simple, clean layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // FilterSelector
            mainGrid.RowDefinitions.Add(new RowDefinition(new GridLength(15))); // Spacing
            mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Deck/Stake selector

            Grid.SetRow(filterSelector, 0);
            mainGrid.Children.Add(filterSelector);

            Grid.SetRow(deckStakeSelector, 2);
            mainGrid.Children.Add(deckStakeSelector);

            return new UserControl
            {
                Content = new Border
                {
                    Background = Avalonia.Media.Brush.Parse("#1e2b2d"),
                    CornerRadius = new Avalonia.CornerRadius(0, 8, 8, 8),
                    Padding = new Avalonia.Thickness(15),
                    Child = mainGrid
                }
            };
        }

        private UserControl CreateSettingsTabContent()
        {
            return new UserControl
            {
                Content = new Border
                {
                    Background = Avalonia.Media.Brush.Parse("#1e2b2d"),
                    CornerRadius = new Avalonia.CornerRadius(0, 8, 8, 8),
                    Padding = new Avalonia.Thickness(25),
                    Child = new StackPanel
                    {
                        Spacing = 15,
                        Children =
                        {
                            new TextBlock { Text = "SEARCH SETTINGS", FontSize = 24, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 0, 20) },
                            new StackPanel
                            {
                                Spacing = 15,
                                Children =
                                {
                                    new TextBlock { Text = "Number of Threads:", FontSize = 14 },
                                    new Slider
                                    {
                                        Minimum = 1,
                                        Maximum = Environment.ProcessorCount,
                                        Value = Math.Min(4, Environment.ProcessorCount),
                                        Width = 300,
                                        TickFrequency = 1,
                                        IsSnapToTickEnabled = true
                                    },
                                    new TextBlock { Text = "Batch Size:", FontSize = 14, Margin = new Avalonia.Thickness(0, 10, 0, 0) },
                                    new Slider
                                    {
                                        Minimum = 100,
                                        Maximum = 10000,
                                        Value = 1000,
                                        Width = 300,
                                        TickFrequency = 100,
                                        IsSnapToTickEnabled = true
                                    },
                                    new CheckBox
                                    {
                                        Content = "Enable progress notifications",
                                        IsChecked = true,
                                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                                    },
                                    new CheckBox
                                    {
                                        Content = "Auto-save results",
                                        IsChecked = false
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private UserControl CreateSearchTabContent()
        {
            var searchButton = new Button
            {
                Content = "START SEARCH",
                Command = StartSearchCommand,
                Background = Avalonia.Media.Brush.Parse("#00b300"),
                Padding = new Avalonia.Thickness(30, 15),
                FontSize = 18,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            
            var stopButton = new Button
            {
                Content = "STOP SEARCH",
                Command = StopSearchCommand,
                Background = Avalonia.Media.Brush.Parse("#d9534f"),
                Padding = new Avalonia.Thickness(30, 15),
                FontSize = 18,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            
            var vibeButton = new Button
            {
                Content = "🎵 VIBE OUT MODE 🎵",
                Command = EnterVibeOutModeCommand,
                Background = Avalonia.Media.Brush.Parse("#9b59b6"),
                Foreground = Avalonia.Media.Brush.Parse("#00FF88"),
                Padding = new Avalonia.Thickness(30, 15),
                FontSize = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            vibeButton.Classes.Add("vibe-button");
            
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children = { searchButton, stopButton, vibeButton }
            };
            
            var progressBar = new ProgressBar
            {
                [!ProgressBar.ValueProperty] = new Avalonia.Data.Binding("SearchProgress"),
                Maximum = 100,
                Height = 30,
                Margin = new Avalonia.Thickness(0, 20, 0, 10)
            };
            
            var statusText = new TextBlock
            {
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding("ProgressText"),
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            
            return new UserControl
            {
                Content = new Border
                {
                    Background = Avalonia.Media.Brush.Parse("#1e2b2d"),
                    CornerRadius = new Avalonia.CornerRadius(0, 8, 8, 8),
                    Padding = new Avalonia.Thickness(25),
                    Child = new StackPanel
                    {
                        Spacing = 20,
                        Children =
                        {
                            new TextBlock 
                            { 
                                Text = "SEARCH CONTROLS", 
                                FontSize = 24, 
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                            },
                            buttonPanel,
                            progressBar,
                            statusText
                        }
                    }
                }
            };
        }

        private UserControl CreateResultsTabContent()
        {
            // Simple results DataGrid with sorting
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserSortColumns = true,
                CanUserReorderColumns = true,
                CanUserResizeColumns = true,
                Background = Avalonia.Media.Brush.Parse("#1a1a1a"),
                Foreground = Avalonia.Media.Brush.Parse("#FFD700"),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
            
            // Bind to SearchResults
            dataGrid.Bind(DataGrid.ItemsSourceProperty, new Avalonia.Data.Binding("SearchResults"));
            
            // Add columns
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Seed",
                Binding = new Avalonia.Data.Binding("Seed"),
                Width = new DataGridLength(120),
                CanUserSort = true
            });
            
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Score",
                Binding = new Avalonia.Data.Binding("TotalScore"),
                Width = new DataGridLength(80),
                CanUserSort = true
            });
            
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Scores",
                Binding = new Avalonia.Data.Binding("ScoresDisplay"),
                Width = new DataGridLength(200),
                CanUserSort = false
            });
            
            // Double-click to copy seed
            dataGrid.DoubleTapped += async (s, e) =>
            {
                if (dataGrid.SelectedItem is Models.SearchResult result)
                {
                    try
                    {
                        await ClipboardService.CopyToClipboardAsync(result.Seed);
                        DebugLogger.Log("SearchModalViewModel", $"Copied seed: {result.Seed}");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("SearchModalViewModel", $"Copy failed: {ex.Message}");
                    }
                }
            };
            
            return new UserControl
            {
                Content = new Border
                {
                    Background = Avalonia.Media.Brush.Parse("#1e2b2d"),
                    CornerRadius = new Avalonia.CornerRadius(0, 8, 8, 8),
                    Padding = new Avalonia.Thickness(15),
                    Child = new StackPanel
                    {
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock 
                            {
                                Text = "SEARCH RESULTS",
                                FontSize = 18,
                                FontWeight = Avalonia.Media.FontWeight.Bold,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Foreground = Avalonia.Media.Brush.Parse("#00FF88")
                            },
                            new TextBlock
                            {
                                Text = "Double-click seed to copy to clipboard",
                                FontSize = 12,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Foreground = Avalonia.Media.Brush.Parse("#CCCCCC")
                            },
                            dataGrid
                        }
                    }
                }
            };
        }

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