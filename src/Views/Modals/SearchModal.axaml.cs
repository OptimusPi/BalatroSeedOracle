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
        public event EventHandler<string>? CreateShortcutRequested;
        // Efficient streaming architecture - no memory explosion!
        private CircularConsoleBuffer _consoleBuffer = new(1000);
        private Avalonia.Threading.DispatcherTimer? _uiRefreshTimer;
        private int _lastKnownResultCount = 0;
        private SearchProgressEventArgs? _latestProgress;
        
        // TEMPORARY - for compilation until full refactor is done
        private readonly ObservableCollection<SearchResult> _searchResults = new();
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _currentSearchId = string.Empty;
        private bool _isSearching = false;
        private Motely.Filters.OuijaConfig? _loadedConfig;

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
        
        // Console is now handled by CircularConsoleBuffer

        // Action buttons
        private Button? _cookButton;
        private Button? _saveWidgetButton;
        private Button? _popOutButton;

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
        private TextBlock? _currentBatchText;
        private TextBlock? _totalBatchesText;
        private TextBlock? _totalSeedsText;
        private TextBlock? _activeFilterNameText;
        private TextBlock? _timeElapsedText;
    private TextBlock? _etaText;
    // Smoothing state
    private DateTime _lastEtaUpdate = DateTime.MinValue;
    private double _smoothedRemainingSeconds = -1; // moving average of remaining seconds
    private readonly double _etaSmoothingFactor = 0.15; // EMA alpha
    private long _lastSeedsAtProgress = 0;
    private DateTime _lastSeedsProgressTime = DateTime.MinValue;
    private double _lastSeedsPerMsObserved = 0;
        private TextBlock? _resultsFoundText;
    private TextBlock? _rarityText; // now 'Rate' percent
    private TextBlock? _rarityStringText; // categorical rarity label
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
        private DateTime _lastResultBatchUpdate = DateTime.UtcNow;
        private DateTime _lastProgressUpdate = DateTime.UtcNow;
        
        // Resume search support
        private ulong? _resumeFromBatch = null;

        // Results panel controls
        private ItemsControl? _resultsItemsControl;
        private StackPanel? _tallyHeadersPanel;
        private TextBlock? _resultsSummary;
        private Button? _exportResultsButton;
        private Button? _refreshResultsButton;
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

        // Allows ModalHelper to set the filter path before async load completes
        public void SetCurrentFilterPath(string path)
        {
            _currentFilterPath = path;
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            // If search is running when modal closes, create home icon
            if (_isSearching && !string.IsNullOrEmpty(_currentFilterPath))
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "SearchModal",
                    "Creating home icon for ongoing search..."
                );
                CreateShortcutRequested?.Invoke(this, _currentFilterPath);
            }
        }

        private async void CheckForResumableSearch()
        {
            try
            {
                var userProfileService = ServiceHelper.GetService<UserProfileService>();

                if (userProfileService?.GetSearchState() is { } resumeState)
                {

                    // Simply restore the search state - user can hit Cook to continue
                    if (!string.IsNullOrEmpty(resumeState.ConfigPath))
                    {
                        await LoadFilterAsync(resumeState.ConfigPath);
                    }
                    
                    // After loading, check if the state was cleared (different filter)
                    var currentState = userProfileService.GetSearchState();
                    if (currentState == null)
                    {
                        // State was cleared because it was for a different filter
                        return;
                    }
                    
                    // Set the UI values
                    if (_threadsSpinner != null) _threadsSpinner.Value = resumeState.ThreadCount;
                    if (_batchSizeSpinner != null) _batchSizeSpinner.Value = resumeState.BatchSize - 1;
                    if (_minScoreSpinner != null) _minScoreSpinner.Value = Math.Min(69, Math.Max(0, resumeState.MinScore));
                    if (_deckAndStakeSelector != null)
                    {
                        _deckAndStakeSelector.SetDeck(resumeState.Deck ?? "Red");
                        _deckAndStakeSelector.SetStake(resumeState.Stake ?? "White");
                    }
                    
                    // Check if search is already running
                    if (_searchInstance != null && _searchInstance.IsRunning)
                    {
                        AddToConsole($"──────────────────────────────────");
                        AddToConsole($"Search is currently running");
                        AddToConsole($"Processing from batch {resumeState.LastCompletedBatch + 1:N0}");
                        
                        // Update button state to show it's running
                        _isSearching = true;
                        if (_cookButton != null)
                        {
                            _cookButton.IsEnabled = true;
                            _cookButton.Content = "STOP SEARCH";
                            _cookButton.Classes.Add("stop");
                        }
                    }
                    else
                    {
                        // Add console message about resumable state
                        AddToConsole($"──────────────────────────────────");
                        AddToConsole($"Previous search detected!");
                        AddToConsole($"Ready to resume from batch {resumeState.LastCompletedBatch + 1:N0}");
                        AddToConsole($"Press 'Cook' to continue searching");
                        
                        // Ensure button is enabled for resuming
                        if (_cookButton != null)
                        {
                            _cookButton.IsEnabled = true;
                            _cookButton.Content = "Let Jimbo COOK!";
                            _cookButton.Classes.Remove("stop");
                        }
                    }
                    
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
                
                // Load the config from file FIRST before creating search instance
                if (!string.IsNullOrEmpty(resumeState.ConfigPath))
                {
                    _currentFilterPath = resumeState.ConfigPath;
                    // Don't use LoadFilterAsync here as it creates its own search instance
                    // Just load the config directly
                    if (File.Exists(resumeState.ConfigPath))
                    {
                        var json = await File.ReadAllTextAsync(resumeState.ConfigPath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };
                        _loadedConfig = JsonSerializer.Deserialize<Motely.Filters.OuijaConfig>(json, options);
                    }
                }
                
                // NOW create a new search instance with the loaded config
                CreateNewSearchInstance();
                
                if (_searchInstance == null) return;
                
                if (!string.IsNullOrEmpty(_currentFilterPath))
                {
                    // Set UI values from saved state
                    if (_threadsSpinner != null) _threadsSpinner.Value = resumeState.ThreadCount;
                    if (_batchSizeSpinner != null) _batchSizeSpinner.Value = resumeState.BatchSize - 1; // UI shows 0-indexed
                    if (_minScoreSpinner != null) _minScoreSpinner.Value = Math.Min(69, Math.Max(0, resumeState.MinScore));
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
                        MinScore = Math.Min(69, Math.Max(0, resumeState.MinScore)),
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
                    
                    // Always use file-based config now
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
            _popOutButton = this.FindControl<Button>("PopOutButton");

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
                _batchSizeSpinner.DisplayValues = new[] { "low", "default", "large", "huge" };
                _batchSizeSpinner.Value = 1; // Set default to "default" which is index 1, batch size 2
            }
            
            if (_minScoreSpinner != null)
            {
                // 0 = Auto, 1..69 explicit min score (nice)
                _minScoreSpinner.DisplayValues = new[] { "Auto" }
                    .Concat(Enumerable.Range(1, 69).Select(i => i.ToString()))
                    .ToArray();
                _minScoreSpinner.Minimum = 0;
                _minScoreSpinner.Maximum = 69; // hard cap so increment button stops at 69
                _minScoreSpinner.Increment = 1;
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
                // Configure for SearchModal context
                _filterSelector.ShowCreateButton = false;
                _filterSelector.IsInSearchModal = true;

                // Connect the FilterLoaded event
                _filterSelector.FilterLoaded += OnFilterSelected;
            }

            // Find results panel controls
            _resultsItemsControl = this.FindControl<ItemsControl>("ResultsItemsControl");
            _tallyHeadersPanel = this.FindControl<StackPanel>("TallyHeadersPanel");
            _resultsSummary = this.FindControl<TextBlock>("ResultsSummary");
            _exportResultsButton = this.FindControl<Button>("ExportResultsButton");
            _refreshResultsButton = this.FindControl<Button>("RefreshResultsButton");
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
            if (_refreshResultsButton != null)
            {
                _refreshResultsButton.Click += OnRefreshResultsClick;
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
            _currentBatchText = this.FindControl<TextBlock>("CurrentBatchText");
            _totalBatchesText = this.FindControl<TextBlock>("TotalBatchesText");
            _totalSeedsText = this.FindControl<TextBlock>("TotalSeedsText");
            _activeFilterNameText = this.FindControl<TextBlock>("ActiveFilterNameText");
            _timeElapsedText = this.FindControl<TextBlock>("TimeElapsedText");
            _etaText = this.FindControl<TextBlock>("EtaText");
            _resultsFoundText = this.FindControl<TextBlock>("ResultsFoundText");
            _rarityText = this.FindControl<TextBlock>("RarityText");
            _rarityStringText = this.FindControl<TextBlock>("RarityStringText");
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
        
        /// <summary>
        /// Go directly to the Search tab with default Red deck and White stake
        /// </summary>
        public void GoToSearchTab()
        {
            // Set defaults: Red deck, White stake
            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.SetDeck("Red");
                _deckAndStakeSelector.SetStake("White");
            }
            
            // IMPORTANT: Set MinScore to 1 to DISABLE auto-cutoff
            // Auto-cutoff is only enabled when MinScore = 0
            if (_minScoreSpinner != null)
            {
                _minScoreSpinner.Value = 1;
            }
            
            // Enable tabs since we have a config loaded
            UpdateTabStates(true);
            
            // Switch directly to the Search tab
            if (_searchTab != null)
            {
                OnTabClick(_searchTab, new RoutedEventArgs());
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
                    
                    // Force flush & reload when opening results tab
                    if (_searchInstance != null)
                    {
                        _searchInstance.ForceFlush();
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
                // Load top results on first open / every switch
                _ = LoadTopResultsAsync();
            }
        }

        private void OnRefreshResultsClick(object? sender, RoutedEventArgs e)
        {
            if (_searchInstance != null)
            {
                _searchInstance.ForceFlush();
            }
            _ = LoadTopResultsAsync();
        }

        private async void OnPopOutClick(object? sender, RoutedEventArgs e)
        {
            if (_searchInstance == null)
            {
                DebugLogger.LogError("SearchModal", "Cannot open advanced view: SearchInstance is null");
                return;
            }

            try
            {
                // Get the filter name to display
                string? filterName = null;
                if (_loadedConfig != null)
                {
                    filterName = _loadedConfig.Name;
                }
                else if (!string.IsNullOrEmpty(_currentFilterPath))
                {
                    filterName = System.IO.Path.GetFileNameWithoutExtension(_currentFilterPath);
                }
                
                var window = new Windows.DataGridResultsWindow(_searchInstance, filterName);
                
                // Get the parent window to show dialog relative to it
                var parentWindow = this.FindAncestorOfType<Window>();
                if (parentWindow != null)
                {
                    await window.ShowDialog(parentWindow);
                }
                else
                {
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Failed to open advanced view: {ex}");
            }
        }

        /// <summary>
        /// Connect to an existing search instance (when opened from desktop icon)
        /// </summary>
        public void ConnectToExistingSearch(string searchId)
        {
            if (string.IsNullOrEmpty(searchId))
            {
                DebugLogger.LogError("SearchModal", "Cannot connect to search: searchId is empty");
                return;
            }
            
            _currentSearchId = searchId;
            var searchManager = App.GetService<SearchManager>();
            if (searchManager != null)
            {
                _searchInstance = searchManager.GetSearch(searchId);
                if (_searchInstance != null)
                {
                    DebugLogger.Log("SearchModal", $"Connected to existing search: {searchId}");
                    
                    // Subscribe to events
                    
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
                    
                    // Update UI based on search state
                    _isSearching = _searchInstance.IsRunning;
                    UpdateCookButtonState();
                }
                else
                {
                    DebugLogger.LogError("SearchModal", $"Search instance not found: {searchId}");
                    
                    // Create a new search instance since the placeholder doesn't exist
                    // This happens when resuming from a saved desktop icon
                    DebugLogger.Log("SearchModal", "Creating new search instance for resumed search");
                    CreateNewSearchInstance();
                    
                    // Check if we have a saved search state to restore
                    var userProfileService = App.GetService<UserProfileService>();
                    if (userProfileService != null)
                    {
                        var searchState = userProfileService.GetSearchState();
                        if (searchState != null && _searchInstance != null)
                        {
                            DebugLogger.Log("SearchModal", $"Restoring search state from batch {searchState.LastCompletedBatch}");
                            // The search instance will restore its state when it starts
                        }
                    }
                    
                    UpdateCookButtonState();
                }
            }
        }
        
        private void UpdateCookButtonState()
        {
            if (_cookButton != null)
            {
                _cookButton.IsEnabled = true;
                _cookButton.Content = _isSearching ? "STOP SEARCH" : "LET JIMBO COOK";
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
                    _loadedConfig = JsonSerializer.Deserialize<Motely.Filters.OuijaConfig>(json, options);
                }

                if (_loadedConfig != null)
                {
                    // Just log that we loaded successfully
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        $"Filter loaded successfully: {_loadedConfig.Name}"
                    );
                    
                    // Show in console what filter was loaded
                    AddToConsole($"Loaded filter: {System.IO.Path.GetFileName(filePath)}");
                    AddToConsole($"Name: {_loadedConfig.Name ?? "Unnamed"}");
                    AddToConsole($"Description: {_loadedConfig.Description ?? "No description"}");
                    
                    // Update the active filter name display
                    if (_activeFilterNameText != null)
                    {
                        _activeFilterNameText.Text = _loadedConfig.Name ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
                    }

                    // Update JSON validation status
                    UpdateJsonValidationStatus(true, "Valid ✓");

                    // SearchHistoryService removed; SearchInstance will configure its own DB when search starts
                    
                    // Check if the saved search state is for a DIFFERENT filter
                    var userProfileService = App.GetService<UserProfileService>();
                    if (userProfileService != null)
                    {
                        var savedState = userProfileService.GetSearchState();
                        if (savedState != null && !string.IsNullOrEmpty(savedState.ConfigPath))
                        {
                            // If the saved state is for a different filter, clear it
                            if (!string.Equals(savedState.ConfigPath, filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                BalatroSeedOracle.Helpers.DebugLogger.Log(
                                    "SearchModal",
                                    $"Clearing saved state for different filter: {savedState.ConfigPath} != {filePath}"
                                );
                                userProfileService.ClearSearchState();
                                _resumeFromBatch = null; // Clear any resume batch
                            }
                        }
                    }

                    // Cook button should always be enabled when a filter is loaded
                    if (_cookButton != null)
                    {
                        _cookButton.IsEnabled = true;
                    }

                    // Enable tabs now that filter is loaded
                    UpdateTabStates(true);
                    
                    // Stay on the filter tab - don't auto-switch
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Error loading filter: {ex.Message}");
            }
        }

        private async void OnCookClick(object? sender, RoutedEventArgs e)
        {
            // Prevent double-clicks by disabling briefly
            if (_cookButton != null)
            {
                _cookButton.IsEnabled = false;
                await Task.Delay(100); // Brief delay to prevent rapid clicks
                _cookButton.IsEnabled = true;
            }
            
            try
            {
                if (string.IsNullOrEmpty(_currentFilterPath))
                {
                    AddToConsole("Error: No filter loaded. Please select a filter first.");
                    return;
                }
                
                // Check if filter file exists
                if (!System.IO.File.Exists(_currentFilterPath))
                {
                    AddToConsole($"Error: Filter file not found: {_currentFilterPath}");
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
                    throw new InvalidOperationException(
                        "SearchInstance is null after CreateNewSearchInstance"
                    );
                }

                if (!_isSearching)
                {
                    // IMMEDIATELY set searching flag to prevent multiple clicks
                    _isSearching = true;
                    
                    // Update button immediately
                    if (_cookButton != null)
                    {
                        _cookButton.Content = "STOP SEARCH";
                        _cookButton.Classes.Add("stop");
                    }
                    
                    // Start search
                    _searchResults.Clear();
                    _newResultsCount = 0; // Reset new results counter

                    // Reset the Motely cancellation flag
                    Motely.Filters.OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;

                    // Get parameters from Balatro spinners
                    int batchSize = (_batchSizeSpinner?.Value ?? 1) + 1; // Convert 0-3 to 1-4 for actual batch size (low=1, default=2, large=3, huge=4)
                    _isDebugMode = _debugCheckBox?.IsChecked ?? false;
                    
                    // Check if we should resume from a saved batch
                    ulong startBatch = _resumeFromBatch ?? 0;
                    if (startBatch == 0 && !_isDebugMode && _searchInstance != null) // Only check for saved batch if not explicitly resuming and not in debug mode
                    {
                        var savedBatch = await _searchInstance.GetLastBatchAsync();
                        if (savedBatch.HasValue && savedBatch.Value > 0)
                        {
                            startBatch = savedBatch.Value;
                            AddToConsole($"Found saved progress at batch {startBatch:N0}");
                        }
                    }
                    
                    var searchConfig = new SearchConfiguration
                    {
                        ThreadCount = _threadsSpinner?.Value ?? 4,
                        MinScore = _minScoreSpinner?.Value ?? 0, // 0 = Auto, 1-69 = explicit cutoff
                        BatchSize = batchSize,
                        StartBatch = startBatch,
                        EndBatch = GetMaxBatchesForBatchSize(batchSize),
                        DebugMode = _isDebugMode,
                        DebugSeed = _isDebugMode ? this.FindControl<TextBox>("DebugSeedInput")?.Text : null,
                        Deck = _deckAndStakeSelector?.SelectedDeckName ?? "Red",
                        Stake = _deckAndStakeSelector?.SelectedStakeName ?? "White",
                    };

                    AddToConsole("──────────────────────────────────");
                    AddToConsole($"Filter: {System.IO.Path.GetFileName(_currentFilterPath)}");
                    AddToConsole($"Path: {_currentFilterPath}");
                    if (_resumeFromBatch.HasValue)
                    {
                        AddToConsole($"Resuming search from batch {_resumeFromBatch.Value:N0}...");
                    }
                    else
                    {
                        AddToConsole("Let Jimbo cook! Starting search...");
                    }

                    // Validate filter path before constructing criteria
                    if (string.IsNullOrWhiteSpace(_currentFilterPath))
                    {
                        AddToConsole("Cannot start: Filter path is empty or null.");
                        _isSearching = false;
                        UpdateSearchUI();
                        return;
                    }

                    // Start search
                    var searchCriteria = new BalatroSeedOracle.Models.SearchCriteria
                    {
                        ConfigPath = _currentFilterPath!, // validated above
                        ThreadCount = searchConfig.ThreadCount,
                        MinScore = searchConfig.MinScore,
                        BatchSize = searchConfig.BatchSize,
                        Deck = searchConfig.Deck,
                        Stake = searchConfig.Stake,
                        EnableDebugOutput = searchConfig.DebugMode,
                        DebugSeed = searchConfig.DebugSeed,
                    };
                    // No need to store batch size for interpolation anymore

                    if (_searchInstance == null)
                    {
                        AddToConsole("Error: Search instance was not created. Aborting start.");
                        _isSearching = false;
                        UpdateSearchUI();
                        return;
                    }

                    await _searchInstance.StartSearchAsync(searchCriteria);
                }
                else
                {
                    // Stop search - immediately update button to show stopping state
                    if (_cookButton != null)
                    {
                        _cookButton.Content = "Stopping Search...";
                        // User says: The let jimbo cook button isn't ever really supposed to be greyed out
                        _cookButton.IsEnabled = true;
                    }
                    
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        $"STOP button clicked - _searchInstance is {(_searchInstance != null ? "not null" : "null")}"
                    );
                    AddToConsole("Stopping search...");
                    
                    // Force UI update to show the button change immediately
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Render);

                    // Stop UI refresh timer
                    _uiRefreshTimer?.Stop();

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
            // Use non-async lambda to avoid CS1998 warning (no awaits inside)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                // Prefer SearchInstance's recorded start time if available for consistency
                if (_searchInstance != null)
                    _searchStartTime = _searchInstance.SearchStartTime;
                else
                    _searchStartTime = DateTime.UtcNow;
                _peakSpeed = 0;
    _etaText = this.FindControl<TextBlock>("EtaText");
                _totalSeeds = 0;
                _lastSeedsAtProgress = 0;
                _lastSeedsProgressTime = DateTime.UtcNow;
                _lastSeedsPerMsObserved = 0;
                _lastSpeedUpdate = DateTime.UtcNow;
                _newResultsCount = 0; // Reset new results counter
                _lastKnownResultCount = 0;
                
                // Results now fetched directly from SearchInstance (legacy _resultsProvider removed)
                
                // Disable auto-refresh timer (manual model now)
                _uiRefreshTimer?.Stop();
                _uiRefreshTimer = null;

                // Start a lightweight UI timer JUST for elapsed time & rarity so they feel real-time.
                // Progress events from Motely can be sparse (batch-sized), causing perceived stutter.
                _uiRefreshTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) }; // keep elapsed/rarity ticking, also interpolation
                _uiRefreshTimer.Tick += (_, _) =>
                {
                    if (!_isSearching) return;
                    if (_timeElapsedText != null)
                    {
                        var elapsed = DateTime.UtcNow - _searchStartTime;
                        _timeElapsedText.Text = $"{elapsed:hh\\:mm\\:ss}";
                    }
                    if (_totalSeeds > 0)
                    {
                        double rarityPercent = (_newResultsCount / (double)_totalSeeds) * 100.0;
                        UpdateRarityUI(rarityPercent);
                    }
                    if (_latestProgress != null)
                    {
                        // Interpolate seeds smoothly between progress events if we have recent speed
                        if (_totalSeedsText != null && _lastSeedsPerMsObserved > 0 && _lastSeedsProgressTime != DateTime.MinValue)
                        {
                            var msSince = (DateTime.UtcNow - _lastSeedsProgressTime).TotalMilliseconds;
                            if (msSince > 0 && msSince < 5000) // don't extrapolate too far
                            {
                                long interpolated = _lastSeedsAtProgress + (long)(_lastSeedsPerMsObserved * msSince);
                                if (interpolated > _totalSeeds) // only show forward
                                {
                                    _totalSeedsText.Text = interpolated.ToString("N0");
                                }
                            }
                        }
                        // Throttle ETA refresh to ~ once per 2s (smoother) using smoothed remaining seconds
                        if ((DateTime.UtcNow - _lastEtaUpdate).TotalMilliseconds >= 2000)
                        {
                            UpdateEta(_latestProgress, true);
                        }
                        else if (_etaText != null && _smoothedRemainingSeconds > 0)
                        {
                            // Decrement displayed remaining time locally for perceived smoothness
                            _etaText.Text = FormatEta(TimeSpan.FromSeconds(Math.Max(0, _smoothedRemainingSeconds - (DateTime.UtcNow - _lastEtaUpdate).TotalSeconds)));
                        }
                    }
                };
                _uiRefreshTimer.Start();
                
                // Generate column headers from active search instance
                if (_searchInstance != null)
                {
                    GenerateTableHeadersFromSearchInstance(_searchInstance);
                }

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

                // No auto load; user will open Results tab and hit Refresh if desired
                AddToConsole("──────────────────────────────────");
                AddToConsole("Manual results mode: Open Results tab + Refresh to load top 1000.");

                // Enable save widget button
                if (_saveWidgetButton != null)
                {
                    _saveWidgetButton.IsEnabled = true;
                }
            });
        }

        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "SearchModal",
                        "Search completed - resetting UI"
                    );
                    _isSearching = false;
                    
                    // Stop the refresh timer
                    _uiRefreshTimer?.Stop();
                    
                    // Get the actual count from the database
                    int actualResultCount = 0;
                    if (_searchInstance != null)
                    {
                        try
                        {
                            actualResultCount = await _searchInstance.GetResultCountAsync();
                            _lastKnownResultCount = actualResultCount;
                        }
                        catch (Exception countEx)
                        {
                            BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Failed to get result count: {countEx.Message}");
                            // Fall back to in-memory count
                            actualResultCount = _searchInstance.ResultCount;
                        }
                    }
                    
                    // Update the results found display with actual count
                    if (_resultsFoundText != null)
                    {
                        _resultsFoundText.Text = actualResultCount.ToString("N0");
                    }
                    
                    // Do one final refresh to ensure everything is up to date
                    await RefreshResultsView();

                    // Update UI using the central method
                    UpdateSearchUI();

                    AddToConsole($"Search complete! Found {actualResultCount:N0} results.");
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
                _consoleOutput.Text = "> Console cleared\n";
            }
        }
        
        private void GenerateTableHeadersFromSearchInstance(SearchInstance instance)
        {
            if (_tallyHeadersPanel == null)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", "Cannot generate headers - missing panel");
                return;
            }
            
            // Clear existing headers
            _tallyHeadersPanel.Children.Clear();
            
            // Get column names from SearchInstance (skip seed and score columns)
            var columnNames = instance.ColumnNames;
            if (columnNames.Count <= 2)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log("SearchModal", "No tally columns in DuckDB table");
                return;
            }
            
            // Prepare labels & compute a consistent width so body & header align
            var tallyLabels = columnNames.Skip(2).Select(n => n.Replace("_", " ")).ToList();
            if (tallyLabels.Count == 0) return;
            int maxLen = tallyLabels.Max(l => l.Length);
            // Estimate width: char count * (fontSize * avg char width factor) + padding & arrow
            double estimated = maxLen * 12 * 0.62 + 18; // FontSize 12
            double uniformWidth = Math.Clamp(estimated, 72, 180);
            // Expose to XAML via resource so row cells can bind
            this.Resources["TallyColumnWidth"] = uniformWidth;

            // Skip first 2 columns (seed, score) to get the tally columns
            for (int i = 2; i < columnNames.Count; i++)
            {
                var label = tallyLabels[i - 2];
                
                // Add header for this column
                int tallyIndex = i - 2; // Adjust for skipping seed/score
            
                var button = new Button
                {
                    Width = uniformWidth,
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Margin = new Thickness(2,0,2,0),
                    Tag = tallyIndex // Store the index for sorting
                };
                button.Classes.Add("header-button");
                
                var stackPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                var headerText = new TextBlock
                {
                    Text = label,
                    FontFamily = App.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground = App.Current?.FindResource("Gold") as IBrush ?? Brushes.Gold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var sortIndicator = new TextBlock
                {
                    Text = " ▼",
                    FontSize = 10,
                    Foreground = App.Current?.FindResource("Gold") as IBrush ?? Brushes.Gold,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsVisible = false,
                    Name = $"TallySort{tallyIndex}"
                };
                
                stackPanel.Children.Add(headerText);
                stackPanel.Children.Add(sortIndicator);
                
                button.Content = stackPanel;
                
                // Capture tallyIndex by value to avoid closure issue
                int currentIndex = tallyIndex;
                button.Click += (s, e) => OnSortByTally(currentIndex);
                
                _tallyHeadersPanel.Children.Add(button);
            }
        }
        
        private void UpdateTallyHeaders()
        {
            if (_tallyHeadersPanel != null && _tallyHeadersPanel.Children.Count == 0 && _searchInstance != null)
            {
                GenerateTableHeadersFromSearchInstance(_searchInstance);
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
            }
        }

    private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => OnSearchProgressUpdated(sender, e));
                return;
            }

            _latestProgress = e;

            // UPDATE THE FUCKING PERCENTAGE DISPLAY!
            if (_progressPercentText != null)
            {
                // Show more decimals when percentage is tiny
                if (e.PercentComplete < 0.01)
                    _progressPercentText.Text = $"{e.PercentComplete:F4}%";
                else if (e.PercentComplete < 1.0)
                    _progressPercentText.Text = $"{e.PercentComplete:F3}%";
                else
                    _progressPercentText.Text = $"{e.PercentComplete:F2}%";
            }

            // Update batch progress in separate boxes
            if (_currentBatchText != null)
            {
                _currentBatchText.Text = $"{e.BatchesSearched:N0}";
            }
            if (_totalBatchesText != null)
            {
                _totalBatchesText.Text = FormatCompactNumber((long)e.TotalBatches);
            }

            if (_totalSeedsText != null)
            {
                _totalSeeds = (long)e.SeedsSearched;
                _totalSeedsText.Text = ((long)e.SeedsSearched).ToString("N0");
                _lastSeedsAtProgress = (long)e.SeedsSearched;
                _lastSeedsProgressTime = DateTime.UtcNow;
                _lastSeedsPerMsObserved = e.SeedsPerMillisecond;
            }

            if (_resultsFoundText != null)
            {
                _resultsFoundText.Text = _newResultsCount.ToString();
            }

            if (_totalSeeds > 0)
            {
                double rarityPercent = (_newResultsCount / (double)_totalSeeds) * 100.0;
                UpdateRarityUI(rarityPercent);
            }

            if (_timeElapsedText != null)
            {
                var elapsed = DateTime.UtcNow - _searchStartTime;
                _timeElapsedText.Text = $"{elapsed:hh\\:mm\\:ss}";
            }
            // Update ETA immediately only if enough time elapsed since last refresh (else let timer handle smoothing)
            if ((DateTime.UtcNow - _lastEtaUpdate).TotalMilliseconds >= 2000)
            {
                UpdateEta(e, true);
            }

            UpdateSpeedometer(e.SeedsPerMillisecond, e.BatchesSearched);
        }

        private void UpdateEta(SearchProgressEventArgs e, bool recompute = false)
        {
            if (_etaText == null) return;
            try
            {
                if (e.PercentComplete <= 0.000001 || e.PercentComplete >= 100.0)
                {
                    _etaText.Text = "--:--:--";
                    _smoothedRemainingSeconds = -1;
                    return;
                }
                var elapsed = DateTime.UtcNow - _searchStartTime;
                double pct = e.PercentComplete / 100.0; // 0..1
                var total = TimeSpan.FromTicks((long)(elapsed.Ticks / pct));
                var remaining = total - elapsed;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                if (recompute)
                {
                    double seconds = remaining.TotalSeconds;
                    if (_smoothedRemainingSeconds < 0) _smoothedRemainingSeconds = seconds;
                    else _smoothedRemainingSeconds = (_etaSmoothingFactor * seconds) + (1 - _etaSmoothingFactor) * _smoothedRemainingSeconds;
                    _etaText.Text = FormatEta(TimeSpan.FromSeconds(_smoothedRemainingSeconds));
                    _lastEtaUpdate = DateTime.UtcNow;
                }
            }
            catch
            {
                _etaText.Text = "--:--:--";
                _smoothedRemainingSeconds = -1;
            }
        }

        private static string FormatEta(TimeSpan remaining)
        {
            if (remaining.TotalDays >= 1)
            {
                int d = (int)remaining.TotalDays;
                int h = remaining.Hours;
                return $"{d}d {h}h";
            }
            if (remaining.TotalHours >= 1)
            {
                int h = (int)remaining.TotalHours;
                int m = remaining.Minutes;
                return $"{h}h {m}m";
            }
            if (remaining.TotalMinutes >= 1)
            {
                int m = (int)remaining.TotalMinutes;
                int s = remaining.Seconds;
                return $"{m}m {s}s";
            }
            int sec = Math.Max(0, (int)Math.Round(remaining.TotalSeconds));
            return sec + "s";
        }

    private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            // DuckDB handles the actual storage (via SearchInstance)
            // We just update the counter - the timer will refresh the view
            _newResultsCount++;
            
            // Add seed to console buffer for immediate feedback
            if (e.Result != null)
            {
                // e.Result.TotalScore is the canonical property name in Models.SearchResult
                _consoleBuffer.AddLine($"{e.Result.Seed},{e.Result.TotalScore}");
                AddToConsole($"{e.Result.Seed},{e.Result.TotalScore}");
            }

            // Instant UI update for results count (no waiting for progress tick)
            if (_resultsFoundText != null)
            {
                _resultsFoundText.Text = _newResultsCount.ToString();
            }

            // Update rarity immediately if we know total seeds
            if (_totalSeeds > 0)
            {
                double rarityPercent = (_newResultsCount / (double)_totalSeeds) * 100.0;
                UpdateRarityUI(rarityPercent, immediate:true);
            }
        }

        private void UpdateRarityUI(double rarityPercent, bool immediate = false)
        {
            if (_rarityText == null) return;
            _rarityText.Text = $"{rarityPercent:F6}%";

            // Determine category & color thresholds (percent values)
            string category;
            // thresholds expressed in percent (e.g., 0.005% -> 0.005)
            // Order: green >0.005, yellow <0.005, orange <0.004, red <0.003, purple <0.0025, blue <0.002, pink <0.001
            // We interpret as: rarityPercent > 0.005 => green (boring)
            // else if rarityPercent > 0.004 => yellow (common)
            // else if rarityPercent > 0.003 => orange (uncommon)
            // else if rarityPercent > 0.0025 => red (rare)
            // else if rarityPercent > 0.002 => purple (legendary)
            // else if rarityPercent > 0.001 => blue (??? not specified for category list, keep legendary?)
            // else pink (mythical)

            // Provided mapping for string: green=boring yellow=common orange=uncommon red=rare purple=legendary pink=mythical
            // Blue threshold present but no name: we'll treat blue as 'legendary' stepping stone above purple? Instead map blue to 'legendary' too.

            IBrush? brush = null;
            var resources = Application.Current?.Resources;

            if (rarityPercent > 0.005)
            {
                category = "boring";
                brush = resources?["Green"] as IBrush;
            }
            else if (rarityPercent > 0.0025)
            {
                category = "common";
                brush = resources?["Gold"] as IBrush ?? resources?["Yellow"] as IBrush;
            }
            else if (rarityPercent > 0.002)
            {
                category = "uncommon";
                brush = resources?["Orange"] as IBrush;
            }
            else if (rarityPercent > 0.001)
            {
                category = "rare";
                brush = resources?["Red"] as IBrush;
            }
            else if (rarityPercent > 0.0008)
            {
                category = "legendary";
                brush = resources?["Purple"] as IBrush;
            }
            else if (rarityPercent > 0.0003)
            {
                category = "mythical";
                brush = resources?["Blue"] as IBrush;
            }
            else
            {
                category = "god tier";
                brush = resources?["Pink"] as IBrush ?? resources?["Magenta"] as IBrush;
            }

            if (brush != null)
            {
                _rarityText.Foreground = brush;
                if (_rarityStringText != null) _rarityStringText.Foreground = brush;
            }
            if (_rarityStringText != null)
            {
                _rarityStringText.Text = category;
            }
        }
        
        private async Task RefreshResultsView()
        {
            if (_searchInstance == null) return;
            await LoadTopResultsAsync();
        }

        private void AddToConsole(string message)
        {
            if (_consoleOutput != null)
            {
                var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
                
                // Truncate message to max 100 characters
                var truncatedMessage = message.Length > 100 ? message.Substring(0, 100) + "..." : message;
                
                _consoleOutput.Text += $"[{timestamp}] {truncatedMessage}\n";

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
                    if (_searchInstance == null)
                    {
                        AddToConsole("Error: No active search instance");
                        return;
                    }

                    // Export directly from SearchInstance database
                    var exportedCount = await _searchInstance.ExportResultsAsync(result.Path.LocalPath);
                    
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
                    
                    // Subscribe to console output
                    
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

                if (_searchInstance == null)
                {
                    AddToConsole("No active search instance.");
                    return;
                }

                // Load existing results from the database
                var existingResults = await _searchInstance.GetAllResultsAsync();
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
                    
                    // Generate table headers for the loaded results
                    UpdateTallyHeaders();
                }
                else
                {
                    AddToConsole("No existing results in database, searching for new seeds...");
                }
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("SearchModal", $"Error loading existing results: {ex.Message}");
                AddToConsole($"Failed to load existing results: {ex.Message}");
            }
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
        public int[]? TallyScores { get; set; }

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
        public double PercentComplete { get; set; }
        public string CurrentSeed { get; set; } = "";
        public ulong SeedsSearched { get; set; }
        public int ResultsFound { get; set; }
        public double SeedsPerMillisecond { get; set; }
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public ulong BatchesSearched { get; set; }
        public ulong TotalBatches { get; set; }
    }

    // Removed duplicate SearchResultEventArgs (now uses Models.SearchResultEventArgs)
    
    public partial class SearchModal
    {
        private void OnSortBySeed(object? sender, RoutedEventArgs e)
        {
            SortResults("seed");
            _ = LoadTopResultsAsync();
        }
        
        private void OnSortByScore(object? sender, RoutedEventArgs e)
        {
            SortResults("score");
            _ = LoadTopResultsAsync();
        }
        
        private void OnSortByTally(int tallyIndex)
        {
            SortResults($"tally{tallyIndex}");
            _ = LoadTopResultsAsync();
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
            
            // Sorting now just updates sort state; data reload happens via DB query
        }

        private async Task LoadTopResultsAsync()
        {
            // Trust that _searchInstance and its database are fully initialized before this is called.
            string order = string.IsNullOrEmpty(_currentSortColumn) ? "score" : _currentSortColumn;
            var top = await _searchInstance!.GetTopResultsAsync(order, _sortAscending, 1000);

            _searchResults.Clear();
            foreach (var r in top)
            {
                _searchResults.Add(new SearchResult
                {
                    Seed = r.Seed,
                    Score = r.TotalScore,
                    TallyScores = r.Scores
                });
            }

            // Update summary
            var total = await _searchInstance.GetResultCountAsync();
            _lastKnownResultCount = total;
            
            // Enable/disable pop-out button based on results
            if (_popOutButton != null)
            {
                _popOutButton.IsEnabled = total > 0;
            }
            
            if (_resultsSummary != null)
            {
                _resultsSummary.Text = $"Showing top {top.Count:N0} / {total:N0} results";
            }
            if (_resultsFoundText != null)
            {
                _resultsFoundText.Text = total.ToString("N0");
            }
        }

    // (Duplicate OnRefreshResultsClick removed; single implementation earlier handles force flush + reload.)
        
        private void UpdateSortIndicators()
        {
            // Hide all indicators
            if (_seedSortIndicator != null) _seedSortIndicator.IsVisible = false;
            if (_scoreSortIndicator != null) _scoreSortIndicator.IsVisible = false;
            
            // Hide all tally indicators
            if (_tallyHeadersPanel != null)
            {
                foreach (var child in _tallyHeadersPanel.Children)
                {
                    if (child is Button button && button.Content is StackPanel stack)
                    {
                        // Find the sort indicator (second child)
                        if (stack.Children.Count > 1 && stack.Children[1] is TextBlock sortIndicator)
                        {
                            sortIndicator.IsVisible = false;
                        }
                    }
                }
            }
            
            // Show and update the current one
            if (_currentSortColumn.StartsWith("tally"))
            {
                // Extract tally index and show that indicator
                if (int.TryParse(_currentSortColumn.Substring(5), out int tallyIndex))
                {
                    if (_tallyHeadersPanel != null && tallyIndex < _tallyHeadersPanel.Children.Count)
                    {
                        if (_tallyHeadersPanel.Children[tallyIndex] is Button button && button.Content is StackPanel stack)
                        {
                            if (stack.Children.Count > 1 && stack.Children[1] is TextBlock sortIndicator)
                            {
                                sortIndicator.IsVisible = true;
                                sortIndicator.Text = _sortAscending ? " ▲" : " ▼";
                            }
                        }
                    }
                }
            }
            else
            {
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
            _uiRefreshTimer?.Stop();
            _uiRefreshTimer = null;
            
            // Unsubscribe from events
            UnsubscribeFromSearchEvents();
            
            GC.SuppressFinalize(this);
        }

        private static string FormatCompactNumber(long value)
        {
            if (value >= 1_000_000_000)
                return (value / 1_000_000_000D).ToString("0.##G").Replace("G", "B"); // unlikely, but just in case
            if (value >= 1_000_000)
            {
                double m = value / 1_000_000D;
                return m >= 100 ? m.ToString("0M") : m.ToString("0.#M");
            }
            if (value >= 1_000)
            {
                double k = value / 1_000D;
                return k >= 100 ? k.ToString("0K") : k.ToString("0.#K");
            }
            return value.ToString("N0");
        }
    }
}
