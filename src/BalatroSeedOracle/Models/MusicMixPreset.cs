using System.Collections.Generic;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a saved audio mix configuration with volume, pan, mute, and solo settings
    /// Saved to: visualizer/audio_mixes/{name}.json
    /// </summary>
    public class MusicMixPreset
    {
        /// <summary>
        /// User-friendly name for this mix preset (e.g., "MainMenu", "SearchScreen", "ResultsView")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary of track settings keyed by track ID
        /// Key examples: "bass1", "drums1", "chords1", "melody1"
        /// </summary>
        public Dictionary<string, TrackMixSettings> Tracks { get; set; } = new();
    }

    /// <summary>
    /// Per-track audio mixing settings
    /// </summary>
    public class TrackMixSettings
    {
        /// <summary>
        /// Track identifier (e.g., "bass1", "drums1")
        /// </summary>
        public string TrackId { get; set; } = string.Empty;

        /// <summary>
        /// User-friendly track name (e.g., "Bass1", "Drums1")
        /// </summary>
        public string TrackName { get; set; } = string.Empty;

        /// <summary>
        /// Volume level (0-1)
        /// </summary>
        public float Volume { get; set; } = 1.0f;

        /// <summary>
        /// Pan position: -1 (full left) to 1 (full right), 0 = center
        /// </summary>
        public float Pan { get; set; } = 0f;

        /// <summary>
        /// Mute state (true = silenced)
        /// </summary>
        public bool Muted { get; set; } = false;

        /// <summary>
        /// Solo state (true = only this track and other soloed tracks play)
        /// </summary>
        public bool Solo { get; set; } = false;
    }
}
