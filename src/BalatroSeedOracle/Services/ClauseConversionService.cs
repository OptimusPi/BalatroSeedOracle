using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.FilterTabs;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Builds the engine's polymorphic IJamlClause types directly from FilterItem.
/// Single entry point: <see cref="BuildClauseFromFilterItem(FilterItem)"/>.
/// </summary>
public sealed class ClauseConversionService
{
    public ClauseRowViewModel? ConvertToClauseViewModel(
        IJamlClause clause,
        string category,
        int nestingLevel = 0
    )
    {
        var vm = new ClauseRowViewModel { NestingLevel = nestingLevel };

        if (clause is LogicClause logic)
        {
            var op = clause is AndClause ? "and" : "or";
            vm.ClauseType = op;
            vm.DisplayText = $"{op.ToUpperInvariant()} Group ({logic.Clauses.Length} items)";
            vm.IsExpanded = false;

            foreach (var nested in logic.Clauses)
            {
                var childVm = ConvertToClauseViewModel(nested, category, nestingLevel + 1);
                if (childVm is not null)
                    vm.Children.Add(childVm);
            }
            return vm;
        }

        var typeName = GetTypeName(clause);
        vm.ClauseType = typeName;

        var value = GetValueName(clause);
        var edition = GetEditionString(clause);
        var stickers = GetStickers(clause);
        var seal = GetSealString(clause);
        var enhancement = GetEnhancementString(clause);

        var displayParts = new List<string>();
        if (!string.IsNullOrEmpty(value))
            displayParts.Add(value);
        if (stickers.Length > 0)
            displayParts.Add($"[{string.Join(", ", stickers)}]");
        if (!string.IsNullOrEmpty(seal))
            displayParts.Add($"Seal: {seal}");
        if (!string.IsNullOrEmpty(enhancement))
            displayParts.Add($"Enhanced: {enhancement}");

        vm.DisplayText = string.Join(" ", displayParts);
        vm.EditionBadge = edition;
        vm.IconPath = GetIconPath(typeName, value);

        if (clause is IAnteScopedClause anteScoped && anteScoped.Antes.Length > 0)
        {
            var min = anteScoped.Antes.Min();
            var max = anteScoped.Antes.Max();
            vm.AnteRange = min == max ? $"Ante {min}" : $"Antes {min}-{max}";
        }

        vm.MinCount = clause.Min;
        vm.ScoreValue = clause.Score;
        vm.ItemKey = $"{category}:{(string.IsNullOrEmpty(value) ? typeName : value)}";

        return vm;
    }

    public IJamlClause? BuildClauseFromFilterItem(FilterItem filterItem)
    {
        if (filterItem is null)
            return null;

        if (filterItem is FilterOperatorItem operatorItem)
        {
            if (operatorItem.OperatorType == "BannedItems")
                return null;

            var isOr = operatorItem.OperatorType.Equals("Or", StringComparison.OrdinalIgnoreCase);
            var logic = isOr ? (LogicClause)new OrClause() : new AndClause();
            var children = new List<IJamlClause>();
            foreach (var child in operatorItem.Children)
            {
                var childClause = BuildClauseFromFilterItem(child);
                if (childClause is not null)
                    children.Add(childClause);
            }
            logic.Clauses = children.ToArray();
            return logic;
        }

        var discriminator = MapCategoryToDiscriminator(filterItem.Category, filterItem.Name, filterItem.Type);
        var antes = filterItem.Antes?.Length > 0 ? filterItem.Antes : [1, 2, 3, 4, 5, 6, 7, 8];
        var min = filterItem.MinCount > 0 ? filterItem.MinCount : 1;

        var clause = BuildClause(filterItem, discriminator, antes, min);
        if (clause is null)
            return null;

        clause.Label = filterItem.Label;
        clause.Score = filterItem.Score;

        return clause;
    }

    private IJamlClause? BuildClause(
        FilterItem filterItem,
        string discriminator,
        int[] antes,
        int min
    )
    {
        var edition = ParseEdition(filterItem.Edition);
        var stickers = ParseStickers(filterItem.Stickers);
        var rank = ParseEnumNullable<MotelyStandardcardRank>(filterItem.Rank);
        var suit = ParseEnumNullable<MotelyStandardcardSuit>(filterItem.Suit);

        switch (discriminator.ToLowerInvariant())
        {
            case "joker":
                return new JokerClause
                {
                    Antes = antes,
                    Min = min,
                    Jokers = ParseEnumArray<MotelyJoker>(filterItem.Name),
                    Edition = edition,
                    Stickers = stickers,
                    Sources = BuildJokerSources(filterItem),
                };
            case "commonjoker":
                return new CommonJokerClause
                {
                    Antes = antes,
                    Min = min,
                    Jokers = ParseEnumArray<MotelyJokerCommon>(filterItem.Name),
                    Edition = edition,
                    Stickers = stickers,
                    Sources = BuildJokerSources(filterItem),
                };
            case "uncommonjoker":
                return new UncommonJokerClause
                {
                    Antes = antes,
                    Min = min,
                    Jokers = ParseEnumArray<MotelyJokerUncommon>(filterItem.Name),
                    Edition = edition,
                    Stickers = stickers,
                    Sources = BuildJokerSources(filterItem),
                };
            case "rarejoker":
                return new RareJokerClause
                {
                    Antes = antes,
                    Min = min,
                    Jokers = ParseEnumArray<MotelyJokerRare>(filterItem.Name),
                    Edition = edition,
                    Stickers = stickers,
                    Sources = BuildJokerSources(filterItem),
                };
            case "legendaryjoker":
                return new LegendaryJokerClause
                {
                    Antes = antes,
                    Min = min,
                    Jokers = ParseEnumArray<MotelyJoker>(filterItem.Name),
                    Edition = edition,
                    Sources = BuildLegendarySources(filterItem),
                };
            case "voucher":
                return new VoucherClause
                {
                    Antes = antes,
                    Min = min,
                    Vouchers = ParseEnumArray<MotelyVoucher>(filterItem.Name),
                    Rolls = [0],
                };
            case "tarotcard":
                return new TarotCardClause
                {
                    Antes = antes,
                    Min = min,
                    Tarots = ParseEnumArray<MotelyTarotCard>(filterItem.Name),
                    Sources = BuildTarotSources(filterItem),
                };
            case "spectralcard":
                return new SpectralCardClause
                {
                    Antes = antes,
                    Min = min,
                    Spectrals = ParseEnumArray<MotelySpectralCard>(filterItem.Name),
                    Sources = BuildSpectralSources(filterItem),
                };
            case "planetcard":
                return new PlanetCardClause
                {
                    Antes = antes,
                    Min = min,
                    Planets = ParseEnumArray<MotelyPlanetCard>(filterItem.Name),
                    Sources = BuildPlanetSources(filterItem),
                };
            case "standardcard":
                return new StandardCardClause
                {
                    Antes = antes,
                    Min = min,
                    Rank = rank,
                    Suit = suit,
                    Edition = edition,
                    Seal = ParseEnumNullable<MotelyItemSeal>(filterItem.Seal),
                    Enhancement = ParseEnumNullable<MotelyItemEnhancement>(filterItem.Enhancement),
                    Sources = BuildStandardSources(filterItem),
                };
            case "boss":
                return new BossClause
                {
                    Antes = antes,
                    Min = min,
                    Bosses = ParseEnumArray<MotelyBossBlind>(filterItem.Name),
                };
            case "tag":
            case "smallblindtag":
            case "bigblindtag":
                return new TagClause
                {
                    Antes = antes,
                    Min = min,
                    Tags = ParseEnumArray<MotelyTag>(filterItem.Name),
                    Rolls = GetTagRolls(discriminator),
                };
            default:
                DebugLogger.LogError(
                    "ClauseConversionService",
                    $"Unsupported discriminator '{discriminator}' for category '{filterItem.Category}'"
                );
                return null;
        }
    }

    #region Helpers

    private static string MapCategoryToDiscriminator(string category, string itemName, string? tagType)
    {
        var lower = category?.ToLowerInvariant();
        return lower switch
        {
            "souljokers" => "legendaryJoker",
            "jokers" => "joker",
            "tarots" => "tarotCard",
            "planets" => "planetCard",
            "spectrals" => "spectralCard",
            "playingcards" => "standardCard",
            "vouchers" => "voucher",
            "tags" => tagType?.ToLowerInvariant() switch
            {
                "smallblindtag" => "smallBlindTag",
                "bigblindtag" => "bigBlindTag",
                _ => "tag",
            },
            "bosses" => "boss",
            "other" => "standardCard",
            "operator" => throw new InvalidOperationException(
                $"FilterOperatorItem should be handled before BuildClause. ItemName: '{itemName}'"
            ),
            _ => throw new ArgumentException(
                $"Unknown category '{category}' for item '{itemName}'."
            ),
        };
    }

    private static int[] GetTagRolls(string discriminator)
    {
        return discriminator.ToLowerInvariant() switch
        {
            "smallblindtag" => [0],
            "bigblindtag" => [1],
            _ => [0, 1],
        };
    }

    private static T[] ParseEnumArray<T>(string value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var results = new List<T>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (Enum.TryParse<T>(trimmed, true, out var parsed))
            {
                results.Add(parsed);
            }
            else
            {
                DebugLogger.LogError(
                    "ClauseConversionService",
                    $"Could not parse '{trimmed}' as {typeof(T).Name}"
                );
            }
        }
        return results.ToArray();
    }

    private static T? ParseEnumNullable<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            return null;
        if (Enum.TryParse<T>(value.Trim(), true, out var parsed))
            return parsed;
        DebugLogger.LogError("ClauseConversionService", $"Could not parse '{value}' as {typeof(T).Name}");
        return null;
    }

    private static MotelyItemEdition? ParseEdition(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition) || edition.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;
        if (Enum.TryParse<MotelyItemEdition>(edition.Trim(), true, out var parsed))
            return parsed;
        DebugLogger.LogError("ClauseConversionService", $"Could not parse edition '{edition}'");
        return null;
    }

    private static MotelyJokerSticker[] ParseStickers(List<string>? stickers)
    {
        if (stickers is null || stickers.Count == 0)
            return [];
        return stickers
            .Select(s => s.Trim())
            .Where(s => Enum.TryParse<MotelyJokerSticker>(s, true, out _))
            .Select(s => Enum.Parse<MotelyJokerSticker>(s, true))
            .ToArray();
    }

    private static JokerSourceConfig? BuildJokerSources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new JokerSourceConfig
        {
            ShopItems = filterItem.ShopSlots ?? [],
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static LegendaryJokerSourceConfig? BuildLegendarySources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new LegendaryJokerSourceConfig
        {
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static TarotCardSourceConfig? BuildTarotSources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new TarotCardSourceConfig
        {
            ShopItems = filterItem.ShopSlots ?? [],
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static SpectralCardSourceConfig? BuildSpectralSources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new SpectralCardSourceConfig
        {
            ShopItems = filterItem.ShopSlots ?? [],
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static PlanetSourceConfig? BuildPlanetSources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new PlanetSourceConfig
        {
            ShopItems = filterItem.ShopSlots ?? [],
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static StandardCardSourceConfig? BuildStandardSources(FilterItem filterItem)
    {
        if (!HasAnySource(filterItem))
            return null;
        return new StandardCardSourceConfig
        {
            ShopItems = filterItem.ShopSlots ?? [],
            BoosterPacks = filterItem.PackPositions ?? [],
        };
    }

    private static bool HasAnySource(FilterItem filterItem)
    {
        return (filterItem.ShopSlots?.Length > 0)
            || (filterItem.PackPositions?.Length > 0);
    }

    #endregion

    #region Display helpers

    private static string GetTypeName(IJamlClause clause)
    {
        return clause switch
        {
            AndClause => "and",
            OrClause => "or",
            JokerClause or CommonJokerClause or UncommonJokerClause or RareJokerClause => "joker",
            LegendaryJokerClause => "legendaryJoker",
            VoucherClause => "voucher",
            TarotCardClause => "tarotCard",
            SpectralCardClause => "spectralCard",
            PlanetCardClause => "planetCard",
            StandardCardClause => "standardCard",
            BossClause => "boss",
            TagClause => "tag",
            _ => clause.GetType().Name.Replace("Clause", ""),
        };
    }

    private static string? GetValueName(IJamlClause clause)
    {
        return clause switch
        {
            JokerClause c => FirstOrNone(c.Jokers),
            CommonJokerClause c => FirstOrNone(c.Jokers),
            UncommonJokerClause c => FirstOrNone(c.Jokers),
            RareJokerClause c => FirstOrNone(c.Jokers),
            LegendaryJokerClause c => FirstOrNone(c.Jokers),
            VoucherClause c => FirstOrNone(c.Vouchers),
            TarotCardClause c => FirstOrNone(c.Tarots),
            SpectralCardClause c => FirstOrNone(c.Spectrals),
            PlanetCardClause c => FirstOrNone(c.Planets),
            BossClause c => FirstOrNone(c.Bosses),
            TagClause c => FirstOrNone(c.Tags),
            StandardCardClause c => FormatStandardCard(c),
            StartingDrawClause c => FormatStartingDraw(c),
            _ => null,
        };
    }

    private static string? FirstOrNone<T>(T[] values) where T : struct, Enum
    {
        if (values.Length == 0)
            return null;
        return values[0].ToString();
    }

    private static string? FormatStandardCard(StandardCardClause c)
    {
        if (c.Rank.HasValue && c.Suit.HasValue)
            return $"{c.Rank.Value} of {c.Suit.Value}";
        if (c.Rank.HasValue)
            return c.Rank.Value.ToString();
        if (c.Suit.HasValue)
            return c.Suit.Value.ToString();
        return null;
    }

    private static string? FormatStartingDraw(StartingDrawClause c)
    {
        if (c.Rank.HasValue && c.Suit.HasValue)
            return $"{c.Rank.Value} of {c.Suit.Value}";
        if (c.Rank.HasValue)
            return c.Rank.Value.ToString();
        if (c.Suit.HasValue)
            return c.Suit.Value.ToString();
        return null;
    }

    private static string? GetEditionString(IJamlClause clause)
    {
        return clause switch
        {
            JokerClause { Edition: { } e } => e.ToString(),
            CommonJokerClause { Edition: { } e } => e.ToString(),
            UncommonJokerClause { Edition: { } e } => e.ToString(),
            RareJokerClause { Edition: { } e } => e.ToString(),
            LegendaryJokerClause { Edition: { } e } => e.ToString(),
            StandardCardClause { Edition: { } e } => e.ToString(),
            _ => null,
        };
    }

    private static string[] GetStickers(IJamlClause clause)
    {
        return clause switch
        {
            JokerClause c => c.Stickers.Select(s => s.ToString()).ToArray(),
            CommonJokerClause c => c.Stickers.Select(s => s.ToString()).ToArray(),
            UncommonJokerClause c => c.Stickers.Select(s => s.ToString()).ToArray(),
            RareJokerClause c => c.Stickers.Select(s => s.ToString()).ToArray(),
            _ => [],
        };
    }

    private static string? GetSealString(IJamlClause clause)
    {
        return clause switch
        {
            StandardCardClause { Seal: { } s } => s.ToString(),
            _ => null,
        };
    }

    private static string? GetEnhancementString(IJamlClause clause)
    {
        return clause switch
        {
            StandardCardClause { Enhancement: { } e } => e.ToString(),
            _ => null,
        };
    }

    private static IImage? GetIconPath(string? type, string? value)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
            return null;

        var spriteService = SpriteService.Instance;

        try
        {
            return type.ToLowerInvariant() switch
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

    #endregion
}
