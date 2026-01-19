using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.Controls;

namespace BalatroSeedOracle.Controls
{
    public partial class SortableResultsGrid : UserControl
    {
        private bool _tallyColumnsInitialized = false;
        private int _initializedColumnCount = 0;
        private ObservableCollection<SearchResult>? _itemsSource;

        public SortableResultsGridViewModel ViewModel { get; }

        // Expose ViewModel events for external access
        public event EventHandler<SearchResult>? SeedCopied
        {
            add => ViewModel.SeedCopied += value;
            remove => ViewModel.SeedCopied -= value;
        }

        public event EventHandler<SearchResult>? SearchSimilarRequested
        {
            add => ViewModel.SearchSimilarRequested += value;
            remove => ViewModel.SearchSimilarRequested -= value;
        }

        public event EventHandler<SearchResult>? AddToFavoritesRequested
        {
            add => ViewModel.AddToFavoritesRequested += value;
            remove => ViewModel.AddToFavoritesRequested -= value;
        }

        public event EventHandler<SearchResult>? ExportSeedRequested
        {
            add => ViewModel.ExportSeedRequested += value;
            remove => ViewModel.ExportSeedRequested -= value;
        }

        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested
        {
            add => ViewModel.ExportAllRequested += value;
            remove => ViewModel.ExportAllRequested -= value;
        }

        public event EventHandler? PopOutRequested
        {
            add => ViewModel.PopOutRequested += value;
            remove => ViewModel.PopOutRequested -= value;
        }

        public event EventHandler<SearchResult>? AnalyzeRequested
        {
            add => ViewModel.AnalyzeRequested += value;
            remove => ViewModel.AnalyzeRequested -= value;
        }

        public SortableResultsGrid()
        {
            ViewModel = new SortableResultsGridViewModel();
            DataContext = ViewModel;

            InitializeComponent();
            InitializeDataGrid();

            // CRITICAL FIX: Listen to DisplayedResults changes to force DataGrid refresh
            ViewModel.DisplayedResults.CollectionChanged += OnDisplayedResultsChanged;

            // Wire up clipboard event
            ViewModel.CopyToClipboardRequested += async (s, text) =>
                await CopyToClipboardAsync(text);
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("SortableResultsGrid", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SortableResultsGrid",
                    $"Failed to copy to clipboard: {ex.Message}"
                );
            }
        }

        private void OnDisplayedResultsChanged(
            object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e
        )
        {
            DebugLogger.Log(
                "SortableResultsGrid",
                $"OnDisplayedResultsChanged: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, DisplayedResults.Count={ViewModel.DisplayedResults.Count}"
            );

            // Ensure tally columns are updated when results change
            if (
                e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Reset
            )
            {
                EnsureTallyColumns();
            }
        }

        private void InitializeDataGrid()
        {
            var dataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
            if (dataGrid != null)
            {
                dataGrid.Sorting += OnDataGridSorting;

                // Enable multi-select with keyboard shortcuts
                dataGrid.KeyDown += OnDataGridKeyDown;

                // Context menu for rows
                dataGrid.ContextMenu = CreateContextMenu();

                // Better selection handling
                dataGrid.SelectionChanged += OnSelectionChanged;
            }

            // Tally columns will be initialized when results are added
            EnsureTallyColumns();

            // DataGrid is bound to DisplayedResults via XAML - no need to set explicitly
            DebugLogger.Log(
                "SortableResultsGrid",
                "DataGrid initialized with XAML binding to DisplayedResults"
            );
        }

        private void OnDataGridKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
            {
                if (e.Key == Avalonia.Input.Key.A)
                {
                    // Ctrl+A: Select all
                    var dataGrid = sender as DataGrid;
                    if (dataGrid != null)
                    {
                        dataGrid.SelectAll();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Avalonia.Input.Key.C)
                {
                    // Ctrl+C: Copy selected
                    ViewModel.CopySelectedCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && ViewModel != null)
            {
                ViewModel.SelectedItems.Clear();
                foreach (var item in dataGrid.SelectedItems)
                {
                    if (item is SearchResult result)
                    {
                        ViewModel.SelectedItems.Add(result);
                    }
                }
            }
        }

        private ContextMenu CreateContextMenu()
        {
            var menu = new ContextMenu();

            var copyItem = new MenuItem
            {
                Header = "Copy Seed",
                Command = ViewModel.CopySeedCommand,
            };
            var copySelectedItem = new MenuItem
            {
                Header = "Copy Selected Seeds",
                Command = ViewModel.CopySelectedCommand,
            };
            var analyzeItem = new MenuItem
            {
                Header = "Analyze Seed",
                Command = ViewModel.AnalyzeCommand,
            };
            var exportSelectedItem = new MenuItem
            {
                Header = "Export Selected",
                Command = ViewModel.ExportSelectedCommand,
            };
            var separator = new Separator();

            menu.Items.Add(copyItem);
            menu.Items.Add(copySelectedItem);
            menu.Items.Add(separator);
            menu.Items.Add(analyzeItem);
            menu.Items.Add(exportSelectedItem);

            // Handle context menu opening to set command parameters
            menu.Opening += (s, e) =>
            {
                var dataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
                if (dataGrid?.SelectedItem is SearchResult selectedResult)
                {
                    copyItem.CommandParameter = selectedResult.Seed;
                    analyzeItem.CommandParameter = selectedResult;
                }
            };

            return menu;
        }

        private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
        {
            // Intercept sorting to handle it in the ViewModel for the entire result set
            var column = e.Column;
            var sortMemberPath = column.SortMemberPath;

            if (string.IsNullOrEmpty(sortMemberPath))
                return;

            // Determine direction (toggle if same column)
            bool descending = true;
            if (ViewModel.CurrentSortProperty == sortMemberPath)
            {
                descending = !ViewModel.SortDescending;
            }

            // Update ViewModel
            ViewModel.CurrentSortProperty = sortMemberPath;
            ViewModel.SortDescending = descending;

            // Note: Visual sort indicators removed - SortDirection not available in Avalonia DataGridColumn

            // Prevent default DataGrid sorting (we handle it manually)
            e.Handled = true;
        }

        private void EnsureTallyColumns()
        {
            var dataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
            if (dataGrid == null)
                return;

            var first = ViewModel.AllResults.FirstOrDefault();
            if (first?.Scores == null || first.Scores.Length == 0)
            {
                // Try items source if AllResults empty
                first = _itemsSource?.FirstOrDefault();
                if (first?.Scores == null || first.Scores.Length == 0)
                    return;
            }

            // Check if we need to rebuild columns
            bool needsRebuild =
                !_tallyColumnsInitialized || _initializedColumnCount != first!.Scores!.Length;

            // Also rebuild if we now have Labels when we didn't before
            if (_tallyColumnsInitialized && first.Labels != null && first.Labels.Length > 0)
            {
                var existingColumns = dataGrid
                    .Columns.Skip(2)
                    .Take(_initializedColumnCount)
                    .ToList();
                if (existingColumns.Any(c => c.Header?.ToString()?.StartsWith("TALLY") == true))
                {
                    needsRebuild = true; // We have placeholder names, rebuild with real Labels
                }
            }

            if (!needsRebuild)
                return;

            // Clear existing tally columns (keep SEED and SCORE)
            if (_tallyColumnsInitialized)
            {
                var columnsToRemove = dataGrid
                    .Columns.Skip(2)
                    .Take(_initializedColumnCount)
                    .ToList();
                foreach (var col in columnsToRemove)
                {
                    dataGrid.Columns.Remove(col);
                }
            }

            // Add tally columns (insert before Actions column)
            var actionsColumnIndex = dataGrid.Columns.Count - 1;
            for (int i = 0; i < first!.Scores!.Length; i++)
            {
                int index = i; // CRITICAL FIX: Capture loop variable
                // UPPERCASE header from Labels (from SearchInstance.ColumnNames)
                var header =
                    (
                        first.Labels != null
                        && index < first.Labels.Length
                        && !string.IsNullOrWhiteSpace(first.Labels[index])
                    )
                        ? first.Labels[index].ToUpperInvariant()
                        : $"TALLY{index + 1}";

                var col = new DataGridTemplateColumn
                {
                    Header = header,
                    Width = new DataGridLength(80),
                    CanUserSort = true,
                    SortMemberPath = $"Scores[{index}]",
                };

                // Bind TextBlock to Scores[i] using proper AvaloniaUI binding
                var template = new FuncDataTemplate<Models.SearchResult>(
                    (item, _) =>
                    {
                        var fontFamily =
                            this.FindResource("BalatroFont") as FontFamily
                            ?? new FontFamily("Consolas");
                        var tb = new TextBlock
                        {
                            FontFamily = fontFamily,
                            FontSize = 14,
                            Foreground = Brushes.White,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            Text =
                                (item?.Scores != null && index < item.Scores.Length)
                                    ? item.Scores[index].ToString()
                                    : "0",
                        };
                        return tb;
                    },
                    true
                );

                col.CellTemplate = template;
                dataGrid.Columns.Insert(actionsColumnIndex + index, col);
            }

            _initializedColumnCount = first.Scores.Length;
            _tallyColumnsInitialized = true;
        }

        private void ResetFromItemsSource()
        {
            DebugLogger.LogImportant(
                "SortableResultsGrid",
                "ResetFromItemsSource: Clearing existing results"
            );
            ViewModel.ClearResults();

            if (_itemsSource != null)
            {
                DebugLogger.LogImportant(
                    "SortableResultsGrid",
                    $"ResetFromItemsSource: Adding {_itemsSource.Count} results from bound collection"
                );
                ViewModel.AddResults(_itemsSource);
                DebugLogger.LogImportant(
                    "SortableResultsGrid",
                    $"ResetFromItemsSource: After adding - DisplayedResults.Count={ViewModel.DisplayedResults.Count}"
                );
            }
            else
            {
                DebugLogger.Log(
                    "SortableResultsGrid",
                    "ResetFromItemsSource: No ItemsSource to add from"
                );
            }

            // Rebuild tally columns if needed
            EnsureTallyColumns();
        }

        // Public method wrappers for simplified API
        public void AddResults(IEnumerable<SearchResult> results)
        {
            ViewModel.AddResults(results);
            EnsureTallyColumns();
        }

        public void AddResult(SearchResult result)
        {
            ViewModel.AddResult(result);
            EnsureTallyColumns();
        }

        public void ClearResults() => ViewModel.ClearResults();

        public IEnumerable<SearchResult> GetAllResults() => ViewModel.GetAllResults();

        public IEnumerable<SearchResult> GetDisplayedResults() => ViewModel.GetDisplayedResults();

        // Bind an external collection of results. Updates grid as collection changes.
        public static readonly StyledProperty<ObservableCollection<SearchResult>?> ItemsSourceProperty =
            AvaloniaProperty.Register<SortableResultsGrid, ObservableCollection<SearchResult>?>(
                nameof(ItemsSource),
                defaultBindingMode: Avalonia.Data.BindingMode.OneWay
            );

        public ObservableCollection<SearchResult>? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsSourceProperty)
            {
                var oldCount = (change.OldValue as ObservableCollection<SearchResult>)?.Count ?? 0;
                var newCount = (change.NewValue as ObservableCollection<SearchResult>)?.Count ?? 0;

                DebugLogger.LogImportant(
                    "SortableResultsGrid",
                    $"ItemsSource CHANGED: Old collection had {oldCount} items, new collection has {newCount} items"
                );

                if (change.OldValue is ObservableCollection<SearchResult> oldCollection)
                {
                    oldCollection.CollectionChanged -= OnItemsSourceChanged;
                    DebugLogger.Log("SortableResultsGrid", "Unsubscribed from old collection");
                }

                _itemsSource = change.NewValue as ObservableCollection<SearchResult>;

                if (_itemsSource != null)
                {
                    DebugLogger.LogImportant(
                        "SortableResultsGrid",
                        $"New ItemsSource bound with {_itemsSource.Count} items - subscribing to changes and resetting grid"
                    );
                    _itemsSource.CollectionChanged += OnItemsSourceChanged;
                    ResetFromItemsSource();
                }
                else
                {
                    DebugLogger.Log(
                        "SortableResultsGrid",
                        "ItemsSource set to null - clearing results"
                    );
                    ClearResults();
                }
            }
        }

        private void OnItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DebugLogger.Log(
                "SortableResultsGrid",
                $"OnItemsSourceChanged: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, DisplayedResults={ViewModel.DisplayedResults.Count}"
            );

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ResetFromItemsSource();
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        DebugLogger.Log(
                            "SortableResultsGrid",
                            $"Adding {e.NewItems.Count} items to grid"
                        );
                        foreach (var item in e.NewItems)
                        {
                            if (item is SearchResult r)
                            {
                                ViewModel.AddResult(r);
                            }
                        }
                        EnsureTallyColumns();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    // For remove/replace/move, just reset from source
                    ResetFromItemsSource();
                    break;
            }
        }
    }
}
