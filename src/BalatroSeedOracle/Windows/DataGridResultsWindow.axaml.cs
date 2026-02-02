using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using Microsoft.Extensions.DependencyInjection;
using TextMateSharp.Grammars;

namespace BalatroSeedOracle.Windows
{
    public partial class DataGridResultsWindow : Window
    {
        private readonly ActiveSearchContext? _searchInstance;
        private readonly string? _filterName;
        private DataGrid? _resultsGrid;
        private DataGrid? _queryResultsGrid;
        private TextBox? _quickSearchBox;
        private SelectableTextBlock? _statusText;
        private SelectableTextBlock? _queryStatusText;
        private Button? _clearSearchButton;
        private Button? _loadMoreButton;
        private TextEditor? _sqlEditor;
        private ComboBox? _exampleQueriesCombo;

        private const int INITIAL_RESULTS_PAGE_SIZE = 1000; // Initial number of results to load

        private ObservableCollection<DataGridResultItem> _results = new();
        private ObservableCollection<DataGridResultItem> _filteredResults = new();
        private int _currentLoadedCount = INITIAL_RESULTS_PAGE_SIZE;
        private int _totalCount = 0;

        public DataGridResultsWindow()
        {
            InitializeComponent();
        }

        public DataGridResultsWindow(ActiveSearchContext searchInstance, string? filterName = null)
        {
            _searchInstance = searchInstance;
            _filterName = filterName;
            InitializeComponent();
            SetupControls();
            SetupSqlEditor();

            // Load data asynchronously
            if (_searchInstance != null)
            {
                _ = LoadDataAsync();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Control references are now auto-generated from x:Name attributes
            // No FindControl anti-pattern needed!
            _resultsGrid = ResultsGrid;
            _queryResultsGrid = QueryResultsGrid;
            _quickSearchBox = QuickSearchBox;
            _statusText = StatusText;
            _queryStatusText = QueryStatusText;
            _clearSearchButton = ClearSearchButton;
            _loadMoreButton = LoadMoreButton;
            _sqlEditor = SqlEditor;
            _exampleQueriesCombo = ExampleQueriesCombo;

            // Add keyboard shortcuts
            KeyDown += OnKeyDown;
        }

        private void SetupControls()
        {
            if (_resultsGrid != null)
            {
                _resultsGrid.ItemsSource = _filteredResults;

                // Add context menu for rows
                _resultsGrid.ContextMenu = CreateRowContextMenu();

                // Enable sorting
                _resultsGrid.Sorting += OnDataGridSorting;
            }

            // Wire up event handlers
            if (_quickSearchBox != null)
            {
                _quickSearchBox.TextChanged += OnQuickSearchTextChanged;
            }

            if (_clearSearchButton != null)
            {
                _clearSearchButton.Click += (s, e) =>
                {
                    if (_quickSearchBox != null)
                        _quickSearchBox.Text = string.Empty;
                };
            }

            if (_loadMoreButton != null)
            {
                _loadMoreButton.Click += async (s, e) => await LoadMoreResultsAsync();
            }

            // Export menu items - direct field access from x:Name
            if (ExportCsvMenuItem != null)
                ExportCsvMenuItem.Click += async (s, e) => await ExportToCsvAsync();
            if (ExportJsonMenuItem != null)
                ExportJsonMenuItem.Click += async (s, e) => await ExportToJsonAsync();
            if (ExportParquetMenuItem != null)
                ExportParquetMenuItem.Click += async (s, e) => await ExportToParquetAsync();
            if (ExportWordlistMenuItem != null)
                ExportWordlistMenuItem.Click += async (s, e) => await ExportToWordlistAsync();
            if (CopyToClipboardMenuItem != null)
                CopyToClipboardMenuItem.Click += CopyToClipboard;

            // Other buttons - direct field access from x:Name
            if (CopyButton != null)
                CopyButton.Click += CopySelectedRows;
            if (SelectAllButton != null)
                SelectAllButton.Click += (s, e) => _resultsGrid?.SelectAll();
            if (ClearSelectionButton != null)
                ClearSelectionButton.Click += (s, e) => _resultsGrid?.SelectedItems.Clear();

            // SQL controls - direct field access from x:Name
            if (RunQueryButton != null)
                RunQueryButton.Click += async (s, e) => await RunSqlQueryAsync();
            if (ClearQueryButton != null)
                ClearQueryButton.Click += (s, e) => _sqlEditor?.Clear();

            if (_exampleQueriesCombo != null)
            {
                _exampleQueriesCombo.SelectionChanged += OnExampleQuerySelected;
            }
        }

        private void SetupSqlEditor()
        {
            if (_sqlEditor == null)
                return;

            // Set default SQL text
            _sqlEditor.Text =
                @"-- DuckDB SQL Query Editor
-- Table: results
-- Columns: seed, score, tally_0, tally_1, etc.

SELECT seed, score 
FROM results 
ORDER BY score DESC 
LIMIT 100;";

            // Defer syntax highlighting setup to avoid rendering issues
            Dispatcher.UIThread.Post(
                () =>
                {
                    try
                    {
                        if (_sqlEditor != null)
                        {
                            _sqlEditor.SyntaxHighlighting =
                                AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition(
                                    "SQL"
                                );
                        }
                    }
                    catch
                    {
                        // Ignore syntax highlighting issues
                    }
                },
                DispatcherPriority.Background
            );
        }

        private ContextMenu CreateRowContextMenu()
        {
            var menu = new ContextMenu();

            var copySeed = new MenuItem { Header = "Copy Seed" };
            copySeed.Click += (s, e) => CopySeedFromSelectedRow();

            var copyRow = new MenuItem { Header = "Copy Row (Tab-delimited)" };
            copyRow.Click += (s, e) => CopySelectedRows(s, e);

            var copyJson = new MenuItem { Header = "Copy Row (JSON)" };
            copyJson.Click += (s, e) => CopySelectedRowsAsJson();

            var viewAnalyzer = new MenuItem { Header = "View in Analyzer" };
            viewAnalyzer.Click += ViewInAnalyzer;

            menu.Items.Add(copySeed);
            menu.Items.Add(copyRow);
            menu.Items.Add(copyJson);
            menu.Items.Add(new Separator());
            menu.Items.Add(viewAnalyzer);

            return menu;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (_searchInstance == null || _resultsGrid == null)
                    return;

                // Update title
                _totalCount = await _searchInstance.GetResultCountAsync();
                var filterDisplay = !string.IsNullOrEmpty(_filterName)
                    ? _filterName
                    : "Unknown Filter";
                Title = $"Results for {filterDisplay} ({_totalCount:N0} seeds)";

                // Get column names from SearchInstance (skip seed and score columns for tally names)
                var columnNames = _searchInstance.ColumnNames;
                var tallyNames = columnNames.Skip(2).Select(n => n.Replace("_", " ")).ToList();

                // Create columns
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CreateColumns(tallyNames);
                });

                // Load top results
                var topResults = await _searchInstance.GetTopResultsAsync(
                    "score",
                    false,
                    _currentLoadedCount
                );

                // Convert to DataGrid items
                var items = topResults
                    .Select(
                        (r, index) =>
                            new DataGridResultItem
                            {
                                Seed = r.Seed,
                                TotalScore = r.TotalScore,
                                Rank = index + 1,
                                TallyScores = r.Scores?.ToList() ?? new List<int>(),
                            }
                    )
                    .ToList();

                // Update UI on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _results.Clear();
                    foreach (var item in items)
                    {
                        _results.Add(item);
                        _filteredResults.Add(item);
                    }

                    UpdateStatus($"Showing {items.Count:N0} of {_totalCount:N0} results");

                    // Enable load more if there are more results
                    if (_loadMoreButton != null)
                        _loadMoreButton.IsEnabled = _currentLoadedCount < _totalCount;
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Failed to load data: {ex}");
                UpdateStatus($"Error loading data: {ex.Message}");
            }
        }

        private async Task LoadMoreResultsAsync()
        {
            if (_searchInstance == null)
                return;

            _currentLoadedCount += 1000;
            await LoadDataAsync();
        }

        private void CreateColumns(List<string> tallyNames)
        {
            if (_resultsGrid == null)
                return;

            _resultsGrid.Columns.Clear();

            // Fixed columns
            _resultsGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Rank",
                    Binding = new Binding("Rank"),
                    Width = new DataGridLength(60),
                }
            );

            _resultsGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Seed",
                    Binding = new Binding("Seed"),
                    Width = new DataGridLength(150),
                }
            );

            _resultsGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Total Score",
                    Binding = new Binding("TotalScore"),
                    Width = new DataGridLength(100),
                }
            );

            // Dynamic tally columns
            for (int i = 0; i < tallyNames.Count; i++)
            {
                var index = i; // Capture for closure
                _resultsGrid.Columns.Add(
                    new DataGridTextColumn
                    {
                        Header = tallyNames[i],
                        Binding = new Binding($"TallyScores[{index}]"),
                        Width = new DataGridLength(80),
                    }
                );
            }
        }

        private void OnQuickSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = _quickSearchBox?.Text?.ToLower() ?? string.Empty;

            if (_clearSearchButton != null)
                _clearSearchButton.IsEnabled = !string.IsNullOrEmpty(searchText);

            // Filter results
            _filteredResults.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                foreach (var item in _results)
                {
                    _filteredResults.Add(item);
                }
            }
            else
            {
                foreach (var item in _results)
                {
                    if (item.Seed.ToLower().Contains(searchText))
                    {
                        _filteredResults.Add(item);
                    }
                }
            }

            UpdateStatus(
                $"Showing {_filteredResults.Count:N0} of {_results.Count:N0} results"
                    + (string.IsNullOrEmpty(searchText) ? "" : " (filtered)")
            );
        }

        private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
        {
            // Let the DataGrid handle sorting automatically
        }

        private async Task RunSqlQueryAsync()
        {
            if (_searchInstance == null || _sqlEditor == null || _queryResultsGrid == null)
                return;

            var sql = _sqlEditor.Text;
            if (string.IsNullOrWhiteSpace(sql))
                return;

            try
            {
                // SQL Editor feature has been removed - database operations are now handled internally by Motely
                UpdateQueryStatus("SQL Editor is no longer available. Database operations are now handled internally by Motely. Use the Results tab to view search results.");
            }
            catch (Exception ex)
            {
                UpdateQueryStatus($"Error: {ex.Message}");
                DebugLogger.LogError("DataGridResultsWindow", $"SQL query failed: {ex}");
            }
        }

        private void OnExampleQuerySelected(object? sender, SelectionChangedEventArgs e)
        {
            if (_sqlEditor == null || _exampleQueriesCombo == null)
                return;

            // Ensure we're on UI thread when updating text
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => OnExampleQuerySelected(sender, e));
                return;
            }

            var selected = _exampleQueriesCombo.SelectedIndex;
            var query = selected switch
            {
                0 => @"-- Top 100 Seeds by Score
SELECT seed, score 
FROM results 
ORDER BY score DESC 
LIMIT 100;",

                1 => @"-- Statistical Analysis
SELECT 
    COUNT(*) as total_seeds,
    AVG(score) as avg_score,
    MIN(score) as min_score,
    MAX(score) as max_score,
    MEDIAN(score) as median_score
FROM results;",

                2 => @"-- Seeds with high scores
SELECT seed, score 
FROM results
WHERE score > 50
ORDER BY score DESC
LIMIT 100;",

                3 => @"-- Show all columns (first 50 rows)
SELECT * 
FROM results
ORDER BY score DESC
LIMIT 50;",

                _ => "",
            };

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    _sqlEditor.Text = query;
                }
                catch
                {
                    // Ignore editor text setting errors
                }
            }
        }

        private async Task ExportToCsvAsync()
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export to CSV",
                    DefaultExtension = "csv",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    },
                }
            );

            if (file == null)
                return;

            try
            {
                var sb = new StringBuilder();

                // Headers
                sb.AppendLine(
                    "Rank,Seed,Total Score,"
                        + string.Join(
                            ",",
                            _searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " "))
                                ?? new List<string>()
                        )
                );

                // Data
                foreach (var item in _filteredResults)
                {
                    sb.AppendLine(
                        $"{item.Rank},{item.Seed},{item.TotalScore},{string.Join(",", item.TallyScores)}"
                    );
                }

                await File.WriteAllTextAsync(file.Path.LocalPath, sb.ToString());
                UpdateStatus($"Exported {_filteredResults.Count} rows to CSV");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to CSV failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }

        private async Task ExportToJsonAsync()
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export to JSON",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    },
                }
            );

            if (file == null)
                return;

            try
            {
                var json = JsonSerializer.Serialize(
                    _filteredResults,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await File.WriteAllTextAsync(file.Path.LocalPath, json);
                UpdateStatus($"Exported {_filteredResults.Count} rows to JSON");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to JSON failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }

        private async Task ExportToParquetAsync()
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export to Parquet",
                    DefaultExtension = "parquet",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Parquet Files") { Patterns = new[] { "*.parquet" } },
                    },
                }
            );

            if (file == null)
                return;

            try
            {
                // Use platform-specific IParquetExporter
                var parquetExporter = App.GetService<IParquetExporter>();
                if (parquetExporter == null || !parquetExporter.IsAvailable)
                {
                    UpdateStatus("Parquet export not available");
                    return;
                }

                // Headers
                var headers = new List<string> { "Rank", "Seed", "Score" };
                headers.AddRange(
                    _searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " "))
                        ?? new List<string>()
                );

                // Build data rows
                var rows = new List<IReadOnlyList<object?>>();
                foreach (var item in _filteredResults)
                {
                    var row = new List<object?> { item.Rank, item.Seed, item.TotalScore };
                    row.AddRange(item.TallyScores.Cast<object?>());
                    rows.Add(row);
                }

                await parquetExporter.ExportAsync(
                    file.Path.LocalPath,
                    headers,
                    rows
                );
                UpdateStatus($"Exported {_filteredResults.Count} rows to Parquet");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to Parquet failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }

        private async Task ExportToWordlistAsync()
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export to Wordlist",
                    DefaultExtension = "txt",
                    SuggestedFileName = $"{_filterName ?? "seeds"}_wordlist",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    },
                }
            );

            if (file == null)
                return;

            try
            {
                // Export just the seeds, one per line - perfect for Motely wordlist input
                var seeds = _filteredResults.Select(r => r.Seed);
                await File.WriteAllLinesAsync(file.Path.LocalPath, seeds);
                UpdateStatus($"Exported {_filteredResults.Count} seeds to wordlist");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to Wordlist failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }

        private void CopyToClipboard(object? sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            // Headers
            sb.AppendLine(
                "Rank\tSeed\tTotal Score\t"
                    + string.Join(
                        "\t",
                        _searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " "))
                            ?? new List<string>()
                    )
            );

            // Data
            foreach (var item in _filteredResults)
            {
                sb.AppendLine(
                    $"{item.Rank}\t{item.Seed}\t{item.TotalScore}\t{string.Join("\t", item.TallyScores)}"
                );
            }

            Clipboard?.SetTextAsync(sb.ToString());
            UpdateStatus("Copied to clipboard");
        }

        private void CopySelectedRows(object? sender, RoutedEventArgs e)
        {
            if (_resultsGrid?.SelectedItems == null)
                return;

            var sb = new StringBuilder();
            foreach (DataGridResultItem item in _resultsGrid.SelectedItems)
            {
                sb.AppendLine(
                    $"{item.Rank}\t{item.Seed}\t{item.TotalScore}\t{string.Join("\t", item.TallyScores)}"
                );
            }

            Clipboard?.SetTextAsync(sb.ToString());
            UpdateStatus($"Copied {_resultsGrid.SelectedItems.Count} rows");
        }

        private void CopySelectedRowsAsJson()
        {
            if (_resultsGrid?.SelectedItems == null)
                return;

            var items = _resultsGrid.SelectedItems.Cast<DataGridResultItem>().ToList();
            var json = JsonSerializer.Serialize(
                items,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Clipboard?.SetTextAsync(json);
            UpdateStatus($"Copied {items.Count} rows as JSON");
        }

        private void CopySeedFromSelectedRow()
        {
            if (_resultsGrid?.SelectedItem is DataGridResultItem item)
            {
                Clipboard?.SetTextAsync(item.Seed);
                UpdateStatus($"Copied seed: {item.Seed}");
            }
        }

        private void ViewInAnalyzer(object? sender, RoutedEventArgs e)
        {
            if (_resultsGrid?.SelectedItem is DataGridResultItem item)
            {
                try
                {
                    // Find the main window and show as modal
                    var mainWindow = this.GetVisualRoot() as Views.MainWindow;
                    var mainMenu = mainWindow?.MainMenu;

                    if (mainMenu != null)
                    {
                        // Create analyzer modal with the seed
                        var analyzeModal = new Views.Modals.AnalyzeModal();
                        analyzeModal.SetSeedAndAnalyze(item.Seed);

                        var modal = new Views.Modals.StandardModal("ANALYZE");
                        modal.SetContent(analyzeModal);
                        modal.BackClicked += (s, ev) => mainMenu.HideModalContent();

                        mainMenu.ShowModalContent(modal, "SEED ANALYZER");
                        DebugLogger.Log(
                            "DataGridResultsWindow",
                            $"Opened analyzer modal for seed: {item.Seed}"
                        );
                    }
                    else
                    {
                        DebugLogger.LogError(
                            "DataGridResultsWindow",
                            "Could not find main menu for modal display"
                        );
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "DataGridResultsWindow",
                        $"Error opening analyzer: {ex.Message}"
                    );
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // F5 to run query
            if (e.Key == Key.F5)
            {
                _ = RunSqlQueryAsync();
                e.Handled = true;
            }
            // Ctrl+C to copy
            else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                CopySelectedRows(null, null!);
                e.Handled = true;
            }
            // Ctrl+A to select all
            else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _resultsGrid?.SelectAll();
                e.Handled = true;
            }
            // Ctrl+F to focus search
            else if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _quickSearchBox?.Focus();
                e.Handled = true;
            }
            // F11 for fullscreen
            else if (e.Key == Key.F11)
            {
                WindowState =
                    WindowState == WindowState.FullScreen
                        ? WindowState.Normal
                        : WindowState.FullScreen;
                e.Handled = true;
            }
            // Escape to exit fullscreen
            else if (e.Key == Key.Escape && WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
                e.Handled = true;
            }
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
            }
        }

        private void UpdateQueryStatus(string message)
        {
            if (_queryStatusText != null)
            {
                _queryStatusText.Text = message;
            }
        }
    }

    public class DataGridResultItem
    {
        public string Seed { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int Rank { get; set; }
        public List<int> TallyScores { get; set; } = new();
    }
}
