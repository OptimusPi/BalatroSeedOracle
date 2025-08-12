using System;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a single search result from Motely
/// </summary>
public class SearchResult
{
    public string Seed { get; set; } = "";
    public int TotalScore { get; set; }
    public int[] Scores { get; set; } = Array.Empty<int>();
}
