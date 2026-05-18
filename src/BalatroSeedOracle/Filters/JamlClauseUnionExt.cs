using System;

namespace Motely.Filters;

internal static class JamlClauseUnionExt
{
    public static string GetTypeName(this JamlClauseUnion c) =>
        c.Joker is not null ? "joker"
        : c.Jokers is not null ? "joker"
        : c.CommonJoker is not null ? "commonJoker"
        : c.CommonJokers is not null ? "commonJoker"
        : c.UncommonJoker is not null ? "uncommonJoker"
        : c.UncommonJokers is not null ? "uncommonJoker"
        : c.RareJoker is not null ? "rareJoker"
        : c.RareJokers is not null ? "rareJoker"
        : c.LegendaryJoker is not null ? "legendaryJoker"
        : c.LegendaryJokers is not null ? "legendaryJoker"
        : c.Voucher is not null ? "voucher"
        : c.Vouchers is not null ? "voucher"
        : c.TarotCard is not null ? "tarotCard"
        : c.TarotCards is not null ? "tarotCard"
        : c.SpectralCard is not null ? "spectralCard"
        : c.SpectralCards is not null ? "spectralCard"
        : c.PlanetCard is not null ? "planetCard"
        : c.Boss is not null ? "boss"
        : c.Tag is not null ? "tag"
        : c.SmallBlindTag is not null ? "smallBlindTag"
        : c.BigBlindTag is not null ? "bigBlindTag"
        : c.StandardCard is not null ? "standardCard"
        : c.StandardCards is not null ? "standardCard"
        : c.ErraticRank is not null ? "erraticRank"
        : c.ErraticSuit is not null ? "erraticSuit"
        : c.ErraticCard is not null ? "erraticCard"
        : c.StartingDraw is not null ? "startingDraw"
        : c.Event is not null ? "event"
        : c.And is not null ? "and"
        : c.Or is not null ? "or"
        : "";

    public static string GetValueName(this JamlClauseUnion c) =>
        c.Joker is { IsAny: true } ? "Any"
        : c.Joker is { } j ? j.Value.ToString()
        : c.CommonJoker is { IsAny: true } ? "Any"
        : c.CommonJoker is { } cj ? cj.Value.ToString()
        : c.UncommonJoker is { IsAny: true } ? "Any"
        : c.UncommonJoker is { } uj ? uj.Value.ToString()
        : c.RareJoker is { IsAny: true } ? "Any"
        : c.RareJoker is { } rj ? rj.Value.ToString()
        : c.LegendaryJoker is { IsAny: true } ? "Any"
        : c.LegendaryJoker is { } lj ? lj.Value.ToString()
        : c.Voucher?.ToString()
        ?? c.TarotCard?.ToString()
        ?? c.SpectralCard?.ToString()
        ?? c.PlanetCard?.ToString()
        ?? c.Boss?.ToString()
        ?? c.Tag?.ToString()
        ?? c.SmallBlindTag?.ToString()
        ?? c.BigBlindTag?.ToString()
        ?? c.StandardCard?.StringValue
        ?? c.ErraticRank
        ?? c.ErraticSuit
        ?? c.ErraticCard
        ?? c.StartingDraw
        ?? c.Event?.ToString()
        ?? "";

    public static void SetDiscriminator(this JamlClauseUnion c, string type, string? value)
    {
        c.Joker = null; c.Jokers = null;
        c.CommonJoker = null; c.CommonJokers = null;
        c.UncommonJoker = null; c.UncommonJokers = null;
        c.RareJoker = null; c.RareJokers = null;
        c.LegendaryJoker = null; c.LegendaryJokers = null;
        c.Voucher = null; c.Vouchers = null;
        c.TarotCard = null; c.TarotCards = null;
        c.SpectralCard = null; c.SpectralCards = null;
        c.PlanetCard = null;
        c.Boss = null;
        c.Tag = null; c.SmallBlindTag = null; c.BigBlindTag = null;
        c.StandardCard = null; c.StandardCards = null;
        c.ErraticRank = null; c.ErraticSuit = null; c.ErraticCard = null;
        c.StartingDraw = null;
        c.Event = null;

        bool isAny = string.Equals(value, "Any", StringComparison.OrdinalIgnoreCase);

        switch (type?.ToLowerInvariant())
        {
            case "joker":
                if (isAny) c.Joker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJoker>.Any;
                else if (Enum.TryParse<Motely.Enums.MotelyJoker>(value, true, out var jv)) c.Joker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJoker>.Of(jv);
                break;
            case "commonjoker":
                if (isAny) c.CommonJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerCommon>.Any;
                else if (Enum.TryParse<Motely.Enums.MotelyJokerCommon>(value, true, out var cjv)) c.CommonJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerCommon>.Of(cjv);
                break;
            case "uncommonjoker":
                if (isAny) c.UncommonJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerUncommon>.Any;
                else if (Enum.TryParse<Motely.Enums.MotelyJokerUncommon>(value, true, out var ujv)) c.UncommonJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerUncommon>.Of(ujv);
                break;
            case "rarejoker":
                if (isAny) c.RareJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerRare>.Any;
                else if (Enum.TryParse<Motely.Enums.MotelyJokerRare>(value, true, out var rjv)) c.RareJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerRare>.Of(rjv);
                break;
            case "legendaryjoker":
            case "souljoker":
                if (isAny) c.LegendaryJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerLegendary>.Any;
                else if (Enum.TryParse<Motely.Enums.MotelyJokerLegendary>(value, true, out var ljv)) c.LegendaryJoker = Motely.Filters.Jaml.EnumOrAny<Motely.Enums.MotelyJokerLegendary>.Of(ljv);
                break;
            case "voucher":
                if (Enum.TryParse<Motely.Enums.MotelyVoucher>(value, true, out var vv)) c.Voucher = vv;
                break;
            case "tarot":
            case "tarotcard":
                if (Enum.TryParse<Motely.Enums.MotelyTarotCard>(value, true, out var tv)) c.TarotCard = tv;
                break;
            case "spectral":
            case "spectralcard":
                if (Enum.TryParse<Motely.Enums.MotelySpectralCard>(value, true, out var sv)) c.SpectralCard = sv;
                break;
            case "planet":
            case "planetcard":
                if (Enum.TryParse<Motely.Enums.MotelyPlanetCard>(value, true, out var pv)) c.PlanetCard = pv;
                break;
            case "boss":
                if (Enum.TryParse<Motely.Enums.MotelyBossBlind>(value, true, out var bv)) c.Boss = bv;
                break;
            case "tag":
                if (Enum.TryParse<Motely.Enums.MotelyTag>(value, true, out var tgv)) c.Tag = tgv;
                break;
            case "smallblindtag":
                if (Enum.TryParse<Motely.Enums.MotelyTag>(value, true, out var sbtv)) c.SmallBlindTag = sbtv;
                break;
            case "bigblindtag":
                if (Enum.TryParse<Motely.Enums.MotelyTag>(value, true, out var bbtv)) c.BigBlindTag = bbtv;
                break;
            case "standardcard":
            case "playingcard":
                c.StandardCard = new Motely.Filters.Jaml.StandardCardValue { StringValue = value };
                break;
            case "erraticrank":
                c.ErraticRank = value;
                break;
            case "erraticsuit":
                c.ErraticSuit = value;
                break;
            case "erraticcard":
                c.ErraticCard = value;
                break;
            case "startingdraw":
                c.StartingDraw = value;
                break;
            case "event":
                if (Enum.TryParse<Motely.Enums.MotelyEventType>(value, true, out var ev)) c.Event = ev;
                break;
        }
    }

    public static string? GetEditionString(this JamlClauseUnion c) => c.Edition?.ToString();

    public static void SetEditionString(this JamlClauseUnion c, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase) || value.Equals("none", StringComparison.Ordinal))
            c.Edition = null;
        else if (Enum.TryParse<Motely.Enums.MotelyItemEdition>(value, true, out var e))
            c.Edition = e;
    }

    public static string? GetSealString(this JamlClauseUnion c) => c.Seal?.ToString();

    public static void SetSealString(this JamlClauseUnion c, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            c.Seal = null;
        else if (Enum.TryParse<Motely.Enums.MotelyItemSeal>(value, true, out var s))
            c.Seal = s;
    }

    public static string? GetEnhancementString(this JamlClauseUnion c) => c.Enhancement?.ToString();

    public static void SetEnhancementString(this JamlClauseUnion c, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            c.Enhancement = null;
        else if (Enum.TryParse<Motely.Enums.MotelyItemEnhancement>(value, true, out var en))
            c.Enhancement = en;
    }

    public static string[]? GetStickerStrings(this JamlClauseUnion c) =>
        c.Stickers?.Length > 0 ? Array.ConvertAll(c.Stickers, s => s.ToString()) : null;

    public static void SetStickerStrings(this JamlClauseUnion c, string[]? value)
    {
        if (value is null || value.Length == 0) { c.Stickers = null; return; }
        var result = new System.Collections.Generic.List<Motely.Enums.MotelyJokerSticker>(value.Length);
        foreach (var s in value)
            if (Enum.TryParse<Motely.Enums.MotelyJokerSticker>(s, true, out var st)) result.Add(st);
        c.Stickers = result.Count > 0 ? result.ToArray() : null;
    }
}
