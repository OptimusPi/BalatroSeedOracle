using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using BalatroSeedOracle.Services.DuckDB;

namespace BalatroSeedOracle.Browser.Services;

/// <summary>
/// Browser implementation of IDuckDBService using DuckDB-WASM via JavaScript interop
/// </summary>
public partial class BrowserDuckDBService : IDuckDBService
{
    private bool _initialized;

    public bool IsAvailable => true;

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await InitializeDuckDBAsync();
        _initialized = true;
    }

    public async Task<IDuckDBConnection> OpenConnectionAsync(string connectionString)
    {
        if (!_initialized)
            await InitializeAsync();

        var connectionId = await OpenConnectionInternalAsync();
        return new BrowserDuckDBConnection(connectionId);
    }

    public string CreateConnectionString(string databasePath)
    {
        // Browser uses in-memory database with OPFS persistence
        return ":memory:";
    }

    /// <summary>
    /// DuckLake is not supported in browser builds (requires native DuckDB extension)
    /// </summary>
    public Task<IDuckDBConnection> OpenDuckLakeConnectionAsync(
        string catalogPath,
        string dataPath,
        string schemaName = "seed_source")
    {
        throw new NotSupportedException("DuckLake is not supported in browser builds. Use standard DuckDB connections instead.");
    }

    [JSImport("DuckDB.initialize", "globalThis")]
    private static partial Task<bool> InitializeDuckDBAsync();

    [JSImport("DuckDB.openConnection", "globalThis")]
    private static partial Task<int> OpenConnectionInternalAsync();
}
