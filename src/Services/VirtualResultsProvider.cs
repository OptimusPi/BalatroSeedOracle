using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Provides virtual scrolling support for search results from DuckDB
    /// </summary>
    public class VirtualResultsProvider
    {
        private readonly SearchHistoryService _historyService;
        private int _totalCount;
        private DateTime _lastCountUpdate = DateTime.MinValue;

        public VirtualResultsProvider(SearchHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <summary>
        /// Get a page of results from the database
        /// </summary>
        public async Task<IEnumerable<SearchResult>> GetPageAsync(int offset, int pageSize)
        {
            // Query only what's needed for display
            return await _historyService.GetResultsPageAsync(offset, pageSize);
        }

        /// <summary>
        /// Get the total count of results (cached for performance)
        /// </summary>
        public async Task<int> GetCountAsync()
        {
            // Cache count for 100ms to avoid hammering the DB
            if (DateTime.UtcNow - _lastCountUpdate > TimeSpan.FromMilliseconds(100))
            {
                _totalCount = await _historyService.GetResultCountAsync();
                _lastCountUpdate = DateTime.UtcNow;
            }
            return _totalCount;
        }

        /// <summary>
        /// Force refresh the count cache
        /// </summary>
        public async Task RefreshCountAsync()
        {
            _totalCount = await _historyService.GetResultCountAsync();
            _lastCountUpdate = DateTime.UtcNow;
        }

        public int CachedCount => _totalCount;
    }
}