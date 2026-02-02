using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;
using Motely;
using Motely.DB;
using Motely.Filters;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation: uses Motely.DB.SequentialLibrary for metadata, loads JAML in BSO.
/// </summary>
public sealed class RestoreActiveSearchesProviderService : IRestoreActiveSearchesProvider
{
    /// <inheritdoc />
    public Task<List<RestoredSearchInfo>> RestoreAsync(string jamlFiltersDir)
    {
        var restored = new List<RestoredSearchInfo>();

        try
        {
            var activeIds = SequentialLibrary.Instance.GetAllActiveSearchIds();

            foreach (var searchId in activeIds)
            {
                try
                {
                    var meta = SequentialLibrary.Instance.GetSearchMeta(searchId);
                    if (meta is null)
                        continue;

                    var jamlPath = Path.Combine(jamlFiltersDir, $"{meta.JamlFilter}.jaml");
                    if (!File.Exists(jamlPath))
                    {
                        SequentialLibrary.Instance.SetSearchActive(searchId, false);
                        continue;
                    }

                    if (!JamlConfigLoader.TryLoadFromJaml(jamlPath, out var config, out _) || config is null)
                    {
                        SequentialLibrary.Instance.SetSearchActive(searchId, false);
                        continue;
                    }

                    config.Deck = meta.Deck;
                    config.Stake = meta.Stake;

                    restored.Add(new RestoredSearchInfo
                    {
                        SearchId = searchId,
                        FilterName = meta.JamlFilter ?? "Unknown",
                        Deck = meta.Deck ?? "Red",
                        Stake = meta.Stake ?? "White",
                        LastSeed = meta.LastSeed,
                        TotalSeedsProcessed = meta.TotalSeedsProcessed,
                        TotalMatches = meta.TotalMatches,
                        Config = config,
                    });
                }
                catch
                {
                    // Skip broken entries
                }
            }
        }
        catch
        {
            // Return whatever we have
        }

        return Task.FromResult(restored);
    }
}
