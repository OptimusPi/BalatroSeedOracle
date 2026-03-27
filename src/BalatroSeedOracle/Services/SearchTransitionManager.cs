using System;
using System.IO;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Manages shader transitions during seed searches.
/// Reads user-configured presets and drives smooth 0-100% transitions.
/// Decoupled from specific view types — uses an Action callback to apply shader params.
/// </summary>
public class SearchTransitionManager
{
    private readonly TransitionService _transitionService;
    private readonly UserProfileService _userProfileService;
    private Action<ShaderParameters>? _applyShaderCallback;
    private ActiveSearchContext? _activeSearch;
    private bool _isTransitionActive;

    public SearchTransitionManager(
        TransitionService transitionService,
        UserProfileService userProfileService)
    {
        _transitionService = transitionService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Register a callback that applies shader parameters to the UI.
    /// Called by the UI layer during initialization.
    /// </summary>
    public void SetShaderCallback(Action<ShaderParameters> callback)
    {
        _applyShaderCallback = callback;
    }

    public void StartSearchTransition(ActiveSearchContext searchInstance)
    {
        var settings = _userProfileService.GetProfile().VisualizerSettings;
        if (!settings.EnableSearchTransition)
        {
            DebugLogger.Log("SearchTransitionManager", "Search transitions disabled by user");
            return;
        }

        StopSearchTransition();

        var startParams = LoadPresetParameters(settings.SearchTransitionStartPresetName);
        var endParams = LoadPresetParameters(settings.SearchTransitionEndPresetName);

        if (startParams == null || endParams == null)
        {
            DebugLogger.LogError("SearchTransitionManager", "Failed to load transition presets");
            return;
        }

        _transitionService.StartTransition(startParams, endParams, ApplyShaderParameters);
        _activeSearch = searchInstance;
        _activeSearch.ProgressUpdated += OnSearchProgressUpdated;
        _isTransitionActive = true;
    }

    public void StopSearchTransition()
    {
        if (_activeSearch != null)
        {
            _activeSearch.ProgressUpdated -= OnSearchProgressUpdated;
            _activeSearch = null;
        }

        if (_isTransitionActive)
        {
            _transitionService.StopTransition();
            _isTransitionActive = false;
        }
    }

    private void OnSearchProgressUpdated(object? sender, SearchProgress progress)
    {
        if (!_isTransitionActive) return;

        float normalizedProgress = (float)(progress.PercentComplete / 100.0);
        _transitionService.SetProgress(normalizedProgress);

        if (progress.IsComplete)
        {
            DebugLogger.LogImportant("SearchTransitionManager", "Search complete — transition finished");
            StopSearchTransition();
        }
    }

    private ShaderParameters? LoadPresetParameters(string? presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
            return null;

        if (presetName == "Default Balatro")
            return VisualizerPresetExtensions.CreateDefaultNormalParameters();

        try
        {
            var presetsPath = Path.Combine(AppContext.BaseDirectory, "Presets");
            var presetFile = Path.Combine(presetsPath, $"{presetName}.json");

            if (!File.Exists(presetFile))
            {
                DebugLogger.LogError("SearchTransitionManager", $"Preset not found: {presetFile}");
                return null;
            }

            var json = File.ReadAllText(presetFile);
            var preset = System.Text.Json.JsonSerializer.Deserialize<VisualizerPreset>(json);
            if (preset != null)
                return VisualizerPresetExtensions.ToShaderParameters(preset);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchTransitionManager", $"Failed to load preset {presetName}: {ex.Message}");
        }
        return null;
    }

    private void ApplyShaderParameters(ShaderParameters parameters)
    {
        try
        {
            _applyShaderCallback?.Invoke(parameters);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchTransitionManager", $"Failed to apply shader: {ex.Message}");
        }
    }
}
