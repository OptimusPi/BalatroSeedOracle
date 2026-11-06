using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Motely;
using Motely.Filters;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Filter checks the editor runs: does the JAML load and plan, and does it ask for a boss in an
/// ante where that boss can never appear.
/// </summary>
public static class FilterCheck
{
    public static bool TryCheck(
        string text,
        [NotNullWhen(true)] out JamlConfig? config,
        [NotNullWhen(true)] out JamlSearchPlan? plan,
        [NotNullWhen(false)] out string? error
    )
    {
        plan = null;
        if (!JamlConfigLoader.TryLoad(text, out config, out var loadError))
        {
            error = loadError ?? "the filter did not load.";
            return false;
        }
        try
        {
            plan = MotelySearchBuilder.CreatePlan(config);
            error = null;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Mistakes that load and plan fine but quietly cost matches: a boss asked for in an ante it
    /// can never appear in (The Eye before ante 3, a showdown boss off ante 8).
    /// </summary>
    public static IReadOnlyList<string> Warnings(JamlConfig config)
    {
        List<string> warnings = [];
        Scan(config.Must, "must");
        Scan(config.Should, "should");
        Scan(config.MustNot, "mustNot");
        return warnings;

        void Scan(List<IMotelyClause> clauses, string section)
        {
            foreach (var (blind, antes) in clauses.OfType<BossClause>().SelectMany(c => c.Bosses.Select(b => (b, c.Antes))))
            {
                var impossible = antes.Where(a => !CanAppear(blind, a)).ToArray();
                if (impossible.Length == 0)
                    continue;

                var rule = blind.GetBossType() == MotelyBossBlindType.Finisher
                    ? "a showdown boss appears only in antes 8, 16, ..."
                    : $"{blind} first appears in ante {Math.Max(1, blind.GetBossMinAnte())}, and never in antes 8, 16, ...";
                warnings.Add(
                    impossible.Length == antes.Length
                        ? $"{section} boss {blind}: antes [{string.Join(", ", antes)}] are all impossible ({rule}); "
                            + (section == "must" ? "the filter can never match." : "this clause never counts.")
                        : $"{section} boss {blind}: ante(s) {string.Join(", ", impossible)} can never hold it ({rule})."
                );
            }
        }

        static bool CanAppear(MotelyBossBlind blind, int ante) =>
            ante >= 1
            && (
                blind.GetBossType() == MotelyBossBlindType.Finisher
                    ? ante % 8 == 0
                    : ante % 8 != 0 && ante >= blind.GetBossMinAnte()
            );
    }
}
