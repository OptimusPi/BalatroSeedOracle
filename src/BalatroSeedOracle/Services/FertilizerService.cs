using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services.DuckDB;

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

    public FertilizerService(IDuckDBService duckDB)
    {
        _duckDB = duckDB ?? throw new ArgumentNullException(nameof(duckDB));
    }

    /// <summary>
    /// Initialize the fertilizer service (call this before using any other methods)
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            await _duckDB.InitializeAsync();

#if !BROWSER
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BalatroSeedOracle"
            );
            Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "fertilizer.db");
            var txtPath = Path.Combine(dataDir, "fertilizer.txt");
#else
            var dbPath = ":memory:";
            var txtPath = string.Empty;
#endif

            var connectionString = _duckDB.CreateConnectionString(dbPath);
            _connection = await _duckDB.OpenConnectionAsync(connectionString);

            // Create seeds table if not exists
            await _connection.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS seeds (
                    seed VARCHAR PRIMARY KEY
                );
                CREATE INDEX IF NOT EXISTS idx_seed ON seeds(seed);
            ");

#if !BROWSER
            // Import from txt file if db is empty and txt exists
            await MigrateFromTxtIfNeededAsync(txtPath);
#endif

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

#if !BROWSER
    private async Task MigrateFromTxtIfNeededAsync(string txtPath)
    {
        try
        {
            if (!File.Exists(txtPath)) return;
            if (_connection == null) return;

            var count = await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM seeds");
            if (count > 0) return; // DB already has data

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
#endif

    /// <summary>
    /// Add seeds to the fertilizer pile (deduplicates automatically)
    /// </summary>
    public async Task AddSeedsAsync(IEnumerable<string> seeds, CancellationToken ct = default)
    {
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null) return;
        }

        try
        {
            var seedList = seeds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (seedList.Count == 0) return;

#if !BROWSER
            await Task.Run(async () =>
            {
                // Write seeds to a temp CSV file
                var tempFile = Path.GetTempFileName();
                File.WriteAllLines(tempFile, seedList);

                // Use DuckDB's native COPY for ultra-fast bulk import
                var escapedPath = tempFile.Replace("\\", "/");
                await _connection.CopyFromFileAsync(
                    escapedPath,
                    "seeds",
                    "DELIMITER E'\\n', HEADER false, QUOTE '', FORMAT csv) ON CONFLICT DO NOTHING"
                );

                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            }, ct);
#else
            // Browser: Use appender for bulk insert
            await using var appender = await _connection.CreateAppenderAsync("main", "seeds");
            foreach (var seed in seedList)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    appender.CreateRow()
                        .AppendValue(seed)
                        .EndRow();
                }
                catch
                {
                    // Ignore duplicates (PRIMARY KEY violation)
                }
            }
            appender.Flush();
#endif

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
            if (_connection == null) return seeds;
        }

        try
        {
            var results = await _connection.ExecuteReaderAsync(
                "SELECT seed FROM seeds",
                reader => reader.GetString(0));
            seeds.AddRange(results);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to get seeds: {ex.Message}");
        }

        return seeds;
    }

    /// <summary>
    /// Get all seeds synchronously (for legacy compatibility)
    /// </summary>
    public List<string> GetAllSeeds()
    {
        return GetAllSeedsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clear all seeds from the fertilizer pile
    /// </summary>
    public async Task ClearAsync()
    {
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null) return;
        }

        try
        {
            await _connection.ExecuteNonQueryAsync("DELETE FROM seeds");

#if !BROWSER
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
#endif

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
#if !BROWSER
        if (_connection == null)
        {
            await InitializeAsync();
            if (_connection == null) return;
        }

        try
        {
            var seeds = await GetAllSeedsAsync();
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BalatroSeedOracle"
            );
            var txtPath = Path.Combine(dataDir, "fertilizer.txt");
            await File.WriteAllLinesAsync(txtPath, seeds);
            DebugLogger.Log("FertilizerService", $"Exported {seeds.Count} seeds to txt");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to export: {ex.Message}");
        }
#else
        // Browser: No file system access - could export to download in future
        await Task.CompletedTask;
        DebugLogger.Log("FertilizerService", "Export not available in browser");
#endif
    }

    private async Task RefreshSeedCountAsync()
    {
        if (_connection == null) return;

        try
        {
            var count = await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM seeds");

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
        if (_disposed) return;

        try
        {
            _connection?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
        }
        catch { }

        _initLock.Dispose();
        _disposed = true;
    }
}
