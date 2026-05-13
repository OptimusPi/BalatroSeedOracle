using System;
using System.Threading;
using Motely.Filters;

namespace Motely.Executors;

// Lifted from upstream Motely (removed when Motely.Orchestration was dissolved).
// BSO still keeps it as the parameter object handed to the bridge that translates
// MotelyJsonConfig → JamlConfig and runs a JamlSearchBuilder-driven search.
public enum ScoreCutoffMode
{
    None = 0,
    Manual = 1,
    AutoBest = 2,
    AutoSmart = 3,
}

public sealed class JsonSearchParams
{
    public int Threads { get; set; } = 1;
    public int BatchCharCount { get; set; } = 4;
    public ulong StartBatch { get; set; }
    public ulong EndBatch { get; set; }
    public string? SpecificSeed { get; set; }
    public int RandomSeeds { get; set; }
    public bool PalindromeSeeds { get; set; }
    public int Cutoff { get; set; }
    public ScoreCutoffMode CutoffMode { get; set; }
    public bool Quiet { get; set; }
    public bool NoFancy { get; set; }
    public string? OutputDbPath { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public Action<MotelySeedScoreTally>? ResultCallback { get; set; }

    // Convenience aliases used by BSO call sites.
    public int BatchSize
    {
        get => BatchCharCount;
        set => BatchCharCount = value;
    }

    public bool EnableDebug { get; set; }
    public string? Deck { get; set; }
    public string? Stake { get; set; }
}
