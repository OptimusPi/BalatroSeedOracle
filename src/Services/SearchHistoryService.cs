using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    public class SearchHistoryService : IDisposable
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private DuckDBConnection? _connection;
        private readonly object _connectionLock = new object();
        private string _currentFilterName = "default";
        private List<string> _columnNames = new List<string>();
        private Motely.Filters.OuijaConfig? _currentConfig;

        public SearchHistoryService()
        {
            // Don't create any database until a search actually starts
        }

        public void SetFilterConfig(Motely.Filters.OuijaConfig config)
        {
            _currentConfig = config;
            
            // Build column names from the Should[] criteria
            _columnNames.Clear();
            _columnNames.Add("seed");
            _columnNames.Add("score");
            
            if (config.Should != null)
            {
                // Track seen names to avoid duplicates
                var seenNames = new HashSet<string>();
                
                foreach (var should in config.Should)
                {
                    var baseName = FormatColumnName(should);
                    var colName = baseName;
                    
                    // If we've seen this name before, append a number
                    int suffix = 2;
                    while (seenNames.Contains(colName))
                    {
                        colName = $"{baseName}_{suffix}";
                        suffix++;
                    }
                    
                    seenNames.Add(colName);
                    _columnNames.Add(colName);
                }
            }
            
            DebugLogger.Log("SearchHistoryService", $"Configured {_columnNames.Count} columns for results table");
        }
        
        private string FormatColumnName(Motely.Filters.OuijaConfig.FilterItem should)
        {
            if (should == null) return "should";
            
            // TODO: When Label property is added to FilterItem, use it here
            // if (!string.IsNullOrEmpty(should.Label))
            // {
            //     return should.Label.Replace(" ", "_").Replace("-", "_").ToLower();
            // }
            
            // Create a column name from the filter properties
            var name = !string.IsNullOrEmpty(should.Value) ? should.Value : should.Type;
            if (!string.IsNullOrEmpty(should.Edition))
                name = should.Edition + "_" + name;
            
            // If should has specific antes, add the first one to the name
            if (should.Antes != null && should.Antes.Length > 0)
                name += "_ante" + should.Antes[0];
                
            // Make it SQL-safe
            name = name?.Replace(" ", "_").Replace("-", "_").ToLower() ?? "column";
            return name;
        }

        public void SetFilterName(string filterPath)
        {
            // Extract filter name from path
            if (!string.IsNullOrEmpty(filterPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filterPath);
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
                // If we don't have column names yet, create a basic table
                if (_columnNames.Count < 2)
                {
                    _columnNames.Clear();
                    _columnNames.Add("seed");
                    _columnNames.Add("score");
                }

                // First check if database file exists and if schema matches
                bool needsRecreate = false;
                if (File.Exists(_dbPath))
                {
                    var connection = GetConnection();
                    
                    // Check if table exists
                    using var checkTable = connection.CreateCommand();
                    checkTable.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'results'";
                    var tableExists = Convert.ToInt32(checkTable.ExecuteScalar()) > 0;
                    
                    if (tableExists)
                    {
                        // Table exists - check if schema matches
                        using var getColumns = connection.CreateCommand();
                        getColumns.CommandText = "PRAGMA table_info(results)";
                        using var reader = getColumns.ExecuteReader();
                        var existingColumns = new List<string>();
                        while (reader.Read())
                        {
                            existingColumns.Add(reader.GetString(1)); // column name is at index 1
                        }
                        
                        // If column count doesn't match, we need to recreate the database
                        if (existingColumns.Count != _columnNames.Count)
                        {
                            DebugLogger.Log("SearchHistoryService", 
                                $"Schema mismatch: existing={existingColumns.Count}, expected={_columnNames.Count}. Backing up and recreating database.");
                            needsRecreate = true;
                        }
                    }
                }
                
                if (needsRecreate)
                {
                    // 1) Close the connection first
                    lock (_connectionLock)
                    {
                        _connection?.Close();
                        _connection?.Dispose();
                        _connection = null;
                    }
                    
                    // 2) Move filtername.duckdb to filtername.duckdb.bak
                    var backupPath = _dbPath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        // If backup already exists, add timestamp
                        backupPath = $"{_dbPath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                    }
                    File.Move(_dbPath, backupPath);
                    DebugLogger.Log("SearchHistoryService", $"Backed up old database to: {backupPath}");
                }
                
                // 3) Create new filtername.duckdb correctly and hold the connection
                var conn = GetConnection();
                
                // 4) Create the table with proper columns
                var columnDefs = new List<string>();
                columnDefs.Add("seed VARCHAR PRIMARY KEY");
                columnDefs.Add("score INT");
                
                // Add a column for each Should[] criterion (if we have more than just seed/score)
                for (int i = 2; i < _columnNames.Count; i++)
                {
                    columnDefs.Add($"{_columnNames[i]} INT");
                }
                
                using var createTable = conn.CreateCommand();
                createTable.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS results (
                        {string.Join(",\n                        ", columnDefs)}
                    )
                ";
                createTable.ExecuteNonQuery();

                // Simple index on score for sorting
                using var createIndex = conn.CreateCommand();
                createIndex.CommandText =
                    @"
                    CREATE INDEX IF NOT EXISTS idx_score ON results(score DESC);
                ";
                createIndex.ExecuteNonQuery();
                
                // Create meta table to store search progress (batch number)
                using var createMeta = conn.CreateCommand();
                createMeta.CommandText = @"
                    CREATE TABLE IF NOT EXISTS search_meta (
                        key VARCHAR PRIMARY KEY,
                        value VARCHAR
                    )
                ";
                createMeta.ExecuteNonQuery();

                DebugLogger.Log("SearchHistoryService", $"Database initialized with {_columnNames.Count} columns");
                // pifreak loves you!
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

                // Build dynamic INSERT statement based on columns
                var columns = string.Join(", ", _columnNames);
                var placeholders = string.Join(", ", Enumerable.Repeat("?", _columnNames.Count));
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT OR REPLACE INTO results ({columns})
                    VALUES ({placeholders})
                ";

                // Add parameters in order: seed, total score, then individual scores
                cmd.Parameters.Add(new DuckDBParameter(result.Seed));
                cmd.Parameters.Add(new DuckDBParameter(result.TotalScore));
                
                // Add each individual score (if we have criterion columns)
                if (result.Scores != null && _columnNames.Count > 2)
                {
                    // We have criterion columns, add scores for each
                    for (int i = 0; i < _columnNames.Count - 2; i++)
                    {
                        if (i < result.Scores.Length)
                        {
                            cmd.Parameters.Add(new DuckDBParameter(result.Scores[i]));
                        }
                        else
                        {
                            // If we don't have enough scores, pad with 0
                            cmd.Parameters.Add(new DuckDBParameter(0));
                        }
                    }
                }

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

                // Select all columns dynamically
                var columns = string.Join(", ", _columnNames);
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT {columns}
                    FROM results
                    ORDER BY score DESC
                    LIMIT 1000
                ";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var seed = reader.GetString(0);
                    var score = reader.GetInt32(1);
                    
                    // Read individual scores from criterion columns
                    int[]? scores = null;
                    if (_columnNames.Count > 2)
                    {
                        scores = new int[_columnNames.Count - 2];
                        for (int i = 0; i < scores.Length; i++)
                        {
                            scores[i] = reader.GetInt32(i + 2);
                        }
                    }
                    
                    results.Add(
                        new SearchResult
                        {
                            Seed = seed,
                            TotalScore = score,
                            Scores = scores
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

                // Export all columns to CSV
                var columns = string.Join(", ", _columnNames);
                
                using var exportCmd = connection.CreateCommand();
                exportCmd.CommandText =
                    $@"
                    COPY (
                        SELECT {columns}
                        FROM results 
                        ORDER BY score DESC, seed
                    ) TO '{csvPath}' (FORMAT CSV, HEADER FALSE)
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
        /// Save the last processed batch number for resume
        /// </summary>
        public async Task SaveLastBatchAsync(ulong batchNumber)
        {
            try
            {
                var connection = GetConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO search_meta (key, value)
                    VALUES ('last_batch', ?)
                ";
                cmd.Parameters.Add(new DuckDBParameter(batchNumber.ToString()));
                await cmd.ExecuteNonQueryAsync();
                
                DebugLogger.Log("SearchHistoryService", $"Saved last batch: {batchNumber}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to save last batch: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the last processed batch number for resume
        /// </summary>
        public async Task<ulong?> GetLastBatchAsync()
        {
            try
            {
                var connection = GetConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT value FROM search_meta WHERE key = 'last_batch'
                ";
                
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && ulong.TryParse(result.ToString(), out var batch))
                {
                    DebugLogger.Log("SearchHistoryService", $"Retrieved last batch: {batch}");
                    return batch;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to get last batch: {ex.Message}");
                return null;
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
                // Build column list for export
                var columns = string.Join(", ", _columnNames);
                
                using var cmd = connection.CreateCommand();
                
                if (extension == ".json")
                {
                    // Export as JSON with all dynamic columns
                    cmd.CommandText = $@"
                        COPY (
                            SELECT {columns}
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
                    // Export as CSV with all columns  
                    cmd.CommandText = $@"
                        COPY (
                            SELECT {columns}
                            FROM results
                            ORDER BY score DESC
                        ) TO '{filePath.Replace("'", "''")}' (FORMAT CSV, HEADER FALSE)
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
        public ulong SearchId { get; set; }
        public string ConfigPath { get; set; } = "";
        public DateTime SearchDate { get; set; }
        public int ResultsFound { get; set; }
        public ulong TotalSeedsSearched { get; set; }
        public double DurationSeconds { get; set; }
        public string Deck { get; set; } = "";
        public string Stake { get; set; } = "";
    }
}
