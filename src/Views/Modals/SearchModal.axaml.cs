using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using ReactiveUI;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class SearchModal : UserControl, IDisposable
    {
        public event EventHandler<string>? CreateDesktopIconRequested;
        private readonly ObservableCollection<SearchResult> _searchResults = new();
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _currentSearchId = string.Empty;
        private bool _isSearching = false;

        // Tab panels
        private Panel? _filterPanel;
        private Panel? _settingsPanel;
        private Panel? _searchPanel;
        private Panel? _resultsPanel;

        // Tab buttons
        private Button? _filterTab;
        private Button? _settingsTab;
        private Button? _searchTab;
        private Button? _resultsTab;

        // Triangle pointer container (for animation)
        private Grid? _triangleContainer;

        // Controls
        private TextBox? _consoleOutput;
        
        // Console throttling
        private readonly List<string> _consoleBuffer = new List<string>();
        private DispatcherTimer? _consoleUpdateTimer;
        private readonly object _consoleBufferLock = new object();
        private int _consoleLineCount = 0;
        private const int MAX_CONSOLE_LINES = 500;

        // Action buttons
        private Button? _cookButton;
        private Button? _saveWidgetButton;

        // Balatro Spinners
        private SpinnerControl? _threadsSpinner;
        private SpinnerControl? _batchSizeSpinner;
        private SpinnerControl? _minScoreSpinner;
        private CheckBox? _debugCheckBox;

        // Deck and Stake selector component
        private DeckAndStakeSelector? _deckAndStakeSelector;
        
        // Track if current search is in debug mode
        private bool _isDebugMode = false;

        // New Search tab UI elements
        private TextBlock? _progressPercentText;
        private TextBlock? _batchesText;
        private TextBlock? _totalSeedsText;
        private TextBlock? _timeElapsedText;
        private TextBlock? _resultsFoundText;
        private TextBlock? _speedText;
        private TextBlock? _speedValueText;
        private TextBlock? _currentSpeedText;
        private TextBlock? _averageSpeedText;
        private TextBlock? _peakSpeedText;
        private Avalonia.Controls.Shapes.Path? _speedArc;
        private Avalonia.Controls.Shapes.Path? _speedArcBackground;

        // Speed tracking
        private double _peakSpeed = 0;
        private double _totalSeeds = 0;
        private DateTime _searchStartTime;
        private DateTime _lastSpeedUpdate = DateTime.UtcNow;
        private int _newResultsCount = 0; // Track only new results found in current search

        // Current filter info
        private string? _currentFilterPath;
        private FilterSelector? _filterSelector;
        
        // Result batching for UI performance
        private readonly List<SearchResult> _pendingResults = new();
        private System.Threading.Timer? _resultBatchTimer;
        private readonly object _resultBatchLock = new object();
        private DateTime _lastResultBatchUpdate = DateTime.UtcNow;
        private DateTime _lastProgressUpdate = DateTime.UtcNow;
        
        // Resume search support
        private ulong? _resumeFromBatch = null;

        // Results panel controls
        private ItemsControl? _resultsItemsControl;
        private StackPanel? _tallyHeadersPanel;
        private TextBlock? _resultsSummary;
        private Button? _exportResultsButton;
        private TextBlock? _jsonValidationStatus;
        private TextBox? _resultsFilterTextBox;
        private Button? _clearFilterButton;
        
        // Sorting
        private string _currentSortColumn = "score";
        private bool _sortAscending = false;
        private TextBlock? _seedSortIndicator;
        private TextBlock? _scoreSortIndicator;

        public SearchModal()
        {
            try
            {
                InitializeComponent();
                this.Unloaded += OnUnloaded;
                
                // Check for resumable search after UI is loaded
                Dispatcher.UIThread.InvokeAsync(CheckForResumableSearch, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Constructor error: {ex}");
                throw;
            }
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            // If search is running when modal closes, create desktop icon
            if (_isSearching && !string.IsNullOrEmpty(_currentFilterPath))
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "SearchModal",
                    "Creating desktop icon for ongoing search..."
                );
                CreateDesktopIconRequested?.Invoke(this, _currentFilterPath);
            }
        }

        private async void CheckForResumableSearch()
        {
            try
            {
                var userProfileService = ServiceHelper.GetService<UserProfileService>();

                if (userProfileService?.GetSearchState() is { } resumeState)
                {
                    // Check if the search is recent (within last 24 hours)
                    var timeSinceSearch = DateTime.UtcNow - resumeState.LastActiveTime;
                    if (timeSinceSearch.TotalHours > 24)
                    {
                        // Too old, clear it
                        userProfileService.ClearSearchState();
                        return;
                    }

                    // Simply restore the search state - user can hit Cook to continue
                    if (!string.IsNullOrEmpty(resumeState.ConfigPath))
                    {
                        await LoadFilterAsync(resumeState.ConfigPath);
                    }
                    
                    // Set the UI values
                    if (_threadsSpinner != null) _threadsSpinner.Value = resumeState.ThreadCount;
                    if (_batchSizeSpinner != null) _batchSizeSpinner.Value = resumeState.BatchSize - 1;
                    if (_minScoreSpinner != null) _minScoreSpinner.Value = resumeState.MinScore;
                    if (_deckAndStakeSelector != null)
                    {
                        _deckAndStakeSelector.SetDeck(resumeState.Deck ?? "Red");
                        _deckAndStakeSelector.SetStake(resumeState.Stake ?? "White");
                    }
                    
                    // Add console message about resumable state
                    AddToConsole($"──────────────────────────────────");
                    AddToConsole($"Previous search detected!");
                    AddToConsole($"Ready to resume from batch {resumeState.LastCompletedBatch + 1:N0}");
                    AddToConsole($"Press 'Cook' to continue searching");
                    
                    // Store the resume batch for when Cook is clicked
                    _resumeFromBatch = resumeState.LastCompletedBatch + 1;
                    
                    // DON'T clear the saved state - it will be cleared when search completes successfully
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Error checking for resumable search: {ex.Message}");
            }
        }

        private async Task ResumeSearch(SearchResumeState resumeState)
        {
            try
            {
                AddToConsole("──────────────────────────────────");
                AddToConsole($"Resuming search from batch {resumeState.LastCompletedBatch:N0}...");
                
                // Create a new search instance
                CreateNewSearchInstance();
                
                if (_searchInstance == null) return;
                
                // Load the config
                Motely.Filters.OuijaConfig? config = null;
                
                if (resumeState.IsDirectConfig && !string.IsNullOrEmpty(resumeState.ConfigJson))
                {
                    // Deserialize the saved config
                    config = JsonSerializer.Deserialize<Motely.Filters.OuijaConfig>(resumeState.ConfigJson);
                    _currentFilterPath = "Direct Config";
                }
                else if (!string.IsNullOrEmpty(resumeState.ConfigPath))
                {
                    // Load from file
                    _currentFilterPath = resumeState.ConfigPath;
                    await LoadFilterAsync(resumeState.ConfigPath);
                }
                
                if (config != null || !string.IsNullOrEmpty(_currentFilterPath))
                {
                    // Set UI values from saved state
                    if (_threadsSpinner != null) _threadsSpinner.Value = resumeState.ThreadCount;
                    if (_batchSizeSpinner != null) _batchSizeSpinner.Value = resumeState.BatchSize - 1; // UI shows 0-indexed
                    if (_minScoreSpinner != null) _minScoreSpinner.Value = resumeState.MinScore;
                    if (_deckAndStakeSelector != null)
                    {
                        _deckAndStakeSelector.SetDeck(resumeState.Deck ?? "Red");
                        _deckAndStakeSelector.SetStake(resumeState.Stake ?? "White");
                    }
                    
                    // Switch to Search tab
                    if (_searchTab != null)
                    {
                        OnTabClick(_searchTab, new RoutedEventArgs());
                    }
                    
                    // Start the search from the saved position
                    var searchConfig = new SearchConfiguration
                    {
                        ThreadCount = resumeState.ThreadCount,
                        MinScore = resumeState.MinScore,
                        BatchSize = resumeState.BatchSize,
                        StartBatch = resumeState.LastCompletedBatch + 1, // Resume from next batch
                        EndBatch = resumeState.EndBatch,
                        DebugMode = _debugCheckBox?.IsChecked ?? false,
                        DebugSeed = (_debugCheckBox?.IsChecked ?? false) ? this.FindControl<TextBox>("DebugSeedInput")?.Text : null,
                        Deck = resumeState.Deck,
                        Stake = resumeState.Stake
                    };
                    
                    _isSearching = true;
                    UpdateSearchUI();
                    _searchStartTime = _searchInstance.SearchStartTime; // Use actual search start time
                    
                    // Enable the stop button
                    if (_cookButton != null)
                    {
                        _cookButton.IsEnabled = true;
                    }
                    
                    if (config != null)
                    {
                        // Direct config
                        await _searchInstance.StartSearchWithConfigAsync(config, searchConfig);
                    }
                    else
                    {
                        // File-based config
                        var searchCriteria = new SearchCriteria
                        {
                            ConfigPath = _currentFilterPath,
                            ThreadCount = searchConfig.ThreadCount,
                            MinScore = searchConfig.MinScore,
                            BatchSize = searchConfig.BatchSize,
                            StartBatch = searchConfig.StartBatch,
                            EndBatch = searchConfig.EndBatch,
                            EnableDebugOutput = searchConfig.DebugMode,
                            Deck = searchConfig.Deck,
                            Stake = searchConfig.Stake
                        };
                        
                        await _searchInstance.StartSearchAsync(searchCriteria);
                    }
                    
                    AddToConsole($"Search resumed successfully from batch {resumeState.LastCompletedBatch + 1}");
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Error resuming search: {ex.Message}");
                AddToConsole($"Failed to resume search: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            try
            {
                AvaloniaXamlLoader.Load(this);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Failed to load XAML: {ex}");
                throw;
            }

            try
            {
                // Find panels
                _filterPanel = this.FindControl<Panel>("FilterPanel");
                _settingsPanel = this.FindControl<Panel>("SettingsPanel");
                _searchPanel = this.FindControl<Panel>("SearchPanel");
                _resultsPanel = this.FindControl<Panel>("ResultsPanel");

                // Find tab buttons
                _filterTab = this.FindControl<Button>("FilterTab");
                _settingsTab = this.FindControl<Button>("SettingsTab");
                _searchTab = this.FindControl<Button>("SearchTab");
                _resultsTab = this.FindControl<Button>("ResultsTab");
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "SearchModal",
                    $"Failed to find panels/tabs: {ex}"
                );
                throw;
            }

            // Find triangle pointer's parent Grid (for animation)
            var tabTriangle = this.FindControl<Polygon>("TabTriangle");
            _triangleContainer = tabTriangle?.Parent as Grid;

            // Find controls
            _consoleOutput = this.FindControl<TextBox>("ConsoleOutput");
            _jsonValidationStatus = this.FindControl<TextBlock>("JsonValidationStatus");

            // Find buttons
            _cookButton = this.FindControl<Button>("CookButton");
            _saveWidgetButton = this.FindControl<Button>("SaveWidgetButton");

            // Find Balatro spinners and set up their ranges
            _threadsSpinner = this.FindControl<SpinnerControl>("ThreadsSpinner");
            _batchSizeSpinner = this.FindControl<SpinnerControl>("BatchSizeSpinner");
            _minScoreSpinner = this.FindControl<SpinnerControl>("MinScoreSpinner");
            
            // Configure spinner display values
            if (_threadsSpinner != null)
            {
                _threadsSpinner.Maximum = Environment.ProcessorCount;
            }
            
            if (_batchSizeSpinner != null)
            {
                _batchSizeSpinner.DisplayValues = new[] { "minimal", "low", "default", "high" };
            }
            
            if (_minScoreSpinner != null)
            {
                _minScoreSpinner.DisplayValues = new[] { "Auto", "1", "2", "3", "4", "5" };
            }

            // Find deck/stake selector component
            _deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");
            if (_deckAndStakeSelector != null)
            {
                // Connect the DeckSelected event to automatically advance to Search tab
                _deckAndStakeSelector.DeckSelected += OnDeckSelected;
            }
            _debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");

            // Find filter selector component
            _filterSelector = this.FindControl<FilterSelector>("FilterSelector");
            if (_filterSelector != null)
            {
                // Hide the "New Blank Filter" button in SearchModal
                _filterSelector.ShowCreateButton = false;

                // Connect the FilterLoaded event
                _filterSelector.FilterLoaded += OnFilterSelected;
            }

            // Find results panel controls
            _resultsItemsControl = this.FindControl<ItemsControl>("ResultsItemsControl");
            _tallyHeadersPanel = this.FindControl<StackPanel>("TallyHeadersPanel");
            _resultsSummary = this.FindControl<TextBlock>("ResultsSummary");
            _exportResultsButton = this.FindControl<Button>("ExportResultsButton");
            _resultsFilterTextBox = this.FindControl<TextBox>("ResultsFilterTextBox");
            _clearFilterButton = this.FindControl<Button>("ClearFilterButton");
            
            // Find sort indicators
            _seedSortIndicator = this.FindControl<TextBlock>("SeedSortIndicator");
            _scoreSortIndicator = this.FindControl<TextBlock>("ScoreSortIndicator");
            
            // Wire up filter text changed event
            if (_resultsFilterTextBox != null)
            {
                _resultsFilterTextBox.TextChanged += OnFilterTextChanged;
            }

            // Set up results items control
            if (_resultsItemsControl != null)
            {
                _resultsItemsControl.ItemsSource = _searchResults;
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"ItemsControl initialized with {_searchResults.Count} items");
            }
            else
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", "Failed to find ResultsItemsControl!");
            }

            // Find new Search tab UI elements
            _progressPercentText = this.FindControl<TextBlock>("ProgressPercentText");
            _batchesText = this.FindControl<TextBlock>("BatchesText");
            _totalSeedsText = this.FindControl<TextBlock>("TotalSeedsText");
            _timeElapsedText = this.FindControl<TextBlock>("TimeElapsedText");
            _resultsFoundText = this.FindControl<TextBlock>("ResultsFoundText");
            _speedText = this.FindControl<TextBlock>("SpeedText");
            _speedValueText = this.FindControl<TextBlock>("SpeedValueText");
            _currentSpeedText = this.FindControl<TextBlock>("CurrentSpeedText");
            _averageSpeedText = this.FindControl<TextBlock>("AverageSpeedText");
            _peakSpeedText = this.FindControl<TextBlock>("PeakSpeedText");
            _speedArc = this.FindControl<Avalonia.Controls.Shapes.Path>("SpeedArc");
            _speedArcBackground = this.FindControl<Avalonia.Controls.Shapes.Path>(
                "SpeedArcBackground"
            );

            // Set up threads spinner with processor count
            if (_threadsSpinner != null)
            {
                _threadsSpinner.Maximum = Environment.ProcessorCount;
                _threadsSpinner.Value = Math.Min(4, Environment.ProcessorCount);
            }

            // Initialize search manager
            _searchManager = App.GetService<SearchManager>();

            // DeckStakeSelector is already initialized and will handle its own display

            // The FilterSelector component will handle loading available filters
            // Enable tabs only after a filter is loaded
            UpdateTabStates(false);
        }

        private async void OnCopySeedClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string seed)
            {
                try
                {
                    if (
                        Application.Current?.ApplicationLifetime
                        is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    )
                    {
                        var mainWindow = desktop.MainWindow;
                        if (mainWindow?.Clipboard != null)
                        {
                            await mainWindow.Clipboard.SetTextAsync(seed);
                        }
                    }
                    AddToConsole($"Copied seed {seed} to clipboard");

                    // Visual feedback
                    var originalContent = button.Content;
                    button.Content = "✓";
                    await Task.Delay(1000);
                    button.Content = originalContent;
                }
                catch (Exception ex)
                {
                    AddToConsole($"Failed to copy seed: {ex.Message}");
                }
            }
        }

        private bool _isLoadingFilter = false;
        
        private async void OnFilterSelected(object? sender, string filterPath)
        {
            // Prevent double loading while in progress
            if (_isLoadingFilter)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"Filter load already in progress");
                return;
            }
            
            // If same filter, just switch to Settings tab
            if (_currentFilterPath == filterPath)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"Filter already loaded, switching to Settings tab");
                // Switch to Deck/Stake tab since filter is already loaded
                if (_settingsTab != null)
                {
                    OnTabClick(_settingsTab, new RoutedEventArgs());
                }
                return;
            }
            
            _isLoadingFilter = true;
            try
            {
                await LoadFilterAsync(filterPath);
            }
            finally
            {
                _isLoadingFilter = false;
            }
        }

        private void OnDeckSelected(object? sender, EventArgs e)
        {
            // Automatically advance to the Search tab when deck is selected
            if (_searchTab != null)
            {
                OnTabClick(_searchTab, new RoutedEventArgs());
            }
        }

        private void OnNewFilterRequested(object? sender, EventArgs e)
        {
            // Close this modal and open the filters modal
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window != null)
            {
                // Find and close the current modal
                var modalHost = window.FindControl<Grid>("ModalHost");
                if (modalHost != null && modalHost.Children.Count > 0)
                {
                    modalHost.Children.Clear();

                    // Open the filters modal
                    var filtersModal = new BalatroSeedOracle.Views.Modals.FiltersModalContent();
                    modalHost.Children.Add(filtersModal);
                }
            }
        }

        private void UpdateTabStates(bool filterLoaded)
        {
            // Check if we have an active search instance (might be from a shortcut)
            bool hasActiveSearch = _searchInstance != null && (_searchInstance.IsRunning || _searchInstance.Results.Count > 0);
            bool shouldEnableTabs = filterLoaded || hasActiveSearch;
            
            if (_settingsTab != null)
            {
                _settingsTab.IsEnabled = shouldEnableTabs;
            }

            if (_searchTab != null)
            {
                _searchTab.IsEnabled = shouldEnableTabs;
            }
            // Enable Results tab if filter is loaded or we have results
            if (_resultsTab != null)
            {
                _resultsTab.IsEnabled = shouldEnableTabs || _searchResults.Count > 0;
            }

        }

        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedTab)
            {
                return;
            }

            // Remove active class from all tabs

            _filterTab?.Classes.Remove("active");
            _settingsTab?.Classes.Remove("active");
            _searchTab?.Classes.Remove("active");
            _resultsTab?.Classes.Remove("active");

            // Move triangle container to correct column
            if (_triangleContainer != null)
            {
                int column = clickedTab.Name switch
                {
                    "FilterTab" => 0,
                    "SettingsTab" => 1,
                    "SearchTab" => 2,
                    "ResultsTab" => 3,
                    _ => 0,
                };
                Grid.SetColumn(_triangleContainer, column);
            }

            // Hide all panels
            if (_filterPanel != null)
            {
                _filterPanel.IsVisible = false;
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.IsVisible = false;
            }

            if (_searchPanel != null)
            {
                _searchPanel.IsVisible = false;
            }

            if (_resultsPanel != null)
            {
                _resultsPanel.IsVisible = false;
            }

            // Show the clicked panel and mark tab as active

            if (clickedTab == _filterTab)
            {
                clickedTab.Classes.Add("active");
                if (_filterPanel != null)
                {
                    _filterPanel.IsVisible = true;
                }
            }
            else if (clickedTab == _settingsTab)
            {
                clickedTab.Classes.Add("active");
                if (_settingsPanel != null)
                {
                    _settingsPanel.IsVisible = true;
                }
            }
            else if (clickedTab == _searchTab)
            {
                clickedTab.Classes.Add("active");
                if (_searchPanel != null)
                {
                    _searchPanel.IsVisible = true;
                }
            }
            else if (clickedTab == _resultsTab)
            {
                clickedTab.Classes.Add("active");
                if (_resultsPanel != null)
                {
                    _resultsPanel.IsVisible = true;
                    BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"Results panel visible: {_resultsPanel.IsVisible}");
                }

                // Ensure ItemsControl is bound 
                if (_resultsItemsControl != null)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"ItemsControl HasItems: {_searchResults.Count}");
                    
                    if (_resultsItemsControl.ItemsSource != _searchResults)
                    {
                        _resultsItemsControl.ItemsSource = _searchResults;
                    }
                    
                    // Set up dynamic column headers for tally scores if not already done
                    UpdateTallyHeaders();
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", "ResultsItemsControl is null!");
                }

                // Apply filter and update summary when opening
                ApplyFilter();
            }
        }

        public async Task LoadFilterAsync(string filePath)
        {
            _currentFilterPath = filePath;

            // Create a search instance if we don't have one
            if (string.IsNullOrEmpty(_currentSearchId))
            {
                CreateNewSearchInstance();
            }

            Motely.Filters.OuijaConfig? config = null;
            try
            {
                // Load the config file asynchronously - proper async I/O
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    config = JsonSerializer.Deserialize<Motely.Filters.OuijaConfig>(json, options);
                }

                if (config != null)
                {
                    // Just log that we loaded successfully
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        $"Filter loaded successfully: {config.Name}"
                    );

                    // Update JSON validation status
                    UpdateJsonValidationStatus(true, "Valid ✓");

                    // Initialize the search history service with this filter
                    var historyService = App.GetService<SearchHistoryService>();
                    if (historyService != null)
                    {
                        historyService.SetFilterName(filePath);
                    }

                    // Cook button should always be enabled when a filter is loaded
                    if (_cookButton != null)
                    {
                        _cookButton.IsEnabled = true;
                    }

                    // Enable tabs now that filter is loaded
                    UpdateTabStates(true);

                    // Switch to Deck/Stake tab when filter is selected
                    if (_settingsTab != null)
                    {
                        OnTabClick(_settingsTab, new RoutedEventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Error loading filter: {ex.Message}");
            }
        }
        /// <summary>
        /// Load a config object directly and start search
        /// </summary>
        public async Task LoadConfigDirectlyAsync(
            Motely.Filters.OuijaConfig config,
            bool autoStartSearch = true
        )
        {
            // Create a new search instance if we don't have one
            if (string.IsNullOrEmpty(_currentSearchId))
            {
                CreateNewSearchInstance();
            }

            if (_searchInstance != null)
            {
                // Set the filter name for history tracking
                var historyService = App.GetService<SearchHistoryService>();
                if (historyService != null)
                {
                    historyService.SetFilterName($"Direct:{config.Name ?? "Unnamed"}");
                }

                // Cook button should always be enabled when a filter is loaded
                if (_cookButton != null)
                {
                    _cookButton.IsEnabled = true;
                }

                // Enable tabs now that filter is loaded
                UpdateTabStates(true);

                // Switch to Deck/Stake tab
                if (_settingsTab != null)
                {
                    OnTabClick(_settingsTab, new RoutedEventArgs());
                }

                // Add to console
                AddToConsole($"Filter loaded: {config.Name ?? "Unnamed"}");

                // Refresh the filter selector to show the current filter
                _filterSelector?.RefreshFilters();

                // Only auto-start search if requested (e.g., from FiltersModal)
                if (autoStartSearch)
                {
                    // Switch to Search tab
                    if (_searchTab != null)
                    {
                        OnTabClick(_searchTab, new RoutedEventArgs());
                    }

                    // Wait a moment for UI to update
                    await Task.Delay(100);

                    // Start the search directly with the config object
                    int batchSize = (_batchSizeSpinner?.Value ?? 2) + 1; // Convert 0-3 to 1-4 for actual batch size
                    _isDebugMode = _debugCheckBox?.IsChecked ?? false;
                    var searchConfig = new SearchConfiguration
                    {
                        ThreadCount = _threadsSpinner?.Value ?? 4,
                        MinScore = _minScoreSpinner?.Value ?? 0,
                        BatchSize = batchSize,
                        StartBatch = _resumeFromBatch ?? 0,
                        EndBatch = GetMaxBatchesForBatchSize(batchSize),
                        DebugMode = _isDebugMode,
                        DebugSeed = _isDebugMode ? this.FindControl<TextBox>("DebugSeedInput")?.Text : null,
                        Deck = _deckAndStakeSelector?.SelectedDeckName ?? "Red",
                        Stake = _deckAndStakeSelector?.SelectedStakeName ?? "White",
                    };

                    AddToConsole("──────────────────────────────────");
                    if (_resumeFromBatch.HasValue)
                    {
                        AddToConsole($"Resuming search from batch {_resumeFromBatch.Value:N0}...");
                    }
                    else
                    {
                        AddToConsole("Let Jimbo cook! Starting search...");
                    }

                    // Start search with the config object directly
                    await _searchInstance.StartSearchWithConfigAsync(config, searchConfig);
                }
            }
        }

        private async void OnCookClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilterPath))
                {
                    AddToConsole("Error: No filter loaded. Please select a filter first.");
                    return;
                }

                // Create a new search instance if we don't have one
                if (string.IsNullOrEmpty(_currentSearchId))
                {
                    CreateNewSearchInstance();
                }

                if (_searchInstance == null)
                {
                    AddToConsole(
                        "Error: Failed to create search instance. Search manager might not be initialized."
                    );
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "SearchModal",
                        "SearchInstance is null after CreateNewSearchInstance"
                    );
                    return;
                }

                if (!_isSearching)
                {
                    // Start search
                    _searchResults.Clear();
                    _newResultsCount = 0; // Reset new results counter

                    // Reset the Motely cancellation flag
                    Motely.Filters.OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;

                    // Get parameters from Balatro spinners
                    int batchSize = (_batchSizeSpinner?.Value ?? 2) + 1; // Convert 0-3 to 1-4 for actual batch size (minimal=1, low=2, default=3, high=4)
                    _isDebugMode = _debugCheckBox?.IsChecked ?? false;
                    var config = new SearchConfiguration
                    {
                        ThreadCount = _threadsSpinner?.Value ?? 4,
                        MinScore = _minScoreSpinner?.Value ?? 0, // 0 = Auto, 1-5 = actual values
                        BatchSize = batchSize,
                        StartBatch = _resumeFromBatch ?? 0,
                        EndBatch = GetMaxBatchesForBatchSize(batchSize),
                        DebugMode = _isDebugMode,
                        DebugSeed = _isDebugMode ? this.FindControl<TextBox>("DebugSeedInput")?.Text : null,
                        Deck = _deckAndStakeSelector?.SelectedDeckName ?? "Red",
                        Stake = _deckAndStakeSelector?.SelectedStakeName ?? "White",
                    };

                    AddToConsole("──────────────────────────────────");
                    if (_resumeFromBatch.HasValue)
                    {
                        AddToConsole($"Resuming search from batch {_resumeFromBatch.Value:N0}...");
                    }
                    else
                    {
                        AddToConsole("Let Jimbo cook! Starting search...");
                    }

                    // Start search
                    var searchCriteria = new BalatroSeedOracle.Models.SearchCriteria
                    {
                        ConfigPath = _currentFilterPath,
                        ThreadCount = config.ThreadCount,
                        MinScore = config.MinScore,
                        BatchSize = config.BatchSize,
                        Deck = config.Deck,
                        Stake = config.Stake,
                        EnableDebugOutput = config.DebugMode,
                        DebugSeed = config.DebugSeed,
                    };

                    await _searchInstance.StartSearchAsync(searchCriteria);
                }
                else
                {
                    // Stop search - immediately update button to show stopping state
                    if (_cookButton != null)
                    {
                        _cookButton.Content = "Stopping Search...";
                        _cookButton.IsEnabled = false;
                    }
                    
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        $"STOP button clicked - _searchInstance is {(_searchInstance != null ? "not null" : "null")}"
                    );
                    AddToConsole("Stopping search...");
                    
                    // Force UI update to show the button change immediately
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Render);

                    // Clear any pending results
                    lock (_resultBatchLock)
                    {
                        _pendingResults.Clear();
                    }

                    try
                    {
                        if (_searchInstance != null)
                        {
                            // Stop the search
                            _searchInstance.StopSearch();
                            
                            // Immediately update UI to show stopped state
                            _isSearching = false;
                            UpdateSearchUI();
                            
                            AddToConsole("Search stopped!");
                        }
                        else
                        {
                            AddToConsole("Warning: Search instance was null");
                        }
                    }
                    catch (Exception stopEx)
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.LogError(
                            "SearchModal",
                            $"Error stopping search: {stopEx}"
                        );
                        AddToConsole($"Error stopping search: {stopEx.Message}");
                    }
                    finally
                    {
                        // ALWAYS update UI to restore normal state
                        _isSearching = false;
                        UpdateSearchUI();

                        // Force enable the cook button to ensure UI isn't stuck
                        if (_cookButton != null)
                        {
                            _cookButton.IsEnabled = true;
                            _cookButton.Content = "Let Jimbo COOK!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"Error starting search: {ex.Message}");
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"OnCookClick error: {ex}");
                _isSearching = false;
                UpdateSearchUI();
            }
        }

        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                _isSearching = true;
                _searchStartTime = DateTime.UtcNow;
                _peakSpeed = 0;
                _totalSeeds = 0;
                _lastSpeedUpdate = DateTime.UtcNow;
                _newResultsCount = 0; // Reset new results counter
                
                // Generate column headers from the filter config
                GenerateTableHeadersFromConfig();

                // Update cook button
                if (_cookButton != null)
                {
                    _cookButton.Content = "STOP SEARCH";
                    // Just add the stop class, don't remove and re-add cook-button
                    _cookButton.Classes.Add("stop");
                }

                // Enable results tab
                if (_resultsTab != null)
                {
                    _resultsTab.IsEnabled = true;
                }

                // Load existing results from .duckdb file if available (skip in debug mode)
                if (!_isDebugMode)
                {
                    AddToConsole("──────────────────────────────────");
                    AddToConsole("Checking for existing results...");
                    await LoadExistingResultsAsync();
                }
                else
                {
                    AddToConsole("──────────────────────────────────");
                    AddToConsole("Debug mode: Skipping database check");
                }

                // Enable save widget button
                if (_saveWidgetButton != null)
                {
                    _saveWidgetButton.IsEnabled = true;
                }
            });
        }

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        "Search completed - resetting UI"
                    );
                    _isSearching = false;

                    // Update UI using the central method
                    UpdateSearchUI();

                    // Only show finished message if we have results (not cancelled)
                    if (_searchResults.Count > 0)
                    {
                        AddToConsole("Search completed!");
                    }
                }
                catch (Exception ex)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "SearchModal",
                        $"Error in OnSearchCompleted: {ex}"
                    );
                }
                finally
                {
                    // Ensure UI is never stuck
                    _isSearching = false;
                    if (_cookButton != null)
                    {
                        _cookButton.IsEnabled = true;
                        _cookButton.Content = "Let Jimbo COOK!";
                    }
                }
            });
        }

        private void OnClearConsoleClick(object? sender, RoutedEventArgs e)
        {
            if (_consoleOutput != null)
            {
                _consoleOutput.Text = "> Motely Search Console\n> Ready for Jimbo to cook...\n";
                _consoleLineCount = 2;
            }
            
            // Clear buffer too
            lock (_consoleBufferLock)
            {
                _consoleBuffer.Clear();
            }
        }
        
        private void GenerateTableHeadersFromConfig()
        {
            if (_tallyHeadersPanel == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", "Cannot generate headers - missing panel");
                return;
            }
            
            // Clear existing headers
            _tallyHeadersPanel.Children.Clear();
            
            // Try to load the config
            Motely.Filters.OuijaConfig? config = null;
            if (!string.IsNullOrEmpty(_currentFilterPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_currentFilterPath);
                    config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.OuijaConfig>(json);
                }
                catch (Exception ex)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Failed to load config for headers: {ex.Message}");
                }
            }
            
            if (config == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", "No config available for headers");
                return;
            }
            
            var labels = new List<string>();
            
            // Add Must items
            if (config.Must != null)
            {
                foreach (var item in config.Must)
                {
                    // Generate label from value and edition
                    var label = item.Value ?? item.Type ?? "Unknown";
                    if (!string.IsNullOrEmpty(item.Edition) && item.Edition != "none")
                    {
                        label = $"{label} {item.Edition}";
                    }
                    labels.Add(label);
                }
            }
            
            // Add Should items  
            if (config.Should != null)
            {
                foreach (var item in config.Should)
                {
                    // Generate label from value and edition
                    var label = item.Value ?? item.Type ?? "Unknown";
                    if (!string.IsNullOrEmpty(item.Edition) && item.Edition != "none")
                    {
                        label = $"{label} {item.Edition}";
                    }
                    labels.Add(label);
                }
            }
            
            BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"Generated {labels.Count} headers from config");
            
            // Add headers for each item
            foreach (var label in labels)
            {
                var header = new TextBlock
                {
                    Text = label,
                    FontFamily = App.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground = App.Current?.FindResource("Gold") as IBrush ?? Brushes.Gold,
                    Width = 80,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 4),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                
                _tallyHeadersPanel.Children.Add(header);
            }
        }
        
        private void UpdateTallyHeaders()
        {
            // Try to generate from config first
            if (_tallyHeadersPanel != null && _tallyHeadersPanel.Children.Count == 0)
            {
                GenerateTableHeadersFromConfig();
            }
        }
        
        private void UnsubscribeFromSearchEvents()
        {
            if (_searchInstance != null)
            {
                _searchInstance.ProgressUpdated -= OnSearchProgressUpdated;
                _searchInstance.ResultFound -= OnResultFound;
                _searchInstance.SearchStarted -= OnSearchStarted;
                _searchInstance.SearchCompleted -= OnSearchCompleted;
                _searchInstance.CutoffChanged -= OnCutoffChanged;
                _searchInstance.ConsoleOutput -= OnConsoleOutput;
            }
        }

        private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            // Throttle progress updates to reduce UI load
            var now = DateTime.UtcNow;
            if ((now - _lastProgressUpdate).TotalMilliseconds < 50)  // Update every 50ms for very responsive speed display
                return;
                
            _lastProgressUpdate = now;
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Don't update if search was stopped
                if (!_isSearching)
                    return;
                    
                // Progress updates are now handled by the UI elements, no console spam needed

                // Update progress stats
                if (_progressPercentText != null)
                {
                    // The percentage should already be correctly calculated in SearchInstance
                    // based on the actual search range (accounting for resume)
                    _progressPercentText.Text = $"{e.PercentComplete}%";
                }

                if (_batchesText != null)
                {
                    _batchesText.Text = $"{e.BatchesSearched:N0}";
                }


                if (_totalSeedsText != null)
                {
                    _totalSeeds = e.SeedsSearched;
                    _totalSeedsText.Text = $"{e.SeedsSearched:N0}";
                }

                if (_resultsFoundText != null)
                {
                    _resultsFoundText.Text = _newResultsCount.ToString();
                }

                // Update time elapsed

                if (_timeElapsedText != null)
                {
                    var elapsed = DateTime.UtcNow - _searchStartTime;
                    _timeElapsedText.Text = $"{elapsed:hh\\:mm\\:ss}";
                }

                // Update speedometer
                UpdateSpeedometer(e.SeedsPerMillisecond, e.BatchesSearched);
            });
        }

        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            // Instead of immediately updating UI, batch the results
            lock (_resultBatchLock)
            {
                _pendingResults.Add(e.Result);
                _newResultsCount++; // Increment new results counter
                
                // Start or reset the batch timer
                if (_resultBatchTimer == null)
                {
                    _resultBatchTimer = new System.Threading.Timer(
                        ProcessBatchedResults,
                        null,
                        100, // Initial delay of 100ms
                        System.Threading.Timeout.Infinite // Don't repeat
                    );
                }
                else
                {
                    // Reset the timer to delay processing if more results are coming
                    _resultBatchTimer.Change(100, System.Threading.Timeout.Infinite);
                }
            }
        }
        
        private void OnCutoffChanged(object? sender, int newCutoff)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_minScoreSpinner != null)
                {
                    _minScoreSpinner.Value = newCutoff;
                    BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", $"Auto-cutoff updated spinner to: {newCutoff}");
                    AddToConsole($"⚡ Auto-cutoff adjusted to: {newCutoff} (filtering out scores below {newCutoff})");
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", "MinScoreSpinner is null in OnCutoffChanged");
                }
            });
        }
        
        private void ProcessBatchedResults(object? state)
        {
            List<SearchResult> resultsToAdd;
            int totalNewResults;
            
            lock (_resultBatchLock)
            {
                if (_pendingResults.Count == 0)
                    return;
                    
                resultsToAdd = new List<SearchResult>(_pendingResults);
                _pendingResults.Clear();
                totalNewResults = _newResultsCount;
            }
            
            // Post to UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // If search was stopped, don't add any more results
                if (!_isSearching)
                    return;
                    
                // Log first result to debug tally scores
                if (resultsToAdd.Count > 0 && _searchResults.Count == 0)
                {
                    var firstResult = resultsToAdd[0];
                    BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", 
                        $"First result: TallyScores={firstResult.TallyScores?.Length ?? 0}, ItemLabels={firstResult.ItemLabels?.Length ?? 0}");
                    if (firstResult.TallyScores != null && firstResult.TallyScores.Length > 0)
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", 
                            $"Tally scores: {string.Join(", ", firstResult.TallyScores)}");
                    }
                    if (firstResult.ItemLabels != null && firstResult.ItemLabels.Length > 0)
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", 
                            $"Item labels: {string.Join(", ", firstResult.ItemLabels)}");
                    }
                }
                    
                // Add all pending results at once
                foreach (var result in resultsToAdd)
                {
                    _searchResults.Add(result);
                }
                
                // Show all seeds with scores
                foreach (var result in resultsToAdd)
                {
                    AddToConsole($"🎉 {result.Seed} - Score: {result.Score:0}");
                }

                // Update results summary
                if (_resultsSummary != null)
                {
                    var existingCount = _searchResults.Count - totalNewResults;
                    if (existingCount > 0)
                    {
                        _resultsSummary.Text = $"Total: {_searchResults.Count} results ({existingCount} from DB + {totalNewResults} new)";
                    }
                    else
                    {
                        _resultsSummary.Text = $"Found {_searchResults.Count} results";
                    }
                }
                
                // Update tally headers if this is the first batch
                if (_searchResults.Count == resultsToAdd.Count)
                {
                    UpdateTallyHeaders();
                }

                // Enable results tab export button
                if (_exportResultsButton != null)
                {
                    _exportResultsButton.IsEnabled = _searchResults.Count > 0;
                }
            });
        }

        private void AddToConsole(string message)
        {
            if (_consoleOutput != null)
            {
                var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
                _consoleOutput.Text += $"[{timestamp}] {message}\n";

                // Auto-scroll to bottom
                _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
            }
        }
        
        private void OnConsoleOutput(object? sender, string line)
        {
            // Update console immediately on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddToConsole(line);
            });
        }
        
        private void StartConsoleUpdateTimer()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_consoleUpdateTimer == null)
                {
                    _consoleUpdateTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(50) // Update 20 times per second for faster response
                    };
                    _consoleUpdateTimer.Tick += OnConsoleUpdateTimerTick;
                }
                
                if (!_consoleUpdateTimer.IsEnabled)
                {
                    _consoleUpdateTimer.Start();
                }
            });
        }
        
        private void OnConsoleUpdateTimerTick(object? sender, EventArgs e)
        {
            List<string> linesToAdd;
            lock (_consoleBufferLock)
            {
                if (_consoleBuffer.Count == 0)
                {
                    _consoleUpdateTimer?.Stop();
                    return;
                }
                
                // Take all buffered lines
                linesToAdd = new List<string>(_consoleBuffer);
                _consoleBuffer.Clear();
            }
            
            if (_consoleOutput != null && linesToAdd.Count > 0)
            {
                // Check if we need to trim old lines
                _consoleLineCount += linesToAdd.Count;
                
                if (_consoleLineCount > MAX_CONSOLE_LINES)
                {
                    // Keep only the last MAX_CONSOLE_LINES lines
                    var allLines = (_consoleOutput.Text ?? string.Empty).Split('\n').ToList();
                    allLines.AddRange(linesToAdd);
                    
                    if (allLines.Count > MAX_CONSOLE_LINES)
                    {
                        allLines = allLines.Skip(allLines.Count - MAX_CONSOLE_LINES).ToList();
                    }
                    
                    _consoleOutput.Text = string.Join("\n", allLines);
                    _consoleLineCount = allLines.Count;
                }
                else
                {
                    // Just append the new lines
                    _consoleOutput.Text += string.Join("\n", linesToAdd) + "\n";
                }
                
                // Auto-scroll to bottom
                _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
            }
        }
        
        private void RestoreConsoleHistory()
        {
            if (_searchInstance == null || _consoleOutput == null)
                return;
                
            // Clear and restore console from history
            _consoleOutput.Text = "> Motely Search Console\n> Restoring previous output...\n";
            
            var history = _searchInstance.GetConsoleHistory();
            if (history != null && history.Count > 0)
            {
                _consoleOutput.Text += "──────────────────────────────────\n";
                foreach (var line in history)
                {
                    _consoleOutput.Text += line + "\n";
                }
                _consoleOutput.Text += "──────────────────────────────────\n";
                _consoleOutput.Text += "Console restored from previous session\n";
            }
            else
            {
                _consoleOutput.Text += "> No previous output to restore\n";
            }
            
            // Auto-scroll to bottom
            _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
        }

        private void UpdateJsonValidationStatus(bool isValid, string message)
        {
            if (_jsonValidationStatus != null)
            {
                _jsonValidationStatus.Text = $"JSON: {message}";
                _jsonValidationStatus.Foreground = isValid
                    ? Application.Current?.FindResource("Green") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#4CAF50"))
                    : Application.Current?.FindResource("Red") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#F44336"));
            }
        }

        private async void OnExportResultsClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Search Results",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV Files")
                    {
                        Patterns = new[] { "*.csv" },
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" },
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" },
                    },
                },
            };

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);

            if (result != null)
            {
                try
                {
                    var historyService = App.GetService<SearchHistoryService>();
                    if (historyService == null)
                    {
                        AddToConsole("Error: Search history service not available");
                        return;
                    }

                    // Export directly from DuckDB with all columns
                    var exportedCount = await historyService.ExportResultsAsync(result.Path.LocalPath);
                    
                    if (exportedCount > 0)
                    {
                        AddToConsole($"Exported {exportedCount} results to {result.Name}");
                    }
                    else
                    {
                        AddToConsole("No results to export");
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"Error exporting results: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Set the search instance to reconnect to an existing search
        /// </summary>
        public void SetSearchInstance(string searchId)
        {
            _currentSearchId = searchId;

            // Connect to existing search instance
            if (_searchManager != null && !string.IsNullOrEmpty(searchId))
            {
                // Unsubscribe from any previous search events
                UnsubscribeFromSearchEvents();
                
                _searchInstance = _searchManager.GetSearch(searchId);
                
                // Switch to appropriate tab when opened from desktop widget
                Dispatcher.UIThread.Post(() => {
                    if (_searchInstance != null && _searchInstance.IsRunning)
                    {
                        // If search is running, show Search tab
                        if (_searchTab != null)
                        {
                            OnTabClick(_searchTab, new RoutedEventArgs());
                        }
                    }
                    else if (_resultsTab != null)
                    {
                        // Otherwise show results tab
                        OnTabClick(_resultsTab, new RoutedEventArgs());
                    }
                }, DispatcherPriority.Loaded);
                if (_searchInstance != null)
                {
                    // Update tab states since we now have an active search
                    UpdateTabStates(true);
                    
                    // Subscribe to events
                    _searchInstance.ProgressUpdated += OnSearchProgressUpdated;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.CutoffChanged += OnCutoffChanged;
                    
                    // Subscribe to console output
                    _searchInstance.ConsoleOutput += OnConsoleOutput;
                    
                    // Restore console history
                    RestoreConsoleHistory();

                    // Load existing results
                    _searchResults.Clear();
                    foreach (var result in _searchInstance.Results)
                    {
                        _searchResults.Add(
                            new SearchResult
                            {
                                Seed = result.Seed,
                                Score = result.TotalScore,
                                TallyScores = result.Scores
                            }
                        );
                    }

                    // Update UI state
                    _isSearching = _searchInstance.IsRunning;
                    _currentFilterPath = _searchInstance.ConfigPath;
                    _searchStartTime = _searchInstance.SearchStartTime; // Use actual search start time
                    UpdateSearchUI();
                    
                    // If search is running, enable the stop button
                    if (_isSearching)
                    {
                        if (_cookButton != null)
                        {
                            _cookButton.IsEnabled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the current search instance ID
        /// </summary>
        public string GetCurrentSearchId()
        {
            return _currentSearchId;
        }

        /// <summary>
        /// Create a new search instance for this modal
        /// </summary>
        private void CreateNewSearchInstance()
        {
            try
            {
                if (_searchManager == null)
                {
                    // Try to get search manager again
                    _searchManager = App.GetService<SearchManager>();
                    if (_searchManager == null)
                    {
                        BalatroSeedOracle.Helpers.DebugLogger.LogError(
                            "SearchModal",
                            "SearchManager is null - service not registered?"
                        );
                        AddToConsole("Error: Search manager service not available");
                        return;
                    }
                }

                // Unsubscribe from any previous search events
                UnsubscribeFromSearchEvents();
                
                _currentSearchId = _searchManager.CreateSearch();
                _searchInstance = _searchManager.GetSearch(_currentSearchId);

                if (_searchInstance != null)
                {
                    // Subscribe to events
                    _searchInstance.ProgressUpdated += OnSearchProgressUpdated;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    _searchInstance.CutoffChanged += OnCutoffChanged;
                    _searchInstance.ConsoleOutput += OnConsoleOutput;

                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        $"Created new search instance: {_currentSearchId}"
                    );
                }
                else
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError(
                        "SearchModal",
                        "Failed to get search instance after creation"
                    );
                    AddToConsole("Error: Failed to create search instance");
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "SearchModal",
                    $"CreateNewSearchInstance error: {ex}"
                );
                AddToConsole($"Error creating search instance: {ex.Message}");
            }
        }

        private void UpdateSearchUI()
        {
            // Update UI based on search state
            if (_cookButton != null)
            {
                _cookButton.Content = _isSearching ? "Stop Jimbo!" : "Let Jimbo COOK!";
                if (_isSearching)
                {
                    _cookButton.Classes.Add("stop");
                }
                else
                {
                    _cookButton.Classes.Remove("stop");
                }
            }

            // Don't disable search tab - user should always be able to see search status
            if (_searchTab != null)
            {
                _searchTab.IsEnabled = true;
            }

            if (_resultsTab != null)
            {
                _resultsTab.IsEnabled = true;
            }

            if (_exportResultsButton != null)
            {
                _exportResultsButton.IsEnabled = _searchResults.Count > 0;
            }

            // Update results summary
            if (_resultsSummary != null)
            {
                _resultsSummary.Text = $"Found {_searchResults.Count} results";
            }
        }

        private async Task LoadExistingResultsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilterPath))
                {
                    AddToConsole("No filter loaded, skipping results check.");
                    return;
                }

                var historyService = App.GetService<SearchHistoryService>();
                if (historyService == null)
                {
                    AddToConsole("History service not available.");
                    return;
                }

                // Load existing results from the database on background thread
                var existingResults = await Task.Run(() => historyService.GetSearchResultsAsync());
                if (existingResults.Count > 0)
                {
                    AddToConsole($"Found {existingResults.Count} existing results in database!");
                    AddToConsole("Loading results to Results tab...");

                    // Clear current results and add the loaded ones on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _searchResults.Clear();
                        foreach (var result in existingResults)
                        {
                            _searchResults.Add(
                                new SearchResult
                                {
                                    Seed = result.Seed,
                                    Score = result.TotalScore,
                                    TallyScores = result.Scores
                                }
                            );
                        }
                    });

                    // Update UI
                    if (_resultsSummary != null)
                    {
                        _resultsSummary.Text =
                            $"Found {_searchResults.Count} results (loaded from database)";
                    }
                    if (_exportResultsButton != null)
                    {
                        _exportResultsButton.IsEnabled = true;
                    }
                }
                else
                {
                    AddToConsole("No existing results in database, searching for new seeds...");
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "SearchModal",
                    $"Error loading existing results: {ex.Message}"
                );
                AddToConsole($"Failed to load existing results: {ex.Message}");
            }
        }

        private void AddTestResults()
        {
            // Add some test results to verify DataGrid is working
            _searchResults.Clear();

            _searchResults.Add(
                new SearchResult
                {
                    Seed = "TESTCODE1",
                    Score = 95.5,
                    Details = "Perkeo (Ante 1), Negative Tag (Ante 2)"
                }
            );

            _searchResults.Add(
                new SearchResult
                {
                    Seed = "TESTCODE2",
                    Score = 88.0,
                    Details = "Blueprint (Ante 2), Brainstorm (Ante 3)"
                }
            );

            _searchResults.Add(
                new SearchResult
                {
                    Seed = "TESTCODE3",
                    Score = 75.5,
                    Details = "Triboulet (Ante 3), Fool Tag (Ante 1)"
                }
            );

            _searchResults.Add(
                new SearchResult
                {
                    Seed = "TESTCODE4",
                    Score = 92.0,
                    Details = "Chicot (Ante 2), Negative Tag (Ante 3)"
                }
            );

            _searchResults.Add(
                new SearchResult
                {
                    Seed = "TESTCODE5",
                    Score = 83.0,
                    Details = "Yorick (Ante 1), Charm Tag (Ante 2)"
                }
            );

            // Update summary
            if (_resultsSummary != null)
            {
                _resultsSummary.Text = $"Found {_searchResults.Count} results (test data)";
            }
            if (_exportResultsButton != null)
            {
                _exportResultsButton.IsEnabled = true;
            }

            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "SearchModal",
                $"Added {_searchResults.Count} test results to DataGrid"
            );
        }

        private static ulong GetMaxBatchesForBatchSize(int batchSize)
        {
            // Total seeds = 35^8 = 2,251,875,390,625
            // Total batches = 35^(8-batchSize)
            return batchSize switch
            {
                1 => 64_339_296_875UL,      // 35^7 = 64,339,296,875
                2 => 1_838_265_625UL,       // 35^6 = 1,838,265,625
                3 => 52_521_875UL,          // 35^5 = 52,521,875
                4 => 1_500_625UL,           // 35^4 = 1,500,625
                5 => 42_875UL,              // 35^3 = 42,875
                6 => 1_225UL,               // 35^2 = 1,225
                7 => 35UL,                  // 35^1 = 35
                8 => 1UL,                   // 35^0 = 1
                _ => throw new ArgumentException($"Invalid batch size: {batchSize}. Valid range is 1-8.")
            };
        }

        private string FormatSeedsPerSecond(double seedsPerMillisecond)
        {
            // Convert from seeds per millisecond to seeds per second
            double seedsPerSecond = seedsPerMillisecond * 1000;
            
            if (seedsPerSecond >= 1_000_000_000)
            {
                return $"{seedsPerSecond / 1_000_000_000:F1}B seeds/s";
            }
            else if (seedsPerSecond >= 1_000_000)
            {
                return $"{seedsPerSecond / 1_000_000:F1}M seeds/s";
            }
            else if (seedsPerSecond >= 1_000)
            {
                return $"{seedsPerSecond / 1_000:F0}K seeds/s";
            }
            else
            {
                return $"{seedsPerSecond:F0} seeds/s";
            }
        }

        private void UpdateSpeedometer(double seedsPerMillisecond, ulong batchesSearched)
        {
            // Update speed values
            if (_speedText != null)
            {
                _speedText.Text = FormatSeedsPerSecond(seedsPerMillisecond);
            }
            
            // Convert to seeds per second for display
            double seedsPerSecond = seedsPerMillisecond * 1000;
            
            if (_speedValueText != null)
            {
                _speedValueText.Text = $"{seedsPerSecond:N0}";
            }

            if (_currentSpeedText != null)
            {
                _currentSpeedText.Text = FormatSeedsPerSecond(seedsPerMillisecond);
            }

            // Track peak speed
            if (seedsPerSecond > _peakSpeed)
            {
                _peakSpeed = seedsPerSecond;
                if (_peakSpeedText != null)
                {
                    _peakSpeedText.Text = FormatSeedsPerSecond(_peakSpeed / 1000);
                }
            }

            // Calculate average speed
            var elapsed = DateTime.UtcNow - _searchStartTime;
            if (elapsed.TotalSeconds > 0 && _totalSeeds > 0)
            {
                double avgSpeed = _totalSeeds / elapsed.TotalSeconds;
                if (_averageSpeedText != null)
                {
                    _averageSpeedText.Text = FormatSeedsPerSecond(avgSpeed / 1000);
                }
            }

            // Update speedometer arc
            if (_speedArc != null && _peakSpeed > 0)
            {
                // Map current speed to arc angle (0-180 degrees)
                double speedRatio = Math.Min(seedsPerSecond / _peakSpeed, 1.0);
                double angle = speedRatio * 180; // 180 degree arc

                // Calculate arc path
                double startAngle = 180; // Start at left side
                double endAngle = startAngle + angle;

                // Convert to radians
                double startRad = startAngle * Math.PI / 180;
                double endRad = endAngle * Math.PI / 180;

                // Calculate arc endpoints
                double centerX = 70;
                double centerY = 110;
                double radius = 55;

                double startX = centerX + radius * Math.Cos(startRad);
                double startY = centerY + radius * Math.Sin(startRad);
                double endX = centerX + radius * Math.Cos(endRad);
                double endY = centerY + radius * Math.Sin(endRad);

                // Large arc flag (1 if angle > 180)
                int largeArcFlag = angle > 180 ? 1 : 0;

                // Create arc path
                string arcPath =
                    $"M {startX:F1},{startY:F1} A {radius},{radius} 0 {largeArcFlag} 1 {endX:F1},{endY:F1}";
                _speedArc.Data = Avalonia.Media.PathGeometry.Parse(arcPath);

                // Update arc color based on speed
                if (speedRatio > 0.8)
                {

                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#FFD700")); // Gold
                }

                else if (speedRatio > 0.5)
                {
                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#90EE90")); // Light green
                }
                else
                {

                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#32CD32")); // Green
                }

            }
        }
    }

    // Supporting classes for the search functionality
    public class SearchConfiguration
    {
        public int ThreadCount { get; set; } = 4;
        public int MinScore { get; set; } = 0;
        public int BatchSize { get; set; } = 4;
        public ulong StartBatch { get; set; } = 0;
        public ulong EndBatch { get; set; } = 1_500_625UL;  // Default for batch size 4
        public bool DebugMode { get; set; } = false;
        public string? DebugSeed { get; set; }
        public string? Deck { get; set; }
        public string? Stake { get; set; }
    }

    public class SearchResult : ReactiveObject
    {
        public string Seed { get; set; } = "";
        public double Score { get; set; }
        public string Details { get; set; } = "";
        public string? Antes { get; set; }
        public string? ItemsJson { get; set; }
        public int[]? TallyScores { get; set; }
        public string[]? ItemLabels { get; set; }

        public ReactiveCommand<string, Unit> CopyCommand { get; }

        public SearchResult()
        {
            CopyCommand = ReactiveCommand.Create<string>(seed =>
            {
                // Copy to clipboard logic here
                if (
                    Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                )
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Clipboard?.SetTextAsync(seed);
                    }
                }
            });
        }
    }

    public class SearchProgressEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public int PercentComplete { get; set; }
        public string CurrentSeed { get; set; } = "";
        public ulong SeedsSearched { get; set; }
        public int ResultsFound { get; set; }
        public double SeedsPerMillisecond { get; set; }
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public ulong BatchesSearched { get; set; }
        public ulong TotalBatches { get; set; }
    }

    public class SearchResultEventArgs : EventArgs
    {
        public SearchResult Result { get; set; } = new();
    }
    
    public partial class SearchModal
    {
        private void OnSortBySeed(object? sender, RoutedEventArgs e)
        {
            SortResults("seed");
        }
        
        private void OnSortByScore(object? sender, RoutedEventArgs e)
        {
            SortResults("score");
        }
        
        
        private void SortResults(string column)
        {
            // If clicking the same column, toggle sort order
            if (_currentSortColumn == column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _currentSortColumn = column;
                _sortAscending = column == "seed"; // Seed ascending by default, others descending
            }
            
            // Update sort indicators
            UpdateSortIndicators();
            
            // Sort the results
            var sorted = column switch
            {
                "seed" => _sortAscending 
                    ? _searchResults.OrderBy(r => r.Seed).ToList()
                    : _searchResults.OrderByDescending(r => r.Seed).ToList(),
                "score" => _sortAscending
                    ? _searchResults.OrderBy(r => r.Score).ToList()
                    : _searchResults.OrderByDescending(r => r.Score).ToList(),
                _ => _searchResults.ToList()
            };
            
            // Clear and repopulate
            _searchResults.Clear();
            foreach (var result in sorted)
            {
                _searchResults.Add(result);
            }
            
            // Reapply filter after sorting
            ApplyFilter();
        }
        
        private void UpdateSortIndicators()
        {
            // Hide all indicators
            if (_seedSortIndicator != null) _seedSortIndicator.IsVisible = false;
            if (_scoreSortIndicator != null) _scoreSortIndicator.IsVisible = false;
            
            // Show and update the current one
            var indicator = _currentSortColumn switch
            {
                "seed" => _seedSortIndicator,
                "score" => _scoreSortIndicator,
                _ => null
            };
            
            if (indicator != null)
            {
                indicator.IsVisible = true;
                indicator.Text = _sortAscending ? " ▲" : " ▼";
            }
        }
        
        private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }
        
        private void OnClearFilterClick(object? sender, RoutedEventArgs e)
        {
            if (_resultsFilterTextBox != null)
            {
                _resultsFilterTextBox.Text = string.Empty;
            }
        }
        
        private void ApplyFilter()
        {
            if (_resultsItemsControl == null || _searchResults == null)
                return;
                
            var filterText = _resultsFilterTextBox?.Text?.Trim()?.ToLowerInvariant() ?? string.Empty;
            
            if (string.IsNullOrEmpty(filterText))
            {
                // Show all results if no filter
                _resultsItemsControl.ItemsSource = _searchResults;
                UpdateResultsSummary();
                return;
            }
            
            // Filter results by seed name
            var filteredResults = _searchResults
                .Where(r => r.Seed.ToLowerInvariant().Contains(filterText))
                .ToList();
                
            _resultsItemsControl.ItemsSource = filteredResults;
            
            // Update summary to show filtered count
            if (_resultsSummary != null)
            {
                _resultsSummary.Text = $"Showing {filteredResults.Count} of {_searchResults.Count} results";
            }
        }
        
        private void OnDebugSeedGotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
        {
            // Select all text when the debug seed input gets focus
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
        
        private void OnDebugSeedTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Only allow alphanumeric characters (A-Z, 1-9, no zeros)
                var text = textBox.Text ?? "";
                var filtered = new System.Text.StringBuilder();
                
                foreach (char c in text.ToUpper())
                {
                    if ((c >= 'A' && c <= 'Z') || (c >= '1' && c <= '9'))
                    {
                        filtered.Append(c);
                    }
                }
                
                var filteredText = filtered.ToString();
                if (filteredText != text)
                {
                    var caretIndex = textBox.CaretIndex;
                    textBox.Text = filteredText;
                    textBox.CaretIndex = Math.Min(caretIndex, filteredText.Length);
                }
            }
        }
        
        private void OnDebugModeChecked(object? sender, RoutedEventArgs e)
        {
            // Enable the debug seed input when debug mode is checked
            var debugSeedInput = this.FindControl<TextBox>("DebugSeedInput");
            if (debugSeedInput != null)
            {
                debugSeedInput.IsEnabled = true;
                debugSeedInput.Focus();
            }
        }
        
        private void OnDebugModeUnchecked(object? sender, RoutedEventArgs e)
        {
            // Disable the debug seed input when debug mode is unchecked
            var debugSeedInput = this.FindControl<TextBox>("DebugSeedInput");
            if (debugSeedInput != null)
            {
                debugSeedInput.IsEnabled = false;
                debugSeedInput.Text = "";
            }
        }
        
        private void UpdateResultsSummary()
        {
            if (_resultsSummary != null)
            {
                if (_searchResults.Count == 0)
                {
                    _resultsSummary.Text = "No results yet";
                }
                else
                {
                    var existingCount = _searchResults.Count - _newResultsCount;
                    if (existingCount > 0)
                    {
                        _resultsSummary.Text = $"Total: {_searchResults.Count} results ({existingCount} from DB + {_newResultsCount} new)";
                    }
                    else
                    {
                        _resultsSummary.Text = $"Found {_searchResults.Count} results";
                    }
                }
            }
        }
        
        public void Dispose()
        {
            // Clean up timer
            _resultBatchTimer?.Dispose();
            _resultBatchTimer = null;
            
            // Clear pending results
            lock (_resultBatchLock)
            {
                _pendingResults.Clear();
            }
            
            // Unsubscribe from events
            UnsubscribeFromSearchEvents();
            
            GC.SuppressFinalize(this);
        }
    }
}
