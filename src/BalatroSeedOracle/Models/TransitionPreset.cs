namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Preset for designing transitions between visual shader states and audio mixes.
    /// Supports both manual (time-based) and music-activated transitions.
    /// </summary>
    public class TransitionPreset
    {
        public string Name { get; set; } = string.Empty;
        public string VisualPresetAName { get; set; } = string.Empty;
        public string VisualPresetBName { get; set; } = string.Empty;
        public string MixAName { get; set; } = string.Empty;
        public string MixBName { get; set; } = string.Empty;
        public string Easing { get; set; } = "Linear";
        public double Duration { get; set; } = 2.0;

        /// <summary>
        /// Enable music-activated transition mode.
        /// When enabled, transition progress is driven by audio trigger intensity instead of time.
        /// </summary>
        public bool MusicActivated { get; set; } = false;

        /// <summary>
        /// Name of the audio trigger that activates this transition (e.g., "Bass1Mid63").
        /// Only used when MusicActivated is true.
        /// </summary>
        public string? AudioTriggerName { get; set; }

        /// <summary>
        /// Minimum trigger intensity (0-1) to start the transition.
        /// Below this threshold, transition stays at Preset A.
        /// </summary>
        public double TriggerThreshold { get; set; } = 0.5;

        /// <summary>
        /// Maximum trigger intensity (0-1) that maps to 100% transition progress.
        /// Above this threshold, transition is fully at Preset B.
        /// </summary>
        public double TriggerMax { get; set; } = 1.0;

        /// <summary>
        /// Smoothing factor for audio-driven transitions (0-1).
        /// Higher values = smoother, less jittery transitions.
        /// </summary>
        public double AudioSmoothing { get; set; } = 0.8;
    }
}
