using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Code folding for the JAML (and JSON) filter editors. JAML is YAML-shaped, so blocks are
/// defined by indentation: every key or <c>- </c> item with deeper-indented lines under it folds
/// (<c>must:</c>, each clause, a <c>sources:</c> block), as does any <c>[</c>/<c>{</c> left open
/// across lines — which covers pretty-printed JSON. The regions come from
/// <see cref="JamlLanguage.FoldRegions"/>; a fold starts at the end of its first line, so the
/// key stays visible with the block collapsed behind it.
/// </summary>
public class JamlFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        manager.UpdateFoldings(CreateNewFoldings(document), -1);
    }

    public static IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        // One fold per start line (an indented block that also opens a bracket would give two):
        // the longest wins.
        var byStart = new SortedDictionary<int, int>();
        foreach (var (startLine, endLine) in JamlLanguage.FoldRegions(document.Text))
        {
            if (endLine + 1 > document.LineCount)
                continue;
            if (!byStart.TryGetValue(startLine, out var existing) || endLine > existing)
                byStart[startLine] = endLine;
        }

        // FoldingManager.UpdateFoldings wants start-offset order, which line order gives.
        var foldings = new List<NewFolding>(byStart.Count);
        foreach (var (startLine, endLine) in byStart)
        {
            var first = document.GetLineByNumber(startLine + 1);
            var last = document.GetLineByNumber(endLine + 1);
            foldings.Add(new NewFolding(first.EndOffset, last.EndOffset) { Name = "…" });
        }
        return foldings;
    }
}
