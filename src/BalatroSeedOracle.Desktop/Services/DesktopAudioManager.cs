using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using SoundFlow.Visualization;

namespace BalatroSeedOracle.Desktop.Services;

/// <summary>
/// Desktop implementation of IAudioManager using SoundFlow library.
/// Registered via DI in Desktop Program.cs
/// </summary>
public class DesktopAudioManager : IAudioManager, IDisposable
{
    private const int UPDATE_RATE_MS = 16;
    private AudioEngine? _engine;
    private AudioPlaybackDevice? _device;
    private readonly Dictionary<string, SoundPlayer> _players = new();
    private readonly Dictionary<string, SpectrumAnalyzer> _analyzers = new();
    private readonly Dictionary<string, SoundPlayer> _sfxPlayers = new();
    private readonly string[] _trackNames = { "Bass1", "Bass2", "Drums1", "Drums2", "Chords1", "Chords2", "Melody1", "Melody2" };
    private readonly string[] _sfxNames = { "highlight1", "paper1", "button" };
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _updateTask;
    private bool _isDisposed;

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
            if (_device != null)
                _device.MasterMixer.Volume = _masterVolume * 2.5f;
        }
    }

    public bool IsPlaying => _device?.IsRunning == true;
    public event Action<float, float, float, float>? AudioAnalysisUpdated;

    public DesktopAudioManager()
    {
        try
        {
            _engine = new MiniAudioEngine();
            var format = AudioFormat.Cd;
            var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
            _device = _engine.InitializePlaybackDevice(defaultDevice, format);
            LoadTracks(format);
            LoadSoundEffects(format);
            _device.MasterMixer.Volume = 0f;
            _masterVolume = 0f;
            _device.Start();
            _cancellationTokenSource = new CancellationTokenSource();
            _updateTask = Task.Run(AnalysisUpdateLoop, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DesktopAudioManager", $"Error initializing: {ex.Message}");
        }
    }

    private void LoadTracks(AudioFormat format)
    {
        var audioDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
        if (!Directory.Exists(audioDir)) return;

        foreach (var trackName in _trackNames)
        {
            var filePath = Path.Combine(audioDir, $"{trackName}.ogg");
            if (!File.Exists(filePath)) continue;
            try
            {
                var fileStream = File.OpenRead(filePath);
                var dataProvider = new StreamDataProvider(_engine!, format, fileStream);
                var player = new SoundPlayer(_engine!, format, dataProvider) { Name = trackName, Volume = 1.0f, IsLooping = true };
                var analyzer = new SpectrumAnalyzer(format, 2048, visualizer: null);
                player.AddAnalyzer(analyzer);
                _device!.MasterMixer.AddComponent(player);
                player.Play();
                _players[trackName] = player;
                _analyzers[trackName] = analyzer;
            }
            catch { }
        }
    }

    private void LoadSoundEffects(AudioFormat format)
    {
        var sfxDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "SFX");
        if (!Directory.Exists(sfxDir)) return;

        foreach (var sfxName in _sfxNames)
        {
            var filePath = Path.Combine(sfxDir, $"{sfxName}.ogg");
            if (!File.Exists(filePath)) continue;
            try
            {
                var fileStream = File.OpenRead(filePath);
                var dataProvider = new StreamDataProvider(_engine!, format, fileStream);
                var player = new SoundPlayer(_engine!, format, dataProvider) { Name = sfxName, Volume = 1.0f, IsLooping = false };
                _device!.MasterMixer.AddComponent(player);
                _sfxPlayers[sfxName] = player;
            }
            catch { }
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
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private void UpdateTrackIntensities()
    {
        Bass1Intensity = GetTrackIntensity("Bass1");
        Bass2Intensity = GetTrackIntensity("Bass2");
        Drums1Intensity = GetTrackIntensity("Drums1");
        Drums2Intensity = GetTrackIntensity("Drums2");
        Chords1Intensity = GetTrackIntensity("Chords1");
        Chords2Intensity = GetTrackIntensity("Chords2");
        Melody1Intensity = GetTrackIntensity("Melody1");
        Melody2Intensity = GetTrackIntensity("Melody2");
        AudioAnalysisUpdated?.Invoke(BassIntensity, ChordsIntensity, MelodyIntensity, Math.Max(Math.Max(BassIntensity, ChordsIntensity), MelodyIntensity));
    }

    private float GetTrackIntensity(string trackName)
    {
        if (!_analyzers.TryGetValue(trackName, out var analyzer)) return 0f;
        var fftData = analyzer.SpectrumData;
        if (fftData.Length == 0) return 0f;
        float sum = 0f;
        foreach (var magnitude in fftData) sum += magnitude * magnitude;
        return (float)Math.Sqrt(sum / fftData.Length);
    }

    public FrequencyBands GetFrequencyBands(string trackName)
    {
        if (!_analyzers.TryGetValue(trackName, out var analyzer)) return new FrequencyBands();
        var fftData = analyzer.SpectrumData;
        if (fftData.Length == 0) return new FrequencyBands();
        const float sampleRate = 44100f;
        float hzPerBin = sampleRate / 2048f;
        int bassStart = (int)(20f / hzPerBin), bassEnd = (int)(250f / hzPerBin), midEnd = (int)(2000f / hzPerBin), highEnd = Math.Min((int)(20000f / hzPerBin), fftData.Length - 1);
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
        if (startBin >= endBin || startBin >= fftData.Length) return 0f;
        endBin = Math.Min(endBin, fftData.Length);
        float sum = 0f;
        for (int i = startBin; i < endBin; i++) sum += fftData[i];
        return sum / (endBin - startBin);
    }

    private float CalculateBandPeak(ReadOnlySpan<float> fftData, int startBin, int endBin)
    {
        if (startBin >= endBin || startBin >= fftData.Length) return 0f;
        endBin = Math.Min(endBin, fftData.Length);
        float peak = 0f;
        for (int i = startBin; i < endBin; i++) if (fftData[i] > peak) peak = fftData[i];
        return peak;
    }

    public void SetTrackVolume(string trackName, float volume) { if (_players.TryGetValue(trackName, out var player)) player.Volume = Math.Clamp(volume, 0f, 1f); }
    public void SetTrackPan(string trackName, float pan) { if (_players.TryGetValue(trackName, out var player)) player.Pan = Math.Clamp(pan, 0f, 1f); }
    public void SetTrackMuted(string trackName, bool muted) { if (_players.TryGetValue(trackName, out var player)) player.Mute = muted; }
    public void Pause() { foreach (var player in _players.Values) player.Pause(); }
    public void Resume() { foreach (var player in _players.Values) player.Play(); }
    public void PlaySfx(string name, float volume = 1.0f) { if (_sfxPlayers.TryGetValue(name, out var player)) { player.Volume = Math.Clamp(volume, 0f, 1f); player.Seek(TimeSpan.Zero); player.Play(); } }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        foreach (var player in _players.Values) { player.Stop(); player.Dispose(); }
        _players.Clear(); _analyzers.Clear();
        foreach (var player in _sfxPlayers.Values) { player.Stop(); player.Dispose(); }
        _sfxPlayers.Clear();
        _device?.Stop(); _device?.Dispose();
        _engine?.Dispose();
    }
}
