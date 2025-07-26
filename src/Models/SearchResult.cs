namespace Oracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int Score { get; set; }
    public string Details { get; set; } = "";
}