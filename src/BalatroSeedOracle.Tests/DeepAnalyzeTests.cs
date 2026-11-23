using BalatroSeedOracle.Analysis;
using Motely.Enums;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// Deep ante walks past the old 0–8 cap, seed-pile paste, and loop landmarks on EFPEEOX5
/// (Ghost / White), matching the JackOS wishlist verification seed.
/// </summary>
public sealed class DeepAnalyzeTests
{
    [Fact]
    public void Analyze_EFPEEOX5_maxAnte_16_walks_past_ante_8()
    {
        var result = MotelyUnitTestAnalyzer.Analyze(
            new MotelyUnitTestAnalysisConfig(
                "EFPEEOX5",
                MotelyDeck.Ghost,
                MotelyStake.White,
                MaxAnte: 16
            )
        );

        Assert.Null(result.Error);
        Assert.Contains(result.Antes, a => a.Ante == 0);
        Assert.Contains(result.Antes, a => a.Ante == 8);
        Assert.Contains(result.Antes, a => a.Ante == 10);
        Assert.Contains(result.Antes, a => a.Ante == 12);
        Assert.Contains(result.Antes, a => a.Ante == 16);
        Assert.Equal(17, result.Antes.Count); // 0..16 inclusive
    }

    [Fact]
    public void Loop_watch_finds_cola_negative_tag_and_oops_on_EFPEEOX5()
    {
        var result = MotelyUnitTestAnalyzer.Analyze(
            new MotelyUnitTestAnalysisConfig(
                "EFPEEOX5",
                MotelyDeck.Ghost,
                MotelyStake.White,
                MaxAnte: 16
            )
        );

        var hits = SeedLoopWatch.Scan(result);
        Assert.Contains(hits, h => h.Label == "Diet Cola" && h.Ante >= 8);
        Assert.Contains(hits, h => h.Label == "Negative Tag" && h.Ante is 10 or 12);
        Assert.Contains(hits, h => h.Label == "Oops! All 6s" && h.Ante == 10);
    }

    [Fact]
    public void Seed_paste_splits_comma_space_and_newline()
    {
        var seeds = SeedPaste.Parse("EFPEEOX5, ABCD1234\nZZZZZZZZ XXXX1111");
        Assert.Equal(["EFPEEOX5", "ABCD1234", "ZZZZZZZZ", "XXXX1111"], seeds);
    }

    [Fact]
    public void Seed_paste_reads_share_url_pile_and_meta()
    {
        var url =
            "https://optimuspi.github.io/Blueprint/?seeds=EFPEEOX5,AAAA1111"
            + "&deck=Ghost%20Deck&stake=White%20Stake&maxAnte=16&seedIndex=1";
        var (seeds, extras) = SeedPaste.ParseWithMeta(url);
        Assert.Equal(["EFPEEOX5", "AAAA1111"], seeds);
        Assert.Equal("Ghost", extras.Deck);
        Assert.Equal("White", extras.Stake);
        Assert.Equal(16, extras.MaxAnte);
        Assert.Equal(1, extras.SeedIndex);
    }

    [Fact]
    public void Default_max_ante_is_16()
    {
        Assert.Equal(16, MotelyUnitTestAnalyzer.DefaultMaxAnte);
    }
}
