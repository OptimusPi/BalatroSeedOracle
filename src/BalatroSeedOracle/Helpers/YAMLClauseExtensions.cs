using System;
using System.Linq;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Factory + mutation helpers for engine filter clauses, used by the visual filter
/// builder when it assembles <see cref="IMotelyClause"/> objects from picked items.
/// TIER0-STUB: replace in Tier 1. Clauses are produced by round-tripping a one-clause
/// JAML document through the engine's own loader (the engine is the single source of
/// truth for clause construction), then decorated via reflection so any clause type
/// keeps working without a per-type switch here.
/// </summary>
public static class YAMLClauseExtensions
{
    /// <summary>
    /// Visual-builder type discriminator → JAML clause key (mirrors the engine's
    /// internal <c>JamlClauseBinding.ClauseTypeByKey</c> for the subset the builder emits).
    /// </summary>
    private static readonly (string Discriminator, string ClauseKey)[] TypeToClauseKey =
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
    ];

    /// <summary>
    /// Build the engine clause for a visual-builder item (e.g. type "Joker", value "LuckyCat"
    /// or the wildcard value "any"). The value is handed to the engine's own JAML loader,
    /// which understands enum names, "any" wildcards, and multi-name lists.
    /// </summary>
    public static IMotelyClause CreateClauseForDiscriminator(string type, string clauseValue)
    {
        var match = TypeToClauseKey.FirstOrDefault(
            pair => string.Equals(pair.Discriminator, type, StringComparison.OrdinalIgnoreCase)
        );
        if (match.Discriminator is null)
        {
            throw new InvalidOperationException(
                $"Visual builder produced unknown filter item type '{type}'."
            );
        }

        var escaped = clauseValue.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var jaml = $"must:\n  - {match.ClauseKey}: [\"{escaped}\"]\n";
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
    public static void SetEditionString(this IMotelyClause clause, string edition)
    {
        var property = clause.GetType().GetProperty("Edition");
        if (
            property is null
            || !property.CanWrite
            || property.PropertyType != typeof(MotelyItemEdition?)
        )
        {
            return;
        }
        if (Enum.TryParse<MotelyItemEdition>(edition, ignoreCase: true, out var parsed))
        {
            property.SetValue(clause, parsed);
        }
    }

    /// <summary>Attach a seal decoration ("Red", …) when the clause supports one.</summary>
    public static void SetSealString(this IMotelyClause clause, string seal) =>
        SetEnumProperty(clause, "Seal", seal);

    /// <summary>Attach an enhancement decoration ("Gold", …) when the clause supports one.</summary>
    public static void SetEnhancementString(this IMotelyClause clause, string enhancement) =>
        SetEnumProperty(clause, "Enhancement", enhancement);

    private static void SetEnumProperty(IMotelyClause clause, string propertyName, string value)
    {
        var property = clause.GetType().GetProperty(propertyName);
        if (property is null || !property.CanWrite)
        {
            return;
        }
        var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (target.IsEnum && Enum.TryParse(target, value, ignoreCase: true, out var parsed))
        {
            property.SetValue(clause, parsed);
        }
    }

    /// <summary>Attach sticker decorations (rental/eternal/perishable) when the clause supports them.</summary>
    public static void SetStickerStrings(this IMotelyClause clause, string[] stickers)
    {
        var property = clause.GetType().GetProperty("Stickers");
        if (property is null || !property.CanWrite || property.PropertyType != typeof(MotelyJokerSticker[]))
        {
            return;
        }
        var parsed = stickers
            .Select(name => Enum.TryParse<MotelyJokerSticker>(name, ignoreCase: true, out var s) ? s : (MotelyJokerSticker?)null)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .ToArray();
        property.SetValue(clause, parsed);
    }
}
