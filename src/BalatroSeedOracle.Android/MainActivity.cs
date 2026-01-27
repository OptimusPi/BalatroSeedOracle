using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using BalatroSeedOracle;
using BalatroSeedOracle.Android.Services;
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
        // Register Android-specific services following Avalonia cross-platform pattern
        PlatformServices.RegisterServices = services =>
        {
            // Android-specific implementations (in Android/Services folder)
            services.AddSingleton<IAppDataStore, AndroidAppDataStore>();
            services.AddSingleton<IDuckDBService, AndroidDuckDBService>();
            services.AddSingleton<IPlatformServices, AndroidPlatformServices>();
            services.AddSingleton<IAudioManager, AndroidAudioManager>();
            services.AddSingleton<IExcelExporter, AndroidExcelExporter>();
        };

        return base.CustomizeAppBuilder(builder);
    }
}
