using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using BalatroSeedOracle;
using BalatroSeedOracle.Browser.Services;
using BalatroSeedOracle.Services;
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

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
