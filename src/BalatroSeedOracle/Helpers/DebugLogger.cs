using System;
using System.IO;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Logs to stderr and debug.log in the run directory. That's it.
/// </summary>
public static class DebugLogger
{
    private static readonly object Lock = new();

    public static void Log(string message) => Write(null, message);

    public static void Log(string category, string message) => Write(category, message);

    public static void LogError(string message) => Write(null, "ERROR: " + message);

    public static void LogError(string category, string message) =>
        Write(category, "ERROR: " + message);

    public static void LogImportant(string category, string message) => Write(category, message);

    public static void LogFormat(string category, string format, params object[] args) =>
        Write(category, string.Format(format, args));

    private static void Write(string? category, string message)
    {
        var line = category is null
            ? $"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}"
            : $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] {message}";
        Console.Error.WriteLine(line);
        lock (Lock)
        {
            File.AppendAllText("debug.log", line + Environment.NewLine);
        }
    }
}
