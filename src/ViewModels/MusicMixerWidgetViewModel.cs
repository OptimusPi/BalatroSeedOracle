using System;
using CommunityToolkit.Mvvm.ComponentModel;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for Music Mixer Widget - 8-track volume and mute controls
    /// Simple, clean interface for controlling individual audio stems
    /// </summary>
    public partial class MusicMixerWidgetViewModel : BaseWidgetViewModel
    {
        private readonly UserProfileService _userProfileService;

        public MusicMixerWidgetViewModel(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

            // Configure widget appearance and position - fourth position (90px spacing)
            WidgetTitle = "Music Mixer";
            WidgetIcon = "🎵";
            IsMinimized = true; // Start minimized
            
            PositionX = 20;
            PositionY = 290;

            // Set custom size for mixer
            Width = 400;
            Height = 500;
        }

        #region Drums1 Track

        [ObservableProperty]
        private double _drums1Volume = 100;

        [ObservableProperty]
        private bool _drums1Muted = false;

        partial void OnDrums1VolumeChanged(double value)
        {
            if (!Drums1Muted)
                ApplyTrackVolume("Drums1", (float)(value / 100.0));
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
        private bool _drums2Muted = false;

        partial void OnDrums2VolumeChanged(double value)
        {
            if (!Drums2Muted)
                ApplyTrackVolume("Drums2", (float)(value / 100.0));
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
        private bool _bass1Muted = false;

        partial void OnBass1VolumeChanged(double value)
        {
            if (!Bass1Muted)
                ApplyTrackVolume("Bass1", (float)(value / 100.0));
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
        private bool _bass2Muted = false;

        partial void OnBass2VolumeChanged(double value)
        {
            if (!Bass2Muted)
                ApplyTrackVolume("Bass2", (float)(value / 100.0));
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
        private bool _chords1Muted = false;

        partial void OnChords1VolumeChanged(double value)
        {
            if (!Chords1Muted)
                ApplyTrackVolume("Chords1", (float)(value / 100.0));
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
        private bool _chords2Muted = false;

        partial void OnChords2VolumeChanged(double value)
        {
            if (!Chords2Muted)
                ApplyTrackVolume("Chords2", (float)(value / 100.0));
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
        private bool _melody1Muted = false;

        partial void OnMelody1VolumeChanged(double value)
        {
            if (!Melody1Muted)
                ApplyTrackVolume("Melody1", (float)(value / 100.0));
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
        private bool _melody2Muted = false;

        partial void OnMelody2VolumeChanged(double value)
        {
            if (!Melody2Muted)
                ApplyTrackVolume("Melody2", (float)(value / 100.0));
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
                DebugLogger.LogError("MusicMixerWidgetViewModel", "SoundFlowAudioManager service not found!");
                return;
            }

            audioManager.SetTrackVolume(trackName, volume);
            DebugLogger.Log("MusicMixerWidgetViewModel", $"{trackName} → {volume:P0}");
        }

        #endregion
    }
}
