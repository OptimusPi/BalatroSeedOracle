using System;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Simple wrapper service for playing UI sound effects
    /// Delegates to the platform IAudioManager for actual playback
    /// </summary>
    public class SoundEffectsService
    {
        private readonly IAudioManager _audioManager;
        private float _volume = 1.0f;

        public SoundEffectsService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public float Volume
        {
            get => _volume;
            set => _volume = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Play a sound effect by name (without extension)
        /// </summary>
        public void PlaySound(string soundName, float volumeMultiplier = 1.0f)
        {
            try
            {
                var effectiveVolume = _volume * volumeMultiplier;
                _audioManager.PlaySfx(soundName, effectiveVolume);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SoundEffectsService",
                    $"Error playing {soundName}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Play the card hover sound effect
        /// </summary>
        public void PlayCardHover()
        {
            PlaySound("paper1", 1f);
        }

        /// <summary>
        /// Play button click sound
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySound("button", 1f);
        }

        /// <summary>
        /// Play highlight sound for UI elements
        /// </summary>
        public void PlayHighlight()
        {
            PlaySound("highlight1", 1f);
        }
    }
}
