using System;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Debug logging helper to replace Console.WriteLine calls
/// </summary>
public static class DebugLogger
{
    // Set to false for production builds - AI AGENT COMPATIBLE
    private static bool EnableDebugLogging = false; // DEFAULT TO FALSE FOR AI COMPATIBILITY
    private static bool EnableVerboseLogging = false; // DEFAULT TO FALSE FOR AI COMPATIBILITY
    private static IPlatformServices? _platformServices;

    /// <summary>
    /// Initialize DebugLogger with platform services (called from App.axaml.cs)
    /// </summary>
    public static void Initialize(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

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
            LogInternal(null, message);
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
            LogInternal(category, message);
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
        LogInternal(null, $"ERROR: {message}");
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
        LogInternal(category, $"ERROR: {message}");
#endif
    }

    /// <summary>
    /// Internal logging implementation to avoid duplication and handle platform differences
    /// </summary>
    private static void LogInternal(string? category, string message)
    {
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        var formattedMessage =
            category != null ? $"[{timestamp}] [{category}] {message}" : $"[{timestamp}] {message}";

        if (_platformServices != null)
        {
            _platformServices.WriteLog(formattedMessage);
        }
        else
        {
            // Fallback if not initialized (shouldn't happen in normal operation)
            try
            {
                Console.WriteLine(formattedMessage);
            }
            catch
            {
                // Last resort: nothing we can do without potentially crashing
            }
        }
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
            var formattedMessage = $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] INFO: {message}";
            if (_platformServices != null)
            {
                _platformServices.WriteDebugLog(formattedMessage);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(formattedMessage);
            }
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
            var msg =
                $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{category}] {string.Format(format, args)}";
            if (_platformServices != null)
            {
                _platformServices.WriteDebugLog(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg);
            }
        }
#endif
    }
}
