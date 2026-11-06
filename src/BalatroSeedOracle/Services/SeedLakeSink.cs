using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Motely;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Per-search "seed lake": the on-disk destination for every matching seed, so results
/// survive regardless of UI state. One plain file per search id under SearchResults/
/// (<c>SearchResults/&lt;id&gt;.csv</c>), the same plain-file convention Motely uses for seed lists;
/// <c>SearchModalViewModel.ReconnectFromLake</c> reads it back line by line.
/// <para>
/// Unscored matches are written as a bare seed string per line (<c>SEED</c>). Scored matches
/// are written as <c>SEED,SCORE[,TALLY0,TALLY1...]</c> so <c>ReconnectFromLake</c> restores
/// both scores and tally breakdowns.
/// </para>
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
                    if (TryParseLine(line, out var seed, out _, out _))
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

    /// <summary>
    /// Parse a lake line (<c>SEED</c> or <c>SEED,SCORE[,TALLY0...]</c>).
    /// </summary>
    public static bool TryParseLine(string? line, out string seed, out int score, out int[]? tallies)
    {
        seed = string.Empty;
        score = 0;
        tallies = null;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var trimmed = line.Trim();
        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex < 0)
        {
            seed = trimmed;
            return seed.Length > 0;
        }

        var parts = trimmed.Split(',');
        seed = parts[0].Trim();
        if (seed.Length == 0)
            return false;

        if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedScore))
        {
            score = parsedScore;
        }

        if (parts.Length > 2)
        {
            tallies = new int[parts.Length - 2];
            for (var i = 2; i < parts.Length; i++)
            {
                int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out tallies[i - 2]);
            }
        }

        return true;
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

    public void OnScored(in MotelyScoredSeedResult result)
    {
        var seed = result.Seed?.Trim();
        if (string.IsNullOrEmpty(seed))
            return;

        var tallies = result.TallyValuesSpan;
        if (result.Score == 0 && tallies.IsEmpty)
        {
            WriteLine(seed, seed);
            return;
        }

        var sb = new StringBuilder(seed.Length + 16);
        sb.Append(seed).Append(',').Append(result.Score.ToString(CultureInfo.InvariantCulture));
        for (var i = 0; i < tallies.Length; i++)
        {
            sb.Append(',').Append(tallies[i].ToString(CultureInfo.InvariantCulture));
        }
        WriteLine(seed, sb.ToString());
    }

    public void OnSeed(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            return;
        var trimmed = seed.Trim();
        WriteLine(trimmed, trimmed);
    }

    private void WriteLine(string seedKey, string formattedLine)
    {
        lock (_sync)
        {
            if (_writer is null || !_known.Add(seedKey))
                return;
            try
            {
                _writer.WriteLine(formattedLine);
            }
            catch (Exception ex)
            {
                // The lake must never take the search down; report the first failure only.
                _known.Remove(seedKey);
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
                // See WriteLine: never throw into the search.
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
