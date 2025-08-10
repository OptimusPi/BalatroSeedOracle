using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Oracle.Helpers;

namespace Oracle.Services
{
    /// <summary>
    /// Manages multiple concurrent search instances
    /// </summary>
    public class SearchManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, SearchInstance> _activeSearches;
        private readonly SearchHistoryService _historyService;

        public SearchManager(SearchHistoryService historyService)
        {
            _activeSearches = new ConcurrentDictionary<string, SearchInstance>();
            _historyService = historyService;
        }

        /// <summary>
        /// Creates a new search instance
        /// </summary>
        /// <returns>The unique ID of the created search</returns>
        public string CreateSearch()
        {
            var searchId = Guid.NewGuid().ToString();
            var searchInstance = new SearchInstance(searchId, _historyService);

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
        /// Stops all active searches
        /// </summary>
        public void StopAllSearches()
        {
            foreach (var search in GetActiveSearches())
            {
                search.StopSearch();
            }
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
}
