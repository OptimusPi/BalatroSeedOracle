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

}
