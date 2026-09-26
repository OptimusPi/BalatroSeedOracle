using System;
using System.Collections.Generic;
using System.IO;
using Motely;
using Motely.Filters;

namespace BalatroSeedOracle.Services;

/// <summary>
/// One filter's seed archive: bare seeds land in <c>Seeds/&lt;filter&gt;.txt</c>, one per line,
/// deduped in memory. Ported from Motely.DataLake (removed from the engine in Motely 26); the
/// app owns it now. Thread-safe; engine result callbacks fire on every worker thread. Seeds are
/// appended to disk every <see cref="FlushEvery"/> finds and on <see cref="Flush"/> / <see cref="Dispose"/>,
/// so the file is readable while the search runs (the minimized-search reconnect reads it).
/// </summary>
public sealed class SeedLakeSink : IMotelyResultSink
{
    private const int FlushEvery = 256;

    private readonly string _seedFilePath;
    private readonly object _gate = new();
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly List<string> _buffer = [];
    private StreamWriter? _writer;
    private bool _disposed;

    /// <summary>The data root, absolute: <paramref name="root"/>, else <c>MOTELY_DATALAKE_PATH</c>, else <c>Seeds</c>.</summary>
    public static string LakeRoot(string? root)
    {
        root ??= Environment.GetEnvironmentVariable("MOTELY_DATALAKE_PATH");
        return string.IsNullOrWhiteSpace(root) ? "Seeds" : root;
    }

    /// <summary>The plain-text seed file this sink writes to.</summary>
    public static string SeedFilePath(string? root, string filterId) =>
        Path.Combine(LakeRoot(root), filterId + ".txt");

    /// <param name="root">Data root; see <see cref="LakeRoot"/>.</param>
    /// <param name="filterId">The JAML filter id; names the seed file.</param>
    public SeedLakeSink(string? root, string filterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterId);
        _seedFilePath = SeedFilePath(root, filterId);
        if (File.Exists(_seedFilePath))
        {
            foreach (var line in File.ReadLines(_seedFilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    _seen.Add(trimmed);
            }
        }
    }

    /// <summary>Every distinct seed this sink knows: the file's contents plus this run's finds.</summary>
    public int Count
    {
        get { lock (_gate) return _seen.Count; }
    }

    public void OnSeed(string seed) => Write(seed);

    public void OnScored(in MotelyScoredSeedResult tally) => Write(tally.Seed);

    private void Write(string seed)
    {
        if (string.IsNullOrEmpty(seed))
            return;

        lock (_gate)
        {
            if (_disposed)
                return;
            if (!_seen.Add(seed))
                return;
            _buffer.Add(seed);
            if (_buffer.Count >= FlushEvery)
                FlushLocked();
        }
    }

    /// <summary>Push buffered seeds to the text file.</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            FlushLocked();
        }
    }

    private void FlushLocked()
    {
        if (_buffer.Count == 0)
            return;

        if (_writer is null)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(_seedFilePath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            _writer = new StreamWriter(_seedFilePath, append: true) { AutoFlush = false };
        }

        foreach (var seed in _buffer)
            _writer.WriteLine(seed);
        _writer.Flush();
        _buffer.Clear();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            try { FlushLocked(); }
            catch { /* best-effort */ }
            _writer?.Dispose();
            _writer = null;
        }
    }
}
