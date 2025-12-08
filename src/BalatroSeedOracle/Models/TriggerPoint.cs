namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a custom trigger point for audio-visual effects
    /// </summary>
    public class TriggerPoint
    {
        /// <summary>
        /// Auto-generated name in format: TrackName + FreqBand + RoundedValue
        /// Example: "Bass1Mid63" for Bass1 track, Mid band, value 63.47
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User-friendly track name (e.g., "Bass1", "Drums1", "Melody1")
        /// </summary>
        public string TrackName { get; set; } = string.Empty;

        /// <summary>
        /// Internal track identifier
        /// </summary>
        public string TrackId { get; set; } = string.Empty;

        /// <summary>
        /// Frequency band: "Low" (Bass), "Mid", or "High" (Treble)
        /// </summary>
        public string FrequencyBand { get; set; } = string.Empty;

        /// <summary>
        /// Threshold value that triggers the effect (0-100)
        /// </summary>
        public double ThresholdValue { get; set; }

        /// <summary>
        /// Effect name to trigger (e.g., "ZoomPunch", "Contrast", "Spin", "Twirl")
        /// </summary>
        public string EffectName { get; set; } = string.Empty;

        /// <summary>
        /// Effect intensity multiplier (typically 0.1 to 2.0)
        /// </summary>
        public double EffectIntensity { get; set; } = 1.0;
    }
}
