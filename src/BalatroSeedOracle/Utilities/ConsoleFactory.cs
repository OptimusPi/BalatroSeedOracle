using System;

namespace BalatroSeedOracle.Utilities;

/// <summary>
/// Platform-agnostic console interface for BSO logging operations
/// </summary>
public interface IBsoConsole
{
    /// <summary>
    /// Gets or sets whether the console is enabled
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Sets the bottom status line (desktop only)
    /// </summary>
    /// <param name="bottomLine">Text to display on bottom line, or null to clear</param>
    void SetBottomLine(string? bottomLine);

    /// <summary>
    /// Writes a line to the console
    /// </summary>
    /// <typeparam name="T">Type of message</typeparam>
    /// <param name="message">Message to write</param>
    void WriteLine<T>(T message);

    /// <summary>
    /// Writes a line to the console
    /// </summary>
    /// <param name="message">Message to write</param>
    void WriteLine(string? message);
}

/// <summary>
/// Factory for creating platform-appropriate console implementations
/// </summary>
public static class ConsoleFactory
{
    private static IBsoConsole? _instance;

    /// <summary>
    /// Gets the appropriate console implementation for the current platform
    /// </summary>
    public static IBsoConsole GetConsole()
    {
        if (_instance == null)
        {
            // Use reflection to check if we're running in browser
            var isBrowser =
                System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser")
                || System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("WASM");

            if (isBrowser)
            {
                // Create a simple browser console implementation
                _instance = new SimpleBrowserConsole();
            }
            else
            {
                // For now, use a simple console that avoids the problematic FancyConsole
                _instance = new SimpleConsole();
            }
        }

        return _instance;
    }

    /// <summary>
    /// Forces a specific console implementation (for testing)
    /// </summary>
    public static void SetConsole(IBsoConsole console)
    {
        _instance = console;
    }
}

/// <summary>
/// Simple browser console implementation
/// </summary>
public class SimpleBrowserConsole : IBsoConsole
{
    public bool IsEnabled { get; set; } = true;

    public void SetBottomLine(string? bottomLine)
    {
        if (IsEnabled && bottomLine != null)
        {
            WriteLine($"[STATUS] {bottomLine}");
        }
    }

    public void WriteLine<T>(T message)
    {
        WriteLine(message?.ToString() ?? null);
    }

    public void WriteLine(string? message)
    {
        if (!IsEnabled)
            return;
        Helpers.DebugLogger.Log("BSO", message ?? "null");
    }
}

/// <summary>
/// Simple desktop console implementation
/// </summary>
public class SimpleConsole : IBsoConsole
{
    public bool IsEnabled { get; set; } = true;

    public void SetBottomLine(string? bottomLine)
    {
        if (IsEnabled && bottomLine != null)
        {
            WriteLine($"[STATUS] {bottomLine}");
        }
    }

    public void WriteLine<T>(T message)
    {
        WriteLine(message?.ToString() ?? null);
    }

    public void WriteLine(string? message)
    {
        if (!IsEnabled)
            return;
        Helpers.DebugLogger.Log("BSO", message ?? "null");
    }
}
