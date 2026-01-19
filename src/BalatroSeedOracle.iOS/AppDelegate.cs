using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;
using BalatroSeedOracle;
using BalatroSeedOracle.Desktop.Services;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;
using BalatroSeedOracle.Services.DuckDB;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register iOS services - iOS has file system access like Desktop
        PlatformServices.RegisterServices = services =>
        {
            // Use Desktop implementations (iOS has file system access)
            services.AddSingleton<IAppDataStore, Desktop.Services.DesktopAppDataStore>();
            services.AddSingleton<IDuckDBService, Desktop.Services.DesktopDuckDBService>();
            services.AddSingleton<IPlatformServices, Desktop.Services.DesktopPlatformServices>();
            
            // Audio (iOS supports audio via SoundFlow)
            services.AddSingleton<IAudioManager, Services.SoundFlowAudioManager>();
            services.AddSingleton<Services.SoundEffectsService>();
        };

        return base.CustomizeAppBuilder(builder);
    }
}
