using System;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services;

/// <summary>Music-reactive additions to the background shader's uniforms.</summary>
public readonly record struct AudioReactiveOffsets(float Spin, float Zoom, float Saturation);

/// <summary>
/// Turns the playing stems' raw RMS levels into 0..1 visualizer levels (per stem group, with an
/// adaptive peak so quiet and loud parts both move the picture, fast attack and slower release)
/// and maps them onto shader effects per the profile's <see cref="VisualizerSettings"/>: each
/// effect's source group (0=None, 1=Drums, 2=Bass, 3=Chords, 4=Melody), its intensity (0..100)
/// and the global AudioIntensity (0..2).
/// </summary>
public sealed class MusicVisualizer
{
    /// <summary>Largest spin_amount boost at full level and intensity.</summary>
    public const float MaxSpinBoost = 0.4f;

    /// <summary>Largest zoom_scale boost (the shader scales uv by 30 + zoom_scale).</summary>
    public const float MaxZoomBoost = 5f;

    /// <summary>Largest saturation_amount boost.</summary>
    public const float MaxSaturationBoost = 1f;

    private const float Attack = 0.6f;
    private const float Release = 0.15f;
    private const float PeakDecay = 0.995f;
    private const float PeakFloor = 0.02f;

    // Indexed by MusicGroup (1..4); slot 0 is "None" and stays 0.
    private readonly float[] _peaks = new float[5];
    private readonly float[] _levels = new float[5];

    /// <summary>Current smoothed 0..1 level of a group.</summary>
    public float Level(MusicGroup group) => _levels[(int)group];

    /// <summary>Advance one frame with the raw RMS of each group.</summary>
    public void Update(Func<MusicGroup, float> rawLevel)
    {
        ArgumentNullException.ThrowIfNull(rawLevel);
        foreach (var group in Enum.GetValues<MusicGroup>())
        {
            var i = (int)group;
            var raw = rawLevel(group);
            raw = float.IsFinite(raw) ? Math.Max(0f, raw) : 0f;
            _peaks[i] = Math.Max(raw, Math.Max(PeakFloor, _peaks[i] * PeakDecay));
            var target = Math.Clamp(raw / _peaks[i], 0f, 1f);
            var rate = target > _levels[i] ? Attack : Release;
            _levels[i] += (target - _levels[i]) * rate;
        }
    }

    /// <summary>Shader offsets for the current levels and visualizer settings.</summary>
    public AudioReactiveOffsets Map(VisualizerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var k = Math.Clamp(settings.AudioIntensity, 0f, 2f);
        return new AudioReactiveOffsets(
            SourceLevel(settings.SpinSource) * Percent(settings.SpinIntensity) * k * MaxSpinBoost,
            SourceLevel(settings.ZoomThumpSource) * Percent(settings.ZoomThumpIntensity) * k * MaxZoomBoost,
            SourceLevel(settings.ColorSaturationSource) * Percent(settings.ColorSaturationIntensity) * k * MaxSaturationBoost
        );
    }

    private float SourceLevel(int source) => source is >= 1 and <= 4 ? _levels[source] : 0f;

    private static float Percent(float value) => float.IsFinite(value) ? Math.Clamp(value / 100f, 0f, 1f) : 0f;
}
