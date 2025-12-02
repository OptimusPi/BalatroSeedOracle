using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Views.Modals;
using Motely.Filters;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages multiple concurrent search instances
    /// </summary>
    public class SearchManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, SearchInstance> _activeSearches;
        private readonly EventFXService? _eventFXService;

        public SearchManager()
        {
            _activeSearches = new ConcurrentDictionary<string, SearchInstance>();
            _eventFXService = ServiceHelper.GetService<EventFXService>();
        }

        /// <summary>
        /// Creates a new search instance
        /// </summary>
        /// <param name="filterNameNormalized">The normalized filter name</param>
        /// <param name="deckName">The deck name</param>
        /// <param name="stakeName">The stake name</param>
        /// <returns>The unique ID of the created search</returns>
        /// <remark>Search results may be invalidated by filter columns, deck and stake selection.</remarks>
        public string CreateSearch(string filterNameNormalized, string deckName, string stakeName)
        {
            var searchId = $"{filterNameNormalized}_{deckName}_{stakeName}";

            // REUSE existing search instance if it exists - preserves the fertilizer pile!
            if (_activeSearches.TryGetValue(searchId, out var existingSearch))
            {
                DebugLogger.Log("SearchManager", $"Reusing existing search instance: {searchId}");
                return searchId;
            }

            // Create new search instance only if one doesn't exist
            var searchResultsDir = AppPaths.SearchResultsDir;
            var dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchId}.db");
            var searchInstance = new SearchInstance(searchId, dbPath);
            WireEventFXToSearchInstance(searchInstance);

            if (_activeSearches.TryAdd(searchId, searchInstance))
            {
                DebugLogger.Log("SearchManager", $"Created new search instance: {searchId}");
                return searchId;
            }

            throw new InvalidOperationException("Failed to create search instance");
        }

        /// <summary>
        /// Gets an existing search instance
        /// </summary>
        /// <param name="searchId">The ID of the search to retrieve</param>
        /// <returns>The search instance if found, null otherwise</returns>
        public SearchInstance? GetSearch(string searchId)
        {
            if (_activeSearches.TryGetValue(searchId, out var search))
            {
                return search;
            }
            return null;
        }

        /// <summary>
        /// Gets an existing search instance
        /// </summary>
        /// <param name="searchId">The ID of the search to retrieve</param>
        /// <returns>The search instance if found, null otherwise</returns>
        public SearchInstance? GetSearch(
            string filterNameNormalized,
            string deckName,
            string stakeName
        )
        {
            var searchId = $"{filterNameNormalized}_{deckName}_{stakeName}";
            return GetSearch(searchId);
        }

        /// <summary>
        /// Removes a search instance
        /// </summary>
        /// <param name="searchId">The ID of the search to remove</param>
        /// <returns>True if the search was removed, false otherwise</returns>
        public bool RemoveSearch(string searchId)
        {
            if (_activeSearches.TryRemove(searchId, out var search))
            {
                search.Dispose();
                DebugLogger.Log("SearchManager", $"Removed search instance: {searchId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all active search instances
        /// </summary>
        /// <returns>Collection of active searches</returns>
        public IEnumerable<SearchInstance> GetActiveSearches()
        {
            return _activeSearches.Values.Where(s => s.IsRunning);
        }

        /// <summary>
        /// Gets all search instances (active and completed)
        /// </summary>
        /// <returns>Collection of all searches</returns>
        public IEnumerable<SearchInstance> GetAllSearches()
        {
            return _activeSearches.Values;
        }

        /// <summary>
        /// Gets or restores a SearchInstance from an existing database file.
        /// If the SearchInstance isn't in memory but the DB file exists, creates it.
        /// </summary>
        /// <param name="searchInstanceId">The SearchInstance ID to get or restore</param>
        /// <returns>The SearchInstance if found/restored, null if DB file doesn't exist</returns>
        public SearchInstance? GetOrRestoreSearch(string searchInstanceId)
        {
            // First check if already in memory
            if (_activeSearches.TryGetValue(searchInstanceId, out var existingSearch))
            {
                return existingSearch;
            }

            // Check if database file exists
            var searchResultsDir = AppPaths.SearchResultsDir;
            var dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchInstanceId}.db");

            if (!System.IO.File.Exists(dbPath))
            {
                DebugLogger.Log(
                    "SearchManager",
                    $"Cannot restore search '{searchInstanceId}' - database file not found"
                );
                return null;
            }

            // Database exists, create SearchInstance to reconnect to it
            try
            {
                var searchInstance = new SearchInstance(searchInstanceId, dbPath);
                WireEventFXToSearchInstance(searchInstance);

                if (_activeSearches.TryAdd(searchInstanceId, searchInstance))
                {
                    DebugLogger.Log(
                        "SearchManager",
                        $"Restored search instance from database: {searchInstanceId}"
                    );
                    return searchInstance;
                }

                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchManager",
                    $"Failed to restore search '{searchInstanceId}': {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Starts a new search with the given criteria and config
        /// </summary>
        public async Task<SearchInstance> StartSearchAsync(
            SearchCriteria criteria,
            Motely.Filters.MotelyJsonConfig config
        )
        {
            DebugLogger.Log("SearchManager", $"StartSearchAsync called for filter: {config.Name}");
            DebugLogger.Log("SearchManager", $"  ConfigPath: {criteria.ConfigPath}");
            DebugLogger.Log("SearchManager", $"  ThreadCount: {criteria.ThreadCount}");
            DebugLogger.Log("SearchManager", $"  BatchSize: {criteria.BatchSize}");

            var filterId = config.Name?.Replace(" ", "_") ?? "unknown";

            // First create the search instance
            DebugLogger.Log(
                "SearchManager",
                $"Creating search instance with ID: {filterId}_{criteria.Deck}_{criteria.Stake}"
            );
            var createdSearchId = CreateSearch(
                filterId,
                criteria.Deck ?? "Red",
                criteria.Stake ?? "White"
            );
            var searchInstance = GetSearch(createdSearchId);

            if (searchInstance == null)
            {
                var errorMsg = $"Failed to create search instance for filter '{config.Name}'";
                DebugLogger.LogError("SearchManager", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            DebugLogger.Log("SearchManager", $"Search instance created: {createdSearchId}");
            DebugLogger.Log(
                "SearchManager",
                $"Starting search with config path: {criteria.ConfigPath}"
            );

            // Start the search with just criteria - config is handled separately
            await searchInstance.StartSearchAsync(criteria);

            DebugLogger.Log("SearchManager", $"Search started successfully!");
            return searchInstance;
        }

        /// <summary>
        /// Stops all active searches
        /// </summary>
        public void StopAllSearches()
        {
            foreach (var search in GetActiveSearches())
            {
                search.StopSearch();
            }
        }

        /// <summary>
        /// CRITICAL: Stop and remove all searches for a specific filter
        /// Called when filter is edited to prevent stale database corruption
        /// </summary>
        /// <param name="filterName">The filter name to stop searches for</param>
        /// <returns>Number of searches stopped</returns>
        public int StopSearchesForFilter(string filterName)
        {
            var stoppedCount = 0;
            var searchesToRemove = new List<string>();

            DebugLogger.Log("SearchManager", $"🛑 Stopping searches for filter: {filterName}");

            // Find all searches that use this filter (searchId format: {filterName}_{deck}_{stake})
            foreach (var kvp in _activeSearches)
            {
                var searchId = kvp.Key;
                var searchInstance = kvp.Value;

                if (searchId.StartsWith($"{filterName}_"))
                {
                    try
                    {
                        DebugLogger.Log("SearchManager", $"🛑 Stopping search: {searchId}");

                        searchInstance.StopSearch();
                        searchInstance.Dispose();

                        searchesToRemove.Add(searchId);
                        stoppedCount++;

                        DebugLogger.Log("SearchManager", $"✅ Stopped search: {searchId}");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "SearchManager",
                            $"Error stopping search {searchId}: {ex.Message}"
                        );
                    }
                }
            }

            // Remove stopped searches from active collection
            foreach (var searchId in searchesToRemove)
            {
                _activeSearches.TryRemove(searchId, out _);
            }

            DebugLogger.Log(
                "SearchManager",
                $"🧹 Filter cleanup complete - stopped {stoppedCount} searches"
            );
            return stoppedCount;
        }

        /// <summary>
        /// Runs a quick synchronous search for testing filters
        /// Returns results directly instead of fire-and-forget
        /// Uses the in-memory config overload - no file I/O required!
        /// </summary>
        public async Task<QuickSearchResults> RunQuickSearchAsync(
            SearchCriteria criteria,
            MotelyJsonConfig config
        )
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var seeds = new List<string>();
            var fullResults = new List<Models.SearchResult>();
            var batchesChecked = 0;

            try
            {
                DebugLogger.Log(
                    "SearchManager",
                    $"Starting quick search with BatchSize={criteria.BatchSize}, MaxResults={criteria.MaxResults}"
                );

                // Ensure MaxResults is set to limit search time
                var maxResults = criteria.MaxResults > 0 ? criteria.MaxResults : 10;

                // Create a temporary search instance for the quick test
                var tempSearchId = $"QuickTest_{Guid.NewGuid():N}";
                var tempDbPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"{tempSearchId}.db"
                );

                SearchInstance? searchInstance = null;

                try
                {
                    searchInstance = new SearchInstance(tempSearchId, tempDbPath);

                    // Start the search with the in-memory config (no file I/O!)
                    var searchTask = searchInstance.StartSearchAsync(criteria, config);

                    // Wait for either completion or max results
                    var timeoutTask = Task.Delay(5000); // 5 second timeout for quick test
                    var completedTask = await Task.WhenAny(searchTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        DebugLogger.Log("SearchManager", "Quick search timed out after 5 seconds");
                        searchInstance.StopSearch();
                    }

                    // Get results from the database
                    if (searchInstance.IsDatabaseInitialized)
                    {
                        var results = await searchInstance.GetTopResultsAsync(
                            "score",
                            false,
                            maxResults
                        );
                        fullResults = results; // Store full results with TotalScore
                        seeds = results.Select(r => r.Seed).ToList();
                        DebugLogger.Log("SearchManager", $"Quick search found {seeds.Count} seeds");
                    }
                }
                finally
                {
                    // Clean up the temporary search instance
                    searchInstance?.Dispose();

                    // Delete the temporary database file
                    try
                    {
                        if (System.IO.File.Exists(tempDbPath))
                        {
                            System.IO.File.Delete(tempDbPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(
                            "SearchManager",
                            $"Failed to delete temp DB: {ex.Message}"
                        );
                    }
                }

                stopwatch.Stop();

                return new QuickSearchResults
                {
                    Seeds = seeds,
                    Results = fullResults,
                    Count = seeds.Count,
                    BatchesChecked = batchesChecked,
                    ElapsedTime = stopwatch.Elapsed.TotalSeconds,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                DebugLogger.LogError("SearchManager", $"Quick search failed: {ex.Message}");
                return new QuickSearchResults
                {
                    Seeds = new List<string>(),
                    Results = new List<Models.SearchResult>(),
                    Count = 0,
                    BatchesChecked = batchesChecked,
                    ElapsedTime = stopwatch.Elapsed.TotalSeconds,
                    Success = false,
                    Error = ex.Message,
                };
            }
        }

        private void WireEventFXToSearchInstance(SearchInstance instance)
        {
            if (_eventFXService == null)
                return;

            instance.SearchStarted += (s, e) =>
            {
                _eventFXService.TriggerEvent(EventFXType.SearchInstanceStart);
            };

            instance.NewHighScoreFound += (s, score) =>
            {
                _eventFXService.TriggerEvent(EventFXType.SearchInstanceFind);
            };
        }

        public void Dispose()
        {
            StopAllSearches();

            foreach (var search in _activeSearches.Values)
            {
                search.Dispose();
            }

            _activeSearches.Clear();
        }
    }

    /// <summary>
    /// Results from a quick test search
    /// </summary>
    public class QuickSearchResults
    {
        public List<string> Seeds { get; set; } = new();
        public List<Models.SearchResult> Results { get; set; } = new();
        public int Count { get; set; }
        public int BatchesChecked { get; set; }
        public double ElapsedTime { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
