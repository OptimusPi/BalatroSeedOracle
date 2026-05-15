using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Motely;
using Motely.Filters;

namespace BalatroSeedOracle.Services;

internal sealed class BsoSearchContext : IDisposable
{
    private readonly string _searchId;
    private readonly string _filterId;
    private readonly Stopwatch _stopwatch = new();
    private readonly List<BsoSeedScoreTally> _results = new();
    private readonly object _resultsLock = new();
    private readonly List<string> _columnNames = new();
    private long _totalSeedsSearched;
    private long _matchingSeeds;
    private long _filteredSeeds;
    private IMotelySearch? _search;
    private CancellationTokenSource? _cts;
    private MotelySearchStatus _status = MotelySearchStatus.Pending;

    public BsoSearchContext(string searchId, string filterId, IEnumerable<string>? columnNames = null)
    {
        _searchId = searchId;
        _filterId = filterId;
        if (columnNames != null)
            _columnNames.AddRange(columnNames);
    }

    public string SearchId => _searchId;
    public string FilterId => _filterId;
    public MotelySearchStatus Status => _status;
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;
    public long TotalSeedsSearched => Interlocked.Read(ref _totalSeedsSearched);
    public long MatchingSeeds => Interlocked.Read(ref _matchingSeeds);
    public long FilteredSeeds => Interlocked.Read(ref _filteredSeeds);
    public int ResultCount
    {
        get { lock (_resultsLock) return _results.Count; }
    }
    public IReadOnlyList<string> ColumnNames => _columnNames;

    public void Attach(IMotelySearch search, CancellationTokenSource cts)
    {
        _search = search;
        _cts = cts;
        _status = MotelySearchStatus.Running;
        _stopwatch.Start();
    }

    public void OnResult(string seed, int score, int[]? tallies)
    {
        var entry = new BsoSeedScoreTally { Seed = seed, Score = score, Tallies = tallies };
        lock (_resultsLock)
            _results.Add(entry);
        Interlocked.Increment(ref _matchingSeeds);
    }

    public void OnProgress(long total, long matches, long filtered)
    {
        Interlocked.Exchange(ref _totalSeedsSearched, total);
        Interlocked.Exchange(ref _matchingSeeds, matches);
        Interlocked.Exchange(ref _filteredSeeds, filtered);
    }

    public IReadOnlyList<BsoSeedScoreTally> GetResults(int offset, int count)
    {
        lock (_resultsLock)
        {
            if (offset >= _results.Count) return Array.Empty<BsoSeedScoreTally>();
            return _results.Skip(offset).Take(count).ToArray();
        }
    }

    public IReadOnlyList<BsoSeedScoreTally> GetTopResults(int limit)
    {
        lock (_resultsLock)
            return _results.OrderByDescending(r => r.Score).Take(limit).ToArray();
    }

    public void Start() { /* search already started by Attach */ }

    public void Pause() => _status = MotelySearchStatus.Paused;

    public void Cancel()
    {
        try { _cts?.Cancel(); } catch { }
        _status = MotelySearchStatus.Cancelled;
        _stopwatch.Stop();
    }

    public void MarkCompleted()
    {
        _status = MotelySearchStatus.Completed;
        _stopwatch.Stop();
    }

    public void ExportTo(string outputPath)
    {
        var ext = System.IO.Path.GetExtension(outputPath).ToLowerInvariant();
        IReadOnlyList<BsoSeedScoreTally> snapshot;
        lock (_resultsLock)
            snapshot = _results.ToArray();
        if (ext == ".csv" || ext == ".txt")
        {
            using var w = new System.IO.StreamWriter(outputPath);
            var header = new List<string> { "seed", "score" };
            header.AddRange(_columnNames);
            w.WriteLine(string.Join(",", header));
            foreach (var r in snapshot)
            {
                var cells = new List<string> { r.Seed, r.Score.ToString(System.Globalization.CultureInfo.InvariantCulture) };
                if (r.Tallies != null)
                    foreach (var t in r.Tallies)
                        cells.Add(t.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.WriteLine(string.Join(",", cells));
            }
        }
        else
        {
            throw new NotSupportedException($"Export to {ext} not supported in this build.");
        }
    }

    public void Dispose()
    {
        try { _search?.Dispose(); } catch { }
        try { _cts?.Dispose(); } catch { }
    }
}
