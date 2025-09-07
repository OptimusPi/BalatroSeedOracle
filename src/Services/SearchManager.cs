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


        public SearchManager()
        {
            _activeSearches = new ConcurrentDictionary<string, SearchInstance>();
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
    
            // Preallocate a database file path so SearchInstance always has a connection string
            var searchResultsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SearchResults");
            System.IO.Directory.CreateDirectory(searchResultsDir);
            var dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchId}.db");
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
        /// Gets an existing search instance
        /// </summary>
        /// <param name="searchId">The ID of the search to retrieve</param>
        /// <returns>The search instance if found, null otherwise</returns>
        public SearchInstance? GetSearch(string filterNameNormalized, string deckName, string stakeName)
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
        /// Starts a new search with the given criteria and config
        /// </summary>
        public async Task<SearchInstance> StartSearchAsync(SearchCriteria criteria, Motely.Filters.MotelyJsonConfig config)
        {
            var filterId = config.Name?.Replace(" ", "_") ?? "unknown";
            var searchId = $"{filterId}_{criteria.Deck}_{criteria.Stake}";
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
