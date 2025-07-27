using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Oracle.Helpers;
using Oracle.Models;
using Motely;
using Motely.Filters;

namespace Oracle.Services;

/// <summary>
/// Service that interfaces with Motely for searching
/// </summary>
public class MotelySearchService : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isRunning;
    private Motely.Filters.OuijaConfig? _currentConfig;
    private IMotelySearch? _currentSearch;
    private readonly SearchHistoryService _historyService;
    private long _currentSearchId = -1;
    private DateTime _searchStartTime;
    
    public bool IsRunning => _isRunning;
    public List<Oracle.Models.SearchResult> Results { get; } = new();

    public MotelySearchService()
    {
        _historyService = ServiceHelper.GetService<SearchHistoryService>() ?? new SearchHistoryService();
    }

    /// <summary>
    /// Load a config file and validate it
    /// </summary>
    public async Task<(bool success, string message)> LoadConfigAsync(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return (false, "Config file not found");

            // Load the config using Motely's loader
            await Task.Run(() => 
            {
                _currentConfig = Motely.Filters.OuijaConfig.LoadFromJson(configPath);
            });

            if (_currentConfig == null)
                return (false, "Failed to parse config file");
            else
            {
                var configAsJson = JsonSerializer.Serialize(_currentConfig);
                Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Loaded config: \r\n{configAsJson}\r\n");
            }
            
            // Count needs and wants
            var needs = _currentConfig.Needs?.Length ?? 0;
            var wants = _currentConfig.Wants?.Length ?? 0;
            
            return (true, $"Loaded config with {needs} needs, {wants} wants");
        }
        catch (Exception ex)
        {
            return (false, $"Error loading config: {ex.Message}");
        }
    }

    /// <summary>
    /// Start a search with the specified criteria
    /// </summary>
    public async Task StartSearchAsync(Oracle.Models.SearchCriteria criteria, IProgress<Oracle.Models.SearchProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"StartSearchAsync called on thread: {Thread.CurrentThread.ManagedThreadId}");
        Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Criteria: Threads={criteria.ThreadCount}, MinScore={criteria.MinScore}");
        
        if (_isRunning || string.IsNullOrEmpty(criteria.ConfigPath))
            return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        Results.Clear();
        _searchStartTime = DateTime.UtcNow;

        try
        {
            // Start a new search in DuckDB
            var deck = criteria.Deck ?? "Red Deck";
            var stake = criteria.Stake ?? "White Stake";
            _currentSearchId = await _historyService.StartNewSearchAsync(
                criteria.ConfigPath, 
                criteria.ThreadCount, 
                criteria.MinScore, 
                criteria.BatchSize,
                deck,
                stake
            );
            // Load config if needed
            if (_currentConfig == null)
            {
                var (success, message) = await LoadConfigAsync(criteria.ConfigPath);
                if (!success)
                {
                    progress?.Report(new Oracle.Models.SearchProgress
                    {
                        Message = message,
                        HasError = true,
                        IsComplete = true
                    });
                    return;
                }
            }

            // Report initial progress
            progress?.Report(new Oracle.Models.SearchProgress
            {
                Message = "Initializing Motely search engine...",
                PercentComplete = 0,
                SeedsSearched = 0
            });

            // Run the search in a background task
            // Task.Run is the modern best practice for background work in AvaloniaUI
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Starting Task.Run for search");
            await Task.Run(() => RunSearch(criteria, progress, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Task.Run completed");
        }
        catch (OperationCanceledException)
        {
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Search was cancelled");
            progress?.Report(new Oracle.Models.SearchProgress
            {
                Message = "Search cancelled",
                IsComplete = true,
                SeedsSearched = 0,
                ResultsFound = Results.Count
            });
        }
        catch (Exception ex)
        {
            progress?.Report(new Oracle.Models.SearchProgress
            {
                Message = $"Error: {ex.Message}",
                HasError = true,
                IsComplete = true
            });
        }
        finally
        {
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "StartSearchAsync finally block - cleaning up");
            _isRunning = false;
            // Don't dispose/null _currentSearch here - we might need it in the catch block
        }
    }

    private async Task RunSearch(Oracle.Models.SearchCriteria criteria, IProgress<Oracle.Models.SearchProgress>? progress, CancellationToken cancellationToken)
    {
        Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"RunSearch started on thread: {Thread.CurrentThread.ManagedThreadId}");
        
        try
        {
            // Reset cancellation flag
            OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = false;
            
            // Create filter descriptor
            var filterDesc = new OuijaJsonFilterDesc(_currentConfig!);
            filterDesc.Cutoff = criteria.MinScore;
            
            // Log the configuration details
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"=== SEARCH CONFIGURATION ===");
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Min Score: {criteria.MinScore}");
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Max Seeds: {criteria.MaxSeeds}");
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Thread Count: {criteria.ThreadCount}");
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Needs: {_currentConfig?.Needs?.Length ?? 0}");
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Wants: {_currentConfig?.Wants?.Length ?? 0}");
            
            if (_currentConfig?.Needs != null)
            {
                foreach (var need in _currentConfig.Needs)
                {
                    var antes = need.SearchAntes ?? Array.Empty<int>();
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"  Need: Type={need.Type}, Value={need.Value ?? "any"}, SearchAntes=[{string.Join(",", antes)}]");
                }
            }
            
            if (_currentConfig?.Wants != null)
            {
                foreach (var want in _currentConfig.Wants)
                {
                    var antes = want.SearchAntes ?? Array.Empty<int>();
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"  Want: Type={want.Type}, Value={want.Value ?? "any"}, Score={want.Score}, SearchAntes=[{string.Join(",", antes)}]");
                }
            }

            // Create search settings - following Motely Program.cs pattern
            var batchSize = criteria.BatchSize;
            var searchSettings = new MotelySearchSettings<OuijaJsonFilterDesc.OuijaJsonFilter>(filterDesc)
                .WithThreadCount(criteria.ThreadCount)
                .WithBatchCharacterCount(batchSize)
                .WithStartBatchIndex(0);

            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Starting search with {criteria.ThreadCount} threads, batch size {batchSize}");
            
            // Start the search
            _currentSearch = searchSettings.Start();
            
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Search started with batch size {batchSize}");

            var startTime = DateTime.UtcNow;
            int loopCount = 0;
            int lastCompletedCount = 0;

            // Monitor the search - similar to how Program.cs does it
            while (_currentSearch.Status == MotelySearchStatus.Running && !cancellationToken.IsCancellationRequested && _isRunning)
            {
                // Double-check cancellation at the start of each iteration
                if (!_isRunning || cancellationToken.IsCancellationRequested || OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled)
                {
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Breaking out of search loop - cancellation requested");
                    break;
                }
                    
                // Get current batch count (this is what's available on the interface)
                var currentBatchCount = _currentSearch.CompletedBatchCount;
                
                // Estimate seeds searched - batchSize determines seeds per batch
                // Balatro uses 35 characters (no 0): 123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ
                // With batchSize=4, each batch has 35^4 = 1,500,625 seeds
                long seedsPerBatch = (long)Math.Pow(35, batchSize); // 35 characters, batchSize positions
                long currentSeeds = (long)currentBatchCount * seedsPerBatch;
                
                var elapsed = DateTime.UtcNow - startTime;
                var seedsPerSecond = elapsed.TotalSeconds > 0 ? currentSeeds / elapsed.TotalSeconds : 0;

                // Collect any new results from the static results queue used by OuijaJsonFilterDesc
                int dequeueCount = 0;
                while (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue != null && 
                       OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out var result))
                {
                    dequeueCount++;
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Result #{dequeueCount} dequeued: Seed={result.Seed}, Score={result.TotalScore}, Success={result.Success}");

                    // Check if result meets minimum score criteria (should already be filtered by Cutoff, but double-check)
                    if (result.TotalScore < criteria.MinScore)
                    {
                        Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"  -> REJECTED: Score {result.TotalScore} < MinScore {criteria.MinScore}");
                        continue;
                    }
                    
                    // Convert OuijaResult to our SearchResult
                    var searchResult = new Oracle.Models.SearchResult
                    {
                        Seed = result.Seed,
                        Score = result.TotalScore,
                        Details = BuildDetailsString(result, _currentConfig!)
                    };
                    Results.Add(searchResult);

                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"  -> ACCEPTED: Added to results (total: {Results.Count})");
                    Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"  -> Details: {searchResult.Details}");
                    
                    // Report new result
                    progress?.Report(new Oracle.Models.SearchProgress
                    {
                        NewResult = searchResult,
                        SeedsSearched = currentSeeds,
                        ResultsFound = Results.Count,
                        Message = $"Found seed: {result.Seed} (Score: {result.TotalScore})",
                        SeedsPerSecond = seedsPerSecond
                    });
                }
                
                if (dequeueCount > 0)
                {
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Processed {dequeueCount} results this iteration");
                }

                // Report progress when batch count changes
                if (currentBatchCount > lastCompletedCount)
                {
                    lastCompletedCount = currentBatchCount;
                    Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Progress: Batch {currentBatchCount}, ~{currentSeeds:N0} seeds, {Results.Count} results found");
                    
                    progress?.Report(new Oracle.Models.SearchProgress
                    {
                        SeedsSearched = currentSeeds,
                        SeedsPerSecond = seedsPerSecond,
                        PercentComplete = 0, // We don't know total batches
                        Message = $"Searched ~{currentSeeds:N0} seeds at {seedsPerSecond:F0}/s",
                        ResultsFound = Results.Count
                    });
                }

                loopCount++;
                if (loopCount % 10 == 0)
                {
                    Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Loop {loopCount}: Status={_currentSearch.Status}, Cancelled={cancellationToken.IsCancellationRequested}");
                }
                
                // Use shorter delay and catch cancellation
                try
                {
                    await Task.Delay(50, cancellationToken); // Reduced from 100ms to 50ms for faster response
                }
                catch (OperationCanceledException)
                {
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Task.Delay cancelled - exiting loop");
                    break;
                }
            }

            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Search loop ended. Status={_currentSearch.Status}, Cancelled={cancellationToken.IsCancellationRequested}");
            
            // Collect any final results (but skip if we're force-stopping)
            int finalResultCount = 0;
            while (!cancellationToken.IsCancellationRequested && 
                   _isRunning &&
                   !OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled &&
                   OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue != null && 
                   OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out var result))
            {
                finalResultCount++;
                Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Final result collected: Seed={result.Seed}, Score={result.TotalScore}");
                
                // Check minimum score
                if (result.TotalScore < criteria.MinScore)
                {
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"  -> REJECTED in final collection: Score {result.TotalScore} < MinScore {criteria.MinScore}");
                    continue;
                }
                
                var searchResult = new Oracle.Models.SearchResult
                {
                    Seed = result.Seed,
                    Score = result.TotalScore,
                    Details = BuildDetailsString(result, _currentConfig!),
                    Ante = 1 // TODO: Get ante from result when available
                };
                
                Results.Add(searchResult);
                
                // Save to DuckDB
                if (_currentSearchId > 0)
                {
                    await _historyService.AddSearchResultAsync(_currentSearchId, searchResult);
                }
            }
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Collected {finalResultCount} final results, {Results.Count} total accepted");

            // Report completion
            var finalBatchCount = _currentSearch?.CompletedBatchCount ?? 0;
            long finalSeeds = (long)finalBatchCount * (long)Math.Pow(35, batchSize);
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Final stats: CompletedBatchCount={finalBatchCount}, EstimatedSeeds={finalSeeds}");
            
            // Complete the search in DuckDB
            if (_currentSearchId > 0)
            {
                var duration = (DateTime.UtcNow - _searchStartTime).TotalSeconds;
                await _historyService.CompleteSearchAsync(_currentSearchId, finalSeeds, duration);
            }
            
            // Only report completion/cancellation if we haven't been force-stopped
            if (_isRunning || !OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled)
            {
                progress?.Report(new Oracle.Models.SearchProgress
                {
                    Message = cancellationToken.IsCancellationRequested || !_isRunning ? "Search cancelled" : 
                        $"Search complete. Found {Results.Count} seeds",
                    IsComplete = true,
                    SeedsSearched = finalSeeds,
                    ResultsFound = Results.Count,
                    PercentComplete = 100
                });
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("MotelySearchService", $"RunSearch exception: {ex.GetType().Name}: {ex.Message}");
            Oracle.Helpers.DebugLogger.LogError("MotelySearchService", $"Stack trace: {ex.StackTrace}");
            
            progress?.Report(new Oracle.Models.SearchProgress
            {
                Message = $"Search error: {ex.Message}",
                HasError = true,
                IsComplete = true
            });
        }
    }

    /// <summary>
    /// Stop the current search
    /// </summary>
    public void StopSearch()
    {
        Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "StopSearch called - FORCE STOPPING NOW");
        
        // IMMEDIATELY set all stop flags
        _isRunning = false;
        OuijaJsonFilterDesc.OuijaJsonFilter.IsCancelled = true;
        
        // Cancel the token source FIRST to interrupt the RunSearch loop
        try
        {
            _cancellationTokenSource?.Cancel();
            Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Cancellation token cancelled");
        }
        catch { }
        
        // Force stop the search engine - Pause first, then Dispose
        try
        {
            if (_currentSearch != null)
            {
                Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Current search status: {_currentSearch.Status}");
                
                // Only pause if it's running
                if (_currentSearch.Status == MotelySearchStatus.Running)
                {
                    _currentSearch.Pause();
                    Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Search paused");
                }
                
                // Dispose in a separate task with timeout to avoid blocking
                var disposeTask = Task.Run(() => {
                    try 
                    {
                        _currentSearch.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Oracle.Helpers.DebugLogger.LogError("MotelySearchService", $"Error disposing search: {ex.Message}");
                    }
                });
                
                // Wait max 1 second for disposal
                if (!disposeTask.Wait(1000))
                {
                    Oracle.Helpers.DebugLogger.LogError("MotelySearchService", "Search disposal timed out");
                }
                
                _currentSearch = null;
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.LogError("MotelySearchService", $"Error stopping search: {ex.Message}");
        }
        
        // Clear the results queue completely
        if (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue != null)
        {
            int drainedCount = 0;
            while (OuijaJsonFilterDesc.OuijaJsonFilter.ResultsQueue.TryDequeue(out _))
            {
                drainedCount++;
            }
            if (drainedCount > 0)
            {
                Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", $"Drained {drainedCount} results from queue");
            }
        }
        
        Oracle.Helpers.DebugLogger.LogImportant("MotelySearchService", "Search STOPPED");
    }

    private string BuildDetailsString(Motely.Filters.OuijaResult result, Motely.Filters.OuijaConfig config)
    {
        var details = new List<string>();
        
        // Add want scores if available
        if (result.ScoreWants != null && config.Wants != null)
        {
            for (int i = 0; i < config.Wants.Length && i < result.ScoreWants.Length; i++)
            {
                var score = result.ScoreWants[i];
                if (score > 0)
                {
                    var want = config.Wants[i];
                    var name = !string.IsNullOrEmpty(want.Value) ? want.Value : want.Type;
                    details.Add($"{name}={score}");
                }
            }
        }
        
        // Add negative joker info if present
        if (result.NaturalNegativeJokers > 0)
            details.Add($"Natural Negatives: {result.NaturalNegativeJokers}");
        if (result.DesiredNegativeJokers > 0)
            details.Add($"Desired Negatives: {result.DesiredNegativeJokers}");
            
        return details.Count > 0 ? string.Join(", ", details) : "No details";
    }
    
    /// <summary>
    /// Dispose and cleanup resources
    /// </summary>
    public void Dispose()
    {
        Oracle.Helpers.DebugLogger.Log("MotelySearchService", "Disposing MotelySearchService - stopping any running searches");
        
        // Force stop any running search
        _isRunning = false;
        
        try
        {
            // Cancel the token source first
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Oracle.Helpers.DebugLogger.Log("MotelySearchService", "Requesting cancellation");
                _cancellationTokenSource.Cancel();
            }
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Error during cancellation: {ex.Message}");
        }
        
        // Dispose the Motely search with timeout
        if (_currentSearch != null)
        {
            Oracle.Helpers.DebugLogger.Log("MotelySearchService", "Disposing Motely search instance");
            var disposeTask = Task.Run(() =>
            {
                try
                {
                    _currentSearch.Dispose();
                }
                catch (Exception ex)
                {
                    Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Error disposing search: {ex.Message}");
                }
            });
            
            // Wait max 3 seconds for disposal
            if (!disposeTask.Wait(3000))
            {
                Oracle.Helpers.DebugLogger.LogError("MotelySearchService", "Motely search disposal timed out after 3 seconds - abandoning");
            }
            else
            {
                Oracle.Helpers.DebugLogger.Log("MotelySearchService", "Motely search disposed successfully");
            }
        }
        
        try
        {
            // Dispose the cancellation token source
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            Oracle.Helpers.DebugLogger.Log("MotelySearchService", $"Error disposing cancellation token: {ex.Message}");
        }
        
        _cancellationTokenSource = null;
        _currentSearch = null;
        
        // Dispose the history service to close the DuckDB connection
        if (_historyService is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        Oracle.Helpers.DebugLogger.Log("MotelySearchService", "MotelySearchService disposed");
    }
}