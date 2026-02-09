using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers; // For DebugLogger
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.Engines; // Added this namespace
using Motely; // For SearchOptionsDto
using Motely.Executors; // For MotelySearchOrchestrator
using Motely.Filters;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger; // Resolve ambiguity

namespace BalatroSeedOracle.Services;

/// <summary>
/// Result of a quick validation search
/// </summary>
public class QuickSearchResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<string> Seeds { get; set; } = new();
    public List<SearchResult>? Results { get; set; }
    public double ElapsedTime { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Manages seed searches in BalatroSeedOracle.
/// 
/// This is a thin wrapper around Motely's search functionality.
/// Motely owns all database operations - BSO just calls Motely.
/// 
/// ZERO database code here - all storage/queries are handled by Motely.
/// </summary>
public sealed class SearchManager : IDisposable
    {
        private readonly IPlatformServices _platformServices;
        private readonly FilterConfigurationService _filterService;
        private readonly ConcurrentDictionary<string, ActiveSearchContext> _activeSearches = new();
        private bool _disposed;
        
        // Engine Management
        private ISearchEngine _currentEngine;
        public ISearchEngine LocalEngine { get; }
        public ISearchEngine RemoteEngine { get; private set; }
        public ISearchEngine ActiveEngine => _currentEngine;

        public SearchManager(
            IPlatformServices platformServices, 
            FilterConfigurationService filterService) // Added filterService
        {
            _platformServices = platformServices;
            _filterService = filterService; // Stored but not used in this snippet
            
            // Initialize Engines
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
            {
                _currentEngine = RemoteEngine;
            }
        }

        /// <summary>
        /// Start a new search using the active engine.
        /// </summary>
        public async Task<ActiveSearchContext> StartSearchAsync(SearchCriteria criteria, MotelyJsonConfig config)
        {
            DebugLogger.Log("SearchManager", $"Starting search via {_currentEngine.Name} for: {config.Name}");

            // Update config with Deck/Stake if provided in criteria (since these are filter properties)
            if (!string.IsNullOrEmpty(criteria.Deck))
            {
                config.Deck = criteria.Deck;
            }
            if (!string.IsNullOrEmpty(criteria.Stake))
            {
                config.Stake = criteria.Stake;
            }

            var options = new SearchOptionsDto
            {
                ThreadCount = criteria.ThreadCount,
                BatchSize = criteria.BatchSize,
                StartBatch = (long)criteria.StartBatch,
                EndBatch = (long)criteria.EndBatch,
                SpecificSeed = criteria.DebugSeed,
                // EnableDebug is not in SearchOptionsDto, handled by engine logging
                // Deck/Stake are in config, not options
            };

            // Delegate to Engine
            // Note: Engines return a SearchID string.
            // We need to wrap this in an ActiveSearchContext to maintain API compatibility with the UI.
            string searchId = await _currentEngine.StartSearchAsync(config, options);
            
            // Create a wrapper context that proxies calls to the engine if needed
            // For LocalEngine, it already launched the Orchestrator internally.
            // For RemoteEngine, we need polling logic (omitted for brevity in this refactor step).
            
            // HACK: For now, if it's local, we grab the orchestrator context via side-channel or assume
            // LocalEngine returns a dummy ID and we rely on legacy behavior for local.
            
            if (_currentEngine is LocalSearchEngine localEngine)
            {
                // LocalEngine implementation above was simplified. 
                // In reality, we should keep the legacy logic for Local until full migration.
                // Reverting to legacy logic for Local to ensure stability:
                return StartSearchLegacy(criteria, config);
            }
            
            // Remote Context Placeholder - using new constructor
            var context = new ActiveSearchContext(searchId, config);
            _activeSearches[searchId] = context;
            return context;
        }

        // Legacy synchronous method kept for compatibility
        public ActiveSearchContext StartSearch(SearchCriteria criteria, MotelyJsonConfig config)
        {
            return StartSearchLegacy(criteria, config);
        }

        private ActiveSearchContext StartSearchLegacy(SearchCriteria criteria, MotelyJsonConfig config)
        {
            // ... (Original Implementation for Local Execution) ...
            // Copy-pasting the original logic here for safety
            
            var searchParams = new JsonSearchParams
            {
                Threads = criteria.ThreadCount,
                BatchSize = criteria.BatchSize,
                StartBatch = criteria.StartBatch,
                EndBatch = criteria.EndBatch,
                SpecificSeed = criteria.DebugSeed,
                EnableDebug = criteria.EnableDebugOutput,
                Deck = criteria.Deck,
                Stake = criteria.Stake,
            };

            var useInMemoryStorage = !_platformServices.SupportsFileSystem;
            ActiveSearchContext? contextRef = null;

            searchParams.ResultCallback = result =>
            {
                var searchResult = new Models.SearchResult
                {
                    Seed = result.Seed,
                    TotalScore = result.Score,
                    Scores = result.TallyColumns?.ToArray() ?? Array.Empty<int>()
                };
                contextRef?.RaiseResultFound(searchResult);
            };

            var motelyContext = MotelySearchOrchestrator.LaunchWithContext(
                config,
                searchParams,
                useInMemoryStorage);

            var context = new ActiveSearchContext(motelyContext, config);
            contextRef = context;
            _activeSearches[context.SearchId] = context;

            return context;
        }

    /// <summary>
    /// Get an existing search by ID
    /// </summary>
    public ActiveSearchContext? GetSearch(string searchId)
    {
        _activeSearches.TryGetValue(searchId, out var context);
        return context;
    }

    /// <summary>
    /// Get all active searches
    /// </summary>
    public IEnumerable<ActiveSearchContext> GetAllSearches()
    {
        return _activeSearches.Values;
    }

    /// <summary>
    /// Remove a search from tracking (after completion/cancellation)
    /// </summary>
    public void RemoveSearch(string searchId)
    {
        if (_activeSearches.TryRemove(searchId, out var context))
        {
            DebugLogger.Log("SearchManager", $"Removed search: {searchId}");
        }
    }

    /// <summary>
    /// Get or restore a search by ID
    /// </summary>
    public ActiveSearchContext? GetOrRestoreSearch(string searchId)
    {
        // For now, just return the active search if it exists
        // Future: could implement persistence/restoration
        return GetSearch(searchId);
    }

    /// <summary>
    /// Initialize the SequentialLibrary for search persistence.
    /// Call this at app startup (Desktop only). Motely.DB owns the implementation.
    /// </summary>
    public void InitializeLibrary(string seedsPath)
    {
        if (!_platformServices.SupportsFileSystem)
        {
            DebugLogger.Log("SearchManager", "Skipping library initialization (browser)");
            return;
        }

        try
        {
            var initializer = ServiceHelper.GetService<ISequentialLibraryInitializer>();
            if (initializer != null)
            {
                initializer.SetLibraryRoot(seedsPath);
                DebugLogger.Log("SearchManager", $"SequentialLibrary initialized at: {seedsPath}");
            }
            else
            {
                DebugLogger.Log("SearchManager", "SequentialLibrary not available (browser build)");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Failed to initialize library: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore active searches from the SequentialLibrary.
    /// Returns metadata for searches that need UI widgets created.
    /// Call this at app startup after InitializeLibrary. Motely.DB owns the implementation.
    /// </summary>
    public async Task<List<RestoredSearchInfo>> RestoreActiveSearchesAsync(string jamlFiltersDir)
    {
        var restored = new List<RestoredSearchInfo>();

        if (!_platformServices.SupportsFileSystem)
        {
            DebugLogger.Log("SearchManager", "Skipping search restoration (browser)");
            return restored;
        }

        try
        {
            var provider = ServiceHelper.GetService<IRestoreActiveSearchesProvider>();
            if (provider == null)
            {
                DebugLogger.Log("SearchManager", "RestoreActiveSearchesProvider not available (browser build)");
                return restored;
            }

            restored = await provider.RestoreAsync(jamlFiltersDir).ConfigureAwait(false);
            DebugLogger.Log("SearchManager", $"Found {restored.Count} active searches to restore");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Error during search restoration: {ex.Message}");
        }

        return restored;
    }

    /// <summary>
    /// Resume a restored search. Call this after UI is ready.
    /// </summary>
    public ActiveSearchContext? ResumeSearch(RestoredSearchInfo info, int threads)
    {
        try
        {
            if (info.Config is null)
                return null;

            var criteria = new SearchCriteria
            {
                ThreadCount = threads,
                BatchSize = 1000,
                Deck = info.Deck ?? "Red",
                Stake = info.Stake ?? "White",
            };

            // Calculate StartBatch from LastSeed if available
            if (!string.IsNullOrEmpty(info.LastSeed))
            {
                // Use SeedMath to convert seed to batch index
                // BatchSize in SearchCriteria is seed digits (1-7), default 3
                criteria.StartBatch = (ulong)SeedMath.SeedToBatchIndex(info.LastSeed, criteria.BatchSize) + 1;
            }

            var context = StartSearch(criteria, info.Config);
            DebugLogger.Log("SearchManager", $"Resumed search: {info.SearchId}");
            return context;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SearchManager", $"Failed to resume search {info.SearchId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Stop all active searches
    /// </summary>
    public void StopAllSearches()
    {
        foreach (var context in _activeSearches.Values.ToList())
        {
            try
            {
                context.Stop();
                context.Dispose();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchManager", $"Error stopping search {context.SearchId}: {ex.Message}");
            }
        }
        _activeSearches.Clear();
        DebugLogger.Log("SearchManager", "All searches stopped");
    }

    /// <summary>
    /// Stop all searches for a specific filter
    /// </summary>
    /// <returns>Number of searches stopped</returns>
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
                try
                {
                    context.Stop();
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("SearchManager", $"Error stopping search {searchId}: {ex.Message}");
                }
            }
        }
        DebugLogger.Log("SearchManager", $"Stopped {toRemove.Count} searches for filter: {filterId}");
        return toRemove.Count;
    }

    /// <summary>
    /// Run a quick search for validation/testing purposes
    /// </summary>
    public async Task<QuickSearchResult> RunQuickSearchAsync(SearchCriteria criteria, MotelyJsonConfig config)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var context = StartSearch(criteria, config);
            
            // Start the search
            context.Start();
            
            // Wait for completion or timeout (quick searches should be fast)
            var maxWait = TimeSpan.FromSeconds(30);
            var waited = TimeSpan.Zero;
            var pollInterval = TimeSpan.FromMilliseconds(100);
            
            while (context.IsRunning && waited < maxWait)
            {
                await Task.Delay(pollInterval);
                waited += pollInterval;
            }
            
            // Get results
            var results = await context.GetResultsPageAsync(0, 1000);
            var elapsed = DateTime.UtcNow - startTime;
            
            // Clean up
            if (context.IsRunning)
            {
                context.Stop();
            }
            RemoveSearch(context.SearchId);
            context.Dispose();
            
            return new QuickSearchResult
            {
                Success = true,
                Count = results.Count,
                Seeds = results.Select(r => r.Seed).ToList(),
                Results = results,
                ElapsedTime = elapsed.TotalSeconds
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
                Error = ex.Message
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

/// <summary>
/// Information about a restored search that needs UI widgets created.
/// </summary>
public class RestoredSearchInfo
{
    public string SearchId { get; set; } = "";
    public string FilterName { get; set; } = "";
    public string Deck { get; set; } = "Red";
    public string Stake { get; set; } = "White";
    public string? LastSeed { get; set; }
    public long TotalSeedsProcessed { get; set; }
    public long TotalMatches { get; set; }
    public MotelyJsonConfig Config { get; set; } = null!;
}
