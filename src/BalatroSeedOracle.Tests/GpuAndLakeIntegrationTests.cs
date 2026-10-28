using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Motely;
using Motely.Enums;
using Motely.Filters;
using Motely.Gpu;
using Motely.Lake;
using Motely.Party;
using Xunit;

namespace BalatroSeedOracle.Tests;

[Collection(HeadlessAvaloniaCollection.Name)]
public class GpuAndLakeIntegrationTests(HeadlessAvaloniaFixture avalonia)
{
    private static readonly Regex SeedRegex = new("^[1-9A-Z]{1,8}$", RegexOptions.Compiled);

    private T OnUi<T>(Func<T> action) => avalonia.Run(action);
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
    public void GpuDeviceService_DiscoversHostHardware_AndReportsFp64Status()
    {
        var service = new GpuDeviceService();
        var status = service.GetStatus();

        Assert.NotNull(status);
        Assert.NotNull(status.DeviceName);
        Assert.NotNull(status.Vendor);

        var bestDev = GpuDeviceInfo.GetBestDevice();
        if (bestDev is not null && bestDev.SupportsFp64)
        {
            Assert.True(status.IsAvailable);
            Assert.Contains(bestDev.Name, status.DeviceName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(bestDev.ComputeUnits, status.ComputeUnits);
        }
        else
        {
            Assert.False(status.IsActive);
        }
    }

    [Fact]
    public void GpuDeviceService_TracksActiveState_AndFiresStatusChanged()
    {
        var service = new GpuDeviceService();
        GpuDeviceStatus? received = null;
        service.StatusChanged += (_, s) => received = s;

        service.SetSearchActive(true, 8_500_000.0);
        var status = service.GetStatus();

        if (status.IsAvailable)
        {
            Assert.True(status.IsActive);
            Assert.Equal(8_500_000.0, status.CurrentSeedsPerSecond);
            Assert.NotNull(received);
            Assert.True(received.IsActive);
        }

        service.SetSearchActive(false);
        var stopped = service.GetStatus();
        Assert.False(stopped.IsActive);
        Assert.Equal(0, stopped.CurrentSeedsPerSecond);
    }

    [Fact]
    public async Task GpuDeviceService_RunsBenchmark_WithoutCrashing()
    {
        var service = new GpuDeviceService();
        var result = await service.RunBenchmarkAsync(batchChars: 3, batchCount: 4);

        Assert.NotNull(result);
        if (GpuDeviceInfo.IsGpuAvailable)
        {
            Assert.True(result.GpuAvailable);
            Assert.True(result.SeedsEvaluated > 0);
            Assert.True(result.MillionsOfSeedsPerSecond >= 0);
        }
    }

    [Fact]
    public void DungmotLakeService_EnumeratesRealLakes_WhenPathExists()
    {
        string lakePath = @"D:\dungmot";
        if (!Directory.Exists(lakePath))
            return; // Skip if on environment without D:\dungmot

        var service = new DungmotLakeService(lakePath);
        Assert.True(service.IsLakeAvailable);

        var partitions = service.GetAvailablePartitions();
        Assert.NotEmpty(partitions);

        // Verify curated ranks, suits, or CSV negative partitions exist
        var curatedRanks = partitions.Where(p => p.Category == LakeCategory.CuratedRanks).ToList();
        var curatedSuits = partitions.Where(p => p.Category == LakeCategory.CuratedSuits).ToList();
        var negatives = partitions.Where(p => p.Category is LakeCategory.NegativeUncommon or LakeCategory.TripleNegative).ToList();

        Assert.True(curatedRanks.Count > 0 || curatedSuits.Count > 0 || negatives.Count > 0);

        // Stream first 5 seeds from first available non-empty partition
        var validPartition = partitions.FirstOrDefault(p => p.EstimatedSeeds > 0);
        if (validPartition != null)
        {
            var seeds = service.StreamSeeds(validPartition, 5).ToList();
            Assert.NotEmpty(seeds);
            foreach (var seed in seeds)
            {
                Assert.True(SeedRegex.IsMatch(seed), $"Seed '{seed}' did not match standard Balatro seed format.");
            }
        }
    }

    [Fact]
    public void DungmotLakeService_WorksWithMockDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "mock-lake-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));
        Directory.CreateDirectory(Path.Combine(tempDir, "suits"));

        try
        {
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "Ace_best.txt"), new[] { "SEEDACE1", "SEEDACE2", "SEEDACE3" });
            File.WriteAllLines(Path.Combine(tempDir, "uncommonss.csv"), new[] { "seed,count", "NEGUNCM1,3", "NEGUNCM2,3" });

            var service = new DungmotLakeService(tempDir);
            Assert.True(service.IsLakeAvailable);

            var partitions = service.GetAvailablePartitions();
            Assert.Equal(2, partitions.Count);

            var acePart = partitions.First(p => p.PartitionKey == "Ace");
            var seeds = service.StreamSeeds(acePart).ToList();
            Assert.Equal(new[] { "SEEDACE1", "SEEDACE2", "SEEDACE3" }, seeds);

            var provider = service.CreateSeedProvider(acePart);
            Assert.NotNull(provider);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void SearchModalViewModel_PersistsGpuSearchToggle_InUserProfile()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            string profileDir = Path.Combine(Path.GetTempPath(), "bso-test-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(profileDir);
            string profileFile = Path.Combine(profileDir, "userprofile.json");

            try
            {
                var profileService = new UserProfileService(profileFile);
                var searchVm = new SearchModalViewModel(
                    profileService,
                    () => new AnalyzeModalViewModel(SpriteService.Instance, profileService)
                );

                Assert.True(searchVm.UseGpuSearch);

                // Toggle off
                searchVm.UseGpuSearch = false;
                Assert.False(profileService.GetProfile().UseGpuSearch);

                // Reload from disk to verify persistence
                var reloadedService = new UserProfileService(profileFile);
                Assert.False(reloadedService.GetProfile().UseGpuSearch);

                // Toggle back on
                searchVm.UseGpuSearch = true;
                var reloadedService2 = new UserProfileService(profileFile);
                Assert.True(reloadedService2.GetProfile().UseGpuSearch);
            }
            finally
            {
                try { Directory.Delete(profileDir, true); } catch { }
            }
        });
    }

    [Fact]
    public void SearchModalViewModel_DbListMode_PopulatesPartitionsFromLakeService()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            string tempDir = Path.Combine(Path.GetTempPath(), "bso-lake-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "King_best.txt"), new[] { "KING0001", "KING0002" });

            try
            {
                var lakeService = new DungmotLakeService(tempDir);
                var profileService = new UserProfileService();
                var searchVm = new SearchModalViewModel(
                    profileService,
                    () => new AnalyzeModalViewModel(SpriteService.Instance, profileService),
                    dungmotLakeService: lakeService
                );

                Assert.NotEmpty(searchVm.AvailableLakePartitions);
                Assert.NotEmpty(searchVm.AvailableDbLists);
                Assert.Equal("Curated Rank: King", searchVm.SelectedDbList);
                Assert.NotNull(searchVm.SelectedLakePartition);
                Assert.Equal("King", searchVm.SelectedLakePartition.PartitionKey);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        });
    }

    [Fact]
    public void SearchModalViewModel_DbListMode_StreamsSeedsIntoSearch()
    {
        OnUi(() =>
        {
            EnsureAppResourcesLoaded();

            string tempDir = Path.Combine(Path.GetTempPath(), "bso-lake-stream-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "Queen_best.txt"), new[] { "QUEEN001", "QUEEN002" });

            try
            {
                var lakeService = new DungmotLakeService(tempDir);
                var profileService = new UserProfileService();
                var searchVm = new SearchModalViewModel(
                    profileService,
                    () => new AnalyzeModalViewModel(SpriteService.Instance, profileService),
                    dungmotLakeService: lakeService
                );

                searchVm.SelectedSearchMode = SearchMode.DbList;
                searchVm.LoadedConfig = new JamlConfig
                {
                    Name = "TestLakeConfig",
                    Deck = MotelyDeck.Red,
                    Stake = MotelyStake.White,
                };

                // Start search in DbList mode
                searchVm.StartSearchCommand.Execute(null);

                // Verify search console accepted DbList mode and started streaming
                Assert.Contains(searchVm.ConsoleOutput, m => m.Text.Contains("Streaming seed lake"));
                Assert.DoesNotContain(searchVm.ConsoleOutput, m => m.Text.Contains("This mode was removed"));

                // Stop search
                if (searchVm.IsSearching)
                {
                    searchVm.StopSearchCommand.Execute(null);
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        });
    }

    [Fact]
    public void BalatroMainMenuViewModel_TracksGpuStatus_AndUpdatesDockText()
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
            FiltersModalViewModel filtersVm = null!;
            filtersVm = new FiltersModalViewModel(
                profileService,
                () => new ViewModels.FilterTabs.ValidateFilterTabViewModel(filtersVm)
            );

            var menuVm = new BalatroMainMenuViewModel(
                profileService,
                searchVm,
                filtersVm,
                () => new AnalyzeModalViewModel(SpriteService.Instance, profileService),
                gpuDeviceService: gpuService
            );

            Assert.NotNull(menuVm.GpuStatusText);
            Assert.NotEmpty(menuVm.GpuStatusText);

            gpuService.SetSearchActive(true, 12_400_000.0);
            if (menuVm.IsGpuAvailable)
            {
                Assert.Contains("12.4M/s", menuVm.GpuStatusText);
            }

            gpuService.SetSearchActive(false);
            if (menuVm.IsGpuAvailable)
            {
                Assert.Equal("GPU READY", menuVm.GpuStatusText);
            }
        });
    }

    [Fact]
    public async Task PartySearchService_LifecycleAndCancellation()
    {
        // Mock HTTP handler for coordinator
        var handler = new TestPartyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        using var partyService = new PartySearchService(httpClient: httpClient);

        PartySearchProgress? lastProgress = null;
        partyService.ProgressUpdated += (_, p) => lastProgress = p;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await partyService.StartPartyWorkerAsync(
            coordinatorUrl: "http://localhost:3141",
            partyId: "test-party-123",
            threads: 1,
            useGpu: false,
            ct: cts.Token
        );

        Assert.True(partyService.IsWorking);
        Assert.Equal("test-party-123", partyService.CurrentPartyId);

        // Stop the worker
        await partyService.StopPartyWorkerAsync();
        Assert.False(partyService.IsWorking);
        Assert.NotNull(lastProgress);
    }

    private sealed class TestPartyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Simulate fully leased coordinator response
            string json = """{"done": true, "reason": "fully leased"}""";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
