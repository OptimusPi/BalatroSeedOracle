# CRITICAL: Must[] Array Early Exit Bug - Performance Regression

## Issue Summary

Motely filter evaluation for Must[] arrays does NOT properly early exit when a filter clause fails for all seeds in a vector batch. This causes severe performance degradation (2-10x slowdown) when multiple filter clauses are present.

## Root Cause

### File: `external/Motely/Motely/filters/MotelyJson/MotelyCompositeFilterDesc.cs`
### Method: `MotelyCompositeFilter.Filter()`
### Lines: 114-131

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
public VectorMask Filter(ref MotelyVectorSearchContext ctx)
{
    // Start with all bits set
    VectorMask result = VectorMask.AllBitsSet;

    // Call each filter directly and AND the results (Must logic)
    foreach (var filter in _filters)
    {
        var filterMask = filter.Filter(ref ctx);  // ← LINE 122: EXPENSIVE CALL
        result &= filterMask;                      // ← LINE 123: AND result

        // Early exit if no seeds pass
        if (result.IsAllFalse())                   // ← LINE 126: CHECK (TOO LATE!)
            return VectorMask.NoBitsSet;
    }

    return result;
}
```

### The Problem

The early exit check on **line 126** happens AFTER the expensive `filter.Filter(ref ctx)` call on **line 122** has already executed.

**Correct Logic Flow (What We Want):**
1. Check if result is already all-false
2. If yes, skip remaining filters (early exit)
3. If no, call next filter

**Current Buggy Logic Flow (What Happens Now):**
1. Call expensive filter.Filter() - **executes entire filter evaluation**
2. AND the result with accumulated mask
3. Check if result is all-false
4. If yes, return early (but damage is already done - filter already executed!)

## Performance Impact

### Example: 3 Must Clauses Filter

```json
{
  "must": [
    { "type": "SoulJoker", "value": "Perkeo", "edition": "Negative" },
    { "type": "SoulJoker", "value": "Triboulet" },
    { "type": "Voucher", "value": "Observatory" }
  ]
}
```

**Current Behavior (BROKEN):**
- Process vector batch of 8 seeds
- Filter 1 (Perkeo): Check 8 antes × 6 pack slots = 48 checks → Returns `0x00` (no seeds match)
- Filter 2 (Triboulet): **STILL EXECUTES** → Check 48 more slots → Returns `0x00`
- Filter 3 (Observatory): **STILL EXECUTES** → Check 8 antes → Returns `0x00`
- **Total: ~104 checks per batch**

**Expected Behavior (WITH FIX):**
- Process vector batch of 8 seeds
- Filter 1 (Perkeo): Check 48 slots → Returns `0x00` (no seeds match)
- **Early exit check:** result is `0x00` (all false) → **SKIP Filter 2 and 3**
- **Total: ~48 checks per batch (2.17x speedup!)**

### Real-World Impact

With N Must clauses where first clause fails:
- **Without fix:** 100% of all filter work executed
- **With fix:** ~(1/N) of filter work executed
- **Expected speedup with 3 clauses:** 3x
- **Expected speedup with 5 clauses:** 5x

## The Fix

### Option 1: Check BEFORE Calling Next Filter (INCORRECT - Not Possible)

We cannot check `result.IsAllFalse()` before calling `filter.Filter()` because `result` is accumulated from ALL previous filters. The current logic is:

```
result = AllBitsSet
result = result & filter1.Filter()  // result might be 0xFF (all pass) or 0x00 (all fail)
result = result & filter2.Filter()  // ← We need result from filter1 before we can check
```

### Option 2: Check After Each Filter (CURRENT CODE - Actually Correct!)

Wait - looking at the code again:

```csharp
foreach (var filter in _filters)
{
    var filterMask = filter.Filter(ref ctx);  // Execute filter
    result &= filterMask;                      // AND with accumulated result

    // Early exit if no seeds pass
    if (result.IsAllFalse())                   // ← This DOES work!
        return VectorMask.NoBitsSet;
}
```

This IS correct! After each filter executes, we immediately check if `result` is all-false. If it is, we return without calling remaining filters.

## ACTUAL ISSUE: Early Exit Not Triggering?

The code HAS the early exit check. So why is performance bad?

### Hypothesis 1: Early Exit Rarely Triggers in Vectorized Mode

A batch of 8 seeds rarely ALL fail the same clause. Even if 7 seeds fail and 1 passes, `result.IsAllFalse()` returns false, so we continue to the next filter.

**Example:**
- Batch of 8 seeds: `[SEED1, SEED2, SEED3, SEED4, SEED5, SEED6, SEED7, SEED8]`
- Filter 1 result: `0b00000001` (only SEED8 matches)
- `result.IsAllFalse()` returns FALSE (bit 0 is set)
- Continue to Filter 2 (wastes time checking seeds 1-7 which already failed)

### Hypothesis 2: Missing Early Exit in Individual Filters

The composite filter has early exit, but individual filters (JokerFilterDesc, SoulJokerFilterDesc, etc.) might process ALL clauses of the same type without early exit.

Looking at `MotelyJsonJokerFilterDesc.cs` lines 272-289:

```csharp
// All clauses must be satisfied (AND logic)
var resultMask = VectorMask.AllBitsSet;
for (int i = 0; i < clauseMasks.Length; i++)
{
    DebugLogger.Log($"[JOKER VECTORIZED] Clause {i} mask: {clauseMasks[i].Value:X}");

    // FIX: If this clause found nothing across all antes, fail immediately
    if (clauseMasks[i].IsAllFalse())
    {
        DebugLogger.Log($"[JOKER VECTORIZED] Clause {i} found no matches - failing all seeds");
        return VectorMask.NoBitsSet;  // ← EARLY EXIT (ALREADY EXISTS!)
    }

    resultMask &= clauseMasks[i];
    DebugLogger.Log($"[JOKER VECTORIZED] Result after clause {i}: {resultMask.Value:X}");
    if (resultMask.IsAllFalse()) return VectorMask.NoBitsSet;  // ← EARLY EXIT (ALREADY EXISTS!)
}
```

**This ALSO has early exit!** Lines 280-283 and line 288.

## DEEPER INVESTIGATION NEEDED

The early exit checks ARE implemented at both levels:
1. **Composite filter level** (line 126 of MotelyCompositeFilterDesc.cs)
2. **Individual filter level** (line 288 of MotelyJsonJokerFilterDesc.cs)

### Possible Issues:

1. **Early exit condition rarely met:** In vectorized mode, batches rarely have ALL seeds fail
2. **Clause processing order:** Clauses are processed in JSON order, not ordered by selectivity
3. **Ante loop doesn't early exit:** Individual filters check ALL antes even if a clause fails early
4. **Individual seed verification overhead:** After vectorized pre-filter, individual verification re-checks everything

### Next Steps:

1. **Profile actual filter execution** - Measure which filters are taking the most time
2. **Check filter JSON** - See user's actual filter configuration
3. **Test with single-clause vs multi-clause filters** - Reproduce the 10x slowdown
4. **Add instrumentation** - Count how often early exit triggers

## Potential Optimizations

### 1. Order Filters by Selectivity (Cheapest First)

Process most selective (likely to fail) filters first to trigger early exit sooner.

### 2. Skip Remaining Antes When Clause Fails

In individual filters, if a clause's mask is all-false after ante N, skip checking antes N+1...8.

### 3. Per-Lane Early Exit (Advanced)

Track which seeds (lanes) have failed and skip processing them in subsequent filters.

## Recommendation

**Need user's actual filter JSON and profiling data to identify the bottleneck.**

The early exit code EXISTS but may not be triggering as expected due to vectorized batch processing semantics.
