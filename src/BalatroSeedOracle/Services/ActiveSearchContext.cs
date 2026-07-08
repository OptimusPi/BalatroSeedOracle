using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.Services;

public sealed class ActiveSearchContext : IDisposable
{
    private readonly BsoSearchContext? _context;
    private readonly JamlConfig _config;
    private readonly string _searchId;

    private bool _hasNewResults;

    internal ActiveSearchContext(BsoSearchContext context, JamlConfig config)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _searchId = context.SearchId;
    }

    public ActiveSearchContext(string searchId, JamlConfig config)
    {
        _searchId = searchId;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _context = null;
    }

    public string SearchId => _searchId;
    public string FilterId => _context?.FilterId ?? "remote_filter";
    public string FilterName => _config.Name ?? SearchId;
    public JamlConfig Config => _config;
    public string ConfigPath => "";
    public string DatabasePath => "";

    public bool IsRunning => _context?.Status == MotelySearchStatus.Running || (_context == null);
    public bool IsPaused => _context?.Status == MotelySearchStatus.Paused;
    public MotelySearchStatus Status => _context?.Status ?? MotelySearchStatus.Running;
    public TimeSpan ElapsedTime => _context?.ElapsedTime ?? TimeSpan.Zero;
    public TimeSpan SearchDuration => _context?.ElapsedTime ?? TimeSpan.Zero;
    public long TotalSeedsSearched => _context?.TotalSeedsSearched ?? 0;
    public long MatchingSeeds => _context?.MatchingSeeds ?? 0;
    public long FilteredSeeds => _context?.FilteredSeeds ?? 0;

    public int ResultCount => _context?.ResultCount ?? 0;
    public IReadOnlyList<string> ColumnNames => _context?.ColumnNames ?? new List<string>();

    public bool HasNewResultsSinceLastQuery => _hasNewResults;
    public void AcknowledgeResultsQueried() => _hasNewResults = false;
    internal void MarkNewResults() => _hasNewResults = true;

    public async Task<List<SearchResult>> GetResultsPageAsync(int offset, int count)
    {
        if (_context == null) return new List<SearchResult>();
        try
        {
            var results = _context.GetResults(offset, count)
                .Select(r => new SearchResult
                {
                    Seed = r.Seed,
                    TotalScore = r.Score,
                    Scores = r.Tallies ?? Array.Empty<int>(),
                })
                .ToList();
            return await Task.FromResult(results);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("ActiveSearchContext", $"GetResultsPageAsync failed: {ex.Message}");
            return new List<SearchResult>();
        }
    }

    public async Task<List<SearchResult>> GetTopResultsAsync(string orderBy, bool ascending, int limit)
    {
        if (_context == null) return new List<SearchResult>();
        try
        {
            var results = _context.GetTopResults(limit)
                .Select(r => new SearchResult
                {
                    Seed = r.Seed,
                    TotalScore = r.Score,
                    Scores = r.Tallies ?? Array.Empty<int>(),
                })
                .ToList();
            return await Task.FromResult(results);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("ActiveSearchContext", $"GetTopResultsAsync failed: {ex.Message}");
            return new List<SearchResult>();
        }
    }

    public async Task<int> GetResultCountAsync() => await Task.FromResult(ResultCount);

    public void ExportTo(string outputPath)
    {
        if (_context == null)
            throw new InvalidOperationException("Export is not supported for remote searches.");
        _context.ExportTo(outputPath);
    }

    public void Start() => _context?.Start();
    public void Pause() => _context?.Pause();
    public void PauseSearch() => _context?.Pause();
    public void Stop() => _context?.Cancel();
    public void StopSearch() => _context?.Cancel();

    public JamlConfig? GetFilterConfig() => _config;

    public event EventHandler<SearchResultEventArgs>? SearchStarted;
    public event EventHandler<SearchResultEventArgs>? SearchCompleted;
    public event EventHandler<SearchProgress>? ProgressUpdated;
    public event EventHandler<SearchResult>? ResultFound;

    internal void RaiseSearchStarted() => SearchStarted?.Invoke(this, new SearchResultEventArgs());
    internal void RaiseSearchCompleted() => SearchCompleted?.Invoke(this, new SearchResultEventArgs());
    internal void RaiseProgressUpdated(SearchProgress progress) =>
        ProgressUpdated?.Invoke(this, progress);
    internal void RaiseResultFound(SearchResult result) => ResultFound?.Invoke(this, result);

    public void Dispose()
    {
        _context?.Dispose();
    }
}
