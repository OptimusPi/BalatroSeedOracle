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
    public class ClauseConversionService
    {
        public ClauseConversionService() { }

        public ClauseRowViewModel ConvertToClauseViewModel(
            JamlClauseUnion clause,
            string category,
            int nestingLevel = 0
        )
        {
            var vm = new ClauseRowViewModel { NestingLevel = nestingLevel };

            var typeName = clause.GetTypeName();

            if (typeName == "or" || typeName == "and")
            {
                vm.ClauseType = typeName;
                vm.DisplayText =
                    $"{typeName.ToUpper()} Group ({clause.Clauses?.Count ?? 0} items)";
                vm.IsExpanded = false;

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

            vm.ClauseType = typeName;

            var displayParts = new List<string>();
            var value = clause.GetValueName();

            if (!string.IsNullOrEmpty(value))
            {
                displayParts.Add(value);
            }

            var edition = clause.GetEditionString();
            if (!string.IsNullOrEmpty(edition))
            {
                vm.EditionBadge = edition;
            }

            if (clause.Stickers != null && clause.Stickers.Length > 0)
            {
                displayParts.Add($"[{string.Join(", ", clause.Stickers.Select(s => s.ToString()))}]");
            }

            var seal = clause.GetSealString();
            if (!string.IsNullOrEmpty(seal))
            {
                displayParts.Add($"Seal: {seal}");
            }
            var enhancement = clause.GetEnhancementString();
            if (!string.IsNullOrEmpty(enhancement))
            {
                displayParts.Add($"Enhanced: {enhancement}");
            }

            vm.DisplayText = string.Join(" ", displayParts);

            vm.IconPath = GetIconPath(typeName, value);

            if (clause.Antes?.Length > 0)
            {
                var min = clause.Antes.Min();
                var max = clause.Antes.Max();
                vm.AnteRange = min == max ? $"Ante {min}" : $"Antes {min}-{max}";
            }

            vm.MinCount = clause.Min;

            vm.ScoreValue = clause.Score ?? 1;

            vm.ItemKey = $"{category}:{(string.IsNullOrEmpty(value) ? typeName : value)}";

            return vm;
        }

        public JamlClauseUnion? ConvertFilterItemToClause(
            FilterItem filterItem,
            ItemConfig config
        )
        {
            if (filterItem == null)
                return null;

            if (filterItem is FilterOperatorItem operatorItem)
            {
                if (operatorItem.OperatorType == "BannedItems")
                    return null;

                var operatorClause = new JamlClauseUnion
                {
                    Clauses = new List<JamlClauseUnion>(),
                };
                var op = operatorItem.OperatorType.ToLowerInvariant();
                if (op == "or") operatorClause.Or = operatorClause.Clauses;
                else operatorClause.And = operatorClause.Clauses;

                foreach (var child in operatorItem.Children)
                {
                    var childConfig = new ItemConfig();
                    var childClause = ConvertFilterItemToClause(child, childConfig);
                    if (childClause != null)
                    {
                        operatorClause.Clauses.Add(childClause);
                    }
                }

                return operatorClause;
            }

            var clause = new JamlClauseUnion
            {
                Antes = config.Antes?.ToArray() ?? new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                Min = config.Min > 0 ? config.Min : null,
                Score = config.Score,
            };

            clause.SetDiscriminator(MapCategoryToType(filterItem.Category, filterItem.Name), filterItem.Name);

            if (!string.IsNullOrEmpty(config.Edition) && config.Edition != "none")
            {
                clause.SetEditionString(config.Edition);
            }

            if (config.Stickers?.Count > 0)
            {
                clause.SetStickerStrings(config.Stickers.ToArray());
            }

            if (IsSourceCapableCategory(filterItem.Category))
            {
                if (HasValidSources(config))
                {
                    clause.Sources = new JamlSources
                    {
                        ShopItems = config.ShopSlots?.ToArray(),
                        BoosterPacks = config.PackSlots?.ToArray(),
                        Tags = config.SkipBlindTags,
                        RequireMega = config.IsMegaArcana,
                    };
                }
            }

            if (filterItem.Category.ToLower() == "playingcards")
            {
                if (!string.IsNullOrEmpty(config.Seal) && config.Seal != "None")
                    clause.SetSealString(config.Seal);
                if (!string.IsNullOrEmpty(config.Enhancement) && config.Enhancement != "None")
                    clause.SetEnhancementString(config.Enhancement);
            }

            return clause;
        }

        private IImage? GetIconPath(string? type, string? value)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
                return null;

            var spriteService = SpriteService.Instance;

            try
            {
                return type?.ToLower() switch
                {
                    "joker" => spriteService.GetJokerImage(value),
                    "legendaryjoker" => spriteService.GetJokerImage(value),
                    "souljoker" => spriteService.GetJokerImage(value),
                    "tarotcard" => spriteService.GetTarotImage(value),
                    "planetcard" => spriteService.GetPlanetCardImage(value),
                    "spectralcard" => spriteService.GetSpectralImage(value),
                    "voucher" => spriteService.GetVoucherImage(value),
                    "standardcard" => null,
                    "playingcard" => null,
                    "tag" => spriteService.GetTagImage(value),
                    "smallblindtag" => spriteService.GetTagImage(value),
                    "bigblindtag" => spriteService.GetTagImage(value),
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

        private string MapCategoryToType(string category, string itemName)
        {
            var lowerCategory = category?.ToLower();
            return lowerCategory switch
            {
                "souljokers" => "legendaryJoker",
                "jokers" => "joker",
                "tarots" => "tarotCard",
                "planets" => "planetCard",
                "spectrals" => "spectralCard",
                "playingcards" => "standardCard",
                "vouchers" => "voucher",
                "tags" => "tag",
                "bosses" => "boss",
                "other" => "standardCard",
                "operator" => throw new InvalidOperationException(
                    $"FilterOperatorItem with category 'Operator' should be handled by early return in ConvertFilterItemToClause. "
                        + $"ItemName: '{itemName}'"
                ),
                _ => throw new ArgumentException(
                    $"Unknown category '{category}' for item '{itemName}'."
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
    }
}
