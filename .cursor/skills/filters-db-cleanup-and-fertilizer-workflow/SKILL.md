---
name: filters-db-cleanup-and-fertilizer-workflow
description: Manages DuckDB cleanup and fertilizer seed export when filter criteria change. Use when modifying filter persistence, results storage, or database lifecycle.
---

# Filter Database Cleanup and Fertilizer Workflow

## Overview

When filter criteria change, stale DuckDB results must be cleaned up to prevent incorrect data. Seeds are first exported to "fertilizer" for reuse.

## Database Files

| File Pattern                                       | Purpose                 |
| -------------------------------------------------- | ----------------------- |
| `SearchResults/{filter}_{deck}_{stake}.duckdb`     | Search results database |
| `SearchResults/{filter}_{deck}_{stake}.duckdb.wal` | Write-Ahead Log         |

## Cleanup Flow

```
Criteria Changed → Stop Searches → Dump to Fertilizer → Delete DB Files
```

### Implementation (from FiltersModalViewModel)

```csharp
private async Task CleanupFilterDatabases()
{
    if (string.IsNullOrEmpty(CurrentFilterPath))
        return;

    var filterName = Path.GetFileNameWithoutExtension(CurrentFilterPath);
    var searchResultsDir = AppPaths.SearchResultsDir;

    // 1. Stop ALL running searches for this filter
    var searchManager = ServiceHelper.GetService<SearchManager>();
    if (searchManager is not null)
    {
        var stoppedSearches = searchManager.StopSearchesForFilter(filterName);
        DebugLogger.Log("Cleanup", $"Stopped {stoppedSearches} running searches");
    }

    // 2. Dump seeds to fertilizer.txt BEFORE deleting
    var dbFiles = Directory.GetFiles(searchResultsDir, $"{filterName}_*.duckdb");
    await DumpDatabasesToFertilizerAsync(dbFiles);

    // 3. Delete database and WAL files
    foreach (var dbFile in dbFiles)
    {
        File.Delete(dbFile);
    }
    foreach (var walFile in Directory.GetFiles(searchResultsDir, $"{filterName}_*.duckdb.wal"))
    {
        File.Delete(walFile);
    }
}
```

## Fertilizer Export

Seeds are exported to `WordLists/fertilizer.txt` before deletion:

```csharp
private async Task DumpDatabasesToFertilizerAsync(string[] dbFiles)
{
    // Platform guard - browser has no filesystem
    if (!_platformServices.SupportsFileSystem)
        return;

    var fertilizerPath = Path.Combine(AppPaths.WordListsDir, "fertilizer.txt");
    var allSeeds = new List<string>();

    foreach (var dbFile in dbFiles)
    {
        using var connection = new DuckDBConnection($"Data Source={dbFile}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT seed FROM results ORDER BY seed";

        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var seed = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(seed))
                allSeeds.Add(seed);
        }
    }

    // Append to fertilizer file
    await File.AppendAllLinesAsync(fertilizerPath, allSeeds).ConfigureAwait(false);
}
```

## Criteria Change Detection

Use hash to detect meaningful changes vs metadata edits:

```csharp
// On save
var currentHash = ComputeCriteriaHash();
var criteriaChanged = _originalCriteriaHash is null || currentHash != _originalCriteriaHash;

if (criteriaChanged)
{
    DebugLogger.LogImportant("Filter", "Criteria changed - cleaning up databases");
    await CleanupFilterDatabases();
}
else
{
    DebugLogger.Log("Filter", "Metadata only - keeping databases");
}
```

## Platform Safety

Always guard filesystem operations:

```csharp
if (!_platformServices.SupportsFileSystem)
{
    // Browser: skip DB operations entirely
    return;
}
```

## DuckDB Connection Best Practices

```csharp
// Always use 'using' for proper disposal
using var connection = new DuckDBConnection(connectionString);
connection.Open();

// Dispose readers immediately
using var reader = await cmd.ExecuteReaderAsync();

// Manual checkpoint after batch operations
connection.Execute("CHECKPOINT;");
```

## Checklist

- [ ] Platform guard with `SupportsFileSystem`
- [ ] Stop running searches before cleanup
- [ ] Export seeds to fertilizer before deletion
- [ ] Delete both `.duckdb` and `.duckdb.wal` files
- [ ] Use criteria hash to detect real changes
- [ ] Proper DuckDB connection disposal
