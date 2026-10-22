using System;
using System.Linq;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Loads <see cref="ShaderParameters"/> by preset name ("normal", "search-complete", …).
/// TIER0-STUB: replace in Tier 1. Named presets resolve through <see cref="PresetHelper"/>;
/// anything unknown falls back to the default Balatro parameters.
/// </summary>
public static class ShaderPresetHelper
{
    public static ShaderParameters Load(string presetName)
    {
        try
        {
            var preset = PresetHelper
                .LoadAllPresets()
                .FirstOrDefault(p => string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));
            if (preset is not null)
                return preset.ToShaderParameters();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(ShaderPresetHelper), $"Load({presetName}) failed: {ex.Message}");
        }
        return VisualizerPresetExtensions.CreateDefaultNormalParameters();
    }
}
