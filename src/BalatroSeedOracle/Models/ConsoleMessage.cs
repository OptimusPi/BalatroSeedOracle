namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a message in the search console with optional copy-to-clipboard functionality.
/// </summary>
public class ConsoleMessage
{
    public string Text { get; set; } = "";
    public string? CopyableText { get; set; }
    public bool HasCopyButton => !string.IsNullOrEmpty(CopyableText);
}
