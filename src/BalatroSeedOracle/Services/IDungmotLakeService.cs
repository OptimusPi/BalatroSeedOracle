using System.Collections.Generic;
using Motely.Lake;
using Motely.SeedProviders;

namespace BalatroSeedOracle.Services;

/// <summary>
/// Information about a pre-computed seed lake dataset partition (TXT, CSV, or Parquet).
/// </summary>
public sealed record LakePartitionInfo(
    string Name,
    LakeCategory Category,
    string FilePath,
    bool IsParquet,
    long EstimatedSeeds,
    string PartitionKey = ""
);

/// <summary>
/// Service reading pre-computed seeds from D:\dungmot seed lakes (Parquet, CSV, TXT)
/// and streaming them into the search engine.
/// </summary>
public interface IDungmotLakeService
{
    /// <summary>Root folder containing dungmot lakes (e.g. D:\dungmot).</summary>
    string LakeRootPath { get; set; }

    /// <summary>True if the lake root path exists and contains readable lake partitions.</summary>
    bool IsLakeAvailable { get; }

    /// <summary>Returns all available lake partitions across TXT, CSV, and Parquet lakes.</summary>
    IReadOnlyList<LakePartitionInfo> GetAvailablePartitions();

    /// <summary>Streams seeds from the specified partition up to maxSeeds (or all if -1).</summary>
    IEnumerable<string> StreamSeeds(LakePartitionInfo partition, int maxSeeds = -1);

    /// <summary>Direct access to underlying Motely lake metadata.</summary>
    IReadOnlyList<LakeMetadata> GetAvailableLakes();

    /// <summary>Direct query for seeds by category and partition key.</summary>
    IEnumerable<string> ReadSeeds(LakeCategory category, string partition, int? limit = null);

    /// <summary>Creates a Motely seed provider for direct feeding into MotelySearchSettings.</summary>
    IMotelySeedProvider CreateSeedProvider(LakePartitionInfo partition, int? limit = null);
}
