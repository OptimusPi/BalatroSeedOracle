namespace Motely.Filters;

public enum MotelySearchStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed,
}

public sealed class BsoSeedScoreTally
{
    public string Seed { get; set; } = "";
    public int Score { get; set; }
    public int[]? Tallies { get; set; }
}

public sealed class SearchOptionsDto
{
    public int ThreadCount { get; set; } = 4;
    public long BatchSize { get; set; } = 1_000_000;
    public long StartBatch { get; set; } = 0;
    public long? MaxBatches { get; set; }
}
