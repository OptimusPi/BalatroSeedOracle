using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BalatroSeedOracle.Services;
using Microsoft.Extensions.Logging;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Application-wide diagnostic logger. Every line goes to:
/// <list type="bullet">
/// <item>an in-memory ring (<see cref="RecentEntries"/>) for diagnostics panes and crash dumps;</item>
/// <item>a rolling log file under <see cref="AppDataPaths.LogsDir"/>
/// (bso-yyyyMMdd.log, rolled at <see cref="MaxFileBytes"/>, newest <see cref="MaxLogFiles"/> kept);</item>
/// <item>an injected <see cref="ILogger"/> when the host wires one up.</item>
/// </list>
/// The file is flushed immediately for warnings/errors and at least once a second otherwise.
/// Logging never throws: if the file cannot be opened the file sink switches itself off.
/// </summary>
public static class DebugLogger
{
    private const int MaxEntries = 2000;

    /// <summary>A log file is rolled over once it grows past this size.</summary>
    public const long MaxFileBytes = 5 * 1024 * 1024;

    /// <summary>How many bso-*.log files are kept in the logs directory.</summary>
    public const int MaxLogFiles = 10;

    private static readonly object Sync = new();
    private static readonly LinkedList<string> Entries = new();

    private static StreamWriter? _fileWriter;
    private static string? _filePath;
    private static DateTime _fileDate;
    private static bool _fileSinkDisabled;
    private static Timer? _flushTimer;
    private static bool _dirty;

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

    /// <summary>The log file currently being written, or null before the first line / when disabled.</summary>
    public static string? LogFilePath
    {
        get
        {
            lock (Sync)
                return _filePath;
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

    /// <summary>Push buffered lines to disk now.</summary>
    public static void Flush()
    {
        lock (Sync)
            FlushFileUnsafe();
    }

    /// <summary>
    /// Close the current file so the next line re-resolves <see cref="AppDataPaths.LogsDir"/>
    /// (used when the data directory is redirected, e.g. in tests).
    /// </summary>
    public static void ResetFileSink()
    {
        lock (Sync)
        {
            CloseFileUnsafe();
            _fileSinkDisabled = false;
        }
    }

    private static void Write(string level, string source, string message)
    {
        var now = DateTime.Now;
        var line = $"{now:HH:mm:ss.fff} [{level}] {source}: {message}";
        lock (Sync)
        {
            Entries.AddLast(line);
            while (Entries.Count > MaxEntries)
                Entries.RemoveFirst();

            WriteToFileUnsafe(now, line, flushNow: level is "ERROR" or "WARN" or "IMPORTANT");
        }
        try
        {
            var logger = Logger;
            if (logger is not null)
            {
                var logLevel = level switch
                {
                    "ERROR" => LogLevel.Error,
                    "WARN" => LogLevel.Warning,
                    _ => LogLevel.Information,
                };
                logger.Log(logLevel, "{Line}", line);
            }
        }
        catch
        {
            // Logging must never throw.
        }
    }

    // ---- file sink (all *Unsafe members require Sync to be held) ----

    private static void WriteToFileUnsafe(DateTime now, string line, bool flushNow)
    {
        if (_fileSinkDisabled)
            return;
        try
        {
            if (_fileWriter is null || now.Date != _fileDate)
                OpenFileUnsafe(now);
            else if (_fileWriter.BaseStream.Length > MaxFileBytes)
                RollFileUnsafe(now);

            if (_fileWriter is null)
                return;

            _fileWriter.Write(now.ToString("yyyy-MM-dd "));
            _fileWriter.WriteLine(line);
            _dirty = true;
            if (flushNow)
                FlushFileUnsafe();
        }
        catch
        {
            // A broken disk must not take the app down; stop trying after the first failure.
            CloseFileUnsafe();
            _fileSinkDisabled = true;
        }
    }

    private static void OpenFileUnsafe(DateTime now)
    {
        CloseFileUnsafe();
        var dir = AppDataPaths.LogsDir;
        Directory.CreateDirectory(dir);
        PruneOldFilesUnsafe(dir);

        var path = Path.Combine(dir, $"bso-{now:yyyyMMdd}.log");
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        _fileWriter = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = false };
        _filePath = path;
        _fileDate = now.Date;
        _fileWriter.WriteLine(
            $"{now:yyyy-MM-dd HH:mm:ss.fff} [INFO] DebugLogger: ---- log opened (pid {Environment.ProcessId}) ----"
        );
        _fileWriter.Flush();

        _flushTimer ??= new Timer(_ => Flush(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private static void RollFileUnsafe(DateTime now)
    {
        var current = _filePath;
        CloseFileUnsafe();
        if (current is not null && File.Exists(current))
        {
            var rolled = Path.Combine(
                Path.GetDirectoryName(current)!,
                $"bso-{now:yyyyMMdd-HHmmss-fff}.log"
            );
            File.Move(current, rolled, overwrite: true);
        }
        OpenFileUnsafe(now);
    }

    private static void PruneOldFilesUnsafe(string dir)
    {
        try
        {
            var stale = new DirectoryInfo(dir)
                .GetFiles("bso-*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(MaxLogFiles - 1);
            foreach (var file in stale)
                file.Delete();
        }
        catch
        {
            // Retention is best effort.
        }
    }

    private static void FlushFileUnsafe()
    {
        if (!_dirty || _fileWriter is null)
            return;
        try
        {
            _fileWriter.Flush();
            _dirty = false;
        }
        catch
        {
            CloseFileUnsafe();
            _fileSinkDisabled = true;
        }
    }

    private static void CloseFileUnsafe()
    {
        try
        {
            _fileWriter?.Flush();
            _fileWriter?.Dispose();
        }
        catch
        {
            // Ignore: we are closing anyway.
        }
        _fileWriter = null;
        _filePath = null;
        _dirty = false;
    }

    private static void OnProcessExit(object? sender, EventArgs e) => Flush();
}
