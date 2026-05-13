using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using Motely.Filters;

namespace Motely.Executors;

// Bridge between BSO's MotelyJsonConfig and the new MotelyJAML search pipeline.
// Converts to JamlConfig via JamlConfigBridge, compiles a JamlSearchPlan, and
// wraps the resulting IMotelySearch so BSO sees the same IMotelySearchContext
// shape it always has.
public static class MotelySearchOrchestrator
{
    public static IMotelySearchContext LaunchWithContext(
        MotelyJsonConfig config,
        JsonSearchParams parameters,
        bool useInMemoryStorage = false)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!JamlConfigBridge.TryConvertToJaml(config, out var jaml, out var error, out _) || jaml is null)
        {
            DebugLogger.LogError("MotelySearchOrchestrator", $"JAML conversion failed: {error}");
            return new FailedMotelySearchContext(config, error ?? "JAML conversion failed");
        }

        try
        {
            var plan = JamlSearchBuilder.CreatePlan(jaml);
            var settings = plan.Settings
                .WithThreadCount(Math.Max(1, parameters.Threads))
                .WithBatchCharacterCount(Math.Clamp(parameters.BatchCharCount, 1, 7));

            if (parameters.StartBatch > 0 && parameters.StartBatch <= long.MaxValue)
                settings.WithStartBatchIndex((long)parameters.StartBatch);
            if (parameters.EndBatch > 0 && parameters.EndBatch <= long.MaxValue)
                settings.WithEndBatchIndex((long)parameters.EndBatch);

            if (!string.IsNullOrWhiteSpace(parameters.SpecificSeed))
                settings.WithListSearch(new[] { parameters.SpecificSeed.ToUpperInvariant() });
            else if (parameters.RandomSeeds > 0)
                settings.WithRandomSearch(parameters.RandomSeeds);
            else
                settings.WithSequentialSearch();

            var ctx = new LiveMotelySearchContext(config, plan);
            settings.WithScoredResultCallback(ctx.OnResult);
            settings.WithProgressCallback(ctx.OnProgress);
            ctx.AttachSearch(settings.CreateSearch());
            return ctx;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("MotelySearchOrchestrator", $"Failed to build search: {ex.Message}");
            return new FailedMotelySearchContext(config, ex.Message);
        }
    }

    public static void SetRepository(object _) { /* legacy: persistence layer removed */ }
}

internal sealed class LiveMotelySearchContext : IMotelySearchContext
{
    private readonly MotelyJsonConfig _config;
    private readonly JamlSearchPlan _plan;
    private readonly ConcurrentQueue<MotelySearchResultRow> _results = new();
    private readonly CancellationTokenSource _cts = new();
    private IMotelySearch? _search;
    private Task? _runTask;
    private DateTime _startedAt = DateTime.UtcNow;
    private long _matchingSeeds;
    private MotelySearchStatus _status = MotelySearchStatus.Created;

    public LiveMotelySearchContext(MotelyJsonConfig config, JamlSearchPlan plan)
    {
        _config = config;
        _plan = plan;
        SearchId = Guid.NewGuid().ToString("N");
        FilterId = _config.Name ?? SearchId;
    }

    public void AttachSearch(IMotelySearch search) => _search = search;

    public string SearchId { get; }
    public string FilterId { get; }
    public MotelySearchStatus Status => _status;
    public TimeSpan ElapsedTime =>
        _search is not null ? TimeSpan.FromMilliseconds(_search.ElapsedMs) : DateTime.UtcNow - _startedAt;
    public long TotalSeedsSearched => _search?.TotalSeedsSearched ?? 0;
    public long MatchingSeeds => Interlocked.Read(ref _matchingSeeds);
    public long FilteredSeeds => _search?.FilteredSeeds ?? 0;
    public int ResultCount => _results.Count;
    public IReadOnlyList<string> ColumnNames => _plan.TallyLabels;

    public IList<MotelySearchResultRow> GetResults(int offset, int count)
    {
        var snapshot = _results.ToArray();
        if (offset >= snapshot.Length) return Array.Empty<MotelySearchResultRow>();
        return snapshot
            .OrderByDescending(r => r.Score)
            .Skip(offset)
            .Take(count)
            .ToArray();
    }

    public IList<MotelySearchResultRow> GetTopResults(int limit = 1000) =>
        _results.ToArray()
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToArray();

    public void ExportTo(string outputPath) =>
        throw new NotSupportedException("Export sink (Motely.DataLake) is not yet wired in.");

    public void Start()
    {
        if (_search is null) return;
        if (_status != MotelySearchStatus.Created && _status != MotelySearchStatus.Paused) return;
        _status = MotelySearchStatus.Running;
        _startedAt = DateTime.UtcNow;
        _runTask = Task.Run(async () =>
        {
            try
            {
                await _search.RunSearchAsync(_cts.Token).ConfigureAwait(false);
                _status = MotelySearchStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                _status = MotelySearchStatus.Cancelled;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("LiveMotelySearchContext", $"Search threw: {ex.Message}");
                _status = MotelySearchStatus.Failed;
            }
        });
    }

    public void Pause() => _status = MotelySearchStatus.Paused; // engine has no pause primitive yet
    public void Cancel()
    {
        try { _cts.Cancel(); } catch { /* already disposed */ }
        try { _search?.Cancel(); } catch { /* already done */ }
    }

    public void Dispose()
    {
        Cancel();
        _search?.Dispose();
        _cts.Dispose();
    }

    internal void OnResult(MotelySeedScoreTally tally)
    {
        var tallies = tally.Tally;
        var tallyInts = new int[tallies.Length];
        for (int i = 0; i < tallies.Length; i++) tallyInts[i] = tallies[i];
        _results.Enqueue(new MotelySearchResultRow
        {
            Seed = tally.Seed,
            Score = tally.Score,
            Tallies = tallyInts,
        });
        Interlocked.Increment(ref _matchingSeeds);
    }

    internal void OnProgress(MotelyProgress _) { /* placeholder — UI polls via TotalSeedsSearched */ }
}

internal sealed class FailedMotelySearchContext : IMotelySearchContext
{
    private readonly MotelyJsonConfig _config;
    public FailedMotelySearchContext(MotelyJsonConfig config, string reason)
    {
        _config = config;
        SearchId = Guid.NewGuid().ToString("N");
        FilterId = _config.Name ?? SearchId;
        FailureReason = reason;
    }
    public string SearchId { get; }
    public string FilterId { get; }
    public string FailureReason { get; }
    public MotelySearchStatus Status => MotelySearchStatus.Failed;
    public TimeSpan ElapsedTime => TimeSpan.Zero;
    public long TotalSeedsSearched => 0;
    public long MatchingSeeds => 0;
    public long FilteredSeeds => 0;
    public int ResultCount => 0;
    public IReadOnlyList<string> ColumnNames { get; } = Array.Empty<string>();
    public IList<MotelySearchResultRow> GetResults(int offset, int count) => Array.Empty<MotelySearchResultRow>();
    public IList<MotelySearchResultRow> GetTopResults(int limit = 1000) => Array.Empty<MotelySearchResultRow>();
    public void ExportTo(string outputPath) => throw new InvalidOperationException(FailureReason);
    public void Start() { }
    public void Pause() { }
    public void Cancel() { }
    public void Dispose() { }
}

// Restored to satisfy DesktopAppExtensions wiring. Holds a global thread budget;
// the actual multi-search coordination lived in Motely.Orchestration and is being
// reintroduced incrementally alongside the JamlSearchBuilder integration.
public sealed class MultiSearchManager
{
    public static MultiSearchManager Instance { get; } = new();
    public int TotalThreads { get; private set; } = Environment.ProcessorCount;
    public void SetTotalThreads(int threads) => TotalThreads = Math.Max(1, threads);
    public void StopAll() { /* no active multi-search registry yet */ }
}
