using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Abstraction for DuckDB service that works across platforms.
/// Desktop uses DuckDB.NET, Browser uses JS interop to DuckDB-WASM.
/// </summary>
public interface IDuckDBService
{
    /// <summary>
    /// Whether the DuckDB service is available and initialized
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initialize the DuckDB service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Open a connection to a database
    /// </summary>
    /// <param name="connectionString">Connection string (path for desktop, identifier for browser)</param>
    /// <returns>Connection instance</returns>
    Task<IDuckDBConnection> OpenConnectionAsync(string connectionString);

    /// <summary>
    /// Create a connection string for a database file
    /// </summary>
    /// <param name="databasePath">Path to the database file</param>
    string CreateConnectionString(string databasePath);

    /// <summary>
    /// Open a connection to a DuckLake catalog (enables multiple concurrent read/write connections)
    /// </summary>
    /// <param name="catalogPath">Path to DuckLake catalog file (.ducklake)</param>
    /// <param name="dataPath">Path to DuckLake data directory (Parquet files)</param>
    /// <param name="schemaName">Schema name to attach as (default: "seed_source")</param>
    /// <returns>Connection instance with DuckLake attached</returns>
    Task<IDuckDBConnection> OpenDuckLakeConnectionAsync(
        string catalogPath,
        string dataPath,
        string schemaName = "seed_source");
}
