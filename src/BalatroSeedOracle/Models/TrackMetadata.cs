using System.Text.Json.Serialization;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Metadata for a music track's FFT analysis settings.
    /// Stored per-track so each music file can have custom frequency thresholds.
    /// </summary>
    public class TrackMetadata
    {
        [JsonPropertyName("trackName")]
        public string TrackName { get; set; } = string.Empty;

        [JsonPropertyName("bassThreshold")]
        public double BassThreshold { get; set; } = 0.5;

        [JsonPropertyName("midThreshold")]
        public double MidThreshold { get; set; } = 0.3;

        [JsonPropertyName("highThreshold")]
        public double HighThreshold { get; set; } = 0.2;

        [JsonPropertyName("bassAvgMax")]
        public double BassAvgMax { get; set; } = 0.0;

        [JsonPropertyName("bassPeakMax")]
        public double BassPeakMax { get; set; } = 0.0;

        [JsonPropertyName("midAvgMax")]
        public double MidAvgMax { get; set; } = 0.0;

        [JsonPropertyName("midPeakMax")]
        public double MidPeakMax { get; set; } = 0.0;

        [JsonPropertyName("highAvgMax")]
        public double HighAvgMax { get; set; } = 0.0;

        [JsonPropertyName("highPeakMax")]
        public double HighPeakMax { get; set; } = 0.0;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
