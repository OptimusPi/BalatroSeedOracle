using System;
using System.Collections.Generic;
using System.Linq;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Aggregated scores for a given daily seed.
    /// </summary>
    public class DaylatroDailyScores
    {
        public string Seed { get; set; } = string.Empty;
        public DateTime DateUtc { get; set; } // canonical date at UTC midnight
        public List<DaylatroHighScore> Scores { get; set; } = new();

        public DaylatroHighScore? GetTopScore() => Scores.OrderByDescending(s => s.Score).FirstOrDefault();
    }
}
