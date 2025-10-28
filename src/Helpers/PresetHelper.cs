using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Helper class for saving and loading visualizer presets
    /// </summary>
    public static class PresetHelper
    {
        private static readonly string PresetsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BalatroSeedOracle",
            "VisualizerPresets"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        static PresetHelper()
        {
            // Ensure the presets directory exists
            if (!Directory.Exists(PresetsDirectory))
            {
                Directory.CreateDirectory(PresetsDirectory);
                DebugLogger.Log("PresetHelper", $"Created presets directory: {PresetsDirectory}");
            }
        }

        /// <summary>
        /// Normalizes a preset name for use as a filename
        /// Removes special characters, replaces spaces with underscores, converts to lowercase
        /// </summary>
        /// <param name="name">The preset name to normalize</param>
        /// <returns>A filesystem-safe normalized name</returns>
        public static string NormalizePresetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "unnamed";
            }

            // Remove special characters and keep only alphanumeric, spaces, hyphens, and underscores
            string normalized = Regex.Replace(name, @"[^a-zA-Z0-9\s\-_]", "");

            // Replace spaces with underscores
            normalized = normalized.Replace(" ", "_");

            // Replace multiple underscores with single underscore
            normalized = Regex.Replace(normalized, @"_+", "_");

            // Trim underscores from start and end
            normalized = normalized.Trim('_');

            // Convert to lowercase
            normalized = normalized.ToLowerInvariant();

            // If the result is empty, use "unnamed"
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "unnamed";
            }

            return normalized;
        }

        /// <summary>
        /// Saves a preset to disk
        /// </summary>
        /// <param name="preset">The preset to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SavePreset(VisualizerPreset preset)
        {
            try
            {
                if (preset == null)
                {
                    DebugLogger.Log("PresetHelper", "Cannot save null preset");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(preset.Name))
                {
                    DebugLogger.Log("PresetHelper", "Cannot save preset without a name");
                    return false;
                }

                // Update modified timestamp
                preset.ModifiedAt = DateTime.UtcNow;

                // Generate filename: {NormalizedName}_{Id}.json
                string normalizedName = NormalizePresetName(preset.Name);
                string filename = $"{normalizedName}_{preset.Id}.json";
                string fullPath = Path.Combine(PresetsDirectory, filename);

                // Serialize and save
                string json = JsonSerializer.Serialize(preset, JsonOptions);
                File.WriteAllText(fullPath, json);

                DebugLogger.Log("PresetHelper", $"Saved preset '{preset.Name}' to {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("PresetHelper", $"Error saving preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a preset from disk by file path
        /// </summary>
        /// <param name="filePath">Full path to the preset file</param>
        /// <returns>The loaded preset, or null if load failed</returns>
        public static VisualizerPreset? LoadPreset(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    DebugLogger.Log("PresetHelper", $"Preset file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var preset = JsonSerializer.Deserialize<VisualizerPreset>(json, JsonOptions);

                DebugLogger.Log("PresetHelper", $"Loaded preset from {filePath}");
                return preset;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "PresetHelper",
                    $"Error loading preset from {filePath}: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Loads all presets from the presets directory
        /// </summary>
        /// <returns>List of all available presets, sorted by name</returns>
        public static List<VisualizerPreset> LoadAllPresets()
        {
            var presets = new List<VisualizerPreset>();

            try
            {
                if (!Directory.Exists(PresetsDirectory))
                {
                    DebugLogger.Log(
                        "PresetHelper",
                        "Presets directory does not exist, returning empty list"
                    );
                    return presets;
                }

                var files = Directory.GetFiles(PresetsDirectory, "*.json");
                DebugLogger.Log("PresetHelper", $"Found {files.Length} preset files");

                foreach (var file in files)
                {
                    var preset = LoadPreset(file);
                    if (preset != null)
                    {
                        presets.Add(preset);
                    }
                }

                // Sort by name
                presets = presets.OrderBy(p => p.Name).ToList();
                DebugLogger.Log("PresetHelper", $"Successfully loaded {presets.Count} presets");
            }
            catch (Exception ex)
            {
                DebugLogger.Log("PresetHelper", $"Error loading presets: {ex.Message}");
            }

            return presets;
        }

        /// <summary>
        /// Deletes a preset file from disk
        /// </summary>
        /// <param name="preset">The preset to delete</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public static bool DeletePreset(VisualizerPreset preset)
        {
            try
            {
                if (preset == null)
                {
                    DebugLogger.Log("PresetHelper", "Cannot delete null preset");
                    return false;
                }

                // Find the file by ID (since the normalized name might have changed)
                var files = Directory.GetFiles(PresetsDirectory, $"*_{preset.Id}.json");

                if (files.Length == 0)
                {
                    DebugLogger.Log("PresetHelper", $"No preset file found for ID {preset.Id}");
                    return false;
                }

                foreach (var file in files)
                {
                    File.Delete(file);
                    DebugLogger.Log("PresetHelper", $"Deleted preset file: {file}");
                }

                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("PresetHelper", $"Error deleting preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the full path to the presets directory
        /// </summary>
        public static string GetPresetsDirectory()
        {
            return PresetsDirectory;
        }

        /// <summary>
        /// Checks if a preset with the given name already exists
        /// </summary>
        /// <param name="name">The preset name to check</param>
        /// <param name="excludeId">Optional preset ID to exclude from the check (for updates)</param>
        /// <returns>True if a preset with this name exists, false otherwise</returns>
        public static bool PresetNameExists(string name, string? excludeId = null)
        {
            try
            {
                var presets = LoadAllPresets();
                return presets.Any(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.Id != excludeId
                );
            }
            catch
            {
                return false;
            }
        }
    }
}
