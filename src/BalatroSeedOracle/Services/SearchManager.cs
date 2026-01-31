using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using Motely;
using Motely.DB;
using Motely.Executors;
using Motely.Filters;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Result of a quick validation search
/// </summary>
public class QuickSearchResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<string> Seeds { get; set; } = new();
    public List<SearchResult>? Results { get; set; }
    public double ElapsedTime { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Manages seed searches in BalatroSeedOracle.
/// 
/// This is a thin wrapper around Motely's search functionality.
/// Motely owns all database operations - BSO just calls Motely.
/// 
/// ZERO database code here - all storage/queries are handled by Motely.
/// </summary>
public sealed class SearchManager : IDisposable
{
    private readonly IPlatformServices _platformServices;
    private readonly ConcurrentDictionary<string, ActiveSearchContext> _activeSearches = new();
    private bool _disposed;

    public SearchManager(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    /// <summary>
    /// Start a new search (sync version).
    /// </summary>
    public ActiveSearchContext StartSearch(SearchCriteria criteria, MotelyJsonConfig config)
    {
        DebugLogger.Log("SearchManager", $"Starting search for filter: {config.Name}");

        // Build Motely search parameters
        var searchParams = new JsonSearchParams
        {
            Threads = criteria.ThreadCount,
            BatchSize = criteria.BatchSize,
            StartBatch = criteria.StartBatch,
            EndBatch = criteria.EndBatch,
            SpecificSeed = criteria.DebugSeed,
            EnableDebug = criteria.EnableDebugOutput,
            Deck = criteria.Deck,
            Stake = criteria.Stake,
        };

        // Determine storage mode based on platform
        // Browser = in-memory (no native DuckDB)
        // Desktop = database (native DuckDB)
        var useInMemoryStorage = !_platformServices.SupportsFileSystem;

        DebugLogger.Log("SearchManager", $"Storage mode: {(useInMemoryStorage ? "In-Memory (Browser)" : "Database (Desktop)")}");

        // Wrap in ActiveSearchContext FIRST so we can set up ResultCallback
        // We'll create a placeholder that will be updated after context creation
        ActiveSearchContext? contextRef = null;

        // Set up ResultCallback for real-time result streaming (like Motely.WASM POC)
        // This pushes results to UI as they're found, not just after completion
        searchParams.ResultCallback = result =>
        {
            // Convert Motely result to BSO SearchResult and raise event
            var searchResult = new Models.SearchResult
            {
                Seed = result.Seed,
                TotalScore = result.Score,
                Scores = result.TallyColumns?.ToArray() ?? Array.Empty<int>()
            };
            
            // Raise event on context (will be set before Start() is called)
            contextRef?.RaiseResultFound(searchResult);
        };

        // JUST CALL MOTELY. That's it.
        // Motely owns everything: SearchId, FilterId, database, queries.
        var motelyContext = MotelySearchOrchestrator.LaunchWithContext(
            config,
            searchParams,
            useInMemoryStorage);

        DebugLogger.Log("SearchManager", $"Search started - SearchId: {motelyContext.SearchId}, FilterId: {motelyContext.FilterId}");

        // Wrap in ActiveSearchContext for UI binding
        var context = new ActiveSearchContext(motelyContext, config);
        contextRef = context; // Assign to closure variable
        _activeSearches[context.SearchId] = context;

        return context;
    }

    /// <summary>
    /// Get an existing search by ID
    /// </summary>
    public ActiveSearchContext? GetSearch(string searchId)
    {
        _activeSearches.TryGetValue(searchId, out var context);
        return context;
    }

    /// <summary>
    /// Get all active searches
    /// </summary>
    public IEnumerable<ActiveSearchContext> GetAllSearches()
    {
        return _activeSearches.Values;
    }

    /// <summary>
    /// Remove a search from tracking (after completion/cancellation)
    /// </summary>
    public void RemoveSearch(string searchId)
    {
        if (_activeSearches.TryRemove(searchId, out var context))
        {
            DebugLogger.Log("SearchManager", $"Removed search: {searchId}");
        }
    }

    /// <summary>
    /// Get or restore a search by ID
    /// </summary>
    public ActiveSearchContext? GetOrRestoreSearch(string searchId)
    {
        // For now, just return the active search if it exists
        // Future: could implement persistence/restoration
        return GetSearch(searchId);
    }

    /// <summary>
    /// Initialize the SequentialLibrary for search persistence.
    /// Call this at app startup (Desktop only).
    /// </summary>
    public void InitializeLibrary(string seedsPath)
    {
        if (!_platformServices.SupportsFileSystem)
        {
            DebugLogger.Log("SearchManager", "Skipping library initialization (browser)");
            return;
        }

        try
        {
            SequentialLibrary.SetLibraryRoot(seedsPath);
            DebugLogger.Log("SearchManager", $"SequentialLibrary initialized at: {seedsPath}");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Failed to initialize library: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore active searches from the SequentialLibrary.
    /// Returns metadata for searches that need UI widgets created.
    /// Call this at app startup after InitializeLibrary.
    /// </summary>
    public async Task<List<RestoredSearchInfo>> RestoreActiveSearchesAsync(string jamlFiltersDir)
    {
        var restored = new List<RestoredSearchInfo>();

        if (!_platformServices.SupportsFileSystem)
        {
            DebugLogger.Log("SearchManager", "Skipping search restoration (browser)");
            return restored;
        }

        try
        {
            // Get all search IDs marked as active
            var activeIds = await MultiSearchManager.Instance.RestoreActiveSearchesAsync();
            DebugLogger.Log("SearchManager", $"Found {activeIds.Count} active searches to restore");

            foreach (var searchId in activeIds)
            {
                try
                {
                    var meta = MultiSearchManager.Instance.GetPersistedMeta(searchId);
                    if (meta is null)
                    {
                        DebugLogger.Log("SearchManager", $"No metadata for search: {searchId}");
                        continue;
                    }

                    // Try to load the JAML config
                    var jamlPath = Path.Combine(jamlFiltersDir, $"{meta.JamlFilter}.jaml");
                    if (!File.Exists(jamlPath))
                    {
                        DebugLogger.Log("SearchManager", $"JAML not found: {jamlPath}");
                        // Mark as inactive since we can't restore
                        SequentialLibrary.Instance.SetSearchActive(searchId, false);
                        continue;
                    }

                    if (!JamlConfigLoader.TryLoadFromJaml(jamlPath, out var config, out var error) || config is null)
                    {
                        DebugLogger.LogError("SearchManager", $"Failed to load JAML: {error}");
                        SequentialLibrary.Instance.SetSearchActive(searchId, false);
                        continue;
                    }

                    // Apply deck/stake from metadata
                    config.Deck = meta.Deck;
                    config.Stake = meta.Stake;

                    restored.Add(new RestoredSearchInfo
                    {
                        SearchId = searchId,
                        FilterName = meta.JamlFilter ?? "Unknown",
                        Deck = meta.Deck ?? "Red",
                        Stake = meta.Stake ?? "White",
                        LastSeed = meta.LastSeed,
                        TotalSeedsProcessed = meta.TotalSeedsProcessed,
                        TotalMatches = meta.TotalMatches,
                        Config = config,
                    });

                    DebugLogger.Log("SearchManager", $"Prepared restoration for: {searchId}");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchManager", $"Error restoring search {searchId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Error during search restoration: {ex.Message}");
        }

        return restored;
    }

    /// <summary>
    /// Resume a restored search. Call this after UI is ready.
    /// </summary>
    public ActiveSearchContext? ResumeSearch(RestoredSearchInfo info, int threads)
    {
        try
        {
            var criteria = new SearchCriteria
            {
                ThreadCount = threads,
                BatchSize = 1000,
                Deck = info.Deck,
                Stake = info.Stake,
            };

            // Calculate StartBatch from LastSeed if available
            if (!string.IsNullOrEmpty(info.LastSeed))
            {
                // Use SeedMath to convert seed to batch index
                // BatchSize in SearchCriteria is seed digits (1-7), default 3
                criteria.StartBatch = (ulong)Motely.SeedMath.SeedToBatchIndex(info.LastSeed, criteria.BatchSize) + 1;
            }

            var context = StartSearch(criteria, info.Config);
            DebugLogger.Log("SearchManager", $"Resumed search: {info.SearchId}");
            return context;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Failed to resume search {info.SearchId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Stop all active searches
    /// </summary>
    public void StopAllSearches()
    {
        foreach (var context in _activeSearches.Values.ToList())
        {
            try
            {
                context.Stop();
                context.Dispose();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchManager", $"Error stopping search {context.SearchId}: {ex.Message}");
            }
        }
        _activeSearches.Clear();
        DebugLogger.Log("SearchManager", "All searches stopped");
    }

    /// <summary>
    /// Stop all searches for a specific filter
    /// </summary>
    /// <returns>Number of searches stopped</returns>
    public int StopSearchesForFilter(string filterId)
    {
        var toRemove = _activeSearches.Values
            .Where(c => c.FilterId == filterId || c.FilterName == filterId)
            .Select(c => c.SearchId)
            .ToList();

        foreach (var searchId in toRemove)
        {
            if (_activeSearches.TryRemove(searchId, out var context))
            {
                try
                {
                    context.Stop();
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchManager", $"Error stopping search {searchId}: {ex.Message}");
                }
            }
        }
        DebugLogger.Log("SearchManager", $"Stopped {toRemove.Count} searches for filter: {filterId}");
        return toRemove.Count;
    }

    /// <summary>
    /// Run a quick search for validation/testing purposes
    /// </summary>
    public async Task<QuickSearchResult> RunQuickSearchAsync(SearchCriteria criteria, MotelyJsonConfig config)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var context = StartSearch(criteria, config);
            
            // Start the search
            context.Start();
            
            // Wait for completion or timeout (quick searches should be fast)
            var maxWait = TimeSpan.FromSeconds(30);
            var waited = TimeSpan.Zero;
            var pollInterval = TimeSpan.FromMilliseconds(100);
            
            while (context.IsRunning && waited < maxWait)
            {
                await Task.Delay(pollInterval);
                waited += pollInterval;
            }
            
            // Get results
            var results = await context.GetResultsPageAsync(0, 1000);
            var elapsed = DateTime.UtcNow - startTime;
            
            // Clean up
            if (context.IsRunning)
            {
                context.Stop();
            }
            RemoveSearch(context.SearchId);
            context.Dispose();
            
            return new QuickSearchResult
            {
                Success = true,
                Count = results.Count,
                Seeds = results.Select(r => r.Seed).ToList(),
                Results = results,
                ElapsedTime = elapsed.TotalSeconds
            };
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Quick search failed: {ex.Message}");
            var elapsed = DateTime.UtcNow - startTime;
            return new QuickSearchResult
            {
                Success = false,
                Count = 0,
                Seeds = new List<string>(),
                ElapsedTime = elapsed.TotalSeconds,
                Error = ex.Message
            };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopAllSearches();
    }
}

/// <summary>
/// Information about a restored search that needs UI widgets created.
/// </summary>
public class RestoredSearchInfo
{
    public string SearchId { get; set; } = "";
    public string FilterName { get; set; } = "";
    public string Deck { get; set; } = "Red";
    public string Stake { get; set; } = "White";
    public string? LastSeed { get; set; }
    public long TotalSeedsProcessed { get; set; }
    public long TotalMatches { get; set; }
    public MotelyJsonConfig Config { get; set; } = null!;
}
