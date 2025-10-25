using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for AudioVisualizerSettingsWidget - PROPER MVVM implementation
    /// All shader parameters exposed as bindable properties with Min/Max/Value
    /// No FindControl, no code-behind logic, no ancestor lookups in View
    /// </summary>
    public partial class AudioVisualizerSettingsWidgetViewModel : BaseWidgetViewModel
    {
        private readonly AudioVisualizerSettingsModalViewModel _settingsViewModel;
        private readonly UserProfileService _userProfileService;
        private Control? _ownerControl;

        public AudioVisualizerSettingsWidgetViewModel(UserProfileService userProfileService)
        {
            // Inject UserProfileService via DI
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

            // Create the underlying settings ViewModel (handles presets, themes, etc.)
            _settingsViewModel = new AudioVisualizerSettingsModalViewModel();

            // Configure base widget properties
            WidgetTitle = "Visual Settings";
            WidgetIcon = "🎨";
            IsMinimized = true; // Start minimized

            // Set fixed position for Audio widget - second position (90px spacing)
            PositionX = 20;
            PositionY = 170;

            // Initialize shader parameters with default values
            InitializeShaderParameters();

            // Wire up property change notifications from underlying ViewModel
            _settingsViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);
                }
            };
        }

        #region Shader Parameters - Time

        [ObservableProperty]
        private double _timeValue = 0;

        [ObservableProperty]
        private double _timeMin = 0;

        [ObservableProperty]
        private double _timeMax = 100;

        partial void OnTimeValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderTime((float)value));
        }

        #endregion

        #region Shader Parameters - SpinTime

        [ObservableProperty]
        private double _spinTimeValue = 0;

        [ObservableProperty]
        private double _spinTimeMin = 0;

        [ObservableProperty]
        private double _spinTimeMax = 100;

        partial void OnSpinTimeValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderSpinTime((float)value));
        }

        #endregion

        #region Shader Parameters - Contrast

        [ObservableProperty]
        private double _contrastValue = 2;

        [ObservableProperty]
        private double _contrastMin = 0.1;

        [ObservableProperty]
        private double _contrastMax = 10;

        partial void OnContrastValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderContrast((float)value));
        }

        #endregion

        #region Shader Parameters - SpinAmount

        [ObservableProperty]
        private double _spinAmountValue = 0.3;

        [ObservableProperty]
        private double _spinAmountMin = 0;

        [ObservableProperty]
        private double _spinAmountMax = 1;

        partial void OnSpinAmountValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderSpinAmount((float)value));
        }

        #endregion

        #region Shader Parameters - ParallaxX

        [ObservableProperty]
        private double _parallaxXValue = 0;

        [ObservableProperty]
        private double _parallaxXMin = -1;

        [ObservableProperty]
        private double _parallaxXMax = 1;

        partial void OnParallaxXValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderParallaxX((float)value));
        }

        #endregion

        #region Shader Parameters - ParallaxY

        [ObservableProperty]
        private double _parallaxYValue = 0;

        [ObservableProperty]
        private double _parallaxYMin = -1;

        [ObservableProperty]
        private double _parallaxYMax = 1;

        partial void OnParallaxYValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderParallaxY((float)value));
        }

        #endregion

        #region Shader Parameters - ZoomScale

        [ObservableProperty]
        private double _zoomScaleValue = 0;

        [ObservableProperty]
        private double _zoomScaleMin = -50;

        [ObservableProperty]
        private double _zoomScaleMax = 50;

        partial void OnZoomScaleValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderZoomPunch((float)value));
        }

        #endregion

        #region Shader Parameters - Saturation

        [ObservableProperty]
        private double _saturationValue = 0;

        [ObservableProperty]
        private double _saturationMin = 0;

        [ObservableProperty]
        private double _saturationMax = 1;

        partial void OnSaturationValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderMelodySaturation((float)value));
        }

        #endregion

        #region Shader Parameters - PixelSize

        [ObservableProperty]
        private double _pixelSizeValue = 1440;

        [ObservableProperty]
        private double _pixelSizeMin = 100;

        [ObservableProperty]
        private double _pixelSizeMax = 5000;

        partial void OnPixelSizeValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderPixelSize((float)value));
        }

        #endregion

        #region Shader Parameters - SpinEase

        [ObservableProperty]
        private double _spinEaseValue = 0.5;

        [ObservableProperty]
        private double _spinEaseMin = 0;

        [ObservableProperty]
        private double _spinEaseMax = 2;

        partial void OnSpinEaseValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderSpinEase((float)value));
        }

        #endregion

        #region Shader Parameters - LoopCount

        [ObservableProperty]
        private double _loopCountValue = 5;  // Default 5 (original hardcoded value)

        [ObservableProperty]
        private double _loopCountMin = 1;

        [ObservableProperty]
        private double _loopCountMax = 10;

        partial void OnLoopCountValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderLoopCount((float)value));
        }

        #endregion

        #region Delegate Properties to Underlying ViewModel (Theme, Audio, etc.)

        // Theme
        public int ThemeIndex
        {
            get => _settingsViewModel.ThemeIndex;
            set
            {
                _settingsViewModel.ThemeIndex = value;
                ApplyShaderParameter(menu => menu.ApplyVisualizerTheme(value));
            }
        }

        public bool IsCustomTheme => _settingsViewModel.IsCustomTheme;

        public int MainColor
        {
            get => _settingsViewModel.MainColor;
            set
            {
                if (_settingsViewModel.MainColor != value)
                {
                    _settingsViewModel.MainColor = value;
                    OnPropertyChanged(nameof(MainColor));
                    ApplyShaderParameter(menu => menu.ApplyMainColor(value));
                }
            }
        }

        public int AccentColor
        {
            get => _settingsViewModel.AccentColor;
            set
            {
                if (_settingsViewModel.AccentColor != value)
                {
                    _settingsViewModel.AccentColor = value;
                    OnPropertyChanged(nameof(AccentColor));
                    ApplyShaderParameter(menu => menu.ApplyAccentColor(value));
                }
            }
        }

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
            set
            {
                _settingsViewModel.ShadowFlickerSource = value;
                ApplyShaderParameter(menu => menu.ApplyShadowFlickerSource(value));
            }
        }

        public int SpinSource
        {
            get => _settingsViewModel.SpinSource;
            set
            {
                _settingsViewModel.SpinSource = value;
                ApplyShaderParameter(menu => menu.ApplySpinSource(value));
            }
        }

        public int BeatPulseSource
        {
            get => _settingsViewModel.BeatPulseSource;
            set
            {
                _settingsViewModel.BeatPulseSource = value;
                ApplyShaderParameter(menu => menu.ApplyBeatPulseSource(value));
            }
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

        // Track Volume Controls (0.0 to 1.0) - Control actual audio playback volume
        private float _drums1Volume = 1.0f;
        public float Drums1Volume
        {
            get => _drums1Volume;
            set
            {
                if (SetProperty(ref _drums1Volume, value))
                {
                    ApplyTrackVolume("Drums1", value);
                }
            }
        }

        private float _drums2Volume = 1.0f;
        public float Drums2Volume
        {
            get => _drums2Volume;
            set
            {
                if (SetProperty(ref _drums2Volume, value))
                {
                    ApplyTrackVolume("Drums2", value);
                }
            }
        }

        private float _bass1Volume = 1.0f;
        public float Bass1Volume
        {
            get => _bass1Volume;
            set
            {
                if (SetProperty(ref _bass1Volume, value))
                {
                    ApplyTrackVolume("Bass1", value);
                }
            }
        }

        private float _bass2Volume = 1.0f;
        public float Bass2Volume
        {
            get => _bass2Volume;
            set
            {
                if (SetProperty(ref _bass2Volume, value))
                {
                    ApplyTrackVolume("Bass2", value);
                }
            }
        }

        private float _chords1Volume = 1.0f;
        public float Chords1Volume
        {
            get => _chords1Volume;
            set
            {
                if (SetProperty(ref _chords1Volume, value))
                {
                    ApplyTrackVolume("Chords1", value);
                }
            }
        }

        private float _chords2Volume = 1.0f;
        public float Chords2Volume
        {
            get => _chords2Volume;
            set
            {
                if (SetProperty(ref _chords2Volume, value))
                {
                    ApplyTrackVolume("Chords2", value);
                }
            }
        }

        private float _melody1Volume = 1.0f;
        public float Melody1Volume
        {
            get => _melody1Volume;
            set
            {
                if (SetProperty(ref _melody1Volume, value))
                {
                    ApplyTrackVolume("Melody1", value);
                }
            }
        }

        private float _melody2Volume = 1.0f;
        public float Melody2Volume
        {
            get => _melody2Volume;
            set
            {
                if (SetProperty(ref _melody2Volume, value))
                {
                    ApplyTrackVolume("Melody2", value);
                }
            }
        }

        private void ApplyTrackVolume(string trackName, float volume)
        {
            var audioManager = Helpers.ServiceHelper.GetService<SoundFlowAudioManager>();
            if (audioManager == null)
            {
                DebugLogger.LogError("AudioVisualizerSettingsWidgetViewModel", "SoundFlowAudioManager service not found!");
                return;
            }

            audioManager.SetTrackVolume(trackName, volume);
            DebugLogger.Log("AudioVisualizerSettingsWidgetViewModel", $"Volume slider: {trackName} → {volume:F2}");
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

        // Presets - Removed, we'll implement our own below
        // public System.Collections.ObjectModel.ObservableCollection<Models.VisualizerPreset> Presets => _settingsViewModel.Presets;

        // public Models.VisualizerPreset? SelectedPreset
        // {
        //     get => _settingsViewModel.SelectedPreset;
        //     set => _settingsViewModel.SelectedPreset = value;
        // }

        // public System.Windows.Input.ICommand LoadPresetCommand => _settingsViewModel.LoadPresetCommand;
        // public System.Windows.Input.ICommand SavePresetCommand => _settingsViewModel.SavePresetCommand;
        // public System.Windows.Input.ICommand DeletePresetCommand => _settingsViewModel.DeletePresetCommand;

        #endregion

        #region Lifecycle & Shader Application

        /// <summary>
        /// Called when the control is attached to the visual tree
        /// This is where we get access to the BalatroMainMenu ancestor
        /// </summary>
        public void OnAttached(Control ownerControl)
        {
            _ownerControl = ownerControl;

            // Apply all current settings to the shader immediately
            ApplyAllShaderParameters();
        }

        /// <summary>
        /// Called when the control is detached from the visual tree
        /// </summary>
        public void OnDetached()
        {
            _ownerControl = null;
        }

        /// <summary>
        /// Initialize shader parameters to default values
        /// </summary>
        private void InitializeShaderParameters()
        {
            // Default values are already set in the field initializers
            // This method is here for future customization if needed
        }

        /// <summary>
        /// Apply a shader parameter update to the BalatroMainMenu
        /// This is the ONLY place we access the visual tree
        /// </summary>
        private void ApplyShaderParameter(Action<BalatroMainMenu> applyAction)
        {
            if (_ownerControl == null) return;

            var mainMenu = _ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu != null)
            {
                applyAction(mainMenu);
            }
        }

        /// <summary>
        /// Apply all shader parameters at once (used on initialization)
        /// </summary>
        private void ApplyAllShaderParameters()
        {
            if (_ownerControl == null) return;

            var mainMenu = _ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null) return;

            // Apply all shader parameters
            mainMenu.ApplyShaderTime((float)TimeValue);
            mainMenu.ApplyShaderSpinTime((float)SpinTimeValue);
            mainMenu.ApplyShaderContrast((float)ContrastValue);
            mainMenu.ApplyShaderSpinAmount((float)SpinAmountValue);
            mainMenu.ApplyShaderParallaxX((float)ParallaxXValue);
            mainMenu.ApplyShaderParallaxY((float)ParallaxYValue);
            mainMenu.ApplyShaderZoomPunch((float)ZoomScaleValue);
            mainMenu.ApplyShaderMelodySaturation((float)SaturationValue);
            mainMenu.ApplyShaderPixelSize((float)PixelSizeValue);
            mainMenu.ApplyShaderSpinEase((float)SpinEaseValue);
            mainMenu.ApplyShaderLoopCount((float)LoopCountValue);

            // Apply theme settings
            mainMenu.ApplyVisualizerTheme(ThemeIndex);
            if (IsCustomTheme)
            {
                mainMenu.ApplyMainColor(MainColor);
                mainMenu.ApplyAccentColor(AccentColor);
            }

            // Apply effect sources
            mainMenu.ApplyShadowFlickerSource(ShadowFlickerSource);
            mainMenu.ApplySpinSource(SpinSource);
            mainMenu.ApplyBeatPulseSource(BeatPulseSource);
        }

        #endregion

        #region Preset Management

        [ObservableProperty]
        private string _currentPresetName = "Default Balatro";

        [RelayCommand]
        private async Task LoadPreset()
        {
            // Open file dialog
            var window = GetMainWindow();
            if (window == null) return;

            var presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
            var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Load Visualizer Preset",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
                },
                SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                    new Uri(presetsPath))
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(dialog);
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                await LoadPresetFromFile(filePath);
            }
        }

        [RelayCommand]
        private async Task SavePreset()
        {
            // Open save dialog
            var window = GetMainWindow();
            if (window == null) return;

            var presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
            var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Visualizer Preset",
                SuggestedFileName = "MyPreset.json",
                DefaultExtension = "json",
                FileTypeChoices = new[] {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
                },
                SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                    new Uri(presetsPath))
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(dialog);
            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                await SavePresetToFile(filePath);
            }
        }

        private async Task LoadPresetFromFile(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var preset = System.Text.Json.JsonSerializer.Deserialize<Models.VisualizerPreset>(json);

                if (preset != null)
                {
                    // Apply all settings from preset
                    ThemeIndex = preset.ThemeIndex;
                    if (preset.MainColor.HasValue) MainColor = preset.MainColor.Value;
                    if (preset.AccentColor.HasValue) AccentColor = preset.AccentColor.Value;

                    // Apply effect sources
                    if (preset.CustomEffects != null)
                    {
                        if (preset.CustomEffects.TryGetValue("ShadowFlicker", out int sf)) ShadowFlickerSource = sf;
                        if (preset.CustomEffects.TryGetValue("Spin", out int sp)) SpinSource = sp;
                        if (preset.CustomEffects.TryGetValue("ZoomThump", out int zt)) _zoomThumpSource = zt;
                        if (preset.CustomEffects.TryGetValue("ColorSaturation", out int cs)) _colorSaturationSource = cs;
                    }

                    CurrentPresetName = preset.Name;
                    DebugLogger.Log("AudioVisualizerWidget", $"Loaded preset: {preset.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget", $"Failed to load preset: {ex.Message}");
            }
        }

        private async Task SavePresetToFile(string filePath)
        {
            try
            {
                var preset = new Models.VisualizerPreset
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    ThemeIndex = ThemeIndex,
                    MainColor = MainColor,
                    AccentColor = AccentColor,
                    CustomEffects = new System.Collections.Generic.Dictionary<string, int>
                    {
                        ["ShadowFlicker"] = ShadowFlickerSource,
                        ["Spin"] = SpinSource,
                        ["ZoomThump"] = _zoomThumpSource,
                        ["ColorSaturation"] = _colorSaturationSource
                    }
                };

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(preset, options);
                await File.WriteAllTextAsync(filePath, json);

                CurrentPresetName = preset.Name;
                DebugLogger.Log("AudioVisualizerWidget", $"Saved preset: {preset.Name}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget", $"Failed to save preset: {ex.Message}");
            }
        }

        private Window? GetMainWindow()
        {
            if (_ownerControl != null)
            {
                return _ownerControl.FindAncestorOfType<Window>();
            }
            return null;
        }

        // Private fields for tracking effect sources not exposed as properties
        private int _zoomThumpSource = 0;
        private int _colorSaturationSource = 0;

        #endregion

        #region Commands

        [RelayCommand]
        private void ExportToJson()
        {
            // TODO: Implement JSON export for all shader parameters
            // This would serialize TimeValue, SpinTimeValue, etc. to a JSON file
            System.Diagnostics.Debug.WriteLine("Export to JSON - Not yet implemented");
        }

        #endregion

        #region Base Widget Overrides

        protected override void OnExpanded()
        {
            base.OnExpanded();
            // Refresh shader parameters when expanded
            ApplyAllShaderParameters();
        }

        protected override void OnMinimized()
        {
            base.OnMinimized();
            // Could auto-save settings when minimized if needed
        }

        #endregion
    }
}
