using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Motely.Filters;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Read-side helpers over engine filter clauses for the visual builder and filter previews: the
/// builder item type a clause maps to, the item name(s) it names, and its decorations. Names are
/// the engine's raw enum names (<c>LuckyCat</c>, <c>TheEye</c>) — what the sprite lookups and
/// <see cref="JamlClauseExtensions.CreateClauseForDiscriminator"/> take — never display strings.
/// The value a clause names is read through the engine's own discriminator property
/// (<see cref="FilterVocabulary.ValuePropertyFor"/>), so every clause type is covered.
/// </summary>
public static class ClauseExtensions
{
    /// <summary>
    /// The item name(s) the clause names (<c>LuckyCat</c>; several as <c>"LuckyCat, Blueprint"</c>),
    /// <c>"Any"</c> for a category wildcard (<c>joker:</c> with no name), or null when the clause
    /// names no items (and/or, event rolls).
    /// </summary>
    public static string? GetValueName(this IMotelyClause clause)
    {
        var key = JamlConfigText.ClauseKeyFor(clause.GetType());
        if (key is null || FilterVocabulary.ValueEnumType(key) is null)
            return null;
        var value = FilterVocabulary.ValuePropertyFor(key)?.GetValue(clause);
        switch (value)
        {
            case Enum single:
                return RawName(single);
            case IEnumerable items and not string:
                var names = Names(items).ToList();
                return names.Count == 0 ? "Any" : string.Join(", ", names);
            default:
                return null;
        }
    }

    /// <summary>The visual-builder item type ("Joker", "SoulJoker", "Tarot", "Boss", "Or", …).</summary>
    public static string? GetTypeName(this IMotelyClause clause) => JamlClauseExtensions.BuilderTypeFor(clause);

    /// <summary>Edition decoration ("Foil", …) or null when the clause carries none.</summary>
    public static string? GetEditionString(this IMotelyClause clause) => EnumProperty(clause, "Edition");

    /// <summary>Seal decoration ("Red", …) or null when none.</summary>
    public static string? GetSealString(this IMotelyClause clause) => EnumProperty(clause, "Seal");

    /// <summary>Enhancement decoration ("Gold", …) or null when none.</summary>
    public static string? GetEnhancementString(this IMotelyClause clause) => EnumProperty(clause, "Enhancement");

    /// <summary>Sticker names (Eternal, Perishable, Rental), or null when the clause carries none.</summary>
    public static string[]? GetStickerStrings(this IMotelyClause clause)
    {
        if (clause.GetType().GetProperty("Stickers")?.GetValue(clause) is not IEnumerable stickers || stickers is string)
            return null;
        var names = Names(stickers).ToArray();
        return names.Length == 0 ? null : names;
    }

    /// <summary>Child clauses of an and/or clause, or null for leaf clauses.</summary>
    public static IMotelyClause[]? GetClauses(this IMotelyClause clause) =>
        clause is LogicClause logic ? logic.Clauses : null;

    /// <summary>Ante numbers the clause applies to (roll indices for roll-scoped event clauses).</summary>
    public static int[] GetAntes(this IMotelyClause clause) =>
        clause switch
        {
            IAnteScopedClause ante => ante.Antes,
            IRollScopedClause roll => roll.Rolls,
            _ => [],
        };

    private static string? EnumProperty(IMotelyClause clause, string propertyName) =>
        clause.GetType().GetProperty(propertyName)?.GetValue(clause) is Enum e ? RawName(e) : null;

    private static IEnumerable<string> Names(IEnumerable values)
    {
        foreach (var value in values)
            if (value is Enum e && RawName(e) is { } name)
                yield return name;
    }

    /// <summary>The enum's own name; null for a "None" placeholder or an undefined value.</summary>
    private static string? RawName(Enum value)
    {
        var name = Enum.GetName(value.GetType(), value);
        return name is null or "None" ? null : name;
    }
}
