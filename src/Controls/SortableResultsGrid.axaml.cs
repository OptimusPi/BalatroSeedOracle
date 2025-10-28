using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.Controls
{
    public partial class SortableResultsGrid : UserControl
    {
        private readonly ObservableCollection<SearchResult> _allResults = new();
        private readonly ObservableCollection<SearchResult> _displayedResults = new();
        private string _currentSortColumn = "TotalScore";
        private bool _sortDescending = true;
        private int _currentPage = 1;
        private int _itemsPerPage = 100;
        private int _totalPages = 1;
        private ObservableCollection<SearchResult>? _itemsSource;
        private bool _tallyColumnsInitialized = false;

        // ICommand properties exposed for XAML bindings
        public ICommand CopySeedCommand { get; }
        public ICommand SearchSimilarCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand ExportSeedCommand { get; }
        public ICommand AnalyzeSeedCommand { get; }

        public event EventHandler<SearchResult>? SeedCopied;
        public event EventHandler<SearchResult>? SearchSimilarRequested;
        public event EventHandler<SearchResult>? AddToFavoritesRequested;
        public event EventHandler<SearchResult>? ExportSeedRequested;
        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested;
        public event EventHandler<SearchResult>? AnalyzeRequested;

        public SortableResultsGrid()
        {
            InitializeComponent();
            InitializeDataGrid();

            // Initialize commands
            CopySeedCommand = new RelayCommand<string>(seed =>
            {
                if (!string.IsNullOrWhiteSpace(seed))
                {
                    CopySeed(seed);
                }
            });

            SearchSimilarCommand = new RelayCommand<SearchResult>(result =>
            {
                if (result != null)
                {
                    SearchSimilar(result);
                }
            });

            AddToFavoritesCommand = new RelayCommand<SearchResult>(result =>
            {
                if (result != null)
                {
                    AddToFavorites(result);
                }
            });

            ExportSeedCommand = new RelayCommand<SearchResult>(result =>
            {
                if (result != null)
                {
                    ExportSeed(result);
                }
            });

            AnalyzeSeedCommand = new RelayCommand<SearchResult>(result =>
            {
                if (result != null)
                {
                    Analyze(result);
                }
            });
        }

        private void InitializeDataGrid()
        {
            var dataGrid = this.FindControl<DataGrid>("ResultsDataGrid")!;
            dataGrid.ItemsSource = _displayedResults;
            // Ensure tally columns once we have items
            EnsureTallyColumns();
            UpdateResultsCount();
            UpdatePageInfo();
        }

        private void EnsureTallyColumns()
        {
            if (_tallyColumnsInitialized)
                return;
            var dataGrid = this.FindControl<DataGrid>("ResultsDataGrid");
            if (dataGrid == null)
                return;

            var first = _allResults.FirstOrDefault();
            if (first?.Scores == null || first.Scores.Length == 0)
            {
                // Try items source if allResults empty
                first = _itemsSource?.FirstOrDefault();
                if (first?.Scores == null || first.Scores.Length == 0)
                    return;
            }

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
                dataGrid.Columns.Add(col);
            }

            _tallyColumnsInitialized = true;
        }

        private void ResetFromItemsSource()
        {
            _allResults.Clear();
            if (_itemsSource != null)
            {
                foreach (var r in _itemsSource)
                {
                    _allResults.Add(r);
                }
            }
            // Rebuild tally columns if needed
            EnsureTallyColumns();
            ApplySorting();
            UpdateDisplay();
        }

        public void AddResults(IEnumerable<SearchResult> results)
        {
            foreach (var result in results)
            {
                _allResults.Add(result);
            }
            EnsureTallyColumns();
            ApplySorting();
            UpdateDisplay();
        }

        public void AddResult(SearchResult result)
        {
            _allResults.Add(result);
            EnsureTallyColumns();
            ApplySorting();
            UpdateDisplay();
        }

        /// <summary>
        /// Clear all results
        /// </summary>
        public void ClearResults()
        {
            _allResults.Clear();
            _displayedResults.Clear();
            _currentPage = 1;
            UpdateResultsCount();
            UpdatePageInfo();
            UpdateStats();
        }

        /// <summary>
        /// Get all results (for export)
        /// </summary>
        public IEnumerable<SearchResult> GetAllResults()
        {
            return _allResults.ToList();
        }

        /// <summary>
        /// Get currently displayed results
        /// </summary>
        public IEnumerable<SearchResult> GetDisplayedResults()
        {
            return _displayedResults.ToList();
        }

        private void ApplySorting()
        {
            var sortedResults = _currentSortColumn switch
            {
                "Seed" => _sortDescending
                    ? _allResults.OrderByDescending(r => r.Seed)
                    : _allResults.OrderBy(r => r.Seed),
                "TotalScore" => _sortDescending
                    ? _allResults.OrderByDescending(r => r.TotalScore)
                    : _allResults.OrderBy(r => r.TotalScore),
                _ => _allResults.OrderByDescending(r => r.TotalScore),
            };

            // Update the all results collection
            _allResults.Clear();
            foreach (var result in sortedResults)
            {
                _allResults.Add(result);
            }
        }

        private void UpdateDisplay()
        {
            // Calculate pagination
            _totalPages = Math.Max(1, (int)Math.Ceiling((double)_allResults.Count / _itemsPerPage));
            _currentPage = Math.Min(_currentPage, _totalPages);

            // Get current page items
            var startIndex = (_currentPage - 1) * _itemsPerPage;
            var pageItems = _allResults.Skip(startIndex).Take(_itemsPerPage);

            // Update displayed results
            _displayedResults.Clear();
            foreach (var item in pageItems)
            {
                _displayedResults.Add(item);
            }

            UpdateResultsCount();
            UpdatePageInfo();
            UpdateStats();
            UpdatePaginationButtons();
        }

        private void UpdateResultsCount()
        {
            var resultsCountText = this.FindControl<TextBlock>("ResultsCountText")!;

            if (_allResults.Count == 0)
            {
                resultsCountText.Text = "No results";
            }
            else if (_allResults.Count == 1)
            {
                resultsCountText.Text = "1 result";
            }
            else
            {
                resultsCountText.Text = $"{_allResults.Count:N0} results";
            }
        }

        private void UpdatePageInfo()
        {
            // Support either legacy TextBlock or new badge-style Button
            var pageBadge = this.FindControl<Button>("PageInfoBadge");
            if (pageBadge is not null)
            {
                pageBadge.Content = $"Page {_currentPage} of {_totalPages}";
            }

            var pageInfoText = this.FindControl<TextBlock>("PageInfoText");
            if (pageInfoText is not null)
            {
                pageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
            }
        }

        private void UpdateStats()
        {
            var statsText = this.FindControl<TextBlock>("StatsText")!;

            if (_allResults.Count == 0)
            {
                statsText.Text = "Ready to search";
                return;
            }

            var highestScore = _allResults.Max(r => r.TotalScore);
            var averageScore = _allResults.Average(r => r.TotalScore);

            statsText.Text =
                $"Best: {highestScore} • Avg: {averageScore:F1} • Count: {_allResults.Count}";
        }

        private void UpdatePaginationButtons()
        {
            var previousButton = this.FindControl<Button>("PreviousButton")!;
            var nextButton = this.FindControl<Button>("NextButton")!;

            previousButton.IsEnabled = _currentPage > 1;
            nextButton.IsEnabled = _currentPage < _totalPages;
        }

        // Event handlers
        private void SortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem)
            {
                var sortTag = selectedItem.Tag?.ToString() ?? "";

                if (sortTag.EndsWith("_Desc"))
                {
                    _currentSortColumn = sortTag.Replace("_Desc", "");
                    _sortDescending = true;
                }
                else if (sortTag.EndsWith("_Asc"))
                {
                    _currentSortColumn = sortTag.Replace("_Asc", "");
                    _sortDescending = false;
                }
                else
                {
                    _currentSortColumn = sortTag;
                    _sortDescending = false;
                }

                ApplySorting();
                UpdateDisplay();

                DebugLogger.Log(
                    "SortableResultsGrid",
                    $"Sorted by {_currentSortColumn} ({(_sortDescending ? "desc" : "asc")})"
                );
            }
        }

        private void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            ExportAllRequested?.Invoke(this, _allResults);
        }

        private void ResultsDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is SearchResult selectedResult)
            {
                // Double-click to copy seed
                CopySeed(selectedResult.Seed);
            }
        }

        private void PreviousButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateDisplay();
            }
        }

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdateDisplay();
            }
        }

        // Public methods for commands (called from templates)
        public async void CopySeed(string seed)
        {
            try
            {
                await ClipboardService.CopyToClipboardAsync(seed);
                DebugLogger.Log("SortableResultsGrid", $"Copied seed to clipboard: {seed}");

                // Find the result and fire event
                var result = _allResults.FirstOrDefault(r => r.Seed == seed);
                if (result != null)
                {
                    SeedCopied?.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SortableResultsGrid", $"Failed to copy seed: {ex.Message}");
            }
        }

        public void SearchSimilar(SearchResult result)
        {
            SearchSimilarRequested?.Invoke(this, result);
            DebugLogger.Log(
                "SortableResultsGrid",
                $"Search similar requested for seed: {result.Seed}"
            );
        }

        public void AddToFavorites(SearchResult result)
        {
            AddToFavoritesRequested?.Invoke(this, result);
            DebugLogger.Log(
                "SortableResultsGrid",
                $"Add to favorites requested for seed: {result.Seed}"
            );
        }

        public void Analyze(SearchResult result)
        {
            AnalyzeRequested?.Invoke(this, result);
            DebugLogger.Log("SortableResultsGrid", $"Analyze requested for seed: {result.Seed}");
        }

        public void ExportSeed(SearchResult result)
        {
            ExportSeedRequested?.Invoke(this, result);
            DebugLogger.Log("SortableResultsGrid", $"Export seed requested for: {result.Seed}");
        }

        /// <summary>
        /// Set items per page
        /// </summary>
        public void SetItemsPerPage(int itemsPerPage)
        {
            _itemsPerPage = Math.Max(10, Math.Min(1000, itemsPerPage));
            _currentPage = 1; // Reset to first page
            UpdateDisplay();
        }

        /// <summary>
        /// Go to specific page
        /// </summary>
        public void GoToPage(int page)
        {
            _currentPage = Math.Max(1, Math.Min(page, _totalPages));
            UpdateDisplay();
        }

        /// <summary>
        /// Filter results by minimum score
        /// </summary>
        public void FilterByMinScore(int minScore)
        {
            var filteredResults = _allResults.Where(r => r.TotalScore >= minScore).ToList();

            _displayedResults.Clear();
            foreach (var result in filteredResults.Take(_itemsPerPage))
            {
                _displayedResults.Add(result);
            }

            var resultsCountText = this.FindControl<TextBlock>("ResultsCountText")!;
            resultsCountText.Text =
                $"{filteredResults.Count} of {_allResults.Count} results (score ≥ {minScore})";
        }

        /// <summary>
        /// Clear any active filters
        /// </summary>
        public void ClearFilters()
        {
            UpdateDisplay(); // This will show all results again
        }

        /// <summary>
        /// Get current sort information
        /// </summary>
        public (string column, bool descending) GetCurrentSort()
        {
            return (_currentSortColumn, _sortDescending);
        }

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
                                _allResults.Add(r);
                            }
                        }
                        EnsureTallyColumns();
                        ApplySorting();
                        UpdateDisplay();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is SearchResult r)
                            {
                                var existing = _allResults.FirstOrDefault(x =>
                                    x.Seed == r.Seed && x.TotalScore == r.TotalScore
                                );
                                if (existing != null)
                                {
                                    _allResults.Remove(existing);
                                }
                            }
                        }
                        UpdateDisplay();
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
