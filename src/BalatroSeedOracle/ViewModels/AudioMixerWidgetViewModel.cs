using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    /// <summary>
    /// ViewModel for Audio Mixer Widget - controls volume, pan, mute, and solo for all tracks
    /// </summary>
    public partial class AudioMixerWidgetViewModel : BaseWidgetViewModel
    {
        private Control? _ownerControl;
        private SoundFlowAudioManager? _audioManager;

        private static readonly string MixPresetsPath = Path.Combine(
            AppContext.BaseDirectory,
            "visualizer",
            "audio_mixes"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public ObservableCollection<TrackMixViewModel> Tracks { get; } = new();

        [ObservableProperty]
        private string _currentPresetName = "Default";

        [ObservableProperty]
        private ObservableCollection<string> _availablePresets = new();

        public AudioMixerWidgetViewModel()
        {
            WidgetTitle = "Audio Mixer";
            WidgetIcon = "TuneVertical";
            IsMinimized = true;

            // Set fixed position for Audio Mixer widget - third position (90px spacing)
            PositionX = 20;
            PositionY = 260;

            // Initialize 8 tracks
            InitializeTracks();

            // Ensure presets directory exists
            Directory.CreateDirectory(MixPresetsPath);

            // Load available presets
            LoadAvailablePresets();

            // Try to load default preset
            _ = LoadPresetAsync("Default");
        }

        private void InitializeTracks()
        {
            var trackNames = new[]
            {
                ("Bass1", "bass1"),
                ("Bass2", "bass2"),
                ("Drums1", "drums1"),
                ("Drums2", "drums2"),
                ("Chords1", "chords1"),
                ("Chords2", "chords2"),
                ("Melody1", "melody1"),
                ("Melody2", "melody2"),
            };

            foreach (var (name, id) in trackNames)
            {
                var track = new TrackMixViewModel
                {
                    TrackName = name,
                    TrackId = id,
                    Volume = 1.0f,
                    Pan = 0f,
                    Muted = false,
                    Solo = false,
                };

                // Wire up property changes to update audio manager
                track.PropertyChanged += (s, e) =>
                {
                    if (_audioManager != null && s is TrackMixViewModel t)
                    {
                        UpdateAudioManagerTrack(t);
                    }
                };

                Tracks.Add(track);
            }
        }

        public void OnAttached(Control ownerControl)
        {
            _ownerControl = ownerControl;

            // Find BalatroMainMenu to get audio manager
            var mainMenu = ownerControl.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu?.ViewModel?.AudioManager is SoundFlowAudioManager soundFlow)
            {
                _audioManager = soundFlow;
                ApplyAllTracksToAudioManager();
            }
        }

        public void OnDetached()
        {
            _ownerControl = null;
            _audioManager = null;
        }

        private void UpdateAudioManagerTrack(TrackMixViewModel track)
        {
            if (_audioManager == null)
                return;

            // Check if any track is soloed
            var hasSolo = Tracks.Any(t => t.Solo);

            // Calculate effective volume
            float effectiveVolume = track.Volume;

            if (track.Muted)
            {
                effectiveVolume = 0f;
            }
            else if (hasSolo && !track.Solo)
            {
                effectiveVolume = 0f; // Mute non-solo tracks when solo is active
            }

            // Apply to audio manager (you'll need to implement this in SoundFlowAudioManager)
            // _audioManager.SetTrackVolume(track.TrackId, effectiveVolume);
            // _audioManager.SetTrackPan(track.TrackId, track.Pan);

            DebugLogger.Log(
                "AudioMixer",
                $"{track.TrackName}: Vol={effectiveVolume:F2}, Pan={track.Pan:F2}"
            );
        }

        private void ApplyAllTracksToAudioManager()
        {
            foreach (var track in Tracks)
            {
                UpdateAudioManagerTrack(track);
            }
        }

        #region Preset Management

        private void LoadAvailablePresets()
        {
            try
            {
                var presets = new List<string> { "Default" };

                if (Directory.Exists(MixPresetsPath))
                {
                    var files = Directory.GetFiles(MixPresetsPath, "*.json");
                    presets.AddRange(files.Select(f => Path.GetFileNameWithoutExtension(f)));
                }

                AvailablePresets = new ObservableCollection<string>(
                    presets.Distinct().OrderBy(p => p)
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioMixer", $"Failed to load presets: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SavePresetAsync()
        {
            try
            {
                // Prompt for preset name (for now, use a default name)
                var presetName = $"Mix_{DateTime.Now:yyyyMMdd_HHmmss}";

                var preset = new MusicMixPreset
                {
                    Name = presetName,
                    Tracks = new Dictionary<string, TrackMixSettings>(),
                };

                foreach (var track in Tracks)
                {
                    preset.Tracks[track.TrackId] = new TrackMixSettings
                    {
                        TrackId = track.TrackId,
                        TrackName = track.TrackName,
                        Volume = track.Volume,
                        Pan = track.Pan,
                        Muted = track.Muted,
                        Solo = track.Solo,
                    };
                }

                var filePath = Path.Combine(MixPresetsPath, $"{presetName}.json");
                var json = JsonSerializer.Serialize(preset, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                CurrentPresetName = presetName;
                LoadAvailablePresets();

                DebugLogger.LogImportant("AudioMixer", $"✅ Mix preset saved: {presetName}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioMixer", $"Failed to save preset: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadPresetAsync(string? presetName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(presetName))
                    presetName = "Default";

                var filePath = Path.Combine(MixPresetsPath, $"{presetName}.json");

                if (!File.Exists(filePath))
                {
                    DebugLogger.Log(
                        "AudioMixer",
                        $"Preset '{presetName}' not found, using defaults"
                    );
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var preset = JsonSerializer.Deserialize<MusicMixPreset>(json, JsonOptions);

                if (preset?.Tracks != null)
                {
                    foreach (var track in Tracks)
                    {
                        if (preset.Tracks.TryGetValue(track.TrackId, out var settings))
                        {
                            track.Volume = settings.Volume;
                            track.Pan = settings.Pan;
                            track.Muted = settings.Muted;
                            track.Solo = settings.Solo;
                        }
                    }

                    CurrentPresetName = presetName;
                    ApplyAllTracksToAudioManager();

                    DebugLogger.Log("AudioMixer", $"Loaded mix preset: {presetName}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AudioMixer", $"Failed to load preset: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for a single track in the mixer
    /// </summary>
    public partial class TrackMixViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _trackName = string.Empty;

        [ObservableProperty]
        private string _trackId = string.Empty;

        [ObservableProperty]
        private float _volume = 1.0f;

        [ObservableProperty]
        private float _pan = 0f;

        [ObservableProperty]
        private bool _muted = false;

        [ObservableProperty]
        private bool _solo = false;
    }
}
