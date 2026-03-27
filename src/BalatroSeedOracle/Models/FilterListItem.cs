namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a filter entry in the selector list.
/// </summary>
public class FilterListItem
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSelected { get; set; }

    public string DisplayIndex => Number.ToString();
}
