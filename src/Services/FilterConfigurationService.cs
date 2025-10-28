using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Shared service for converting between visual selections and Motely JSON config
    /// Extracted from the original FiltersModal BuildMotelyJsonConfigFromSelections logic
    /// </summary>
    public interface IFilterConfigurationService
    {
        MotelyJsonConfig BuildConfigFromSelections(
            List<string> selectedMust,
            List<string> selectedShould,
            List<string> selectedMustNot,
            Dictionary<string, ItemConfig> itemConfigs,
            string filterName = "",
            string filterDescription = ""
        );
    }

    public class FilterConfigurationService : IFilterConfigurationService
    {
        private readonly UserProfileService _userProfileService;

        public FilterConfigurationService(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        public MotelyJsonConfig BuildConfigFromSelections(
            List<string> selectedMust,
            List<string> selectedShould,
            List<string> selectedMustNot,
            Dictionary<string, ItemConfig> itemConfigs,
            string filterName = "",
            string filterDescription = ""
        )
        {
            var config = new MotelyJsonConfig
            {
                Deck = "Red", // Default deck
                Stake = "White", // Default stake
                Name = string.IsNullOrEmpty(filterName) ? "Untitled Filter" : filterName,
                Description = filterDescription,
                DateCreated = DateTime.UtcNow,
                Author = _userProfileService.GetAuthorName(),
            };

            // Initialize collections
            config.Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
            config.Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
            config.MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>();

            // Convert all items using the helper method that handles unique keys
            FixUniqueKeyParsing(selectedMust, config.Must, itemConfigs, 0);
            FixUniqueKeyParsing(selectedShould, config.Should, itemConfigs, 1);
            FixUniqueKeyParsing(selectedMustNot, config.MustNot, itemConfigs, 0);

            DebugLogger.Log(
                "FilterConfigurationService",
                $"Built config: {config.Must.Count} must, {config.Should.Count} should, {config.MustNot.Count} mustNot"
            );
            return config;
        }

        // COMPLETE method copied from original FiltersModal.FixUniqueKeyParsing()
        private void FixUniqueKeyParsing(
            List<string> items,
            List<MotelyJsonConfig.MotleyJsonFilterClause> targetList,
            Dictionary<string, ItemConfig> itemConfigs,
            int defaultScore = 0
        )
        {
            foreach (var item in items)
            {
                // Handle both formats: "Category:Item" and "Category:Item#123"
                var colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    var category = item.Substring(0, colonIndex);
                    var itemNameWithSuffix = item.Substring(colonIndex + 1);

                    // Remove the unique key suffix if present
                    var hashIndex = itemNameWithSuffix.IndexOf('#');
                    var itemName =
                        hashIndex > 0
                            ? itemNameWithSuffix.Substring(0, hashIndex)
                            : itemNameWithSuffix;

                    var hasConfig = itemConfigs.ContainsKey(item);
                    var itemConfig = hasConfig ? itemConfigs[item] : new ItemConfig();

                    var filterItem = CreateFilterItemFromSelection(category, itemName, itemConfig);
                    if (filterItem != null)
                    {
                        // Preserve user-specified score from ItemConfig when available; otherwise use default per list
                        filterItem.Score = hasConfig ? itemConfig.Score : defaultScore;
                        targetList.Add(filterItem);
                    }
                }
            }
        }

        // SIMPLIFIED version of CreateFilterItemFromSelection for the service
        private MotelyJsonConfig.MotleyJsonFilterClause? CreateFilterItemFromSelection(
            string category,
            string itemName,
            ItemConfig config
        )
        {
            var filterItem = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min,
            };

            var normalizedCategory = category.ToLower();

            // Set type and value based on category
            switch (normalizedCategory)
            {
                case "jokers":
                    filterItem.Type = "joker";
                    filterItem.Value = itemName;
                    if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                    {
                        filterItem.Edition = config.Edition;
                    }
                    break;

                case "souljokers":
                    filterItem.Type = "souljoker";
                    filterItem.Value = itemName;
                    break;

                case "tarots":
                    filterItem.Type = "tarotcard";
                    filterItem.Value = itemName;
                    break;

                case "spectrals":
                    filterItem.Type = "spectralcard";
                    filterItem.Value = itemName;
                    break;

                case "planets":
                    filterItem.Type = "planetcard";
                    filterItem.Value = itemName;
                    break;

                case "vouchers":
                    filterItem.Type = "voucher";
                    filterItem.Value = itemName;
                    break;

                case "tags":
                    filterItem.Type = config.TagType ?? "smallblindtag";
                    filterItem.Value = itemName;
                    break;

                case "bosses":
                    filterItem.Type = "boss";
                    filterItem.Value = itemName;
                    break;

                case "playingcards":
                    filterItem.Type = "playingcard";
                    filterItem.Value = itemName;
                    break;

                default:
                    DebugLogger.LogError(
                        "FilterConfigurationService",
                        $"Unknown category: {category}"
                    );
                    return null;
            }

            // Add Sources for items that support them
            bool canHaveSources =
                normalizedCategory == "jokers"
                || normalizedCategory == "souljokers"
                || normalizedCategory == "tarots"
                || normalizedCategory == "spectrals"
                || normalizedCategory == "planets"
                || normalizedCategory == "playingcards";

            if (canHaveSources && config.Sources != null)
            {
                filterItem.Sources = new MotelyJsonConfig.SourcesConfig();

                if (config.Sources is Dictionary<string, List<int>> sourcesDict)
                {
                    if (sourcesDict.ContainsKey("shopSlots"))
                        filterItem.Sources.ShopSlots = sourcesDict["shopSlots"].ToArray();
                    if (sourcesDict.ContainsKey("packSlots"))
                        filterItem.Sources.PackSlots = sourcesDict["packSlots"].ToArray();
                }
            }

            return filterItem;
        }
    }
}
