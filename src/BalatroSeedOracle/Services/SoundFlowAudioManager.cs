#if !BROWSER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Visualization;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Multi-track audio manager using SoundFlow cross-platform engine
    /// Plays 8 independent audio stems with per-track volume control and FFT analysis
    /// CROSS-PLATFORM: Works on Windows, Mac, Linux, iOS, Android
    /// </summary>
    public class SoundFlowAudioManager : IDisposable
    {
        private const int UPDATE_RATE_MS = 16; // ~60 FPS

        // Singleton instance
        public static SoundFlowAudioManager Instance { get; } = new();

        // SoundFlow components
        private AudioEngine? _engine;
        private AudioPlaybackDevice? _device;
        private readonly Dictionary<string, SoundPlayer> _players = new();
        private readonly Dictionary<string, SpectrumAnalyzer> _analyzers = new();
        private readonly Dictionary<string, SoundPlayer> _sfxPlayers = new();

        // Track names
        private readonly string[] _trackNames =
        {
            "Bass1",
            "Bass2",
            "Drums1",
            "Drums2",
            "Chords1",
            "Chords2",
            "Melody1",
            "Melody2",
        };

        // Sound effect names
        private readonly string[] _sfxNames = { "highlight1", "paper1", "button" };

        // Update loop
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _updateTask;
        private bool _isDisposed;

        #region Public Properties - Per-Track Intensities

        public float Bass1Intensity { get; private set; }
        public float Bass2Intensity { get; private set; }
        public float Drums1Intensity { get; private set; }
        public float Drums2Intensity { get; private set; }
        public float Chords1Intensity { get; private set; }
        public float Chords2Intensity { get; private set; }
        public float Melody1Intensity { get; private set; }
        public float Melody2Intensity { get; private set; }

        #endregion

        #region Public Properties - Aggregate Intensities (for compatibility)

        public float BassIntensity => (Bass1Intensity + Bass2Intensity) / 2f;
        public float DrumsIntensity => (Drums1Intensity + Drums2Intensity) / 2f;
        public float ChordsIntensity => (Chords1Intensity + Chords2Intensity) / 2f;
        public float MelodyIntensity => (Melody1Intensity + Melody2Intensity) / 2f;

        #endregion

        #region Public Properties - Master Controls

        private float _masterVolume = 1.0f;
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Math.Clamp(value, 0f, 1f);
                if (_device != null)
                    // Apply 2.5x gain boost (audio files are mastered too quiet)
                    _device.MasterMixer.Volume = _masterVolume * 2.5f;
            }
        }

        public bool IsPlaying => _device?.IsRunning == true;

        #endregion

        #region Events

        public event Action<float, float, float, float>? AudioAnalysisUpdated;

        #endregion

        public SoundFlowAudioManager()
        {
            try
            {
                DebugLogger.Log(
                    "SoundFlowAudioManager",
                    "Initializing cross-platform audio engine..."
                );

                // 1. Create audio engine (cross-platform)
                _engine = new MiniAudioEngine();

                // 2. Define audio format
                var format = AudioFormat.Cd; // 44.1kHz, 16-bit Stereo

                // 3. Initialize playback device (default device)
                var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
                _device = _engine.InitializePlaybackDevice(defaultDevice, format);

                DebugLogger.Log(
                    "SoundFlowAudioManager",
                    $"Initialized playback device: {_device.Info?.Name ?? "Default"}"
                );

                // 4. Load all 8 tracks as SoundPlayers
                LoadTracks(format);

                // 4b. Load sound effects (highlight1.ogg, paper1.ogg, button.ogg)
                LoadSoundEffects(format);

                // 5. Start the device MUTED (settings will restore user's volume preference)
                _device.MasterMixer.Volume = 0f; // CRITICAL: Start muted to prevent audio blast on startup
                _masterVolume = 0f; // Sync internal state
                _device.Start();

                DebugLogger.Log(
                    "SoundFlowAudioManager",
                    $"Device started MUTED (waiting for user settings to restore volume) with {_players.Count} independent tracks"
                );

                // 6. Start analysis update loop
                _cancellationTokenSource = new CancellationTokenSource();
                _updateTask = Task.Run(AnalysisUpdateLoop, _cancellationTokenSource.Token);

                DebugLogger.Log(
                    "SoundFlowAudioManager",
                    $"✓ Initialized with {_players.Count} tracks, {_sfxPlayers.Count} SFX, and REAL FFT analysis"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SoundFlowAudioManager",
                    $"Error initializing SoundFlow: {ex.Message}"
                );
                DebugLogger.LogError("SoundFlowAudioManager", $"Stack trace: {ex.StackTrace}");
            }
        }

        private void LoadTracks(AudioFormat format)
        {
            var audioDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
            if (!Directory.Exists(audioDir))
            {
                throw new DirectoryNotFoundException(
                    $"[SoundFlowAudioManager] Assets/Audio directory not found at: {audioDir}"
                );
            }

            // Load each track - OGG format (cross-platform, web-optimized)
            foreach (var trackName in _trackNames)
            {
                var filePath = Path.Combine(audioDir, $"{trackName}.ogg");
                if (!File.Exists(filePath))
                {
                    DebugLogger.LogError("SoundFlowAudioManager", $"Missing {trackName}.ogg");
                    continue;
                }

                try
                {
                    DebugLogger.Log("SoundFlowAudioManager", $"Loading {trackName} from {filePath}");

                    // Create StreamDataProvider - this will auto-detect format and decode
                    var fileStream = File.OpenRead(filePath);
                    var dataProvider = new StreamDataProvider(_engine!, format, fileStream);

                    // Create SoundPlayer
                    var player = new SoundPlayer(_engine!, format, dataProvider);
                    player.Name = trackName;
                    player.Volume = 1.0f; // 100% by default
                    player.IsLooping = true; // Loop all tracks

                    // Create SpectrumAnalyzer for FFT analysis
                    const int fftSize = 2048; // Power of 2 for FFT
                    var analyzer = new SpectrumAnalyzer(format, fftSize, visualizer: null);

                    // Attach analyzer to player
                    player.AddAnalyzer(analyzer);

                    // Add player to MasterMixer
                    _device!.MasterMixer.AddComponent(player);

                    // Start playback
                    player.Play();

                    _players[trackName] = player;
                    _analyzers[trackName] = analyzer;

                    var extension = Path.GetExtension(filePath);
                    DebugLogger.Log(
                        "SoundFlowAudioManager",
                        $"✓ Loaded and playing: {trackName}{extension}"
                    );
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SoundFlowAudioManager",
                        $"Error loading {trackName}: {ex.Message}"
                    );
                    DebugLogger.LogError("SoundFlowAudioManager", $"  Stack: {ex.StackTrace}");
                }
            }
        }

        private void LoadSoundEffects(AudioFormat format)
        {
            var sfxDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "SFX");
            if (!Directory.Exists(sfxDir))
            {
                throw new DirectoryNotFoundException(
                    $"[SoundFlowAudioManager] Assets/Audio/SFX directory not found at: {sfxDir}"
                );
            }

            // Load each sound effect
            foreach (var sfxName in _sfxNames)
            {
                var filePath = Path.Combine(sfxDir, $"{sfxName}.ogg");
                if (!File.Exists(filePath))
                {
                    DebugLogger.LogError(
                        "SoundFlowAudioManager",
                        $"Missing {sfxName}.ogg at {filePath}"
                    );
                    continue;
                }

                try
                {
                    DebugLogger.Log("SoundFlowAudioManager", $"Loading SFX {sfxName} from {filePath}");

                    // Create StreamDataProvider for the SFX file
                    var fileStream = File.OpenRead(filePath);
                    var dataProvider = new StreamDataProvider(_engine!, format, fileStream);

                    // Create SoundPlayer for SFX
                    var player = new SoundPlayer(_engine!, format, dataProvider);
                    player.Name = sfxName;
                    player.Volume = 1.0f; // 100% by default
                    player.IsLooping = false; // Don't loop SFX

                    // Add player to MasterMixer (but don't start playing yet)
                    _device!.MasterMixer.AddComponent(player);

                    _sfxPlayers[sfxName] = player;

                    DebugLogger.Log("SoundFlowAudioManager", $"✓ Loaded SFX: {sfxName}.ogg");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "SoundFlowAudioManager",
                        $"Error loading SFX {sfxName}: {ex.Message}"
                    );
                    DebugLogger.LogError("SoundFlowAudioManager", $"  Stack: {ex.StackTrace}");
                }
            }
        }

        private async Task AnalysisUpdateLoop()
        {
            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    UpdateTrackIntensities();
                    await Task.Delay(UPDATE_RATE_MS, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SoundFlowAudioManager", $"Analysis error: {ex.Message}");
                }
            }
        }

        private void UpdateTrackIntensities()
        {
            // Update per-track intensities from FFT analyzers
            Bass1Intensity = GetTrackIntensity("Bass1");
            Bass2Intensity = GetTrackIntensity("Bass2");
            Drums1Intensity = GetTrackIntensity("Drums1");
            Drums2Intensity = GetTrackIntensity("Drums2");
            Chords1Intensity = GetTrackIntensity("Chords1");
            Chords2Intensity = GetTrackIntensity("Chords2");
            Melody1Intensity = GetTrackIntensity("Melody1");
            Melody2Intensity = GetTrackIntensity("Melody2");

            // Fire aggregate event for compatibility
            float bass = BassIntensity;
            float mid = ChordsIntensity;
            float treble = MelodyIntensity;
            float peak = Math.Max(Math.Max(bass, mid), treble);

            AudioAnalysisUpdated?.Invoke(bass, mid, treble, peak);
        }

        private float GetTrackIntensity(string trackName)
        {
            if (!_analyzers.TryGetValue(trackName, out var analyzer))
                return 0f;

            // Get FFT spectrum data
            var fftData = analyzer.SpectrumData;
            if (fftData.Length == 0)
                return 0f;

            // Calculate RMS (root mean square) of FFT magnitudes
            float sum = 0f;
            foreach (var magnitude in fftData)
            {
                sum += magnitude * magnitude;
            }
            return (float)Math.Sqrt(sum / fftData.Length);
        }

        /// <summary>
        /// Get frequency band data (Bass/Mid/High) from a track's FFT analyzer
        /// Sample rate is 44100 Hz, FFT size is 2048
        /// Each bin represents: sampleRate / fftSize = 44100 / 2048 ≈ 21.5 Hz per bin
        /// </summary>
        public FrequencyBands GetFrequencyBands(string trackName)
        {
            if (!_analyzers.TryGetValue(trackName, out var analyzer))
                return new FrequencyBands();

            var fftData = analyzer.SpectrumData;
            if (fftData.Length == 0)
                return new FrequencyBands();

            const float sampleRate = 44100f;
            float hzPerBin = sampleRate / 2048f; // ~21.5 Hz per bin

            // Frequency ranges (in Hz):
            // Bass: 20-250 Hz (bins 1-12)
            // Mid: 250-2000 Hz (bins 12-93)
            // High: 2000-20000 Hz (bins 93-930)

            int bassStart = (int)(20f / hzPerBin);
            int bassEnd = (int)(250f / hzPerBin);
            int midEnd = (int)(2000f / hzPerBin);
            int highEnd = Math.Min((int)(20000f / hzPerBin), fftData.Length - 1);

            return new FrequencyBands
            {
                BassAvg = CalculateBandAverage(fftData, bassStart, bassEnd),
                BassPeak = CalculateBandPeak(fftData, bassStart, bassEnd),
                MidAvg = CalculateBandAverage(fftData, bassEnd, midEnd),
                MidPeak = CalculateBandPeak(fftData, bassEnd, midEnd),
                HighAvg = CalculateBandAverage(fftData, midEnd, highEnd),
                HighPeak = CalculateBandPeak(fftData, midEnd, highEnd),
            };
        }

        private float CalculateBandAverage(ReadOnlySpan<float> fftData, int startBin, int endBin)
        {
            if (startBin >= endBin || startBin >= fftData.Length)
                return 0f;

            endBin = Math.Min(endBin, fftData.Length);

            float sum = 0f;
            for (int i = startBin; i < endBin; i++)
            {
                sum += fftData[i];
            }
            return sum / (endBin - startBin);
        }

        private float CalculateBandPeak(ReadOnlySpan<float> fftData, int startBin, int endBin)
        {
            if (startBin >= endBin || startBin >= fftData.Length)
                return 0f;

            endBin = Math.Min(endBin, fftData.Length);

            float peak = 0f;
            for (int i = startBin; i < endBin; i++)
            {
                if (fftData[i] > peak)
                    peak = fftData[i];
            }
            return peak;
        }

        /// <summary>
        /// Set volume for a specific track (0.0 = silent, 1.0 = 100%)
        /// </summary>
        public void SetTrackVolume(string trackName, float volume)
        {
            if (_players.TryGetValue(trackName, out var player))
            {
                player.Volume = Math.Clamp(volume, 0f, 1f);
                DebugLogger.Log("SoundFlowAudioManager", $"Set {trackName} volume to {volume:P0}");
            }
        }

        /// <summary>
        /// Set pan for a specific track (0.0 = left, 0.5 = center, 1.0 = right)
        /// </summary>
        public void SetTrackPan(string trackName, float pan)
        {
            if (_players.TryGetValue(trackName, out var player))
            {
                player.Pan = Math.Clamp(pan, 0f, 1f);
                DebugLogger.Log("SoundFlowAudioManager", $"Set {trackName} pan to {pan:F2}");
            }
        }

        /// <summary>
        /// Mute/unmute a specific track
        /// </summary>
        public void SetTrackMuted(string trackName, bool muted)
        {
            if (_players.TryGetValue(trackName, out var player))
            {
                player.Mute = muted;
                DebugLogger.Log("SoundFlowAudioManager", $"Set {trackName} muted={muted}");
            }
        }

        public void Pause()
        {
            foreach (var player in _players.Values)
            {
                player.Pause();
            }
        }

        public void Resume()
        {
            foreach (var player in _players.Values)
            {
                player.Play();
            }
        }

        /// <summary>
        /// Play a pre-loaded sound effect by name
        /// Available effects: highlight1, paper1, button
        /// </summary>
        public void PlaySfx(string name, float volume = 1.0f)
        {
            if (!_sfxPlayers.TryGetValue(name, out var player))
                return;

            try
            {
                player.Volume = Math.Clamp(volume, 0f, 1f);
                player.Seek(TimeSpan.Zero);
                player.Play();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Stop update loop
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            // Stop all music tracks
            foreach (var player in _players.Values)
            {
                player.Stop();
                player.Dispose();
            }
            _players.Clear();
            _analyzers.Clear();

            // Stop all sound effects
            foreach (var player in _sfxPlayers.Values)
            {
                player.Stop();
                player.Dispose();
            }
            _sfxPlayers.Clear();

            // Stop and dispose device
            _device?.Stop();
            _device?.Dispose();

            // Dispose engine
            _engine?.Dispose();

            DebugLogger.Log("SoundFlowAudioManager", "Disposed");
        }
    }

    /// <summary>
    /// Frequency band data extracted from FFT analysis
    /// Bass: 20-250 Hz, Mid: 250-2000 Hz, High: 2000-20000 Hz
    /// </summary>
    public struct FrequencyBands
    {
        public float BassAvg { get; set; } // Average magnitude in bass range
        public float BassPeak { get; set; } // Peak magnitude in bass range
        public float MidAvg { get; set; } // Average magnitude in mid range
        public float MidPeak { get; set; } // Peak magnitude in mid range
        public float HighAvg { get; set; } // Average magnitude in high range
        public float HighPeak { get; set; } // Peak magnitude in high range
    }
}
#else
// Browser implementation using Web Audio API via JavaScript interop
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Services
{
    public sealed partial class SoundFlowAudioManager : IDisposable
    {
        private const int UPDATE_RATE_MS = 16; // ~60 FPS

        // Singleton instance
        public static SoundFlowAudioManager Instance { get; } = new();

        // Track names
        private readonly string[] _trackNames =
        {
            "Bass1", "Bass2", "Drums1", "Drums2", "Chords1", "Chords2", "Melody1", "Melody2",
        };

        // Sound effect names
        private readonly string[] _sfxNames = { "highlight1", "paper1", "button" };

        // Update loop
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _updateTask;
        private bool _isDisposed;
        private bool _isInitialized;

        #region Public Properties - Per-Track Intensities

        public float Bass1Intensity { get; private set; }
        public float Bass2Intensity { get; private set; }
        public float Drums1Intensity { get; private set; }
        public float Drums2Intensity { get; private set; }
        public float Chords1Intensity { get; private set; }
        public float Chords2Intensity { get; private set; }
        public float Melody1Intensity { get; private set; }
        public float Melody2Intensity { get; private set; }

        #endregion

        #region Public Properties - Aggregate Intensities

        public float BassIntensity => (Bass1Intensity + Bass2Intensity) / 2f;
        public float DrumsIntensity => (Drums1Intensity + Drums2Intensity) / 2f;
        public float ChordsIntensity => (Chords1Intensity + Chords2Intensity) / 2f;
        public float MelodyIntensity => (Melody1Intensity + Melody2Intensity) / 2f;

        #endregion

        #region Public Properties - Master Controls

        private float _masterVolume = 0f;
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Math.Clamp(value, 0f, 1f);
                SetMasterVolumeJS(_masterVolume);
            }
        }

        public bool IsPlaying => _isInitialized;

        #endregion

        #region Events

        public event Action<float, float, float, float>? AudioAnalysisUpdated;

        #endregion

        public SoundFlowAudioManager()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                DebugLogger.Log("SoundFlowAudioManager", "Initializing Web Audio API for browser...");

                // Initialize Web Audio API
                await InitializeWebAudioJS();

                // Load all tracks
                var audioBaseUrl = "/Assets/Audio/";
                foreach (var trackName in _trackNames)
                {
                    var audioUrl = $"{audioBaseUrl}{trackName}.ogg";
                    await LoadTrackJS(trackName, audioUrl, true);
                }

                // Load sound effects
                var sfxBaseUrl = "/Assets/Audio/SFX/";
                foreach (var sfxName in _sfxNames)
                {
                    var audioUrl = $"{sfxBaseUrl}{sfxName}.ogg";
                    await LoadSfxJS(sfxName, audioUrl);
                }

                // Start muted
                SetMasterVolumeJS(0f);

                // Start analysis update loop
                _cancellationTokenSource = new CancellationTokenSource();
                _updateTask = Task.Run(AnalysisUpdateLoop, _cancellationTokenSource.Token);

                _isInitialized = true;
                DebugLogger.Log("SoundFlowAudioManager", "✓ Web Audio API initialized with 8 tracks and SFX");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SoundFlowAudioManager", $"Error initializing Web Audio API: {ex.Message}");
            }
        }

        private async Task AnalysisUpdateLoop()
        {
            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    UpdateTrackIntensities();
                    await Task.Delay(UPDATE_RATE_MS, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SoundFlowAudioManager", $"Analysis error: {ex.Message}");
                }
            }
        }

        private void UpdateTrackIntensities()
        {
            // Get frequency bands and calculate intensity from bass peak
            Bass1Intensity = GetTrackIntensity("Bass1");
            Bass2Intensity = GetTrackIntensity("Bass2");
            Drums1Intensity = GetTrackIntensity("Drums1");
            Drums2Intensity = GetTrackIntensity("Drums2");
            Chords1Intensity = GetTrackIntensity("Chords1");
            Chords2Intensity = GetTrackIntensity("Chords2");
            Melody1Intensity = GetTrackIntensity("Melody1");
            Melody2Intensity = GetTrackIntensity("Melody2");

            // Fire aggregate event
            float bass = BassIntensity;
            float mid = ChordsIntensity;
            float treble = MelodyIntensity;
            float peak = Math.Max(Math.Max(bass, mid), treble);

            AudioAnalysisUpdated?.Invoke(bass, mid, treble, peak);
        }

        private float GetTrackIntensity(string trackName)
        {
            var bands = GetFrequencyBands(trackName);
            // Use bass peak as intensity indicator
            return bands.BassPeak;
        }

        public FrequencyBands GetFrequencyBands(string trackName)
        {
            var jsObj = GetFrequencyBandsJS(trackName);
            if (jsObj == null) return new FrequencyBands();
            
            return new FrequencyBands
            {
                BassAvg = (float)jsObj.GetPropertyAsDouble("bassAvg"),
                BassPeak = (float)jsObj.GetPropertyAsDouble("bassPeak"),
                MidAvg = (float)jsObj.GetPropertyAsDouble("midAvg"),
                MidPeak = (float)jsObj.GetPropertyAsDouble("midPeak"),
                HighAvg = (float)jsObj.GetPropertyAsDouble("highAvg"),
                HighPeak = (float)jsObj.GetPropertyAsDouble("highPeak"),
            };
        }

        public void SetTrackVolume(string trackName, float volume)
        {
            SetTrackVolumeJS(trackName, Math.Clamp(volume, 0f, 1f));
        }

        public void SetTrackPan(string trackName, float pan)
        {
            // Web Audio API panning - not implemented in basic version
            DebugLogger.Log("SoundFlowAudioManager", $"Pan not yet implemented for browser (track: {trackName}, pan: {pan})");
        }

        public void SetTrackMuted(string trackName, bool muted)
        {
            SetTrackMutedJS(trackName, muted);
        }

        public void Pause()
        {
            PauseJS();
        }

        public void Resume()
        {
            _ = ResumeJS();
        }

        public void PlaySfx(string name, float volume = 1.0f)
        {
            PlaySfxJS(name, Math.Clamp(volume, 0f, 1f));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            DisposeJS();
            DebugLogger.Log("SoundFlowAudioManager", "Disposed");
        }

        // JavaScript interop methods
        [JSImport("WebAudioManager.initialize", "js/webaudio-interop.js")]
        private static partial Task<bool> InitializeWebAudioJS();

        [JSImport("WebAudioManager.loadTrack", "js/webaudio-interop.js")]
        private static partial Task<bool> LoadTrackJS(string trackName, string audioUrl, bool loop);

        [JSImport("WebAudioManager.loadSfx", "js/webaudio-interop.js")]
        private static partial Task<bool> LoadSfxJS(string sfxName, string audioUrl);

        [JSImport("WebAudioManager.setTrackVolume", "js/webaudio-interop.js")]
        private static partial bool SetTrackVolumeJS(string trackName, float volume);

        [JSImport("WebAudioManager.setMasterVolume", "js/webaudio-interop.js")]
        private static partial bool SetMasterVolumeJS(float volume);

        [JSImport("WebAudioManager.setTrackMuted", "js/webaudio-interop.js")]
        private static partial bool SetTrackMutedJS(string trackName, bool muted);

        [JSImport("WebAudioManager.pause", "js/webaudio-interop.js")]
        private static partial bool PauseJS();

        [JSImport("WebAudioManager.resume", "js/webaudio-interop.js")]
        private static partial Task<bool> ResumeJS();

        [JSImport("WebAudioManager.playSfx", "js/webaudio-interop.js")]
        private static partial bool PlaySfxJS(string sfxName, float volume);

        [JSImport("WebAudioManager.getFrequencyBands", "js/webaudio-interop.js")]
        private static partial JSObject GetFrequencyBandsJS(string trackName);

        [JSImport("WebAudioManager.dispose", "js/webaudio-interop.js")]
        private static partial void DisposeJS();
    }

    /// <summary>
    /// Frequency band data extracted from FFT analysis
    /// Bass: 20-250 Hz, Mid: 250-2000 Hz, High: 2000-20000 Hz
    /// </summary>
    public struct FrequencyBands
    {
        public float BassAvg { get; set; }
        public float BassPeak { get; set; }
        public float MidAvg { get; set; }
        public float MidPeak { get; set; }
        public float HighAvg { get; set; }
        public float HighPeak { get; set; }
    }
}
#endif
