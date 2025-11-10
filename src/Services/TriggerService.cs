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
    }
}
