# PRD: Filter Tab ViewModels - Complete Refactoring

## Executive Summary
Eliminate **~1,920 lines (64%)** of duplicate code across 3 filter tab ViewModels through proper MVVM architecture refactoring. This addresses critical technical debt identified by code review.

## Current State - The Problem

**Three ViewModels with massive duplication:**
- `ConfigureFilterTabViewModel.cs` - Manages MUST/MUST_NOT zones
- `ConfigureScoreTabViewModel.cs` - Manages weighted scoring
- `VisualBuilderTabViewModel.cs` - Visual filter builder

**Duplication breakdown:**
- Data loading logic: 400 lines × 3 = 1,200 lines
- Filtering logic: 135 lines × 3 = 405 lines
- Grouping logic: 120 lines × 3 = 360 lines
- Auto-save logic: 120 lines × 3 = 360 lines
- Properties/collections: ~200 lines × 3 = 600 lines
- **Total: ~1,920 duplicate lines**

## Goal
Implement proper MVVM separation of concerns with:
- Base class for shared ViewModel logic
- Service layer for business logic (data loading, filtering)
- Shared models for common types
- **Result: ~1,920 lines eliminated, single source of truth**

---

## Phase 1: Extract FilterTabViewModelBase

### Create Base Class
**File:** `x:\BalatroSeedOracle\src\ViewModels\FilterTabs\FilterTabViewModelBase.cs`

**Extract from all 3 ViewModels:**

#### Shared Properties
```csharp
[ObservableProperty] private string _searchFilter = "";
[ObservableProperty] private bool _isLoading = true;
[ObservableProperty] private string _selectedMainCategory = "Joker";
[ObservableProperty] private bool _isJokerCategorySelected = true;
[ObservableProperty] private bool _isConsumableCategorySelected = false;
[ObservableProperty] private bool _isSkipTagCategorySelected = false;
[ObservableProperty] private bool _isBossCategorySelected = false;
[ObservableProperty] private bool _isVoucherCategorySelected = false;
[ObservableProperty] private bool _isStandardCardCategorySelected = false;
```

#### Shared Collections (All + Filtered)
```csharp
public ObservableCollection<FilterItem> AllJokers { get; }
public ObservableCollection<FilterItem> AllTags { get; }
public ObservableCollection<FilterItem> AllVouchers { get; }
public ObservableCollection<FilterItem> AllTarots { get; }
public ObservableCollection<FilterItem> AllPlanets { get; }
public ObservableCollection<FilterItem> AllSpectrals { get; }
public ObservableCollection<FilterItem> AllBosses { get; }
public ObservableCollection<FilterItem> AllWildcards { get; }
public ObservableCollection<FilterItem> AllStandardCards { get; }

public ObservableCollection<FilterItem> FilteredJokers { get; }
public ObservableCollection<FilterItem> FilteredTags { get; }
// ... etc for all filtered collections
```

#### Shared Methods
```csharp
protected virtual void LoadGameData() { }
protected virtual void LoadGameDataAsync() { }
protected virtual void ApplyFilter() { }
protected virtual void RebuildGroupedItems() { }
public virtual void SetCategory(string category) { }
```

#### Auto-Save Logic
```csharp
protected CancellationTokenSource? _autoSaveCts;
protected void TriggerAutoSave() { }
protected virtual async Task PerformAutoSave() { }
protected virtual void OnZoneCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { }
```

#### Parent ViewModel Integration
```csharp
protected readonly FiltersModalViewModel? _parentViewModel;
protected FilterTabViewModelBase(FiltersModalViewModel? parentViewModel)
{
    _parentViewModel = parentViewModel;
    // Subscribe to parent property changes
}
```

**Lines Saved:** ~1,200 lines (move to base, delete from 3 VMs)

---

## Phase 2: Extract FilterItemDataService

### Create Service Interface + Implementation
**File:** `x:\BalatroSeedOracle\src\Services\FilterItemDataService.cs`

**Purpose:** Single source of truth for loading all game data (jokers, vouchers, etc.)

```csharp
public interface IFilterItemDataService
{
    Task LoadGameDataAsync(FilterItemCollections collections);
}

public class FilterItemCollections
{
    public ObservableCollection<FilterItem> AllJokers { get; init; }
    public ObservableCollection<FilterItem> AllTags { get; init; }
    public ObservableCollection<FilterItem> AllVouchers { get; init; }
    public ObservableCollection<FilterItem> AllTarots { get; init; }
    public ObservableCollection<FilterItem> AllPlanets { get; init; }
    public ObservableCollection<FilterItem> AllSpectrals { get; init; }
    public ObservableCollection<FilterItem> AllBosses { get; init; }
    public ObservableCollection<FilterItem> AllWildcards { get; init; }
    public ObservableCollection<FilterItem> AllStandardCards { get; init; }
}

public class FilterItemDataService : IFilterItemDataService
{
    private readonly SpriteService _spriteService;

    public FilterItemDataService(SpriteService spriteService)
    {
        _spriteService = spriteService;
    }

    public async Task LoadGameDataAsync(FilterItemCollections collections)
    {
        // ALL the wildcard/joker/tag/voucher/tarot/planet/spectral/boss loading logic
        // SINGLE IMPLEMENTATION - no more 3x duplication
    }
}
```

**Changes to ViewModels:**
- Replace `LoadGameData()` implementation with call to service
- Inject `IFilterItemDataService` via constructor

**Lines Saved:** ~400 lines (consolidated into service, deleted from 3 VMs)

---

## Phase 3: Extract FilterItemFilterService

### Create Service Interface + Implementation
**File:** `x:\BalatroSeedOracle\src\Services\FilterItemFilterService.cs`

**Purpose:** Centralized filtering logic

```csharp
public interface IFilterItemFilterService
{
    void ApplyFilter(
        string searchFilter,
        ObservableCollection<FilterItem> source,
        ObservableCollection<FilterItem> destination
    );

    void ApplyFilterToAll(
        string searchFilter,
        FilterItemCollections all,
        FilterItemCollections filtered
    );
}

public class FilterItemFilterService : IFilterItemFilterService
{
    public void ApplyFilter(
        string searchFilter,
        ObservableCollection<FilterItem> source,
        ObservableCollection<FilterItem> destination)
    {
        destination.Clear();

        if (string.IsNullOrEmpty(searchFilter))
        {
            foreach (var item in source)
                destination.Add(item);
            return;
        }

        var filter = searchFilter.ToLowerInvariant();
        foreach (var item in source)
        {
            if (item.Name.ToLowerInvariant().Contains(filter) ||
                item.DisplayName.ToLowerInvariant().Contains(filter))
            {
                destination.Add(item);
            }
        }
    }

    public void ApplyFilterToAll(string searchFilter, FilterItemCollections all, FilterItemCollections filtered)
    {
        ApplyFilter(searchFilter, all.AllJokers, filtered.FilteredJokers);
        ApplyFilter(searchFilter, all.AllTags, filtered.FilteredTags);
        ApplyFilter(searchFilter, all.AllVouchers, filtered.FilteredVouchers);
        ApplyFilter(searchFilter, all.AllTarots, filtered.FilteredTarots);
        ApplyFilter(searchFilter, all.AllPlanets, filtered.FilteredPlanets);
        ApplyFilter(searchFilter, all.AllSpectrals, filtered.FilteredSpectrals);
        ApplyFilter(searchFilter, all.AllBosses, filtered.FilteredBosses);
        ApplyFilter(searchFilter, all.AllWildcards, filtered.FilteredWildcards);
        ApplyFilter(searchFilter, all.AllStandardCards, filtered.FilteredStandardCards);
    }
}
```

**Changes to ViewModels:**
- Replace `ApplyFilter()` implementation with call to service
- Inject `IFilterItemFilterService` via constructor

**Lines Saved:** ~300 lines (consolidated into service, deleted from 3 VMs)

---

## Phase 4: Move ItemGroup to Shared Models

### Create Shared Model
**File:** `x:\BalatroSeedOracle\src\Models\FilterItemGroup.cs`

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a group of filter items (e.g., "Legendary Jokers", "Vouchers")
    /// Used for visual grouping in filter tabs
    /// </summary>
    public class FilterItemGroup : ObservableObject
    {
        public string GroupName { get; set; } = "";
        public ObservableCollection<FilterItem> Items { get; set; } = new();

        // All items render at same size: 5-wide (380px shelf, 70px cards)
        public double ShelfMaxWidth => 380;
        public double CardWidth => 70;
        public double CardHeight => 110;
        public double ImageWidth => 64;
        public double ImageHeight => 85;
    }
}
```

**Changes to ViewModels:**
- Delete `ItemGroup` class definition from all 3 ViewModels
- Update `GroupedItems` type: `ObservableCollection<FilterItemGroup>`
- Update XAML `x:DataType` references to use shared type

**Changes to XAML:**
- Update namespace: `xmlns:models="using:BalatroSeedOracle.Models"`
- Update DataType: `x:DataType="models:FilterItemGroup"` (instead of `vm:ItemGroup`)

**Lines Saved:** ~20 lines (3 duplicate definitions → 1 shared model)

---

## Implementation Order

### Step 1: Create New Files
1. Create `FilterTabViewModelBase.cs`
2. Create `FilterItemDataService.cs`
3. Create `FilterItemFilterService.cs`
4. Create `FilterItemGroup.cs` (move from ViewModels)

### Step 2: Update Dependency Injection
**File:** `ServiceCollectionExtensions.cs`

Add service registrations:
```csharp
services.AddSingleton<IFilterItemDataService, FilterItemDataService>();
services.AddSingleton<IFilterItemFilterService, FilterItemFilterService>();
```

### Step 3: Refactor ViewModels
For each of the 3 ViewModels:

1. **Change inheritance:** `public partial class ConfigureFilterTabViewModel : FilterTabViewModelBase`
2. **Update constructor:**
   ```csharp
   public ConfigureFilterTabViewModel(
       FiltersModalViewModel? parentViewModel,
       IFilterItemDataService dataService,
       IFilterItemFilterService filterService)
       : base(parentViewModel, dataService, filterService)
   {
       // Only tab-specific initialization here
   }
   ```
3. **Delete all duplicated code:**
   - Remove shared properties
   - Remove shared collections
   - Remove `LoadGameData()` implementation (call base)
   - Remove `ApplyFilter()` implementation (call base)
   - Remove `ItemGroup` class definition
   - Remove auto-save helpers (use base)

4. **Keep only tab-specific code:**
   - ConfigureFilterTab: MUST/MUST_NOT zone management
   - ConfigureScoreTab: Weighted scoring logic
   - VisualBuilderTab: Edition/sticker/seal button logic

### Step 4: Update XAML Files
For each XAML file that uses ItemGroup:

1. Add models namespace: `xmlns:models="using:BalatroSeedOracle.Models"`
2. Update DataType: `x:DataType="models:FilterItemGroup"`
3. Update any ItemGroup references to FilterItemGroup

---

## Testing Checklist

### Functional Testing
- [ ] All 3 filter tabs load correctly
- [ ] Data loads (jokers, vouchers, tags, etc.) in all tabs
- [ ] Search/filtering works in all tabs
- [ ] Category switching works in all tabs
- [ ] ConfigureFilter: MUST/MUST_NOT zones work
- [ ] ConfigureScore: Weighted scoring works
- [ ] VisualBuilder: Edition/sticker buttons show correctly
- [ ] Auto-save triggers in all tabs
- [ ] Parent ViewModel sync works (filter name, etc.)

### Visual Testing
- [ ] ItemGroup rendering unchanged (card sizes, shelf width)
- [ ] Vouchers display in correct order (pairs together)
- [ ] No visual regressions

### Build Testing
- [ ] Build succeeds with 0 errors
- [ ] No new warnings introduced
- [ ] All DI registrations correct

---

## Success Criteria

✅ FilterTabViewModelBase created with all shared logic
✅ FilterItemDataService created and injected
✅ FilterItemFilterService created and injected
✅ FilterItemGroup moved to Models namespace
✅ All 3 ViewModels refactored to use base class + services
✅ ~1,920 lines of code eliminated
✅ Build succeeds
✅ All functional tests pass
✅ No visual regressions

---

## Risk Mitigation

**High Risk Areas:**
1. **Constructor injection** - Ensure DI wiring is correct
2. **Virtual method overrides** - Ensure derived classes call base when needed
3. **Thread safety** - Preserve UI thread dispatching in base class
4. **Auto-save timing** - Ensure debouncing still works correctly

**Rollback Plan:**
- Git commit before starting refactoring
- If critical issues, revert commit and address in smaller chunks

---

## Files to Create
1. `x:\BalatroSeedOracle\src\ViewModels\FilterTabs\FilterTabViewModelBase.cs`
2. `x:\BalatroSeedOracle\src\Services\FilterItemDataService.cs`
3. `x:\BalatroSeedOracle\src\Services\FilterItemFilterService.cs`
4. `x:\BalatroSeedOracle\src\Models\FilterItemGroup.cs`

## Files to Modify
1. `x:\BalatroSeedOracle\src\ViewModels\FilterTabs\ConfigureFilterTabViewModel.cs`
2. `x:\BalatroSeedOracle\src\ViewModels\FilterTabs\ConfigureScoreTabViewModel.cs`
3. `x:\BalatroSeedOracle\src\ViewModels\FilterTabs\VisualBuilderTabViewModel.cs`
4. `x:\BalatroSeedOracle\src\Extensions\ServiceCollectionExtensions.cs`
5. `x:\BalatroSeedOracle\src\Components\FilterTabs\ConfigureFilterTab.axaml` (ItemGroup → FilterItemGroup)
6. `x:\BalatroSeedOracle\src\Components\FilterTabs\ConfigureScoreTab.axaml` (ItemGroup → FilterItemGroup)
7. `x:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml` (ItemGroup → FilterItemGroup)

## Estimated Time
- Phase 1 (Base Class): 30-45 minutes
- Phase 2 (Data Service): 20-30 minutes
- Phase 3 (Filter Service): 15-20 minutes
- Phase 4 (Shared Models): 10 minutes
- Testing: 30 minutes
- **Total: ~2-2.5 hours**

---

**PITFREAK IS DEAD! LONG LIVE PITFREAK!**
