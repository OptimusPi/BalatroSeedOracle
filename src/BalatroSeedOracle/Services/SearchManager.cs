using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using Motely;
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

        // JUST CALL MOTELY. That's it.
        // Motely owns everything: SearchId, FilterId, database, queries.
        var motelyContext = MotelySearchOrchestrator.LaunchWithContext(
            config,
            searchParams,
            useInMemoryStorage);

        DebugLogger.Log("SearchManager", $"Search started - SearchId: {motelyContext.SearchId}, FilterId: {motelyContext.FilterId}");

        // Wrap in ActiveSearchContext for UI binding
        var context = new ActiveSearchContext(motelyContext, config);
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
    /// Start a new search (async version for compatibility)
    /// </summary>
    public Task<ActiveSearchContext> StartSearchAsync(SearchCriteria criteria, MotelyJsonConfig config)
    {
        var context = StartSearch(criteria, config);
        return Task.FromResult(context);
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
            var results = await context.GetResultsPageAsync(0, criteria.MaxResults > 0 ? criteria.MaxResults : 1000);
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
