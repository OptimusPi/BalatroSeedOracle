using System;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Debug logging helper to replace Console.WriteLine calls
/// </summary>
public static class DebugLogger
{
    // Set to false for production builds
    private static bool EnableDebugLogging = false; // Disabled by default
    private static bool EnableVerboseLogging = false; // For extra verbose output

    /// <summary>
    /// Sets whether debug logging is enabled
    /// </summary>
    public static void SetDebugEnabled(bool enabled)
    {
        EnableDebugLogging = enabled;
    }

    /// <summary>
    /// Sets whether verbose logging is enabled
    /// </summary>
    public static void SetVerboseEnabled(bool enabled)
    {
        EnableVerboseLogging = enabled;
    }

    /// <summary>
    /// Logs a debug message with timestamp if debug logging is enabled
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
#if DEBUG
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
#endif
    }

    /// <summary>
    /// Logs a debug message with category and timestamp if debug logging is enabled
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The message to log</param>
    public static void Log(string category, string message)
    {
#if DEBUG
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] {message}");
        }
#endif
    }

    /// <summary>
    /// Logs an error message (only in DEBUG builds)
    /// </summary>
    /// <param name="message">The error message to log</param>
    public static void LogError(string message)
    {
#if DEBUG
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ERROR: {message}");
#endif
    }

    /// <summary>
    /// Logs an error message with category (only in DEBUG builds)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The error message to log</param>
    public static void LogError(string category, string message)
    {
#if DEBUG
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] ERROR: {message}");
#endif
    }

    /// <summary>
    /// Logs important information (only shown if verbose logging is enabled)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The important message to log</param>
    public static void LogImportant(string category, string message)
    {
#if DEBUG
        if (EnableVerboseLogging || EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] INFO: {message}");
        }
#endif
    }

    /// <summary>
    /// Logs formatted message with parameters
    /// </summary>
    public static void LogFormat(string category, string format, params object[] args)
    {
#if DEBUG
        if (EnableDebugLogging)
        {
            Console.WriteLine(
                $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] {string.Format(format, args)}"
            );
        }
#endif
    }
}
