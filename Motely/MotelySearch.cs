using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Motely.Filters;

namespace Motely;

public interface IMotelySeedFilter
{
    public VectorMask Filter(ref MotelyVectorSearchContext searchContext);
}

public interface IMotelySeedFilterDesc
{
    public IMotelySeedFilter CreateFilter(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedFilterDesc<TFilter> : IMotelySeedFilterDesc
    where TFilter : struct, IMotelySeedFilter
{
    public new TFilter CreateFilter(ref MotelyFilterCreationContext ctx);

    IMotelySeedFilter IMotelySeedFilterDesc.CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        return CreateFilter(ref ctx);
    }
}

public interface IMotelySeedScoreDesc
{
    public IMotelySeedScoreProvider CreateScoreProvider(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedScoreDesc<TScoreProvider> : IMotelySeedScoreDesc
    where TScoreProvider : struct, IMotelySeedScoreProvider
{
    public new TScoreProvider CreateScoreProvider(ref MotelyFilterCreationContext ctx);

    IMotelySeedScoreProvider IMotelySeedScoreDesc.CreateScoreProvider(
        ref MotelyFilterCreationContext ctx
    )
    {
        return CreateScoreProvider(ref ctx);
    }
}

public interface IMotelySeedScores
{
    string Seed { get; }
    int Score { get; }
    byte[] Tally { get; }
}

public interface IMotelySeedScoreProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask Score(
        ref MotelyVectorSearchContext searchContext,
        MotelyScoredSeedResult[] buffer,
        VectorMask baseFilterMask,
        int scoreThreshold = 0
    );
}

// ── Seed analyze provider ─────────────────────────────────────────────────────
// Twin of the score provider: runs per-seed AFTER the filter (and after scoring, on the
// survivors), on the SAME MotelyVectorSearchContext — so a 1-thread search can produce the full
// per-seed breakdown inline. Tap a found seed → its data already rode along, no second interop.
public interface IMotelySeedAnalyzeDesc
{
    public IMotelySeedAnalyzeProvider CreateAnalyzeProvider(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedAnalyzeDesc<TAnalyzeProvider> : IMotelySeedAnalyzeDesc
    where TAnalyzeProvider : struct, IMotelySeedAnalyzeProvider
{
    public new TAnalyzeProvider CreateAnalyzeProvider(ref MotelyFilterCreationContext ctx);

    IMotelySeedAnalyzeProvider IMotelySeedAnalyzeDesc.CreateAnalyzeProvider(
        ref MotelyFilterCreationContext ctx
    )
    {
        return CreateAnalyzeProvider(ref ctx);
    }
}

public interface IMotelySeedAnalyzeProvider
{
    // Analyze the seeds in baseFilterMask, firing the provider's own per-seed callback. No return
    // mask: analysis never gates the search — it observes the survivors and emits their breakdowns.
    void Analyze(ref MotelyVectorSearchContext searchContext, VectorMask baseFilterMask);
}

public interface IMotelySeedRouter
{
    public void InjectSingleSeedContext(in MotelySingleSearchContext ctx);
}

public interface IMotelySeedRouterDesc
{
    public IMotelySeedRouter CreateSeedRouter(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedRouterDesc<TProvider> : IMotelySeedRouterDesc
    where TProvider : struct, IMotelySeedRouter
{
    public new TProvider CreateSeedRouter(ref MotelyFilterCreationContext ctx);

    IMotelySeedRouter IMotelySeedRouterDesc.CreateSeedRouter(ref MotelyFilterCreationContext ctx)
    {
        return CreateSeedRouter(ref ctx);
    }
}

public enum MotelySearchMode
{
    Sequential,
    Provider,
}

// IMotelySeedProvider and its implementations live in Motely.SeedProviders (Motely/SeedProviders/MotelySeedProviders.cs).

public interface IMotelySearchSettings
{
    // BaseFilterDescBase and AdditionalFilters live on MotelySearchSettings<TBaseFilter> only.
    // They expose IMotelySeedFilterDesc, whose CreateFilter takes a ref parameter, so no boundary
    // that copies values can carry them — and a caller that wants to read back the composed filter
    // chain is a native caller by definition.
    IMotelySearchSettings WithAdditionalFilter(IMotelySeedFilterDesc filterDesc);
    IMotelySearchSettings WithThreadCount(int threadCount);
    IMotelySearchSettings WithBatchCharacterCount(int batchCharacterCount);
    IMotelySearchSettings WithProviderBatchSeedCount(int seedCount);
    IMotelySearchSettings WithStartBatchIndex(long startBatchIndex);
    IMotelySearchSettings WithEndBatchIndex(long endBatchIndex);
    IMotelySearchSettings WithSeedScoreProvider(IMotelySeedScoreDesc seedScoreDesc);
    IMotelySearchSettings WithSeedAnalyzeProvider(IMotelySeedAnalyzeDesc seedAnalyzeDesc);
    IMotelySearchSettings WithSeedRouter(IMotelySeedRouterDesc desc);

    // WithSeedGenerator lives on MotelySearchSettings<TBaseFilter> only. A lazy sequence has no
    // value to hand a serializer, and the two doors that do cross cover what a remote caller
    // needs: WithSeedList for a materialized list, WithProviderSearch for a generated space
    // (aesthetic, keyword, lake) whose length the provider already knows.
    /// <summary>
    /// A materialized seed list — every seed known, count exact. This is the shape that can cross
    /// a serialization boundary, since a deferred sequence has no value to hand a serializer.
    /// </summary>
    IMotelySearchSettings WithSeedList(string[] seeds);
    IMotelySearchSettings WithRandomSearch(int count);
    IMotelySearchSettings WithAestheticSearch(
        JamlAesthetic aesthetic,
        char[]? paddingAlphabet = null
    );
    IMotelySearchSettings WithProviderSearch(IMotelySeedProvider provider);
    IMotelySearchSettings WithSequentialSearch();
    IMotelySearchSettings WithDeck(MotelyDeck deck);
    IMotelySearchSettings WithStake(MotelyStake stake);
    IMotelySearchSettings WithProgressCallback(Action<MotelyProgress> callback);
    IMotelySearchSettings WithProgressReportIntervalMs(long intervalMs);
    IMotelySearchSettings WithCsvOutput(bool csvOutput);
    IMotelySearchSettings WithQuietMode(bool quietMode);
    IMotelySearchSettings WithSeedMatchCallback(Action<string> callback);
    IMotelySearchSettings WithScoredResultCallback(Action<MotelyScoredSeedResult> callback);
    IMotelySearchSettings WithAutoScoreCutoff(bool enabled = true);
    IMotelySearchSettings StopAfter(long matchCount);

    // WithJimmolate lives on MotelySearchSettings<TBaseFilter> only, alongside
    // WithSeedGenerator and AdditionalFilters. Its MotelyIndividualSeedSearcher delegate is the
    // only thing that surfaces MotelySingleSearchContext to the boundary, and renaming the member
    // away removes it from JavaScript while leaving the delegate's declaration behind, pointing at
    // a type with no JS node — Bootsharp's DeclarationGenerator then throws building the delegate's
    // argument. A member that cannot cross leaves the contract; erasure alone is not enough.
    // The feature itself is unchanged for native callers: JAML is what crosses a boundary.

    IMotelySearch CreateSearch();
    IMotelySearch Start(CancellationToken cancellationToken = default);
}

public sealed class MotelySearchSettings<TBaseFilter>(
    IMotelySeedFilterDesc<TBaseFilter> baseFilterDesc
) : IMotelySearchSettings
    where TBaseFilter : struct, IMotelySeedFilter
{
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public long StartBatchIndex { get; set; } = 0;
    public long EndBatchIndex { get; set; } = long.MaxValue;

    public IMotelySeedFilterDesc<TBaseFilter> BaseFilterDesc { get; set; } = baseFilterDesc;

    /// <summary>The base desc as the non-generic engine type. Native callers only.</summary>
    public IMotelySeedFilterDesc BaseFilterDescBase => BaseFilterDesc;

    public IList<IMotelySeedFilterDesc>? AdditionalFilters { get; set; } = null;

    public IMotelySeedScoreDesc? SeedScoreDesc { get; set; } = null;

    public IMotelySeedAnalyzeDesc? SeedAnalyzeDesc { get; set; } = null;

    public IMotelySeedRouterDesc? SeedRouterDesc { get; set; } = null;

    public MotelySearchMode Mode { get; set; }

    /// <summary>
    /// The object which provides seeds to search. Should only be non-null if
    /// `Mode` is set to `Provider`.
    /// </summary>
    public IMotelySeedProvider? SeedProvider { get; set; }

    /// <summary>
    /// The number of seed characters each sequential batch contains.
    ///
    /// For example, with a value of 3 one batch would go through 35^3 seeds.
    /// Only meaningful when `Mode` is set to `Sequential` — partial-hash cache benefit.
    /// </summary>
    public int SequentialBatchCharacterCount { get; set; } = 3;

    /// <summary>
    /// How many seeds a provider-mode plan processes before one progress report / cutoff gate.
    /// SIMD width stays <see cref="MotelyGlobals.MaxVectorWidth"/> (8); this only amortizes
    /// interop and chatter over a large provider stream (aesthetics, keywords, lakes).
    /// Default <see cref="MotelyGlobals.DefaultProviderBatchSeedCount"/> (35³). Cap at 35⁴ if needed.
    /// </summary>
    public int ProviderBatchSeedCount { get; set; } = MotelyGlobals.DefaultProviderBatchSeedCount;

    public MotelyDeck Deck { get; set; } = MotelyDeck.Red;
    public MotelyStake Stake { get; set; } = MotelyStake.White;

    public bool CsvOutput { get; set; } = false;
    public bool QuietMode { get; set; } = false;
    public bool AutoScoreCutoff { get; set; } = false;

    /// <summary>
    /// Stop searching once AT LEAST this many seeds have matched; 0 (the default) searches the
    /// whole space. "At least" is the honest contract, not a promise of exactly N: a batch scores
    /// all 8 SIMD lanes before anyone checks for cancellation, and every worker thread is mid-batch
    /// when the limit trips, so <c>StopAfter(1)</c> routinely delivers a handful. Callers who want
    /// one seed should take the first result, not assume they received a single one.
    /// </summary>
    public long StopAfterMatches { get; set; } = 0;

    /// <summary>
    /// Callback for progress updates - useful for UI progress bars and logging
    /// Receives MotelyProgress object with all progress data
    /// </summary>
    public Action<MotelyProgress>? ProgressCallback { get; set; }

    public long ProgressReportIntervalMs { get; set; } = 800;

    /// <summary>
    /// Callback invoked when a seed matches all filters (no score provider).
    /// Consumers provide their own output handler (e.g. Console.WriteLine for CLI).
    /// </summary>
    public Action<string>? SeedMatchCallback { get; set; }

    /// <summary>
    /// Callback invoked for scored result rows so callers can persist structured results.
    /// </summary>
    public Action<MotelyScoredSeedResult>? ScoredResultCallback { get; set; }

    public MotelySearchSettings<TBaseFilter> WithThreadCount(int threadCount)
    {
        ThreadCount = threadCount;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithStartBatchIndex(long startBatchIndex)
    {
        StartBatchIndex = startBatchIndex;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithEndBatchIndex(long endBatchIndex)
    {
        EndBatchIndex = endBatchIndex;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithBatchCharacterCount(int batchCharacterCount)
    {
        SequentialBatchCharacterCount = batchCharacterCount;
        return this;
    }

    /// <summary>
    /// Provider-mode only: seeds per progress/report batch (not SIMD width). See
    /// <see cref="ProviderBatchSeedCount"/>.
    /// </summary>
    public MotelySearchSettings<TBaseFilter> WithProviderBatchSeedCount(int seedCount)
    {
        if (seedCount < MotelyGlobals.MaxVectorWidth)
            seedCount = MotelyGlobals.MaxVectorWidth;
        ProviderBatchSeedCount = seedCount;
        return this;
    }

    /// <inheritdoc cref="IMotelySearchSettings.WithSeedGenerator" />
    public MotelySearchSettings<TBaseFilter> WithSeedGenerator(
        IEnumerable<string> seeds,
        int seedCount
    )
    {
        return WithProviderSearch(new MotelySeedListProvider(seeds, seedCount));
    }

    /// <summary>Materialized seed list; count is exact. See <see cref="IMotelySearchSettings.WithSeedList"/>.</summary>
    public MotelySearchSettings<TBaseFilter> WithSeedList(string[] seeds)
    {
        return WithSeedGenerator(seeds, seeds.Length);
    }

    public MotelySearchSettings<TBaseFilter> WithRandomSearch(int count)
    {
        return WithProviderSearch(new MotelyRandomSeedProvider(count));
    }

    public MotelySearchSettings<TBaseFilter> WithAestheticSearch(
        JamlAesthetic aesthetic,
        char[]? paddingAlphabet = null
    )
    {
        return WithProviderSearch(new MotelyAestheticSeedProvider(aesthetic, paddingAlphabet));
    }

    public MotelySearchSettings<TBaseFilter> WithProviderSearch(IMotelySeedProvider provider)
    {
        SeedProvider = provider;
        Mode = MotelySearchMode.Provider;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithSequentialSearch()
    {
        SeedProvider = null;
        Mode = MotelySearchMode.Sequential;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithAdditionalFilter(IMotelySeedFilterDesc filterDesc)
    {
        AdditionalFilters ??= [];
        AdditionalFilters.Add(filterDesc);
        return this;
    }

    // Interface implementation for chained calls
    IMotelySearchSettings IMotelySearchSettings.WithAdditionalFilter(
        IMotelySeedFilterDesc filterDesc
    )
    {
        return WithAdditionalFilter(filterDesc);
    }

    public MotelySearchSettings<TBaseFilter> WithSeedScoreProvider(
        IMotelySeedScoreDesc seedScoreDesc
    )
    {
        SeedScoreDesc = seedScoreDesc;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithSeedAnalyzeProvider(
        IMotelySeedAnalyzeDesc seedAnalyzeDesc
    )
    {
        SeedAnalyzeDesc = seedAnalyzeDesc;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithSeedRouter(IMotelySeedRouterDesc desc)
    {
        SeedRouterDesc = desc;
        return this;
    }

    // Explicit interface implementations for chaining
    IMotelySearchSettings IMotelySearchSettings.WithThreadCount(int threadCount) =>
        WithThreadCount(threadCount);

    IMotelySearchSettings IMotelySearchSettings.WithBatchCharacterCount(int count) =>
        WithBatchCharacterCount(count);

    IMotelySearchSettings IMotelySearchSettings.WithProviderBatchSeedCount(int seedCount) =>
        WithProviderBatchSeedCount(seedCount);

    IMotelySearchSettings IMotelySearchSettings.WithStartBatchIndex(long index) =>
        WithStartBatchIndex(index);

    IMotelySearchSettings IMotelySearchSettings.WithEndBatchIndex(long index) =>
        WithEndBatchIndex(index);

    IMotelySearchSettings IMotelySearchSettings.WithSeedScoreProvider(IMotelySeedScoreDesc desc) =>
        WithSeedScoreProvider(desc);

    IMotelySearchSettings IMotelySearchSettings.WithSeedAnalyzeProvider(
        IMotelySeedAnalyzeDesc desc
    ) => WithSeedAnalyzeProvider(desc);

    IMotelySearchSettings IMotelySearchSettings.WithSeedRouter(IMotelySeedRouterDesc desc) =>
        WithSeedRouter(desc);

    IMotelySearchSettings IMotelySearchSettings.WithSeedList(string[] seeds) => WithSeedList(seeds);

    IMotelySearchSettings IMotelySearchSettings.WithRandomSearch(int count) =>
        WithRandomSearch(count);

    IMotelySearchSettings IMotelySearchSettings.WithAestheticSearch(
        JamlAesthetic aesthetic,
        char[]? paddingAlphabet
    ) => WithAestheticSearch(aesthetic, paddingAlphabet);

    IMotelySearchSettings IMotelySearchSettings.WithProviderSearch(IMotelySeedProvider provider) =>
        WithProviderSearch(provider);

    IMotelySearchSettings IMotelySearchSettings.WithSequentialSearch() => WithSequentialSearch();

    IMotelySearchSettings IMotelySearchSettings.WithDeck(MotelyDeck deck) => WithDeck(deck);

    IMotelySearchSettings IMotelySearchSettings.WithStake(MotelyStake stake) => WithStake(stake);

    IMotelySearchSettings IMotelySearchSettings.WithProgressCallback(
        Action<MotelyProgress> callback
    ) => WithProgressCallback(callback);

    IMotelySearchSettings IMotelySearchSettings.WithProgressReportIntervalMs(long intervalMs) =>
        WithProgressReportIntervalMs(intervalMs);

    IMotelySearchSettings IMotelySearchSettings.WithCsvOutput(bool csvOutput) =>
        WithCsvOutput(csvOutput);

    IMotelySearchSettings IMotelySearchSettings.WithQuietMode(bool quietMode) =>
        WithQuietMode(quietMode);

    IMotelySearchSettings IMotelySearchSettings.WithSeedMatchCallback(Action<string> callback) =>
        WithSeedMatchCallback(callback);

    IMotelySearchSettings IMotelySearchSettings.WithScoredResultCallback(
        Action<MotelyScoredSeedResult> callback
    ) => WithScoredResultCallback(callback);

    IMotelySearchSettings IMotelySearchSettings.WithAutoScoreCutoff(bool enabled) =>
        WithAutoScoreCutoff(enabled);

    IMotelySearchSettings IMotelySearchSettings.StopAfter(long matchCount) =>
        StopAfter(matchCount);

    IMotelySearch IMotelySearchSettings.Start(CancellationToken cancellationToken) =>
        Start(cancellationToken);


    /// <inheritdoc cref="IMotelySearchSettings.CreateSearch" />
    public IMotelySearch CreateSearch() => new MotelySearch<TBaseFilter>(this);

    public MotelySearchSettings<TBaseFilter> WithDeck(MotelyDeck deck)
    {
        Deck = deck;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithStake(MotelyStake stake)
    {
        Stake = stake;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithProgressCallback(Action<MotelyProgress> callback)
    {
        ProgressCallback = callback;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithProgressReportIntervalMs(long intervalMs)
    {
        ProgressReportIntervalMs = Math.Max(0, intervalMs);
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithCsvOutput(bool csvOutput)
    {
        CsvOutput = csvOutput;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithQuietMode(bool quietMode)
    {
        QuietMode = quietMode;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithSeedMatchCallback(Action<string> callback)
    {
        SeedMatchCallback = callback;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithScoredResultCallback(
        Action<MotelyScoredSeedResult> callback
    )
    {
        ScoredResultCallback = callback;
        return this;
    }

    public MotelySearchSettings<TBaseFilter> WithAutoScoreCutoff(bool enabled = true)
    {
        AutoScoreCutoff = enabled;
        return this;
    }

    /// <inheritdoc cref="StopAfterMatches" />
    public MotelySearchSettings<TBaseFilter> StopAfter(long matchCount)
    {
        StopAfterMatches = matchCount;
        return this;
    }

    /// <inheritdoc cref="IMotelySearchSettings.WithJimmolate"/>
    public MotelySearchSettings<TBaseFilter> WithJimmolate(
        MotelyIndividualSeedSearcher searcher,
        int scoreCutoff = 1
    ) => WithAdditionalFilter(new Motely.Filters.Native.JimmolateFilterDesc(searcher, scoreCutoff));

    public IMotelySearch Start(CancellationToken cancellationToken = default)
    {
        MotelySearch<TBaseFilter> search = new(this);

        return search.Start(cancellationToken);
    }
}

public interface IMotelySearch : IDisposable
{
    long ElapsedMs { get; }
    long TotalSeedsSearched { get; }
    long MatchingSeeds { get; }
    long FilteredSeeds { get; }
    bool IsCompleted { get; }
    bool IsSequentialBatchSearch { get; }
    long BatchIndex { get; }
    long CompletedBatchCount { get; }

    /// <summary>
    /// True when the run ended because <see cref="MotelySearchSettings{TBaseFilter}.StopAfterMatches"/>
    /// was reached rather than because the space ran out. Throughput is not a meaningful number for
    /// such a run — it stopped on purpose, mid-batch — so callers should report time-to-find instead
    /// of seeds/sec.
    /// </summary>
    bool StoppedOnMatchLimit { get; }

    IMotelySearch Start(CancellationToken cancellationToken = default);
    Task RunSearchAsync(CancellationToken cancellationToken = default);
    void AwaitCompletion();
    Task WaitForCompletionAsync(CancellationToken cancellationToken = default);
}

internal unsafe interface IInternalMotelySearch : IMotelySearch
{
    internal int PseudoHashKeyLengthCount { get; }
    internal int* PseudoHashKeyLengths { get; }
}

public struct MotelySearchParameters
{
    public MotelyStake Stake;
    public MotelyDeck Deck;
}

public sealed unsafe partial class MotelySearch<TBaseFilter> : IInternalMotelySearch
    where TBaseFilter : struct, IMotelySeedFilter
{
    /// <summary>Shared lock for console output (replaces removed FancyConsole.ConsoleLock).</summary>
    internal static readonly object ConsoleLock = new();

    private readonly MotelySearchParameters _searchParameters;

    internal CancellationToken _cancellationToken = CancellationToken.None;

    // StopAfter: workers already poll _cancellationToken at every batch boundary, so reaching the
    // match limit cancels through that same path rather than adding a second stop signal for every
    // loop to check. The source is linked to the caller's token, so either can end the run.
    private readonly long _stopAfterMatches;
    private long _stopMatchCount;
    private CancellationTokenSource? _stopSource;

    // Distinguishes "stopped because we found what was asked for" from "the caller cancelled".
    // SignalSearchCompleted reports the former as a completed search — hitting the limit is the
    // search succeeding, and a caller awaiting completion must not read it as an aborted run.
    private int _stoppedOnMatchLimit;

    private readonly TaskCompletionSource<bool> _completionSource = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private int _isDisposed;
    private int _hasStarted;

    private readonly TBaseFilter _baseFilter;
    private readonly IMotelySeedFilter[] _additionalFilters;
    private readonly int _pseudoHashKeyLengthCount;
    private readonly bool _isProviderMode;

    private readonly IMotelySeedScoreProvider? _scoreProvider;
    private readonly IMotelySeedAnalyzeProvider? _analyzeProvider;
    private readonly IMotelySeedRouter? _seedRouter;

    // IInternalMotelySearch implementation
    int IInternalMotelySearch.PseudoHashKeyLengthCount => _pseudoHashKeyLengthCount;
    private readonly int* _pseudoHashKeyLengths;
    int* IInternalMotelySearch.PseudoHashKeyLengths => _pseudoHashKeyLengths;

    private readonly long _startBatchIndex;
    private readonly long _endBatchIndex;
    private long _batchIndex;
    private readonly MotelySearchPlan[] _plans;
    private readonly int _threadCount;
    private readonly bool _runInline;

    // True when exactly one thread ever advances _batchIndex: the browser pump (single thread,
    // regardless of how many plans ProcessorCount allocated) or a 1-thread native run. Lets
    // TryExecuteSequentialBatch use a plain increment instead of an atomic it can't contend on.
    private readonly bool _singleBatchConsumer;

    // Native worker threads, kept so Dispose can Join them before freeing the native memory
    // they read — a thread still mid-batch is dereferencing buffers Dispose would otherwise
    // FreeHGlobal out from under it (use-after-free). Null on the browser path: one cooperative
    // pump thread that checks _isDisposed itself, nothing to join.
    private Thread[]? _workerThreads;

    public bool IsCompleted => _completionSource.Task.IsCompleted;
    public bool IsSequentialBatchSearch => !_isProviderMode;

    /// <inheritdoc />
    public bool StoppedOnMatchLimit => Volatile.Read(ref _stoppedOnMatchLimit) != 0;

    public long BatchIndex => _batchIndex;

    // Batches actually completed (aggregated from thread-local counters)
    public long CompletedBatchCount
    {
        get
        {
            long totalBatches = 0;
            for (int i = 0; i < _plans.Length; i++)
            {
                totalBatches += _plans[i].SnapshotBatchesCompleted();
            }

            // In provider mode, _startBatchIndex doesn't apply (batches aren't sequential)
            return _isProviderMode ? totalBatches : _startBatchIndex + totalBatches;
        }
    }

    public long TotalSeedsSearched
    {
        get
        {
            // One path: both modes count seeds they looked at. Deriving sequential from
            // completedBatches * SeedsPerBatch credits a whole batch to a run that stopped inside
            // one, which is wrong by up to 35^batchCharCount per thread.
            long totalSeeds = 0;
            for (int i = 0; i < _plans.Length; i++)
            {
                totalSeeds += _plans[i].SnapshotSeedsSearched();
            }
            return totalSeeds;
        }
    }
    public long MatchingSeeds
    {
        get
        {
            long totalSeeds = 0;
            for (int i = 0; i < _plans.Length; i++)
            {
                totalSeeds += _plans[i].SnapshotMatchingSeeds();
            }
            return totalSeeds;
        }
    }

    /// <summary>
    /// Seeds searched that did not match (base filter rejected, additional filter rejected,
    /// or below score cutoff). Equal to <see cref="TotalSeedsSearched"/> minus
    /// <see cref="MatchingSeeds"/>. WASM consumers rely on this being non-zero during a
    /// run to render rejection bars / throughput stats.
    /// </summary>
    public long FilteredSeeds
    {
        get
        {
            long total = TotalSeedsSearched;
            long matched = MatchingSeeds;
            long diff = total - matched;
            // Snapshots race with workers, so on rare reads matched can momentarily
            // exceed total by a handful of seeds. Clamp to 0 so consumers never see negatives.
            return diff > 0 ? diff : 0;
        }
    }

    public long ElapsedMs => _elapsedTime.ElapsedMilliseconds;
    public TimeSpan ElapsedTime => _elapsedTime.Elapsed;

    /// <summary>
    /// Tries to get the score provider if one was configured.
    /// </summary>
    public bool TryGetScoreProvider([NotNullWhen(true)] out IMotelySeedScoreProvider? scoreProvider)
    {
        scoreProvider = _scoreProvider;
        return scoreProvider != null;
    }

    /// <summary>
    /// Tries to get the analyze provider if one was configured.
    /// </summary>
    public bool TryGetAnalyzeProvider(
        [NotNullWhen(true)] out IMotelySeedAnalyzeProvider? analyzeProvider
    )
    {
        analyzeProvider = _analyzeProvider;
        return analyzeProvider != null;
    }

    /// <summary>
    /// Tries to get the single seed router if configured
    /// </summary>
    public bool TryGetSingleSeedRouter([NotNullWhen(true)] out IMotelySeedRouter? seedRouter)
    {
        seedRouter = _seedRouter;
        return seedRouter != null;
    }

    private readonly Action<MotelyProgress>? _progressCallback;
    private readonly Action<string>? _seedMatchCallback;
    private readonly Action<MotelyScoredSeedResult>? _scoredResultCallback;
    private readonly bool _autoScoreCutoff;
    private readonly long _progressReportIntervalMs;
    /// <summary>Provider-mode: seeds to chew per report batch (SIMD still 8-wide).</summary>
    private readonly int _providerBatchSeedCount;

    private readonly Stopwatch _elapsedTime = new();
    private long _lastProgressReportElapsedMs = -1;
    private long _lastReportSeeds;

    public MotelySearch(MotelySearchSettings<TBaseFilter> settings)
    {
        _isProviderMode = settings.Mode == MotelySearchMode.Provider;
        _searchParameters = new() { Deck = settings.Deck, Stake = settings.Stake };
        _progressCallback = settings.ProgressCallback;
        _progressReportIntervalMs = settings.ProgressReportIntervalMs;
        _seedMatchCallback = settings.SeedMatchCallback;
        _scoredResultCallback = settings.ScoredResultCallback;
        _autoScoreCutoff = settings.AutoScoreCutoff;
        _stopAfterMatches = settings.StopAfterMatches;
        _providerBatchSeedCount = Math.Max(
            MotelyGlobals.MaxVectorWidth,
            settings.ProviderBatchSeedCount
        );

        MotelyFilterCreationContext filterCreationContext = new(in _searchParameters)
        {
            IsAdditionalFilter = false,
            SeedMatchCallback = _seedMatchCallback,
        };

        _baseFilter = settings.BaseFilterDesc.CreateFilter(ref filterCreationContext);

        if (settings.AdditionalFilters == null)
        {
            _additionalFilters = [];
        }
        else
        {
            _additionalFilters = new IMotelySeedFilter[settings.AdditionalFilters.Count];
            for (int i = 0; i < _additionalFilters.Length; i++)
            {
                filterCreationContext.IsAdditionalFilter = true;
                _additionalFilters[i] = settings
                    .AdditionalFilters[i]
                    .CreateFilter(ref filterCreationContext);
            }
        }

        // Create the score provider if one was specified
        if (settings.SeedScoreDesc != null)
        {
            _scoreProvider = settings.SeedScoreDesc.CreateScoreProvider(ref filterCreationContext);
        }

        // Create the analyze provider if one was specified
        if (settings.SeedAnalyzeDesc != null)
        {
            _analyzeProvider = settings.SeedAnalyzeDesc.CreateAnalyzeProvider(
                ref filterCreationContext
            );
        }

        // Create the context provider if one was specified
        if (settings.SeedRouterDesc != null)
        {
            _seedRouter = settings.SeedRouterDesc.CreateSeedRouter(ref filterCreationContext);
        }

        _startBatchIndex = settings.StartBatchIndex;
        _endBatchIndex = settings.EndBatchIndex;

        // Initialize to one BEFORE start since ThreadMain increments BEFORE searching
        // StartBatchIndex is always >= 0 now (defaults to 0)
        _batchIndex = _startBatchIndex - 1;

        int[] pseudohashKeyLengths = [.. filterCreationContext.CachedPseudohashKeyLengths];
        _pseudoHashKeyLengthCount = pseudohashKeyLengths.Length;

        _pseudoHashKeyLengths = (int*)Marshal.AllocHGlobal(sizeof(int) * _pseudoHashKeyLengthCount);
        for (int i = 0; i < _pseudoHashKeyLengthCount; i++)
        {
            _pseudoHashKeyLengths[i] = pseudohashKeyLengths[i];
        }

        _threadCount = Math.Max(1, settings.ThreadCount);
        // A materialized seed list is the whole workload, known up front — there is nothing to
        // pump between. Run it on the calling thread so the synchronous AwaitCompletion() works
        // on every platform, including the browser's single thread where the pump would deadlock
        // it. Generative providers (sequential, random, aesthetic) are open-ended and still pump.
        _runInline = settings.SeedProvider is MotelySeedListProvider;
        _singleBatchConsumer = OperatingSystem.IsBrowser() || _threadCount == 1;
        _plans = new MotelySearchPlan[_threadCount];
        for (int i = 0; i < _threadCount; i++)
        {
            _plans[i] = settings.Mode switch
            {
                MotelySearchMode.Sequential => new MotelySequentialSearchPlan(this, settings, i),
                MotelySearchMode.Provider => new MotelyProviderSearchPlan(this, settings, i),
                _ => throw new InvalidEnumArgumentException(nameof(settings.Mode)),
            };
        }
    }

    private void RunWorkerBody(MotelySearchPlan plan)
    {
        if (_isProviderMode)
        {
            RunProviderPlan(plan);
        }
        else
        {
            plan.ExecuteSequentialPlan();
        }
    }

    /// <summary>Single place to complete <see cref="_completionSource"/> after worker(s) finish (sync path).</summary>
    private void SignalSearchCompleted()
    {
        Thread.MemoryBarrier();
        bool stoppedOnLimit = Volatile.Read(ref _stoppedOnMatchLimit) != 0;
        bool completed =
            Volatile.Read(ref _isDisposed) == 0
            && (stoppedOnLimit || !_cancellationToken.IsCancellationRequested);
        _completionSource.TrySetResult(completed);
    }

    // Native driver for provider mode: spin batches flat-out on this (p)thread, then flush.
    // Mirror of MotelySearchPlan.ExecuteSequentialPlan. The browser drives the same per-batch
    // primitive cooperatively via RunProviderBrowserPumpAsync instead of this tight loop.
    private void RunProviderPlan(MotelySearchPlan plan)
    {
        while (plan.TryExecuteProviderBatch()) { }
        plan.FlushFilterBatches();
    }

    public Task RunSearchAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
        BeginSearch(cancellationToken);
        return _completionSource.Task;
    }

    public IMotelySearch Start(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
        BeginSearch(cancellationToken);
        return this;
    }

    private void BeginSearch(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _hasStarted, 1) != 0)
            throw new InvalidOperationException("Search has already been started.");

        if (_stopAfterMatches > 0)
        {
            _stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationToken = _stopSource.Token;
        }
        else
        {
            _cancellationToken = cancellationToken;
        }

        StartSearchThreads();
    }

    /// <summary>
    /// Counts one matched seed toward <see cref="MotelySearchSettings{TBaseFilter}.StopAfterMatches"/>
    /// and cancels the run once the limit is reached. Called from the reporting paths, which are
    /// the only places a match is counted (a seed dropped by the auto-cutoff never gets here).
    /// Threads in flight finish their current batch, so the run delivers at least the limit.
    /// </summary>
    internal void NoteMatchForStop()
    {
        if (_stopAfterMatches <= 0)
            return;

        if (Interlocked.Increment(ref _stopMatchCount) != _stopAfterMatches)
            return;

        // Exactly the thread that crosses the threshold cancels, so Cancel() runs once even
        // though later matches keep incrementing past it.
        Interlocked.Exchange(ref _stoppedOnMatchLimit, 1);
        try
        {
            _stopSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Disposed mid-run: the search is already ending, which is what cancelling wanted.
        }
    }

    /// <summary>
    /// Starts search threads without blocking the caller.
    /// Completion is signaled via <see cref="_completionSource"/>.
    /// </summary>
    /// <remarks>
    /// Both paths run worker bodies off the caller thread so <see cref="RunSearchAsync"/>
    /// returns a live Task (await stays safe on the same thread). Workers capture the first
    /// exception and complete <see cref="_completionSource"/> when the last worker exits —
    /// failures surface to the awaiter instead of hanging.
    /// </remarks>
    private void StartSearchThreads()
    {
        _elapsedTime.Start();

        // Inline (Jamlyzer): the entire bounded workload runs synchronously on the caller,
        // so AwaitCompletion() is safe on every platform, including the browser's single
        // thread where the pump below would deadlock it.
        if (_runInline)
        {
            try
            {
                RunWorkerBody(_plans[0]);
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                // honour cooperative cancellation — no error
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
                return;
            }
            SignalSearchCompleted();
            return;
        }

        // Browser: one thread, no pthreads. Pump cooperatively (await Task.Delay(1) between
        // batches) so the single thread surfaces to the event loop and the UI repaints instead
        // of freezing under the SIMD grind. The pump completes _completionSource on its way out;
        // Bootsharp marshals that Task to the JS Promise.
        if (OperatingSystem.IsBrowser())
        {
            Task pump = _isProviderMode
                ? RunProviderBrowserPumpAsync()
                : RunSequentialBrowserPumpAsync();

            // Don't drop the handle. If the pump faults before it can signal completion, route
            // the exception onto the SAME _completionSource the Promise awaits — otherwise the
            // await hangs forever with no error (the detached `_ = pump()` it replaced did exactly
            // that). This is the browser's stand-in for the native WorkerCoordinator's catch.
            pump.ContinueWith(
                t => _completionSource.TrySetException(t.Exception!.InnerException ?? t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
            return;
        }

        WorkerCoordinator coordinator = new(this, _threadCount);

        var threads = new Thread[_threadCount];
        for (int i = 0; i < _threadCount; i++)
        {
            int threadIdx = i;
            threads[i] = new Thread(() => coordinator.RunWorker(threadIdx))
            {
                Name = $"Motely Search Thread {threadIdx}",
                IsBackground = true,
            };
            threads[i].Start();
        }
        // Publish only after all are started; Dispose can't run before Start() returns to the
        // caller (same thread), so no reader sees a partially-filled array.
        _workerThreads = threads;
    }

    /// <summary>
    /// Tracks the running worker count and routes the first thrown exception back
    /// through <see cref="_completionSource"/>. One instance per search.
    /// </summary>
    private sealed class WorkerCoordinator
    {
        private readonly MotelySearch<TBaseFilter> _owner;
        private int _remaining;
        private Exception? _firstError;

        public WorkerCoordinator(MotelySearch<TBaseFilter> owner, int totalWorkers)
        {
            _owner = owner;
            _remaining = totalWorkers;
        }

        public void RunWorker(int idx)
        {
            try
            {
                _owner.RunWorkerBody(_owner._plans[idx]);
            }
            catch (OperationCanceledException)
                when (_owner._cancellationToken.IsCancellationRequested)
            {
                // honour cooperative cancellation — no error
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WORKER EXCEPTION] {ex}");
                Interlocked.CompareExchange(ref _firstError, ex, null);
            }
            finally
            {
                if (Interlocked.Decrement(ref _remaining) == 0)
                {
                    Thread.MemoryBarrier();
                    var err = Volatile.Read(ref _firstError);
                    if (err is not null)
                    {
                        _owner._completionSource.TrySetException(err);
                    }
                    else
                    {
                        bool completed =
                            Volatile.Read(ref _owner._isDisposed) == 0
                            && !_owner._cancellationToken.IsCancellationRequested;
                        _owner._completionSource.TrySetResult(completed);
                    }
                }
            }
        }
    }

    public Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        return _completionSource.Task.WaitAsync(cancellationToken);
    }

    public void AwaitCompletion()
    {
        _completionSource.Task.GetAwaiter().GetResult();
    }

    /// <summary>After each batch: fire the progress callback with <see cref="MotelyProgress"/> (callers format strings).</summary>
    private void PrintReport()
    {
        if (_progressCallback == null)
            return;

        long elapsedMS = _elapsedTime.ElapsedMilliseconds;
        if (
            _progressReportIntervalMs > 0
            && _lastProgressReportElapsedMs >= 0
            && elapsedMS - _lastProgressReportElapsedMs < _progressReportIntervalMs
        )
            return;

        // Save previous report state for windowed (instantaneous) throughput before updating.
        long prevReportSeeds = _lastReportSeeds;
        long prevReportMs = _lastProgressReportElapsedMs;
        _lastProgressReportElapsedMs = elapsedMS;

        long thisCompletedCount = CompletedBatchCount;
        long totalBatches = _plans[0].MaxBatch;
        long seedsSearched = TotalSeedsSearched;

        double percentComplete;
        double totalPortionFinished;
        double thisPortionFinished;

        if (_isProviderMode && _plans[0] is MotelyProviderSearchPlan providerPlan)
        {
            long totalSeeds = providerPlan.SeedProvider.SeedCount;
            totalPortionFinished = totalSeeds > 0 ? (double)seedsSearched / totalSeeds : 0;
            percentComplete = totalPortionFinished * 100.0;
            thisPortionFinished = totalPortionFinished;
        }
        else
        {
            long batchesSinceStart = thisCompletedCount - _startBatchIndex;
            long totalBatchesToDo = _plans[0].MaxBatch - _startBatchIndex;
            totalPortionFinished = totalBatches > 0 ? (double)thisCompletedCount / totalBatches : 0;
            percentComplete = totalPortionFinished * 100.0;
            thisPortionFinished =
                totalBatchesToDo > 0 ? (double)batchesSinceStart / totalBatchesToDo : 0.0;
        }

        // Windowed throughput: seeds processed since the last report, divided by wall time
        // since the last report. Gives actual current speed instead of the misleading
        // lifetime average (which starts slow due to JIT/cache warmup and climbs forever).
        double seedsPerMs;
        if (prevReportMs >= 0)
        {
            long deltaSeeds = seedsSearched - prevReportSeeds;
            long deltaMs = elapsedMS - prevReportMs;
            seedsPerMs = deltaMs > 0 ? (double)deltaSeeds / deltaMs : 0;
        }
        else
        {
            // First report: no previous data, fall back to lifetime average.
            seedsPerMs = elapsedMS > 1 ? (double)seedsSearched / elapsedMS : 0;
        }
        _lastReportSeeds = seedsSearched;

        long? etaMs = null;
        if (thisPortionFinished >= 0.0000001)
        {
            double totalTimeEstimate = elapsedMS / thisPortionFinished;
            double timeLeftMs = totalTimeEstimate - elapsedMS;
            if (
                !double.IsNaN(timeLeftMs)
                && !double.IsInfinity(timeLeftMs)
                && timeLeftMs >= 0
                && timeLeftMs <= 365.0 * 24 * 60 * 60 * 1000
            )
                etaMs = (long)timeLeftMs;
        }

        var progress = new MotelyProgress
        {
            CompletedBatchCount = thisCompletedCount,
            TotalBatchCount = totalBatches,
            SeedsSearched = seedsSearched,
            MatchingSeeds = MatchingSeeds,
            SeedsPerMillisecond = seedsPerMs,
            PercentComplete = percentComplete,
            ElapsedMilliseconds = elapsedMS,
            EstimatedTimeRemainingMilliseconds = etaMs,
        };
        _progressCallback(progress);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        // Quiesce native workers before freeing the memory they read. _isDisposed is set above,
        // so each worker drops out at its next batch boundary; Join then waits for any in-flight
        // batch to actually finish — otherwise the FreeHGlobal below races a thread mid-read.
        // Skip self-join (Dispose from a worker/finalizer thread) to avoid deadlocking on self.
        if (_workerThreads is not null)
        {
            int self = Environment.CurrentManagedThreadId;
            foreach (Thread t in _workerThreads)
                if (t.ManagedThreadId != self)
                    t.Join();
        }

        // A constructor that threw (bad Mode, filter creation failure) still queues this
        // object for finalization, so Dispose runs against partially-built state: _plans
        // may be null or have null tail entries, and the native buffer may never have been
        // allocated. Guard each piece; FreeHGlobal(0) is a no-op.
        if (_plans is not null)
            for (int i = 0; i < _plans.Length; i++)
                _plans[i]?.Dispose();
        Marshal.FreeHGlobal((nint)_pseudoHashKeyLengths);

        // After the Join above, so no worker is still inside NoteMatchForStop calling Cancel().
        _stopSource?.Dispose();

        _completionSource.TrySetResult(false);

        GC.SuppressFinalize(this);
    }

    ~MotelySearch()
    {
        if (Volatile.Read(ref _isDisposed) == 0)
        {
            Dispose();
        }
    }

    private abstract class MotelySearchPlan : IDisposable
    {
        public const int MAX_SEED_WAIT_MS = 500;

        // Auto-cutoff rate gate (see AutoCutoffState). Evaluated once per batch.
        // Engage the monotonic-max clamp only when raw matches arrive faster than this;
        // below it there's no interop pressure, so report everything. Conservative defaults,
        // tunable. The window keeps the rate estimate stable across a single batch's jitter.
        private const long AUTO_CUTOFF_GATE_WINDOW_MS = 250;
        private const long AUTO_CUTOFF_ENGAGE_RATE_PER_SEC = 2000;

        public readonly MotelySearch<TBaseFilter> Search;
        public readonly int ThreadIndex;

        public long MaxBatch { get; internal set; }
        public long SeedsPerBatch { get; internal set; }

        // ========== THREAD-LOCAL PERFORMANCE ARCHITECTURE ==========
        // PATTERN: Thread-local accumulate → Batch-boundary pull/clear → Global aggregate
        // This eliminates hot-path Interlocked operations and I/O bottlenecks

        // Thread-local counters - NO Interlocked in hot path!
        // Each thread owns these counters; reporting aggregates snapshots.
        internal long _localMatchingSeeds = 0;
        internal long _localBatchesCompleted = 0;
        internal long _localSeedsSearched = 0;
        private readonly AutoCutoffState _autoCutoffState = new() { LearnedCutoff = int.MinValue };

        // Per-plan auto-cutoff rate gate state. A class (not struct) on purpose: the field
        // above is readonly, and the gate mutates these members in place each batch.
        private sealed class AutoCutoffState
        {
            // Highest score seen so far; the monotonic-max clamp floor. Starts at int.MinValue.
            public int LearnedCutoff;

            // Raw candidate matches seen before the clamp (drives the rate estimate).
            public long RawMatches;

            // RawMatches snapshot at the last gate evaluation.
            public long LastGateRawMatches;

            // Elapsed ms at the last gate evaluation.
            public long LastGateMs;

            // Candidates dropped by the clamp once engaged.
            public long SeedsFiltered;

            // Whether the monotonic-max clamp is currently active.
            public bool Engaged;
        }

        // Pre-allocated result buffer - ONE allocation per thread, reused forever
        // Old stale data is fine - mask controls which slots are valid
        protected readonly MotelyScoredSeedResult[] _resultBuffer = new MotelyScoredSeedResult[
            MotelyGlobals.MaxVectorWidth
        ];

        [InlineArray(MotelyGlobals.MaxSeedLength)]
        internal struct FilterSeedBatchCharacters
        {
            public Vector512<double> Character;
        }

        internal struct FilterSeedBatch
        {
            public FilterSeedBatchCharacters SeedCharacters;
            public Vector512<double>* SeedHashes;
            public PartialSeedHashCache SeedHashCache;
            public int SeedLength;
            public int SeedCount;
            public long WaitStartMS;
        }

        internal readonly FilterSeedBatch* _filterSeedBatches;

        // Provider-mode: this thread has exhausted the seed provider and should idle.
        internal bool _providerExhausted;

        public MotelySearchPlan(MotelySearch<TBaseFilter> search, int threadIndex)
        {
            Search = search;
            ThreadIndex = threadIndex;

            if (search._additionalFilters.Length != 0)
            {
                _filterSeedBatches = (FilterSeedBatch*)
                    Marshal.AllocHGlobal(
                        sizeof(FilterSeedBatch) * search._additionalFilters.Length
                    );

                int allocatedCount = 0;
                try
                {
                    for (int i = 0; i < search._additionalFilters.Length; i++)
                    {
                        FilterSeedBatch* batch = &_filterSeedBatches[i];

                        *batch = new()
                        {
                            SeedHashes = (Vector512<double>*)
                                Marshal.AllocHGlobal(
                                    sizeof(Vector512<double>)
                                        * MotelyGlobals.MaxCachedPseudoHashKeyLength
                                ),
                        };
                        allocatedCount = i + 1; // Track successful allocations

                        batch->SeedHashCache = new(search, batch->SeedHashes);
                    }
                }
                catch
                {
                    // Clean up any allocated memory on exception
                    for (int i = 0; i < allocatedCount; i++)
                    {
                        if (_filterSeedBatches[i].SeedHashes != null)
                        {
                            Marshal.FreeHGlobal((nint)_filterSeedBatches[i].SeedHashes);
                        }
                    }
                    Marshal.FreeHGlobal((nint)_filterSeedBatches);
                    _filterSeedBatches = null;
                    throw;
                }
            }
        }

        internal void ExecuteSequentialPlan()
        {
            // Native driver: spin batches flat-out on this (p)thread, then flush.
            while (TryExecuteSequentialBatch()) { }
            FlushFilterBatches();
        }

        /// <summary>
        /// Runs one sequential batch. Returns false when the search is finished — disposed,
        /// cancelled, or every batch consumed — at which point the caller flushes. Pulled
        /// out of the loop so the browser pump can drive a single batch and await between
        /// them for a repaint; the native driver just calls it in a tight loop.
        /// </summary>
        internal bool TryExecuteSequentialBatch()
        {
            if (Volatile.Read(ref Search._isDisposed) != 0)
                return false;

            if (Search._cancellationToken.IsCancellationRequested)
                return false;

            long batchIdx = Search._singleBatchConsumer
                ? ++Search._batchIndex
                : Interlocked.Increment(ref Search._batchIndex);

            if (batchIdx >= Search._endBatchIndex || batchIdx >= MaxBatch)
                return false;

            SearchSequentialBatch(batchIdx);

            // Only book a batch that ran to the end. SearchVector returns early on cancellation and
            // the outer loop keeps calling it, so an abandoned batch still returns normally —
            // counting it would overstate CompletedBatchCount and the --startBatch resume hint it
            // feeds. Once per batch, never per seed.
            if (
                Volatile.Read(ref Search._isDisposed) == 0
                && !Search._cancellationToken.IsCancellationRequested
            )
                _localBatchesCompleted++;

            // Check for timed-out filter batches
            if (Search._additionalFilters.Length != 0)
            {
                for (int i = 0; i < Search._additionalFilters.Length; i++)
                {
                    FilterSeedBatch* batch = &_filterSeedBatches[i];

                    if (batch->SeedCount != 0)
                    {
                        if (
                            Search._elapsedTime.ElapsedMilliseconds - batch->WaitStartMS
                            >= MAX_SEED_WAIT_MS
                        )
                        {
                            SearchFilterBatch(i, batch);
                            Debug.Assert(
                                batch->SeedCount == 0,
                                "Batch should be reset after SearchFilterBatch"
                            );
                        }
                    }
                }
            }

            // Re-evaluate the per-thread auto-cutoff rate gate for the next batch.
            if (Search._autoScoreCutoff)
                UpdateAutoCutoffGate();

            // Report progress
            Search.PrintReport();

            return true;
        }

        /// <summary>
        /// Runs one provider <b>report</b> batch: many SIMD packs (8 seeds each) until
        /// <see cref="MotelySearch{TBaseFilter}._providerBatchSeedCount"/> seeds are chewed,
        /// the provider is empty, or cancel fires. Returns false when finished — disposed,
        /// cancelled, or the provider is exhausted — at which point the caller flushes.
        /// Sequential's batchCharCount is a different machine (space partition + partial hash);
        /// here batch size only amortizes progress chat and interop over huge provider streams.
        /// </summary>
        internal bool TryExecuteProviderBatch()
        {
            if (Volatile.Read(ref Search._isDisposed) != 0)
                return false;

            if (Search._cancellationToken.IsCancellationRequested)
                return false;

            if (_providerExhausted)
                return false;

            long target = Search._providerBatchSeedCount;
            long chewed = 0;
            while (
                chewed < target
                && !_providerExhausted
                && Volatile.Read(ref Search._isDisposed) == 0
                && !Search._cancellationToken.IsCancellationRequested
            )
            {
                long before = _localSeedsSearched;
                SearchProviderBatch();
                long got = _localSeedsSearched - before;
                if (got <= 0)
                    break;
                chewed += got;
            }

            // Nothing ran (already empty on entry) — stop without counting a report batch.
            if (chewed == 0)
                return false;

            _localBatchesCompleted++;

            if (Search._autoScoreCutoff)
                UpdateAutoCutoffGate();

            // Always report after a non-empty chew — including the drain batch — so progress
            // can hit 100% on short lists and StopAfter races still see a final tick.
            Search.PrintReport();

            // Exhausted after this report batch → caller flushes and exits.
            return !_providerExhausted
                && Volatile.Read(ref Search._isDisposed) == 0
                && !Search._cancellationToken.IsCancellationRequested;
        }

        /// <summary>Flush any seeds still sitting in filter batches when the search ends.</summary>
        internal void FlushFilterBatches()
        {
            if (Search._additionalFilters.Length != 0 && _filterSeedBatches != null)
            {
                for (int i = 0; i < Search._additionalFilters.Length; i++)
                {
                    FilterSeedBatch* batch = &_filterSeedBatches[i];
                    if (batch->SeedCount != 0)
                    {
                        SearchFilterBatch(i, batch);
                    }
                }
            }
        }

        // Per-thread auto-cutoff rate gate. Measures the RAW match rate (candidates seen
        // before the clamp) over a short window and engages the monotonic-max clamp only
        // while that rate would pressure the scored-result callback (costly across the WASM
        // interop boundary). Rare searches never engage and report every match. No locking:
        // each plan owns its own AutoCutoffState. A single threshold is safe because the raw
        // rate is independent of whether the clamp is engaged, so it cannot oscillate.
        private void UpdateAutoCutoffGate()
        {
            long nowMs = Search._elapsedTime.ElapsedMilliseconds;
            long windowMs = nowMs - _autoCutoffState.LastGateMs;
            if (windowMs < AUTO_CUTOFF_GATE_WINDOW_MS)
                return;

            long windowMatches = _autoCutoffState.RawMatches - _autoCutoffState.LastGateRawMatches;
            long ratePerSec = windowMatches * 1000 / windowMs;

            _autoCutoffState.Engaged = ratePerSec >= AUTO_CUTOFF_ENGAGE_RATE_PER_SEC;

            _autoCutoffState.LastGateMs = nowMs;
            _autoCutoffState.LastGateRawMatches = _autoCutoffState.RawMatches;
        }

        internal abstract void SearchProviderBatch();
        internal abstract void SearchSequentialBatch(long batchIdx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long SnapshotMatchingSeeds() => Volatile.Read(ref _localMatchingSeeds);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long SnapshotBatchesCompleted() => Volatile.Read(ref _localBatchesCompleted);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long SnapshotSeedsSearched() => Volatile.Read(ref _localSeedsSearched);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SearchSeeds(in MotelySearchContextParams searchContextParams)
        {
            // This is the method for searching the base filter, we should not be searching additional filters
            Debug.Assert(!searchContextParams.IsAdditionalFilter);

            MotelyVectorSearchContext searchContext = new(
                in Search._searchParameters,
                in searchContextParams
            );

            VectorMask searchResultMask = Search._baseFilter.Filter(ref searchContext);

            searchContextParams.SeedHashCache->Reset();
            if (searchResultMask.IsPartiallyTrue())
            {
                if (Search._additionalFilters.Length == 0)
                {
                    ReportSeeds(searchResultMask, in searchContextParams);
                }
                else
                {
                    BatchSeeds(0, searchResultMask, in searchContextParams);
                }
            }
        }

        // Extracts the actual seed characters from a search context and reports that seed
        private void ReportSeeds(
            VectorMask searchResultMask,
            in MotelySearchContextParams searchParams
        )
        {
            Debug.Assert(
                searchResultMask.IsPartiallyTrue(),
                "Mask should be checked for partial truth before calling report seeds (for performance)."
            );

            // If we have a score provider, ALWAYS use it (it handles scoring, cutoff AND printing)
            // Previously this was gated by _csvOutput, which caused normal runs to bypass scoring
            // and fall back to ReportBasicSeeds (printing every raw seed).
            if (Search.TryGetScoreProvider(out var scoreProvider))
            {
                // Create search context for scoring
                MotelyVectorSearchContext searchContext = new(
                    in Search._searchParameters,
                    in searchParams
                );

                // Call the score provider with the mask of seeds that passed filters
                // The score provider will handle scoring and calling the callback
                VectorMask scoredMask = scoreProvider.Score(
                    ref searchContext,
                    _resultBuffer,
                    searchResultMask
                );

                // Report the scored results!
                ReportScoredResults(scoredMask, in searchParams);
            }
            else if (Search._seedRouter != null)
            {
                for (int lane = 0; lane < MotelyGlobals.MaxVectorWidth; lane++)
                {
                    if (searchParams.IsLaneValid(lane))
                    {
                        MotelySingleSearchContext singleCtx = new(
                            in Search._searchParameters,
                            in searchParams,
                            lane
                        );
                        Search._seedRouter.InjectSingleSeedContext(in singleCtx);
                    }
                }
            }
            else
            {
                // No score provider - report basic seeds
                ReportBasicSeeds(searchResultMask, in searchParams);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportScoredResults(
            VectorMask resultMask,
            in MotelySearchContextParams searchParams
        )
        {
            // Do NOT write to console here - the callback already handled output!
            // The score provider invokes the callback which writes to Console.
            // This method ONLY updates counters for statistics tracking.
            // The callback flow is:
            //   1. scoreProvider.Score() -> invokes callback -> Console.WriteLine (FIRST OUTPUT)
            //   2. ReportScoredResults() -> ONLY increment counter (NO OUTPUT)

            for (int lane = 0; lane < MotelyGlobals.MaxVectorWidth; lane++)
            {
                if (resultMask[lane] && searchParams.IsLaneValid(lane))
                {
                    if (Search._autoScoreCutoff)
                    {
                        int score = _resultBuffer[lane].Score;

                        // Count every scored candidate BEFORE the clamp — this is the raw
                        // match rate that drives the gate (see UpdateAutoCutoffGate).
                        _autoCutoffState.RawMatches++;

                        // Clamp only while engaged: drop anything below the running max.
                        if (_autoCutoffState.Engaged && score < _autoCutoffState.LearnedCutoff)
                        {
                            _autoCutoffState.SeedsFiltered++;
                            continue;
                        }

                        // Only ever raise the bar. (While disengaged we report low scores
                        // too, so a plain assign here would wrongly lower the running max.)
                        if (score > _autoCutoffState.LearnedCutoff)
                            _autoCutoffState.LearnedCutoff = score;
                    }

                    Search._scoredResultCallback?.Invoke(_resultBuffer[lane]);
                    _localMatchingSeeds++;
                    Search.NoteMatchForStop();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportBasicSeeds(
            VectorMask searchResultMask,
            in MotelySearchContextParams searchParams
        )
        {
            char* seed = stackalloc char[MotelyGlobals.MaxSeedLength];

            for (int lane = 0; lane < MotelyGlobals.MaxVectorWidth; lane++)
            {
                if (searchResultMask[lane] && searchParams.IsLaneValid(lane))
                {
                    int length = searchParams.GetSeed(lane, seed);

                    // Increment thread-local counter
                    _localMatchingSeeds++;

                    string seedStr = new Span<char>(seed, length).ToString();
                    Search._seedMatchCallback?.Invoke(seedStr);
                    Search.NoteMatchForStop();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BatchSeeds(
            int filterIndex,
            VectorMask searchResultMask,
            in MotelySearchContextParams searchParams
        )
        {
            Debug.Assert(
                _filterSeedBatches != null
                    && Search._additionalFilters != null
                    && filterIndex >= 0
                    && filterIndex < Search._additionalFilters.Length,
                $"Invalid filterIndex {filterIndex}, _additionalFilters={(Search._additionalFilters == null ? "NULL" : $"Length={Search._additionalFilters.Length}")}"
            );

            Debug.Assert(searchParams.SeedHashCache != null, "SeedHashCache is null");

            FilterSeedBatch* filterBatch = &_filterSeedBatches[filterIndex];

            Debug.Assert(
                filterBatch->SeedHashes != null,
                $"filterBatch->SeedHashes is null for filterIndex {filterIndex}"
            );

            Debug.Assert(
                searchResultMask.IsPartiallyTrue(),
                "Mask should be checked for partial truth before calling enqueue seeds (for performance)."
            );

            for (int lane = 0; lane < Vector512<double>.Count; lane++)
            {
                if (searchResultMask[lane] && searchParams.IsLaneValid(lane))
                {
                    int seedBatchIndex = filterBatch->SeedCount;

                    if (seedBatchIndex == 0)
                    {
                        filterBatch->SeedLength = searchParams.SeedLength;
                    }
                    else
                    {
                        // Each batch can only contain seeds of the same length, we should check if this seed can go into the batch
                        if (filterBatch->SeedLength != searchParams.SeedLength)
                        {
                            // This seed is a different length to the ones already in the batch :c
                            // Let's flush the batch and start again.
                            SearchFilterBatch(filterIndex, filterBatch);

                            Debug.Assert(
                                filterBatch->SeedCount == 0,
                                "Searching the batch should have reset it."
                            );
                            seedBatchIndex = 0;

                            filterBatch->SeedLength = searchParams.SeedLength;
                        }
                        // else: Same length - seedBatchIndex already equals current SeedCount, ready to use
                    }

                    ++filterBatch->SeedCount;

                    // Store the seed digits
                    {
                        int i = 0;
                        for (; i < searchParams.SeedLastCharactersLength; i++)
                        {
                            ((double*)&filterBatch->SeedCharacters)[
                                i * Vector512<double>.Count + seedBatchIndex
                            ] = ((double*)searchParams.SeedLastCharacters)[
                                i * Vector512<double>.Count + lane
                            ];
                        }

                        for (
                            int firstCharIndex = 0;
                            firstCharIndex < searchParams.SeedFirstCharactersLength;
                            firstCharIndex++
                        )
                        {
                            ((double*)&filterBatch->SeedCharacters)[
                                (searchParams.SeedLastCharactersLength + firstCharIndex)
                                    * Vector512<double>.Count
                                    + seedBatchIndex
                            ] = searchParams.SeedFirstCharacters[firstCharIndex];
                        }
                    }

                    // Store the cached hashes
                    // The cache structure: Cache[partialHashLength] already points to the correct Vector512<double>*
                    // for that key length. We just need to copy lane 'lane' from source to lane 'seedBatchIndex' in target.
                    if (
                        Search._pseudoHashKeyLengths == null
                        || Search._pseudoHashKeyLengthCount <= 0
                    )
                        return;

                    for (int i = 0; i < Search._pseudoHashKeyLengthCount; i++)
                    {
                        int partialHashLength = Search._pseudoHashKeyLengths[i];

                        // Ensure cache entry exists before accessing
                        if (searchParams.SeedHashCache == null)
                            continue;

                        if (partialHashLength >= MotelyGlobals.MaxCachedPseudoHashKeyLength)
                            Console.WriteLine(
                                $"partialHashLength {partialHashLength} >= MotelyGlobals.MaxCachedPseudoHashKeyLength {MotelyGlobals.MaxCachedPseudoHashKeyLength}"
                            );

                        if (searchParams.SeedHashCache->Cache[partialHashLength] == null)
                            continue;

                        // Cache[partialHashLength] already points to the correct Vector512<double>*
                        // for this key length (set up by PartialSeedHashCache ctor:
                        //     InitialCache[keyLength] = &partialSeedHashes[i]
                        // ). So the source vector is one Vector512 and we just need lane `lane`.
                        // Using `[i * Vector512<double>.Count + lane]` here reads `_hashes[2*i]`
                        // — correct only when i == 0, wrong vector or OOB for i >= 1 — which
                        // silently drops seeds from multi-keylength additional-filter chains.
                        // Regression covered by Tacodiva/Motely#5 and
                        // Motely.Tests/ChainedMustClauseSeedTests.cs.
                        double sourceValue = (
                            (double*)searchParams.SeedHashCache->Cache[partialHashLength]
                        )[lane];

                        // Write to target: filterBatch->SeedHashes is an array of Vector512<double>
                        ((double*)filterBatch->SeedHashes)[
                            i * Vector512<double>.Count + seedBatchIndex
                        ] = sourceValue;
                    }

                    if (seedBatchIndex == Vector512<double>.Count - 1)
                    {
                        // The queue if full of seeds! We can run the search
                        SearchFilterBatch(filterIndex, filterBatch);
                    }
                }
            }
        }

        // Searches a batch with a filter then resets that batch
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SearchFilterBatch(int filterIndex, FilterSeedBatch* filterBatch)
        {
            Debug.Assert(filterBatch->SeedCount != 0);

            // Zero unused lanes so IsLaneValid returns false for lane >= SeedCount.
            // Without this, stale data from the previous batch makes those lanes appear
            // valid, causing the same seeds to be re-processed and reported as duplicates.
            //
            // We also zero the cached partial-hash vectors for those padding lanes: BatchSeeds
            // writes only to lane = seedBatchIndex, so any lane >= SeedCount retains a stale
            // hash from the last partial batch that touched this slot. Filter code that reads
            // the hash cache across all 8 SIMD lanes (voucher / Tarot / Planet / tag / Spectral
            // resample loops) will otherwise produce garbage PRNG output for padding lanes and
            // can spin forever trying to reroll them into a legal value. See Tacodiva/Motely#5
            // for the source-side handoff bug; this is the destination-side hygiene pass.
            int count = filterBatch->SeedCount;
            if (count < MotelyGlobals.MaxVectorWidth)
            {
                double* chars = (double*)&filterBatch->SeedCharacters;
                for (int lane = count; lane < MotelyGlobals.MaxVectorWidth; lane++)
                {
                    chars[lane] = 0; // First char of each lane; enough for IsLaneValid
                }

                double* hashes = (double*)filterBatch->SeedHashes;
                int keyCount = Search._pseudoHashKeyLengthCount;
                for (int i = 0; i < keyCount; i++)
                {
                    for (int lane = count; lane < MotelyGlobals.MaxVectorWidth; lane++)
                    {
                        hashes[i * Vector512<double>.Count + lane] = 0;
                    }
                }
            }

            MotelySearchContextParams searchParams = new(
                &filterBatch->SeedHashCache,
                filterBatch->SeedLength,
                0,
                null,
                (Vector512<double>*)&filterBatch->SeedCharacters,
                isAdditionalFilter: true
            );

            MotelyVectorSearchContext searchContext = new(
                in Search._searchParameters,
                in searchParams
            );

            VectorMask searchResultMask = Search
                ._additionalFilters[filterIndex]
                .Filter(ref searchContext);

            if (searchResultMask.IsPartiallyTrue())
            {
                int nextFilterIndex = filterIndex + 1;

                if (nextFilterIndex == Search._additionalFilters.Length)
                {
                    // If this was the last filter, we can report the seeds
                    ReportSeeds(searchResultMask, in searchParams);
                }
                else
                {
                    Debug.Assert(
                        nextFilterIndex < Search._additionalFilters.Length && nextFilterIndex >= 0,
                        $"nextFilterIndex {nextFilterIndex} >= _additionalFilters.Length {Search._additionalFilters.Length} or nextFilterIndex < 0   "
                    );
                    BatchSeeds(nextFilterIndex, searchResultMask, in searchParams);
                }
            }

            // Clear dynamic pseudohash entries this filter cached for THIS batch's seeds. Without
            // this, the next batch that reuses this FilterSeedBatch hits CachePartialHash's
            // "already cached" short-circuit and re-derives the next seeds against the previous
            // batch's hashes — silently dropping every batch after the first. Mirrors the provider's
            // post-base-filter Reset (see SearchSeeds). The base-key hashes live in SeedHashes and
            // are restored from InitialCache, so the next BatchSeeds copy is unaffected.
            filterBatch->SeedHashCache.Reset();

            filterBatch->SeedCount = 0;
        }

        public void Dispose()
        {
            // FIX: Check if _filterSeedBatches is not null before freeing
            if (_filterSeedBatches != null)
            {
                for (int i = 0; i < Search._additionalFilters.Length; i++)
                {
                    _filterSeedBatches[i].SeedHashCache.Dispose();
                    if (_filterSeedBatches[i].SeedHashes != null)
                    {
                        Marshal.FreeHGlobal((nint)_filterSeedBatches[i].SeedHashes);
                    }
                }

                Marshal.FreeHGlobal((nint)_filterSeedBatches);
            }
        }
    }

    private sealed unsafe class MotelyProviderSearchPlan : MotelySearchPlan
    {
        public readonly IMotelySeedProvider SeedProvider;

        private readonly Vector512<double>* _hashes;
        private readonly PartialSeedHashCache* _hashCache;

        private readonly Vector512<double>* _seedCharacterMatrix;

        // Thread-local seed batch buffer to avoid allocations per batch
        private readonly string[] _seedBatchBuffer = new string[MotelyGlobals.MaxVectorWidth];

        public MotelyProviderSearchPlan(
            MotelySearch<TBaseFilter> search,
            MotelySearchSettings<TBaseFilter> settings,
            int index
        )
            : base(search, index)
        {
            if (settings.SeedProvider == null)
                throw new ArgumentException(
                    "Cannot create a provider search without a seed provider."
                );

            SeedProvider = settings.SeedProvider;

            // Progress denominator uses report-batch size (many SIMD packs), not 8-lane width.
            // Unknown seed count (-1) → large estimate for progress only; termination is provider empty.
            long seedCount = SeedProvider.SeedCount;
            long reportBatch = Math.Max(
                MotelyGlobals.MaxVectorWidth,
                (long)search._providerBatchSeedCount
            );
            MaxBatch =
                seedCount >= 0
                    ? (seedCount + reportBatch - 1) / reportBatch
                    : long.MaxValue / reportBatch;
            SeedsPerBatch = reportBatch;

            _hashes = (Vector512<double>*)
                Marshal.AllocHGlobal(sizeof(Vector512<double>) * search._pseudoHashKeyLengthCount);

            _hashCache = (PartialSeedHashCache*)Marshal.AllocHGlobal(sizeof(PartialSeedHashCache));
            *_hashCache = new PartialSeedHashCache(search, _hashes);

            _seedCharacterMatrix = (Vector512<double>*)
                Marshal.AllocHGlobal(sizeof(Vector512<double>) * MotelyGlobals.MaxSeedLength);
        }

        internal override void SearchProviderBatch()
        {
            // Batch retrieve seeds in one lock operation - much faster!
            // Use thread-local buffer to avoid allocations per batch
            int fetched = SeedProvider.NextSeeds(_seedBatchBuffer);

            // If we got no seeds at all, this thread is exhausted
            if (fetched == 0)
            {
                // Mark this thread exhausted so ThreadMain idles it (and we only decrement once)
                _providerExhausted = true;

                return;
            }

            // Valid seeds only, packed into dense lanes 0..validCount-1.
            // Motely charset has no '0' — list providers often feed decimal strings ("10", "20")
            // that must be dropped. Leaving a hole in seedLengths and still iterating `fetched`
            // made the scalar path read stack garbage as a length and OOB the seed stackalloc
            // (S8CoverageClimbTests.NegativeLegendarySimdFront_ComposedWithSoulConfirm_FindsSeeds).
            int* seedLengths = stackalloc int[MotelyGlobals.MaxVectorWidth];
            bool homogeneousSeedLength = true;
            int validCount = 0;

            for (int seedIdx = 0; seedIdx < fetched; seedIdx++)
            {
                ReadOnlySpan<char> seed = _seedBatchBuffer[seedIdx].AsSpan();

                if (
                    seed.IsEmpty
                    || seed.Length > MotelyGlobals.MaxSeedLength
                    || seed.IndexOf('0') >= 0
                )
                {
                    continue;
                }

                int lane = validCount;
                if (lane >= MotelyGlobals.MaxVectorWidth)
                    break;

                seedLengths[lane] = seed.Length;
                if (lane > 0 && seedLengths[0] != seed.Length)
                    homogeneousSeedLength = false;

                for (int i = 0; i < seed.Length; i++)
                {
                    ((double*)_seedCharacterMatrix)[i * MotelyGlobals.MaxVectorWidth + lane] =
                        seed[i];
                }
                for (int i = seed.Length; i < MotelyGlobals.MaxSeedLength; i++)
                {
                    ((double*)_seedCharacterMatrix)[i * MotelyGlobals.MaxVectorWidth + lane] = 0;
                }

                validCount++;
            }

            // Entire fetch was invalid (e.g. all decimal seeds containing '0'). Advance to the
            // next provider batch — do not treat the thread as exhausted.
            if (validCount == 0)
                return;

            for (int lane = validCount; lane < MotelyGlobals.MaxVectorWidth; lane++)
            {
                for (int i = 0; i < MotelyGlobals.MaxSeedLength; i++)
                {
                    ((double*)_seedCharacterMatrix)[i * MotelyGlobals.MaxVectorWidth + lane] = 0;
                }
            }

            _localSeedsSearched += validCount;

            if (homogeneousSeedLength)
            {
                // If all the seeds are the same length, we can be fast and vectorize!
                int seedLength = seedLengths[0];

                // Calculate the partial psuedohash cache
                for (
                    int pseudohashKeyIdx = 0;
                    pseudohashKeyIdx < Search._pseudoHashKeyLengthCount;
                    pseudohashKeyIdx++
                )
                {
                    int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                    Vector512<double> numVector = Vector512<double>.One;

                    for (int i = seedLength - 1; i >= 0; i--)
                    {
                        numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

                        numVector = Vector512.Multiply(numVector, _seedCharacterMatrix[i]);

                        numVector = Vector512.Multiply(numVector, Math.PI);
                        numVector = Vector512.Add(
                            numVector,
                            Vector512.Create((i + pseudohashKeyLength + 1) * Math.PI)
                        );

                        Vector512<double> intPart = Vector512.Floor(numVector);
                        numVector = Vector512.Subtract(numVector, intPart);
                    }

                    _hashes[pseudohashKeyIdx] = numVector;
                }

                SearchSeeds(
                    new MotelySearchContextParams(
                        _hashCache,
                        seedLength,
                        0,
                        null,
                        _seedCharacterMatrix
                    )
                );
            }
            else
            {
                // Otherwise, we need to search all the seeds individually
                Span<char> seed = stackalloc char[MotelyGlobals.MaxSeedLength];

                for (int i = 0; i < validCount; i++)
                {
                    int seedLength = seedLengths[i];

                    for (int j = 0; j < seedLength; j++)
                    {
                        seed[j] = (char)
                            ((double*)_seedCharacterMatrix)[j * MotelyGlobals.MaxVectorWidth + i];
                    }

                    SearchSingleSeed(seed[..seedLength]);
                }
            }
        }

        internal override void SearchSequentialBatch(long batchIdx)
        {
            throw new InvalidOperationException(
                $"{nameof(MotelyProviderSearchPlan)} does not support sequential batch search."
            );
        }

        private void SearchSingleSeed(ReadOnlySpan<char> seed)
        {
            // Skip empty seeds (indicates we've run out of seeds in the list)
            if (seed.IsEmpty)
                return;

            char* seedLastCharacters = stackalloc char[MotelyGlobals.MaxSeedLength - 1];

            // Calculate the partial psuedohash cache
            for (
                int pseudohashKeyIdx = 0;
                pseudohashKeyIdx < Search._pseudoHashKeyLengthCount;
                pseudohashKeyIdx++
            )
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = seed.Length - 1; i >= 0; i--)
                {
                    num =
                        (
                            1.1239285023 / num * seed[i] * Math.PI
                            + (i + pseudohashKeyLength + 1) * Math.PI
                        ) % 1;
                }

                _hashes[pseudohashKeyIdx] = Vector512.Create(num);
            }

            for (int i = 0; i < seed.Length - 1; i++)
            {
                seedLastCharacters[i] = seed[i + 1];
            }

            Vector512<double> firstCharacterVector = Vector512.CreateScalar((double)seed[0]);

            SearchSeeds(
                new MotelySearchContextParams(
                    _hashCache,
                    seed.Length,
                    seed.Length - 1,
                    seedLastCharacters,
                    &firstCharacterVector
                )
            );
        }

        public new void Dispose()
        {
            base.Dispose();

            _hashCache->Dispose();
            Marshal.FreeHGlobal((nint)_hashCache);

            Marshal.FreeHGlobal((nint)_hashes);
            Marshal.FreeHGlobal((nint)_seedCharacterMatrix);
        }
    }

    private sealed unsafe class MotelySequentialSearchPlan : MotelySearchPlan
    {
        // A cache of vectors containing all the seed's digits.
        private static readonly Vector512<double>[] SeedDigitVectors = new Vector512<double>[
            (MotelyGlobals.SeedDigits.Length + MotelyGlobals.MaxVectorWidth - 1)
                / MotelyGlobals.MaxVectorWidth
        ];

        static MotelySequentialSearchPlan()
        {
            Span<double> vector = stackalloc double[MotelyGlobals.MaxVectorWidth];

            for (int i = 0; i < SeedDigitVectors.Length; i++)
            {
                for (int j = 0; j < MotelyGlobals.MaxVectorWidth; j++)
                {
                    int index = i * MotelyGlobals.MaxVectorWidth + j;

                    if (index >= MotelyGlobals.SeedDigits.Length)
                    {
                        vector[j] = 0;
                    }
                    else
                    {
                        vector[j] = MotelyGlobals.SeedDigits[index];
                    }
                }

                SeedDigitVectors[i] = Vector512.Create<double>(vector);
            }
        }

        private readonly int _batchCharCount;
        private readonly int _nonBatchCharCount;

        private readonly char* _digits;
        private readonly Vector512<double>* _hashes;
        private readonly PartialSeedHashCache* _hashCache;

        public MotelySequentialSearchPlan(
            MotelySearch<TBaseFilter> search,
            MotelySearchSettings<TBaseFilter> settings,
            int index
        )
            : base(search, index)
        {
            _digits = (char*)Marshal.AllocHGlobal(sizeof(char) * MotelyGlobals.MaxSeedLength);

            _batchCharCount = settings.SequentialBatchCharacterCount;
            SeedsPerBatch = (long)Math.Pow(MotelyGlobals.SeedDigits.Length, _batchCharCount);

            _nonBatchCharCount = MotelyGlobals.MaxSeedLength - _batchCharCount;
            MaxBatch = (long)Math.Pow(MotelyGlobals.SeedDigits.Length, _nonBatchCharCount);

            // Safety check for pseudoHashKeyLengthCount to prevent null pointer issues
            if (Search._pseudoHashKeyLengthCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid pseudoHashKeyLengthCount: {Search._pseudoHashKeyLengthCount}. Search may not be properly initialized."
                );
            }

            _hashes = (Vector512<double>*)
                Marshal.AllocHGlobal(
                    sizeof(Vector512<double>)
                        * Search._pseudoHashKeyLengthCount
                        * (_batchCharCount + 1)
                );

            _hashCache = (PartialSeedHashCache*)Marshal.AllocHGlobal(sizeof(PartialSeedHashCache));
            *_hashCache = new PartialSeedHashCache(search, &_hashes[0]);
        }

        internal override void SearchSequentialBatch(long batchIdx)
        {
            // Figure out which digits this search is doing
            for (int i = _nonBatchCharCount - 1; i >= 0; i--)
            {
                int charIndex = (int)(batchIdx % MotelyGlobals.SeedDigits.Length);
                _digits[MotelyGlobals.MaxSeedLength - i - 1] = MotelyGlobals.SeedDigits[charIndex];
                batchIdx /= MotelyGlobals.SeedDigits.Length;
            }

            Vector512<double>* hashes = &_hashes[
                _batchCharCount * Search._pseudoHashKeyLengthCount
            ];

            // Calculate hash for the first digits at all the required pseudohash lengths
            for (
                int pseudohashKeyIdx = 0;
                pseudohashKeyIdx < Search._pseudoHashKeyLengthCount;
                pseudohashKeyIdx++
            )
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = MotelyGlobals.MaxSeedLength - 1; i > _batchCharCount - 1; i--)
                {
                    num =
                        (
                            1.1239285023 / num * _digits[i] * Math.PI
                            + (i + pseudohashKeyLength + 1) * Math.PI
                        ) % 1;
                }

                // We only need to write to the first lane because that's the only one that we need
                *(double*)&hashes[pseudohashKeyIdx] = num;
            }

            // Start searching
            for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
            {
                SearchVector(_batchCharCount - 1, SeedDigitVectors[vectorIndex], hashes, 0);
            }
        }

        internal override void SearchProviderBatch()
        {
            throw new InvalidOperationException(
                $"{nameof(MotelySequentialSearchPlan)} does not support provider batch search."
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SearchVector(
            int i,
            Vector512<double> seedDigitVector,
            Vector512<double>* nums,
            int numsLaneIndex
        )
        {
            // Check for cancellation/disposal periodically to make large batches responsive
            if (
                Volatile.Read(ref Search._isDisposed) != 0
                || Search._cancellationToken.IsCancellationRequested
            )
            {
                return;
            }

            Vector512<double>* hashes = &_hashes[i * Search._pseudoHashKeyLengthCount];

            for (
                int pseudohashKeyIdx = 0;
                pseudohashKeyIdx < Search._pseudoHashKeyLengthCount;
                pseudohashKeyIdx++
            )
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];
                Vector512<double> calcVector = Vector512.Create(
                    1.1239285023 / ((double*)&nums[pseudohashKeyIdx])[numsLaneIndex]
                );

                calcVector = Vector512.Multiply(calcVector, seedDigitVector);

                calcVector = Vector512.Multiply(calcVector, Math.PI);
                calcVector = Vector512.Add(
                    calcVector,
                    Vector512.Create((i + pseudohashKeyLength + 1) * Math.PI)
                );

                Vector512<double> intPart = Vector512.Floor(calcVector);
                calcVector = Vector512.Subtract(calcVector, intPart);

                hashes[pseudohashKeyIdx] = calcVector;
            }

            if (i == 0)
            {
                // The one leaf every sequential seed passes through, once per 8-wide vector. Count
                // here rather than deriving from completed batches, so a run that stops mid-batch
                // reports what it looked at. The final vector is zero-padded — 35 digits don't fill
                // 5 lanes of 8, see the lane loop below breaking on 0 — so count carrying lanes.
                _localSeedsSearched +=
                    MotelyGlobals.MaxVectorWidth
                    - BitOperations.PopCount(
                        Vector512
                            .Equals(seedDigitVector, Vector512<double>.Zero)
                            .ExtractMostSignificantBits()
                    );

                SearchSeeds(
                    new MotelySearchContextParams(
                        _hashCache,
                        MotelyGlobals.MaxSeedLength,
                        MotelyGlobals.MaxSeedLength - 1,
                        &_digits[1],
                        &seedDigitVector
                    )
                );
            }
            else
            {
                for (int lane = 0; lane < MotelyGlobals.MaxVectorWidth; lane++)
                {
                    if (seedDigitVector[lane] == 0)
                        break;

                    _digits[i] = (char)seedDigitVector[lane];

                    for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
                    {
                        SearchVector(i - 1, SeedDigitVectors[vectorIndex], hashes, lane);
                        // Abort loop immediately if cancellation occurred in recursive call
                        if (
                            Volatile.Read(ref Search._isDisposed) != 0
                            || Search._cancellationToken.IsCancellationRequested
                        )
                        {
                            return;
                        }
                    }
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();

            _hashCache->Dispose();
            Marshal.FreeHGlobal((nint)_hashCache);

            Marshal.FreeHGlobal((nint)_digits);
            Marshal.FreeHGlobal((nint)_hashes);
        }
    }
}
