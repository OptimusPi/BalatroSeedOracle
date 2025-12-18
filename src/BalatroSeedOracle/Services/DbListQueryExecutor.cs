using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services.DuckDB;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Handles DuckDB queries for DB List mode, supporting both desktop and browser builds
    /// </summary>
    public class DbListQueryExecutor : IDisposable
    {
        private readonly IDuckDBService _duckDBService;
        private bool _disposed = false;

        public DbListQueryExecutor(IDuckDBService duckDBService)
        {
            _duckDBService = duckDBService ?? throw new ArgumentNullException(nameof(duckDBService));
        }

        /// <summary>
        /// Query a DuckDB database file for seed results with comprehensive error handling
        /// </summary>
        public async Task<List<SearchResult>> QueryDatabaseAsync(
            string dbFilePath,
            SearchCriteria criteria,
            IProgress<SearchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(dbFilePath))
                throw new ArgumentException("Database file path cannot be null or empty", nameof(dbFilePath));

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            var results = new List<SearchResult>();
            
            try
            {
                DebugLogger.Log("DbListQueryExecutor", $"Starting query on database: {dbFilePath}");

                // Validate database file exists
                if (!File.Exists(dbFilePath))
                {
                    throw new FileNotFoundException($"Database file not found: {dbFilePath}");
                }

                // Check file size (should be > 0 for a valid DuckDB database)
                var fileInfo = new FileInfo(dbFilePath);
                if (fileInfo.Length == 0)
                {
                    throw new InvalidDataException($"Database file is empty: {dbFilePath}");
                }

                // Initialize DuckDB service if needed
                if (!_duckDBService.IsAvailable)
                {
                    await _duckDBService.InitializeAsync();
                    if (!_duckDBService.IsAvailable)
                    {
                        throw new InvalidOperationException("Failed to initialize DuckDB service");
                    }
                }

                // Create connection string for the database file
                var connectionString = _duckDBService.CreateConnectionString(dbFilePath);
                
                // Open connection with timeout
                using var connection = await OpenConnectionWithTimeout(connectionString, cancellationToken);
                
                // Validate database schema
                await ValidateDatabaseSchema(connection, criteria);
                
                // Get table information first
                var tables = await GetTableNamesAsync(connection);
                if (tables.Count == 0)
                {
                    throw new InvalidOperationException("No tables found in database. The database may be corrupted or empty.");
                }

                // Use the first table (assuming it's the results table)
                var tableName = tables[0];
                DebugLogger.Log("DbListQueryExecutor", $"Querying table: {tableName}");

                // Build query based on criteria with validation
                var query = BuildQuery(tableName, criteria);
                DebugLogger.Log("DbListQueryExecutor", $"Executing query: {query}");

                // Execute query with progress reporting
                var queryResults = await ExecuteQueryWithProgress(connection, query, progress, cancellationToken);
                
                // Convert results to SearchResult objects with validation
                results = ConvertToSearchResults(queryResults, criteria.MinScore);

                DebugLogger.Log("DbListQueryExecutor", $"Query completed. Found {results.Count} results");

                // Report final progress
                progress?.Report(new SearchProgress
                {
                    PercentComplete = 100.0,
                    SeedsSearched = (ulong)results.Count,
                    ResultsFound = results.Count,
                    EstimatedTimeRemaining = TimeSpan.Zero,
                    Message = $"Query complete. Found {results.Count} results."
                });

                return results;
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("DbListQueryExecutor", "Query was cancelled by user");
                throw;
            }
            catch (FileNotFoundException ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Database file not found: {ex.Message}");
                throw new InvalidOperationException($"Database file not found: {Path.GetFileName(dbFilePath)}", ex);
            }
            catch (InvalidDataException ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Invalid database file: {ex.Message}");
                throw new InvalidOperationException($"Database file is invalid or corrupted: {Path.GetFileName(dbFilePath)}", ex);
            }
            catch (InvalidOperationException ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Database operation failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Unexpected database query error: {ex.Message}");
                throw new InvalidOperationException($"Failed to query database: {ex.Message}", ex);
            }
        }

        private async Task<IDuckDBConnection> OpenConnectionWithTimeout(string connectionString, CancellationToken cancellationToken)
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            var connectionTask = _duckDBService.OpenConnectionAsync(connectionString);
            
            var completedTask = await Task.WhenAny(connectionTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Database connection timed out after 30 seconds");
            }
            
            return await connectionTask;
        }

        private async Task ValidateDatabaseSchema(IDuckDBConnection connection, SearchCriteria criteria)
        {
            try
            {
                // Check if we can query the information schema
                var schemaCheck = await connection.ExecuteReaderAsync(
                    "SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = 'main'",
                    reader => reader.GetInt32(0)
                );

                if (!schemaCheck.Any())
                {
                    throw new InvalidOperationException("Cannot read database schema - database may be corrupted");
                }

                DebugLogger.Log("DbListQueryExecutor", "Database schema validation passed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database schema validation failed: {ex.Message}", ex);
            }
        }

        private async Task<List<Dictionary<string, object>>> ExecuteQueryWithProgress(
            IDuckDBConnection connection, 
            string query,
            IProgress<SearchProgress>? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // Report query start
                progress?.Report(new SearchProgress
                {
                    PercentComplete = 50.0,
                    SeedsSearched = 0,
                    ResultsFound = 0,
                    EstimatedTimeRemaining = TimeSpan.FromSeconds(5),
                    Message = "Executing database query..."
                });

                var results = new List<Dictionary<string, object>>();
                
                var queryResult = await connection.ExecuteReaderAsync(query, reader =>
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    return row;
                });
                
                results.AddRange(queryResult);

                // Report query completion
                progress?.Report(new SearchProgress
                {
                    PercentComplete = 90.0,
                    SeedsSearched = (ulong)results.Count,
                    ResultsFound = results.Count,
                    EstimatedTimeRemaining = TimeSpan.FromSeconds(1),
                    Message = $"Processing {results.Count} results..."
                });

                return results;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Query execution failed: {ex.Message}");
                throw new InvalidOperationException($"Failed to execute query: {ex.Message}", ex);
            }
        }

        private async Task<List<string>> GetTableNamesAsync(IDuckDBConnection connection)
        {
            var tables = new List<string>();
            
            try
            {
                // Query to get all table names
                var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'main'";
                var result = await connection.ExecuteReaderAsync(query, reader => 
                {
                    var tableName = reader.GetString(0);
                    return tableName;
                });
                
                tables.AddRange(result);
            }
            catch (Exception ex)
            {
                DebugLogger.Log("DbListQueryExecutor", $"Failed to get table names: {ex.Message}");
            }

            return tables;
        }

        private string BuildQuery(string tableName, SearchCriteria criteria)
        {
            var query = $"SELECT * FROM {tableName}";
            
            var conditions = new List<string>();

            // Add minimum score filter if specified
            if (criteria.MinScore > 0)
            {
                conditions.Add($"score >= {criteria.MinScore}");
            }

            // Add deck filter if specified
            if (!string.IsNullOrEmpty(criteria.Deck) && criteria.Deck != "Red")
            {
                conditions.Add($"deck = '{criteria.Deck}'");
            }

            // Add stake filter if specified
            if (!string.IsNullOrEmpty(criteria.Stake) && criteria.Stake != "White")
            {
                conditions.Add($"stake = '{criteria.Stake}'");
            }

            // Add WHERE clause if there are conditions
            if (conditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            // Add ordering and limit
            query += " ORDER BY score DESC, seed ASC";
            
            // Add limit if MaxResults is specified
            if (criteria.MaxResults > 0)
            {
                query += $" LIMIT {criteria.MaxResults}";
            }

            return query;
        }

        private async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(
            IDuckDBConnection connection, 
            string query)
        {
            var results = new List<Dictionary<string, object>>();
            
            try
            {
                var queryResult = await connection.ExecuteReaderAsync(query, reader =>
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    return row;
                });
                
                results.AddRange(queryResult);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("DbListQueryExecutor", $"Query execution failed: {ex.Message}");
                throw;
            }

            return results;
        }

        private List<SearchResult> ConvertToSearchResults(
            List<Dictionary<string, object>> queryResults, 
            int minScore)
        {
            var results = new List<SearchResult>();

            foreach (var row in queryResults)
            {
                try
                {
                    // Extract basic fields
                    var seed = row.TryGetValue("seed", out var seedValue) 
                        ? seedValue?.ToString() ?? string.Empty 
                        : string.Empty;

                    var score = row.TryGetValue("score", out var scoreValue) 
                        ? Convert.ToInt32(scoreValue) 
                        : 0;

                    // Skip if below minimum score
                    if (score < minScore)
                        continue;

                    // Create SearchResult with basic properties
                    var result = new SearchResult
                    {
                        Seed = seed,
                        TotalScore = score
                    };

                    // For now, we'll just store the basic result
                    // Additional fields like deck/stake could be added to Labels or handled separately
                    // if needed in the future

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("DbListQueryExecutor", $"Failed to convert row to SearchResult: {ex.Message}");
                }
            }

            return results;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // DuckDB service is injected and managed by DI, don't dispose it here
                _disposed = true;
            }
        }
    }
}
