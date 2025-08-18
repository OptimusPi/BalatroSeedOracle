using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Helpers;

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
        /// <param name="filterName">Optional filter name for better file naming</param>
        /// <returns>The unique ID of the created search</returns>
        public string CreateSearch(string? filterName = null)
        {
            var searchId = Guid.NewGuid().ToString();
            
            // Create a meaningful filename if filter name provided
            string dbPath;
            if (!string.IsNullOrWhiteSpace(filterName))
            {
                dbPath = FilterNameNormalizer.CreateDuckDbFilename(filterName, searchId);
                DebugLogger.Log("SearchManager", $"Using filter-based filename: {dbPath}");
            }
            else
            {
                // Fallback to GUID-based name
                var searchResultsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SearchResults");
                System.IO.Directory.CreateDirectory(searchResultsDir);
                dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchId}.duckdb");
            }
            
            var searchInstance = new SearchInstance(searchId, dbPath);

            if (_activeSearches.TryAdd(searchId, searchInstance))
            {
                DebugLogger.Log("SearchManager", $"Created new search instance: {searchId} at {dbPath}");
                return searchId;
            }

            throw new InvalidOperationException("Failed to create search instance");
        }
        
        /// <summary>
        /// Resumes an existing search from a DuckDB file
        /// </summary>
        /// <param name="dbPath">Path to the existing DuckDB file</param>
        /// <returns>The search ID if resume successful, null otherwise</returns>
        public string? ResumeSearch(string dbPath)
        {
            if (!System.IO.File.Exists(dbPath))
            {
                DebugLogger.LogError("SearchManager", $"Cannot resume - file not found: {dbPath}");
                return null;
            }
            
            try
            {
                var searchId = Guid.NewGuid().ToString(); // New ID for this session
                var searchInstance = new SearchInstance(searchId, dbPath, isResume: true);
                
                if (_activeSearches.TryAdd(searchId, searchInstance))
                {
                    DebugLogger.Log("SearchManager", $"Resumed search from: {dbPath} with new ID: {searchId}");
                    
                    // Load the last processed batch from the database
                    var lastBatch = searchInstance.GetLastProcessedBatch();
                    if (lastBatch != null)
                    {
                        DebugLogger.Log("SearchManager", $"Resuming from batch: {lastBatch}");
                    }
                    
                    return searchId;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchManager", $"Failed to resume search: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Finds existing search databases for a filter
        /// </summary>
        /// <param name="filterName">The filter name to search for</param>
        /// <returns>Array of matching DuckDB file paths</returns>
        public string[] FindExistingSearches(string filterName)
        {
            return FilterNameNormalizer.FindMatchingDuckDbFiles(filterName);
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
