using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Transition Designer Widget - Design and test audio/visual transitions
    /// </summary>
    public partial class TransitionDesignerWidgetViewModel : BaseWidgetViewModel
    {
        private const double DefaultDuration = 2.0;
        private const string DefaultEasing = "Linear";
        private const string NoneOption = "(none)";

        private readonly TransitionService? _transitionService;
        private readonly TriggerService? _triggerService;
        private System.Timers.Timer? _audioUpdateTimer;
        private double _smoothedAudioProgress = 0.0;

        public TransitionDesignerWidgetViewModel(
            TransitionService? transitionService = null,
            TriggerService? triggerService = null
        )
        {
            _transitionService = transitionService;
            _triggerService = triggerService;

            // HIGH #3 FIX: Log warning if TransitionService wasn't injected
            if (_transitionService is null)
            {
                DebugLogger.Log(
                    "TransitionDesigner",
                    "Warning: TransitionService not injected - using fallback animation"
                );
            }

            // Configure widget appearance
            WidgetTitle = "Transition Designer";
            WidgetIcon = "MovieOpen";
            IsMinimized = true;

            PositionX = 20;
            PositionY = 500;

            Width = 420;
            Height = 650; // Increased height for music controls

            // Initialize easing options
            EasingOptions = new ObservableCollection<string> { "Linear", "EaseIn", "EaseOut", "EaseInOut" };

            // Load real preset options
            LoadPresetOptions();
            LoadAudioTriggerOptions();

            // Set defaults
            SelectedAudioMixA = AudioMixOptions.FirstOrDefault() ?? NoneOption;
            SelectedAudioMixB = AudioMixOptions.FirstOrDefault() ?? NoneOption;
            SelectedVisualPresetA = VisualPresetOptions.FirstOrDefault() ?? NoneOption;
            SelectedVisualPresetB = VisualPresetOptions.FirstOrDefault() ?? NoneOption;
            SelectedEasing = DefaultEasing;
            TransitionProgress = 0.0;
            TransitionDuration = DefaultDuration;
            MusicActivated = false;
            TriggerThreshold = 0.5;
            TriggerMax = 1.0;
            AudioSmoothing = 0.8;
        }

        private void LoadPresetOptions()
        {
            // Load audio mixer presets
            var mixerNames = MixerHelper.LoadAllMixerNames();
            AudioMixOptions = new ObservableCollection<string>(mixerNames.Count > 0 ? mixerNames : new[] { "(none)" });

            // Load visual shader presets
            var shaderNames = ShaderPresetHelper.ListNames();
            VisualPresetOptions = new ObservableCollection<string>(
                shaderNames.Count > 0 ? shaderNames : new[] { "(none)" }
            );

            DebugLogger.Log(
                "TransitionDesigner",
                $"Loaded {mixerNames.Count} mixer presets, {shaderNames.Count} visual presets"
            );
        }

        private void LoadAudioTriggerOptions()
        {
            if (_triggerService == null)
            {
                AudioTriggerOptions = new ObservableCollection<string> { NoneOption };
                return;
            }

            var triggers = _triggerService.GetTriggersByType("Audio").Select(t => t.Name).OrderBy(n => n).ToList();

            AudioTriggerOptions = new ObservableCollection<string>(
                triggers.Count > 0 ? new[] { NoneOption }.Concat(triggers).ToList() : new[] { NoneOption }
            );

            DebugLogger.Log("TransitionDesigner", $"Loaded {triggers.Count} audio triggers");
        }

        // Audio Mix Options
        [ObservableProperty]
        private ObservableCollection<string> _audioMixOptions = new();

        [ObservableProperty]
        private string _selectedAudioMixA = "(none)";

        [ObservableProperty]
        private string _selectedAudioMixB = "(none)";

        // Visual Preset Options
        [ObservableProperty]
        private ObservableCollection<string> _visualPresetOptions = new();

        [ObservableProperty]
        private string _selectedVisualPresetA = "(none)";

        [ObservableProperty]
        private string _selectedVisualPresetB = "(none)";

        // Transition Progress
        [ObservableProperty]
        private double _transitionProgress;

        [ObservableProperty]
        private double _transitionDuration = 2.0;

        [ObservableProperty]
        private bool _isTestRunning;

        public string TransitionProgressText =>
            $"{TransitionProgress:P0} ({(TransitionProgress == 0.0 ? "A" : TransitionProgress >= 1.0 ? "B" : "Lerp")})";

        // Easing
        [ObservableProperty]
        private ObservableCollection<string> _easingOptions = new();

        [ObservableProperty]
        private string _selectedEasing = "Linear";

        // Duration options
        public ObservableCollection<string> DurationOptions { get; } = new() { "0.5s", "1s", "2s", "3s", "5s", "10s" };

        [ObservableProperty]
        private string _selectedDuration = "2s";

        partial void OnSelectedDurationChanged(string value)
        {
            if (double.TryParse(value.TrimEnd('s'), out var seconds))
            {
                TransitionDuration = seconds;
            }
        }

        // Music-Activated Transition Settings
        [ObservableProperty]
        private ObservableCollection<string> _audioTriggerOptions = new();

        [ObservableProperty]
        private string _selectedAudioTrigger = "(none)";

        [ObservableProperty]
        private bool _musicActivated;

        [ObservableProperty]
        private double _triggerThreshold = 0.5;

        [ObservableProperty]
        private double _triggerMax = 1.0;

        [ObservableProperty]
        private double _audioSmoothing = 0.8;

        partial void OnMusicActivatedChanged(bool value)
        {
            if (value)
            {
                StartAudioMonitoring();
            }
            else
            {
                StopAudioMonitoring();
            }
        }

        [RelayCommand]
        private void RefreshPresets()
        {
            LoadPresetOptions();
        }

        [RelayCommand]
        private async Task TestTransition()
        {
            if (IsTestRunning)
            {
                IsTestRunning = false;
                StopAudioMonitoring();
                return;
            }

            IsTestRunning = true;
            TransitionProgress = 0.0;
            _smoothedAudioProgress = 0.0;

            // Load presets
            var presetA = ShaderPresetHelper.Load(SelectedVisualPresetA);
            var presetB = ShaderPresetHelper.Load(SelectedVisualPresetB);

            if (presetA is null || presetB is null)
            {
                DebugLogger.LogError(
                    "TransitionDesigner",
                    $"Cannot test: preset A or B not found ({SelectedVisualPresetA}, {SelectedVisualPresetB})"
                );
                IsTestRunning = false;
                return;
            }

            // Use TransitionService if available
            if (_transitionService is not null)
            {
                _transitionService.StartTransition(
                    presetA,
                    presetB,
                    ApplyShaderParameters,
                    MusicActivated ? null : TimeSpan.FromSeconds(TransitionDuration)
                );

                DebugLogger.Log(
                    "TransitionDesigner",
                    $"Started transition via TransitionService: {SelectedVisualPresetA} → {SelectedVisualPresetB} (MusicActivated={MusicActivated})"
                );
            }

            if (MusicActivated)
            {
                // Music-activated mode: progress is driven by audio trigger
                StartAudioMonitoring();
            }
            else
            {
                // Time-based mode: progress is driven by elapsed time
                var steps = 60;
                var stepDelay = (int)(TransitionDuration * 1000 / steps);

                for (int i = 0; i <= steps; i++)
                {
                    if (!IsTestRunning)
                        break;

                    var t = (double)i / steps;
                    TransitionProgress = ApplyEasing(t);

                    await Task.Delay(stepDelay);
                }

                IsTestRunning = false;
                TransitionProgress = 1.0;
            }
        }

        private void StartAudioMonitoring()
        {
            if (_audioUpdateTimer != null)
                return;

            _audioUpdateTimer = new System.Timers.Timer(16); // ~60 FPS
            _audioUpdateTimer.Elapsed += (s, e) => UpdateAudioDrivenProgress();
            _audioUpdateTimer.Start();

            DebugLogger.Log("TransitionDesigner", "Started audio monitoring for music-activated transition");
        }

        private void StopAudioMonitoring()
        {
            if (_audioUpdateTimer != null)
            {
                _audioUpdateTimer.Stop();
                _audioUpdateTimer.Dispose();
                _audioUpdateTimer = null;
            }
        }

        private void UpdateAudioDrivenProgress()
        {
            if (!IsTestRunning || !MusicActivated || _triggerService is null)
                return;

            if (string.IsNullOrEmpty(SelectedAudioTrigger) || SelectedAudioTrigger == NoneOption)
                return;

            var trigger = _triggerService.GetTrigger(SelectedAudioTrigger);
            if (trigger is null)
                return;

            // Get trigger intensity (0-1)
            double triggerIntensity = trigger.GetIntensity();

            // Map trigger intensity to transition progress
            // Below threshold = 0% (Preset A)
            // At threshold = 0%
            // At max = 100% (Preset B)
            double rawProgress = 0.0;
            if (triggerIntensity > TriggerThreshold)
            {
                var range = TriggerMax - TriggerThreshold;
                if (range > 0)
                {
                    rawProgress = Math.Clamp((triggerIntensity - TriggerThreshold) / range, 0.0, 1.0);
                }
            }

            // Apply smoothing (exponential moving average)
            _smoothedAudioProgress = AudioSmoothing * _smoothedAudioProgress + (1.0 - AudioSmoothing) * rawProgress;

            // Apply easing
            TransitionProgress = ApplyEasing(_smoothedAudioProgress);

            // Update TransitionService progress to drive shader interpolation
            if (_transitionService is not null)
            {
                _transitionService.SetProgress((float)TransitionProgress);
            }
        }

        private void ApplyShaderParameters(ShaderParameters parameters)
        {
            // This callback is called by TransitionService during the transition
            // The shader parameters are applied to BalatroShaderBackground via TransitionService
        }

        private double ApplyEasing(double t)
        {
            return SelectedEasing switch
            {
                "EaseIn" => t * t,
                "EaseOut" => 1 - (1 - t) * (1 - t),
                "EaseInOut" => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2,
                _ => t, // Linear
            };
        }

        [RelayCommand]
        private void StopTransition()
        {
            IsTestRunning = false;
            StopAudioMonitoring();
            _transitionService?.StopTransition();
        }

        [RelayCommand]
        private void SaveTransition()
        {
            try
            {
                var defaultName = $"Transition_{DateTime.Now:yyyyMMdd_HHmmss}";

                var preset = new TransitionPreset
                {
                    Name = defaultName,
                    VisualPresetAName = SelectedVisualPresetA,
                    VisualPresetBName = SelectedVisualPresetB,
                    MixAName = SelectedAudioMixA,
                    MixBName = SelectedAudioMixB,
                    Easing = SelectedEasing,
                    Duration = TransitionDuration > 0 ? TransitionDuration : DefaultDuration,
                    MusicActivated = MusicActivated,
                    AudioTriggerName =
                        MusicActivated && SelectedAudioTrigger != NoneOption ? SelectedAudioTrigger : null,
                    TriggerThreshold = TriggerThreshold,
                    TriggerMax = TriggerMax,
                    AudioSmoothing = AudioSmoothing,
                };

                if (TransitionPresetHelper.Save(preset))
                {
                    DebugLogger.Log(
                        "TransitionDesigner",
                        $"Saved transition: {defaultName} (MusicActivated={MusicActivated})"
                    );
                }
                else
                {
                    DebugLogger.LogError("TransitionDesigner", "Failed to save transition");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TransitionDesigner", $"SaveTransition error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void LoadTransition()
        {
            try
            {
                var names = TransitionPresetHelper.ListNames();
                if (names.Count == 0)
                {
                    DebugLogger.Log("TransitionDesigner", "No saved transitions found");
                    return;
                }

                // Load the most recent one (last in list)
                var preset = TransitionPresetHelper.Load(names.Last());
                if (preset is not null)
                {
                    SelectedVisualPresetA =
                        preset.VisualPresetAName ?? VisualPresetOptions.FirstOrDefault() ?? NoneOption;
                    SelectedVisualPresetB =
                        preset.VisualPresetBName ?? VisualPresetOptions.FirstOrDefault() ?? NoneOption;
                    SelectedAudioMixA = preset.MixAName ?? AudioMixOptions.FirstOrDefault() ?? NoneOption;
                    SelectedAudioMixB = preset.MixBName ?? AudioMixOptions.FirstOrDefault() ?? NoneOption;
                    SelectedEasing = preset.Easing ?? DefaultEasing;
                    TransitionDuration = preset.Duration > 0 ? preset.Duration : DefaultDuration;

                    // Load music-activated settings
                    MusicActivated = preset.MusicActivated;
                    SelectedAudioTrigger = preset.AudioTriggerName ?? NoneOption;
                    TriggerThreshold = preset.TriggerThreshold;
                    TriggerMax = preset.TriggerMax;
                    AudioSmoothing = preset.AudioSmoothing;

                    DebugLogger.Log(
                        "TransitionDesigner",
                        $"Loaded transition: {preset.Name} (MusicActivated={MusicActivated})"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TransitionDesigner", $"LoadTransition error: {ex.Message}");
            }
        }

        partial void OnTransitionProgressChanged(double value)
        {
            OnPropertyChanged(nameof(TransitionProgressText));
        }

        [RelayCommand]
        private void RefreshAudioTriggers()
        {
            LoadAudioTriggerOptions();
        }
    }
}
