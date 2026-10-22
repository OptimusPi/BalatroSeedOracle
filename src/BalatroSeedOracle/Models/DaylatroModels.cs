using System.Collections.Generic;

namespace BalatroSeedOracle.Models;

/// <summary>
/// One day's Daylatro high-score entry.
/// TIER0-STUB: replace in Tier 1. Shape mirrors the persisted daylatro_scores.json; no
/// in-app consumer reads these beyond (de)serialization yet.
/// </summary>
public class DaylatroHighScore
{
    public string Seed { get; set; } = string.Empty;

    public long Score { get; set; }

    public string Date { get; set; } = string.Empty;
}

/// <summary>A day's score table: the date plus that day's high scores.</summary>
public class DaylatroDailyScores
{
    public string Date { get; set; } = string.Empty;

    public List<DaylatroHighScore> Scores { get; set; } = new();
}
