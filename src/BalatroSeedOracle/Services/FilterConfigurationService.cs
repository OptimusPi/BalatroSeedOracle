using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    public interface IFilterConfigurationService
    {
        JamlRootDocument BuildConfigFromSelections(
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

        public JamlRootDocument BuildConfigFromSelections(
            List<string> selectedMust,
            List<string> selectedShould,
            List<string> selectedMustNot,
            Dictionary<string, ItemConfig> itemConfigs,
            string filterName = "",
            string filterDescription = ""
        )
        {
            var config = new JamlRootDocument
            {
                Deck = "Red",
                Stake = "White",
                Name = string.IsNullOrEmpty(filterName) ? "Untitled Filter" : filterName,
                Description = filterDescription,
                DateCreated = DateTime.UtcNow.ToString("o"),
                Author = _userProfileService.GetAuthorName(),
            };

            config.Must = new List<JamlClauseUnion>();
            config.Should = new List<JamlClauseUnion>();
            config.MustNot = new List<JamlClauseUnion>();

            FixUniqueKeyParsing(selectedMust, config.Must, itemConfigs, 0);
            FixUniqueKeyParsing(selectedShould, config.Should, itemConfigs, 1);
            FixUniqueKeyParsing(selectedMustNot, config.MustNot, itemConfigs, 0);

            DebugLogger.Log(
                "FilterConfigurationService",
                $"Built config: {config.Must.Count} must, {config.Should.Count} should, {config.MustNot.Count} mustNot"
            );
            return config;
        }

        private void FixUniqueKeyParsing(
            List<string> items,
            List<JamlClauseUnion> targetList,
            Dictionary<string, ItemConfig> itemConfigs,
            int defaultScore = 0
        )
        {
            foreach (var item in items)
            {
                if (itemConfigs.ContainsKey(item))
                {
                    var itemConfig = itemConfigs[item];

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
                        continue;
                    }
                }

                var colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    var category = item.Substring(0, colonIndex);
                    var itemNameWithSuffix = item.Substring(colonIndex + 1);

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
                        filterItem.Score = hasConfig ? itemConfig.Score : defaultScore;
                        targetList.Add(filterItem);
                    }
                }
            }
        }

        private JamlClauseUnion? CreateOperatorClause(ItemConfig operatorConfig)
        {
            if (string.IsNullOrEmpty(operatorConfig.OperatorType))
                return null;

            var operatorType = operatorConfig.OperatorType.ToUpper() == "OR" ? "or" : "and";

            var operatorClause = new JamlClauseUnion
            {
                Clauses = new List<JamlClauseUnion>(),
                Mode = !string.IsNullOrEmpty(operatorConfig.Mode) ? operatorConfig.Mode : "Max",
            };

            if (operatorType == "or")
                operatorClause.Or = operatorClause.Clauses;
            else
                operatorClause.And = operatorClause.Clauses;

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

        private JamlClauseUnion? ConvertItemConfigToClause(ItemConfig config)
        {
            var clause = new JamlClauseUnion
            {
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min,
            };

            var normalizedType = config.ItemType.ToLower();
            switch (normalizedType)
            {
                case "joker":
                    clause.SetDiscriminator("joker", config.ItemName);
                    if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                        clause.SetEditionString(config.Edition);
                    break;
                case "souljoker":
                    clause.SetDiscriminator("legendaryJoker", config.ItemName);
                    break;
                case "tarot":
                    clause.SetDiscriminator("tarotCard", config.ItemName);
                    break;
                case "spectral":
                    clause.SetDiscriminator("spectralCard", config.ItemName);
                    break;
                case "planet":
                    clause.SetDiscriminator("planetCard", config.ItemName);
                    break;
                case "voucher":
                    clause.SetDiscriminator("voucher", config.ItemName);
                    break;
                case "smallblindtag":
                case "bigblindtag":
                case "tag":
                    clause.SetDiscriminator((config.TagType ?? "smallBlindTag"), config.ItemName);
                    break;
                case "boss":
                    clause.SetDiscriminator("boss", config.ItemName);
                    break;
                case "playingcard":
                    clause.SetDiscriminator("standardCard", config.ItemName);
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

        private JamlClauseUnion? CreateFilterItemFromSelection(
            string category,
            string itemName,
            ItemConfig config
        )
        {
            var filterItem = new JamlClauseUnion
            {
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min,
            };

            var normalizedCategory = category.ToLower();

            switch (normalizedCategory)
            {
                case "jokers":
                    filterItem.SetDiscriminator("joker", itemName);
                    if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
                        filterItem.SetEditionString(config.Edition);
                    break;
                case "souljokers":
                    filterItem.SetDiscriminator("legendaryJoker", itemName);
                    break;
                case "tarots":
                    filterItem.SetDiscriminator("tarotCard", itemName);
                    break;
                case "spectrals":
                    filterItem.SetDiscriminator("spectralCard", itemName);
                    break;
                case "planets":
                    filterItem.SetDiscriminator("planetCard", itemName);
                    break;
                case "vouchers":
                    filterItem.SetDiscriminator("voucher", itemName);
                    break;
                case "tags":
                    filterItem.SetDiscriminator((config.TagType ?? "smallBlindTag"), itemName);
                    break;
                case "bosses":
                    filterItem.SetDiscriminator("boss", itemName);
                    break;
                case "playingcards":
                    filterItem.SetDiscriminator("standardCard", itemName);
                    break;
                default:
                    DebugLogger.LogError(
                        "FilterConfigurationService",
                        $"Unknown category: {category}"
                    );
                    return null;
            }

            bool canHaveSources =
                normalizedCategory == "jokers"
                || normalizedCategory == "souljokers"
                || normalizedCategory == "tarots"
                || normalizedCategory == "spectrals"
                || normalizedCategory == "planets"
                || normalizedCategory == "playingcards";

            if (canHaveSources && config.Sources != null)
            {
                filterItem.Sources = new JamlSources();

                if (config.Sources is Dictionary<string, List<int>> sourcesDict)
                {
                    if (sourcesDict.ContainsKey("shopSlots"))
                        filterItem.Sources.ShopItems = sourcesDict["shopSlots"].ToArray();
                    if (sourcesDict.ContainsKey("packSlots"))
                        filterItem.Sources.BoosterPacks = sourcesDict["packSlots"].ToArray();
                }
            }

            return filterItem;
        }
    }
}
