using System;

namespace Motely.Executors;

// Holds a global thread budget for any future multi-search coordination.
// The orchestrator + IMotelySearchContext machinery from the revive-app branch
// was stripped because it conflicted with BSO's JamlRootDocument-based pipeline;
// reintroduce on top of JamlSearchBuilder when the new sink lands.
public sealed class MultiSearchManager
{
    public static MultiSearchManager Instance { get; } = new();
    public int TotalThreads { get; private set; } = Environment.ProcessorCount;
    public void SetTotalThreads(int threads) => TotalThreads = Math.Max(1, threads);
    public void StopAll() { /* no active multi-search registry yet */ }
}
