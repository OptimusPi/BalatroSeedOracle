#if !BROWSER
using System.Threading.Tasks;

namespace BalatroSeedOracle.Services.DuckDB;

/// <summary>
/// Desktop implementation of IDuckDBService using DuckDB.NET.Data
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
}
#endif
