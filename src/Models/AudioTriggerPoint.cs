namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Audio-based trigger that activates when frequency band exceeds threshold
    /// Implements ITrigger for use in the unified trigger system
    /// </summary>
    public class AudioTriggerPoint : ITrigger
    {
        /// <summary>
        /// Auto-generated name in format: TrackName + FreqBand + RoundedValue
        /// Example: "Bass1Mid63" for Bass1 track, Mid band, threshold value 0.63
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Trigger type identifier (always "Audio" for AudioTriggerPoint)
        /// </summary>
        public string TriggerType => "Audio";

        /// <summary>
        /// User-friendly track name (e.g., "Bass1", "Drums1", "Melody1")
        /// </summary>
        public string TrackName { get; set; } = string.Empty;

        /// <summary>
        /// Internal track identifier (lowercase, e.g., "bass1", "drums1")
        /// </summary>
        public string TrackId { get; set; } = string.Empty;

        /// <summary>
        /// Frequency band: "Low" (Bass), "Mid", or "High" (Treble)
        /// </summary>
        public string FrequencyBand { get; set; } = string.Empty;

        /// <summary>
        /// Threshold value that triggers the effect (0-1 normalized)
        /// </summary>
        public double ThresholdValue { get; set; }

        // Runtime state - should be injected by audio manager
        private float _currentValue = 0f;
        private bool _isActive = false;

        /// <summary>
        /// Update runtime state from audio manager
        /// Should be called each frame by the audio system
        /// </summary>
        public void UpdateState(float currentBandValue)
        {
            _currentValue = currentBandValue;
            _isActive = currentBandValue > ThresholdValue;
        }

        /// <summary>
        /// Check if current band value exceeds threshold
        /// </summary>
        public bool IsActive()
        {
            return _isActive;
        }

        /// <summary>
        /// Get current band value (0-1 range)
        /// </summary>
        public float GetIntensity()
        {
            return _currentValue;
        }
    }
}
