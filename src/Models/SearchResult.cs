namespace Oracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int Score { get; set; }
    public string Details { get; set; } = "";
    public int Ante { get; set; } = 1;
    public string ScoreBreakdown { get; set; } = ""; // JSON array of individual scores
}