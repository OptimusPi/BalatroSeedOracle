using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Centralized service for managing and evaluating all triggers (Audio, Mouse, GameEvent, etc.)
    /// Responsible for:
    /// - Loading AudioTriggerPoints from JSON
    /// - Registering triggers from different sources
    /// - Evaluating trigger states each frame
    /// - Providing trigger lookup by name
    /// </summary>
    public class TriggerService
    {
        private readonly Dictionary<string, ITrigger> _triggers = new();
        private readonly string _audioTriggersPath;

        // Inertia state tracking for AddInertia mode (shader param → velocity)
        private readonly Dictionary<string, float> _inertiaVelocities = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public TriggerService()
        {
            _audioTriggersPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "visualizer",
                "audio_triggers"
            );

            // Ensure directory exists
            Directory.CreateDirectory(_audioTriggersPath);

            // Load all audio trigger points on startup
            LoadAudioTriggerPoints();
        }

        /// <summary>
        /// Register a trigger (Audio, Mouse, GameEvent, etc.)
        /// </summary>
        public void RegisterTrigger(ITrigger trigger)
        {
            if (string.IsNullOrEmpty(trigger.Name))
            {
                DebugLogger.LogError("TriggerService", "Cannot register trigger with empty name");
                return;
            }

            _triggers[trigger.Name] = trigger;
            DebugLogger.Log(
                "TriggerService",
                $"Registered trigger: {trigger.Name} (Type: {trigger.TriggerType})"
            );
        }

        /// <summary>
        /// Unregister a trigger by name
        /// </summary>
        public void UnregisterTrigger(string triggerName)
        {
            if (_triggers.Remove(triggerName))
            {
                DebugLogger.Log("TriggerService", $"Unregistered trigger: {triggerName}");
            }
        }

        /// <summary>
        /// Get a trigger by name
        /// </summary>
        public ITrigger? GetTrigger(string triggerName)
        {
            return _triggers.TryGetValue(triggerName, out var trigger) ? trigger : null;
        }

        /// <summary>
        /// Get all registered triggers
        /// </summary>
        public IEnumerable<ITrigger> GetAllTriggers()
        {
            return _triggers.Values;
        }

        /// <summary>
        /// Get all triggers of a specific type
        /// </summary>
        public IEnumerable<ITrigger> GetTriggersByType(string triggerType)
        {
            return _triggers.Values.Where(t => t.TriggerType == triggerType);
        }

        /// <summary>
        /// Load all AudioTriggerPoints from JSON files
        /// </summary>
        private void LoadAudioTriggerPoints()
        {
            try
            {
                if (!Directory.Exists(_audioTriggersPath))
                {
                    DebugLogger.Log(
                        "TriggerService",
                        "Audio triggers directory does not exist, creating it"
                    );
                    Directory.CreateDirectory(_audioTriggersPath);
                    return;
                }

                var jsonFiles = Directory.GetFiles(_audioTriggersPath, "*.json");
                var loadedCount = 0;

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var triggerPoint = JsonSerializer.Deserialize<AudioTriggerPoint>(
                            json,
                            JsonOptions
                        );

                        if (triggerPoint != null && !string.IsNullOrEmpty(triggerPoint.Name))
                        {
                            RegisterTrigger(triggerPoint);
                            loadedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "TriggerService",
                            $"Failed to load trigger from {Path.GetFileName(file)}: {ex.Message}"
                        );
                    }
                }

                DebugLogger.Log(
                    "TriggerService",
                    $"Loaded {loadedCount} audio trigger points from {jsonFiles.Length} files"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "TriggerService",
                    $"Failed to load audio trigger points: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Save an AudioTriggerPoint to JSON
        /// </summary>
        public void SaveAudioTriggerPoint(AudioTriggerPoint trigger)
        {
            try
            {
                var fileName = $"{trigger.Name}.json";
                var filePath = Path.Combine(_audioTriggersPath, fileName);

                var json = JsonSerializer.Serialize(trigger, JsonOptions);
                File.WriteAllText(filePath, json);

                // Register the trigger
                RegisterTrigger(trigger);

                DebugLogger.LogImportant(
                    "TriggerService",
                    $"Saved audio trigger: {trigger.Name} to {fileName}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "TriggerService",
                    $"Failed to save trigger {trigger.Name}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Delete an AudioTriggerPoint from disk and unregister it
        /// </summary>
        public void DeleteAudioTriggerPoint(string triggerName)
        {
            try
            {
                var fileName = $"{triggerName}.json";
                var filePath = Path.Combine(_audioTriggersPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    UnregisterTrigger(triggerName);
                    DebugLogger.Log("TriggerService", $"Deleted trigger: {triggerName}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "TriggerService",
                    $"Failed to delete trigger {triggerName}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Update all AudioTriggerPoint states from audio manager
        /// Should be called each frame
        /// </summary>
        public void UpdateAudioTriggers(
            Dictionary<string, (double Low, double Mid, double High)> bandValues
        )
        {
            foreach (var trigger in GetTriggersByType("Audio").OfType<AudioTriggerPoint>())
            {
                // Get band value for this trigger's track
                if (bandValues.TryGetValue(trigger.TrackId, out var bands))
                {
                    float currentValue = trigger.FrequencyBand switch
                    {
                        "Low" => (float)bands.Low,
                        "Mid" => (float)bands.Mid,
                        "High" => (float)bands.High,
                        _ => 0f,
                    };

                    trigger.UpdateState(currentValue);
                }
            }
        }

        /// <summary>
        /// Apply shader parameter mappings with springloaded physics
        /// Call this each frame to update shader parameters based on active triggers
        /// </summary>
        /// <param name="mappings">List of trigger-to-shader mappings</param>
        /// <param name="currentParams">Current shader parameter values (will be modified in-place)</param>
        public void ApplyShaderMappings(
            List<ShaderParamMapping> mappings,
            Dictionary<string, float> currentParams
        )
        {
            foreach (var mapping in mappings)
            {
                // Get the trigger
                var trigger = GetTrigger(mapping.TriggerName);
                if (trigger == null)
                    continue;

                // Get trigger intensity (0-1 normalized value)
                float triggerIntensity = trigger.GetIntensity();

                // Apply multiplier
                float scaledValue = triggerIntensity * mapping.Multiplier;

                // Get current param value (default to 0 if not exists)
                if (!currentParams.TryGetValue(mapping.ShaderParam, out float currentValue))
                {
                    currentValue = 0f;
                }

                // Apply effect mode
                float newValue;
                if (mapping.Mode == EffectMode.SetValue)
                {
                    // SetValue mode: Directly set the value (smooth transition)
                    newValue = scaledValue;
                }
                else // AddInertia mode
                {
                    // AddInertia mode: Springloaded physics (flick to target, then decay back)

                    // Get or initialize velocity for this param
                    string velocityKey = $"{mapping.ShaderParam}_velocity";
                    if (!_inertiaVelocities.TryGetValue(velocityKey, out float velocity))
                    {
                        velocity = 0f;
                    }

                    // If trigger is active, add "force" to velocity
                    if (trigger.IsActive())
                    {
                        velocity += scaledValue;
                    }

                    // Apply decay to velocity (springloaded feel)
                    velocity *= mapping.InertiaDecay;

                    // Update param value with velocity
                    newValue = currentValue + velocity;

                    // Store updated velocity
                    _inertiaVelocities[velocityKey] = velocity;
                }

                // Clamp to min/max
                newValue = Math.Clamp(newValue, mapping.MinValue, mapping.MaxValue);

                // Update current params
                currentParams[mapping.ShaderParam] = newValue;
            }
        }

        /// <summary>
        /// Reset all inertia velocities to zero (useful when changing presets)
        /// </summary>
        public void ResetInertia()
        {
            _inertiaVelocities.Clear();
            DebugLogger.Log("TriggerService", "Reset all inertia velocities");
        }
    }
}
