using System;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a single submitted Daylatro score entry.
    /// </summary>
    public class DaylatroHighScore
    {
        public string Seed { get; set; } = string.Empty; // Daily seed (YYYY-MM-DD derived Balatro seed)
        public string Player { get; set; } = string.Empty; // Author/player name (3 chars for Daylatro)
        public long Score { get; set; } // Chips (bestHand in Daylatro)
        public int Ante { get; set; } = 8; // Which ante the score was achieved on
        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
