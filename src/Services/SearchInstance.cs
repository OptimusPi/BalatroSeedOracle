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
        private DateTime _searchStartTime;
        private readonly ObservableCollection<BalatroSeedOracle.Models.SearchResult> _results;
        private readonly List<string> _consoleHistory = new();
        private readonly object _consoleHistoryLock = new();
        private Task? _searchTask;
        private bool _preventStateSave = false;  // Flag to prevent saving state when icon is removed
        
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
        public DateTime SearchStartTime => _searchStartTime;
        public string ConfigPath { get; private set; } = string.Empty;
        public string FilterName { get; private set; } = "Unknown";
        public OuijaConfig? GetFilterConfig() => _currentConfig;
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

        public SearchInstance(string searchId, SearchHistoryService historyService)
        {
            _searchId = searchId;
            _historyService = historyService;
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
            _results = new ObservableCollection<BalatroSeedOracle.Models.SearchResult>();
        }

        /// <summary>
        /// Start searching with a file path
        /// </summary>
        private async Task StartSearchFromFileAsync(
            string configPath,
            SearchConfiguration config,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"StartSearchFromFileAsync ENTERED! configPath={configPath}");
            
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search already running");
                return;
            }

            try
            {
                DebugLogger.Log(
                    $"SearchInstance[{_searchId}]",
                    $"Starting search from file: {configPath}"
                );

                // Load the config from file
                var json = await File.ReadAllTextAsync(configPath);
                var ouijaConfig = JsonSerializer.Deserialize<OuijaConfig>(json);
                if (ouijaConfig == null)
                {
                    throw new InvalidOperationException($"Failed to load config from {configPath}");
                }
                
                _currentConfig = ouijaConfig;
                _currentSearchConfig = config;
                ConfigPath = configPath;
                FilterName = ouijaConfig.Name ?? Path.GetFileNameWithoutExtension(configPath);
                _searchStartTime = DateTime.UtcNow;
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );

                // Clear previous results
                _results.Clear();

                // Notify UI that search started
                SearchStarted?.Invoke(this, EventArgs.Empty);

                // Progress is handled directly in motelyProgress callback below

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
                    DebugSeed = config.DebugSeed,
                };

                // The history service should already be configured by SearchModal
                // Just verify it has the right config
                if (_historyService.ColumnNames.Count <= 2)
                {
                    DebugLogger.LogError($"SearchInstance[{_searchId}]", "History service not properly configured - only has seed/score columns!");
                    _historyService.SetFilterConfig(ouijaConfig);
                    _historyService.SetFilterName(ConfigPath);
                }

                // Register direct callback with OuijaJsonFilterDesc
                DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Registering OnResultFound callback");
                OuijaJsonFilterDesc.OnResultFound = async (seed, totalScore, scores) =>
                {
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"Direct callback - Found: {seed} Score: {totalScore}");
                    
                    var result = new SearchResult
                    {
                        Seed = seed,
                        TotalScore = totalScore,
                        Scores = scores,
                        Labels = ouijaConfig.Should?.Select(s => s.Value ?? "Unknown").ToArray()
                    };
                    
                    // Add to results collection
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
                                Seed = seed,
                                Score = totalScore,
                                TallyScores = scores
                            },
                        }
                    );
                };

                // Run the search using the MotelySearchService pattern
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Starting in-process search...");

                _searchTask = Task.Run(
                    () =>
                        RunSearchInProcess(
                            ouijaConfig,
                            searchCriteria,
                            progress,
                            _cancellationTokenSource.Token
                        ),
                    _cancellationTokenSource.Token
                );
                
                await _searchTask;

                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search completed");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Search was cancelled");
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
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"StartSearchAsync ENTERED! ConfigPath={criteria.ConfigPath}");
            
            // Load the config from file
            if (string.IsNullOrEmpty(criteria.ConfigPath))
            {
                throw new ArgumentException("Config path is required");
            }
            
            DebugLogger.Log(
                $"SearchInstance[{_searchId}]",
                $"Loading config from: {criteria.ConfigPath}"
            );

            // Load and validate the Ouija config
            var json = await File.ReadAllTextAsync(criteria.ConfigPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var config = JsonSerializer.Deserialize<OuijaConfig>(json, options);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load config from {criteria.ConfigPath}"
                );
            }

            // Store the config path for reference
            ConfigPath = criteria.ConfigPath;
            FilterName = System.IO.Path.GetFileNameWithoutExtension(criteria.ConfigPath);

            // Convert SearchCriteria to SearchConfiguration
            var searchConfig = new SearchConfiguration
            {
                ThreadCount = criteria.ThreadCount,
                MinScore = criteria.MinScore,
                BatchSize = criteria.BatchSize,
                Deck = criteria.Deck,
                Stake = criteria.Stake,
                StartBatch = criteria.StartBatch,
                EndBatch = criteria.EndBatch,
                DebugMode = criteria.EnableDebugOutput,
                DebugSeed = criteria.DebugSeed
            };

            // Call the main search method with the file path
            await StartSearchFromFileAsync(criteria.ConfigPath, searchConfig, progress, cancellationToken);
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
            StopSearch(false);
        }
        
        public void StopSearch(bool preventStateSave)
        {
            if (_isRunning)
            {
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Stopping search...");
                
                _preventStateSave = preventStateSave;

                // Flush any pending search state to disk before stopping (unless prevented)
                if (!preventStateSave)
                {
                    _userProfileService?.FlushProfile();
                }

                // Force _isRunning to false IMMEDIATELY
                _isRunning = false;
                
                // Set the cancellation flag that Motely checks
                OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = true;

                // Cancel the token immediately
                _cancellationTokenSource?.Cancel();
                
                // Wait for the search task to complete (with timeout)
                if (_searchTask != null && !_searchTask.IsCompleted)
                {
                    try
                    {
                        // Wait up to 1 second for the task to complete
                        if (!_searchTask.Wait(1000))
                        {
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", "Search task did not complete within timeout");
                        }
                    }
                    catch (AggregateException)
                    {
                        // Expected when task is cancelled
                    }
                    _searchTask = null;
                }
                
                // Send completed event immediately so UI updates
                SearchCompleted?.Invoke(this, EventArgs.Empty);

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

                        // Then dispose with a very short timeout - we don't care if it completes
                        var disposeTask = Task.Run(() => _currentSearch.Dispose());
                        if (!disposeTask.Wait(TimeSpan.FromMilliseconds(200)))
                        {
                            // Don't wait, just log and continue
                            DebugLogger.LogError($"SearchInstance[{_searchId}]", "Search disposal timed out (abandoning)");
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


        private async Task RunSearchInProcess(
            OuijaConfig config,
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken
        )
        {
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "RunSearchInProcess ENTERED!");
            DebugLogger.LogImportant($"SearchInstance[{_searchId}]", $"OnResultFound is {(OuijaJsonFilterDesc.OnResultFound != null ? "SET" : "NULL")}");
            
            try
            {
                
                // Reset cancellation flag
                OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;
                
                // Enable Motely's DebugLogger if in debug mode
                Motely.DebugLogger.IsEnabled = criteria.EnableDebugOutput;
                if (criteria.EnableDebugOutput)
                {
                    DebugLogger.LogImportant($"SearchInstance[{_searchId}]", "Debug mode enabled - Motely DebugLogger activated");
                }

                // Create filter descriptor
                var filterDesc = new OuijaJsonFilterDesc(config);
                
                // ALWAYS pass MinScore directly to Motely - let Motely handle auto-cutoff internally!
                // MinScore=0 means auto-cutoff in Motely
                filterDesc.Cutoff = criteria.MinScore;
                filterDesc.AutoCutoff = criteria.MinScore == 0;
                
                DebugLogger.Log($"SearchInstance[{_searchId}]", $"Passing cutoff={criteria.MinScore} to Motely (0=auto)");

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
                    //5 => 42_875UL,                  // 35^3 = 42,875
                    //6 => 1_225UL,                   // 35^2 = 1,225
                    //7 => 35UL,                      // 35^1 = 35
                    //8 => 1UL,                       // 35^0 = 1 // these are all way too big and don't make the search faster.
                    _ => throw new ArgumentException($"Invalid batch character count: {batchSize}. Valid range is 1-4.")
                };
                
                // If no end batch specified, search all
                ulong effectiveEndBatch = (criteria.EndBatch == 0) ? totalBatches : criteria.EndBatch;
                
                // Create progress callback for Motely
                var motelyProgress = new Progress<Motely.MotelyProgress>(mp =>
                {
                    // Simple percentage: completed / total * 100
                    double actualPercent = (mp.CompletedBatchCount / (double)totalBatches) * 100.0;
                    
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
                        
                        // Save to database for resume - save the NEXT batch to process, not the completed one
                        // This prevents re-processing the same batch on resume
                        _ = _historyService.SaveLastBatchAsync(mp.CompletedBatchCount + 1);
                        
                        // Only save state if not prevented
                        if (!_preventStateSave)
                        {
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

                // Start the search - use list search if a specific seed is provided
                if (!string.IsNullOrEmpty(criteria.DebugSeed))
                {
                    var seedList = new List<string> { criteria.DebugSeed };
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        $"Searching for specific seed: {criteria.DebugSeed}"
                    );
                    _currentSearch = searchSettings.WithListSearch(seedList).Start();
                }
                else
                {
                    _currentSearch = searchSettings.Start();
                }

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
                        // Check more frequently for faster stop response
                        await Task.Delay(50, cancellationToken);
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
                
                // If we exited the loop due to cancellation, stop the Motely search
                if (_currentSearch != null && _currentSearch.Status == MotelySearchStatus.Running)
                {
                    DebugLogger.LogImportant(
                        $"SearchInstance[{_searchId}]",
                        "Stopping Motely search due to cancellation"
                    );
                    try
                    {
                        _currentSearch.Pause();
                        _currentSearch.Dispose();
                        _currentSearch = null; // Mark as disposed
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            $"SearchInstance[{_searchId}]",
                            $"Error stopping Motely search: {ex.Message}"
                        );
                    }
                }


                // Report completion (get count before disposing)
                var finalBatchCount = 0UL;
                if (_currentSearch != null)
                {
                    try
                    {
                        finalBatchCount = _currentSearch.CompletedBatchCount;
                    }
                    catch
                    {
                        // If disposed, use 0
                        finalBatchCount = 0UL;
                    }
                }
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
                // Reset Motely's DebugLogger
                Motely.DebugLogger.IsEnabled = false;
            }
        }

        // HandleAutoCutoff method removed - now handled inline when results are captured

        private void SaveSearchState(ulong completedBatch, ulong totalBatches, ulong endBatch)
        {
            if (_preventStateSave) return; // Don't save if we're removing the icon
            
            try
            {
                if (_currentConfig == null || _currentSearchConfig == null) return;
                
                var state = new SearchResumeState
                {
                    ConfigPath = ConfigPath,
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
                DebugLogger.Log($"SearchInstance[{_searchId}]", "Dispose called while running - forcing stop");
                StopSearch();
                
                // Wait a bit for graceful shutdown
                System.Threading.Thread.Sleep(1000);
            }
            
            // Ensure the search task is completed or abandoned
            if (_searchTask != null && !_searchTask.IsCompleted)
            {
                try
                {
                    // Give it one more chance to complete
                    _searchTask.Wait(100);
                }
                catch { /* ignore */ }
                _searchTask = null;
            }
            
            _cancellationTokenSource?.Dispose();
            
            // Force dispose of the search if it still exists and not already disposed
            if (_currentSearch != null)
            {
                try
                {
                    // Check if not already disposed
                    if (_currentSearch.Status != MotelySearchStatus.Disposed)
                    {
                        _currentSearch.Dispose();
                    }
                }
                catch { /* ignore disposal errors */ }
                _currentSearch = null;
            }
            
            DebugLogger.Log($"SearchInstance[{_searchId}]", "Dispose completed");
            
            GC.SuppressFinalize(this);
        }
    }
}
