using System;
using System.IO;
using System.Text.Json;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Music Mixer Widget - 8-track volume, pan, and mute controls
    /// Simple, clean interface for controlling individual audio stems
    /// </summary>
    public partial class MusicMixerWidgetViewModel : BaseWidgetViewModel
    {
        private readonly UserProfileService _userProfileService;
        private const string MIXER_SETTINGS_FILE = "mixer_settings.json";

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
        private double _drums1Pan = 0;

        [ObservableProperty]
        private bool _drums1Muted = false;

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

        #endregion

        #region Drums2 Track

        [ObservableProperty]
        private double _drums2Volume = 100;

        [ObservableProperty]
        private double _drums2Pan = 0;

        [ObservableProperty]
        private bool _drums2Muted = false;

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

        #endregion

        #region Bass1 Track

        [ObservableProperty]
        private double _bass1Volume = 100;

        [ObservableProperty]
        private double _bass1Pan = 0;

        [ObservableProperty]
        private bool _bass1Muted = false;

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

        #endregion

        #region Bass2 Track

        [ObservableProperty]
        private double _bass2Volume = 100;

        [ObservableProperty]
        private double _bass2Pan = 0;

        [ObservableProperty]
        private bool _bass2Muted = false;

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

        #endregion

        #region Chords1 Track

        [ObservableProperty]
        private double _chords1Volume = 100;

        [ObservableProperty]
        private double _chords1Pan = 0;

        [ObservableProperty]
        private bool _chords1Muted = false;

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

        #endregion

        #region Chords2 Track

        [ObservableProperty]
        private double _chords2Volume = 100;

        [ObservableProperty]
        private double _chords2Pan = 0;

        [ObservableProperty]
        private bool _chords2Muted = false;

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

        #endregion

        #region Melody1 Track

        [ObservableProperty]
        private double _melody1Volume = 100;

        [ObservableProperty]
        private double _melody1Pan = 0;

        [ObservableProperty]
        private bool _melody1Muted = false;

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

        #endregion

        #region Melody2 Track

        [ObservableProperty]
        private double _melody2Volume = 100;

        [ObservableProperty]
        private double _melody2Pan = 0;

        [ObservableProperty]
        private bool _melody2Muted = false;

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
                    Drums1 = new TrackSettings { Volume = Drums1Volume, Pan = Drums1Pan, Muted = Drums1Muted },
                    Drums2 = new TrackSettings { Volume = Drums2Volume, Pan = Drums2Pan, Muted = Drums2Muted },
                    Bass1 = new TrackSettings { Volume = Bass1Volume, Pan = Bass1Pan, Muted = Bass1Muted },
                    Bass2 = new TrackSettings { Volume = Bass2Volume, Pan = Bass2Pan, Muted = Bass2Muted },
                    Chords1 = new TrackSettings { Volume = Chords1Volume, Pan = Chords1Pan, Muted = Chords1Muted },
                    Chords2 = new TrackSettings { Volume = Chords2Volume, Pan = Chords2Pan, Muted = Chords2Muted },
                    Melody1 = new TrackSettings { Volume = Melody1Volume, Pan = Melody1Pan, Muted = Melody1Muted },
                    Melody2 = new TrackSettings { Volume = Melody2Volume, Pan = Melody2Pan, Muted = Melody2Muted }
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MIXER_SETTINGS_FILE, json);

                DebugLogger.Log("MusicMixerWidgetViewModel", $"Mixer settings saved to {MIXER_SETTINGS_FILE}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MusicMixerWidgetViewModel", $"Failed to save mixer settings: {ex.Message}");
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

                    // Load Drums2
                    Drums2Volume = settings.Drums2.Volume;
                    Drums2Pan = settings.Drums2.Pan;
                    Drums2Muted = settings.Drums2.Muted;

                    // Load Bass1
                    Bass1Volume = settings.Bass1.Volume;
                    Bass1Pan = settings.Bass1.Pan;
                    Bass1Muted = settings.Bass1.Muted;

                    // Load Bass2
                    Bass2Volume = settings.Bass2.Volume;
                    Bass2Pan = settings.Bass2.Pan;
                    Bass2Muted = settings.Bass2.Muted;

                    // Load Chords1
                    Chords1Volume = settings.Chords1.Volume;
                    Chords1Pan = settings.Chords1.Pan;
                    Chords1Muted = settings.Chords1.Muted;

                    // Load Chords2
                    Chords2Volume = settings.Chords2.Volume;
                    Chords2Pan = settings.Chords2.Pan;
                    Chords2Muted = settings.Chords2.Muted;

                    // Load Melody1
                    Melody1Volume = settings.Melody1.Volume;
                    Melody1Pan = settings.Melody1.Pan;
                    Melody1Muted = settings.Melody1.Muted;

                    // Load Melody2
                    Melody2Volume = settings.Melody2.Volume;
                    Melody2Pan = settings.Melody2.Pan;
                    Melody2Muted = settings.Melody2.Muted;

                    DebugLogger.Log("MusicMixerWidgetViewModel", "Mixer settings loaded successfully");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MusicMixerWidgetViewModel", $"Failed to load mixer settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset all mixer settings to defaults (100% volume, 0 pan, unmuted)
        /// </summary>
        [RelayCommand]
        private void ResetMixer()
        {
            // Reset Drums1
            Drums1Volume = 100;
            Drums1Pan = 0;
            Drums1Muted = false;

            // Reset Drums2
            Drums2Volume = 100;
            Drums2Pan = 0;
            Drums2Muted = false;

            // Reset Bass1
            Bass1Volume = 100;
            Bass1Pan = 0;
            Bass1Muted = false;

            // Reset Bass2
            Bass2Volume = 100;
            Bass2Pan = 0;
            Bass2Muted = false;

            // Reset Chords1
            Chords1Volume = 100;
            Chords1Pan = 0;
            Chords1Muted = false;

            // Reset Chords2
            Chords2Volume = 100;
            Chords2Pan = 0;
            Chords2Muted = false;

            // Reset Melody1
            Melody1Volume = 100;
            Melody1Pan = 0;
            Melody1Muted = false;

            // Reset Melody2
            Melody2Volume = 100;
            Melody2Pan = 0;
            Melody2Muted = false;

            DebugLogger.Log("MusicMixerWidgetViewModel", "Mixer reset to defaults");
        }

        #endregion
    }
}
