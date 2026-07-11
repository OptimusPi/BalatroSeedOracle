using System;
using BalatroSeedOracle.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    public enum EventFXType
    {
        IntroAnimation,
        SearchInstanceStart,
        SearchInstanceFind,
        AuthorLaunchEdit,
        SearchLaunchModal,
        DesignerLaunchModal,
        AnalyzerLaunchModal,
        SettingsLaunchModal,
    }

    public class EventFXConfig
    {
        public string? EventName { get; set; }
        public string? TransitionPreset { get; set; }
        public string? Duration { get; set; }
        public string? Easing { get; set; }
    }

    public class EventFXService
    {
        private readonly TransitionService? _transitionService;
        private readonly Dictionary<EventFXType, EventFXConfig> _configCache = new();

        // The shader hookup: who owns the background provides "where are we now" and
        // "apply this frame". Connected once by BalatroMainMenu when the shader is ready;
        // until then TriggerEvent degrades to the old log-only behavior.
        private Func<Models.ShaderParameters>? _getCurrentParameters;
        private Action<Models.ShaderParameters>? _applyParameters;

        public EventFXService(TransitionService? transitionService = null)
        {
            _transitionService = transitionService;
        }

        /// <summary>
        /// Wires this service to the live shader background. <paramref name="getCurrentParameters"/>
        /// supplies the transition's starting point (the shader's current state, so mid-flight
        /// retriggers blend instead of jumping); <paramref name="applyParameters"/> pushes each
        /// interpolated frame to the shader.
        /// </summary>
        public void Connect(
            Func<Models.ShaderParameters> getCurrentParameters,
            Action<Models.ShaderParameters> applyParameters
        )
        {
            _getCurrentParameters = getCurrentParameters;
            _applyParameters = applyParameters;
        }

        public EventFXConfig? GetConfig(EventFXType eventType)
        {
            if (_configCache.TryGetValue(eventType, out var cached))
                return cached;

            var config = LoadConfig(eventType);
            if (config != null)
                _configCache[eventType] = config;

            return config;
        }

        public void ClearCache()
        {
            _configCache.Clear();
        }

        public void ClearCache(EventFXType eventType)
        {
            _configCache.Remove(eventType);
        }

        public void TriggerEvent(EventFXType eventType, double? durationOverride = null)
        {
            var config = GetConfig(eventType);
            if (
                config == null
                || string.IsNullOrEmpty(config.TransitionPreset)
                || config.TransitionPreset == "(none)"
            )
            {
                DebugLogger.Log("EventFXService", $"No FX configured for {eventType}");
                return;
            }

            var duration = durationOverride ?? ParseDuration(config.Duration);

            DebugLogger.Log(
                "EventFXService",
                $"Triggering {eventType}: preset={config.TransitionPreset}, duration={duration}s, easing={config.Easing}"
            );

            if (
                _transitionService == null
                || _getCurrentParameters == null
                || _applyParameters == null
            )
            {
                DebugLogger.Log(
                    "EventFXService",
                    $"Shader not connected yet - {eventType} FX skipped"
                );
                return;
            }

            // Start from the shader's live state so retriggering mid-transition blends
            // smoothly, and run to the preset the user designed for this event.
            // Easing is stored in the config but not yet interpreted - TransitionService
            // LERPs linearly today; honoring Easing is the next step, not silently done.
            var startParameters = _getCurrentParameters().Clone();
            var targetParameters = ResolvePreset(config.TransitionPreset);
            _transitionService.StartTransition(
                startParameters,
                targetParameters,
                _applyParameters,
                TimeSpan.FromSeconds(duration)
            );
        }

        /// <summary>
        /// Resolves a transition target by name: the user's own saved visualizer presets
        /// (by display name, case-insensitive) win, so an event can transition to any theme
        /// designed in the visualizer settings; otherwise falls back to raw shader preset
        /// files / built-in defaults ("intro", "normal") via <see cref="ShaderPresetHelper"/>.
        /// </summary>
        private static Models.ShaderParameters ResolvePreset(string name)
        {
            foreach (var preset in Helpers.PresetHelper.LoadAllPresets())
            {
                if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                    return preset.ToShaderParameters();
            }

            return ShaderPresetHelper.Load(name);
        }

        private double ParseDuration(string? durationStr)
        {
            if (string.IsNullOrEmpty(durationStr))
                return 2.0;

            var trimmed = durationStr.TrimEnd('s');
            return double.TryParse(trimmed, out var val) ? val : 2.0;
        }

        private EventFXConfig? LoadConfig(EventFXType eventType)
        {
            try
            {
                var fileName = GetConfigFileName(eventType);
                var configPath = Path.Combine(AppPaths.EventFXDir, fileName);

                if (!File.Exists(configPath))
                    return null;

                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize(json, BalatroSeedOracle.Json.BsoJsonSerializerContext.Default.EventFXConfig);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "EventFXService",
                    $"Failed to load config for {eventType}: {ex.Message}"
                );
                return null;
            }
        }

        private string GetConfigFileName(EventFXType eventType)
        {
            return eventType switch
            {
                EventFXType.IntroAnimation => "intro_animation.json",
                EventFXType.SearchInstanceStart => "search_instance_start.json",
                EventFXType.SearchInstanceFind => "search_instance_find.json",
                EventFXType.AuthorLaunchEdit => "author_launch_edit.json",
                EventFXType.SearchLaunchModal => "search_launch_modal.json",
                EventFXType.DesignerLaunchModal => "designer_launch_modal.json",
                EventFXType.AnalyzerLaunchModal => "analyzer_launch_modal.json",
                EventFXType.SettingsLaunchModal => "settings_launch_modal.json",
                _ => "unknown.json",
            };
        }
    }
}
