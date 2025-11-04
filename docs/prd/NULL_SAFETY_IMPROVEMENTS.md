# NULL SAFETY & INPUT VALIDATION - Defensive Programming

**Status:** 🟡 HIGH PRIORITY
**Priority:** P1 - Fix This Sprint
**Estimated Time:** 2-3 hours
**Generated:** 2025-11-03

---

## Overview

Improve null safety and input validation to prevent crashes and provide better user feedback when invalid data is encountered.

---

## Issue #1: Database Column Access Without Validation

### Problem
**File:** `src/Services/SearchInstance.cs:371-391`
**Severity:** HIGH

Database reader assumes columns exist without validation:

```csharp
while (await reader.ReadAsync().ConfigureAwait(false))
{
    var seed = reader.GetString(0);      // ❌ Assumes column 0 exists
    var score = reader.GetInt32(1);      // ❌ Assumes column 1 exists

    // Later there IS a check:
    if (columnIndex < reader.FieldCount && !reader.IsDBNull(columnIndex))
    {
        scores[i] = reader.GetInt32(columnIndex);
    }
}
```

### Impact
- Database schema changes → IndexOutOfRangeException
- Corrupted database → crash with no recovery
- No user feedback about what went wrong

### Acceptance Criteria
- [ ] Add column existence validation before GetString/GetInt32 calls
- [ ] Add column name validation (use GetOrdinal with try-catch)
- [ ] Log descriptive error if expected column is missing
- [ ] Return empty results instead of crashing
- [ ] Add database schema version check

### Implementation

```csharp
// BEFORE:
var seed = reader.GetString(0);
var score = reader.GetInt32(1);

// AFTER:
if (reader.FieldCount < 2)
{
    DebugLogger.LogError("SearchInstance",
        $"Invalid database schema: expected at least 2 columns, got {reader.FieldCount}");
    return new SearchResults { Success = false, Error = "Database schema invalid" };
}

try
{
    var seed = reader.GetString(0);
    var score = reader.GetInt32(1);
}
catch (InvalidCastException ex)
{
    DebugLogger.LogError("SearchInstance",
        $"Database column type mismatch: {ex.Message}");
    continue; // Skip this row
}
```

---

## Issue #2: Missing Filter Validation Before Search

### Problem
**File:** `src/Services/SearchInstance.cs:119-120`
**Severity:** MEDIUM

Good: Validates dbPath is not null/empty
Missing: No validation of filter config contents before execution

```csharp
// Current validation:
if (string.IsNullOrWhiteSpace(dbPath))
{
    // Good! But incomplete...
}

// Missing validation:
// - Filter has at least one criterion?
// - Deck/stake values are valid?
// - Filter name is safe for filesystem?
// - Score ranges are reasonable?
```

### Impact
- Search runs with invalid filter → wastes CPU time
- Invalid filter → confusing "no results" message
- Missing filter criteria → returns all seeds (huge result set)
- Invalid deck/stake → searches wrong database

### Acceptance Criteria
- [ ] Validate filter has at least one MUST or SHOULD criterion
- [ ] Validate deck name is in allowed list
- [ ] Validate stake name is in allowed list
- [ ] Validate filter name contains no invalid filesystem characters
- [ ] Return descriptive error message for each validation failure

### Implementation

```csharp
public class FilterValidator
{
    private static readonly string[] ValidDecks = new[]
    {
        "Red", "Blue", "Yellow", "Green", "Black", "Magic",
        "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac",
        "Painted", "Anaglyph", "Plasma", "Erratic"
    };

    private static readonly string[] ValidStakes = new[]
    {
        "white", "red", "green", "black", "blue", "purple", "orange", "gold"
    };

    public static ValidationResult ValidateSearchCriteria(SearchCriteria criteria, MotelyJsonConfig filter)
    {
        if (filter == null)
            return ValidationResult.Fail("Filter is null");

        if ((filter.Must?.Count ?? 0) == 0 && (filter.Should?.Count ?? 0) == 0)
            return ValidationResult.Fail("Filter must have at least one MUST or SHOULD criterion");

        if (!string.IsNullOrEmpty(criteria.Deck) && !ValidDecks.Contains(criteria.Deck))
            return ValidationResult.Fail($"Invalid deck: {criteria.Deck}");

        if (!string.IsNullOrEmpty(criteria.Stake) && !ValidStakes.Contains(criteria.Stake))
            return ValidationResult.Fail($"Invalid stake: {criteria.Stake}");

        if (criteria.BatchSize < 1 || criteria.BatchSize > 8)
            return ValidationResult.Fail($"Batch size must be 1-8, got {criteria.BatchSize}");

        return ValidationResult.Success();
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string error) => new() { IsValid = false, Error = error };
}
```

---

## Issue #3: Null-Forgiving Operator Overuse

### Problem
**Files:** Multiple
**Severity:** MEDIUM

Excessive use of `null!` assertions suggests missing null-safety design:

```csharp
private static SpriteService _instance = null!;
private Dictionary<string, SpritePosition> jokerPositions = null!;
```

### Impact
- `null!` tells compiler "trust me, this won't be null"
- If it IS null → runtime crash with no compile-time warning
- Makes it harder to find real null reference bugs

### Acceptance Criteria
- [ ] Replace static singleton with lazy initialization
- [ ] Use proper initialization patterns instead of `null!`
- [ ] Add null checks before accessing fields marked with `null!`
- [ ] Reduce `null!` usage by 50% minimum

### Implementation

```csharp
// BEFORE (BAD):
private static SpriteService _instance = null!;
public static SpriteService Instance => _instance ??= new SpriteService();

// AFTER (GOOD):
private static readonly Lazy<SpriteService> _instance = new(() => new SpriteService());
public static SpriteService Instance => _instance.Value;
```

```csharp
// BEFORE (BAD):
private Dictionary<string, SpritePosition> jokerPositions = null!;

public void Initialize()
{
    jokerPositions = new Dictionary<string, SpritePosition>();
}

// AFTER (GOOD):
private Dictionary<string, SpritePosition>? jokerPositions;

public void Initialize()
{
    jokerPositions = new Dictionary<string, SpritePosition>();
}

public SpritePosition? GetPosition(string key)
{
    if (jokerPositions == null)
    {
        DebugLogger.LogError("SpriteService", "GetPosition called before Initialize");
        return null;
    }

    return jokerPositions.TryGetValue(key, out var position) ? position : null;
}
```

---

## Issue #4: Array/Collection Access Without Bounds Check

### Problem
**Files:** Multiple
**Severity:** MEDIUM

While some code uses `Math.Min` correctly, there are 46+ uses of `?.ToString()`, `?.Count`, `?.Length` that assume nullability is handled inline.

### Examples
```csharp
// Safe usage:
rawJsonForDebug.Substring(0, Math.Min(500, rawJsonForDebug.Length))

// Risky usage pattern:
var item = collection?.FirstOrDefault();
var name = item.Name;  // ❌ Crash if item is null
```

### Acceptance Criteria
- [ ] Add null checks after null-conditional operators
- [ ] Use null-coalescing operator for safe defaults
- [ ] Add bounds checks for array access
- [ ] Wrap risky operations in try-catch with logging

### Implementation

```csharp
// BEFORE:
var item = collection?.FirstOrDefault();
var name = item.Name;  // ❌ NullReferenceException

// AFTER - Option 1 (defensive):
var item = collection?.FirstOrDefault();
var name = item?.Name ?? "Unknown";

// AFTER - Option 2 (explicit):
var item = collection?.FirstOrDefault();
if (item == null)
{
    DebugLogger.LogWarning("ClassName", "Collection is empty or null");
    return;
}
var name = item.Name;
```

---

## Issue #5: Missing Input Validation in UI

### Problem
**Files:** Multiple ViewModels
**Severity:** MEDIUM

User input is not validated before saving:

- No validation that filter name is not empty before saving
- No validation that filter name contains valid characters
- No validation of numeric inputs (batch size, ante ranges)
- No validation of deck/stake selections

### Acceptance Criteria
- [ ] Add validation attributes to ViewModel properties
- [ ] Show validation errors in UI
- [ ] Disable save buttons when validation fails
- [ ] Sanitize user input before filesystem operations

### Implementation

```csharp
// Add to SaveFilterTabViewModel:

[ObservableProperty]
[NotifyDataErrorInfo]
[Required(ErrorMessage = "Filter name is required")]
[RegularExpression(@"^[a-zA-Z0-9_\-\s]+$", ErrorMessage = "Filter name contains invalid characters")]
private string _filterName = "";

[ObservableProperty]
[NotifyDataErrorInfo]
[Range(1, 8, ErrorMessage = "Batch size must be between 1 and 8")]
private int _batchSize = 3;

private bool CanSave()
{
    return !HasErrors && !string.IsNullOrWhiteSpace(FilterName);
}
```

---

## Implementation Order

1. **Phase 1 - Critical Database Safety** (1 hour)
   - Fix database column validation in SearchInstance
   - Add schema version check
   - Test with corrupted database

2. **Phase 2 - Filter Validation** (1 hour)
   - Create FilterValidator class
   - Add validation before search execution
   - Add user-facing error messages
   - Test with invalid filters

3. **Phase 3 - Null Safety** (1 hour)
   - Replace `null!` with proper patterns
   - Add null checks after `?.` operators
   - Use `??` for safe defaults
   - Audit all dictionary/collection access

4. **Phase 4 - UI Validation** (30 min)
   - Add validation attributes to ViewModels
   - Update UI to show validation errors
   - Test user input edge cases

---

## Test Plan

### Database Validation Tests
1. Create database with wrong schema → verify graceful error
2. Create database with wrong column types → verify skip + log
3. Empty database → verify returns empty results

### Filter Validation Tests
1. Empty filter (no MUST/SHOULD) → verify error message
2. Invalid deck name → verify error message
3. Invalid stake name → verify error message
4. Valid filter → verify search runs

### Null Safety Tests
1. Call GetPosition before Initialize → verify error + no crash
2. Access null collection → verify safe default
3. Array access out of bounds → verify safe handling

### UI Validation Tests
1. Enter filter name with `/` characters → verify error shown
2. Leave filter name empty → verify save disabled
3. Enter batch size > 8 → verify error shown
4. Valid inputs → verify save enabled

---

## Success Metrics

- ✅ Zero crashes from null reference exceptions
- ✅ Zero crashes from index out of range
- ✅ All invalid user input shows error message
- ✅ `null!` usage reduced by 50%+
- ✅ All database operations have validation

---

## Dependencies

- System.ComponentModel.DataAnnotations (for validation attributes)
- DebugLogger (already exists)
- No external dependencies

---

## Estimated Effort

- Database safety: 1 hour
- Filter validation: 1 hour
- Null safety refactoring: 1 hour
- UI validation: 30 minutes
- Testing: 30 minutes
- **Total: 4 hours**

---

## Assignee

coding-agent (automated via Claude Code)

---

## Notes

**Philosophy:** Fail fast with descriptive errors rather than failing silently.

User should ALWAYS know:
- What went wrong
- Why it went wrong
- How to fix it

No silent failures. No mysterious crashes. No "it just doesn't work" scenarios.
