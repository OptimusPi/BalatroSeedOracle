using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Velopack;

namespace BalatroSeedOracle;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize Velopack
        VelopackApp.Build().Run();

        // Enable debug logging
        Helpers.DebugLogger.SetDebugEnabled(true);

        // Start Avalonia
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, this method is called by the platform-specific entry points
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
