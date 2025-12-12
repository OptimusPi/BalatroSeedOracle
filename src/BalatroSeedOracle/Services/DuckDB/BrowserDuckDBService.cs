#if BROWSER
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

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

    [JSImport("DuckDB.initialize", "globalThis")]
    private static partial Task<bool> InitializeDuckDBAsync();

    [JSImport("DuckDB.openConnection", "globalThis")]
    private static partial Task<int> OpenConnectionInternalAsync();
}
#endif
