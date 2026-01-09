#if !BROWSER
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DuckDB.NET.Data;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Desktop implementation of IDuckDBConnection wrapping DuckDBConnection
/// </summary>
public class DesktopDuckDBConnection : IDuckDBConnection
{
    private readonly DuckDBConnection _connection;
    private bool _disposed;

    public DesktopDuckDBConnection(string connectionString)
    {
        _connection = new DuckDBConnection(connectionString);
    }

    /// <summary>
    /// Constructor that wraps an existing DuckDBConnection (for DuckLake connections)
    /// </summary>
    public DesktopDuckDBConnection(DuckDBConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public bool IsOpen => _connection.State == ConnectionState.Open;

    private void EnsureOpen()
    {
        if (!IsOpen)
            _connection.Open();
    }

    public async Task ExecuteNonQueryAsync(string sql)
    {
        EnsureOpen();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql)
    {
        EnsureOpen();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<IEnumerable<T>> ExecuteReaderAsync<T>(string sql, Func<IDuckDBDataReader, T> mapper)
    {
        EnsureOpen();
        var results = new List<T>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var wrapper = new DesktopDuckDBDataReader(reader);
            results.Add(mapper(wrapper));
        }
        return results;
    }

    public Task<IDuckDBAppender> CreateAppenderAsync(string schema, string tableName)
    {
        EnsureOpen();
        var appender = _connection.CreateAppender(tableName);
        return Task.FromResult<IDuckDBAppender>(new DesktopDuckDBAppender(appender));
    }

    public async Task CopyFromFileAsync(string filePath, string tableName, string options = "")
    {
        EnsureOpen();
        var escapedPath = filePath.Replace("\\", "/").Replace("'", "''");
        var sql = string.IsNullOrEmpty(options)
            ? $"COPY {tableName} FROM '{escapedPath}'"
            : $"COPY {tableName} FROM '{escapedPath}' ({options})";
        await ExecuteNonQueryAsync(sql);
    }

    public async Task CopyToFileAsync(string tableName, string filePath, string format = "csv")
    {
        EnsureOpen();
        var escapedPath = filePath.Replace("\\", "/").Replace("'", "''");
        await ExecuteNonQueryAsync($"COPY {tableName} TO '{escapedPath}' (FORMAT {format})");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Close();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        _disposed = true;
    }
}

/// <summary>
/// Desktop implementation of data reader wrapping DbDataReader
/// </summary>
internal class DesktopDuckDBDataReader : IDuckDBDataReader
{
    private readonly System.Data.Common.DbDataReader _reader;

    public DesktopDuckDBDataReader(System.Data.Common.DbDataReader reader)
    {
        _reader = reader;
    }

    public int FieldCount => _reader.FieldCount;
    public string GetName(int ordinal) => _reader.GetName(ordinal);
    public int GetOrdinal(string name) => _reader.GetOrdinal(name);
    public bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
    public string GetString(int ordinal) => _reader.GetString(ordinal);
    public int GetInt32(int ordinal) => _reader.GetInt32(ordinal);
    public long GetInt64(int ordinal) => _reader.GetInt64(ordinal);
    public double GetDouble(int ordinal) => _reader.GetDouble(ordinal);
    public bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);
    public object GetValue(int ordinal) => _reader.GetValue(ordinal);
}
#endif
