# Filter Deletion Bug Fix

## Problem Summary

**Issue:** When deleting a filter via the "Delete this Filter" button in FilterSelectionModal, the filter was removed from the cache but the UI continued showing stale data.

**User Experience:**
- User opens DESIGNER -> FilterSelectionModal
- User selects a filter (e.g., "any-perkeo")
- User clicks "Delete this Filter"
- Filter is deleted from disk and cache (confirmed by log: `[FilterCacheService] Removed filter from cache: any-perkeo`)
- Modal stays open (correct behavior)
- **BUG:** UI still shows deleted filter with all details, buttons remain active, filter appears in sidebar list

## Root Cause Analysis

### Architecture Overview
```
FilterSelectionModal (View)
  └─ FilterSelectionModalViewModel
      └─ PaginatedFilterBrowserViewModel
          └─ Loads filters from IFilterCacheService on construction
```

### The Problem
1. **Initial Load:** `PaginatedFilterBrowserViewModel` loads filters from cache once during construction
2. **Delete Flow (OLD):**
   - `FilterSelectionModalViewModel.ConfirmDelete()` raised `ModalCloseRequested` event
   - `BalatroMainMenu.axaml.cs` handled the event and called `FilterService.DeleteFilterAsync()`
   - `FilterService.DeleteFilterAsync()` removed filter from disk and cache
   - **NO UI REFRESH** - `PaginatedFilterBrowserViewModel` never reloaded its data
3. **Result:** UI showed stale data from the original load, completely unaware of deletion

### Why This Violated MVVM
- Business logic (deletion) was in code-behind (`BalatroMainMenu.axaml.cs`)
- ViewModel had no knowledge of the deletion
- No refresh mechanism to update the UI after cache changes

## Solution

### Changes Made

#### 1. FilterSelectionModalViewModel.cs (Line 231-306)
**Before:**
```csharp
public void ConfirmDelete()
{
    if (SelectedFilter == null || SelectedFilter.IsCreateNew)
        return;

    Result = new FilterSelectionResult
    {
        Cancelled = false,
        Action = FilterAction.Delete,
        FilterId = SelectedFilter.FilterId,
    };

    ModalCloseRequested?.Invoke(this, EventArgs.Empty);
}
```

**After:**
```csharp
public async void ConfirmDelete()
{
    if (SelectedFilter == null || SelectedFilter.IsCreateNew)
        return;

    var filterIdToDelete = SelectedFilter.FilterId;
    var filterNameToDelete = SelectedFilter.Name;

    // Get FilterService to perform the deletion
    var filterService = Helpers.ServiceHelper.GetRequiredService<Services.IFilterService>();
    var filtersDir = System.IO.Path.Combine(
        System.IO.Directory.GetCurrentDirectory(),
        "JsonItemFilters"
    );
    var filterPath = System.IO.Path.Combine(filtersDir, $"{filterIdToDelete}.json");

    // Perform deletion (this also removes from cache)
    var deleted = await filterService.DeleteFilterAsync(filterPath);

    if (!deleted)
    {
        DebugLogger.LogError("FilterSelectionModalVM", $"Failed to delete filter: {filterIdToDelete}");
        return;
    }

    // CRITICAL: Clear the selected filter FIRST to avoid showing stale data
    SelectedFilter = null;
    FilterList.SelectedFilter = null;

    // Refresh the filter list to reflect the deletion (reloads from cache/disk)
    FilterList.RefreshFilters();

    // Auto-select the first filter if any remain, otherwise show placeholder
    if (FilterList.CurrentPageFilters.Count > 0)
    {
        var firstFilterViewModel = FilterList.CurrentPageFilters[0];
        await FilterList.SelectFilterCommand.ExecuteAsync(firstFilterViewModel);
    }

    // Modal stays open so user can continue managing filters
}
```

**Key Improvements:**
- ViewModel now handles deletion directly (proper MVVM separation)
- Clears selected filter immediately to prevent stale data display
- Refreshes the filter list from cache after deletion
- Auto-selects first remaining filter or shows placeholder
- Modal stays open (as originally intended)
- Does NOT fire `ModalCloseRequested` event

#### 2. BalatroMainMenu.axaml.cs (Line 500-508)
**Before:**
```csharp
case Models.FilterAction.Delete:
    // Delete the filter and stay in modal (using FilterService)
    if (result.FilterId != null)
    {
        var filterService2 = Helpers.ServiceHelper.GetRequiredService<IFilterService>();
        var filtersDir = System.IO.Path.Combine(
            System.IO.Directory.GetCurrentDirectory(),
            "JsonItemFilters"
        );
        var filterPath = System.IO.Path.Combine(filtersDir, $"{result.FilterId}.json");
        await filterService2.DeleteFilterAsync(filterPath);
    }
    // DO NOT close modal - let user continue managing filters
    // The FilterSelectionModal will auto-refresh its list
    break;
```

**After:**
```csharp
case Models.FilterAction.Delete:
    // NOTE: Delete is now handled entirely in FilterSelectionModalViewModel.ConfirmDelete()
    // This case should never be reached since ConfirmDelete() doesn't invoke ModalCloseRequested anymore
    // Kept for backwards compatibility in case of refactoring
    Helpers.DebugLogger.Log(
        "BalatroMainMenu",
        "Delete action reached ModalCloseRequested - this should not happen. Delete is handled in ViewModel."
    );
    break;
```

**Rationale:**
- Moved business logic from code-behind to ViewModel
- Code-behind now only handles UI navigation concerns
- Delete case kept as safety fallback but should never execute

### How It Works Now

**New Delete Flow:**
1. User clicks "Delete this Filter" button
2. `FilterSelectionModalViewModel.Delete()` command executes
3. Confirmation dialog shown by View (code-behind)
4. User confirms deletion
5. `FilterSelectionModalViewModel.ConfirmDelete()` executes:
   - Calls `FilterService.DeleteFilterAsync()` to delete from disk and cache
   - Clears `SelectedFilter` to hide details panel
   - Calls `FilterList.RefreshFilters()` to reload from cache
   - Auto-selects first remaining filter (if any)
   - Modal stays open
6. UI updates reactively via data binding
   - Left sidebar shows updated filter list
   - Right panel shows new selection or placeholder
   - Deleted filter is gone immediately

### Data Flow
```
User Action
    ↓
FilterSelectionModalViewModel.Delete()
    ↓
Confirmation Dialog (View)
    ↓
FilterSelectionModalViewModel.ConfirmDelete()
    ↓
FilterService.DeleteFilterAsync()
    ├─ Delete file from disk
    └─ IFilterCacheService.RemoveFilter() - remove from cache
    ↓
Clear SelectedFilter (triggers property change notifications)
    ↓
FilterList.RefreshFilters()
    └─ LoadFilters() - reloads from cache
        ↓
        IFilterCacheService.GetAllFilters()
    ↓
Auto-select first filter (if any)
    ↓
UI updates via data binding
```

## Testing Verification

### Build Status
✅ Build succeeded with 0 warnings, 0 errors

### Manual Test Cases

**Test 1: Delete filter with multiple filters present**
- Open FilterSelectionModal
- Select a filter
- Click "Delete this Filter"
- Confirm deletion
- Expected: Filter disappears from list, first remaining filter auto-selected

**Test 2: Delete last filter**
- Open FilterSelectionModal with only one filter
- Select the filter
- Click "Delete this Filter"
- Confirm deletion
- Expected: Filter disappears, placeholder shown ("Select a Filter" or "CREATE NEW FILTER")

**Test 3: Verify cache consistency**
- Delete a filter
- Check debug logs for: `[FilterCacheService] Removed filter from cache: {filterId}`
- Verify filter no longer appears in list
- Close and reopen modal
- Verify filter still doesn't appear (cache persisted correctly)

## MVVM Compliance

### Before (Violation)
- ❌ Business logic in code-behind (`BalatroMainMenu.axaml.cs`)
- ❌ ViewModel unaware of deletion operation
- ❌ No refresh mechanism
- ❌ Tight coupling between View and Service layer

### After (Proper MVVM)
- ✅ Business logic in ViewModel (`FilterSelectionModalViewModel`)
- ✅ ViewModel manages deletion and refresh
- ✅ View only handles UI concerns (dialogs, navigation)
- ✅ Data binding handles UI updates
- ✅ Proper separation of concerns

## Related Files

### Modified
- `X:\BalatroSeedOracle\src\ViewModels\FilterSelectionModalViewModel.cs`
- `X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs`

### Dependencies (No Changes Required)
- `X:\BalatroSeedOracle\src\Services\FilterService.cs` - Already handles cache removal
- `X:\BalatroSeedOracle\src\Services\FilterCacheService.cs` - Already provides RemoveFilter()
- `X:\BalatroSeedOracle\src\ViewModels\PaginatedFilterBrowserViewModel.cs` - Already has RefreshFilters()

## Constraints Met

✅ Modal stays open after deletion (as originally designed)
✅ No full modal re-render (just updates the list via data binding)
✅ Handles edge case: deleting last filter (shows placeholder)
✅ Proper MVVM patterns (ViewModel handles business logic)
✅ Cache consistency maintained
✅ UI reflects deletion immediately

## Future Improvements (Optional)

1. **Add animation:** Fade out deleted filter before removing from list
2. **Undo functionality:** Allow user to undo deletion within a timeout period
3. **Better feedback:** Show toast notification "Filter deleted successfully"
4. **Preserve selection context:** Remember which filter was selected before deletion and select the next one in sequence instead of always selecting first
