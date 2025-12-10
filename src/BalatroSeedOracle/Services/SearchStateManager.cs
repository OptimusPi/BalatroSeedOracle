#if !BROWSER
using System;
using System.IO;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using DuckDB.NET.Data;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages search state persistence to DuckDB for resume functionality
    /// </summary>
    public static class SearchStateManager
    {
        /// <summary>
        /// Load search state from the filter's DuckDB database
        /// </summary>
        /// <param name="filterDbPath">Path to the filter's .db file</param>
        /// <returns>SearchState if found, null otherwise</returns>
        public static SearchState? LoadSearchState(string filterDbPath)
        {
            try
            {
                if (string.IsNullOrEmpty(filterDbPath) || !File.Exists(filterDbPath))
                {
                    DebugLogger.Log(
                        "SearchStateManager",
                        $"Database file not found: {filterDbPath}"
                    );
                    return null;
                }

                using var connection = new DuckDBConnection($"Data Source={filterDbPath}");
                connection.Open();

                // Ensure table exists
                EnsureSearchStateTableExists(connection);

                using var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    SELECT id, deck_index, stake_index, batch_size, last_completed_batch,
                           search_mode, wordlist_name, updated_at
                    FROM search_state
                    WHERE id = 1
                ";

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new SearchState
                    {
                        Id = reader.GetInt32(0),
                        DeckIndex = reader.GetInt32(1),
                        StakeIndex = reader.GetInt32(2),
                        BatchSize = reader.GetInt32(3),
                        LastCompletedBatch = reader.GetInt32(4),
                        SearchMode = reader.GetInt32(5),
                        WordListName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        UpdatedAt = reader.GetDateTime(7),
                    };
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
        public static void SaveSearchState(string filterDbPath, SearchState state)
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

                using var connection = new DuckDBConnection($"Data Source={filterDbPath}");
                connection.Open();

                // Ensure table exists
                EnsureSearchStateTableExists(connection);

                using var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    INSERT INTO search_state (id, deck_index, stake_index, batch_size, last_completed_batch,
                                             search_mode, wordlist_name, updated_at)
                    VALUES (1, ?, ?, ?, ?, ?, ?, ?)
                    ON CONFLICT (id) DO UPDATE SET
                        deck_index = excluded.deck_index,
                        stake_index = excluded.stake_index,
                        batch_size = excluded.batch_size,
                        last_completed_batch = excluded.last_completed_batch,
                        search_mode = excluded.search_mode,
                        wordlist_name = excluded.wordlist_name,
                        updated_at = excluded.updated_at
                ";

                command.Parameters.Add(new DuckDBParameter(state.DeckIndex));
                command.Parameters.Add(new DuckDBParameter(state.StakeIndex));
                command.Parameters.Add(new DuckDBParameter(state.BatchSize));
                command.Parameters.Add(new DuckDBParameter(state.LastCompletedBatch));
                command.Parameters.Add(new DuckDBParameter(state.SearchMode));
                command.Parameters.Add(
                    new DuckDBParameter(state.WordListName ?? (object)DBNull.Value)
                );
                command.Parameters.Add(new DuckDBParameter(state.UpdatedAt));

                command.ExecuteNonQuery();
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
        private static void EnsureSearchStateTableExists(DuckDBConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS search_state (
                    id INTEGER PRIMARY KEY,
                    deck_index INTEGER,
                    stake_index INTEGER,
                    batch_size INTEGER,
                    last_completed_batch INTEGER,
                    search_mode INTEGER,
                    wordlist_name TEXT,
                    updated_at TIMESTAMP
                )
            ";
            command.ExecuteNonQuery();
        }
    }
}
#else
// Browser stub - search state management not available
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    public static class SearchStateManager
    {
        public static void Initialize() { }
        public static void SaveSearchState(string dbPath, SearchState state) { }
        public static SearchState? LoadSearchState(string dbPath) => null;
        public static void ClearSearchState(string configPath) { }
    }
}
#endif
