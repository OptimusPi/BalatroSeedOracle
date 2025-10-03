using System;
using System.Collections.Generic;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a saved preset for audio visualizer settings
    /// </summary>
    public class VisualizerPreset
    {
        /// <summary>
        /// Unique identifier for the preset
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User-friendly name for the preset
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Selected theme index (0=Default, 1=Wave Rider, etc., last=CUSTOMIZE)
        /// </summary>
        public int ThemeIndex { get; set; }

        /// <summary>
        /// Custom main color index (0-7 for colors, 8 for None/Theme Default)
        /// </summary>
        public int? MainColor { get; set; }

        /// <summary>
        /// Custom accent color index (0-7 for colors, 8 for None/Theme Default)
        /// </summary>
        public int? AccentColor { get; set; }

        /// <summary>
        /// Audio reactivity intensity (0-2)
        /// </summary>
        public float AudioIntensity { get; set; }

        /// <summary>
        /// Mouse parallax effect strength (0-2)
        /// </summary>
        public float ParallaxStrength { get; set; }

        /// <summary>
        /// Base animation speed (0-3)
        /// </summary>
        public float TimeSpeed { get; set; }

        /// <summary>
        /// Enable seed found audio event trigger
        /// </summary>
        public bool SeedFoundTrigger { get; set; }

        /// <summary>
        /// Enable high score seed audio event trigger
        /// </summary>
        public bool HighScoreSeedTrigger { get; set; }

        /// <summary>
        /// Enable search complete audio event trigger
        /// </summary>
        public bool SearchCompleteTrigger { get; set; }

        /// <summary>
        /// Audio source for seed found trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int SeedFoundAudioSource { get; set; } = 1;

        /// <summary>
        /// Audio source for high score trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int HighScoreAudioSource { get; set; } = 4;

        /// <summary>
        /// Audio source for search complete trigger (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public int SearchCompleteAudioSource { get; set; } = 3;

        /// <summary>
        /// Custom effect mappings for advanced customization
        /// Key: Effect name (e.g., "ShadowFlicker", "Spin", "Twirl", "ZoomThump", "ColorSaturation", "BeatPulse")
        /// Value: Audio source index (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody)
        /// </summary>
        public Dictionary<string, int>? CustomEffects { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
