using AvaloniaEdit;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Hover tooltips over JAML editor tokens (clause docs, enum values).
/// TIER0-STUB: replace in Tier 1. Install is a no-op until tooltip content providers
/// are wired to the engine's clause key documentation.
/// </summary>
public class YAMLHoverTooltipService
{
    private readonly TextEditor _editor;

    public YAMLHoverTooltipService(TextEditor editor)
    {
        _editor = editor;
    }

    public void Install()
    {
        // TIER0-STUB: no hover handlers yet.
    }
}
