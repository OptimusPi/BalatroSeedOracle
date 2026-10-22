using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Application-wide diagnostic logger.
/// TIER0-STUB: replace in Tier 1. Static in-memory ring sink plus console echo; optionally
/// forwards to an injected <see cref="ILogger"/> when the host wires one up.
/// </summary>
public static class DebugLogger
{
    private const int MaxEntries = 2000;

    private static readonly object Sync = new();
    private static readonly LinkedList<string> Entries = new();

    /// <summary>Optional host logger; null until an host injects one.</summary>
    public static ILogger? Logger { get; set; }

    /// <summary>Most recent log lines, oldest first (diagnostics pane / crash dumps).</summary>
    public static IReadOnlyList<string> RecentEntries
    {
        get
        {
            lock (Sync)
            {
                var copy = new string[Entries.Count];
                Entries.CopyTo(copy, 0);
                return copy;
            }
        }
    }

    public static void Log(string source, string message) => Write("INFO", source, message);

    /// <summary>Single-argument convenience overload: the message is the only payload.</summary>
    public static void Log(string message) => Write("INFO", "App", message);

    public static void LogImportant(string source, string message) => Write("IMPORTANT", source, message);

    public static void LogError(string source, string message) => Write("ERROR", source, message);

    /// <summary>Single-argument convenience overload: the message is the only payload.</summary>
    public static void LogError(string message) => Write("ERROR", "App", message);

    public static void LogWarning(string source, string message) => Write("WARN", source, message);

    private static void Write(string level, string source, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {source}: {message}";
        lock (Sync)
        {
            Entries.AddLast(line);
            while (Entries.Count > MaxEntries)
                Entries.RemoveFirst();
        }
        try
        {
            Logger?.LogInformation("{Line}", line);
        }
        catch
        {
            // Logging must never throw.
        }
    }
}
