namespace Oracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int TotalScore { get; set; }
    public string ScoreBreakdown { get; set; } = ""; // JSON array of individual scores
}