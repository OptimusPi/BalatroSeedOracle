using System;
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

        #region Static Compatibility Methods (for legacy code)

        /// <summary>
        /// Static method for backward compatibility. Use LoadSearchStateAsync for new code.
        /// </summary>
        public static SearchState? LoadSearchState(string filterDbPath)
        {
#if BROWSER
            // Browser: Search functionality not available, return null
            return null;
#else
            try
            {
                var instance = ServiceHelper.GetService<SearchStateManager>();
                if (instance == null)
                {
                    DebugLogger.LogError("SearchStateManager", "Service not available");
                    return null;
                }
                return instance.LoadSearchStateAsync(filterDbPath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchStateManager", $"Static LoadSearchState failed: {ex.Message}");
                return null;
            }
#endif
        }

        /// <summary>
        /// Static method for backward compatibility. Use SaveSearchStateAsync for new code.
        /// </summary>
        public static void SaveSearchState(string filterDbPath, SearchState state)
        {
#if BROWSER
            // Browser: Search functionality not available, no-op
            return;
#else
            try
            {
                var instance = ServiceHelper.GetService<SearchStateManager>();
                if (instance == null)
                {
                    DebugLogger.LogError("SearchStateManager", "Service not available");
                    return;
                }
                instance.SaveSearchStateAsync(filterDbPath, state).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchStateManager", $"Static SaveSearchState failed: {ex.Message}");
            }
#endif
        }

        #endregion

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

                var results = await connection.ExecuteReaderAsync(
                    @"SELECT id, deck_index, stake_index, batch_size, last_completed_batch,
                             search_mode, wordlist_name, updated_at
                      FROM search_state
                      WHERE id = 1",
                    reader => new SearchState
                    {
                        Id = reader.GetInt32(0),
                        DeckIndex = reader.GetInt32(1),
                        StakeIndex = reader.GetInt32(2),
                        BatchSize = reader.GetInt32(3),
                        LastCompletedBatch = reader.GetInt32(4),
                        SearchMode = reader.GetInt32(5),
                        WordListName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        UpdatedAt = DateTime.Parse(reader.GetString(7)),
                    });

                foreach (var state in results)
                {
                    return state; // Return first result
                }

                return null;
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

                // Use INSERT OR REPLACE pattern
                var wordlistValue = state.WordListName != null ? $"'{state.WordListName}'" : "NULL";
                var sql = $@"
                    INSERT INTO search_state (id, deck_index, stake_index, batch_size, last_completed_batch,
                                             search_mode, wordlist_name, updated_at)
                    VALUES (1, {state.DeckIndex}, {state.StakeIndex}, {state.BatchSize}, {state.LastCompletedBatch},
                            {state.SearchMode}, {wordlistValue}, '{state.UpdatedAt:yyyy-MM-dd HH:mm:ss}')
                    ON CONFLICT (id) DO UPDATE SET
                        deck_index = excluded.deck_index,
                        stake_index = excluded.stake_index,
                        batch_size = excluded.batch_size,
                        last_completed_batch = excluded.last_completed_batch,
                        search_mode = excluded.search_mode,
                        wordlist_name = excluded.wordlist_name,
                        updated_at = excluded.updated_at
                ";

                await connection.ExecuteNonQueryAsync(sql);
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
            // Note: BalatroSeedOracle's search_state has additional columns (deck_index, stake_index, etc.)
            // that Motely's schema doesn't include. For now, keep inline but could extend DuckDBSchema if needed.
            // Using Motely's base schema and adding BalatroSeedOracle-specific columns
            var baseSchema = DuckDBSchema.SearchStateTableSchema();
            await connection.ExecuteNonQueryAsync(baseSchema);
            
            // Add BalatroSeedOracle-specific columns if they don't exist
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
