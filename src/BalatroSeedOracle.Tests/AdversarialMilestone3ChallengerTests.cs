using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Intrinsics;
using System.Text.RegularExpressions;
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
/// Adversarial stress tests and parity verification for Milestone 3 services and submodule integration.
/// Challenges:
/// 1. Stress tests DungmotLakeService and SeedLakeService under streaming throughput, concurrency, and malformed inputs.
/// 2. Stress tests PartySearchService under rapid lifecycle thrashing, multi-threaded worker loops, and network delays.
/// 3. Verifies strict RNG fidelity and 35 NaN-hash seed fallback (52 Tens of Spades) via src/MotelyJAML.
/// </summary>
public sealed class AdversarialMilestone3ChallengerTests
{
    private static readonly Regex SeedRegex = new("^[1-9A-Z]{1,8}$", RegexOptions.Compiled);

    /// <summary>
    /// All 35 canonical seeds that evaluate Balatro erratic pseudohash to NaN.
    /// </summary>
    public static readonly string[] NaNSeeds =
    [
        "7LB2WVPK", "BGY5SDS", "1V3UQ6AP", "36756M6I", "3WBV4FSC",
        "627SP6LE", "6WG44S7Z", "81N7QMOM", "9PCOBR2D", "B9A4IA7S",
        "CJFU1DL1", "DHHHTLKL", "ENNGZMF2", "FAH3Y3S9", "HOWRQJVZ",
        "I4N2BX1E", "IU2LKGT8", "N9ER1XLW", "NSPURPAH", "NWMELXAM",
        "O6BRINCY", "O7CMD6K7", "PHLSY9F4", "Q6SEQZLP", "QIB7UA76",
        "RDNCZ58P", "S4DM5V37", "S4TMWWAE", "S793AULP", "SNML7XC8",
        "SS44ACWE", "SSU4L34S", "TO7WNQA5", "XEQH7CP9", "ZZFHQFDG"
    ];

    public static readonly string[] VariableLengthSeeds =
    [
        "1", "A", "42", "AB", "123", "XYZ", "CATS", "ALEEB", "MOTELY", "PIROCKS", "12345678"
    ];

    #region Part 1: DungmotLakeService & SeedLakeService Stress Tests

    [Fact]
    public void LakeService_ConcurrentStressStream_UnderHighThroughput()
    {
        string tempLake = Path.Combine(Path.GetTempPath(), "lake-stress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempLake, "ranks"));
        Directory.CreateDirectory(Path.Combine(tempLake, "suits"));

        try
        {
            // Generate mock lake partitions with 20,000 seeds each using valid Balatro seed characters (1-9, A-Z)
            var rankSeeds = Enumerable.Range(1, 20000).Select(i => $"ACE{i % 9 + 1}AAAA").ToList();
            File.WriteAllLines(Path.Combine(tempLake, "ranks", "Ace_best.txt"), rankSeeds);

            var suitSeeds = Enumerable.Range(1, 20000).Select(i => $"HRT{i % 9 + 1}BBBB").ToList();
            File.WriteAllLines(Path.Combine(tempLake, "suits", "Heart_best.txt"), suitSeeds);

            // Generate CSV with headers, comments, and extra columns
            var csvLines = new List<string> { "Progress,100%", "SEED,SCORE,EXTRA" };
            csvLines.AddRange(Enumerable.Range(1, 20000).Select(i => $"NEG{i % 9 + 1}CCCC,3,bonus"));
            File.WriteAllLines(Path.Combine(tempLake, "uncommonss.csv"), csvLines);

            var lakeService = new DungmotLakeService(tempLake);
            Assert.True(lakeService.IsLakeAvailable);

            var partitions = lakeService.GetAvailablePartitions();
            Assert.Equal(3, partitions.Count);

            // Concurrently stream from 16 worker tasks simultaneously
            int workerCount = 16;
            var exceptions = new ConcurrentBag<Exception>();
            var streamedCounts = new ConcurrentBag<int>();

            Parallel.For(0, workerCount, new ParallelOptions { MaxDegreeOfParallelism = workerCount }, i =>
            {
                try
                {
                    var part = partitions[i % partitions.Count];
                    int maxToRead = 1500;
                    var seeds = lakeService.StreamSeeds(part, maxToRead).ToList();

                    if (seeds.Count != maxToRead)
                        throw new InvalidOperationException($"Expected {maxToRead} seeds, got {seeds.Count} for {part.Name}");

                    foreach (var s in seeds)
                    {
                        if (!SeedRegex.IsMatch(s))
                            throw new FormatException($"Invalid seed format: '{s}'");
                    }

                    streamedCounts.Add(seeds.Count);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Assert.Empty(exceptions);
            Assert.Equal(workerCount, streamedCounts.Count);
            Assert.Equal(workerCount * 1500, streamedCounts.Sum());
        }
        finally
        {
            try { Directory.Delete(tempLake, true); } catch { }
        }
    }

    [Fact]
    public void LakeService_MalformedAndBoundaryHandling()
    {
        string tempLake = Path.Combine(Path.GetTempPath(), "lake-malformed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempLake, "ranks"));

        try
        {
            // Empty file
            File.WriteAllText(Path.Combine(tempLake, "ranks", "Empty_best.txt"), "");

            // Whitespace and comments only
            File.WriteAllLines(Path.Combine(tempLake, "ranks", "Comments_best.txt"),
                ["   ", "# This is a comment", "   # Another comment", ""]);

            // Seeds mixed with whitespace and blank lines
            File.WriteAllLines(Path.Combine(tempLake, "ranks", "Dirty_best.txt"),
                ["  SEED0001  ", "", "#comment", "SEED0002", "   "]);

            var lakeService = new DungmotLakeService(tempLake);
            var partitions = lakeService.GetAvailablePartitions();

            var emptyPart = partitions.First(p => p.PartitionKey == "Empty");
            Assert.Empty(lakeService.StreamSeeds(emptyPart).ToList());

            var commentPart = partitions.First(p => p.PartitionKey == "Comments");
            Assert.Empty(lakeService.StreamSeeds(commentPart).ToList());

            var dirtyPart = partitions.First(p => p.PartitionKey == "Dirty");
            var dirtySeeds = lakeService.StreamSeeds(dirtyPart).ToList();
            Assert.Equal(["SEED0001", "SEED0002"], dirtySeeds);

            // Boundary: SeedLakeService.ReadSeeds limit <= 0 returns empty
            Assert.Empty(lakeService.ReadSeeds(LakeCategory.CuratedRanks, "Dirty", limit: 0).ToList());

            // Boundary: DungmotLakeService.StreamSeeds with maxSeeds == 0 returns empty
            var zeroMaxSeeds = lakeService.StreamSeeds(dirtyPart, 0).ToList();
            Assert.Empty(zeroMaxSeeds);

            // Boundary: DungmotLakeService.StreamSeeds with maxSeeds == -1 returns all seeds
            var unlimitedSeeds = lakeService.StreamSeeds(dirtyPart, -1).ToList();
            Assert.Equal(["SEED0001", "SEED0002"], unlimitedSeeds);

            // Dynamic path change
            lakeService.LakeRootPath = Path.Combine(tempLake, "nonexistent");
            Assert.False(lakeService.IsLakeAvailable);
            Assert.Empty(lakeService.GetAvailablePartitions());
        }
        finally
        {
            try { Directory.Delete(tempLake, true); } catch { }
        }
    }

    [Fact]
    public void LakeService_CreateSeedProvider_PaginatesAndFiltersAccurately()
    {
        string tempLake = Path.Combine(Path.GetTempPath(), "lake-provider-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempLake, "ranks"));

        try
        {
            var seedList = Enumerable.Range(1, 500).Select(i => $"PROV{i:D4}").ToList();
            File.WriteAllLines(Path.Combine(tempLake, "ranks", "King_best.txt"), seedList);

            var lakeService = new DungmotLakeService(tempLake);
            var kingPart = lakeService.GetAvailablePartitions().First(p => p.PartitionKey == "King");

            // Limit to 50
            var provider = lakeService.CreateSeedProvider(kingPart, limit: 50);
            Assert.NotNull(provider);

            var collected = new List<string>();
            string seed;
            while (!string.IsNullOrEmpty(seed = provider.NextSeed()))
            {
                collected.Add(seed);
            }

            Assert.Equal(50, collected.Count);
            Assert.Equal("PROV0001", collected[0]);
            Assert.Equal("PROV0050", collected[49]);
        }
        finally
        {
            try { Directory.Delete(tempLake, true); } catch { }
        }
    }

    #endregion

    #region Part 2: PartySearchService Stress Tests

    [Fact]
    public async Task PartySearchService_RapidStartStopThrashing_DoesNotDeadlockOrThrow()
    {
        var handler = new DelayedPartyHttpMessageHandler(delayMs: 10);
        using var http = new HttpClient(handler);
        using var service = new PartySearchService(httpClient: http);

        // Rapid start / stop cycles in quick succession
        for (int i = 0; i < 15; i++)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await service.StartPartyWorkerAsync(
                coordinatorUrl: "http://coordinator.test",
                partyId: $"party-thrash-{i}",
                threads: 1,
                useGpu: false,
                ct: cts.Token
            );

            Assert.NotNull(service.CurrentProgress);
            Assert.Equal($"party-thrash-{i}", service.CurrentPartyId);

            // Immediately stop or wait short time
            if (i % 2 == 0)
            {
                await Task.Delay(15);
            }
            await service.StopPartyWorkerAsync();
            Assert.False(service.IsWorking);
        }
    }

    [Fact]
    public async Task PartySearchService_SimulatedWorkLoop_ReceivesProgressAndLogsConcurrently()
    {
        var handler = new MultiLeaseFakeHandler(totalLeases: 5);
        using var http = new HttpClient(handler);
        using var service = new PartySearchService(httpClient: http);

        var progressReports = new ConcurrentBag<PartySearchProgress>();
        var logMessages = new ConcurrentBag<string>();

        service.ProgressUpdated += (_, p) => progressReports.Add(p);
        service.LogMessageReceived += (_, m) => logMessages.Add(m);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await service.StartPartyWorkerAsync(
            coordinatorUrl: "http://coordinator.test",
            partyId: "multi-lease-party",
            threads: 2,
            useGpu: false,
            ct: cts.Token
        );

        // Wait for worker to finish the 5 leases and complete
        int retries = 0;
        while (service.IsWorking && retries++ < 50)
        {
            await Task.Delay(100);
        }

        Assert.False(service.IsWorking);
        Assert.NotEmpty(progressReports);
        Assert.NotEmpty(logMessages);

        var finalProgress = service.CurrentProgress;
        Assert.NotNull(finalProgress);
        Assert.True(finalProgress.LeasesCompleted >= 5);
    }

    [Fact]
    public async Task PartySearchService_HandlesServerErrors_WithoutCrashing()
    {
        var handler = new FailingPartyHttpMessageHandler(failCount: 3);
        using var http = new HttpClient(handler);
        using var service = new PartySearchService(httpClient: http);

        var progressReports = new List<PartySearchProgress>();
        service.ProgressUpdated += (_, p) =>
        {
            lock (progressReports) progressReports.Add(p);
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        await service.StartPartyWorkerAsync(
            coordinatorUrl: "http://coordinator.test",
            partyId: "retry-party",
            threads: 1,
            useGpu: false,
            ct: cts.Token
        );

        int wait = 0;
        while (service.IsWorking && wait++ < 40)
        {
            await Task.Delay(100);
        }

        // Even after retries and ultimate completion or error, service gracefully returns to idle
        Assert.False(service.IsWorking);
    }

    #endregion

    #region Part 3: RNG & NaN-Hash Seed Fallback Parity

    [Fact]
    public void Exactly35Seeds_EvaluatedViaSubmodule_ProduceErraticNaNState()
    {
        Assert.Equal(35, NaNSeeds.Length);

        foreach (var seed in NaNSeeds)
        {
            using var search = new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(new PassthroughFilterDesc())
                .WithDeck(MotelyDeck.Erratic)
                .WithStake(MotelyStake.White)
                .WithSeedGenerator([seed], 1)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .WithJimmolate(ctx =>
                {
                    var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                    Assert.True(double.IsNaN(stream.State), $"Seed {seed} PRNG state must be NaN.");
                    return 1;
                })
                .Start();

            search.AwaitCompletion();
            Assert.Equal(1, search.MatchingSeeds);
        }
    }

    [Fact]
    public void All35NaNSeeds_Produce52TensOfSpades_InScalarAndVectorEngines()
    {
        // 1. Scalar Engine Verification
        var config = new JamlConfig
        {
            Deck = MotelyDeck.Erratic,
            Stake = MotelyStake.White,
        };
        config.Seeds.AddRange(NaNSeeds);

        var scalarResults = MotelyJamlyzer.Analyze(config);
        Assert.Equal(35, scalarResults.Count);

        foreach (var res in scalarResults)
        {
            Assert.NotNull(res.ErraticDeck);
            Assert.Equal(52, res.ErraticDeck.Length);

            foreach (var card in res.ErraticDeck)
            {
                Assert.Equal(MotelyStandardcardRank.Ten, card.StandardcardRank);
                Assert.Equal(MotelyStandardcardSuit.Spades, card.StandardcardSuit);
                Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, card.Value);
            }
        }

        // 2. Vector SIMD Engine Verification across all 35 seeds in chunks of 8
        for (int chunkStart = 0; chunkStart < NaNSeeds.Length; chunkStart += 8)
        {
            var chunk = NaNSeeds.Skip(chunkStart).Take(8).ToArray();
            var paddedChunk = chunk.Length == 8 ? chunk : [.. chunk, .. Enumerable.Repeat(NaNSeeds[0], 8 - chunk.Length)];

            var vectorCardResults = new ConcurrentDictionary<string, List<MotelyItem>>();
            var captureFilterDesc = new ChallengerCardCaptureFilterDesc((seed, index, item) =>
            {
                var list = vectorCardResults.GetOrAdd(seed, _ => []);
                lock (list)
                {
                    if (list.Count == index)
                        list.Add(item);
                }
            });

            using var vectorSearch = new MotelySearchSettings<ChallengerCardCaptureFilterDesc.CardCaptureFilter>(captureFilterDesc)
                .WithDeck(MotelyDeck.Erratic)
                .WithStake(MotelyStake.White)
                .WithSeedGenerator(paddedChunk, paddedChunk.Length)
                .WithThreadCount(1)
                .WithQuietMode(true)
                .Start();

            vectorSearch.AwaitCompletion();

            foreach (var s in chunk)
            {
                Assert.True(vectorCardResults.TryGetValue(s, out var cards), $"Vector cards not captured for {s}");
                Assert.Equal(52, cards.Count);
                foreach (var card in cards)
                {
                    Assert.Equal(new MotelyItem(MotelyStandardCard.TenOfSpades).Value, card.Value);
                }
            }
        }
    }

    [Fact]
    public void VariableLengthSeeds_1To8Chars_EvaluateDeterministicallyWithoutCrash()
    {
        foreach (var seed in VariableLengthSeeds)
        {
            Assert.True(SeedRegex.IsMatch(seed), $"Seed '{seed}' must match 1-8 char format");

            var cfg = new JamlConfig
            {
                Deck = MotelyDeck.Red,
                Stake = MotelyStake.White,
            };
            cfg.Seeds.Add(seed);

            var res = MotelyJamlyzer.Analyze(cfg);
            Assert.Single(res);
            Assert.Equal(seed, res[0].Seed);
            Assert.NotEmpty(res[0].Antes);
        }
    }

    #endregion

    #region Helper Classes for HTTP Simulation

    private sealed class DelayedPartyHttpMessageHandler(int delayMs) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delayMs, cancellationToken);
            string json = """{"done": true, "reason": "complete"}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class MultiLeaseFakeHandler(int totalLeases) : HttpMessageHandler
    {
        private int _currentLease = 0;
        private readonly object _gate = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/api/party/next")
            {
                lock (_gate)
                {
                    if (_currentLease < totalLeases)
                    {
                        _currentLease++;
                        string leaseJson = $$"""
                        {
                            "lease": {
                                "partyId": "multi-lease-party",
                                "jaml": "deck: Red\nstake: White\n",
                                "batchChars": 1,
                                "startBlock": {{_currentLease * 10}},
                                "blockCount": 2,
                                "totalBlocks": 100,
                                "workerToken": "tok-{{_currentLease}}",
                                "expiresAt": "{{DateTime.UtcNow.AddMinutes(5):O}}"
                            }
                        }
                        """;
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(leaseJson, System.Text.Encoding.UTF8, "application/json")
                        });
                    }

                    string doneJson = """{"done": true, "reason": "all leases completed"}""";
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(doneJson, System.Text.Encoding.UTF8, "application/json")
                    });
                }
            }

            if (path == "/api/party/report")
            {
                string reportRes = """{"ok": true, "confirmed": [], "recorded": 0}""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(reportRes, System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FailingPartyHttpMessageHandler(int failCount) : HttpMessageHandler
    {
        private int _attempts = 0;
        private readonly object _gate = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _attempts++;
                if (_attempts <= failCount)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Server error", System.Text.Encoding.UTF8, "text/plain")
                    });
                }

                string doneJson = """{"done": true, "reason": "complete after recovery"}""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(doneJson, System.Text.Encoding.UTF8, "application/json")
                });
            }
        }
    }

    private sealed class ChallengerCardCaptureFilterDesc(Action<string, int, MotelyItem> recordVectorCard)
        : IMotelySeedFilterDesc<ChallengerCardCaptureFilterDesc.CardCaptureFilter>
    {
        public CardCaptureFilter CreateFilter(ref MotelyFilterCreationContext ctx) =>
            new(recordVectorCard);

        public readonly struct CardCaptureFilter : IMotelySeedFilter
        {
            private readonly Action<string, int, MotelyItem> _recordVectorCard;

            public CardCaptureFilter(Action<string, int, MotelyItem> recordVectorCard)
            {
                _recordVectorCard = recordVectorCard;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                var stream = ctx.CreateErraticDeckPrngStream(isCached: false);
                var cards = new MotelyItemVector[52];
                for (int i = 0; i < 52; i++)
                {
                    cards[i] = ctx.GetNextErraticDeckCard(ref stream);
                }

                var recorder = _recordVectorCard;
                return ctx.SearchIndividualSeeds(single =>
                {
                    int lane = single.VectorLane;
                    string seed = single.GetSeed();
                    for (int i = 0; i < 52; i++)
                    {
                        recorder(seed, i, cards[i][lane]);
                    }
                    return 1;
                });
            }
        }
    }

    #endregion
}
