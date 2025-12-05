using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Components;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Transition Designer Widget - Design and test audio/visual transitions
    /// </summary>
    public partial class TransitionDesignerWidgetViewModel : BaseWidgetViewModel, IWidget
    {
        private const double DefaultDuration = 2.0;
        private const string DefaultEasing = "Linear";
        private const string NoneOption = "(none)";

        private readonly TransitionService? _transitionService;

        public TransitionDesignerWidgetViewModel(TransitionService? transitionService = null)
        {
            _transitionService = transitionService;

            // HIGH #3 FIX: Log warning if TransitionService wasn't injected
            if (_transitionService == null)
            {
                DebugLogger.Log(
                    "TransitionDesigner",
                    "Warning: TransitionService not injected - using fallback animation"
                );
            }

            // Configure widget appearance
            WidgetTitle = "Transition Designer";
            WidgetIcon = "🎬";
            IsMinimized = true;

            PositionX = 20;
            PositionY = 500;

            Width = 420;
            Height = 550;

            // Initialize easing options
            EasingOptions = new ObservableCollection<string>
            {
                "Linear",
                "EaseIn",
                "EaseOut",
                "EaseInOut",
            };

            // Load real preset options
            LoadPresetOptions();

            // Set defaults
            SelectedAudioMixA = AudioMixOptions.FirstOrDefault() ?? NoneOption;
            SelectedAudioMixB = AudioMixOptions.FirstOrDefault() ?? NoneOption;
            SelectedVisualPresetA = VisualPresetOptions.FirstOrDefault() ?? NoneOption;
            SelectedVisualPresetB = VisualPresetOptions.FirstOrDefault() ?? NoneOption;
            SelectedEasing = DefaultEasing;
            TransitionProgress = 0.0;
            TransitionDuration = DefaultDuration;
        }

        private void LoadPresetOptions()
        {
            // Load audio mixer presets
            var mixerNames = MixerHelper.LoadAllMixerNames();
            AudioMixOptions = new ObservableCollection<string>(
                mixerNames.Count > 0 ? mixerNames : new[] { "(none)" }
            );

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
        public ObservableCollection<string> DurationOptions { get; } = new()
        {
            "0.5s", "1s", "2s", "3s", "5s", "10s"
        };

        [ObservableProperty]
        private string _selectedDuration = "2s";

        partial void OnSelectedDurationChanged(string value)
        {
            if (double.TryParse(value.TrimEnd('s'), out var seconds))
            {
                TransitionDuration = seconds;
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
                return;
            }

            IsTestRunning = true;
            TransitionProgress = 0.0;

            // Load presets
            var presetA = ShaderPresetHelper.Load(SelectedVisualPresetA);
            var presetB = ShaderPresetHelper.Load(SelectedVisualPresetB);

            if (presetA == null || presetB == null)
            {
                DebugLogger.LogError(
                    "TransitionDesigner",
                    $"Cannot test: preset A or B not found ({SelectedVisualPresetA}, {SelectedVisualPresetB})"
                );
                IsTestRunning = false;
                return;
            }

            // Use TransitionService if available
            if (_transitionService != null)
            {
                _transitionService.StartTransition(
                    presetA,
                    presetB,
                    ApplyShaderParameters,
                    TimeSpan.FromSeconds(TransitionDuration)
                );

                DebugLogger.Log(
                    "TransitionDesigner",
                    $"Started transition via TransitionService: {SelectedVisualPresetA} → {SelectedVisualPresetB}"
                );
            }

            // HIGH #1 & #2 FIX: Always run animation loop to update progress and track completion
            var steps = 60;
            var stepDelay = (int)(TransitionDuration * 1000 / steps);
            var startTime = DateTime.UtcNow;

            for (int i = 0; i <= steps; i++)
            {
                if (!IsTestRunning)
                    break;

                var t = (double)i / steps;
                TransitionProgress = ApplyEasing(t);

                await Task.Delay(stepDelay);
            }

            // Only set false AFTER animation completes
            IsTestRunning = false;
            TransitionProgress = 1.0;
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
                };

                if (TransitionPresetHelper.Save(preset))
                {
                    DebugLogger.Log("TransitionDesigner", $"Saved transition: {defaultName}");
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
                if (preset != null)
                {
                    SelectedVisualPresetA =
                        preset.VisualPresetAName
                        ?? VisualPresetOptions.FirstOrDefault()
                        ?? NoneOption;
                    SelectedVisualPresetB =
                        preset.VisualPresetBName
                        ?? VisualPresetOptions.FirstOrDefault()
                        ?? NoneOption;
                    SelectedAudioMixA =
                        preset.MixAName ?? AudioMixOptions.FirstOrDefault() ?? NoneOption;
                    SelectedAudioMixB =
                        preset.MixBName ?? AudioMixOptions.FirstOrDefault() ?? NoneOption;
                    SelectedEasing = preset.Easing ?? DefaultEasing;
                    TransitionDuration = preset.Duration > 0 ? preset.Duration : DefaultDuration;

                    DebugLogger.Log("TransitionDesigner", $"Loaded transition: {preset.Name}");
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

        #region IWidget Implementation

        public string Id => "transition-designer";
        public string Title 
        {
            get => WidgetTitle;
            set => WidgetTitle = value;
        }
        public string IconResource { get; set; } = "MovieFilter";
        public WidgetState State => IsMinimized ? WidgetState.Minimized : WidgetState.Open;
        public int NotificationCount { get; set; } = 0;
        public double ProgressValue { get; set; } = 0.0;
        public bool ShowCloseButton { get; set; } = true;
        public bool ShowPopOutButton { get; set; } = false;
        public Point Position 
        { 
            get => new Point(PositionX, PositionY);
            set { PositionX = value.X; PositionY = value.Y; }
        }
        public Size Size 
        { 
            get => new Size(Width, Height);
            set { Width = value.Width; Height = value.Height; }
        }
        public bool IsDocked { get; set; } = false;
        public DockPosition DockPosition { get; set; } = DockPosition.None;
        public object? PersistedState { get; set; }

        #pragma warning disable CS0067
        public event EventHandler<WidgetStateChangedEventArgs>? StateChanged;
        public event EventHandler<EventArgs>? CloseRequested;
        #pragma warning restore CS0067

        public async Task OpenAsync()
        {
            ExpandCommand?.Execute(null);
        }

        public async Task MinimizeAsync()
        {
            MinimizeCommand?.Execute(null);
        }

        public async Task CloseAsync()
        {
            CloseCommand?.Execute(null);
        }

        public UserControl GetContentView()
        {
            var widget = new Components.TransitionDesignerWidget();
            widget.DataContext = this;
            return widget;
        }

        public void UpdateNotifications(int count)
        {
            NotificationCount = Math.Max(0, count);
        }

        public void UpdateProgress(double value)
        {
            ProgressValue = Math.Clamp(value, 0.0, 1.0);
        }

        public async Task<object?> SaveStateAsync()
        {
            // Save transition designer state
            return await Task.FromResult(PersistedState);
        }

        public async Task LoadStateAsync(object? state)
        {
            PersistedState = state;
            await Task.CompletedTask;
        }

        #endregion
    }
}
