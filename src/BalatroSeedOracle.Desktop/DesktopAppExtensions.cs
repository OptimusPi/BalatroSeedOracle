using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using BalatroSeedOracle.Desktop.Views;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using Motely.DB;
using Motely.Executors;

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

        // Set the platform-specific initialization callback (awaited from App after intro transition)
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
                        DebugLogger.LogError(
                            "App",
                            $"Failed to initialize desktop widgets: {ex.Message}"
                        );
                    }
                }
                else
                {
                    DebugLogger.LogError("App", "MainWindow not set - this should not happen");
                }
            }
        };
    }

    /// <summary>
    /// Initialize SequentialLibrary and restore active searches.
    /// </summary>
    private static async Task InitializeSearchLibraryAsync()
    {
        try
        {
            MotelySearchOrchestrator.SetRepository(new MotelyRepository());

            // Set thread budget for MultiSearchManager
            MultiSearchManager.Instance.SetTotalThreads(Environment.ProcessorCount);
            DebugLogger.Log("App", $"Thread budget set to: {Environment.ProcessorCount}");

            // Restore active searches from the database
            var searchManager = App.GetService<SearchManager>();
            if (searchManager != null)
            {
                var jamlFiltersDir = AppPaths.FiltersDir;
                var restoredSearches = await searchManager.RestoreActiveSearchesAsync(
                    jamlFiltersDir
                );

                if (restoredSearches.Count > 0)
                {
                    DebugLogger.Log("App", $"Found {restoredSearches.Count} searches to restore");

                    // For now, just log that we found them
                    // The user can manually resume through the UI
                    foreach (var search in restoredSearches)
                    {
                        DebugLogger.Log(
                            "App",
                            $"  - {search.FilterName} ({search.Deck}/{search.Stake}) @ seed {search.LastSeed}"
                        );
                    }

                    // TODO: Auto-create SearchWidget for each restored search
                    // This requires access to the MainMenu's WidgetDock
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to initialize search library: {ex.Message}");
        }
    }
}
