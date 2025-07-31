using System;
using System.Threading;
using System.Threading.Tasks;
using Motely.Filters;
using Oracle.Models;
using Oracle.Helpers;
using System.Text.Json;

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
        private long _currentSearchId = -1;
        private int _resultCount = 0;
        
        public event Action<SearchResult>? ResultCaptured;
        public event Action<int>? ResultCountChanged;
        
        public int CapturedCount => _resultCount;
        public bool IsCapturing => _captureTask != null && !_captureTask.IsCompleted;
        
        public MotelyResultCapture(SearchHistoryService searchHistory)
        {
            _searchHistory = searchHistory ?? throw new ArgumentNullException(nameof(searchHistory));
        }
        
        /// <summary>
        /// Start capturing results from Motely's queue
        /// </summary>
        public async Task StartCaptureAsync(long searchId)
        {
            if (_captureTask != null && !_captureTask.IsCompleted)
            {
                throw new InvalidOperationException("Capture is already running");
            }
            
            _currentSearchId = searchId;
            _resultCount = 0;
            _cts = new CancellationTokenSource();
            
            DebugLogger.Log("MotelyResultCapture", $"Starting capture for search {searchId}");
            
            _captureTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out var result))
                        {
                            var searchResult = new SearchResult
                            {
                                Seed = result.Seed,
                                Score = result.TotalScore,
                                Details = GenerateDetails(result),
                                Ante = ExtractAnteFromResult(result), 
                                ScoreBreakdown = SerializeScoreBreakdown(result.ScoreWants)
                            };
                            
                            // Store in DuckDB
                            await _searchHistory.AddSearchResultAsync(searchId, searchResult);
                            
                            // Update count and raise events
                            Interlocked.Increment(ref _resultCount);
                            ResultCaptured?.Invoke(searchResult);
                            ResultCountChanged?.Invoke(_resultCount);
                            
                            DebugLogger.Log("MotelyResultCapture", $"Captured result: {result.Seed} (Score: {result.TotalScore})");
                        }
                        else
                        {
                            // No results available, wait a bit
                            await Task.Delay(10, _cts.Token);
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
            }, _cts.Token);
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Stop capturing results
        /// </summary>
        public async Task StopCaptureAsync()
        {
            if (_cts == null || _captureTask == null)
                return;
                
            DebugLogger.Log("MotelyResultCapture", $"Stopping capture for search {_currentSearchId}. Captured {_resultCount} results.");
            
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
        /// Generate human-readable details from the result
        /// </summary>
        private string GenerateDetails(OuijaResult result)
        {
            if (!result.Success)
                return "Failed";
                
            // TODO: Generate more detailed information based on the filter configuration
            // For now, just show the score breakdown
            if (result.ScoreWants != null && result.ScoreWants.Length > 0)
            {
                return $"Scores: {string.Join(", ", result.ScoreWants)}";
            }
            
            return "";
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
        /// Extract ante information from result if available
        /// </summary>
        private int ExtractAnteFromResult(OuijaResult result)
        {
            // TODO: When OuijaResult includes ante info, extract it here
            // For now, default to 1
            return 1;
        }
        
        public void Dispose()
        {
            StopCaptureAsync().GetAwaiter().GetResult();
        }
    }
}