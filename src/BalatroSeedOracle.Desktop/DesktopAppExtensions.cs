using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using Motely.DB;
using Motely.Executors;

namespace BalatroSeedOracle.Desktop;

/// <summary>
/// Desktop-specific initialization for App.
/// </summary>
public static class DesktopAppInitializer
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        App.PlatformSpecificInitialization = async () =>
        {
            try
            {
                await InitializeSearchLibraryAsync();
                DebugLogger.Log("App", "Desktop initialization complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("App", $"Failed desktop init: {ex.Message}");
            }
        };
    }

    private static async Task InitializeSearchLibraryAsync()
    {
        try
        {
            MotelySearchOrchestrator.SetRepository(new MotelyRepository());

            MultiSearchManager.Instance.SetTotalThreads(Environment.ProcessorCount);
            DebugLogger.Log("App", $"Thread budget: {Environment.ProcessorCount}");

            var searchManager = App.GetService<SearchManager>();
            if (searchManager != null)
            {
                var restoredSearches = await searchManager.RestoreActiveSearchesAsync(AppPaths.FiltersDir);
                if (restoredSearches.Count > 0)
                {
                    DebugLogger.Log("App", $"Found {restoredSearches.Count} searches to restore");
                    foreach (var search in restoredSearches)
                        DebugLogger.Log("App", $"  - {search.FilterName} ({search.Deck}/{search.Stake}) @ seed {search.LastSeed}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to initialize search library: {ex.Message}");
        }
    }
}
