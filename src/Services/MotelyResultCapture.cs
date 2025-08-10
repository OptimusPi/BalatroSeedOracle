using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Motely.Filters;
using Oracle.Helpers;
using Oracle.Models;

namespace Oracle.Services
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

            _captureTask = Task.Run(
                async () =>
                {
                    try
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            if (
                                OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(
                                    out var result
                                )
                            )
                            {
                                var searchResult = new SearchResult
                                {
                                    Seed = result.Seed,
                                    TotalScore = result.TotalScore,
                                    ScoreBreakdown = SerializeScoreBreakdown(result.ScoreWants),
                                    ScoreLabels = ExtractScoreLabels(),
                                    TallyScores = result.ScoreWants,
                                    ItemLabels = ExtractScoreLabels(),
                                };
                                
                                DebugLogger.Log(
                                    "MotelyResultCapture",
                                    $"Result for {result.Seed}: TallyScores={result.ScoreWants?.Length ?? 0}, Labels={searchResult.ItemLabels?.Length ?? 0}"
                                );
                                if (searchResult.ItemLabels != null && searchResult.ItemLabels.Length > 0)
                                {
                                    DebugLogger.Log(
                                        "MotelyResultCapture",
                                        $"Labels: {string.Join(", ", searchResult.ItemLabels)}"
                                    );
                                }
                                if (searchResult.TallyScores != null && searchResult.TallyScores.Length > 0)
                                {
                                    DebugLogger.Log(
                                        "MotelyResultCapture",
                                        $"Scores: {string.Join(", ", searchResult.TallyScores)}"
                                    );
                                }

                                // Store in DuckDB
                                await _searchHistory.AddSearchResultAsync(searchResult);

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
                                // No results available, wait a bit
                                await Task.Delay(500, _cts.Token);
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
                },
                _cts.Token
            );

            await Task.CompletedTask;
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
                await _captureTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            _cts.Dispose();
            _cts = null;
            _captureTask = null;
        }

        /// <summary>
        /// Serialize score breakdown as JSON
        /// </summary>
        private string SerializeScoreBreakdown(int[]? scores)
        {
            if (scores == null || scores.Length == 0)
                return "[]";

            return JsonSerializer.Serialize(scores);
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
