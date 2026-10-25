using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Motely;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>How urgently the editor paints a <see cref="JamlDiagnostic"/>.</summary>
public enum JamlDiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>
/// One editor marker: a 1-based <paramref name="Line"/> (as the engine reports it) and a 0-based,
/// end-exclusive column range on that line.
/// </summary>
public sealed record JamlDiagnostic(
    int Line,
    int StartColumn,
    int EndColumn,
    string Message,
    JamlDiagnosticSeverity Severity
);

/// <summary>What a completion inserts and how the list describes it.</summary>
public enum JamlCompletionKind
{
    RootKey,
    ClauseKey,
    Property,
    Value,
}

/// <summary>One completion candidate: the text it inserts, its kind, and a one-line description.</summary>
public sealed record JamlCompletion(string Text, JamlCompletionKind Kind, string Detail);

/// <summary>
/// The JAML language brain behind the filter editor — completion, hover, diagnostics and the
/// folding outline — computed straight off the engine: <see cref="FilterVocabulary"/> (reflected
/// from the very binding table <see cref="JamlConfigLoader"/> reads), the loader itself, and
/// <see cref="FilterCheck"/>. Nothing here knows a clause key or an enum name by heart, so an
/// engine that grows a clause grows the editor with it. Avalonia-free so tests can call it.
/// Mirrors the engine's own language server (Motely.Lsp), which is an executable and not
/// referenceable from here.
/// </summary>
public static class JamlLanguage
{
    private static readonly string[] SectionKeys = ["must", "should", "mustNot"];

    // ── Completion ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Candidates for the caret at the end of <paramref name="textBeforeCursor"/>, filtered by the
    /// token typed so far (prefix matches first, then substring matches).
    /// </summary>
    public static IReadOnlyList<JamlCompletion> Complete(string textBeforeCursor)
    {
        var lines = SplitLines(textBeforeCursor ?? "");
        var lineIndex = lines.Length - 1;
        var prefix = lines[lineIndex];

        var indent = CountIndent(prefix);
        var body = prefix.TrimStart();
        var isListItem = body.StartsWith("- ", StringComparison.Ordinal) || body == "-";
        if (isListItem)
            body = body.TrimStart('-').TrimStart();

        var colon = KeyColon(body);
        if (colon >= 0)
        {
            var key = body[..colon].Trim();
            var valueText = body[(colon + 1)..];
            return CompleteValue(key, LastValueToken(valueText));
        }

        return CompleteKey(lines, lineIndex, indent, isListItem, body);
    }

    /// <summary>
    /// Offset in <paramref name="text"/> where the token ending at <paramref name="caret"/> starts —
    /// the range a picked completion replaces. Only identifier characters count, so the
    /// <c>- </c> of a list item and the <c>: </c> of a key stay put.
    /// </summary>
    public static int TokenStart(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var start = caret;
        while (start > 0 && IsWordChar(text[start - 1]))
            start--;
        return start;
    }

    /// <summary>
    /// Whether typing <paramref name="typed"/> (just inserted, so it ends
    /// <paramref name="textBeforeCursor"/>) should open completion on its own: after the space
    /// of <c>- </c> or <c>key: </c>, after <c>[</c> or <c>, </c> in a value list, and on the first
    /// letter of a word.
    /// </summary>
    public static bool ShouldAutoTrigger(string textBeforeCursor, string typed)
    {
        if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(textBeforeCursor))
            return false;
        var line = SplitLines(textBeforeCursor)[^1];
        if (line.TrimStart().StartsWith('#'))
            return false;

        if (typed == " ")
            return line.EndsWith("- ", StringComparison.Ordinal) && line.TrimStart() == "- "
                || line.EndsWith(": ", StringComparison.Ordinal)
                || line.EndsWith(", ", StringComparison.Ordinal);
        if (typed == "[")
            return true;
        if (typed.Length == 1 && char.IsLetter(typed[0]))
        {
            var start = TokenStart(textBeforeCursor, textBeforeCursor.Length);
            return textBeforeCursor.Length - start == 1;
        }
        return false;
    }

    private static IReadOnlyList<JamlCompletion> CompleteValue(string key, string typed)
    {
        if (FilterVocabulary.IsDiscriminator(key) && FilterVocabulary.ValueEnumType(key) is { } valueEnum)
            return Filter(Enum.GetNames(valueEnum), typed, JamlCompletionKind.Value, KindOf(valueEnum));

        if (FilterVocabulary.EnumTypeForKey(key) is { } keyEnum)
            return Filter(Enum.GetNames(keyEnum), typed, JamlCompletionKind.Value, KindOf(keyEnum));

        return [];
    }

    private static IReadOnlyList<JamlCompletion> CompleteKey(
        string[] lines,
        int line,
        int indent,
        bool isListItem,
        string typed
    )
    {
        if (indent == 0 && !isListItem)
            return Filter(FilterVocabulary.RootKeys, typed, JamlCompletionKind.RootKey, "filter key");

        var context = ContextAt(lines, line);

        if (isListItem && context.InClauseList)
            return Filter(SingularDiscriminators(), typed, JamlCompletionKind.ClauseKey, "clause");

        if (context.BlockKey is "sources" && context.Discriminator is { } sourceDisc)
            return Filter(
                FilterVocabulary.SourceKeysFor(sourceDisc),
                typed,
                JamlCompletionKind.Property,
                $"{sourceDisc} source"
            );

        if (context.BlockKey is "with")
            return Filter(FilterVocabulary.WithBlockKeys, typed, JamlCompletionKind.Property, "with-block key");

        if (context.Discriminator is { } disc)
            return Filter(
                FilterVocabulary.ClauseKeysFor(disc).Where(k => !FilterVocabulary.IsDiscriminator(k)),
                typed,
                JamlCompletionKind.Property,
                $"{disc} clause key"
            );

        return [];
    }

    /// <summary>
    /// Clause keys as authors write them: the engine accepts plural aliases (<c>jokers</c>,
    /// <c>bosses</c>) too, but offering both doubles the list, so a plural that only repeats a
    /// singular of the same clause type is left out.
    /// </summary>
    public static IReadOnlyList<string> SingularDiscriminators()
    {
        var table = FilterVocabulary.DiscriminatorTable;
        return
        [
            .. table
                .Where(info => !table.Any(other =>
                    other.ClauseType == info.ClauseType
                    && (info.Key.Equals(other.Key + "s", StringComparison.OrdinalIgnoreCase)
                        || info.Key.Equals(other.Key + "es", StringComparison.OrdinalIgnoreCase))))
                .Select(info => info.Key),
        ];
    }

    private static IReadOnlyList<JamlCompletion> Filter(
        IEnumerable<string> names,
        string typed,
        JamlCompletionKind kind,
        string detail
    )
    {
        var starts = new List<JamlCompletion>();
        var contains = new List<JamlCompletion>();
        foreach (var name in names.Distinct(StringComparer.Ordinal))
        {
            if (typed.Length == 0 || name.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                starts.Add(new JamlCompletion(name, kind, detail));
            else if (name.Contains(typed, StringComparison.OrdinalIgnoreCase))
                contains.Add(new JamlCompletion(name, kind, detail));
        }
        starts.AddRange(contains);
        return starts;
    }

    /// <summary>The token being typed in a value: <c>The</c> in <c>[TheHook, The</c>.</summary>
    private static string LastValueToken(string valueText)
    {
        var end = valueText.Length;
        var start = end;
        while (start > 0 && IsWordChar(valueText[start - 1]))
            start--;
        return valueText[start..end];
    }

    /// <summary>The colon that ends a mapping key (<c>key:</c> followed by space or end), or -1.</summary>
    private static int KeyColon(string body)
    {
        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];
            if (c is '"' or '\'' or '[' or '{' or '#')
                return -1;
            if (c == ':' && (i + 1 == body.Length || body[i + 1] == ' '))
                return i;
        }
        return -1;
    }

    // ── Hover ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plain-text description of the word at 0-based <paramref name="line"/> /
    /// <paramref name="column"/>, or null when it is not JAML vocabulary.
    /// </summary>
    public static string? Hover(string text, int line, int column)
    {
        var lines = SplitLines(text ?? "");
        if (line < 0 || line >= lines.Length)
            return null;
        var word = WordAt(lines[line], column);
        if (word.Length == 0)
            return null;

        var body = lines[line].TrimStart();
        if (body.StartsWith('#'))
            return null;

        if (FilterVocabulary.IsDiscriminator(word))
        {
            var description = $"{word} — JAML clause";
            if (FilterVocabulary.ValueEnumType(word) is { } valueEnum)
                description += $"\nValue: {KindOf(valueEnum)} ({Enum.GetNames(valueEnum).Length} names, {valueEnum.Name})";
            if (FilterVocabulary.RollsAreInlineFor(word))
                description += "\nEvent clause: the value is the roll list (e.g. luckyMoney: [1, 2]).";
            var keys = FilterVocabulary.ClauseKeysFor(word).Where(k => !FilterVocabulary.IsDiscriminator(k)).ToArray();
            if (keys.Length > 0)
                description += $"\nKeys: {string.Join(", ", keys)}";
            return description;
        }

        if (indentIsRoot(lines[line]) && FilterVocabulary.IsRootKey(word) && IsKeyAt(lines[line], word))
            return $"{word} — filter key" + RootKeyNote(word);

        var context = ContextAt(lines, line);
        if (context.Discriminator is { } disc
            && IsKeyAt(lines[line], word)
            && FilterVocabulary.ClauseKeysFor(disc).Any(k => k.Equals(word, StringComparison.OrdinalIgnoreCase)))
            return $"{word} — key of the {disc} clause" + ClauseKeyNote(word);

        // A value: prefer the enum of the key it is the value of (boss: TheEye → boss blind).
        var lineKey = LineKey(lines[line]);
        var keyEnum = lineKey is null
            ? null
            : FilterVocabulary.IsDiscriminator(lineKey)
                ? FilterVocabulary.ValueEnumType(lineKey)
                : FilterVocabulary.EnumTypeForKey(lineKey);
        if (keyEnum is not null
            && Enum.GetNames(keyEnum).FirstOrDefault(n => n.Equals(word, StringComparison.OrdinalIgnoreCase)) is { } named)
            return $"{named} — {KindOf(keyEnum)} ({keyEnum.Name})";

        foreach (var (enumType, kind) in FilterVocabulary.ValueEnumKinds)
            if (Enum.GetNames(enumType).FirstOrDefault(n => n.Equals(word, StringComparison.OrdinalIgnoreCase)) is { } exact)
                return $"{exact} — {kind} ({enumType.Name})";

        return null;

        static bool indentIsRoot(string raw) => raw.Length > 0 && raw[0] is not ' ' and not '\t' and not '-';
    }

    private static string RootKeyNote(string key) =>
        key switch
        {
            "must" => "\nEvery clause here must hit, or the seed is dropped.",
            "should" => "\nClauses here add to the seed's score.",
            "mustNot" => "\nAny clause that hits here drops the seed.",
            "deck" or "stake" => $"\nOne of: {string.Join(", ", Enum.GetNames(FilterVocabulary.EnumTypeForKey(key)!))}",
            "cutoff" => "\nMinimum score a seed needs to be reported.",
            _ => "",
        };

    private static string ClauseKeyNote(string key) =>
        key.ToLowerInvariant() switch
        {
            "antes" => "\nAntes the clause looks in (1-based), e.g. [1, 2, 3].",
            "score" => "\nPoints a should clause adds per hit.",
            "min" => "\nHits needed for the clause to count.",
            "max" => "\nUpper bound on counted hits.",
            "label" => "\nColumn name for this clause in results.",
            "sources" => "\nWhere to look: shopItems, packSlots, tags, …",
            _ when FilterVocabulary.EnumTypeForKey(key) is { } t => $"\nValue: {KindOf(t)} ({t.Name})",
            _ => "",
        };

    private static bool IsKeyAt(string rawLine, string word) =>
        LineKey(rawLine)?.Equals(word, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>The mapping key a line declares (<c>boss</c> in <c>  - boss: TheEye</c>), or null.</summary>
    private static string? LineKey(string rawLine)
    {
        var body = rawLine.TrimStart();
        if (body.StartsWith("- ", StringComparison.Ordinal))
            body = body[2..].TrimStart();
        var colon = KeyColon(body);
        return colon > 0 ? body[..colon].Trim() : null;
    }

    private static string WordAt(string lineText, int column)
    {
        if (lineText.Length == 0 || column < 0 || column > lineText.Length)
            return "";
        var at = Math.Min(column, lineText.Length - 1);
        if (!IsWordChar(lineText[at]))
        {
            if (at > 0 && IsWordChar(lineText[at - 1]) && column == at)
                at--;
            else
                return "";
        }
        var start = at;
        while (start > 0 && IsWordChar(lineText[start - 1]))
            start--;
        var end = at;
        while (end + 1 < lineText.Length && IsWordChar(lineText[end + 1]))
            end++;
        return lineText[start..(end + 1)];
    }

    private static string KindOf(Type enumType) =>
        FilterVocabulary.ValueEnumKinds.FirstOrDefault(k => k.EnumType == enumType).Kind ?? enumType.Name;

    // ── Diagnostics ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything the engine has to say about <paramref name="text"/>: the loader's error (placed
    /// on the token it names when it names one), else the search plan's error, else the
    /// <see cref="FilterCheck.Warnings"/> each placed on the clause line it is about.
    /// </summary>
    public static IReadOnlyList<JamlDiagnostic> Diagnose(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        var lines = SplitLines(text);

        JamlConfig config;
        try
        {
            config = JamlConfigLoader.FromJaml(text);
        }
        catch (Exception ex)
        {
            return [LoadError(ex, lines)];
        }

        try
        {
            _ = MotelySearchBuilder.CreatePlan(config);
        }
        catch (InvalidOperationException ex)
        {
            return [OnFirstContentLine(lines, ex.Message, JamlDiagnosticSeverity.Error)];
        }

        return [.. FilterCheck.Warnings(config).Select(w => PlaceWarning(w, lines))];
    }

    private static JamlDiagnostic LoadError(Exception ex, string[] lines)
    {
        JamlFilterException? located = null;
        for (Exception? walk = ex; walk is not null; walk = walk.InnerException)
            if (walk is JamlFilterException jfe)
            {
                located = jfe;
                break;
            }
        if (located is null)
            return OnFirstContentLine(lines, ex.Message, JamlDiagnosticSeverity.Error);

        var lineIndex = Math.Clamp(located.Line - 1, 0, lines.Length - 1);

        // "Property 'antez' not found on type …" — squiggle the rejected key.
        if (Quoted(located.Message, "Property '") is { } key
            && FindToken(lines, lineIndex, key, keyOnly: true) is { } keySpan)
            return Error(keySpan.Line, keySpan.Start, keySpan.Start + key.Length);

        // "Requested value 'TheEyez' was not found." — squiggle the unknown name.
        if (Quoted(located.Message, "value '") is { } name
            && FindToken(lines, lineIndex, name, keyOnly: false) is { } nameSpan)
            return Error(nameSpan.Line, nameSpan.Start, nameSpan.Start + name.Length);

        // Otherwise: the region the scanner was inside, from the mark to the end of its line.
        var raw = lines[lineIndex];
        var start = Math.Clamp(located.Column, 0, raw.Length);
        if (start >= raw.TrimEnd().Length)
            start = CountIndent(raw);
        var end = Math.Max(raw.TrimEnd().Length, start + 1);
        return Error(lineIndex, start, end);

        JamlDiagnostic Error(int zeroLine, int s, int e) =>
            new(zeroLine + 1, s, e, located.Message, JamlDiagnosticSeverity.Error);
    }

    /// <summary>
    /// <c>must boss TheEye: antes [1] are all impossible …</c> → the line in the <c>must:</c>
    /// section that names TheEye (or the section header when it cannot be found).
    /// </summary>
    private static JamlDiagnostic PlaceWarning(string warning, string[] lines)
    {
        var match = Regex.Match(warning, @"^(?<section>must|should|mustNot) boss (?<name>\w+):");
        if (match.Success)
        {
            var (from, to) = SectionRange(lines, match.Groups["section"].Value);
            if (from >= 0)
            {
                var name = match.Groups["name"].Value;
                for (var i = from + 1; i < to; i++)
                {
                    var at = WordIndex(lines[i], name);
                    if (at >= 0)
                        return new JamlDiagnostic(i + 1, at, at + name.Length, warning, JamlDiagnosticSeverity.Warning);
                }
                return new JamlDiagnostic(from + 1, 0, Math.Max(1, lines[from].TrimEnd().Length), warning, JamlDiagnosticSeverity.Warning);
            }
        }
        return OnFirstContentLine(lines, warning, JamlDiagnosticSeverity.Warning);
    }

    /// <summary>Lines [header, next root key) of a root section such as <c>must:</c>; (-1, -1) if absent.</summary>
    private static (int From, int To) SectionRange(string[] lines, string section)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (!Regex.IsMatch(lines[i], $@"^{section}\s*:"))
                continue;
            var end = i + 1;
            while (end < lines.Length && (lines[end].Length == 0 || lines[end][0] is ' ' or '\t' or '-' or '#'))
                end++;
            return (i, end);
        }
        return (-1, -1);
    }

    private static JamlDiagnostic OnFirstContentLine(string[] lines, string message, JamlDiagnosticSeverity severity)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd();
            if (trimmed.Trim().Length > 0)
                return new JamlDiagnostic(i + 1, CountIndent(trimmed), trimmed.Length, message, severity);
        }
        return new JamlDiagnostic(1, 0, 1, message, severity);
    }

    /// <summary>The token's line and column: the reported line first, then nearest lines outward.</summary>
    private static (int Line, int Start)? FindToken(string[] lines, int around, string token, bool keyOnly)
    {
        for (var distance = 0; distance < lines.Length; distance++)
        {
            foreach (var i in new[] { around + distance, around - distance }.Distinct())
            {
                if (i < 0 || i >= lines.Length)
                    continue;
                var at = WordIndex(lines[i], token);
                while (at >= 0)
                {
                    if (!keyOnly || lines[i][(at + token.Length)..].TrimStart().StartsWith(':'))
                        return (i, at);
                    at = WordIndex(lines[i], token, at + 1);
                }
            }
        }
        return null;
    }

    private static int WordIndex(string line, string word, int from = 0)
    {
        var at = line.IndexOf(word, from, StringComparison.Ordinal);
        while (at >= 0)
        {
            var beforeOk = at == 0 || !IsWordChar(line[at - 1]);
            var afterOk = at + word.Length >= line.Length || !IsWordChar(line[at + word.Length]);
            if (beforeOk && afterOk)
                return at;
            at = line.IndexOf(word, at + 1, StringComparison.Ordinal);
        }
        return -1;
    }

    private static string? Quoted(string message, string marker)
    {
        var start = message.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;
        start += marker.Length;
        var end = message.IndexOf('\'', start);
        return end > start ? message[start..end] : null;
    }

    // ── Folding ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Foldable regions of a JAML document as 0-based (first line, last line) pairs: every key or
    /// list item whose following lines are indented deeper (a JAML block), plus any bracket
    /// (<c>[</c>/<c>{</c>) left open across lines (flow lists, and pretty-printed JSON, which is JAML
    /// too). Blank and comment lines inside a block never end it; trailing ones are not folded in.
    /// </summary>
    public static IReadOnlyList<(int StartLine, int EndLine)> FoldRegions(string text)
    {
        var lines = SplitLines(text ?? "");
        var regions = new HashSet<(int, int)>();

        // Indentation blocks. A "- " item's content sits at indent + 2, so its children are
        // measured against that.
        var open = new Stack<(int Line, int Level)>();
        var lastContent = -1;
        for (var i = 0; i <= lines.Length; i++)
        {
            var isEnd = i == lines.Length;
            if (!isEnd && IsBlankOrComment(lines[i]))
                continue;
            var level = isEnd ? -1 : Level(lines[i]);
            while (open.Count > 0 && level <= open.Peek().Level)
            {
                var (startLine, _) = open.Pop();
                if (lastContent > startLine)
                    regions.Add((startLine, lastContent));
            }
            if (isEnd)
                break;
            open.Push((i, level));
            lastContent = i;
        }

        // Brackets left open across lines.
        var brackets = new Stack<(char Char, int Line)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var inString = '\0';
            foreach (var c in lines[i])
            {
                if (inString != '\0')
                {
                    if (c == inString)
                        inString = '\0';
                    continue;
                }
                if (c is '"' or '\'')
                    inString = c;
                else if (c == '#' && brackets.Count == 0)
                    break;
                else if (c is '[' or '{')
                    brackets.Push((c, i));
                else if (c is ']' or '}' && brackets.Count > 0)
                {
                    var (openChar, line) = brackets.Pop();
                    if ((openChar == '[') == (c == ']') && i > line)
                        regions.Add((line, i));
                }
            }
        }

        return [.. regions.OrderBy(r => r.Item1).ThenByDescending(r => r.Item2)];
    }

    /// <summary>
    /// Nesting level of a line: twice its indent, plus one for a <c>- </c> item, so items nest
    /// under a key at their own indent (<c>must:</c> then <c>- joker: X</c>) and an item's
    /// further keys nest under the item.
    /// </summary>
    private static int Level(string line) =>
        CountIndent(line) * 2 + (line.TrimStart().StartsWith('-') ? 1 : 0);

    private static bool IsBlankOrComment(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.Length == 0 || trimmed.StartsWith('#');
    }

    // ── Shared ──────────────────────────────────────────────────────────────────────────

    private readonly record struct LineContext(string? Discriminator, string? BlockKey, bool InClauseList);

    /// <summary>
    /// Walk upward from <paramref name="line"/> collecting the enclosing shape: the clause's
    /// discriminator, the nearest block key (<c>sources:</c>, <c>with:</c>, <c>clauses:</c>), and
    /// whether we are inside a must/should/mustNot list (or an and/or <c>clauses:</c> list).
    /// </summary>
    private static LineContext ContextAt(string[] lines, int line)
    {
        string? discriminator = null;
        string? blockKey = null;
        var inClauseList = false;

        var reference = int.MaxValue;
        if (line >= 0 && line < lines.Length && lines[line].Trim().Length > 0)
            reference = EffectiveIndent(lines[line]);

        for (var i = Math.Min(line, lines.Length - 1); i >= 0; i--)
        {
            var raw = lines[i];
            if (IsBlankOrComment(raw))
                continue;
            var lineIndent = EffectiveIndent(raw);
            if (i != line && lineIndent > reference)
                continue;
            if (i != line)
                reference = lineIndent;

            var body = raw.TrimStart();
            if (body.StartsWith("- ", StringComparison.Ordinal))
                body = body[2..].TrimStart();
            var colon = KeyColon(body);
            if (colon <= 0)
                continue;
            var key = body[..colon].Trim();

            if (discriminator is null && FilterVocabulary.IsDiscriminator(key))
                discriminator = key;
            else if (blockKey is null && discriminator is null && key is "sources" or "with" or "clauses")
                blockKey = key;

            if (SectionKeys.Contains(key) && CountIndent(raw) == 0)
            {
                inClauseList = true;
                break;
            }
        }

        return new LineContext(discriminator, blockKey, inClauseList);
    }

    private static int EffectiveIndent(string line)
    {
        var indent = CountIndent(line);
        return line.TrimStart().StartsWith('-') ? indent + 2 : indent;
    }

    private static int CountIndent(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
            count++;
        return count;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Split('\n');
}
