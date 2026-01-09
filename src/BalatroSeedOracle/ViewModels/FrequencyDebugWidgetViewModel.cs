using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private Task? _saveTask;
        private const int SAVE_DEBOUNCE_MS = 250;

        private static readonly string MetadataDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Audio",
            "Metadata"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

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
            WidgetIcon = "ChartBar";
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
            _updateTask = FrequencyUpdateLoopAsync(_updateCancellation.Token);
        }

        private async Task FrequencyUpdateLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
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
                    await Task.Delay(16, cancellationToken);
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
                    await Task.Delay(100, cancellationToken);
                }
            }
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

                        DebugLogger.Log("FrequencyDebugWidget", $"Loaded metadata for {trackName}");
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

            // Track debounced save task - no fire-and-forget!
            _ = DebouncedSaveMetadataAsync(token);
        }

        private async Task DebouncedSaveMetadataAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Wait for debounce period (250ms)
                await Task.Delay(SAVE_DEBOUNCE_MS, cancellationToken);

                // If not cancelled, save the metadata
                if (!cancellationToken.IsCancellationRequested)
                {
                    SaveMetadataForCurrentTrack();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when user changes value again before save completes
            }
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
        private async Task SaveBassTriggerPoint()
        {
            await SaveTriggerPoint("Bass", BassThreshold);
        }

        [RelayCommand]
        private async Task SaveMidTriggerPoint()
        {
            await SaveTriggerPoint("Mid", MidThreshold);
        }

        [RelayCommand]
        private async Task SaveHighTriggerPoint()
        {
            await SaveTriggerPoint("High", HighThreshold);
        }

        private async Task SaveTriggerPoint(string frequencyBand, double thresholdValue)
        {
            try
            {
                var trackName = _trackNames[SelectedTrackIndex];

                // Auto-generate name: TrackName + FreqBand + ValueWithoutDecimals
                // e.g., "Bass1Mid63" for Bass1 track, Mid band, value 0.63
                var valueInt = (int)Math.Round(thresholdValue * 100); // Convert 0.63 to 63
                var defaultName = $"{trackName}{frequencyBand}{valueInt}";

                // Find the main window early to get resources
                var mainWindow = _ownerControl?.FindAncestorOfType<Window>();
                if (mainWindow == null)
                {
                    DebugLogger.LogError(
                        "FrequencyDebugWidget",
                        "Cannot show dialog: main window not found"
                    );
                    return;
                }

                // Create dialog using the SAME pattern as ShowFilterNameInputDialog in BalatroMainMenu
                var dialog = new Window
                {
                    Width = 450,
                    Height = 250,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None,
                    Background = Avalonia.Media.Brushes.Transparent,
                    TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                };

                string? result = null;
                var textBox = new TextBox
                {
                    Text = defaultName,
                    Watermark = "e.g., deepBass1, banana, etc.",
                    Margin = new Avalonia.Thickness(0, 10, 0, 0),
                    FontSize = 18,
                    Padding = new Avalonia.Thickness(12, 8),
                    MinHeight = 45,
                };

                var okButton = new Button
                {
                    Content = "SAVE",
                    Classes = { "btn-blue" },
                    MinWidth = 120,
                    Height = 45,
                };

                var cancelButton = new Button
                {
                    Content = "CANCEL",
                    Classes = { "btn-red" },
                    MinWidth = 120,
                    Height = 45,
                };

                okButton.Click += (s, e) =>
                {
                    result = textBox.Text;
                    dialog.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    result = null;
                    dialog.Close();
                };

                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter)
                    {
                        result = textBox.Text;
                        dialog.Close();
                    }
                };

                // Build dialog UI (same structure as ShowFilterNameInputDialog)
                var mainBorder = new Border
                {
                    Background = mainWindow.FindResource("DarkBorder") as Avalonia.Media.IBrush,
                    BorderBrush = mainWindow.FindResource("LightGrey") as Avalonia.Media.IBrush,
                    BorderThickness = new Avalonia.Thickness(3),
                    CornerRadius = new Avalonia.CornerRadius(16),
                };

                var mainGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

                // Title bar
                var titleBar = new Border
                {
                    [Grid.RowProperty] = 0,
                    Background = mainWindow.FindResource("ModalGrey") as Avalonia.Media.IBrush,
                    CornerRadius = new Avalonia.CornerRadius(14, 14, 0, 0),
                    Padding = new Avalonia.Thickness(20, 12),
                };

                var titleText = new TextBlock
                {
                    Text = "Save Audio Trigger",
                    FontSize = 24,
                    Foreground = mainWindow.FindResource("White") as Avalonia.Media.IBrush,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                };

                titleBar.Child = titleText;
                mainGrid.Children.Add(titleBar);

                // Content area
                var contentBorder = new Border
                {
                    [Grid.RowProperty] = 1,
                    Background = mainWindow.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                    Padding = new Avalonia.Thickness(24),
                };

                var contentStack = new StackPanel { Spacing = 8 };
                contentStack.Children.Add(
                    new TextBlock
                    {
                        Text = "Trigger Name:",
                        FontSize = 16,
                        Foreground = mainWindow.FindResource("White") as Avalonia.Media.IBrush,
                    }
                );
                contentStack.Children.Add(textBox);

                contentBorder.Child = contentStack;
                mainGrid.Children.Add(contentBorder);

                // Button area
                var buttonBorder = new Border
                {
                    [Grid.RowProperty] = 2,
                    Background = mainWindow.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                    CornerRadius = new Avalonia.CornerRadius(0, 0, 14, 14),
                    Padding = new Avalonia.Thickness(20, 12, 20, 20),
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 12,
                };
                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                buttonBorder.Child = buttonPanel;
                mainGrid.Children.Add(buttonBorder);

                mainBorder.Child = mainGrid;
                dialog.Content = mainBorder;

                // Show dialog the CORRECT way (same as BalatroMainMenu - await ShowDialog)
                await dialog.ShowDialog(mainWindow);

                // If user cancelled or entered empty name, exit
                if (string.IsNullOrWhiteSpace(result))
                {
                    DebugLogger.Log("FrequencyDebugWidget", "Audio trigger save cancelled");
                    return;
                }

                // Normalize the filename (a-zA-Z0-9 only, replace everything else with underscore)
                var normalizedName = NormalizeFilename(result);

                // Create AudioTriggerPoint using new model
                var audioTrigger = new AudioTriggerPoint
                {
                    Name = normalizedName,
                    TrackName = trackName,
                    TrackId = trackName.ToLowerInvariant(),
                    FrequencyBand = frequencyBand,
                    ThresholdValue = thresholdValue,
                };

                // Create trigger point directory if it doesn't exist
                var triggerPointsDir = Path.Combine(
                    AppContext.BaseDirectory,
                    "visualizer",
                    "audio_triggers"
                );
                Directory.CreateDirectory(triggerPointsDir);

                // Save as individual JSON file
                var fileName = $"{normalizedName}.json";
                var filePath = Path.Combine(triggerPointsDir, fileName);

                var json = JsonSerializer.Serialize(audioTrigger, JsonOptions);
                File.WriteAllText(filePath, json);

                DebugLogger.LogImportant(
                    "FrequencyDebugWidget",
                    $"✅ Audio trigger saved: {normalizedName} ({trackName} @ {frequencyBand} = {thresholdValue:F2})"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "FrequencyDebugWidget",
                    $"Failed to save audio trigger: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Normalizes a filename by replacing any character that is NOT a-zA-Z0-9 with underscore.
        /// Even spaces become underscores.
        /// </summary>
        private static string NormalizeFilename(string input)
        {
            // Replace any character that is NOT alphanumeric with underscore
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "_");
        }

        #endregion
    }
}
