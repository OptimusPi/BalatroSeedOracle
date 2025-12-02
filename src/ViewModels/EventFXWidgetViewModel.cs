using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels;

public partial class EventFXWidgetViewModel : BaseWidgetViewModel
{
    private readonly TransitionService? _transitionService;
    private readonly EventFXService? _eventFXService;

    public EventFXWidgetViewModel(
        TransitionService? transitionService = null,
        EventFXService? eventFXService = null
    )
    {
        _transitionService = transitionService;
        _eventFXService = eventFXService;

        WidgetTitle = "Event FX";
        WidgetIcon = "✨";
        IsMinimized = true;

        PositionX = 20;
        PositionY = 400;
        Width = 380;
        Height = 400;

        LoadTransitionPresets();
        LoadEventConfig();
    }

    public ObservableCollection<string> EventOptions { get; } = new()
    {
        "Intro Animation",
        "Search Instance Start",
        "Search Instance Find",
        "Author Launch Edit",
        "Search Launch Modal",
        "Designer Launch Modal",
        "Analyzer Launch Modal",
        "Settings Launch Modal",
    };

    [ObservableProperty]
    private string _selectedEvent = "Intro Animation";

    partial void OnSelectedEventChanged(string value)
    {
        LoadEventConfig();
    }

    public ObservableCollection<string> TransitionPresetOptions { get; } = new();

    [ObservableProperty]
    private string _selectedTransitionPreset = "(none)";

    public ObservableCollection<string> DurationOptions { get; } = new()
    {
        "0.5s", "1s", "2s", "3s", "5s", "10s"
    };

    [ObservableProperty]
    private string _selectedDuration = "2s";

    public ObservableCollection<string> EasingOptions { get; } = new()
    {
        "Linear", "EaseIn", "EaseOut", "EaseInOut"
    };

    [ObservableProperty]
    private string _selectedEasing = "EaseOut";

    private void LoadTransitionPresets()
    {
        TransitionPresetOptions.Clear();
        TransitionPresetOptions.Add("(none)");

        try
        {
            var presets = TransitionPresetHelper.ListNames();
            foreach (var name in presets)
            {
                TransitionPresetOptions.Add(name);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("EventFXWidget", $"Failed to load transition presets: {ex.Message}");
        }
    }

    private string GetConfigFileName(string eventName)
    {
        return eventName.ToLowerInvariant().Replace(" ", "_") + ".json";
    }

    private void LoadEventConfig()
    {
        try
        {
            var configPath = Path.Combine(AppPaths.EventFXDir, GetConfigFileName(SelectedEvent));
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<EventFXConfig>(json);
                if (config != null)
                {
                    SelectedTransitionPreset = config.TransitionPreset ?? "(none)";
                    SelectedDuration = config.Duration ?? "2s";
                    SelectedEasing = config.Easing ?? "EaseOut";
                    return;
                }
            }
            SelectedTransitionPreset = "(none)";
            SelectedDuration = "2s";
            SelectedEasing = "EaseOut";
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("EventFXWidget", $"Failed to load event config: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Preview()
    {
        if (SelectedTransitionPreset == "(none)")
        {
            DebugLogger.Log("EventFXWidget", "No transition preset selected for preview");
            return;
        }

        if (_transitionService == null)
        {
            DebugLogger.LogError("EventFXWidget", "TransitionService not available");
            return;
        }

        if (!double.TryParse(SelectedDuration.TrimEnd('s'), out var duration))
            duration = 2.0;

        DebugLogger.Log("EventFXWidget", $"Previewing {SelectedEvent} with preset {SelectedTransitionPreset}, duration {duration}s, easing {SelectedEasing}");
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var config = new EventFXConfig
            {
                EventName = SelectedEvent,
                TransitionPreset = SelectedTransitionPreset,
                Duration = SelectedDuration,
                Easing = SelectedEasing
            };

            var configPath = Path.Combine(AppPaths.EventFXDir, GetConfigFileName(SelectedEvent));
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            _eventFXService?.ClearCache();
            DebugLogger.Log("EventFXWidget", $"Saved {SelectedEvent} config");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("EventFXWidget", $"Failed to save event config: {ex.Message}");
        }
    }
}
