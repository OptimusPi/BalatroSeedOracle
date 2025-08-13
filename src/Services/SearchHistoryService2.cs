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
    public class SearchHistoryService2 : IDisposable
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private DuckDBConnection? _connection;
        private DuckDBAppender? _appender;
        private readonly object _connectionLock = new object();
        private string _currentFilterName = "default";
        private string[]? _columnLabels;
        private bool _tableInitialized = false;

        public SearchHistoryService2()
        {
            // Don't create any database until a search actually starts
        }

        public void SetFilterName(string filterPath)
        {
            // Extract filter name from path
            if (!string.IsNullOrEmpty(filterPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filterPath);
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
                _appender?.Dispose();
                _appender = null;
                _connection?.Dispose();
                _connection = null;
                _tableInitialized = false;
                _columnLabels = null;
            }

            // Set up new database path
            var searchResultsDir = Path.Combine(Directory.GetCurrentDirectory(), "SearchResults");
            Directory.CreateDirectory(searchResultsDir);

            _dbPath = Path.Combine(searchResultsDir, $"{_currentFilterName}.duckdb");
            _connectionString = $"Data Source={_dbPath}";

            DebugLogger.Log("SearchHistoryService", $"Database path set to: {_dbPath}");
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

        private async Task InitializeTable(string[] labels)
        {
            var connection = GetConnection();
            
            // Build CREATE TABLE with dynamic columns
            var columns = new List<string>
            {
                "seed VARCHAR PRIMARY KEY",
                "score INTEGER"
            };
            
            // Add a column for each "should" item score
            for (int i = 0; i < labels.Length; i++)
            {
                columns.Add($"s{i} INTEGER");
            }
            
            using var createTable = connection.CreateCommand();
            createTable.CommandText = $"CREATE TABLE IF NOT EXISTS results ({string.Join(", ", columns)})";
            await createTable.ExecuteNonQueryAsync();
            
            // Create index on score
            using var createIndex = connection.CreateCommand();
            createIndex.CommandText = "CREATE INDEX IF NOT EXISTS idx_score ON results(score DESC)";
            await createIndex.ExecuteNonQueryAsync();
            
            _columnLabels = labels;
            _tableInitialized = true;
            
            // Create appender for bulk inserts
            lock (_connectionLock)
            {
                _appender = connection.CreateAppender("results");
            }
            
            DebugLogger.Log("SearchHistoryService", $"Initialized table with {labels.Length} score columns");
        }

        public async Task AddSearchResultAsync(SearchResult result)
        {
            try
            {
                // Initialize table on first result that has labels
                if (!_tableInitialized && result.Labels != null && result.Labels.Length > 0)
                {
                    await InitializeTable(result.Labels);
                }
                
                if (!_tableInitialized || _appender == null)
                {
                    DebugLogger.LogError("SearchHistoryService", "Cannot add result - table not initialized");
                    return;
                }
                
                lock (_connectionLock)
                {
                    var row = _appender.CreateRow();
                    row.AppendValue(result.Seed)
                       .AppendValue(result.TotalScore);
                    
                    // Append individual scores
                    if (result.Scores != null && _columnLabels != null)
                    {
                        for (int i = 0; i < _columnLabels.Length; i++)
                        {
                            if (i < result.Scores.Length)
                                row.AppendValue(result.Scores[i]);
                            else
                                row.AppendValue(0);
                        }
                    }
                    else if (_columnLabels != null)
                    {
                        // Fill with zeros if no scores
                        for (int i = 0; i < _columnLabels.Length; i++)
                        {
                            row.AppendValue(0);
                        }
                    }
                    
                    row.EndRow();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to add result: {ex.Message}");
            }
        }

        public async Task<List<SearchResult>> GetSearchResultsAsync()
        {
            var results = new List<SearchResult>();
            
            try
            {
                if (!_tableInitialized)
                {
                    return results;
                }
                
                // Flush appender to ensure all data is written
                lock (_connectionLock)
                {
                    _appender?.Dispose();
                    _appender = GetConnection().CreateAppender("results");
                }
                
                var connection = GetConnection();
                using var cmd = connection.CreateCommand();
                
                // Build dynamic SELECT
                var columns = new List<string> { "seed", "score" };
                if (_columnLabels != null)
                {
                    for (int i = 0; i < _columnLabels.Length; i++)
                    {
                        columns.Add($"s{i}");
                    }
                }
                
                cmd.CommandText = $"SELECT {string.Join(", ", columns)} FROM results ORDER BY score DESC LIMIT 1000";
                
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var scores = new int[_columnLabels?.Length ?? 0];
                    if (_columnLabels != null)
                    {
                        for (int i = 0; i < _columnLabels.Length; i++)
                        {
                            scores[i] = reader.IsDBNull(i + 2) ? 0 : reader.GetInt32(i + 2);
                        }
                    }
                    
                    results.Add(new SearchResult
                    {
                        Seed = reader.GetString(0),
                        TotalScore = reader.GetInt32(1),
                        Scores = scores
                    });
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to get results: {ex.Message}");
            }
            
            return results;
        }

        public async Task<bool> ExportToCsvAsync(string csvPath)
        {
            try
            {
                if (!_tableInitialized)
                {
                    return false;
                }
                
                // Build headers
                var headers = new List<string> { "Seed", "Score" };
                if (_columnLabels != null)
                {
                    headers.AddRange(_columnLabels);
                }
                
                // Write CSV manually with proper headers
                using var writer = new StreamWriter(csvPath);
                await writer.WriteLineAsync(string.Join(",", headers));
                
                var results = await GetSearchResultsAsync();
                foreach (var result in results)
                {
                    var row = new List<string> { result.Seed, result.TotalScore.ToString() };
                    if (result.Scores != null)
                    {
                        row.AddRange(result.Scores.Select(s => s.ToString()));
                    }
                    await writer.WriteLineAsync(string.Join(",", row));
                }
                
                DebugLogger.Log("SearchHistoryService", $"Exported {results.Count} results to: {csvPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchHistoryService", $"Failed to export CSV: {ex.Message}");
                return false;
            }
        }

        public string[] GetColumnLabels()
        {
            return _columnLabels ?? Array.Empty<string>();
        }

        public void Dispose()
        {
            lock (_connectionLock)
            {
                _appender?.Dispose();
                _appender = null;
                _connection?.Dispose();
                _connection = null;
                DebugLogger.Log("SearchHistoryService", "Closed DuckDB connection");
            }
        }
    }
}