using Xunit;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Tests;

public class SoundEffectServiceTests
{
    private static string SfxDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "BalatroSeedOracle", "Assets", "Audio", "SFX");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Assets/Audio/SFX not found above " + AppContext.BaseDirectory);
    }

    private static Stream OpenSfx(string name) => File.OpenRead(Path.Combine(SfxDir(), name));

    [Theory]
    [InlineData(SoundEffect.Button)]
    [InlineData(SoundEffect.Highlight)]
    [InlineData(SoundEffect.Paper)]
    public void Every_effect_decodes_to_stereo_mix_format_audio(SoundEffect effect)
    {
        using var stream = OpenSfx(SoundEffectService.FileNameOf(effect));
        var samples = SoundEffectService.Decode(stream);

        Assert.True(samples.Length > SoundEffectService.MixFormat.SampleRate / 20, "expected at least 50 ms of audio");
        Assert.Equal(0, samples.Length % 2);
        Assert.Contains(samples, s => Math.Abs(s) > 0.01f); // not silence
    }

    [Fact]
    public void Play_feeds_the_mixer_scaled_by_volume()
    {
        var volume = 0.5f;
        var service = new SoundEffectService(OpenSfx, () => volume, enableOutput: false);

        Assert.True(service.Play(SoundEffect.Paper));
        Assert.Equal(1, service.PlayCount);
        Assert.False(service.IsOutputRunning);

        var mixed = new float[SoundEffectService.MixFormat.SampleRate * 2];
        var read = service.Mixer!.Read(mixed, 0, mixed.Length);
        Assert.Equal(mixed.Length, read);

        using var stream = OpenSfx("paper1.ogg");
        var source = SoundEffectService.Decode(stream);
        var peakSource = source.Max(Math.Abs);
        var peakMixed = mixed.Max(Math.Abs);
        Assert.InRange(peakMixed, peakSource * 0.49f, peakSource * 0.51f);
    }

    [Fact]
    public void Muted_or_zero_volume_plays_nothing()
    {
        var service = new SoundEffectService(OpenSfx, () => 0f, enableOutput: false);
        Assert.False(service.Play(SoundEffect.Button));
        Assert.Equal(0, service.PlayCount);
    }

    [Fact]
    public void Missing_asset_is_a_logged_no_op()
    {
        var service = new SoundEffectService(_ => throw new FileNotFoundException("gone"), () => 1f, enableOutput: false);
        Assert.False(service.Play(SoundEffect.Highlight));
        service.PlayCardDrop();
        service.PlayFilterSwitch();
    }
}
