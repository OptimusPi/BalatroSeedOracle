using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Motely;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Per-search "seed lake": the on-disk destination for every matching seed, so results
/// survive regardless of UI state. One CSV per search id under SearchResults/.
/// TIER0-STUB: replace in Tier 1. Appends seeds to the CSV (dedupe on read); the full
/// DuckDB-backed lake layer is a Tier 1 feature.
/// </summary>
public sealed class SeedLakeSink : IDisposable
{
    private readonly string _filePath;
    private readonly StreamWriter? _writer;

    public SeedLakeSink(ILogger? logger, string searchId)
    {
        _filePath = SeedFilePath(logger, searchId);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
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
        }
    }

    /// <summary>The lake file for a search id (same answer for instance and static callers).</summary>
    public static string SeedFilePath(ILogger? logger, string searchId)
    {
        var safe = string.Concat(searchId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safe))
            safe = "search";
        return Path.Combine("SearchResults", $"{safe}.csv");
    }

    public string FilePath => _filePath;

    public void OnScored(in MotelyScoredSeedResult result) => WriteSeed(result.Seed);

    public void OnSeed(string seed) => WriteSeed(seed);

    private void WriteSeed(string? seed)
    {
        if (_writer is null || string.IsNullOrWhiteSpace(seed))
            return;
        try
        {
            _writer.WriteLine(seed);
        }
        catch (Exception)
        {
            // The lake must never take the search down.
        }
    }

    public void Flush() => _writer?.Flush();

    public void Dispose() => _writer?.Dispose();
}
