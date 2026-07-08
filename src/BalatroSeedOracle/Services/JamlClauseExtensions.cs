using System;
using System.Linq;
using BalatroSeedOracle.Models;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services;

/// <summary>
/// UI helpers that read/write the engine's polymorphic IJamlClause types without inventing
/// BSO-side JAML serialization glue. The engine still owns all YAML loading and writing.
/// </summary>
public static class JamlClauseExtensions
{
    public static string? GetTypeName(this IJamlClause clause)
    {
        return clause switch
        {
            AndClause => "and",
            OrClause => "or",
            JokerClause => "joker",
            CommonJokerClause => "commonJoker",
            UncommonJokerClause => "uncommonJoker",
            RareJokerClause => "rareJoker",
            LegendaryJokerClause => "legendaryJoker",
            VoucherClause => "voucher",
            TarotCardClause => "tarotCard",
            SpectralCardClause => "spectralCard",
            PlanetCardClause => "planetCard",
            StandardCardClause => "standardCard",
            BossClause => "boss",
            TagClause => "tag",
            StartingDrawClause => "startingDraw",
            _ => clause.GetType().Name.Replace("Clause", ""),
        };
    }

    public static string? GetValueName(this IJamlClause clause)
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

    public static string? GetEditionString(this IJamlClause clause)
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

    public static string? GetSealString(this IJamlClause clause)
    {
        return clause switch
        {
            StandardCardClause { Seal: { } s } => s.ToString(),
            _ => null,
        };
    }

    public static string? GetEnhancementString(this IJamlClause clause)
    {
        return clause switch
        {
            StandardCardClause { Enhancement: { } e } => e.ToString(),
            _ => null,
        };
    }

    public static string[] GetStickerStrings(this IJamlClause clause)
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

    public static int[] GetAntes(this IJamlClause clause)
    {
        return clause switch
        {
            IAnteScopedClause c => c.Antes,
            _ => [],
        };
    }

    public static void SetAntes(this IJamlClause clause, int[] antes)
    {
        if (clause is IAnteScopedClause c)
            c.Antes = antes;
    }

    public static void SetEditionString(this IJamlClause clause, string? edition)
    {
        var parsed = ParseEdition(edition);
        if (parsed is null)
            return;
        switch (clause)
        {
            case JokerClause c: c.Edition = parsed; break;
            case CommonJokerClause c: c.Edition = parsed; break;
            case UncommonJokerClause c: c.Edition = parsed; break;
            case RareJokerClause c: c.Edition = parsed; break;
            case LegendaryJokerClause c: c.Edition = parsed; break;
            case StandardCardClause c: c.Edition = parsed; break;
        }
    }

    public static void SetStickerStrings(this IJamlClause clause, string[] stickers)
    {
        var parsed = stickers
            .Select(s => s.Trim())
            .Where(s => Enum.TryParse<MotelyJokerSticker>(s, true, out _))
            .Select(s => Enum.Parse<MotelyJokerSticker>(s, true))
            .ToArray();
        switch (clause)
        {
            case JokerClause c: c.Stickers = parsed; break;
            case CommonJokerClause c: c.Stickers = parsed; break;
            case UncommonJokerClause c: c.Stickers = parsed; break;
            case RareJokerClause c: c.Stickers = parsed; break;
        }
    }

    public static void SetSealString(this IJamlClause clause, string? seal)
    {
        if (clause is StandardCardClause c && ParseEnumNullable<MotelyItemSeal>(seal) is { } s)
            c.Seal = s;
    }

    public static void SetEnhancementString(this IJamlClause clause, string? enhancement)
    {
        if (clause is StandardCardClause c && ParseEnumNullable<MotelyItemEnhancement>(enhancement) is { } e)
            c.Enhancement = e;
    }

    public static IJamlClause CreateClauseForDiscriminator(string discriminator, string? value)
    {
        return discriminator?.ToLowerInvariant() switch
        {
            "joker" or "jokers" => new JokerClause { Jokers = ParseEnumArray<MotelyJoker>(value) },
            "commonjoker" or "commonjokers" => new CommonJokerClause { Jokers = ParseEnumArray<MotelyJokerCommon>(value) },
            "uncommonjoker" or "uncommonjokers" => new UncommonJokerClause { Jokers = ParseEnumArray<MotelyJokerUncommon>(value) },
            "rarejoker" or "rarejokers" => new RareJokerClause { Jokers = ParseEnumArray<MotelyJokerRare>(value) },
            "legendaryjoker" or "legendaryjokers" or "souljoker" => new LegendaryJokerClause { Jokers = ParseEnumArray<MotelyJoker>(value) },
            "voucher" or "vouchers" => new VoucherClause { Vouchers = ParseEnumArray<MotelyVoucher>(value), Rolls = [0] },
            "tarotcard" or "tarot" or "tarots" => new TarotCardClause { Tarots = ParseEnumArray<MotelyTarotCard>(value) },
            "spectralcard" or "spectral" or "spectrals" => new SpectralCardClause { Spectrals = ParseEnumArray<MotelySpectralCard>(value) },
            "planetcard" or "planet" or "planets" => new PlanetCardClause { Planets = ParseEnumArray<MotelyPlanetCard>(value) },
            "standardcard" or "playingcard" or "playingcards" => new StandardCardClause(),
            "boss" or "bosses" => new BossClause { Bosses = ParseEnumArray<MotelyBossBlind>(value) },
            "tag" or "tags" or "smallblindtag" or "bigblindtag" => new TagClause { Tags = ParseEnumArray<MotelyTag>(value), Rolls = GetTagRolls(discriminator) },
            "and" => new AndClause(),
            "or" => new OrClause(),
            "startingdraw" or "startingdraw" => new StartingDrawClause(),
            _ => throw new NotSupportedException($"Unsupported clause discriminator '{discriminator}'")
        };
    }

    public static void SetDiscriminator(this IJamlClause clause, string discriminator, string value)
    {
        // The concrete type already encodes the discriminator; this method just populates the value.
        // For cases where the caller created a generic placeholder, we replace the concrete values.
        switch (clause)
        {
            case JokerClause c: c.Jokers = ParseEnumArray<MotelyJoker>(value); break;
            case CommonJokerClause c: c.Jokers = ParseEnumArray<MotelyJokerCommon>(value); break;
            case UncommonJokerClause c: c.Jokers = ParseEnumArray<MotelyJokerUncommon>(value); break;
            case RareJokerClause c: c.Jokers = ParseEnumArray<MotelyJokerRare>(value); break;
            case LegendaryJokerClause c: c.Jokers = ParseEnumArray<MotelyJoker>(value); break;
            case VoucherClause c: c.Vouchers = ParseEnumArray<MotelyVoucher>(value); break;
            case TarotCardClause c: c.Tarots = ParseEnumArray<MotelyTarotCard>(value); break;
            case SpectralCardClause c: c.Spectrals = ParseEnumArray<MotelySpectralCard>(value); break;
            case PlanetCardClause c: c.Planets = ParseEnumArray<MotelyPlanetCard>(value); break;
            case BossClause c: c.Bosses = ParseEnumArray<MotelyBossBlind>(value); break;
            case TagClause c: c.Tags = ParseEnumArray<MotelyTag>(value); break;
        }
    }

    public static IJamlClause[]? GetClauses(this IJamlClause clause)
    {
        return clause is LogicClause logic ? logic.Clauses : null;
    }

    public static bool IsOr(this IJamlClause clause) => clause is OrClause;
    public static bool IsAnd(this IJamlClause clause) => clause is AndClause;

    public static void SetClauses(this IJamlClause clause, IJamlClause[] clauses)
    {
        if (clause is LogicClause logic)
            logic.Clauses = clauses;
    }

    public static void AddOr(this IJamlClause clause, IJamlClause child)
    {
        if (clause is OrClause logic)
            logic.Clauses = [.. logic.Clauses, child];
    }

    public static void AddAnd(this IJamlClause clause, IJamlClause child)
    {
        if (clause is AndClause logic)
            logic.Clauses = [.. logic.Clauses, child];
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

    private static T[] ParseEnumArray<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(t => Enum.TryParse<T>(t, true, out _))
            .Select(t => Enum.Parse<T>(t, true))
            .ToArray();
    }

    private static T? ParseEnumNullable<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            return null;
        if (Enum.TryParse<T>(value.Trim(), true, out var parsed))
            return parsed;
        return null;
    }

    private static MotelyItemEdition? ParseEdition(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition) || edition.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;
        if (Enum.TryParse<MotelyItemEdition>(edition.Trim(), true, out var parsed))
            return parsed;
        return null;
    }

    private static int[] GetTagRolls(string discriminator)
    {
        return discriminator?.ToLowerInvariant() switch
        {
            "smallblindtag" => [0],
            "bigblindtag" => [1],
            _ => [0, 1],
        };
    }
}
