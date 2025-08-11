using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Motely.Filters;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service that captures results directly from Motely's result queue
    /// and stores them in DuckDB in real-time
    /// </summary>
    public class MotelyResultCapture : IDisposable
    {
        private readonly SearchHistoryService _searchHistory;
        private CancellationTokenSource? _cts;
        private Task? _captureTask;
        private int _resultCount = 0;
        private Motely.Filters.OuijaConfig? _filterConfig;
    private bool _headerLabelsSent = false; // only send labels once for header

        public event Action<SearchResult>? ResultCaptured;
        public event Action<int>? ResultCountChanged;

        public int CapturedCount => _resultCount;
        public bool IsCapturing => _captureTask != null && !_captureTask.IsCompleted;

        public MotelyResultCapture(SearchHistoryService searchHistory)
        {
            _searchHistory =
                searchHistory ?? throw new ArgumentNullException(nameof(searchHistory));
        }

        /// <summary>
        /// Set the filter configuration to extract labels
        /// </summary>
        public void SetFilterConfig(Motely.Filters.OuijaConfig config)
        {
            _filterConfig = config;
            DebugLogger.Log(
                "MotelyResultCapture",
                $"Filter config set: {config?.Name ?? "null"}, Should items: {config?.Should?.Count ?? 0}"
            );
        }

        /// <summary>
        /// Start capturing results from Motely's queue
        /// </summary>
        public async Task StartCaptureAsync()
        {
            if (_captureTask != null && !_captureTask.IsCompleted)
            {
                throw new InvalidOperationException("Capture is already running");
            }

            _resultCount = 0;
            _cts = new CancellationTokenSource();

            DebugLogger.Log("MotelyResultCapture", "Starting result capture");

            // Reset header state for a fresh capture session
            _headerLabelsSent = false;

                // Start the dedicated capture loop task on the thread pool to avoid UI context capture
                _captureTask = Task.Run(() => CaptureLoopAsync(_cts.Token));

            await Task.CompletedTask;
        }

        private async Task CaptureLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out var result))
                    {
                        // Only attach labels on the first result so UI can build headers.
                        // Subsequent rows don't need redundant labels.
                        var labels = !_headerLabelsSent ? ExtractScoreLabels() : null;
                        if (labels != null)
                        {
                            _headerLabelsSent = true;
                        }

                        var searchResult = new SearchResult
                        {
                            Seed = result.Seed,
                            TotalScore = result.TotalScore,
                            Scores = result.ScoreWants,
                            Labels = labels,
                        };

                        DebugLogger.Log(
                            "MotelyResultCapture",
                            $"Result for {result.Seed}: Scores={result.ScoreWants?.Length ?? 0}, Labels={searchResult.Labels?.Length ?? 0}"
                        );
                        if (searchResult.Labels != null && searchResult.Labels.Length > 0)
                        {
                            DebugLogger.Log(
                                "MotelyResultCapture",
                                $"Labels: {string.Join(", ", searchResult.Labels)}"
                            );
                        }
                        if (searchResult.Scores != null && searchResult.Scores.Length > 0)
                        {
                            DebugLogger.Log(
                                "MotelyResultCapture",
                                $"Scores: {string.Join(", ", searchResult.Scores)}"
                            );
                        }

                        // Store in DuckDB
                        await _searchHistory.AddSearchResultAsync(searchResult).ConfigureAwait(false);

                        // Update count and raise events
                        Interlocked.Increment(ref _resultCount);
                        ResultCaptured?.Invoke(searchResult);
                        ResultCountChanged?.Invoke(_resultCount);

                        DebugLogger.Log(
                            "MotelyResultCapture",
                            $"Captured result: {result.Seed} (Score: {result.TotalScore})"
                        );
                    }
                    else
                    {
                        // No results available, wait briefly to reduce shutdown latency
                        await Task.Delay(100, token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("MotelyResultCapture", "Capture cancelled");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MotelyResultCapture", $"Capture error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop capturing results
        /// </summary>
    public async Task StopCaptureAsync()
        {
            if (_cts == null || _captureTask == null)
                return;

            DebugLogger.Log(
                "MotelyResultCapture",
                $"Stopping capture. Captured {_resultCount} results."
            );

            _cts.Cancel();

            try
            {
                // Wait up to 2 seconds for the capture task to complete
                var completed = await Task.WhenAny(_captureTask, Task.Delay(2000));
                if (completed != _captureTask)
                {
                    DebugLogger.LogError("MotelyResultCapture", "Capture task did not complete within timeout");
                }
                else
                {
                    // Observe any cancellation exception
                    await _captureTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }

            _cts.Dispose();
            _cts = null;
            _captureTask = null;
        }


        /// <summary>
        /// Extract score labels from the filter configuration
        /// </summary>
        private string[] ExtractScoreLabels()
        {
            if (_filterConfig?.Should == null)
            {
                DebugLogger.Log("MotelyResultCapture", "ExtractScoreLabels: No filter config or Should items");
                return Array.Empty<string>();
            }

            var labels = new List<string>();
            foreach (var item in _filterConfig.Should)
            {
                // Just use the item value as the label
                var label = item.Value ?? "Unknown";
                labels.Add(label);
            }

            DebugLogger.Log("MotelyResultCapture", $"ExtractScoreLabels: Found {labels.Count} labels: {string.Join(", ", labels)}");
            return labels.ToArray();
        }

        public void Dispose()
        {
            StopCaptureAsync().GetAwaiter().GetResult();
        }
    }
}
