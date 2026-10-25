using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers;

/// <summary>
/// Visualizer preset persistence: one JSON file per preset in VisualizerPresets/.
/// The format is the plain <see cref="VisualizerPreset"/> JSON (no versioning needed today);
/// unreadable files are skipped and logged, and saves are atomic.
/// </summary>
public static class PresetHelper
{
    private static readonly string PresetDir = "VisualizerPresets";

    public static List<VisualizerPreset> LoadAllPresets()
    {
        var presets = new List<VisualizerPreset>();
        try
        {
            if (Directory.Exists(PresetDir))
            {
                foreach (var path in Directory.EnumerateFiles(PresetDir, "*.json"))
                {
                    try
                    {
                        var preset = JsonSerializer.Deserialize(
                            File.ReadAllText(path),
                            BsoJsonSerializerContext.Default.VisualizerPreset
                        );
                        if (preset is not null)
                            presets.Add(preset);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(nameof(PresetHelper), $"Skipping {path}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(PresetHelper), $"LoadAllPresets failed: {ex.Message}");
        }
        return presets;
    }

    public static bool PresetNameExists(string presetName) =>
        LoadAllPresets().Exists(p => string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));

    public static bool SavePreset(VisualizerPreset preset)
    {
        try
        {
            Directory.CreateDirectory(PresetDir);
            var safe = string.Concat(
                (preset.Name ?? "preset").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
            );
            if (safe.Length == 0)
                safe = "preset";
            var json = JsonSerializer.Serialize(preset, BsoJsonSerializerContext.Default.VisualizerPreset);
            JsonFileStore.WriteAllTextAtomic(Path.Combine(PresetDir, $"{safe}.json"), json);
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(PresetHelper), $"SavePreset failed: {ex.Message}");
            return false;
        }
    }

    public static bool DeletePreset(VisualizerPreset preset)
    {
        try
        {
            var safe = string.Concat(
                (preset.Name ?? "preset").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
            );
            var path = Path.Combine(PresetDir, $"{safe}.json");
            if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(PresetHelper), $"DeletePreset failed: {ex.Message}");
            return false;
        }
    }

    public static int ClearAllPresets()
    {
        var count = 0;
        try
        {
            if (Directory.Exists(PresetDir))
            {
                foreach (var path in Directory.EnumerateFiles(PresetDir, "*.json"))
                {
                    File.Delete(path);
                    count++;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(PresetHelper), $"ClearAllPresets failed: {ex.Message}");
        }
        return count;
    }
}
