using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Music Mixer Widget - 8-track volume, pan, and mute controls
    /// Simple, clean interface for controlling individual audio stems
    /// </summary>
    public partial class MusicMixerWidgetViewModel : BaseWidgetViewModel
    {
        private readonly UserProfileService _userProfileService;
        private static readonly string MIXER_SETTINGS_DIR = AppPaths.MixerSettingsDir;

        private static readonly string MIXER_SETTINGS_FILE = System.IO.Path.Combine(
            MIXER_SETTINGS_DIR,
            "mixer_settings.json"
        );

        // Store previous mute states before applying solo
        private Dictionary<string, bool> _previousMuteStates = new();

        public MusicMixerWidgetViewModel(UserProfileService userProfileService)
        {
            _userProfileService =
                userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

            // Configure widget appearance and position - fourth position (90px spacing)
            WidgetTitle = "Music Mixer";
            WidgetIcon = "🎵";
            IsMinimized = true; // Start minimized

            PositionX = 20;
            PositionY = 350;

            // Set custom size for mixer
            Width = 400;
            Height = 500;

            // Load saved mixer settings if they exist
            LoadMixerSettings();
        }

        #region Drums1 Track

        [ObservableProperty]
        private double _drums1Volume = 100;

        [ObservableProperty]
        private double _drums1Pan = 50;

        [ObservableProperty]
        private bool _drums1Muted = false;

        [ObservableProperty]
        private bool _drums1Solo = false;

        partial void OnDrums1VolumeChanged(double value)
        {
            if (!Drums1Muted)
                ApplyTrackVolume("Drums1", (float)(value / 100.0));
        }

        partial void OnDrums1PanChanged(double value)
        {
            ApplyTrackPan("Drums1", (float)(value / 100.0));
        }

        partial void OnDrums1MutedChanged(bool value)
        {
            ApplyTrackVolume("Drums1", value ? 0f : (float)(Drums1Volume / 100.0));
        }

        partial void OnDrums1SoloChanged(bool value)
        {
            HandleSoloToggle("Drums1", value);
        }

        #endregion

        #region Drums2 Track

        [ObservableProperty]
        private double _drums2Volume = 100;

        [ObservableProperty]
        private double _drums2Pan = 50;

        [ObservableProperty]
        private bool _drums2Muted = false;

        [ObservableProperty]
        private bool _drums2Solo = false;

        partial void OnDrums2VolumeChanged(double value)
        {
            if (!Drums2Muted)
                ApplyTrackVolume("Drums2", (float)(value / 100.0));
        }

        partial void OnDrums2PanChanged(double value)
        {
            ApplyTrackPan("Drums2", (float)(value / 100.0));
        }

        partial void OnDrums2MutedChanged(bool value)
        {
            ApplyTrackVolume("Drums2", value ? 0f : (float)(Drums2Volume / 100.0));
        }

        partial void OnDrums2SoloChanged(bool value)
        {
            HandleSoloToggle("Drums2", value);
        }

        #endregion

        #region Bass1 Track

        [ObservableProperty]
        private double _bass1Volume = 100;

        [ObservableProperty]
        private double _bass1Pan = 50;

        [ObservableProperty]
        private bool _bass1Muted = false;

        [ObservableProperty]
        private bool _bass1Solo = false;

        partial void OnBass1VolumeChanged(double value)
        {
            if (!Bass1Muted)
                ApplyTrackVolume("Bass1", (float)(value / 100.0));
        }

        partial void OnBass1PanChanged(double value)
        {
            ApplyTrackPan("Bass1", (float)(value / 100.0));
        }

        partial void OnBass1MutedChanged(bool value)
        {
            ApplyTrackVolume("Bass1", value ? 0f : (float)(Bass1Volume / 100.0));
        }

        partial void OnBass1SoloChanged(bool value)
        {
            HandleSoloToggle("Bass1", value);
        }

        #endregion

        #region Bass2 Track

        [ObservableProperty]
        private double _bass2Volume = 100;

        [ObservableProperty]
        private double _bass2Pan = 50;

        [ObservableProperty]
        private bool _bass2Muted = false;

        [ObservableProperty]
        private bool _bass2Solo = false;

        partial void OnBass2VolumeChanged(double value)
        {
            if (!Bass2Muted)
                ApplyTrackVolume("Bass2", (float)(value / 100.0));
        }

        partial void OnBass2PanChanged(double value)
        {
            ApplyTrackPan("Bass2", (float)(value / 100.0));
        }

        partial void OnBass2MutedChanged(bool value)
        {
            ApplyTrackVolume("Bass2", value ? 0f : (float)(Bass2Volume / 100.0));
        }

        partial void OnBass2SoloChanged(bool value)
        {
            HandleSoloToggle("Bass2", value);
        }

        #endregion

        #region Chords1 Track

        [ObservableProperty]
        private double _chords1Volume = 100;

        [ObservableProperty]
        private double _chords1Pan = 50;

        [ObservableProperty]
        private bool _chords1Muted = false;

        [ObservableProperty]
        private bool _chords1Solo = false;

        partial void OnChords1VolumeChanged(double value)
        {
            if (!Chords1Muted)
                ApplyTrackVolume("Chords1", (float)(value / 100.0));
        }

        partial void OnChords1PanChanged(double value)
        {
            ApplyTrackPan("Chords1", (float)(value / 100.0));
        }

        partial void OnChords1MutedChanged(bool value)
        {
            ApplyTrackVolume("Chords1", value ? 0f : (float)(Chords1Volume / 100.0));
        }

        partial void OnChords1SoloChanged(bool value)
        {
            HandleSoloToggle("Chords1", value);
        }

        #endregion

        #region Chords2 Track

        [ObservableProperty]
        private double _chords2Volume = 100;

        [ObservableProperty]
        private double _chords2Pan = 50;

        [ObservableProperty]
        private bool _chords2Muted = false;

        [ObservableProperty]
        private bool _chords2Solo = false;

        partial void OnChords2VolumeChanged(double value)
        {
            if (!Chords2Muted)
                ApplyTrackVolume("Chords2", (float)(value / 100.0));
        }

        partial void OnChords2PanChanged(double value)
        {
            ApplyTrackPan("Chords2", (float)(value / 100.0));
        }

        partial void OnChords2MutedChanged(bool value)
        {
            ApplyTrackVolume("Chords2", value ? 0f : (float)(Chords2Volume / 100.0));
        }

        partial void OnChords2SoloChanged(bool value)
        {
            HandleSoloToggle("Chords2", value);
        }

        #endregion

        #region Melody1 Track

        [ObservableProperty]
        private double _melody1Volume = 100;

        [ObservableProperty]
        private double _melody1Pan = 50;

        [ObservableProperty]
        private bool _melody1Muted = false;

        [ObservableProperty]
        private bool _melody1Solo = false;

        partial void OnMelody1VolumeChanged(double value)
        {
            if (!Melody1Muted)
                ApplyTrackVolume("Melody1", (float)(value / 100.0));
        }

        partial void OnMelody1PanChanged(double value)
        {
            ApplyTrackPan("Melody1", (float)(value / 100.0));
        }

        partial void OnMelody1MutedChanged(bool value)
        {
            ApplyTrackVolume("Melody1", value ? 0f : (float)(Melody1Volume / 100.0));
        }

        partial void OnMelody1SoloChanged(bool value)
        {
            HandleSoloToggle("Melody1", value);
        }

        #endregion

        #region Melody2 Track

        [ObservableProperty]
        private double _melody2Volume = 100;

        [ObservableProperty]
        private double _melody2Pan = 50;

        [ObservableProperty]
        private bool _melody2Muted = false;

        [ObservableProperty]
        private bool _melody2Solo = false;

        partial void OnMelody2VolumeChanged(double value)
        {
            if (!Melody2Muted)
                ApplyTrackVolume("Melody2", (float)(value / 100.0));
        }

        partial void OnMelody2PanChanged(double value)
        {
            ApplyTrackPan("Melody2", (float)(value / 100.0));
        }

        partial void OnMelody2MutedChanged(bool value)
        {
            ApplyTrackVolume("Melody2", value ? 0f : (float)(Melody2Volume / 100.0));
        }

        partial void OnMelody2SoloChanged(bool value)
        {
            HandleSoloToggle("Melody2", value);
        }

        #endregion

        #region Audio Control

        /// <summary>
        /// Apply volume changes to the audio manager
        /// </summary>
        private void ApplyTrackVolume(string trackName, float volume)
        {
            var audioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
            if (audioManager == null)
            {
                DebugLogger.LogError(
                    "MusicMixerWidgetViewModel",
                    "SoundFlowAudioManager service not found!"
                );
                return;
            }

            audioManager.SetTrackVolume(trackName, volume);
            DebugLogger.Log("MusicMixerWidgetViewModel", $"{trackName} volume → {volume:P0}");
        }

        /// <summary>
        /// Apply pan changes to the audio manager
        /// </summary>
        private void ApplyTrackPan(string trackName, float pan)
        {
            var audioManager = ServiceHelper.GetService<SoundFlowAudioManager>();
            if (audioManager == null)
            {
                DebugLogger.LogError(
                    "MusicMixerWidgetViewModel",
                    "SoundFlowAudioManager service not found!"
                );
                return;
            }

            audioManager.SetTrackPan(trackName, pan);
            DebugLogger.Log("MusicMixerWidgetViewModel", $"{trackName} pan → {pan:F2}");
        }

        /// <summary>
        /// Handle Solo toggle - mutes all other tracks when a track is soloed
        /// Multiple tracks can be soloed at once
        /// </summary>
        private void HandleSoloToggle(string trackName, bool isSolo)
        {
            var trackList = new[]
            {
                "Drums1",
                "Drums2",
                "Bass1",
                "Bass2",
                "Chords1",
                "Chords2",
                "Melody1",
                "Melody2",
            };

            if (isSolo)
            {
                // Save current mute states if this is the first solo
                var anySoloActive =
                    Drums1Solo
                    || Drums2Solo
                    || Bass1Solo
                    || Bass2Solo
                    || Chords1Solo
                    || Chords2Solo
                    || Melody1Solo
                    || Melody2Solo;

                if (!anySoloActive)
                {
                    _previousMuteStates.Clear();
                    _previousMuteStates["Drums1"] = Drums1Muted;
                    _previousMuteStates["Drums2"] = Drums2Muted;
                    _previousMuteStates["Bass1"] = Bass1Muted;
                    _previousMuteStates["Bass2"] = Bass2Muted;
                    _previousMuteStates["Chords1"] = Chords1Muted;
                    _previousMuteStates["Chords2"] = Chords2Muted;
                    _previousMuteStates["Melody1"] = Melody1Muted;
                    _previousMuteStates["Melody2"] = Melody2Muted;
                }

                // Mute all tracks except soloed ones
                foreach (var track in trackList)
                {
                    bool trackIsSoloed = GetTrackSoloState(track);
                    bool shouldMute = !trackIsSoloed;
                    SetTrackMutedState(track, shouldMute);
                }
            }
            else
            {
                // Check if any tracks are still soloed
                var anySoloActive =
                    Drums1Solo
                    || Drums2Solo
                    || Bass1Solo
                    || Bass2Solo
                    || Chords1Solo
                    || Chords2Solo
                    || Melody1Solo
                    || Melody2Solo;

                if (!anySoloActive)
                {
                    // Restore previous mute states
                    foreach (var track in trackList)
                    {
                        if (_previousMuteStates.TryGetValue(track, out var previousState))
                        {
                            SetTrackMutedState(track, previousState);
                        }
                    }
                    _previousMuteStates.Clear();
                }
                else
                {
                    // Some tracks are still soloed, update mute states
                    foreach (var track in trackList)
                    {
                        bool trackIsSoloed = GetTrackSoloState(track);
                        bool shouldMute = !trackIsSoloed;
                        SetTrackMutedState(track, shouldMute);
                    }
                }
            }

            DebugLogger.Log("MusicMixerWidgetViewModel", $"{trackName} solo toggled to {isSolo}");
        }

        /// <summary>
        /// Get the solo state of a track
        /// </summary>
        private bool GetTrackSoloState(string trackName)
        {
            return trackName switch
            {
                "Drums1" => Drums1Solo,
                "Drums2" => Drums2Solo,
                "Bass1" => Bass1Solo,
                "Bass2" => Bass2Solo,
                "Chords1" => Chords1Solo,
                "Chords2" => Chords2Solo,
                "Melody1" => Melody1Solo,
                "Melody2" => Melody2Solo,
                _ => false,
            };
        }

        /// <summary>
        /// Set the muted state of a track (without triggering solo logic)
        /// </summary>
        private void SetTrackMutedState(string trackName, bool muted)
        {
            switch (trackName)
            {
                case "Drums1":
                    Drums1Muted = muted;
                    break;
                case "Drums2":
                    Drums2Muted = muted;
                    break;
                case "Bass1":
                    Bass1Muted = muted;
                    break;
                case "Bass2":
                    Bass2Muted = muted;
                    break;
                case "Chords1":
                    Chords1Muted = muted;
                    break;
                case "Chords2":
                    Chords2Muted = muted;
                    break;
                case "Melody1":
                    Melody1Muted = muted;
                    break;
                case "Melody2":
                    Melody2Muted = muted;
                    break;
            }
        }

        #endregion

        #region Save/Load/Reset Commands

        /// <summary>
        /// Save current mixer settings to JSON file
        /// </summary>
        [RelayCommand]
        private void SaveMixerSettings()
        {
            try
            {
                var settings = new MixerSettings
                {
                    Drums1 = new TrackSettings
                    {
                        Volume = Drums1Volume,
                        Pan = Drums1Pan,
                        Muted = Drums1Muted,
                        Solo = Drums1Solo,
                    },
                    Drums2 = new TrackSettings
                    {
                        Volume = Drums2Volume,
                        Pan = Drums2Pan,
                        Muted = Drums2Muted,
                        Solo = Drums2Solo,
                    },
                    Bass1 = new TrackSettings
                    {
                        Volume = Bass1Volume,
                        Pan = Bass1Pan,
                        Muted = Bass1Muted,
                        Solo = Bass1Solo,
                    },
                    Bass2 = new TrackSettings
                    {
                        Volume = Bass2Volume,
                        Pan = Bass2Pan,
                        Muted = Bass2Muted,
                        Solo = Bass2Solo,
                    },
                    Chords1 = new TrackSettings
                    {
                        Volume = Chords1Volume,
                        Pan = Chords1Pan,
                        Muted = Chords1Muted,
                        Solo = Chords1Solo,
                    },
                    Chords2 = new TrackSettings
                    {
                        Volume = Chords2Volume,
                        Pan = Chords2Pan,
                        Muted = Chords2Muted,
                        Solo = Chords2Solo,
                    },
                    Melody1 = new TrackSettings
                    {
                        Volume = Melody1Volume,
                        Pan = Melody1Pan,
                        Muted = Melody1Muted,
                        Solo = Melody1Solo,
                    },
                    Melody2 = new TrackSettings
                    {
                        Volume = Melody2Volume,
                        Pan = Melody2Pan,
                        Muted = Melody2Muted,
                        Solo = Melody2Solo,
                    },
                };

                if (!Directory.Exists(MIXER_SETTINGS_DIR))
                {
                    Directory.CreateDirectory(MIXER_SETTINGS_DIR);
                }

                var json = JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(MIXER_SETTINGS_FILE, json);

                DebugLogger.Log(
                    "MusicMixerWidgetViewModel",
                    $"Mixer settings saved to {MIXER_SETTINGS_FILE}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "MusicMixerWidgetViewModel",
                    $"Failed to save mixer settings: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Load mixer settings from JSON file
        /// </summary>
        [RelayCommand]
        private void LoadMixerSettings()
        {
            try
            {
                if (!File.Exists(MIXER_SETTINGS_FILE))
                {
                    DebugLogger.Log("MusicMixerWidgetViewModel", "No saved mixer settings found");
                    return;
                }

                var json = File.ReadAllText(MIXER_SETTINGS_FILE);
                var settings = JsonSerializer.Deserialize<MixerSettings>(json);

                if (settings != null)
                {
                    // Load Drums1
                    Drums1Volume = settings.Drums1.Volume;
                    Drums1Pan = settings.Drums1.Pan;
                    Drums1Muted = settings.Drums1.Muted;
                    Drums1Solo = settings.Drums1.Solo;

                    // Load Drums2
                    Drums2Volume = settings.Drums2.Volume;
                    Drums2Pan = settings.Drums2.Pan;
                    Drums2Muted = settings.Drums2.Muted;
                    Drums2Solo = settings.Drums2.Solo;

                    // Load Bass1
                    Bass1Volume = settings.Bass1.Volume;
                    Bass1Pan = settings.Bass1.Pan;
                    Bass1Muted = settings.Bass1.Muted;
                    Bass1Solo = settings.Bass1.Solo;

                    // Load Bass2
                    Bass2Volume = settings.Bass2.Volume;
                    Bass2Pan = settings.Bass2.Pan;
                    Bass2Muted = settings.Bass2.Muted;
                    Bass2Solo = settings.Bass2.Solo;

                    // Load Chords1
                    Chords1Volume = settings.Chords1.Volume;
                    Chords1Pan = settings.Chords1.Pan;
                    Chords1Muted = settings.Chords1.Muted;
                    Chords1Solo = settings.Chords1.Solo;

                    // Load Chords2
                    Chords2Volume = settings.Chords2.Volume;
                    Chords2Pan = settings.Chords2.Pan;
                    Chords2Muted = settings.Chords2.Muted;
                    Chords2Solo = settings.Chords2.Solo;

                    // Load Melody1
                    Melody1Volume = settings.Melody1.Volume;
                    Melody1Pan = settings.Melody1.Pan;
                    Melody1Muted = settings.Melody1.Muted;
                    Melody1Solo = settings.Melody1.Solo;

                    // Load Melody2
                    Melody2Volume = settings.Melody2.Volume;
                    Melody2Pan = settings.Melody2.Pan;
                    Melody2Muted = settings.Melody2.Muted;
                    Melody2Solo = settings.Melody2.Solo;

                    DebugLogger.Log(
                        "MusicMixerWidgetViewModel",
                        "Mixer settings loaded successfully"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "MusicMixerWidgetViewModel",
                    $"Failed to load mixer settings: {ex.Message}"
                );
            }
        }

        [RelayCommand]
        private void SaveMixAs()
        {
            try
            {
                var name = ShowNameDialog("Save Mix", "Enter a name for this mix:", "MyMix");
                if (string.IsNullOrWhiteSpace(name))
                    return;

                var settings = BuildCurrentMixerSettings();
                if (MixerHelper.SaveMixer(name, settings))
                {
                    DebugLogger.Log("MusicMixerWidgetViewModel", $"Saved mix '{name}'");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MusicMixerWidgetViewModel", $"SaveMixAs failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void LoadMixFrom()
        {
            try
            {
                var names = MixerHelper.LoadAllMixerNames();
                if (names.Count == 0)
                {
                    DebugLogger.Log("MusicMixerWidgetViewModel", "No saved mixes found");
                    return;
                }

                var selected = ShowSelectionDialog("Load Mix", "Choose a saved mix:", names);
                if (string.IsNullOrWhiteSpace(selected))
                    return;

                var settings = MixerHelper.LoadMixer(selected);
                if (settings != null)
                {
                    ApplyMixerSettings(settings);
                    DebugLogger.Log("MusicMixerWidgetViewModel", $"Loaded mix '{selected}'");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MusicMixerWidgetViewModel", $"LoadMixFrom failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AnimateToMix()
        {
            try
            {
                var names = MixerHelper.LoadAllMixerNames();
                if (names.Count == 0) return;
                var selected = ShowSelectionDialog("Animate To Mix", "Choose a saved mix:", names);
                if (string.IsNullOrWhiteSpace(selected)) return;
                var target = MixerHelper.LoadMixer(selected);
                if (target == null) return;
                _ = AnimateToMixer(target, TimeSpan.FromSeconds(2.0));
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MusicMixerWidgetViewModel", $"AnimateToMix failed: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task AnimateToMixer(MixerSettings target, TimeSpan duration)
        {
            var start = DateTime.UtcNow;
            var sD1V = Drums1Volume; var sD1P = Drums1Pan; var sD1M = Drums1Muted;
            var sD2V = Drums2Volume; var sD2P = Drums2Pan; var sD2M = Drums2Muted;
            var sB1V = Bass1Volume; var sB1P = Bass1Pan; var sB1M = Bass1Muted;
            var sB2V = Bass2Volume; var sB2P = Bass2Pan; var sB2M = Bass2Muted;
            var sC1V = Chords1Volume; var sC1P = Chords1Pan; var sC1M = Chords1Muted;
            var sC2V = Chords2Volume; var sC2P = Chords2Pan; var sC2M = Chords2Muted;
            var sM1V = Melody1Volume; var sM1P = Melody1Pan; var sM1M = Melody1Muted;
            var sM2V = Melody2Volume; var sM2P = Melody2Pan; var sM2M = Melody2Muted;
            while (true)
            {
                var elapsed = DateTime.UtcNow - start;
                if (elapsed >= duration)
                {
                    Drums1Volume = target.Drums1.Volume; Drums1Pan = target.Drums1.Pan; Drums1Muted = target.Drums1.Muted;
                    Drums2Volume = target.Drums2.Volume; Drums2Pan = target.Drums2.Pan; Drums2Muted = target.Drums2.Muted;
                    Bass1Volume = target.Bass1.Volume; Bass1Pan = target.Bass1.Pan; Bass1Muted = target.Bass1.Muted;
                    Bass2Volume = target.Bass2.Volume; Bass2Pan = target.Bass2.Pan; Bass2Muted = target.Bass2.Muted;
                    Chords1Volume = target.Chords1.Volume; Chords1Pan = target.Chords1.Pan; Chords1Muted = target.Chords1.Muted;
                    Chords2Volume = target.Chords2.Volume; Chords2Pan = target.Chords2.Pan; Chords2Muted = target.Chords2.Muted;
                    Melody1Volume = target.Melody1.Volume; Melody1Pan = target.Melody1.Pan; Melody1Muted = target.Melody1.Muted;
                    Melody2Volume = target.Melody2.Volume; Melody2Pan = target.Melody2.Pan; Melody2Muted = target.Melody2.Muted;
                    break;
                }
                float t = (float)(elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                float p = 1f - (1f - t) * (1f - t);
                Drums1Volume = sD1V + (target.Drums1.Volume - sD1V) * p; Drums1Pan = sD1P + (target.Drums1.Pan - sD1P) * p; Drums1Muted = p >= 1f ? target.Drums1.Muted : sD1M;
                Drums2Volume = sD2V + (target.Drums2.Volume - sD2V) * p; Drums2Pan = sD2P + (target.Drums2.Pan - sD2P) * p; Drums2Muted = p >= 1f ? target.Drums2.Muted : sD2M;
                Bass1Volume = sB1V + (target.Bass1.Volume - sB1V) * p; Bass1Pan = sB1P + (target.Bass1.Pan - sB1P) * p; Bass1Muted = p >= 1f ? target.Bass1.Muted : sB1M;
                Bass2Volume = sB2V + (target.Bass2.Volume - sB2V) * p; Bass2Pan = sB2P + (target.Bass2.Pan - sB2P) * p; Bass2Muted = p >= 1f ? target.Bass2.Muted : sB2M;
                Chords1Volume = sC1V + (target.Chords1.Volume - sC1V) * p; Chords1Pan = sC1P + (target.Chords1.Pan - sC1P) * p; Chords1Muted = p >= 1f ? target.Chords1.Muted : sC1M;
                Chords2Volume = sC2V + (target.Chords2.Volume - sC2V) * p; Chords2Pan = sC2P + (target.Chords2.Pan - sC2P) * p; Chords2Muted = p >= 1f ? target.Chords2.Muted : sC2M;
                Melody1Volume = sM1V + (target.Melody1.Volume - sM1V) * p; Melody1Pan = sM1P + (target.Melody1.Pan - sM1P) * p; Melody1Muted = p >= 1f ? target.Melody1.Muted : sM1M;
                Melody2Volume = sM2V + (target.Melody2.Volume - sM2V) * p; Melody2Pan = sM2P + (target.Melody2.Pan - sM2P) * p; Melody2Muted = p >= 1f ? target.Melody2.Muted : sM2M;
                await System.Threading.Tasks.Task.Delay(16);
            }
        }

        private MixerSettings BuildCurrentMixerSettings()
        {
            return new MixerSettings
            {
                Drums1 = new TrackSettings { Volume = Drums1Volume, Pan = Drums1Pan, Muted = Drums1Muted, Solo = Drums1Solo },
                Drums2 = new TrackSettings { Volume = Drums2Volume, Pan = Drums2Pan, Muted = Drums2Muted, Solo = Drums2Solo },
                Bass1 = new TrackSettings { Volume = Bass1Volume, Pan = Bass1Pan, Muted = Bass1Muted, Solo = Bass1Solo },
                Bass2 = new TrackSettings { Volume = Bass2Volume, Pan = Bass2Pan, Muted = Bass2Muted, Solo = Bass2Solo },
                Chords1 = new TrackSettings { Volume = Chords1Volume, Pan = Chords1Pan, Muted = Chords1Muted, Solo = Chords1Solo },
                Chords2 = new TrackSettings { Volume = Chords2Volume, Pan = Chords2Pan, Muted = Chords2Muted, Solo = Chords2Solo },
                Melody1 = new TrackSettings { Volume = Melody1Volume, Pan = Melody1Pan, Muted = Melody1Muted, Solo = Melody1Solo },
                Melody2 = new TrackSettings { Volume = Melody2Volume, Pan = Melody2Pan, Muted = Melody2Muted, Solo = Melody2Solo },
            };
        }

        private void ApplyMixerSettings(MixerSettings settings)
        {
            Drums1Volume = settings.Drums1.Volume;
            Drums1Pan = settings.Drums1.Pan;
            Drums1Muted = settings.Drums1.Muted;
            Drums1Solo = settings.Drums1.Solo;

            Drums2Volume = settings.Drums2.Volume;
            Drums2Pan = settings.Drums2.Pan;
            Drums2Muted = settings.Drums2.Muted;
            Drums2Solo = settings.Drums2.Solo;

            Bass1Volume = settings.Bass1.Volume;
            Bass1Pan = settings.Bass1.Pan;
            Bass1Muted = settings.Bass1.Muted;
            Bass1Solo = settings.Bass1.Solo;

            Bass2Volume = settings.Bass2.Volume;
            Bass2Pan = settings.Bass2.Pan;
            Bass2Muted = settings.Bass2.Muted;
            Bass2Solo = settings.Bass2.Solo;

            Chords1Volume = settings.Chords1.Volume;
            Chords1Pan = settings.Chords1.Pan;
            Chords1Muted = settings.Chords1.Muted;
            Chords1Solo = settings.Chords1.Solo;

            Chords2Volume = settings.Chords2.Volume;
            Chords2Pan = settings.Chords2.Pan;
            Chords2Muted = settings.Chords2.Muted;
            Chords2Solo = settings.Chords2.Solo;

            Melody1Volume = settings.Melody1.Volume;
            Melody1Pan = settings.Melody1.Pan;
            Melody1Muted = settings.Melody1.Muted;
            Melody1Solo = settings.Melody1.Solo;

            Melody2Volume = settings.Melody2.Volume;
            Melody2Pan = settings.Melody2.Pan;
            Melody2Muted = settings.Melody2.Muted;
            Melody2Solo = settings.Melody2.Solo;
        }

        private string? ShowNameDialog(string title, string labelText, string watermark)
        {
            try
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 360,
                    Height = 180,
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
                var textBox = new TextBox { Watermark = watermark, FontSize = 14, Padding = new Avalonia.Thickness(10) };
                var buttons = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                };

                var cancelBtn = new Button { Content = "Cancel", Width = 90, Padding = new Avalonia.Thickness(10, 5) };
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
                saveBtn.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(textBox.Text)) { result = textBox.Text; dialog.Close(); } };
                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter && !string.IsNullOrWhiteSpace(textBox.Text)) { result = textBox.Text; dialog.Close(); }
                    else if (e.Key == Avalonia.Input.Key.Escape) { dialog.Close(); }
                };

                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(saveBtn);
                panel.Children.Add(label);
                panel.Children.Add(textBox);
                panel.Children.Add(buttons);
                dialog.Content = panel;

                var owner = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                if (owner != null) { dialog.ShowDialog(owner).GetAwaiter().GetResult(); }
                return result;
            }
            catch { return null; }
        }

        private string? ShowSelectionDialog(string title, string labelText, System.Collections.Generic.List<string> options)
        {
            try
            {
                var dialog = new Window
                {
                    Title = title,
                    Width = 420,
                    Height = 220,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.Parse("#0D0D0D")),
                };

                var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
                var label = new TextBlock { Text = labelText, Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontSize = 14 };
                var combo = new ComboBox { ItemsSource = options, SelectedIndex = options.Count > 0 ? 0 : -1 };
                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10 };
                var cancelBtn = new Button { Content = "Cancel", Width = 90, Padding = new Avalonia.Thickness(10, 5) };
                var loadBtn = new Button { Content = "Load", Width = 90, Padding = new Avalonia.Thickness(10, 5), Background = new SolidColorBrush(Color.Parse("#FFD700")), Foreground = new SolidColorBrush(Color.Parse("#0D0D0D")) };
                string? result = null;
                cancelBtn.Click += (s, e) => dialog.Close();
                loadBtn.Click += (s, e) => { result = combo.SelectedItem as string; dialog.Close(); };
                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(loadBtn);
                panel.Children.Add(label);
                panel.Children.Add(combo);
                panel.Children.Add(buttons);
                dialog.Content = panel;
                var owner = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                if (owner != null) { dialog.ShowDialog(owner).GetAwaiter().GetResult(); }
                return result;
            }
            catch { return null; }
        }

        /// <summary>
        /// Reset all mixer settings to defaults (100% volume, 50 pan (center), unmuted, not soloed)
        /// </summary>
        [RelayCommand]
        private void ResetMixer()
        {
            // Reset Drums1
            Drums1Volume = 100;
            Drums1Pan = 50;
            Drums1Muted = false;
            Drums1Solo = false;

            // Reset Drums2
            Drums2Volume = 100;
            Drums2Pan = 50;
            Drums2Muted = false;
            Drums2Solo = false;

            // Reset Bass1
            Bass1Volume = 100;
            Bass1Pan = 50;
            Bass1Muted = false;
            Bass1Solo = false;

            // Reset Bass2
            Bass2Volume = 100;
            Bass2Pan = 50;
            Bass2Muted = false;
            Bass2Solo = false;

            // Reset Chords1
            Chords1Volume = 100;
            Chords1Pan = 50;
            Chords1Muted = false;
            Chords1Solo = false;

            // Reset Chords2
            Chords2Volume = 100;
            Chords2Pan = 50;
            Chords2Muted = false;
            Chords2Solo = false;

            // Reset Melody1
            Melody1Volume = 100;
            Melody1Pan = 50;
            Melody1Muted = false;
            Melody1Solo = false;

            // Reset Melody2
            Melody2Volume = 100;
            Melody2Pan = 50;
            Melody2Muted = false;
            Melody2Solo = false;

            DebugLogger.Log("MusicMixerWidgetViewModel", "Mixer reset to defaults");
        }

        #endregion
    }
}
