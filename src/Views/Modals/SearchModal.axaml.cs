using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using Oracle.Controls;
using Oracle.Components;
using Oracle.Helpers;
using ReactiveUI;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using System.Text.Json;
using System.IO;
using Avalonia.Controls.Primitives;
using System.Globalization;
using Avalonia.VisualTree;

namespace Oracle.Views.Modals
{
    public partial class SearchModal : UserControl
    {
        public event EventHandler<string>? CreateDesktopIconRequested;
        private readonly ObservableCollection<SearchResult> _searchResults = new();
        private SearchInstance? _searchInstance;
        private SearchManager? _searchManager;
        private string _currentSearchId = string.Empty;
        private bool _isSearching = false;
        
        // Tab panels
        private Panel? _filterPanel;
        private ScrollViewer? _settingsPanel;
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
        
        // New Search tab UI elements
        private TextBlock? _progressPercentText;
        private TextBlock? _batchesText;
        private TextBlock? _totalSeedsText;
        private TextBlock? _timeElapsedText;
        private TextBlock? _resultsFoundText;
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
        private DateTime _lastSpeedUpdate = DateTime.Now;
        
        // Current filter info
        private string? _currentFilterPath;
        private FilterSelector? _filterSelector;
        
        // Results panel controls
        private DataGrid? _resultsDataGrid;
        private TextBlock? _resultsSummary;
        private Button? _exportResultsButton;
        private TextBlock? _jsonValidationStatus;
        
        public SearchModal()
        {
            try
            {
                InitializeComponent();
                this.Unloaded += OnUnloaded;
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"Constructor error: {ex}");
                throw;
            }
        }
        
        private void OnUnloaded(object? sender, EventArgs e)
        {
            // If search is running when modal closes, create desktop icon
            if (_isSearching && !string.IsNullOrEmpty(_currentFilterPath))
            {
                Oracle.Helpers.DebugLogger.Log("SearchModal", "Creating desktop icon for ongoing search...");
                CreateDesktopIconRequested?.Invoke(this, _currentFilterPath);
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
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"Failed to load XAML: {ex}");
                throw;
            }
            
            try
            {
                // Find panels
                _filterPanel = this.FindControl<Panel>("FilterPanel");
                _settingsPanel = this.FindControl<ScrollViewer>("SettingsPanel");
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
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"Failed to find panels/tabs: {ex}");
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
            
            // Find deck/stake selector component
            _deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");
            _debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");
            
            // Find filter selector component
            _filterSelector = this.FindControl<FilterSelector>("FilterSelector");
            if (_filterSelector != null)
            {
                // Hide the "New Blank Filter" button in SearchModal
                _filterSelector.ShowCreateButton = false;
                
                // Connect the FilterLoaded event
                _filterSelector.FilterLoaded += OnFilterLoaded;
            }
            
            // Find results panel controls
            _resultsDataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
            _resultsSummary = this.FindControl<TextBlock>("ResultsSummary");
            _exportResultsButton = this.FindControl<Button>("ExportResultsButton");
            
            // Set up results data grid
            if (_resultsDataGrid != null)
            {
                _resultsDataGrid.ItemsSource = _searchResults;
                _resultsDataGrid.DoubleTapped += OnResultsDataGridDoubleTapped;
            }
            
            // Find new Search tab UI elements
            _progressPercentText = this.FindControl<TextBlock>("ProgressPercentText");
            _batchesText = this.FindControl<TextBlock>("BatchesText");
            _totalSeedsText = this.FindControl<TextBlock>("TotalSeedsText");
            _timeElapsedText = this.FindControl<TextBlock>("TimeElapsedText");
            _resultsFoundText = this.FindControl<TextBlock>("ResultsFoundText");
            _speedValueText = this.FindControl<TextBlock>("SpeedValueText");
            _currentSpeedText = this.FindControl<TextBlock>("CurrentSpeedText");
            _averageSpeedText = this.FindControl<TextBlock>("AverageSpeedText");
            _peakSpeedText = this.FindControl<TextBlock>("PeakSpeedText");
            _speedArc = this.FindControl<Avalonia.Controls.Shapes.Path>("SpeedArc");
            _speedArcBackground = this.FindControl<Avalonia.Controls.Shapes.Path>("SpeedArcBackground");
            
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

        private void OnResultsDataGridDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_resultsDataGrid?.SelectedItem is SearchResult sr)
            {
                try
                {
                    if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow;
                        mainWindow?.Clipboard?.SetTextAsync(sr.Seed);
                    }
                    AddToConsole($"Copied seed {sr.Seed} to clipboard");
                }
                catch (Exception ex)
                {
                    AddToConsole($"Failed to copy seed: {ex.Message}");
                }
            }
        }
        
        private async void OnFilterLoaded(object? sender, string filterPath)
        {
            await LoadFilterAsync(filterPath);
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
                    var filtersModal = new Oracle.Views.Modals.FiltersModalContent();
                    modalHost.Children.Add(filtersModal);
                }
            }
        }
        
        private void UpdateTabStates(bool filterLoaded)
        {
            if (_settingsTab != null) _settingsTab.IsEnabled = filterLoaded;
            if (_searchTab != null) _searchTab.IsEnabled = filterLoaded;
            // Enable Results tab if filter is loaded (we'll show test data if no real results)
            if (_resultsTab != null) _resultsTab.IsEnabled = filterLoaded;
        }
        
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedTab) return;
            
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
                    _ => 0
                };
                Grid.SetColumn(_triangleContainer, column);
            }
            
            // Hide all panels
            if (_filterPanel != null) _filterPanel.IsVisible = false;
            if (_settingsPanel != null) _settingsPanel.IsVisible = false;
            if (_searchPanel != null) _searchPanel.IsVisible = false;
            if (_resultsPanel != null) _resultsPanel.IsVisible = false;
            
            // Show the clicked panel and mark tab as active
            if (clickedTab == _filterTab)
            {
                clickedTab.Classes.Add("active");
                if (_filterPanel != null) _filterPanel.IsVisible = true;
            }
            else if (clickedTab == _settingsTab)
            {
                clickedTab.Classes.Add("active");
                if (_settingsPanel != null) _settingsPanel.IsVisible = true;
            }
            else if (clickedTab == _searchTab)
            {
                clickedTab.Classes.Add("active");
                if (_searchPanel != null) _searchPanel.IsVisible = true;
            }
            else if (clickedTab == _resultsTab)
            {
                clickedTab.Classes.Add("active");
                if (_resultsPanel != null) _resultsPanel.IsVisible = true;
                
                // Ensure DataGrid is bound (avoid test data injection in production)
                if (_resultsDataGrid != null && _resultsDataGrid.ItemsSource != _searchResults)
                {
                    _resultsDataGrid.ItemsSource = _searchResults;
                }
                
                // Update summary when opening if no results yet
                if (_resultsSummary != null && _searchResults.Count == 0)
                {
                    _resultsSummary.Text = "No results yet";
                }
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
                    // Load the config using Motely's loader
                    var config = await Task.Run(() => Motely.Filters.OuijaConfig.LoadFromJson(filePath));
                    
                    if (config != null)
                    {
                        // Just log that we loaded successfully
                        Oracle.Helpers.DebugLogger.Log("SearchModal", $"Filter loaded successfully: {config.Name}");
                        
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
                        
                        // Add to console
                        AddToConsole($"Filter loaded: {System.IO.Path.GetFileName(filePath)}");
                        
                        // If this is a temp file (from FiltersModal), automatically start the search
                        if (filePath.Contains("temp_filter_") && filePath.Contains(".ouija.json"))
                        {
                            Oracle.Helpers.DebugLogger.Log("SearchModal", $"Detected temp filter, auto-switching to search tab: {filePath}");
                            // Switch to Search tab
                            if (_searchTab != null)
                            {
                                OnTabClick(_searchTab, new RoutedEventArgs());
                            }
                            
                            // Wait a moment for UI to update, then start the search
                            await Task.Delay(100);
                            OnCookClick(null, new RoutedEventArgs());
                        }
                        else
                        {
                            Oracle.Helpers.DebugLogger.Log("SearchModal", $"Normal filter loaded, staying on current tab: {filePath}");
                        }
                    }
                    else
                    {
                        // Show error in console
                        AddToConsole("Error: Failed to load filter configuration");
                        UpdateJsonValidationStatus(false, "Invalid: Failed to load");
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"Failed to load filter: {ex.Message}");
                    UpdateJsonValidationStatus(false, $"Invalid: {ex.Message}");
                }
        }
        
        /// <summary>
        /// Load a config object directly and start search (no temp files!)
        /// </summary>
        public async Task LoadConfigDirectlyAsync(Motely.Filters.OuijaConfig config, bool autoStartSearch = true)
        {
            Oracle.Helpers.DebugLogger.Log("SearchModal", "LoadConfigDirectlyAsync - NO TEMP FILES!");
            
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
                    var searchConfig = new SearchConfiguration
                    {
                        ThreadCount = _threadsSpinner?.Value ?? 4,
                        MinScore = _minScoreSpinner?.Value ?? 0,
                        BatchSize = (_batchSizeSpinner?.Value ?? 3) + 1,
                        StartBatch = 0,
                        EndBatch = -1,
                        DebugMode = _debugCheckBox?.IsChecked ?? false,
                        Deck = _deckAndStakeSelector?.SelectedDeckName ?? "Red",
                        Stake = _deckAndStakeSelector?.SelectedStakeName ?? "White"
                    };
                    
                    AddToConsole("Let Jimbo cook! Starting search...");
                    
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
                    AddToConsole("Error: Failed to create search instance. Search manager might not be initialized.");
                    Oracle.Helpers.DebugLogger.LogError("SearchModal", "SearchInstance is null after CreateNewSearchInstance");
                    return;
                }
                
                if (!_isSearching)
                {
                    // Start search
                    _searchResults.Clear();
                    
                    // Get parameters from Balatro spinners
                    var config = new SearchConfiguration
                    {
                        ThreadCount = _threadsSpinner?.Value ?? 4,
                        MinScore = _minScoreSpinner?.Value ?? 0, // 0 = Auto, 1-5 = actual values
                        BatchSize = (_batchSizeSpinner?.Value ?? 3) + 1, // Convert 1-4 to 2-5 for actual batch size
                        StartBatch = 0,
                        EndBatch = 999999,
                        DebugMode = _debugCheckBox?.IsChecked ?? false,
                        Deck = _deckAndStakeSelector?.SelectedDeckName ?? "Red",
                        Stake = _deckAndStakeSelector?.SelectedStakeName ?? "White"
                    };
                    
                    AddToConsole("Let Jimbo cook! Starting search...");
                    
                    // Start search
                    var searchCriteria = new Oracle.Models.SearchCriteria
                    {
                        ConfigPath = _currentFilterPath,
                        ThreadCount = config.ThreadCount,
                        MinScore = config.MinScore,
                        BatchSize = config.BatchSize,
                        Deck = config.Deck,
                        Stake = config.Stake
                    };
                    
                    await _searchInstance.StartSearchAsync(searchCriteria);
                }
                else
                {
                    // Stop search
                    _searchInstance.StopSearch();
                    AddToConsole("Jimbo stopped cooking!");
                    
                    // Immediately update UI
                    _isSearching = false;
                    UpdateSearchUI();
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"Error starting search: {ex.Message}");
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"OnCookClick error: {ex}");
                _isSearching = false;
                UpdateSearchUI();
            }
        }
        
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                _isSearching = true;
                _searchStartTime = DateTime.Now;
                _peakSpeed = 0;
                _totalSeeds = 0;
                _lastSpeedUpdate = DateTime.Now;
                
                // Update cook button
                if (_cookButton != null)
                {
                    _cookButton.Content = "Stop Jimbo!";
                    // Just add the stop class, don't remove and re-add cook-button
                    _cookButton.Classes.Add("stop");
                }

                // Enable results tab
                if (_resultsTab != null) _resultsTab.IsEnabled = true;
                
                // Load existing results from .duckdb file if available
                await LoadExistingResultsAsync();
                
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
                _isSearching = false;
                
                // Update cook button
                if (_cookButton != null)
                {
                    _cookButton.Content = "Let Jimbo COOK!";
                    _cookButton.Classes.Remove("stop");
                }
                
                AddToConsole("Jimbo finished cooking!");
            });
        }
        
        private void OnClearConsoleClick(object? sender, RoutedEventArgs e)
        {
            if (_consoleOutput != null)
            {
                _consoleOutput.Text = "> Motely Search Console\n> Ready for Jimbo to cook...\n";
            }
        }
        
        private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddToConsole($"Searched: {e.SeedsSearched:N0} | Found: {e.ResultsFound} | Speed: {e.SeedsPerSecond:N0}/s");
                
                // Update progress stats
                if (_progressPercentText != null && e.TotalBatches > 0)
                {
                    double percent = (e.BatchesSearched / (double)e.TotalBatches) * 100;
                    _progressPercentText.Text = $"{percent:F1}%";
                }
                
                if (_batchesText != null)
                    _batchesText.Text = $"{e.BatchesSearched:N0}";
                
                if (_totalSeedsText != null)
                {
                    _totalSeeds = e.SeedsSearched;
                    _totalSeedsText.Text = $"{e.SeedsSearched:N0}";
                }
                
                if (_resultsFoundText != null)
                    _resultsFoundText.Text = e.ResultsFound.ToString();
                
                // Update time elapsed
                if (_timeElapsedText != null)
                {
                    var elapsed = DateTime.Now - _searchStartTime;
                    _timeElapsedText.Text = $"{elapsed:hh\\:mm\\:ss}";
                }
                
                // Update speedometer
                UpdateSpeedometer(e.SeedsPerSecond, e.BatchesSearched);
            });
        }
        
        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _searchResults.Add(e.Result);
                AddToConsole($"🎉 Found seed: {e.Result.Seed} (Score: {e.Result.Score})");
                
                // Update results summary
                if (_resultsSummary != null)
                {
                    _resultsSummary.Text = $"Found {_searchResults.Count} results";
                }
                
                // Enable results tab export button
                if (_exportResultsButton != null) _exportResultsButton.IsEnabled = _searchResults.Count > 0;
                
                // Auto-switch to Results tab on first result
                if (_searchResults.Count == 1 && _resultsTab != null && _resultsTab.IsEnabled)
                {
                    OnTabClick(_resultsTab, new RoutedEventArgs());
                }
            });
        }
        
        private void OnConsoleOutput(object? sender, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddToConsole(message);
            });
        }
        
        private void AddToConsole(string message)
        {
            if (_consoleOutput != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _consoleOutput.Text += $"[{timestamp}] {message}\n";
                
                // Auto-scroll to bottom
                _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
            }
        }
        
        private void UpdateJsonValidationStatus(bool isValid, string message)
        {
            if (_jsonValidationStatus != null)
            {
                _jsonValidationStatus.Text = $"JSON: {message}";
                _jsonValidationStatus.Foreground = isValid 
                    ? Application.Current?.FindResource("Green") as IBrush ?? new SolidColorBrush(Color.Parse("#4CAF50"))
                    : Application.Current?.FindResource("Red") as IBrush ?? new SolidColorBrush(Color.Parse("#F44336"));
            }
        }
        private async void OnExportResultsClick(object? sender, RoutedEventArgs e)
        {
            if (_searchResults.Count == 0) return;
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Search Results",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };
            
            var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            
            if (result != null)
            {
                try
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Seed,Score,Timestamp,Details");
                    
                    foreach (var r in _searchResults)
                    {
                        csv.AppendLine($"{r.Seed},{r.Score:F1},{r.Timestamp:yyyy-MM-dd HH:mm:ss},\"{r.Details}\"");
                    }
                    
                    await System.IO.File.WriteAllTextAsync(result.Path.LocalPath, csv.ToString());
                    AddToConsole($"Exported {_searchResults.Count} results to {result.Name}");
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
                _searchInstance = _searchManager.GetSearch(searchId);
                if (_searchInstance != null)
                {
                    // Subscribe to events
                    _searchInstance.ProgressUpdated += OnSearchProgressUpdated;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.ConsoleOutput += OnConsoleOutput;
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    
                    // Load existing results
                    _searchResults.Clear();
                    foreach (var result in _searchInstance.Results)
                    {
                        _searchResults.Add(new SearchResult 
                        { 
                            Seed = result.Seed,
                            Score = result.TotalScore,
                            Details = result.ScoreBreakdown
                        });
                    }
                    
                    // Update UI state
                    _isSearching = _searchInstance.IsRunning;
                    _currentFilterPath = _searchInstance.ConfigPath;
                    UpdateSearchUI();
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
                        Oracle.Helpers.DebugLogger.LogError("SearchModal", "SearchManager is null - service not registered?");
                        AddToConsole("Error: Search manager service not available");
                        return;
                    }
                }
                
                _currentSearchId = _searchManager.CreateSearch();
                _searchInstance = _searchManager.GetSearch(_currentSearchId);
                
                if (_searchInstance != null)
                {
                    // Subscribe to events
                    _searchInstance.ProgressUpdated += OnSearchProgressUpdated;
                    _searchInstance.ResultFound += OnResultFound;
                    _searchInstance.ConsoleOutput += OnConsoleOutput;
                    _searchInstance.SearchStarted += OnSearchStarted;
                    _searchInstance.SearchCompleted += OnSearchCompleted;
                    
                    Oracle.Helpers.DebugLogger.Log("SearchModal", $"Created new search instance: {_currentSearchId}");
                }
                else
                {
                    Oracle.Helpers.DebugLogger.LogError("SearchModal", "Failed to get search instance after creation");
                    AddToConsole("Error: Failed to create search instance");
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"CreateNewSearchInstance error: {ex}");
                AddToConsole($"Error creating search instance: {ex.Message}");
            }
        }
        
        private void UpdateSearchUI()
        {
            // Update UI based on search state
            if (_cookButton != null)
            {
                _cookButton.Content = _isSearching ? "STOP COOKING" : "LET JIMBO COOK!";
            }
            
            if (_searchTab != null)
            {
                _searchTab.IsEnabled = !_isSearching;
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
                    return;
                    
                var historyService = App.GetService<SearchHistoryService>();
                if (historyService == null)
                    return;
                    
                // Load existing results from the database
                var existingResults = await historyService.GetSearchResultsAsync();
                if (existingResults.Count > 0)
                {
                    AddToConsole($"Loaded {existingResults.Count} existing results from database");
                    
                    // Clear current results and add the loaded ones
                    _searchResults.Clear();
                    foreach (var result in existingResults)
                    {
                        _searchResults.Add(new SearchResult
                        {
                            Seed = result.Seed,
                            Score = result.TotalScore,
                            Details = result.ScoreBreakdown,
                            Timestamp = DateTime.Now
                        });
                    }
                    
                    // Update UI
                    if (_resultsSummary != null)
                    {
                        _resultsSummary.Text = $"Found {_searchResults.Count} results (from database)";
                    }
                    if (_exportResultsButton != null)
                    {
                        _exportResultsButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.LogError("SearchModal", $"Error loading existing results: {ex.Message}");
                AddToConsole($"Failed to load existing results: {ex.Message}");
            }
        }
        
        private void AddTestResults()
        {
            // Add some test results to verify DataGrid is working
            _searchResults.Clear();
            
            _searchResults.Add(new SearchResult 
            { 
                Seed = "TESTCODE1",
                Score = 95.5,
                Details = "Perkeo (Ante 1), Negative Tag (Ante 2)",
                Timestamp = DateTime.Now.AddMinutes(-5)
            });
            
            _searchResults.Add(new SearchResult 
            { 
                Seed = "TESTCODE2",
                Score = 88.0,
                Details = "Blueprint (Ante 2), Brainstorm (Ante 3)",
                Timestamp = DateTime.Now.AddMinutes(-3)
            });
            
            _searchResults.Add(new SearchResult 
            { 
                Seed = "TESTCODE3",
                Score = 75.5,
                Details = "Triboulet (Ante 3), Fool Tag (Ante 1)",
                Timestamp = DateTime.Now.AddMinutes(-2)
            });
            
            _searchResults.Add(new SearchResult 
            { 
                Seed = "TESTCODE4",
                Score = 92.0,
                Details = "Chicot (Ante 2), Negative Tag (Ante 3)",
                Timestamp = DateTime.Now.AddMinutes(-1)
            });
            
            _searchResults.Add(new SearchResult 
            { 
                Seed = "TESTCODE5",
                Score = 83.0,
                Details = "Yorick (Ante 1), Charm Tag (Ante 2)",
                Timestamp = DateTime.Now
            });
            
            // Update summary
            if (_resultsSummary != null)
            {
                _resultsSummary.Text = $"Found {_searchResults.Count} results (test data)";
            }
            if (_exportResultsButton != null)
            {
                _exportResultsButton.IsEnabled = true;
            }
            
            Oracle.Helpers.DebugLogger.Log("SearchModal", $"Added {_searchResults.Count} test results to DataGrid");
        }
        
        private void UpdateSpeedometer(double currentSpeed, int batchesSearched)
        {
            // Update speed values
            if (_speedValueText != null)
            {
                _speedValueText.Text = $"{currentSpeed:N0}";
            }
            
            if (_currentSpeedText != null)
            {
                _currentSpeedText.Text = $"{currentSpeed:N0} seeds/s";
            }
            
            // Track peak speed
            if (currentSpeed > _peakSpeed)
            {
                _peakSpeed = currentSpeed;
                if (_peakSpeedText != null)
                {
                    _peakSpeedText.Text = $"{_peakSpeed:N0} seeds/s";
                }
            }
            
            // Calculate average speed
            var elapsed = DateTime.Now - _searchStartTime;
            if (elapsed.TotalSeconds > 0 && _totalSeeds > 0)
            {
                double avgSpeed = _totalSeeds / elapsed.TotalSeconds;
                if (_averageSpeedText != null)
                {
                    _averageSpeedText.Text = $"{avgSpeed:N0} seeds/s";
                }
            }
            
            // Update speedometer arc
            if (_speedArc != null && _peakSpeed > 0)
            {
                // Map current speed to arc angle (0-180 degrees)
                double speedRatio = Math.Min(currentSpeed / _peakSpeed, 1.0);
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
                string arcPath = $"M {startX:F1},{startY:F1} A {radius},{radius} 0 {largeArcFlag} 1 {endX:F1},{endY:F1}";
                _speedArc.Data = Avalonia.Media.PathGeometry.Parse(arcPath);
                
                // Update arc color based on speed
                if (speedRatio > 0.8)
                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#FFD700")); // Gold
                else if (speedRatio > 0.5)
                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#90EE90")); // Light green
                else
                    _speedArc.Stroke = new SolidColorBrush(Color.Parse("#32CD32")); // Green
            }
        }
    }
    
    // Supporting classes for the search functionality
    public class SearchConfiguration
    {
        public int ThreadCount { get; set; } = 4;
        public int MinScore { get; set; } = 0;
        public int BatchSize { get; set; } = 4;
        public int StartBatch { get; set; } = 0;
        public int EndBatch { get; set; } = 999999;
        public bool DebugMode { get; set; } = false;
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
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public ReactiveCommand<string, Unit> CopyCommand { get; }
        
        public SearchResult()
        {
            CopyCommand = ReactiveCommand.Create<string>(seed =>
            {
                // Copy to clipboard logic here
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
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
        public long SeedsSearched { get; set; }
        public int ResultsFound { get; set; }
        public double SeedsPerSecond { get; set; }
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public int BatchesSearched { get; set; }
        public int TotalBatches { get; set; }
    }
    
    public class SearchResultEventArgs : EventArgs
    {
        public SearchResult Result { get; set; } = new();
    }
}
