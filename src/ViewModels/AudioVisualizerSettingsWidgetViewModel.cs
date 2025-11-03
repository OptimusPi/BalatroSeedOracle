using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            _userProfileService =
                userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

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

            // Initialize frequency breakpoints collection
            FrequencyBreakpoints = new ObservableCollection<FrequencyBreakpoint>();
            MelodicBreakpoints = new ObservableCollection<MelodicBreakpoint>();

            // Load audio trigger points from individual JSON files
            _ = LoadAudioTriggerPoints();

            // Load old trigger points from file (backwards compatibility)
            _ = LoadTriggerPointsFromFile();

            // Wire up property change notifications from underlying ViewModel
            _settingsViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    OnPropertyChanged(e.PropertyName);
                }
            };
        }

        #region Frequency Breakpoints

        /// <summary>
        /// Collection of frequency-based breakpoints for advanced audio reactivity
        /// </summary>
        public ObservableCollection<FrequencyBreakpoint> FrequencyBreakpoints { get; }

        /// <summary>
        /// Collection of melodic breakpoints for music-based triggers
        /// </summary>
        public ObservableCollection<MelodicBreakpoint> MelodicBreakpoints { get; }

        [ObservableProperty]
        private bool _enableSeedFoundTrigger = false;

        [ObservableProperty]
        private bool _enableHighScoreTrigger = false;

        [ObservableProperty]
        private bool _enableSearchCompleteTrigger = false;

        /// <summary>
        /// Adds a new frequency breakpoint with default values
        /// </summary>
        [RelayCommand]
        private void AddFrequencyBreakpoint()
        {
            var newBreakpoint = new FrequencyBreakpoint
            {
                Name = $"Band {FrequencyBreakpoints.Count + 1}",
                StartHz = 20f,
                EndHz = 250f,
                UseLogarithmic = false,
                Threshold = 0.5f,
                Enabled = true,
                // Backwards compatibility
                FrequencyHz = 250.0f,
                AmplitudeThreshold = 0.5f,
                EffectName = "Pulse",
                EffectIntensity = 1.0f,
                DurationMs = 500
            };

            FrequencyBreakpoints.Add(newBreakpoint);
            DebugLogger.Log("AudioVisualizerWidget", $"Added frequency breakpoint: {newBreakpoint.Name}");
        }

        /// <summary>
        /// Removes a frequency breakpoint from the collection
        /// </summary>
        [RelayCommand]
        private void RemoveFrequencyBreakpoint(FrequencyBreakpoint breakpoint)
        {
            if (breakpoint != null)
            {
                FrequencyBreakpoints.Remove(breakpoint);
                DebugLogger.Log("AudioVisualizerWidget", $"Removed frequency breakpoint: {breakpoint.Name}");
            }
        }

        /// <summary>
        /// Adds a new melodic breakpoint with default values
        /// </summary>
        [RelayCommand]
        private void AddMelodicBreakpoint()
        {
            var newBreakpoint = new MelodicBreakpoint
            {
                Name = $"Melodic {MelodicBreakpoints.Count + 1}",
                MinFrequency = 200f,
                MaxFrequency = 2000f,
                MinNoteCount = 2,
                SustainMs = 100f,
                Enabled = true,
                // Backwards compatibility
                TargetNoteHz = 440f,
                FrequencyTolerance = 5.0f,
                MinDurationMs = 100,
                EffectName = "ColorShift",
                EffectIntensity = 1.0f
            };

            MelodicBreakpoints.Add(newBreakpoint);
            DebugLogger.Log("AudioVisualizerWidget", $"Added melodic breakpoint: {newBreakpoint.Name}");
        }

        /// <summary>
        /// Removes a melodic breakpoint from the collection
        /// </summary>
        [RelayCommand]
        private void RemoveMelodicBreakpoint(MelodicBreakpoint breakpoint)
        {
            if (breakpoint != null)
            {
                MelodicBreakpoints.Remove(breakpoint);
                DebugLogger.Log("AudioVisualizerWidget", $"Removed melodic breakpoint: {breakpoint.Name}");
            }
        }

        #endregion

        #region Test Effect Properties and Commands

        /// <summary>
        /// Test value for Zoom Punch effect
        /// </summary>
        [ObservableProperty]
        private double _testZoomPunchValue = 15.0;

        /// <summary>
        /// Test value for Contrast Boost effect
        /// </summary>
        [ObservableProperty]
        private double _testContrastValue = 2.0;

        /// <summary>
        /// Test value for Spin effect
        /// </summary>
        [ObservableProperty]
        private double _testSpinValue = 5.0;

        /// <summary>
        /// Test value for Twirl effect
        /// </summary>
        [ObservableProperty]
        private double _testTwirlValue = 1.0;

        /// <summary>
        /// Triggers a manual Zoom Punch effect test
        /// </summary>
        [RelayCommand]
        private void TestZoomPunch()
        {
            VisualizerEventManager.Instance.TriggerManualEffect("Zoom", (float)TestZoomPunchValue);
            DebugLogger.Log("AudioVisualizerWidget", $"Testing Zoom Punch: {TestZoomPunchValue:F1}");
        }

        /// <summary>
        /// Triggers a manual Contrast Boost effect test
        /// </summary>
        [RelayCommand]
        private void TestContrast()
        {
            VisualizerEventManager.Instance.TriggerManualEffect("Contrast", (float)TestContrastValue);
            DebugLogger.Log("AudioVisualizerWidget", $"Testing Contrast: {TestContrastValue:F1}");
        }

        /// <summary>
        /// Triggers a manual Spin effect test
        /// </summary>
        [RelayCommand]
        private void TestSpin()
        {
            VisualizerEventManager.Instance.TriggerManualEffect("Spin", (float)TestSpinValue);
            DebugLogger.Log("AudioVisualizerWidget", $"Testing Spin: {TestSpinValue:F1}");
        }

        /// <summary>
        /// Triggers a manual Twirl effect test
        /// </summary>
        [RelayCommand]
        private void TestTwirl()
        {
            VisualizerEventManager.Instance.TriggerManualEffect("Twirl", (float)TestTwirlValue);
            DebugLogger.Log("AudioVisualizerWidget", $"Testing Twirl: {TestTwirlValue:F1}");
        }

        #endregion

        #region Audio Trigger Mapping Properties

        /// <summary>
        /// Collection of available audio triggers (loaded from visualizer/audio_triggers/)
        /// </summary>
        public ObservableCollection<AudioTriggerPoint> AvailableAudioTriggers { get; } = new ObservableCollection<AudioTriggerPoint>();

        // Zoom Scale Mapping
        [ObservableProperty]
        private AudioTriggerPoint? _zoomScaleTrigger;

        [ObservableProperty]
        private int _zoomScaleEffectMode = 0; // 0=SetValue, 1=AddInertia

        [ObservableProperty]
        private float _zoomScaleMultiplier = 1.0f;

        [ObservableProperty]
        private float _zoomScaleInertiaDecay = 0.9f;

        public bool ZoomScaleIsInertiaMode => ZoomScaleEffectMode == 1;

        partial void OnZoomScaleEffectModeChanged(int value)
        {
            OnPropertyChanged(nameof(ZoomScaleIsInertiaMode));
        }

        // Contrast Mapping
        [ObservableProperty]
        private AudioTriggerPoint? _contrastTrigger;

        [ObservableProperty]
        private int _contrastEffectMode = 0;

        [ObservableProperty]
        private float _contrastMultiplier = 1.0f;

        [ObservableProperty]
        private float _contrastInertiaDecay = 0.9f;

        public bool ContrastIsInertiaMode => ContrastEffectMode == 1;

        partial void OnContrastEffectModeChanged(int value)
        {
            OnPropertyChanged(nameof(ContrastIsInertiaMode));
        }

        // Spin Amount Mapping
        [ObservableProperty]
        private AudioTriggerPoint? _spinAmountTrigger;

        [ObservableProperty]
        private int _spinAmountEffectMode = 1; // Default to AddInertia for spin (feels better)

        [ObservableProperty]
        private float _spinAmountMultiplier = 1.0f;

        [ObservableProperty]
        private float _spinAmountInertiaDecay = 0.95f;

        public bool SpinAmountIsInertiaMode => SpinAmountEffectMode == 1;

        partial void OnSpinAmountEffectModeChanged(int value)
        {
            OnPropertyChanged(nameof(SpinAmountIsInertiaMode));
        }

        #endregion

        #region Old Trigger Point Creator Properties (DEPRECATED - to be removed)

        /// <summary>
        /// Collection of saved trigger points (OLD MODEL - use AudioTriggerPoint instead)
        /// </summary>
        public ObservableCollection<TriggerPoint> TriggerPoints { get; } = new ObservableCollection<TriggerPoint>();

        /// <summary>
        /// Selected track index for trigger point creator
        /// </summary>
        [ObservableProperty]
        private int _selectedTriggerTrackIndex = 0;

        /// <summary>
        /// Selected frequency band index (0=Low, 1=Mid, 2=High)
        /// </summary>
        [ObservableProperty]
        private int _selectedFrequencyBandIndex = 1;

        /// <summary>
        /// Threshold value for trigger point (0-100)
        /// </summary>
        [ObservableProperty]
        private double _triggerThresholdValue = 50.0;

        /// <summary>
        /// Selected effect index for trigger point
        /// </summary>
        [ObservableProperty]
        private int _selectedTriggerEffectIndex = 0;

        /// <summary>
        /// Effect intensity for trigger point
        /// </summary>
        [ObservableProperty]
        private double _triggerEffectIntensity = 1.0;

        /// <summary>
        /// Current audio band value for LED indicator
        /// </summary>
        [ObservableProperty]
        private double _currentBandValue = 0.0;

        /// <summary>
        /// LED indicator color (red when threshold is exceeded, gray otherwise)
        /// </summary>
        [ObservableProperty]
        private string _triggerLedColor = "#404040";

        /// <summary>
        /// Saves the currently configured trigger point
        /// </summary>
        [RelayCommand]
        private async Task SaveTriggerPoint()
        {
            try
            {
                // Get track name from index
                var trackNames = new[] { "Bass1", "Bass2", "Drums1", "Drums2", "Chords1", "Chords2", "Melody1", "Melody2" };
                var trackName = trackNames[SelectedTriggerTrackIndex];

                // Get frequency band name
                var bandNames = new[] { "Low", "Mid", "High" };
                var bandName = bandNames[SelectedFrequencyBandIndex];

                // Get effect name
                var effectNames = new[] { "ZoomPunch", "Contrast", "Spin", "Twirl" };
                var effectName = effectNames[SelectedTriggerEffectIndex];

                // Generate auto-name: TrackName + Band + RoundedValue
                var roundedValue = (int)Math.Round(TriggerThresholdValue);
                var autoName = $"{trackName}{bandName}{roundedValue}";

                // Create new trigger point
                var triggerPoint = new TriggerPoint
                {
                    Name = autoName,
                    TrackName = trackName,
                    TrackId = trackName.ToLowerInvariant(),
                    FrequencyBand = bandName,
                    ThresholdValue = TriggerThresholdValue,
                    EffectName = effectName,
                    EffectIntensity = TriggerEffectIntensity
                };

                // Add to collection
                TriggerPoints.Add(triggerPoint);

                // Save to JSON
                await SaveTriggerPointsToFile();

                DebugLogger.Log("AudioVisualizerWidget",
                    $"Saved trigger point: {autoName} ({trackName} {bandName} @ {TriggerThresholdValue:F1} -> {effectName})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget",
                    $"Failed to save trigger point: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves trigger points to JSON file
        /// </summary>
        private async Task SaveTriggerPointsToFile()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var visualizerDir = Path.Combine(appDir, "visualizer");
                if (!Directory.Exists(visualizerDir))
                {
                    Directory.CreateDirectory(visualizerDir);
                }

                var filePath = Path.Combine(visualizerDir, "trigger_points.json");

                var data = new { TriggerPoints = TriggerPoints };
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(data, options);

                await File.WriteAllTextAsync(filePath, json);
                DebugLogger.Log("AudioVisualizerWidget", $"Saved {TriggerPoints.Count} trigger points to {filePath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget",
                    $"Failed to save trigger points file: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads AudioTriggerPoints from individual JSON files in visualizer/audio_triggers/
        /// </summary>
        private async Task LoadAudioTriggerPoints()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var audioTriggersDir = Path.Combine(appDir, "visualizer", "audio_triggers");

                if (!Directory.Exists(audioTriggersDir))
                {
                    DebugLogger.Log("AudioVisualizerWidget", "Audio triggers directory not found, creating it");
                    Directory.CreateDirectory(audioTriggersDir);
                    return;
                }

                var jsonFiles = Directory.GetFiles(audioTriggersDir, "*.json");
                AvailableAudioTriggers.Clear();

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var trigger = System.Text.Json.JsonSerializer.Deserialize<AudioTriggerPoint>(json,
                            new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                            });

                        if (trigger != null && !string.IsNullOrEmpty(trigger.Name))
                        {
                            AvailableAudioTriggers.Add(trigger);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("AudioVisualizerWidget",
                            $"Failed to load audio trigger from {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                DebugLogger.Log("AudioVisualizerWidget",
                    $"Loaded {AvailableAudioTriggers.Count} audio triggers from {jsonFiles.Length} files");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget",
                    $"Failed to load audio triggers: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads trigger points from JSON file (OLD MODEL - deprecated)
        /// </summary>
        private async Task LoadTriggerPointsFromFile()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(appDir, "visualizer", "trigger_points.json");

                if (!File.Exists(filePath))
                {
                    DebugLogger.Log("AudioVisualizerWidget", "No trigger points file found");
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<TriggerPointsData>(json);

                if (data?.TriggerPoints != null)
                {
                    TriggerPoints.Clear();
                    foreach (var tp in data.TriggerPoints)
                    {
                        TriggerPoints.Add(tp);
                    }
                    DebugLogger.Log("AudioVisualizerWidget", $"Loaded {TriggerPoints.Count} trigger points");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget",
                    $"Failed to load trigger points file: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper class for JSON deserialization
        /// </summary>
        private class TriggerPointsData
        {
            public List<TriggerPoint>? TriggerPoints { get; set; }
        }

        #endregion

        #region Effect to Trigger Mapping Properties

        /// <summary>
        /// Selected trigger point for Zoom Punch effect
        /// </summary>
        [ObservableProperty]
        private TriggerPoint? _zoomPunchTrigger;

        /// <summary>
        /// Selected trigger point for Contrast Boost effect
        /// </summary>
        [ObservableProperty]
        private TriggerPoint? _contrastBoostTrigger;

        /// <summary>
        /// Selected trigger point for Spin effect
        /// </summary>
        [ObservableProperty]
        private TriggerPoint? _spinTrigger;

        /// <summary>
        /// Selected trigger point for Twirl effect
        /// </summary>
        [ObservableProperty]
        private TriggerPoint? _twirlTrigger;

        partial void OnZoomPunchTriggerChanged(TriggerPoint? value)
        {
            _ = SaveEffectMappingsToTheme();
        }

        partial void OnContrastBoostTriggerChanged(TriggerPoint? value)
        {
            _ = SaveEffectMappingsToTheme();
        }

        partial void OnSpinTriggerChanged(TriggerPoint? value)
        {
            _ = SaveEffectMappingsToTheme();
        }

        partial void OnTwirlTriggerChanged(TriggerPoint? value)
        {
            _ = SaveEffectMappingsToTheme();
        }

        /// <summary>
        /// Saves effect mappings to the current shader theme
        /// </summary>
        private async Task SaveEffectMappingsToTheme()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var visualizerDir = Path.Combine(appDir, "visualizer", "themes");
                if (!Directory.Exists(visualizerDir))
                {
                    Directory.CreateDirectory(visualizerDir);
                }

                var themeName = CurrentPresetName.Replace(" ", "_").ToLowerInvariant();
                var filePath = Path.Combine(visualizerDir, $"{themeName}.json");

                // Create theme data with effect mappings
                var themeData = new
                {
                    ThemeName = CurrentPresetName,
                    ThemeIndex = ThemeIndex,
                    MainColor = MainColor,
                    AccentColor = AccentColor,
                    EffectMappings = new
                    {
                        ZoomPunch = ZoomPunchTrigger?.Name ?? "",
                        ContrastBoost = ContrastBoostTrigger?.Name ?? "",
                        Spin = SpinTrigger?.Name ?? "",
                        Twirl = TwirlTrigger?.Name ?? ""
                    },
                    EffectRanges = new
                    {
                        ZoomPunchMin = -10.0,
                        ZoomPunchMax = 50.0,
                        ContrastMin = 0.5,
                        ContrastMax = 8.0,
                        SpinMin = 0.0,
                        SpinMax = 10.0,
                        TwirlMin = 0.0,
                        TwirlMax = 5.0
                    }
                };

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(themeData, options);

                await File.WriteAllTextAsync(filePath, json);
                DebugLogger.Log("AudioVisualizerWidget", $"Saved effect mappings to theme: {filePath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioVisualizerWidget",
                    $"Failed to save effect mappings to theme: {ex.Message}");
            }
        }

        #endregion

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
        private double _loopCountValue = 5; // Default 5 (original hardcoded value)

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

        [ObservableProperty]
        private int _twirlSource = 0;

        partial void OnTwirlSourceChanged(int value)
        {
            ApplyShaderParameter(menu => menu.ApplyTwirlSource(value));
        }

        [ObservableProperty]
        private int _zoomThumpSource = 0;

        partial void OnZoomThumpSourceChanged(int value)
        {
            ApplyShaderParameter(menu => menu.ApplyZoomThumpSource(value));
        }

        [ObservableProperty]
        private int _colorSaturationSource = 0;

        partial void OnColorSaturationSourceChanged(int value)
        {
            ApplyShaderParameter(menu => menu.ApplyColorSaturationSource(value));
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
                DebugLogger.LogError(
                    "AudioVisualizerSettingsWidgetViewModel",
                    "SoundFlowAudioManager service not found!"
                );
                return;
            }

            audioManager.SetTrackVolume(trackName, volume);
            DebugLogger.Log(
                "AudioVisualizerSettingsWidgetViewModel",
                $"Volume slider: {trackName} → {volume:F2}"
            );
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
            if (_ownerControl == null)
                return;

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
            if (_ownerControl == null)
                return;

            var mainMenu = _ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null)
                return;

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
            if (window == null)
                return;

            var presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
            var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Load Visualizer Preset",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" },
                    },
                },
                SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                    new Uri(presetsPath)
                ),
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
            if (window == null)
                return;

            var presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
            var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Visualizer Preset",
                SuggestedFileName = "MyPreset.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" },
                    },
                },
                SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                    new Uri(presetsPath)
                ),
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
                var preset = System.Text.Json.JsonSerializer.Deserialize<Models.VisualizerPreset>(
                    json
                );

                if (preset != null)
                {
                    // Apply all settings from preset
                    ThemeIndex = preset.ThemeIndex;
                    if (preset.MainColor.HasValue)
                        MainColor = preset.MainColor.Value;
                    if (preset.AccentColor.HasValue)
                        AccentColor = preset.AccentColor.Value;

                    // Apply game event triggers
                    SeedFoundTrigger = preset.SeedFoundTrigger;
                    HighScoreSeedTrigger = preset.HighScoreSeedTrigger;
                    SearchCompleteTrigger = preset.SearchCompleteTrigger;

                    // Apply effect sources
                    if (preset.CustomEffects != null)
                    {
                        if (preset.CustomEffects.TryGetValue("ShadowFlicker", out int sf))
                            ShadowFlickerSource = sf;
                        if (preset.CustomEffects.TryGetValue("Spin", out int sp))
                            SpinSource = sp;
                        if (preset.CustomEffects.TryGetValue("BeatPulse", out int bp))
                            BeatPulseSource = bp;
                        if (preset.CustomEffects.TryGetValue("Twirl", out int tw))
                            TwirlSource = tw;
                        if (preset.CustomEffects.TryGetValue("ZoomThump", out int zt))
                            ZoomThumpSource = zt;
                        if (preset.CustomEffects.TryGetValue("ColorSaturation", out int cs))
                            ColorSaturationSource = cs;
                    }

                    // Load frequency breakpoints
                    FrequencyBreakpoints.Clear();
                    if (preset.FrequencyBreakpoints != null)
                    {
                        foreach (var breakpoint in preset.FrequencyBreakpoints)
                        {
                            FrequencyBreakpoints.Add(breakpoint);
                        }
                        DebugLogger.Log("AudioVisualizerWidget",
                            $"Loaded {preset.FrequencyBreakpoints.Count} frequency breakpoints");
                    }

                    // Load melodic breakpoints
                    MelodicBreakpoints.Clear();
                    if (preset.MelodicBreakpoints != null)
                    {
                        foreach (var breakpoint in preset.MelodicBreakpoints)
                        {
                            MelodicBreakpoints.Add(breakpoint);
                        }
                        DebugLogger.Log("AudioVisualizerWidget",
                            $"Loaded {preset.MelodicBreakpoints.Count} melodic breakpoints");
                    }

                    CurrentPresetName = preset.Name;
                    DebugLogger.Log("AudioVisualizerWidget", $"Loaded preset: {preset.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to load preset: {ex.Message}"
                );
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
                    SeedFoundTrigger = SeedFoundTrigger,
                    HighScoreSeedTrigger = HighScoreSeedTrigger,
                    SearchCompleteTrigger = SearchCompleteTrigger,
                    CustomEffects = new System.Collections.Generic.Dictionary<string, int>
                    {
                        ["ShadowFlicker"] = ShadowFlickerSource,
                        ["Spin"] = SpinSource,
                        ["BeatPulse"] = BeatPulseSource,
                        ["Twirl"] = TwirlSource,
                        ["ZoomThump"] = ZoomThumpSource,
                        ["ColorSaturation"] = ColorSaturationSource,
                    },
                    FrequencyBreakpoints = FrequencyBreakpoints.Count > 0
                        ? new System.Collections.Generic.List<FrequencyBreakpoint>(FrequencyBreakpoints)
                        : null,
                    MelodicBreakpoints = MelodicBreakpoints.Count > 0
                        ? new System.Collections.Generic.List<MelodicBreakpoint>(MelodicBreakpoints)
                        : null,
                };

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(preset, options);
                await File.WriteAllTextAsync(filePath, json);

                CurrentPresetName = preset.Name;
                DebugLogger.Log("AudioVisualizerWidget", $"Saved preset: {preset.Name}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to save preset: {ex.Message}"
                );
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
