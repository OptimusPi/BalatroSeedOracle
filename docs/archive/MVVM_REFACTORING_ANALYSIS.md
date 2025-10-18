# MVVM Refactoring Analysis - BalatroSeedOracle

**Analysis Date:** 2025-10-10
**Current Branch:** MVVMRefactor
**Analysis Scope:** Complete codebase architecture review

---

## EXECUTIVE SUMMARY

The codebase has **CRITICAL MVVM violations** that need immediate attention. The worst offender is `FiltersModal.axaml.cs` with **8,824 lines** of code-behind containing massive business logic that should be in ViewModels.

### Critical Statistics
- **God Class Identified:** `FiltersModal.axaml.cs` (8,824 lines, 145+ methods)
- **Total ViewModels:** 36 (many properly implemented)
- **Code-Behind Files with Violations:** 8+ files with significant business logic
- **UI-in-ViewModel Violations:** 4 ViewModels creating UI controls directly

---

## 1. GOD CLASSES DISCOVERED

### CRITICAL: FiltersModal.axaml.cs
**File:** `X:/BalatroSeedOracle/src/Views/Modals/FiltersModal.axaml.cs`
**Lines:** 8,824
**Methods:** 145+
**Severity:** CRITICAL

**Responsibilities (Too Many!):**
1. Filter creation and editing UI
2. Drag-and-drop item management
3. JSON editing and validation
4. Filter saving/loading
5. Item configuration popup management
6. Tab navigation
7. Search functionality
8. Preview generation
9. Database cleanup logic
10. File I/O operations

**Issues:**
- Massive monolithic code-behind file
- Business logic mixed with UI code
- Direct database operations in view
- Complex state management in view
- No separation of concerns
- Nearly impossible to unit test
- Performance issues due to excessive FindControl calls

**Impact:** This single file represents ~77% of all code-behind violations in the project.

---

### MAJOR: BalatroMainMenu.axaml.cs
**File:** `X:/BalatroSeedOracle/src/Views/BalatroMainMenu.axaml.cs`
**Lines:** 681
**Methods:** ~40
**Severity:** MAJOR (but mostly fixed)

**Status:** Partially refactored - ViewModel exists (`BalatroMainMenuViewModel`, 680 lines) but some UI logic remains in code-behind.

**Remaining Issues:**
- Modal creation logic in code-behind (should be in ViewModel or factory)
- Direct shader manipulation calls
- Some view-specific event wiring that could be data-bound

**Strengths:**
- Good ViewModel separation
- Events properly wired
- Most business logic moved to ViewModel

---

### MODERATE: ToolsModal.axaml.cs
**File:** `X:/BalatroSeedOracle/src/Views/Modals/ToolsModal.axaml.cs`
**Lines:** 376
**Methods:** ~20
**Severity:** MODERATE

**Issues:**
- Business logic for tools functionality in code-behind
- No ViewModel present
- Direct file operations
- State management in view

---

### MODERATE: WordListsModal.axaml.cs
**File:** `X:/BalatroSeedOracle/src/Views/Modals/WordListsModal.axaml.cs`
**Lines:** 274
**Methods:** ~15
**Severity:** MODERATE

**Issues:**
- Wordlist management logic in code-behind
- No ViewModel
- File operations in view
- CRUD operations in view layer

---

## 2. MVVM VIOLATIONS BY CATEGORY

### A. Business Logic in Code-Behind

**Critical Violations:**

1. **FiltersModal.axaml.cs** - Complete filter management system in code-behind
   - Filter CRUD operations
   - JSON serialization/deserialization
   - Database cleanup
   - Validation logic
   - Search algorithms

2. **ToolsModal.axaml.cs** - Tool functionality in code-behind
   - Seed generation logic
   - File export operations
   - Settings management

3. **WordListsModal.axaml.cs** - Wordlist management in code-behind
   - CRUD operations
   - File I/O
   - Validation

**Impact:** These violations make the code untestable and tightly couple business logic to UI.

---

### B. UI Creation in ViewModels

**Violations Found:**

1. **SearchModalViewModel.cs** (813 lines)
   - Lines 703-766: `CreateFilterTabContent()` creates UI controls programmatically
   - Creates `FilterSelector`, `DeckAndStakeSelector`, `Grid`, `Border` controls
   - **Fix:** Move to XAML UserControl with proper data binding

2. **AudioVisualizerSettingsModalViewModel.cs** (790 lines)
   - Lines 674-786: `ShowPresetNameDialog()` creates entire dialog UI in ViewModel
   - Creates `Window`, `TextBox`, `Button`, `StackPanel` controls
   - **Fix:** Create dedicated PresetNameDialog.axaml view

3. **FilterSelectorViewModel.cs** (578 lines)
   - Lines with `new Grid`, `new Button` for dynamic filter cards
   - **Fix:** Use data templates in XAML

4. **SearchDesktopIconViewModel.cs** (616 lines)
   - Lines 325-409: `CreateFilterPreview()` creates Canvas and Image controls
   - **Fix:** Move to XAML with proper data binding

**Pattern:** These violations occur when developers try to create dynamic UI without leveraging XAML data templates.

---

### C. God ViewModels (Too Many Responsibilities)

**Large ViewModels:**

1. **FiltersModalViewModel.cs** - 823 lines
   - Actually GOOD architecture (proper MVVM)
   - Multiple concerns but properly separated
   - Uses child ViewModels (VisualBuilderTab, JsonEditorTab, SaveFilterTab)
   - **Status:** Well-structured, no refactoring needed

2. **SearchModalViewModel.cs** - 813 lines
   - Tab management
   - Search orchestration
   - Results management
   - Settings management
   - **Fix:** Split into SearchCoordinator + TabViewModels

3. **AudioVisualizerSettingsModalViewModel.cs** - 790 lines
   - Theme management
   - Audio source configuration
   - Shader parameter management
   - Preset management
   - **Fix:** Split into ThemeViewModel, AudioViewModel, PresetViewModel

4. **VisualBuilderTabViewModel.cs** - 740 lines
   - Item catalog management
   - Filter building
   - Category management
   - Search functionality
   - **Fix:** Extract ItemCatalogViewModel, FilterBuilderViewModel

5. **BalatroMainMenuViewModel.cs** - 680 lines
   - Menu state
   - Modal management
   - Audio management
   - Settings management
   - Shader parameter application (20+ methods!)
   - **Fix:** Extract ModalService, ShaderSettingsViewModel

---

### D. Missing ViewModels

**Code-Behind Files Without ViewModels:**

1. `ToolsModal.axaml.cs` (376 lines) - Needs ToolsModalViewModel
2. `WordListsModal.axaml.cs` (274 lines) - Needs WordListsModalViewModel
3. `StandardModal.axaml.cs` (160 lines) - Needs StandardModalViewModel
4. `FilterCreationModal.axaml.cs` (111 lines) - Needs FilterCreationModalViewModel (or merge into FiltersModal)
5. `AnalyzeModal.axaml.cs` (87 lines) - Needs AnalyzeModalViewModel
6. `AuthorModal.axaml.cs` (84 lines) - Needs AuthorModalViewModel

---

## 3. ARCHITECTURAL ISSUES

### A. Tight Coupling

**Problem Areas:**

1. **Direct Service Instantiation in ViewModels**
   ```csharp
   // BAD (found in some ViewModels)
   var service = new SomeService();

   // GOOD (use DI)
   private readonly SomeService _service;
   public MyViewModel(SomeService service) { _service = service; }
   ```

2. **ViewModels Creating Views**
   - SearchModalViewModel creates FilterSelector
   - AudioVisualizerSettingsModalViewModel creates Window
   - **Fix:** Use events or navigation service

3. **Parent-Child ViewModel Coupling**
   - VisualBuilderTabViewModel has direct reference to FiltersModalViewModel parent
   - JsonEditorTabViewModel same issue
   - **Fix:** Use messaging/events or shared state service

---

### B. Code Duplication

**Identified Duplications:**

1. **Deck/Stake Selection Logic**
   - Duplicated in: FiltersModalViewModel, SearchModalViewModel, DeckAndStakeSelectorViewModel
   - **Fix:** Create shared DeckStakeService

2. **Filter Validation Logic**
   - Scattered across: FiltersModal, FiltersModalViewModel, JsonEditorTabViewModel
   - **Fix:** Centralize in FilterValidationService

3. **Sprite Loading**
   - Repeated patterns in multiple ViewModels
   - **Fix:** Already has SpriteService, but not consistently used

---

### C. Performance Issues

**FiltersModal Performance Problems:**

1. **Excessive FindControl Calls**
   - Lines 107-192 cache controls (good!)
   - But still 60+ uncached FindControl calls in the 8,824-line file
   - **Fix:** Complete the caching or use proper data binding

2. **Scroll Handler Thrashing**
   - Lines 111-113 note debouncing needed (100ms throttle)
   - Fires 60-120 times per second
   - **Fix:** Implement proper debouncing or use reactive extensions

3. **No Virtualization**
   - Large item lists not virtualized
   - **Fix:** Use VirtualizingStackPanel or similar

---

## 4. PROPOSED COMPONENT ARCHITECTURE

### Phase 1: FiltersModal Deconstruction (HIGHEST PRIORITY)

**Current:** FiltersModal.axaml.cs (8,824 lines) + FiltersModalViewModel (823 lines)

**Proposed Structure:**

```
FiltersModal/
├── ViewModels/
│   ├── FiltersModalViewModel.cs (300 lines) - Orchestrator only
│   ├── FilterEditorViewModel.cs (200 lines) - Edit operations
│   ├── FilterListViewModel.cs (200 lines) - List management
│   ├── FilterPreviewViewModel.cs (150 lines) - Preview generation
│   └── Tabs/
│       ├── VisualBuilderTabViewModel.cs (KEEP - 740 lines, well-structured)
│       ├── JsonEditorTabViewModel.cs (KEEP - 533 lines, well-structured)
│       └── SaveFilterTabViewModel.cs (KEEP - 267 lines, well-structured)
├── Views/
│   ├── FiltersModal.axaml (XAML only)
│   ├── FiltersModal.axaml.cs (50 lines - wire-up only)
│   ├── FilterEditor.axaml
│   ├── FilterList.axaml
│   └── FilterPreview.axaml
├── Services/
│   ├── FilterValidationService.cs
│   ├── FilterPersistenceService.cs
│   └── FilterSearchService.cs
└── Models/
    ├── FilterEditorState.cs
    └── FilterSearchCriteria.cs
```

**Why This Works:**
- Separates concerns (list, edit, preview, save)
- Each ViewModel has single responsibility
- Code-behind becomes thin wire-up layer
- Testable components
- Reusable services

---

### Phase 2: SearchModal Refactoring

**Current:** SearchModalViewModel (813 lines) with mixed responsibilities

**Proposed:**

```
SearchModal/
├── ViewModels/
│   ├── SearchCoordinatorViewModel.cs (200 lines) - Orchestrates search flow
│   ├── FilterSelectionViewModel.cs (150 lines) - Filter selection
│   ├── SearchSettingsViewModel.cs (200 lines) - Search parameters
│   ├── SearchExecutionViewModel.cs (250 lines) - Run/stop/pause
│   └── SearchResultsViewModel.cs (200 lines) - Results display
└── Services/
    ├── SearchStateService.cs - Manages search state
    └── SearchResumeService.cs - Handles resume logic
```

---

### Phase 3: AudioVisualizerSettings Refactoring

**Current:** AudioVisualizerSettingsModalViewModel (790 lines)

**Proposed:**

```
AudioVisualizerSettings/
├── ViewModels/
│   ├── ThemeSelectionViewModel.cs (150 lines)
│   ├── AudioSourceMappingViewModel.cs (200 lines)
│   ├── EffectIntensityViewModel.cs (150 lines)
│   ├── PresetManagerViewModel.cs (200 lines)
│   └── ShaderDebugViewModel.cs (100 lines)
├── Views/
│   ├── ThemeSelection.axaml
│   ├── AudioSourceMapping.axaml
│   ├── EffectIntensity.axaml
│   └── PresetManager.axaml
└── Dialogs/
    └── PresetNameDialog.axaml - Replace UI-in-ViewModel!
```

---

### Phase 4: BalatroMainMenu Cleanup

**Current:** BalatroMainMenuViewModel (680 lines) with 20+ shader methods

**Proposed:**

```
MainMenu/
├── ViewModels/
│   ├── MainMenuViewModel.cs (200 lines) - Menu state only
│   ├── ShaderSettingsViewModel.cs (150 lines) - Shader params
│   └── ModalCoordinatorViewModel.cs (100 lines) - Modal routing
├── Services/
│   ├── ModalNavigationService.cs - Central modal management
│   └── ShaderConfigurationService.cs - Shader parameter application
```

**Benefits:**
- Remove 20+ ApplyXxx() methods from ViewModel
- ShaderConfigurationService handles all shader interactions
- ModalNavigationService routes modal requests

---

## 5. REFACTORING PLAN (PRIORITIZED)

### PRIORITY 1: CRITICAL - FiltersModal Deconstruction
**Estimated Effort:** 3-5 days
**Impact:** Eliminates 77% of MVVM violations

**Tasks:**
1. Create FilterEditorViewModel (extract editing logic from code-behind)
2. Create FilterListViewModel (extract list management)
3. Create FilterPreviewViewModel (extract preview generation)
4. Convert FiltersModal.axaml.cs to thin wire-up (target: <100 lines)
5. Create FilterValidationService (centralize validation)
6. Create FilterPersistenceService (centralize save/load)
7. Move ALL business logic from code-behind to ViewModels
8. Add comprehensive unit tests for new ViewModels

**Validation:**
- Code-behind file size: 8,824 → <100 lines (98% reduction)
- ViewModels testable: 0% → 100%
- Separation of concerns: Clear boundaries between components

---

### PRIORITY 2: HIGH - Remove UI-in-ViewModel Violations
**Estimated Effort:** 1-2 days
**Impact:** Fixes 4 critical MVVM violations

**Tasks:**

1. **SearchModalViewModel.cs**
   - Replace `CreateFilterTabContent()` with FilterTabContent.axaml UserControl
   - Use proper data binding instead of programmatic UI creation

2. **AudioVisualizerSettingsModalViewModel.cs**
   - Create PresetNameDialog.axaml view
   - Replace `ShowPresetNameDialog()` with proper dialog service call

3. **FilterSelectorViewModel.cs**
   - Use ItemsControl with DataTemplate instead of creating controls
   - Bind to FilterItems collection with proper templates

4. **SearchDesktopIconViewModel.cs**
   - Create FilterPreviewControl.axaml with data templates
   - Replace `CreateFilterPreview()` with data-bound control

**Validation:**
- Zero `new Grid()`, `new Button()`, etc. in ViewModels
- All UI defined in XAML
- ViewModels only manage data, not UI structure

---

### PRIORITY 3: HIGH - Create Missing ViewModels
**Estimated Effort:** 2-3 days
**Impact:** Completes MVVM coverage

**Tasks:**

1. **ToolsModalViewModel** - Extract from ToolsModal.axaml.cs (376 lines)
2. **WordListsModalViewModel** - Extract from WordListsModal.axaml.cs (274 lines)
3. **StandardModalViewModel** - Extract from StandardModal.axaml.cs (160 lines)
4. **AnalyzeModalViewModel** - Already exists (AnalyzeModalViewModel.cs, 339 lines) - wire it up!
5. **AuthorModalViewModel** - Extract from AuthorModal.axaml.cs (84 lines)

**Validation:**
- Every modal has corresponding ViewModel
- Code-behind files <50 lines (wire-up only)
- Business logic in ViewModels

---

### PRIORITY 4: MEDIUM - Split God ViewModels
**Estimated Effort:** 3-4 days
**Impact:** Improves maintainability and testability

**Tasks:**

1. **SearchModalViewModel** (813 → 300 lines)
   - Extract SearchCoordinatorViewModel
   - Extract SearchSettingsViewModel
   - Extract SearchExecutionViewModel
   - Extract SearchResultsViewModel

2. **AudioVisualizerSettingsModalViewModel** (790 → 300 lines)
   - Extract ThemeSelectionViewModel
   - Extract AudioSourceMappingViewModel
   - Extract EffectIntensityViewModel
   - Extract PresetManagerViewModel

3. **VisualBuilderTabViewModel** (740 → 400 lines)
   - Extract ItemCatalogViewModel
   - Extract FilterBuilderViewModel

4. **BalatroMainMenuViewModel** (680 → 300 lines)
   - Extract ShaderSettingsViewModel
   - Extract ModalCoordinatorViewModel

**Validation:**
- No ViewModel exceeds 400 lines
- Each ViewModel has single responsibility
- Clear separation of concerns

---

### PRIORITY 5: LOW - Cleanup & Optimization
**Estimated Effort:** 2-3 days
**Impact:** Code quality improvements

**Tasks:**

1. **Eliminate Code Duplication**
   - Create DeckStakeService for shared deck/stake logic
   - Centralize filter validation in FilterValidationService
   - Create SpriteLoaderService for consistent sprite loading

2. **Performance Optimization**
   - Complete control caching in FiltersModal
   - Implement proper debouncing for scroll handlers
   - Add virtualization for large lists

3. **Dependency Injection Cleanup**
   - Ensure all ViewModels use constructor injection
   - Remove any `new Service()` instantiation
   - Register all services in ServiceCollectionExtensions.cs

**Validation:**
- Zero service instantiation in ViewModels
- All common logic in shared services
- Performance metrics improved (measure scroll/load times)

---

## 6. SUCCESS METRICS

### Before Refactoring
- **Total Code-Behind Lines:** 11,390 (FiltersModal: 8,824)
- **ViewModels with UI Creation:** 4
- **Missing ViewModels:** 6
- **God ViewModels (>600 lines):** 5
- **Testability:** <20% (most logic in code-behind)
- **MVVM Compliance:** ~40%

### After Refactoring (Target)
- **Total Code-Behind Lines:** <500 (95% reduction)
- **ViewModels with UI Creation:** 0 (100% fixed)
- **Missing ViewModels:** 0 (100% coverage)
- **God ViewModels (>600 lines):** 0 (all split)
- **Testability:** >90% (business logic in ViewModels)
- **MVVM Compliance:** >95%

---

## 7. IMPLEMENTATION GUIDELINES

### A. MVVM Best Practices

**DO:**
- Use CommunityToolkit.MVVM `[ObservableProperty]` and `[RelayCommand]`
- Constructor inject all dependencies
- Raise events for view-specific actions (dialogs, navigation)
- Keep ViewModels testable (no UI dependencies)
- Use data binding for ALL UI updates
- Keep code-behind thin (<50 lines per file)

**DON'T:**
- Create UI controls in ViewModels (`new Button()`, `new Grid()`, etc.)
- Access UI controls directly from ViewModels
- Put business logic in code-behind
- Use FindControl() for anything except initial wire-up
- Instantiate services directly (`new Service()`)
- Mix concerns in a single ViewModel

---

### B. Code-Behind Rules

**Allowed in Code-Behind:**
```csharp
// 1. Control references (cached)
private Button? _myButton;

// 2. Event wire-up
private void InitializeComponent()
{
    AvaloniaXamlLoader.Load(this);
    _myButton = this.FindControl<Button>("MyButton");
    ViewModel.SomeEvent += OnSomeEvent;
}

// 3. View-specific event handlers (delegation to ViewModel)
private void OnSomeEvent(object? sender, EventArgs e)
{
    ViewModel.HandleSomeEvent();
}

// 4. Focus/scroll requests from ViewModel events
ViewModel.FocusRequested += (s, e) => _textBox?.Focus();
```

**NOT Allowed in Code-Behind:**
```csharp
// ❌ Business logic
private void CalculateSomething() { ... }

// ❌ Service calls
_service.DoSomething();

// ❌ State management
_currentState = newState;

// ❌ Data manipulation
_collection.Add(item);
```

---

### C. ViewModel Rules

**Allowed in ViewModels:**
```csharp
// ✅ Observable properties
[ObservableProperty]
private string _title;

// ✅ Commands
[RelayCommand]
private void DoSomething() { ... }

// ✅ Business logic
private void CalculateResult() { ... }

// ✅ Service orchestration
await _service.DoWorkAsync();

// ✅ Events for view actions
public event EventHandler? FocusRequested;
```

**NOT Allowed in ViewModels:**
```csharp
// ❌ UI control creation
var button = new Button();

// ❌ Dialog UI creation
var dialog = new Window { Content = ... };

// ❌ Direct UI manipulation
someControl.Visibility = Visibility.Hidden;

// ❌ FindControl usage
var control = FindControl<Button>("Name");
```

---

## 8. TESTING STRATEGY

### Unit Testing (After Refactoring)

**Target Coverage:** >90% for ViewModels

**Example Test Structure:**
```csharp
public class FilterEditorViewModelTests
{
    [Fact]
    public async Task SaveFilter_ValidFilter_CallsService()
    {
        // Arrange
        var mockService = new Mock<IFilterService>();
        var vm = new FilterEditorViewModel(mockService.Object);
        vm.FilterName = "Test Filter";

        // Act
        await vm.SaveFilterCommand.ExecuteAsync(null);

        // Assert
        mockService.Verify(s => s.SaveAsync(It.IsAny<Filter>()), Times.Once);
    }
}
```

---

## 9. MIGRATION PATH

### Step-by-Step Approach

1. **Create ViewModel First**
   - Extract business logic from code-behind
   - Create properties, commands, events
   - Wire up to existing code-behind

2. **Incrementally Move Logic**
   - One method at a time
   - Test after each move
   - Update code-behind to delegate to ViewModel

3. **Update XAML Bindings**
   - Replace code-behind references with ViewModel bindings
   - Remove programmatic UI updates
   - Use data templates for dynamic content

4. **Cleanup Code-Behind**
   - Remove business logic
   - Keep only wire-up code
   - Target <50 lines

5. **Add Tests**
   - Unit test ViewModels
   - Integration test critical flows

---

## 10. RISKS & MITIGATION

### Risk 1: FiltersModal Complexity
**Risk:** FiltersModal is too complex to refactor in one pass
**Mitigation:** Incremental refactoring with feature flags, extensive testing

### Risk 2: Breaking Changes
**Risk:** Refactoring breaks existing functionality
**Mitigation:** Comprehensive regression tests, parallel implementation, gradual rollout

### Risk 3: Performance Regression
**Risk:** Refactoring introduces performance issues
**Mitigation:** Performance testing before/after, profiling, optimization pass

### Risk 4: Time Overrun
**Risk:** Refactoring takes longer than estimated
**Mitigation:** Prioritized phases, deliver incrementally, focus on high-impact areas first

---

## CONCLUSION

This codebase has **significant MVVM violations** centered primarily around `FiltersModal.axaml.cs`. The refactoring work is substantial but manageable with a phased approach.

**Key Takeaways:**
1. **FiltersModal is the elephant in the room** - 8,824 lines of code-behind must be refactored
2. **Most ViewModels are well-structured** - The team understands MVVM principles
3. **UI-in-ViewModel violations are isolated** - 4 specific cases to fix
4. **Missing ViewModels are straightforward** - Extract from existing code-behind
5. **God ViewModels can be split** - Clear separation of concerns possible

**Recommendation:** Start with **Priority 1 (FiltersModal)** as it has the highest impact. This single file represents 77% of the MVVM violations in the entire codebase.

---

**Next Steps:**
1. Review this analysis with the team
2. Agree on priorities and timeline
3. Create feature branch for refactoring
4. Begin with FiltersModal deconstruction
5. Deliver incrementally with tests
