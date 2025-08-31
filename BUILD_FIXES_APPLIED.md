# Build Fixes Applied

## Issues Fixed:

### 1. CS8506: No best type found for switch expression (Line ~888)
**Problem:** Switch expression returned different types (string?, double, bool, object[]?, etc.)
**Solution:** Cast all return values to `object?` and added proper null handling

**Before:**
```csharp
var value = property.Value.ValueKind switch
{
    JsonValueKind.String => property.Value.GetString(),
    JsonValueKind.Number => property.Value.GetDouble(),
    // ... different return types
};
```

**After:**
```csharp
object? value = property.Value.ValueKind switch
{
    JsonValueKind.String => (object?)property.Value.GetString(),
    JsonValueKind.Number => (object?)property.Value.GetDouble(),
    // ... all cast to object?
};

if (value != null)
{
    newFilter[property.Name] = value;
}
```

### 2. CS8601: Possible null reference assignment (Lines ~777, 789)
**Problem:** `Path.GetDirectoryName()` can return null
**Solution:** Added explicit null checks and error handling

**Before:**
```csharp
var clonePath = Path.Combine(Path.GetDirectoryName(_selectedFilter.FilePath)!, cloneFileName);
```

**After:**
```csharp
var originalDir = Path.GetDirectoryName(_selectedFilter.FilePath);
if (string.IsNullOrEmpty(originalDir))
{
    DebugLogger.LogError("BalatroFilterSelector", "Could not determine directory for original filter");
    return;
}
var clonePath = Path.Combine(originalDir, cloneFileName);
```

## Files Modified:
- ✅ `src/Components/BalatroFilterSelector.axaml.cs`

## Next Steps:
1. Test the build
2. Test the functionality
3. Remove unused FilterCreationModal files if everything works

## Expected Behavior After Fix:
1. Build should complete without errors
2. Clone functionality should work properly
3. JSON serialization should handle all property types correctly
4. Proper error handling for file operations
