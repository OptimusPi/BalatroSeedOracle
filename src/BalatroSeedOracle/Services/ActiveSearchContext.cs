using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.DuckDB;
using Motely;
using Motely.Filters;
using Motely.Reporting;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Minimal context holding Motely search references.
/// Uses IDuckDBService for cross-platform DB access (native on Desktop, WASM on Browser).
/// </summary>
public sealed class ActiveSearchContext : IDisposable
{
    public string SearchId { get; }
    public IMotelySearch Search { get; }
    public MotelyJsonConfig Config { get; }
    public string FilterName => Config.Name ?? SearchId;
    public string DatabasePath { get; }
    public string ConfigPath => ""; // Config is in-memory
    public bool IsRunning => Search.Status == MotelySearchStatus.Running;
    public bool IsPaused => Search.Status == MotelySearchStatus.Paused;
    public TimeSpan ElapsedTime => Search.ElapsedTime;
    public TimeSpan SearchDuration => Search.ElapsedTime;
    public long TotalSeedsSearched => Search.TotalSeedsSearched;
    public long MatchingSeeds => Search.MatchingSeeds;

    private bool _hasNewResults;
    public bool HasNewResultsSinceLastQuery => _hasNewResults;

    public IReadOnlyList<string> ColumnNames =>
        MotelyRunConfig
            .Factory(Config)
            .Columns.Select(c => c.Name)
            .Prepend("score")
            .Prepend("seed")
            .ToList();

    // Events for UI binding
    public event EventHandler<SearchResultEventArgs>? SearchStarted;
    public event EventHandler<SearchResultEventArgs>? SearchCompleted;
    public event EventHandler<SearchProgress>? ProgressUpdated;

    public ActiveSearchContext(
        string searchId,
        IMotelySearch search,
        MotelyJsonConfig config,
        string? dbPath = null
    )
    {
        SearchId = searchId;
        Search = search;
        Config = config;
        DatabasePath = dbPath ?? "";
    }

    public void StopSearch() => Search.Cancel();

    public void Stop() => Search.Cancel();

    public void PauseSearch() => Search.Pause();

    public void Pause() => Search.Pause();

    public void Start() => Search.Start();

    public MotelyJsonConfig? GetFilterConfig() => Config;

    public void AcknowledgeResultsQueried() => _hasNewResults = false;

    internal void MarkNewResults() => _hasNewResults = true;

    /// <summary>
    /// Get result count using cross-platform IDuckDBService
    /// </summary>
    public int ResultCount
    {
        get
        {
            try
            {
                var duckDb = ServiceHelper.GetService<IDuckDBService>();
                if (duckDb == null || string.IsNullOrEmpty(DatabasePath))
                    return 0;

                using var conn = duckDb.OpenConnectionAsync(DatabasePath).GetAwaiter().GetResult();
                return (int)conn.GetRowCountAsync("results").GetAwaiter().GetResult();
            }
            catch
            {
                return 0;
            }
        }
    }

    public Task<int> GetResultCountAsync() => Task.FromResult(ResultCount);

    /// <summary>
    /// Query results using cross-platform IDuckDBService
    /// </summary>
    public async Task<List<SearchResult>> GetResultsPageAsync(int offset, int count)
    {
        var duckDb = ServiceHelper.GetService<IDuckDBService>();
        if (duckDb == null || string.IsNullOrEmpty(DatabasePath))
            return new List<SearchResult>();

        try
        {
            await using var conn = await duckDb.OpenConnectionAsync(DatabasePath);
            var sql = $"SELECT * FROM results ORDER BY score DESC LIMIT {count} OFFSET {offset}";
            var (columns, rows) = await conn.ExecuteSqlAsync(sql);

            var runConfig = MotelyRunConfig.Factory(Config);
            return rows.Select(r => new SearchResult
                {
                    Seed = r.TryGetValue("seed", out var s) ? s?.ToString() ?? "" : "",
                    TotalScore = r.TryGetValue("score", out var sc) ? Convert.ToInt32(sc) : 0,
                    Scores = runConfig
                        .Columns.Where(c => c.Type == ColumnType.ScoreTally)
                        .Select(c => r.TryGetValue(c.Name, out var v) ? Convert.ToInt32(v) : 0)
                        .ToArray(),
                })
                .ToList();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(
                "ActiveSearchContext",
                $"GetResultsPageAsync failed: {ex.Message}"
            );
            return new List<SearchResult>();
        }
    }

    /// <summary>
    /// Query top results using cross-platform IDuckDBService
    /// </summary>
    public async Task<List<SearchResult>> GetTopResultsAsync(
        string orderBy,
        bool ascending,
        int limit
    )
    {
        var duckDb = ServiceHelper.GetService<IDuckDBService>();
        if (duckDb == null || string.IsNullOrEmpty(DatabasePath))
            return new List<SearchResult>();

        try
        {
            await using var conn = await duckDb.OpenConnectionAsync(DatabasePath);
            var direction = ascending ? "ASC" : "DESC";
            var sql = $"SELECT * FROM results ORDER BY {orderBy} {direction} LIMIT {limit}";
            var (columns, rows) = await conn.ExecuteSqlAsync(sql);

            var runConfig = MotelyRunConfig.Factory(Config);
            return rows.Select(r => new SearchResult
                {
                    Seed = r.TryGetValue("seed", out var s) ? s?.ToString() ?? "" : "",
                    TotalScore = r.TryGetValue("score", out var sc) ? Convert.ToInt32(sc) : 0,
                    Scores = runConfig
                        .Columns.Where(c => c.Type == ColumnType.ScoreTally)
                        .Select(c => r.TryGetValue(c.Name, out var v) ? Convert.ToInt32(v) : 0)
                        .ToArray(),
                })
                .ToList();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("ActiveSearchContext", $"GetTopResultsAsync failed: {ex.Message}");
            return new List<SearchResult>();
        }
    }

    public void Dispose()
    {
        Search.Dispose();
    }
}
