using System.Collections.Generic;
using AvaloniaEdit.CodeCompletion;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Context-aware completion provider for the JAML (YAML) filter editor.
/// TIER0-STUB: replace in Tier 1 with engine-aware completions (JamlConfigLoader.ClauseKeys,
/// filter keys, enum values). Returning no completions keeps editing functional.
/// </summary>
public static class YAMLAutocompletionHelper
{
    public static IReadOnlyList<ICompletionData> GetCompletionsForContext(string textBeforeCursor) =>
        [];
}

/// <summary>
/// Context-aware completion provider for the JSON filter editor.
/// TIER0-STUB: replace in Tier 1 alongside <see cref="YAMLAutocompletionHelper"/>.
/// </summary>
public static class JsonAutocompletionHelper
{
    public static IReadOnlyList<ICompletionData> GetCompletionsForContext(string textBeforeCursor) =>
        [];
}
