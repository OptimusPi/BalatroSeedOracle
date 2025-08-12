using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Motely;
using Motely.Filters;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;
using OuijaConfig = Motely.Filters.OuijaConfig;
using SearchResult = BalatroSeedOracle.Models.SearchResult;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Represents a single search instance that can run independently
    /// </summary>
    public class SearchInstance : IDisposable
    {
        private readonly string _searchId;
        private readonly SearchHistoryService _historyService;
        private readonly UserProfileService? _userProfileService;
        private CancellationTokenSource? _cancellationTokenSource;
        private volatile bool _isRunning;
        private volatile bool _isPaused;
        private OuijaConfig? _currentConfig;
        private IMotelySearch? _currentSearch;
        private MotelyResultCapture? _resultCapture;
        private DateTime _searchStartTime;
        private readonly ObservableCollection<BalatroSeedOracle.Models.SearchResult> _results;
        private readonly List<string> _consoleHistory = new();
        private readonly object _consoleHistoryLock = new();
        
        // Auto-cutoff tracking
        private bool _isAutoCutoffEnabled;
        private int _currentCutoff = 0;
        
        // Search state tracking for resume
        private SearchConfiguration? _currentSearchConfig;
        private ulong _lastSavedBatch = 0;
        private DateTime _lastStateSave = DateTime.UtcNow;

        // Properties
        public string SearchId
        {
            get => _searchId;
        }
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public ObservableCollection<BalatroSeedOracle.Models.SearchResult> Results => _results;
        public TimeSpan SearchDuration => DateTime.UtcNow - _searchStartTime;
        public string ConfigPath { get; private set; } = string.Empty;
        public string FilterName { get; private set; } = "Unknown";
        public int ResultCount => _results.Count;
        
        /// <summary>
        /// Gets the console output history for this search
        /// </summary>
        public List<string> GetConsoleHistory()
        {
            lock (_consoleHistoryLock)
            {
                return new List<string>(_consoleHistory);
            }
        }

        // Events for UI integration
        public event EventHandler? SearchStarted;
        public event EventHandler? SearchCompleted;
        public event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
        public event EventHandler<SearchResultEventArgs>? ResultFound;
        public event EventHandler<int>? CutoffChanged;
    public event EventHandler<string>? ConsoleOutput;

        public SearchInstance(string searchId, SearchHistoryService historyService)
        {
            _searchId = searchId;
            _historyService = historyService;
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
            _results = new ObservableCollection<BalatroSeedOracle.Models.SearchResult>();
        }

        /// <summary>
        /// Start searching with a direct config object
        /// </summary>
        public async Task StartSearchWithConfigAsync(
            OuijaConfig ouijaConfig,
            SearchConfiguration config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "StartSearchWithConfigAsync called - NO TEMP FILES!"
                );

                _currentConfig = ouijaConfig;
                _currentSearchConfig = config;
                ConfigPath = "Direct Config";
                FilterName = $"Search {_searchId.Substring(0, 8)}";
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

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
                    Stake = config.Stake ?? "White",
                    StartBatch = config.StartBatch,
                    EndBatch = config.EndBatch,
                    EnableDebugOutput = config.DebugMode,
                };

                // No need to start a search in database - just store results as they come

                // Set up result capture
                _resultCapture = new MotelyResultCapture(_historyService);
                _resultCapture.SetFilterConfig(ouijaConfig);
                _resultCapture.ResultCaptured += async (result) =>
                {
                    // If auto-cutoff is enabled, check if this result might increase the cutoff
                    if (_isAutoCutoffEnabled && result.TotalScore > _currentCutoff && result.TotalScore <= 5)
                    {
                        var oldCutoff = _currentCutoff;
                        // Increment cutoff by 1 to avoid skipping score levels
                        _currentCutoff = Math.Min(_currentCutoff + 1, result.TotalScore);
                        
                        DebugLogger.Log($"SearchInstance[{_searchId}]", 
                            $"Auto-cutoff: Found seed with score {result.TotalScore}, increasing cutoff from {oldCutoff} to {_currentCutoff}");
                        
                        // Notify UI of cutoff change
                        CutoffChanged?.Invoke(this, _currentCutoff);
                    }
                    
                    // Filter based on current cutoff - don't add to results or database if below cutoff
                    if (result.TotalScore < _currentCutoff)
                    {
                        return;
                    }
                    
                    _results.Add(result);

                    // Save to database
                    await _historyService.AddSearchResultAsync(result);

                    // Notify UI
                    ResultFound?.Invoke(
                        this,
                        new SearchResultEventArgs
                        {
                            Result = new BalatroSeedOracle.Views.Modals.SearchResult
                            {
                                Seed = result.Seed,
                                Score = result.TotalScore,
                                Details = "", // Removed ScoreBreakdown
                                TallyScores = result.Scores,
                                ItemLabels = result.Labels
                            },
                        }
                    );
                };

                // Start capturing
                await _resultCapture.StartCaptureAsync();

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                await Task.Run(
                    () =>
                        RunSearchInProcess(
                            ouijaConfig,
                            searchCriteria,
                            progressWrapper,
                            _cancellationTokenSource.Token
                        ),
                    _cancellationTokenSource.Token
                );

                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search completed");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search was cancelled");
                await CompleteSearch(true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Search failed: {ex.Message}"
                );
                progress?.Report(
                    new SearchProgress
                    {
                        Message = $"Search failed: {ex.Message}",
                        HasError = true,
                        IsComplete = true,
                    }
                );
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
        public async Task StartSearchAsync(
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search with config: {criteria.ConfigPath}"
                );

                ConfigPath = criteria.ConfigPath ?? string.Empty;
                FilterName = System.IO.Path.GetFileNameWithoutExtension(
                    criteria.ConfigPath ?? string.Empty
                );
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                // Clear previous results
                _results.Clear();

                // Load and validate the Ouija config
                if (string.IsNullOrEmpty(criteria.ConfigPath))
                {
                    throw new ArgumentException("Config path is required");
                }
                
                // Proper async I/O instead of Task.Run
                var json = await File.ReadAllTextAsync(criteria.ConfigPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                _currentConfig = JsonSerializer.Deserialize<OuijaConfig>(json, options);

                if (_currentConfig == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to load config from {criteria.ConfigPath}"
                    );
                }

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

                // Create progress wrapper
                var progressWrapper = new Progress<SearchProgress>(p =>
                {
                    progress?.Report(p);
                    HandleSearchProgress(p);
                });

                // No need to start a search in database - just store results as they come

                // Set up result capture
                _resultCapture = new MotelyResultCapture(_historyService);
                if (_currentConfig != null)
                {
                    _resultCapture.SetFilterConfig(_currentConfig);
                }
                _resultCapture.ResultCaptured += async (result) =>
                {
                    // If auto-cutoff is enabled, check if this result might increase the cutoff
                    if (_isAutoCutoffEnabled && result.TotalScore > _currentCutoff && result.TotalScore <= 5)
                    {
                        var oldCutoff = _currentCutoff;
                        // Increment cutoff by 1 to avoid skipping score levels
                        _currentCutoff = Math.Min(_currentCutoff + 1, result.TotalScore);
                        
                        DebugLogger.Log($"SearchInstance[{_searchId}]", 
                            $"Auto-cutoff: Found seed with score {result.TotalScore}, increasing cutoff from {oldCutoff} to {_currentCutoff}");
                        
                        // Notify UI of cutoff change
                        CutoffChanged?.Invoke(this, _currentCutoff);
                    }
                    
                    // Filter based on current cutoff - don't add to results or database if below cutoff
                    if (result.TotalScore < _currentCutoff)
                    {
                        return;
                    }
                    
                    _results.Add(result);

                    // Save to database
                    await _historyService.AddSearchResultAsync(result);

                    // Notify UI
                    ResultFound?.Invoke(
                        this,
                        new SearchResultEventArgs
                        {
                            Result = new BalatroSeedOracle.Views.Modals.SearchResult
                            {
                                Seed = result.Seed,
                                Score = result.TotalScore,
                                Details = "", // Removed ScoreBreakdown
                                TallyScores = result.Scores,
                                ItemLabels = result.Labels
                            },
                        }
                    );
                };

                // Start capturing
                await _resultCapture.StartCaptureAsync();

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                if (_currentConfig != null)
                {
                    await Task.Run(
                        () =>
                            RunSearchInProcess(
                                _currentConfig,
                                criteria,
                                progressWrapper,
                                _cancellationTokenSource.Token
                            ),
                        _cancellationTokenSource.Token
                    );
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
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"Search failed: {ex.Message}"
                );
                progress?.Report(
                    new SearchProgress
                    {
                        Message = $"Search failed: {ex.Message}",
                        HasError = true,
                        IsComplete = true,
                    }
                );
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
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    "Resume not supported - search needs to be restarted"
                );
            }
        }

        public void StopSearch()
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Stopping search...");

                // Flush any pending search state to disk before stopping
                _userProfileService?.FlushProfile();

                // Set the cancellation flag that Motely checks
                OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = true;

                // Also cancel the token
                _cancellationTokenSource?.Cancel();

                // Force _isRunning to false immediately
                _isRunning = false;

                // Stop result capture first
                if (_resultCapture != null)
                {
                    try
                    {
                        var stopTask = _resultCapture.StopCaptureAsync();
                        if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                        {
                            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Result capture stop timed out (continuing shutdown)");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Error stopping result capture: {ex.Message}");
                    }
                }

                // IMPORTANT: Stop the actual search service!
                if (_currentSearch != null)
                {
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "Pausing and disposing search");

                    try
                    {
                        // Pause first if it's running
                        if (_currentSearch.Status == MotelySearchStatus.Running)
                        {
                            _currentSearch.Pause();
                            DebugLogger.Log($"SearchInstance[{_searchId}]", "Search paused");
                        }

                        // Then dispose with a timeout
                        var disposeTask = Task.Run(() => _currentSearch.Dispose());
                        if (!disposeTask.Wait(TimeSpan.FromSeconds(2)))
                        {
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", "Search disposal timed out");
                        }
                        else
                        {
                            DebugLogger.Log($"SearchInstance[{_searchId}]", "Search disposed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Error disposing search: {ex.Message}");
                    }
                    finally
                    {
                        _currentSearch = null;
                    }
                }
            }
        }

        private void HandleSearchProgress(SearchProgress progress)
        {
            // Update UI with progress
            var eventArgs = new SearchProgressEventArgs
            {
                Message = progress.Message ?? string.Empty,
                PercentComplete = (int)progress.PercentComplete,
                SeedsSearched = progress.SeedsSearched,
                ResultsFound = progress.ResultsFound,
                IsComplete = progress.IsComplete,
                HasError = progress.HasError,
            };

            ProgressUpdated?.Invoke(this, eventArgs);

            
        }

        private Task CompleteSearch(bool wasCancelled)
        {
            // No need to complete search in database
            return Task.CompletedTask;
        }

        private async Task RunSearchInProcess(
            OuijaConfig config,
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken
        )
        {
            // Capture original console output and route filtered lines to UI
            var originalOut = Console.Out;
            FilteredConsoleWriter? filteredWriter = null;
            
            try
            {
                // Redirect console output to filter out duplicate seed reports and raise event
                bool filterSeedLines = !criteria.EnableDebugOutput; // debug shows raw seed CSV too
                filteredWriter = new FilteredConsoleWriter(originalOut, line =>
                {
                    // Store in history
                    lock (_consoleHistoryLock)
                    {
                        _consoleHistory.Add(line);
                        // Keep only last 1000 lines to prevent memory issues
                        if (_consoleHistory.Count > 1000)
                        {
                            _consoleHistory.RemoveAt(0);
                        }
                    }
                    try { ConsoleOutput?.Invoke(this, line); } catch { /* ignore */ }
                }, filterSeedLines);
                Console.SetOut(filteredWriter);
                
                // Reset cancellation flag
                OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;

                // Create filter descriptor
                var filterDesc = new OuijaJsonFilterDesc(config);
                
                // Enable auto-cutoff if MinScore is 0
                _isAutoCutoffEnabled = criteria.MinScore == 0;
                _currentCutoff = _isAutoCutoffEnabled ? 1 : criteria.MinScore; // Start at 1 for auto
                filterDesc.Cutoff = _currentCutoff;
                
                if (_isAutoCutoffEnabled)
                {
                    DebugLogger.Log($"SearchInstance[{_searchId}]", "Auto-cutoff enabled, starting at 1");
                }
                else
                {
                    DebugLogger.Log($"SearchInstance[{_searchId}]", $"Auto-cutoff disabled, using fixed cutoff: {criteria.MinScore}");
                }

                // Create search settings - following Motely Program.cs pattern
                var batchSize = criteria.BatchSize;
                
                // Calculate total batches based on batch character count
                // Total seeds = 35^8 = 2,251,875,390,625
                // Total batches = 35^(8-batchSize)
                ulong totalBatches = batchSize switch 
                {
                    1 => 64_339_296_875UL,          // 35^7 = 64,339,296,875
                    2 => 1_838_265_625UL,           // 35^6 = 1,838,265,625
                    3 => 52_521_875UL,              // 35^5 = 52,521,875
                    4 => 1_500_625UL,               // 35^4 = 1,500,625
                    5 => 42_875UL,                  // 35^3 = 42,875
                    6 => 1_225UL,                   // 35^2 = 1,225
                    7 => 35UL,                      // 35^1 = 35
                    8 => 1UL,                       // 35^0 = 1
                    _ => throw new ArgumentException($"Invalid batch character count: {batchSize}. Valid range is 1-8.")
                };
                
                // Apply end batch limit first
                ulong effectiveEndBatch = criteria.EndBatch;
                if (criteria.EndBatch == 0 || criteria.EndBatch == ulong.MaxValue)
                {
                    effectiveEndBatch = totalBatches;
                }
                
                // Create progress callback for Motely
                var motelyProgress = new Progress<Motely.MotelyProgress>(mp =>
                {
                    // Calculate actual percentage based on effective range
                    double actualPercent = 0;
                    if (effectiveEndBatch > criteria.StartBatch)
                    {
                        actualPercent = ((mp.CompletedBatchCount - criteria.StartBatch) / 
                                        (double)(effectiveEndBatch - criteria.StartBatch)) * 100;
                        actualPercent = Math.Min(Math.Max(actualPercent, 0), 100);
                    }
                    
                    // This runs on the captured synchronization context
                    progress?.Report(
                        new SearchProgress
                        {
                            SeedsSearched = mp.SeedsSearched,
                            SeedsPerMillisecond = mp.SeedsPerMillisecond,
                            PercentComplete = actualPercent,
                            Message = $"⏱️ {mp.FormattedMessage}",
                            ResultsFound = Results.Count,
                        }
                    );
                    
                    // Also raise our event
                    ProgressUpdated?.Invoke(
                        this,
                        new SearchProgressEventArgs
                        {
                            SeedsSearched = mp.SeedsSearched,
                            ResultsFound = Results.Count,
                            SeedsPerMillisecond = mp.SeedsPerMillisecond,
                            PercentComplete = (int)actualPercent,
                            BatchesSearched = mp.CompletedBatchCount,
                            TotalBatches = effectiveEndBatch,  // Use the actual end batch, not Motely's total
                        }
                    );
                    
                    // Update batch in memory on every progress update
                    if (mp.CompletedBatchCount != _lastSavedBatch)
                    {
                        _lastSavedBatch = mp.CompletedBatchCount;
                        
                        // First time? Save the full state
                        if (_userProfileService?.GetSearchState() == null)
                        {
                            SaveSearchState(mp.CompletedBatchCount, totalBatches, effectiveEndBatch);
                            _lastStateSave = DateTime.UtcNow;
                        }
                        else
                        {
                            // Just update the batch number in memory
                            _userProfileService?.UpdateSearchBatch(mp.CompletedBatchCount);
                            
                            // Write to disk every 10 seconds
                            if (DateTime.UtcNow > _lastStateSave.AddSeconds(10))
                            {
                                _userProfileService?.FlushProfile();
                                _lastStateSave = DateTime.UtcNow;
                            }
                        }
                    }
                });
                
                var searchSettings = new MotelySearchSettings<OuijaJsonFilterDesc.OuijaJsonFilter>(
                    filterDesc
                ).WithThreadCount(criteria.ThreadCount)
                    .WithBatchCharacterCount(batchSize)
                    .WithStartBatchIndex(criteria.StartBatch)
                    .WithSequentialSearch()
                    .WithProgressCallback(motelyProgress);

                // Use the already declared effectiveEndBatch
                searchSettings.WithEndBatchIndex(effectiveEndBatch);

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search with {criteria.ThreadCount} threads, batch size {batchSize}"
                );

                // Start the search
                _currentSearch = searchSettings.Start();

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Search started with batch size {batchSize}, total batches: {totalBatches}"
                );

                // Wait for search to complete - progress comes through callbacks!
                while (
                    _currentSearch.Status == MotelySearchStatus.Running
                    && !cancellationToken.IsCancellationRequested
                    && _isRunning
                )
                {
                    // Check for cancellation
                    if (!_isRunning || cancellationToken.IsCancellationRequested || OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            "Cancellation requested - stopping search"
                        );
                        break;
                    }

                    // Just wait - all updates come through callbacks now!
                    try
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        DebugLogger.LogImportant(
                            $"SearchInstance[{_searchId}]",
                            "Search cancelled"
                        );
                        break;
                    }
                }

                DebugLogger.LogImportant(
                    $"SearchInstance[{_searchId}]",
                    $"Search loop ended. Status={_currentSearch.Status}"
                );

                // Give capture service a moment to catch any final results
                if (_resultCapture != null && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, CancellationToken.None);
                }

                // Report completion
                var finalBatchCount = _currentSearch?.CompletedBatchCount ?? 0UL;
                ulong finalSeeds = finalBatchCount * (ulong)Math.Pow(35, batchSize);

                // Clear search state if completed successfully
                if (!cancellationToken.IsCancellationRequested && _isRunning && finalBatchCount >= effectiveEndBatch)
                {
                    _userProfileService?.ClearSearchState();
                }

                progress?.Report(
                    new SearchProgress
                    {
                        Message =
                            cancellationToken.IsCancellationRequested || !_isRunning
                                ? "Search cancelled"
                                : $"Search complete. Found {Results.Count} seeds",
                        IsComplete = true,
                        SeedsSearched = finalSeeds,
                        ResultsFound = Results.Count,
                        PercentComplete = 100,
                    }
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    $"SearchInstance[{_searchId}]",
                    $"RunSearchInProcess exception: {ex.Message}"
                );
                progress?.Report(
                    new SearchProgress
                    {
                        Message = $"Search error: {ex.Message}",
                        HasError = true,
                        IsComplete = true,
                    }
                );
            }
            finally
            {
                // Restore original console output
                if (originalOut != null)
                {
                    Console.SetOut(originalOut);
                }
                filteredWriter?.Dispose();
            }
        }

        // HandleAutoCutoff method removed - now handled inline when results are captured

        private void SaveSearchState(ulong completedBatch, ulong totalBatches, ulong endBatch)
        {
            try
            {
                if (_currentConfig == null || _currentSearchConfig == null) return;
                
                var state = new SearchResumeState
                {
                    IsDirectConfig = !string.IsNullOrEmpty(ConfigPath) && ConfigPath == "Direct Config",
                    ConfigPath = ConfigPath == "Direct Config" ? null : ConfigPath,
                    ConfigJson = ConfigPath == "Direct Config" ? 
                        JsonSerializer.Serialize(_currentConfig, new JsonSerializerOptions { WriteIndented = true }) : 
                        null,
                    LastCompletedBatch = completedBatch,
                    EndBatch = endBatch,
                    BatchSize = _currentSearchConfig.BatchSize,
                    ThreadCount = _currentSearchConfig.ThreadCount,
                    MinScore = _currentSearchConfig.MinScore,
                    Deck = _currentSearchConfig.Deck,
                    Stake = _currentSearchConfig.Stake,
                    LastActiveTime = DateTime.UtcNow,
                    TotalBatches = totalBatches
                };
                
                _userProfileService?.SaveSearchState(state);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"SearchInstance[{_searchId}]", $"Failed to save search state: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // First stop the search if it's running
            if (_isRunning)
            {
                StopSearch();
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            // Make sure to stop and dispose the search service
            if (_currentSearch != null)
            {
                // Pause first if it's running
                if (_currentSearch.Status == MotelySearchStatus.Running)
                {
                    _currentSearch.Pause();
                }
                _currentSearch.Dispose();
                _currentSearch = null;
            }

            if (_resultCapture != null)
            {
                Task.Run(async () => await _resultCapture.StopCaptureAsync()).Wait(1000);
                _resultCapture.Dispose();
            }
            _resultCapture = null;

            GC.SuppressFinalize(this);
        }
    }
}
