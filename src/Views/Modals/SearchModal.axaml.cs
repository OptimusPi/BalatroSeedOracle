using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Services;
using ReactiveUI;

namespace Oracle.Views.Modals
{
    public partial class SearchModal : UserControl
    {
        private readonly ObservableCollection<SearchResult> _searchResults = new();
        private MotelySearchService? _searchService;
        private bool _isSearching = false;
        
        // Tab panels
        private Panel? _searchPanel;
        private Panel? _consolePanel;
        private Panel? _resultsPanel;
        
        // Tab buttons
        private Button? _searchTab;
        private Button? _consoleTab;
        private Button? _resultsTab;
        
        // Controls
        private TextBox? _filterPathInput;
        private TextBlock? _filterDescription;
        private TextBlock? _seedsSearchedText;
        private TextBlock? _foundCountText;
        private TextBlock? _speedText;
        private TextBox? _consoleOutput;
        private ItemsControl? _resultsList;
        private Panel? _statsPanel;
        
        // Action button
        private Button? _playButton;
        
        // Parameters
        private NumericUpDown? _threadsUpDown;
        private NumericUpDown? _cutoffUpDown;
        private NumericUpDown? _batchSizeUpDown;
        private CheckBox? _debugCheckBox;
        
        public SearchModal()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Find controls
            _searchPanel = this.FindControl<Panel>("SearchPanel");
            _consolePanel = this.FindControl<Panel>("ConsolePanel");
            _resultsPanel = this.FindControl<Panel>("ResultsPanel");
            _statsPanel = this.FindControl<Panel>("StatsPanel");
            
            _searchTab = this.FindControl<Button>("SearchTab");
            _consoleTab = this.FindControl<Button>("ConsoleTab");
            _resultsTab = this.FindControl<Button>("ResultsTab");
            
            _filterPathInput = this.FindControl<TextBox>("FilterPathInput");
            _filterDescription = this.FindControl<TextBlock>("FilterDescription");
            _seedsSearchedText = this.FindControl<TextBlock>("SeedsSearchedText");
            _foundCountText = this.FindControl<TextBlock>("FoundCountText");
            _speedText = this.FindControl<TextBlock>("SpeedText");
            _consoleOutput = this.FindControl<TextBox>("ConsoleOutput");
            _resultsList = this.FindControl<ItemsControl>("ResultsList");
            
            _playButton = this.FindControl<Button>("PlayButton");
            
            _threadsUpDown = this.FindControl<NumericUpDown>("ThreadsUpDown");
            _cutoffUpDown = this.FindControl<NumericUpDown>("CutoffUpDown");
            _batchSizeUpDown = this.FindControl<NumericUpDown>("BatchSizeUpDown");
            _debugCheckBox = this.FindControl<CheckBox>("DebugCheckBox");
            
            // Set up results
            if (_resultsList != null)
            {
                _resultsList.ItemsSource = _searchResults;
            }
            
            // Initialize search service
            _searchService = App.GetService<MotelySearchService>();
            if (_searchService != null)
            {
                _searchService.ProgressUpdated += OnSearchProgressUpdated;
                _searchService.ResultFound += OnResultFound;
                _searchService.ConsoleOutput += OnConsoleOutput;
                _searchService.SearchStarted += OnSearchStarted;
                _searchService.SearchCompleted += OnSearchCompleted;
            }
        }
        
        private void OnTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedTab) return;
            
            // Remove active class from all tabs
            _searchTab?.Classes.Remove("active");
            _consoleTab?.Classes.Remove("active");
            _resultsTab?.Classes.Remove("active");
            
            // Hide all panels
            if (_searchPanel != null) _searchPanel.IsVisible = false;
            if (_consolePanel != null) _consolePanel.IsVisible = false;
            if (_resultsPanel != null) _resultsPanel.IsVisible = false;
            
            // Show the clicked panel and mark tab as active
            if (clickedTab == _searchTab)
            {
                clickedTab.Classes.Add("active");
                if (_searchPanel != null) _searchPanel.IsVisible = true;
            }
            else if (clickedTab == _consoleTab)
            {
                clickedTab.Classes.Add("active");
                if (_consolePanel != null) _consolePanel.IsVisible = true;
            }
            else if (clickedTab == _resultsTab)
            {
                clickedTab.Classes.Add("active");
                if (_resultsPanel != null) _resultsPanel.IsVisible = true;
            }
        }
        
        private async void OnBrowseFilterClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Filter Configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };
            
            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            
            if (result?.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                
                // Load the config
                if (_searchService != null)
                {
                    var (success, message, name, author, description) = await _searchService.LoadConfigAsync(filePath);
                    if (success)
                    {
                        // Update UI
                        if (_filterPathInput != null)
                        {
                            _filterPathInput.Text = System.IO.Path.GetFileName(filePath);
                        }
                        
                        if (_filterDescription != null)
                        {
                            var desc = "";
                            if (!string.IsNullOrEmpty(name)) desc += name;
                            if (!string.IsNullOrEmpty(author)) desc += $" by {author}";
                            if (!string.IsNullOrEmpty(description)) desc += $"\n{description}";
                            if (string.IsNullOrEmpty(desc)) desc = message;
                            
                            _filterDescription.Text = desc;
                        }
                        
                        // Enable play button
                        if (_playButton != null)
                        {
                            _playButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        // Show error
                        if (_filterDescription != null)
                        {
                            _filterDescription.Text = $"Error: {message}";
                        }
                    }
                }
            }
        }
        
        private async void OnPlayClick(object? sender, RoutedEventArgs e)
        {
            if (_searchService == null) return;
            
            if (!_isSearching)
            {
                // Start search
                _searchResults.Clear();
                
                // Get parameters
                var config = new SearchConfiguration
                {
                    ThreadCount = (int)(_threadsUpDown?.Value ?? 4),
                    MinScore = (int)(_cutoffUpDown?.Value ?? 0),
                    BatchSize = (int)(_batchSizeUpDown?.Value ?? 4),
                    StartBatch = 0,
                    EndBatch = 999999,
                    DebugMode = _debugCheckBox?.IsChecked ?? false
                };
                
                // Start search
                await _searchService.StartSearchAsync(config);
            }
            else
            {
                // Stop search
                _searchService.StopSearch();
            }
        }
        
        private void OnSearchStarted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = true;
                
                // Update button
                if (_playButton != null)
                {
                    _playButton.Content = "STOP";
                    _playButton.Classes.Remove("play-button");
                    _playButton.Classes.Add("btn-red");
                }
                
                // Show stats panel
                if (_statsPanel != null)
                {
                    _statsPanel.IsVisible = true;
                }
            });
        }
        
        private void OnSearchCompleted(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _isSearching = false;
                
                // Update button
                if (_playButton != null)
                {
                    _playButton.Content = "SEARCH";
                    _playButton.Classes.Remove("btn-red");
                    _playButton.Classes.Add("play-button");
                }
                
                // Switch to results tab if we found anything
                if (_searchResults.Count > 0 && _resultsTab != null)
                {
                    OnTabClick(_resultsTab, new RoutedEventArgs());
                }
            });
        }
        
        private void OnClearConsoleClick(object? sender, RoutedEventArgs e)
        {
            if (_consoleOutput != null)
            {
                _consoleOutput.Text = "> Motely Search Console\n> Ready for commands...\n";
            }
        }
        
        private void OnSearchProgressUpdated(object? sender, SearchProgressEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_seedsSearchedText != null) 
                    _seedsSearchedText.Text = e.SeedsSearched.ToString("N0");
                if (_foundCountText != null) 
                    _foundCountText.Text = e.ResultsFound.ToString();
                if (_speedText != null) 
                    _speedText.Text = $"{e.SeedsPerSecond:N0}/s";
            });
        }
        
        private void OnResultFound(object? sender, SearchResultEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _searchResults.Add(e.Result);
            });
        }
        
        private void OnConsoleOutput(object? sender, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_consoleOutput != null)
                {
                    _consoleOutput.Text += $"> {message}\n";
                    // Auto-scroll to bottom
                    _consoleOutput.CaretIndex = _consoleOutput.Text.Length;
                }
            });
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
        public int SeedsSearched { get; set; }
        public int ResultsFound { get; set; }
        public double SeedsPerSecond { get; set; }
    }
    
    public class SearchResultEventArgs : EventArgs
    {
        public SearchResult Result { get; set; } = new();
    }
}