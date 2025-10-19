using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;
using Complex = NAudio.Dsp.Complex;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Features.VibeOut;
using NAudio.Vorbis;

namespace BalatroSeedOracle.Services
{

    public class VibeAudioManager : IDisposable
    {
        private WaveOutEvent? _waveOut;
        private MixingSampleProvider? _mixer;

        // Individual audio tracks with gain control
        private readonly Dictionary<string, AudioTrack> _tracks = new();

        // Volume fade timer
        private System.Timers.Timer? _volumeFadeTimer;

        // SFX support
        private float _sfxVolume = 1.0f;
        
        // FFT Analysis for drums (beat detection)
        private readonly float[] _fftBuffer = new float[2048];
        private readonly Complex[] _fftComplex = new Complex[2048];
        private readonly float[] _frequencyBands = new float[32];
        private int _fftPos = 0;

        // FFT Analysis for melodic content (color intensity)
        private readonly float[] _melodicFftBuffer = new float[2048];
        private readonly Complex[] _melodicFftComplex = new Complex[2048];
        private int _melodicFftPos = 0;

        // Audio analysis results (for shader integration)
        public float AudioBass { get; private set; } = 0f;      // 0-300 Hz (drums)
        public float AudioMid { get; private set; } = 0f;       // 300-3000 Hz (melodic)
        public float AudioTreble { get; private set; } = 0f;    // 3000+ Hz (melodic)
        public float AudioPeak { get; private set; } = 0f;      // Overall volume (melodic)
        public float BeatDetection { get; private set; } = 0f;  // Beat pulse for visual effects

        // Individual track intensities - use FFT analysis weighted by track volume
        public float DrumsIntensity => GetTrackIntensity("Drums1", "Drums2", AudioBass); // Drums = bass frequencies
        public float BassIntensity => GetTrackIntensity("Bass1", "Bass2", AudioBass); // Bass = bass frequencies
        public float ChordsIntensity => GetTrackIntensity("Chords1", "Chords2", AudioMid); // Chords = mid frequencies
        public float MelodyIntensity => GetTrackIntensity("Melody1", "Melody2", AudioTreble); // Melody = treble frequencies

        // Beat detection state
        private float _lastBassLevel = 0f;
        private int _beatCooldown = 0;
        private const int BEAT_COOLDOWN_FRAMES = 20; // ~400ms between beats (prevent doubles)

        // Adaptive peak tracking for normalization (0.0 = quiet, 1.0 = max peak)
        private float _bassPeak = 10f;    // Track recent peak for bass (start at reasonable value)
        private float _midPeak = 10f;     // Track recent peak for mid
        private float _treblePeak = 10f;  // Track recent peak for treble
        private const float PEAK_DECAY = 0.9995f; // Very slow decay to adapt to quieter/louder sections
        private const float PEAK_ATTACK = 0.1f; // Fast attack when new peak detected
        private const float PEAK_FLOOR = 1.0f; // Minimum peak value to prevent division by zero

        // Events for UI integration
        public event Action<float, float, float, float>? AudioAnalysisUpdated; // bass, mid, treble, peak
        public event Action<float>? BeatDetected; // beat intensity

        private float GetTrackIntensity(string track1, string track2, float fftIntensity)
        {
            // Get combined volume of tracks (0-2 range since both can be at 1.0)
            float volume = 0f;
            if (_tracks.TryGetValue(track1, out var t1) && t1.CurrentVolume > 0.01f)
                volume += t1.CurrentVolume;
            if (_tracks.TryGetValue(track2, out var t2) && t2.CurrentVolume > 0.01f)
                volume += t2.CurrentVolume;

            // If no tracks playing, return 0
            if (volume < 0.01f) return 0f;

            // Return FFT intensity weighted by track volume (normalize to 0-1)
            return Math.Clamp(fftIntensity * (volume / 2f), 0f, 1f);
        }
        

        public VibeAudioManager()
        {
            try
            {
                InitializeAudio();
                LoadAllTracks();

                // Start volume fade timer for smooth transitions (60fps)
                _volumeFadeTimer = new System.Timers.Timer(1000.0 / 60.0); // ~16ms = 60fps
                _volumeFadeTimer.Elapsed += (s, e) => UpdateAllVolumes();
                _volumeFadeTimer.AutoReset = true;
                _volumeFadeTimer.Start();

                // Start all tracks playing at default volumes (user can adjust with sliders)
                InitializeAllTracks();
            }
            catch
            {
                // Silently fail - audio not critical to app function
            }
        }

        private void InitializeAudio()
        {
            try
            {
                _waveOut = new WaveOutEvent
                {
                    DesiredLatency = 100  // 100ms buffer (prevents crackle/pops)
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeAudioManager", $"Audio init failed: {ex.Message}");
            }
        }
        
        private void LoadAllTracks()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var audioPath = Path.Combine(baseDir, "Assets", "Audio");

            if (!Directory.Exists(audioPath))
            {
                DebugLogger.LogError("VibeAudioManager", $"Audio directory not found: {audioPath}");
                return;
            }

            var trackNames = new[] { "Drums1", "Drums2", "Bass1", "Bass2", "Chords1", "Chords2", "Melody1", "Melody2" };

            // STEP 1: Load all track files (DON'T add to mixer yet!)
            foreach (var trackName in trackNames)
            {
                var extensions = new[] { ".ogg", ".wav", ".mp3" };
                bool loaded = false;

                foreach (var ext in extensions)
                {
                    var filePath = Path.Combine(audioPath, $"{trackName}{ext}");
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var track = new AudioTrack(filePath, trackName);
                            _tracks[trackName] = track;

                            // Initialize mixer with first track's sample rate
                            if (_mixer == null && _waveOut != null)
                            {
                                var waveFormat = track.SampleProvider.WaveFormat;
                                DebugLogger.Log("VibeAudioManager", $"Auto-detected audio format: {waveFormat.SampleRate}Hz, {waveFormat.Channels} channels");
                                _mixer = new MixingSampleProvider(waveFormat);
                                _mixer.ReadFully = true;
                                _waveOut.Init(_mixer);
                                _waveOut.Volume = 1.0f;
                                // DON'T PLAY YET!
                            }

                            loaded = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("VibeAudioManager", $"Failed to load {trackName}{ext}: {ex.Message}");
                        }
                    }
                }

                if (!loaded)
                {
                    DebugLogger.Log("VibeAudioManager", $"⚠️ No compatible audio file found for {trackName}.");
                }
            }

            // STEP 2: Add ALL tracks to mixer BEFORE starting playback
            foreach (var trackName in trackNames)
            {
                if (_tracks.TryGetValue(trackName, out var track))
                {
                    ISampleProvider provider;

                    if (trackName == "Drums1" || trackName == "Drums2")
                    {
                        provider = new AudioAnalyzerProvider(track.SampleProvider, this, true);
                    }
                    else if (trackName.StartsWith("Bass") || trackName.StartsWith("Chords") || trackName.StartsWith("Melody"))
                    {
                        provider = new AudioAnalyzerProvider(track.SampleProvider, this, false);
                    }
                    else
                    {
                        provider = track.SampleProvider;
                    }

                    _mixer!.AddMixerInput(provider);
                    track.IsInMixer = true;
                    track.SetImmediateVolume(0.0f);
                }
            }

            // STEP 3: NOW start playback (all tracks at sample 0!)
            if (_waveOut != null && _tracks.Count > 0)
            {
                _waveOut.Play();
            }
        }
        
        /// <summary>
        /// Initialize all 8 tracks to play simultaneously at user-defined volumes
        /// No more state machine bullshit - user controls everything!
        /// </summary>
        private void InitializeAllTracks()
        {
            // Load user volume settings from profile
            var profile = App.GetService<UserProfileService>()?.GetProfile();
            var settings = profile?.VibeOutSettings;

            if (settings == null)
            {
                // Default volumes if no settings
                SetTrackVolume("Drums1", 1.0f, 0.0f);
                SetTrackVolume("Drums2", 1.0f, 0.0f);
                SetTrackVolume("Bass1", 1.0f, -0.3f);
                SetTrackVolume("Bass2", 1.0f, 0.1f);
                SetTrackVolume("Chords1", 1.0f, -0.15f);
                SetTrackVolume("Chords2", 1.0f, 0.05f);
                SetTrackVolume("Melody1", 1.0f, -0.15f);
                SetTrackVolume("Melody2", 1.0f, 0.05f);
            }
            else
            {
                // Use user's saved volumes
                SetTrackVolume("Drums1", settings.Drums1Volume, 0.0f);
                SetTrackVolume("Drums2", settings.Drums2Volume, 0.0f);
                SetTrackVolume("Bass1", settings.Bass1Volume, -0.3f);
                SetTrackVolume("Bass2", settings.Bass2Volume, 0.1f);
                SetTrackVolume("Chords1", settings.Chords1Volume, -0.15f);
                SetTrackVolume("Chords2", settings.Chords2Volume, 0.05f);
                SetTrackVolume("Melody1", settings.Melody1Volume, -0.15f);
                SetTrackVolume("Melody2", settings.Melody2Volume, 0.05f);
            }
        }

        /// <summary>
        /// Set track volume directly (bypasses state machine)
        /// </summary>
        public void SetTrackVolume(string trackName, float volume, float pan = 0.0f)
        {
            if (_tracks.TryGetValue(trackName, out var track))
            {
                track.SetTarget(Math.Clamp(volume, 0f, 1f), pan);
            }
        }

        private void SetTrack(string trackName, float targetVolume, float targetPan)
        {
            if (_tracks.TryGetValue(trackName, out var track))
            {
                track.SetTarget(targetVolume, targetPan);
            }
            else
            {
            }
        }

        private void UpdateAllVolumes()
        {
            foreach (var track in _tracks.Values)
            {
                track.UpdateVolume(0.15f);
            }
        }
        
        // Called by AudioAnalyzerProvider during audio processing
        internal void ProcessDrumSample(float left, float right)
        {
            // Add samples to drums FFT buffer (for beat detection)
            _fftBuffer[_fftPos] = (left + right) * 0.5f; // Mono mix
            _fftPos = (_fftPos + 1) % _fftBuffer.Length;

            // Perform FFT analysis every 1024 samples
            if (_fftPos % 1024 == 0)
            {
                PerformDrumFFTAnalysis();
            }
        }

        internal void ProcessMelodicSample(float left, float right)
        {
            // Add samples to melodic FFT buffer (for color intensity)
            _melodicFftBuffer[_melodicFftPos] = (left + right) * 0.5f; // Mono mix
            _melodicFftPos = (_melodicFftPos + 1) % _melodicFftBuffer.Length;

            // Perform FFT analysis every 1024 samples
            if (_melodicFftPos % 1024 == 0)
            {
                PerformMelodicFFTAnalysis();
            }
        }
        
        private void PerformDrumFFTAnalysis()
        {
            // Copy buffer to complex array
            for (int i = 0; i < _fftBuffer.Length; i++)
            {
                _fftComplex[i] = new Complex { X = _fftBuffer[i], Y = 0 };
            }
            
            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log2(_fftComplex.Length), _fftComplex);
            
            // Extract frequency bands
            var sampleRate = 44100f;
            var frequencyStep = sampleRate / _fftComplex.Length;
            
            // Calculate bass (0-300 Hz), mid (300-3000 Hz), treble (3000+ Hz)
            float bassSum = 0f, midSum = 0f, trebleSum = 0f;
            int bassCount = 0, midCount = 0, trebleCount = 0;
            
            for (int i = 1; i < _fftComplex.Length / 2; i++) // Skip DC component
            {
                var frequency = i * frequencyStep;
                var magnitude = (float)Math.Sqrt(_fftComplex[i].X * _fftComplex[i].X + _fftComplex[i].Y * _fftComplex[i].Y);
                
                if (frequency < 300)
                {
                    bassSum += magnitude;
                    bassCount++;
                }
                else if (frequency < 3000)
                {
                    midSum += magnitude;
                    midCount++;
                }
                else if (frequency < 12000) // Cap at 12kHz for treble
                {
                    trebleSum += magnitude;
                    trebleCount++;
                }
            }
            
            // Update audio analysis (with smoothing)
            var bassLevel = bassCount > 0 ? bassSum / bassCount : 0f;
            var midLevel = midCount > 0 ? midSum / midCount : 0f;
            var trebleLevel = trebleCount > 0 ? trebleSum / trebleCount : 0f;
            var peakLevel = Math.Max(Math.Max(bassLevel, midLevel), trebleLevel);

            // Adaptive peak tracking with decay
            _bassPeak = Math.Max(PEAK_FLOOR, _bassPeak * PEAK_DECAY); // Slow decay with floor
            _midPeak = Math.Max(PEAK_FLOOR, _midPeak * PEAK_DECAY);
            _treblePeak = Math.Max(PEAK_FLOOR, _treblePeak * PEAK_DECAY);

            // Fast attack when new peak detected
            if (bassLevel > _bassPeak) _bassPeak = _bassPeak * (1f - PEAK_ATTACK) + bassLevel * PEAK_ATTACK;
            if (midLevel > _midPeak) _midPeak = _midPeak * (1f - PEAK_ATTACK) + midLevel * PEAK_ATTACK;
            if (trebleLevel > _treblePeak) _treblePeak = _treblePeak * (1f - PEAK_ATTACK) + trebleLevel * PEAK_ATTACK;

            // Normalize to 0.0-1.0 range using adaptive peaks (safe division with floor)
            float normalizedBass = Math.Clamp(bassLevel / _bassPeak, 0f, 1f);
            float normalizedMid = Math.Clamp(midLevel / _midPeak, 0f, 1f);
            float normalizedTreble = Math.Clamp(trebleLevel / _treblePeak, 0f, 1f);
            float normalizedPeak = Math.Max(Math.Max(normalizedBass, normalizedMid), normalizedTreble);

            // Smooth the values (exponential moving average)
            const float smoothing = 0.8f;
            AudioBass = AudioBass * smoothing + normalizedBass * (1f - smoothing);
            AudioMid = AudioMid * smoothing + normalizedMid * (1f - smoothing);
            AudioTreble = AudioTreble * smoothing + normalizedTreble * (1f - smoothing);
            AudioPeak = AudioPeak * smoothing + normalizedPeak * (1f - smoothing);
            
            // Beat detection: Look for sudden bass increase
            _beatCooldown = Math.Max(0, _beatCooldown - 1);

            if (_beatCooldown == 0)
            {
                // More sensitive beat detection - catch all kicks!
                var bassIncrease = bassLevel - _lastBassLevel;
                if (bassIncrease > 0.15f && bassLevel > 0.3f) // Much more sensitive!
                {
                    BeatDetection = Math.Clamp(bassLevel * 0.5f, 0f, 1f);

                    // Fire event on UI thread to prevent cross-thread exceptions
                    var beatValue = BeatDetection;
                    Dispatcher.UIThread.Post(() =>
                    {
                        BeatDetected?.Invoke(beatValue);
                    }, DispatcherPriority.Normal);

                    _beatCooldown = BEAT_COOLDOWN_FRAMES;
                }
            }

            _lastBassLevel = bassLevel;
            BeatDetection *= 0.95f; // Decay beat intensity
        }

        private void PerformMelodicFFTAnalysis()
        {
            // Copy buffer to complex array
            for (int i = 0; i < _melodicFftBuffer.Length; i++)
            {
                _melodicFftComplex[i] = new Complex { X = _melodicFftBuffer[i], Y = 0 };
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log2(_melodicFftComplex.Length), _melodicFftComplex);

            // Extract frequency bands
            var sampleRate = 44100f;
            var frequencyStep = sampleRate / _melodicFftComplex.Length;

            // Calculate mid and treble for color intensity
            float midSum = 0f, trebleSum = 0f;
            int midCount = 0, trebleCount = 0;

            for (int i = 1; i < _melodicFftComplex.Length / 2; i++)
            {
                var frequency = i * frequencyStep;
                var magnitude = (float)Math.Sqrt(_melodicFftComplex[i].X * _melodicFftComplex[i].X + _melodicFftComplex[i].Y * _melodicFftComplex[i].Y);

                if (frequency >= 300 && frequency < 3000)
                {
                    midSum += magnitude;
                    midCount++;
                }
                else if (frequency >= 3000 && frequency < 12000)
                {
                    trebleSum += magnitude;
                    trebleCount++;
                }
            }

            var midLevel = midCount > 0 ? midSum / midCount : 0f;
            var trebleLevel = trebleCount > 0 ? trebleSum / trebleCount : 0f;
            var peakLevel = Math.Max(midLevel, trebleLevel);

            // Adaptive peak tracking with decay (same as drum FFT)
            _midPeak = Math.Max(PEAK_FLOOR, _midPeak * PEAK_DECAY); // Slow decay with floor
            _treblePeak = Math.Max(PEAK_FLOOR, _treblePeak * PEAK_DECAY);

            // Fast attack when new peak detected
            if (midLevel > _midPeak) _midPeak = _midPeak * (1f - PEAK_ATTACK) + midLevel * PEAK_ATTACK;
            if (trebleLevel > _treblePeak) _treblePeak = _treblePeak * (1f - PEAK_ATTACK) + trebleLevel * PEAK_ATTACK;

            // Normalize to 0.0-1.0 range using adaptive peaks (safe division with floor)
            float normalizedMid = Math.Clamp(midLevel / _midPeak, 0f, 1f);
            float normalizedTreble = Math.Clamp(trebleLevel / _treblePeak, 0f, 1f);
            float normalizedPeak = Math.Max(normalizedMid, normalizedTreble);

            // Smooth the values
            const float smoothing = 0.8f;
            AudioMid = AudioMid * smoothing + normalizedMid * (1f - smoothing);
            AudioTreble = AudioTreble * smoothing + normalizedTreble * (1f - smoothing);
            AudioPeak = AudioPeak * smoothing + normalizedPeak * (1f - smoothing);

            // Fire events for UI/shader integration on UI thread
            var bass = AudioBass;
            var mid = AudioMid;
            var treble = AudioTreble;
            var peak = AudioPeak;
            Dispatcher.UIThread.Post(() =>
            {
                AudioAnalysisUpdated?.Invoke(bass, mid, treble, peak);
            }, DispatcherPriority.Normal);
        }
        
        public void SetMasterVolume(float volume)
        {
            if (_waveOut != null)
            {
                _waveOut.Volume = Math.Clamp(volume, 0f, 1f);
            }
        }

        public void SetMusicVolume(float volume) => SetMasterVolume(volume);
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Math.Clamp(volume, 0f, 1f);
        }

        /// <summary>
        /// Plays a one-shot sound effect (button click, etc.)
        /// </summary>
        public void PlayClickSound()
        {
            try
            {
                // Play on a background thread to avoid blocking UI
                Task.Run(() =>
                {
                    try
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var sfxPath = Path.Combine(baseDir, "Assets", "Audio", "SFX", "button.ogg");

                        if (!File.Exists(sfxPath))
                        {
                            DebugLogger.LogError("VibeAudioManager", $"SFX file not found: {sfxPath}");
                            return;
                        }

                        // Create a new WaveOut for this SFX (allows overlapping sounds)
                        using var reader = new VorbisWaveReader(sfxPath);
                        using var waveOut = new WaveOutEvent();
                        waveOut.Init(reader);
                        waveOut.Volume = _sfxVolume;
                        waveOut.Play();

                        // Wait for the sound to finish playing
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("VibeAudioManager", $"Failed to play click sound: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeAudioManager", $"Failed to start click sound playback: {ex.Message}");
            }
        }
        
        public void Pause()
        {
            _waveOut?.Pause();
        }
        
        public void Resume()
        {
            _waveOut?.Play();
        }
        
        public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;
        
        public void Dispose()
        {
            _volumeFadeTimer?.Stop();
            _volumeFadeTimer?.Dispose();

            _waveOut?.Stop();
            _waveOut?.Dispose();

            foreach (var track in _tracks.Values)
            {
                track.Dispose();
            }
            _tracks.Clear();
        }
    }
    
    // Helper class for individual audio tracks with smooth volume + pan control
    internal class AudioTrack : IDisposable
    {
        public string Name { get; }
        public ISampleProvider SampleProvider { get; }
        public float CurrentVolume { get; private set; }
        public float TargetVolume { get; private set; }
        public float CurrentPan { get; private set; } // -1 = full left, 0 = center, +1 = full right
        public float TargetPan { get; private set; }
        public bool IsInMixer { get; set; }

        private readonly WaveStream _reader;
        private readonly LoopStream _loopStream;
        private readonly VolumeSampleProvider _volumeProvider;
        private readonly LowpassFilterSampleProvider _lowpassFilter;
        private readonly StereoBalanceSampleProvider _panProvider;
        
        public AudioTrack(string filePath, string name)
        {
            Name = name;
            
            // Check file extension to determine how to load
            var extension = Path.GetExtension(filePath).ToLower();
            
            if (extension == ".ogg")
            {
                // Use NAudio.Vorbis for OGG files
                _reader = new VorbisWaveReader(filePath);
            }
            else
            {
                // Use NAudio for WAV/MP3
                _reader = new AudioFileReader(filePath);
            }
            
            _loopStream = new LoopStream(_reader);

            // Auto-detect format from file (usually 48kHz or 44.1kHz stereo) - chain volume → lowpass → stereo balance
            var baseSample = _loopStream.ToSampleProvider();
            _volumeProvider = new VolumeSampleProvider(baseSample) { Volume = 0f };
            _lowpassFilter = new LowpassFilterSampleProvider(_volumeProvider); // Warm filter
            _panProvider = new StereoBalanceSampleProvider(_lowpassFilter); // Custom stereo balance
            SampleProvider = _panProvider;
        }
        
        public void SetTarget(float volume, float pan)
        {
            TargetVolume = Math.Clamp(volume, 0f, 1f);
            TargetPan = Math.Clamp(pan, -1f, 1f);
        }

        public void SetTargetVolume(float volume)
        {
            TargetVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void SetTargetPan(float pan)
        {
            TargetPan = Math.Clamp(pan, -1f, 1f);
        }

        public void SyncToPosition(long position)
        {
            if (_loopStream != null)
            {
                _loopStream.Position = position;
            }
        }

        public long GetPosition()
        {
            return _loopStream?.Position ?? 0;
        }

        public void SetLowpassCutoff(float cutoffHz)
        {
            _lowpassFilter.CutoffFrequency = cutoffHz;
        }

        public void SetImmediateVolume(float volume)
        {
            TargetVolume = Math.Clamp(volume, 0f, 1f);
            CurrentVolume = TargetVolume;
            _volumeProvider.Volume = CurrentVolume;
        }

        public void UpdateVolume(float speed)
        {
            // Smooth volume + pan transitions (DoTween style crossfade)
            // Speed is now configurable from VibeAudioManager!

            // Update volume
            if (Math.Abs(CurrentVolume - TargetVolume) > 0.001f)
            {
                CurrentVolume += (TargetVolume - CurrentVolume) * speed;
                _volumeProvider.Volume = CurrentVolume;
            }
            else
            {
                CurrentVolume = TargetVolume;
                _volumeProvider.Volume = CurrentVolume;
            }

            // Update pan (stereo position)
            if (Math.Abs(CurrentPan - TargetPan) > 0.001f)
            {
                CurrentPan += (TargetPan - CurrentPan) * speed;
                _panProvider.Pan = CurrentPan;
            }
            else
            {
                CurrentPan = TargetPan;
                _panProvider.Pan = CurrentPan;
            }
        }
        
        public void Dispose()
        {
            _reader?.Dispose();
            _loopStream?.Dispose();
        }
    }
    
    // Custom sample provider that performs audio analysis
    internal class AudioAnalyzerProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly VibeAudioManager _manager;
        private readonly bool _isDrums; // true = drums (beat detection), false = melodic (color intensity)

        public WaveFormat WaveFormat => _source.WaveFormat;

        public AudioAnalyzerProvider(ISampleProvider source, VibeAudioManager manager, bool isDrums)
        {
            _source = source;
            _manager = manager;
            _isDrums = isDrums;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = _source.Read(buffer, offset, count);

            // Analyze audio samples (stereo)
            for (int i = offset; i < offset + samplesRead; i += 2)
            {
                if (i + 1 < offset + samplesRead)
                {
                    var left = buffer[i];
                    var right = buffer[i + 1];

                    if (_isDrums)
                        _manager.ProcessDrumSample(left, right);
                    else
                        _manager.ProcessMelodicSample(left, right);
                }
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// Simple stereo lowpass filter for warm, soft sound
    /// Uses biquad filter with adjustable cutoff frequency
    /// </summary>
    internal class LowpassFilterSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private float _cutoffFrequency = 20000f; // Default: no filtering
        private float _targetCutoffFrequency = 20000f;

        // Biquad filter coefficients
        private float _a0, _a1, _a2, _b1, _b2;

        // Filter state for left and right channels
        private float _x1L, _x2L, _y1L, _y2L; // Left channel history
        private float _x1R, _x2R, _y1R, _y2R; // Right channel history

        public WaveFormat WaveFormat => _source.WaveFormat;

        public LowpassFilterSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels != 2)
                throw new ArgumentException("Source must be stereo");
            _source = source;
            UpdateCoefficients();
        }

        public float CutoffFrequency
        {
            get => _targetCutoffFrequency;
            set
            {
                _targetCutoffFrequency = Math.Clamp(value, 200f, 20000f);
            }
        }

        private void UpdateCoefficients()
        {
            // Smooth transition to target frequency
            const float smoothing = 0.05f; // Slow smooth transition
            _cutoffFrequency += (_targetCutoffFrequency - _cutoffFrequency) * smoothing;

            // Biquad lowpass filter design
            var sampleRate = WaveFormat.SampleRate;
            var omega = 2.0f * MathF.PI * _cutoffFrequency / sampleRate;
            var cosOmega = MathF.Cos(omega);
            var sinOmega = MathF.Sin(omega);
            var alpha = sinOmega / (2.0f * 0.707f); // Q = 0.707 (Butterworth)

            var b0 = (1.0f - cosOmega) / 2.0f;
            var b1 = 1.0f - cosOmega;
            var b2 = (1.0f - cosOmega) / 2.0f;
            var a0 = 1.0f + alpha;
            var a1 = -2.0f * cosOmega;
            var a2 = 1.0f - alpha;

            // Normalize coefficients
            _a0 = b0 / a0;
            _a1 = b1 / a0;
            _a2 = b2 / a0;
            _b1 = a1 / a0;
            _b2 = a2 / a0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            // Update filter coefficients if cutoff changed
            UpdateCoefficients();

            // Apply lowpass filter to stereo audio
            for (int i = offset; i < offset + samplesRead; i += 2)
            {
                // Left channel
                float inputL = buffer[i];
                float outputL = _a0 * inputL + _a1 * _x1L + _a2 * _x2L - _b1 * _y1L - _b2 * _y2L;

                _x2L = _x1L;
                _x1L = inputL;
                _y2L = _y1L;
                _y1L = outputL;

                buffer[i] = outputL;

                // Right channel
                float inputR = buffer[i + 1];
                float outputR = _a0 * inputR + _a1 * _x1R + _a2 * _x2R - _b1 * _y1R - _b2 * _y2R;

                _x2R = _x1R;
                _x1R = inputR;
                _y2R = _y1R;
                _y1R = outputR;

                buffer[i + 1] = outputR;
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// Custom stereo balance provider for STEREO audio (not mono panning)
    /// Adjusts the balance between left and right channels
    /// -1 = 100% left, 0 = center (both equal), +1 = 100% right
    /// </summary>
    internal class StereoBalanceSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public float Pan { get; set; } = 0f; // -1 to +1

        public WaveFormat WaveFormat => _source.WaveFormat;

        public StereoBalanceSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels != 2)
                throw new ArgumentException("Source must be stereo");
            _source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            // Apply stereo balance
            for (int i = offset; i < offset + samplesRead; i += 2)
            {
                float left = buffer[i];
                float right = buffer[i + 1];

                if (Pan < 0) // Pan left: reduce right channel
                {
                    float leftGain = 1.0f;
                    float rightGain = 1.0f + Pan; // Pan = -1 means rightGain = 0
                    buffer[i] = left * leftGain;
                    buffer[i + 1] = right * rightGain;
                }
                else if (Pan > 0) // Pan right: reduce left channel
                {
                    float leftGain = 1.0f - Pan; // Pan = +1 means leftGain = 0
                    float rightGain = 1.0f;
                    buffer[i] = left * leftGain;
                    buffer[i + 1] = right * rightGain;
                }
                // else Pan == 0: no change (center)
            }

            return samplesRead;
        }
    }
}
