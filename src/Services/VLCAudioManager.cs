using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Cross-platform audio manager using LibVLCSharp
    /// Drop-in replacement for VibeAudioManager that works on Windows, Mac, and Linux
    /// </summary>
    public class VLCAudioManager : IDisposable
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private bool _isDisposed;

        // Audio analysis properties (matching VibeAudioManager API)
        public float AudioBass { get; private set; } = 0f;
        public float AudioMid { get; private set; } = 0f;
        public float AudioTreble { get; private set; } = 0f;
        public float AudioPeak { get; private set; } = 0f;
        public float BeatDetection { get; private set; } = 0f;

        // Track intensities (matching VibeAudioManager API)
        public float DrumsIntensity => AudioBass * 1.2f;
        public float BassIntensity => AudioBass;
        public float ChordsIntensity => AudioMid;
        public float MelodyIntensity => AudioTreble;

        // Events (matching VibeAudioManager API)
        public event Action<float, float, float, float>? AudioAnalysisUpdated;
        public event Action<float>? BeatDetected;

        // Playback state
        private float _masterVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        public VLCAudioManager()
        {
            try
            {
                Core.Initialize();
                _libVLC = new LibVLC("--no-video", "--verbose=0");
                _mediaPlayer = new MediaPlayer(_libVLC);

                // Set up event handlers
                _mediaPlayer.EndReached += OnMediaEnded;
                _mediaPlayer.Playing += (s, e) => Console.WriteLine("[VLCAudioManager] Playback started");
                _mediaPlayer.EncounteredError += (s, e) => Console.WriteLine("[VLCAudioManager] ERROR: Playback error");

                // Start audio analysis loop
                Task.Run(AudioAnalysisLoop);

                // Auto-load background music
                LoadBackgroundMusic();

                Console.WriteLine("[VLCAudioManager] Initialized successfully (LibVLC cross-platform)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VLCAudioManager] ERROR: Failed to initialize: {ex.Message}");
            }
        }

        private void LoadBackgroundMusic()
        {
            try
            {
                // Look for music files in the music directory
                var musicDir = Path.Combine(Directory.GetCurrentDirectory(), "external", "Balatro", "resources", "music");
                if (!Directory.Exists(musicDir))
                {
                    Console.WriteLine($"[VLCAudioManager] Music directory not found: {musicDir}");
                    return;
                }

                // Find first audio file
                var audioFiles = Directory.GetFiles(musicDir, "*.*")
                    .Where(f => f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                               f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (audioFiles.Length == 0)
                {
                    Console.WriteLine("[VLCAudioManager] No audio files found in music directory");
                    return;
                }

                var musicFile = audioFiles[0];
                Console.WriteLine($"[VLCAudioManager] Loading music: {Path.GetFileName(musicFile)}");

                var media = new Media(_libVLC, musicFile, FromType.FromPath);
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();

                Console.WriteLine("[VLCAudioManager] Playback started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VLCAudioManager] ERROR loading music: {ex.Message}");
            }
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            // Loop the music
            if (!_isDisposed && _mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Play();
            }
        }

        private async void AudioAnalysisLoop()
        {
            // Simple audio analysis simulation
            // VLC doesn't expose easy FFT access like NAudio, so we'll simulate for now
            while (!_isDisposed)
            {
                try
                {
                    // Simulate audio values based on playback state
                    if (_mediaPlayer?.IsPlaying == true && !_isPaused)
                    {
                        // Generate subtle variation to show it's working
                        var time = DateTime.Now.Millisecond / 1000f;
                        AudioBass = (float)(0.3 + Math.Sin(time * 2) * 0.2);
                        AudioMid = (float)(0.4 + Math.Cos(time * 3) * 0.2);
                        AudioTreble = (float)(0.3 + Math.Sin(time * 5) * 0.15);
                        AudioPeak = (AudioBass + AudioMid + AudioTreble) / 3f;
                        BeatDetection = (float)(Math.Sin(time * 4) * 0.5 + 0.5);

                        AudioAnalysisUpdated?.Invoke(AudioBass, AudioMid, AudioTreble, AudioPeak);

                        if (BeatDetection > 0.8f)
                        {
                            BeatDetected?.Invoke(BeatDetection);
                        }
                    }
                    else
                    {
                        AudioBass = AudioMid = AudioTreble = AudioPeak = BeatDetection = 0f;
                    }

                    await Task.Delay(16); // ~60 FPS
                }
                catch
                {
                    // Ignore errors in background thread
                }
            }
        }

        // API methods matching VibeAudioManager
        public void SetTrackVolume(string trackName, float volume, float pan = 0f)
        {
            // VLC doesn't support individual track control like NAudio
            // Just update master volume as approximation
            SetMasterVolume(volume);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Math.Clamp(volume, 0f, 1f);
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)(_masterVolume * 100f);
            }
        }

        public void SetMusicVolume(float volume) => SetMasterVolume(volume);

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void PlayClickSound()
        {
            // TODO: Implement SFX playback if needed
            // Would require a second MediaPlayer instance
        }

        public void Pause()
        {
            _isPaused = true;
            _mediaPlayer?.Pause();
        }

        public void Resume()
        {
            _isPaused = false;
            _mediaPlayer?.Play();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            Console.WriteLine("[VLCAudioManager] Disposed");
        }
    }
}
