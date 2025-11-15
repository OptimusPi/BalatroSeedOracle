using System;
using System.Collections.Generic;
using System.Linq;
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
        public ClauseConversionService()
        {
        }

        /// <summary>
        /// Converts a MotelyJsonConfig.MotleyJsonFilterClause to a ClauseRowViewModel
        /// Used for displaying clauses in the Validate Filter tab
        /// </summary>
        public ClauseRowViewModel ConvertToClauseViewModel(
            MotelyJsonConfig.MotleyJsonFilterClause clause,
            string category,
            int nestingLevel = 0)
        {
            var vm = new ClauseRowViewModel
            {
                NestingLevel = nestingLevel
            };

            // Handle nested OR/AND clauses
            if (clause.Type?.ToLower() == "or" || clause.Type?.ToLower() == "and")
            {
                vm.ClauseType = clause.Type;
                vm.DisplayText = $"{clause.Type.ToUpper()} Group ({clause.Clauses?.Count ?? 0} items)";
                vm.IsExpanded = false;

                // Process nested clauses
                if (clause.Clauses != null)
                {
                    foreach (var nestedClause in clause.Clauses)
                    {
                        var childVm = ConvertToClauseViewModel(nestedClause, category, nestingLevel + 1);
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
            ItemConfig config)
        {
            if (filterItem == null)
                return null;

            var clause = new MotelyJsonConfig.MotleyJsonFilterClause
            {
                Type = MapCategoryToType(filterItem.Category, filterItem.Name),
                Value = filterItem.Name,
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min > 0 ? config.Min : null,
                Score = config.Score
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
                        RequireMega = config.IsMegaArcana ? true : null
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

        private string GetIconPath(string? type, string? value)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
                return "";

            // Build sprite path based on type and value
            // This assumes sprites are in standard locations
            var normalizedValue = value.Replace(" ", "_").ToLower();

            return type?.ToLower() switch
            {
                "joker" or "souljoker" => $"avares://BalatroSeedOracle/Assets/Sprites/Jokers/{normalizedValue}.png",
                "tarotcard" => $"avares://BalatroSeedOracle/Assets/Sprites/Tarots/{normalizedValue}.png",
                "planetcard" => $"avares://BalatroSeedOracle/Assets/Sprites/Planets/{normalizedValue}.png",
                "spectralcard" => $"avares://BalatroSeedOracle/Assets/Sprites/Spectrals/{normalizedValue}.png",
                "voucher" => $"avares://BalatroSeedOracle/Assets/Sprites/Vouchers/{normalizedValue}.png",
                "playingcard" => $"avares://BalatroSeedOracle/Assets/Sprites/PlayingCards/{normalizedValue}.png",
                "tag" => $"avares://BalatroSeedOracle/Assets/Sprites/Tags/{normalizedValue}.png",
                "boss" => $"avares://BalatroSeedOracle/Assets/Sprites/Bosses/{normalizedValue}.png",
                _ => ""
            };
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
                _ => ""
            };
        }

        private string MapCategoryToType(string category, string itemName)
        {
            return category?.ToLower() switch
            {
                "souljokers" => "SoulJoker",
                "jokers" => itemName.StartsWith("j_") ? "Joker" : itemName,
                "tarots" => "TarotCard",
                "planets" => "PlanetCard",
                "spectrals" => "SpectralCard",
                "playingcards" => "PlayingCard",
                "vouchers" => "Voucher",
                "tags" => "Tag",
                "bosses" => "Boss",
                _ => category
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