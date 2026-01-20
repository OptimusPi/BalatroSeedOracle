---
name: duckdb-results-debugging
description: Debugs DuckDB results issues including empty results, missing data, and database file problems. Use when results are wrong, missing, or UI shows empty despite DB file existing, especially when comparing desktop vs browser behavior.
---

# DuckDB Results Debugging

## When to Use

- Results are missing or incorrect
- DB file exists but UI shows empty
- Desktop and Browser show different results
- Results disappear after criteria changes

## Key Locations

| Location                                                         | Description                                |
| ---------------------------------------------------------------- | ------------------------------------------ |
| `AppPaths.SearchResultsDir`                                      | Results DB storage (`Data/SearchResults/`) |
| `src/BalatroSeedOracle/Services/DuckDB/IDuckDBService.cs`        | DuckDB interface                           |
| `src/BalatroSeedOracle.Desktop/Services/DesktopDuckDBService.cs` | Desktop implementation                     |
| `src/BalatroSeedOracle.Browser/Services/BrowserDuckDBService.cs` | Browser implementation (limited)           |

## Diagnostic Checklist

### 1. Verify Platform Support

```csharp
// Check if platform supports file system
if (!_platformServices.SupportsFileSystem)
{
    // Browser: No direct file I/O - uses virtual paths
    // Desktop: Full file system access
}
```

Browser uses virtual path `/data` with no actual file I/O.

### 2. Locate Result DBs

On Desktop, results are stored in:

```
{DataRoot}/SearchResults/
├── {FilterHash}.duckdb      # Result database
├── {FilterHash}.duckdb.wal  # Write-ahead log (if uncommitted)
```

### 3. Check What Invalidates Results

Results are invalidated when:

- Filter criteria change (schema or values)
- Search parameters change
- Manual cache clear
- DB file is deleted or corrupted

### 4. Safe Inspection (Read-Only)

```sql
-- Check row count
SELECT COUNT(*) FROM results;

-- View schema
DESCRIBE results;

-- Sample data
SELECT * FROM results LIMIT 10;
```

**Never write to the DB while the app is running** - can cause corruption.

## Common Issues

### Empty Results Despite DB Exists

| Check                   | Fix                                    |
| ----------------------- | -------------------------------------- |
| WAL not checkpointed    | App crash before commit - restart app  |
| Schema mismatch         | Clear cache, re-run search             |
| Wrong filter hash       | Criteria changed - results invalidated |
| Transaction uncommitted | Wait for search completion             |

### Desktop vs Browser Differences

| Feature      | Desktop                  | Browser          |
| ------------ | ------------------------ | ---------------- |
| File system  | ✅ Full                  | ❌ Virtual paths |
| DuckDB       | Native                   | WASM (limited)   |
| Results grid | ✅ `SupportsResultsGrid` | ❌ Not supported |
| Persistence  | File-based               | localStorage     |

### WAL File Issues

- `.wal` file present = uncommitted changes
- Force checkpoint: app restart or clean shutdown
- Large WAL = potential crash during write

## Debug Logging

Add timing/state logs:

```csharp
DebugLogger.Log("DuckDB", $"Query start: {query}");
// ... execute
DebugLogger.Log("DuckDB", $"Query complete: {rowCount} rows in {elapsed}ms");
```

## Browser-Specific Guards

Always check platform before file operations:

```csharp
if (_platformServices.SupportsFileSystem)
{
    // Desktop: Use file-based DuckDB
    var dbPath = Path.Combine(AppPaths.SearchResultsDir, $"{hash}.duckdb");
}
else
{
    // Browser: Results not persisted to file system
    // Use in-memory or localStorage approach
}
```

## Recovery Steps

1. **Close the application** (ensures WAL is checkpointed)
2. **Check DB file exists** at expected path
3. **Verify file size** (0 bytes = corruption)
4. **Try re-opening** - DuckDB auto-recovers from WAL
5. **If still broken**: Delete `.duckdb` and `.wal` files, re-run search

## Checklist

- [ ] Verified platform (`SupportsFileSystem`, `SupportsResultsGrid`)
- [ ] Located result DB file in `SearchResults/`
- [ ] Checked for `.wal` file (uncommitted data)
- [ ] Verified filter hash matches current criteria
- [ ] Confirmed search completed successfully
- [ ] Tested on correct platform (Desktop for file-based results)
