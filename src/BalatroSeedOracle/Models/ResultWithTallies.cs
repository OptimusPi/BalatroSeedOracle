using System.Collections.Generic;

namespace BalatroSeedOracle.Models;

/// <summary>
/// Represents a search result with seed, score, and tallies.
/// Shared model for both desktop and browser builds (Motely.DuckDB.ResultWithTallies is #if !BROWSER)
/// </summary>
public class ResultWithTallies
{
    public string Seed { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<int> Tallies { get; set; } = new();
}
