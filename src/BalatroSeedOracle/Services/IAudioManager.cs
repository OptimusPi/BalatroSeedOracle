namespace BalatroSeedOracle.Services;

/// <summary>
/// Interface for audio management - platform-agnostic abstraction
/// Following Avalonia UI pattern: interface in shared project, implementation in platform head projects
/// </summary>
public interface IAudioManager
{
    float MasterVolume { get; set; }
    void SetTrackVolume(string trackName, float volume);
    void PlaySfx(string name, float volume);
    event System.Action<float, float, float, float>? AudioAnalysisUpdated;
}
