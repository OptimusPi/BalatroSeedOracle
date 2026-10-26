using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.FilterTabs;
using Motely.Filters;
using Xunit;
using Xunit.Abstractions;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// The designer must give back the filter it was handed: open a filter the way "Edit this
/// Filter" does (<see cref="FiltersModalViewModel.ReloadVisualFromSavedFile"/> into a real
/// <see cref="VisualBuilderTabViewModel"/>), build it back with
/// <see cref="FiltersModalViewModel.BuildConfigFromCurrentState"/> (what Validate and Save use),
/// and the config is the same, by the engine's own reading of both.
/// </summary>
[Collection(HeadlessAvaloniaCollection.Name)]
public class BuilderRoundTripTests(HeadlessAvaloniaFixture avalonia, ITestOutputHelper output)
{
    private static readonly string RepoRoot = FindRepoRoot();
    private const string WaterbearPath = @"D:\BalatroSeedOracle\YAMLFilters\waterbear.yaml";
    private static readonly string[] WaterbearSeeds = ["DL241111", "8N5A1111", "MW1G1111"];

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "MotelyJAML", "JamlFilters")))
                return dir.FullName;
        throw new DirectoryNotFoundException("src/MotelyJAML/JamlFilters not found above the test output.");
    }

    /// <summary>Every engine filter: the shipped JamlFilters and the engine tests' JamlFilters.</summary>
    public static TheoryData<string> CorpusFilters()
    {
        var data = new TheoryData<string>();
        foreach (var file in CorpusFiles())
            data.Add(file);
        return data;
    }

    private static List<string> CorpusFiles()
    {
        var data = new List<string>();
        foreach (var root in new[] { Path.Combine("src", "MotelyJAML", "JamlFilters"), Path.Combine("src", "MotelyJAML", "Motely.Tests", "JamlFilters") })
        {
            var dir = Path.Combine(RepoRoot, root);
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                         .Where(f => Services.FilterFiles.IsFilterFile(f))
                         .OrderBy(f => f, StringComparer.Ordinal))
                data.Add(Path.GetRelativePath(RepoRoot, file));
        }
        return data;
    }

    // ── the designer, headless ──

    private sealed record Designer(FiltersModalViewModel Vm, VisualBuilderTabViewModel Visual);

    /// <summary>Open the file as "Edit this Filter" does (BalatroMainMenu.ShowFiltersModalDirectAsync).</summary>
    private static Designer Open(string path)
    {
        var vm = new FiltersModalViewModel();
        var visual = new VisualBuilderTabViewModel(vm);
        vm.VisualBuilderTab = visual;
        vm.CurrentFilterPath = path;
        // Completes synchronously: the load is file read + in-memory population.
        vm.ReloadVisualFromSavedFile().GetAwaiter().GetResult();
        return new Designer(vm, visual);
    }

    private T OnUi<T>(Func<T> action) => avalonia.Run(action);

    // ── equivalence, through the engine ──

    /// <summary>
    /// The config as the engine's JSON of its JAML text, keys sorted at every level. Name, author
    /// and description are compared only when the original wrote them: the designer names an
    /// unnamed filter and stamps an author on one without, by design. An original without an id
    /// gets one on save, so its id is left out too.
    /// </summary>
    private static string Canonical(JamlConfig config, JamlConfig original)
    {
        var json = JsonNode.Parse(JamlConfigLoader.ToJson(JamlConfigText.ToJaml(config)))!.AsObject();
        if (original.Name is null)
            json.Remove("name");
        if (original.Author is null)
            json.Remove("author");
        if (original.Description is null)
            json.Remove("description");
        if (string.IsNullOrEmpty(original.Id))
            json.Remove("id");
        return Sorted(json)!.ToJsonString();
    }

    private static JsonNode? Sorted(JsonNode? node) =>
        node switch
        {
            JsonObject obj => new JsonObject(
                obj.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => KeyValuePair.Create(p.Key, Sorted(p.Value)))
            ),
            JsonArray array => new JsonArray(array.Select(Sorted).ToArray()),
            null => null,
            _ => JsonNode.Parse(node.ToJsonString()),
        };

    private static void AssertSameFilter(JamlConfig original, JamlConfig built, string because)
    {
        Assert.Equal(original.Must.Count, built.Must.Count);
        Assert.Equal(original.Should.Count, built.Should.Count);
        Assert.Equal(original.MustNot.Count, built.MustNot.Count);
        var expected = Canonical(original, original);
        var actual = Canonical(built, original);
        Assert.True(expected == actual, $"{because}\nexpected: {expected}\nactual:   {actual}");
    }

    // ── 1. every engine filter round-trips ──

    [Theory]
    [MemberData(nameof(CorpusFilters))]
    public void EveryEngineFilter_RoundTripsThroughTheBuilder(string relativePath)
    {
        var path = Path.Combine(RepoRoot, relativePath);
        if (!JamlConfigLoader.TryLoad(File.ReadAllText(path), out var original, out _))
            return; // the engine itself refuses this file; nothing for the designer to keep

        var (built, rebuilt, saved) = OnUi(() =>
        {
            var designer = Open(path);
            var first = designer.Vm.BuildConfigFromCurrentState();
            // Entering BUILD FILTER again rebuilds the zones from state.
            designer.Visual.LoadFromParentCollections();
            var second = designer.Vm.BuildConfigFromCurrentState();
            return (first, second, JamlConfigText.ToJaml(first));
        });

        AssertSameFilter(original, built, $"{relativePath}: builder lost or changed something");
        AssertSameFilter(original, rebuilt, $"{relativePath}: re-entering the builder lost something");
        Assert.True(JamlConfigLoader.TryLoad(saved, out var reloaded, out var error), $"{relativePath}: saved text doesn't load: {error}");
        AssertSameFilter(original, reloaded, $"{relativePath}: the saved file reads back differently");
    }

    [Fact]
    public void Corpus_IsTheWholeEngineSet()
    {
        var files = CorpusFiles();
        var refused = files.Where(f => !JamlConfigLoader.TryLoad(File.ReadAllText(Path.Combine(RepoRoot, f)), out _, out _)).ToList();
        var loadable = files.Count - refused.Count;
        output.WriteLine($"{files.Count} corpus filters, {loadable} load, {refused.Count} refused by the engine:");
        foreach (var file in refused)
            output.WriteLine("  " + file);
        Assert.True(files.Count >= 370, $"only {files.Count} corpus files found");
        Assert.True(loadable >= 300, $"only {loadable} of {files.Count} corpus files load");
    }

    // ── 2. the waterbear ──

    private static JamlConfig LoadWaterbear()
    {
        Assert.True(File.Exists(WaterbearPath), $"{WaterbearPath} is missing");
        Assert.True(JamlConfigLoader.TryLoad(File.ReadAllText(WaterbearPath), out var config, out var error), error);
        return config;
    }

    private static JsonObject Json(JamlConfig config) =>
        JsonNode.Parse(JamlConfigLoader.ToJson(JamlConfigText.ToJaml(config)))!.AsObject();

    [Fact]
    public void Waterbear_KeepsAntesSourcesAndEveryScore()
    {
        var original = LoadWaterbear();
        var built = OnUi(() => Open(WaterbearPath).Vm.BuildConfigFromCurrentState());

        AssertSameFilter(original, built, "waterbear");

        var json = Json(built);
        var perkeo = json["must"]![0]!.AsObject();
        Assert.Equal("[0]", perkeo["antes"]!.ToJsonString());
        Assert.Equal("[0,1,2]", perkeo["sources"]!["boosterPacks"]!.ToJsonString());
        Assert.Equal("[1]", json["must"]![1]!["antes"]!.ToJsonString());

        int[] scores = [70, 60, 50, 50, 40, 40, 40, 35, 35, 30, 30, 20];
        Assert.Equal(scores, built.Should.Select(c => c.Score).ToArray());
        Assert.Equal(1, built.Should[^1].Max);
        Assert.Equal("Negative", built.Should[6].GetEditionString());
    }

    // ── 3. an edit in the builder lands, the rest of the clause stays ──

    [Fact]
    public void Waterbear_EditionEditedInBuilder_IsSaved_AndTheClauseKeepsTheRest()
    {
        var original = LoadWaterbear();
        var built = OnUi(() =>
        {
            var designer = Open(WaterbearPath);
            var perkeo = designer.Visual.SelectedMust[0];
            Assert.Equal("Perkeo", perkeo.Name);
            perkeo.Edition = "Foil";
            var blueprint = designer.Visual.SelectedShould.Single(i => i.Name == "Blueprint");
            blueprint.Edition = "Polychrome";
            return designer.Vm.BuildConfigFromCurrentState();
        });

        var json = Json(built);
        var perkeoJson = json["must"]![0]!.AsObject();
        Assert.Equal("Foil", built.Must[0].GetEditionString());
        Assert.Equal("[0]", perkeoJson["antes"]!.ToJsonString());
        Assert.Equal("[0,1,2]", perkeoJson["sources"]!["boosterPacks"]!.ToJsonString());

        Assert.Equal("Polychrome", built.Should[1].GetEditionString());
        Assert.Equal(60, built.Should[1].Score);

        // Only those two clauses changed.
        var expected = Json(original);
        Assert.Equal(expected["must"]![1]!.ToJsonString(), json["must"]![1]!.ToJsonString());
        for (var i = 0; i < original.Should.Count; i++)
            if (i != 1)
                Assert.Equal(expected["should"]![i]!.ToJsonString(), json["should"]![i]!.ToJsonString());

        // And the loaded config itself is untouched (the builder copies before it edits).
        Assert.Null(original.Must[0].GetEditionString());
    }

    [Fact]
    public void Waterbear_EditionClearedInBuilder_IsCleared_AndTheClauseKeepsTheRest()
    {
        // The builder used to only add: clearing the Negative off `joker: Any` saved it Negative.
        var original = LoadWaterbear();
        var index = original.Should.FindIndex(c => c.GetEditionString() == "Negative");
        Assert.True(index >= 0, "waterbear has a Negative should-clause");

        var built = OnUi(() =>
        {
            var designer = Open(WaterbearPath);
            designer.Visual.SelectedShould[index].Edition = null;
            return designer.Vm.BuildConfigFromCurrentState();
        });

        Assert.Null(built.Should[index].GetEditionString());
        Assert.Equal(original.Should[index].Score, built.Should[index].Score);
        Assert.Equal("Negative", original.Should[index].GetEditionString()); // the loaded config is untouched
        var expected = Json(original);
        var json = Json(built);
        for (var i = 0; i < original.Should.Count; i++)
            if (i != index)
                Assert.Equal(expected["should"]![i]!.ToJsonString(), json["should"]![i]!.ToJsonString());
    }

    [Fact]
    public void Waterbear_ItemMovedToShould_KeepsItsClause()
    {
        var built = OnUi(() =>
        {
            var designer = Open(WaterbearPath);
            var hieroglyph = designer.Visual.SelectedMust[1];
            designer.Visual.SelectedMust.Remove(hieroglyph);
            designer.Visual.SelectedShould.Add(hieroglyph);
            return designer.Vm.BuildConfigFromCurrentState();
        });

        Assert.Single(built.Must);
        Assert.Equal("[1]", Json(built)["should"]!.AsArray().Last()!["antes"]!.ToJsonString());
    }

    [Fact]
    public void BannedItemsTrayInState_StaysMustNot_WhenTheBuilderIsReentered()
    {
        var built = OnUi(() =>
        {
            var vm = new FiltersModalViewModel();
            var visual = new VisualBuilderTabViewModel(vm);
            vm.VisualBuilderTab = visual;
            // What SyncOperatorToParent writes for a Banned Items tray dropped in MUST.
            vm.ItemConfigs["item_1"] = new ItemConfig
            {
                ItemKey = "item_1",
                ItemType = "Operator",
                ItemName = "BannedItems",
                OperatorType = "BannedItems",
                Children = [new ItemConfig { ItemType = "Joker", ItemName = "Blueprint" }],
            };
            vm.SelectedMust.Add("item_1");
            visual.LoadFromParentCollections();
            return vm.BuildConfigFromCurrentState();
        });

        Assert.Empty(built.Must);
        Assert.Single(built.MustNot);
        Assert.Equal("Blueprint", built.MustNot[0].GetValueName());
    }

    // ── 4. same search results ──

    private static Dictionary<string, (int Score, string Tallies)> Run(JamlConfig config, string[] seeds)
    {
        var rows = new ConcurrentDictionary<string, (int, string)>();
        using var search = MotelySearchBuilder
            .CreateSettings(config, int.MinValue) // --cutoff off: every seed's raw score
            .WithSeedList(seeds)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithScoredResultCallback(r => rows[r.Seed] = (r.Score, string.Join(",", r.Tallies)))
            .Start();
        search.AwaitCompletion();
        return new Dictionary<string, (int, string)>(rows);
    }

    [Fact]
    public void Waterbear_RoundTripped_ScoresTheSameSeedsTheSame()
    {
        var original = LoadWaterbear();
        var built = OnUi(() => Open(WaterbearPath).Vm.BuildConfigFromCurrentState());

        var before = Run(original, WaterbearSeeds);
        var after = Run(built, WaterbearSeeds);

        Assert.Equal(135, before["DL241111"].Score);
        Assert.Equal(70, before["8N5A1111"].Score);
        Assert.Equal(170, before["MW1G1111"].Score);
        Assert.Equal(before.OrderBy(kv => kv.Key), after.OrderBy(kv => kv.Key));
    }

    /// <summary>Seeds every corpus filter is replayed over, original and round-tripped.</summary>
    private static readonly string[] CorpusSeeds = ["DL241111", "8N5A1111", "MW1G1111", "ALEEB", "11111111", "ZZZZZZZZ"];

    [Theory]
    [MemberData(nameof(CorpusFilters))]
    public void EveryEngineFilter_RoundTripped_ScoresTheSame(string relativePath)
    {
        var path = Path.Combine(RepoRoot, relativePath);
        if (!JamlConfigLoader.TryLoad(File.ReadAllText(path), out var original, out _))
            return;

        Dictionary<string, (int, string)> before;
        try
        {
            before = Run(original, CorpusSeeds);
        }
        catch (Exception ex)
        {
            output.WriteLine($"not searched (engine refuses as written): {ex.Message}");
            return; // the engine won't search this filter as written; not the designer's to keep
        }

        var built = OnUi(() => Open(path).Vm.BuildConfigFromCurrentState());
        Assert.Equal(before.OrderBy(kv => kv.Key), Run(built, CorpusSeeds).OrderBy(kv => kv.Key));
    }
}
