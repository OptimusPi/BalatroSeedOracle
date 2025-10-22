using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.ViewModels
{
    public partial class FrequencyDebugWidgetViewModel : BaseWidgetViewModel
    {
        private Control? _ownerControl;
        private SoundFlowAudioManager? _audioManager;
        private CancellationTokenSource? _updateCancellation;
        private Task? _updateTask;

        private readonly string[] _trackNames =
        {
            "Bass1", "Bass2",
            "Drums1", "Drums2",
            "Chords1", "Chords2",
            "Melody1", "Melody2"
        };

        [ObservableProperty]
        private int _selectedTrackIndex = 2; // Default to Drums1

        [ObservableProperty]
        private double _bassAvg;

        [ObservableProperty]
        private double _bassPeak;

        [ObservableProperty]
        private double _midAvg;

        [ObservableProperty]
        private double _midPeak;

        [ObservableProperty]
        private double _highAvg;

        [ObservableProperty]
        private double _highPeak;

        // Captured max values
        [ObservableProperty]
        private double _bassAvgMax;

        [ObservableProperty]
        private double _bassPeakMax;

        [ObservableProperty]
        private double _midAvgMax;

        [ObservableProperty]
        private double _midPeakMax;

        [ObservableProperty]
        private double _highAvgMax;

        [ObservableProperty]
        private double _highPeakMax;

        public FrequencyDebugWidgetViewModel()
        {
            WidgetTitle = "Frequency Analyzer";
            WidgetIcon = "📊";
            IsMinimized = true;

            // Position below DayLatro widget (with proper spacing!)
            PositionX = 20;
            PositionY = 370;
        }

        public void OnAttached(Control ownerControl)
        {
            _ownerControl = ownerControl;

            // Find BalatroMainMenu to get audio manager
            var mainMenu = ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu?.ViewModel?.AudioManager is SoundFlowAudioManager soundFlow)
            {
                _audioManager = soundFlow;
                StartFrequencyUpdates();
            }
        }

        public void OnDetached()
        {
            StopFrequencyUpdates();
            _ownerControl = null;
            _audioManager = null;
        }

        private void StartFrequencyUpdates()
        {
            if (_audioManager == null || _updateTask != null)
                return;

            _updateCancellation = new CancellationTokenSource();
            _updateTask = Task.Run(async () =>
            {
                while (!_updateCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Check if audio manager is still available (prevents crash during shutdown)
                        if (_audioManager == null)
                            break;

                        var trackName = _trackNames[SelectedTrackIndex];
                        var bands = _audioManager.GetFrequencyBands(trackName);

                        // Update UI on UI thread
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            BassAvg = bands.BassAvg;
                            BassPeak = bands.BassPeak;
                            MidAvg = bands.MidAvg;
                            MidPeak = bands.MidPeak;
                            HighAvg = bands.HighAvg;
                            HighPeak = bands.HighPeak;

                            // Capture max values
                            if (bands.BassAvg > BassAvgMax) BassAvgMax = bands.BassAvg;
                            if (bands.BassPeak > BassPeakMax) BassPeakMax = bands.BassPeak;
                            if (bands.MidAvg > MidAvgMax) MidAvgMax = bands.MidAvg;
                            if (bands.MidPeak > MidPeakMax) MidPeakMax = bands.MidPeak;
                            if (bands.HighAvg > HighAvgMax) HighAvgMax = bands.HighAvg;
                            if (bands.HighPeak > HighPeakMax) HighPeakMax = bands.HighPeak;
                        });

                        // ~60 FPS update rate
                        await Task.Delay(16, _updateCancellation.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Audio manager was disposed during update - exit cleanly
                        if (_audioManager == null)
                            break;

                        Helpers.DebugLogger.LogError("FrequencyDebugWidget", $"Update error: {ex.Message}");
                        await Task.Delay(100, _updateCancellation.Token);
                    }
                }
            }, _updateCancellation.Token);
        }

        private void StopFrequencyUpdates()
        {
            _updateCancellation?.Cancel();
            _updateTask?.Wait(TimeSpan.FromSeconds(1));
            _updateCancellation?.Dispose();
            _updateCancellation = null;
            _updateTask = null;
        }

        protected override void OnExpanded()
        {
            base.OnExpanded();
            StartFrequencyUpdates();
        }

        protected override void OnMinimized()
        {
            base.OnMinimized();
            StopFrequencyUpdates();
        }

        [RelayCommand]
        private void ResetMaxValues()
        {
            BassAvgMax = 0;
            BassPeakMax = 0;
            MidAvgMax = 0;
            MidPeakMax = 0;
            HighAvgMax = 0;
            HighPeakMax = 0;
        }
    }
}
