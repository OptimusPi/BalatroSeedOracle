using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;
using Avalonia.Controls;

namespace BalatroSeedOracle.ViewModels
{
    public class AudioVisualizerSettingsModalViewModel : BaseViewModel
    {
        private readonly UserProfileService _userProfileService;

        // Total number of themes (8 built-in + 1 CUSTOMIZE option)
        private const int TotalThemeCount = 9;
        private const int CustomizeThemeIndex = 8;

        // Backing fields
        private int _themeIndex;
        private int _mainColor;
        private int _accentColor;
        private float _audioIntensity;
        private float _parallaxStrength;
        private float _timeSpeed;
        private bool _seedFoundTrigger;
        private bool _highScoreSeedTrigger;
        private bool _searchCompleteTrigger;
        private int _seedFoundAudioSource;
        private int _highScoreAudioSource;
        private int _searchCompleteAudioSource;

        // Audio source mappings for shader effects
        private int _shadowFlickerSource;
        private int _spinSource;
        private int _twirlSource;
        private int _zoomThumpSource;
        private int _colorSaturationSource;
        private int _beatPulseSource;

        // Intensity sliders for shader effects (0-100%)
        private float _shadowFlickerIntensity = 50f;
        private float _spinIntensity = 50f;
        private float _twirlIntensity = 50f;
        private float _zoomThumpIntensity = 50f;
        private float _colorSaturationIntensity = 50f;
        private float _beatPulseIntensity = 50f;

        // Display text fields
        private string _audioIntensityLabel = "Off";
        private string _audioIntensityNumeric = "0.0x";
        private string _parallaxLabel = "Subtle";
        private string _parallaxNumeric = "0.29x";
        private string _timeSpeedLabel = "Normal";
        private string _timeSpeedNumeric = "1.0x";

        // Preset management
        private ObservableCollection<VisualizerPreset> _presets = new();
        private VisualizerPreset? _selectedPreset;

        public AudioVisualizerSettingsModalViewModel()
        {
            _userProfileService = App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            // Initialize commands
            SavePresetCommand = new RelayCommand(SavePreset);
            LoadPresetCommand = new RelayCommand<VisualizerPreset>(LoadPreset);
            DeletePresetCommand = new RelayCommand<VisualizerPreset>(DeletePreset);

            // Load settings from profile
            LoadSettings();

            // Load existing presets
            LoadPresetsFromDisk();
        }

        #region Properties

        public int ThemeIndex
        {
            get => _themeIndex;
            set
            {
                if (SetProperty(ref _themeIndex, value))
                {
                    SaveTheme();
                    OnThemeChanged?.Invoke(this, value);
                    // Notify IsCustomTheme changed when theme changes
                    OnPropertyChanged(nameof(IsCustomTheme));
                }
            }
        }

        /// <summary>
        /// Returns true if the CUSTOMIZE theme is selected
        /// </summary>
        public bool IsCustomTheme => ThemeIndex == CustomizeThemeIndex;

        public int MainColor
        {
            get => _mainColor;
            set
            {
                if (SetProperty(ref _mainColor, value))
                {
                    SaveSettings();
                    OnMainColorChanged?.Invoke(this, value);
                }
            }
        }

        public int AccentColor
        {
            get => _accentColor;
            set
            {
                if (SetProperty(ref _accentColor, value))
                {
                    SaveSettings();
                    OnAccentColorChanged?.Invoke(this, value);
                }
            }
        }

        public float AudioIntensity
        {
            get => _audioIntensity;
            set
            {
                if (SetProperty(ref _audioIntensity, value))
                {
                    UpdateAudioIntensityLabels(value);
                    SaveSettings();
                    OnAudioIntensityChanged?.Invoke(this, value);
                }
            }
        }

        public float ParallaxStrength
        {
            get => _parallaxStrength;
            set
            {
                if (SetProperty(ref _parallaxStrength, value))
                {
                    UpdateParallaxLabels(value);
                    SaveSettings();
                    OnParallaxStrengthChanged?.Invoke(this, value);
                }
            }
        }

        public float TimeSpeed
        {
            get => _timeSpeed;
            set
            {
                if (SetProperty(ref _timeSpeed, value))
                {
                    UpdateTimeSpeedLabels(value);
                    SaveSettings();
                    OnTimeSpeedChanged?.Invoke(this, value);
                }
            }
        }

        // CheckBox properties
        public bool SeedFoundTrigger
        {
            get => _seedFoundTrigger;
            set
            {
                if (SetProperty(ref _seedFoundTrigger, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool HighScoreSeedTrigger
        {
            get => _highScoreSeedTrigger;
            set
            {
                if (SetProperty(ref _highScoreSeedTrigger, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool SearchCompleteTrigger
        {
            get => _searchCompleteTrigger;
            set
            {
                if (SetProperty(ref _searchCompleteTrigger, value))
                {
                    SaveSettings();
                }
            }
        }

        // Audio source properties
        public int SeedFoundAudioSource
        {
            get => _seedFoundAudioSource;
            set
            {
                if (SetProperty(ref _seedFoundAudioSource, value))
                {
                    SaveSettings();
                }
            }
        }

        public int HighScoreAudioSource
        {
            get => _highScoreAudioSource;
            set
            {
                if (SetProperty(ref _highScoreAudioSource, value))
                {
                    SaveSettings();
                }
            }
        }

        public int SearchCompleteAudioSource
        {
            get => _searchCompleteAudioSource;
            set
            {
                if (SetProperty(ref _searchCompleteAudioSource, value))
                {
                    SaveSettings();
                }
            }
        }

        public int ShadowFlickerSource
        {
            get => _shadowFlickerSource;
            set
            {
                if (SetProperty(ref _shadowFlickerSource, value))
                {
                    SaveSettings();
                    OnShadowFlickerSourceChanged?.Invoke(this, value);
                }
            }
        }

        public int SpinSource
        {
            get => _spinSource;
            set
            {
                if (SetProperty(ref _spinSource, value))
                {
                    SaveSettings();
                    OnSpinSourceChanged?.Invoke(this, value);
                }
            }
        }

        public int TwirlSource
        {
            get => _twirlSource;
            set
            {
                if (SetProperty(ref _twirlSource, value))
                {
                    SaveSettings();
                    OnTwirlSourceChanged?.Invoke(this, value);
                }
            }
        }

        public int ZoomThumpSource
        {
            get => _zoomThumpSource;
            set
            {
                if (SetProperty(ref _zoomThumpSource, value))
                {
                    SaveSettings();
                    OnZoomThumpSourceChanged?.Invoke(this, value);
                }
            }
        }

        public int ColorSaturationSource
        {
            get => _colorSaturationSource;
            set
            {
                if (SetProperty(ref _colorSaturationSource, value))
                {
                    SaveSettings();
                    OnColorSaturationSourceChanged?.Invoke(this, value);
                }
            }
        }

        public int BeatPulseSource
        {
            get => _beatPulseSource;
            set
            {
                if (SetProperty(ref _beatPulseSource, value))
                {
                    SaveSettings();
                    OnBeatPulseSourceChanged?.Invoke(this, value);
                }
            }
        }

        // Intensity properties for shader effects
        public float ShadowFlickerIntensity
        {
            get => _shadowFlickerIntensity;
            set
            {
                if (SetProperty(ref _shadowFlickerIntensity, value))
                {
                    SaveSettings();
                    OnShadowFlickerIntensityChanged?.Invoke(this, value / 100f); // Convert to 0-1 range
                }
            }
        }

        public float SpinIntensity
        {
            get => _spinIntensity;
            set
            {
                if (SetProperty(ref _spinIntensity, value))
                {
                    SaveSettings();
                    OnSpinIntensityChanged?.Invoke(this, value / 100f);
                }
            }
        }

        public float TwirlIntensity
        {
            get => _twirlIntensity;
            set
            {
                if (SetProperty(ref _twirlIntensity, value))
                {
                    SaveSettings();
                    OnTwirlIntensityChanged?.Invoke(this, value / 100f);
                }
            }
        }

        public float ZoomThumpIntensity
        {
            get => _zoomThumpIntensity;
            set
            {
                if (SetProperty(ref _zoomThumpIntensity, value))
                {
                    SaveSettings();
                    OnZoomThumpIntensityChanged?.Invoke(this, value / 100f);
                }
            }
        }

        public float ColorSaturationIntensity
        {
            get => _colorSaturationIntensity;
            set
            {
                if (SetProperty(ref _colorSaturationIntensity, value))
                {
                    SaveSettings();
                    OnColorSaturationIntensityChanged?.Invoke(this, value / 100f);
                }
            }
        }

        public float BeatPulseIntensity
        {
            get => _beatPulseIntensity;
            set
            {
                if (SetProperty(ref _beatPulseIntensity, value))
                {
                    SaveSettings();
                    OnBeatPulseIntensityChanged?.Invoke(this, value / 100f);
                }
            }
        }

        // Display label properties
        public string AudioIntensityLabel
        {
            get => _audioIntensityLabel;
            set => SetProperty(ref _audioIntensityLabel, value);
        }

        public string AudioIntensityNumeric
        {
            get => _audioIntensityNumeric;
            set => SetProperty(ref _audioIntensityNumeric, value);
        }

        public string ParallaxLabel
        {
            get => _parallaxLabel;
            set => SetProperty(ref _parallaxLabel, value);
        }

        public string ParallaxNumeric
        {
            get => _parallaxNumeric;
            set => SetProperty(ref _parallaxNumeric, value);
        }

        public string TimeSpeedLabel
        {
            get => _timeSpeedLabel;
            set => SetProperty(ref _timeSpeedLabel, value);
        }

        public string TimeSpeedNumeric
        {
            get => _timeSpeedNumeric;
            set => SetProperty(ref _timeSpeedNumeric, value);
        }

        /// <summary>
        /// Collection of saved presets
        /// </summary>
        public ObservableCollection<VisualizerPreset> Presets
        {
            get => _presets;
            set => SetProperty(ref _presets, value);
        }

        /// <summary>
        /// Currently selected preset (for loading)
        /// </summary>
        public VisualizerPreset? SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }

        // 🐛 SHADER DEBUG PROPERTIES (Advanced users only!)
        private float _shaderContrast = 2.0f;
        private float _shaderSpinAmount = 0.3f;
        private float _shaderZoomPunch = 0.0f;
        private float _shaderMelodySaturation = 0.0f;

        public float ShaderContrast
        {
            get => _shaderContrast;
            set
            {
                if (SetProperty(ref _shaderContrast, value))
                {
                    OnPropertyChanged(nameof(ShaderContrastText));
                    OnShaderContrastChanged?.Invoke(this, value);
                }
            }
        }

        public string ShaderContrastText => $"{_shaderContrast:F1}";

        public float ShaderSpinAmount
        {
            get => _shaderSpinAmount;
            set
            {
                if (SetProperty(ref _shaderSpinAmount, value))
                {
                    OnPropertyChanged(nameof(ShaderSpinAmountText));
                    OnShaderSpinAmountChanged?.Invoke(this, value);
                }
            }
        }

        public string ShaderSpinAmountText => $"{_shaderSpinAmount:F2}";

        public float ShaderZoomPunch
        {
            get => _shaderZoomPunch;
            set
            {
                if (SetProperty(ref _shaderZoomPunch, value))
                {
                    OnPropertyChanged(nameof(ShaderZoomPunchText));
                    OnShaderZoomPunchChanged?.Invoke(this, value);
                }
            }
        }

        public string ShaderZoomPunchText => $"{_shaderZoomPunch:F2}";

        public float ShaderMelodySaturation
        {
            get => _shaderMelodySaturation;
            set
            {
                if (SetProperty(ref _shaderMelodySaturation, value))
                {
                    OnPropertyChanged(nameof(ShaderMelodySaturationText));
                    OnShaderMelodySaturationChanged?.Invoke(this, value);
                }
            }
        }

        public string ShaderMelodySaturationText => $"{_shaderMelodySaturation:F2}";

        #endregion

        #region Commands

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }

        #endregion

        #region Events

        public event EventHandler<int>? OnThemeChanged;
        public event EventHandler<int>? OnMainColorChanged;
        public event EventHandler<int>? OnAccentColorChanged;
        public event EventHandler<float>? OnAudioIntensityChanged;
        public event EventHandler<float>? OnParallaxStrengthChanged;
        public event EventHandler<float>? OnTimeSpeedChanged;

        // Shader effect audio source events
        public event EventHandler<int>? OnShadowFlickerSourceChanged;
        public event EventHandler<int>? OnSpinSourceChanged;
        public event EventHandler<int>? OnTwirlSourceChanged;
        public event EventHandler<int>? OnZoomThumpSourceChanged;
        public event EventHandler<int>? OnColorSaturationSourceChanged;
        public event EventHandler<int>? OnBeatPulseSourceChanged;

        // Shader effect intensity events (0-1 range)
        public event EventHandler<float>? OnShadowFlickerIntensityChanged;
        public event EventHandler<float>? OnSpinIntensityChanged;
        public event EventHandler<float>? OnTwirlIntensityChanged;
        public event EventHandler<float>? OnZoomThumpIntensityChanged;
        public event EventHandler<float>? OnColorSaturationIntensityChanged;
        public event EventHandler<float>? OnBeatPulseIntensityChanged;

        // Shader debug events
        public event EventHandler<float>? OnShaderContrastChanged;
        public event EventHandler<float>? OnShaderSpinAmountChanged;
        public event EventHandler<float>? OnShaderZoomPunchChanged;
        public event EventHandler<float>? OnShaderMelodySaturationChanged;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            var vibeSettings = profile.VibeOutSettings;

            // Load values from profile
            _themeIndex = vibeSettings.ThemeIndex;
            _mainColor = vibeSettings.MainColor;
            _accentColor = vibeSettings.AccentColor;
            _audioIntensity = vibeSettings.AudioIntensity;
            _parallaxStrength = vibeSettings.ParallaxStrength;
            _timeSpeed = vibeSettings.TimeSpeed;

            // Load trigger settings
            _seedFoundTrigger = vibeSettings.SeedFoundTrigger;
            _highScoreSeedTrigger = vibeSettings.HighScoreSeedTrigger;
            _searchCompleteTrigger = vibeSettings.SearchCompleteTrigger;
            _seedFoundAudioSource = vibeSettings.SeedFoundAudioSource;
            _highScoreAudioSource = vibeSettings.HighScoreAudioSource;
            _searchCompleteAudioSource = vibeSettings.SearchCompleteAudioSource;

            // Load shader effect audio sources
            _shadowFlickerSource = vibeSettings.ShadowFlickerSource;
            _spinSource = vibeSettings.SpinSource;
            _twirlSource = vibeSettings.TwirlSource;
            _zoomThumpSource = vibeSettings.ZoomThumpSource;
            _colorSaturationSource = vibeSettings.ColorSaturationSource;
            _beatPulseSource = vibeSettings.BeatPulseSource;

            // Load shader effect intensities
            _shadowFlickerIntensity = vibeSettings.ShadowFlickerIntensity;
            _spinIntensity = vibeSettings.SpinIntensity;
            _twirlIntensity = vibeSettings.TwirlIntensity;
            _zoomThumpIntensity = vibeSettings.ZoomThumpIntensity;
            _colorSaturationIntensity = vibeSettings.ColorSaturationIntensity;
            _beatPulseIntensity = vibeSettings.BeatPulseIntensity;

            // Update display labels
            UpdateAudioIntensityLabels(_audioIntensity);
            UpdateParallaxLabels(_parallaxStrength);
            UpdateTimeSpeedLabels(_timeSpeed);

            // Notify all properties changed
            OnPropertyChanged(nameof(ThemeIndex));
            OnPropertyChanged(nameof(MainColor));
            OnPropertyChanged(nameof(AccentColor));
            OnPropertyChanged(nameof(AudioIntensity));
            OnPropertyChanged(nameof(ParallaxStrength));
            OnPropertyChanged(nameof(TimeSpeed));
            OnPropertyChanged(nameof(SeedFoundTrigger));
            OnPropertyChanged(nameof(HighScoreSeedTrigger));
            OnPropertyChanged(nameof(SearchCompleteTrigger));
            OnPropertyChanged(nameof(SeedFoundAudioSource));
            OnPropertyChanged(nameof(HighScoreAudioSource));
            OnPropertyChanged(nameof(SearchCompleteAudioSource));
            OnPropertyChanged(nameof(ShadowFlickerSource));
            OnPropertyChanged(nameof(SpinSource));
            OnPropertyChanged(nameof(TwirlSource));
            OnPropertyChanged(nameof(ZoomThumpSource));
            OnPropertyChanged(nameof(ColorSaturationSource));
            OnPropertyChanged(nameof(BeatPulseSource));
            OnPropertyChanged(nameof(ShadowFlickerIntensity));
            OnPropertyChanged(nameof(SpinIntensity));
            OnPropertyChanged(nameof(TwirlIntensity));
            OnPropertyChanged(nameof(ZoomThumpIntensity));
            OnPropertyChanged(nameof(ColorSaturationIntensity));
            OnPropertyChanged(nameof(BeatPulseIntensity));
            OnPropertyChanged(nameof(IsCustomTheme));

            DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Settings loaded - Theme: {_themeIndex}, Audio: {_audioIntensity}");
        }

        private void SaveSettings()
        {
            var profile = _userProfileService.GetProfile();
            var vibeSettings = profile.VibeOutSettings;

            vibeSettings.ThemeIndex = _themeIndex;
            vibeSettings.MainColor = _mainColor;
            vibeSettings.AccentColor = _accentColor;
            vibeSettings.AudioIntensity = _audioIntensity;
            vibeSettings.ParallaxStrength = _parallaxStrength;
            vibeSettings.TimeSpeed = _timeSpeed;
            vibeSettings.SeedFoundTrigger = _seedFoundTrigger;
            vibeSettings.HighScoreSeedTrigger = _highScoreSeedTrigger;
            vibeSettings.SearchCompleteTrigger = _searchCompleteTrigger;
            vibeSettings.SeedFoundAudioSource = _seedFoundAudioSource;
            vibeSettings.HighScoreAudioSource = _highScoreAudioSource;
            vibeSettings.SearchCompleteAudioSource = _searchCompleteAudioSource;
            vibeSettings.ShadowFlickerSource = _shadowFlickerSource;
            vibeSettings.SpinSource = _spinSource;
            vibeSettings.TwirlSource = _twirlSource;
            vibeSettings.ZoomThumpSource = _zoomThumpSource;
            vibeSettings.ColorSaturationSource = _colorSaturationSource;
            vibeSettings.BeatPulseSource = _beatPulseSource;
            vibeSettings.ShadowFlickerIntensity = _shadowFlickerIntensity;
            vibeSettings.SpinIntensity = _spinIntensity;
            vibeSettings.TwirlIntensity = _twirlIntensity;
            vibeSettings.ZoomThumpIntensity = _zoomThumpIntensity;
            vibeSettings.ColorSaturationIntensity = _colorSaturationIntensity;
            vibeSettings.BeatPulseIntensity = _beatPulseIntensity;

            _userProfileService.SaveProfile(profile);
        }

        private void SaveTheme()
        {
            var profile = _userProfileService.GetProfile();
            profile.VibeOutSettings.ThemeIndex = _themeIndex;
            _userProfileService.SaveProfile(profile);
            DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Theme saved: {_themeIndex}");
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
                _ => "Wild"
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
                _ => "Strong"
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
                _ => "Hyper"
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
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Loaded {Presets.Count} presets");
            }
            catch (Exception ex)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Error loading presets: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current settings as a new preset
        /// </summary>
        private async void SavePreset()
        {
            try
            {
                // Show input dialog to get preset name
                var presetName = await ShowPresetNameDialog();
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", "Save preset cancelled - no name provided");
                    return;
                }

                // Check if name already exists
                if (PresetHelper.PresetNameExists(presetName))
                {
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Preset name '{presetName}' already exists");
                    // TODO: Show error message to user
                    return;
                }

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
                    SearchCompleteAudioSource = SearchCompleteAudioSource
                };

                // Save to disk
                if (PresetHelper.SavePreset(preset))
                {
                    Presets.Add(preset);
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Saved preset '{presetName}'");
                }
                else
                {
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Failed to save preset '{presetName}'");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Error saving preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a preset and applies its settings
        /// </summary>
        private void LoadPreset(VisualizerPreset? preset)
        {
            if (preset == null)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", "Cannot load null preset");
                return;
            }

            try
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Loading preset '{preset.Name}'");

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

                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Loaded preset '{preset.Name}' successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Error loading preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a preset from disk and the collection
        /// </summary>
        private void DeletePreset(VisualizerPreset? preset)
        {
            if (preset == null)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", "Cannot delete null preset");
                return;
            }

            try
            {
                if (PresetHelper.DeletePreset(preset))
                {
                    Presets.Remove(preset);
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Deleted preset '{preset.Name}'");
                }
                else
                {
                    DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Failed to delete preset '{preset.Name}'");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("AudioVisualizerSettingsModalViewModel", $"Error deleting preset: {ex.Message}");
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
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0D0D0D"))
                };

                var panel = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15
                };

                var label = new TextBlock
                {
                    Text = "Enter a name for this preset:",
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFD700")),
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    FontSize = 14
                };

                var textBox = new TextBox
                {
                    Watermark = "My Awesome Preset",
                    FontSize = 14,
                    Padding = new Avalonia.Thickness(10)
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 100,
                    Padding = new Avalonia.Thickness(10, 5)
                };

                var saveButton = new Button
                {
                    Content = "Save",
                    Width = 100,
                    Padding = new Avalonia.Thickness(10, 5),
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFD700")),
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0D0D0D"))
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
                    if (e.Key == Avalonia.Input.Key.Enter && !string.IsNullOrWhiteSpace(textBox.Text))
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

                await dialog.ShowDialog(Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null);

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerSettingsModalViewModel", $"Failed to show preset name dialog: {ex.Message}");
                return $"Preset_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }

        #endregion
    }
}
