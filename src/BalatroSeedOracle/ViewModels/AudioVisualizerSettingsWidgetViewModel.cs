using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
        private readonly SoundFlowAudioManager? _soundFlowAudioManager;
        private readonly TransitionService? _transitionService;
        private Control? _ownerControl;
        private bool _isApplyingManualTransition = false;

        public AudioVisualizerSettingsWidgetViewModel(
            UserProfileService userProfileService,
            SoundFlowAudioManager? soundFlowAudioManager = null,
            TransitionService? transitionService = null,
            WidgetPositionService? widgetPositionService = null
        )
            : base(widgetPositionService)
        {
            // Inject UserProfileService via DI
            _userProfileService =
                userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _soundFlowAudioManager = soundFlowAudioManager;
            _transitionService = transitionService;

            // Create the underlying settings ViewModel (handles presets, themes, etc.)
            _settingsViewModel = new AudioVisualizerSettingsModalViewModel();

            // Configure base widget properties
            WidgetTitle = "Visual Settings";
            WidgetIcon = "Palette";
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

            // Load search transition settings from user profile
            LoadSearchTransitionSettings();

            // Load available preset names for search transition dropdowns
            RefreshPresetList();

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
                DurationMs = 500,
            };

            FrequencyBreakpoints.Add(newBreakpoint);
            DebugLogger.Log(
                "AudioVisualizerWidget",
                $"Added frequency breakpoint: {newBreakpoint.Name}"
            );
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
                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    $"Removed frequency breakpoint: {breakpoint.Name}"
                );
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
                EffectIntensity = 1.0f,
            };

            MelodicBreakpoints.Add(newBreakpoint);
            DebugLogger.Log(
                "AudioVisualizerWidget",
                $"Added melodic breakpoint: {newBreakpoint.Name}"
            );
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
                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    $"Removed melodic breakpoint: {breakpoint.Name}"
                );
            }
        }

        #endregion

        [ObservableProperty]
        private string? _manualTransitionMixAName;

        [ObservableProperty]
        private string? _manualTransitionMixBName;

        [RelayCommand]
        private async Task AnimateVisualAndMixToB()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ManualTransitionMixBName))
                    return;
                var mixB = MixerHelper.LoadMixer(ManualTransitionMixBName!);
                if (mixB == null)
                    return;

                var mixA = !string.IsNullOrWhiteSpace(ManualTransitionMixAName)
                    ? MixerHelper.LoadMixer(ManualTransitionMixAName!)
                    : null;

                var duration = TimeSpan.FromSeconds(2.0);
                var start = DateTime.UtcNow;
                var s = mixA ?? mixB;
                if (_soundFlowAudioManager == null)
                    return;

                // Blend visuals using TransitionService over the same duration
                if (
                    _transitionService != null
                    && !string.IsNullOrWhiteSpace(ManualTransitionPresetA)
                    && !string.IsNullOrWhiteSpace(ManualTransitionPresetB)
                )
                {
                    var startParams = ShaderPresetHelper.Load(ManualTransitionPresetA!);
                    var endParams = ShaderPresetHelper.Load(ManualTransitionPresetB!);
                    _transitionService.StartTransition(
                        startParams,
                        endParams,
                        ApplyShaderParameters,
                        duration
                    );
                }

                while (true)
                {
                    var elapsed = DateTime.UtcNow - start;

                    if (elapsed >= duration)
                    {
                        ApplyMixerToEngine(mixB);
                        if (!string.IsNullOrWhiteSpace(ManualTransitionPresetB))
                            CurrentPresetName = ManualTransitionPresetB;
                        break;
                    }

                    float t = (float)(elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                    float p = 1f - (1f - t) * (1f - t);

                    _soundFlowAudioManager.SetTrackVolume(
                        "Drums1",
                        (float)(
                            ((s.Drums1.Volume + (mixB.Drums1.Volume - s.Drums1.Volume) * p)) / 100.0
                        )
                    );
                    var d1PanUi = (s.Drums1.Pan + (mixB.Drums1.Pan - s.Drums1.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Drums1",
                        (float)Math.Clamp((d1PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Drums2",
                        (float)(
                            ((s.Drums2.Volume + (mixB.Drums2.Volume - s.Drums2.Volume) * p)) / 100.0
                        )
                    );
                    var d2PanUi = (s.Drums2.Pan + (mixB.Drums2.Pan - s.Drums2.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Drums2",
                        (float)Math.Clamp((d2PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Bass1",
                        (float)(
                            ((s.Bass1.Volume + (mixB.Bass1.Volume - s.Bass1.Volume) * p)) / 100.0
                        )
                    );
                    var b1PanUi = (s.Bass1.Pan + (mixB.Bass1.Pan - s.Bass1.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Bass1",
                        (float)Math.Clamp((b1PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Bass2",
                        (float)(
                            ((s.Bass2.Volume + (mixB.Bass2.Volume - s.Bass2.Volume) * p)) / 100.0
                        )
                    );
                    var b2PanUi = (s.Bass2.Pan + (mixB.Bass2.Pan - s.Bass2.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Bass2",
                        (float)Math.Clamp((b2PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Chords1",
                        (float)(
                            ((s.Chords1.Volume + (mixB.Chords1.Volume - s.Chords1.Volume) * p))
                            / 100.0
                        )
                    );
                    var c1PanUi = (s.Chords1.Pan + (mixB.Chords1.Pan - s.Chords1.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Chords1",
                        (float)Math.Clamp((c1PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Chords2",
                        (float)(
                            ((s.Chords2.Volume + (mixB.Chords2.Volume - s.Chords2.Volume) * p))
                            / 100.0
                        )
                    );
                    var c2PanUi = (s.Chords2.Pan + (mixB.Chords2.Pan - s.Chords2.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Chords2",
                        (float)Math.Clamp((c2PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Melody1",
                        (float)(
                            ((s.Melody1.Volume + (mixB.Melody1.Volume - s.Melody1.Volume) * p))
                            / 100.0
                        )
                    );
                    var m1PanUi = (s.Melody1.Pan + (mixB.Melody1.Pan - s.Melody1.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Melody1",
                        (float)Math.Clamp((m1PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    _soundFlowAudioManager.SetTrackVolume(
                        "Melody2",
                        (float)(
                            ((s.Melody2.Volume + (mixB.Melody2.Volume - s.Melody2.Volume) * p))
                            / 100.0
                        )
                    );
                    var m2PanUi = (s.Melody2.Pan + (mixB.Melody2.Pan - s.Melody2.Pan) * p);
                    _soundFlowAudioManager.SetTrackPan(
                        "Melody2",
                        (float)Math.Clamp((m2PanUi + 100.0) / 200.0, 0.0, 1.0)
                    );

                    await Task.Delay(16);
                }

                _soundFlowAudioManager.SetTrackMuted("Drums1", mixB.Drums1.Muted);
                _soundFlowAudioManager.SetTrackMuted("Drums2", mixB.Drums2.Muted);
                _soundFlowAudioManager.SetTrackMuted("Bass1", mixB.Bass1.Muted);
                _soundFlowAudioManager.SetTrackMuted("Bass2", mixB.Bass2.Muted);
                _soundFlowAudioManager.SetTrackMuted("Chords1", mixB.Chords1.Muted);
                _soundFlowAudioManager.SetTrackMuted("Chords2", mixB.Chords2.Muted);
                _soundFlowAudioManager.SetTrackMuted("Melody1", mixB.Melody1.Muted);
                _soundFlowAudioManager.SetTrackMuted("Melody2", mixB.Melody2.Muted);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"AnimateVisualAndMixToB failed: {ex.Message}"
                );
            }
        }

        private void ApplyShaderParameters(ShaderParameters p)
        {
            if (_ownerControl == null)
                return;
            var mainMenu = _ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null)
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                mainMenu.ApplyShaderTime(p.TimeSpeed);
                mainMenu.ApplyShaderSpinTime(p.SpinTimeSpeed);
                mainMenu.ApplyShaderContrast(p.Contrast);
                mainMenu.ApplyShaderSpinAmount(p.SpinAmount);
                mainMenu.ApplyShaderParallaxX(p.ParallaxX);
                mainMenu.ApplyShaderParallaxY(p.ParallaxY);
                mainMenu.ApplyShaderZoomPunch(p.ZoomScale);
                mainMenu.ApplyShaderMelodySaturation(p.SaturationAmount);
                mainMenu.ApplyShaderPixelSize(p.PixelSize);
                mainMenu.ApplyShaderSpinEase(p.SpinEase);
                mainMenu.ApplyShaderLoopCount(p.LoopCount);
            });
        }

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
        /// Psychedelic Blend - smoothly morphs between normal and full trippy mode (0.0 = normal, 1.0 = maximum psychedelic)
        /// </summary>
        [ObservableProperty]
        private double _psychedelicBlend = 0.0;

        /// <summary>
        /// Triggers a manual Zoom Punch effect test
        /// </summary>
        [RelayCommand]
        private void TestZoomPunch()
        {
            VisualizerEventManager.Instance.TriggerManualEffect("Zoom", (float)TestZoomPunchValue);
            DebugLogger.Log(
                "AudioVisualizerWidget",
                $"Testing Zoom Punch: {TestZoomPunchValue:F1}"
            );
        }

        /// <summary>
        /// Triggers a manual Contrast Boost effect test
        /// </summary>
        [RelayCommand]
        private void TestContrast()
        {
            VisualizerEventManager.Instance.TriggerManualEffect(
                "Contrast",
                (float)TestContrastValue
            );
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
        public ObservableCollection<AudioTriggerPoint> AvailableAudioTriggers { get; } =
            new ObservableCollection<AudioTriggerPoint>();

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

        #region Audio Trigger Loading

        /// <summary>
        /// Loads AudioTriggerPoints from individual JSON files in visualizer/audio_triggers/
        /// </summary>
        private async Task LoadAudioTriggerPoints()
        {
            try
            {
                var appDir = AppContext.BaseDirectory;
                var audioTriggersDir = Path.Combine(appDir, "visualizer", "audio_triggers");

                if (!Directory.Exists(audioTriggersDir))
                {
                    DebugLogger.Log(
                        "AudioVisualizerWidget",
                        "Audio triggers directory not found, creating it"
                    );
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
                        var trigger =
                            System.Text.Json.JsonSerializer.Deserialize<AudioTriggerPoint>(
                                json,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = System
                                        .Text
                                        .Json
                                        .JsonNamingPolicy
                                        .CamelCase,
                                }
                            );

                        if (trigger != null && !string.IsNullOrEmpty(trigger.Name))
                        {
                            AvailableAudioTriggers.Add(trigger);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "AudioVisualizerWidget",
                            $"Failed to load audio trigger from {Path.GetFileName(file)}: {ex.Message}"
                        );
                    }
                }

                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    $"Loaded {AvailableAudioTriggers.Count} audio triggers from {jsonFiles.Length} files"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to load audio triggers: {ex.Message}"
                );
            }
        }

        #endregion

        #region Shader Parameters - Time

        [ObservableProperty]
        private double _timeValue = 1;

        [ObservableProperty]
        private double _timeMin = 0;

        [ObservableProperty]
        private double _timeMax = 100;

        partial void OnTimeValueChanged(double value)
        {
            if (_syncingFromShader)
                return;
            ApplyShaderParameter(menu => menu.ApplyShaderTime((float)value));
        }

        #endregion

        #region Shader Parameters - SpinTime

        [ObservableProperty]
        private double _spinTimeValue = 1;

        [ObservableProperty]
        private double _spinTimeMin = 0;

        [ObservableProperty]
        private double _spinTimeMax = 100;

        partial void OnSpinTimeValueChanged(double value)
        {
            if (_syncingFromShader)
                return;
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
        private double _loopCountMin = 0;

        [ObservableProperty]
        private double _loopCountMax = 100;

        partial void OnLoopCountValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyShaderLoopCount((float)value));
        }

        #endregion

        #region Shader Parameters - Psychedelic Blend

        [ObservableProperty]
        private double _psychedelicBlendValue = 0.0;

        partial void OnPsychedelicBlendValueChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicBlend((float)value));
        }

        [ObservableProperty]
        private double _psychedelicSpeed = 1.0;

        partial void OnPsychedelicSpeedChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicSpeed((float)value));
        }

        [ObservableProperty]
        private double _psychedelicComplexity = 1.0;

        partial void OnPsychedelicComplexityChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicComplexity((float)value));
        }

        [ObservableProperty]
        private double _psychedelicColorCycle = 1.0;

        partial void OnPsychedelicColorCycleChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicColorCycle((float)value));
        }

        [ObservableProperty]
        private double _psychedelicKaleidoscope = 0.0;

        partial void OnPsychedelicKaleidoscopeChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicKaleidoscope((float)value));
        }

        [ObservableProperty]
        private double _psychedelicFluidFlow = 0.0;

        partial void OnPsychedelicFluidFlowChanged(double value)
        {
            ApplyShaderParameter(menu => menu.ApplyPsychedelicFluidFlow((float)value));
        }

        #endregion

        #region Delegate Properties to Underlying ViewModel (Theme, Audio, etc.)

        // Theme
        private int ClampThemeIndex(int value) => value < 0 ? 0 : (value > 1 ? 1 : value);

        public int ThemeIndex
        {
            get => ClampThemeIndex(_settingsViewModel.ThemeIndex);
            set
            {
                var v = ClampThemeIndex(value);
                _settingsViewModel.ThemeIndex = v;
                // ApplyVisualizerTheme was empty stub - removed
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

        [ObservableProperty]
        private int _mainColorR = 255;

        partial void OnMainColorRChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyMainColor(
                    new SkiaSharp.SKColor((byte)_mainColorR, (byte)_mainColorG, (byte)_mainColorB)
                )
            );
        }

        [ObservableProperty]
        private int _mainColorG = 76;

        partial void OnMainColorGChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyMainColor(
                    new SkiaSharp.SKColor((byte)_mainColorR, (byte)_mainColorG, (byte)_mainColorB)
                )
            );
        }

        [ObservableProperty]
        private int _mainColorB = 64;

        partial void OnMainColorBChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyMainColor(
                    new SkiaSharp.SKColor((byte)_mainColorR, (byte)_mainColorG, (byte)_mainColorB)
                )
            );
        }

        [ObservableProperty]
        private int _accentColorR = 0;

        partial void OnAccentColorRChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyAccentColor(
                    new SkiaSharp.SKColor(
                        (byte)_accentColorR,
                        (byte)_accentColorG,
                        (byte)_accentColorB
                    )
                )
            );
        }

        [ObservableProperty]
        private int _accentColorG = 147;

        partial void OnAccentColorGChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyAccentColor(
                    new SkiaSharp.SKColor(
                        (byte)_accentColorR,
                        (byte)_accentColorG,
                        (byte)_accentColorB
                    )
                )
            );
        }

        [ObservableProperty]
        private int _accentColorB = 255;

        partial void OnAccentColorBChanged(int value)
        {
            ApplyShaderParameter(menu =>
                menu.ApplyAccentColor(
                    new SkiaSharp.SKColor(
                        (byte)_accentColorR,
                        (byte)_accentColorG,
                        (byte)_accentColorB
                    )
                )
            );
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
                // ApplyBeatPulseSource was empty stub - removed
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

        private void InitializeShaderParameters()
        {
            // Default values are already set in the field initializers
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
            // ApplyVisualizerTheme was empty stub - removed
            if (IsCustomTheme)
            {
                mainMenu.ApplyMainColor(MainColor);
                mainMenu.ApplyAccentColor(AccentColor);
            }

            // Apply effect sources
            mainMenu.ApplyShadowFlickerSource(ShadowFlickerSource);
            mainMenu.ApplySpinSource(SpinSource);
            // ApplyBeatPulseSource was empty stub - removed
        }

        #endregion

        #region Preset Management

        [ObservableProperty]
        private string _currentPresetName = "Default Balatro";

        // ============================================
        // SEARCH TRANSITION SETTINGS (UI Configuration)
        // ============================================

        /// <summary>
        /// Enable shader transition during searches (colors/effects change as search progresses 0-100%)
        /// </summary>
        [ObservableProperty]
        private bool _enableSearchTransition;

        /// <summary>
        /// List of available preset names for dropdown selection
        /// </summary>
        public ObservableCollection<string> AvailablePresetNames { get; } =
            new ObservableCollection<string> { "Default Balatro" };

        public ObservableCollection<string> AvailableShaderPresetNames { get; } =
            new ObservableCollection<string> { "Default" };

        /// <summary>
        /// Selected start preset name for search transitions
        /// </summary>
        [ObservableProperty]
        private string? _searchTransitionStartPresetName;

        /// <summary>
        /// Selected end preset name for search transitions
        /// </summary>
        [ObservableProperty]
        private string? _searchTransitionEndPresetName;

        /// <summary>
        /// Selected preset A for manual transition testing
        /// </summary>
        [ObservableProperty]
        private string? _manualTransitionPresetA;

        /// <summary>
        /// Selected preset B for manual transition testing
        /// </summary>
        [ObservableProperty]
        private string? _manualTransitionPresetB;

        /// <summary>
        /// Manual transition progress (0-100%)
        /// </summary>
        [ObservableProperty]
        private double _manualTransitionProgress = 0.0;

        partial void OnManualTransitionProgressChanged(double value)
        {
            // Prevent recursive calls during transition
            if (_isApplyingManualTransition)
                return;

            // Apply manual transition when slider moves
            ApplyManualTransition(value / 100.0); // Convert 0-100 to 0-1
        }

        partial void OnEnableSearchTransitionChanged(bool value)
        {
            // Save to user profile when changed
            if (_userProfileService != null)
            {
                _userProfileService.GetProfile().VisualizerSettings.EnableSearchTransition = value;
                _userProfileService.SaveProfile();
            }
        }

        partial void OnSearchTransitionStartPresetNameChanged(string? value)
        {
            // Save to user profile when changed
            if (_userProfileService != null)
            {
                _userProfileService
                    .GetProfile()
                    .VisualizerSettings.SearchTransitionStartPresetName = value;
                _userProfileService.SaveProfile();
            }
        }

        partial void OnSearchTransitionEndPresetNameChanged(string? value)
        {
            // Save to user profile when changed
            if (_userProfileService != null)
            {
                _userProfileService.GetProfile().VisualizerSettings.SearchTransitionEndPresetName =
                    value;
                _userProfileService.SaveProfile();
            }
        }

        /// <summary>
        /// Loads search transition settings from user profile
        /// </summary>
        private void LoadSearchTransitionSettings()
        {
            try
            {
                var settings = _userProfileService.GetProfile().VisualizerSettings;
                EnableSearchTransition = settings.EnableSearchTransition;
                SearchTransitionStartPresetName = settings.SearchTransitionStartPresetName;
                SearchTransitionEndPresetName = settings.SearchTransitionEndPresetName;

                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    $"Loaded search transition settings: Enabled={EnableSearchTransition}, Start={SearchTransitionStartPresetName}, End={SearchTransitionEndPresetName}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to load search transition settings: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Loads available preset names from disk
        /// </summary>
        [RelayCommand]
        private void RefreshPresetList()
        {
            try
            {
                AvailablePresetNames.Clear();

                // Keep only default Balatro
                AvailablePresetNames.Add("Default Balatro");

                // Load user-created presets from disk
                var presets = Helpers.PresetHelper.LoadAllPresets();
                foreach (var preset in presets)
                {
                    if (!string.IsNullOrWhiteSpace(preset.Name))
                    {
                        AvailablePresetNames.Add(preset.Name);
                    }
                }

                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    $"Loaded {AvailablePresetNames.Count} preset names"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to load preset names: {ex.Message}"
                );
            }
        }

        [RelayCommand]
        private void RefreshShaderPresetList()
        {
            try
            {
                AvailableShaderPresetNames.Clear();
                AvailableShaderPresetNames.Add("Default");
                var names = ShaderPresetHelper.ListNames();
                foreach (var n in names)
                    AvailableShaderPresetNames.Add(n);
            }
            catch (Exception ex)
            {
                // Log the failure - user needs to know if preset list doesn't refresh
                Helpers.DebugLogger.LogError(
                    "AudioVisualizerSettings",
                    $"❌ Failed to refresh shader preset list: {ex.Message}"
                );
            }
        }

        [RelayCommand]
        private async Task LoadPreset()
        {
            // Open file dialog in the correct VisualizerPresets folder
            var window = GetMainWindow();
            if (window == null)
                return;

            var presetsPath = AppPaths.VisualizerPresetsDir;
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
            // Show simple name input dialog instead of full file picker
            var window = GetMainWindow();
            if (window == null)
                return;

            // Prompt for preset name
            var presetName = await ModalHelper.ShowTextInputDialogAsync(
                window,
                "Save Preset",
                "Enter preset name:",
                CurrentPresetName ?? "MyPreset"
            );

            if (string.IsNullOrWhiteSpace(presetName))
                return;

            // Sanitize the name for a safe filename
            var safeName = SanitizeFileName(presetName);
            var filePath = Path.Combine(AppPaths.VisualizerPresetsDir, $"{safeName}.json");

            await SavePresetToFile(filePath);

            // Refresh the preset list so it shows up in dropdowns
            RefreshShaderPresetList();
        }

        private static string SanitizeFileName(string name)
        {
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
            // Trim and replace spaces with underscores for safety
            sanitized = sanitized.Trim();
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Preset";
            return sanitized;
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
                    ThemeIndex = ClampThemeIndex(preset.ThemeIndex);
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
                        DebugLogger.Log(
                            "AudioVisualizerWidget",
                            $"Loaded {preset.FrequencyBreakpoints.Count} frequency breakpoints"
                        );
                    }

                    // Load melodic breakpoints
                    MelodicBreakpoints.Clear();
                    if (preset.MelodicBreakpoints != null)
                    {
                        foreach (var breakpoint in preset.MelodicBreakpoints)
                        {
                            MelodicBreakpoints.Add(breakpoint);
                        }
                        DebugLogger.Log(
                            "AudioVisualizerWidget",
                            $"Loaded {preset.MelodicBreakpoints.Count} melodic breakpoints"
                        );
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
                    ThemeIndex = ClampThemeIndex(ThemeIndex),
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
                    FrequencyBreakpoints =
                        FrequencyBreakpoints.Count > 0
                            ? new System.Collections.Generic.List<FrequencyBreakpoint>(
                                FrequencyBreakpoints
                            )
                            : null,
                    MelodicBreakpoints =
                        MelodicBreakpoints.Count > 0
                            ? new System.Collections.Generic.List<MelodicBreakpoint>(
                                MelodicBreakpoints
                            )
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
            // Feature coming in future update - settings export to JSON
            DebugLogger.Log("AudioVisualizerSettingsWidget", "Export to JSON requested");
        }

        #endregion

        #region Startup Presets

        [ObservableProperty]
        private string? _startupIntroPresetName = "Default";

        [ObservableProperty]
        private string? _startupEndPresetName = "Default";

        partial void OnStartupIntroPresetNameChanged(string? value)
        {
            try
            {
                ShaderPresetHelper.Activate("intro", value ?? "Default");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "AudioVisualizerSettings",
                    $"❌ Failed to activate intro preset '{value}': {ex.Message}"
                );
            }
        }

        partial void OnStartupEndPresetNameChanged(string? value)
        {
            try
            {
                ShaderPresetHelper.Activate("normal", value ?? "Default");
            }
            catch (Exception ex)
            {
                Helpers.DebugLogger.LogError(
                    "AudioVisualizerSettings",
                    $"❌ Failed to activate normal preset '{value}': {ex.Message}"
                );
            }
        }

        private void ApplyMixerToEngine(MixerSettings settings)
        {
            if (_soundFlowAudioManager == null)
                return;
            _soundFlowAudioManager.SetTrackVolume(
                "Drums1",
                (float)(settings.Drums1.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Drums1",
                (float)Math.Clamp((settings.Drums1.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Drums1", settings.Drums1.Muted);

            _soundFlowAudioManager.SetTrackVolume(
                "Drums2",
                (float)(settings.Drums2.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Drums2",
                (float)Math.Clamp((settings.Drums2.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Drums2", settings.Drums2.Muted);

            _soundFlowAudioManager.SetTrackVolume("Bass1", (float)(settings.Bass1.Volume / 100.0));
            _soundFlowAudioManager.SetTrackPan(
                "Bass1",
                (float)Math.Clamp((settings.Bass1.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Bass1", settings.Bass1.Muted);

            _soundFlowAudioManager.SetTrackVolume("Bass2", (float)(settings.Bass2.Volume / 100.0));
            _soundFlowAudioManager.SetTrackPan(
                "Bass2",
                (float)Math.Clamp((settings.Bass2.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Bass2", settings.Bass2.Muted);

            _soundFlowAudioManager.SetTrackVolume(
                "Chords1",
                (float)(settings.Chords1.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Chords1",
                (float)Math.Clamp((settings.Chords1.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Chords1", settings.Chords1.Muted);

            _soundFlowAudioManager.SetTrackVolume(
                "Chords2",
                (float)(settings.Chords2.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Chords2",
                (float)Math.Clamp((settings.Chords2.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Chords2", settings.Chords2.Muted);

            _soundFlowAudioManager.SetTrackVolume(
                "Melody1",
                (float)(settings.Melody1.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Melody1",
                (float)Math.Clamp((settings.Melody1.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Melody1", settings.Melody1.Muted);

            _soundFlowAudioManager.SetTrackVolume(
                "Melody2",
                (float)(settings.Melody2.Volume / 100.0)
            );
            _soundFlowAudioManager.SetTrackPan(
                "Melody2",
                (float)Math.Clamp((settings.Melody2.Pan + 100.0) / 200.0, 0.0, 1.0)
            );
            _soundFlowAudioManager.SetTrackMuted("Melody2", settings.Melody2.Muted);
        }

        private async Task<string?> ShowNameDialogAsync(
            string title,
            string labelText,
            string watermark
        )
        {
            try
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 480,
                    Height = 220,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.Parse("#0D0D0D")),
                };
                var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
                var label = new TextBlock
                {
                    Text = labelText,
                    Foreground = new SolidColorBrush(Color.Parse("#FFD700")),
                    FontSize = 14,
                };
                var textBox = new TextBox
                {
                    Watermark = watermark,
                    FontSize = 14,
                    Padding = new Avalonia.Thickness(10),
                };
                var buttons = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                };
                var cancelBtn = new Button
                {
                    Content = "Cancel",
                    Width = 90,
                    Padding = new Avalonia.Thickness(10, 5),
                };
                var saveBtn = new Button
                {
                    Content = "Save",
                    Width = 90,
                    Padding = new Avalonia.Thickness(10, 5),
                    Background = new SolidColorBrush(Color.Parse("#FFD700")),
                    Foreground = new SolidColorBrush(Color.Parse("#0D0D0D")),
                };
                string? result = null;
                cancelBtn.Click += (s, e) => dialog.Close();
                saveBtn.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        result = textBox.Text;
                        dialog.Close();
                    }
                };
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
                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(saveBtn);
                panel.Children.Add(label);
                panel.Children.Add(textBox);
                panel.Children.Add(buttons);
                dialog.Content = panel;
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
            catch
            {
                return null;
            }
        }

        private async Task<string?> ShowSelectionDialogAsync(
            string title,
            string labelText,
            System.Collections.Generic.List<string> options
        )
        {
            try
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 520,
                    Height = 260,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.Parse("#0D0D0D")),
                };
                var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
                var label = new TextBlock
                {
                    Text = labelText,
                    Foreground = new SolidColorBrush(Color.Parse("#FFD700")),
                    FontSize = 14,
                };
                var combo = new ComboBox
                {
                    ItemsSource = options,
                    SelectedIndex = options.Count > 0 ? 0 : -1,
                };
                var buttons = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                };
                var cancelBtn = new Button
                {
                    Content = "Cancel",
                    Width = 90,
                    Padding = new Avalonia.Thickness(10, 5),
                };
                var loadBtn = new Button
                {
                    Content = "OK",
                    Width = 90,
                    Padding = new Avalonia.Thickness(10, 5),
                    Background = new SolidColorBrush(Color.Parse("#FFD700")),
                    Foreground = new SolidColorBrush(Color.Parse("#0D0D0D")),
                };
                string? result = null;
                cancelBtn.Click += (s, e) => dialog.Close();
                loadBtn.Click += (s, e) =>
                {
                    result = combo.SelectedItem as string;
                    dialog.Close();
                };
                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(loadBtn);
                panel.Children.Add(label);
                panel.Children.Add(combo);
                panel.Children.Add(buttons);
                dialog.Content = panel;
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
            catch
            {
                return null;
            }
        }

        #endregion

        #region Manual Transition Tester

        /// <summary>
        /// Applies manual transition between Preset A and Preset B at given progress
        /// </summary>
        /// <param name="progress">0.0 = Preset A, 1.0 = Preset B</param>
        private async void ApplyManualTransition(double progress)
        {
            // Prevent recursive calls
            if (_isApplyingManualTransition)
                return;

            // Skip if presets not selected
            if (
                string.IsNullOrWhiteSpace(ManualTransitionPresetA)
                || string.IsNullOrWhiteSpace(ManualTransitionPresetB)
            )
            {
                return;
            }

            _isApplyingManualTransition = true;
            try
            {
                // Build preset file paths
                var presetsDir = Path.Combine(AppContext.BaseDirectory, "Presets");
                var presetAPath = Path.Combine(presetsDir, $"{ManualTransitionPresetA}.json");
                var presetBPath = Path.Combine(presetsDir, $"{ManualTransitionPresetB}.json");

                // Check if files exist
                if (!File.Exists(presetAPath) || !File.Exists(presetBPath))
                {
                    DebugLogger.LogError(
                        "AudioVisualizerWidget",
                        $"One or both presets not found: {ManualTransitionPresetA}, {ManualTransitionPresetB}"
                    );
                    return;
                }

                // Load preset A
                var jsonA = await File.ReadAllTextAsync(presetAPath);
                var presetA = System.Text.Json.JsonSerializer.Deserialize<Models.VisualizerPreset>(
                    jsonA
                );

                // Load preset B
                var jsonB = await File.ReadAllTextAsync(presetBPath);
                var presetB = System.Text.Json.JsonSerializer.Deserialize<Models.VisualizerPreset>(
                    jsonB
                );

                if (presetA != null && presetB != null)
                {
                    // Simple interpolation of theme and colors for now
                    // Full shader parameter interpolation would go here

                    // LERP theme index (rounded to nearest int)
                    var lerpedThemeIndex = (int)
                        Math.Round(
                            presetA.ThemeIndex * (1 - progress) + presetB.ThemeIndex * progress
                        );

                    // Apply the interpolated theme
                    ThemeIndex = ClampThemeIndex(lerpedThemeIndex);

                    DebugLogger.Log(
                        "AudioVisualizerWidget",
                        $"Manual transition {progress:P0}: {ManualTransitionPresetA} → {ManualTransitionPresetB} (Theme: {lerpedThemeIndex})"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AudioVisualizerWidget",
                    $"Failed to apply manual transition: {ex.Message}"
                );
            }
            finally
            {
                _isApplyingManualTransition = false;
            }
        }

        /// <summary>
        /// Plays automatic transition from 0% to 100% over 3 seconds using Avalonia Transitions
        /// </summary>
        [RelayCommand]
        private async Task PlayManualTransition()
        {
            if (
                string.IsNullOrWhiteSpace(ManualTransitionPresetA)
                || string.IsNullOrWhiteSpace(ManualTransitionPresetB)
            )
            {
                DebugLogger.Log(
                    "AudioVisualizerWidget",
                    "Manual transition skipped: select Preset A and Preset B first"
                );
                return;
            }
            // Reset to 0% first
            ManualTransitionProgress = 0;

            // Wait a frame to ensure the transition system sees the 0 value
            await Task.Delay(50);

            // Set to 100% - the DoubleTransition in XAML will smoothly animate from 0 to 100
            ManualTransitionProgress = 100;

            DebugLogger.Log(
                "AudioVisualizerWidget",
                "Manual transition animation started (Avalonia Transitions)"
            );
        }

        #endregion

        #region Base Widget Overrides

        private bool _syncingFromShader;

        protected override void OnExpanded()
        {
            base.OnExpanded();
            SyncFromShader();
        }

        protected override void OnMinimized()
        {
            base.OnMinimized();
            // Could auto-save settings when minimized if needed
        }

        private void SyncFromShader()
        {
            if (_ownerControl == null)
                return;
            var mainMenu = _ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null)
                return;
            _syncingFromShader = true;
            TimeValue = mainMenu.GetTimeSpeed();
            SpinTimeValue = mainMenu.GetSpinTimeSpeed();
            _syncingFromShader = false;
        }

        #endregion
    }
}
