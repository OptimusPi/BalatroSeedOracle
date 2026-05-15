using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using BalatroSeedOracle.Desktop.Views;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop;

public static class DesktopAppInitializer
{
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        App.PlatformSpecificInitialization = async () =>
        {
            var app = Avalonia.Application.Current;
            if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow is BalatroSeedOracle.Views.MainWindow mainWindow)
                {
                    try
                    {
                        mainWindow.InitializeDesktopWidgets();
                        DebugLogger.Log("App", "Desktop widgets initialized successfully");
                        await InitializeSearchLibraryAsync();
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

    private static async Task InitializeSearchLibraryAsync()
    {
        try
        {
            var searchManager = App.GetService<SearchManager>();
            if (searchManager != null)
            {
                var jamlFiltersDir = AppPaths.FiltersDir;
                var restoredSearches = await searchManager.RestoreActiveSearchesAsync(jamlFiltersDir);
                if (restoredSearches.Count > 0)
                {
                    DebugLogger.Log("App", $"Found {restoredSearches.Count} searches to restore");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to initialize search library: {ex.Message}");
        }
    }
}
