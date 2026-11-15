using System.Collections.Concurrent;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Motely.Filters;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// The score floor a desktop search runs with (<see cref="SearchModalViewModel.EngineCutoff"/>, what
/// Start Search hands the engine): a filter's own <c>cutoff:</c> applies until the user sets a
/// Min Score, and a Min Score the user set wins over it.
/// </summary>
[Collection(HeadlessAvaloniaCollection.Name)]
public class SearchCutoffTests(HeadlessAvaloniaFixture avalonia)
{
    // The waterbear scores these 135, 70 and 170 (BuilderRoundTripTests pins those numbers).
    private static readonly string[] Seeds = ["DL241111", "8N5A1111", "MW1G1111"];

    private static JamlConfig Waterbear(int? cutoff)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            foreach (var folder in new[] { "YAMLFilters", "YamlFilters" })
            {
                var path = Path.Combine(dir.FullName, folder, "waterbear.yaml");
                if (!File.Exists(path))
                    continue;
                Assert.True(JamlConfigLoader.TryLoad(File.ReadAllText(path), out var config, out var error), error);
                Assert.Null(config.Cutoff); // the file has none; each test states its own
                config.Cutoff = cutoff;
                return config;
            }
        }
        throw new FileNotFoundException("YamlFilters/waterbear.yaml not found above the test output.");
    }

    /// <summary>The floor a search modal would start with, Min Score untouched or set to <paramref name="minScore"/>.</summary>
    private int? CutoffOfASearchModal(int? minScore = null) =>
        avalonia.Run(() =>
        {
            LifecycleAndRegressionTests.EnsureAppResourcesLoaded(); // the view model builds its tab views
            var profile = new UserProfileService();
            var vm = new SearchModalViewModel(profile, () => new AnalyzeModalViewModel(SpriteService.Instance, profile));
            if (minScore is { } set)
                vm.MinScore = set;
            return vm.EngineCutoff;
        });

    /// <summary>The seeds the engine reports, searched as Start Search does: <c>CreateSettings(config, cutoff)</c>.</summary>
    private static string[] Reported(JamlConfig config, int? engineCutoff)
    {
        var rows = new ConcurrentDictionary<string, int>();
        using var search = MotelySearchBuilder
            .CreateSettings(config, engineCutoff)
            .WithSeedList(Seeds)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithScoredResultCallback(r => rows[r.Seed] = r.Score)
            .Start();
        search.AwaitCompletion();
        return rows.Keys.Order(StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void MinScoreUntouched_TheFiltersOwnCutoffApplies()
    {
        // cutoff: 100 sits between 70 and 135: the 70 seed must go.
        var reported = Reported(Waterbear(cutoff: 100), CutoffOfASearchModal());
        Assert.Equal(["DL241111", "MW1G1111"], reported);
    }

    [Fact]
    public void MinScoreUntouched_AFilterWithoutACutoff_ReportsEverySeedItMatches()
    {
        var reported = Reported(Waterbear(cutoff: null), CutoffOfASearchModal());
        Assert.Equal(["8N5A1111", "DL241111", "MW1G1111"], reported);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void MinScoreSetByTheUser_WinsOverTheFiltersCutoff(int minScore)
    {
        Assert.Equal(minScore, CutoffOfASearchModal(minScore));
        var reported = Reported(Waterbear(cutoff: 100), CutoffOfASearchModal(minScore));
        Assert.Equal(["8N5A1111", "DL241111", "MW1G1111"], reported);
    }

    [Fact]
    public void MinScoreSetBackToAuto_IsUntouchedAgain()
    {
        // The spinner's "Auto" is -1 (SpinnerControl / SpinnerDisplayConverter).
        Assert.Null(CutoffOfASearchModal(minScore: -1));
    }
}
