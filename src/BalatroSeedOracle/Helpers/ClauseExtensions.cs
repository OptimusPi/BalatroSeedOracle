using System.Collections.Generic;
using System.Linq;
using Motely.Filters;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Display helpers over engine filter clauses: what an item is called, which item(s) it
/// names, and the edition/seal/enhancement/sticker decorations it carries.
/// TIER0-STUB: replace in Tier 1 with a proper clause-type → display-metadata mapping
/// (reflection-based here so every current and future clause type keeps working).
/// </summary>
public static class ClauseExtensions
{
    /// <summary>Display name of the clause's item list (e.g. "Joker4, Joker10"), or null when none.</summary>
    public static string? GetValueName(this IMotelyClause clause)
    {
        foreach (var prop in clause.GetType().GetProperties())
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length != 0)
                continue;
            if (prop.PropertyType == typeof(JamlEventRolls))
                continue;
            var value = prop.GetValue(clause);
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var names = Names(enumerable).ToList();
                if (names.Count > 0)
                    return string.Join(", ", names);
            }
            else if (value is System.Enum single)
            {
                var name = DisplayName(single);
                if (name is not null)
                    return name;
            }
        }
        return null;
    }

    /// <summary>Short category name for the clause ("Joker", "Legendary Joker", "Boss", …).</summary>
    public static string? GetTypeName(this IMotelyClause clause)
    {
        var name = clause.GetType().Name;
        return name.EndsWith("Clause", System.StringComparison.Ordinal)
            ? SplitPascal(name[..^"Clause".Length])
            : name;
    }

    /// <summary>Edition decoration ("Foil", …) or null when the clause has/none carries one.</summary>
    public static string? GetEditionString(this IMotelyClause clause) =>
        EnumProperty(clause, "Edition");

    /// <summary>Seal decoration ("Red", …) or null when none.</summary>
    public static string? GetSealString(this IMotelyClause clause) =>
        EnumProperty(clause, "Seal");

    /// <summary>Enhancement decoration ("Gold", …) or null when none.</summary>
    public static string? GetEnhancementString(this IMotelyClause clause) =>
        EnumProperty(clause, "Enhancement");

    /// <summary>Sticker names (rental/eternal/perishable), or null when the clause carries none.</summary>
    public static string[]? GetStickerStrings(this IMotelyClause clause)
    {
        var stickers = clause.GetType().GetProperty("Stickers")?.GetValue(clause);
        if (stickers is not System.Collections.IEnumerable enumerable || stickers is string)
            return null;
        var names = Names(enumerable).ToList();
        return names.Count == 0 ? null : names.ToArray();
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

    private static string? EnumProperty(IMotelyClause clause, string propertyName)
    {
        var value = clause.GetType().GetProperty(propertyName)?.GetValue(clause);
        return value is System.Enum e ? DisplayName(e) : null;
    }

    private static IEnumerable<string> Names(System.Collections.IEnumerable values)
    {
        foreach (var value in values)
        {
            if (value is System.Enum e)
            {
                var name = DisplayName(e);
                if (name is not null)
                    yield return name;
            }
        }
    }

    private static string? DisplayName(System.Enum value)
    {
        var name = System.Enum.GetName(value.GetType(), value);
        return name is null or "None" ? null : SplitPascal(name);
    }

    private static string SplitPascal(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
