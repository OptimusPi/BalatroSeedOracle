using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroSeedOracle.Analysis;

/// <summary>Extras pulled from a JackOS / Blueprint share URL alongside a seed pile.</summary>
public sealed record SeedPasteExtras(
    string? Deck = null,
    string? Stake = null,
    int? MaxAnte = null,
    int? SeedIndex = null
);

/// <summary>
/// Parse multi-seed paste: newline, comma, space, or a share URL with <c>?seeds=</c>.
/// </summary>
public static class SeedPaste
{
    public const int MaxSeedChars = 8;

    private static readonly Regex SeedToken = new(
        @"^[A-Za-z0-9_-]{1,8}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex DeckSuffix = new(
        @"\s+deck$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex StakeSuffix = new(
        @"\s+stake$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Clean one token. Rejects oversized garbage early so a huge unbroken paste cannot
    /// stack-allocate a megabyte buffer.
    /// </summary>
    public static string CleanSeedToken(string s)
    {
        var trimmed = s.Trim();
        if (trimmed.Length == 0)
            return "";
        // Seeds are at most 8 chars; anything longer after strip is not a seed.
        if (trimmed.Length > MaxSeedChars * 4)
            return "";

        var sb = new StringBuilder(Math.Min(trimmed.Length, MaxSeedChars));
        foreach (var c in trimmed)
        {
            char u = char.ToUpperInvariant(c);
            if (char.IsLetterOrDigit(u) || u is '_' or '-')
            {
                if (sb.Length >= MaxSeedChars)
                    return ""; // too many kept chars → not a Balatro seed
                sb.Append(u);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Parse seeds. When <paramref name="preserveDuplicates"/> is false (default for free text),
    /// duplicates collapse. URL piles keep positions so <c>seedIndex</c> still points at the
    /// authored slot; callers then map that index onto the unique list.
    /// </summary>
    public static IReadOnlyList<string> Parse(string raw, bool preserveDuplicates = false)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            return [];

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                var qs = ParseQuery(uri.Query);
                if (qs.TryGetValue("seeds", out var pile) && !string.IsNullOrWhiteSpace(pile))
                    return Parse(pile, preserveDuplicates: true);

                var pathMatch = Regex.Match(
                    uri.AbsolutePath,
                    @"^/seed/([A-Za-z0-9_-]{1,8})/?$",
                    RegexOptions.IgnoreCase
                );
                if (pathMatch.Success)
                {
                    var tok = CleanSeedToken(pathMatch.Groups[1].Value);
                    return tok.Length > 0 && SeedToken.IsMatch(tok) ? [tok] : [];
                }

                if (qs.TryGetValue("seed", out var one) && !string.IsNullOrWhiteSpace(one))
                {
                    var tok = CleanSeedToken(one);
                    return tok.Length > 0 && SeedToken.IsMatch(tok) ? [tok] : [];
                }
            }
        }

        var tokens = trimmed
            .Split([',', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanSeedToken)
            .Where(s => s.Length > 0 && SeedToken.IsMatch(s));

        return preserveDuplicates
            ? tokens.ToList()
            : tokens.Distinct(StringComparer.Ordinal).ToList();
    }

    public static (IReadOnlyList<string> Seeds, SeedPasteExtras Extras) ParseWithMeta(string raw)
    {
        var extras = new SeedPasteExtras();
        var trimmed = raw.Trim();
        bool fromUrl = false;
        if (
            trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            fromUrl = true;
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                var qs = ParseQuery(uri.Query);
                string? deck = null;
                string? stake = null;
                int? maxAnte = null;
                int? seedIndex = null;

                if (qs.TryGetValue("deck", out var d) && !string.IsNullOrWhiteSpace(d))
                    deck = DeckSuffix.Replace(d.Trim(), "");
                if (qs.TryGetValue("stake", out var st) && !string.IsNullOrWhiteSpace(st))
                    stake = StakeSuffix.Replace(st.Trim(), "");
                if (qs.TryGetValue("maxAnte", out var ma)
                    && int.TryParse(ma, out var maxAnteVal))
                    maxAnte = Math.Min(39, Math.Max(8, maxAnteVal));
                if (qs.TryGetValue("seedIndex", out var si)
                    && int.TryParse(si, out var idx))
                    seedIndex = Math.Max(0, idx);

                extras = new SeedPasteExtras(deck, stake, maxAnte, seedIndex);
            }
        }

        var rawSeeds = Parse(raw, preserveDuplicates: fromUrl);
        // Unique list for the UI pile; remap seedIndex to the unique seed that occupied that slot.
        var unique = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in rawSeeds)
        {
            if (seen.Add(s))
                unique.Add(s);
        }

        int? remappedIndex = extras.SeedIndex;
        if (fromUrl && extras.SeedIndex is int authored && authored < rawSeeds.Count)
        {
            var target = rawSeeds[authored];
            remappedIndex = unique.IndexOf(target);
            if (remappedIndex < 0)
                remappedIndex = 0;
        }
        else if (remappedIndex is int ri)
        {
            remappedIndex = Math.Clamp(ri, 0, Math.Max(0, unique.Count - 1));
        }

        return (unique, extras with { SeedIndex = remappedIndex });
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return dict;
        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq < 0)
                continue;
            // Form-style query strings use + for space; Uri.UnescapeDataString leaves them alone.
            var key = Uri.UnescapeDataString(part[..eq].Replace('+', ' '));
            var val = Uri.UnescapeDataString(part[(eq + 1)..].Replace('+', ' '));
            dict[key] = val;
        }
        return dict;
    }
}
