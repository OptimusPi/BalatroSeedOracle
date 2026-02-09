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
        private readonly SearchManager? _searchManager; // Nullable if service might fail

        [ObservableProperty]
        private int _visualizerTheme;
        
        // Search Engine Settings
        [ObservableProperty]
        private int _selectedSearchEngineIndex; // 0=Local, 1=Public, 2=Custom
        
        [ObservableProperty]
        private string _customRemoteUrl = "http://localhost:5000";
        
        [ObservableProperty]
        private bool _isCustomUrlVisible;

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

        [ObservableProperty]
        private bool _showEventFXWidget = false;

        partial void OnVisualizerThemeChanged(int value)
        {
            SaveVisualizerTheme();
        }

        partial void OnSelectedSearchEngineIndexChanged(int value)
        {
            IsCustomUrlVisible = value == 2;
            UpdateSearchEngine();
        }

        partial void OnCustomRemoteUrlChanged(string value)
        {
            if (SelectedSearchEngineIndex == 2)
                UpdateSearchEngine();
        }

        private void UpdateSearchEngine()
        {
            switch (SelectedSearchEngineIndex)
            {
                case 0:
                    _searchManager.SetEngine(_searchManager.LocalEngine);
                    break;
                case 1:
                    _searchManager.SetRemoteUrl("https://api.motely.gg");
                    break;
                case 2:
                    if (!string.IsNullOrWhiteSpace(CustomRemoteUrl))
                        _searchManager.SetRemoteUrl(CustomRemoteUrl);
                    break;
            }
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

        partial void OnShowEventFXWidgetChanged(bool value)
        {
            SaveFeatureToggles();
        }

        public SettingsModalViewModel()
        {
            _userProfileService =
                App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");
            
            // Allow null for previewer/design time, but log warning
            _searchManager = App.GetService<SearchManager>();
            if (_searchManager == null)
            {
                // In production this might be fatal, but for now we allow it
                DebugLogger.Log("SettingsModalViewModel", "SearchManager not available");
            }

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
            
            // Load Engine Settings
            if (_searchManager.ActiveEngine.IsLocal)
            {
                SelectedSearchEngineIndex = 0;
            }
            else if (_searchManager.ActiveEngine.Name.Contains("api.motely.gg"))
            {
                SelectedSearchEngineIndex = 1;
            }
            else
            {
                SelectedSearchEngineIndex = 2;
                IsCustomUrlVisible = true;
                // Ideally extract URL from ActiveEngine name if possible
            }

            // Load feature toggles (default OFF if not in profile)
            ShowMusicMixerWidget = profile.FeatureToggles?.ShowMusicMixer ?? false;
            ShowVisualizerWidget = profile.FeatureToggles?.ShowVisualizer ?? false;
            ShowTransitionDesignerWidget = profile.FeatureToggles?.ShowTransitionDesigner ?? false;
            ShowFertilizerWidget = profile.FeatureToggles?.ShowFertilizer ?? false;
            ShowHostApiWidget = profile.FeatureToggles?.ShowHostServer ?? false;
            ShowEventFXWidget = profile.FeatureToggles?.ShowEventFX ?? false;

            DebugLogger.Log(
                "SettingsModalViewModel",
                $"Settings loaded - Visualizer theme: {VisualizerTheme}, Features: Mixer={ShowMusicMixerWidget}, Viz={ShowVisualizerWidget}, Trans={ShowTransitionDesignerWidget}, Fert={ShowFertilizerWidget}, Host={ShowHostApiWidget}"
            );
        }

        private void SaveFeatureToggles()
        {
            var profile = _userProfileService.GetProfile();
            if (profile.FeatureToggles is null)
            {
                profile.FeatureToggles = new Models.FeatureToggles();
            }

            profile.FeatureToggles.ShowMusicMixer = ShowMusicMixerWidget;
            profile.FeatureToggles.ShowVisualizer = ShowVisualizerWidget;
            profile.FeatureToggles.ShowTransitionDesigner = ShowTransitionDesignerWidget;
            profile.FeatureToggles.ShowFertilizer = ShowFertilizerWidget;
            profile.FeatureToggles.ShowHostServer = ShowHostApiWidget;
            profile.FeatureToggles.ShowEventFX = ShowEventFXWidget;

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
