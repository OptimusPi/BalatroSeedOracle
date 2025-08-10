using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Oracle.Helpers;
using Oracle.Models;

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

            // Set up new database path - simple: filterName.duckdb
            var searchResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
            Directory.CreateDirectory(searchResultsDir);

            _dbPath = Path.Combine(searchResultsDir, $"{_currentFilterName}.duckdb");
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

                // Create enhanced results table with JSON support
                using var createTable = connection.CreateCommand();
                createTable.CommandText =
                    @"
                    CREATE TABLE IF NOT EXISTS results (
                        seed VARCHAR PRIMARY KEY,
                        score DOUBLE,
                        details VARCHAR,
                        tally_scores JSON,
                        item_labels JSON,
                        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )
                ";
                createTable.ExecuteNonQuery();

                // Simple index on score for sorting
                using var createIndex = connection.CreateCommand();
                createIndex.CommandText =
                    @"
                    CREATE INDEX IF NOT EXISTS idx_score ON results(score DESC);
                ";
                createIndex.ExecuteNonQuery();

                DebugLogger.Log("SearchHistoryService", "Database initialized successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchHistoryService",
                    $"Failed to initialize database: {ex.Message}"
                );
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
        /// Add a search result to the database
        /// </summary>
        public async Task AddSearchResultAsync(SearchResult result)
        {
            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText =
                    @"
                    INSERT OR REPLACE INTO results (seed, score, details, tally_scores, item_labels, timestamp)
                    VALUES (?, ?, ?, ?, ?, ?)
                ";

                cmd.Parameters.Add(new DuckDBParameter(result.Seed));
                cmd.Parameters.Add(new DuckDBParameter(result.TotalScore));
                cmd.Parameters.Add(new DuckDBParameter("")); // ScoreBreakdown removed
                
                // Convert arrays to JSON strings for storage
                var tallyScoresJson = result.Scores != null 
                    ? JsonSerializer.Serialize(result.Scores)
                    : null;
                var itemLabelsJson = result.Labels != null
                    ? JsonSerializer.Serialize(result.Labels) 
                    : null;
                    
                cmd.Parameters.Add(new DuckDBParameter(tallyScoresJson ?? (object)DBNull.Value));
                cmd.Parameters.Add(new DuckDBParameter(itemLabelsJson ?? (object)DBNull.Value));
                cmd.Parameters.Add(new DuckDBParameter(result.Timestamp));

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchHistoryService",
                    $"Failed to add search result: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Get all results from the database
        /// </summary>
        public async Task<List<SearchResult>> GetSearchResultsAsync()
        {
            var results = new List<SearchResult>();

            try
            {
                var connection = GetConnection();

                using var cmd = connection.CreateCommand();
                cmd.CommandText =
                    @"
                    SELECT seed, score, details
                    FROM results
                    ORDER BY score DESC
                    LIMIT 1000
                ";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(
                        new SearchResult
                        {
                            Seed = reader.GetString(0),
                            TotalScore = reader.GetInt32(1),
                            // ScoreBreakdown removed, but still reading from column 2 for compatibility
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SearchHistoryService",
                    $"Failed to get search results: {ex.Message}"
                );
            }

            return results;
        }

        /// <summary>
        /// Export results to a CSV file
        /// </summary>
        public async Task<bool> ExportToCsvAsync(string csvPath)
        {
            try
            {
                var connection = GetConnection();

                using var exportCmd = connection.CreateCommand();
                exportCmd.CommandText =
                    $@"
                    COPY (
                        SELECT seed, score, details 
                        FROM results 
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

        /// <summary>
        /// Export all results from DuckDB to a file (CSV or JSON) with all columns
        /// </summary>
        public async Task<int> ExportResultsAsync(string filePath)
        {
            var connection = GetConnection();
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            try
            {
                using var cmd = connection.CreateCommand();
                
                if (extension == ".json")
                {
                    // Export as JSON with all columns
                    cmd.CommandText = @"
                        COPY (
                            SELECT 
                                seed,
                                score,
                                details,
                                tally_scores,
                                item_labels,
                                timestamp
                            FROM results
                            ORDER BY score DESC
                        ) TO ? (FORMAT JSON, ARRAY true)
                    ";
                    var param = new DuckDBParameter();
                    param.Value = filePath;
                    cmd.Parameters.Add(param);
                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Export as CSV - for now, just export the basic columns plus JSON as strings
                    // DuckDB doesn't support complex JSON flattening in COPY TO
                    cmd.CommandText = $@"
                        COPY (
                            SELECT 
                                seed,
                                score,
                                timestamp,
                                details,
                                COALESCE(tally_scores::VARCHAR, '') as tally_scores,
                                COALESCE(item_labels::VARCHAR, '') as item_labels
                            FROM results
                            ORDER BY score DESC
                        ) TO '{filePath.Replace("'", "''")}' (FORMAT CSV, HEADER TRUE)
                    ";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Get count of exported rows
                using var countCmd = connection.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(*) FROM results";
                var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                
                DebugLogger.Log("SearchHistoryService", $"Exported {count} results to: {filePath}");
                return count;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Error exporting results: {ex.Message}");
                throw;
            }
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
