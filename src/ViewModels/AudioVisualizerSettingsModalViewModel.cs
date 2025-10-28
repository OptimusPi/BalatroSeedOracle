using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class AudioVisualizerSettingsModalViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;
        private System.Timers.Timer? _settingsSaveDebounce;
        private const int SettingsSaveDebounceMs = 250;

        // Total number of themes (8 built-in + 1 CUSTOMIZE option)
        private const int TotalThemeCount = 9;
        private const int CustomizeThemeIndex = 8;

        // Backing fields
        [ObservableProperty]
        private int _themeIndex;

        [ObservableProperty]
        private int _mainColor;

        [ObservableProperty]
        private int _accentColor;

        [ObservableProperty]
        private float _audioIntensity;

        [ObservableProperty]
        private float _parallaxStrength;

        [ObservableProperty]
        private float _timeSpeed;

        [ObservableProperty]
        private bool _seedFoundTrigger;

        [ObservableProperty]
        private bool _highScoreSeedTrigger;

        [ObservableProperty]
        private bool _searchCompleteTrigger;

        [ObservableProperty]
        private int _seedFoundAudioSource;

        [ObservableProperty]
        private int _highScoreAudioSource;

        [ObservableProperty]
        private int _searchCompleteAudioSource;

        // Audio source mappings for shader effects
        [ObservableProperty]
        private int _shadowFlickerSource;

        [ObservableProperty]
        private int _spinSource;

        [ObservableProperty]
        private int _twirlSource;

        [ObservableProperty]
        private int _zoomThumpSource;

        [ObservableProperty]
        private int _colorSaturationSource;

        [ObservableProperty]
        private int _beatPulseSource;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Intensity sliders for shader effects (0-100%)
        [ObservableProperty]
        private float _shadowFlickerIntensity = 50f;

        [ObservableProperty]
        private float _spinIntensity = 50f;

        [ObservableProperty]
        private float _twirlIntensity = 50f;

        [ObservableProperty]
        private float _zoomThumpIntensity = 50f;

        [ObservableProperty]
        private float _colorSaturationIntensity = 50f;

        [ObservableProperty]
        private float _beatPulseIntensity = 50f;

        // Beat detection threshold (NEW!)
        [ObservableProperty]
        private float _beatThreshold = 0.5f;

        // Per-track sensitivity for visualization (NEW!)
        [ObservableProperty]
        private float _drums1Sensitivity = 1.0f;

        [ObservableProperty]
        private float _drums2Sensitivity = 1.0f;

        [ObservableProperty]
        private float _bass1Sensitivity = 1.0f;

        [ObservableProperty]
        private float _bass2Sensitivity = 1.0f;

        [ObservableProperty]
        private float _chords1Sensitivity = 1.0f;

        [ObservableProperty]
        private float _chords2Sensitivity = 1.0f;

        [ObservableProperty]
        private float _melody1Sensitivity = 1.0f;

        [ObservableProperty]
        private float _melody2Sensitivity = 1.0f;

        // Vibe intensity multiplier (replaces magic 0.05f!)
        [ObservableProperty]
        private float _vibeIntensityMultiplier = 0.05f;

        // Display text fields
        [ObservableProperty]
        private string _audioIntensityLabel = "Off";

        [ObservableProperty]
        private string _audioIntensityNumeric = "0.0x";

        [ObservableProperty]
        private string _parallaxLabel = "Subtle";

        [ObservableProperty]
        private string _parallaxNumeric = "0.29x";

        [ObservableProperty]
        private string _timeSpeedLabel = "Normal";

        [ObservableProperty]
        private string _timeSpeedNumeric = "1.0x";

        // Preset management
        [ObservableProperty]
        private ObservableCollection<VisualizerPreset> _presets = new();

        [ObservableProperty]
        private VisualizerPreset? _selectedPreset;

        // Shader debug properties
        [ObservableProperty]
        private float _shaderContrast = 2.0f;

        [ObservableProperty]
        private float _shaderSpinAmount = 0.3f;

        [ObservableProperty]
        private float _shaderZoomPunch = 0.0f;

        [ObservableProperty]
        private float _shaderMelodySaturation = 0.0f;

        // Range settings for mapping audio into shader parameter ranges
        [ObservableProperty]
        private float _contrastRangeMin = 1.0f;

        [ObservableProperty]
        private float _contrastRangeMax = 4.0f;

        [ObservableProperty]
        private float _spinRangeMin = 0.0f;

        [ObservableProperty]
        private float _spinRangeMax = 1.0f;

        [ObservableProperty]
        private float _twirlRangeMin = 0.0f;

        [ObservableProperty]
        private float _twirlRangeMax = 0.5f;

        [ObservableProperty]
        private float _zoomPunchRangeMin = 0.0f;

        [ObservableProperty]
        private float _zoomPunchRangeMax = 10.0f;

        [ObservableProperty]
        private float _melodySatRangeMin = 0.0f;

        [ObservableProperty]
        private float _melodySatRangeMax = 1.0f;

        public AudioVisualizerSettingsModalViewModel()
        {
            _userProfileService =
                App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            // Load settings from profile
            LoadSettings();

            // Load existing presets
            LoadPresetsFromDisk();
        }

        #region Properties

        /// <summary>
        /// Returns true if the CUSTOMIZE theme is selected
        /// </summary>
        public bool IsCustomTheme => ThemeIndex == CustomizeThemeIndex;

        public string ShaderContrastText => $"{ShaderContrast:F1}";
        public string ShaderSpinAmountText => $"{ShaderSpinAmount:F2}";
        public string ShaderZoomPunchText => $"{ShaderZoomPunch:F2}";
        public string ShaderMelodySaturationText => $"{ShaderMelodySaturation:F2}";

        #endregion

        #region Range Events

        public event EventHandler<(float min, float max)>? ContrastRangeChangedEvent;
        public event EventHandler<(float min, float max)>? SpinRangeChangedEvent;
        public event EventHandler<(float min, float max)>? TwirlRangeChangedEvent;
        public event EventHandler<(float min, float max)>? ZoomPunchRangeChangedEvent;
        public event EventHandler<(float min, float max)>? MelodySatRangeChangedEvent;

        #endregion

        #region Generated Property Changed Methods

        partial void OnThemeIndexChanged(int value)
        {
            SaveTheme();
            ThemeChangedEvent?.Invoke(this, value);
            OnPropertyChanged(nameof(IsCustomTheme));
        }

        partial void OnMainColorChanged(int value)
        {
            SaveSettings();
            MainColorChangedEvent?.Invoke(this, value);
        }

        partial void OnAccentColorChanged(int value)
        {
            SaveSettings();
            AccentColorChangedEvent?.Invoke(this, value);
        }

        partial void OnAudioIntensityChanged(float value)
        {
            UpdateAudioIntensityLabels(value);
            SaveSettings();
            AudioIntensityChangedEvent?.Invoke(this, value);
        }

        partial void OnParallaxStrengthChanged(float value)
        {
            UpdateParallaxLabels(value);
            SaveSettings();
            ParallaxStrengthChangedEvent?.Invoke(this, value);
        }

        partial void OnTimeSpeedChanged(float value)
        {
            UpdateTimeSpeedLabels(value);
            SaveSettings();
            TimeSpeedChangedEvent?.Invoke(this, value);
        }

        partial void OnSeedFoundTriggerChanged(bool value)
        {
            SaveSettings();
        }

        partial void OnHighScoreSeedTriggerChanged(bool value)
        {
            SaveSettings();
        }

        partial void OnSearchCompleteTriggerChanged(bool value)
        {
            SaveSettings();
        }

        partial void OnSeedFoundAudioSourceChanged(int value)
        {
            SaveSettings();
        }

        partial void OnHighScoreAudioSourceChanged(int value)
        {
            SaveSettings();
        }

        partial void OnSearchCompleteAudioSourceChanged(int value)
        {
            SaveSettings();
        }

        partial void OnShadowFlickerSourceChanged(int value)
        {
            SaveSettings();
            ShadowFlickerSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnSpinSourceChanged(int value)
        {
            SaveSettings();
            SpinSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnTwirlSourceChanged(int value)
        {
            SaveSettings();
            TwirlSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnZoomThumpSourceChanged(int value)
        {
            SaveSettings();
            ZoomThumpSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnColorSaturationSourceChanged(int value)
        {
            SaveSettings();
            ColorSaturationSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnBeatPulseSourceChanged(int value)
        {
            SaveSettings();
            BeatPulseSourceChangedEvent?.Invoke(this, value);
        }

        partial void OnShadowFlickerIntensityChanged(float value)
        {
            SaveSettings();
            ShadowFlickerIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnSpinIntensityChanged(float value)
        {
            SaveSettings();
            SpinIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnTwirlIntensityChanged(float value)
        {
            SaveSettings();
            TwirlIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnZoomThumpIntensityChanged(float value)
        {
            SaveSettings();
            ZoomThumpIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnColorSaturationIntensityChanged(float value)
        {
            SaveSettings();
            ColorSaturationIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnBeatPulseIntensityChanged(float value)
        {
            SaveSettings();
            BeatPulseIntensityChangedEvent?.Invoke(this, value / 100f);
        }

        partial void OnShaderContrastChanged(float value)
        {
            OnPropertyChanged(nameof(ShaderContrastText));
            ShaderContrastChangedEvent?.Invoke(this, value);
        }

        partial void OnShaderSpinAmountChanged(float value)
        {
            OnPropertyChanged(nameof(ShaderSpinAmountText));
            ShaderSpinAmountChangedEvent?.Invoke(this, value);
        }

        partial void OnShaderZoomPunchChanged(float value)
        {
            OnPropertyChanged(nameof(ShaderZoomPunchText));
            ShaderZoomPunchChangedEvent?.Invoke(this, value);
        }

        partial void OnShaderMelodySaturationChanged(float value)
        {
            OnPropertyChanged(nameof(ShaderMelodySaturationText));
            ShaderMelodySaturationChangedEvent?.Invoke(this, value);
        }

        partial void OnContrastRangeMinChanged(float value)
        {
            ContrastRangeChangedEvent?.Invoke(this, (ContrastRangeMin, ContrastRangeMax));
            SaveSettings();
        }

        partial void OnContrastRangeMaxChanged(float value)
        {
            ContrastRangeChangedEvent?.Invoke(this, (ContrastRangeMin, ContrastRangeMax));
            SaveSettings();
        }

        partial void OnSpinRangeMinChanged(float value)
        {
            SpinRangeChangedEvent?.Invoke(this, (SpinRangeMin, SpinRangeMax));
            SaveSettings();
        }

        partial void OnSpinRangeMaxChanged(float value)
        {
            SpinRangeChangedEvent?.Invoke(this, (SpinRangeMin, SpinRangeMax));
            SaveSettings();
        }

        partial void OnTwirlRangeMinChanged(float value)
        {
            TwirlRangeChangedEvent?.Invoke(this, (TwirlRangeMin, TwirlRangeMax));
            SaveSettings();
        }

        partial void OnTwirlRangeMaxChanged(float value)
        {
            TwirlRangeChangedEvent?.Invoke(this, (TwirlRangeMin, TwirlRangeMax));
            SaveSettings();
        }

        partial void OnZoomPunchRangeMinChanged(float value)
        {
            ZoomPunchRangeChangedEvent?.Invoke(this, (ZoomPunchRangeMin, ZoomPunchRangeMax));
            SaveSettings();
        }

        partial void OnZoomPunchRangeMaxChanged(float value)
        {
            ZoomPunchRangeChangedEvent?.Invoke(this, (ZoomPunchRangeMin, ZoomPunchRangeMax));
            SaveSettings();
        }

        partial void OnMelodySatRangeMinChanged(float value)
        {
            MelodySatRangeChangedEvent?.Invoke(this, (MelodySatRangeMin, MelodySatRangeMax));
            SaveSettings();
        }

        partial void OnMelodySatRangeMaxChanged(float value)
        {
            MelodySatRangeChangedEvent?.Invoke(this, (MelodySatRangeMin, MelodySatRangeMax));
            SaveSettings();
        }

        #endregion

        #region Events

        public event EventHandler<int>? ThemeChangedEvent;
        public event EventHandler<int>? MainColorChangedEvent;
        public event EventHandler<int>? AccentColorChangedEvent;
        public event EventHandler<float>? AudioIntensityChangedEvent;
        public event EventHandler<float>? ParallaxStrengthChangedEvent;
        public event EventHandler<float>? TimeSpeedChangedEvent;

        // Shader effect audio source events
        public event EventHandler<int>? ShadowFlickerSourceChangedEvent;
        public event EventHandler<int>? SpinSourceChangedEvent;
        public event EventHandler<int>? TwirlSourceChangedEvent;
        public event EventHandler<int>? ZoomThumpSourceChangedEvent;
        public event EventHandler<int>? ColorSaturationSourceChangedEvent;
        public event EventHandler<int>? BeatPulseSourceChangedEvent;

        // Shader effect intensity events (0-1 range)
        public event EventHandler<float>? ShadowFlickerIntensityChangedEvent;
        public event EventHandler<float>? SpinIntensityChangedEvent;
        public event EventHandler<float>? TwirlIntensityChangedEvent;
        public event EventHandler<float>? ZoomThumpIntensityChangedEvent;
        public event EventHandler<float>? ColorSaturationIntensityChangedEvent;
        public event EventHandler<float>? BeatPulseIntensityChangedEvent;

        // Shader debug events
        public event EventHandler<float>? ShaderContrastChangedEvent;
        public event EventHandler<float>? ShaderSpinAmountChangedEvent;
        public event EventHandler<float>? ShaderZoomPunchChangedEvent;
        public event EventHandler<float>? ShaderMelodySaturationChangedEvent;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            var vibeSettings = profile.VisualizerSettings;

            // Load values from profile - use generated properties
            ThemeIndex = vibeSettings.ThemeIndex;
            MainColor = vibeSettings.MainColor;
            AccentColor = vibeSettings.AccentColor;
            AudioIntensity = vibeSettings.AudioIntensity;
            ParallaxStrength = vibeSettings.ParallaxStrength;
            TimeSpeed = vibeSettings.TimeSpeed;

            // Load trigger settings
            SeedFoundTrigger = vibeSettings.SeedFoundTrigger;
            HighScoreSeedTrigger = vibeSettings.HighScoreSeedTrigger;
            SearchCompleteTrigger = vibeSettings.SearchCompleteTrigger;
            SeedFoundAudioSource = vibeSettings.SeedFoundAudioSource;
            HighScoreAudioSource = vibeSettings.HighScoreAudioSource;
            SearchCompleteAudioSource = vibeSettings.SearchCompleteAudioSource;

            // Load shader effect audio sources
            ShadowFlickerSource = vibeSettings.ShadowFlickerSource;
            SpinSource = vibeSettings.SpinSource;
            TwirlSource = vibeSettings.TwirlSource;
            ZoomThumpSource = vibeSettings.ZoomThumpSource;
            ColorSaturationSource = vibeSettings.ColorSaturationSource;
            BeatPulseSource = vibeSettings.BeatPulseSource;

            // Load shader effect intensities
            ShadowFlickerIntensity = vibeSettings.ShadowFlickerIntensity;
            SpinIntensity = vibeSettings.SpinIntensity;
            TwirlIntensity = vibeSettings.TwirlIntensity;
            ZoomThumpIntensity = vibeSettings.ZoomThumpIntensity;
            ColorSaturationIntensity = vibeSettings.ColorSaturationIntensity;
            BeatPulseIntensity = vibeSettings.BeatPulseIntensity;

            DebugLogger.Log(
                "AudioVisualizerSettingsModalViewModel",
                $"Settings loaded - Theme: {ThemeIndex}, Audio: {AudioIntensity}"
            );
        }

        private void SaveSettings()
        {
            var profile = _userProfileService.GetProfile();
            var vibeSettings = profile.VisualizerSettings;

            vibeSettings.ThemeIndex = ThemeIndex;
            vibeSettings.MainColor = MainColor;
            vibeSettings.AccentColor = AccentColor;
            vibeSettings.AudioIntensity = AudioIntensity;
            vibeSettings.ParallaxStrength = ParallaxStrength;
            vibeSettings.TimeSpeed = TimeSpeed;
            vibeSettings.SeedFoundTrigger = SeedFoundTrigger;
            vibeSettings.HighScoreSeedTrigger = HighScoreSeedTrigger;
            vibeSettings.SearchCompleteTrigger = SearchCompleteTrigger;
            vibeSettings.SeedFoundAudioSource = SeedFoundAudioSource;
            vibeSettings.HighScoreAudioSource = HighScoreAudioSource;
            vibeSettings.SearchCompleteAudioSource = SearchCompleteAudioSource;
            vibeSettings.ShadowFlickerSource = ShadowFlickerSource;
            vibeSettings.SpinSource = SpinSource;
            vibeSettings.TwirlSource = TwirlSource;
            vibeSettings.ZoomThumpSource = ZoomThumpSource;
            vibeSettings.ColorSaturationSource = ColorSaturationSource;
            vibeSettings.BeatPulseSource = BeatPulseSource;
            vibeSettings.ShadowFlickerIntensity = ShadowFlickerIntensity;
            vibeSettings.SpinIntensity = SpinIntensity;
            vibeSettings.TwirlIntensity = TwirlIntensity;
            vibeSettings.ZoomThumpIntensity = ZoomThumpIntensity;
            vibeSettings.ColorSaturationIntensity = ColorSaturationIntensity;
            vibeSettings.BeatPulseIntensity = BeatPulseIntensity;

            ScheduleSettingsSave();
        }

        private void ScheduleSettingsSave()
        {
            if (_settingsSaveDebounce == null)
            {
                _settingsSaveDebounce = new System.Timers.Timer(SettingsSaveDebounceMs)
                {
                    AutoReset = false,
                };
                _settingsSaveDebounce.Elapsed += (s, e) =>
                {
                    try
                    {
                        // Save off UI thread to avoid freezes while dragging
                        Task.Run(() => _userProfileService.SaveProfile());
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "AudioVisualizerSettingsModalViewModel",
                            $"Failed to save settings: {ex.Message}"
                        );
                    }
                };
            }

            _settingsSaveDebounce.Stop();
            _settingsSaveDebounce.Start();
        }

        private void SaveTheme()
        {
            var profile = _userProfileService.GetProfile();
            profile.VisualizerSettings.ThemeIndex = ThemeIndex;
            _userProfileService.SaveProfile(profile);
            DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Theme saved: {ThemeIndex}");
        }

        private void UpdateAudioIntensityLabels(float value)
        {
            AudioIntensityNumeric = $"{value:F2}x";
            AudioIntensityLabel = value switch
            {
                <= 0.1f => "Off",
                <= 0.5f => "Calm",
                <= 1.0f => "Mild",
                <= 1.5f => "Strong",
                _ => "Wild",
            };
        }

        private void UpdateParallaxLabels(float value)
        {
            ParallaxNumeric = $"{value:F2}x";
            ParallaxLabel = value switch
            {
                <= 0.1f => "Off",
                <= 0.8f => "Subtle",
                <= 1.5f => "Medium",
                _ => "Strong",
            };
        }

        private void UpdateTimeSpeedLabels(float value)
        {
            TimeSpeedNumeric = $"{value:F2}x";
            TimeSpeedLabel = value switch
            {
                <= 0.1f => "Frozen",
                <= 0.7f => "Slow",
                <= 1.3f => "Normal",
                <= 2.0f => "Fast",
                _ => "Hyper",
            };
        }

        #endregion

        #region Preset Management

        /// <summary>
        /// Loads all presets from disk
        /// </summary>
        private void LoadPresetsFromDisk()
        {
            try
            {
                var loadedPresets = PresetHelper.LoadAllPresets();
                Presets.Clear();
                foreach (var preset in loadedPresets)
                {
                    Presets.Add(preset);
                }
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Loaded {Presets.Count} presets"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Error loading presets: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Saves the current settings as a new preset
        /// </summary>
        [RelayCommand]
        private async Task SavePreset()
        {
            try
            {
                // Show input dialog to get preset name
                var presetName = await ShowPresetNameDialog();
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    DebugLogger.Log(
                        "AudioVisualizerSettingsModalViewModel",
                        "Save preset cancelled - no name provided"
                    );
                    return;
                }

                // Check if name already exists
                if (PresetHelper.PresetNameExists(presetName))
                {
                    ErrorMessage =
                        $"Preset '{presetName}' already exists. Please choose a different name.";
                    return;
                }

                // Clear any previous error
                ErrorMessage = string.Empty;

                // Create new preset from current settings
                var preset = new VisualizerPreset
                {
                    Name = presetName,
                    ThemeIndex = ThemeIndex,
                    MainColor = MainColor,
                    AccentColor = AccentColor,
                    AudioIntensity = AudioIntensity,
                    ParallaxStrength = ParallaxStrength,
                    TimeSpeed = TimeSpeed,
                    SeedFoundTrigger = SeedFoundTrigger,
                    HighScoreSeedTrigger = HighScoreSeedTrigger,
                    SearchCompleteTrigger = SearchCompleteTrigger,
                    SeedFoundAudioSource = SeedFoundAudioSource,
                    HighScoreAudioSource = HighScoreAudioSource,
                    SearchCompleteAudioSource = SearchCompleteAudioSource,
                };

                // Save to disk
                if (PresetHelper.SavePreset(preset))
                {
                    Presets.Add(preset);
                    DebugLogger.Log(
                        "AudioVisualizerSettingsModalViewModel",
                        $"Saved preset '{presetName}'"
                    );
                }
                else
                {
                    DebugLogger.Log(
                        "AudioVisualizerSettingsModalViewModel",
                        $"Failed to save preset '{presetName}'"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Error saving preset: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Loads a preset and applies its settings
        /// </summary>
        [RelayCommand]
        private void LoadPreset(VisualizerPreset? preset)
        {
            if (preset == null)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", "Cannot load null preset");
                return;
            }

            try
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Loading preset '{preset.Name}'"
                );

                // Apply preset settings
                ThemeIndex = preset.ThemeIndex;
                MainColor = preset.MainColor ?? 8;
                AccentColor = preset.AccentColor ?? 8;
                AudioIntensity = preset.AudioIntensity;
                ParallaxStrength = preset.ParallaxStrength;
                TimeSpeed = preset.TimeSpeed;
                SeedFoundTrigger = preset.SeedFoundTrigger;
                HighScoreSeedTrigger = preset.HighScoreSeedTrigger;
                SearchCompleteTrigger = preset.SearchCompleteTrigger;
                SeedFoundAudioSource = preset.SeedFoundAudioSource;
                HighScoreAudioSource = preset.HighScoreAudioSource;
                SearchCompleteAudioSource = preset.SearchCompleteAudioSource;

                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Loaded preset '{preset.Name}' successfully"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Error loading preset: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Deletes a preset from disk and the collection
        /// </summary>
        [RelayCommand]
        private void DeletePreset(VisualizerPreset? preset)
        {
            if (preset == null)
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    "Cannot delete null preset"
                );
                return;
            }

            try
            {
                if (PresetHelper.DeletePreset(preset))
                {
                    Presets.Remove(preset);
                    DebugLogger.Log(
                        "AudioVisualizerSettingsModalViewModel",
                        $"Deleted preset '{preset.Name}'"
                    );
                }
                else
                {
                    DebugLogger.Log(
                        "AudioVisualizerSettingsModalViewModel",
                        $"Failed to delete preset '{preset.Name}'"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Error deleting preset: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Shows a dialog to get preset name from user
        /// </summary>
        private async System.Threading.Tasks.Task<string?> ShowPresetNameDialog()
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Save Preset",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#0D0D0D")
                    ),
                };

                var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };

                var label = new TextBlock
                {
                    Text = "Enter a name for this preset:",
                    Foreground = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#FFD700")
                    ),
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    FontSize = 14,
                };

                var textBox = new TextBox
                {
                    Watermark = "My Awesome Preset",
                    FontSize = 14,
                    Padding = new Avalonia.Thickness(10),
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 100,
                    Padding = new Avalonia.Thickness(10, 5),
                };

                var saveButton = new Button
                {
                    Content = "Save",
                    Width = 100,
                    Padding = new Avalonia.Thickness(10, 5),
                    Background = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#FFD700")
                    ),
                    Foreground = new Avalonia.Media.SolidColorBrush(
                        Avalonia.Media.Color.Parse("#0D0D0D")
                    ),
                };

                string? result = null;

                cancelButton.Click += (s, e) => dialog.Close();
                saveButton.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        result = textBox.Text;
                        dialog.Close();
                    }
                };

                // Enter key saves
                textBox.KeyDown += (s, e) =>
                {
                    if (
                        e.Key == Avalonia.Input.Key.Enter
                        && !string.IsNullOrWhiteSpace(textBox.Text)
                    )
                    {
                        result = textBox.Text;
                        dialog.Close();
                    }
                    else if (e.Key == Avalonia.Input.Key.Escape)
                    {
                        dialog.Close();
                    }
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(saveButton);

                panel.Children.Add(label);
                panel.Children.Add(textBox);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;

                // Focus the textbox when dialog opens
                dialog.Opened += (s, e) => textBox.Focus();

                var owner = Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (owner != null)
                {
                    await dialog.ShowDialog(owner);
                }

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerSettingsModalViewModel",
                    $"Failed to show preset name dialog: {ex.Message}"
                );
                return $"Preset_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }

        #endregion
    }
}
