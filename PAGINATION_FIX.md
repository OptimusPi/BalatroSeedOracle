# Filter List Pagination Fix

## Problem
The pagination controls in the `FilterSelectorControl` appeared broken - clicking the previous/next buttons did nothing, and the page indicator always showed "1/1" even though there were 112 filters loaded.

## Root Cause
The `DEFAULT_FILTERS_PER_PAGE` constant was set to 120, but there are only 112 total filters in the `JsonItemFilters` directory. Since all filters fit on one page, pagination appeared non-functional:
- Total filters: 112
- Filters per page: 120
- Result: 1 page total (112 / 120 = 0.93, rounded up to 1)

## Solution
Changed `DEFAULT_FILTERS_PER_PAGE` from 120 to 7 filters per page for proper pagination and better UX.

## Code Changes

### File: `src/ViewModels/FilterListViewModel.cs`

**Location**: Line ~17

**Before**:
```csharp
public partial class FilterListViewModel : ObservableObject
{
    // Fixed pagination size for stability
    private const int DEFAULT_FILTERS_PER_PAGE = 120;
    private const double ITEM_HEIGHT = 32.0; // kept for any consumers; no dynamic sizing
```

**After**:
```csharp
public partial class FilterListViewModel : ObservableObject
{
    // Fixed pagination size for stability (7 filters per page for good UX)
    private const int DEFAULT_FILTERS_PER_PAGE = 7;
    private const double ITEM_HEIGHT = 32.0; // kept for any consumers; no dynamic sizing
```

## Result
With 112 filters and 7 per page:
- **Total pages**: 16 pages (112 / 7 = 16)
- **Page indicator**: Now shows "1/16", "2/16", etc.
- **Pagination controls**: Previous/Next buttons now work correctly
- **Navigation**: Users can browse through all filters in manageable chunks

## Why 7 Filters Per Page?
- Fits comfortably in the UI without scrolling
- Matches the visual design space in both SearchModal and FiltersModal
- Common pagination size for list-style UIs
- Provides smooth navigation experience

## Technical Details

### Pagination Implementation
The `FilterListViewModel` implements pagination with:
- `[RelayCommand] NextPage()` - Advances to next page
- `[RelayCommand] PreviousPage()` - Goes to previous page
- `UpdatePage()` - Refreshes displayed items and page state
- Bindings in AXAML:
  - `Command="{Binding NextPageCommand}"`
  - `Command="{Binding PreviousPageCommand}"`
  - `IsEnabled="{Binding CanGoToNextPage}"`
  - `IsEnabled="{Binding CanGoToPreviousPage}"`

### Where It's Used
The `FilterSelectorControl` is used in:
1. **SearchModal** - "Select Filter" tab (programmatically created in `SearchModalViewModel.CreateFilterTabContent()`)
2. **FiltersModal** - Left side panel (declared in `FiltersModal.axaml`)

Both now have working pagination!

## Testing
- ✅ App builds and runs successfully
- ✅ Pagination commands properly bound via MVVM
- ✅ With 112 filters, now shows 16 pages
- ✅ Previous/Next buttons work correctly
- ✅ Page indicator updates accurately
- ✅ Works in both SearchModal and FiltersModal

## Additional Notes
The original value of 120 may have been set during testing or as a "show all" mode, but this defeats the purpose of pagination and makes the control appear broken when there are fewer items than the page size.
