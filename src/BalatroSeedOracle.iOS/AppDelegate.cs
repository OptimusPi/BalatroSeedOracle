using System;
using Avalonia;
using Avalonia.iOS;
using BalatroSeedOracle;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Export;
using BalatroSeedOracle.Services.Storage;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace BalatroSeedOracle.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register iOS services using Avalonia best practices
        PlatformServices.RegisterServices = services =>
        {
            // iOS-specific storage with full file system access
            services.AddSingleton<IAppDataStore, Services.iOSAppDataStoreNative>();

            // iOS-specific implementations
            services.AddSingleton<IDuckDBService, iOSDuckDBService>();
            services.AddSingleton<IPlatformServices, iOSPlatformServices>();
            services.AddSingleton<IExcelExporter, iOSExcelExporter>();
            services.AddSingleton<IAudioManager, iOSAudioManager>();
        };

        return base.CustomizeAppBuilder(builder);
    }
}

/// <summary>iOS DuckDB service - stub until native DuckDB works on iOS</summary>
internal sealed class iOSDuckDBService : IDuckDBService
{
    public bool IsAvailable => false;

    public System.Threading.Tasks.Task InitializeAsync() =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<IDuckDBConnection> OpenConnectionAsync(
        string connectionString
    ) => System.Threading.Tasks.Task.FromResult<IDuckDBConnection>(new iOSDuckDBConnection());

    public string CreateConnectionString(string databasePath) => databasePath;

    public System.Threading.Tasks.Task<IDuckDBConnection> OpenDuckLakeConnectionAsync(
        string catalogPath,
        string dataPath,
        string schemaName = "seed_source"
    ) => System.Threading.Tasks.Task.FromResult<IDuckDBConnection>(new iOSDuckDBConnection());
}

internal sealed class iOSDuckDBConnection : IDuckDBConnection
{
    public bool IsOpen => false;

    public void Dispose() { }

    public System.Threading.Tasks.ValueTask DisposeAsync() =>
        System.Threading.Tasks.ValueTask.CompletedTask;

    public System.Threading.Tasks.Task ExecuteNonQueryAsync(string sql) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<T?> ExecuteScalarAsync<T>(string sql) =>
        System.Threading.Tasks.Task.FromResult<T?>(default);

    public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<T>> ExecuteReaderAsync<T>(
        string sql,
        Func<IDuckDBDataReader, T> mapper
    ) =>
        System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IEnumerable<T>>(
            System.Array.Empty<T>()
        );

    public System.Threading.Tasks.Task<IDuckDBAppender> CreateAppenderAsync(
        string schema,
        string tableName
    ) => System.Threading.Tasks.Task.FromResult<IDuckDBAppender>(new iOSDuckDBAppender());

    public System.Threading.Tasks.Task CopyFromFileAsync(
        string filePath,
        string tableName,
        string options = ""
    ) => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task CopyToFileAsync(
        string tableName,
        string filePath,
        string format = "csv"
    ) => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<long> GetRowCountAsync(string tableName) =>
        System.Threading.Tasks.Task.FromResult(0L);

    public System.Threading.Tasks.Task EnsureTableExistsAsync(string createTableSql) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<System.Collections.Generic.List<string>> GetAllSeedsAsync(
        string tableName,
        string seedColumnName,
        string? orderBy = null
    ) => System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<string>());

    public System.Threading.Tasks.Task ClearTableAsync(string tableName) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task CreateIndexAsync(string indexSql) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<System.Collections.Generic.List<string>> GetTableNamesAsync() =>
        System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.List<string>());

    public System.Threading.Tasks.Task<System.Collections.Generic.List<BalatroSeedOracle.Models.ResultWithTallies>> QueryResultsAsync(
        string tableName,
        int? minScore = null,
        string? deck = null,
        string? stake = null,
        int limit = 1000
    ) =>
        System.Threading.Tasks.Task.FromResult(
            new System.Collections.Generic.List<BalatroSeedOracle.Models.ResultWithTallies>()
        );

    public System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<
        string,
        object?
    >?> LoadRowByIdAsync(string tableName, string idColumn, int id) =>
        System.Threading.Tasks.Task.FromResult<System.Collections.Generic.Dictionary<
            string,
            object?
        >?>(null);

    public System.Threading.Tasks.Task UpsertRowAsync(
        string tableName,
        System.Collections.Generic.Dictionary<string, object?> values,
        string keyColumn
    ) => System.Threading.Tasks.Task.CompletedTask;
}

internal sealed class iOSDuckDBAppender : IDuckDBAppender
{
    public void Dispose() { }

    public System.Threading.Tasks.ValueTask DisposeAsync() =>
        System.Threading.Tasks.ValueTask.CompletedTask;

    public System.Threading.Tasks.Task FlushAsync() => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task CloseAsync() => System.Threading.Tasks.Task.CompletedTask;

    public IDuckDBAppender CreateRow() => this;

    public IDuckDBAppender AppendValue(string? value) => this;

    public IDuckDBAppender AppendValue(int value) => this;

    public IDuckDBAppender AppendValue(long value) => this;

    public IDuckDBAppender AppendValue(double value) => this;

    public IDuckDBAppender AppendValue(bool value) => this;

    public IDuckDBAppender AppendNullValue() => this;

    public IDuckDBAppender EndRow() => this;
}

/// <summary>iOS platform services</summary>
internal sealed class iOSPlatformServices : IPlatformServices
{
    public bool SupportsFileSystem => true;
    public bool SupportsAudio => true;
    public bool SupportsAnalyzer => false;
    public bool SupportsResultsGrid => true;

    public string GetTempDirectory() => System.IO.Path.GetTempPath();

    public void EnsureDirectoryExists(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    public System.Threading.Tasks.Task WriteCrashLogAsync(string message) =>
        System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<string?> ReadTextFromPathAsync(string path) =>
        System.Threading.Tasks.Task.FromResult<string?>(null);

    public System.Threading.Tasks.Task<bool> FileExistsAsync(string path) =>
        System.Threading.Tasks.Task.FromResult(false);

    public void WriteLog(string message) { }

    public void WriteDebugLog(string message) { }
}

/// <summary>iOS Excel exporter - stub</summary>
internal sealed class iOSExcelExporter : IExcelExporter
{
    public bool IsAvailable => false;

    public System.Threading.Tasks.Task ExportAsync(
        string filePath,
        string sheetName,
        System.Collections.Generic.IReadOnlyList<string> headers,
        System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<object?>> rows
    ) => System.Threading.Tasks.Task.CompletedTask;
}

/// <summary>iOS Audio manager - stub</summary>
internal sealed class iOSAudioManager : IAudioManager
{
    public float MasterVolume { get; set; } = 1.0f;

    public void SetTrackVolume(string trackName, float volume) { }

    public void PlaySfx(string name, float volume) { }

    public event System.Action<float, float, float, float>? AudioAnalysisUpdated;
}
