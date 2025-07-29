using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Services;

namespace Oracle.Components
{
    public partial class SearchWidget : UserControl
    {
        private readonly MotelySearchService _searchService;
        private readonly StringBuilder _terminalBuffer = new();
        private CancellationTokenSource? _searchCancellation;
        private string? _configPath;
        private int _foundCount = 0;
        private bool _isDragging = false;
        private Point _clickPoint;
        private Point _startPosition;
        private string _currentConfigName = "";
        private SearchParams _searchParams = new SearchParams();
        private DateTime _lastUIUpdate = DateTime.MinValue;
        private DateTime _lastTerminalUpdate = DateTime.MinValue;
        private const int UI_UPDATE_INTERVAL_MS = 100; // Update UI max 10 times per second
        private const int TERMINAL_UPDATE_INTERVAL_MS = 250; // Update terminal max 4 times per second
        private bool _isConfigExpanded = true; // Start with config expanded
        
        // Public properties for SearchModal
        public string? ConfigPath => _configPath;
        public bool IsRunning => _searchService.IsRunning;
        public int FoundCount => _foundCount;
        public List<SearchResult> Results => _searchService.Results;
        
        public SearchWidget()
        {
            InitializeComponent();
            
            // Get service from DI using helper
            _searchService = ServiceHelper.GetService<MotelySearchService>() ?? new MotelySearchService();
            
            // Make the expanded view draggable
            var expandedView = this.FindControl<Border>("ExpandedView");
            if (expandedView != null)
            {
                expandedView.PointerPressed += OnDragStart;
                expandedView.PointerMoved += OnDragMove;
                expandedView.PointerReleased += OnDragEnd;
            }
        }
        
        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
        
        private void OnMinimizedClick(object? sender, PointerPressedEventArgs e)
        {
            var minimizedView = this.FindControl<Grid>("MinimizedView");
            var expandedView = this.FindControl<Border>("ExpandedView");
            
            if (minimizedView != null && expandedView != null)
            {
                minimizedView.IsVisible = false;
                expandedView.IsVisible = true;
            }
        }
        
        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            var minimizedView = this.FindControl<Grid>("MinimizedView");
            var expandedView = this.FindControl<Border>("ExpandedView");
            
            if (minimizedView != null && expandedView != null)
            {
                minimizedView.IsVisible = true;
                expandedView.IsVisible = false;
            }
        }
        
        private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        {
            Oracle.Helpers.DebugLogger.Log("SearchWidget", "OnMaximizeClick called");
            
            // Get the main menu
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            Oracle.Helpers.DebugLogger.Log("SearchWidget", $"MainWindow found: {mainWindow != null}");
            
            // The MainWindow content is a Grid, need to find BalatroMainMenu within it
            Views.BalatroMainMenu? mainMenu = null;
            if (mainWindow?.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Views.BalatroMainMenu menu)
                    {
                        mainMenu = menu;
                        break;
                    }
                }
            }
            
            Oracle.Helpers.DebugLogger.Log("SearchWidget", $"MainMenu found: {mainMenu != null}");
            
            if (mainMenu != null)
            {
                // Hide the widget
                this.IsVisible = false;
                Oracle.Helpers.DebugLogger.Log("SearchWidget", "Widget hidden, calling ShowSearchModal");
                
                // Show the full modal with current results
                mainMenu.ShowSearchModal(this);
            }
            else
            {
                Oracle.Helpers.DebugLogger.Log("SearchWidget", "Cannot find main menu - maximize failed");
            }
        }
        
        private void OnMainMenuClick(object? sender, RoutedEventArgs e)
        {
            // Hide the widget
            this.IsVisible = false;
        }
        
        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            // Stop any running search
            _searchCancellation?.Cancel();
            
            // Hide the widget entirely
            this.IsVisible = false;
        }
        
        private void OnDragStart(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _clickPoint = e.GetPosition(this);
                _startPosition = new Point(this.Margin.Left, this.Margin.Top);
                ((Control)sender!).Cursor = new Cursor(StandardCursorType.DragMove);
            }
        }
        
        private void OnDragMove(object? sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(this.Parent as Visual);
                var deltaX = currentPosition.X - _clickPoint.X - _startPosition.X;
                var deltaY = currentPosition.Y - _clickPoint.Y - _startPosition.Y;
                
                // Update widget position using margin
                this.Margin = new Thickness(
                    Math.Max(0, _startPosition.X + deltaX),
                    Math.Max(0, _startPosition.Y + deltaY),
                    0, 0);
            }
        }
        
        private void OnDragEnd(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            ((Control)sender!).Cursor = new Cursor(StandardCursorType.Arrow);
        }
        
        private async void OnLoadConfigClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var storageProvider = topLevel?.StorageProvider;
                
                if (storageProvider == null) return;

                var options = new FilePickerOpenOptions
                {
                    Title = "Select Motely Config File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Config Files") 
                        { 
                            Patterns = new[] { "*.ouija.json", "*.json" }
                        }
                    }
                };

                var files = await storageProvider.OpenFilePickerAsync(options);
                var file = files.FirstOrDefault();
                
                if (file != null)
                {
                    await LoadConfig(file.Path.LocalPath);
                }
            }
            catch (Exception ex)
            {
                WriteToTerminal($"Error loading config: {ex.Message}", isError: true);
            }
        }
        
        public async Task LoadConfig(string configPath)
        {
            try
            {
                _configPath = configPath;
                
                // Extract config name without extension
                var filename = Path.GetFileName(configPath);
                _currentConfigName = filename.Replace(".ouija.json", "").Replace(".json", "");
                
                UpdateTitle("Ready");
                WriteToTerminal($"Loading config: {filename}");
                
                // Validate the config
                var (success, message) = await _searchService.LoadConfigAsync(configPath);
                
                if (success)
                {
                    WriteToTerminal($"Config loaded: {message}");
                    
                    var cookButton = this.FindControl<Button>("CookButton");
                    if (cookButton != null)
                        cookButton.IsEnabled = true;
                        
                    // Update config path display
                    var configPathBox = this.FindControl<TextBox>("ConfigPathBox");
                    if (configPathBox != null)
                        configPathBox.Text = _configPath;
                        
                    // Show loaded indicator
                    var indicator = this.FindControl<Border>("ConfigLoadedIndicator");
                    if (indicator != null)
                        indicator.IsVisible = true;
                }
                else
                {
                    WriteToTerminal($"Config error: {message}", isError: true);
                }
            }
            catch (Exception ex)
            {
                WriteToTerminal($"Error: {ex.Message}", isError: true);
            }
        }
        
        private async void OnCookClick(object? sender, RoutedEventArgs e)
        {
            var cookButton = this.FindControl<Button>("CookButton");
            
            if (cookButton?.Content?.ToString()?.Contains("Let Jimbo Cook") == true)
            {
                // Start the search
                await StartSearch();
            }
            else
            {
                // Stop the search
                StopSearch();
            }
        }
        
        private async Task StartSearch()
        {
            if (string.IsNullOrEmpty(_configPath)) return;
            
            try
            {
                _foundCount = 0;
                UpdateNotificationBadge(0);
                
                // Get search parameters from UI controls
                var threadsUpDown = this.FindControl<NumericUpDown>("ThreadsUpDown");
                var batchSizeUpDown = this.FindControl<NumericUpDown>("BatchSizeUpDown");
                var scoreMinUpDown = this.FindControl<NumericUpDown>("ScoreMinUpDown");
                var debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");
                
                var threadCount = (int)(threadsUpDown?.Value ?? 4);
                var minScore = (int)(scoreMinUpDown?.Value ?? 1);
                var batchSize = (int)(batchSizeUpDown?.Value ?? 4);
                var debug = debugCheckBox?.IsChecked ?? false;
                
                // Set debug logging based on checkbox
                Oracle.Helpers.DebugLogger.SetDebugEnabled(debug);
                
                // Update button to stop mode
                var cookButton = this.FindControl<Button>("CookButton");
                if (cookButton != null)
                {
                    cookButton.Content = "STOP SEARCH";
                    cookButton.Classes.Clear();
                    cookButton.Classes.Add("oracle-btn");
                    cookButton.Classes.Add("button-color-red");
                    cookButton.IsEnabled = true; // Ensure button stays enabled
                    cookButton.IsHitTestVisible = true;
                }
                
                WriteToTerminal($"Let Jimbo cook! Starting search...");
                UpdateTitle("Cooking...");
                
                // Create cancellation token
                _searchCancellation = new CancellationTokenSource();
                
                // Set up progress reporting
                var progress = new Progress<SearchProgress>(OnProgressUpdate);
                
                // Create search criteria
                var criteria = new SearchCriteria
                {
                    ConfigPath = _configPath,
                    ThreadCount = threadCount,
                    MaxSeeds = long.MaxValue, // Search all seeds
                    MinScore = minScore,
                    BatchSize = batchSize,
                    Deck = _searchParams.Deck,
                    Stake = _searchParams.Stake,
                    EnableDebugOutput = debug
                };
                
                // Start search
                await _searchService.StartSearchAsync(criteria, progress, _searchCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                WriteToTerminal("Jimbo stopped cooking (search cancelled)");
                UpdateTitle("Stopped");
            }
            catch (Exception ex)
            {
                WriteToTerminal($"Jimbo burned the kitchen! Error: {ex.Message}", isError: true);
                UpdateTitle("Error");
            }
            finally
            {
                // Reset button to cook mode
                var cookButton = this.FindControl<Button>("CookButton");
                if (cookButton != null)
                {
                    cookButton.Content = "Let Jimbo Cook!";
                    cookButton.Classes.Clear();
                    cookButton.Classes.Add("oracle-btn");
                    cookButton.Classes.Add("button-color-green");
                }
                
                _searchCancellation?.Dispose();
                _searchCancellation = null;
            }
        }
        
        public void StopSearch()
        {
            // Stop messages should be immediate - no throttling
            Dispatcher.UIThread.Post(() => 
            {
                WriteToTerminal("Jimbo is putting down the spatula...");
                
                // Keep button enabled and clickable during stop
                var cookButton = this.FindControl<Button>("CookButton");
                if (cookButton != null)
                {
                    cookButton.IsEnabled = true;
                    cookButton.IsHitTestVisible = true;
                }
            });
            
            try
            {
                // Cancel the token first
                _searchCancellation?.Cancel();
                
                // Then stop the search service
                _searchService.StopSearch();
                
                Dispatcher.UIThread.Post(() => 
                {
                    WriteToTerminal("Jimbo stopped cooking");
                    UpdateTitle("Stopped");
                    ResetUIAfterStop();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => WriteToTerminal($"Stop error: {ex.Message}"));
            }
        }
        
        private void ResetUIAfterStop()
        {
            var cookButton = this.FindControl<Button>("CookButton");
            var loadButton = this.FindControl<Button>("LoadConfigButton");
            
            if (cookButton != null) 
            {
                cookButton.Content = "Let Jimbo Cook!";
                cookButton.Classes.Clear();
                cookButton.Classes.Add("oracle-btn");
                cookButton.Classes.Add("button-color-green");
                cookButton.IsEnabled = true;
            }
            if (loadButton != null) loadButton.IsEnabled = true;
        }
        
        
        private void OnProgressUpdate(SearchProgress progress)
        {
            var now = DateTime.Now;
            
            // Add any new results immediately (we always want to count them)
            if (progress.NewResult != null)
            {
                _foundCount++;
                
                // But throttle UI updates
                if ((now - _lastUIUpdate).TotalMilliseconds >= UI_UPDATE_INTERVAL_MS)
                {
                    _lastUIUpdate = now;
                    Dispatcher.UIThread.Post(() =>
                    {
                        UpdateNotificationBadge(_foundCount);
                        WriteToTerminal($"Found: {progress.NewResult.Seed} ({progress.NewResult.Score:N0})");
                    });
                }
                Oracle.Helpers.DebugLogger.LogImportant("SearchWidget", $">>> RESULT FOUND: Seed={progress.NewResult.Seed}, Score={progress.NewResult.Score}, Total Found={_foundCount}");
            }
            else if (!string.IsNullOrEmpty(progress.Message))
            {
                // Throttle terminal messages
                if ((now - _lastTerminalUpdate).TotalMilliseconds >= TERMINAL_UPDATE_INTERVAL_MS)
                {
                    _lastTerminalUpdate = now;
                    // Only show important messages in the compact terminal
                    if (progress.Message.Contains("Found") || 
                        progress.Message.Contains("complete") ||
                        progress.SeedsSearched % 10000 == 0) // Changed from 1000 to 10000 for less spam
                    {
                        Dispatcher.UIThread.Post(() => WriteToTerminal(progress.Message));
                        Oracle.Helpers.DebugLogger.Log("SearchWidget", $"Terminal message: {progress.Message}");
                    }
                }
            }
            
            // Always show completion message
            if (progress.IsComplete)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateNotificationBadge(_foundCount);
                    WriteToTerminal($"Jimbo finished cooking! Found {_foundCount} seeds");
                    Oracle.Helpers.DebugLogger.LogImportant("SearchWidget", $">>> SEARCH COMPLETE: Total results found: {_foundCount}");
                });
            }
        }
        
        private void WriteToTerminal(string message, bool isError = false)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{timestamp}] {message}";
            
            // Keep terminal at 100 lines max in widget mode (was 1000)
            var lines = _terminalBuffer.ToString().Split('\n').ToList();
            if (lines.Count > 100)
            {
                lines.RemoveRange(0, lines.Count - 100);
            }
            lines.Add(line);
            
            _terminalBuffer.Clear();
            _terminalBuffer.AppendLine(string.Join('\n', lines));
            
            // Update UI
            var terminalOutput = this.FindControl<TextBox>("TerminalOutput");
            if (terminalOutput != null)
            {
                terminalOutput.Text = _terminalBuffer.ToString();
                
                // Auto-scroll to bottom
                var scrollViewer = this.FindControl<ScrollViewer>("TerminalScrollViewer");
                scrollViewer?.ScrollToEnd();
            }
        }
        
        
        private void UpdateNotificationBadge(int count)
        {
            var badge = this.FindControl<Border>("NotificationBadge");
            var countText = this.FindControl<TextBlock>("NotificationCount");
            
            if (badge != null && countText != null)
            {
                badge.IsVisible = count > 0;
                countText.Text = count > 99 ? "99+" : count.ToString();
            }
        }
        
        /// <summary>
        /// Start search with specific parameters (called from SearchModal)
        /// </summary>
        public void StartSearchWithParams(int threadCount, int minScore, int seedsToSearch)
        {
            // Update the UI controls
            var threadsUpDown = this.FindControl<NumericUpDown>("ThreadsUpDown");
            var scoreMinUpDown = this.FindControl<NumericUpDown>("ScoreMinUpDown");
            
            if (threadsUpDown != null) threadsUpDown.Value = threadCount;
            if (scoreMinUpDown != null) scoreMinUpDown.Value = minScore;
            
            // Auto-start the search
            OnCookClick(null, new RoutedEventArgs());
        }
        
        private void UpdateTitle(string status)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = status;
            }
        }
        
        private void OnConfigToggleClick(object? sender, RoutedEventArgs e)
        {
            _isConfigExpanded = !_isConfigExpanded;
            
            var configContent = this.FindControl<StackPanel>("ConfigContent");
            var expandIcon = this.FindControl<TextBlock>("ConfigExpandIcon");
            
            if (configContent != null)
            {
                configContent.IsVisible = _isConfigExpanded;
            }
            
            if (expandIcon != null)
            {
                expandIcon.Text = _isConfigExpanded ? "▼" : "▶";
            }
        }
        
        private void OnClearConsoleClick(object? sender, RoutedEventArgs e)
        {
            var terminalOutput = this.FindControl<TextBox>("TerminalOutput");
            if (terminalOutput != null)
            {
                terminalOutput.Text = "";
                _terminalBuffer.Clear();
            }
        }
        
        private class SearchParams
        {
            public int ThreadCount { get; set; } = 8;
            public int MinScore { get; set; } = 1;
            public int BatchSize { get; set; } = 4;
            public string Deck { get; set; } = "Red Deck";
            public string Stake { get; set; } = "White Stake";
        }
    }
}