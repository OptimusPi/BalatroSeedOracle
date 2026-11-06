using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BalatroSeedOracle.Helpers;
using Motely.Lake;
using Motely.SeedProviders;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Service wrapping Motely.Lake.ISeedLakeService / SeedLakeService for reading
/// pre-computed seed lakes (Parquet, CSV, TXT) from D:\dungmot.
/// </summary>
public sealed class DungmotLakeService : IDungmotLakeService
{
    private string _lakeRootPath;
    private ISeedLakeService _lakeService;

    public DungmotLakeService(string? rootPath = null, ISeedLakeService? underlying = null)
    {
        _lakeRootPath = !string.IsNullOrWhiteSpace(rootPath)
            ? rootPath
            : (Directory.Exists(@"D:\dungmot") ? @"D:\dungmot" : AppContext.BaseDirectory);

        _lakeService = underlying ?? new SeedLakeService(_lakeRootPath);
        DebugLogger.Log(nameof(DungmotLakeService), $"Initialized with root path: {_lakeRootPath}");
    }

    /// <inheritdoc/>
    public string LakeRootPath
    {
        get => _lakeRootPath;
        set
        {
            if (_lakeRootPath != value)
            {
                _lakeRootPath = value;
                _lakeService = new SeedLakeService(_lakeRootPath);
                DebugLogger.Log(nameof(DungmotLakeService), $"Updated root path to: {_lakeRootPath}");
            }
        }
    }

    /// <inheritdoc/>
    public bool IsLakeAvailable
    {
        get
        {
            try
            {
                return Directory.Exists(_lakeRootPath) && _lakeService.GetAvailableLakes().Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<LakePartitionInfo> GetAvailablePartitions()
    {
        try
        {
            var lakes = _lakeService.GetAvailableLakes();
            return lakes.Select(m => new LakePartitionInfo(
                Name: m.Name,
                Category: m.Category,
                FilePath: m.Path,
                IsParquet: m.IsParquet,
                EstimatedSeeds: m.EstimatedSeedCount,
                PartitionKey: m.Partition
            )).ToList();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(nameof(DungmotLakeService), $"Failed to enumerate lake partitions: {ex.Message}");
            return Array.Empty<LakePartitionInfo>();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> StreamSeeds(LakePartitionInfo partition, int maxSeeds = -1)
    {
        ArgumentNullException.ThrowIfNull(partition);
        return _lakeService.ReadSeeds(partition.Category, partition.PartitionKey, maxSeeds > 0 ? maxSeeds : null);
    }

    /// <inheritdoc/>
    public IReadOnlyList<LakeMetadata> GetAvailableLakes() => _lakeService.GetAvailableLakes();

    /// <inheritdoc/>
    public IEnumerable<string> ReadSeeds(LakeCategory category, string partition, int? limit = null) =>
        _lakeService.ReadSeeds(category, partition, limit);

    /// <inheritdoc/>
    public IMotelySeedProvider CreateSeedProvider(LakePartitionInfo partition, int? limit = null)
    {
        ArgumentNullException.ThrowIfNull(partition);
        return _lakeService.CreateSeedProvider(partition.Category, partition.PartitionKey, limit);
    }
}
