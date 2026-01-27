using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using BalatroSeedOracle;
using BalatroSeedOracle.Desktop.Services;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Android;

[Activity(
    Label = "Balatro Seed Oracle",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register Android services - Android has file system access like Desktop
        PlatformServices.RegisterServices = services =>
        {
            // Use Desktop implementations (Android has file system access like Desktop)
            services.AddSingleton<IAppDataStore, DesktopAppDataStore>();
            services.AddSingleton<IDuckDBService, DesktopDuckDBService>();
            services.AddSingleton<IPlatformServices, DesktopPlatformServices>();

            // Audio (Android supports audio via SoundFlow)
            services.AddSingleton<IAudioManager, SoundFlowAudioManager>();
            services.AddSingleton<SoundEffectsService>();

            // Excel export
            services.AddSingleton<IExcelExporter, ClosedXmlExcelExporter>();
        };

        return base.CustomizeAppBuilder(builder);
    }
}
