using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

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

        partial void OnVisualizerThemeChanged(int value)
        {
            SaveVisualizerTheme();
        }

        public SettingsModalViewModel()
        {
            _userProfileService = App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            LoadSettings();
        }

        #region Events

        public event EventHandler? CloseRequested;
        public event EventHandler? AdvancedSettingsRequested;
        public event EventHandler<int>? VisualizerThemeChanged;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            VisualizerTheme = profile.VisualizerSettings.ThemeIndex;
            DebugLogger.Log("SettingsModalViewModel", $"Settings loaded - Visualizer theme: {VisualizerTheme}");
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
