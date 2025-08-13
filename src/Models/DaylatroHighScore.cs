using System;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a single submitted Daylatro score entry.
    /// </summary>
    public class DaylatroHighScore
    {
        public string Seed { get; set; } = string.Empty; // Daily seed (YYYY-MM-DD derived Balatro seed)
        public string Player { get; set; } = string.Empty; // Author/player name
        public long Score { get; set; } // Chips
        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
