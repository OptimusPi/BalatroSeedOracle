using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Motely;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Per-search "seed lake": the on-disk destination for every matching seed, so results
/// survive regardless of UI state. One plain file per search id under SearchResults/
/// (<c>SearchResults/&lt;id&gt;.csv</c>, one seed per line), the same plain-file convention
/// Motely uses for seed lists; <c>SearchModalViewModel.ReconnectFromLake</c> reads it back line by line.
/// <para>
/// Seeds are deduplicated on write: the existing file is indexed when the sink opens, so
/// re-running the same filter (same id, same lake) never appends a seed twice. Search
/// worker threads call <see cref="OnScored"/>/<see cref="OnSeed"/> concurrently, so all
/// writes are serialized behind a lock.
/// </para>
/// </summary>
public sealed class SeedLakeSink : IDisposable
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly ILogger? _logger;
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private StreamWriter? _writer;
    private bool _writeFailureLogged;

    public SeedLakeSink(ILogger? logger, string searchId)
    {
        _logger = logger;
        _filePath = SeedFilePath(logger, searchId);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);
            if (File.Exists(_filePath))
            {
                foreach (var line in File.ReadLines(_filePath))
                {
                    var seed = line.Trim();
                    if (seed.Length > 0)
                        _known.Add(seed);
                }
            }
            _writer = new StreamWriter(
                new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read)
            )
            {
                AutoFlush = true,
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "SeedLakeSink: cannot open {Path}: {Message}", _filePath, ex.Message);
            Helpers.DebugLogger.LogError(nameof(SeedLakeSink), $"Cannot open {_filePath}: {ex.Message}");
        }
    }

    /// <summary>The lake file for a search id (same answer for instance and static callers).</summary>
    public static string SeedFilePath(ILogger? logger, string searchId)
    {
        var safe = string.Concat(
            (searchId ?? string.Empty).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
        );
        if (string.IsNullOrWhiteSpace(safe))
            safe = "search";
        return Path.Combine("SearchResults", $"{safe}.csv");
    }

    public string FilePath => _filePath;

    /// <summary>Distinct seeds in the lake (existing file plus everything written since opening).</summary>
    public int Count
    {
        get
        {
            lock (_sync)
                return _known.Count;
        }
    }

    public void OnScored(in MotelyScoredSeedResult result) => WriteSeed(result.Seed);

    public void OnSeed(string seed) => WriteSeed(seed);

    private void WriteSeed(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            return;
        seed = seed.Trim();
        lock (_sync)
        {
            if (_writer is null || !_known.Add(seed))
                return;
            try
            {
                _writer.WriteLine(seed);
            }
            catch (Exception ex)
            {
                // The lake must never take the search down; report the first failure only.
                _known.Remove(seed);
                if (!_writeFailureLogged)
                {
                    _writeFailureLogged = true;
                    _logger?.LogError(ex, "SeedLakeSink: write to {Path} failed", _filePath);
                    Helpers.DebugLogger.LogError(nameof(SeedLakeSink), $"Write to {_filePath} failed: {ex.Message}");
                }
            }
        }
    }

    public void Flush()
    {
        lock (_sync)
        {
            try
            {
                _writer?.Flush();
            }
            catch (Exception)
            {
                // See WriteSeed: never throw into the search.
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            try
            {
                _writer?.Dispose();
            }
            catch (Exception)
            {
                // Disposal during shutdown must not throw.
            }
            _writer = null;
        }
    }
}
