using System;

namespace Oracle.Helpers;

/// <summary>
/// Debug logging helper to replace Console.WriteLine calls
/// </summary>
public static class DebugLogger
{
    // Set to false for production builds
    private static bool EnableDebugLogging = false;  // Disabled by default
    private static bool EnableVerboseLogging = false;  // For extra verbose output

    /// <summary>
    /// Sets whether debug logging is enabled
    /// </summary>
    public static void SetDebugEnabled(bool enabled)
    {
        EnableDebugLogging = enabled;
    }

    /// <summary>
    /// Logs a debug message with timestamp if debug logging is enabled
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }

    /// <summary>
    /// Logs a debug message with category and timestamp if debug logging is enabled
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The message to log</param>
    public static void Log(string category, string message)
    {
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{category}] {message}");
        }
    }

    /// <summary>
    /// Logs an error message (always shown regardless of debug setting)
    /// </summary>
    /// <param name="message">The error message to log</param>
    public static void LogError(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {message}");
    }

    /// <summary>
    /// Logs an error message with category (always shown regardless of debug setting)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The error message to log</param>
    public static void LogError(string category, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{category}] ERROR: {message}");
    }

    /// <summary>
    /// Logs important information (only shown if verbose logging is enabled)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The important message to log</param>
    public static void LogImportant(string category, string message)
    {
        if (EnableVerboseLogging || EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{category}] INFO: {message}");
        }
    }

    /// <summary>
    /// Logs formatted message with parameters
    /// </summary>
    public static void LogFormat(string category, string format, params object[] args)
    {
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{category}] {string.Format(format, args)}");
        }
    }
}