using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        private readonly SoundEffectsService? _soundEffectsService;
        private readonly Dictionary<EventFXType, EventFXConfig> _configCache = new();

        public EventFXService(
            TransitionService? transitionService = null,
            SoundEffectsService? soundEffectsService = null
        )
        {
            _transitionService = transitionService;
            _soundEffectsService = soundEffectsService;
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
            if (config == null || config.TransitionPreset == "(none)")
            {
                DebugLogger.Log("EventFXService", $"No FX configured for {eventType}");
                return;
            }

            var duration = durationOverride ?? ParseDuration(config.Duration);

            DebugLogger.Log(
                "EventFXService",
                $"Triggering {eventType}: preset={config.TransitionPreset}, duration={duration}s, easing={config.Easing}"
            );
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
                return JsonSerializer.Deserialize<EventFXConfig>(json);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("EventFXService", $"Failed to load config for {eventType}: {ex.Message}");
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
