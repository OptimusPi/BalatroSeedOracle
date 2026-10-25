using System.Text.Json;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using NAudio.Vorbis;
using NAudio.Wave;
using Xunit;

namespace BalatroSeedOracle.Tests;

public class MusicServiceTests
{
    private const long ExpectedStemFrames = 12_482_048; // 283.04 s at 44.1 kHz (Ogg granule of every stem)

    private static string AudioDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "BalatroSeedOracle", "Assets", "Audio");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Assets/Audio not found above " + AppContext.BaseDirectory);
    }

    private static Stream OpenStem(string name) => File.OpenRead(Path.Combine(AudioDir(), name));

    private static StemSource ArrayStem(float[] interleaved)
    {
        var provider = new ArrayProvider(interleaved);
        return new StemSource(provider, provider.Rewind, interleaved.Length / 2);
    }

    /// <summary>Stereo ramp: frame f has left = f, right = -f (so every frame is identifiable).</summary>
    private static float[] Ramp(int frames, float scale = 1f)
    {
        var data = new float[frames * 2];
        for (var f = 0; f < frames; f++)
        {
            data[f * 2] = f * scale;
            data[f * 2 + 1] = -f * scale;
        }
        return data;
    }

    // ------------------------------------------------------------------ real stems

    [Fact]
    public void Every_stem_is_a_44k_stereo_vorbis_file_of_the_same_length()
    {
        foreach (var stem in MusicService.Stems)
        {
            using var reader = new VorbisWaveReader(OpenStem(MusicService.FileNameOf(stem)));
            Assert.Equal(44100, reader.WaveFormat.SampleRate);
            Assert.Equal(2, reader.WaveFormat.Channels);
            Assert.Equal(ExpectedStemFrames, reader.Length / reader.WaveFormat.BlockAlign);
        }
    }

    [Fact]
    public void Stems_decode_and_the_bass_follows_the_shipped_envelope()
    {
        // envelopes.json: per-stem loudness at 30 Hz. Decode the first 10 s of Bass1 and
        // compare our RMS per 1/30 s window with it.
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AudioDir(), "envelopes.json")));
        Assert.Equal(30, doc.RootElement.GetProperty("rateHz").GetInt32());
        var envelope = doc.RootElement.GetProperty("stems").GetProperty("Bass1")
            .EnumerateArray().Select(e => e.GetDouble()).Take(300).ToArray();

        using var reader = new VorbisWaveReader(OpenStem("Bass1.ogg"));
        var window = new float[44100 / 30 * 2];
        var rms = new double[envelope.Length];
        for (var w = 0; w < rms.Length; w++)
        {
            var read = 0;
            while (read < window.Length)
            {
                var n = reader.Read(window, read, window.Length - read);
                Assert.True(n > 0);
                read += n;
            }
            rms[w] = StemLoopMixer.Rms(window);
        }

        Assert.True(rms.Take(8).All(r => r < 0.01), "Bass1 is silent for the first ~0.3 s");
        Assert.Contains(rms, r => r > 0.05);
        Assert.True(Correlation(rms, envelope) > 0.8, $"correlation {Correlation(rms, envelope):0.000}");
    }

    [Fact]
    public void Rewinding_a_vorbis_stem_reproduces_its_first_samples_exactly()
    {
        using var reader = new VorbisWaveReader(OpenStem("Chords1.ogg"));
        var first = new float[4096];
        Assert.Equal(first.Length, reader.Read(first, 0, first.Length));

        // Play into the tail, as the loop does, then rewind.
        reader.Position = reader.Length - 1000L * reader.WaveFormat.BlockAlign;
        var tail = new float[4000];
        Assert.Equal(2000, reader.Read(tail, 0, tail.Length)); // exactly 1000 frames left
        reader.Position = 0;

        var again = new float[first.Length];
        Assert.Equal(again.Length, reader.Read(again, 0, again.Length));
        Assert.Equal(first, again);
    }

    // ------------------------------------------------------------------ loop / mix math

    [Theory]
    [InlineData(new[] { 2 })]
    [InlineData(new[] { 666 })]
    [InlineData(new[] { 2000 })]
    [InlineData(new[] { 4096 })]
    [InlineData(new[] { 14, 1998, 2, 3528, 700 })]
    public void Loop_wraps_without_gaps_or_repeats_and_stems_stay_in_step(int[] blockSizes)
    {
        const int loop = 1000;
        var a = Ramp(loop);
        var b = Ramp(loop, 0.001f);
        var mixer = new StemLoopMixer(new[] { ArrayStem(a), ArrayStem(b) }, new[] { 1f, 0.5f }, () => 1f);
        Assert.Equal(loop, mixer.LoopFrames);

        const int totalFrames = 3500;
        var output = new List<float>();
        var i = 0;
        while (output.Count < totalFrames * 2)
        {
            var size = Math.Min(blockSizes[i++ % blockSizes.Length], totalFrames * 2 - output.Count);
            var block = new float[size];
            Assert.Equal(size, mixer.Read(block, 0, size));
            output.AddRange(block);
        }

        for (var f = 0; f < totalFrames; f++)
        {
            var src = f % loop;
            var expected = src + 0.5f * src * 0.001f;
            Assert.Equal(expected, output[f * 2], 3);
            Assert.Equal(-expected, output[f * 2 + 1], 3);
        }
        Assert.Equal(3, mixer.LoopCount);
        Assert.Equal(totalFrames % loop, mixer.PositionFrames);
        Assert.Equal(totalFrames, mixer.FramesPlayed);
    }

    [Fact]
    public void Loop_length_is_the_shortest_stem_and_longer_stems_restart_with_it()
    {
        var shortStem = Ramp(300);
        var longStem = Ramp(500);
        var mixer = new StemLoopMixer(new[] { ArrayStem(shortStem), ArrayStem(longStem) }, new[] { 1f, 0f }, () => 1f);
        Assert.Equal(300, mixer.LoopFrames);

        var buffer = new float[700 * 2];
        mixer.Read(buffer, 0, buffer.Length);
        mixer.SetGains(new[] { 0f, 1f }); // now hear the long stem: it must be in step at frame 100
        var next = new float[2];
        mixer.Read(next, 0, 2);
        Assert.Equal(100f, next[0]);
    }

    [Fact]
    public void Master_volume_scales_the_mix_and_mute_is_silent_and_holds_the_position()
    {
        var constant = Enumerable.Repeat(0.5f, 200).ToArray(); // 100 frames
        var volume = 0.5f;
        var mixer = new StemLoopMixer(new[] { ArrayStem(constant), ArrayStem(constant) }, new[] { 1f, 0.5f }, () => volume);

        var block = new float[40];
        mixer.Read(block, 0, block.Length);
        Assert.All(block, s => Assert.Equal((0.5f + 0.25f) * 0.5f, s, 5));
        Assert.Equal(20, mixer.PositionFrames);

        volume = 0f;
        Array.Fill(block, 9f);
        Assert.Equal(block.Length, mixer.Read(block, 0, block.Length));
        Assert.All(block, s => Assert.Equal(0f, s));
        Assert.Equal(20, mixer.PositionFrames); // muted: nothing decoded, nothing skipped
        Assert.Equal(0f, mixer.GetLevel(0));

        volume = 1f;
        mixer.Read(block, 0, block.Length);
        Assert.All(block, s => Assert.Equal(0.75f, s, 5));
        Assert.Equal(40, mixer.PositionFrames);
    }

    [Fact]
    public void Levels_are_the_rms_of_each_gained_stem_before_master_volume()
    {
        var constant = Enumerable.Repeat(0.5f, 2000).ToArray();
        var sine = new float[2000];
        for (var f = 0; f < 1000; f++)
            sine[f * 2] = sine[f * 2 + 1] = 0.8f * MathF.Sin(2 * MathF.PI * f / 100f);
        var mixer = new StemLoopMixer(
            new[] { ArrayStem(constant), ArrayStem(constant), ArrayStem(sine), ArrayStem(constant) },
            new[] { 1f, 0.5f, 1f, 0f },
            () => 0.25f
        );

        mixer.Read(new float[2000], 0, 2000); // exactly 10 sine periods
        Assert.Equal(0.5f, mixer.GetLevel(0), 4);
        Assert.Equal(0.25f, mixer.GetLevel(1), 4);
        Assert.Equal(0.8f / MathF.Sqrt(2), mixer.GetLevel(2), 3);
        Assert.Equal(0f, mixer.GetLevel(3));

        Assert.Equal(1f, StemLoopMixer.Rms(new[] { 1f, -1f, 1f, -1f }), 6);
        Assert.Equal(0f, StemLoopMixer.Rms(ReadOnlySpan<float>.Empty));
    }

    [Fact]
    public void Gains_follow_the_mixer_settings_and_profile_track_volumes()
    {
        var gains = MusicService.ComputeGains(MusicService.DefaultMainMenuMix(), new VisualizerSettings());
        Assert.Equal(new[] { 0.4f, 0f, 1f, 0f, 1f, 0f, 1f, 0f }, gains);

        var tracks = new VisualizerSettings { Bass1Volume = 0.5f };
        Assert.Equal(0.5f, MusicService.ComputeGains(MusicService.DefaultMainMenuMix(), tracks)[(int)MusicStem.Bass1]);

        var solo = new MixerSettings { Melody2 = new TrackSettings { Volume = 50, Solo = true } };
        Assert.Equal(new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.5f }, MusicService.ComputeGains(solo, null));
    }

    // ------------------------------------------------------------------ service, shared mixer

    [Fact]
    public async Task Music_plays_through_the_sound_effect_mixer_and_follows_the_volume()
    {
        var volume = 1f;
        var sfx = new SoundEffectService(_ => throw new FileNotFoundException(), () => volume, enableOutput: false);
        using var music = new MusicService(
            OpenStem,
            () => volume,
            () => MusicService.ComputeGains(MusicService.DefaultMainMenuMix(), null),
            sfx.AddInput,
            sfx.RemoveInput,
            () => sfx.IsOutputRunning
        );

        Assert.True(await music.StartAsync());
        Assert.True(await music.StartAsync()); // idempotent
        Assert.Same(music.Mixer, music.Mixer);

        // 3 s in, the bass, chords and drums are all sounding (Bass1 starts at 0.3 s).
        var buffer = new float[44100 * 2 * 3];
        Assert.Equal(buffer.Length, sfx.Mixer!.Read(buffer, 0, buffer.Length));
        Assert.True(StemLoopMixer.Rms(buffer.AsSpan(^44100..)) > 0.01f);
        Assert.True(music.GetGroupLevel(MusicGroup.Bass) > 0f);
        Assert.True(music.GetGroupLevel(MusicGroup.Chords) > 0f);
        Assert.Equal(0f, music.GetLevel(MusicStem.Bass2)); // muted in the main-menu mix

        volume = 0f;
        sfx.Mixer.Read(buffer, 0, 4410);
        Assert.All(buffer.Take(4410), s => Assert.Equal(0f, s));
        Assert.Equal(0f, music.GetGroupLevel(MusicGroup.Bass));

        music.Dispose();
        volume = 1f;
        sfx.Mixer.Read(buffer, 0, 4410);
        Assert.All(buffer.Take(4410), s => Assert.Equal(0f, s)); // detached from the shared mixer

        var disposed = new MusicService(OpenStem, () => 1f, () => new float[8], sfx.AddInput, sfx.RemoveInput, () => false);
        disposed.Dispose();
        Assert.False(await disposed.StartAsync());
    }

    [Fact]
    public async Task Missing_stems_leave_the_app_running_silently()
    {
        var sfx = new SoundEffectService(_ => throw new FileNotFoundException(), () => 1f, enableOutput: false);
        using var music = new MusicService(
            _ => throw new FileNotFoundException("gone"),
            () => 1f,
            () => new float[8],
            sfx.AddInput,
            sfx.RemoveInput,
            () => false
        );
        Assert.False(await music.StartAsync());
        Assert.False(music.IsStarted);
        Assert.Equal(0f, music.GetGroupLevel(MusicGroup.Melody));
    }

    // ------------------------------------------------------------------ visualizer mapping

    [Fact]
    public void Visualizer_maps_group_levels_onto_the_configured_effects()
    {
        var viz = new MusicVisualizer();
        for (var i = 0; i < 30; i++)
            viz.Update(g => g == MusicGroup.Bass ? 0.2f : 0f);
        Assert.InRange(viz.Level(MusicGroup.Bass), 0.99f, 1f);
        Assert.Equal(0f, viz.Level(MusicGroup.Melody));

        var settings = new VisualizerSettings
        {
            SpinSource = (int)MusicGroup.Bass,
            SpinIntensity = 100,
            ZoomThumpSource = (int)MusicGroup.Bass,
            ZoomThumpIntensity = 50,
            ColorSaturationSource = 0,
            AudioIntensity = 1,
        };
        var offsets = viz.Map(settings);
        Assert.Equal(MusicVisualizer.MaxSpinBoost, offsets.Spin, 2);
        Assert.Equal(MusicVisualizer.MaxZoomBoost / 2, offsets.Zoom, 1);
        Assert.Equal(0f, offsets.Saturation);

        settings.AudioIntensity = 0;
        Assert.Equal(default, viz.Map(settings));

        for (var i = 0; i < 60; i++)
            viz.Update(_ => 0f);
        Assert.True(viz.Level(MusicGroup.Bass) < 0.01f, "silence releases to zero");
    }

    private static double Correlation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        var mx = x.Average();
        var my = y.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (var i = 0; i < x.Count; i++)
        {
            sxy += (x[i] - mx) * (y[i] - my);
            sxx += (x[i] - mx) * (x[i] - mx);
            syy += (y[i] - my) * (y[i] - my);
        }
        return sxy / Math.Sqrt(sxx * syy);
    }

    private sealed class ArrayProvider : ISampleProvider
    {
        private readonly float[] _data;
        private int _pos;

        public ArrayProvider(float[] data) => _data = data;

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public void Rewind() => _pos = 0;

        public int Read(float[] buffer, int offset, int count)
        {
            var n = Math.Min(count, _data.Length - _pos);
            Array.Copy(_data, _pos, buffer, offset, n);
            _pos += n;
            return n;
        }
    }
}

public class MainMenuVolumePersistenceTests
{
    [Fact]
    public void Mute_survives_a_restart_and_unmutes_to_the_saved_volume()
    {
        var path = Path.Combine(TestDataRoot.NewDir(), "User", "userprofile.json");
        var service = new UserProfileService(path);

        // Mute at 60%: the profile keeps 0.6 and the muted flag.
        BalatroSeedOracle.ViewModels.BalatroMainMenuViewModel.StoreVolume(service.GetProfile(), 0, previousVolume: 60);
        service.FlushProfile();

        var reloaded = new UserProfileService(path).GetProfile();
        Assert.True(reloaded.IsMusicMuted);
        Assert.Equal(0.6f, reloaded.MusicVolume, 3);
        var (volume, previous) = BalatroSeedOracle.ViewModels.BalatroMainMenuViewModel.VolumeFromProfile(reloaded);
        Assert.Equal(0, volume);
        Assert.Equal(60, previous, 3);

        BalatroSeedOracle.ViewModels.BalatroMainMenuViewModel.StoreVolume(reloaded, 35, previous);
        Assert.False(reloaded.IsMusicMuted);
        Assert.Equal(0.35f, reloaded.MusicVolume, 3);
        var (unmuted, _) = BalatroSeedOracle.ViewModels.BalatroMainMenuViewModel.VolumeFromProfile(reloaded);
        Assert.Equal(35, unmuted, 3);
    }

    [Fact]
    public void A_profile_muted_at_zero_volume_unmutes_to_the_default()
    {
        var profile = new UserProfile { MusicVolume = 0, IsMusicMuted = true };
        Assert.Equal((0, 70), BalatroSeedOracle.ViewModels.BalatroMainMenuViewModel.VolumeFromProfile(profile));
    }
}

