using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.DuckDB;
using Motely.DuckDB;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages search state persistence to DuckDB for resume functionality.
    /// Uses IDuckDBService abstraction for cross-platform compatibility.
    /// </summary>
    public class SearchStateManager
    {
        private readonly IDuckDBService _duckDB;

        public SearchStateManager(IDuckDBService duckDB)
        {
            _duckDB = duckDB ?? throw new ArgumentNullException(nameof(duckDB));
        }

        // Static blocking methods removed - all callers now use async versions

        /// <summary>
        /// Load search state from the filter's DuckDB database
        /// </summary>
        /// <param name="filterDbPath">Path to the filter's .db file</param>
        /// <returns>SearchState if found, null otherwise</returns>
        public async Task<SearchState?> LoadSearchStateAsync(string filterDbPath)
        {
            try
            {
#if !BROWSER
                if (string.IsNullOrEmpty(filterDbPath) || !File.Exists(filterDbPath))
                {
                    DebugLogger.Log(
                        "SearchStateManager",
                        $"Database file not found: {filterDbPath}"
                    );
                    return null;
                }
#endif

                var connectionString = _duckDB.CreateConnectionString(filterDbPath);
                await using var connection = await _duckDB.OpenConnectionAsync(connectionString);

                // Ensure table exists
                await EnsureSearchStateTableExistsAsync(connection);

                // Use high-level method - no SQL construction in BSO!
                var row = await connection.LoadRowByIdAsync("search_state", "id", 1);
                if (row == null)
                    return null;

                // Convert dictionary to SearchState
                return new SearchState
                {
                    Id = row.TryGetValue("id", out var id) ? Convert.ToInt32(id) : 1,
                    DeckIndex = row.TryGetValue("deck_index", out var di) ? Convert.ToInt32(di) : 0,
                    StakeIndex = row.TryGetValue("stake_index", out var si) ? Convert.ToInt32(si) : 0,
                    BatchSize = row.TryGetValue("batch_size", out var bs) ? Convert.ToInt32(bs) : 3,
                    LastCompletedBatch = row.TryGetValue("last_completed_batch", out var lcb) ? Convert.ToInt32(lcb) : 0,
                    SearchMode = row.TryGetValue("search_mode", out var sm) ? Convert.ToInt32(sm) : 0,
                    WordListName = row.TryGetValue("wordlist_name", out var wln) ? wln?.ToString() : null,
                    UpdatedAt = row.TryGetValue("updated_at", out var ua) && ua != null 
                        ? DateTime.Parse(ua.ToString()!) 
                        : DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchStateManager",
                    $"Failed to load search state: {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Save search state to the filter's DuckDB database
        /// </summary>
        /// <param name="filterDbPath">Path to the filter's .db file</param>
        /// <param name="state">SearchState to save</param>
        public async Task SaveSearchStateAsync(string filterDbPath, SearchState state)
        {
            try
            {
                if (string.IsNullOrEmpty(filterDbPath))
                {
                    DebugLogger.LogError(
                        "SearchStateManager",
                        "Cannot save state: filter path is null or empty"
                    );
                    return;
                }

                var connectionString = _duckDB.CreateConnectionString(filterDbPath);
                await using var connection = await _duckDB.OpenConnectionAsync(connectionString);

                // Ensure table exists
                await EnsureSearchStateTableExistsAsync(connection);

                // Use high-level method - no SQL construction in BSO!
                var values = new Dictionary<string, object?>
                {
                    ["id"] = 1,
                    ["deck_index"] = state.DeckIndex,
                    ["stake_index"] = state.StakeIndex,
                    ["batch_size"] = state.BatchSize,
                    ["last_completed_batch"] = state.LastCompletedBatch,
                    ["search_mode"] = state.SearchMode,
                    ["wordlist_name"] = state.WordListName,
                    ["updated_at"] = state.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                await connection.UpsertRowAsync("search_state", values, "id");
                DebugLogger.Log(
                    "SearchStateManager",
                    $"Saved search state: Batch {state.LastCompletedBatch}, Mode {state.SearchMode}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchStateManager",
                    $"Failed to save search state: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Convert batch number from one batch size to another using 35^n formula
        /// Batch sizes: 0=35^1, 1=35^2, 2=35^3, 3=35^4
        /// </summary>
        /// <param name="batchNumber">Current batch number</param>
        /// <param name="fromSize">Current batch size (0-3)</param>
        /// <param name="toSize">Target batch size (0-3)</param>
        /// <returns>Converted batch number</returns>
        public static int ConvertBatchNumber(int batchNumber, int fromSize, int toSize)
        {
            try
            {
                // Calculate total seeds processed at old batch size
                long seedsProcessed = batchNumber * (long)Math.Pow(35, fromSize + 1);

                // Calculate equivalent batch number at new batch size
                long newBatchSize = (long)Math.Pow(35, toSize + 1);
                int newBatchNumber = (int)(seedsProcessed / newBatchSize);

                DebugLogger.Log(
                    "SearchStateManager",
                    $"Converted batch {batchNumber} (size {fromSize}) to batch {newBatchNumber} (size {toSize})"
                );

                return newBatchNumber;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchStateManager",
                    $"Failed to convert batch number: {ex.Message}"
                );
                return 0;
            }
        }

        /// <summary>
        /// Ensure the search_state table exists in the database
        /// </summary>
        private async Task EnsureSearchStateTableExistsAsync(IDuckDBConnection connection)
        {
            // Use Motely's schema - no SQL construction in BSO!
            var baseSchema = DuckDBSchema.SearchStateTableSchema();
            await connection.EnsureTableExistsAsync(baseSchema);
            
            // Add BalatroSeedOracle-specific columns if they don't exist
            // Note: ALTER TABLE still needs SQL, but this is schema migration, not business logic
            await connection.ExecuteNonQueryAsync(@"
                ALTER TABLE search_state ADD COLUMN IF NOT EXISTS deck_index INTEGER;
                ALTER TABLE search_state ADD COLUMN IF NOT EXISTS stake_index INTEGER;
                ALTER TABLE search_state ADD COLUMN IF NOT EXISTS search_mode INTEGER;
                ALTER TABLE search_state ADD COLUMN IF NOT EXISTS wordlist_name TEXT;
                ALTER TABLE search_state ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ");
        }
    }
}
