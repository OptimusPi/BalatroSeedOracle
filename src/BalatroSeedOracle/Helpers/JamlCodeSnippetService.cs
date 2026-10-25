using System;
using System.Linq;
using AvaloniaEdit;
using Motely.Schema;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Tab-expandable snippets in the JAML editor. Type a word and press Tab:
/// <list type="bullet">
/// <item><c>filter</c> at column 0 → a whole filter skeleton (name, deck, stake, must, should);</item>
/// <item><c>must</c> / <c>should</c> / <c>mustNot</c> at column 0 → the section with its first item;</item>
/// <item>any clause key (<c>joker</c>, <c>boss</c>, <c>or</c>, …) as a list item → the clause with
/// the keys it takes (<c>antes:</c> for ante-scoped clauses, <c>clauses:</c> for and/or).</item>
/// </list>
/// Clause keys come from the engine (<see cref="FilterVocabulary"/>), so new clauses expand too.
/// Anything else returns false and Tab indents as usual.
/// </summary>
public class JamlCodeSnippetService
{
    private readonly TextEditor _editor;

    public JamlCodeSnippetService(TextEditor editor)
    {
        _editor = editor;
    }

    /// <summary>A snippet expansion: replace [<see cref="Start"/>, caret) with <see cref="Text"/>, caret to <see cref="Caret"/>.</summary>
    public readonly record struct Expansion(int Start, string Text, int Caret);

    /// <summary>Try to expand a snippet at the caret; false = let the editor handle the key.</summary>
    public bool TryExpandSnippet()
    {
        var document = _editor.Document;
        if (document is null)
            return false;
        var caret = _editor.CaretOffset;
        var line = document.GetLineByOffset(caret);
        var column = caret - line.Offset;
        if (Expand(document.GetText(line.Offset, line.Length), column) is not { } expansion)
            return false;

        document.Replace(line.Offset + expansion.Start, column - expansion.Start, expansion.Text);
        _editor.CaretOffset = line.Offset + expansion.Start + expansion.Caret;
        return true;
    }

    /// <summary>
    /// The expansion for the word ending at <paramref name="column"/> of <paramref name="lineText"/>,
    /// or null when that word is not a snippet in that position. Columns are 0-based and relative
    /// to the line.
    /// </summary>
    public static Expansion? Expand(string lineText, int column)
    {
        column = Math.Clamp(column, 0, lineText.Length);
        var before = lineText[..column];
        var start = JamlLanguage.TokenStart(before, column);
        var word = before[start..];
        if (word.Length == 0)
            return null;
        var lead = before[..start];

        // Root position: nothing before the word.
        if (lead.Length == 0)
        {
            switch (word)
            {
                case "filter":
                {
                    const string Head = "name: ";
                    var text = Head + "\ndescription: \nauthor: \ndeck: Red\nstake: White\nmust:\n  - \nshould:\n  - ";
                    return new Expansion(start, text, Head.Length);
                }
                case "must" or "should" or "mustNot":
                {
                    var text = $"{word}:\n  - ";
                    return new Expansion(start, text, text.Length);
                }
            }
            return null;
        }

        // List-item position: "  - joker" → the clause.
        var trimmedLead = lead.TrimEnd();
        if (!trimmedLead.EndsWith('-') || trimmedLead.TrimStart() != "-" || !FilterVocabulary.IsDiscriminator(word))
            return null;

        var childIndent = new string(' ', lead.Length);
        var keys = FilterVocabulary.ClauseKeysFor(word);
        string snippet;
        int caret;
        if (word is "and" or "or")
        {
            snippet = $"{word}:\n{childIndent}  clauses:\n{childIndent}    - ";
            caret = snippet.Length;
        }
        else
        {
            snippet = $"{word}: ";
            caret = snippet.Length;
            if (keys.Contains("antes", StringComparer.OrdinalIgnoreCase))
                snippet += $"\n{childIndent}antes: [1, 2, 3]";
        }
        return new Expansion(start, snippet, caret);
    }
}
