using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.Controls
{
    /// <summary>
    /// The results grid's data: results collect in a buffer, and the rows on show are rebuilt from
    /// it at most once per <see cref="RefreshIntervalMs"/> while results keep arriving.
    /// </summary>
    public partial class SortableResultsGridViewModel : ObservableObject
    {
        public event EventHandler<string>? CopyToClipboardRequested;
        private const int MaxDisplayResults = 1000;
        private const int RefreshIntervalMs = 300;

        // Background storage (thread-safe)
        private readonly List<SearchResult> _backingResults = new();
        private readonly object _lock = new();

        // The one refresh that is waiting to run, if any. Results that arrive while it waits ride
        // along with it, so a search that never pauses still shows its seeds.
        private CancellationTokenSource? _pendingRefresh;
        private Task? _refreshTask;

        // UI-bound collection - ONLY updated by a refresh
        [ObservableProperty]
        private ObservableCollection<SearchResult> _displayedResults = new();

        // Stats
        [ObservableProperty]
        private int _totalResultCount;

        [ObservableProperty]
        private string _resultsCountText = "0 results";

        [ObservableProperty]
        private string _statsText = "Ready to search";

        // Sorting
        [ObservableProperty]
        private int _selectedSortIndex = 1; // Default: Score descending

        [ObservableProperty]
        private string _currentSortProperty = "TotalScore";

        [ObservableProperty]
        private bool _sortDescending = true;

        // Filtering
        [ObservableProperty]
        private string _quickFilterText = "";

        [ObservableProperty]
        private bool _hasFilter = false;

        // Selection
        [ObservableProperty]
        private SearchResult? _selectedItem;

        [ObservableProperty]
        private ObservableCollection<SearchResult> _selectedItems = new();

        [ObservableProperty]
        private bool _hasSelection = false;

        [ObservableProperty]
        private string _selectedCountText = "";

        // Column visibility
        [ObservableProperty]
        private Dictionary<string, bool> _columnVisibility = new();

        // TreeDataGrid source - MVVM binding for the grid
        private FlatTreeDataGridSource<SearchResult>? _gridSource;
        private string[]? _currentLabels;

        public FlatTreeDataGridSource<SearchResult>? GridSource
        {
            get => _gridSource;
            private set => SetProperty(ref _gridSource, value);
        }

        /// <summary>The tally column labels of the results on show, in column order.</summary>
        public IReadOnlyList<string> TallyLabels => _currentLabels ?? [];

        // Commands
        public IAsyncRelayCommand<string> CopySeedCommand { get; }
        public IRelayCommand ExportAllCommand { get; }
        public IRelayCommand PopOutCommand { get; }
        public IRelayCommand<SearchResult> AnalyzeCommand { get; }
        public IRelayCommand CopySelectedCommand { get; }
        public IRelayCommand ExportSelectedCommand { get; }
        public IRelayCommand ClearFilterCommand { get; }
        public IRelayCommand ToggleColumnMenuCommand { get; }

        // Read-only view of all results (for tally column initialization)
        public IReadOnlyList<SearchResult> AllResults
        {
            get
            {
                lock (_lock)
                {
                    return _backingResults.ToList().AsReadOnly();
                }
            }
        }

        // Events (some are subscribed externally via code-behind)
        public event EventHandler? PopOutRequested;
        public event EventHandler<IEnumerable<SearchResult>>? ExportAllRequested;
#pragma warning disable CS0067 // Events are subscribed externally through code-behind
        public event EventHandler<SearchResult>? SeedCopied;
        public event EventHandler<SearchResult>? SearchSimilarRequested;
        public event EventHandler<SearchResult>? AddToFavoritesRequested;
        public event EventHandler<SearchResult>? ExportSeedRequested;
#pragma warning restore CS0067
        public event EventHandler<SearchResult>? AnalyzeRequested;

        public SortableResultsGridViewModel()
        {
            CopySeedCommand = new AsyncRelayCommand<string>(CopySeedAsync);
            ExportAllCommand = new RelayCommand(ExportAll);
            PopOutCommand = new RelayCommand(() => PopOutRequested?.Invoke(this, EventArgs.Empty));
            AnalyzeCommand = new RelayCommand<SearchResult>(result =>
            {
                if (result is not null)
                    AnalyzeRequested?.Invoke(this, result);
            });
            CopySelectedCommand = new RelayCommand(CopySelected, () => HasSelection);
            ExportSelectedCommand = new RelayCommand(ExportSelected, () => HasSelection);
            ClearFilterCommand = new RelayCommand(() => QuickFilterText = "", () => HasFilter);
            ToggleColumnMenuCommand = new RelayCommand(ToggleColumnMenu);

            // Initialize column visibility (all visible by default)
            ColumnVisibility["Seed"] = true;
            ColumnVisibility["Score"] = true;
            ColumnVisibility["Actions"] = true;

            // Listen to selection changes
            SelectedItems.CollectionChanged += (s, e) => UpdateSelectionStats();
        }

        /// <summary>
        /// Add a single result. It shows on the next refresh.
        /// </summary>
        public void AddResult(SearchResult result)
        {
            lock (_lock)
            {
                _backingResults.Add(result);
                TotalResultCount = _backingResults.Count;
            }

            ScheduleRefresh();
        }

        /// <summary>
        /// Drop a single result, as when the live window slides past its cap. It goes on the next
        /// refresh; nothing is reloaded.
        /// </summary>
        public void RemoveResult(SearchResult result)
        {
            lock (_lock)
            {
                _backingResults.Remove(result);
                TotalResultCount = _backingResults.Count;
            }

            ScheduleRefresh();
        }

        /// <summary>
        /// Add multiple results at once. A bulk load shows at once, a handful on the next refresh.
        /// </summary>
        public void AddResults(IEnumerable<SearchResult> results)
        {
            var resultsArray = results.ToArray();

            lock (_lock)
            {
                _backingResults.AddRange(resultsArray);
                TotalResultCount = _backingResults.Count;
            }

            DebugLogger.Log(
                "SortableResultsGridVM",
                $"AddResults: Added {resultsArray.Length} items, total now {TotalResultCount}"
            );

            // For bulk additions (like loading saved results), use immediate refresh
            // For single additions (like real-time search), use the scheduled refresh
            if (resultsArray.Length > 10)
            {
                DebugLogger.Log(
                    "SortableResultsGridVM",
                    "Bulk addition detected - forcing immediate refresh"
                );
                ForceRefresh();
            }
            else
            {
                ScheduleRefresh();
            }
        }

        /// <summary>
        /// Clear all results
        /// </summary>
        public void ClearResults()
        {
            lock (_lock)
            {
                _backingResults.Clear();
                TotalResultCount = 0;
            }

            // Immediate clear on UI
            Dispatcher.UIThread.Post(() =>
            {
                DisplayedResults.Clear();
                UpdateStatsText();
            });
        }

        /// <summary>
        /// Refresh now, in place of any refresh that is waiting.
        /// </summary>
        public void ForceRefresh()
        {
            lock (_lock)
            {
                _pendingRefresh?.Cancel();
                _pendingRefresh = null;
            }

            RefreshDisplayedResults();
        }

        /// <summary>
        /// Schedule a refresh one interval from now, unless one is already waiting. The wait is
        /// not restarted by each result: restarting it meant a search that found seeds faster
        /// than the interval never showed any until it stopped.
        /// </summary>
        private void ScheduleRefresh()
        {
            CancellationTokenSource mine;
            lock (_lock)
            {
                if (_pendingRefresh is not null)
                    return;
                mine = _pendingRefresh = new CancellationTokenSource();
            }

            _refreshTask = RefreshAfterIntervalAsync(mine);
        }

        private async Task RefreshAfterIntervalAsync(CancellationTokenSource mine)
        {
            try
            {
                await Task.Delay(RefreshIntervalMs, mine.Token);
            }
            catch (TaskCanceledException)
            {
                // ForceRefresh ran in its place.
                mine.Dispose();
                return;
            }

            bool stillMine;
            lock (_lock)
            {
                stillMine = ReferenceEquals(_pendingRefresh, mine);
                if (stillMine)
                    _pendingRefresh = null;
            }

            mine.Dispose();
            if (stillMine)
                RefreshDisplayedResults();
        }

        private void RefreshDisplayedResults()
        {
            List<SearchResult> top1000;

            lock (_lock)
            {
                IEnumerable<SearchResult> filtered = _backingResults;

                // Apply quick filter if set
                if (!string.IsNullOrWhiteSpace(QuickFilterText))
                {
                    var filterLower = QuickFilterText.ToLowerInvariant();
                    filtered = filtered.Where(r =>
                        r.Seed.ToLowerInvariant().Contains(filterLower)
                        || r.TotalScore.ToString().Contains(filterLower)
                        || (
                            r.Scores is not null
                            && r.Scores.Any(s => s.ToString().Contains(filterLower))
                        )
                        || (
                            r.Labels is not null
                            && r.Labels.Any(l => l.ToLowerInvariant().Contains(filterLower))
                        )
                    );
                }

                IOrderedEnumerable<SearchResult> query;

                // Handle sorting based on CurrentSortProperty
                if (CurrentSortProperty == "Seed")
                {
                    query = SortDescending
                        ? filtered.OrderByDescending(r => r.Seed)
                        : filtered.OrderBy(r => r.Seed);
                }
                else if (CurrentSortProperty == "TotalScore")
                {
                    query = SortDescending
                        ? filtered.OrderByDescending(r => r.TotalScore)
                        : filtered.OrderBy(r => r.TotalScore);
                }
                else if (CurrentSortProperty.StartsWith("Scores["))
                {
                    // Extract index from "Scores[N]"
                    try
                    {
                        int startIndex = CurrentSortProperty.IndexOf('[') + 1;
                        int endIndex = CurrentSortProperty.IndexOf(']');
                        int scoreIndex = int.Parse(
                            CurrentSortProperty.Substring(startIndex, endIndex - startIndex)
                        );

                        query = SortDescending
                            ? filtered.OrderByDescending(r =>
                                (r.Scores is not null && scoreIndex < r.Scores.Length)
                                    ? r.Scores[scoreIndex]
                                    : 0
                            )
                            : filtered.OrderBy(r =>
                                (r.Scores is not null && scoreIndex < r.Scores.Length)
                                    ? r.Scores[scoreIndex]
                                    : 0
                            );
                    }
                    catch
                    {
                        // Fallback to TotalScore
                        query = SortDescending
                            ? filtered.OrderByDescending(r => r.TotalScore)
                            : filtered.OrderBy(r => r.TotalScore);
                    }
                }
                else
                {
                    // Default fallback
                    query = SortDescending
                        ? filtered.OrderByDescending(r => r.TotalScore)
                        : filtered.OrderBy(r => r.TotalScore);
                }

                top1000 = query.Take(MaxDisplayResults).ToList();
            }

            // Update UI on dispatcher thread
            Dispatcher.UIThread.Post(() =>
            {
                // Sorted by score, a long search mostly finds seeds that miss the top rows. The rows
                // are rebuilt only when they change, so the grid keeps its scroll place and selection.
                if (!DisplayedResults.SequenceEqual(top1000))
                {
                    DisplayedResults.Clear();
                    foreach (var result in top1000)
                    {
                        DisplayedResults.Add(result);
                    }

                    DebugLogger.Log(
                        "SortableResultsGridVM",
                        $"Refreshed: {TotalResultCount} total, showing {DisplayedResults.Count}"
                    );
                }

                // Rebuild grid source if labels changed
                RebuildGridSourceIfNeeded();

                UpdateStatsText();
            });
        }

        /// <summary>
        /// Rebuild TreeDataGrid source when labels change (dynamic tally columns).
        /// </summary>
        private void RebuildGridSourceIfNeeded()
        {
            // Get labels from first result
            var firstResult = DisplayedResults.FirstOrDefault();
            var newLabels = firstResult?.Labels;

            // Check if labels changed
            bool labelsChanged = !LabelsEqual(_currentLabels, newLabels);

            if (labelsChanged || GridSource is null)
            {
                _currentLabels = newLabels;
                RebuildGridSource();
            }
            // Note: FlatTreeDataGridSource wraps DisplayedResults which is ObservableCollection,
            // so it automatically updates when the collection changes.
        }

        private static bool LabelsEqual(string[]? a, string[]? b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        private void RebuildGridSource()
        {
            var source = new FlatTreeDataGridSource<SearchResult>(DisplayedResults);

            source.WithTextColumn<SearchResult, string>(
                "SEED",
                x => x.Seed,
                o => o.Width = new GridLength(120));

            source.WithTextColumn<SearchResult, int>(
                "SCORE",
                x => x.TotalScore,
                o => o.Width = new GridLength(80));

            if (_currentLabels is not null)
            {
                for (int i = 0; i < _currentLabels.Length; i++)
                {
                    var index = i;
                    var label = _currentLabels[i];

                    // The column reads this expression as a binding path, so it must be a
                    // plain member/index chain: a conditional or a method call throws
                    // ExpressionParseException. Typed int, the column also sorts as numbers.
                    source.WithTextColumn<SearchResult, int>(
                        label.ToUpperInvariant(),
                        x => x.Scores![index],
                        o => o.Width = new GridLength(70));
                }
            }

            GridSource = source;
            OnPropertyChanged(nameof(TallyLabels));

            DebugLogger.Log(
                "SortableResultsGridVM",
                $"Rebuilt grid source with {source.Columns.Count} columns ({_currentLabels?.Length ?? 0} tally columns)"
            );
        }

        partial void OnSelectedSortIndexChanged(int value)
        {
            // Update sort properties based on legacy index
            switch (value)
            {
                case 0: // SEED (A-Z)
                    CurrentSortProperty = "Seed";
                    SortDescending = false;
                    break;
                case 1: // SCORE (DESC)
                    CurrentSortProperty = "TotalScore";
                    SortDescending = true;
                    break;
                case 2: // SCORE (ASC)
                    CurrentSortProperty = "TotalScore";
                    SortDescending = false;
                    break;
            }

            // Refresh results
            RefreshDisplayedResults();
        }

        partial void OnCurrentSortPropertyChanged(string value) => RefreshDisplayedResults();

        partial void OnSortDescendingChanged(bool value) => RefreshDisplayedResults();

        partial void OnQuickFilterTextChanged(string value)
        {
            HasFilter = !string.IsNullOrWhiteSpace(value);
            RefreshDisplayedResults();
        }

        private void UpdateSelectionStats()
        {
            HasSelection = SelectedItems.Count > 0;
            SelectedCountText = HasSelection ? $"({SelectedItems.Count} selected)" : "";
            CopySelectedCommand.NotifyCanExecuteChanged();
            ExportSelectedCommand.NotifyCanExecuteChanged();
        }

        private void CopySelected()
        {
            if (SelectedItems.Count == 0)
                return;

            var seeds = string.Join("\n", SelectedItems.Select(r => r.Seed));
            CopyToClipboardRequested?.Invoke(this, seeds);
        }

        private void ExportSelected()
        {
            if (SelectedItems.Count > 0)
            {
                ExportAllRequested?.Invoke(this, SelectedItems);
            }
        }

        private void ToggleColumnMenu()
        {
            // Column visibility menu - implemented via button in UI
            // Users can reorder/resize columns directly in DataGrid
            // Full column visibility toggle can be added later if needed
            DebugLogger.Log(
                "SortableResultsGridVM",
                "Column menu toggle - column reordering/resizing available in DataGrid"
            );
        }

        private void UpdateStatsText()
        {
            ResultsCountText = TotalResultCount switch
            {
                0 => "No results",
                1 => "1 result",
                _ => $"{TotalResultCount:N0} results",
            };

            if (DisplayedResults.Count == 0)
            {
                StatsText = "Ready to search";
                return;
            }

            var highestScore = DisplayedResults.Max(r => r.TotalScore);
            var averageScore = DisplayedResults.Average(r => r.TotalScore);
            var showingText =
                TotalResultCount > MaxDisplayResults ? $" (showing top {MaxDisplayResults})" : "";

            StatsText =
                $"Best: {highestScore} • Avg: {averageScore:F1} • Count: {TotalResultCount}{showingText}";
        }

        private async Task CopySeedAsync(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                await Task.CompletedTask;
                return;
            }

            try
            {
                CopyToClipboardRequested?.Invoke(this, seed);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SortableResultsGridVM", $"Copy failed: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private void ExportAll()
        {
            List<SearchResult> allResults;
            lock (_lock)
            {
                allResults = _backingResults.ToList();
            }
            ExportAllRequested?.Invoke(this, allResults);
        }

        public IEnumerable<SearchResult> GetAllResults()
        {
            lock (_lock)
            {
                return _backingResults.ToList();
            }
        }

        public IEnumerable<SearchResult> GetDisplayedResults()
        {
            return DisplayedResults.ToList();
        }
    }
}
