using AvaloniaEdit;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Tab-expandable code snippets in the JAML editor (e.g. "must" → must: block skeleton).
/// TIER0-STUB: replace in Tier 1. Never expands, so Tab keeps its default indentation
/// behavior in the editor.
/// </summary>
public class YAMLCodeSnippetService
{
    private readonly TextEditor _editor;

    public YAMLCodeSnippetService(TextEditor editor)
    {
        _editor = editor;
    }

    /// <summary>Try to expand a snippet at the caret; false = let the editor handle the key.</summary>
    public bool TryExpandSnippet() => false;
}
