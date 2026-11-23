using System;
using System.Collections.Generic;
using System.Linq;
using Motely;
using Motely.Analysis;

namespace BalatroSeedOracle.Analysis;

/// <summary>One Blueprint-loop landmark in a seed (0-based shop/pack slots).</summary>
public sealed record LoopWatchHit(string Label, int Ante, string Location, string Kind)
{
    public string Display => $"{Label} · A{Ante} · {Location}";
}

/// <summary>
/// Scans a Jamlyzer breakdown for Nat's loop landmarks: Soul→legendary, Diet Cola, Ankh,
/// Negative Tag, Oops! All 6s, Blueprint, Brainstorm, Showman.
/// </summary>
public static class SeedLoopWatch
{
    private static readonly HashSet<string> WatchJokers = new(StringComparer.Ordinal)
    {
        "Diet Cola",
        "Oops! All 6s",
        "Blueprint",
        "Brainstorm",
        "Showman",
        "Perkeo",
    };

    private static readonly HashSet<string> WatchSpectrals = new(StringComparer.Ordinal) { "Ankh" };

    private static readonly HashSet<string> WatchTags = new(StringComparer.Ordinal)
    {
        "Negative Tag",
    };

    public static IReadOnlyList<LoopWatchHit> Scan(MotelyUnitTestAnalysis analysis)
    {
        if (analysis.Error is not null || analysis.Antes.Count == 0)
            return [];

        var hits = new List<LoopWatchHit>();

        foreach (var leg in analysis.LegendaryOrder ?? [])
        {
            hits.Add(
                new(
                    $"Soul → {FormatUtils.FormatDisplayName(leg.Joker.ToString())}",
                    leg.Ante,
                    $"soul pack slot {leg.PackSlot}",
                    "soul"
                )
            );
        }

        foreach (var ante in analysis.Antes)
        {
            if (ante.Ante < 0)
                continue;

            var smallTag = FormatUtils.FormatTag(ante.SmallBlindTag);
            var bigTag = FormatUtils.FormatTag(ante.BigBlindTag);
            if (WatchTags.Contains(smallTag))
                hits.Add(new(smallTag, ante.Ante, "small blind tag", "tag"));
            if (WatchTags.Contains(bigTag))
                hits.Add(new(bigTag, ante.Ante, "big blind tag", "tag"));

            for (int i = 0; i < ante.ShopQueue.Count; i++)
            {
                var name = BaseName(ante.ShopQueue[i].Name);
                var kind = ClassifyCard(name);
                if (kind is null)
                    continue;
                hits.Add(new(name, ante.Ante, $"shop slot {i}", kind));
            }

            for (int p = 0; p < ante.Packs.Count; p++)
            {
                var pack = ante.Packs[p];
                for (int c = 0; c < pack.Items.Count; c++)
                {
                    var name = BaseName(pack.Items[c].Name);
                    // The Soul is already covered via LegendaryOrder with the real legendary name.
                    if (name is "The Soul" or "Soul")
                        continue;
                    var kind = ClassifyCard(name);
                    if (kind is null)
                        continue;
                    hits.Add(new(name, ante.Ante, $"pack slot {p} card {c}", kind));
                }
            }
        }

        hits.Sort(
            (x, y) =>
            {
                int a = x.Ante.CompareTo(y.Ante);
                if (a != 0)
                    return a;
                int l = string.Compare(x.Label, y.Label, StringComparison.Ordinal);
                return l != 0 ? l : string.Compare(x.Location, y.Location, StringComparison.Ordinal);
            }
        );
        return hits;
    }

    /// <summary>Strip edition/sticker prefixes so "Negative Blueprint" still matches Blueprint.</summary>
    private static string BaseName(string formatted)
    {
        // FormatItem puts stickers then edition then name. Take the last segment that matches a watch
        // name, else the full string after known prefixes.
        foreach (var joker in WatchJokers)
        {
            if (formatted.EndsWith(joker, StringComparison.Ordinal))
                return joker;
        }
        foreach (var spectral in WatchSpectrals)
        {
            if (formatted.EndsWith(spectral, StringComparison.Ordinal))
                return spectral;
        }
        return formatted;
    }

    private static string? ClassifyCard(string name)
    {
        if (WatchJokers.Contains(name))
            return "joker";
        if (WatchSpectrals.Contains(name))
            return "spectral";
        return null;
    }

    public static string SummarizeKinds(IReadOnlyList<LoopWatchHit> hits)
    {
        if (hits.Count == 0)
            return "No loop landmarks in this depth.";
        var parts = hits
            .GroupBy(h => h.Label)
            .OrderBy(g => g.Min(h => h.Ante))
            .Select(g =>
            {
                var antes = string.Join(",", g.Select(h => h.Ante).Distinct().OrderBy(x => x));
                return $"{g.Key} @A{antes}";
            });
        return string.Join(" · ", parts);
    }
}
