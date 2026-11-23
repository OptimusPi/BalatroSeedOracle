using System;
using System.Collections.Generic;
using System.Linq;
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

    public static string CleanSeedToken(string s)
    {
        var trimmed = s.Trim().ToUpperInvariant();
        Span<char> buf = stackalloc char[trimmed.Length];
        int n = 0;
        foreach (var c in trimmed)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-')
                buf[n++] = c;
        }
        return new string(buf[..n]);
    }

    public static IReadOnlyList<string> Parse(string raw)
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
                    return Parse(pile);

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

        return trimmed
            .Split([',', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanSeedToken)
            .Where(s => s.Length > 0 && SeedToken.IsMatch(s))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static (IReadOnlyList<string> Seeds, SeedPasteExtras Extras) ParseWithMeta(string raw)
    {
        var extras = new SeedPasteExtras();
        var trimmed = raw.Trim();
        if (
            trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
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

        return (Parse(raw), extras);
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
            var key = Uri.UnescapeDataString(part[..eq]);
            var val = Uri.UnescapeDataString(part[(eq + 1)..]);
            dict[key] = val;
        }
        return dict;
    }
}
