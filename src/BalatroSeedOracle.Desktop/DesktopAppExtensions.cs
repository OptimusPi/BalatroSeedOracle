using System;
using Avalonia.Controls.ApplicationLifetimes;
using BalatroSeedOracle.Desktop.Views;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Desktop;

/// <summary>
/// Desktop-specific initialization for App.
/// Sets up platform-specific widgets after MainWindow is created.
/// Pattern: "folder thing" — widget init is triggered only from Desktop (via PlatformSpecificInitialization); no shared provider.
/// No polling: ShowLoadingWindowAndPreloadSprites sets desktop.MainWindow synchronously before any await,
/// so MainWindow is available when PlatformSpecificInitialization runs.
/// </summary>
public static class DesktopAppInitializer
{
    private static bool _initialized = false;

    /// <summary>
    /// Initialize desktop-specific app features.
    /// Call this before starting Avalonia.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        // Set the platform-specific initialization callback
        App.PlatformSpecificInitialization = () =>
        {
            var app = Avalonia.Application.Current;
            if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // No polling needed: ShowLoadingWindowAndPreloadSprites sets desktop.MainWindow synchronously
                // (before any await), so it's available when PlatformSpecificInitialization runs.
                if (desktop.MainWindow is BalatroSeedOracle.Views.MainWindow mainWindow)
                {
                    try
                    {
                        mainWindow.InitializeDesktopWidgets();
                        DebugLogger.Log("App", "Desktop widgets initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("App", $"Failed to initialize desktop widgets: {ex.Message}");
                    }
                }
                else
                {
                    DebugLogger.LogError("App", "MainWindow not set - this should not happen");
                }
            }
        };
    }
}
