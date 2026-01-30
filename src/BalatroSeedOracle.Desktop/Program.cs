using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using BalatroSeedOracle;
using BalatroSeedOracle.Desktop.Services;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Desktop;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Set up global exception handlers BEFORE Avalonia starts
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Enable debug logging
            DebugLogger.SetDebugEnabled(true);

            // Initialize Desktop-specific app features
            DesktopAppInitializer.Initialize();

            // Register Desktop-specific services
            PlatformServices.RegisterServices = services =>
            {
                // Platform-specific implementations
                services.AddSingleton<IAppDataStore, Desktop.Services.DesktopAppDataStore>();
                // IDuckDBService removed - Motely now owns all database operations
                services.AddSingleton<
                    IPlatformServices,
                    Desktop.Services.DesktopPlatformServices
                >();

                // Desktop-only services
                services.AddSingleton<IAudioManager, DesktopAudioManager>();
                services.AddSingleton<SoundEffectsService>();
                services.AddSingleton<IParquetExporter, ParquetExporter>();
                services.AddSingleton<IResultsDatabaseExporter, ResultsDatabaseExporter>();

                // API host
                services.AddSingleton<IApiHostService, DesktopApiHostService>();
            };

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
                Console.Error.WriteLine(errorMsg);
            }

            Console.Error.WriteLine($"\n\nFATAL ERROR: {ex.Message}");
            Console.Error.WriteLine($"See crash log: {crashLog}");
            Console.Error.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
            throw;
        }
    }

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

        DebugLogger.LogError("UNHANDLED", $"Unhandled exception: {ex?.Message}");

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
            Console.Error.WriteLine($"\n\nAPP CRASHING: {ex?.Message}");
            Console.Error.WriteLine($"Crash log: {crashLog}\n");

            try
            {
                var userProfile = App.GetService<UserProfileService>();
                userProfile?.FlushProfile();
            }
            catch
            {
                // Ignore errors during emergency save
            }
        }
    }

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

        DebugLogger.LogError(
            "UNOBSERVED_TASK",
            $"Unobserved task exception: {e.Exception.Message}"
        );

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
            File.AppendAllText(crashLog, errorMsg);
        }
        catch
        {
            Console.Error.WriteLine(errorMsg);
        }

        e.SetObserved();
    }

    private static string GetCrashLogPath()
    {
        try
        {
            return Path.Combine(AppPaths.DataRootDir, "crash.log");
        }
        catch
        {
            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create
            );
            return Path.Combine(localAppData, "BalatroSeedOracle", "crash.log");
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().WithInterFont();
}
