using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using Motely.Filters;

namespace BalatroSeedOracle.Browser.Interop
{
    public partial class SearchInterop
    {
        [JSExport]
        public static async Task<string> StartSingleSeedSearch(string seed, string jamlConfigJson)
        {
            try
            {
                DebugLogger.Log("SearchInterop", $"Starting single seed search for: {seed}");

                // Get SearchManager
                var searchManager = App.GetService<SearchManager>();
                if (searchManager == null)
                {
                    DebugLogger.LogError("SearchInterop", "SearchManager not found");
                    return "error: SearchManager not found";
                }

                // Parse Config using AOT-compatible source-generated context
                var config = JsonSerializer.Deserialize(
                    jamlConfigJson,
                    MotelyJsonSerializerContext.Default.MotelyJsonConfig
                );

                if (config == null)
                {
                    DebugLogger.LogError("SearchInterop", "Failed to parse JAML config");
                    return "error: Invalid JAML config";
                }

                // Create Criteria
                var criteria = new SearchCriteria
                {
                    DebugSeed = seed,
                    ThreadCount = 1,
                    BatchSize = 1,
                    Deck = config.Deck ?? "Red",
                    Stake = config.Stake ?? "White",
                };

                // Start Search
                var context = await searchManager.StartSearchAsync(criteria, config);
                return context.SearchId;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchInterop", $"Search failed: {ex.Message}");
                return $"error: {ex.Message}";
            }
        }

        [JSExport]
        public static void CancelSearch(string searchId)
        {
            var searchManager = App.GetService<SearchManager>();
            var context = searchManager?.GetSearch(searchId);
            if (context != null)
            {
                context.Stop();
            }
        }
    }
}
