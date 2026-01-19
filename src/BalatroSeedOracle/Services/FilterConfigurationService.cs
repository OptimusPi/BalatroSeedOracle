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
            config.Must = new List<MotelyJsonConfig.MotelyJsonFilterClause>();
            config.Should = new List<MotelyJsonConfig.MotelyJsonFilterClause>();
            config.MustNot = new List<MotelyJsonConfig.MotelyJsonFilterClause>();

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
            List<MotelyJsonConfig.MotelyJsonFilterClause> targetList,
            Dictionary<string, ItemConfig> itemConfigs,
            int defaultScore = 0
        )
        {
            foreach (var item in items)
            {
                // Check if this is an operator by looking up the config directly
                if (itemConfigs.ContainsKey(item))
                {
                    var itemConfig = itemConfigs[item];

                    // Handle operator items specially
                    if (
                        itemConfig.ItemType == "Operator"
                        && !string.IsNullOrEmpty(itemConfig.OperatorType)
                    )
                    {
                        var operatorClause = CreateOperatorClause(itemConfig);
                        if (operatorClause != null)
                        {
                            targetList.Add(operatorClause);
                        }
                        continue; // Skip normal processing for operators
                    }
                }

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

        /// <summary>
        /// Creates a MotleyJsonFilterClause for an operator (Or/And) with nested child clauses
        /// </summary>
        private MotelyJsonConfig.MotelyJsonFilterClause? CreateOperatorClause(
            ItemConfig operatorConfig
        )
        {
            if (string.IsNullOrEmpty(operatorConfig.OperatorType))
                return null;

            // Convert "OR"/"AND" to "Or"/"And" for JSON format
            var operatorType = operatorConfig.OperatorType.ToUpper() == "OR" ? "Or" : "And";

            var operatorClause = new MotelyJsonConfig.MotelyJsonFilterClause
            {
                Type = operatorType, // "Or" or "And"
                Clauses = new List<MotelyJsonConfig.MotelyJsonFilterClause>(),
            };

            // Set Mode for Or/And operators (default to "Max" if not specified)
            if (operatorType == "Or" || operatorType == "And")
            {
                operatorClause.Mode = !string.IsNullOrEmpty(operatorConfig.Mode)
                    ? operatorConfig.Mode
                    : "Max";
            }

            // Convert each child ItemConfig to a MotleyJsonFilterClause
            if (operatorConfig.Children != null)
            {
                foreach (var childConfig in operatorConfig.Children)
                {
                    var childClause = ConvertItemConfigToClause(childConfig);
                    if (childClause != null)
                    {
                        operatorClause.Clauses.Add(childClause);
                    }
                }
            }

            DebugLogger.Log(
                "FilterConfigurationService",
                $"Created {operatorConfig.OperatorType} operator with {operatorClause.Clauses?.Count ?? 0} children"
            );

            return operatorClause;
        }

        /// <summary>
        /// Converts an ItemConfig to a MotleyJsonFilterClause
        /// </summary>
        private MotelyJsonConfig.MotelyJsonFilterClause? ConvertItemConfigToClause(
            ItemConfig config
        )
        {
            var antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var normalizedType = config.ItemType.ToLower();

            MotelyJsonConfig.MotelyJsonFilterClause clause;
            switch (normalizedType)
            {
                case "joker":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "Joker",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                        Edition =
                            (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                                ? config.Edition
                                : null,
                    };
                    break;

                case "souljoker":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "SoulJoker",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "tarot":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "TarotCard",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "spectral":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "SpectralCard",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "planet":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "PlanetCard",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "voucher":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "Voucher",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "smallblindtag":
                case "bigblindtag":
                case "tag":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = config.TagType ?? "SmallBlindTag",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "boss":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "Boss",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "playingcard":
                    clause = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "PlayingCard",
                        Value = config.ItemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                default:
                    DebugLogger.LogError(
                        "FilterConfigurationService",
                        $"Unknown ItemType: {config.ItemType}"
                    );
                    return null;
            }

            return clause;
        }

        // SIMPLIFIED version of CreateFilterItemFromSelection for the service
        private MotelyJsonConfig.MotelyJsonFilterClause? CreateFilterItemFromSelection(
            string category,
            string itemName,
            ItemConfig config
        )
        {
            var normalizedCategory = category.ToLower();
            var antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            MotelyJsonConfig.MotelyJsonFilterClause filterItem;

            // Set type and value based on category
            switch (normalizedCategory)
            {
                case "jokers":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "joker",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                        Edition =
                            (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                                ? config.Edition
                                : null,
                    };
                    break;

                case "souljokers":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "souljoker",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "tarots":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "tarotcard",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "spectrals":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "spectralcard",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "planets":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "planetcard",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "vouchers":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "voucher",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "tags":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = config.TagType ?? "smallblindtag",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "bosses":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "boss",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
                    break;

                case "playingcards":
                    filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
                    {
                        Type = "playingcard",
                        Value = itemName,
                        Antes = antes,
                        Min = config.Min,
                    };
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
                filterItem.Sources = new SourcesConfig();

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
