using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using Motely.Filters;
using ItemConfig = BalatroSeedOracle.Models.ItemConfig;

namespace BalatroSeedOracle.Services
{
    public class FilterSerializationService
    {
        private readonly UserProfileService _userProfileService;

        public FilterSerializationService(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        public string SerializeConfig(JamlRootDocument config)
        {
            if (string.IsNullOrWhiteSpace(config.Name))
                config.Name = "Untitled Filter";
            if (string.IsNullOrWhiteSpace(config.Author))
                config.Author = _userProfileService?.GetAuthorName() ?? "Jimbo";
            if (string.IsNullOrWhiteSpace(config.DateCreated))
                config.DateCreated = DateTime.UtcNow.ToString("o");

            return JsonSerializer.Serialize(
                config,
                MotelyJsonSerializerContext.Default.JamlRootDocument
            );
        }

        public JamlRootDocument? DeserializeConfig(string json)
        {
            try
            {
                var config = JsonSerializer.Deserialize(
                    json,
                    MotelyJsonSerializerContext.Default.JamlRootDocument
                );
                if (config == null)
                {
                    DebugLogger.LogError(
                        "FilterSerializationService",
                        "DeserializeConfig returned null"
                    );
                    return null;
                }

                DebugLogger.Log(
                    "FilterSerializationService",
                    $"Deserialized config: Name='{config.Name}', Must={(config.Must?.Count ?? 0)}, Should={(config.Should?.Count ?? 0)}, MustNot={(config.MustNot?.Count ?? 0)}"
                );
                return config;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSerializationService",
                    $"Failed to deserialize config: {ex.Message}"
                );
                return null;
            }
        }

        public JamlRootDocument? DeserializeConfigFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    DebugLogger.LogError(
                        "FilterSerializationService",
                        $"File not found: {filePath}"
                    );
                    return null;
                }

                var text = File.ReadAllText(filePath);
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".yaml" || ext == ".yml" || ext == ".jaml")
                {
                    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                        .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();
                    return deserializer.Deserialize<JamlRootDocument>(text);
                }
                return DeserializeConfig(text);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FilterSerializationService",
                    $"Error loading config from file '{filePath}': {ex.Message}"
                );
                return null;
            }
        }

        public JamlClauseUnion? CreateFilterClause(
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

            bool canHaveSources = IsSourceCapableCategory(normalizedCategory);

            if (canHaveSources && HasValidSources(config))
            {
                filterItem.Sources = new JamlSources
                {
                    ShopItems = config.ShopSlots?.ToArray(),
                    BoosterPacks = config.PackSlots?.ToArray(),
                    Tags = config.SkipBlindTags,
                    RequireMega = config.IsMegaArcana ? true : null,
                };
            }

            filterItem.SetDiscriminator(MapCategoryToType(normalizedCategory, itemName), itemName);

            if (config.Edition != null && config.Edition != "none")
            {
                filterItem.SetEditionString(config.Edition);
            }

            if (config.Stickers?.Count > 0)
            {
                filterItem.SetStickerStrings(config.Stickers.ToArray());
            }

            if (normalizedCategory == "playingcards")
            {
                if (config.Seal != "None")
                    filterItem.SetSealString(config.Seal);
                if (config.Enhancement != "None")
                    filterItem.SetEnhancementString(config.Enhancement);
            }

            return filterItem;
        }

        public void ConvertSelectionsToFilterClauses(
            ObservableCollection<string> items,
            Dictionary<string, ItemConfig> itemConfigs,
            List<JamlClauseUnion> targetList,
            int defaultScore = 0
        )
        {
            foreach (var item in items)
            {
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

                    var itemConfig = itemConfigs.ContainsKey(item)
                        ? itemConfigs[item]
                        : new ItemConfig();

                    var filterItem = CreateFilterClause(category, itemName, itemConfig);
                    if (filterItem != null)
                    {
                        filterItem.Score =
                            itemConfig.Score > 0
                                ? itemConfig.Score
                                : (itemConfig.Antes?.Count ?? Math.Max(defaultScore, 1));
                        targetList.Add(filterItem);
                    }
                }
            }
        }

        private bool IsSourceCapableCategory(string category)
        {
            return category == "jokers"
                || category == "souljokers"
                || category == "tarots"
                || category == "spectrals"
                || category == "planets"
                || category == "playingcards";
        }

        private bool HasValidSources(ItemConfig config)
        {
            return (config.ShopSlots?.Count > 0)
                || (config.PackSlots?.Count > 0)
                || config.SkipBlindTags
                || config.IsMegaArcana;
        }

        private string MapCategoryToType(string category, string itemName)
        {
            switch (category)
            {
                case "souljokers":
                    return "legendaryJoker";
                case "jokers":
                    return "joker";
                case "tarots":
                    return "tarotCard";
                case "planets":
                    return "planetCard";
                case "spectrals":
                    return "spectralCard";
                case "playingcards":
                    return "standardCard";
                case "vouchers":
                    return "voucher";
                case "tags":
                    return "tag";
                case "bosses":
                    return "boss";
                default:
                    return category;
            }
        }
    }
}
