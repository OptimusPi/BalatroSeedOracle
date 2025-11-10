namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Maps a trigger to a shader parameter with configurable effect mode and intensity
    /// </summary>
    public class ShaderParamMapping
    {
        /// <summary>
        /// Shader parameter name (e.g., "ZoomScale", "Contrast", "SpinAmount", "ParallaxX")
        /// </summary>
        public string ShaderParam { get; set; } = string.Empty;

        /// <summary>
        /// Name of the trigger to listen to (references ITrigger.Name, typically an AudioTriggerPoint)
        /// </summary>
        public string TriggerName { get; set; } = string.Empty;

        /// <summary>
        /// How the trigger affects the shader parameter
        /// </summary>
        public EffectMode Mode { get; set; } = EffectMode.SetValue;

        /// <summary>
        /// Inertia decay rate for AddInertia mode (0-1)
        /// Higher values = slower decay, more "alive" feel
        /// Example: 0.95 = retains 95% of velocity each frame
        /// </summary>
        public float InertiaDecay { get; set; } = 0.9f;

        /// <summary>
        /// Multiplier to scale trigger intensity before applying to shader param
        /// Example: 2.0 = double the effect strength
        /// </summary>
        public float Multiplier { get; set; } = 1.0f;

        /// <summary>
        /// Minimum value clamp for the shader parameter
        /// </summary>
        public float MinValue { get; set; } = 0f;

        /// <summary>
        /// Maximum value clamp for the shader parameter
        /// </summary>
        public float MaxValue { get; set; } = 1f;
    }

    /// <summary>
    /// Defines how a trigger affects a shader parameter
    /// </summary>
    public enum EffectMode
    {
        /// <summary>
        /// Directly set shader param to trigger intensity (instant, snappy)
        /// Example: Zoom = trigger intensity * multiplier
        /// </summary>
        SetValue,

        /// <summary>
        /// Add "force" to param with velocity and decay (smooth, alive feel)
        /// Example: Each trigger hit adds velocity, which decays over time
        /// Creates bouncy, physics-based motion
        /// </summary>
        AddInertia,
    }
}
