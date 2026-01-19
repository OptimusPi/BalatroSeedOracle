using System;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Log levels for BSO logging (from most to least severe)
/// </summary>
public enum BsoLogLevel
{
    Error = 0, // Critical errors that should always be logged
    Warning = 1, // Warnings about potential issues
    Important = 2, // Important operational information
    Debug = 3, // Debug-level diagnostic information
    Verbose =
        4 // Verbose/trace-level information
    ,
}

/// <summary>
/// Production-capable logging helper with runtime-configurable log levels
/// </summary>
public static class DebugLogger
{
    private static BsoLogLevel _minimumLevel = BsoLogLevel.Warning; // Default: errors + warnings in Release
    private static IPlatformServices? _platformServices;

    /// <summary>
    /// Initialize DebugLogger with platform services (called from App.axaml.cs)
    /// </summary>
    public static void Initialize(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    /// <summary>
    /// Sets the minimum log level (messages below this level are suppressed)
    /// </summary>
    public static void SetMinimumLevel(BsoLogLevel level)
    {
        _minimumLevel = level;
    }

    /// <summary>
    /// Gets the current minimum log level
    /// </summary>
    public static BsoLogLevel GetMinimumLevel() => _minimumLevel;

    /// <summary>
    /// Legacy: Sets whether debug logging is enabled (maps to Debug level)
    /// </summary>
    [Obsolete("Use SetMinimumLevel(BsoLogLevel.Debug) instead")]
    public static void SetDebugEnabled(bool enabled)
    {
        _minimumLevel = enabled ? BsoLogLevel.Debug : BsoLogLevel.Warning;
    }

    /// <summary>
    /// Legacy: Sets whether verbose logging is enabled (maps to Verbose level)
    /// </summary>
    [Obsolete("Use SetMinimumLevel(BsoLogLevel.Verbose) instead")]
    public static void SetVerboseEnabled(bool enabled)
    {
        _minimumLevel = enabled ? BsoLogLevel.Verbose : BsoLogLevel.Warning;
    }

    /// <summary>
    /// Logs a debug message with timestamp (Debug level)
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
        if (_minimumLevel >= BsoLogLevel.Debug)
        {
            LogInternal(null, message, BsoLogLevel.Debug);
        }
    }

    /// <summary>
    /// Logs a debug message with category and timestamp (Debug level)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The message to log</param>
    public static void Log(string category, string message)
    {
        if (_minimumLevel >= BsoLogLevel.Debug)
        {
            LogInternal(category, message, BsoLogLevel.Debug);
        }
    }

    /// <summary>
    /// Logs an error message (Error level - always logged unless explicitly disabled)
    /// </summary>
    /// <param name="message">The error message to log</param>
    public static void LogError(string message)
    {
        if (_minimumLevel >= BsoLogLevel.Error)
        {
            LogInternal(null, $"ERROR: {message}", BsoLogLevel.Error);
        }
    }

    /// <summary>
    /// Logs an error message with category (Error level - always logged unless explicitly disabled)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The error message to log</param>
    public static void LogError(string category, string message)
    {
        if (_minimumLevel >= BsoLogLevel.Error)
        {
            LogInternal(category, $"ERROR: {message}", BsoLogLevel.Error);
        }
    }

    /// <summary>
    /// Logs a warning message (Warning level)
    /// </summary>
    /// <param name="message">The warning message to log</param>
    public static void LogWarning(string message)
    {
        if (_minimumLevel >= BsoLogLevel.Warning)
        {
            LogInternal(null, $"WARNING: {message}", BsoLogLevel.Warning);
        }
    }

    /// <summary>
    /// Logs a warning message with category (Warning level)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The warning message to log</param>
    public static void LogWarning(string category, string message)
    {
        if (_minimumLevel >= BsoLogLevel.Warning)
        {
            LogInternal(category, $"WARNING: {message}", BsoLogLevel.Warning);
        }
    }

    /// <summary>
    /// Internal logging implementation to avoid duplication and handle platform differences
    /// </summary>
    private static void LogInternal(string? category, string message, BsoLogLevel level)
    {
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        var formattedMessage =
            category != null ? $"[{timestamp}] [{category}] {message}" : $"[{timestamp}] {message}";

        if (_platformServices != null)
        {
            // Use WriteLog for production-relevant messages (Error, Warning, Important)
            // Use WriteDebugLog for debug/verbose messages
            if (level <= BsoLogLevel.Important)
            {
                _platformServices.WriteLog(formattedMessage);
            }
            else
            {
                _platformServices.WriteDebugLog(formattedMessage);
            }
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
    /// Logs important information (Important level)
    /// </summary>
    /// <param name="category">The category/component name</param>
    /// <param name="message">The important message to log</param>
    public static void LogImportant(string category, string message)
    {
        if (_minimumLevel >= BsoLogLevel.Important)
        {
            LogInternal(category, $"INFO: {message}", BsoLogLevel.Important);
        }
    }

    /// <summary>
    /// Logs formatted message with parameters (Debug level)
    /// </summary>
    public static void LogFormat(string category, string format, params object[] args)
    {
        if (_minimumLevel >= BsoLogLevel.Debug)
        {
            var message = string.Format(format, args);
            LogInternal(category, message, BsoLogLevel.Debug);
        }
    }
}
