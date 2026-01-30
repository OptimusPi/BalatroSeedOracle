using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BalatroSeedOracle;
using BalatroSeedOracle.Browser.Services;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        // Register Browser-specific services
        PlatformServices.RegisterServices = services =>
        {
            // Platform-specific implementations
            services.AddSingleton<IAppDataStore, BrowserLocalStorageAppDataStore>();
            // IDuckDBService removed - Motely now owns all database operations
            services.AddSingleton<IPlatformServices>(sp =>
            {
                var store = sp.GetRequiredService<IAppDataStore>();
                return new BrowserPlatformServices(store);
            });

            // IApiHostService: NOT registered - API hosting not supported in browser
            // Consumers must handle null service (Avalonia best practice: no stub implementations)

            // Excel export (stub - not implemented for browser yet)
            services.AddSingleton<IParquetExporter, BrowserParquetExporter>();
            // Database export (.db / .ducklake) available on Desktop only
            services.AddSingleton<IResultsDatabaseExporter, BrowserResultsDatabaseExporter>();

            // Browser audio using Web Audio API
            services.AddSingleton<IAudioManager, BrowserAudioManager>();
        };

        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
