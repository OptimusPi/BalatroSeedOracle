using System.Globalization;

using System;
using System.Collections.Generic;
using System.Linq;
using Motely;
using Motely.Filters;

namespace BalatroSeedOracle.Analysis;

/// <summary>
/// A clause written as one line of text instead of a mapping. A list item in <c>must</c>,
/// <c>should</c>, <c>mustNot</c> (or an <c>and:</c>/<c>or:</c> list) that is a plain string is
/// read here and becomes exactly the clause its mapping twin would:
/// <code>
/// [Edition] &lt;Name&gt; [in ante|antes &lt;ante list&gt;] [score &lt;N&gt;]
///
/// - Perkeo in ante 1 or 2              → legendaryJoker: Perkeo,  antes: [1, 2]
/// - The Eye in ante 3                  → boss: TheEye,            antes: [3]
/// - Negative Blueprint in antes 1-3    → joker: Blueprint, edition: Negative, antes: [1, 2, 3]
/// - Black Hole in ante 2, 3 and 4      → spectralCard: BlackHole, antes: [2, 3, 4]
/// - Perkeo in antes 1-8 score 5        → legendaryJoker: Perkeo,  antes: [1..8], score: 5
/// - Triboulet                          → legendaryJoker: Triboulet (no antes key)
/// </code>
/// <para>
/// <b>Name</b>: an engine name from one of the families jokers, bosses, vouchers, tags, tarot,
/// planet and spectral cards, matched ignoring case, spaces and punctuation ("the eye" is TheEye,
/// "Charm Tag" is CharmTag), and a leading "The" either way ("Hook" is TheHook). A legendary joker
/// becomes a <c>legendaryJoker</c> clause, any other joker a <c>joker</c> clause. <b>Edition</b>
/// (Foil, Holographic, Polychrome, Negative) is an optional first word, for jokers only.
/// <b>Ante list</b>: numbers and inclusive ranges (<c>1-3</c>) separated by commas, <c>or</c> or
/// <c>and</c>, after <c>in ante</c>/<c>in antes</c>. The antes come out ascending, without repeats.
/// Without them the line is its mapping twin with no <c>antes</c> key: the clause's own default
/// antes, or the filter's <c>defaults:</c>. <b>Score</b>: a trailing <c>score N</c> is
/// <c>score: N</c>. Every other key keeps its default (min 1, default sources); anything more
/// needs the mapping form.
/// </para>
/// No reflection and no Regex: the names come from <see cref="Enum.GetValues{TEnum}"/> per
/// family, so it runs unchanged under NativeAOT.
/// </summary>
internal static class JamlShorthand
{
    internal enum Family { Joker, Boss, Voucher, Tag, Tarot, Planet, Spectral }

    /// <summary>One name the shorthand can resolve: its family, engine name and enum value.</summary>
    internal readonly record struct Entry(Family Family, string Name, int Value);

    private const string Forms = "`ante 1`, `antes 1 or 2`, `antes 1, 2 and 3` or `antes 1-3`";

    /// <summary>Largest ante the shorthand reads; it keeps a typo'd range (<c>1-1000000</c>) from allocating.</summary>
    private const int MaxAnte = 999;

    private static readonly MotelyItemEdition[] Editions =
        [MotelyItemEdition.Foil, MotelyItemEdition.Holographic, MotelyItemEdition.Polychrome, MotelyItemEdition.Negative];

    private static readonly Dictionary<string, Entry[]> EngineIndex = BuildIndex(EngineEntries());

    /// <summary>Every name of every family the shorthand knows, straight off the engine's enums.</summary>
    internal static IEnumerable<Entry> EngineEntries()
    {
        foreach (var v in Enum.GetValues<MotelyJoker>()) yield return new(Family.Joker, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelyBossBlind>()) yield return new(Family.Boss, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelyVoucher>()) yield return new(Family.Voucher, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelyTag>()) yield return new(Family.Tag, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelyTarotCard>()) yield return new(Family.Tarot, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelyPlanetCard>()) yield return new(Family.Planet, v.ToString(), (int)v);
        foreach (var v in Enum.GetValues<MotelySpectralCard>()) yield return new(Family.Spectral, v.ToString(), (int)v);
    }

    /// <summary>Normalized name → every entry spelled that way (more than one is an ambiguous name).</summary>
    internal static Dictionary<string, Entry[]> BuildIndex(IEnumerable<Entry> entries) =>
        entries
            .GroupBy(e => Normalize(e.Name), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

    /// <summary>
    /// One shorthand clause read: the clause, and whether the line stated its antes and its score
    /// (a key the line leaves out is the mapping twin's omitted key: the clause default, or the
    /// filter's <c>defaults:</c>).
    /// </summary>
    internal readonly record struct Reading(IMotelyClause Clause, bool StatesAntes, bool StatesScore);

    /// <summary>Reads one shorthand clause. A <see cref="FormatException"/> carries the one-line correction.</summary>
    public static Reading Parse(string text) => Parse(text, EngineIndex);

    internal static Reading Parse(string text, Dictionary<string, Entry[]> index)
    {
        var line = text.Trim();
        if (line.Length == 0)
            throw new FormatException("an empty clause: write one like `Perkeo in ante 1`, or a mapping like `joker: Perkeo`.");

        var words = Words(line);
        int? score = ReadScore(ref line, ref words);

        int at = -1;
        for (int i = words.Count - 2; i >= 0; i--)
        {
            if (Is(line, words[i], "in") && IsAnteWord(line, words[i + 1]))
            {
                at = i;
                break;
            }
        }

        if (at < 0)
        {
            int ante = words.FindIndex(w => IsAnteWord(line, w));
            if (ante > 0)
            {
                var before = line[..words[ante].Start].TrimEnd();
                throw new FormatException(
                    $"'{line}': write `in` before the antes, as in `{before} in {line[words[ante].Start..]}`.");
            }
            int bareIn = words.FindLastIndex(w => Is(line, w, "in"));
            if (bareIn > 0 && bareIn + 1 < words.Count && char.IsAsciiDigit(line[words[bareIn + 1].Start]))
            {
                var before = line[..words[bareIn].Start].TrimEnd();
                throw new FormatException(
                    $"'{line}': write `ante` after `in`, as in `{before} in ante {line[words[bareIn + 1].Start..]}`.");
            }

            // A bare name: every ante the clause looks in when its mapping twin omits `antes`.
            var (bareEntry, bareEdition) = Resolve(line, index);
            return new(Build(bareEntry, bareEdition, null, score), StatesAntes: false, StatesScore: score is not null);
        }

        var name = line[..words[at].Start].TrimEnd();
        if (name.Length == 0)
            throw new FormatException($"'{line}' names nothing before `in`: write `<name> in ante N`, as in `Perkeo in ante 1`.");
        var antes = ParseAntes(line[(words[at + 1].Start + words[at + 1].Length)..]);

        var (entry, edition) = Resolve(name, index);
        return new(Build(entry, edition, antes, score), StatesAntes: true, StatesScore: score is not null);
    }

    /// <summary>
    /// Takes a trailing <c>score N</c> (N a whole number, may be negative) off the line. A
    /// <c>score</c> anywhere else, or without its number, is an error; no engine name holds the
    /// word (pinned by a test).
    /// </summary>
    private static int? ReadScore(ref string line, ref List<Word> words)
    {
        int at = -1;
        for (int i = 0; i < words.Count; i++)
            if (Is(line, words[i], "score") || Is(line, words[i], "score:"))
                at = at < 0 ? i : throw new FormatException($"'{line}' says `score` twice: write it once, at the end, as in `Perkeo in ante 1 score 5`.");
        if (at < 0)
            return null;

        var example = at == 0 ? "Perkeo in ante 1 score 5" : $"{line[..words[at].Start].TrimEnd()} score 5";
        if (at != words.Count - 2 || at == 0)
            throw new FormatException($"'{line}': write the score as `score N` at the end, as in `{example}`.");
        var number = line.Substring(words[^1].Start, words[^1].Length);
        if (!int.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int score))
            throw new FormatException($"'{line}': the score is a whole number, as in `{example}`.");

        line = line[..words[at].Start].TrimEnd();
        words = words.GetRange(0, at);
        return score;
    }

    // ── Name ─────────────────────────────────────────────────────────────────────────────

    private static (Entry Entry, MotelyItemEdition? Edition) Resolve(string name, Dictionary<string, Entry[]> index)
    {
        // The whole name first: "Negative Tag" is a tag, not a Negative "Tag".
        if (Find(name, index) is { } whole)
            return (Single(name, whole), null);

        MotelyItemEdition? edition = null;
        var bare = name;
        int space = name.IndexOf(' ');
        if (space > 0)
        {
            foreach (var e in Editions)
            {
                if (name.AsSpan(0, space).Equals(e.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    edition = e;
                    bare = name[(space + 1)..].Trim();
                    break;
                }
            }
        }

        if (edition is { } ed && Find(bare, index) is { } found)
        {
            var entry = Single(bare, found);
            if (entry.Family != Family.Joker)
                throw new FormatException(
                    $"'{name}': only a joker takes an edition, and {entry.Name} is {Article(entry.Family)}; drop `{ed}`.");
            return (entry, ed);
        }

        var suggestions = Closest(Normalize(bare), index);
        throw new FormatException(
            suggestions.Count == 0
                ? $"'{bare}' is not a joker, boss, voucher, tag, tarot, planet or spectral card name."
                : $"'{bare}' is not a known name; did you mean {JoinOr(suggestions)}?");
    }

    /// <summary>
    /// The entries spelled <paramref name="name"/>, ignoring case, spaces and punctuation, and a
    /// leading "The" either way (<c>The Luchador</c> is Luchador, <c>Fool</c> is TheFool); null when
    /// none is.
    /// </summary>
    private static Entry[]? Find(string name, Dictionary<string, Entry[]> index)
    {
        var key = Normalize(name);
        if (key.Length == 0)
            return null;
        if (index.TryGetValue(key, out var found))
            return found;
        var other = key.StartsWith("the", StringComparison.Ordinal) ? key[3..] : "the" + key;
        return index.TryGetValue(other, out found) ? found : null;
    }

    /// <summary>
    /// One name, with an optional edition first word, resolved the way a shorthand line's name is:
    /// the entry and the edition it states. A <see cref="FormatException"/> carries the same
    /// one-line correction (closest names, ambiguity, an edition on a non-joker).
    /// </summary>
    internal static (Entry Entry, MotelyItemEdition? Edition) ResolveName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new FormatException("an empty name: write a joker, boss, voucher, tag, tarot, planet or spectral card name, as in `Perkeo`.");
        return Resolve(trimmed, EngineIndex);
    }

    /// <summary>Every engine name <paramref name="name"/> may mean, across the families; null when none.</summary>
    internal static Entry[]? Lookup(string name) => Find(name, EngineIndex);

    /// <summary>What an entry is ("uncommon joker", "boss" …) and the clause keys that take it, most specific first.</summary>
    internal static (string Kind, string[] Keys) Kind(Entry entry)
    {
        if (entry.Family != Family.Joker)
            return (Article(entry.Family)[(Article(entry.Family).IndexOf(' ') + 1)..], [MappingKey(entry)]);
        return ((MotelyJokerRarity)(entry.Value & MotelyGlobals.JokerRarityMask)) switch
        {
            MotelyJokerRarity.Legendary => ("legendary joker", ["legendaryJoker"]),
            MotelyJokerRarity.Rare => ("rare joker", ["rareJoker", "joker"]),
            MotelyJokerRarity.Uncommon => ("uncommon joker", ["uncommonJoker", "joker"]),
            _ => ("common joker", ["commonJoker", "joker"]),
        };
    }

    private static Entry Single(string name, Entry[] entries)
    {
        if (entries.Length == 1)
            return entries[0];
        var each = entries.Select(e => $"{Article(e.Family)} (`{MappingKey(e)}: {e.Name}`)");
        throw new FormatException(
            $"'{name}' is {string.Join(" and ", each)}: use the mapping form to say which, e.g. `{MappingKey(entries[0])}: {entries[0].Name}`.");
    }

    /// <summary>Up to three names nearest by edit distance, then by those that contain the text.</summary>
    private static List<string> Closest(string key, Dictionary<string, Entry[]> index)
    {
        if (key.Length == 0)
            return [];
        int limit = Math.Max(2, key.Length / 3);
        return index
            .Select(kv => (kv.Value[0].Name, Distance: kv.Key.Contains(key, StringComparison.Ordinal) || key.Contains(kv.Key, StringComparison.Ordinal)
                ? Math.Min(limit, Levenshtein(key, kv.Key))
                : Levenshtein(key, kv.Key)))
            .Where(c => c.Distance <= limit)
            .OrderBy(c => c.Distance)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .Take(3)
            .Select(c => c.Name)
            .ToList();
    }

    /// <summary>Letters and digits only, lower case: "The Eye", "the_eye" and "TheEye" are one name.</summary>
    internal static string Normalize(string name) =>
        new string(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
            prev[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (int j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    // ── Antes ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>item (sep item)*</c>, where an item is <c>N</c> or <c>N-M</c> and a separator is
    /// <c>,</c>, <c>or</c>, <c>and</c>, <c>, or</c> or <c>, and</c>.
    /// </summary>
    internal static int[] ParseAntes(string text)
    {
        var list = text.Trim();
        if (list.Length == 0)
            throw new FormatException($"no ante numbers after `ante`: write {Forms}.");
        var antes = new SortedSet<int>();
        int pos = 0;
        bool expectItem = true;
        bool sawComma = false;

        while (true)
        {
            SkipSpaces(list, ref pos);
            if (pos >= list.Length)
                break;

            if (expectItem)
            {
                int lo = ReadNumber(list, ref pos, text);
                int hi = lo;
                SkipSpaces(list, ref pos);
                if (pos < list.Length && list[pos] is '-' or '–')
                {
                    pos++;
                    SkipSpaces(list, ref pos);
                    hi = ReadNumber(list, ref pos, text);
                    if (hi < lo)
                        throw new FormatException($"ante range '{lo}-{hi}' runs backwards: write {hi}-{lo}.");
                }
                for (int a = lo; a <= hi; a++)
                    antes.Add(a);
                expectItem = false;
                sawComma = false;
                continue;
            }

            if (list[pos] == ',' && !sawComma)
            {
                pos++;
                sawComma = true;
                // A comma alone separates; ", or" / ", and" too.
                SkipSpaces(list, ref pos);
                if (TryWord(list, ref pos, "or") || TryWord(list, ref pos, "and")) { }
                expectItem = true;
                continue;
            }
            if (TryWord(list, ref pos, "or") || TryWord(list, ref pos, "and"))
            {
                expectItem = true;
                continue;
            }
            throw BadAntes(text);
        }

        if (antes.Count == 0 || expectItem)
            throw BadAntes(text);
        return [.. antes];
    }

    private static int ReadNumber(string list, ref int pos, string text)
    {
        int start = pos;
        while (pos < list.Length && char.IsAsciiDigit(list[pos]))
            pos++;
        if (pos == start)
            throw BadAntes(text);
        if (pos - start > 3)
            throw new FormatException($"ante '{list[start..pos]}' is out of range: antes run from 0 to {MaxAnte}.");
        // A number glued to letters ("3rd", "1x") is not an ante.
        if (pos < list.Length && char.IsLetter(list[pos]))
            throw BadAntes(text);
        return int.Parse(list.AsSpan(start, pos - start), NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static bool TryWord(string list, ref int pos, string word)
    {
        if (pos + word.Length > list.Length
            || !list.AsSpan(pos, word.Length).Equals(word, StringComparison.OrdinalIgnoreCase)
            || pos + word.Length < list.Length && char.IsLetter(list[pos + word.Length]))
            return false;
        pos += word.Length;
        return true;
    }

    private static void SkipSpaces(string list, ref int pos)
    {
        while (pos < list.Length && char.IsWhiteSpace(list[pos]))
            pos++;
    }

    private static FormatException BadAntes(string text) =>
        new($"ante list '{text.Trim()}' is not one the shorthand reads: write {Forms}.");

    // ── Words ────────────────────────────────────────────────────────────────────────────

    private readonly record struct Word(int Start, int Length);

    private static List<Word> Words(string line)
    {
        var words = new List<Word>();
        int i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
                i++;
            if (i > start)
                words.Add(new Word(start, i - start));
        }
        return words;
    }

    private static bool Is(string line, Word word, string text) =>
        line.AsSpan(word.Start, word.Length).Equals(text, StringComparison.OrdinalIgnoreCase);

    private static bool IsAnteWord(string line, Word word) => Is(line, word, "ante") || Is(line, word, "antes");

    // ── Clause ───────────────────────────────────────────────────────────────────────────

    /// <summary>The clause, with <paramref name="antes"/> and <paramref name="score"/> where the line states them and its own defaults elsewhere.</summary>
    private static IMotelyClause Build(Entry entry, MotelyItemEdition? edition, int[]? antes, int? score)
    {
        IMotelyClause clause = entry.Family switch
        {
            Family.Joker when IsLegendary((MotelyJoker)entry.Value) =>
                new LegendaryJokerClause { Jokers = [(MotelyJoker)entry.Value], Edition = edition },
            Family.Joker => new JokerClause { Jokers = [(MotelyJoker)entry.Value], Edition = edition },
            Family.Boss => new BossClause { Bosses = [(MotelyBossBlind)entry.Value] },
            Family.Voucher => new VoucherClause { Vouchers = [(MotelyVoucher)entry.Value] },
            Family.Tag => new TagClause { Tags = [(MotelyTag)entry.Value] },
            Family.Tarot => new TarotCardClause { Tarots = [(MotelyTarotCard)entry.Value] },
            Family.Planet => new PlanetCardClause { Planets = [(MotelyPlanetCard)entry.Value] },
            Family.Spectral => new SpectralCardClause { Spectrals = [(MotelySpectralCard)entry.Value] },
            _ => throw new InvalidOperationException($"No shorthand clause for family {entry.Family}."),
        };
        if (antes is not null)
            ((IAnteScopedClause)clause).Antes = antes;
        if (score is { } points)
            clause.Score = points;
        return clause;
    }

    private static bool IsLegendary(MotelyJoker joker) =>
        (MotelyJokerRarity)((int)joker & MotelyGlobals.JokerRarityMask) == MotelyJokerRarity.Legendary;

    /// <summary>The mapping key that names an entry, for messages that point at the mapping form.</summary>
    private static string MappingKey(Entry entry) =>
        entry.Family switch
        {
            Family.Joker when IsLegendary((MotelyJoker)entry.Value) => "legendaryJoker",
            Family.Joker => "joker",
            Family.Boss => "boss",
            Family.Voucher => "voucher",
            Family.Tag => "tag",
            Family.Tarot => "tarotCard",
            Family.Planet => "planetCard",
            _ => "spectralCard",
        };

    private static string Article(Family family) =>
        family switch
        {
            Family.Joker => "a joker",
            Family.Boss => "a boss",
            Family.Voucher => "a voucher",
            Family.Tag => "a tag",
            Family.Tarot => "a tarot card",
            Family.Planet => "a planet card",
            _ => "a spectral card",
        };

    private static string JoinOr(List<string> names) =>
        names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
}
