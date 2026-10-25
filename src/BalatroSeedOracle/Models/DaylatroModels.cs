using System.Collections.Generic;

namespace BalatroSeedOracle.Models;

/// <summary>
/// One day's Daylatro high-score entry.
/// Shape mirrors the persisted daylatro_scores.json and is registered with
/// BsoJsonSerializerContext; (de)serialization is the only consumer today.
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
