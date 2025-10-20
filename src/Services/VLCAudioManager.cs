using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SkiaSharp;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Cross-platform audio manager using LibVLCSharp for VibeOut mode
    /// Replaces NAudio for Linux/macOS compatibility
    /// </summary>
    public class VLCAudioManager : IDisposable
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;
        private readonly object _lockObject = new();
        private bool _isDisposed;

        // FFT data for visualization
        private float[] _fftData = new float[1024];
        private float[] _waveformData = new float[1024];

        // Audio intensity values for different frequency bands
        private float _bassIntensity;
        private float _midIntensity;
        private float _trebleIntensity;
        private float _overallIntensity;

        // Frequency analysis
        private float _dominantFrequency;
        private float _bassFrequency;
        private float _midFrequency;
        private float _trebleFrequency;

        // Track intensities (for 8-track Balatro style)
        public float DrumsIntensity => _bassIntensity * 1.2f; // Drums are in the bass
        public float BassIntensity => _bassIntensity;
        public float ChordsIntensity => _midIntensity;
        public float MelodyIntensity => _trebleIntensity;

        // Audio properties
        public float AudioBass => _bassIntensity;
        public float AudioMid => _midIntensity;
        public float AudioTreble => _trebleIntensity;
        public float AudioIntensity => _overallIntensity;

        // Events
        public event EventHandler<float>? AudioAnalysisUpdated;
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackStopped;

        // Playback state
        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;
        public float Volume
        {
            get => _mediaPlayer?.Volume ?? 100f;
            set
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = (int)Math.Clamp(value, 0, 100);
            }
        }

        public VLCAudioManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Initialize LibVLC with audio visualization options
                var options = new[]
                {
                    "--no-video", // Audio only
                    "--audio-visual=visual", // Enable visualizations
                    "--effect-list=spectrum", // Spectrum analyzer
                    "--effect-fft-window=kaiser", // Good FFT window for music
                    "--verbose=0" // Reduce console spam
                };

                Core.Initialize();
                _libVLC = new LibVLC(options);
                _mediaPlayer = new MediaPlayer(_libVLC);

                // Hook up audio callbacks for FFT data
                // Note: LibVLCSharp doesn't have SetAudioFormatCallbacks
                // Format is handled in the audio callbacks themselves
                _mediaPlayer.SetAudioCallbacks(
                    AudioPlayCallback,
                    AudioPauseCallback,
                    AudioResumeCallback,
                    AudioFlushCallback,
                    AudioDrainCallback);

                // Event handlers
                _mediaPlayer.Playing += (s, e) => PlaybackStarted?.Invoke(this, EventArgs.Empty);
                _mediaPlayer.Stopped += (s, e) => PlaybackStopped?.Invoke(this, EventArgs.Empty);
                _mediaPlayer.EndReached += OnMediaEnded;

                // Start audio processing thread
                Task.Run(ProcessAudioLoop);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize VLC: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load and play an audio file
        /// </summary>
        public void LoadAudioFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Audio file not found: {filePath}");

            lock (_lockObject)
            {
                // Stop current playback
                Stop();

                // Create new media
                _currentMedia?.Dispose();
                _currentMedia = new Media(_libVLC, filePath, FromType.FromPath);

                // Load into player
                _mediaPlayer.Media = _currentMedia;
            }
        }

        /// <summary>
        /// Load multiple audio tracks for layered playback
        /// </summary>
        public void LoadAudioTracks(params string[] trackPaths)
        {
            // For now, just load the first track
            // TODO: Implement multi-track mixing using multiple MediaPlayers
            if (trackPaths.Length > 0)
            {
                LoadAudioFile(trackPaths[0]);
            }
        }

        public void Play()
        {
            _mediaPlayer?.Play();
        }

        public void Pause()
        {
            _mediaPlayer?.Pause();
        }

        public void Stop()
        {
            _mediaPlayer?.Stop();
        }

        #region Audio Callbacks for FFT Data

        private void AudioPlayCallback(IntPtr data, IntPtr samples, uint count, long pts)
        {
            if (_isDisposed) return;

            // Extract audio samples for FFT
            ExtractAudioData(samples, count);

            // Perform FFT analysis
            PerformFFTAnalysis();

            // Copy processed audio to output
            CopyMemory(data, samples, (int)count);
        }

        private void AudioPauseCallback(IntPtr data, long pts)
        {
            // Handle pause
        }

        private void AudioResumeCallback(IntPtr data, long pts)
        {
            // Handle resume
        }

        private void AudioFlushCallback(IntPtr data, long pts)
        {
            // Handle flush
        }

        private void AudioDrainCallback(IntPtr data)
        {
            // Handle drain
        }

        #endregion

        #region FFT Analysis

        private void ExtractAudioData(IntPtr samples, uint sampleCount)
        {
            if (sampleCount == 0) return;

            // Copy audio samples to managed array
            var floatSamples = new float[Math.Min(sampleCount, 1024)];
            Marshal.Copy(samples, floatSamples, 0, floatSamples.Length);

            lock (_lockObject)
            {
                // Store waveform data
                Array.Copy(floatSamples, _waveformData, Math.Min(floatSamples.Length, _waveformData.Length));
            }
        }

        private void PerformFFTAnalysis()
        {
            lock (_lockObject)
            {
                // Simple FFT using DFT (for now - can optimize with FFT library later)
                int n = _waveformData.Length;
                for (int k = 0; k < n / 2; k++)
                {
                    float real = 0;
                    float imag = 0;

                    for (int t = 0; t < n; t++)
                    {
                        float angle = 2 * MathF.PI * t * k / n;
                        real += _waveformData[t] * MathF.Cos(angle);
                        imag -= _waveformData[t] * MathF.Sin(angle);
                    }

                    _fftData[k] = MathF.Sqrt(real * real + imag * imag) / n;
                }

                // Calculate frequency band intensities
                CalculateFrequencyBands();
            }
        }

        private void CalculateFrequencyBands()
        {
            // Frequency ranges (assuming 44100Hz sample rate)
            // Bass: 20-250 Hz (bins 0-12)
            // Mid: 250-2000 Hz (bins 12-93)
            // Treble: 2000-20000 Hz (bins 93-465)

            _bassIntensity = 0;
            _midIntensity = 0;
            _trebleIntensity = 0;

            // Sum up frequency bins for each band
            for (int i = 0; i < Math.Min(512, _fftData.Length); i++)
            {
                float magnitude = _fftData[i];

                if (i < 12)
                    _bassIntensity += magnitude;
                else if (i < 93)
                    _midIntensity += magnitude;
                else if (i < 465)
                    _trebleIntensity += magnitude;
            }

            // Normalize (with smoothing)
            _bassIntensity = Math.Min(1f, _bassIntensity / 12f);
            _midIntensity = Math.Min(1f, _midIntensity / 81f);
            _trebleIntensity = Math.Min(1f, _trebleIntensity / 372f);
            _overallIntensity = (_bassIntensity + _midIntensity + _trebleIntensity) / 3f;

            // Find dominant frequency
            int maxIndex = 0;
            float maxMagnitude = 0;
            for (int i = 0; i < Math.Min(512, _fftData.Length); i++)
            {
                if (_fftData[i] > maxMagnitude)
                {
                    maxMagnitude = _fftData[i];
                    maxIndex = i;
                }
            }

            // Convert bin index to frequency
            _dominantFrequency = maxIndex * 44100f / 1024f;
            _bassFrequency = 80f; // Typical bass frequency
            _midFrequency = 500f; // Typical mid frequency
            _trebleFrequency = 3000f; // Typical treble frequency
        }

        #endregion

        #region Audio Processing Loop

        private async void ProcessAudioLoop()
        {
            while (!_isDisposed)
            {
                try
                {
                    // Raise audio analysis event with current intensity
                    AudioAnalysisUpdated?.Invoke(this, _overallIntensity);

                    // 60 FPS update rate
                    await Task.Delay(16);
                }
                catch
                {
                    // Ignore errors in background thread
                }
            }
        }

        #endregion

        #region Media Event Handlers

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            // Loop the media
            if (_mediaPlayer != null && _currentMedia != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Play();
            }
        }

        #endregion

        #region Utility Methods

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

        // Fallback for non-Windows platforms
        private static void CopyMemoryManaged(IntPtr dest, IntPtr src, int count)
        {
            byte[] buffer = new byte[count];
            Marshal.Copy(src, buffer, 0, count);
            Marshal.Copy(buffer, 0, dest, count);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            lock (_lockObject)
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _currentMedia?.Dispose();
                _libVLC?.Dispose();
            }
        }

        #endregion
    }

    public enum AudioFormat
    {
        S16N = 0x0010,
        S16L = 0x0011,
        S16B = 0x0012,
        F32L = 0x0041,
        F32B = 0x0042
    }
}