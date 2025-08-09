using System;

namespace Oracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public double TotalScore { get; set; }
    public string ScoreBreakdown { get; set; } = ""; // JSON array of individual scores
    
    // Enhanced fields for better display
    public int[]? TallyScores { get; set; }
    public string[]? ItemLabels { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}