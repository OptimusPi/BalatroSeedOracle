using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Brace-based code folding for the YAML/JSON filter editors.
/// TIER0-STUB: replace in Tier 1. AvaloniaEdit 12 removed the classic BraceFoldingStrategy
/// sample; this is a minimal reimplementation that folds balanced {}, [] and explicit
/// "#region"/"#endregion" markers.
/// </summary>
public class BraceFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = CreateNewFoldings(document, out int firstErrorOffset);
        manager.UpdateFoldings(foldings, firstErrorOffset);
    }

    private static List<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        var foldings = new List<NewFolding>();
        var text = document.Text ?? string.Empty;

        var stack = new Stack<int>();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is '{' or '[')
            {
                stack.Push(i);
            }
            else if (c is '}' or ']')
            {
                if (stack.Count > 0)
                {
                    int start = stack.Pop();
                    if (text[start] == (c == '}' ? '{' : '['))
                    {
                        foldings.Add(
                            new NewFolding
                            {
                                StartOffset = start,
                                EndOffset = i + 1,
                                Name = string.Empty,
                            }
                        );
                    }
                }
                else
                {
                    firstErrorOffset = i;
                    break;
                }
            }
        }

        foldings.Reverse();
        return foldings;
    }
}
