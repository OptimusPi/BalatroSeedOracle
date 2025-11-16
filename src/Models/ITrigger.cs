namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Base interface for all trigger types (Audio, Mouse, GameEvent, etc.)
    /// Triggers are conditions that can activate shader effects or other visual/audio reactions
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// Unique name for this trigger (e.g., "Bass1Mid63", "MouseParallax", "SeedFoundEvent")
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Type of trigger: "Audio", "Mouse", "GameEvent", etc.
        /// </summary>
        string TriggerType { get; }

        /// <summary>
        /// Check if trigger condition is currently met (threshold exceeded, event fired, etc.)
        /// </summary>
        bool IsActive();

        /// <summary>
        /// Get current intensity/value of the trigger (0-1 or custom range)
        /// Used to modulate effect strength
        /// </summary>
        float GetIntensity();
    }
}
