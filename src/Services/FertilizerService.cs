using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using DuckDB.NET.Data;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Manages the global fertilizer seed pile using DuckDB for fast storage.
/// The fertilizer is a GLOBAL pile of seeds from all searches - never cleared automatically.
/// Seeds from the top 1000 results of every search get added to grow the pile.
/// </summary>
public class FertilizerService : IDisposable
{
    private static FertilizerService? _instance;
    private static readonly object _lock = new();

    public static FertilizerService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new FertilizerService();
                }
            }
            return _instance;
        }
    }

    private readonly string _dbPath;
    private readonly string _txtPath;
    private DuckDBConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Event raised when the seed count changes
    /// </summary>
    public event EventHandler<long>? SeedCountChanged;

    /// <summary>
    /// Current seed count in the fertilizer pile
    /// </summary>
    public long SeedCount { get; private set; }

    private FertilizerService()
    {
        var dataDir = Helpers.AppPaths.DataRootDir;

        _dbPath = Path.Combine(dataDir, "fertilizer.db");
        _txtPath = Path.Combine(dataDir, "fertilizer.txt");

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var connectionString = $"Data Source={_dbPath}";
            _connection = new DuckDBConnection(connectionString);
            _connection.Open();

            // Create seeds table if not exists
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS seeds (
                    seed VARCHAR PRIMARY KEY
                );
                CREATE INDEX IF NOT EXISTS idx_seed ON seeds(seed);
            ";
            cmd.ExecuteNonQuery();

            // Import from txt file if db is empty and txt exists
            MigrateFromTxtIfNeeded();

            // Update count
            RefreshSeedCount();

            DebugLogger.Log("FertilizerService", $"Initialized with {SeedCount} seeds");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to initialize: {ex.Message}");
        }
    }

    private void MigrateFromTxtIfNeeded()
    {
        try
        {
            if (!File.Exists(_txtPath)) return;

            using var countCmd = _connection!.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM seeds";
            var count = (long)countCmd.ExecuteScalar()!;

            if (count > 0) return; // DB already has data

            var seeds = File.ReadAllLines(_txtPath)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (seeds.Count == 0) return;

            DebugLogger.Log("FertilizerService", $"Migrating {seeds.Count} seeds from txt to db");

            using var appender = _connection.CreateAppender("seeds");
            foreach (var seed in seeds)
            {
                var row = appender.CreateRow();
                row.AppendValue(seed);
                row.EndRow();
            }
            appender.Close();

            DebugLogger.Log("FertilizerService", "Migration complete");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Migration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Add seeds to the fertilizer pile (deduplicates automatically)
    /// </summary>
    public async Task AddSeedsAsync(IEnumerable<string> seeds, CancellationToken ct = default)
    {
        if (_connection == null) return;

        try
        {
            var seedList = seeds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (seedList.Count == 0) return;

            await Task.Run(() =>
            {
                // Use INSERT OR IGNORE for deduplication
                using var cmd = _connection.CreateCommand();
                foreach (var seed in seedList)
                {
                    if (ct.IsCancellationRequested) break;

                    cmd.CommandText = $"INSERT OR IGNORE INTO seeds VALUES ('{seed}')";
                    cmd.ExecuteNonQuery();
                }
            }, ct);

            RefreshSeedCount();
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
        if (_connection == null) return;

        try
        {
            var seedList = seeds.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (seedList.Count == 0) return;

            using var cmd = _connection.CreateCommand();
            foreach (var seed in seedList)
            {
                cmd.CommandText = $"INSERT OR IGNORE INTO seeds VALUES ('{seed}')";
                cmd.ExecuteNonQuery();
            }

            RefreshSeedCount();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to add seeds: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all seeds from the fertilizer pile
    /// </summary>
    public List<string> GetAllSeeds()
    {
        var seeds = new List<string>();
        if (_connection == null) return seeds;

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT seed FROM seeds";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                seeds.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to get seeds: {ex.Message}");
        }

        return seeds;
    }

    /// <summary>
    /// Clear all seeds from the fertilizer pile
    /// </summary>
    public async Task ClearAsync()
    {
        if (_connection == null) return;

        try
        {
            await Task.Run(() =>
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "DELETE FROM seeds";
                cmd.ExecuteNonQuery();
            });

            // Also clear the txt file if it exists
            if (File.Exists(_txtPath))
            {
                File.Delete(_txtPath);
            }

            RefreshSeedCount();
            DebugLogger.Log("FertilizerService", "Fertilizer pile cleared");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to clear: {ex.Message}");
        }
    }

    /// <summary>
    /// Export seeds to txt file for compatibility with MotelyAPI
    /// </summary>
    public async Task ExportToTxtAsync()
    {
        if (_connection == null) return;

        try
        {
            var seeds = await Task.Run(() => GetAllSeeds());
            await File.WriteAllLinesAsync(_txtPath, seeds);
            DebugLogger.Log("FertilizerService", $"Exported {seeds.Count} seeds to txt");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FertilizerService", $"Failed to export: {ex.Message}");
        }
    }

    private void RefreshSeedCount()
    {
        if (_connection == null) return;

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM seeds";
            var count = (long)cmd.ExecuteScalar()!;

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
            _connection?.Close();
            _connection?.Dispose();
        }
        catch { }

        _disposed = true;
    }
}
