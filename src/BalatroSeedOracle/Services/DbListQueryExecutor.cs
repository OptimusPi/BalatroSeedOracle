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
            CancellationToken cancellationToken = default
        )
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

                // Get table information first - use high-level method, no SQL!
                var tables = await connection.GetTableNamesAsync();
                if (tables.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No tables found in database. The database may be corrupted or empty."
                    );
                }

                // Use the first table (assuming it's the results table)
                var tableName = tables[0];
                DebugLogger.Log("DbListQueryExecutor", $"Querying table: {tableName}");

                // Use high-level method - no SQL construction in BSO!
                // This uses Motely's DuckDBQueryHelpers internally
                progress?.Report(new SearchProgress { PercentComplete = 50.0, Message = "Querying database..." });

                var resultsWithTallies = await connection.QueryResultsAsync(
                    tableName,
                    criteria.MinScore > 0 ? criteria.MinScore : null,
                    criteria.Deck != "Red" ? criteria.Deck : null,
                    criteria.Stake != "White" ? criteria.Stake : null,
                    criteria.MaxResults > 0 ? criteria.MaxResults : 1000
                );

                // Convert to BSO SearchResult format
                results = resultsWithTallies
                    .Select(r => new SearchResult
                    {
                        Seed = r.Seed,
                        TotalScore = r.Score,
                        Scores = r.Tallies?.ToArray(),
                        Labels = null, // Would need column names from Motely
                    })
                    .ToList();

                DebugLogger.Log("DbListQueryExecutor", $"Query completed. Found {results.Count} results");

                // Report final progress
                progress?.Report(
                    new SearchProgress
                    {
                        PercentComplete = 100.0,
                        SeedsSearched = (ulong)results.Count,
                        ResultsFound = results.Count,
                        EstimatedTimeRemaining = TimeSpan.Zero,
                        Message = $"Query complete. Found {results.Count} results.",
                    }
                );

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
                throw new InvalidOperationException(
                    $"Database file is invalid or corrupted: {Path.GetFileName(dbFilePath)}",
                    ex
                );
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

        private async Task<IDuckDBConnection> OpenConnectionWithTimeout(
            string connectionString,
            CancellationToken cancellationToken
        )
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
                // Check if we can query the information schema - use high-level method!
                var tableCount = (await connection.GetTableNamesAsync()).Count;

                if (tableCount == 0)
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

        // Removed ExecuteQueryWithProgress - now using QueryResultsAsync which uses Motely's helpers

        // Removed GetTableNamesAsync - now using IDuckDBConnection.GetTableNamesAsync() directly

        // Removed BuildQuery, ExecuteQueryAsync, ConvertToSearchResults - now using Motely's QueryResultsAsync

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
