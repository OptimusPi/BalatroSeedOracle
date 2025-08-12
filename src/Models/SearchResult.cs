namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int TotalScore { get; set; }
    public int[]? Scores { get; set; }
    public string[]? Labels { get; set; }  // Only used temporarily for first result to establish column headers
}
