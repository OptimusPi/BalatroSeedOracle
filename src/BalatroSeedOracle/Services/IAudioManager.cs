namespace BalatroSeedOracle.Services;

/// <summary>
/// Interface for audio management - platform-agnostic abstraction
/// Following Avalonia UI pattern: interface in shared project, implementation in platform head projects
/// </summary>
public interface IAudioManager
{
    float MasterVolume { get; set; }
    bool IsPlaying { get; }
    float Bass1Intensity { get; }
    float Bass2Intensity { get; }
    float Drums1Intensity { get; }
    float Drums2Intensity { get; }
    float Chords1Intensity { get; }
    float Chords2Intensity { get; }
    float Melody1Intensity { get; }
    float Melody2Intensity { get; }
    float BassIntensity { get; }
    float DrumsIntensity { get; }
    float ChordsIntensity { get; }
    float MelodyIntensity { get; }
    void SetTrackVolume(string trackName, float volume);
    void SetTrackPan(string trackName, float pan);
    void SetTrackMuted(string trackName, bool muted);
    void Pause();
    void Resume();
    void PlaySfx(string name, float volume = 1.0f);
    FrequencyBands GetFrequencyBands(string trackName);
    event System.Action<float, float, float, float>? AudioAnalysisUpdated;
}

public struct FrequencyBands
{
    public float BassAvg { get; set; }
    public float BassPeak { get; set; }
    public float MidAvg { get; set; }
    public float MidPeak { get; set; }
    public float HighAvg { get; set; }
    public float HighPeak { get; set; }
}
