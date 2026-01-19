using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;
using Motely.Filters;
using ItemConfig = BalatroSeedOracle.Models.ItemConfig;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service responsible for serializing and deserializing MotelyJsonConfig filters.
    /// Extracted from FiltersModal to reduce god class complexity.
    /// </summary>
    public class FilterSerializationService
    {
        private readonly UserProfileService _userProfileService;

        public FilterSerializationService(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Serializes a MotelyJsonConfig to JSON string with proper formatting
        /// </summary>
        public string SerializeConfig(MotelyJsonConfig config)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(
                stream,
                new JsonWriterOptions { Indented = true }
            );

            writer.WriteStartObject();

            // Metadata
            writer.WriteString(
                "name",
                !string.IsNullOrWhiteSpace(config.Name) ? config.Name : "Untitled Filter"
            );

            // Only write description if it has content
            if (!string.IsNullOrWhiteSpace(config.Description))
            {
                writer.WriteString("description", config.Description);
            }

            var authorName = !string.IsNullOrWhiteSpace(config.Author)
                ? config.Author
                : (_userProfileService?.GetAuthorName() ?? "Jimbo");
            writer.WriteString("author", authorName);

            var created = config.DateCreated ?? DateTime.UtcNow;
            writer.WriteString("dateCreated", created.ToString("o"));

            // Top-level scoring mode (optional)
            if (!string.IsNullOrWhiteSpace(config.Mode))
            {
                writer.WriteString("mode", config.Mode);
            }

            // Must array - always write (even if empty, for easy editing)
            writer.WriteStartArray("must");
            foreach (var item in config.Must ?? new List<MotelyJsonConfig.MotelyJsonFilterClause>())
            {
                WriteFilterItem(writer, item);
            }
            writer.WriteEndArray();

            // Should array - always write (even if empty, for easy editing)
            writer.WriteStartArray("should");
            foreach (
                var item in config.Should ?? new List<MotelyJsonConfig.MotelyJsonFilterClause>()
            )
            {
                WriteFilterItem(writer, item, includeScore: true);
            }
            writer.WriteEndArray();

            // MustNot array - always write (even if empty, for easy editing)
            writer.WriteStartArray("mustNot");
            foreach (
                var item in config.MustNot ?? new List<MotelyJsonConfig.MotelyJsonFilterClause>()
            )
            {
                WriteFilterItem(writer, item);
            }
            writer.WriteEndArray();

            // Deck and Stake - only write if not null
            if (!string.IsNullOrWhiteSpace(config.Deck))
            {
                writer.WriteString("deck", config.Deck);
            }

            if (!string.IsNullOrWhiteSpace(config.Stake))
            {
                writer.WriteString("stake", config.Stake);
            }

            writer.WriteEndObject();
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());

            // Compact number arrays to single line (antes, shopSlots, packSlots)
            return CompactNumberArrays(json);
        }

        /// <summary>
        /// Compacts number arrays (antes, shopSlots, packSlots) to single line for easier editing
        /// Example: "antes": [1,2,3,4,5,6,7,8] instead of multi-line
        /// </summary>
        private string CompactNumberArrays(string json)
        {
            // Regex pattern: "arrayName": [\n  numbers\n]  →  "arrayName": [1,2,3,...]
            var pattern =
                @"""(antes|shopSlots|packSlots)"":\s*\[\s*\n\s*((?:\d+,?\s*\n?\s*)*)\s*\]";

            return System.Text.RegularExpressions.Regex.Replace(
                json,
                pattern,
                match =>
                {
                    var arrayName = match.Groups[1].Value;
                    var numbersText = match.Groups[2].Value;

                    // Extract all numbers
                    var numbers = System
                        .Text.RegularExpressions.Regex.Matches(numbersText, @"\d+")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Value);

                    // Format as: "antes": [1,2,3,4,5,6,7,8]
                    return $"\"{arrayName}\": [{string.Join(",", numbers)}]";
                }
            );
        }

        /// <summary>
        /// Deserializes JSON string to MotelyJsonConfig with hardened options.
        /// Allows comments and trailing commas; case-insensitive property names.
        /// </summary>
        public MotelyJsonConfig? DeserializeConfig(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };

                var config = JsonSerializer.Deserialize<MotelyJsonConfig>(json, options);
                if (config == null)
                {
                    DebugLogger.LogError(
                        "FilterSerializationService",
                        "DeserializeConfig returned null"
                    );
                    return null;
                }

                // Basic sanity logging to aid debugging malformed files
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

        /// <summary>
        /// Safely load a config from a file path using Motely's robust loader.
        /// Falls back to hardened JSON options if Motely loader fails.
        /// </summary>
        public MotelyJsonConfig? DeserializeConfigFromFile(string filePath)
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

                if (MotelyJsonConfig.TryLoadFromJsonFile(filePath, out var config))
                {
                    DebugLogger.Log(
                        "FilterSerializationService",
                        $"Loaded config via Motely loader: {Path.GetFileName(filePath)}"
                    );
                    return config;
                }

                // Fallback: read text and use hardened options
                var json = File.ReadAllText(filePath);
                var fallback = DeserializeConfig(json);
                if (fallback == null)
                {
                    DebugLogger.LogError(
                        "FilterSerializationService",
                        $"Fallback deserialization failed for {filePath}"
                    );
                }
                return fallback;
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

        /// <summary>
        /// Creates a filter clause from selection data
        /// </summary>
        public MotelyJsonConfig.MotelyJsonFilterClause? CreateFilterClause(
            string category,
            string itemName,
            ItemConfig config
        )
        {
            var filterItem = new MotelyJsonConfig.MotelyJsonFilterClause
            {
                Type = "", // Will be set below based on category
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min,
            };

            var normalizedCategory = category.ToLower();

            // Determine if this item type can have sources
            bool canHaveSources = IsSourceCapableCategory(normalizedCategory);

            if (canHaveSources && HasValidSources(config))
            {
                filterItem.Sources = new SourcesConfig
                {
                    ShopSlots = config.ShopSlots?.ToArray(),
                    PackSlots = config.PackSlots?.ToArray(),
                    Tags = config.SkipBlindTags ? true : null,
                    RequireMega = config.IsMegaArcana ? true : null,
                };
            }

            // Map category to type
            filterItem.Type = MapCategoryToType(normalizedCategory, itemName);
            filterItem.Value = itemName;

            // Handle edition for applicable items
            if (config.Edition != null && config.Edition != "none")
            {
                filterItem.Edition = config.Edition;
            }

            // Handle stickers if configured
            if (config.Stickers?.Count > 0)
            {
                filterItem.Stickers = config.Stickers;
            }

            // Handle playing card specific properties
            if (normalizedCategory == "playingcards")
            {
                if (config.Seal != "None")
                    filterItem.Seal = config.Seal;
                if (config.Enhancement != "None")
                    filterItem.Enhancement = config.Enhancement;
            }

            return filterItem;
        }

        /// <summary>
        /// Converts a collection of item selections to filter clauses
        /// </summary>
        public void ConvertSelectionsToFilterClauses(
            ObservableCollection<string> items,
            Dictionary<string, ItemConfig> itemConfigs,
            List<MotelyJsonConfig.MotelyJsonFilterClause> targetList,
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

                    var itemConfig = itemConfigs.ContainsKey(item)
                        ? itemConfigs[item]
                        : new ItemConfig();

                    var filterItem = CreateFilterClause(category, itemName, itemConfig);
                    if (filterItem != null)
                    {
                        // Apply score from config if specified, otherwise use antes-based default
                        filterItem.Score =
                            itemConfig.Score > 0
                                ? itemConfig.Score
                                : (itemConfig.Antes?.Count ?? Math.Max(defaultScore, 1));
                        targetList.Add(filterItem);
                    }
                }
            }
        }

        #region Private Helper Methods

        private void WriteFilterItem(
            Utf8JsonWriter writer,
            MotelyJsonConfig.MotelyJsonFilterClause item,
            bool includeScore = false
        )
        {
            writer.WriteStartObject();

            // Type and value
            writer.WriteString("type", item.Type);
            if (!string.IsNullOrEmpty(item.Value))
            {
                writer.WriteString("value", item.Value);
            }
            else if (item.Values?.Length > 0)
            {
                writer.WriteStartArray("values");
                foreach (var val in item.Values)
                {
                    writer.WriteStringValue(val);
                }
                writer.WriteEndArray();
            }

            // Edition
            if (!string.IsNullOrEmpty(item.Edition))
            {
                writer.WriteString("edition", item.Edition);
            }

            // Antes
            if (item.Antes != null && item.Antes.Length > 0)
            {
                writer.WriteStartArray("antes");
                foreach (var ante in item.Antes)
                {
                    writer.WriteNumberValue(ante);
                }
                writer.WriteEndArray();
            }

            // Sources (only for applicable item types)
            bool canHaveSources =
                item.Type != null
                && !item.Type.Equals("tag", StringComparison.OrdinalIgnoreCase)
                && !item.Type.Equals("voucher", StringComparison.OrdinalIgnoreCase)
                && !item.Type.Equals("boss", StringComparison.OrdinalIgnoreCase);

            if (canHaveSources && item.Sources != null)
            {
                writer.WriteStartObject("sources");

                if (item.Sources.ShopSlots != null && item.Sources.ShopSlots.Length > 0)
                {
                    writer.WriteStartArray("shopSlots");
                    foreach (var slot in item.Sources.ShopSlots)
                    {
                        writer.WriteNumberValue(slot);
                    }
                    writer.WriteEndArray();
                }

                if (item.Sources.PackSlots != null && item.Sources.PackSlots.Length > 0)
                {
                    writer.WriteStartArray("packSlots");
                    foreach (var slot in item.Sources.PackSlots)
                    {
                        writer.WriteNumberValue(slot);
                    }
                    writer.WriteEndArray();
                }

                if (item.Sources.Tags.HasValue)
                {
                    writer.WriteBoolean("tags", item.Sources.Tags.Value);
                }

                if (item.Sources.RequireMega.HasValue)
                {
                    writer.WriteBoolean("requireMega", item.Sources.RequireMega.Value);
                }

                writer.WriteEndObject();
            }

            // Stickers
            if (item.Stickers?.Count > 0)
            {
                writer.WriteStartArray("stickers");
                foreach (var sticker in item.Stickers)
                {
                    writer.WriteStringValue(sticker);
                }
                writer.WriteEndArray();
            }

            // Playing card properties
            if (!string.IsNullOrEmpty(item.Seal))
            {
                writer.WriteString("seal", item.Seal);
            }
            if (!string.IsNullOrEmpty(item.Enhancement))
            {
                writer.WriteString("enhancement", item.Enhancement);
            }

            // Min count
            if (item.Min.HasValue && item.Min.Value > 0)
            {
                writer.WriteNumber("min", (decimal)item.Min.Value);
            }

            // Score (for should clauses) - include even if zero or negative
            if (includeScore)
            {
                writer.WriteNumber("score", item.Score);
            }

            // Nested clauses for And/Or grouping
            if (item.Clauses?.Count > 0)
            {
                writer.WriteStartArray("clauses");
                foreach (var nestedClause in item.Clauses)
                {
                    WriteFilterItem(writer, nestedClause, includeScore);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
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
                    return "SoulJoker";
                case "jokers":
                    return itemName.StartsWith("j_") ? "Joker" : itemName;
                case "tarots":
                    // Align with Motely type naming
                    return "TarotCard";
                case "planets":
                    // Align with Motely type naming
                    return "PlanetCard";
                case "spectrals":
                    // Align with Motely type naming
                    return "SpectralCard";
                case "playingcards":
                    return "PlayingCard";
                case "vouchers":
                    return "Voucher";
                case "tags":
                    return "Tag";
                case "bosses":
                    return "Boss";
                default:
                    return category;
            }
        }

        #endregion
    }
}
