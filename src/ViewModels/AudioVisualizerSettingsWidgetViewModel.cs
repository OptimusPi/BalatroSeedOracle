using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for AudioVisualizerSettingsWidget - a movable, minimizable widget
    /// Wraps the existing AudioVisualizerSettingsModalViewModel for all settings logic
    /// </summary>
    public partial class AudioVisualizerSettingsWidgetViewModel : BaseWidgetViewModel
    {
        private readonly AudioVisualizerSettingsModalViewModel _settingsViewModel;

        public AudioVisualizerSettingsWidgetViewModel()
        {
            // Create the underlying settings ViewModel (does all the heavy lifting)
            _settingsViewModel = new AudioVisualizerSettingsModalViewModel();

            // Configure base widget properties
            WidgetTitle = "Audio Visualizer";
            WidgetIcon = "🎵";
            IsMinimized = true; // Start minimized

            // Position below Genie widget
            PositionX = 20;
            PositionY = 130;
        }

        #region Delegate Properties to Underlying ViewModel

        // Theme
        public int ThemeIndex
        {
            get => _settingsViewModel.ThemeIndex;
            set => _settingsViewModel.ThemeIndex = value;
        }

        public bool IsCustomTheme => _settingsViewModel.IsCustomTheme;

        public int MainColor
        {
            get => _settingsViewModel.MainColor;
            set => _settingsViewModel.MainColor = value;
        }

        public int AccentColor
        {
            get => _settingsViewModel.AccentColor;
            set => _settingsViewModel.AccentColor = value;
        }

        // Intensity Sliders
        public float AudioIntensity
        {
            get => _settingsViewModel.AudioIntensity;
            set => _settingsViewModel.AudioIntensity = value;
        }

        public string AudioIntensityNumeric => _settingsViewModel.AudioIntensityNumeric;

        public float ParallaxStrength
        {
            get => _settingsViewModel.ParallaxStrength;
            set => _settingsViewModel.ParallaxStrength = value;
        }

        public string ParallaxNumeric => _settingsViewModel.ParallaxNumeric;

        public float TimeSpeed
        {
            get => _settingsViewModel.TimeSpeed;
            set => _settingsViewModel.TimeSpeed = value;
        }

        public string TimeSpeedNumeric => _settingsViewModel.TimeSpeedNumeric;

        // Audio Event Triggers
        public bool SeedFoundTrigger
        {
            get => _settingsViewModel.SeedFoundTrigger;
            set => _settingsViewModel.SeedFoundTrigger = value;
        }

        public bool HighScoreSeedTrigger
        {
            get => _settingsViewModel.HighScoreSeedTrigger;
            set => _settingsViewModel.HighScoreSeedTrigger = value;
        }

        public bool SearchCompleteTrigger
        {
            get => _settingsViewModel.SearchCompleteTrigger;
            set => _settingsViewModel.SearchCompleteTrigger = value;
        }

        // Shader Effect Mappings
        public int ShadowFlickerSource
        {
            get => _settingsViewModel.ShadowFlickerSource;
            set => _settingsViewModel.ShadowFlickerSource = value;
        }

        public int SpinSource
        {
            get => _settingsViewModel.SpinSource;
            set => _settingsViewModel.SpinSource = value;
        }

        public int BeatPulseSource
        {
            get => _settingsViewModel.BeatPulseSource;
            set => _settingsViewModel.BeatPulseSource = value;
        }

        // Beat Detection & Sensitivity
        public float BeatThreshold
        {
            get => _settingsViewModel.BeatThreshold;
            set => _settingsViewModel.BeatThreshold = value;
        }

        public float VibeIntensityMultiplier
        {
            get => _settingsViewModel.VibeIntensityMultiplier;
            set => _settingsViewModel.VibeIntensityMultiplier = value;
        }

        public float Drums1Sensitivity
        {
            get => _settingsViewModel.Drums1Sensitivity;
            set => _settingsViewModel.Drums1Sensitivity = value;
        }

        public float Drums2Sensitivity
        {
            get => _settingsViewModel.Drums2Sensitivity;
            set => _settingsViewModel.Drums2Sensitivity = value;
        }

        public float Bass1Sensitivity
        {
            get => _settingsViewModel.Bass1Sensitivity;
            set => _settingsViewModel.Bass1Sensitivity = value;
        }

        public float Bass2Sensitivity
        {
            get => _settingsViewModel.Bass2Sensitivity;
            set => _settingsViewModel.Bass2Sensitivity = value;
        }

        public float Chords1Sensitivity
        {
            get => _settingsViewModel.Chords1Sensitivity;
            set => _settingsViewModel.Chords1Sensitivity = value;
        }

        public float Chords2Sensitivity
        {
            get => _settingsViewModel.Chords2Sensitivity;
            set => _settingsViewModel.Chords2Sensitivity = value;
        }

        public float Melody1Sensitivity
        {
            get => _settingsViewModel.Melody1Sensitivity;
            set => _settingsViewModel.Melody1Sensitivity = value;
        }

        public float Melody2Sensitivity
        {
            get => _settingsViewModel.Melody2Sensitivity;
            set => _settingsViewModel.Melody2Sensitivity = value;
        }

        // Shader Effect Intensities
        public float ShadowFlickerIntensity
        {
            get => _settingsViewModel.ShadowFlickerIntensity;
            set => _settingsViewModel.ShadowFlickerIntensity = value;
        }

        public float SpinIntensity
        {
            get => _settingsViewModel.SpinIntensity;
            set => _settingsViewModel.SpinIntensity = value;
        }

        public float BeatPulseIntensity
        {
            get => _settingsViewModel.BeatPulseIntensity;
            set => _settingsViewModel.BeatPulseIntensity = value;
        }

        // Presets
        public System.Collections.ObjectModel.ObservableCollection<Models.VisualizerPreset> Presets => _settingsViewModel.Presets;

        public Models.VisualizerPreset? SelectedPreset
        {
            get => _settingsViewModel.SelectedPreset;
            set => _settingsViewModel.SelectedPreset = value;
        }

        public System.Windows.Input.ICommand LoadPresetCommand => _settingsViewModel.LoadPresetCommand;
        public System.Windows.Input.ICommand SavePresetCommand => _settingsViewModel.SavePresetCommand;
        public System.Windows.Input.ICommand DeletePresetCommand => _settingsViewModel.DeletePresetCommand;

        #endregion

        #region Widget-Specific Commands

        [RelayCommand]
        private void ToggleMinimize()
        {
            IsMinimized = !IsMinimized;
        }

        #endregion

        #region Base Widget Overrides

        protected override void OnExpanded()
        {
            base.OnExpanded();
            // Could load settings fresh when expanded if needed
        }

        protected override void OnMinimized()
        {
            base.OnMinimized();
            // Could auto-save when minimized if needed
        }

        #endregion

        #region Lifecycle

        public void Initialize()
        {
            // Wire up property change notifications from underlying ViewModel
            _settingsViewModel.PropertyChanged += (s, e) =>
            {
                // Propagate property changes from the settings ViewModel
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);
                }
            };
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        #endregion
    }
}
