using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Simple sound effects player for UI sounds (hover, click, etc.)
    /// Uses SoundFlow's MiniAudio backend for cross-platform OGG playback
    /// </summary>
    public class SoundEffectsService : IDisposable
    {
        private AudioEngine? _engine;
        private AudioPlaybackDevice? _device;
        private readonly Dictionary<string, byte[]> _loadedSounds = new();
        private readonly Dictionary<string, SoundPlayer> _activePlayers = new();
        private bool _isDisposed;
        private float _volume = 0.5f; // Default 50% volume for SFX

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                if (_device != null)
                    _device.MasterMixer.Volume = _volume;
            }
        }

        public SoundEffectsService()
        {
            try
            {
                Console.WriteLine("[SoundEffectsService] Initializing SFX audio engine...");

                // Create audio engine
                _engine = new MiniAudioEngine();

                // Define audio format (CD quality)
                var format = AudioFormat.Cd; // 44.1kHz, 16-bit Stereo

                // Initialize playback device
                var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
                _device = _engine.InitializePlaybackDevice(defaultDevice, format);

                // Set initial volume
                _device.MasterMixer.Volume = _volume;

                // Start the device
                _device.Start();

                // Pre-load common sound effects
                PreloadSounds();

                Console.WriteLine($"[SoundEffectsService] ✓ Initialized with {_loadedSounds.Count} preloaded sounds");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SoundEffectsService] ERROR: Failed to initialize: {ex.Message}");
            }
        }

        private void PreloadSounds()
        {
            // Find SFX directory
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Audio", "SFX"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "Assets", "Audio", "SFX"),
            };

            string? sfxDir = possiblePaths.FirstOrDefault(Directory.Exists);
            if (sfxDir == null)
            {
                Console.WriteLine("[SoundEffectsService] WARNING: Could not find Assets/Audio/SFX directory");
                return;
            }

            // Load all .ogg files in the SFX directory
            var oggFiles = Directory.GetFiles(sfxDir, "*.ogg", SearchOption.TopDirectoryOnly);
            foreach (var filePath in oggFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var audioData = File.ReadAllBytes(filePath);
                    _loadedSounds[fileName] = audioData;
                    Console.WriteLine($"[SoundEffectsService] Preloaded: {fileName}.ogg ({audioData.Length} bytes)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundEffectsService] Failed to load {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Play a sound effect by name (without extension)
        /// </summary>
        /// <param name="soundName">Name of the sound file (e.g., "paper1", "highlight1")</param>
        /// <param name="pitch">Pitch multiplier (1.0 = normal, 0.5-2.0 recommended range)</param>
        /// <param name="volumeMultiplier">Volume multiplier (0.0-1.0)</param>
        public void PlaySound(string soundName, float pitch = 1.0f, float volumeMultiplier = 1.0f)
        {
            if (_engine == null || _device == null || _isDisposed)
                return;

            if (!_loadedSounds.TryGetValue(soundName, out var audioData))
            {
                Console.WriteLine($"[SoundEffectsService] Sound not found: {soundName}");
                return;
            }

            try
            {
                // Create a memory stream from the preloaded audio data
                var memoryStream = new MemoryStream(audioData);

                // Create data provider
                var format = AudioFormat.Cd;
                var dataProvider = new StreamDataProvider(_engine, format, memoryStream);

                // Create one-shot player
                var player = new SoundPlayer(_engine, format, dataProvider);
                player.Volume = Math.Clamp(volumeMultiplier, 0f, 1f);
                player.IsLooping = false;

                // Apply pitch shift by changing playback rate
                // Note: MiniAudio's pitch shift might not be available, so we'll try setting it
                // If not supported, sound will play at normal pitch
                try
                {
                    // Pitch is implemented as playback rate in many audio engines
                    // This may or may not work depending on SoundFlow's implementation
                    player.PlaybackRate = Math.Clamp(pitch, 0.5f, 2.0f);
                }
                catch
                {
                    // Pitch shift not supported, play at normal speed
                }

                // Add to mixer
                _device.MasterMixer.AddComponent(player);

                // Start playback
                player.Play();

                // Clean up when finished (simple approach: remove after 2 seconds)
                _ = Task.Delay(2000).ContinueWith(_ =>
                {
                    try
                    {
                        player.Stop();
                        _device.MasterMixer.RemoveComponent(player);
                        player.Dispose();
                        memoryStream.Dispose();
                    }
                    catch { /* Ignore cleanup errors */ }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SoundEffectsService] Error playing {soundName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Play the card hover sound effect (paper1.ogg from Balatro)
        /// Matches Balatro's implementation: random pitch 0.9-1.1, volume 0.35
        /// </summary>
        public void PlayCardHover()
        {
            var random = new Random();
            var pitch = 0.9f + (float)(random.NextDouble() * 0.2); // 0.9 to 1.1
            PlaySound("paper1", pitch, 0.35f);
        }

        /// <summary>
        /// Play button click sound
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySound("button", 1.0f, 0.3f);
        }

        /// <summary>
        /// Play highlight sound for UI elements
        /// </summary>
        public void PlayHighlight(float pitch = 1.0f)
        {
            PlaySound("highlight1", pitch, 0.2f);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Stop all active players
            foreach (var player in _activePlayers.Values)
            {
                try
                {
                    player.Stop();
                    player.Dispose();
                }
                catch { /* Ignore errors during disposal */ }
            }
            _activePlayers.Clear();

            // Clear preloaded sounds
            _loadedSounds.Clear();

            // Stop and dispose device
            _device?.Stop();
            _device?.Dispose();

            // Dispose engine
            _engine?.Dispose();

            Console.WriteLine("[SoundEffectsService] Disposed");
        }
    }
}
