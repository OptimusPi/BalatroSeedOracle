using System;
using System.Collections.Generic;
using System.Threading;
using Motely.Filters;

namespace Motely.Executors;

// Bridge that used to live in Motely.Orchestration. The real implementation
// (JamlConfigLoader.TryLoad → JamlSearchBuilder.CreatePlan → MotelySearch.Start)
// is wired in a follow-up. Today it returns a context that surfaces zeroed
// stats and throws on Start so calling code lights up the UI shell without
// pretending a search is in progress.
public static class MotelySearchOrchestrator
{
    public static IMotelySearchContext LaunchWithContext(
        MotelyJsonConfig config,
        JsonSearchParams parameters,
        bool useInMemoryStorage = false)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(parameters);
        return new PendingMotelySearchContext(config, parameters);
    }

    public static void SetRepository(object _) { /* legacy: persistence layer removed */ }
}

internal sealed class PendingMotelySearchContext : IMotelySearchContext
{
    private readonly DateTime _createdAt = DateTime.UtcNow;
    private readonly MotelyJsonConfig _config;

    public PendingMotelySearchContext(MotelyJsonConfig config, JsonSearchParams _)
    {
        _config = config;
        SearchId = Guid.NewGuid().ToString("N");
        FilterId = _config.Name ?? SearchId;
        Status = MotelySearchStatus.Created;
    }

    public string SearchId { get; }
    public string FilterId { get; }
    public MotelySearchStatus Status { get; private set; }
    public TimeSpan ElapsedTime => DateTime.UtcNow - _createdAt;
    public long TotalSeedsSearched => 0;
    public long MatchingSeeds => 0;
    public long FilteredSeeds => 0;
    public int ResultCount => 0;
    public IReadOnlyList<string> ColumnNames { get; } = Array.Empty<string>();

    public IList<MotelySearchResultRow> GetResults(int offset, int count) =>
        Array.Empty<MotelySearchResultRow>();

    public IList<MotelySearchResultRow> GetTopResults(int limit = 1000) =>
        Array.Empty<MotelySearchResultRow>();

    public void ExportTo(string outputPath) =>
        throw new NotSupportedException("Export is not wired in this build.");

    public void Start() => Status = MotelySearchStatus.Running;
    public void Pause() => Status = MotelySearchStatus.Paused;
    public void Cancel() => Status = MotelySearchStatus.Cancelled;
    public void Dispose() { Status = MotelySearchStatus.Cancelled; }
}

// Restored to satisfy DesktopAppExtensions wiring. Holds a global thread budget
// only; the actual multi-search coordination lived in Motely.Orchestration and
// is reintroduced incrementally alongside the JamlSearchBuilder integration.
public sealed class MultiSearchManager
{
    public static MultiSearchManager Instance { get; } = new();
    public int TotalThreads { get; private set; } = Environment.ProcessorCount;
    public void SetTotalThreads(int threads) => TotalThreads = Math.Max(1, threads);
    public void StopAll() { /* no active searches in the stub path */ }
}
