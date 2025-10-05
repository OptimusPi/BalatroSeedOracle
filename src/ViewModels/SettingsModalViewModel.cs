using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the simplified dock settings modal (theme picker only)
    /// </summary>
    public class SettingsModalViewModel : BaseViewModel
    {
        private readonly UserProfileService _userProfileService;
        private int _visualizerTheme;

        public SettingsModalViewModel()
        {
            _userProfileService = App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            CloseCommand = new RelayCommand(Close);
            AdvancedSettingsCommand = new RelayCommand(OpenAdvancedSettings);

            LoadSettings();
        }

        #region Properties

        public int VisualizerTheme
        {
            get => _visualizerTheme;
            set
            {
                if (SetProperty(ref _visualizerTheme, value))
                {
                    SaveVisualizerTheme();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand CloseCommand { get; }
        public ICommand AdvancedSettingsCommand { get; }

        #endregion

        #region Events

        public event EventHandler? CloseRequested;
        public event EventHandler? AdvancedSettingsRequested;
        public event EventHandler<int>? OnVisualizerThemeChanged;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            _visualizerTheme = profile.VibeOutSettings.ThemeIndex;
            OnPropertyChanged(nameof(VisualizerTheme));
            DebugLogger.Log("SettingsModalViewModel", $"Settings loaded - Visualizer theme: {_visualizerTheme}");
        }

        private void SaveVisualizerTheme()
        {
            var profile = _userProfileService.GetProfile();
            profile.VibeOutSettings.ThemeIndex = _visualizerTheme;
            _userProfileService.SaveProfile(profile);
            OnVisualizerThemeChanged?.Invoke(this, _visualizerTheme);
            DebugLogger.Log("SettingsModalViewModel", $"Visualizer theme saved: {_visualizerTheme}");
        }

        private void Close()
        {
            DebugLogger.Log("SettingsModalViewModel", "Close requested");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

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
