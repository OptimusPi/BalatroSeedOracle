using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services.DuckDB;
using Motely.DuckDB;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Manages the global fertilizer seed pile using DuckDB for fast storage.
/// The fertilizer is a GLOBAL pile of seeds from all searches - never cleared automatically.
/// Seeds from the top 1000 results of every search get added to grow the pile.
/// Uses IDuckDBService abstraction for cross-platform compatibility (Desktop and Browser).
/// </summary>
public class FertilizerService : IDisposable
{
    private readonly IDuckDBService _duckDB;
    private readonly IPlatformServices _platformServices;
    private IDuckDBConnection? _connection;
    private bool _disposed;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Event raised when the seed count changes
    /// </summary>
    public event EventHandler<long>? SeedCountChanged;

    /// <summary>
    /// Current seed count in the fertilizer pile
    /// </summary>
    public long SeedCount { get; private set; }

    public FertilizerService(IDuckDBService duckDB, IPlatformServices platformServices)
    {
        _duckDB = duckDB ?? throw new ArgumentNullException(nameof(duckDB));
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
    }

    /// <summary>
    /// Initialize the fertilizer service (call this before using any other methods)
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await _duckDB.InitializeAsync();

            string dbPath;
            string txtPath;
            if (_platformServices.SupportsFileSystem)
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BalatroSeedOracle"
                );
                _platformServices.EnsureDirectoryExists(dataDir);
                dbPath = Path.Combine(dataDir, "fertilizer.db");
                txtPath = Path.Combine(dataDir, "fertilizer.txt");
            }
            else
            {
                dbPath = ":memory:";
                txtPath = string.Empty;
            }

            var connectionString = _duckDB.CreateConnectionString(dbPath);
            _connection = await _duckDB.OpenConnectionAsync(connectionString);

            // Create seeds table using centralized schema from Motely
            var fertilizerSchema = DuckDBSchema.FertilizerTableSchema();
            await _connection.EnsureTableExistsAsync(fertilizerSchema);

            // Create index (schema doesn't include indexes, add separately)
            // Use high-level method - no SQL construction in BSO!
            await _connection.CreateIndexAsync("CREATE INDEX IF NOT EXISTS idx_seed ON seeds(seed);");

            if (_platformServices.SupportsFileSystem)
            {
                // Import from txt file if db is empty and txt exists
                await MigrateFromTxtIfNeededAsync(txtPath);
            }

            // Update count
            await RefreshSeedCountAsync();

            _initialized = true;
            DebugLogger.Log("FertilizerService", $"Initialized with {SeedCount} seeds");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to initialize: {ex.Message}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task MigrateFromTxtIfNeededAsync(string txtPath)
    {
        try
        {
            if (!File.Exists(txtPath))
                return;
            if (_connection == null)
                return;

            var count = await _connection.GetRowCountAsync("seeds");
            if (count > 0)
                return; // DB already has data

            DebugLogger.Log("FertilizerService", $"Migrating from {txtPath} using DuckDB COPY");

            // Use COPY FROM - DuckDB will treat each line as a single column value
            var escapedPath = txtPath.Replace("\\", "/").Replace("'", "''");
            await _connection.CopyFromFileAsync(
                escapedPath,
                "seeds",
                "HEADER false, AUTO_DETECT false, COLUMNS {'seed': 'VARCHAR'}"
            );

            DebugLogger.Log("FertilizerService", "Migration complete");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Migration failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Add seeds to the fertilizer pile (deduplicates automatically)
    /// </summary>
    public async Task AddSeedsAsync(IEnumerable<string> seeds, CancellationToken ct = default)
    {
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null)
                return;
        }

        try
        {
            var seedList = seeds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (seedList.Count == 0)
                return;

            if (_platformServices.SupportsFileSystem)
            {
                // Desktop: Bulk insert via temp file + COPY FROM (handles duplicates via ON CONFLICT in schema)
                var tempFile = Path.Combine(_platformServices.GetTempDirectory(), Path.GetRandomFileName());
                try
                {
                    // ASYNC file write - don't block the thread!
                    await File.WriteAllLinesAsync(tempFile, seedList, ct);

                    // Use high-level CopyFromFileAsync - no SQL construction in BSO!
                    // Note: DuckDB COPY handles duplicates if table has PRIMARY KEY/UNIQUE constraint
                    await _connection.CopyFromFileAsync(
                        tempFile,
                        "seeds",
                        "HEADER false, DELIMITER E'\\n', COLUMNS {'seed': 'VARCHAR'}"
                    );
                }
                finally
                {
                    // ALWAYS cleanup temp file, log failures
                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("FertilizerService", $"Temp file cleanup failed: {ex.Message}");
                    }
                }
            }
            else
            {
                // Browser: Use appender for bulk insert
                await using var appender = await _connection.CreateAppenderAsync("main", "seeds");
                foreach (var seed in seedList)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    try
                    {
                        appender.CreateRow().AppendValue(seed).EndRow();
                    }
                    catch
                    {
                        // Ignore duplicates (PRIMARY KEY violation)
                    }
                }
                appender.Flush();
            }

            await RefreshSeedCountAsync();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to add seeds: {ex.Message}");
        }
    }

    /// <summary>
    /// Add seeds synchronously (for use from non-async contexts)
    /// </summary>
    public void AddSeeds(IEnumerable<string> seeds)
    {
        // Fire and forget the async version
        _ = AddSeedsAsync(seeds, CancellationToken.None);
    }

    /// <summary>
    /// Get all seeds from the fertilizer pile
    /// </summary>
    public async Task<List<string>> GetAllSeedsAsync()
    {
        var seeds = new List<string>();
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null)
                return seeds;
        }

        try
        {
            var results = await _connection.GetAllSeedsAsync("seeds", "seed");
            seeds.AddRange(results);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to get seeds: {ex.Message}");
        }

        return seeds;
    }

    // Synchronous GetAllSeeds() removed - use GetAllSeedsAsync() instead

    /// <summary>
    /// Clear all seeds from the fertilizer pile
    /// </summary>
    public async Task ClearAsync()
    {
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null)
                return;
        }

        try
        {
            await _connection.ClearTableAsync("seeds");

            if (_platformServices.SupportsFileSystem)
            {
                // Also clear the txt file if it exists
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BalatroSeedOracle"
                );
                var txtPath = Path.Combine(dataDir, "fertilizer.txt");
                if (File.Exists(txtPath))
                {
                    File.Delete(txtPath);
                }
            }

            await RefreshSeedCountAsync();
            DebugLogger.Log("FertilizerService", "Fertilizer pile cleared");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to clear: {ex.Message}");
        }
    }

    /// <summary>
    /// Export seeds to txt file for compatibility with MotelyAPI (desktop only)
    /// </summary>
    public async Task ExportToTxtAsync()
    {
        if (!_platformServices.SupportsFileSystem)
            return;

        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null)
                return;
        }

        try
        {
            var seeds = await GetAllSeedsAsync();
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BalatroSeedOracle"
            );
            _platformServices.EnsureDirectoryExists(dataDir);
            var txtPath = Path.Combine(dataDir, "fertilizer.txt");
            await File.WriteAllLinesAsync(txtPath, seeds);
            DebugLogger.Log("FertilizerService", $"Exported {seeds.Count} seeds to txt");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to export: {ex.Message}");
        }
    }

    private async Task RefreshSeedCountAsync()
    {
        if (_connection == null)
            return;

        try
        {
            var count = await _connection.GetRowCountAsync("seeds");

            if (count != SeedCount)
            {
                SeedCount = count;
                SeedCountChanged?.Invoke(this, count);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to count seeds: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _connection?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
        }
        catch { }

        _initLock.Dispose();
        _disposed = true;
    }
}
