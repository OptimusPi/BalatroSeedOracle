using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BalatroSeedOracle;
using BalatroSeedOracle.Browser.Services;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        // Configure logging for browser (check for log level in URL query params or env)
        ConfigureLogging();

        // Register Browser-specific services
        PlatformServices.RegisterServices = services =>
        {
            // Platform-specific implementations
            services.AddSingleton<IAppDataStore, BrowserLocalStorageAppDataStore>();
            services.AddSingleton<IDuckDBService, BrowserDuckDBService>();
            services.AddSingleton<IPlatformServices>(sp =>
            {
                var store = sp.GetRequiredService<IAppDataStore>();
                return new BrowserPlatformServices(store);
            });

            // API host
            services.AddSingleton<IApiHostService, BrowserApiHostService>();

            // Note: Audio services (SoundFlowAudioManager, SoundEffectsService) are not available in browser
        };

        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    private static void ConfigureLogging()
    {
        // Default log level for browser (Release builds: warnings only)
#if DEBUG
        var defaultLevel = BsoLogLevel.Debug;
#else
        var defaultLevel = BsoLogLevel.Warning;
#endif

        var logLevel = defaultLevel;

        // Browser can check environment variable (if WASM runtime supports it)
        try
        {
            var envLogLevel = Environment.GetEnvironmentVariable("BSO_LOG_LEVEL");
            if (
                !string.IsNullOrEmpty(envLogLevel)
                && TryParseLogLevel(envLogLevel, out var parsedLevel)
            )
            {
                logLevel = parsedLevel;
            }
        }
        catch
        {
            // Environment variables may not be available in browser
        }

        DebugLogger.SetMinimumLevel(logLevel);
        System.Console.WriteLine($"BSO Browser Logging configured: {logLevel}");
    }

    private static bool TryParseLogLevel(string value, out BsoLogLevel level)
    {
        switch (value.ToLowerInvariant())
        {
            case "error":
                level = BsoLogLevel.Error;
                return true;
            case "warning":
            case "warn":
                level = BsoLogLevel.Warning;
                return true;
            case "important":
            case "info":
                level = BsoLogLevel.Important;
                return true;
            case "debug":
                level = BsoLogLevel.Debug;
                return true;
            case "verbose":
            case "trace":
                level = BsoLogLevel.Verbose;
                return true;
            default:
                level = BsoLogLevel.Warning;
                return false;
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
