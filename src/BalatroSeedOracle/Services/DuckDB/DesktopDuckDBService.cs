#if !BROWSER
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Motely.DuckDB;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Desktop implementation of IDuckDBService using DuckDB.NET.Data
/// Supports both standard DuckDB connections and DuckLake (multiplayer mode)
/// </summary>
public class DesktopDuckDBService : IDuckDBService
{
    public bool IsAvailable => true;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IDuckDBConnection> OpenConnectionAsync(string connectionString)
    {
        // Connection opens automatically on first use via EnsureOpen()
        var connection = new DesktopDuckDBConnection(connectionString);
        return Task.FromResult<IDuckDBConnection>(connection);
    }

    public string CreateConnectionString(string databasePath)
    {
        return $"Data Source={databasePath}";
    }

    /// <summary>
    /// Open a DuckLake connection - enables multiple concurrent read/write connections!
    /// This solves the "only one read/write connection" limitation of vanilla DuckDB.
    /// </summary>
    public Task<IDuckDBConnection> OpenDuckLakeConnectionAsync(
        string catalogPath,
        string dataPath,
        string schemaName = "seed_source")
    {
        // Use Motely's DuckDBConnectionFactory which handles DuckLake setup
        var duckDBConnection = DuckDBConnectionFactory.CreateConnectionWithDuckLake(
            catalogPath,
            dataPath,
            schemaName
        );

        // Wrap in our abstraction
        var connection = new DesktopDuckDBConnection(duckDBConnection);
        return Task.FromResult<IDuckDBConnection>(connection);
    }
}
#endif
