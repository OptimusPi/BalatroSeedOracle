using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BalatroSeedOracle;
using BalatroSeedOracle.Browser.Services;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;
using BalatroSeedOracle.Services.DuckDB;
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
            services.AddSingleton<IDuckDBService, BrowserDuckDBService>();
            services.AddSingleton<IPlatformServices>(sp => 
            {
                var store = sp.GetRequiredService<IAppDataStore>();
                return new BrowserPlatformServices(store);
            });
            
            // API host
            services.AddSingleton<IApiHostService, BrowserApiHostService>();
            
            // Browser audio using Web Audio API (full implementation in SoundFlowAudioManager)
            // SoundFlowAudioManager has browser implementation in #else block
            services.AddSingleton<IAudioManager>(sp => BalatroSeedOracle.Services.SoundFlowAudioManager.Instance);
            services.AddSingleton<BalatroSeedOracle.Services.SoundEffectsService>();
        };

        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
