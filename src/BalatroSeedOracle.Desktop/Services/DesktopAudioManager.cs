using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IAudioManager on the Avalonia Accelerate MediaPlayer.
/// Eight stem players (Bass/Drums/Chords/Melody x2) loop in lockstep so the per-stem
/// volume mixer keeps working. Shader reactivity comes from precomputed 30Hz RMS
/// envelopes (Assets/Audio/envelopes.json) sampled at the playback position — no
/// runtime FFT, no native audio-analysis dependency.
/// </summary>
public class DesktopAudioManager : IAudioManager, IDisposable
{
    private static readonly string[] TrackNames =
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
    private static readonly string[] SfxNames = { "highlight1", "paper1", "button" };

    private readonly Dictionary<string, MediaPlayer> _players = new();
    private readonly Dictionary<string, MediaPlayer> _sfxPlayers = new();
    private readonly Dictionary<string, float> _trackGains = new();
    private readonly Dictionary<string, bool> _trackMuted = new();
    private Dictionary<string, float[]> _envelopes = new();
    private double _envelopeRateHz = 30.0;
    private DispatcherTimer? _intensityTimer;
    private bool _isPaused;
    private bool _isDisposed;
    private bool _initialized;

    public float Bass1Intensity { get; private set; }
    public float Bass2Intensity { get; private set; }
    public float Drums1Intensity { get; private set; }
    public float Drums2Intensity { get; private set; }
    public float Chords1Intensity { get; private set; }
    public float Chords2Intensity { get; private set; }
    public float Melody1Intensity { get; private set; }
    public float Melody2Intensity { get; private set; }
    public float BassIntensity => (Bass1Intensity + Bass2Intensity) / 2f;
    public float DrumsIntensity => (Drums1Intensity + Drums2Intensity) / 2f;
    public float ChordsIntensity => (Chords1Intensity + Chords2Intensity) / 2f;
    public float MelodyIntensity => (Melody1Intensity + Melody2Intensity) / 2f;

    private float _masterVolume = 1.0f;
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Math.Clamp(value, 0f, 1f);
            ApplyVolumes();
        }
    }

    public bool IsPlaying => _initialized && !_isPaused;
    public event Action<float, float, float, float>? AudioAnalysisUpdated;

    public DesktopAudioManager() { }

    /// <summary>
    /// Initializes MediaPlayer instances. Runs on the UI thread (MediaPlayer is an Avalonia
    /// component and is awaited from startup after the window is shown). Failures are logged
    /// and degrade to silent audio, never a crash.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var audioDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
            if (!Directory.Exists(audioDir))
            {
                DebugLogger.LogError("DesktopAudioManager", $"Audio dir missing: {audioDir}");
                return;
            }

            LoadEnvelopes(Path.Combine(audioDir, "envelopes.json"));

            foreach (var name in TrackNames)
            {
                var path = Path.Combine(audioDir, $"{name}.ogg");
                if (!File.Exists(path))
                    continue;
                var player = new MediaPlayer();
                await player.InitializeAsync();
                await player.SetSourceAsync(new UriSource(new Uri(path).AbsoluteUri));
                await player.PrepareAsync();
                player.IsLoopingEnabled = true;
                _players[name] = player;
                _trackGains[name] = 1.0f;
                _trackMuted[name] = false;
            }

            var sfxDir = Path.Combine(audioDir, "SFX");
            foreach (var name in SfxNames)
            {
                var path = Path.Combine(sfxDir, $"{name}.ogg");
                if (!File.Exists(path))
                    continue;
                var player = new MediaPlayer();
                await player.InitializeAsync();
                await player.SetSourceAsync(new UriSource(new Uri(path).AbsoluteUri));
                await player.PrepareAsync();
                _sfxPlayers[name] = player;
            }

            if (_players.Count == 0)
            {
                DebugLogger.LogError("DesktopAudioManager", "No stems loaded — silent run");
                return;
            }

            ApplyVolumes();
            await StartAllStemsAsync();

            // 30Hz intensity sampling from precomputed envelopes, driven off stem[0]'s clock.
            _intensityTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(33),
                DispatcherPriority.Background,
                (_, _) => UpdateIntensities()
            );
            _intensityTimer.Start();
            _initialized = true;
            DebugLogger.Log(
                "DesktopAudioManager",
                $"MediaPlayer audio up: {_players.Count} stems, {_sfxPlayers.Count} sfx"
            );
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DesktopAudioManager", $"Error initializing: {ex.Message}");
        }
    }

    private void LoadEnvelopes(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                DebugLogger.LogError("DesktopAudioManager", $"envelopes.json missing: {path}");
                return;
            }
            // JsonDocument, not the reflection serializer — the Desktop head builds with
            // JsonSerializerIsReflectionEnabledByDefault=false.
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            _envelopeRateHz = root.GetProperty("rateHz").GetDouble();
            var stems = new Dictionary<string, float[]>();
            foreach (var stem in root.GetProperty("stems").EnumerateObject())
            {
                var arr = new float[stem.Value.GetArrayLength()];
                int i = 0;
                foreach (var v in stem.Value.EnumerateArray())
                    arr[i++] = (float)v.GetDouble();
                stems[stem.Name] = arr;
            }
            _envelopes = stems;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DesktopAudioManager", $"envelopes.json unreadable: {ex.Message}");
        }
    }

    private async Task StartAllStemsAsync()
    {
        // Stems loop natively (IsLoopingEnabled); this only starts the initial lockstep run.
        foreach (var player in _players.Values)
            await player.PlayAsync();
    }

    private void UpdateIntensities()
    {
        if (_isPaused || _players.Count == 0)
            return;
        // One clock for all stems: the first player's position.
        TimeSpan pos = TimeSpan.Zero;
        foreach (var player in _players.Values)
        {
            pos = player.Position;
            break;
        }

        Bass1Intensity = SampleEnvelope("Bass1", pos);
        Bass2Intensity = SampleEnvelope("Bass2", pos);
        Drums1Intensity = SampleEnvelope("Drums1", pos);
        Drums2Intensity = SampleEnvelope("Drums2", pos);
        Chords1Intensity = SampleEnvelope("Chords1", pos);
        Chords2Intensity = SampleEnvelope("Chords2", pos);
        Melody1Intensity = SampleEnvelope("Melody1", pos);
        Melody2Intensity = SampleEnvelope("Melody2", pos);

        AudioAnalysisUpdated?.Invoke(
            BassIntensity,
            ChordsIntensity,
            MelodyIntensity,
            Math.Max(Math.Max(BassIntensity, ChordsIntensity), MelodyIntensity)
        );
    }

    // Envelopes are peak-normalized 0..1, but the shader bridge's vibe knobs were tuned
    // against the old FFT RMS scale (~0.02..0.1). Feeding raw 0..1 made the background
    // spin ~10x harder than designed. This rescales to the range the tuning expects.
    private const float EnvelopeToFftScale = 0.1f;

    private float SampleEnvelope(string trackName, TimeSpan position)
    {
        if (!_envelopes.TryGetValue(trackName, out var env) || env.Length == 0)
            return 0f;
        // A muted or silenced stem contributes nothing to the visualizer, same as live FFT did.
        if (_trackMuted.GetValueOrDefault(trackName) || _trackGains.GetValueOrDefault(trackName) <= 0f)
            return 0f;
        int idx = (int)(position.TotalSeconds * _envelopeRateHz);
        if (idx < 0)
            idx = 0;
        if (idx >= env.Length)
            idx = env.Length - 1;
        return env[idx] * _trackGains.GetValueOrDefault(trackName, 1f) * EnvelopeToFftScale;
    }

    private void ApplyVolumes()
    {
        foreach (var (name, player) in _players)
        {
            player.Volume = Math.Clamp(_trackGains.GetValueOrDefault(name, 1f) * _masterVolume, 0f, 1f);
            player.IsMuted = _trackMuted.GetValueOrDefault(name);
        }
    }

    public void SetTrackVolume(string trackName, float volume)
    {
        _trackGains[trackName] = Math.Clamp(volume, 0f, 1f);
        ApplyVolumes();
    }

    // MediaPlayer has no per-player pan. No caller in the app uses pan; honest no-op.
    public void SetTrackPan(string trackName, float pan) { }

    public void SetTrackMuted(string trackName, bool muted)
    {
        _trackMuted[trackName] = muted;
        ApplyVolumes();
    }

    public void Pause()
    {
        _isPaused = true;
        foreach (var player in _players.Values)
            _ = player.PauseAsync();
    }

    public void Resume()
    {
        _isPaused = false;
        foreach (var player in _players.Values)
            _ = player.PlayAsync();
    }

    public async void PlaySfx(string name, float volume = 1.0f)
    {
        if (!_sfxPlayers.TryGetValue(name, out var player))
            return;
        try
        {
            player.Volume = Math.Clamp(volume * _masterVolume, 0f, 1f);
            player.Position = TimeSpan.Zero;
            await player.PlayAsync();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DesktopAudioManager", $"SFX '{name}' failed: {ex.Message}");
        }
    }

    // Live FFT is gone (envelope-driven reactivity instead) and this has zero callers.
    // Kept only to satisfy IAudioManager.
    public FrequencyBands GetFrequencyBands(string trackName) => default;

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _intensityTimer?.Stop();
        foreach (var player in _players.Values)
        {
            _ = ShutdownPlayerAsync(player);
        }
        _players.Clear();
        foreach (var player in _sfxPlayers.Values)
            _ = ShutdownPlayerAsync(player);
        _sfxPlayers.Clear();
    }

    private static async Task ShutdownPlayerAsync(MediaPlayer player)
    {
        try
        {
            await player.StopAsync();
            await player.ReleaseAsync();
            await player.UnInitialize();
        }
        catch
        {
            // App is going down; nothing useful to do with a shutdown failure.
        }
    }
}
