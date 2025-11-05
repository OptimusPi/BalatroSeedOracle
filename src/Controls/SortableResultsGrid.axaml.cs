using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
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
        }

        private void InitializeDataGrid()
        {
            // Tally columns will be initialized when results are added
            EnsureTallyColumns();
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
            bool needsRebuild = !_tallyColumnsInitialized || _initializedColumnCount != first!.Scores!.Length;

            // Also rebuild if we now have Labels when we didn't before
            if (_tallyColumnsInitialized && first.Labels != null && first.Labels.Length > 0)
            {
                var existingColumns = dataGrid.Columns.Skip(2).Take(_initializedColumnCount).ToList();
                if (existingColumns.Any(c => c.Header?.ToString()?.StartsWith("TALLY") == true))
                {
                    needsRebuild = true; // We have placeholder names, rebuild with real Labels
                }
            }

            if (!needsRebuild)
                return;

            // Clear existing tally columns (keep SEED and TOTALSCORE)
            if (_tallyColumnsInitialized)
            {
                var columnsToRemove = dataGrid.Columns.Skip(2).Take(_initializedColumnCount).ToList();
                foreach (var col in columnsToRemove)
                {
                    dataGrid.Columns.Remove(col);
                }
            }

            // Add tally columns (insert before Actions column)
            var actionsColumnIndex = dataGrid.Columns.Count - 1;
            for (int i = 0; i < first!.Scores!.Length; i++)
            {
                // UPPERCASE header from Labels (from SearchInstance.ColumnNames)
                var header =
                    (
                        first.Labels != null
                        && i < first.Labels.Length
                        && !string.IsNullOrWhiteSpace(first.Labels[i])
                    )
                        ? first.Labels[i].ToUpperInvariant()
                        : $"TALLY{i + 1}";

                var col = new DataGridTemplateColumn
                {
                    Header = header,
                    Width = new DataGridLength(80),
                };

                // Bind TextBlock to Scores[i]
                var template = new FuncDataTemplate<Models.SearchResult>(
                    (item, _) =>
                    {
                        var tb = new TextBlock
                        {
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 11,
                        };
                        tb.Bind(TextBlock.TextProperty, new Binding($"Scores[{i}]"));
                        return tb;
                    },
                    true
                );

                col.CellTemplate = template;
                dataGrid.Columns.Insert(actionsColumnIndex + i, col);
            }

            _initializedColumnCount = first.Scores.Length;
            _tallyColumnsInitialized = true;
        }

        private void ResetFromItemsSource()
        {
            ViewModel.ClearResults();
            if (_itemsSource != null)
            {
                ViewModel.AddResults(_itemsSource);
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
        public ObservableCollection<SearchResult>? ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged -= OnItemsSourceChanged;
                }

                _itemsSource = value;

                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged += OnItemsSourceChanged;
                    ResetFromItemsSource();
                }
                else
                {
                    ClearResults();
                }
            }
        }

        private void OnItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ResetFromItemsSource();
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
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
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is SearchResult r)
                            {
                                var existing = ViewModel.AllResults.FirstOrDefault(x =>
                                    x.Seed == r.Seed && x.TotalScore == r.TotalScore
                                );
                                if (existing != null)
                                {
                                    ViewModel.AllResults.Remove(existing);
                                }
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    ResetFromItemsSource();
                    break;
            }
        }
    }
}
