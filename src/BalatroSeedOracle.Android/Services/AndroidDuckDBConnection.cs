using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.DuckDB;
using DuckDB.NET.Data;

namespace BalatroSeedOracle.Android.Services;

/// <summary>
/// Android implementation of IDuckDBConnection wrapping DuckDBConnection
/// </summary>
public class AndroidDuckDBConnection : IDuckDBConnection
{
    private readonly DuckDBConnection _connection;
    private bool _disposed;

    public AndroidDuckDBConnection(string connectionString)
    {
        _connection = new DuckDBConnection(connectionString);
    }

    /// <summary>
    /// Constructor that wraps an existing DuckDBConnection (for DuckLake connections)
    /// </summary>
    public AndroidDuckDBConnection(DuckDBConnection connection)
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
            var wrapper = new AndroidDuckDBDataReader(reader);
            results.Add(mapper(wrapper));
        }
        return results;
    }

    public Task<IDuckDBAppender> CreateAppenderAsync(string schema, string tableName)
    {
        EnsureOpen();
        var appender = _connection.CreateAppender(tableName);
        return Task.FromResult<IDuckDBAppender>(new AndroidDuckDBAppender(appender));
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

    public Task<long> GetRowCountAsync(string tableName)
    {
        EnsureOpen();
        // Use Motely's helper - no SQL in BSO!
        return Task.FromResult(Motely.DuckDB.DuckDBOperations.GetRowCount(_connection, tableName));
    }

    public Task EnsureTableExistsAsync(string createTableSql)
    {
        EnsureOpen();
        // Use Motely's helper - no SQL in BSO!
        Motely.DuckDB.DuckDBTableManager.EnsureTableExists(_connection, createTableSql);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetAllSeedsAsync(string tableName, string seedColumnName, string? orderBy = null)
    {
        EnsureOpen();
        // Use Motely's helper - no SQL in BSO!
        var seeds = Motely.DuckDB.DuckDBQueryHelpers.GetAllSeeds(_connection, tableName, seedColumnName);
        return Task.FromResult(seeds);
    }

    public Task ClearTableAsync(string tableName)
    {
        EnsureOpen();
        // Simple operation - use SQL here (infrastructure layer, not business logic)
        return ExecuteNonQueryAsync($"DELETE FROM {tableName}");
    }

    public Task CreateIndexAsync(string indexSql)
    {
        EnsureOpen();
        // Use Motely's helper - no SQL construction in BSO!
        Motely.DuckDB.DuckDBTableManager.CreateIndex(_connection, indexSql);
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        EnsureOpen();
        var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'main'";
        var results = await ExecuteReaderAsync<string>(query, reader => reader.GetString(0));
        return results.ToList();
    }

    public Task<List<BalatroSeedOracle.Models.ResultWithTallies>> QueryResultsAsync(
        string tableName,
        int? minScore = null,
        string? deck = null,
        string? stake = null,
        int limit = 1000
    )
    {
        EnsureOpen();
        var motelyResults = Motely.DuckDB.DuckDBQueryHelpers.GetResultsWithTallies(
            _connection,
            offset: 0,
            limit: limit,
            orderBy: "score",
            ascending: false
        );

        var results = motelyResults
            .Select(row => new BalatroSeedOracle.Models.ResultWithTallies
            {
                Seed = row["seed"]?.ToString() ?? "",
                Score = Convert.ToInt32(row["score"] ?? 0),
                Tallies = row.Where(kvp => kvp.Key != "seed" && kvp.Key != "score")
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => Convert.ToInt32(kvp.Value ?? 0))
                    .ToList(),
            })
            .ToList();

        if (minScore.HasValue && minScore.Value > 0)
            results = results.Where(r => r.Score >= minScore.Value).ToList();

        return Task.FromResult(results);
    }

    public async Task<Dictionary<string, object?>?> LoadRowByIdAsync(string tableName, string idColumn, int id)
    {
        EnsureOpen();
        var query = $"SELECT * FROM {tableName} WHERE {idColumn} = {id} LIMIT 1";
        var results = await ExecuteReaderAsync<Dictionary<string, object?>>(
            query,
            reader =>
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                return row;
            }
        );
        return results.FirstOrDefault();
    }

    public async Task UpsertRowAsync(string tableName, Dictionary<string, object?> values, string keyColumn)
    {
        EnsureOpen();
        var columns = string.Join(", ", values.Keys);
        var valuePlaceholders = string.Join(
            ", ",
            values.Select(
                (kvp, i) =>
                    kvp.Value == null ? "NULL"
                    : kvp.Value is string ? $"'{kvp.Value.ToString()!.Replace("'", "''")}'"
                    : kvp.Value.ToString()
            )
        );
        var updates = string.Join(", ", values.Keys.Where(k => k != keyColumn).Select(k => $"{k} = excluded.{k}"));

        var sql =
            $"INSERT INTO {tableName} ({columns}) VALUES ({valuePlaceholders}) "
            + $"ON CONFLICT ({keyColumn}) DO UPDATE SET {updates}";
        await ExecuteNonQueryAsync(sql);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _connection?.Close();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        _disposed = true;
    }
}

/// <summary>
/// Android implementation of data reader wrapping DbDataReader
/// </summary>
internal class AndroidDuckDBDataReader : IDuckDBDataReader
{
    private readonly System.Data.Common.DbDataReader _reader;

    public AndroidDuckDBDataReader(System.Data.Common.DbDataReader reader)
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
