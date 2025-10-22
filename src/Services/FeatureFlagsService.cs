using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages feature flags for experimental features
    /// </summary>
    public class FeatureFlagsService
    {
        private readonly string _settingsPath;
        private Dictionary<string, bool> _flags;
        private static FeatureFlagsService? _instance;

        public static FeatureFlagsService Instance => _instance ??= new FeatureFlagsService();

        // Feature flag keys
        public const string GENIE_ENABLED = "GenieEnabled";
        public const string DAYLATRO_ENABLED = "DaylatroEnabled";
        public const string SHADER_BACKGROUNDS = "ShaderBackgrounds";
        public const string VISUALIZER_WIDGET = "VisualizerWidget";
        public const string EXPERIMENTAL_SEARCH = "ExperimentalSearch";
        public const string DEBUG_MODE = "DebugMode";

        private FeatureFlagsService()
        {
            _settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "feature_flags.json");
            _flags = new Dictionary<string, bool>();
            LoadFlags();
        }

        /// <summary>
        /// Check if a feature is enabled
        /// </summary>
        public bool IsEnabled(string feature)
        {
            return _flags.TryGetValue(feature, out var enabled) && enabled;
        }

        /// <summary>
        /// Toggle a feature on/off
        /// </summary>
        public void SetFeature(string feature, bool enabled)
        {
            _flags[feature] = enabled;
            SaveFlags();
            DebugLogger.Log("FeatureFlags", $"Feature '{feature}' set to {enabled}");
        }

        /// <summary>
        /// Get all feature flags
        /// </summary>
        public Dictionary<string, bool> GetAllFlags()
        {
            return new Dictionary<string, bool>(_flags);
        }

        /// <summary>
        /// Get user-friendly feature descriptions
        /// </summary>
        public static Dictionary<string, string> GetFeatureDescriptions()
        {
            return new Dictionary<string, string>
            {
                { GENIE_ENABLED, "AI Assistant (Genie)" },
                { DAYLATRO_ENABLED, "Daylatro Mode (daily challenges)" },
                { SHADER_BACKGROUNDS, "Animated Shader Backgrounds" },
                { EXPERIMENTAL_SEARCH, "Experimental Search Features" },
                { DEBUG_MODE, "Debug Mode (verbose logging)" }
            };
        }

        private void LoadFlags()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _flags = JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                             ?? new Dictionary<string, bool>();
                    DebugLogger.Log("FeatureFlags", $"Loaded {_flags.Count} feature flags");
                }
                else
                {
                    // Default settings - most experimental features OFF
                    _flags = new Dictionary<string, bool>
                    {
                        { GENIE_ENABLED, false },  // Genie OFF by default
                        { DAYLATRO_ENABLED, true },  // Daylatro ON by default
                        { SHADER_BACKGROUNDS, true },  // Shaders ON
                        { EXPERIMENTAL_SEARCH, false },  // Experimental search OFF
                        { DEBUG_MODE, false }  // Debug OFF
                    };
                    SaveFlags();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FeatureFlags", $"Error loading flags: {ex.Message}");
                _flags = new Dictionary<string, bool>();
            }
        }

        private void SaveFlags()
        {
            try
            {
                var json = JsonSerializer.Serialize(_flags, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, json);
                DebugLogger.Log("FeatureFlags", "Feature flags saved");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FeatureFlags", $"Error saving flags: {ex.Message}");
            }
        }
    }
}