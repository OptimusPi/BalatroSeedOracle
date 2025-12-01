using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.FilterTabs;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service responsible for converting between different filter clause representations.
    /// Centralizes the conversion logic to avoid duplication across ViewModels.
    /// </summary>
    public class ClauseConversionService
    {
        public ClauseConversionService() { }

        /// <summary>
        /// Converts a MotelyJsonConfig.MotleyJsonFilterClause to a ClauseRowViewModel
        /// Used for displaying clauses in the Validate Filter tab
        /// </summary>
        public ClauseRowViewModel ConvertToClauseViewModel(
            MotelyJsonConfig.MotleyJsonFilterClause clause,
            string category,
            int nestingLevel = 0
        )
        {
            var vm = new ClauseRowViewModel { NestingLevel = nestingLevel };

            // Handle nested OR/AND clauses
            if (clause.Type?.ToLower() == "or" || clause.Type?.ToLower() == "and")
            {
                vm.ClauseType = clause.Type;
                vm.DisplayText =
                    $"{clause.Type.ToUpper()} Group ({clause.Clauses?.Count ?? 0} items)";
                vm.IsExpanded = false;

                // Process nested clauses
                if (clause.Clauses != null)
                {
                    foreach (var nestedClause in clause.Clauses)
                    {
                        var childVm = ConvertToClauseViewModel(
                            nestedClause,
                            category,
                            nestingLevel + 1
                        );
                        if (childVm != null)
                        {
                            vm.Children.Add(childVm);
                        }
                    }
                }
                return vm;
            }

            // Regular item clause
            vm.ClauseType = clause.Type ?? "";

            // Build display text
            var displayParts = new List<string>();

            // Add main value/values
            if (!string.IsNullOrEmpty(clause.Value))
            {
                displayParts.Add(clause.Value);
            }
            else if (clause.Values?.Length > 0)
            {
                displayParts.Add(string.Join(", ", clause.Values));
            }

            // Add edition if specified
            if (!string.IsNullOrEmpty(clause.Edition))
            {
                vm.EditionBadge = clause.Edition;
            }

            // Add stickers if specified
            if (clause.Stickers?.Count > 0)
            {
                displayParts.Add($"[{string.Join(", ", clause.Stickers)}]");
            }

            // Add seal/enhancement for playing cards
            if (!string.IsNullOrEmpty(clause.Seal))
            {
                displayParts.Add($"Seal: {clause.Seal}");
            }
            if (!string.IsNullOrEmpty(clause.Enhancement))
            {
                displayParts.Add($"Enhanced: {clause.Enhancement}");
            }

            vm.DisplayText = string.Join(" ", displayParts);

            // Set icon path based on type and value
            vm.IconPath = GetIconPath(clause.Type, clause.Value);

            // Set ante range
            if (clause.Antes?.Length > 0)
            {
                var min = clause.Antes.Min();
                var max = clause.Antes.Max();
                vm.AnteRange = min == max ? $"Ante {min}" : $"Antes {min}-{max}";
            }

            // Set min count
            vm.MinCount = clause.Min;

            // Set score value (for Should clauses)
            vm.ScoreValue = clause.Score;

            // Set ItemKey for potential editing
            vm.ItemKey = $"{category}:{clause.Value ?? clause.Type}";

            return vm;
        }

        /// <summary>
        /// Converts a FilterItem to a MotelyJsonConfig.MotleyJsonFilterClause
        /// Used when building filter configs from Visual Builder selections
        /// </summary>
        public MotelyJsonConfig.MotleyJsonFilterClause? ConvertFilterItemToClause(
            FilterItem filterItem,
            ItemConfig config
        )
        {
            if (filterItem == null)
                return null;

            // Handle FilterOperatorItem (OR/AND/BannedItems)
            if (filterItem is FilterOperatorItem operatorItem)
            {
                // BannedItems should not be converted to a clause
                // (caller should handle these by adding children to MustNot array)
                if (operatorItem.OperatorType == "BannedItems")
                    return null;

                // Create OR/AND clause with nested children
                var operatorClause = new MotelyJsonConfig.MotleyJsonFilterClause
                {
                    Type = operatorItem.OperatorType switch
                    {
                        "OR" => "Or",
                        "AND" => "And",
                        _ => operatorItem.OperatorType, // Fallback
                    },
                    Clauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
                };

                // Recursively convert children
                foreach (var child in operatorItem.Children)
                {
                    // Use empty config for children (they should have their own configs if needed)
                    var childConfig = new ItemConfig();
                    var childClause = ConvertFilterItemToClause(child, childConfig);
                    if (childClause != null)
                    {
                        operatorClause.Clauses.Add(childClause);
                    }
                }

                return operatorClause;
            }

            // Regular item clause
            var clause = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = MapCategoryToType(filterItem.Category, filterItem.Name),
                Value = filterItem.Name,
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min > 0 ? config.Min : null,
                Score = config.Score,
            };

            // Add edition if specified
            if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
            {
                clause.Edition = config.Edition;
            }

            // Add stickers if specified
            if (config.Stickers?.Count > 0)
            {
                clause.Stickers = config.Stickers;
            }

            // Add sources for applicable item types
            if (IsSourceCapableCategory(filterItem.Category))
            {
                if (HasValidSources(config))
                {
                    clause.Sources = new MotelyJsonConfig.SourcesConfig
                    {
                        ShopSlots = config.ShopSlots?.ToArray(),
                        PackSlots = config.PackSlots?.ToArray(),
                        Tags = config.SkipBlindTags ? true : null,
                        RequireMega = config.IsMegaArcana ? true : null,
                    };
                }
            }

            // Handle playing card specific properties
            if (filterItem.Category.ToLower() == "playingcards")
            {
                if (!string.IsNullOrEmpty(config.Seal) && config.Seal != "None")
                    clause.Seal = config.Seal;
                if (!string.IsNullOrEmpty(config.Enhancement) && config.Enhancement != "None")
                    clause.Enhancement = config.Enhancement;
            }

            return clause;
        }

        #region Private Helper Methods

        private IImage? GetIconPath(string? type, string? value)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
                return null;

            // Use SpriteService to get the actual sprite image
            var spriteService = SpriteService.Instance;

            try
            {
                return type?.ToLower() switch
                {
                    "joker" => spriteService.GetJokerImage(value),
                    "souljoker" => spriteService.GetJokerImage(value),
                    "tarotcard" => spriteService.GetTarotImage(value),
                    "planetcard" => spriteService.GetPlanetCardImage(value),
                    "spectralcard" => spriteService.GetSpectralImage(value),
                    "voucher" => spriteService.GetVoucherImage(value),
                    "playingcard" => null, // Playing cards need rank/suit, not handled here
                    "tag" => spriteService.GetTagImage(value),
                    "boss" => spriteService.GetBossImage(value),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ClauseConversionService",
                    $"Failed to get icon for type '{type}', value '{value}': {ex.Message}"
                );
                return null;
            }
        }

        private string MapTypeToCategory(string type)
        {
            return type?.ToLower() switch
            {
                "joker" => "jokers",
                "souljoker" => "souljokers",
                "tarotcard" => "tarots",
                "planetcard" => "planets",
                "spectralcard" => "spectrals",
                "playingcard" => "playingcards",
                "voucher" => "vouchers",
                "tag" => "tags",
                "boss" => "bosses",
                _ => throw new ArgumentException(),
            };
        }

        private string MapCategoryToType(string category, string itemName)
        {
            var lowerCategory = category?.ToLower();
            return lowerCategory switch
            {
                "souljokers" => "SoulJoker",
                "jokers" => "Joker",
                "tarots" => "TarotCard",
                "planets" => "PlanetCard",
                "spectrals" => "SpectralCard",
                "playingcards" => "PlayingCard",
                "vouchers" => "Voucher",
                "tags" => "Tag",
                "bosses" => "Boss",
                "other" => "PlayingCard", // Fallback for miscategorized items (usually standard cards)
                "operator" => throw new InvalidOperationException(
                    $"FilterOperatorItem with category 'Operator' should be handled by early return in ConvertFilterItemToClause. "
                        + $"If you see this error, a regular FilterItem was incorrectly created with Category='Operator'. "
                        + $"ItemName: '{itemName}'"
                ),
                _ => throw new ArgumentException(
                    $"Unknown category '{category}' for item '{itemName}'. "
                        + $"Valid categories: jokers, souljokers, tarots, planets, spectrals, playingcards, vouchers, tags, bosses, other. "
                        + $"If this is a new item type, add it to MapCategoryToType switch expression."
                ),
            };
        }

        private bool IsSourceCapableCategory(string category)
        {
            var lowerCategory = category?.ToLower();
            return lowerCategory == "jokers"
                || lowerCategory == "souljokers"
                || lowerCategory == "tarots"
                || lowerCategory == "spectrals"
                || lowerCategory == "planets"
                || lowerCategory == "playingcards";
        }

        private bool HasValidSources(ItemConfig config)
        {
            return (config.ShopSlots?.Count > 0)
                || (config.PackSlots?.Count > 0)
                || config.SkipBlindTags
                || config.IsMegaArcana;
        }

        #endregion
    }
}
