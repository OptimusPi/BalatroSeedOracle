using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BalatroSeedOracle.Helpers;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BalatroSeedOracle.Services;

/// <summary>The UI sound effects shipped in Assets/Audio/SFX.</summary>
public enum SoundEffect
{
    /// <summary>button.ogg: clicks, spinner arrows.</summary>
    Button,

    /// <summary>highlight1.ogg: hover / selection highlight.</summary>
    Highlight,

    /// <summary>paper1.ogg: a card landing in a drop zone.</summary>
    Paper,
}

/// <summary>
/// UI sound effects (card drops, filter switches) played with NAudio: the .ogg assets are
/// decoded once with NAudio.Vorbis into 44.1 kHz stereo float buffers and mixed into one
/// long-lived WinMM output (<see cref="WaveOutEvent"/> + <see cref="MixingSampleProvider"/>),
/// so overlapping effects never block each other or the UI thread.
/// <para>
/// The background music (<see cref="MusicService"/>) is another input of the same mixer
/// (<see cref="AddInput"/>): one output device per process.
/// </para>
/// <para>
/// Volume follows the main-menu volume control: the live <see cref="UserProfileService"/>
/// profile's <c>MusicVolume</c>, and nothing plays while <c>IsMusicMuted</c> is set.
/// Playback is Windows-only (WinMM); elsewhere, or when no output device exists, every call
/// is a logged no-op. Audio problems are logged and never thrown.
/// </para>
/// </summary>
public sealed class SoundEffectService : IDisposable
{
    /// <summary>Format of every decoded effect and of the mixer.</summary>
    public static readonly WaveFormat MixFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private static readonly Lazy<SoundEffectService> LazyInstance = new(() =>
        new SoundEffectService(OpenAvaloniaAsset, ReadProfileVolume, enableOutput: true)
    );

    public static SoundEffectService Instance => LazyInstance.Value;

    /// <summary>Dispose the shared instance (closing the output device) if it was ever created.</summary>
    public static void DisposeShared()
    {
        if (LazyInstance.IsValueCreated)
            LazyInstance.Value.Dispose();
    }

    private readonly Func<string, Stream> _openAsset;
    private readonly Func<float> _volume;
    private readonly bool _enableOutput;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<SoundEffect, float[]?> _cache = new();

    private MixingSampleProvider? _mixer;
    private IWavePlayer? _output;
    private bool _outputFailed;
    private int _playCount;
    private bool _disposed;

    /// <param name="openAsset">Opens an SFX file by name (e.g. "button.ogg").</param>
    /// <param name="volume">Current effective volume 0..1 (0 = muted), read at every play.</param>
    /// <param name="enableOutput">False decodes and mixes without opening an audio device (tests).</param>
    public SoundEffectService(Func<string, Stream> openAsset, Func<float> volume, bool enableOutput)
    {
        _openAsset = openAsset ?? throw new ArgumentNullException(nameof(openAsset));
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
        _enableOutput = enableOutput;
    }

    /// <summary>Effects actually handed to the mixer since start (diagnostics/tests).</summary>
    public int PlayCount => Volatile.Read(ref _playCount);

    /// <summary>True once an output device is open and playing the mixer.</summary>
    public bool IsOutputRunning
    {
        get
        {
            lock (_sync)
                return _output?.PlaybackState == PlaybackState.Playing;
        }
    }

    /// <summary>The mixer effects are added to (exposed so tests can pull samples without a device).</summary>
    public ISampleProvider? Mixer
    {
        get
        {
            lock (_sync)
                return _mixer;
        }
    }

    public void PlayFilterSwitch() => Play(SoundEffect.Button);

    public void PlayCardDrop() => Play(SoundEffect.Paper);

    public void PlayButton() => Play(SoundEffect.Button);

    public void PlayHighlight() => Play(SoundEffect.Highlight);

    /// <summary>Queue an effect; returns true when it was handed to the mixer.</summary>
    public bool Play(SoundEffect effect)
    {
        try
        {
            var volume = Math.Clamp(_volume(), 0f, 1f);
            if (volume <= 0f)
                return false; // muted / volume at zero

            var samples = _cache.GetOrAdd(effect, Load);
            if (samples is null || samples.Length == 0)
                return false;

            lock (_sync)
            {
                if (_disposed)
                    return false;
                EnsureMixerUnsafe();
                if (_mixer is null)
                    return false;

                _mixer.AddMixerInput(
                    new VolumeSampleProvider(new BufferSampleProvider(samples, MixFormat)) { Volume = volume }
                );
            }

            var count = Interlocked.Increment(ref _playCount);
            DebugLogger.Log(
                nameof(SoundEffectService),
                $"Play {effect} #{count} vol={volume:0.00} ({samples.Length / MixFormat.Channels} frames, output {(IsOutputRunning ? "playing" : "not running")})"
            );
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(SoundEffectService), $"Play {effect} failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Add a long-lived input (the background music) to the same mixer and output device the
    /// effects use, opening the device if it is not open yet. There is one device and one mixer
    /// per process. Returns false when the service is disposed.
    /// </summary>
    public bool AddInput(ISampleProvider input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.WaveFormat.Equals(MixFormat))
            throw new ArgumentException($"Input must be {MixFormat}, got {input.WaveFormat}", nameof(input));
        lock (_sync)
        {
            if (_disposed)
                return false;
            EnsureMixerUnsafe();
            if (_mixer is null)
                return false;
            _mixer.AddMixerInput(input);
            return true;
        }
    }

    /// <summary>Remove an input added with <see cref="AddInput"/>.</summary>
    public void RemoveInput(ISampleProvider input)
    {
        lock (_sync)
            _mixer?.RemoveMixerInput(input);
    }

    /// <summary>File name of an effect inside Assets/Audio/SFX.</summary>
    public static string FileNameOf(SoundEffect effect) =>
        effect switch
        {
            SoundEffect.Button => "button.ogg",
            SoundEffect.Highlight => "highlight1.ogg",
            SoundEffect.Paper => "paper1.ogg",
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null),
        };

    /// <summary>Decode an Ogg Vorbis stream into interleaved <see cref="MixFormat"/> samples.</summary>
    public static float[] Decode(Stream oggStream)
    {
        using var reader = new VorbisWaveReader(oggStream, closeOnDispose: false);
        ISampleProvider provider = reader;
        if (provider.WaveFormat.Channels == 1)
            provider = new MonoToStereoSampleProvider(provider);
        else if (provider.WaveFormat.Channels != 2)
            throw new NotSupportedException($"{provider.WaveFormat.Channels}-channel effects are not supported");
        if (provider.WaveFormat.SampleRate != MixFormat.SampleRate)
            provider = new WdlResamplingSampleProvider(provider, MixFormat.SampleRate);

        var all = new List<float>(MixFormat.SampleRate * 2);
        var buffer = new float[MixFormat.SampleRate];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            all.AddRange(new ArraySegment<float>(buffer, 0, read));
        return all.ToArray();
    }

    private float[]? Load(SoundEffect effect)
    {
        var name = FileNameOf(effect);
        try
        {
            using var stream = _openAsset(name);
            var samples = Decode(stream);
            DebugLogger.Log(nameof(SoundEffectService), $"Decoded {name}: {samples.Length} samples");
            return samples;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(SoundEffectService), $"Cannot load {name}: {ex.Message}");
            return null;
        }
    }

    private void EnsureMixerUnsafe()
    {
        if (_mixer is not null)
            return;
        _mixer = new MixingSampleProvider(MixFormat) { ReadFully = true };

        if (!_enableOutput || _outputFailed)
            return;
        if (!OperatingSystem.IsWindows())
        {
            _outputFailed = true;
            DebugLogger.LogWarning(nameof(SoundEffectService), "No audio output on this platform; effects are silent");
            return;
        }

        WaveOutEvent? output = null;
        try
        {
            // Init throws (MmException) when there is no output device; handled below.
            output = new WaveOutEvent { DesiredLatency = 120, NumberOfBuffers = 3 };
            output.PlaybackStopped += (_, e) =>
            {
                if (e.Exception is not null)
                    DebugLogger.LogError(nameof(SoundEffectService), $"Audio output stopped: {e.Exception.Message}");
                else
                    DebugLogger.Log(nameof(SoundEffectService), "Audio output stopped");
            };
            output.Init(_mixer);
            output.Play();
            _output = output;
            DebugLogger.Log(
                nameof(SoundEffectService),
                $"Audio output started (WaveOutEvent, {MixFormat.SampleRate} Hz stereo, state {output.PlaybackState})"
            );
        }
        catch (Exception ex)
        {
            _outputFailed = true;
            try
            {
                output?.Dispose();
            }
            catch
            {
                // Already failing; nothing more to release.
            }
            DebugLogger.LogWarning(
                nameof(SoundEffectService),
                $"Cannot open audio output ({ex.Message}); effects are silent"
            );
        }
    }

    private static Stream OpenAvaloniaAsset(string fileName) =>
        Avalonia.Platform.AssetLoader.Open(
            new Uri($"avares://BalatroSeedOracle/Assets/Audio/SFX/{fileName}")
        );

    /// <summary>Main-menu volume (profile MusicVolume), or 0 when muted.</summary>
    internal static float ReadProfileVolume()
    {
        var profile = App.GetService<UserProfileService>()?.GetProfile();
        if (profile is null)
            return 1f;
        return profile.IsMusicMuted ? 0f : profile.MusicVolume;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            try
            {
                _output?.Stop();
                _output?.Dispose();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(nameof(SoundEffectService), $"Dispose failed: {ex.Message}");
            }
            _output = null;
            _mixer?.RemoveAllMixerInputs();
        }
    }

    /// <summary>Plays a decoded buffer once, then ends (the mixer then drops it).</summary>
    private sealed class BufferSampleProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private int _position;

        public BufferSampleProvider(float[] samples, WaveFormat format)
        {
            _samples = samples;
            WaveFormat = format;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var available = Math.Min(count, _samples.Length - _position);
            if (available <= 0)
                return 0;
            Array.Copy(_samples, _position, buffer, offset, available);
            _position += available;
            return available;
        }
    }
}
