using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Json;
using BalatroSeedOracle.Models;
using NAudio.Vorbis;
using NAudio.Wave;

namespace BalatroSeedOracle.Services;

/// <summary>The eight music stems in Assets/Audio (one file each, all the same length).</summary>
public enum MusicStem
{
    Drums1,
    Drums2,
    Bass1,
    Bass2,
    Chords1,
    Chords2,
    Melody1,
    Melody2,
}

/// <summary>Stem groups the visualizer settings name as audio sources (1=Drums, 2=Bass, 3=Chords, 4=Melody).</summary>
public enum MusicGroup
{
    Drums = 1,
    Bass = 2,
    Chords = 3,
    Melody = 4,
}

/// <summary>One stem for <see cref="StemLoopMixer"/>: a stereo float source plus how to restart it at frame 0.</summary>
public sealed class StemSource
{
    public StemSource(ISampleProvider provider, Action rewind, long lengthFrames)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Rewind = rewind ?? throw new ArgumentNullException(nameof(rewind));
        LengthFrames = lengthFrames;
    }

    public ISampleProvider Provider { get; }
    public Action Rewind { get; }
    public long LengthFrames { get; }
}

/// <summary>
/// Plays every stem in lock-step from one shared frame counter and loops them all together:
/// when the counter reaches <see cref="LoopFrames"/> (the shortest stem) every stem is rewound
/// to frame 0 and the same output buffer keeps filling, so the loop has no gap and no
/// repeated or dropped frame. Each stem is scaled by its mix gain and the sum by the master
/// volume. While the master volume is 0 (muted) the output is silence and the stems do not
/// advance (no decoding), so unmuting resumes where it stopped.
/// <para>
/// Per-stem RMS levels (after the mix gain, before the master volume) are published after
/// every block for the background visualizer.
/// </para>
/// </summary>
public sealed class StemLoopMixer : ISampleProvider
{
    private const int Channels = 2;

    private readonly StemSource[] _stems;
    private readonly Func<float> _masterVolume;
    private float[] _gains;
    private readonly float[] _levels;
    private float[] _scratch = new float[8192];
    private long _position;
    private long _framesPlayed;
    private int _loops;
    private bool? _wasAudible;

    public StemLoopMixer(IReadOnlyList<StemSource> stems, float[] gains, Func<float> masterVolume)
    {
        ArgumentNullException.ThrowIfNull(stems);
        if (stems.Count == 0)
            throw new ArgumentException("At least one stem is required", nameof(stems));
        foreach (var s in stems)
        {
            if (s.Provider.WaveFormat.Channels != Channels || s.Provider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException($"Stems must be stereo float, got {s.Provider.WaveFormat}", nameof(stems));
        }
        _stems = stems.ToArray();
        _masterVolume = masterVolume ?? throw new ArgumentNullException(nameof(masterVolume));
        _gains = CheckGains(gains);
        _levels = new float[_stems.Length];
        LoopFrames = _stems.Min(s => s.LengthFrames);
        if (LoopFrames <= 0)
            throw new ArgumentException("Stems are empty", nameof(stems));
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_stems[0].Provider.WaveFormat.SampleRate, Channels);
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>Frames in one loop: the shortest stem's length.</summary>
    public long LoopFrames { get; }

    /// <summary>Current frame inside the loop (0..LoopFrames-1).</summary>
    public long PositionFrames => Interlocked.Read(ref _position);

    /// <summary>Frames output while audible since start (diagnostics).</summary>
    public long FramesPlayed => Interlocked.Read(ref _framesPlayed);

    /// <summary>Completed loops since start.</summary>
    public int LoopCount => Volatile.Read(ref _loops);

    /// <summary>Raised on the audio thread when the loop wraps (argument: loops completed).</summary>
    public event Action<int>? Looped;

    /// <summary>Raised on the audio thread when output switches between audible and muted.</summary>
    public event Action<bool>? AudibleChanged;

    /// <summary>Replace the per-stem gains (same order as the stems). Takes effect on the next block.</summary>
    public void SetGains(float[] gains) => Volatile.Write(ref _gains, CheckGains(gains));

    /// <summary>Copy of the per-stem gains in use.</summary>
    public float[] Gains => (float[])Volatile.Read(ref _gains).Clone();

    /// <summary>RMS of one stem in the last block (0 when muted).</summary>
    public float GetLevel(int stem) => Volatile.Read(ref _levels[stem]);

    public int Read(float[] buffer, int offset, int count)
    {
        count -= count % Channels;
        if (count <= 0)
            return 0;

        var master = _masterVolume();
        master = float.IsFinite(master) ? Math.Clamp(master, 0f, 1f) : 0f;
        var audible = master > 0f;
        if (_wasAudible != audible)
        {
            _wasAudible = audible;
            AudibleChanged?.Invoke(audible);
        }

        Array.Clear(buffer, offset, count);
        if (!audible)
        {
            for (var i = 0; i < _levels.Length; i++)
                Volatile.Write(ref _levels[i], 0f);
            return count;
        }

        if (_scratch.Length < count)
            _scratch = new float[count];
        var gains = Volatile.Read(ref _gains);
        Span<double> sumSquares = stackalloc double[_stems.Length];

        var framesNeeded = count / Channels;
        var done = 0;
        while (done < framesNeeded)
        {
            var chunk = (int)Math.Min(framesNeeded - done, LoopFrames - _position);
            var samples = chunk * Channels;
            var outStart = offset + done * Channels;
            for (var i = 0; i < _stems.Length; i++)
            {
                // Every stem reads exactly `chunk` frames, even at gain 0, so they stay in step.
                ReadExactly(_stems[i].Provider, _scratch, samples);
                var g = gains[i];
                if (g == 0f)
                    continue;
                double sq = 0;
                for (var k = 0; k < samples; k++)
                {
                    var v = _scratch[k] * g;
                    sq += v * v;
                    buffer[outStart + k] += v * master;
                }
                sumSquares[i] += sq;
            }

            done += chunk;
            var next = _position + chunk;
            if (next >= LoopFrames)
            {
                foreach (var stem in _stems)
                    stem.Rewind();
                next = 0;
                var loops = Interlocked.Increment(ref _loops);
                Looped?.Invoke(loops);
            }
            Interlocked.Exchange(ref _position, next);
        }

        for (var i = 0; i < _stems.Length; i++)
            Volatile.Write(ref _levels[i], (float)Math.Sqrt(sumSquares[i] / count));
        Interlocked.Add(ref _framesPlayed, framesNeeded);
        return count;
    }

    /// <summary>Root mean square of a sample block (0 for an empty block).</summary>
    public static float Rms(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
            return 0f;
        double sq = 0;
        foreach (var s in samples)
            sq += (double)s * s;
        return (float)Math.Sqrt(sq / samples.Length);
    }

    private static void ReadExactly(ISampleProvider provider, float[] scratch, int samples)
    {
        var filled = 0;
        while (filled < samples)
        {
            var read = provider.Read(scratch, filled, samples - filled);
            if (read <= 0)
                break;
            filled += read;
        }
        if (filled < samples)
            Array.Clear(scratch, filled, samples - filled); // stem ended early: silence, keep the frame count
    }

    private float[] CheckGains(float[] gains)
    {
        ArgumentNullException.ThrowIfNull(gains);
        if (gains.Length != _stems.Length)
            throw new ArgumentException($"Expected {_stems.Length} gains, got {gains.Length}", nameof(gains));
        return gains.Select(g => float.IsFinite(g) ? Math.Max(0f, g) : 0f).ToArray();
    }
}

/// <summary>
/// Background music: the eight Ogg stems in Assets/Audio streamed with NAudio.Vorbis (decoded
/// a block at a time on the audio thread, so nothing is held fully decoded: each stem would be
/// ~100 MB as floats), looped in sync by <see cref="StemLoopMixer"/> and fed into the single
/// NAudio output owned by <see cref="SoundEffectService"/>.
/// <para>
/// The stem mix is the original app's main-menu mixer preset (Drums1 at 40%, Bass1, Chords1
/// and Melody1 at full, the "2" variations muted), overridable with a
/// MixerSettings/mainMenu.json or MixerPresets/mainMenu.json under the data directory, and
/// scaled by the profile's per-track volumes. Pan is not applied (the stems are already stereo
/// mixes and the old presets disagree on the pan scale).
/// </para>
/// <para>
/// The master volume is read live from the profile (MusicVolume, 0 while IsMusicMuted), so the
/// main menu's volume popup controls the music directly. Every failure (missing asset, no
/// output device) is logged and leaves the app running silently.
/// </para>
/// </summary>
public sealed class MusicService : IDisposable
{
    public static readonly IReadOnlyList<MusicStem> Stems = Enum.GetValues<MusicStem>();

    private static readonly Lazy<MusicService> LazyInstance = new(() =>
        new MusicService(
            OpenAvaloniaAsset,
            SoundEffectService.ReadProfileVolume,
            ReadProfileMix,
            input => SoundEffectService.Instance.AddInput(input),
            input => SoundEffectService.Instance.RemoveInput(input),
            () => SoundEffectService.Instance.IsOutputRunning
        )
    );

    public static MusicService Instance => LazyInstance.Value;

    /// <summary>True once <see cref="Instance"/> has been created (lets shutdown skip creating it).</summary>
    public static bool IsInstanceCreated => LazyInstance.IsValueCreated;

    private readonly Func<string, Stream> _openAsset;
    private readonly Func<float> _masterVolume;
    private readonly Func<float[]> _mixGains;
    private readonly Func<ISampleProvider, bool> _attach;
    private readonly Action<ISampleProvider> _detach;
    private readonly Func<bool> _isOutputRunning;
    private readonly object _sync = new();
    private readonly List<IDisposable> _readers = new();
    private Task<bool>? _startTask;
    private StemLoopMixer? _mixer;
    private Timer? _statusTimer;
    private bool _disposed;

    /// <param name="openAsset">Opens a stem file by name (e.g. "Bass1.ogg").</param>
    /// <param name="masterVolume">Effective music volume 0..1 (0 = muted), read every audio block.</param>
    /// <param name="mixGains">Per-stem gains in <see cref="Stems"/> order.</param>
    /// <param name="attach">Adds the music to the shared output mixer; false if it could not.</param>
    /// <param name="detach">Removes it again.</param>
    /// <param name="isOutputRunning">Whether the shared output device is playing (diagnostics).</param>
    public MusicService(
        Func<string, Stream> openAsset,
        Func<float> masterVolume,
        Func<float[]> mixGains,
        Func<ISampleProvider, bool> attach,
        Action<ISampleProvider> detach,
        Func<bool> isOutputRunning
    )
    {
        _openAsset = openAsset ?? throw new ArgumentNullException(nameof(openAsset));
        _masterVolume = masterVolume ?? throw new ArgumentNullException(nameof(masterVolume));
        _mixGains = mixGains ?? throw new ArgumentNullException(nameof(mixGains));
        _attach = attach ?? throw new ArgumentNullException(nameof(attach));
        _detach = detach ?? throw new ArgumentNullException(nameof(detach));
        _isOutputRunning = isOutputRunning ?? throw new ArgumentNullException(nameof(isOutputRunning));
    }

    /// <summary>The playing loop, once started (tests pull samples from it).</summary>
    public StemLoopMixer? Mixer
    {
        get
        {
            lock (_sync)
                return _mixer;
        }
    }

    public bool IsStarted => Mixer is not null;

    /// <summary>File name of a stem inside Assets/Audio.</summary>
    public static string FileNameOf(MusicStem stem) => $"{stem}.ogg";

    /// <summary>Last-block RMS of one stem (0 before start or while muted).</summary>
    public float GetLevel(MusicStem stem) => Mixer?.GetLevel((int)stem) ?? 0f;

    /// <summary>Combined level of a stem group: the energy sum of its two stems' RMS.</summary>
    public float GetGroupLevel(MusicGroup group)
    {
        var mixer = Mixer;
        if (mixer is null)
            return 0f;
        var first = (int)(group - 1) * 2;
        var a = mixer.GetLevel(first);
        var b = mixer.GetLevel(first + 1);
        return MathF.Sqrt(a * a + b * b);
    }

    /// <summary>
    /// Open the stems and start the loop on a worker thread (never blocks the caller).
    /// Completes with false, after logging why, when music cannot play.
    /// </summary>
    public Task<bool> StartAsync()
    {
        lock (_sync)
        {
            if (_disposed)
                return Task.FromResult(false);
            return _startTask ??= Task.Run(Start);
        }
    }

    /// <summary>Re-read the stem mix (mixer preset and profile track volumes).</summary>
    public void ReloadMix()
    {
        try
        {
            Mixer?.SetGains(_mixGains());
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(MusicService), $"Cannot apply the stem mix: {ex.Message}");
        }
    }

    private bool Start()
    {
        try
        {
            var sources = new List<StemSource>(Stems.Count);
            foreach (var stem in Stems)
                sources.Add(OpenStem(stem));

            var gains = _mixGains();
            var mixer = new StemLoopMixer(sources, gains, _masterVolume);
            mixer.Looped += loops =>
                DebugLogger.Log(nameof(MusicService), $"Music loop #{loops} wrapped to frame 0 ({mixer.LoopFrames} frames per loop)");
            mixer.AudibleChanged += audible =>
                DebugLogger.Log(
                    nameof(MusicService),
                    audible
                        ? $"Music playing (volume {_masterVolume():0.00}, frame {mixer.PositionFrames})"
                        : $"Music muted (stopped at frame {mixer.PositionFrames})"
                );

            lock (_sync)
            {
                if (_disposed)
                {
                    DisposeReaders();
                    return false;
                }
                if (!_attach(mixer))
                {
                    DisposeReaders();
                    DebugLogger.LogWarning(nameof(MusicService), "Audio mixer unavailable; music is silent");
                    return false;
                }
                _mixer = mixer;
                _statusTimer = new Timer(_ => LogStatus(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            }

            var stemList = string.Join(
                ", ",
                Stems.Select((s, i) => $"{s}={gains[i]:0.00}")
            );
            DebugLogger.Log(
                nameof(MusicService),
                $"Music started: {Stems.Count} stems looping every {mixer.LoopFrames} frames "
                    + $"({mixer.LoopFrames / (double)mixer.WaveFormat.SampleRate:0.00} s), gains [{stemList}], "
                    + $"volume {_masterVolume():0.00}, output {(_isOutputRunning() ? "playing" : "not running")}"
            );
            return true;
        }
        catch (Exception ex)
        {
            DisposeReaders();
            DebugLogger.LogError(nameof(MusicService), $"Music cannot start: {ex.Message}");
            return false;
        }
    }

    private StemSource OpenStem(MusicStem stem)
    {
        // Copy the compressed file into memory (~5.7 MB) so decoding never touches the asset stream again.
        var data = new MemoryStream();
        using (var asset = _openAsset(FileNameOf(stem)))
            asset.CopyTo(data);
        data.Position = 0;

        var reader = new VorbisWaveReader(data, closeOnDispose: true);
        lock (_sync)
            _readers.Add(reader);
        var format = reader.WaveFormat;
        if (format.Channels != 2 || format.SampleRate != SoundEffectService.MixFormat.SampleRate)
            throw new NotSupportedException($"{FileNameOf(stem)} is {format}; stems must be 44.1 kHz stereo");
        var frames = reader.Length / format.BlockAlign;
        return new StemSource(reader, () => reader.Position = 0, frames);
    }

    private void LogStatus()
    {
        var mixer = Mixer;
        if (mixer is null)
            return;
        var levels = string.Join(
            " ",
            Enum.GetValues<MusicGroup>().Select(g => $"{g}={GetGroupLevel(g):0.000}")
        );
        DebugLogger.Log(
            nameof(MusicService),
            $"Music status: {(_masterVolume() > 0 ? "playing" : "muted")}, frame {mixer.PositionFrames}/{mixer.LoopFrames}, "
                + $"{mixer.FramesPlayed} frames played, loops {mixer.LoopCount}, levels {levels}, "
                + $"output {(_isOutputRunning() ? "playing" : "not running")}"
        );
    }

    private void DisposeReaders()
    {
        lock (_sync)
        {
            foreach (var r in _readers)
            {
                try
                {
                    r.Dispose();
                }
                catch
                {
                    // Best effort on teardown.
                }
            }
            _readers.Clear();
        }
    }

    public void Dispose()
    {
        StemLoopMixer? mixer;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            mixer = _mixer;
            _mixer = null;
            _statusTimer?.Dispose();
            _statusTimer = null;
        }
        if (mixer is not null)
        {
            try
            {
                _detach(mixer);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(nameof(MusicService), $"Detach failed: {ex.Message}");
            }
            DebugLogger.Log(nameof(MusicService), $"Music stopped after {mixer.FramesPlayed} frames");
        }
        // Detached from the mixer under its lock, so no audio-thread Read is still using the readers.
        DisposeReaders();
    }

    // ---------------------------------------------------------------- mix settings

    /// <summary>
    /// The original app's main-menu mix (MixerPresets/mainMenu.json): Drums1 ~40%, Bass1,
    /// Chords1 and Melody1 at 100%, all "2" variations muted.
    /// </summary>
    public static MixerSettings DefaultMainMenuMix() =>
        new()
        {
            Drums1 = new TrackSettings { Volume = 40 },
            Drums2 = new TrackSettings { Volume = 95, Muted = true },
            Bass1 = new TrackSettings { Volume = 100 },
            Bass2 = new TrackSettings { Volume = 100, Muted = true },
            Chords1 = new TrackSettings { Volume = 100 },
            Chords2 = new TrackSettings { Volume = 100, Muted = true },
            Melody1 = new TrackSettings { Volume = 100 },
            Melody2 = new TrackSettings { Volume = 86, Muted = true },
        };

    /// <summary>
    /// Per-stem gains (in <see cref="Stems"/> order) from mixer settings (volume 0..100, muted,
    /// solo: when any track is soloed only soloed tracks sound) times the profile's per-track
    /// volumes (0..1).
    /// </summary>
    public static float[] ComputeGains(MixerSettings mix, VisualizerSettings? trackVolumes)
    {
        ArgumentNullException.ThrowIfNull(mix);
        var tracks = Stems.Select(s => TrackOf(mix, s) ?? new TrackSettings()).ToArray();
        var anySolo = tracks.Any(t => t.Solo);
        var gains = new float[tracks.Length];
        for (var i = 0; i < tracks.Length; i++)
        {
            var t = tracks[i];
            if (t.Muted || (anySolo && !t.Solo))
                continue;
            var g = (float)Math.Clamp(t.Volume / 100.0, 0.0, 1.0);
            if (trackVolumes is not null)
                g *= Math.Clamp(TrackVolumeOf(trackVolumes, Stems[i]), 0f, 1f);
            gains[i] = float.IsFinite(g) ? g : 0f;
        }
        return gains;
    }

    private static TrackSettings? TrackOf(MixerSettings mix, MusicStem stem) =>
        stem switch
        {
            MusicStem.Drums1 => mix.Drums1,
            MusicStem.Drums2 => mix.Drums2,
            MusicStem.Bass1 => mix.Bass1,
            MusicStem.Bass2 => mix.Bass2,
            MusicStem.Chords1 => mix.Chords1,
            MusicStem.Chords2 => mix.Chords2,
            MusicStem.Melody1 => mix.Melody1,
            MusicStem.Melody2 => mix.Melody2,
            _ => null,
        };

    private static float TrackVolumeOf(VisualizerSettings v, MusicStem stem) =>
        stem switch
        {
            MusicStem.Drums1 => v.Drums1Volume,
            MusicStem.Drums2 => v.Drums2Volume,
            MusicStem.Bass1 => v.Bass1Volume,
            MusicStem.Bass2 => v.Bass2Volume,
            MusicStem.Chords1 => v.Chords1Volume,
            MusicStem.Chords2 => v.Chords2Volume,
            MusicStem.Melody1 => v.Melody1Volume,
            MusicStem.Melody2 => v.Melody2Volume,
            _ => 1f,
        };

    /// <summary>Load a mixer settings file, or null (logged) when it is missing or unreadable.</summary>
    public static MixerSettings? LoadMixerSettings(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            return JsonSerializer.Deserialize(File.ReadAllText(path), BsoJsonSerializerContext.Default.MixerSettings);
        }
        catch (Exception ex)
        {
            DebugLogger.LogWarning(nameof(MusicService), $"Ignoring mixer settings {path}: {ex.Message}");
            return null;
        }
    }

    private static float[] ReadProfileMix()
    {
        MixerSettings? mix = null;
        string source = "built-in main-menu mix";
        foreach (var dir in new[] { "MixerSettings", "MixerPresets" })
        {
            var path = Path.Combine(AppDataPaths.Root, dir, "mainMenu.json");
            mix = LoadMixerSettings(path);
            if (mix is not null)
            {
                source = path;
                break;
            }
        }
        var profile = App.GetService<UserProfileService>()?.GetProfile();
        var gains = ComputeGains(mix ?? DefaultMainMenuMix(), profile?.VisualizerSettings);
        DebugLogger.Log(nameof(MusicService), $"Stem mix from {source}");
        return gains;
    }

    private static Stream OpenAvaloniaAsset(string fileName) =>
        Avalonia.Platform.AssetLoader.Open(new Uri($"avares://BalatroSeedOracle/Assets/Audio/{fileName}"));
}
