# ARCHITECTURE REFACTORING - Decompose God Classes

**Status:** 🟠 MEDIUM PRIORITY
**Priority:** P2 - Technical Debt Reduction
**Estimated Time:** 8-12 hours
**Generated:** 2025-11-03

---

## Overview

Decompose large, complex classes (>1500 lines) into smaller, focused components following Single Responsibility Principle. Extract services, split ViewModels, and improve code organization.

---

## Problem: God Classes

### Large Files Identified

| File | Lines | Complexity | Issue |
|------|-------|------------|-------|
| SearchInstance.cs | 2086 | VERY HIGH | Service doing too many things |
| VisualBuilderTab.axaml.cs | 2037 | VERY HIGH | Complex drag/drop + UI management |
| SearchModalViewModel.cs | 1794 | HIGH | ViewModel with too many concerns |
| BalatroMainMenu.axaml.cs | 1775 | HIGH | View with business logic |
| FiltersModalViewModel.cs | 1732 | HIGH | Large ViewModel needs service extraction |

### Impact
- Hard to understand and maintain
- Hard to test (too many dependencies)
- High risk of bugs (changes affect many features)
- Poor code reusability
- Long build times for modified files

---

## Refactoring #1: SearchInstance → Multiple Services

### Current State
**File:** `src/Services/SearchInstance.cs` (2086 lines)
**Issues:**
- Handles database operations
- Handles search execution
- Handles result management
- Handles state persistence
- Handles progress reporting
- Uses multiple synchronization primitives (volatile, lock, Interlocked)

### Target Architecture

Split into 5 focused services:

```
SearchInstance.cs (2086 lines)
├── SearchDatabaseService.cs (300 lines) - Database query execution
├── SearchStateManager.cs (200 lines) - State persistence
├── SearchProgressReporter.cs (150 lines) - Progress events
├── SearchResultManager.cs (250 lines) - Result collection & sorting
└── SearchOrchestrator.cs (400 lines) - Coordinates the above services
```

### SearchDatabaseService

**Responsibility:** Execute database queries only

```csharp
public interface ISearchDatabaseService
{
    Task<DatabaseQueryResult> ExecuteSearchQueryAsync(
        string dbPath,
        string query,
        int maxResults,
        CancellationToken cancellationToken
    );

    Task<bool> ValidateDatabaseAsync(string dbPath);
}

public class SearchDatabaseService : ISearchDatabaseService
{
    // Focused on database operations only
    // No state management
    // No progress reporting
    // Just query execution
}
```

### SearchStateManager

**Responsibility:** Save/load search state

```csharp
public interface ISearchStateManager
{
    Task SaveStateAsync(SearchState state);
    Task<SearchState?> LoadStateAsync(string searchId);
    void InvalidateState(string searchId);
}

public class SearchStateManager : ISearchStateManager
{
    private readonly string _stateDirectory;
    private readonly object _saveLock = new();

    // Focused on state persistence only
    // No search logic
    // No database operations
}
```

### SearchProgressReporter

**Responsibility:** Report search progress

```csharp
public interface ISearchProgressReporter
{
    event EventHandler<SearchProgressEventArgs>? ProgressUpdated;
    event EventHandler<SearchResultEventArgs>? ResultFound;

    void ReportProgress(long batchesProcessed, long totalBatches, double elapsedSeconds);
    void ReportResult(SearchResult result);
}

public class SearchProgressReporter : ISearchProgressReporter
{
    // Focused on progress reporting only
    // Thread-safe event handling
    // No search logic
}
```

### SearchResultManager

**Responsibility:** Collect and manage results

```csharp
public interface ISearchResultManager
{
    void AddResult(SearchResult result);
    IReadOnlyList<SearchResult> GetTopResults(int count);
    void Clear();
    int ResultCount { get; }
}

public class SearchResultManager : ISearchResultManager
{
    private readonly SortedSet<SearchResult> _results;
    private readonly object _resultsLock = new();
    private readonly int _maxResults;

    // Focused on result management only
    // Thread-safe collection
    // Automatic sorting
}
```

### SearchOrchestrator

**Responsibility:** Coordinate all search services

```csharp
public class SearchOrchestrator : ISearchOrchestrator
{
    private readonly ISearchDatabaseService _database;
    private readonly ISearchStateManager _stateManager;
    private readonly ISearchProgressReporter _progressReporter;
    private readonly ISearchResultManager _resultManager;

    public SearchOrchestrator(
        ISearchDatabaseService database,
        ISearchStateManager stateManager,
        ISearchProgressReporter progressReporter,
        ISearchResultManager resultManager
    )
    {
        _database = database;
        _stateManager = stateManager;
        _progressReporter = progressReporter;
        _resultManager = resultManager;
    }

    public async Task<SearchResults> ExecuteSearchAsync(
        SearchCriteria criteria,
        MotelyJsonConfig filter,
        CancellationToken cancellationToken
    )
    {
        // Orchestrate the search using injected services
        // High-level coordination only
        // No low-level implementation details
    }
}
```

### Benefits
- Each service <400 lines (easier to understand)
- Single Responsibility Principle
- Easy to test in isolation
- Easy to replace implementations
- Clear separation of concerns

### Acceptance Criteria
- [ ] Create 5 new service files
- [ ] Register services in DI container
- [ ] Update SearchInstance to use services (or replace with SearchOrchestrator)
- [ ] Unit tests for each service
- [ ] Integration test for full search flow
- [ ] Verify performance is same or better

---

## Refactoring #2: VisualBuilderTab → Smaller Components

### Current State
**File:** `src/Components/FilterTabs/VisualBuilderTab.axaml.cs` (2037 lines)
**Issues:**
- Handles drag/drop logic (500+ lines)
- Handles operator tray management (200+ lines)
- Handles animation logic (300+ lines)
- Handles drop zone detection (200+ lines)
- Handles item configuration (200+ lines)
- Contains business logic that should be in ViewModel

### Target Architecture

Split into focused components:

```
VisualBuilderTab.axaml.cs (2037 lines)
├── Behaviors/
│   ├── DragDropBehavior.cs (400 lines) - Generic drag/drop
│   ├── OperatorTrayBehavior.cs (200 lines) - Tray management
│   └── CardAnimationBehavior.cs (300 lines) - Animations
├── Services/
│   └── DropZoneDetectionService.cs (150 lines) - Hit testing
└── VisualBuilderTab.axaml.cs (300 lines) - UI only
```

### DragDropBehavior

**Responsibility:** Generic drag/drop functionality

```csharp
public class DragDropBehavior : Behavior<Control>
{
    // Attached properties:
    public static readonly AttachedProperty<bool> IsDraggableProperty;
    public static readonly AttachedProperty<bool> IsDropTargetProperty;

    // Events:
    public event EventHandler<DragStartedEventArgs>? DragStarted;
    public event EventHandler<DragCompletedEventArgs>? DragCompleted;

    // Generic drag/drop logic
    // No Balatro-specific code
    // Reusable for other projects
}
```

### OperatorTrayBehavior

**Responsibility:** OR/AND tray management

```csharp
public class OperatorTrayBehavior : Behavior<Border>
{
    public static readonly StyledProperty<FilterOperatorItem?> OperatorProperty;

    // Manages single tray
    // Handles adding/removing children
    // Handles copy logic
}
```

### Benefits
- View code-behind <300 lines
- Drag/drop logic reusable
- Business logic moved to ViewModel
- Behaviors testable independently

### Acceptance Criteria
- [ ] Create behavior classes
- [ ] Move drag/drop logic to behaviors
- [ ] Move business logic to ViewModel
- [ ] Update XAML to use behaviors
- [ ] Test drag/drop functionality
- [ ] Verify animations still work

---

## Refactoring #3: SearchModalViewModel → Feature ViewModels

### Current State
**File:** `src/ViewModels/SearchModalViewModel.cs` (1794 lines)
**Issues:**
- Manages search execution
- Manages filter selection
- Manages result display
- Manages export functionality
- Manages UI state for multiple tabs
- Too many responsibilities

### Target Architecture

Split by feature:

```
SearchModalViewModel.cs (1794 lines)
├── SearchExecutionViewModel.cs (400 lines) - Execute search
├── SearchResultsViewModel.cs (300 lines) - Display results
├── SearchExportViewModel.cs (200 lines) - Export results
└── SearchModalViewModel.cs (400 lines) - Orchestrate child VMs
```

### Pattern: Composite ViewModel

```csharp
public class SearchModalViewModel : ObservableObject
{
    public SearchExecutionViewModel ExecutionViewModel { get; }
    public SearchResultsViewModel ResultsViewModel { get; }
    public SearchExportViewModel ExportViewModel { get; }

    public SearchModalViewModel(
        SearchExecutionViewModel executionVM,
        SearchResultsViewModel resultsVM,
        SearchExportViewModel exportVM
    )
    {
        ExecutionViewModel = executionVM;
        ResultsViewModel = resultsVM;
        ExportViewModel = exportVM;

        // Wire up communication between child VMs
        ExecutionViewModel.SearchCompleted += OnSearchCompleted;
    }

    private void OnSearchCompleted(object? sender, SearchResults results)
    {
        ResultsViewModel.DisplayResults(results);
        ExportViewModel.EnableExport(results);
    }
}
```

### Benefits
- Each ViewModel <400 lines
- Clear feature boundaries
- Easy to test features independently
- Easy to reuse ViewModels

### Acceptance Criteria
- [ ] Create feature ViewModels
- [ ] Update SearchModalViewModel to composite pattern
- [ ] Update XAML DataContext bindings
- [ ] Unit tests for each feature ViewModel
- [ ] Integration test for full search flow

---

## Refactoring #4: Extract Duplicate Game Data Loading

### Problem
**Multiple Files**
**Severity:** MEDIUM

Game data loading duplicated across ViewModels:

```csharp
// Seen in FilterTabs, SearchModal, VisualBuilder, etc.
AllJokers = new ObservableCollection<FilterItem>(
    BalatroData.Jokers.Select(kvp => new FilterItem
    {
        Name = kvp.Key,
        DisplayName = kvp.Value,
        Category = "Jokers"
    })
);
```

### Solution

Create `IFilterItemFactory` service:

```csharp
public interface IFilterItemFactory
{
    ObservableCollection<FilterItem> CreateJokerItems();
    ObservableCollection<FilterItem> CreateTarotItems();
    ObservableCollection<FilterItem> CreatePlanetItems();
    ObservableCollection<FilterItem> CreateSpectralItems();
    ObservableCollection<FilterItem> CreateVoucherItems();
    ObservableCollection<FilterItem> CreateTagItems();
    ObservableCollection<FilterItem> CreateBossItems();

    ObservableCollection<FilterItem> CreateItemsByCategory(string category);
}

public class FilterItemFactory : IFilterItemFactory
{
    public ObservableCollection<FilterItem> CreateJokerItems()
    {
        return new ObservableCollection<FilterItem>(
            BalatroData.Jokers.Select(kvp => new FilterItem
            {
                Name = kvp.Key,
                DisplayName = kvp.Value,
                Category = "Jokers"
            })
        );
    }

    // ... other methods
}
```

### Usage in ViewModels

```csharp
public class VisualBuilderTabViewModel : ObservableObject
{
    private readonly IFilterItemFactory _itemFactory;

    public VisualBuilderTabViewModel(IFilterItemFactory itemFactory)
    {
        _itemFactory = itemFactory;
        AllJokers = _itemFactory.CreateJokerItems();
        AllTarots = _itemFactory.CreateTarotItems();
        // etc.
    }
}
```

### Benefits
- No code duplication
- Centralized game data mapping
- Easy to update data format
- Easy to add new item categories

### Acceptance Criteria
- [ ] Create FilterItemFactory service
- [ ] Register in DI container
- [ ] Update all ViewModels to use factory
- [ ] Remove duplicate game data loading code
- [ ] Verify all features still work

---

## Refactoring #5: Consolidate Hardcoded Magic Values

### Problem
**Multiple Files**
**Severity:** MEDIUM

Magic numbers scattered throughout:

```csharp
// Card dimensions hardcoded in multiple places
var width = 71;
var height = 95;

// Animation durations hardcoded
await Task.Delay(500);

// UI constants hardcoded
const int MaxResults = 100;
```

### Solution

Create `GameConstants.cs` and `UIConstants.cs`:

```csharp
public static class GameConstants
{
    // Card dimensions
    public const int CardWidth = 71;
    public const int CardHeight = 95;

    // Game mechanics
    public static readonly string[] ValidDecks = new[]
    {
        "Red", "Blue", "Yellow", "Green", "Black", "Magic",
        "Nebula", "Ghost", "Abandoned", "Checkered", "Zodiac",
        "Painted", "Anaglyph", "Plasma", "Erratic"
    };

    public static readonly string[] ValidStakes = new[]
    {
        "white", "red", "green", "black", "blue", "purple", "orange", "gold"
    };

    // Search ranges
    public const int MinBatchSize = 1;
    public const int MaxBatchSize = 8;
}

public static class UIConstants
{
    // Animation durations (ms)
    public const int ShortAnimationDuration = 150;
    public const int MediumAnimationDuration = 300;
    public const int LongAnimationDuration = 500;

    // UI limits
    public const int MaxSearchResults = 100;
    public const int MaxFilterItems = 1000;

    // Debounce times
    public const int AutoSaveDebounceMs = 500;
    public const int SearchDebounceMs = 300;
}
```

### Usage

```csharp
// BEFORE:
var width = 71;
await Task.Delay(500);

// AFTER:
var width = GameConstants.CardWidth;
await Task.Delay(UIConstants.LongAnimationDuration);
```

### Benefits
- Single source of truth
- Easy to adjust values
- Self-documenting code
- Easy to find all uses of a constant

### Acceptance Criteria
- [ ] Create GameConstants.cs
- [ ] Create UIConstants.cs
- [ ] Replace all magic numbers with constants
- [ ] Verify all features still work
- [ ] Document constants in XML comments

---

## Implementation Order

### Phase 1: Create Service Abstractions (2 hours)
1. Create FilterItemFactory
2. Create DropZoneDetectionService
3. Register in DI container

### Phase 2: Extract SearchInstance Services (3 hours)
1. Create SearchDatabaseService
2. Create SearchStateManager
3. Create SearchProgressReporter
4. Create SearchResultManager
5. Create SearchOrchestrator
6. Test each service

### Phase 3: Simplify VisualBuilderTab (2 hours)
1. Create DragDropBehavior
2. Create OperatorTrayBehavior
3. Move business logic to ViewModel
4. Test drag/drop functionality

### Phase 4: Split Large ViewModels (2 hours)
1. Split SearchModalViewModel
2. Split FiltersModalViewModel (if needed)
3. Update XAML bindings
4. Test all features

### Phase 5: Consolidate Constants (1 hour)
1. Create GameConstants.cs
2. Create UIConstants.cs
3. Replace magic numbers
4. Test all features

---

## Test Plan

### Service Tests
- Unit test each new service independently
- Mock dependencies
- Verify behavior in isolation

### Integration Tests
- Test search flow end-to-end
- Test filter creation flow
- Test drag/drop flow
- Verify performance

### Regression Tests
- Full app smoke test
- Test all user workflows
- Verify no features broke

---

## Success Metrics

- ✅ No files >1000 lines
- ✅ Average file size <500 lines
- ✅ Each class has single responsibility
- ✅ Code duplication reduced by 30%
- ✅ Test coverage increased to 60%+
- ✅ Build time reduced by 10%+

---

## Risks & Mitigation

### Risk: Breaking Existing Functionality
**Mitigation:**
- Incremental refactoring (one service at a time)
- Comprehensive testing after each change
- Keep old code alongside new until proven
- Easy rollback via git

### Risk: Performance Degradation
**Mitigation:**
- Benchmark before/after
- Profile hot paths
- Use async/await properly
- Minimize allocations

### Risk: Over-Engineering
**Mitigation:**
- Only split when >1000 lines
- Only extract when duplicated 3+ times
- Pragmatic, not dogmatic
- YAGNI principle

---

## Dependencies

- Microsoft.Extensions.DependencyInjection (already installed)
- No new dependencies needed

---

## Estimated Effort

- Service abstractions: 2 hours
- SearchInstance refactor: 3 hours
- VisualBuilderTab refactor: 2 hours
- ViewModel splitting: 2 hours
- Constants consolidation: 1 hour
- Testing: 2 hours
- **Total: 12 hours**

---

## Assignee

coding-agent (automated via Claude Code)

---

## Benefits

### Short Term
- Easier to understand code
- Easier to find bugs
- Easier to add features

### Long Term
- Sustainable development velocity
- New team members onboard faster
- Less technical debt accumulation
- Higher code quality

---

## Notes

**This is the most impactful refactoring work**, but also the most time-consuming. Prioritize the most problematic files first:

1. SearchInstance (most complex)
2. VisualBuilderTab (hardest to maintain)
3. SearchModalViewModel (affects user experience)
4. Constants (quick win)
5. FilterItemFactory (reduces duplication)

**Do NOT do all at once** - incremental refactoring reduces risk.

---

## References

- [Single Responsibility Principle](https://en.wikipedia.org/wiki/Single-responsibility_principle)
- [Service Layer Pattern](https://martinfowler.com/eaaCatalog/serviceLayer.html)
- [Composite ViewModel Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/maui/mvvm#composite-viewmodels)
