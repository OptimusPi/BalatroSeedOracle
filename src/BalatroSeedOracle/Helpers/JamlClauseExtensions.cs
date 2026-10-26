using System;
using System.Collections.Generic;
using System.Linq;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Factory + mutation helpers for engine filter clauses, used by the visual filter builder when
/// it assembles <see cref="IMotelyClause"/> objects from picked items. Clauses are built by the
/// engine's own loader from a one-clause JAML document, so the engine stays the single source of
/// truth for clause construction (names, "any" wildcards, aliases); decorations are set through
/// the engine's bindable properties, so every clause type works without a per-type switch here.
/// </summary>
public static class JamlClauseExtensions
{
    /// <summary>
    /// Visual-builder item types → JAML clause keys. Order matters for the reverse lookup
    /// (<see cref="BuilderTypeFor"/>): the first builder type naming a key is the one reported.
    /// Any other engine clause key is accepted as a builder type as-is (<c>luckyMoney</c>).
    /// </summary>
    private static readonly (string BuilderType, string ClauseKey)[] BuilderTypes =
    [
        ("Joker", "joker"),
        ("SoulJoker", "legendaryJoker"),
        ("CommonJoker", "commonJoker"),
        ("UncommonJoker", "uncommonJoker"),
        ("RareJoker", "rareJoker"),
        ("Tarot", "tarotCard"),
        ("Planet", "planetCard"),
        ("Spectral", "spectralCard"),
        ("Voucher", "voucher"),
        ("Tag", "tag"),
        ("SmallBlindTag", "smallBlindTag"),
        ("BigBlindTag", "bigBlindTag"),
        ("Boss", "boss"),
        ("BoosterPack", "boosterPack"),
        ("PokerHand", "pokerHand"),
        ("StandardCard", "standardCard"),
        ("PlayingCard", "standardCard"),
        ("And", "and"),
        ("Or", "or"),
    ];

    /// <summary>The JAML clause key for a visual-builder item type, or null when the engine has none.</summary>
    public static string? ClauseKeyForBuilderType(string type)
    {
        foreach (var (builderType, clauseKey) in BuilderTypes)
            if (builderType.Equals(type, StringComparison.OrdinalIgnoreCase))
                return clauseKey;
        return FilterVocabulary.IsDiscriminator(type)
            ? FilterVocabulary.Discriminators.First(k => k.Equals(type, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    /// <summary>The visual-builder item type for a clause ("Joker", "SoulJoker", "Tarot", "Or", "LuckyMoney", …).</summary>
    public static string? BuilderTypeFor(IMotelyClause clause)
    {
        var key = JamlConfigText.ClauseKeyFor(clause.GetType());
        if (key is null)
            return null;
        foreach (var (builderType, clauseKey) in BuilderTypes)
            if (clauseKey == key)
                return builderType;
        return char.ToUpperInvariant(key[0]) + key[1..];
    }

    /// <summary>
    /// Build the engine clause for a visual-builder item (e.g. type "Joker", value "LuckyCat"
    /// or the wildcard value "any"). The value is handed to the engine's own JAML loader,
    /// which understands enum names and "any" wildcards.
    /// </summary>
    public static IMotelyClause CreateClauseForDiscriminator(string type, string clauseValue)
    {
        var clauseKey = ClauseKeyForBuilderType(type)
            ?? throw new InvalidOperationException($"Visual builder produced unknown filter item type '{type}'.");

        var escaped = clauseValue.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var jaml = clauseKey is "and" or "or"
            ? $"must:\n  - {clauseKey}:\n      clauses: []\n"
            : $"must:\n  - {clauseKey}: \"{escaped}\"\n";
        var config = JamlConfigLoader.FromJaml(jaml);
        return config.Must[0];
    }

    /// <summary>Scope the clause to the given antes (or roll indices for roll-scoped clauses).</summary>
    public static void SetAntes(this IMotelyClause clause, int[] antes)
    {
        switch (clause)
        {
            case IAnteScopedClause ante:
                ante.Antes = antes;
                break;
            case IRollScopedClause roll:
                roll.Rolls = antes;
                break;
        }
    }

    /// <summary>Attach an edition decoration ("Foil", …) when the clause supports one.</summary>
    public static void SetEditionString(this IMotelyClause clause, string edition) =>
        SetEnumProperty(clause, "Edition", edition);

    /// <summary>Attach a seal decoration ("Red", …) when the clause supports one.</summary>
    public static void SetSealString(this IMotelyClause clause, string seal) =>
        SetEnumProperty(clause, "Seal", seal);

    /// <summary>Attach an enhancement decoration ("Gold", …) when the clause supports one.</summary>
    public static void SetEnhancementString(this IMotelyClause clause, string enhancement) =>
        SetEnumProperty(clause, "Enhancement", enhancement);

    /// <summary>Attach sticker decorations (rental/eternal/perishable) when the clause supports them.</summary>
    public static void SetStickerStrings(this IMotelyClause clause, string[] stickers)
    {
        var property = clause.GetType().GetProperty("Stickers");
        if (property is null || !property.CanWrite || property.PropertyType != typeof(MotelyJokerSticker[]))
            return;
        var parsed = new List<MotelyJokerSticker>();
        foreach (var name in stickers)
            if (Enum.TryParse<MotelyJokerSticker>(name, ignoreCase: true, out var sticker))
                parsed.Add(sticker);
        property.SetValue(clause, parsed.ToArray());
    }

    /// <summary>
    /// Remove an enum-valued decoration (<c>Edition</c>, <c>Seal</c>, <c>Enhancement</c>) so the clause
    /// matches any: the property goes back to null. A clause without it is left alone.
    /// </summary>
    public static void ClearDecoration(this IMotelyClause clause, string propertyName)
    {
        var property = clause.GetType().GetProperty(propertyName);
        if (property is { CanWrite: true } && Nullable.GetUnderlyingType(property.PropertyType) is not null)
            property.SetValue(clause, null);
    }

    /// <summary>
    /// Set an enum-valued decoration (<c>Edition</c>, <c>Seal</c>, …) by name. A name the enum does
    /// not know is ignored, as is a clause without that property — the builder offers decorations
    /// for every item and only some clauses carry them.
    /// </summary>
    private static void SetEnumProperty(IMotelyClause clause, string propertyName, string value)
    {
        var property = clause.GetType().GetProperty(propertyName);
        if (property is null || !property.CanWrite)
            return;
        var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (target.IsEnum && Enum.TryParse(target, value, ignoreCase: true, out var parsed))
            property.SetValue(clause, parsed);
    }
}
