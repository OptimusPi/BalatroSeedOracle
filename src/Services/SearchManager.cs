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

        // Events for MVVM (temporarily unused - will be used when MVVM is fully integrated)
        #pragma warning disable CS0067
        public event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
        public event EventHandler<SearchResultEventArgs>? ResultFound;
        public event EventHandler? SearchCompleted;
        #pragma warning restore CS0067

        public SearchManager()
        {
            _activeSearches = new ConcurrentDictionary<string, SearchInstance>();
        }

        /// <summary>
        /// Creates a new search instance
        /// </summary>
        /// <returns>The unique ID of the created search</returns>
        public string CreateSearch()
        {
            var searchId = Guid.NewGuid().ToString();
            // Preallocate a database file path so SearchInstance always has a connection string
            var searchResultsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SearchResults");
            System.IO.Directory.CreateDirectory(searchResultsDir);
            var dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchId}.duckdb");
            var searchInstance = new SearchInstance(searchId, dbPath);

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
        /// Starts a new search with the given criteria and config
        /// </summary>
        public async Task<SearchInstance> StartSearchAsync(SearchCriteria criteria, MotelyJsonConfig config)
        {
            var searchId = CreateSearch();
            var searchInstance = GetSearch(searchId);
            
            if (searchInstance == null)
                throw new InvalidOperationException("Failed to create search instance");
            
            // Start the search with just criteria - config is handled separately
            await searchInstance.StartSearchAsync(criteria);
            
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
