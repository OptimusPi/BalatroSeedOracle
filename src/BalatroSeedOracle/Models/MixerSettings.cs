using System.Text.Json.Serialization;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents the settings for a single audio track in the mixer
    /// </summary>
    public class TrackSettings
    {
        [JsonPropertyName("volume")]
        public double Volume { get; set; } = 100;

        [JsonPropertyName("pan")]
        public double Pan { get; set; } = 0;

        [JsonPropertyName("muted")]
        public bool Muted { get; set; } = false;

        [JsonPropertyName("solo")]
        public bool Solo { get; set; } = false;
    }

    /// <summary>
    /// Serializable settings for the entire Music Mixer widget
    /// </summary>
    public class MixerSettings
    {
        [JsonPropertyName("drums1")]
        public TrackSettings Drums1 { get; set; } = new();

        [JsonPropertyName("drums2")]
        public TrackSettings Drums2 { get; set; } = new();

        [JsonPropertyName("bass1")]
        public TrackSettings Bass1 { get; set; } = new();

        [JsonPropertyName("bass2")]
        public TrackSettings Bass2 { get; set; } = new();

        [JsonPropertyName("chords1")]
        public TrackSettings Chords1 { get; set; } = new();

        [JsonPropertyName("chords2")]
        public TrackSettings Chords2 { get; set; } = new();

        [JsonPropertyName("melody1")]
        public TrackSettings Melody1 { get; set; } = new();

        [JsonPropertyName("melody2")]
        public TrackSettings Melody2 { get; set; } = new();
    }
}
