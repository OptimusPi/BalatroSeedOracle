using System;
using System.IO;
using Avalonia.Controls;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages shader transitions during seed searches.
    /// Reads user-configured presets and drives smooth 0-100% transitions.
    /// </summary>
    public class SearchTransitionManager
    {
        private readonly TransitionService _transitionService;
        private readonly UserProfileService _userProfileService;
        private SearchInstance? _activeSearch;
        private bool _isTransitionActive = false;

        public SearchTransitionManager(
            TransitionService transitionService,
            UserProfileService userProfileService
        )
        {
            _transitionService = transitionService;
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Starts monitoring a search and driving shader transitions based on progress
        /// </summary>
        public void StartSearchTransition(SearchInstance searchInstance)
        {
            // Check if search transitions are enabled
            var settings = _userProfileService.GetProfile().VisualizerSettings;
            if (!settings.EnableSearchTransition)
            {
                DebugLogger.Log("SearchTransitionManager", "Search transitions disabled by user");
                return;
            }

            // Stop any existing transition
            StopSearchTransition();

            DebugLogger.LogImportant(
                "SearchTransitionManager",
                $"Starting search transition: {settings.SearchTransitionStartPresetName} → {settings.SearchTransitionEndPresetName}"
            );

            // Load start and end presets
            var startParams = LoadPresetParameters(settings.SearchTransitionStartPresetName);
            var endParams = LoadPresetParameters(settings.SearchTransitionEndPresetName);

            if (startParams == null || endParams == null)
            {
                DebugLogger.LogError(
                    "SearchTransitionManager",
                    "Failed to load transition presets - aborting transition"
                );
                return;
            }

            // Start the transition
            _transitionService.StartTransition(
                startParams,
                endParams,
                ApplyShaderParameters
            );

            // Hook into search progress
            _activeSearch = searchInstance;
            _activeSearch.ProgressUpdated += OnSearchProgressUpdated;
            _isTransitionActive = true;
        }

        /// <summary>
        /// Stops the current search transition
        /// </summary>
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

        /// <summary>
        /// Called when search progress updates - drives the shader transition
        /// </summary>
        private void OnSearchProgressUpdated(object? sender, SearchProgress progress)
        {
            if (!_isTransitionActive)
                return;

            // Convert progress (0-100%) to normalized value (0.0-1.0)
            float normalizedProgress = (float)(progress.PercentComplete / 100.0);

            // Drive the transition!
            _transitionService.SetProgress(normalizedProgress);

            if (progress.IsComplete)
            {
                DebugLogger.LogImportant("SearchTransitionManager", "Search complete - transition finished!");
                StopSearchTransition();
            }
        }

        /// <summary>
        /// Loads shader parameters from a preset name
        /// </summary>
        private ShaderParameters? LoadPresetParameters(string? presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                DebugLogger.LogError("SearchTransitionManager", "Preset name is empty");
                return null;
            }

            // Handle built-in presets (consolidated)
            if (presetName == "Default Balatro")
            {
                return VisualizerPresetExtensions.CreateDefaultNormalParameters();
            }

            // Try to load from disk
            try
            {
                var presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
                var presetFile = Path.Combine(presetsPath, $"{presetName}.json");

                if (!File.Exists(presetFile))
                {
                    DebugLogger.LogError(
                        "SearchTransitionManager",
                        $"Preset file not found: {presetFile}"
                    );
                    return null;
                }

                var json = File.ReadAllText(presetFile);
                var preset = System.Text.Json.JsonSerializer.Deserialize<VisualizerPreset>(json);

                if (preset != null)
                {
                    // Convert VisualizerPreset to ShaderParameters
                    // For now, just use defaults and log a warning
                    DebugLogger.Log(
                        "SearchTransitionManager",
                        $"Loaded preset: {presetName} (using default conversion)"
                    );
                    return VisualizerPresetExtensions.ToShaderParameters(preset);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchTransitionManager",
                    $"Failed to load preset {presetName}: {ex.Message}"
                );
            }

            return null;
        }

        /// <summary>
        /// Applies shader parameters to the main menu's shader background
        /// </summary>
        private void ApplyShaderParameters(ShaderParameters parameters)
        {
            try
            {
                // Find the main window and apply shader parameters
                // This is a bit hacky but works for now - could be improved with dependency injection
                if (Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow is Views.MainWindow mainWindow)
                    {
                        var mainMenu = mainWindow.FindControl<Views.BalatroMainMenu>("MainMenu");
                        if (mainMenu != null)
                        {
                            ApplyToMainMenu(mainMenu, parameters);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchTransitionManager",
                    $"Failed to apply shader parameters: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Applies shader parameters to BalatroMainMenu using reflection
        /// </summary>
        private void ApplyToMainMenu(Views.BalatroMainMenu mainMenu, ShaderParameters parameters)
        {
            try
            {
                var shaderBackgroundField = typeof(Views.BalatroMainMenu).GetField(
                    "_shaderBackground",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );

                if (shaderBackgroundField?.GetValue(mainMenu)
                    is Controls.BalatroShaderBackground shaderBackground)
                {
                    shaderBackground.SetTime(parameters.TimeSpeed);
                    shaderBackground.SetSpinTime(parameters.SpinTimeSpeed);
                    shaderBackground.SetMainColor(parameters.MainColor);
                    shaderBackground.SetAccentColor(parameters.AccentColor);
                    shaderBackground.SetBackgroundColor(parameters.BackgroundColor);
                    shaderBackground.SetContrast(parameters.Contrast);
                    shaderBackground.SetSpinAmount(parameters.SpinAmount);
                    shaderBackground.SetParallax(parameters.ParallaxX, parameters.ParallaxY);
                    shaderBackground.SetZoomScale(parameters.ZoomScale);
                    shaderBackground.SetSaturationAmount(parameters.SaturationAmount);
                    shaderBackground.SetSaturationAmount2(parameters.SaturationAmount2);
                    shaderBackground.SetPixelSize(parameters.PixelSize);
                    shaderBackground.SetSpinEase(parameters.SpinEase);
                    shaderBackground.SetLoopCount(parameters.LoopCount);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchTransitionManager",
                    $"Failed to apply to main menu: {ex.Message}"
                );
            }
        }
    }
}
