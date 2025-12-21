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
        private const int MaxDisplayResults = 1000;
        private const int DebounceDelayMs = 300;

        // Background storage (thread-safe)
        private readonly List<SearchResult> _backingResults = new();
        private readonly object _lock = new();

        // Debounce timer
        private CancellationTokenSource? _debounceTokenSource;

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

        // Commands
        public IAsyncRelayCommand<string> CopySeedCommand { get; }
        public IRelayCommand ExportAllCommand { get; }
        public IRelayCommand PopOutCommand { get; }
        public IRelayCommand<SearchResult> AnalyzeCommand { get; }

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

            DebugLogger.Log("SortableResultsGridVM", $"AddResults: Added {resultsArray.Length} items, total now {TotalResultCount}");
            
            // For bulk additions (like loading saved results), use immediate refresh
            // For single additions (like real-time search), use debounced refresh
            if (resultsArray.Length > 10)
            {
                DebugLogger.Log("SortableResultsGridVM", "Bulk addition detected - forcing immediate refresh");
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

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelayMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        RefreshDisplayedResults();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when debounce is reset
                }
            }, token);
        }

        private void RefreshDisplayedResults()
        {
            List<SearchResult> top1000;

            lock (_lock)
            {
                IOrderedEnumerable<SearchResult> query;

                // Handle sorting based on CurrentSortProperty
                if (CurrentSortProperty == "Seed")
                {
                    query = SortDescending 
                        ? _backingResults.OrderByDescending(r => r.Seed) 
                        : _backingResults.OrderBy(r => r.Seed);
                }
                else if (CurrentSortProperty == "TotalScore")
                {
                    query = SortDescending 
                        ? _backingResults.OrderByDescending(r => r.TotalScore) 
                        : _backingResults.OrderBy(r => r.TotalScore);
                }
                else if (CurrentSortProperty.StartsWith("Scores["))
                {
                    // Extract index from "Scores[N]"
                    try
                    {
                        int startIndex = CurrentSortProperty.IndexOf('[') + 1;
                        int endIndex = CurrentSortProperty.IndexOf(']');
                        int scoreIndex = int.Parse(CurrentSortProperty.Substring(startIndex, endIndex - startIndex));

                        query = SortDescending 
                            ? _backingResults.OrderByDescending(r => (r.Scores != null && scoreIndex < r.Scores.Length) ? r.Scores[scoreIndex] : 0) 
                            : _backingResults.OrderBy(r => (r.Scores != null && scoreIndex < r.Scores.Length) ? r.Scores[scoreIndex] : 0);
                    }
                    catch
                    {
                        // Fallback to TotalScore
                        query = SortDescending 
                            ? _backingResults.OrderByDescending(r => r.TotalScore) 
                            : _backingResults.OrderBy(r => r.TotalScore);
                    }
                }
                else
                {
                    // Default fallback
                    query = SortDescending 
                        ? _backingResults.OrderByDescending(r => r.TotalScore) 
                        : _backingResults.OrderBy(r => r.TotalScore);
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

        private void UpdateStatsText()
        {
            ResultsCountText = TotalResultCount switch
            {
                0 => "No results",
                1 => "1 result",
                _ => $"{TotalResultCount:N0} results"
            };

            if (DisplayedResults.Count == 0)
            {
                StatsText = "Ready to search";
                return;
            }

            var highestScore = DisplayedResults.Max(r => r.TotalScore);
            var averageScore = DisplayedResults.Average(r => r.TotalScore);
            var showingText = TotalResultCount > MaxDisplayResults
                ? $" (showing top {MaxDisplayResults})"
                : "";

            StatsText = $"Best: {highestScore} • Avg: {averageScore:F1} • Count: {TotalResultCount}{showingText}";
        }

        private async Task CopySeedAsync(string? seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return;

            try
            {
                await ClipboardService.CopyToClipboardAsync(seed);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SortableResultsGridVM", $"Copy failed: {ex.Message}");
            }
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
