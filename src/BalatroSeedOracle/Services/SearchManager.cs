using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely;
using Motely.Executors;
using Motely.Filters;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Thin wrapper around Motely.Orchestration for BSO's UI.
/// Delegates to MotelySearchOrchestrator - NO reimplementation.
/// </summary>
public class SearchManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ActiveSearchContext> _searches = new();
    private readonly EventFXService? _eventFXService;

    public SearchManager()
    {
        _eventFXService = ServiceHelper.GetService<EventFXService>();
    }

    public ActiveSearchContext? GetSearch(string searchId)
    {
        _searches.TryGetValue(searchId, out var ctx);
        return ctx;
    }

    public ActiveSearchContext? GetSearch(string filterName, string deck, string stake)
    {
        var searchId = $"{filterName}_{deck}_{stake}";
        return GetSearch(searchId);
    }

    public IEnumerable<ActiveSearchContext> GetActiveSearches() =>
        _searches.Values.Where(s => s.IsRunning);

    public IEnumerable<ActiveSearchContext> GetAllSearches() => _searches.Values;

    public async Task<ActiveSearchContext> StartSearchAsync(
        SearchCriteria criteria,
        MotelyJsonConfig config
    )
    {
        var filterName = config.Name?.Replace(" ", "_") ?? "unknown";
        var searchId = $"{filterName}_{criteria.Deck ?? "Red"}_{criteria.Stake ?? "White"}";
        var dbPath = Path.Combine(AppPaths.SearchResultsDir, $"{searchId}.db");

        // Stop existing search with same ID
        if (_searches.TryRemove(searchId, out var existing))
        {
            existing.Stop();
            existing.Dispose();
        }

        // Ensure directory exists
        Directory.CreateDirectory(AppPaths.SearchResultsDir);

        // Build params for Motely
        var searchParams = new JsonSearchParams
        {
            Threads = criteria.ThreadCount > 0 ? criteria.ThreadCount : Environment.ProcessorCount,
            BatchSize = criteria.BatchSize > 0 ? criteria.BatchSize : 3,
            StartBatch = (ulong)criteria.StartBatch,
            EndBatch = criteria.EndBatch > 0 ? (ulong)criteria.EndBatch : 0,
            OutputDbPath = dbPath,
            ProgressCallback = progress =>
            {
                // Progress is handled by Motely internally
                DebugLogger.Log("SearchManager", $"Progress: {progress.PercentComplete:F2}%");
            },
        };

        // Apply seed source based on search mode
        if (!string.IsNullOrEmpty(criteria.DebugSeed))
        {
            searchParams.SeedList = new List<string> { criteria.DebugSeed };
        }
        else if (!string.IsNullOrEmpty(criteria.WordList))
        {
            searchParams.SeedSources = criteria.WordList;
        }
        else if (!string.IsNullOrEmpty(criteria.DbList))
        {
            searchParams.SeedSources = criteria.DbList;
        }

        // Launch via Motely Orchestrator
        var motelySearch = MotelySearchOrchestrator.Launch(config, searchParams);

        // ActiveSearchContext uses IDuckDBService for cross-platform DB access
        var context = new ActiveSearchContext(searchId, motelySearch, config, dbPath);

        _searches[searchId] = context;

        // Trigger EventFX
        _eventFXService?.TriggerEvent(EventFXType.SearchInstanceStart);

        // Start the search
        await Task.Run(() => context.Start());

        DebugLogger.Log("SearchManager", $"Started search: {searchId}");
        return context;
    }

    public void StopSearch(string searchId)
    {
        if (_searches.TryGetValue(searchId, out var ctx))
        {
            ctx.Stop();
            DebugLogger.Log("SearchManager", $"Stopped search: {searchId}");
        }
    }

    public void StopAllSearches()
    {
        foreach (var ctx in _searches.Values)
        {
            ctx.Stop();
        }
    }

    public int StopSearchesForFilter(string filterName)
    {
        var count = 0;
        var toRemove = _searches.Keys.Where(k => k.StartsWith($"{filterName}_")).ToList();

        foreach (var key in toRemove)
        {
            if (_searches.TryRemove(key, out var ctx))
            {
                ctx.Stop();
                ctx.Dispose();
                count++;
            }
        }
        return count;
    }

    public bool RemoveSearch(string searchId)
    {
        if (_searches.TryRemove(searchId, out var ctx))
        {
            ctx.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets or restores a search from an existing database file
    /// </summary>
    public ActiveSearchContext? GetOrRestoreSearch(string searchInstanceId)
    {
        // First check if already in memory
        if (_searches.TryGetValue(searchInstanceId, out var existing))
            return existing;

        // Check if database file exists to restore from
        var dbPath = Path.Combine(AppPaths.SearchResultsDir, $"{searchInstanceId}.db");
        if (!File.Exists(dbPath))
        {
            DebugLogger.Log(
                "SearchManager",
                $"Cannot restore search '{searchInstanceId}' - DB not found"
            );
            return null;
        }

        // Cannot fully restore without the config - return null for now
        // A proper restoration would require storing config in the DB or separately
        DebugLogger.Log(
            "SearchManager",
            $"DB exists for '{searchInstanceId}' but cannot restore without config"
        );
        return null;
    }

    /// <summary>
    /// Quick synchronous search for filter testing
    /// </summary>
    public async Task<QuickSearchResults> RunQuickSearchAsync(
        SearchCriteria criteria,
        MotelyJsonConfig config
    )
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<SearchResult>();

        try
        {
            var maxResults = criteria.MaxResults > 0 ? criteria.MaxResults : 10;
            var tempId = $"QuickTest_{Guid.NewGuid():N}";
            var tempDbPath = Path.Combine(AppPaths.TempDir, $"{tempId}.db");

            Directory.CreateDirectory(AppPaths.TempDir);

            var searchParams = new JsonSearchParams
            {
                Threads = Math.Max(1, criteria.ThreadCount),
                BatchSize = criteria.BatchSize > 0 ? criteria.BatchSize : 3,
                OutputDbPath = tempDbPath,
            };

            if (!string.IsNullOrEmpty(criteria.DebugSeed))
            {
                searchParams.SeedList = new List<string> { criteria.DebugSeed };
            }

            var search = MotelySearchOrchestrator.Launch(config, searchParams);
            search.Start();

            // Wait with timeout
            await Task.Delay(5000);

            search.Cancel();

            // Get results from temp DB using cross-platform IDuckDBService
            var duckDb = ServiceHelper.GetService<DuckDB.IDuckDBService>();
            if (duckDb != null && File.Exists(tempDbPath))
            {
                try
                {
                    await using var conn = await duckDb.OpenConnectionAsync(tempDbPath);
                    var sql =
                        $"SELECT seed, score FROM results ORDER BY score DESC LIMIT {maxResults}";
                    var (columns, rows) = await conn.ExecuteSqlAsync(sql);
                    results = rows.Select(r => new SearchResult
                        {
                            Seed = r.TryGetValue("seed", out var s) ? s?.ToString() ?? "" : "",
                            TotalScore = r.TryGetValue("score", out var sc)
                                ? Convert.ToInt32(sc)
                                : 0,
                        })
                        .ToList();
                }
                finally
                {
                    // Cleanup
                    try
                    {
                        File.Delete(tempDbPath);
                    }
                    catch { }
                }
            }

            stopwatch.Stop();
            return new QuickSearchResults
            {
                Seeds = results.Select(r => r.Seed).ToList(),
                Results = results,
                Count = results.Count,
                ElapsedTime = stopwatch.Elapsed.TotalSeconds,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            DebugLogger.LogError("SearchManager", $"Quick search failed: {ex.Message}");
            return new QuickSearchResults
            {
                Seeds = new List<string>(),
                Results = new List<SearchResult>(),
                Count = 0,
                ElapsedTime = stopwatch.Elapsed.TotalSeconds,
                Success = false,
                Error = ex.Message,
            };
        }
    }

    public void Dispose()
    {
        StopAllSearches();
        foreach (var ctx in _searches.Values)
        {
            ctx.Dispose();
        }
        _searches.Clear();
    }
}

/// <summary>
/// Results from a quick test search
/// </summary>
public class QuickSearchResults
{
    public List<string> Seeds { get; set; } = new();
    public List<SearchResult> Results { get; set; } = new();
    public int Count { get; set; }
    public int BatchesChecked { get; set; }
    public double ElapsedTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = "";
}
