using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
    public enum AudioState
    {
        MainMenu,      // Drums1 + Bass1 (low volume)
        ModalOpen,     // + Chords1  
        VibeLevel1,    // Active search - Drums1 + Bass1 + Chords1
        VibeLevel2,    // Good seeds found - DRUMS2 + Bass1 + Chords1 (BEATS KICK IN!)
        VibeLevel3     // Maximum vibe - Drums2 + Bass2 + Chords2 + Melody1
    }

    public class VibeAudioManager : IDisposable
    {
        private WaveOutEvent? _waveOut;
        private MixingSampleProvider? _mixer;
        
        // Individual audio tracks with gain control
        private readonly Dictionary<string, AudioTrack> _tracks = new();
        private AudioState _currentState = AudioState.VibeLevel1; // Start as different state to force transition
        
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

        // Individual track intensities
        public float MelodyIntensity => GetTrackIntensity("Melody1", "Melody2");
        public float ChordsIntensity => GetTrackIntensity("Chords1", "Chords2");
        public float BassTrackIntensity => GetTrackIntensity("Bass1", "Bass2");

        // Beat detection state
        private float _lastBassLevel = 0f;
        private int _beatCooldown = 0;
        private const int BEAT_COOLDOWN_FRAMES = 20; // ~400ms between beats (prevent doubles)
        
        // Events for UI integration
        public event Action<float, float, float, float>? AudioAnalysisUpdated; // bass, mid, treble, peak
        public event Action<float>? BeatDetected; // beat intensity

        private float GetTrackIntensity(string track1, string track2)
        {
            float intensity = 0f;
            if (_tracks.TryGetValue(track1, out var t1))
                intensity += t1.CurrentVolume;
            if (_tracks.TryGetValue(track2, out var t2))
                intensity += t2.CurrentVolume;
            return intensity;
        }
        
        public VibeAudioManager()
        {
            Console.WriteLine("🎵🎵🎵 VIBE AUDIO MANAGER CONSTRUCTOR CALLED! 🎵🎵🎵");
            try
            {
                InitializeAudio();
                Console.WriteLine("✅ InitializeAudio() completed");

                LoadAllTracks();
                Console.WriteLine($"✅ LoadAllTracks() completed - Loaded {_tracks.Count} tracks");

                TransitionTo(AudioState.MainMenu);
                Console.WriteLine("✅ TransitionTo(MainMenu) completed");

                Console.WriteLine($"🎵 VIBE AUDIO MANAGER READY! {_tracks.Count} tracks loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ VIBE AUDIO MANAGER FAILED: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void InitializeAudio()
        {
            try
            {
                _waveOut = new WaveOutEvent();
                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
                _mixer.ReadFully = true;

                _waveOut.Init(_mixer);
                _waveOut.Volume = 1.0f;
                _waveOut.Play();
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

            // Load all your custom tracks
            var trackNames = new[] { "Drums1", "Drums2", "Bass1", "Bass2", "Chords1", "Chords2", "Melody1", "Melody2" };

            foreach (var trackName in trackNames)
            {
                // Try OGG first (preferred), then WAV, then MP3
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

                            // ADD ALL TRACKS TO MIXER IMMEDIATELY (at silent volume)
                            if (_mixer != null)
                            {
                                ISampleProvider provider;

                                // Drums -> beat detection, Others -> color intensity
                                if (trackName == "Drums1" || trackName == "Drums2")
                                {
                                    provider = new AudioAnalyzerProvider(track.SampleProvider, this, true); // Drums for beats
                                }
                                else if (trackName.StartsWith("Bass") || trackName.StartsWith("Chords") || trackName.StartsWith("Melody"))
                                {
                                    provider = new AudioAnalyzerProvider(track.SampleProvider, this, false); // Melodic for colors
                                }
                                else
                                {
                                    provider = track.SampleProvider; // No analysis
                                }

                                _mixer.AddMixerInput(provider);
                                track.IsInMixer = true;
                                track.SetImmediateVolume(0.01f);  // Nearly silent but still playing for sync
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
        }
        
        public void TransitionTo(AudioState newState)
        {
            if (_currentState == newState) return;
            
            DebugLogger.Log("VibeAudioManager", $"🎵 Audio transition: {_currentState} → {newState}");
            _currentState = newState;
            
            // First, set all tracks to target volumes
            foreach (var track in _tracks.Values)
            {
                track.SetTargetVolume(0f); // Default to off
            }
            
            // Configure tracks for the new state
            switch (newState)
            {
                case AudioState.MainMenu:
                    // FULL VOLUME ALL TRACKS!
                    SetTrackVolume("Drums1", 1.0f);
                    SetTrackVolume("Bass1", 1.0f);
                    SetTrackVolume("Chords1", 1.0f);
                    SetTrackVolume("Melody1", 1.0f);
                    break;
                    
                case AudioState.ModalOpen:
                    SetTrackVolume("Drums1", 0.7f);
                    SetTrackVolume("Bass1", 0.6f);
                    SetTrackVolume("Chords1", 0.5f);
                    break;
                    
                case AudioState.VibeLevel1:
                    SetTrackVolume("Drums1", 0.8f);
                    SetTrackVolume("Bass1", 0.7f);
                    SetTrackVolume("Chords1", 0.6f);
                    break;
                    
                case AudioState.VibeLevel2:
                    // THE DRUMS SWITCH! 🔥
                    SetTrackVolume("Drums1", 0f);    // Fade out calm drums
                    SetTrackVolume("Drums2", 0.9f);  // SICK BEATS ACTIVATE!
                    SetTrackVolume("Bass1", 0.8f);
                    SetTrackVolume("Chords1", 0.7f);
                    break;
                    
                case AudioState.VibeLevel3:
                    // MAXIMUM OVERDRIVE! 🚀
                    SetTrackVolume("Drums2", 1.0f);  // FULL BEATS
                    SetTrackVolume("Bass2", 0.9f);   // THICC BASS
                    SetTrackVolume("Chords2", 0.8f); // RICH HARMONY
                    SetTrackVolume("Melody1", 0.7f); // MELODY ENTERS
                    break;
            }
            
            // Add active tracks to mixer if not already present
            EnsureActiveTracksInMixer();
        }
        
        private void SetTrackVolume(string trackName, float targetVolume)
        {
            if (_tracks.TryGetValue(trackName, out var track))
            {
                Console.WriteLine($"SetTrackVolume: {trackName} = {targetVolume}");
                track.SetImmediateVolume(targetVolume);  // ACTUALLY SET THE VOLUME NOW!
            }
            else
            {
                Console.WriteLine($"⚠️ Track not found: {trackName}");
            }
        }
        
        private void EnsureActiveTracksInMixer()
        {
            if (_mixer == null) return;

            Console.WriteLine($"EnsureActiveTracksInMixer called - checking {_tracks.Count} tracks");

            foreach (var track in _tracks.Values)
            {
                Console.WriteLine($"  Track: {track.Name}, TargetVolume: {track.TargetVolume}, IsInMixer: {track.IsInMixer}");

                if (track.TargetVolume > 0f && !track.IsInMixer)
                {
                    Console.WriteLine($"    Adding {track.Name} to mixer...");
                    // Determine if this is a drum track or melodic
                    bool isDrums = track.Name == "Drums1" || track.Name == "Drums2";
                    var analyzerProvider = new AudioAnalyzerProvider(track.SampleProvider, this, isDrums);
                    _mixer.AddMixerInput(analyzerProvider);
                    track.IsInMixer = true;
                    Console.WriteLine($"    ✅ {track.Name} added to mixer");
                }
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

            // AMPLIFY: Tracks are at low volume for sync, so amplify FFT values significantly
            const float amplification = 100f;
            bassLevel *= amplification;
            midLevel *= amplification;
            trebleLevel *= amplification;
            peakLevel *= amplification;

            // Smooth the values (exponential moving average)
            const float smoothing = 0.8f;
            AudioBass = AudioBass * smoothing + bassLevel * (1f - smoothing);
            AudioMid = AudioMid * smoothing + midLevel * (1f - smoothing);
            AudioTreble = AudioTreble * smoothing + trebleLevel * (1f - smoothing);
            AudioPeak = AudioPeak * smoothing + peakLevel * (1f - smoothing);
            
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

            // Amplify melodic content (HUGE amplification since tracks at 0.01 volume)
            const float amplification = 10000f; // 100x more since tracks are at 1% volume
            midLevel *= amplification;
            trebleLevel *= amplification;
            peakLevel *= amplification;

            // Smooth the values
            const float smoothing = 0.8f;
            AudioMid = AudioMid * smoothing + midLevel * (1f - smoothing);
            AudioTreble = AudioTreble * smoothing + trebleLevel * (1f - smoothing);
            AudioPeak = AudioPeak * smoothing + peakLevel * (1f - smoothing);

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
        
        // Public interface for game events
        public void OnModalOpened() => TransitionTo(AudioState.ModalOpen);
        public void OnModalClosed() => TransitionTo(AudioState.MainMenu);
        public void OnSearchStarted() => TransitionTo(AudioState.VibeLevel1);
        public void OnGoodSeedFound(int score)
        {
            if (score > 50)
            {
                TransitionTo(AudioState.VibeLevel2); // DRUMS2 KICKS IN!
            }
            if (score > 100)
            {
                TransitionTo(AudioState.VibeLevel3); // MAXIMUM VIBE!
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            if (_waveOut != null)
            {
                _waveOut.Volume = Math.Clamp(volume, 0f, 1f);
            }
        }

        public void SetMusicVolume(float volume) => SetMasterVolume(volume);
        public void SetSfxVolume(float volume) { /* TODO: SFX volume */ }
        
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
            _waveOut?.Stop();
            _waveOut?.Dispose();
            
            foreach (var track in _tracks.Values)
            {
                track.Dispose();
            }
            _tracks.Clear();
        }
    }
    
    // Helper class for individual audio tracks with smooth volume control
    internal class AudioTrack : IDisposable
    {
        public string Name { get; }
        public ISampleProvider SampleProvider { get; }
        public float CurrentVolume { get; private set; }
        public float TargetVolume { get; private set; }
        public bool IsInMixer { get; set; }
        
        private readonly WaveStream _reader;
        private readonly LoopStream _loopStream;
        private readonly VolumeSampleProvider _volumeProvider;
        
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
            
            // All files are 48kHz stereo - no resampling needed!
            _volumeProvider = new VolumeSampleProvider(_loopStream.ToSampleProvider()) { Volume = 0f };
            SampleProvider = _volumeProvider;
        }
        
        public void SetTargetVolume(float volume)
        {
            TargetVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void SetImmediateVolume(float volume)
        {
            TargetVolume = Math.Clamp(volume, 0f, 1f);
            CurrentVolume = TargetVolume;
            _volumeProvider.Volume = CurrentVolume;
            Console.WriteLine($"  → {Name} volume NOW: {_volumeProvider.Volume}");
        }

        public void UpdateVolume()
        {
            // Smooth volume transitions
            const float speed = 0.05f;
            if (Math.Abs(CurrentVolume - TargetVolume) > 0.01f)
            {
                CurrentVolume += (TargetVolume - CurrentVolume) * speed;
                _volumeProvider.Volume = CurrentVolume;
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
}
