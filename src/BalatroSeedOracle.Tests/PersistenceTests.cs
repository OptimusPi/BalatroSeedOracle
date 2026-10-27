using Xunit;
using System.Text.Json.Nodes;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Tests;

public class AppDataPathsTests
{
    [Fact]
    public void Tests_never_resolve_to_the_real_app_data_directory()
    {
        var real = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BalatroSeedOracle"
        );
        Assert.Equal(TestDataRoot.Root, AppDataPaths.Root);
        Assert.NotEqual(real, AppDataPaths.Root, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith(TestDataRoot.Root, AppDataPaths.FavoritesFile);
        Assert.Equal(Path.Combine(TestDataRoot.Root, "User", "userprofile.json"), AppDataPaths.UserProfileFile);
    }
}

public class UserProfileServiceTests
{
    // Same shape as a real userprofile.json, including a key this build does not model.
    private const string ExistingProfile = """
        {
          "authorName": "pifreak",
          "savedSearchWidgets": [],
          "animationEnabled": true,
          "musicVolume": 0.4,
          "isMusicMuted": true,
          "featureToggles": { "showMusicMixer": false, "showFertilizer": true },
          "hostApiSettings": { "host": "localhost", "port": 3141, "threadCount": 16 }
        }
        """;

    private static string WriteProfile(string json)
    {
        var path = Path.Combine(TestDataRoot.NewDir(), "User", "userprofile.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Constructor_loads_existing_profile_before_anyone_reads_it()
    {
        var service = new UserProfileService(WriteProfile(ExistingProfile));

        Assert.Equal("pifreak", service.GetAuthorName());
        Assert.Equal(0.4f, service.GetProfile().MusicVolume);
        Assert.True(service.GetProfile().IsMusicMuted);
        Assert.Equal(16, service.GetProfile().HostApiSettings.ThreadCount);
    }

    [Fact]
    public async Task LoadUserProfileAsync_does_not_swap_the_live_profile()
    {
        var service = new UserProfileService(WriteProfile(ExistingProfile));
        var before = service.GetProfile();
        await service.LoadUserProfileAsync();
        Assert.Same(before, service.GetProfile());
    }

    [Fact]
    public void Author_round_trips_and_unknown_keys_survive()
    {
        var path = WriteProfile(ExistingProfile);
        var service = new UserProfileService(path);
        service.SetAuthorName("Round Trip");

        var reloaded = new UserProfileService(path);
        Assert.Equal("Round Trip", reloaded.GetAuthorName());
        Assert.Equal(0.4f, reloaded.GetProfile().MusicVolume);

        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("Round Trip", (string?)json["authorName"]);
        Assert.True((bool)json["featureToggles"]!["showFertilizer"]!);
        Assert.False(json.ContainsKey("AuthorName")); // stays camelCase, no duplicate keys
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void Missing_file_uses_defaults_and_creates_the_file_on_save()
    {
        var path = Path.Combine(TestDataRoot.NewDir(), "User", "userprofile.json");
        var service = new UserProfileService(path);
        Assert.Equal("Jimbo", service.GetAuthorName());
        Assert.False(File.Exists(path));

        service.GetProfile().MusicVolume = 0.25f;
        service.SaveProfile();

        var reloaded = new UserProfileService(path);
        Assert.Equal(0.25f, reloaded.GetProfile().MusicVolume);
    }

    [Fact]
    public void Cleared_search_state_is_removed_from_the_file()
    {
        var path = WriteProfile(ExistingProfile);
        var service = new UserProfileService(path);
        service.SaveSearchState(new SearchResumeState { ConfigPath = "x.yaml", LastCompletedBatch = 7 });
        Assert.Equal(7ul, new UserProfileService(path).GetSearchState()!.LastCompletedBatch);

        service.ClearSearchState();
        Assert.Null(new UserProfileService(path).GetSearchState());
        Assert.False(JsonNode.Parse(File.ReadAllText(path))!.AsObject().ContainsKey("lastSearchState"));
    }

    [Fact]
    public void Unreadable_file_is_backed_up_before_it_is_replaced()
    {
        var path = WriteProfile("{ this is not json");
        var service = new UserProfileService(path);
        Assert.Equal("Jimbo", service.GetAuthorName());

        service.SetAuthorName("After Corruption");

        var backups = Directory.GetFiles(Path.GetDirectoryName(path)!, "userprofile.json.unreadable-*.bak");
        Assert.Single(backups);
        Assert.Equal("{ this is not json", File.ReadAllText(backups[0]));
        Assert.Equal("After Corruption", new UserProfileService(path).GetAuthorName());
    }
}

public class FavoritesServiceTests
{
    // Same shape as a real favorites.json (PascalCase, legacy keys, CommonSets, computed HasZoneInfo).
    private const string ExistingFavorites = """
        {
          "FavoriteItems": [ "Jokers:Perkeo#10", "Tags:NegativeTag#11" ],
          "CommonSets": [
            { "Name": "Photo Chad", "Description": "Picture perfect combo", "Items": [ "photograph", "hangingchad" ],
              "Tags": [ "#FaceCards" ], "MustItems": [], "ShouldItems": [], "MustNotItems": [], "HasZoneInfo": false },
            { "Name": "BaronMime", "Description": "Kings", "Items": [ "baron", "mime" ],
              "Tags": [], "MustItems": [], "ShouldItems": [], "MustNotItems": [], "HasZoneInfo": false }
          ]
        }
        """;

    private static string WriteFavorites(string json)
    {
        var path = Path.Combine(TestDataRoot.NewDir(), "favorites.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Loads_existing_items_including_legacy_keys()
    {
        var service = new FavoritesService(WriteFavorites(ExistingFavorites));
        Assert.True(service.IsFavorite("Jokers:Perkeo#10"));
        Assert.True(service.IsFavorite("tags:negativetag#11")); // case-insensitive lookup
        Assert.Equal(2, service.FavoriteItems.Count);
    }

    [Fact]
    public void Add_and_remove_round_trip_without_touching_common_sets()
    {
        var path = WriteFavorites(ExistingFavorites);
        var service = new FavoritesService(path);
        service.Add("Joker_Blueprint");
        service.Remove("Tags:NegativeTag#11");

        var reloaded = new FavoritesService(path);
        Assert.True(reloaded.IsFavorite("Joker_Blueprint"));
        Assert.True(reloaded.IsFavorite("Jokers:Perkeo#10"));
        Assert.False(reloaded.IsFavorite("Tags:NegativeTag#11"));
        Assert.Equal(new[] { "Jokers:Perkeo#10", "Joker_Blueprint" }, reloaded.FavoriteItems);

        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.True(json.ContainsKey("FavoriteItems"));
        Assert.False(json.ContainsKey("favoriteItems"));
        var sets = json["CommonSets"]!.AsArray();
        Assert.Equal(2, sets.Count);
        Assert.Equal("Photo Chad", (string?)sets[0]!["Name"]);
        Assert.Equal("hangingchad", (string?)sets[0]!["Items"]![1]);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void Duplicate_add_and_missing_remove_do_not_rewrite_the_file()
    {
        var path = WriteFavorites(ExistingFavorites);
        var service = new FavoritesService(path);
        var before = File.GetLastWriteTimeUtc(path);
        var bytes = File.ReadAllBytes(path);

        service.Add("JOKERS:PERKEO#10");
        service.Remove("not-a-favorite");

        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Equal(before, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public void New_file_uses_the_favorites_data_layout()
    {
        var path = Path.Combine(TestDataRoot.NewDir(), "favorites.json");
        var service = new FavoritesService(path);
        Assert.Empty(service.FavoriteItems);

        service.Add("Joker_Perkeo");

        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("Joker_Perkeo", (string?)json["FavoriteItems"]![0]);
        Assert.Empty(json["CommonSets"]!.AsArray());
    }

    [Fact]
    public void Unreadable_file_is_backed_up_before_it_is_replaced()
    {
        var path = WriteFavorites("[ \"not an object\" ]");
        var service = new FavoritesService(path);
        Assert.Empty(service.FavoriteItems);

        service.Add("Joker_Perkeo");

        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "favorites.json.unreadable-*.bak"));
        Assert.True(new FavoritesService(path).IsFavorite("Joker_Perkeo"));
    }
}

public class JsonFileStoreTests
{
    [Fact]
    public void Merge_replaces_known_keys_case_insensitively_and_keeps_the_rest()
    {
        var original = JsonNode.Parse("""{ "FavoriteItems": [1], "Other": "keep" }""")!.AsObject();
        var merged = JsonFileStore.Merge(original, new JsonObject { ["favoriteItems"] = new JsonArray(2, 3) });

        Assert.Equal(2, merged.Count);
        Assert.Equal(2, merged["FavoriteItems"]!.AsArray().Count);
        Assert.Equal("keep", (string?)merged["Other"]);
        Assert.Single(original["FavoriteItems"]!.AsArray()); // original untouched
    }

    [Fact]
    public void Atomic_write_replaces_existing_file_and_leaves_no_temp_files()
    {
        var dir = TestDataRoot.NewDir();
        var path = Path.Combine(dir, "nested", "file.json");
        JsonFileStore.WriteAllTextAtomic(path, "one");
        JsonFileStore.WriteAllTextAtomic(path, "two");

        Assert.Equal("two", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!));
    }
}

public class SeedLakeSinkTests
{
    [Fact]
    public void Seeds_are_deduplicated_across_threads_and_reopens()
    {
        var id = "bso-test-" + Guid.NewGuid().ToString("N");
        var path = SeedLakeSink.SeedFilePath(null, id);
        try
        {
            using (var sink = new SeedLakeSink(null, id))
            {
                Parallel.For(0, 4000, i => sink.OnSeed($"SEED{i % 500:D4}"));
                Assert.Equal(500, sink.Count);
            }

            using (var sink = new SeedLakeSink(null, id))
            {
                Assert.Equal(500, sink.Count);
                sink.OnSeed("SEED0001"); // already in the lake
                sink.OnSeed("NEWSEED1");
                sink.OnSeed("  ");
            }

            var lines = File.ReadAllLines(path);
            Assert.Equal(501, lines.Length);
            Assert.Equal(lines.Length, lines.Distinct().Count());
            Assert.All(lines, l => Assert.Matches("^[A-Z0-9]+$", l)); // no interleaved writes
            Assert.Equal("NEWSEED1", lines[^1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Scored_results_persist_and_parse_score_and_tallies()
    {
        var id = "bso-scored-" + Guid.NewGuid().ToString("N");
        var path = SeedLakeSink.SeedFilePath(null, id);
        try
        {
            using (var sink = new SeedLakeSink(null, id))
            {
                var r1 = new Motely.Filters.MotelyScoredSeedResult { Seed = "SEED1111", Score = 42, Tallies = [10, 20, 12] };
                var r2 = new Motely.Filters.MotelyScoredSeedResult { Seed = "SEED1111", Score = 99, Tallies = [99] };
                sink.OnScored(in r1);
                sink.OnScored(in r2); // duplicate seed ignored
                sink.OnSeed("SEED2222");
            }

            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);

            Assert.True(SeedLakeSink.TryParseLine(lines[0], out var seed1, out var score1, out var tallies1));
            Assert.Equal("SEED1111", seed1);
            Assert.Equal(42, score1);
            Assert.Equal(new[] { 10, 20, 12 }, tallies1);

            Assert.True(SeedLakeSink.TryParseLine(lines[1], out var seed2, out var score2, out var tallies2));
            Assert.Equal("SEED2222", seed2);
            Assert.Equal(0, score2);
            Assert.Null(tallies2);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Search_id_is_sanitized_into_the_search_results_folder()
    {
        var path = SeedLakeSink.SeedFilePath(null, "a/b:c*d");
        Assert.Equal(Path.Combine("SearchResults", "abcd.csv"), path);
        Assert.Equal(Path.Combine("SearchResults", "search.csv"), SeedLakeSink.SeedFilePath(null, "///"));
    }
}

public class DebugLoggerFileTests
{
    [Fact]
    public void Lines_are_written_to_a_log_file_in_the_data_directory()
    {
        var marker = "file-sink-" + Guid.NewGuid().ToString("N");
        DebugLogger.LogError("DebugLoggerFileTests", marker);
        DebugLogger.Flush();

        var path = DebugLogger.LogFilePath;
        Assert.NotNull(path);
        Assert.StartsWith(Path.Combine(TestDataRoot.Root, "Logs"), path);
        Assert.Matches(@"bso-\d{8}\.log$", path);

        using var stream = new FileStream(path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        Assert.Contains($"[ERROR] DebugLoggerFileTests: {marker}", text);
        Assert.Contains(marker, string.Join("\n", DebugLogger.RecentEntries));
    }
}
