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
}
