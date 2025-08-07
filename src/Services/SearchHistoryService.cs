using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using DuckDB.NET.Data;
using Oracle.Models;
using Oracle.Helpers;

namespace Oracle.Services
{
    public class SearchHistoryService : IDisposable
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private DuckDBConnection? _connection;
        private readonly object _connectionLock = new object();
        private string _currentFilterName = "default";

        public SearchHistoryService()
        {
            // Don't create any database until a search actually starts
        }

        public void SetFilterName(string filterPath)
        {
            // Extract filter name from path
            if (!string.IsNullOrEmpty(filterPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filterPath);
                // Remove .ouija if present
                if (fileName.EndsWith(".ouija", StringComparison.OrdinalIgnoreCase))
                    fileName = fileName.Substring(0, fileName.Length - 6);
                _currentFilterName = fileName;
            }
            else
            {
                _currentFilterName = "default";
            }

            // Close existing connection if any
            lock (_connectionLock)
            {
                _connection?.Dispose();
                _connection = null;
            }

            // Set up new database path
            var searchResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
            Directory.CreateDirectory(searchResultsDir);


            _dbPath = Path.Combine(searchResultsDir, $"{_currentFilterName}.ouija.duckdb");
            _connectionString = $"Data Source={_dbPath}";


            DebugLogger.Log("SearchHistoryService", $"Database path set to: {_dbPath}");
            InitializeDatabase();
        }

        private DuckDBConnection GetConnection()
        {
            lock (_connectionLock)
            {
                if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection?.Dispose();
                    _connection = new DuckDBConnection(_connectionString);
                    _connection.Open();
                    DebugLogger.Log("SearchHistoryService", "Created new DuckDB connection");
                }
                return _connection;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                var connection = GetConnection();

                using var createSequences = connection.CreateCommand();
                createSequences.CommandText = @"
                    CREATE SEQUENCE IF NOT EXISTS serial_id_seq START WITH 1 INCREMENT BY 1;
                    CREATE SEQUENCE IF NOT EXISTS item_id_seq START WITH 1 INCREMENT BY 1;
                    CREATE SEQUENCE IF NOT EXISTS result_id_seq START WITH 1 INCREMENT BY 1;
                ";
                createSequences.ExecuteNonQuery();


                // Create searches table
                using var createSearchesTable = connection.CreateCommand();
                createSearchesTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS searches (
                        search_id INTEGER PRIMARY KEY DEFAULT nextval('serial_id_seq'),
                        config_path VARCHAR,
                        config_hash VARCHAR,
                        search_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        thread_count INTEGER,
                        min_score INTEGER,
                        batch_size INTEGER,
                        deck VARCHAR,
                        stake VARCHAR,
                        max_ante INTEGER,
                        total_seeds_searched BIGINT,
                        results_found INTEGER,
                        duration_seconds DOUBLE,
                        search_status VARCHAR DEFAULT 'running'
                    )
                ";
                createSearchesTable.ExecuteNonQuery();

                // Create results table
                using var createResultsTable = connection.CreateCommand();
                createResultsTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS search_results (
                        result_id INTEGER PRIMARY KEY DEFAULT nextval('result_id_seq'),
                        search_id INTEGER,
                        seed VARCHAR,
                        score INTEGER,
                        details TEXT,
                        ante INTEGER,
                        score_breakdown TEXT,
                        found_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (search_id) REFERENCES searches(search_id)
                    )
                ";
                createResultsTable.ExecuteNonQuery();

                // Create filter items table for search reconstruction
                using var createFilterTable = connection.CreateCommand();
                createFilterTable.CommandText = @"
                    CREATE TABLE IF NOT EXISTS filter_items (
                        item_id INTEGER PRIMARY KEY DEFAULT nextval('item_id_seq'),
                        search_id INTEGER,
                        filter_type VARCHAR,
                        item_type VARCHAR,
                        item_value VARCHAR,
                        edition VARCHAR,
                        score INTEGER,
                        antes TEXT,
                        FOREIGN KEY (search_id) REFERENCES searches(search_id)
                    )
                ";
                createFilterTable.ExecuteNonQuery();

                // Create indexes for better performance
                using var createIndexes = connection.CreateCommand();
                createIndexes.CommandText = @"
                    CREATE INDEX IF NOT EXISTS idx_search_date ON searches(search_date);
                    CREATE INDEX IF NOT EXISTS idx_seed ON search_results(seed);
                    CREATE INDEX IF NOT EXISTS idx_score ON search_results(score DESC);
                    CREATE INDEX IF NOT EXISTS idx_search_id ON search_results(search_id);
                    CREATE INDEX IF NOT EXISTS idx_config_hash ON searches(config_hash);
                    CREATE INDEX IF NOT EXISTS idx_filter_search ON filter_items(search_id);
                ";
                createIndexes.ExecuteNonQuery();





                DebugLogger.Log("SearchHistoryService", "Database initialized successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to initialize database: {ex.Message}");
            }
        }

        public void Dispose()
        {
            lock (_connectionLock)
            {
                _connection?.Dispose();
                _connection = null;
                DebugLogger.Log("SearchHistoryService", "Closed DuckDB connection");
            }
        }

        /// <summary>
        /// Import search results from a CSV file directly into DuckDB
        /// </summary>
        public async Task<int> ImportFromCsvAsync(long searchId, string csvPath)
        {
            try
            {
                if (!File.Exists(csvPath))
                {
                    DebugLogger.LogError("SearchHistoryService", $"CSV file not found: {csvPath}");
                    return 0;
                }

                var connection = GetConnection();

                // Create a temporary table to hold the CSV data
                using var createTempTable = connection.CreateCommand();
                createTempTable.CommandText = @"
                    CREATE TEMPORARY TABLE IF NOT EXISTS csv_import (
                        seed VARCHAR,
                        score INTEGER,
                        details VARCHAR
                    )
                ";
                await createTempTable.ExecuteNonQueryAsync();

                // Import CSV data
                using var importCmd = connection.CreateCommand();
                importCmd.CommandText = $@"
                    COPY csv_import FROM '{csvPath}' (FORMAT CSV, HEADER TRUE, DELIMITER ',')
                ";
                await importCmd.ExecuteNonQueryAsync();

                // Insert into search_results with the search_id
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO search_results (search_id, seed, score, details)
                    SELECT $1, seed, score, details FROM csv_import
                ";
                insertCmd.Parameters.Add(new DuckDBParameter("$1", searchId));
                var rowsInserted = await insertCmd.ExecuteNonQueryAsync();

                // Clean up temp table
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = "DROP TABLE csv_import";
                await dropCmd.ExecuteNonQueryAsync();

                DebugLogger.Log("SearchHistoryService", $"Imported {rowsInserted} results from CSV");
                return rowsInserted;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to import CSV: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Export search results to a CSV file
        /// </summary>
        public async Task<bool> ExportToCsvAsync(long searchId, string csvPath)
        {
            try
            {
                var connection = GetConnection();

                using var exportCmd = connection.CreateCommand();
                exportCmd.CommandText = $@"
                    COPY (
                        SELECT seed, score, details 
                        FROM search_results 
                        WHERE search_id = {searchId}
                        ORDER BY score DESC, seed
                    ) TO '{csvPath}' (FORMAT CSV, HEADER TRUE)
                ";
                await exportCmd.ExecuteNonQueryAsync();

                DebugLogger.Log("SearchHistoryService", $"Exported results to: {csvPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to export CSV: {ex.Message}");
                return false;
            }
        }

        public async Task<long> StartNewSearchAsync(string configPath, int threadCount, int minScore,
            int batchSize, string deck, string stake, int maxAnte = 39, string? configHash = null)
        {
            try
            {
                // Update filter name based on config path
                SetFilterName(configPath);


                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO searches (config_path, config_hash, thread_count, min_score, batch_size, 
                                          deck, stake, max_ante, results_found, search_status)
                    VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
                    RETURNING search_id
                ";

                cmd.Parameters.Add(new DuckDBParameter("$1", configPath));
                cmd.Parameters.Add(new DuckDBParameter("$2", configHash ?? ""));
                cmd.Parameters.Add(new DuckDBParameter("$3", threadCount));
                cmd.Parameters.Add(new DuckDBParameter("$4", minScore));
                cmd.Parameters.Add(new DuckDBParameter("$5", batchSize));
                cmd.Parameters.Add(new DuckDBParameter("$6", deck));
                cmd.Parameters.Add(new DuckDBParameter("$7", stake));
                cmd.Parameters.Add(new DuckDBParameter("$8", maxAnte));
                cmd.Parameters.Add(new DuckDBParameter("$9", 0)); // results_found starts at 0
                cmd.Parameters.Add(new DuckDBParameter("$10", "running"));

                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    DebugLogger.LogError("SearchHistoryService", "Failed to get search_id from INSERT");
                    return -1;
                }
                var searchId = Convert.ToInt64(result);
                DebugLogger.Log("SearchHistoryService", $"Started new search with ID: {searchId}");
                return searchId;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to start new search: {ex.Message}");
                return -1;
            }
        }

        public async Task AddSearchResultAsync(long searchId, SearchResult result)
        {
            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO search_results (search_id, seed, score, score_breakdown)
                    VALUES (?, ?, ?, ?)
                    ";

                cmd.Parameters.Add(new DuckDBParameter(searchId));
                cmd.Parameters.Add(new DuckDBParameter(result.Seed));
                cmd.Parameters.Add(new DuckDBParameter(result.TotalScore));
                cmd.Parameters.Add(new DuckDBParameter(result.ScoreBreakdown ?? ""));

                await cmd.ExecuteNonQueryAsync();


                // Update the results count
                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE searches 
                    SET results_found = results_found + 1 
                    WHERE search_id = ?
                ";
                updateCmd.Parameters.Add(new DuckDBParameter(searchId));
                await updateCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to add search result: {ex.Message}");
            }
        }

        public async Task CompleteSearchAsync(long searchId, long totalSeedsSearched, double durationSeconds, bool wasCancelled = false)
        {
            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE searches 
                    SET total_seeds_searched = ?, duration_seconds = ?, search_status = ?
                    WHERE search_id = ?
                ";

                cmd.Parameters.Add(new DuckDBParameter(totalSeedsSearched));
                cmd.Parameters.Add(new DuckDBParameter(durationSeconds));
                cmd.Parameters.Add(new DuckDBParameter(wasCancelled ? "cancelled" : "completed"));
                cmd.Parameters.Add(new DuckDBParameter(searchId));

                await cmd.ExecuteNonQueryAsync();
                var status = wasCancelled ? "cancelled" : "completed";
                DebugLogger.Log("SearchHistoryService", $"Search {searchId} {status}: {totalSeedsSearched} seeds in {durationSeconds:F2}s");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to complete search: {ex.Message}");
            }
        }

        public async Task<List<SearchHistorySummary>> GetRecentSearchesAsync(int limit = 10)
        {
            var results = new List<SearchHistorySummary>();


            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT search_id, config_path, search_date, results_found, 
                           total_seeds_searched, duration_seconds, deck, stake
                    FROM searches
                    ORDER BY search_date DESC
                    LIMIT $1
                ";
                cmd.Parameters.Add(new DuckDBParameter("$1", limit));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new SearchHistorySummary
                    {
                        SearchId = reader.GetInt64(0),
                        ConfigPath = reader.GetString(1),
                        SearchDate = reader.GetDateTime(2),
                        ResultsFound = reader.GetInt32(3),
                        TotalSeedsSearched = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                        DurationSeconds = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                        Deck = reader.GetString(6),
                        Stake = reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to get recent searches: {ex.Message}");
            }

            return results;
        }

        public async Task<List<SearchResult>> GetSearchResultsAsync(long searchId)
        {
            var results = new List<SearchResult>();


            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT seed, score, score_breakdown
                    FROM search_results
                    WHERE search_id = $1
                    ORDER BY score DESC
                ";
                cmd.Parameters.Add(new DuckDBParameter("$1", searchId));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new SearchResult
                    {
                        Seed = reader.GetString(0),
                        TotalScore = reader.GetInt32(1),
                        ScoreBreakdown = reader.IsDBNull(2) ? "" : reader.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to get search results: {ex.Message}");
            }

            return results;
        }


        /// <summary>
        /// Get live results as an observable collection that updates automatically
        /// </summary>
        public async Task<ObservableCollection<SearchResultViewModel>> GetLiveResultsObservableAsync(long searchId)
        {
            var collection = new ObservableCollection<SearchResultViewModel>();


            try
            {
                // Initial load of existing results
                var results = await GetSearchResultsAsync(searchId);
                int index = 1;
                foreach (var result in results)
                {
                    collection.Add(new SearchResultViewModel
                    {
                        Index = index++,
                        Seed = result.Seed,
                        Score = result.TotalScore,
                        ScoreBreakdown = result.ScoreBreakdown
                    });
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to get live results: {ex.Message}");
            }


            return collection;
        }


        /// <summary>
        /// Save filter configuration for a search
        /// </summary>
        public async Task SaveFilterItemsAsync(long searchId, Motely.Filters.OuijaConfig config)
        {
            try
            {
                var connection = GetConnection();


                // Save MUST items
                foreach (var item in config.Must)
                {
                    await SaveFilterItemAsync(connection, searchId, "must", item);
                }


                // Save SHOULD items
                foreach (var item in config.Should)
                {
                    await SaveFilterItemAsync(connection, searchId, "should", item);
                }


                // Save MUST NOT items
                foreach (var item in config.MustNot)
                {
                    await SaveFilterItemAsync(connection, searchId, "mustnot", item);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to save filter items: {ex.Message}");
            }
        }

        private async Task SaveFilterItemAsync(DuckDBConnection connection, long searchId, string filterType,
            Motely.Filters.OuijaConfig.FilterItem item)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO filter_items (search_id, filter_type, item_type, item_value, edition, score, antes)
                VALUES (?, ?, ?, ?, ?, ?, ?)
";
            cmd.Parameters.Add(new DuckDBParameter(searchId));
            cmd.Parameters.Add(new DuckDBParameter(filterType));
            cmd.Parameters.Add(new DuckDBParameter(item.Type));
            cmd.Parameters.Add(new DuckDBParameter(item.Value));
            cmd.Parameters.Add(new DuckDBParameter(item.Edition ?? ""));
            cmd.Parameters.Add(new DuckDBParameter(item.Score));
            cmd.Parameters.Add(new DuckDBParameter(System.Text.Json.JsonSerializer.Serialize(item.SearchAntes)));

            await cmd.ExecuteNonQueryAsync();
        }
    }

    public class SearchHistorySummary
    {
        public long SearchId { get; set; }
        public string ConfigPath { get; set; } = "";
        public DateTime SearchDate { get; set; }
        public int ResultsFound { get; set; }
        public long TotalSeedsSearched { get; set; }
        public double DurationSeconds { get; set; }
        public string Deck { get; set; } = "";
        public string Stake { get; set; } = "";
    }
}