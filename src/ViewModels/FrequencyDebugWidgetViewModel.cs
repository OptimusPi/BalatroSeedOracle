using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            "Bass1",
            "Bass2",
            "Drums1",
            "Drums2",
            "Chords1",
            "Chords2",
            "Melody1",
            "Melody2",
        };

        // Debounced save state
        private CancellationTokenSource? _saveCancellation;
        private const int SAVE_DEBOUNCE_MS = 250;

        private static readonly string MetadataDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "Audio",
            "Metadata"
        );

        private static readonly JsonSerializerOptions JsonOptions =
            new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, };

        [ObservableProperty]
        private int _selectedTrackIndex = 2; // Default to Drums1

        partial void OnSelectedTrackIndexChanged(int value)
        {
            LoadMetadataForCurrentTrack();
        }

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

        // Beat detection thresholds
        [ObservableProperty]
        private double _bassThreshold = 0.5;

        partial void OnBassThresholdChanged(double value)
        {
            DebouncedSaveMetadata();
        }

        [ObservableProperty]
        private double _midThreshold = 0.3;

        partial void OnMidThresholdChanged(double value)
        {
            DebouncedSaveMetadata();
        }

        [ObservableProperty]
        private double _highThreshold = 0.2;

        partial void OnHighThresholdChanged(double value)
        {
            DebouncedSaveMetadata();
        }

        // LED brightness values (0.0 to 1.0, decays naturally)
        [ObservableProperty]
        private double _bassBeatBrightness = 0.0;

        [ObservableProperty]
        private double _midBeatBrightness = 0.0;

        [ObservableProperty]
        private double _highBeatBrightness = 0.0;

        // Decay rate per frame (0.75 = 25% decay per frame)
        private const double DECAY_RATE = 0.75;

        public FrequencyDebugWidgetViewModel()
        {
            WidgetTitle = "Frequency Analyzer";
            WidgetIcon = "📊";
            IsMinimized = true;

            // Set fixed position for FrequencyDebug widget - fifth position (90px spacing)
            PositionX = 20;
            PositionY = 440;

            // Ensure metadata directory exists
            EnsureMetadataDirectoryExists();

            // Load metadata for default track
            LoadMetadataForCurrentTrack();
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
            _updateTask = Task.Run(
                async () =>
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
                                if (bands.BassAvg > BassAvgMax)
                                    BassAvgMax = bands.BassAvg;
                                if (bands.BassPeak > BassPeakMax)
                                    BassPeakMax = bands.BassPeak;
                                if (bands.MidAvg > MidAvgMax)
                                    MidAvgMax = bands.MidAvg;
                                if (bands.MidPeak > MidPeakMax)
                                    MidPeakMax = bands.MidPeak;
                                if (bands.HighAvg > HighAvgMax)
                                    HighAvgMax = bands.HighAvg;
                                if (bands.HighPeak > HighPeakMax)
                                    HighPeakMax = bands.HighPeak;

                                // Beat detection with decay
                                // If beat detected, snap to 100%. Otherwise, decay by 25% per frame.
                                if (bands.BassPeak > BassThreshold)
                                    BassBeatBrightness = 1.0;
                                else
                                    BassBeatBrightness *= DECAY_RATE;

                                if (bands.MidPeak > MidThreshold)
                                    MidBeatBrightness = 1.0;
                                else
                                    MidBeatBrightness *= DECAY_RATE;

                                if (bands.HighPeak > HighThreshold)
                                    HighBeatBrightness = 1.0;
                                else
                                    HighBeatBrightness *= DECAY_RATE;
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

                            Helpers.DebugLogger.LogError(
                                "FrequencyDebugWidget",
                                $"Update error: {ex.Message}"
                            );
                            await Task.Delay(100, _updateCancellation.Token);
                        }
                    }
                },
                _updateCancellation.Token
            );
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

            // Save after resetting
            DebouncedSaveMetadata();
        }

        #region Metadata Save/Load

        private void EnsureMetadataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(MetadataDirectory))
                {
                    Directory.CreateDirectory(MetadataDirectory);
                    DebugLogger.Log(
                        "FrequencyDebugWidget",
                        $"Created metadata directory: {MetadataDirectory}"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FrequencyDebugWidget",
                    $"Failed to create metadata directory: {ex.Message}"
                );
            }
        }

        private string GetMetadataFilePath(string trackName)
        {
            return Path.Combine(MetadataDirectory, $"{trackName.ToLower()}.meta.json");
        }

        private void LoadMetadataForCurrentTrack()
        {
            if (SelectedTrackIndex < 0 || SelectedTrackIndex >= _trackNames.Length)
                return;

            var trackName = _trackNames[SelectedTrackIndex];
            var filePath = GetMetadataFilePath(trackName);

            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var metadata = JsonSerializer.Deserialize<TrackMetadata>(json, JsonOptions);

                    if (metadata != null)
                    {
                        // Cancel any pending saves before loading to avoid save loop
                        _saveCancellation?.Cancel();
                        _saveCancellation?.Dispose();
                        _saveCancellation = null;

                        // Load values using properties (will trigger property changed but save is cancelled)
                        BassThreshold = metadata.BassThreshold;
                        MidThreshold = metadata.MidThreshold;
                        HighThreshold = metadata.HighThreshold;
                        BassAvgMax = metadata.BassAvgMax;
                        BassPeakMax = metadata.BassPeakMax;
                        MidAvgMax = metadata.MidAvgMax;
                        MidPeakMax = metadata.MidPeakMax;
                        HighAvgMax = metadata.HighAvgMax;
                        HighPeakMax = metadata.HighPeakMax;

                        DebugLogger.Log(
                            "FrequencyDebugWidget",
                            $"Loaded metadata for {trackName}"
                        );
                    }
                }
                else
                {
                    DebugLogger.Log(
                        "FrequencyDebugWidget",
                        $"No metadata file found for {trackName}, using defaults"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FrequencyDebugWidget",
                    $"Failed to load metadata for {trackName}: {ex.Message}"
                );
            }
        }

        private void DebouncedSaveMetadata()
        {
            // Cancel any existing pending save
            _saveCancellation?.Cancel();
            _saveCancellation?.Dispose();

            _saveCancellation = new CancellationTokenSource();
            var token = _saveCancellation.Token;

            Task.Run(
                async () =>
                {
                    try
                    {
                        // Wait for debounce period (250ms)
                        await Task.Delay(SAVE_DEBOUNCE_MS, token);

                        // If not cancelled, save the metadata
                        if (!token.IsCancellationRequested)
                        {
                            SaveMetadataForCurrentTrack();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when user changes value again before save completes
                    }
                },
                token
            );
        }

        private void SaveMetadataForCurrentTrack()
        {
            if (SelectedTrackIndex < 0 || SelectedTrackIndex >= _trackNames.Length)
                return;

            var trackName = _trackNames[SelectedTrackIndex];
            var filePath = GetMetadataFilePath(trackName);

            try
            {
                var metadata = new TrackMetadata
                {
                    TrackName = trackName,
                    BassThreshold = BassThreshold,
                    MidThreshold = MidThreshold,
                    HighThreshold = HighThreshold,
                    BassAvgMax = BassAvgMax,
                    BassPeakMax = BassPeakMax,
                    MidAvgMax = MidAvgMax,
                    MidPeakMax = MidPeakMax,
                    HighAvgMax = HighAvgMax,
                    HighPeakMax = HighPeakMax,
                };

                var json = JsonSerializer.Serialize(metadata, JsonOptions);
                File.WriteAllText(filePath, json);

                DebugLogger.Log("FrequencyDebugWidget", $"Saved metadata for {trackName}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FrequencyDebugWidget",
                    $"Failed to save metadata for {trackName}: {ex.Message}"
                );
            }
        }

        #endregion

        #region Trigger Point Commands

        [RelayCommand]
        private void SaveBassTriggerPoint()
        {
            SaveTriggerPoint("Bass", BassThreshold);
        }

        [RelayCommand]
        private void SaveMidTriggerPoint()
        {
            SaveTriggerPoint("Mid", MidThreshold);
        }

        [RelayCommand]
        private void SaveHighTriggerPoint()
        {
            SaveTriggerPoint("High", HighThreshold);
        }

        private void SaveTriggerPoint(string frequencyBand, double thresholdValue)
        {
            try
            {
                var trackName = _trackNames[SelectedTrackIndex];

                // Auto-generate name: TrackName + FreqBand + ValueWithoutDecimals
                // e.g., "Bass1Mid63" for Bass1 track, Mid band, value 0.63
                var valueInt = (int)Math.Round(thresholdValue * 100); // Convert 0.63 to 63
                var triggerPointName = $"{trackName}{frequencyBand}{valueInt}";

                // Create AudioTriggerPoint using new model
                var audioTrigger = new AudioTriggerPoint
                {
                    Name = triggerPointName,
                    TrackName = trackName,
                    TrackId = trackName.ToLowerInvariant(),
                    FrequencyBand = frequencyBand,
                    ThresholdValue = thresholdValue
                };

                // Create trigger point directory if it doesn't exist
                var triggerPointsDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "visualizer",
                    "audio_triggers"
                );
                Directory.CreateDirectory(triggerPointsDir);

                // Save as individual JSON file
                var fileName = $"{triggerPointName}.json";
                var filePath = Path.Combine(triggerPointsDir, fileName);

                var json = JsonSerializer.Serialize(audioTrigger, JsonOptions);
                File.WriteAllText(filePath, json);

                DebugLogger.LogImportant("FrequencyDebugWidget",
                    $"✅ Audio trigger saved: {triggerPointName} ({trackName} @ {frequencyBand} = {thresholdValue:F2})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FrequencyDebugWidget", $"Failed to save audio trigger: {ex.Message}");
            }
        }

        #endregion
    }
}
