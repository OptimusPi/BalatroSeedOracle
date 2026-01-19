using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Service for code snippets (Tab expansion) in JAML editor
    /// </summary>
    public class JamlCodeSnippetService
    {
        private readonly TextEditor _editor;
        private readonly Dictionary<string, Snippet> _snippets = new();

        public JamlCodeSnippetService(TextEditor editor)
        {
            _editor = editor;
            InitializeSnippets();
        }

        private void InitializeSnippets()
        {
            // Joker snippet
            _snippets["joker"] = new Snippet
            {
                Trigger = "joker",
                Content = "joker: Blueprint\nantes: [1, 2]\nscore: 10",
                Description = "Joker clause template",
            };

            // Anchor definition snippet
            _snippets["anchor"] = new Snippet
            {
                Trigger = "anchor",
                Content = "name: &name value",
                Description = "Anchor definition",
            };

            // And clause snippet
            _snippets["and"] = new Snippet
            {
                Trigger = "and",
                Content =
                    "And:\n  Antes: [1, 2]\n  Mode: Max\n  Score: 100\n  clauses:\n    - joker: Blueprint",
                Description = "And clause template",
            };

            // Or clause snippet
            _snippets["or"] = new Snippet
            {
                Trigger = "or",
                Content = "Or:\n  - joker: Blueprint\n    antes: [1, 2]",
                Description = "Or clause template",
            };

            // Cluster pattern snippet
            _snippets["cluster"] = new Snippet
            {
                Trigger = "cluster",
                Content =
                    "joker_cluster: &joker_cluster\n  - joker: *desired_joker\n    ShopSlots: [2,3,4]\n    score: *score_per_joker\n  - joker: *desired_joker\n    ShopSlots: [4,5,6]\n    score: *score_per_joker",
                Description = "Joker cluster pattern with anchors",
            };

            // Negative tag snippet
            _snippets["neg"] = new Snippet
            {
                Trigger = "neg",
                Content = "smallblindtag: NegativeTag",
                Description = "Negative tag clause",
            };
        }

        public bool TryExpandSnippet()
        {
            if (_editor.Document == null || _editor.TextArea == null)
                return false;

            var offset = _editor.CaretOffset;
            var line = _editor.Document.GetLineByOffset(offset);
            var lineText = _editor.Document.GetText(line.Offset, line.Length);
            var column = offset - line.Offset;

            // Get word before cursor
            var wordStart = column;
            while (wordStart > 0 && char.IsLetterOrDigit(lineText[wordStart - 1]))
                wordStart--;

            var word = lineText.Substring(wordStart, column - wordStart).ToLower();

            if (_snippets.TryGetValue(word, out var snippet))
            {
                // Replace the word with snippet content
                var replaceStart = line.Offset + wordStart;
                var replaceLength = column - wordStart;

                // Calculate indentation
                var indent = GetIndentation(lineText);
                var indentedContent = IndentSnippet(snippet.Content, indent);

                _editor.Document.Replace(replaceStart, replaceLength, indentedContent);

                // Move cursor to end of snippet
                var newOffset = replaceStart + indentedContent.Length;
                _editor.CaretOffset = newOffset;
                _editor.TextArea.Caret.BringCaretToView();

                return true;
            }

            return false;
        }

        private string GetIndentation(string lineText)
        {
            var indent = "";
            foreach (var c in lineText)
            {
                if (c == ' ')
                    indent += " ";
                else if (c == '\t')
                    indent += "    ";
                else
                    break;
            }
            return indent;
        }

        private string IndentSnippet(string content, string baseIndent)
        {
            var lines = content.Split('\n');
            var result = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    result.Add(lines[i]);
                }
                else
                {
                    result.Add(baseIndent + lines[i]);
                }
            }

            return string.Join("\n", result);
        }

        private class Snippet
        {
            public string Trigger { get; set; } = "";
            public string Content { get; set; } = "";
            public string Description { get; set; } = "";
        }
    }
}
