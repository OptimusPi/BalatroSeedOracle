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
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using DuckDB.NET.Data;
using TextMateSharp.Grammars;

namespace BalatroSeedOracle.Windows
{
    public partial class DataGridResultsWindow : Window
    {
        private readonly SearchInstance? _searchInstance;
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
        
        private ObservableCollection<DataGridResultItem> _results = new();
        private ObservableCollection<DataGridResultItem> _filteredResults = new();
        private int _currentLoadedCount = 1000;
        private int _totalCount = 0;
        
        public DataGridResultsWindow()
        {
            InitializeComponent();
        }
        
        public DataGridResultsWindow(SearchInstance searchInstance, string? filterName = null)
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
            
            // Get control references
            _resultsGrid = this.FindControl<DataGrid>("ResultsGrid");
            _queryResultsGrid = this.FindControl<DataGrid>("QueryResultsGrid");
            _quickSearchBox = this.FindControl<TextBox>("QuickSearchBox");
            _statusText = this.FindControl<SelectableTextBlock>("StatusText");
            _queryStatusText = this.FindControl<SelectableTextBlock>("QueryStatusText");
            _clearSearchButton = this.FindControl<Button>("ClearSearchButton");
            _loadMoreButton = this.FindControl<Button>("LoadMoreButton");
            _sqlEditor = this.FindControl<TextEditor>("SqlEditor");
            _exampleQueriesCombo = this.FindControl<ComboBox>("ExampleQueriesCombo");
            
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
            
            // Export menu items
            var exportCsv = this.FindControl<MenuItem>("ExportCsvMenuItem");
            var exportJson = this.FindControl<MenuItem>("ExportJsonMenuItem");
            var exportExcel = this.FindControl<MenuItem>("ExportExcelMenuItem");
            var copyClipboard = this.FindControl<MenuItem>("CopyToClipboardMenuItem");
            
            if (exportCsv != null) exportCsv.Click += async (s, e) => await ExportToCsvAsync();
            if (exportJson != null) exportJson.Click += async (s, e) => await ExportToJsonAsync();
            if (exportExcel != null) exportExcel.Click += async (s, e) => await ExportToExcelAsync();
            if (copyClipboard != null) copyClipboard.Click += CopyToClipboard;
            
            // Other buttons
            var copyButton = this.FindControl<Button>("CopyButton");
            var selectAllButton = this.FindControl<Button>("SelectAllButton");
            var clearSelectionButton = this.FindControl<Button>("ClearSelectionButton");
            
            if (copyButton != null) copyButton.Click += CopySelectedRows;
            if (selectAllButton != null) selectAllButton.Click += (s, e) => _resultsGrid?.SelectAll();
            if (clearSelectionButton != null) clearSelectionButton.Click += (s, e) => _resultsGrid?.SelectedItems.Clear();
            
            // SQL controls
            var runQueryButton = this.FindControl<Button>("RunQueryButton");
            var clearQueryButton = this.FindControl<Button>("ClearQueryButton");
            
            if (runQueryButton != null) runQueryButton.Click += async (s, e) => await RunSqlQueryAsync();
            if (clearQueryButton != null) clearQueryButton.Click += (s, e) => _sqlEditor?.Clear();
            
            if (_exampleQueriesCombo != null)
            {
                _exampleQueriesCombo.SelectionChanged += OnExampleQuerySelected;
            }
        }
        
        private void SetupSqlEditor()
        {
            if (_sqlEditor == null) return;
            
            // Set default SQL text
            _sqlEditor.Text = @"-- DuckDB SQL Query Editor
-- Table: results
-- Columns: seed, score, tally_0, tally_1, etc.

SELECT seed, score 
FROM results 
ORDER BY score DESC 
LIMIT 100;";
            
            // Setup syntax highlighting (basic for now)
            _sqlEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
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
                var filterDisplay = !string.IsNullOrEmpty(_filterName) ? _filterName : "Unknown Filter";
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
                var topResults = await _searchInstance.GetTopResultsAsync("score", false, _currentLoadedCount);
                
                // Convert to DataGrid items
                var items = topResults.Select((r, index) => new DataGridResultItem
                {
                    Seed = r.Seed,
                    TotalScore = r.TotalScore,
                    Rank = index + 1,
                    TallyScores = r.Scores?.ToList() ?? new List<int>()
                }).ToList();
                
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
            if (_searchInstance == null) return;
            
            _currentLoadedCount += 1000;
            await LoadDataAsync();
        }
        
        private void CreateColumns(List<string> tallyNames)
        {
            if (_resultsGrid == null)
                return;
            
            _resultsGrid.Columns.Clear();
            
            // Fixed columns
            _resultsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Rank",
                Binding = new Binding("Rank"),
                Width = new DataGridLength(60)
            });
            
            _resultsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Seed",
                Binding = new Binding("Seed"),
                Width = new DataGridLength(150)
            });
            
            _resultsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Total Score",
                Binding = new Binding("TotalScore"),
                Width = new DataGridLength(100)
            });
            
            // Dynamic tally columns
            for (int i = 0; i < tallyNames.Count; i++)
            {
                var index = i; // Capture for closure
                _resultsGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = tallyNames[i],
                    Binding = new Binding($"TallyScores[{index}]"),
                    Width = new DataGridLength(80)
                });
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
            
            UpdateStatus($"Showing {_filteredResults.Count:N0} of {_results.Count:N0} results" +
                        (string.IsNullOrEmpty(searchText) ? "" : " (filtered)"));
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
                UpdateQueryStatus("Executing query...");
                var stopwatch = Stopwatch.StartNew();
                
                // Get the database path from SearchInstance
                var dbPath = _searchInstance.DatabasePath;
                
                using (var connection = new DuckDB.NET.Data.DuckDBConnection($"DataSource={dbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = command.ExecuteReader())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            
                            stopwatch.Stop();
                            
                            // Display results in the query results grid
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                _queryResultsGrid.ItemsSource = dataTable.DefaultView;
                                UpdateQueryStatus($"Query completed: {dataTable.Rows.Count} rows, {stopwatch.ElapsedMilliseconds}ms");
                            });
                        }
                    }
                }
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
            
            var selected = _exampleQueriesCombo.SelectedIndex;
            var query = selected switch
            {
                0 => @"-- Top 100 Seeds by Score
SELECT seed, total_score 
FROM results 
ORDER BY total_score DESC 
LIMIT 100;",
                
                1 => @"-- Statistical Analysis
SELECT 
    COUNT(*) as total_seeds,
    AVG(total_score) as avg_score,
    MIN(total_score) as min_score,
    MAX(total_score) as max_score,
    MEDIAN(total_score) as median_score
FROM results;",
                
                2 => @"-- Seeds with high scores
SELECT seed, total_score 
FROM results
WHERE total_score > 50
ORDER BY total_score DESC
LIMIT 100;",
                
                3 => @"-- Show all columns (first 50 rows)
SELECT * 
FROM results
ORDER BY total_score DESC
LIMIT 50;",
                
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(query))
                _sqlEditor.Text = query;
        }
        
        private async Task ExportToCsvAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export to CSV",
                DefaultExtension = "csv",
                FileTypeChoices = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
            });
            
            if (file == null) return;
            
            try
            {
                var sb = new StringBuilder();
                
                // Headers
                sb.AppendLine("Rank,Seed,Total Score," + string.Join(",", 
                    _searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " ")) ?? new List<string>()));
                
                // Data
                foreach (var item in _filteredResults)
                {
                    sb.AppendLine($"{item.Rank},{item.Seed},{item.TotalScore},{string.Join(",", item.TallyScores)}");
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
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export to JSON",
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
            });
            
            if (file == null) return;
            
            try
            {
                var json = JsonSerializer.Serialize(_filteredResults, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(file.Path.LocalPath, json);
                UpdateStatus($"Exported {_filteredResults.Count} rows to JSON");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to JSON failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }
        
        private async Task ExportToExcelAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export to Excel",
                DefaultExtension = "xlsx",
                FileTypeChoices = new[] { new FilePickerFileType("Excel Files") { Patterns = new[] { "*.xlsx" } } }
            });
            
            if (file == null) return;
            
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Search Results");
                
                // Headers
                var headers = new List<string> { "Rank", "Seed", "Total Score" };
                headers.AddRange(_searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " ")) ?? new List<string>());
                
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }
                
                // Data
                int row = 2;
                foreach (var item in _filteredResults)
                {
                    worksheet.Cell(row, 1).Value = item.Rank;
                    worksheet.Cell(row, 2).Value = item.Seed;
                    worksheet.Cell(row, 3).Value = item.TotalScore;
                    
                    for (int i = 0; i < item.TallyScores.Count; i++)
                    {
                        worksheet.Cell(row, i + 4).Value = item.TallyScores[i];
                    }
                    row++;
                }
                
                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();
                
                workbook.SaveAs(file.Path.LocalPath);
                UpdateStatus($"Exported {_filteredResults.Count} rows to Excel");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DataGridResultsWindow", $"Export to Excel failed: {ex}");
                UpdateStatus($"Export failed: {ex.Message}");
            }
        }
        
        private void CopyToClipboard(object? sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            
            // Headers
            sb.AppendLine("Rank\tSeed\tTotal Score\t" + string.Join("\t", 
                _searchInstance?.ColumnNames.Skip(2).Select(n => n.Replace("_", " ")) ?? new List<string>()));
            
            // Data
            foreach (var item in _filteredResults)
            {
                sb.AppendLine($"{item.Rank}\t{item.Seed}\t{item.TotalScore}\t{string.Join("\t", item.TallyScores)}");
            }
            
            Clipboard?.SetTextAsync(sb.ToString());
            UpdateStatus("Copied to clipboard");
        }
        
        private void CopySelectedRows(object? sender, RoutedEventArgs e)
        {
            if (_resultsGrid?.SelectedItems == null) return;
            
            var sb = new StringBuilder();
            foreach (DataGridResultItem item in _resultsGrid.SelectedItems)
            {
                sb.AppendLine($"{item.Rank}\t{item.Seed}\t{item.TotalScore}\t{string.Join("\t", item.TallyScores)}");
            }
            
            Clipboard?.SetTextAsync(sb.ToString());
            UpdateStatus($"Copied {_resultsGrid.SelectedItems.Count} rows");
        }
        
        private void CopySelectedRowsAsJson()
        {
            if (_resultsGrid?.SelectedItems == null) return;
            
            var items = _resultsGrid.SelectedItems.Cast<DataGridResultItem>().ToList();
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            
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
                // TODO: Open analyzer with this seed
                DebugLogger.Log("DataGridResultsWindow", $"View in analyzer: {item.Seed}");
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
                WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
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