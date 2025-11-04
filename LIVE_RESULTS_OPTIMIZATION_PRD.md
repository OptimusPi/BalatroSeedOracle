# Live Results Query Optimization PRD

**Date**: 2025-11-03
**Author**: pifreak
**Status**: READY FOR IMPLEMENTATION
**Assigned**: C# Performance Specialist Agent

---

## Problem Statement

Currently, `SearchModalViewModel.OnProgressUpdated()` queries DuckDB for new results **every 2 seconds** regardless of whether any new results have been added since the last query.

**Current Wasteful Pattern** (Line 1372):
```csharp
// Fires every 500ms via ProgressUpdated event
private void OnProgressUpdated(object? sender, SearchProgress progress)
{
    // ...

    // WASTEFUL: Queries DuckDB every 2 seconds even if no new results
    if ((DateTime.Now - _lastResultsLoadTime).TotalSeconds >= 2)
    {
        var newResults = await _currentSearch.GetResultsPageAsync(offset, 100);
        // ...
    }
}
```

**Why This is Wasteful**:
- ProgressUpdated fires every 500ms (4x per 2-second window)
- Most progress updates add ZERO results (just batch progress tracking)
- DuckDB query overhead: `SELECT * FROM results ORDER BY score DESC LIMIT 100`
- DuckDB has to scan + sort even when no new rows exist
- UI thread marshalling overhead for identical data

**Actual Behavior**:
- Motely adds results in **batches** (batch size = 3 by default)
- Each batch processes ~1.3M seeds (421,875 seeds × 3)
- Results are only added when batch **completes**
- Between batches: ProgressUpdated fires ~600 times with NO new results
- **Result**: 300+ wasteful queries per hour during search

---

## Proposed Solution

### Invalidation Flag Pattern

Add a boolean flag `HasNewResultsSinceLastQuery` to `SearchInstance` that:
1. Gets set to `true` when `AddSearchResult()` writes to DuckDB
2. UI checks this flag every 1 second
3. Only queries DuckDB if flag is `true`
4. Resets flag to `false` after successful query

**Benefits**:
- Eliminates 95%+ of wasteful queries (only query when results actually exist)
- Reduces DuckDB load during long searches
- Reduces UI thread marshalling overhead
- Same user experience (results still appear within 1 second of being added)

---

## Implementation Plan

### Phase 1: Add Invalidation Flag to SearchInstance

**File**: `x:\BalatroSeedOracle\src\Services\SearchInstance.cs`

**Add Field** (after line 43):
```csharp
private volatile bool _hasNewResultsSinceLastQuery = false;
```

**Add Property** (after line 89):
```csharp
/// <summary>
/// Indicates whether new results have been added since the last UI query.
/// Used to avoid wasteful DuckDB queries when no new results exist.
/// </summary>
public bool HasNewResultsSinceLastQuery => _hasNewResultsSinceLastQuery;

/// <summary>
/// Resets the new results flag after UI has queried and displayed them.
/// </summary>
public void AcknowledgeResultsQueried()
{
    _hasNewResultsSinceLastQuery = false;
}
```

**Set Flag in AddSearchResult** (line 322, after incrementing _resultCount):
```csharp
private void AddSearchResult(SearchResult result)
{
    if (!_dbInitialized)
        return;

    try
    {
        lock (_appenderLock)
        {
            _appender ??= _connection.CreateAppender("results");

            var row = _appender.CreateRow();
            row.AppendValue(result.Seed).AppendValue(result.TotalScore);

            int tallyCount = _columnNames.Count - 2;
            for (int i = 0; i < tallyCount; i++)
            {
                int val = (result.Scores != null && i < result.Scores.Length) ? result.Scores[i] : 0;
                row.AppendValue(val);
            }
            row.EndRow();
        }

        // Invalidate query cache - new results are available
        _hasNewResultsSinceLastQuery = true; // ADD THIS LINE
    }
    catch (Exception ex)
    {
        // ... existing error handling
    }
}
```

**Set Flag in Batch Results Callback** (line 1634, in result callback):
```csharp
// Inside RunSearchInProcess(), MotelyJsonSeedScoreDesc result callback
resultCallback: (seed, score, individualScores) =>
{
    Interlocked.Increment(ref _resultCount);

    var result = new SearchResult
    {
        Seed = seed,
        TotalScore = score,
        Scores = individualScores?.ToArray() ?? Array.Empty<int>(),
        Labels = _columnNames.Skip(2).ToArray()
    };

    AddSearchResult(result);

    // Invalidation flag already set in AddSearchResult()
}
```

---

### Phase 2: Update SearchModalViewModel Query Logic

**File**: `x:\BalatroSeedOracle\src\ViewModels\SearchModalViewModel.cs`

**Replace Timer-Based Query** (line 1372) with **Invalidation-Based Query**:

```csharp
// BEFORE (wasteful):
if ((DateTime.Now - _lastResultsLoadTime).TotalSeconds >= 2)
{
    var newResults = await _currentSearch.GetResultsPageAsync(offset, 100);
    // ...
}

// AFTER (efficient):
if ((DateTime.Now - _lastResultsLoadTime).TotalSeconds >= 1.0 &&
    _currentSearch.HasNewResultsSinceLastQuery)
{
    var newResults = await _currentSearch.GetResultsPageAsync(offset, 100);

    // Acknowledge that we've queried and displayed the new results
    _currentSearch.AcknowledgeResultsQueried();

    // ... existing result processing
    _lastResultsLoadTime = DateTime.Now;
}
```

**Rationale for 1-second interval**:
- Original 2-second interval was arbitrary
- 1 second provides snappier feedback when batches complete
- Still prevents over-querying (only fires if flag is set)
- Motely batches take 1-10 seconds to complete, so 1s polling is fine

---

### Phase 3: Edge Cases & Thread Safety

**Thread Safety**:
- ✅ `volatile bool` for lock-free reads (progress updates on UI thread)
- ✅ `AddSearchResult()` already uses lock for DuckDB appender
- ✅ Flag writes are atomic (single bool assignment)
- ✅ No race conditions: worst case = one extra query (acceptable)

**Edge Cases**:

1. **Search completion with pending results**:
   - `OnSearchCompleted()` already calls `LoadExistingResults()` (line 848)
   - This final load ignores the flag and queries unconditionally
   - ✅ No change needed

2. **Multiple results added in quick succession**:
   - Flag gets set to `true` by first `AddSearchResult()`
   - Subsequent calls are no-ops (flag already true)
   - Next 1-second poll queries once, resets flag
   - ✅ Works correctly (batches results into single query)

3. **Search paused/resumed**:
   - Flag state persists across pause/resume
   - If results were added before pause, flag is still `true`
   - Next poll after resume will query correctly
   - ✅ No change needed

4. **No results ever found**:
   - Flag never gets set to `true`
   - UI never wastes time querying empty DuckDB
   - ✅ Perfect behavior

---

## Success Criteria

### Performance Metrics

**Before Optimization**:
- Queries per minute during search: ~30 (every 2 seconds)
- Queries per hour: ~1,800
- Wasteful queries (no new results): ~95% (1,710/hour)
- DuckDB overhead: High (constant sorting of growing result set)

**After Optimization**:
- Queries per minute during search: ~1-6 (only when batches complete)
- Queries per hour: ~60-360 (depends on batch completion rate)
- Wasteful queries: ~0% (flag prevents queries when no new results)
- DuckDB overhead: Low (queries only when new data exists)

**Expected Reduction**: **95%+ fewer queries** during typical searches

### Behavioral Testing

✅ **Test 1: Slow search with infrequent results**
- Create filter that finds 1 result per 10 batches
- Verify UI only queries ~6 times per minute (once per batch)
- Verify no queries between batches

✅ **Test 2: Fast search with frequent results**
- Create filter that finds 100 results per batch
- Verify UI queries every 1 second (when batches complete quickly)
- Verify results appear within 1 second of batch completion

✅ **Test 3: Search with zero results**
- Create impossible filter (no seeds match)
- Verify UI never queries DuckDB during search
- Verify final `LoadExistingResults()` still runs on completion

✅ **Test 4: Pause/Resume with pending results**
- Start search, pause after batch completes
- Verify flag is still `true` after resume
- Verify next poll loads the pending results

---

## Implementation Checklist

- [ ] Add `_hasNewResultsSinceLastQuery` field to SearchInstance
- [ ] Add `HasNewResultsSinceLastQuery` property to SearchInstance
- [ ] Add `AcknowledgeResultsQueried()` method to SearchInstance
- [ ] Set flag in `AddSearchResult()` after appending to DuckDB
- [ ] Update `OnProgressUpdated()` to check flag before querying
- [ ] Call `AcknowledgeResultsQueried()` after successful query
- [ ] Change poll interval from 2 seconds to 1 second
- [ ] Test all 4 behavioral scenarios
- [ ] Verify build succeeds with 0 warnings
- [ ] Measure query reduction in real searches

---

## Code Locations

**SearchInstance.cs**:
- Line 43: Add `_hasNewResultsSinceLastQuery` field
- Line 89: Add `HasNewResultsSinceLastQuery` property + `AcknowledgeResultsQueried()` method
- Line 322: Set flag in `AddSearchResult()`

**SearchModalViewModel.cs**:
- Line 1372: Update query logic to check flag
- Line 1372: Change interval from 2.0 to 1.0 seconds
- Line 1372: Add `AcknowledgeResultsQueried()` call after query

---

## Rollback Plan

If issues arise:
1. Remove flag check from `OnProgressUpdated()` (revert to time-based query)
2. Keep 1-second interval (still an improvement over 2 seconds)
3. No database changes required (flag is in-memory only)

---

## Future Optimizations (Out of Scope)

1. **Batch size notifications**: Instead of boolean flag, use atomic counter of new results
   - Allows UI to query larger pages when many results are pending
   - Example: `if (newResultCount > 500) GetResultsPageAsync(offset, 500)`

2. **DuckDB incremental queries**: Use `WHERE seed > lastLoadedSeed` instead of `LIMIT/OFFSET`
   - Eliminates sorting overhead on repeat queries
   - Requires tracking last loaded seed

3. **Result streaming via events**: Fire `NewResultsAdded` event from SearchInstance
   - UI subscribes to event instead of polling
   - Eliminates polling overhead entirely

These are NOT part of this PRD - focus on the simple invalidation flag pattern first.

---

**Status**: READY FOR IMPLEMENTATION
**Estimated Time**: 30 minutes
**Risk Level**: Low (simple boolean flag, no schema changes)
**Performance Impact**: **High** (95%+ query reduction)
