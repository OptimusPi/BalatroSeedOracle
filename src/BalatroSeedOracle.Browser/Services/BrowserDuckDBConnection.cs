using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.DuckDB;

namespace BalatroSeedOracle.Browser.Services;

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

    public async Task<long> GetRowCountAsync(string tableName)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        return await ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM {tableName}");
    }

    public Task EnsureTableExistsAsync(string createTableSql)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        return ExecuteNonQueryAsync(createTableSql);
    }

    public async Task<List<string>> GetAllSeedsAsync(string tableName, string seedColumnName, string? orderBy = null)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        var query = $"SELECT {seedColumnName} FROM {tableName}";
        if (!string.IsNullOrEmpty(orderBy))
            query += $" {orderBy}";
        
        var results = await ExecuteReaderAsync<string>(query, reader => reader.GetString(0));
        return results.ToList();
    }

    public Task ClearTableAsync(string tableName)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        return ExecuteNonQueryAsync($"DELETE FROM {tableName}");
    }

    public Task CreateIndexAsync(string indexSql)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        return ExecuteNonQueryAsync(indexSql);
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'main'";
        var results = await ExecuteReaderAsync<string>(query, reader => reader.GetString(0));
        return results.ToList();
    }

    public async Task<List<Models.ResultWithTallies>> QueryResultsAsync(
        string tableName,
        int? minScore = null,
        string? deck = null,
        string? stake = null,
        int limit = 1000)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        // Build query - this is infrastructure layer, not business logic
        var conditions = new List<string>();
        if (minScore.HasValue && minScore.Value > 0)
            conditions.Add($"score >= {minScore.Value}");
        if (!string.IsNullOrEmpty(deck) && deck != "Red")
            conditions.Add($"deck = '{deck}'");
        if (!string.IsNullOrEmpty(stake) && stake != "White")
            conditions.Add($"stake = '{stake}'");
        
        var whereClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        var query = $"SELECT * FROM {tableName}{whereClause} ORDER BY score DESC, seed ASC LIMIT {limit}";
        
        // Execute and convert to ResultWithTallies
        var rows = await ExecuteReaderAsync<Dictionary<string, object>>(query, reader =>
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            }
            return row;
        });
        
        // Convert to ResultWithTallies format
        var results = new List<Models.ResultWithTallies>();
        foreach (var row in rows)
        {
            var seed = row.TryGetValue("seed", out var s) ? s?.ToString() ?? "" : "";
            var score = row.TryGetValue("score", out var sc) ? Convert.ToInt32(sc) : 0;
            var tallies = row.Where(kvp => kvp.Key != "seed" && kvp.Key != "score" && kvp.Value != null)
                .Select(kvp => Convert.ToInt32(kvp.Value))
                .ToList();
            
            results.Add(new Models.ResultWithTallies
            {
                Seed = seed,
                Score = score,
                Tallies = tallies
            });
        }
        
        return results;
    }

    public async Task<Dictionary<string, object?>?> LoadRowByIdAsync(string tableName, string idColumn, int id)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        var query = $"SELECT * FROM {tableName} WHERE {idColumn} = {id} LIMIT 1";
        var results = await ExecuteReaderAsync<Dictionary<string, object?>>(query, reader =>
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            return row;
        });
        return results.FirstOrDefault();
    }

    public async Task UpsertRowAsync(string tableName, Dictionary<string, object?> values, string keyColumn)
    {
        // Browser: Use SQL (Motely helpers not available in browser)
        var columns = string.Join(", ", values.Keys);
        var valuePlaceholders = string.Join(", ", values.Select(kvp => 
            kvp.Value == null ? "NULL" : 
            kvp.Value is string ? $"'{kvp.Value.ToString()!.Replace("'", "''")}'" :
            kvp.Value.ToString()));
        var updates = string.Join(", ", values.Keys.Where(k => k != keyColumn).Select(k => 
            $"{k} = excluded.{k}"));
        
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({valuePlaceholders}) " +
                  $"ON CONFLICT ({keyColumn}) DO UPDATE SET {updates}";
        await ExecuteNonQueryAsync(sql);
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
    private static extern Task ExecuteAsync(int connId, string sql);

    [JSImport("DuckDB.query", "globalThis")]
    private static extern Task<string> QueryAsync(int connId, string sql);

    [JSImport("DuckDB.closeConnection", "globalThis")]
    private static extern Task CloseConnectionAsync(int connId);

    [JSImport("DuckDB.createAppender", "globalThis")]
    private static extern Task<int> CreateAppenderInternalAsync(int connId, string schema, string table);

    [JSImport("DuckDB.exportToCSV", "globalThis")]
    private static extern Task<string> ExportToCSVInternalAsync(int connId, string tableName);
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
