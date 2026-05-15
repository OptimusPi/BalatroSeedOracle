using System;
using System.Collections.Generic;

namespace Motely;

public sealed class SearchOptionsDto
{
    public int? ThreadCount { get; set; }
    public int? BatchSize { get; set; }
    public long? StartBatch { get; set; }
    public long? EndBatch { get; set; }
    public string? SpecificSeed { get; set; }
    public string? StartSeed { get; set; }
}

public sealed class JsonSearchParams
{
    public int Threads { get; set; } = Environment.ProcessorCount;
    public int BatchSize { get; set; } = 3;
    public ulong StartBatch { get; set; } = 0;
    public ulong EndBatch { get; set; } = ulong.MaxValue;
    public string? SpecificSeed { get; set; }
    public bool EnableDebug { get; set; }
    public string? Deck { get; set; }
    public string? Stake { get; set; }
    public Action<BsoSeedScoreTally>? ResultCallback { get; set; }
}

public sealed class BsoSeedScoreTally
{
    public string Seed { get; set; } = "";
    public int Score { get; set; }
    public int[]? Tallies { get; set; }
}

public enum MotelySearchStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed,
}
