using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Transition Designer Widget - Design and test audio/visual transitions
    /// </summary>
    public partial class TransitionDesignerWidgetViewModel : BaseWidgetViewModel
    {
        public TransitionDesignerWidgetViewModel()
        {
            // Configure widget appearance
            WidgetTitle = "Transition Designer";
            WidgetIcon = "🎬";
            IsMinimized = true;

            PositionX = 20;
            PositionY = 500; // Below other widgets

            Width = 420;
            Height = 550;

            // Initialize collections
            AudioMixOptions = new ObservableCollection<string> { "Default" };
            VisualPresetOptions = new ObservableCollection<string> { "Default" };
            EasingOptions = new ObservableCollection<string>
            {
                "Linear",
                "CubicEaseIn",
                "CubicEaseOut",
                "CubicEaseInOut",
                "QuadraticEaseIn",
                "QuadraticEaseOut",
                "QuadraticEaseInOut"
            };

            // Set defaults
            SelectedAudioMixA = "Default";
            SelectedAudioMixB = "Default";
            SelectedVisualPresetA = "Default";
            SelectedVisualPresetB = "Default";
            SelectedEasing = "CubicEaseOut";
            TransitionProgress = 0.0;
        }

        // Audio Mix Options
        [ObservableProperty]
        private ObservableCollection<string> _audioMixOptions;

        [ObservableProperty]
        private string _selectedAudioMixA = "Default";

        [ObservableProperty]
        private string _selectedAudioMixB = "Default";

        // Visual Preset Options
        [ObservableProperty]
        private ObservableCollection<string> _visualPresetOptions;

        [ObservableProperty]
        private string _selectedVisualPresetA = "Default";

        [ObservableProperty]
        private string _selectedVisualPresetB = "Default";

        // Transition Progress
        [ObservableProperty]
        private double _transitionProgress;

        [ObservableProperty]
        private bool _isTestRunning;

        public string TransitionProgressText => $"{TransitionProgress:P0} ({(TransitionProgress == 0.0 ? "Pure A" : TransitionProgress == 1.0 ? "Pure B" : "Lerp")})";

        // Easing
        [ObservableProperty]
        private ObservableCollection<string> _easingOptions;

        [ObservableProperty]
        private string _selectedEasing = "CubicEaseOut";

        // Commands
        [RelayCommand]
        private async Task TestTransition()
        {
            IsTestRunning = true;

            // Animate from 0.0 to 1.0 over 2 seconds
            var duration = TimeSpan.FromSeconds(2);
            var steps = 60;
            var stepDelay = duration.TotalMilliseconds / steps;

            for (int i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                // Apply easing (for now, just linear - can add easing functions later)
                TransitionProgress = t;
                OnPropertyChanged(nameof(TransitionProgressText));

                await Task.Delay((int)stepDelay);

                if (!IsTestRunning)
                    break;
            }

            IsTestRunning = false;
        }

        [RelayCommand]
        private void SaveTransition()
        {
            // TODO: Implement transition saving
            Helpers.DebugLogger.Log("TransitionDesigner", "Save Transition requested");
        }

        partial void OnTransitionProgressChanged(double value)
        {
            OnPropertyChanged(nameof(TransitionProgressText));

            // TODO: Apply lerp between A and B based on progress
            // This will interpolate audio mix and visual preset
        }
    }
}
