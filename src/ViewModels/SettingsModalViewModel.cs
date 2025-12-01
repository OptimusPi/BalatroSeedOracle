using System;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the simplified dock settings modal (theme picker only)
    /// </summary>
    public partial class SettingsModalViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;

        [ObservableProperty]
        private int _visualizerTheme;

        // Feature toggles (default OFF)
        [ObservableProperty]
        private bool _showMusicMixerWidget = false;

        [ObservableProperty]
        private bool _showVisualizerWidget = false;

        [ObservableProperty]
        private bool _showTransitionDesignerWidget = false;

        [ObservableProperty]
        private bool _showFertilizerWidget = false;

        [ObservableProperty]
        private bool _showHostApiWidget = false;

        partial void OnVisualizerThemeChanged(int value)
        {
            SaveVisualizerTheme();
        }

        partial void OnShowMusicMixerWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        partial void OnShowVisualizerWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        partial void OnShowTransitionDesignerWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        partial void OnShowFertilizerWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        partial void OnShowHostApiWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        public SettingsModalViewModel()
        {
            _userProfileService =
                App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            LoadSettings();
        }

        #region Events

        public event EventHandler? CloseRequested;
        public event EventHandler? AdvancedSettingsRequested;
        public event EventHandler<int>? VisualizerThemeChanged;
        public event EventHandler? FeatureTogglesChanged;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            VisualizerTheme = profile.VisualizerSettings.ThemeIndex;

            // Load feature toggles (default OFF if not in profile)
            ShowMusicMixerWidget = profile.FeatureToggles?.ShowMusicMixer ?? false;
            ShowVisualizerWidget = profile.FeatureToggles?.ShowVisualizer ?? false;
            ShowTransitionDesignerWidget = profile.FeatureToggles?.ShowTransitionDesigner ?? false;
            ShowFertilizerWidget = profile.FeatureToggles?.ShowFertilizer ?? false;
            ShowHostApiWidget = profile.FeatureToggles?.ShowHostServer ?? false;

            DebugLogger.Log(
                "SettingsModalViewModel",
                $"Settings loaded - Visualizer theme: {VisualizerTheme}, Features: Mixer={ShowMusicMixerWidget}, Viz={ShowVisualizerWidget}, Trans={ShowTransitionDesignerWidget}, Fert={ShowFertilizerWidget}, Host={ShowHostApiWidget}"
            );
        }

        private void SaveFeatureToggles()
        {
            var profile = _userProfileService.GetProfile();
            if (profile.FeatureToggles == null)
            {
                profile.FeatureToggles = new Models.FeatureToggles();
            }

            profile.FeatureToggles.ShowMusicMixer = ShowMusicMixerWidget;
            profile.FeatureToggles.ShowVisualizer = ShowVisualizerWidget;
            profile.FeatureToggles.ShowTransitionDesigner = ShowTransitionDesignerWidget;
            profile.FeatureToggles.ShowFertilizer = ShowFertilizerWidget;
            profile.FeatureToggles.ShowHostServer = ShowHostApiWidget;

            _userProfileService.SaveProfile(profile);
            FeatureTogglesChanged?.Invoke(this, EventArgs.Empty);
            DebugLogger.Log("SettingsModalViewModel", "Feature toggles saved");
        }

        private void SaveVisualizerTheme()
        {
            var profile = _userProfileService.GetProfile();
            profile.VisualizerSettings.ThemeIndex = VisualizerTheme;
            _userProfileService.SaveProfile(profile);
            VisualizerThemeChanged?.Invoke(this, VisualizerTheme);
            DebugLogger.Log("SettingsModalViewModel", $"Visualizer theme saved: {VisualizerTheme}");
        }

        [RelayCommand]
        private void Close()
        {
            DebugLogger.Log("SettingsModalViewModel", "Close requested");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenAdvancedSettings()
        {
            DebugLogger.Log("SettingsModalViewModel", "Advanced settings requested");
            // First close the current modal
            CloseRequested?.Invoke(this, EventArgs.Empty);
            // Then request opening advanced settings
            AdvancedSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
