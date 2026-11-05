using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using Motely;
using Motely.Analysis;
using Motely.Enums;
using Motely.Filters;
using Motely.Lake;
using Motely.Party;
using Xunit;

namespace BalatroSeedOracle.Tests;

/// <summary>
/// Empirical Adversarial Stress Harness for Milestone 4.
/// Stress-tests:
/// 1. PartySearchService concurrency, lifecycle edge cases, pre-cancelled tokens, disposal, and exception isolation.
/// 2. DungmotLakeService boundary cases: nulls, zero limits, negative limits, path mutation, and missing partitions.
/// 3. Deterministic NaN-seed fidelity across 7LB2WVPK, BGY5SDS, ZZFHQFDG and all 35 canonical seeds.
/// </summary>
public sealed class AdversarialMilestone4ChallengerTests
{
    public static readonly string[] CanonicalNaNSeeds =
    [
        "7LB2WVPK", "BGY5SDS", "ZZFHQFDG", "1V3UQ6AP", "36756M6I", "3WBV4FSC",
        "627SP6LE", "6WG44S7Z", "81N7QMOM", "9PCOBR2D", "B9A4IA7S", "CJFU1DL1",
        "DHHHTLKL", "ENNGZMF2", "FAH3Y3S9", "HOWRQJVZ", "I4N2BX1E", "IU2LKGT8",
        "N9ER1XLW", "NSPURPAH", "NWMELXAM", "O6BRINCY", "O7CMD6K7", "PHLSY9F4",
        "Q6SEQZLP", "QIB7UA76", "RDNCZ58P", "S4DM5V37", "S4TMWWAE", "S793AULP",
        "SNML7XC8", "SS44ACWE", "SSU4L34S", "TO7WNQA5", "XEQH7CP9"
    ];

    #region 1. PartySearchService Adversarial Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task PartySearchService_ThrowsOnInvalidPartyId(string invalidPartyId)
    {
        using var service = new PartySearchService();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.StartPartyWorkerAsync("http://coordinator.test", invalidPartyId, 1, false, CancellationToken.None));
    }

    [Theory]
    [InlineData("invalid-uri-without-scheme")]
    [InlineData("://missing-scheme")]
    public async Task PartySearchService_ThrowsOnInvalidCoordinatorUrl(string invalidUrl)
    {
        using var service = new PartySearchService();
        await Assert.ThrowsAsync<UriFormatException>(() =>
            service.StartPartyWorkerAsync(invalidUrl, "valid-party", 1, false, CancellationToken.None));
    }

    [Fact]
    public async Task PartySearchService_PreCancelledToken_TransitionsCleanlyWithoutHanging()
    {
        var handler = new SimpleDoneHandler();
        using var http = new HttpClient(handler);
        using var service = new PartySearchService(httpClient: http);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        await service.StartPartyWorkerAsync("http://coordinator.test", "precancelled-party", 1, false, cts.Token);

        // Pre-cancelled token must not leave IsWorking stuck in true state
        bool remainsWorkingStuck = service.IsWorking;
        Assert.False(remainsWorkingStuck, "Pre-cancelled token must transition cleanly without leaving IsWorking = true");

        // Calling StopPartyWorkerAsync clears the stuck state
        await service.StopPartyWorkerAsync();
        Assert.False(service.IsWorking);
    }

    [Fact]
    public async Task PartySearchService_StopWhenIdle_IsSafeAndIdempotent()
    {
        using var service = new PartySearchService();
        Assert.False(service.IsWorking);
        Assert.Null(service.CurrentProgress);

        // Should return without error multiple times
        await service.StopPartyWorkerAsync();
        await service.StopPartyWorkerAsync();
        Assert.False(service.IsWorking);
    }

    [Fact]
    public async Task PartySearchService_ConcurrentStartStopRace_DoesNotDeadlockOrCorrupt()
    {
        var handler = new SimpleDoneHandler();
        using var http = new HttpClient(handler);
        using var service = new PartySearchService(httpClient: http);

        var exceptions = new ConcurrentBag<Exception>();

        // 10 concurrent tasks racing Start and Stop
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(40));
                    await service.StartPartyWorkerAsync("http://coordinator.test", $"race-{i}", 1, false, cts.Token);
                }
                else
                {
                    await service.StopPartyWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);

        // Final clean stop
        await service.StopPartyWorkerAsync();
        Assert.False(service.IsWorking);
    }

    [Fact]
    public void PartySearchService_DisposeWhileIdle_DoesNotThrow()
    {
        var service = new PartySearchService();
        var ex = Record.Exception(() => service.Dispose());
        Assert.Null(ex);
    }

    #endregion

    #region 2. DungmotLakeService Adversarial Tests

    [Fact]
    public void DungmotLakeService_NullPartition_ThrowsArgumentNullException()
    {
        var service = new DungmotLakeService();
        Assert.Throws<ArgumentNullException>(() => service.StreamSeeds(null!));
        Assert.Throws<ArgumentNullException>(() => service.CreateSeedProvider(null!));
    }

    [Fact]
    public void DungmotLakeService_BoundaryLimits_ZeroNegativeAndOne()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "lake-boundary-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));

        try
        {
            var testSeeds = new[] { "SEED0001", "SEED0002", "SEED0003", "SEED0004", "SEED0005" };
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "Ace_best.txt"), testSeeds);

            var service = new DungmotLakeService(tempDir);
            var part = service.GetAvailablePartitions().First(p => p.PartitionKey == "Ace");

            // maxSeeds == 0 -> empty
            Assert.Empty(service.StreamSeeds(part, maxSeeds: 0).ToList());

            // maxSeeds == 1 -> exactly 1
            var single = service.StreamSeeds(part, maxSeeds: 1).ToList();
            Assert.Single(single);
            Assert.Equal("SEED0001", single[0]);

            // maxSeeds == -1 -> all 5
            var allMinusOne = service.StreamSeeds(part, maxSeeds: -1).ToList();
            Assert.Equal(5, allMinusOne.Count);
            Assert.Equal(testSeeds, allMinusOne);

            // maxSeeds == -999 -> all 5
            var allNegative = service.StreamSeeds(part, maxSeeds: -999).ToList();
            Assert.Equal(5, allNegative.Count);

            // CreateSeedProvider with limit: 0 -> returns provider with empty string NextSeed()
            var providerZero = service.CreateSeedProvider(part, limit: 0);
            Assert.NotNull(providerZero);
            Assert.Empty(providerZero.NextSeed());

            // CreateSeedProvider with limit: 2 -> yields exactly 2 then empty string
            var providerTwo = service.CreateSeedProvider(part, limit: 2);
            Assert.NotNull(providerTwo);
            Assert.Equal("SEED0001", providerTwo.NextSeed());
            Assert.Equal("SEED0002", providerTwo.NextSeed());
            Assert.Empty(providerTwo.NextSeed());
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void DungmotLakeService_MissingPartitionFile_ThrowsFileNotFoundException()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "lake-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new DungmotLakeService(tempDir);
            Assert.Throws<FileNotFoundException>(() =>
                service.ReadSeeds(LakeCategory.CuratedRanks, "NonExistentPartition").ToList());
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void DungmotLakeService_HighConcurrencyReaders_DoNotConflict()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "lake-concurrency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "ranks"));

        try
        {
            var seedList = Enumerable.Range(1, 1000).Select(i => $"ACE{i:D5}").ToList();
            File.WriteAllLines(Path.Combine(tempDir, "ranks", "Ace_best.txt"), seedList);

            var service = new DungmotLakeService(tempDir);
            var part = service.GetAvailablePartitions().First();

            var exceptions = new ConcurrentBag<Exception>();
            int threadCount = 32;

            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, _ =>
            {
                try
                {
                    var read = service.StreamSeeds(part, 100).ToList();
                    if (read.Count != 100)
                        throw new InvalidOperationException($"Expected 100, got {read.Count}");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.Empty(exceptions);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    #endregion

    #region 3. Mandatory NaN Seed Empirical Verification

    [Theory]
    [InlineData("7LB2WVPK")]
    [InlineData("BGY5SDS")]
    [InlineData("ZZFHQFDG")]
    public void MandatoryNaNSeeds_ProduceExact52TensOfSpades(string nanSeed)
    {
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White
        };
        config.Seeds.Add(nanSeed);

        var results = MotelyJamlyzer.Analyze(config);
        Assert.Single(results);

        var run = results[0];
        Assert.NotNull(run.ErraticDeck);
        Assert.Equal(52, run.ErraticDeck.Length);

        var expectedCard = new MotelyItem(MotelyStandardCard.TenOfSpades);

        foreach (var card in run.ErraticDeck)
        {
            Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
            Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
            Assert.Equal(expectedCard.Value, card.Value);
        }
    }

    [Fact]
    public void All35CanonicalNaNSeeds_DeterministicallyYieldAll52TensOfSpades()
    {
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White
        };
        config.Seeds.AddRange(CanonicalNaNSeeds);

        var results = MotelyJamlyzer.Analyze(config);
        Assert.Equal(35, results.Count);

        var expectedCard = new MotelyItem(MotelyStandardCard.TenOfSpades);

        foreach (var run in results)
        {
            Assert.NotNull(run.ErraticDeck);
            Assert.Equal(52, run.ErraticDeck.Length);

            for (int i = 0; i < 52; i++)
            {
                var card = run.ErraticDeck[i];
                Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
                Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
                Assert.Equal(expectedCard.Value, card.Value);
            }
        }
    }

    #endregion

    private sealed class SimpleDoneHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string json = """{"done": true, "reason": "complete"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
