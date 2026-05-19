using System.Runtime.Versioning;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using BalatroSeedOracle;
using BalatroSeedOracle.Android.Services;
using BalatroSeedOracle.Services;
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
public class MainActivity : AvaloniaMainActivity
{
    [SupportedOSPlatform("android24.0")]
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Avalonia 12 dropped AvaloniaMainActivity<TApp>/CustomizeAppBuilder.
        // Register Android-specific services before Avalonia initializes.
        PlatformServices.RegisterServices = services =>
        {
            services.AddSingleton<IAppDataStore, AndroidAppDataStore>();
            services.AddSingleton<IPlatformServices, AndroidPlatformServices>();
            services.AddSingleton<IAudioManager, AndroidAudioManager>();
        };

        base.OnCreate(savedInstanceState);
    }
}
