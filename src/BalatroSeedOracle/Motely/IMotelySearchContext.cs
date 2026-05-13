using System;
using System.Collections.Generic;

namespace Motely.Executors;

// BSO-owned re-creation of the IMotelySearchContext surface BSO already calls
// against. Upstream Motely no longer exposes a wrapping search context
// (`Motely.Orchestration` was dissolved); this is the seam BSO needs in order
// to keep the UI talking to a stable shape while the JamlConfig-driven engine
// is wired up underneath.
public enum MotelySearchStatus
{
    Created,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed,
}

public sealed class MotelySearchResultRow
{
    public string Seed { get; set; } = string.Empty;
    public int Score { get; set; }
    public int[]? Tallies { get; set; }
}

public interface IMotelySearchContext : IDisposable
{
    string SearchId { get; }
    string FilterId { get; }
    MotelySearchStatus Status { get; }
    TimeSpan ElapsedTime { get; }
    long TotalSeedsSearched { get; }
    long MatchingSeeds { get; }
    long FilteredSeeds { get; }
    int ResultCount { get; }
    IReadOnlyList<string> ColumnNames { get; }
    IList<MotelySearchResultRow> GetResults(int offset, int count);
    IList<MotelySearchResultRow> GetTopResults(int limit = 1000);
    void ExportTo(string outputPath);
    void Start();
    void Pause();
    void Cancel();
}
