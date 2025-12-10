using System.Collections.Generic;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// New visualizer preset model that references AudioTriggerPoints and uses ShaderParamMappings
    /// This is the refactored version that separates concerns:
    /// - FFT Window creates AudioTriggerPoints
    /// - Audio Mixer creates MusicMixPresets
    /// - Visualizer Settings creates VisualizerPresetNew (references AudioTriggerPoints)
    ///
    /// Saved to: visualizer/visualizer_presets/{name}.json
    /// </summary>
    public class VisualizerPresetNew
    {
        /// <summary>
        /// User-friendly name for this preset (e.g., "Default", "WaveRider", "Inferno")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Default/base values for shader parameters (before any trigger modulation)
        /// Key: Shader parameter name (e.g., "ZoomScale", "Contrast", "SpinAmount")
        /// Value: Base value (e.g., 1.0 for ZoomScale, 0.0 for SpinAmount)
        /// </summary>
        public Dictionary<string, float> DefaultShaderParams { get; set; } = new();

        /// <summary>
        /// List of trigger-to-shader-parameter mappings
        /// Defines which triggers affect which shader params and how
        /// </summary>
        public List<ShaderParamMapping> TriggerMappings { get; set; } = new();

        /// <summary>
        /// Selected theme index (0=Default, 1=Wave Rider, etc.)
        /// </summary>
        public int ThemeIndex { get; set; } = 0;

        /// <summary>
        /// Custom main color index (if using custom theme)
        /// </summary>
        public int? MainColor { get; set; }

        /// <summary>
        /// Custom accent color index (if using custom theme)
        /// </summary>
        public int? AccentColor { get; set; }
    }
}
