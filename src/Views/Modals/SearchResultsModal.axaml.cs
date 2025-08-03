using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Oracle.Services;
using Oracle.Helpers;
using Avalonia.VisualTree;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;

namespace Oracle.Views.Modals
{
    public partial class SearchResultsModal : StandardModal
    {
        private DataGrid? _resultsGrid;
        private TextBlock? _filterNameText;
        private TextBlock? _resultCountText;
        private TextBlock? _highestScoreText;
        private TextBlock? _averageScoreText;
        private TextBlock? _searchTimeText;
        private StackPanel? _searchTimePanel;

        private ObservableCollection<ResultRowViewModel> _results;
        private string _filterName = "Unknown Filter";
        private SearchHistoryService? _searchHistoryService;
        private SpriteService? _spriteService;

        public SearchResultsModal()
        {
            _results = new ObservableCollection<ResultRowViewModel>();
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Get control references
            _resultsGrid = this.FindControl<DataGrid>("ResultsGrid");
            _filterNameText = this.FindControl<TextBlock>("FilterNameText");
            _resultCountText = this.FindControl<TextBlock>("ResultCountText");
            _highestScoreText = this.FindControl<TextBlock>("HighestScoreText");
            _averageScoreText = this.FindControl<TextBlock>("AverageScoreText");
            _searchTimeText = this.FindControl<TextBlock>("SearchTimeText");
            _searchTimePanel = this.FindControl<StackPanel>("SearchTimePanel");

            // Get services
            _searchHistoryService = ServiceHelper.GetService<SearchHistoryService>();
            _spriteService = ServiceHelper.GetService<SpriteService>();

            // Setup DataGrid
            if (_resultsGrid != null)
            {
                _resultsGrid.ItemsSource = _results;
                _resultsGrid.DoubleTapped += OnRowDoubleTapped;
                _resultsGrid.KeyDown += OnGridKeyDown;
            }

            // Wire up buttons
            var exportButton = this.FindControl<Button>("ExportCsvButton");
            if (exportButton != null)
                exportButton.Click += OnExportCsvClick;

            var copyAllButton = this.FindControl<Button>("CopyAllButton");
            if (copyAllButton != null)
                copyAllButton.Click += OnCopyAllSeedsClick;

            var loadHistoryButton = this.FindControl<Button>("LoadHistoryButton");
            if (loadHistoryButton != null)
                loadHistoryButton.Click += OnLoadHistoryClick;

            var refreshButton = this.FindControl<Button>("RefreshButton");
            if (refreshButton != null)
                refreshButton.Click += OnRefreshClick;
        }

        public void LoadResults(List<SearchResult> results, string filterName, TimeSpan? searchDuration = null)
        {
            _filterName = filterName;
            _results.Clear();

            foreach (var result in results)
            {
                _results.Add(new ResultRowViewModel(result, _spriteService));
            }

            UpdateStats(searchDuration);
            UpdateUI();
        }

        public async Task LoadHistoricalResults(int searchId)
        {
            if (_searchHistoryService == null) return;

            try
            {
                // TODO: Get search info from history service
                // For now, just get the results

                var results = await _searchHistoryService.GetSearchResultsAsync(searchId);

                _filterName = "Historical Search";
                _results.Clear();

                // Convert Oracle.Models.SearchResult to our local SearchResult format
                foreach (var result in results)
                {
                    var modalResult = new SearchResult
                    {
                        Seed = result.Seed,
                        Score = result.Score,
                        Antes = result.Ante.ToString(),
                        ItemsJson = result.ScoreBreakdown,
                        Timestamp = DateTime.Now
                    };
                    _results.Add(new ResultRowViewModel(modalResult, _spriteService));
                }

                UpdateStats(null);
                UpdateUI();

                // Show timestamp column for historical results
                if (_resultsGrid != null)
                {
                    var timestampColumn = _resultsGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Timestamp");
                    if (timestampColumn != null)
                        timestampColumn.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchResultsModal", $"Failed to load historical results: {ex.Message}");
            }
        }

        private void UpdateStats(TimeSpan? searchDuration)
        {
            if (_results.Count == 0)
            {
                if (_highestScoreText != null) _highestScoreText.Text = "0";
                if (_averageScoreText != null) _averageScoreText.Text = "0";
                return;
            }

            var scores = _results.Select(r => r.Score).ToList();
            var highest = scores.Max();
            var average = scores.Average();

            if (_highestScoreText != null)
                _highestScoreText.Text = highest.ToString("N0");

            if (_averageScoreText != null)
                _averageScoreText.Text = average.ToString("N1");

            if (searchDuration.HasValue && _searchTimePanel != null && _searchTimeText != null)
            {
                _searchTimePanel.IsVisible = true;
                _searchTimeText.Text = FormatDuration(searchDuration.Value);
            }
        }

        private void UpdateUI()
        {
            if (_filterNameText != null)
                _filterNameText.Text = _filterName;

            if (_resultCountText != null)
                _resultCountText.Text = _results.Count.ToString("N0");

            // Update the modal title through the base class
            SetTitle($"Search Results - {_filterName} ({_results.Count:N0} results)");
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{duration:h\\:mm\\:ss}";
            else if (duration.TotalMinutes >= 1)
                return $"{duration:m\\:ss}";
            else
                return $"{duration.TotalSeconds:F1}s";
        }

        private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_resultsGrid?.SelectedItem is ResultRowViewModel result)
            {
                await CopySeedToClipboard(result.Seed);
            }
        }

        private async void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                await CopySelectedSeeds();
                e.Handled = true;
            }
            else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _resultsGrid?.SelectAll();
                e.Handled = true;
            }
        }

        private async Task CopySelectedSeeds()
        {
            if (_resultsGrid == null) return;

            var selectedItems = _resultsGrid.SelectedItems.Cast<ResultRowViewModel>().ToList();
            if (selectedItems.Count == 0) return;

            var seeds = string.Join(Environment.NewLine, selectedItems.Select(r => r.Seed));
            await CopyToClipboard(seeds);
        }

        private async Task CopySeedToClipboard(string seed)
        {
            await CopyToClipboard(seed);
            // Could show a toast notification here
        }

        private async Task CopyToClipboard(string text)
        {
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchResultsModal", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export Search Results",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    },
                    SuggestedFileName = $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                });

                if (file == null) return;
                var result = file.Path.LocalPath;
                if (string.IsNullOrEmpty(result)) return;

                await ExportToCsv(result);

                // Show success notification
                DebugLogger.Log("SearchResultsModal", $"Exported {_results.Count} results to {result}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchResultsModal", $"Export failed: {ex.Message}");
            }
        }

        private async Task ExportToCsv(string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Seed,Score,Antes,Items Found,Timestamp");

            foreach (var result in _results)
            {
                var items = string.Join("; ", result.FoundItems.Select(i => i.Tooltip));
                csv.AppendLine($"{result.Seed},{result.Score},{result.Antes},\"{items}\",{result.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        private async void OnCopyAllSeedsClick(object? sender, RoutedEventArgs e)
        {
            var seeds = string.Join(Environment.NewLine, _results.Select(r => r.Seed));
            await CopyToClipboard(seeds);
        }

        private async void OnLoadHistoryClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_searchHistoryService == null) return;

                // Get recent searches
                var recentSearches = await _searchHistoryService.GetRecentSearchesAsync(10);

                if (recentSearches.Count == 0)
                {
                    DebugLogger.Log("SearchResultsModal", "No search history found");
                    return;
                }

                // For now, just load the most recent search
                var mostRecent = recentSearches.First();
                await LoadHistoricalResults((int)mostRecent.SearchId);

                DebugLogger.Log("SearchResultsModal", $"Loaded historical search: {mostRecent.ConfigPath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchResultsModal", $"Failed to load history: {ex.Message}");
            }
        }

        private void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            // Refresh by updating the UI display with current data
            UpdateStats(null);
            UpdateUI();

            // Sort results by score descending for better display
            var sortedResults = _results.OrderByDescending(r => r.Score).ToList();
            _results.Clear();
            foreach (var result in sortedResults)
            {
                _results.Add(result);
            }

            DebugLogger.Log("SearchResultsModal", $"Refreshed display with {_results.Count} results");
        }

        private async void OnCopySeedClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string seed)
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.Clipboard != null)
                    {
                        await topLevel.Clipboard.SetTextAsync(seed);
                        DebugLogger.Log("SearchResultsModal", $"Copied seed to clipboard: {seed}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchResultsModal", $"Failed to copy seed: {ex.Message}");
                }
            }
        }
    }

    public class ResultRowViewModel : INotifyPropertyChanged
    {
        private SearchResult _result;
        private SpriteService? _spriteService;

        public string Seed => _result.Seed;
        public double Score => _result.Score;
        public string FormattedScore => Score.ToString("N0", CultureInfo.InvariantCulture);
        public string Antes => _result.Antes ?? "N/A";
        public DateTime Timestamp => _result.Timestamp;
        public ObservableCollection<FoundItemViewModel> FoundItems { get; }

        public string ScoreClass
        {
            get
            {
                if (Score >= 100) return "score-high";
                if (Score >= 50) return "score-medium";
                return "score-low";
            }
        }

        public ResultRowViewModel(SearchResult result, SpriteService? spriteService)
        {
            _result = result;
            _spriteService = spriteService;
            FoundItems = new ObservableCollection<FoundItemViewModel>();

            // Parse items from JSON if available
            if (!string.IsNullOrEmpty(result.ItemsJson))
            {
                try
                {
                    // Parse JSON and create FoundItemViewModel instances
                    var json = System.Text.Json.JsonDocument.Parse(result.ItemsJson);
                    if (json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var property in json.RootElement.EnumerateObject())
                        {
                            var itemName = property.Name;
                            var itemValue = property.Value;

                            // Try to get the sprite for this item
                            var bitmap = _spriteService?.GetJokerImage(itemName) as Bitmap;
                            if (bitmap == null)
                            {
                                // Try other sprite types if not a joker
                                bitmap = _spriteService?.GetVoucherImage(itemName) as Bitmap ??
                                        _spriteService?.GetTarotImage(itemName) as Bitmap ??
                                        _spriteService?.GetTagImage(itemName) as Bitmap;
                            }

                            FoundItems.Add(new FoundItemViewModel
                            {
                                Icon = bitmap,
                                Tooltip = itemName.Replace("_", " ")
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("ResultRowViewModel", $"Failed to parse items JSON: {ex.Message}");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FoundItemViewModel
    {
        public Bitmap? Icon { get; set; }
        public string Tooltip { get; set; } = "";
    }

}