using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

namespace BalatroSeedOracle;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Set up global exception handlers BEFORE Avalonia starts
            // These catch exceptions that would otherwise crash the app silently
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Configure logging level from environment variable or CLI args
            ConfigureLogging(args);

            // Start Avalonia
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Last resort crash handler - write to file
            var crashLog = GetCrashLogPath();
            var errorMsg =
                $"=== FATAL CRASH: {DateTime.Now} ===\n"
                + $"Exception: {ex.GetType().FullName}\n"
                + $"Message: {ex.Message}\n"
                + $"Stack Trace:\n{ex.StackTrace}\n"
                + $"Inner Exception: {ex.InnerException?.Message ?? "None"}\n\n";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
                File.AppendAllText(crashLog, errorMsg);
                Console.Error.WriteLine($"Fatal error logged to: {crashLog}");
            }
            catch
            {
                // If we can't even write the crash log, just print to console
                Console.Error.WriteLine(errorMsg);
            }

            Console.Error.WriteLine($"\n\nFATAL ERROR: {ex.Message}");
            Console.Error.WriteLine($"See crash log: {crashLog}");
            Console.Error.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
            throw; // Re-throw to ensure proper exit code
        }
    }

    /// <summary>
    /// Handles unhandled exceptions on any thread (including async void methods)
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var crashLog = GetCrashLogPath();

        var errorMsg =
            $"=== UNHANDLED EXCEPTION: {DateTime.Now} ===\n"
            + $"Is Terminating: {e.IsTerminating}\n"
            + $"Exception: {ex?.GetType().FullName ?? "Unknown"}\n"
            + $"Message: {ex?.Message ?? "No message"}\n"
            + $"Stack Trace:\n{ex?.StackTrace ?? "No stack trace"}\n\n";

        // Log to debug logger
        Helpers.DebugLogger.LogError("UNHANDLED", $"Unhandled exception: {ex?.Message}");

        // Write to crash log
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
            File.AppendAllText(crashLog, errorMsg);
        }
        catch
        {
            Console.Error.WriteLine(errorMsg);
        }

        if (e.IsTerminating)
        {
            // App is crashing - try to save user data
            Console.Error.WriteLine($"\n\n❌ APP CRASHING: {ex?.Message}");
            Console.Error.WriteLine($"Crash log: {crashLog}\n");

            // Try to flush user profile before exit
            try
            {
                var userProfile = App.GetService<Services.UserProfileService>();
                userProfile?.FlushProfile();
            }
            catch
            {
                // Ignore errors during emergency save
            }
        }
    }

    /// <summary>
    /// Handles unobserved Task exceptions (fire-and-forget tasks that throw)
    /// </summary>
    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e
    )
    {
        var crashLog = GetCrashLogPath();

        var errorMsg =
            $"=== UNOBSERVED TASK EXCEPTION: {DateTime.Now} ===\n"
            + $"Exception: {e.Exception.GetType().FullName}\n"
            + $"Message: {e.Exception.Message}\n"
            + $"Stack Trace:\n{e.Exception.StackTrace}\n\n";

        // Log to debug logger
        Helpers.DebugLogger.LogError(
            "UNOBSERVED_TASK",
            $"Unobserved task exception: {e.Exception.Message}"
        );

        // Write to crash log
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
            File.AppendAllText(crashLog, errorMsg);
        }
        catch
        {
            Console.Error.WriteLine(errorMsg);
        }

        // Mark as observed to prevent app crash
        // This allows the app to continue running, but we've logged the error
        e.SetObserved();
    }

    private static void ConfigureLogging(string[] args)
    {
        // Default log level based on build configuration
#if DEBUG
        var defaultLevel = Helpers.BsoLogLevel.Debug; // Debug builds: show debug logs
#else
        var defaultLevel = Helpers.BsoLogLevel.Warning; // Release builds: errors + warnings only
#endif

        var logLevel = defaultLevel;

        // Check environment variable first
        var envLogLevel = Environment.GetEnvironmentVariable("BSO_LOG_LEVEL");
        if (!string.IsNullOrEmpty(envLogLevel))
        {
            if (TryParseLogLevel(envLogLevel, out var parsedLevel))
            {
                logLevel = parsedLevel;
            }
            else
            {
                Console.Error.WriteLine(
                    $"Warning: Invalid BSO_LOG_LEVEL value '{envLogLevel}'. Using default: {defaultLevel}"
                );
            }
        }

        // Check CLI args (overrides environment variable)
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--log-level=", StringComparison.OrdinalIgnoreCase))
            {
                var value = args[i].Substring("--log-level=".Length);
                if (TryParseLogLevel(value, out var parsedLevel))
                {
                    logLevel = parsedLevel;
                }
                else
                {
                    Console.Error.WriteLine(
                        $"Warning: Invalid --log-level value '{value}'. Using default: {defaultLevel}"
                    );
                }
                break;
            }
        }

        Helpers.DebugLogger.SetMinimumLevel(logLevel);
        Console.WriteLine($"BSO Logging configured: {logLevel}");
    }

    private static bool TryParseLogLevel(string value, out Helpers.BsoLogLevel level)
    {
        switch (value.ToLowerInvariant())
        {
            case "error":
                level = Helpers.BsoLogLevel.Error;
                return true;
            case "warning":
            case "warn":
                level = Helpers.BsoLogLevel.Warning;
                return true;
            case "important":
            case "info":
                level = Helpers.BsoLogLevel.Important;
                return true;
            case "debug":
                level = Helpers.BsoLogLevel.Debug;
                return true;
            case "verbose":
            case "trace":
                level = Helpers.BsoLogLevel.Verbose;
                return true;
            default:
                level = Helpers.BsoLogLevel.Warning;
                return false;
        }
    }

    /// <summary>
    /// Gets the path to the crash log file
    /// </summary>
    private static string GetCrashLogPath()
    {
        try
        {
            // Try to use AppPaths if available
            return Path.Combine(Helpers.AppPaths.DataRootDir, "crash.log");
        }
        catch
        {
            // Fallback if AppPaths fails during startup
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "BalatroSeedOracle", "crash.log");
        }
    }

    // Avalonia configuration, this method is called by the platform-specific entry points
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
