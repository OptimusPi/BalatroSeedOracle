# Motely Integration Fix - SearchInstance

## Problem Summary

SearchInstance was incorrectly processing Motely filter configurations, causing three critical issues:

1. **Skipping And/Or clauses** - Logical operator clauses were being dropped entirely
2. **Duplicate filter grouping logic** - Had a buggy copy of FilterCategoryMapper.GroupClausesByCategory
3. **Mixing Should clauses into filters** - Should clauses (for scoring) were incorrectly included in filter inputs

## Console Output Before Fix
```
[00:27:24] [SearchInstance[SUAS_Anaglyph_White]] Skipping logical operator clause: And
```

This indicated And/Or clauses were being discarded instead of properly processed.

## Root Cause Analysis

### Issue 1: Skipping And/Or Clauses
**Location:** SearchInstance.cs, lines 1499-1510 (now removed)

**Broken code:**
```csharp
foreach (var clause in clauses)
{
    // Skip logical operators (And/Or) - they're handled in scoring, not filtering
    if (clause.ItemTypeEnum == MotelyFilterItemType.And ||
        clause.ItemTypeEnum == MotelyFilterItemType.Or)
    {
        DebugLogger.Log($"SearchInstance[{_searchId}]",
            $"Skipping logical operator clause: {clause.ItemTypeEnum}");
        continue;  // ❌ WRONG - And/Or are legitimate filter categories!
    }
    // ...
}
```

**Why it was wrong:**
- And/Or clauses are **legitimate filter categories** (FilterCategory.And, FilterCategory.Or)
- The reference implementation (JsonSearchExecutor) **never skips** these clauses
- FilterCategoryMapper.GroupClausesByCategory explicitly handles them:
  ```csharp
  MotelyFilterItemType.And => FilterCategory.And,
  MotelyFilterItemType.Or => FilterCategory.Or,
  ```
- Skipping them breaks composite filter logic where And/Or groupings are essential

### Issue 2: Duplicate Filter Grouping Logic
**Location:** SearchInstance.cs, lines 1477-1550 (now removed)

**Problem:**
- SearchInstance had its own GroupClausesByCategory method
- This duplicated FilterCategoryMapper.GroupClausesByCategory
- The duplicate had the critical And/Or skipping bug
- Created maintenance burden with divergent implementations

**Reference implementation:**
```csharp
// JsonSearchExecutor.cs, line 336
Dictionary<FilterCategory, List<MotelyJsonConfig.MotleyJsonFilterClause>>
    clausesByCategory = FilterCategoryMapper.GroupClausesByCategory(mustClauses);
```

### Issue 3: Mixing Should Clauses Into Filters
**Location:** SearchInstance.cs, lines 1613-1640 (now fixed)

**Broken code:**
```csharp
// Combine all clauses for the new API
var allClauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
if (config.Must != null)
    allClauses.AddRange(config.Must);
if (config.Should != null)
    allClauses.AddRange(config.Should);  // ❌ WRONG!
if (config.MustNot != null)
    allClauses.AddRange(config.MustNot);

var clausesByCategory = GroupClausesByCategory(allClauses);
```

**Why it was wrong:**
- **Must clauses** = filtering (which seeds to accept)
- **Should clauses** = scoring (how to score accepted seeds)
- **MustNot clauses** = filtering with inversion (which seeds to reject)

The reference implementation clearly separates these:
```csharp
// JsonSearchExecutor.cs, lines 307-336
List<MotelyJsonConfig.MotleyJsonFilterClause> mustClauses =
    config.Must?.ToList() ?? [];

// Only MUST clauses go to filter grouping
Dictionary<FilterCategory, List<MotelyJsonConfig.MotleyJsonFilterClause>>
    clausesByCategory = FilterCategoryMapper.GroupClausesByCategory(mustClauses);
```

## The Fix

### Change 1: Import Motely.Utils
```csharp
using Motely.Utils;
```

### Change 2: Remove Duplicate GroupClausesByCategory Method
Deleted lines 1477-1550, replaced with:
```csharp
// Removed - using shared FilterCategoryMapper.GroupClausesByCategory instead
```

### Change 2a: Route And/Or Categories to Composite Filter (New)
Added special handling for single And/Or categories to use MotelyCompositeFilterDesc:
```csharp
// Single category - check if it's And/Or (composite) or specialized filter
var primaryCategory = categories[0];
var primaryClauses = clausesByCategory[primaryCategory];

// CRITICAL FIX: And/Or categories need MotelyCompositeFilterDesc, not SpecializedFilterFactory
if (primaryCategory == FilterCategory.And || primaryCategory == FilterCategory.Or)
{
    DebugLogger.LogImportant(
        $"SearchInstance[{_searchId}]",
        $"Single {primaryCategory} category - using composite filter with {primaryClauses.Count} clauses"
    );

    var compositeFilter = new MotelyCompositeFilterDesc(primaryClauses);
    // ... setup and start
}
else
{
    // Regular specialized filter for other categories
    var filterDesc = Motely.Utils.SpecializedFilterFactory.CreateSpecializedFilter(
        primaryCategory,
        primaryClauses
    );
    // ... setup and start
}
```

**Why this is important:**
- And/Or are **logical operators**, not item types (like Joker, Voucher, etc.)
- They require MotelyCompositeFilterDesc to handle nested clause evaluation
- SpecializedFilterFactory is only for item-based filters (Joker, Voucher, TarotCard, etc.)
- This matches how JsonSearchExecutor handles And/Or categories (lines 745-747, 792-795)

### Change 3: Fix Clause Preparation in RunSearchInProcess
**Before:**
```csharp
var allClauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
if (config.Must != null)
    allClauses.AddRange(config.Must);
if (config.Should != null)
    allClauses.AddRange(config.Should);  // ❌ WRONG!
if (config.MustNot != null)
    allClauses.AddRange(config.MustNot);

var clausesByCategory = GroupClausesByCategory(allClauses);
```

**After:**
```csharp
// CRITICAL FIX: Only MUST clauses go to filters - Should clauses are for scoring only!
List<MotelyJsonConfig.MotleyJsonFilterClause> mustClauses =
    config.Must?.ToList() ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>();

// Initialize parsed enums for all MUST clauses with helpful errors
for (int i = 0; i < mustClauses.Count; i++)
{
    var clause = mustClauses[i];
    try
    {
        clause.InitializeParsedEnums();
    }
    catch (Exception ex)
    {
        var typeText = string.IsNullOrEmpty(clause.Type) ? "<missing>" : clause.Type;
        var valueText = !string.IsNullOrEmpty(clause.Value)
            ? clause.Value
            : (clause.Values != null && clause.Values.Length > 0
                ? string.Join(", ", clause.Values)
                : "<none>");
        throw new ArgumentException(
            $"Config error in MUST[{i}] — type: '{typeText}', value(s): '{valueText}'. {ex.Message}\n" +
            $"Suggestion: Add 'type' and 'value' (or 'values'): {{ \"type\": \"Joker\", \"value\": \"Perkeo\" }}"
        );
    }
}

// Group MUST clauses by category using shared utility (matches JsonSearchExecutor.cs)
var clausesByCategory = FilterCategoryMapper.GroupClausesByCategory(mustClauses);
```

### Change 4: Enhanced Logging
Added diagnostic logging to show proper category grouping:
```csharp
// Log the grouped categories (including And/Or if present)
DebugLogger.LogImportant(
    $"SearchInstance[{_searchId}]",
    $"Grouped into {clausesByCategory.Count} filter categories:"
);
foreach (var kvp in clausesByCategory)
{
    DebugLogger.LogImportant(
        $"SearchInstance[{_searchId}]",
        $"  {kvp.Key}: {kvp.Value.Count} clause(s)"
    );
}
```

### Change 5: Proper MustNot Handling in Composite Filter
Updated composite filter creation to match JsonSearchExecutor pattern:
```csharp
// Merge MustNot clauses into mustClauses with IsInverted flag (like JsonSearchExecutor does)
var allRequiredClauses = new List<MotelyJsonConfig.MotleyJsonFilterClause>(mustClauses);

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
            // Error handling...
        }
    }

    // Mark mustNot clauses as inverted and add to the composite
    foreach (var clause in config.MustNot)
    {
        clause.IsInverted = true;
        allRequiredClauses.Add(clause);
    }
}

var compositeFilter = new MotelyCompositeFilterDesc(allRequiredClauses);
```

## Expected Behavior After Fix

### Console Output
Instead of:
```
[00:27:24] [SearchInstance[SUAS_Anaglyph_White]] Skipping logical operator clause: And
```

You'll see:
```
[00:27:24] [SearchInstance[SUAS_Anaglyph_White]] Grouped into 2 filter categories:
[00:27:24] [SearchInstance[SUAS_Anaglyph_White]]   And: 1 clause(s)
[00:27:24] [SearchInstance[SUAS_Anaglyph_White]]   Joker: 3 clause(s)
```

### Filter Processing
1. **And/Or clauses** are properly grouped into FilterCategory.And/Or
2. **Must clauses** are separated from Should clauses (clean separation)
3. **Should clauses** only affect scoring, not filtering
4. **MustNot clauses** are merged with IsInverted flag in composite filters

### Code Quality
1. Single source of truth: FilterCategoryMapper.GroupClausesByCategory
2. Matches proven working reference: JsonSearchExecutor.cs
3. Proper error messages with helpful suggestions
4. Enhanced diagnostic logging

## Testing Recommendations

1. **Test with And/Or clauses** - Verify they're no longer skipped
2. **Test composite filters** - Verify multi-category filters work correctly
3. **Test scoring** - Verify Should clauses still work for scoring
4. **Test MustNot clauses** - Verify exclusion logic works
5. **Check console output** - Verify proper category grouping is logged

## References

- **JsonSearchExecutor.cs** (lines 191-926) - Proven working reference implementation
- **FilterCategoryMapper.cs** (lines 12-62) - Shared utility for category grouping
- **MotelyCompositeFilterDesc** - Handles And/Or composite logic

## Impact

### Performance
- No performance regression (actually slightly better due to removed duplicate code)
- Proper filter categorization enables optimal vectorization

### Correctness
- And/Or clauses now work as intended
- Clean separation between filtering (Must/MustNot) and scoring (Should)
- Matches proven working reference implementation

### Maintainability
- Removed duplicate code
- Single source of truth for filter grouping
- Better error messages with helpful suggestions
- Enhanced diagnostic logging
