namespace BalatroSeedOracle.Services;

/// <summary>
/// UI sound effects (card drops, tab switches, seed-found chimes).
/// TIER0-STUB: replace in Tier 1. All playback is a silent no-op until the audio asset
/// pipeline (NAudio/Vorbis) is restored.
/// </summary>
public class SoundEffectService
{
    public static SoundEffectService Instance { get; } = new();

    public void PlayFilterSwitch() { }

    public void PlayCardDrop() { }
}
