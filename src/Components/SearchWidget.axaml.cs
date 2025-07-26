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
            // Get the main menu
            var mainWindow = TopLevel.GetTopLevel(this) as Window;
            var mainMenu = mainWindow?.Content as Views.BalatroMainMenu;
            
            if (mainMenu != null)
            {
                mainMenu.ShowSearchModal(this);
            }
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
                var configPathBox = this.FindControl<TextBox>("ConfigPathBox");
                if (configPathBox != null)
                    configPathBox.Text = Path.GetFileName(configPath);
                
                WriteToTerminal($"Loading config: {Path.GetFileName(configPath)}");
                
                // Validate the config
                var (success, message) = await _searchService.LoadConfigAsync(configPath);
                
                if (success)
                {
                    WriteToTerminal($"Config loaded: {message}");
                    UpdateStatus($"Ready - {Path.GetFileNameWithoutExtension(configPath)}");
                    
                    var startButton = this.FindControl<Button>("StartButton");
                    if (startButton != null)
                        startButton.IsEnabled = true;
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
        
        private async void OnStartClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_configPath)) return;
            
            try
            {
                _foundCount = 0;
                UpdateNotificationBadge(0);
                
                // Get search parameters
                var threadCount = (int)(this.FindControl<NumericUpDown>("ThreadCountBox")?.Value ?? 8);
                var minScore = (int)(this.FindControl<NumericUpDown>("MinScoreBox")?.Value ?? 1);
                
                // Get batch size from the dropdown
                var batchSizeBox = this.FindControl<ComboBox>("BatchSizeBox");
                var selectedItem = batchSizeBox?.SelectedItem as ComboBoxItem;
                var batchSize = int.Parse(selectedItem?.Tag?.ToString() ?? "4");
                
                // Update UI state
                var startButton = this.FindControl<Button>("StartButton");
                var stopButton = this.FindControl<Button>("StopButton");
                var loadConfigButton = this.FindControl<Button>("LoadConfigButton");
                
                if (startButton != null) startButton.IsEnabled = false;
                if (stopButton != null) stopButton.IsEnabled = true;
                if (loadConfigButton != null) loadConfigButton.IsEnabled = false;
                
                WriteToTerminal($"Starting search...");
                UpdateStatus("Searching...");
                
                // Create cancellation token
                _searchCancellation = new CancellationTokenSource();
                
                // Set up progress reporting
                var progress = new Progress<SearchProgress>(OnProgressUpdate);
                
                // Create search criteria
                var criteria = new SearchCriteria
                {
                    ConfigPath = _configPath,
                    ThreadCount = threadCount,
                    MaxSeeds = 10000000, // Fixed for widget
                    MinScore = minScore,
                    BatchSize = batchSize
                };
                
                // Start search
                await Task.Run(async () =>
                {
                    await _searchService.StartSearchAsync(criteria, progress, _searchCancellation.Token);
                }, _searchCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                WriteToTerminal("Search cancelled");
                UpdateStatus("Cancelled");
            }
            catch (Exception ex)
            {
                WriteToTerminal($"Error: {ex.Message}", isError: true);
                UpdateStatus("Error");
            }
            finally
            {
                // Reset UI state
                var startButton = this.FindControl<Button>("StartButton");
                var stopButton = this.FindControl<Button>("StopButton");
                var loadConfigButton = this.FindControl<Button>("LoadConfigButton");
                
                if (startButton != null) startButton.IsEnabled = true;
                if (stopButton != null) stopButton.IsEnabled = false;
                if (loadConfigButton != null) loadConfigButton.IsEnabled = true;
                
                _searchCancellation?.Dispose();
                _searchCancellation = null;
            }
        }
        
        private async void OnStopClick(object? sender, RoutedEventArgs e)
        {
            WriteToTerminal("Stopping...");
            
            // Disable stop button immediately to prevent spam clicks
            var stopButton = this.FindControl<Button>("StopButton");
            if (stopButton != null) stopButton.IsEnabled = false;
            
            try
            {
                // Cancel on background thread to avoid blocking UI
                await Task.Run(() => 
                {
                    _searchCancellation?.Cancel();
                    _searchService.StopSearch();
                });
                
                // Give it a moment to clean up
                await Task.Delay(1000);
                
                WriteToTerminal("Search stopped");
                UpdateStatus("Stopped");
            }
            catch (Exception ex)
            {
                WriteToTerminal($"Stop error: {ex.Message}");
            }
            finally
            {
                // Reset UI state
                ResetUIAfterStop();
            }
        }
        
        private void ResetUIAfterStop()
        {
            var startButton = this.FindControl<Button>("StartButton");
            var stopButton = this.FindControl<Button>("StopButton");
            var loadButton = this.FindControl<Button>("LoadConfigButton");
            
            if (startButton != null) startButton.IsEnabled = true;
            if (stopButton != null) stopButton.IsEnabled = false; 
            if (loadButton != null) loadButton.IsEnabled = true;
        }
        
        private void OnClearClick(object? sender, RoutedEventArgs e)
        {
            _terminalBuffer.Clear();
            _foundCount = 0;
            UpdateNotificationBadge(0);
            
            var terminalOutput = this.FindControl<TextBox>("TerminalOutput");
            if (terminalOutput != null)
                terminalOutput.Text = "";
            
            UpdateStatus("Ready");
        }
        
        private void OnProgressUpdate(SearchProgress progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Add any new results
                if (progress.NewResult != null)
                {
                    _foundCount++;
                    UpdateNotificationBadge(_foundCount);
                    WriteToTerminal($"Found: {progress.NewResult.Seed} ({progress.NewResult.Score:N0})");
                    Oracle.Helpers.DebugLogger.LogImportant("SearchWidget", $">>> RESULT DISPLAYED: Seed={progress.NewResult.Seed}, Score={progress.NewResult.Score}, Total Found={_foundCount}");
                }
                else if (!string.IsNullOrEmpty(progress.Message))
                {
                    // Only show important messages in the compact terminal
                    if (progress.Message.Contains("Found") || 
                        progress.Message.Contains("complete") ||
                        progress.SeedsSearched % 1000 == 0)
                    {
                        WriteToTerminal(progress.Message);
                        Oracle.Helpers.DebugLogger.Log("SearchWidget", $"Terminal message: {progress.Message}");
                    }
                }
                
                // Update status
                if (progress.IsComplete)
                {
                    UpdateStatus($"Complete - Found {_foundCount} seeds");
                    Oracle.Helpers.DebugLogger.LogImportant("SearchWidget", $">>> SEARCH COMPLETE: Total results found: {_foundCount}");
                }
                else
                {
                    UpdateStatus($"Searching... {progress.PercentComplete:F0}%");
                }
            });
        }
        
        private void WriteToTerminal(string message, bool isError = false)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{timestamp}] {message}";
            
            // Keep terminal compact - only last 20 lines
            var lines = _terminalBuffer.ToString().Split('\n').ToList();
            if (lines.Count > 20)
            {
                lines.RemoveRange(0, lines.Count - 20);
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
        
        private void UpdateStatus(string status)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = status;
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
            var threadCountBox = this.FindControl<NumericUpDown>("ThreadCountBox");
            var minScoreBox = this.FindControl<NumericUpDown>("MinScoreBox");
            
            if (threadCountBox != null) threadCountBox.Value = threadCount;
            if (minScoreBox != null) minScoreBox.Value = minScore;
            
            // Auto-start the search
            OnStartClick(null, new RoutedEventArgs());
        }
        
        /// <summary>
        /// Stop any running search (called during disposal)
        /// </summary>
        public void StopSearch()
        {
            Oracle.Helpers.DebugLogger.Log("SearchWidget", "StopSearch called - stopping any running search");
            
            try
            {
                _searchCancellation?.Cancel();
                _searchService.StopSearch();
            }
            catch (Exception ex)
            {
                Oracle.Helpers.DebugLogger.Log("SearchWidget", $"Error stopping search: {ex.Message}");
            }
        }
    }
}