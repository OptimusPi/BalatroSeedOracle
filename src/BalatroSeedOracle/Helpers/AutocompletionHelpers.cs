using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// One entry in the editor's completion list. Picking it replaces the token being typed
/// (identifier characters only — the completion window's segment may include a leading
/// <c>-</c> or quote, which is kept) with <see cref="Text"/>, plus <see cref="Suffix"/>.
/// </summary>
public sealed class JamlCompletionData : ICompletionData
{
    public JamlCompletionData(JamlCompletion completion, string suffix = "")
    {
        Completion = completion;
        Suffix = suffix;
    }

    public JamlCompletion Completion { get; }

    /// <summary>Text appended after the name, e.g. <c>": "</c> after a clause key.</summary>
    public string Suffix { get; }

    public IImage? Image => null;

    public string Text => Completion.Text;

    public object Content => Completion.Text;

    public object Description => $"{Completion.Text} — {Completion.Detail}";

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var document = textArea.Document;
        var end = Math.Min(completionSegment.EndOffset, document.TextLength);
        // Replace the identifier being typed, not the whole segment: the window may have opened
        // on the "-" of a list item or a quote, which stay.
        var start = JamlLanguage.TokenStart(document.GetText(0, end), end);
        var nextChar = end < document.TextLength ? document.GetCharAt(end) : '\n';
        var insert = Text + (nextChar is ':' or '"' ? "" : Suffix);

        // "boss:" + pick → "boss: TheEye", not "boss:TheEye".
        if (start > 0 && document.GetCharAt(start - 1) is ':' or '-' or ',')
            insert = " " + insert;

        document.Replace(start, end - start, insert);

        // A picked key leaves the caret at "key: " — go straight on to its values (boss names
        // after "boss: "), once this window has closed.
        if (insert.EndsWith(": ", StringComparison.Ordinal))
            Dispatcher.UIThread.Post(() => ShowFollowUp(textArea), DispatcherPriority.Background);
    }

    private static void ShowFollowUp(TextArea textArea)
    {
        var document = textArea.Document;
        if (document is null)
            return;
        var caret = textArea.Caret.Offset;
        var items = JamlAutocompletionHelper.GetCompletionsForContext(document.GetText(0, caret));
        if (items.Count == 0)
            return;
        var window = new CompletionWindow(textArea) { StartOffset = caret, EndOffset = caret };
        foreach (var item in items)
            window.CompletionList.CompletionData.Add(item);
        window.Show();
    }
}

/// <summary>
/// Context-aware completions for the JAML filter editor, straight from the engine's vocabulary
/// (<see cref="FilterVocabulary"/>): root keys at column zero, clause keys after <c>- </c> in a
/// must/should/mustNot list, a clause's own keys under it, and the enum names a key takes after
/// <c>key: </c> (boss names after <c>boss: </c>, decks after <c>deck: </c>, …).
/// </summary>
public static class JamlAutocompletionHelper
{
    public static IReadOnlyList<ICompletionData> GetCompletionsForContext(string textBeforeCursor) =>
        [.. JamlLanguage.Complete(textBeforeCursor).Select(c => new JamlCompletionData(c, SuffixFor(c)))];

    /// <summary>Keys complete as <c>key: </c> so the value can be typed straight away.</summary>
    private static string SuffixFor(JamlCompletion completion) =>
        completion.Kind == JamlCompletionKind.Value ? "" : ": ";
}

/// <summary>
/// Context-aware completions for the JSON filter editor. JSON is JAML, so the vocabulary is the
/// engine's again: after an opening quote in key position, root keys at the top level and clause
/// keys inside a clause object; after <c>"key": "</c>, the names that key takes.
/// </summary>
public static class JsonAutocompletionHelper
{
    public static IReadOnlyList<ICompletionData> GetCompletionsForContext(string textBeforeCursor)
    {
        textBeforeCursor ??= "";

        // Value position: "boss": "The   or   "jokers": ["Blue
        var value = Regex.Match(textBeforeCursor, "\"(?<key>\\w+)\"\\s*:\\s*(\\[\\s*(\"\\w*\"\\s*,\\s*)*)?\"(?<typed>\\w*)$");
        if (value.Success)
        {
            var key = value.Groups["key"].Value;
            var enumType = FilterVocabulary.IsDiscriminator(key)
                ? FilterVocabulary.ValueEnumType(key)
                : FilterVocabulary.EnumTypeForKey(key);
            if (enumType is null)
                return [];
            return Wrap(Enum.GetNames(enumType), value.Groups["typed"].Value, JamlCompletionKind.Value, enumType.Name);
        }

        // Key position: an opening quote right after "{" or "," (whitespace allowed).
        var key2 = Regex.Match(textBeforeCursor, "[{,]\\s*\"(?<typed>\\w*)$");
        if (!key2.Success)
            return [];
        var typed = key2.Groups["typed"].Value;
        var depth = ObjectDepth(textBeforeCursor);
        if (depth <= 1)
            return Wrap(FilterVocabulary.RootKeys, typed, JamlCompletionKind.RootKey, "filter key");

        // Inside a clause object: the clause keys, plus the keys of a clause already named in it.
        var objectStart = textBeforeCursor.LastIndexOf('{');
        var named = Regex.Matches(textBeforeCursor[Math.Max(0, objectStart)..], "\"(?<k>\\w+)\"\\s*:")
            .Select(m => m.Groups["k"].Value)
            .FirstOrDefault(FilterVocabulary.IsDiscriminator);
        var names = named is null
            ? JamlLanguage.SingularDiscriminators()
            : FilterVocabulary.ClauseKeysFor(named).Where(k => !FilterVocabulary.IsDiscriminator(k)).ToList();
        return Wrap(names, typed, named is null ? JamlCompletionKind.ClauseKey : JamlCompletionKind.Property, named ?? "clause");
    }

    private static IReadOnlyList<ICompletionData> Wrap(
        IEnumerable<string> names,
        string typed,
        JamlCompletionKind kind,
        string detail
    ) =>
        [
            .. names
                .Where(n => typed.Length == 0 || n.Contains(typed, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.StartsWith(typed, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .Select(n => new JamlCompletionData(new JamlCompletion(n, kind, detail))),
        ];

    /// <summary>How many objects are open at the end of <paramref name="text"/> (strings skipped).</summary>
    private static int ObjectDepth(string text)
    {
        var depth = 0;
        var inString = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\')
                    i++;
                else if (c == '"')
                    inString = false;
                continue;
            }
            if (c == '"')
                inString = true;
            else if (c == '{')
                depth++;
            else if (c == '}')
                depth--;
        }
        return depth;
    }
}
