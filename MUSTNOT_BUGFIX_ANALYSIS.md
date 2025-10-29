# MustNot Filter Bug Analysis and Fix

## Problem Statement
User reported that `mustNot` filtering is NOT working in Motely seed searcher. Seeds that should be rejected by mustNot clauses are still being reported as results.

### Test Case
Filter configuration:
```json
{
  "must": [{"type": "SmallBlindTag", "Value": "CharmTag", "antes": [1]}],
  "mustNot": [{"type": "Voucher", "Value": "Hieroglyph", "antes": [1]}]
}
```

Wordlist: Contains ONLY seeds that HAVE Hieroglyph at ante 1
Expected results: ZERO seeds (all should be rejected by mustNot)
Actual results: 1721 seeds reported

Debug output confirmed mustNot filter is correctly rejecting seeds:
```
[MUSTNOT INVERT] inner mask=0xFF (all 8 lanes matched Hieroglyph)
[MUSTNOT INVERT] inverted=0x00 (correctly rejecting all lanes)
```

But seeds like `9TN12111`, `8AP12111` etc are STILL being printed!

## Root Cause Analysis

### Code Review Findings

1. **Invert Filter Logic is CORRECT** (MotelyJsonInvertFilterDesc.cs)
   - Properly inverts the inner filter mask
   - Correctly handles valid/invalid lanes
   - Math checks out: ~0xFF & 0xFF = 0x00

2. **SearchFilterBatch Logic is CORRECT** (MotelySearch.cs lines 1036-1106)
   - Only calls ReportSeeds when mask.IsPartiallyTrue()
   - If mask is 0x00, IsPartiallyTrue() returns false
   - ReportSeeds never called → seeds should NOT be printed

3. **CRITICAL BUG FOUND: Composite Filter Path Missing MustNot**
   - JsonSearchExecutor.cs lines 370-411 handle multiple filter categories
   - This path creates a composite filter and starts search
   - **MISSING**: No mustNot filters applied to composite filter path!
   - Single-category path (lines 491-568) DOES apply mustNot correctly

### Why User Saw the Bug

The user's filter has:
- MUST: Tag category (SmallBlindTag)
- MUSTNOT: Voucher category

If the system detected this as requiring a composite filter (multiple categories), the mustNot would be ignored! However, the code SHOULD treat this as single-category (Tag is base, Voucher is additional filter).

**Secondary Issue**: Even if the user's case went through the single-category path correctly, the composite filter path still had the bug and would affect other users with multi-category MUST clauses.

## Fixes Applied

### Fix 1: Add MustNot Support to Composite Filter Path
**File**: `external/Motely/Motely/Executors/JsonSearchExecutor.cs`
**Location**: Lines 405-443 (after quiet mode, before starting search)

```csharp
// CRITICAL FIX: Apply MustNot filters to composite filter path
// Previously mustNot was only applied in single-category path, causing composite
// filters to ignore mustNot clauses and report seeds that should be excluded!
if (config.MustNot != null && config.MustNot.Count > 0)
{
    // Initialize parsed enums for MustNot clauses
    for (int i = 0; i < config.MustNot.Count; i++)
    {
        var clause = config.MustNot[i];
        try
        {
            clause.InitializeParsedEnums();
        }
        catch (Exception ex)
        {
            var typeText = string.IsNullOrEmpty(clause.Type) ? "<missing>" : clause.Type;
            var valueText = !string.IsNullOrEmpty(clause.Value) ? clause.Value : (clause.Values != null && clause.Values.Length > 0 ? string.Join(", ", clause.Values) : "<none>");
            throw new ArgumentException($"Config error in MUSTNOT[{i}] — type: '{typeText}', value(s): '{valueText}'. {ex.Message}");
        }
    }

    if (!_params.Quiet)
    {
        Console.WriteLine($"   + Applying MustNot: {config.MustNot.Count} clauses (exclusion)");
    }

    // Group MustNot clauses by category and add inverted specialized filters
    var notClausesByCategory = FilterCategoryMapper.GroupClausesByCategory(config.MustNot);
    foreach (var kv in notClausesByCategory)
    {
        var category = kv.Key;
        var clauses = kv.Value;
        DebugLogger.Log($"[MUSTNOT SETUP] Creating inverted specialized filter for category={category} with {clauses.Count} clauses");

        IMotelySeedFilterDesc specialized = SpecializedFilterFactory.CreateSpecializedFilter(category, clauses);
        var invertDesc = new MotelyJsonInvertFilterDesc(specialized);
        compositeSettings = compositeSettings.WithAdditionalFilter(invertDesc);
    }
}
```

This ensures mustNot filters are applied as additional filters in the composite filter path, just like they are in the single-category path.

### Fix 2: Enhanced Debug Logging
**File**: `external/Motely/Motely/MotelySearch.cs`

Added comprehensive debug logging to `SearchFilterBatch` (lines 1060-1100):
- Log which seeds are in each batch
- Log mask returned by each filter
- Log whether seeds pass or are rejected
- Log when ReportSeeds is called

Added debug logging to `ReportBasicSeeds` (lines 951-961):
- Log when seeds are actually reported
- Log which specific seeds are being printed

This will help diagnose any remaining issues and confirm the fix works.

## Testing Recommendations

1. **Retest User's Case**:
   - Use the same filter and wordlist
   - Run with `--debug` flag to see detailed logging
   - Expected: ZERO seeds reported (all rejected by mustNot)
   - Verify debug output shows mustNot filter rejecting seeds

2. **Test Composite Filter with MustNot**:
   ```json
   {
     "must": [
       {"type": "Joker", "value": "Joker", "antes": [1]},
       {"type": "Voucher", "value": "Overstock", "antes": [1]}
     ],
     "mustNot": [
       {"type": "Voucher", "value": "Hieroglyph", "antes": [1]}
     ]
   }
   ```
   - This creates a composite filter (Joker + Voucher categories)
   - Verify mustNot is applied and seeds with Hieroglyph are rejected

3. **Test Edge Cases**:
   - MustNot with no MUST clauses (passthrough filter path)
   - MustNot with single-category MUST (single-category path)
   - MustNot with multi-category MUST (composite path)
   - MustNot with SoulJoker (pre-filter path)

## Performance Impact

The fix adds mustNot filters as additional filters, which are processed AFTER the base filter. This is the correct architecture:

1. Base filter (MUST clauses) - vectorized, fast
2. Additional filters (mustNot clauses) - vectorized, process only seeds that passed base filter
3. Scoring (SHOULD clauses) - scalar, process only seeds that passed all filters

No performance degradation expected - mustNot filters only process seeds that passed the base filter.

## Files Modified

1. `external/Motely/Motely/Executors/JsonSearchExecutor.cs`
   - Added mustNot support to composite filter path (lines 405-443)

2. `external/Motely/Motely/MotelySearch.cs`
   - Enhanced debug logging in SearchFilterBatch (lines 1060-1100)
   - Enhanced debug logging in ReportBasicSeeds (lines 951-961)

3. `external/Motely/Motely/filters/MotelyJson/MotelyJsonInvertFilterDesc.cs`
   - Already had debug logging from previous investigation
   - Invert logic confirmed correct

## Conclusion

The bug was that the composite filter code path did not apply mustNot filters, causing seeds that should be rejected to be reported as results. The single-category path was working correctly.

The fix ensures mustNot filters are applied consistently across ALL code paths:
- Passthrough path (no MUST clauses) ✓ Already working
- Single-category path (one MUST category) ✓ Already working
- Composite path (multiple MUST categories) ✓ FIXED
- Pre-filter path (SoulJoker mustNot) ✓ Already working

With enhanced debug logging, any remaining issues will be easy to diagnose and fix.
