using System;
using BalatroSeedOracle.Json;
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
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Windows
{
    public partial class DataGridResultsWindow : Window
    {
        private DataGrid? _resultsGrid;
        private DataGrid? _queryResultsGrid;
        private TextBox? _quickSearchBox;
        private SelectableTextBlock? _statusText;
        private SelectableTextBlock? _queryStatusText;
        private Button? _clearSearchButton;
        private Button? _loadMoreButton;
        private TextEditor? _sqlEditor;
        private ComboBox? _exampleQueriesCombo;

        private ObservableCollection<DataGridResultItem> _results = new();
        private ObservableCollection<DataGridResultItem> _filteredResults = new();

        public DataGridResultsWindow()
        {
            InitializeComponent();
            WireUpControls();
            SetupControls();
            SetupSqlEditor();
        }

        private void WireUpControls()
        {
            // Control references are auto-generated from x:Name attributes
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

            // Export menu items - direct field access from x:Name
            if (ExportCsvMenuItem != null)
                ExportCsvMenuItem.Click += async (s, e) => await ExportToCsvAsync();
            if (ExportJsonMenuItem != null)
                ExportJsonMenuItem.Click += async (s, e) => await ExportToJsonAsync();
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
                RunQueryButton.Click += (s, e) => RunSqlQuery();
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

        private void RunSqlQuery()
        {
            if (_sqlEditor == null || _queryResultsGrid == null)
                return;

            var sql = _sqlEditor.Text;
            if (string.IsNullOrWhiteSpace(sql))
                return;

            try
            {
                // SQL Editor feature has been removed - database operations are now handled internally by Motely
                UpdateQueryStatus(
                    "SQL Editor is no longer available. Database operations are now handled internally by Motely. Use the Results tab to view search results."
                );
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

            var sb = new StringBuilder();
            sb.AppendLine("rank,seed,score,tallies");
            foreach (var item in _filteredResults)
            {
                sb.AppendLine(
                    $"{item.Rank},{item.Seed},{item.TotalScore},{string.Join(",", item.TallyScores)}"
                );
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(sb.ToString());
            UpdateStatus($"Exported {_filteredResults.Count} rows to CSV");
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

            var json = JsonSerializer.Serialize(
                _filteredResults.ToList(),
                BsoJsonSerializerContext.Default.ListDataGridResultItem
            );

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
            UpdateStatus($"Exported {_filteredResults.Count} rows to JSON");
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
                    SuggestedFileName = "seeds_wordlist",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    },
                }
            );

            if (file == null)
                return;

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            foreach (var item in _filteredResults)
            {
                await writer.WriteLineAsync(item.Seed);
            }
            UpdateStatus($"Exported {_filteredResults.Count} seeds to wordlist");
        }

        private void CopyToClipboard(object? sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            // Headers
            sb.AppendLine("Rank\tSeed\tTotal Score");

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
                BalatroSeedOracle.Json.BsoJsonSerializerContext.Default.ListDataGridResultItem
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
                    var mainWindow = TopLevel.GetTopLevel(this) as Views.MainWindow;
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
                RunSqlQuery();
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
