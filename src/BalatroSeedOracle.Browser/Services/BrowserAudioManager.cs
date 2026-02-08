using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser implementation of IAudioManager using Web Audio API via JavaScript interop.
/// Registered via DI in Browser Program.cs
/// </summary>
public sealed partial class BrowserAudioManager : IAudioManager, IDisposable
{
    private const int UPDATE_RATE_MS = 16;
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
    private readonly string[] _sfxNames = { "highlight1", "paper1", "button" };
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _updateTask;
    private bool _isDisposed;
    private bool _isInitialized;

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
    public event Action<float, float, float, float>? AudioAnalysisUpdated;

    public BrowserAudioManager()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await InitializeWebAudioJS();
            // Use relative paths so audio works when served under any base path (e.g. /BSO/)
            var audioBaseUrl = "Assets/Audio/";
            foreach (var trackName in _trackNames)
            {
                try
                {
                    await LoadTrackJS(trackName, $"{audioBaseUrl}{trackName}.ogg", true);
                }
                catch (Exception trackEx)
                {
                    DebugLogger.LogError("BrowserAudioManager", $"Skipping track {trackName}: {trackEx.Message}");
                }
            }
            var sfxBaseUrl = "Assets/Audio/SFX/";
            foreach (var sfxName in _sfxNames)
            {
                try
                {
                    await LoadSfxJS(sfxName, $"{sfxBaseUrl}{sfxName}.ogg");
                }
                catch (Exception sfxEx)
                {
                    DebugLogger.LogError("BrowserAudioManager", $"Skipping SFX {sfxName}: {sfxEx.Message}");
                }
            }
            // Sync current master volume to JS (ViewModel may have set it before we were ready; don't overwrite with 0)
            SetMasterVolumeJS(_masterVolume);
            _cancellationTokenSource = new CancellationTokenSource();
            _updateTask = Task.Run(AnalysisUpdateLoop, _cancellationTokenSource.Token);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("BrowserAudioManager", $"Error initializing: {ex.Message}");
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
        AudioAnalysisUpdated?.Invoke(
            BassIntensity,
            ChordsIntensity,
            MelodyIntensity,
            Math.Max(Math.Max(BassIntensity, ChordsIntensity), MelodyIntensity)
        );
    }

    private float GetTrackIntensity(string trackName) => GetFrequencyBands(trackName).BassPeak;

    public FrequencyBands GetFrequencyBands(string trackName)
    {
        // Don't call into JS if audio never initialized - avoids synchronous JS interop
        // from background threads which triggers "Blocking the thread" warnings and can
        // cause Mono assertion crashes on the Finalizer thread.
        if (!_isInitialized)
            return new FrequencyBands();

        try
        {
            var jsObj = GetFrequencyBandsJS(trackName);
            if (jsObj == null)
                return new FrequencyBands();
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
        catch
        {
            return new FrequencyBands();
        }
    }

    public void SetTrackVolume(string trackName, float volume) =>
        SetTrackVolumeJS(trackName, Math.Clamp(volume, 0f, 1f));

    public void SetTrackPan(string trackName, float pan) { }

    public void SetTrackMuted(string trackName, bool muted) => SetTrackMutedJS(trackName, muted);

    public void Pause() => PauseJS();

    public void Resume() => _ = ResumeJS();

    public void PlaySfx(string name, float volume = 1.0f) =>
        PlaySfxJS(name, Math.Clamp(volume, 0f, 1f));

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        DisposeJS();
    }

    // JS interop imports for Web Audio API
    // Using globalThis.WebAudioManager since the JS module sets window.WebAudioManager globally
    // Per Microsoft guidance: when JS doesn't export from module, use global scope in [JSImport]
    [JSImport("globalThis.WebAudioManager.initialize")]
    private static partial Task InitializeWebAudioJS();

    [JSImport("globalThis.WebAudioManager.loadTrack")]
    private static partial Task LoadTrackJS(string trackName, string audioUrl, bool loop);

    [JSImport("globalThis.WebAudioManager.loadSfx")]
    private static partial Task LoadSfxJS(string sfxName, string audioUrl);

    [JSImport("globalThis.WebAudioManager.setTrackVolume")]
    private static partial bool SetTrackVolumeJS(string trackName, float volume);

    [JSImport("globalThis.WebAudioManager.setMasterVolume")]
    private static partial bool SetMasterVolumeJS(float volume);

    [JSImport("globalThis.WebAudioManager.setTrackMuted")]
    private static partial bool SetTrackMutedJS(string trackName, bool muted);

    [JSImport("globalThis.WebAudioManager.pause")]
    private static partial bool PauseJS();

    [JSImport("globalThis.WebAudioManager.resume")]
    private static partial Task ResumeJS();

    [JSImport("globalThis.WebAudioManager.playSfx")]
    private static partial bool PlaySfxJS(string sfxName, float volume);

    [JSImport("globalThis.WebAudioManager.getFrequencyBands")]
    private static partial JSObject GetFrequencyBandsJS(string trackName);

    [JSImport("globalThis.WebAudioManager.dispose")]
    private static partial void DisposeJS();
}
