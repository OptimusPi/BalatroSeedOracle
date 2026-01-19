using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Abstraction for DuckDB connection that works across platforms.
/// Designed to be async-first for browser compatibility.
/// </summary>
public interface IDuckDBConnection : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Whether the connection is open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Execute a non-query SQL command asynchronously (DDL, INSERT, UPDATE, DELETE)
    /// </summary>
    Task ExecuteNonQueryAsync(string sql);

    /// <summary>
    /// Execute a scalar query asynchronously and return the result
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(string sql);

    /// <summary>
    /// Execute a query and map results using a custom mapper
    /// </summary>
    Task<IEnumerable<T>> ExecuteReaderAsync<T>(string sql, Func<IDuckDBDataReader, T> mapper);

    /// <summary>
    /// Create an appender for bulk inserts asynchronously
    /// </summary>
    Task<IDuckDBAppender> CreateAppenderAsync(string schema, string tableName);

    /// <summary>
    /// Copy data from a file to a table (DuckDB COPY command) - Desktop only
    /// </summary>
    Task CopyFromFileAsync(string filePath, string tableName, string options = "");

    /// <summary>
    /// Copy data from a table to a file (DuckDB COPY TO command) - Desktop only
    /// </summary>
    Task CopyToFileAsync(string tableName, string filePath, string format = "csv");

    /// <summary>
    /// Get row count for a table (uses Motely's DuckDBOperations internally)
    /// </summary>
    Task<long> GetRowCountAsync(string tableName);

    /// <summary>
    /// Ensure a table exists using schema from Motely (uses DuckDBTableManager internally)
    /// </summary>
    Task EnsureTableExistsAsync(string createTableSql);

    /// <summary>
    /// Get all seeds from a table (uses Motely's DuckDBQueryHelpers internally)
    /// </summary>
    Task<List<string>> GetAllSeedsAsync(string tableName, string seedColumnName, string? orderBy = null);

    /// <summary>
    /// Clear all rows from a table
    /// </summary>
    Task ClearTableAsync(string tableName);

    /// <summary>
    /// Create an index on a table (uses Motely's DuckDBTableManager internally)
    /// </summary>
    Task CreateIndexAsync(string indexSql);

    /// <summary>
    /// Get all table names from the database (uses Motely's helpers internally)
    /// </summary>
    Task<List<string>> GetTableNamesAsync();

    /// <summary>
    /// Query results from a table with filters (uses Motely's DuckDBQueryHelpers internally)
    /// This replaces SQL construction in BSO business logic
    /// </summary>
    Task<List<Models.ResultWithTallies>> QueryResultsAsync(
        string tableName,
        int? minScore = null,
        string? deck = null,
        string? stake = null,
        int limit = 1000
    );

    /// <summary>
    /// Load a single row by ID from a table (uses Motely's helpers internally)
    /// </summary>
    Task<Dictionary<string, object?>?> LoadRowByIdAsync(string tableName, string idColumn, int id);

    /// <summary>
    /// Upsert a row (INSERT OR REPLACE) - uses Motely's helpers internally
    /// </summary>
    Task UpsertRowAsync(string tableName, Dictionary<string, object?> values, string keyColumn);
}
