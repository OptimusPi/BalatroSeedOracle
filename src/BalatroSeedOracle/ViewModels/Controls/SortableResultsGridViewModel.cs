using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.Controls
{
    /// <summary>
    /// Simple, debounced results grid - collects results in background, updates UI periodically
    /// </summary>
    public partial class SortableResultsGridViewModel : ObservableObject
    {
        public event EventHandler<string>? CopyToClipboardRequested;
        private const int MaxDisplayResults = 1000;
        private const int DebounceDelayMs = 300;

        // Background storage (thread-safe)
        private readonly List<SearchResult> _backingResults = new();
        private readonly object _lock = new();

        // Debounce timer
        private CancellationTokenSource? _debounceTokenSource;
        private Task? _debounceTask;

        // UI-bound collection - ONLY updated via debounced refresh
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
                if (result != null)
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
        /// Add a single result - triggers debounced UI refresh
        /// </summary>
        public void AddResult(SearchResult result)
        {
            lock (_lock)
            {
                _backingResults.Add(result);
                TotalResultCount = _backingResults.Count;
            }

            // Debounce the UI update
            ScheduleDebouncedRefresh();
        }

        /// <summary>
        /// Add multiple results at once - triggers single debounced UI refresh
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
            // For single additions (like real-time search), use debounced refresh
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
                ScheduleDebouncedRefresh();
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
        /// Force immediate refresh (bypass debounce)
        /// </summary>
        public void ForceRefresh()
        {
            _debounceTokenSource?.Cancel();
            RefreshDisplayedResults();
        }

        private void ScheduleDebouncedRefresh()
        {
            // Cancel previous debounce
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            // Track debounce task properly - no fire-and-forget!
            _debounceTask = DebouncedRefreshAsync(token);
        }

        private async Task DebouncedRefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(DebounceDelayMs, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    RefreshDisplayedResults();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debounce is reset
            }
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
                            r.Scores != null
                            && r.Scores.Any(s => s.ToString().Contains(filterLower))
                        )
                        || (
                            r.Labels != null
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
                                (r.Scores != null && scoreIndex < r.Scores.Length)
                                    ? r.Scores[scoreIndex]
                                    : 0
                            )
                            : filtered.OrderBy(r =>
                                (r.Scores != null && scoreIndex < r.Scores.Length)
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
                DisplayedResults.Clear();
                foreach (var result in top1000)
                {
                    DisplayedResults.Add(result);
                }

                UpdateStatsText();

                DebugLogger.Log(
                    "SortableResultsGridVM",
                    $"Refreshed: {TotalResultCount} total, showing {DisplayedResults.Count}"
                );
            });
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

        private Task CopySeedAsync(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return Task.CompletedTask;

            try
            {
                CopyToClipboardRequested?.Invoke(this, seed);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SortableResultsGridVM", $"Copy failed: {ex.Message}");
            }
            return Task.CompletedTask;
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
