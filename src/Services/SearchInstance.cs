using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.Helpers;
using Oracle.Models;
using Oracle.Views.Modals;
using Motely;
using Motely.Filters;
using OuijaConfig = Motely.Filters.OuijaConfig;
using DebugLogger = Oracle.Helpers.DebugLogger;
using SearchResult = Oracle.Models.SearchResult;

namespace Oracle.Services
{
    /// <summary>
    /// Represents a single search instance that can run independently
    /// </summary>
    public class SearchInstance : IDisposable
    {
        private readonly string _searchId;
        private readonly SearchHistoryService _historyService;
        private CancellationTokenSource? _cancellationTokenSource;
        private volatile bool _isRunning;
        private volatile bool _isPaused;
        private OuijaConfig? _currentConfig;
        private IMotelySearch? _currentSearch;
        private MotelyResultCapture? _resultCapture;
        private long _currentSearchDbId = -1;
        private DateTime _searchStartTime;
        private readonly ObservableCollection<Oracle.Models.SearchResult> _results;

        // Properties
        public string SearchId { get => _searchId; }
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public ObservableCollection<Oracle.Models.SearchResult> Results => _results;
        public TimeSpan SearchDuration => DateTime.UtcNow - _searchStartTime;
        public string ConfigPath { get; private set; } = string.Empty;
        public string FilterName { get; private set; } = "Unknown";
        public int ResultCount => _results.Count;

        // Events for UI integration
        public event EventHandler? SearchStarted;
        public event EventHandler? SearchCompleted;
        public event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
        public event EventHandler<SearchResultEventArgs>? ResultFound;
        public event EventHandler<string>? ConsoleOutput;

        public SearchInstance(string searchId, SearchHistoryService historyService)
        {
            _searchId = searchId;
            _historyService = historyService;
            _results = new ObservableCollection<Oracle.Models.SearchResult>();
        }

        /// <summary>
        /// Start searching with a direct config object
        /// </summary>
        public async Task StartSearchWithConfigAsync(OuijaConfig ouijaConfig, SearchConfiguration config, 
            IProgress<SearchProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "StartSearchWithConfigAsync called - NO TEMP FILES!");
                
                _currentConfig = ouijaConfig;
                ConfigPath = "Direct Config";
                FilterName = $"Search {_searchId.Substring(0, 8)}";
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Clear previous results
                _results.Clear();

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

                // Create progress wrapper
                var progressWrapper = new Progress<SearchProgress>(p =>
                {
                    progress?.Report(p);
                    HandleSearchProgress(p);
                });

                // Set up search configuration from the modal
                var searchCriteria = new SearchCriteria
                {
                    ThreadCount = config.ThreadCount,
                    MinScore = config.MinScore,
                    BatchSize = config.BatchSize,
                    Deck = config.Deck ?? "Red",
                    Stake = config.Stake ?? "White"
                };

                // Start a new search in DuckDB
                var deck = config.Deck ?? "Red";
                var stake = config.Stake ?? "White";

                _currentSearchDbId = await _historyService.StartNewSearchAsync(
                    FilterName,
                    config.ThreadCount,
                    config.MinScore,
                    config.BatchSize,
                    deck,
                    stake,
                    39  // maxAnte default value
                );

                if (_currentSearchDbId <= 0)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", "Failed to create new search in database");
                    progress?.Report(new SearchProgress
                    {
                        Message = "Failed to initialize search in database",
                        HasError = true,
                        IsComplete = true
                    });
                    return;
                }

                // Save filter configuration
                await _historyService.SaveFilterItemsAsync(_currentSearchDbId, ouijaConfig);
                
                // Set up result capture
                _resultCapture = new MotelyResultCapture(_historyService);
                _resultCapture.ResultCaptured += (result) =>
                {
                    _results.Add(result);
                    
                    // Notify UI
                    ResultFound?.Invoke(this, new SearchResultEventArgs 
                    { 
                        Result = new Oracle.Views.Modals.SearchResult 
                        { 
                            Seed = result.Seed, 
                            Score = result.TotalScore, 
                            Details = result.ScoreBreakdown 
                        } 
                    });
                    
                    ConsoleOutput?.Invoke(this, $"Found seed: {result.Seed} (Score: {result.TotalScore})");
                };
                
                // Start capturing
                await _resultCapture.StartCaptureAsync(_currentSearchDbId);

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");
                
                await Task.Run(() => RunSearchInProcess(ouijaConfig, searchCriteria, progressWrapper, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search completed");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search was cancelled");
                await CompleteSearch(true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Search failed: {ex.Message}");
                progress?.Report(new SearchProgress
                {
                    Message = $"Search failed: {ex.Message}",
                    HasError = true,
                    IsComplete = true
                });
            }
            finally
            {
                _isRunning = false;
                SearchCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Start searching with a config file path
        /// </summary>
        public async Task StartSearchAsync(SearchCriteria criteria, IProgress<SearchProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", $"Starting search with config: {criteria.ConfigPath}");
                
                ConfigPath = criteria.ConfigPath ?? string.Empty;
                FilterName = System.IO.Path.GetFileNameWithoutExtension(criteria.ConfigPath ?? string.Empty);
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Clear previous results
                _results.Clear();

                // Load and validate the Ouija config
                if (string.IsNullOrEmpty(criteria.ConfigPath))
                {
                    throw new ArgumentException("Config path is required");
                }
                _currentConfig = await Task.Run(() => OuijaConfig.LoadFromJson(criteria.ConfigPath));

                if (_currentConfig == null)
                {
                    throw new InvalidOperationException($"Failed to load config from {criteria.ConfigPath}");
                }

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

                // Create progress wrapper
                var progressWrapper = new Progress<SearchProgress>(p =>
                {
                    progress?.Report(p);
                    HandleSearchProgress(p);
                });

                // Start a new search in DuckDB
                var deck = criteria.Deck ?? "Red";
                var stake = criteria.Stake ?? "White";

                _currentSearchDbId = await _historyService.StartNewSearchAsync(
                    criteria.ConfigPath ?? string.Empty,
                    criteria.ThreadCount,
                    criteria.MinScore,
                    criteria.BatchSize,
                    deck,
                    stake,
                    39  // maxAnte default value
                );

                if (_currentSearchDbId <= 0)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", "Failed to create new search in database");
                    progress?.Report(new SearchProgress
                    {
                        Message = "Failed to initialize search in database",
                        HasError = true,
                        IsComplete = true
                    });
                    return;
                }

                // Save filter configuration
                if (_currentConfig != null)
                {
                    await _historyService.SaveFilterItemsAsync(_currentSearchDbId, _currentConfig);
                }
                
                // Set up result capture
                _resultCapture = new MotelyResultCapture(_historyService);
                _resultCapture.ResultCaptured += (result) =>
                {
                    _results.Add(result);
                    
                    // Notify UI
                    ResultFound?.Invoke(this, new SearchResultEventArgs 
                    { 
                        Result = new Oracle.Views.Modals.SearchResult 
                        { 
                            Seed = result.Seed, 
                            Score = result.TotalScore, 
                            Details = result.ScoreBreakdown 
                        } 
                    });
                    
                    ConsoleOutput?.Invoke(this, $"Found seed: {result.Seed} (Score: {result.TotalScore})");
                };
                
                // Start capturing
                await _resultCapture.StartCaptureAsync(_currentSearchDbId);

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");
                
                if (_currentConfig != null)
                {
                    await Task.Run(() => RunSearchInProcess(_currentConfig, criteria, progressWrapper, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                }

                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search completed");
                await CompleteSearch(false);
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search was cancelled");
                await CompleteSearch(true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Search failed: {ex.Message}");
                progress?.Report(new SearchProgress
                {
                    Message = $"Search failed: {ex.Message}",
                    HasError = true,
                    IsComplete = true
                });
            }
            finally
            {
                _isRunning = false;
                SearchCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        public void PauseSearch()
        {
            if (_isRunning && !_isPaused)
            {
                _isPaused = true;
                if (_currentSearch != null && _currentSearch.Status == MotelySearchStatus.Running)
                {
                    _currentSearch.Pause();
                }
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search paused");
            }
        }

        public void ResumeSearch()
        {
            if (_isRunning && _isPaused)
            {
                _isPaused = false;
                // Motely doesn't have a Resume method - search has to be restarted
                // For now, just update the paused flag
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Resume not supported - search needs to be restarted");
            }
        }

        public void StopSearch()
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Stopping search...");
                _cancellationTokenSource?.Cancel();
            }
        }

        private void HandleSearchProgress(SearchProgress progress)
        {
            // Update UI with progress
            var eventArgs = new SearchProgressEventArgs
            {
                Message = progress.Message ?? string.Empty,
                PercentComplete = (int)progress.PercentComplete,
                SeedsSearched = (int)progress.SeedsSearched,
                ResultsFound = progress.ResultsFound,
                IsComplete = progress.IsComplete,
                HasError = progress.HasError
            };

            ProgressUpdated?.Invoke(this, eventArgs);

            // Output to console if available
            if (!string.IsNullOrEmpty(progress.Message))
            {
                ConsoleOutput?.Invoke(this, progress.Message);
            }
        }


        private async Task CompleteSearch(bool wasCancelled)
        {
            if (_currentSearchDbId > 0)
            {
                var duration = SearchDuration.TotalSeconds;
                var totalSeeds = 0; // Will be calculated from batch count
                
                await _historyService.CompleteSearchAsync(
                    _currentSearchDbId, 
                    totalSeeds, 
                    duration, 
                    wasCancelled
                );
            }
        }

        private async Task RunSearchInProcess(OuijaConfig config, SearchCriteria criteria, IProgress<SearchProgress>? progress, CancellationToken cancellationToken)
        {
            try
            {
                // Reset cancellation flag
                OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;

                // Create filter descriptor
                var filterDesc = new OuijaJsonFilterDesc(config);
                filterDesc.Cutoff = criteria.MinScore;

                // Create search settings - following Motely Program.cs pattern
                var batchSize = criteria.BatchSize;
                var searchSettings = new MotelySearchSettings<OuijaJsonFilterDesc.OuijaJsonFilter>(filterDesc)
                    .WithThreadCount(criteria.ThreadCount)
                    .WithBatchCharacterCount(batchSize)
                    .WithStartBatchIndex(criteria.StartBatch)
                    .WithSequentialSearch();
                
                // Calculate total batches if end batch is specified
                long? totalBatches = null;
                if (criteria.EndBatch > 0)
                {
                    totalBatches = criteria.EndBatch - criteria.StartBatch;
                }

                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Starting search with {criteria.ThreadCount} threads, batch size {batchSize}");

                // Start the search
                _currentSearch = searchSettings.Start();

                var startTime = DateTime.UtcNow;
                int lastCompletedCount = 0;

                // Monitor the search
                while (_currentSearch.Status == MotelySearchStatus.Running && !cancellationToken.IsCancellationRequested && _isRunning)
                {
                    if (!_isRunning || cancellationToken.IsCancellationRequested || OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled)
                    {
                        DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Breaking out of search loop - cancellation requested");
                        break;
                    }

                    // Get current batch count
                    var currentBatchCount = _currentSearch.CompletedBatchCount;

                    // Estimate seeds searched
                    long seedsPerBatch = (long)Math.Pow(35, batchSize);
                    long currentSeeds = (long)currentBatchCount * seedsPerBatch;

                    var elapsed = DateTime.UtcNow - startTime;
                    var seedsPerSecond = elapsed.TotalSeconds > 0 ? currentSeeds / elapsed.TotalSeconds : 0;

                    // Report progress when batch count changes
                    if (currentBatchCount > lastCompletedCount)
                    {
                        lastCompletedCount = currentBatchCount;
                        
                        // Calculate percentage if we have total batches
                        double percentComplete = 0;
                        if (totalBatches.HasValue && totalBatches.Value > 0)
                        {
                            var batchesCompleted = currentBatchCount - criteria.StartBatch;
                            percentComplete = Math.Min(100, (double)batchesCompleted / totalBatches.Value * 100);
                        }
                        
                        progress?.Report(new SearchProgress
                        {
                            SeedsSearched = currentSeeds,
                            SeedsPerSecond = seedsPerSecond,
                            PercentComplete = percentComplete,
                            Message = percentComplete > 0 ? 
                                $"Searched ~{currentSeeds:N0} seeds ({percentComplete:F1}%) at {seedsPerSecond:F0}/s" :
                                $"Searched ~{currentSeeds:N0} seeds at {seedsPerSecond:F0}/s",
                            ResultsFound = Results.Count
                        });
                        
                        // Raise progress updated event
                        ProgressUpdated?.Invoke(this, new SearchProgressEventArgs
                        {
                            SeedsSearched = (int)currentSeeds,
                            ResultsFound = Results.Count,
                            SeedsPerSecond = seedsPerSecond,
                            PercentComplete = (int)percentComplete
                        });
                    }

                    await Task.Delay(50, cancellationToken);
                }

                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Search loop ended. Status={_currentSearch.Status}");

                // Give capture service a moment to catch any final results
                if (_resultCapture != null && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, CancellationToken.None);
                }

                // Report completion
                var finalBatchCount = _currentSearch?.CompletedBatchCount ?? 0;
                long finalSeeds = (long)finalBatchCount * (long)Math.Pow(35, batchSize);
                
                progress?.Report(new SearchProgress
                {
                    Message = cancellationToken.IsCancellationRequested || !_isRunning ? "Search cancelled" :
                        $"Search complete. Found {Results.Count} seeds",
                    IsComplete = true,
                    SeedsSearched = finalSeeds,
                    ResultsFound = Results.Count,
                    PercentComplete = 100
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"RunSearchInProcess exception: {ex.Message}");
                progress?.Report(new SearchProgress
                {
                    Message = $"Search error: {ex.Message}",
                    HasError = true,
                    IsComplete = true
                });
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _currentSearch?.Dispose();
            if (_resultCapture != null)
            {
                Task.Run(async () => await _resultCapture.StopCaptureAsync()).Wait(1000);
                _resultCapture.Dispose();
            }
            _resultCapture = null;
        }
    }
}