#if BROWSER
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Browser implementation of IDuckDBConnection using DuckDB-WASM via JavaScript interop
/// </summary>
public partial class BrowserDuckDBConnection : IDuckDBConnection
{
    private readonly int _connectionId;
    private bool _disposed;

    public BrowserDuckDBConnection(int connectionId)
    {
        _connectionId = connectionId;
    }

    public bool IsOpen => !_disposed;

    public async Task ExecuteNonQueryAsync(string sql)
    {
        await ExecuteAsync(_connectionId, sql);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql)
    {
        var json = await QueryAsync(_connectionId, sql);
        var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

        if (rows == null || rows.Count == 0)
            return default;

        var firstRow = rows[0];
        if (firstRow.Count == 0)
            return default;

        var firstValue = firstRow.Values.GetEnumerator();
        firstValue.MoveNext();

        return ConvertJsonElement<T>(firstValue.Current);
    }

    public async Task<IEnumerable<T>> ExecuteReaderAsync<T>(string sql, Func<IDuckDBDataReader, T> mapper)
    {
        var json = await QueryAsync(_connectionId, sql);
        var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

        if (rows == null)
            return Array.Empty<T>();

        var results = new List<T>();
        foreach (var row in rows)
        {
            var reader = new BrowserDuckDBDataReader(row);
            results.Add(mapper(reader));
        }
        return results;
    }

    public async Task<IDuckDBAppender> CreateAppenderAsync(string schema, string table)
    {
        var appenderId = await CreateAppenderInternalAsync(_connectionId, schema, table);
        return new BrowserDuckDBAppender(appenderId);
    }

    public Task CopyFromFileAsync(string filePath, string tableName, string options = "")
    {
        // Browser doesn't have file system access - this is a no-op or could use virtual file system
        throw new NotSupportedException("File system access not available in browser. Use alternative data loading methods.");
    }

    public Task CopyToFileAsync(string tableName, string filePath, string format = "csv")
    {
        // Browser doesn't have file system access - could export to download
        throw new NotSupportedException("File system access not available in browser. Use ExportToCSV for data export.");
    }

    public async Task<string> ExportToCSVAsync(string tableName)
    {
        return await ExportToCSVInternalAsync(_connectionId, tableName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _ = CloseConnectionAsync(_connectionId);
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await CloseConnectionAsync(_connectionId);
        _disposed = true;
    }

    private static T? ConvertJsonElement<T>(JsonElement element)
    {
        var targetType = typeof(T);

        if (targetType == typeof(int) || targetType == typeof(int?))
            return (T)(object)element.GetInt32();
        if (targetType == typeof(long) || targetType == typeof(long?))
            return (T)(object)element.GetInt64();
        if (targetType == typeof(double) || targetType == typeof(double?))
            return (T)(object)element.GetDouble();
        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return (T)(object)element.GetBoolean();
        if (targetType == typeof(string))
            return (T)(object?)element.GetString()!;

        return default;
    }

    [JSImport("DuckDB.execute", "globalThis")]
    private static partial Task ExecuteAsync(int connId, string sql);

    [JSImport("DuckDB.query", "globalThis")]
    private static partial Task<string> QueryAsync(int connId, string sql);

    [JSImport("DuckDB.closeConnection", "globalThis")]
    private static partial Task CloseConnectionAsync(int connId);

    [JSImport("DuckDB.createAppender", "globalThis")]
    private static partial Task<int> CreateAppenderInternalAsync(int connId, string schema, string table);

    [JSImport("DuckDB.exportToCSV", "globalThis")]
    private static partial Task<string> ExportToCSVInternalAsync(int connId, string tableName);
}

/// <summary>
/// Browser implementation of data reader using JSON dictionary
/// </summary>
public class BrowserDuckDBDataReader : IDuckDBDataReader
{
    private readonly Dictionary<string, JsonElement> _row;
    private readonly string[] _columnNames;

    public BrowserDuckDBDataReader(Dictionary<string, JsonElement> row)
    {
        _row = row;
        _columnNames = new string[row.Count];
        row.Keys.CopyTo(_columnNames, 0);
    }

    public int FieldCount => _row.Count;

    public string GetName(int ordinal) => _columnNames[ordinal];

    public int GetOrdinal(string name)
    {
        for (int i = 0; i < _columnNames.Length; i++)
        {
            if (_columnNames[i] == name)
                return i;
        }
        throw new ArgumentException($"Column '{name}' not found");
    }

    public bool IsDBNull(int ordinal) => _row[_columnNames[ordinal]].ValueKind == JsonValueKind.Null;

    public string GetString(int ordinal) => _row[_columnNames[ordinal]].GetString() ?? string.Empty;

    public int GetInt32(int ordinal) => _row[_columnNames[ordinal]].GetInt32();

    public long GetInt64(int ordinal) => _row[_columnNames[ordinal]].GetInt64();

    public double GetDouble(int ordinal) => _row[_columnNames[ordinal]].GetDouble();

    public bool GetBoolean(int ordinal) => _row[_columnNames[ordinal]].GetBoolean();

    public object GetValue(int ordinal)
    {
        var element = _row[_columnNames[ordinal]];
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => DBNull.Value,
            _ => element.ToString()
        };
    }
}
#endif
