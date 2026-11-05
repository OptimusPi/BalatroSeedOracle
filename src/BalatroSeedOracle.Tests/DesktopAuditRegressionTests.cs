using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views.SearchModalTabs;
using Motely;
using Motely.Enums;
using Motely.Filters;
using Motely.SeedProviders;
using Xunit;

namespace BalatroSeedOracle.Tests;

[Collection(HeadlessAvaloniaCollection.Name)]
public sealed class DesktopAuditRegressionTests(HeadlessAvaloniaFixture avalonia)
{
    private void OnUi(Action action) => avalonia.Run(() => { action(); return 0; });

    private static void EnsureAppResourcesLoaded()
    {
        var app = Application.Current;
        if (app == null || app.Resources.ContainsKey("BalatroFont"))
            return;

        var templateApp = new BalatroSeedOracle.App();
        templateApp.Initialize();
        var resources = templateApp.Resources.ToList();
        templateApp.Resources.Clear();
        foreach (var kv in resources)
        {
            app.Resources[kv.Key] = kv.Value;
        }
        var styles = templateApp.Styles.ToList();
        templateApp.Styles.Clear();
        foreach (var style in styles)
        {
            app.Styles.Add(style);
        }
    }

    [Fact]
    public void SearchModalViewModel_SelectedDbListIndexChanged_UpdatesSelectedLakePartition_AndShowsThreadSlider()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            string tempDir = Path.Combine(TestDataRoot.NewDir(), "lake");
            Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "Ace_best.txt"), ["ACE00001", "ACE00002"]);
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "King_best.txt"), ["KING0001", "KING0002", "KING0003"]);

            var lakeService = new DungmotLakeService(tempDir);
            var profileService = new UserProfileService();
            var searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService),
                dungmotLakeService: lakeService
            );

            Assert.Equal(2, searchVm.AvailableLakePartitions.Count);

            searchVm.SelectedSearchMode = SearchMode.DbList;
            Assert.True(searchVm.IsThreadsVisible, "DbList mode must keep thread controls visible for multi-threaded SIMD verification.");

            // Switch selection to index 1 and verify SelectedLakePartition follows immediately.
            searchVm.SelectedDbListIndex = 1;
            Assert.Same(searchVm.AvailableLakePartitions[1], searchVm.SelectedLakePartition);

            searchVm.LoadedConfig = new JamlConfig
            {
                Name = "PartitionSwitchTest",
                Deck = MotelyDeck.Red,
                Stake = MotelyStake.White,
            };

            searchVm.StartSearchCommand.Execute(null);
            Assert.Contains(
                searchVm.ConsoleOutput,
                m => m.Text.Contains(searchVm.AvailableLakePartitions[1].Name, StringComparison.Ordinal)
            );

            if (searchVm.IsSearching)
            {
                searchVm.StopSearchCommand.Execute(null);
            }
        });
    }

    [Fact]
    public void SearchModalViewModel_WordListMode_PopulatesAesthetics_AndDoesNotFallThroughToSequential()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            var profileService = new UserProfileService();
            var searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService)
            );

            Assert.NotEmpty(searchVm.AvailableWordLists);
            int palindromeIdx = searchVm.AvailableWordLists.IndexOf(nameof(MotelyAesthetic.Palindrome));
            Assert.True(palindromeIdx >= 0, "Built-in MotelyAesthetic dictionaries must populate AvailableWordLists.");

            searchVm.SelectedSearchMode = SearchMode.WordList;
            searchVm.SelectedWordListIndex = palindromeIdx;
            searchVm.LoadedConfig = new JamlConfig
            {
                Name = "WordListCheck",
                Deck = MotelyDeck.Red,
                Stake = MotelyStake.White,
            };

            searchVm.StartSearchCommand.Execute(null);

            Assert.Contains(
                searchVm.ConsoleOutput,
                m => m.Text.Contains("Searching word/pattern dictionary: Palindrome", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                searchVm.ConsoleOutput,
                m => m.Text.Contains("Starting search from the beginning (batch 0)", StringComparison.Ordinal)
            );

            if (searchVm.IsSearching)
            {
                searchVm.StopSearchCommand.Execute(null);
            }
        });
    }

    [Fact]
    public void SearchModalViewModel_GpuActiveState_StaysFalse_ForProviderModeAndZeroGpuOpFilter()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            var gpuService = new GpuDeviceService();
            var profileService = new UserProfileService();
            var searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService),
                gpuDeviceService: gpuService
            );

            searchVm.UseGpuSearch = true;
            searchVm.LoadedConfig = new JamlConfig
            {
                Name = "ClauseFreeConfig",
                Deck = MotelyDeck.Red,
                Stake = MotelyStake.White,
            };

            // 1. Provider mode (SingleSeed) never uses GPU.
            searchVm.SelectedSearchMode = SearchMode.SingleSeed;
            searchVm.SeedInput = "ALEEB";
            searchVm.StartSearchCommand.Execute(null);
            Assert.False(gpuService.GetStatus().IsActive);
            if (searchVm.IsSearching)
            {
                searchVm.StopSearchCommand.Execute(null);
            }

            // 2. AllSeeds with zero selective GPU ops also stays on CPU SIMD.
            searchVm.SelectedSearchMode = SearchMode.AllSeeds;
            searchVm.StartSearchCommand.Execute(null);
            Assert.False(gpuService.GetStatus().IsActive);
            if (searchVm.IsGpuAvailable)
            {
                Assert.Contains(
                    searchVm.ConsoleOutput,
                    m => m.Text.Contains("no selective GPU pre-filter clauses", StringComparison.OrdinalIgnoreCase)
                );
            }
            if (searchVm.IsSearching)
            {
                searchVm.StopSearchCommand.Execute(null);
            }
        });
    }

    [Fact]
    public async Task PartySearchService_InvalidCoordinatorUrl_LeavesIsWorkingFalse()
    {
        using var service = new PartySearchService();
        await Assert.ThrowsAsync<UriFormatException>(() =>
            service.StartPartyWorkerAsync("not-a-valid-uri", "test-party", 1, false, CancellationToken.None)
        );
        Assert.False(service.IsWorking);
        Assert.Null(service.CurrentPartyId);
    }

    [Fact]
    public void SearchTab_ExposesEditablePartyCoordinatorUrlBox_TwoWayBoundToViewModel()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            var profileService = new UserProfileService();
            var searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService)
            );

            var tab = new SearchTab { DataContext = searchVm };
            var window = new Window { Content = tab };
            window.Show();
            try
            {
                var urlBox = tab.FindControl<TextBox>("PartyCoordinatorUrlBox");
                Assert.NotNull(urlBox);
                Assert.Equal(searchVm.PartyCoordinatorUrl, urlBox!.Text);

                urlBox.Text = "http://party.seedfinder.example:3141";
                Assert.Equal("http://party.seedfinder.example:3141", searchVm.PartyCoordinatorUrl);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ShippedYamlFilters_AllValidateCleanly()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "YamlFilters")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        Assert.NotNull(dir);

        string yamlDir = Path.Combine(dir!, "YamlFilters");
        var files = Directory.EnumerateFiles(yamlDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".jaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(files.Count >= 155, $"Expected at least 155 filters in {yamlDir}, found {files.Count}.");

        var failures = new System.Collections.Generic.List<string>();
        foreach (var file in files)
        {
            if (!FilterCheck.TryCheck(File.ReadAllText(file), out _, out _, out var error))
            {
                failures.Add($"{Path.GetRelativePath(yamlDir, file)}: {error}");
            }
        }

        Assert.True(failures.Count == 0, "Invalid shipped filters:\n" + string.Join("\n", failures));
    }

    [Fact]
    public async Task SearchModalViewModel_DefaultZeroMinScore_PreservesLoadedConfigCutoffAndNegativeScores()
    {
        var profileService = new UserProfileService();
        SearchModalViewModel? searchVm = null;

        // 1. JAML cutoff: 50 must NOT be overridden down to 0 when MinScore == 0.
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();
            searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService)
            );
            Assert.Equal(0, searchVm.MinScore);
            searchVm.SelectedSearchMode = SearchMode.SingleSeed;
            searchVm.SeedInput = "ALEEB";
            searchVm.LoadedConfig = JamlConfigLoader.FromJaml("""
                name: HighCutoffFilter
                deck: Red
                stake: White
                cutoff: 50
                should:
                  - boss: any
                    antes: [1]
                    score: 10
                """);
            searchVm.StartSearchCommand.Execute(null);
        });

        for (int i = 0; i < 60; i++)
        {
            bool running = true;
            OnUi(() =>
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                running = searchVm!.IsSearching;
            });
            if (!running)
                break;
            await Task.Delay(25);
        }

        OnUi(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Empty(searchVm!.SearchResults);
        });

        // 2. Negative penalty should: clause with no cutoff: must NOT be dropped at 0 when MinScore == 0.
        OnUi(() =>
        {
            searchVm!.SelectedSearchMode = SearchMode.SingleSeed;
            searchVm.SeedInput = "ALEEB";
            searchVm.LoadedConfig = JamlConfigLoader.FromJaml("""
                name: NegativePenaltyFilter
                deck: Red
                stake: White
                should:
                  - boss: any
                    antes: [1]
                    score: -5
                """);
            searchVm.StartSearchCommand.Execute(null);
        });

        for (int i = 0; i < 60; i++)
        {
            bool running = true;
            OnUi(() =>
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                running = searchVm!.IsSearching;
            });
            if (!running)
                break;
            await Task.Delay(25);
        }

        OnUi(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var match = Assert.Single(searchVm!.SearchResults);
            Assert.Equal("ALEEB", match.Seed);
            Assert.Equal(-5, match.TotalScore);
        });
    }

    [Fact]
    public async Task ZeroLengthEmptySeed_DesktopSearchAnalyzeLocateAndLakeSink_RoundTripCleanly()
    {
        var profileService = new UserProfileService();
        SearchModalViewModel? searchVm = null;
        AnalyzeModalViewModel? analyzeVm = null;

        // 1. SearchModalViewModel SingleSeed with explicit "" searches and matches the 0-char seed.
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();
            searchVm = new SearchModalViewModel(
                profileService,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService)
            );
            searchVm.SelectedSearchMode = SearchMode.SingleSeed;
            searchVm.SeedInput = "\"\"";
            searchVm.LoadedConfig = JamlConfigLoader.FromJaml("""
                name: DesktopEmptySeedCheck
                deck: Erratic
                stake: White
                must:
                  - boss: The Goad
                    antes: [1]
                  - voucher: Crystal Ball
                    antes: [1]
                  - tag: Coupon Tag
                    antes: [1]
                """);
            searchVm.StartSearchCommand.Execute(null);
        });

        for (int i = 0; i < 60; i++)
        {
            bool running = true;
            OnUi(() =>
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                running = searchVm!.IsSearching;
            });
            if (!running)
                break;
            await Task.Delay(25);
        }

        OnUi(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var match = Assert.Single(searchVm!.SearchResults);
            Assert.Equal(string.Empty, match.Seed);
        });

        // 2. AnalyzeModalViewModel keeps CanExecute false on blank "", and analyzes/locates on "\"\"".
        OnUi(() =>
        {
            analyzeVm = new AnalyzeModalViewModel(SpriteService.Instance, profileService);
            Assert.False(analyzeVm.AnalyzeSeedCommand.CanExecute(null));
            Assert.False(analyzeVm.FindCommand.CanExecute(null));

            analyzeVm.SeedInput = "\"\"";
            analyzeVm.FindInput = "Coupon Tag";
            Assert.True(analyzeVm.AnalyzeSeedCommand.CanExecute(null));
            Assert.True(analyzeVm.FindCommand.CanExecute(null));
        });

        await analyzeVm!.AnalyzeSeedCommand.ExecuteAsync(null);
        await analyzeVm.FindCommand.ExecuteAsync(null);

        OnUi(() =>
        {
            Assert.True(analyzeVm.HasAnalysisResults);
            Assert.NotNull(analyzeVm.CurrentAnalysis);
            Assert.Equal(string.Empty, analyzeVm.CurrentAnalysis!.Seed);
            Assert.Equal(8, analyzeVm.Antes.Count);
            Assert.Equal(MotelyBossBlind.TheGoad, analyzeVm.Antes[0].Boss);
            Assert.Equal(MotelyVoucher.CrystalBall, analyzeVm.Antes[0].Voucher);
            Assert.Equal(MotelyTag.CouponTag, analyzeVm.Antes[0].SmallBlindTag.Tag);
            Assert.Equal(MotelyTag.RareTag, analyzeVm.Antes[0].BigBlindTag.Tag);
            Assert.False(analyzeVm.HasFindError);
            Assert.Contains(analyzeVm.FindRows, r => r.Ante == 1);
        });

        // 3. SeedLakeSink persists and reloads the 0-char seed while ignoring whitespace-only lines.
        var id = "bso-empty-seed-" + Guid.NewGuid().ToString("N");
        var lakePath = SeedLakeSink.SeedFilePath(null, id);
        try
        {
            using (var sink = new SeedLakeSink(null, id))
            {
                var scored = new MotelyScoredSeedResult { Seed = string.Empty, Score = 15, Tallies = [5, 10] };
                sink.OnScored(in scored);
                sink.OnSeed("   "); // whitespace-only must be ignored
                Assert.Equal(1, sink.Count);
            }

            var lines = File.ReadAllLines(lakePath);
            var line = Assert.Single(lines);
            Assert.Equal("\"\",15,5,10", line);
            Assert.True(SeedLakeSink.TryParseLine(line, out var parsedSeed, out var parsedScore, out var parsedTallies));
            Assert.Equal(string.Empty, parsedSeed);
            Assert.Equal(15, parsedScore);
            Assert.Equal(new[] { 5, 10 }, parsedTallies);
        }
        finally
        {
            if (File.Exists(lakePath))
                File.Delete(lakePath);
        }
    }
}
