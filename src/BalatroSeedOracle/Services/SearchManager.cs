using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Engines;
using Motely;
using Motely.Filters;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.Services;

public class QuickSearchResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<string> Seeds { get; set; } = new();
    public List<SearchResult>? Results { get; set; }
    public double ElapsedTime { get; set; }
    public string? Error { get; set; }
}

public sealed class SearchManager : IDisposable
{
    private readonly IPlatformServices _platformServices;
    private readonly FilterConfigurationService _filterService;
    private readonly ConcurrentDictionary<string, ActiveSearchContext> _activeSearches = new();
    private bool _disposed;

    private ISearchEngine _currentEngine;
    public ISearchEngine LocalEngine { get; }
    public ISearchEngine RemoteEngine { get; private set; }
    public ISearchEngine ActiveEngine => _currentEngine;

    public SearchManager(
        IPlatformServices platformServices,
        FilterConfigurationService filterService
    )
    {
        _platformServices = platformServices;
        _filterService = filterService;

        LocalEngine = new LocalSearchEngine(platformServices);
        RemoteEngine = new RemoteSearchEngine("https://api.motely.gg");

        _currentEngine = LocalEngine;
    }

    public void SetEngine(ISearchEngine engine)
    {
        _currentEngine = engine;
        DebugLogger.Log("SearchManager", $"Switched to engine: {engine.Name}");
    }

    public void SetRemoteUrl(string url)
    {
        RemoteEngine = new RemoteSearchEngine(url);
        if (_currentEngine is RemoteSearchEngine)
            _currentEngine = RemoteEngine;
    }

    public async Task<ActiveSearchContext> StartSearchAsync(
        SearchCriteria criteria,
        JamlRootDocument config
    )
    {
        DebugLogger.Log("SearchManager", $"Starting search via {_currentEngine.Name} for: {config.Name}");

        if (!string.IsNullOrEmpty(criteria.Deck)) config.Deck = criteria.Deck;
        if (!string.IsNullOrEmpty(criteria.Stake)) config.Stake = criteria.Stake;

        var options = new SearchOptionsDto
        {
            ThreadCount = criteria.ThreadCount,
            BatchSize = criteria.BatchSize,
            StartBatch = (long)criteria.StartBatch,
            EndBatch = (long)criteria.EndBatch,
            SpecificSeed = criteria.DebugSeed,
        };

        if (_currentEngine is LocalSearchEngine)
            return StartSearchLegacy(criteria, config);

        string searchId = await _currentEngine.StartSearchAsync(config, options);
        var context = new ActiveSearchContext(searchId, config);
        _activeSearches[searchId] = context;
        return context;
    }

    public ActiveSearchContext StartSearch(SearchCriteria criteria, JamlRootDocument config)
        => StartSearchLegacy(criteria, config);

    private ActiveSearchContext StartSearchLegacy(SearchCriteria criteria, JamlRootDocument config)
    {
        var yaml = JamlFormatter.Format(config);
        if (!Motely.Filters.Jaml.JamlConfigLoader.TryLoad(yaml, out var jamlConfig, out var conversionError) || jamlConfig is null)
        {
            DebugLogger.LogError("SearchManager", $"Failed to parse JAML: {conversionError}");
            throw new InvalidOperationException($"Filter config could not be parsed as JAML: {conversionError}");
        }

        var settings = JamlSearchBuilder
            .CreateSettings(jamlConfig)
            .WithThreadCount(criteria.ThreadCount > 0 ? criteria.ThreadCount : Environment.ProcessorCount)
            .WithBatchCharacterCount(criteria.BatchSize > 0 ? criteria.BatchSize : 3)
            .WithStartBatchIndex((long)criteria.StartBatch)
            .WithEndBatchIndex(criteria.EndBatch <= long.MaxValue ? (long)criteria.EndBatch : long.MaxValue)
            .WithDeck(jamlConfig.Deck)
            .WithStake(jamlConfig.Stake);

        var searchId = Guid.NewGuid().ToString("N");
        var bsoContext = new BsoSearchContext(searchId, config.Name ?? "filter");
        var activeContext = new ActiveSearchContext(bsoContext, config);

        settings.WithSeedMatchCallback(seed =>
        {
            bsoContext.OnResult(seed, 0, null);
            activeContext.RaiseResultFound(new SearchResult { Seed = seed, TotalScore = 0 });
        });
        settings.WithScoredResultCallback(tally =>
        {
            var tallies = tally.TallyValuesSpan.ToArray();
            bsoContext.OnResult(tally.Seed, tally.Score, tallies);
            activeContext.RaiseResultFound(new SearchResult
            {
                Seed = tally.Seed,
                TotalScore = tally.Score,
                Scores = tallies,
            });
        });
        settings.WithProgressCallback(progress =>
        {
            bsoContext.OnProgress(progress.SeedsSearched, progress.MatchingSeeds, 0);
            activeContext.RaiseProgressUpdated(new SearchProgress
            {
                PercentComplete = progress.PercentComplete,
                SeedsSearched = (ulong)progress.SeedsSearched,
                SeedsPerMillisecond = progress.SeedsPerMillisecond,
                ResultsFound = (int)progress.MatchingSeeds,
                EstimatedTimeRemaining = progress.EstimatedTimeRemainingMilliseconds.HasValue
                    ? TimeSpan.FromMilliseconds(progress.EstimatedTimeRemainingMilliseconds.Value)
                    : null,
            });
        });

        var cts = new CancellationTokenSource();
        var search = settings.Start(cts.Token);
        bsoContext.Attach(search, cts);

        _activeSearches[searchId] = activeContext;
        activeContext.RaiseSearchStarted();
        return activeContext;
    }

    public ActiveSearchContext? GetSearch(string searchId)
    {
        _activeSearches.TryGetValue(searchId, out var context);
        return context;
    }

    public IEnumerable<ActiveSearchContext> GetAllSearches() => _activeSearches.Values;

    public void RemoveSearch(string searchId)
    {
        if (_activeSearches.TryRemove(searchId, out _))
            DebugLogger.Log("SearchManager", $"Removed search: {searchId}");
    }

    public ActiveSearchContext? GetOrRestoreSearch(string searchId) => GetSearch(searchId);

    public void InitializeLibrary(string seedsPath)
    {
        DebugLogger.Log("SearchManager", $"InitializeLibrary({seedsPath}) — sequential library not wired in this build");
    }

    public Task<List<RestoredSearchInfo>> RestoreActiveSearchesAsync(string jamlFiltersDir)
    {
        return Task.FromResult(new List<RestoredSearchInfo>());
    }

    public ActiveSearchContext? ResumeSearch(RestoredSearchInfo info, int threads)
    {
        try
        {
            if (info.Config is null) return null;
            var criteria = new SearchCriteria
            {
                ThreadCount = threads,
                BatchSize = 1000,
                Deck = info.Deck ?? "Red",
                Stake = info.Stake ?? "White",
            };
            if (!string.IsNullOrEmpty(info.LastSeed))
                criteria.StartBatch = (ulong)SeedMath.SeedToBatchIndex(info.LastSeed, criteria.BatchSize) + 1;
            return StartSearch(criteria, info.Config);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Failed to resume search {info.SearchId}: {ex.Message}");
            return null;
        }
    }

    public void StopAllSearches()
    {
        foreach (var context in _activeSearches.Values.ToList())
        {
            try { context.Stop(); context.Dispose(); }
            catch (Exception ex) { DebugLogger.LogError("SearchManager", $"Error stopping search {context.SearchId}: {ex.Message}"); }
        }
        _activeSearches.Clear();
    }

    public int StopSearchesForFilter(string filterId)
    {
        var toRemove = _activeSearches.Values
            .Where(c => c.FilterId == filterId || c.FilterName == filterId)
            .Select(c => c.SearchId)
            .ToList();

        foreach (var searchId in toRemove)
        {
            if (_activeSearches.TryRemove(searchId, out var context))
            {
                try { context.Stop(); context.Dispose(); } catch (Exception ex) { DebugLogger.LogError("SearchManager", $"Error stopping search {context.SearchId}: {ex.Message}"); }
            }
        }
        return toRemove.Count;
    }

    public async Task<QuickSearchResult> RunQuickSearchAsync(SearchCriteria criteria, JamlRootDocument config)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var context = StartSearch(criteria, config);
            context.Start();

            var maxWait = TimeSpan.FromSeconds(30);
            var waited = TimeSpan.Zero;
            var pollInterval = TimeSpan.FromMilliseconds(100);
            while (context.IsRunning && waited < maxWait)
            {
                await Task.Delay(pollInterval);
                waited += pollInterval;
            }

            var results = await context.GetResultsPageAsync(0, 1000);
            var elapsed = DateTime.UtcNow - startTime;

            if (context.IsRunning) context.Stop();
            RemoveSearch(context.SearchId);
            context.Dispose();

            return new QuickSearchResult
            {
                Success = true,
                Count = results.Count,
                Seeds = results.Select(r => r.Seed).ToList(),
                Results = results,
                ElapsedTime = elapsed.TotalSeconds,
            };
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Quick search failed: {ex.Message}");
            var elapsed = DateTime.UtcNow - startTime;
            return new QuickSearchResult
            {
                Success = false,
                Count = 0,
                Seeds = new List<string>(),
                ElapsedTime = elapsed.TotalSeconds,
                Error = ex.Message,
            };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAllSearches();
    }
}

public class RestoredSearchInfo
{
    public string SearchId { get; set; } = "";
    public string FilterName { get; set; } = "";
    public string Deck { get; set; } = "Red";
    public string Stake { get; set; } = "White";
    public string? LastSeed { get; set; }
    public long TotalSeedsProcessed { get; set; }
    public long TotalMatches { get; set; }
    public JamlRootDocument Config { get; set; } = null!;
}
