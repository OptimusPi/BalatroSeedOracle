using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Android.Services;

public class AndroidAppDataStore : IAppDataStore
{
    private readonly string _basePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BalatroSeedOracle"
    );

    private string GetPath(string key) => Path.Combine(_basePath, key);

    public ValueTask<bool> ExistsAsync(string key) => new(File.Exists(GetPath(key)));

    public async Task<string?> ReadTextAsync(string key)
    {
        var p = GetPath(key);
        return File.Exists(p) ? await File.ReadAllTextAsync(p) : null;
    }

    public async Task WriteTextAsync(string key, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GetPath(key))!);
        await File.WriteAllTextAsync(GetPath(key), content);
    }

    public ValueTask DeleteAsync(string key)
    {
        var p = GetPath(key);
        if (File.Exists(p))
            File.Delete(p);
        return default;
    }

    public ValueTask<IReadOnlyList<string>> ListKeysAsync(string prefix) =>
        new(
            Directory.Exists(_basePath)
                ? Directory
                    .GetFiles(_basePath, $"{prefix}*")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Select(f => f!)
                    .ToList()
                : new List<string>()
        );

    public ValueTask<bool> FileExistsAsync(string path) => new(File.Exists(path));
}

public class AndroidDuckDBService : IDuckDBService
{
    public bool IsAvailable => true;

    public Task InitializeAsync() => Task.CompletedTask;

    public string CreateConnectionString(string databasePath) => $"DataSource={databasePath}";

    public Task<IDuckDBConnection> OpenConnectionAsync(string connectionString) =>
        Task.FromResult<IDuckDBConnection>(new AndroidDuckDBConnection(connectionString));

    public Task<IDuckDBConnection> OpenDuckLakeConnectionAsync(
        string catalogPath,
        string dataPath,
        string schemaName = "seed_source"
    ) => OpenConnectionAsync(CreateConnectionString(catalogPath));
}

public class AndroidDuckDBConnection : IDuckDBConnection
{
    private readonly DuckDB.NET.Data.DuckDBConnection _connection;
    private bool _disposed;

    public AndroidDuckDBConnection(string cs)
    {
        _connection = new DuckDB.NET.Data.DuckDBConnection(cs);
        _connection.Open();
    }

    public bool IsOpen => _connection.State == System.Data.ConnectionState.Open;

    public async Task ExecuteNonQueryAsync(string sql)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? default : (T)Convert.ChangeType(r, typeof(T));
    }

    public async Task<IEnumerable<T>> ExecuteReaderAsync<T>(
        string sql,
        Func<IDuckDBDataReader, T> mapper
    )
    {
        var results = new List<T>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(mapper(new AndroidDataReader(reader)));
        return results;
    }

    public Task<IDuckDBAppender> CreateAppenderAsync(string schema, string tableName) =>
        Task.FromResult<IDuckDBAppender>(new AndroidAppender(_connection, tableName));

    public Task CopyFromFileAsync(string filePath, string tableName, string options = "") =>
        ExecuteNonQueryAsync($"COPY {tableName} FROM '{filePath}' {options}");

    public Task CopyToFileAsync(string tableName, string filePath, string format = "csv") =>
        ExecuteNonQueryAsync($"COPY {tableName} TO '{filePath}' (FORMAT {format})");

    public async Task<long> GetRowCountAsync(string tableName)
    {
        var r = await ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM {tableName}");
        return r;
    }

    public Task EnsureTableExistsAsync(string createTableSql) =>
        ExecuteNonQueryAsync(createTableSql);

    public async Task<List<string>> GetAllSeedsAsync(
        string tableName,
        string seedColumnName,
        string? orderBy = null
    )
    {
        var sql =
            $"SELECT DISTINCT {seedColumnName} FROM {tableName}"
            + (orderBy != null ? $" ORDER BY {orderBy}" : "");
        return (await QueryAsync(sql)).Select(r => r[seedColumnName]?.ToString() ?? "").ToList();
    }

    public Task ClearTableAsync(string tableName) =>
        ExecuteNonQueryAsync($"DELETE FROM {tableName}");

    public Task CreateIndexAsync(string indexSql) => ExecuteNonQueryAsync(indexSql);

    public async Task<List<string>> GetTableNamesAsync() =>
        (
            await QueryAsync(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'main'"
            )
        )
            .Select(r => r["table_name"]?.ToString() ?? "")
            .ToList();

    public Task<List<BalatroSeedOracle.Models.ResultWithTallies>> QueryResultsAsync(
        string tableName,
        int? minScore = null,
        string? deck = null,
        string? stake = null,
        int limit = 1000
    ) => Task.FromResult(new List<BalatroSeedOracle.Models.ResultWithTallies>());

    public async Task<Dictionary<string, object?>?> LoadRowByIdAsync(
        string tableName,
        string idColumn,
        int id
    ) =>
        (
            await QueryAsync($"SELECT * FROM {tableName} WHERE {idColumn} = {id} LIMIT 1")
        ).FirstOrDefault();

    public Task UpsertRowAsync(
        string tableName,
        Dictionary<string, object?> values,
        string keyColumn
    )
    {
        var cols = string.Join(", ", values.Keys);
        var vals = string.Join(", ", values.Values.Select(v => v == null ? "NULL" : $"'{v}'"));
        var updates = string.Join(
            ", ",
            values.Keys.Where(k => k != keyColumn).Select(k => $"{k} = excluded.{k}")
        );
        return ExecuteNonQueryAsync(
            $"INSERT INTO {tableName} ({cols}) VALUES ({vals}) ON CONFLICT ({keyColumn}) DO UPDATE SET {updates}"
        );
    }

    public async Task<(
        List<string> Columns,
        List<Dictionary<string, object?>> Rows
    )> ExecuteSqlAsync(string sql)
    {
        var columns = new List<string>();
        var rows = new List<Dictionary<string, object?>>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        for (int i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return (columns, rows);
    }

    private async Task<List<Dictionary<string, object?>>> QueryAsync(string sql)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Close();
            _connection.Dispose();
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _disposed = true;
        }
    }
}

public class AndroidDataReader : IDuckDBDataReader
{
    private readonly System.Data.Common.DbDataReader _r;

    public AndroidDataReader(System.Data.Common.DbDataReader r) => _r = r;

    public int FieldCount => _r.FieldCount;

    public string GetName(int ordinal) => _r.GetName(ordinal);

    public int GetOrdinal(string name) => _r.GetOrdinal(name);

    public bool IsDBNull(int ordinal) => _r.IsDBNull(ordinal);

    public string GetString(int ordinal) => _r.GetString(ordinal);

    public int GetInt32(int ordinal) => _r.GetInt32(ordinal);

    public long GetInt64(int ordinal) => _r.GetInt64(ordinal);

    public double GetDouble(int ordinal) => _r.GetDouble(ordinal);

    public bool GetBoolean(int ordinal) => _r.GetBoolean(ordinal);

    public object GetValue(int ordinal) => _r.GetValue(ordinal);
}

public class AndroidAppender : IDuckDBAppender
{
    public AndroidAppender(DuckDB.NET.Data.DuckDBConnection c, string t) { }

    public IDuckDBAppender CreateRow() => this;

    public IDuckDBAppender AppendValue(string? v) => this;

    public IDuckDBAppender AppendValue(int v) => this;

    public IDuckDBAppender AppendValue(long v) => this;

    public IDuckDBAppender AppendValue(double v) => this;

    public IDuckDBAppender AppendValue(bool v) => this;

    public IDuckDBAppender AppendNullValue() => this;

    public IDuckDBAppender EndRow() => this;

    public Task FlushAsync() => Task.CompletedTask;

    public Task CloseAsync() => Task.CompletedTask;

    public void Dispose() { }

    public ValueTask DisposeAsync() => default;
}

public class AndroidPlatformServices : IPlatformServices
{
    public bool SupportsFileSystem => true;
    public bool SupportsAudio => false;
    public bool SupportsAnalyzer => false;
    public bool SupportsResultsGrid => true;

    public string GetTempDirectory() => Path.GetTempPath();

    public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);

    public Task WriteCrashLogAsync(string content) =>
        File.WriteAllTextAsync(
            Path.Combine(GetTempDirectory(), $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
            content
        );

    public Task<string?> ReadTextFromPathAsync(string path) =>
        Task.FromResult<string?>(File.Exists(path) ? File.ReadAllText(path) : null);

    public Task<bool> FileExistsAsync(string path) => Task.FromResult(File.Exists(path));

    public void WriteLog(string message) { }

    public void WriteDebugLog(string message) { }
}

public class AndroidAudioManager : IAudioManager
{
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsPlaying => false;
    public float Bass1Intensity => 0;
    public float Bass2Intensity => 0;
    public float Drums1Intensity => 0;
    public float Drums2Intensity => 0;
    public float Chords1Intensity => 0;
    public float Chords2Intensity => 0;
    public float Melody1Intensity => 0;
    public float Melody2Intensity => 0;
    public float BassIntensity => 0;
    public float DrumsIntensity => 0;
    public float ChordsIntensity => 0;
    public float MelodyIntensity => 0;

    public void SetTrackVolume(string t, float v) { }

    public void SetTrackPan(string t, float p) { }

    public void SetTrackMuted(string t, bool m) { }

    public void Pause() { }

    public void Resume() { }

    public void PlaySfx(string n, float v = 1.0f) { }

    public FrequencyBands GetFrequencyBands(string t) => default;

    public event Action<float, float, float, float>? AudioAnalysisUpdated;
}

public class AndroidExcelExporter : IExcelExporter
{
    public bool IsAvailable => false;

    public Task ExportAsync(
        string filePath,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows
    ) => Task.CompletedTask;
}
