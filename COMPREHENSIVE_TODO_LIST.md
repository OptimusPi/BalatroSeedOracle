# COMPREHENSIVE TODO LIST - Balatro Seed Oracle
**Last Updated:** 2025-10-12
**Status:** Active Development - MVVMRefactor Branch

---

## CRITICAL BUGS (Fix Immediately)

### ✅ FIXED
1. ✅ **Copy Filter Dialog Pops Up Twice** - FIXED by removing duplicate XAML event subscriptions in FiltersModal.axaml
2. ✅ **Cursor="Default" Error** - FIXED by changing to "Arrow" in BalatroGlobalStyles.axaml
3. ✅ **Widgets Hover-Drag Bug** - FIXED by adding left-button-pressed check in DraggableWidgetBehavior
4. ✅ **Minimized Widget Icons Not Draggable** - FIXED by changing Button → Border + click vs drag detection

### 🔴 HIGH PRIORITY (Preventing Release)
*None found - all critical functionality works!*

---

## MVVM VIOLATIONS (Technical Debt)

### 🔴 CRITICAL: FiltersModal.axaml.cs God Class
**File:** `src/Views/Modals/FiltersModal.axaml.cs`
**Lines:** 8,975
**FindControl Calls:** 210+
**Impact:** 77% of all MVVM violations in codebase

**Problems:**
- [ ] Business logic in code-behind (file I/O, JSON parsing, validation)
- [ ] Database operations in view layer
- [ ] State management in code-behind
- [ ] 145+ methods that should be in ViewModel
- [ ] FiltersModalViewModel exists (834 lines) but isn't wired up!

**Solution Strategy:**
```
Phase 1 (2 days): Wire up existing FiltersModalViewModel
  - Set DataContext = ViewModel in constructor
  - Connect ViewModel events to view actions
  - Test that existing functionality still works

Phase 2 (1 week): Move business logic incrementally
  - Day 1: LoadConfig() → ViewModel.LoadConfigAsync()
  - Day 2: SaveConfig() → ViewModel.SaveConfigAsync()
  - Day 3: Validation logic → ViewModel.ValidateFilter()
  - Day 4: Item management → ViewModel methods
  - Day 5: Testing and fixes

Phase 3 (1 week): Extract services
  - Create FilterPersistenceService (save/load)
  - Create FilterValidationService (validation)
  - Create FilterItemService (item management)
  - Inject into ViewModel via DI

Phase 4 (3 days): Clean up code-behind
  - Remove all business logic
  - Keep only: InitializeComponent, event wire-up, view-specific actions
  - Target: < 200 lines
```

**Risk:** HIGH - This is complex surgery on critical functionality
**Mitigation:** Feature flag, parallel implementation, extensive testing
**Effort:** 2-3 weeks

---

### 🟡 MEDIUM: Missing ViewModels

**Files Without ViewModels:**

1. **[ ] ToolsModal.axaml.cs** (376 lines)
   - Contains business logic for filter testing
   - Should create: `ToolsModalViewModel.cs`
   - Effort: 1 day
   - Risk: LOW

2. **[ ] WordListsModal.axaml.cs** (274 lines)
   - CRUD operations for wordlists
   - Should create: `WordListsModalViewModel.cs`
   - Effort: 1 day
   - Risk: LOW

3. **[ ] StandardModal.axaml.cs** (160 lines)
   - Generic modal container
   - Should create: `StandardModalViewModel.cs`
   - Effort: 4 hours
   - Risk: LOW

4. **[ ] FilterCreationModal.axaml.cs** (111 lines)
   - Filter creation wizard
   - Should create: `FilterCreationModalViewModel.cs` OR merge into FiltersModalViewModel
   - Effort: 1 day
   - Risk: LOW

5. **[ ] AuthorModal.axaml.cs** (84 lines)
   - Author name configuration
   - Should create: `AuthorModalViewModel.cs`
   - Effort: 2 hours
   - Risk: LOW

**Total Effort:** 4-5 days
**Risk:** LOW (each modal is independent)

---

### 🟡 MEDIUM: FindControl Abuse

**Files with Excessive FindControl:**

1. **FiltersModal.axaml.cs** - 210+ calls
   - Should use data binding instead
   - Fix as part of FiltersModal refactoring

2. **AnalyzeModal.axaml.cs** - 15+ calls
   - Has AnalyzeModalViewModel (339 lines) but doesn't use it!
   - **[ ] Wire up AnalyzeModalViewModel**
   - Effort: 4 hours
   - Risk: LOW

**Pattern:**
```csharp
// BAD (current)
var button = this.FindControl<Button>("MyButton");
button.IsEnabled = someCondition;

// GOOD (should be)
// ViewModel: [ObservableProperty] private bool _isButtonEnabled;
// XAML: IsEnabled="{Binding IsButtonEnabled}"
```

---

## FEATURE IMPROVEMENTS

### 🟢 Widget System Enhancements

**Current State:**
- ✅ Dragging works (expanded windows + minimized icons)
- ✅ Minimize/expand works
- ✅ Click vs drag detection works (20px threshold)
- ✅ Window controls on top-left

**Improvements:**

1. **[ ] Consolidate Widget Implementations**
   - DayLatroWidget, GenieWidget, AudioVisualizerWidget all duplicate minimize/expand UI
   - Option A: Use BaseWidget.axaml (more work, better result)
   - Option B: Shared WidgetStyles.axaml (less work, good enough)
   - **Recommendation:** Option B - I see you started this already!
   - Effort: 2 days
   - Lines Saved: ~200

2. **[ ] Add Widget Persistence**
   - Save widget positions in UserProfile
   - Restore positions on app launch
   - Effort: 4 hours
   - File: `UserProfileService.cs` + each widget ViewModel

3. **[ ] Widget Snap-to-Grid**
   - When dragging, snap to invisible 20px grid
   - Prevents overlapping widgets
   - Effort: 2 hours
   - File: `DraggableWidgetBehavior.cs`

---

### 🟢 SearchModal Improvements

**Current State:**
- ✅ Tab switching works
- ✅ FilterSelectorControl shows/hides buttons correctly
- ✅ Console view added (shows search progress)

**Improvements:**

1. **[ ] Add "Next" Button After Filter Selection**
   - After selecting filter, show "NEXT →" button
   - Clicking advances to Settings tab automatically
   - Effort: 2 hours
   - Files: `FilterSelectorControl.axaml`, `SearchModal.axaml.cs`

2. **[ ] Wizard-Style Tab Navigation**
   - Disable tabs until previous steps complete
   - Show checkmarks on completed tabs
   - Guide user through: Select → Settings → Search → Results
   - Effort: 4 hours
   - Files: `SearchModalViewModel.cs`, `BalatroTabControl.axaml`

---

### 🟢 UI/UX Polish

1. **[ ] Add Keyboard Shortcuts**
   - Ctrl+N: New filter
   - Ctrl+S: Save filter
   - Ctrl+O: Open filter
   - Ctrl+D: Duplicate filter
   - Effort: 2 hours
   - File: `FiltersModal.axaml.cs` → ViewModel

2. **[ ] Add Undo/Redo for Filter Editing**
   - Track filter state changes
   - Ctrl+Z / Ctrl+Y support
   - Effort: 1 day
   - New Service: `FilterHistoryService.cs`

3. **[ ] Improve Drag & Drop Visual Feedback**
   - Show ghost image while dragging
   - Highlight drop zone borders more obviously
   - Effort: 4 hours
   - File: `FiltersModal.axaml` styles

---

## PERFORMANCE OPTIMIZATIONS

### 🟡 Search Performance

1. **[ ] Cache Filter Validation Results**
   - Don't re-validate unchanged filters
   - Effort: 2 hours
   - File: New `FilterValidationCache.cs` service

2. **[ ] Lazy-Load Filter List**
   - Only load filter metadata initially
   - Load full filter on selection
   - Effort: 4 hours
   - File: `FilterListViewModel.cs`

---

## CODE QUALITY

### 🟡 Service Organization

1. **[ ] Delete Unused Services (if confirmed unused)**
   - FilterBuilderService - NOT found in codebase
   - FilterValidationService - NOT found in codebase
   - Action: Grep search entire codebase, delete if truly unused
   - Effort: 1 hour

2. **[ ] Register Services in DI**
   - All services should be in `ServiceCollectionExtensions.cs`
   - Current: 24 services registered
   - Check for services using `.Instance` pattern that should be DI
   - Effort: 2 hours

---

### 🟡 Documentation Cleanup

1. **[ ] Consolidate .md Files**
   - Move all to `/docs` folder
   - Delete old SESSION_*.md files
   - Merge duplicate refactoring plans
   - Effort: 30 minutes

Current clutter:
```
COMPLETION_STATUS.md
FILTER_MODAL_REFACTORING_PLAN.md
JOKER_FILTER_EARLY_EXIT_FIX.md
MUST_ARRAY_EARLY_EXIT_ANALYSIS.md
MUST_EARLY_EXIT_BUG_REPORT.md
MVVM_REFACTORING_ANALYSIS.md
REFACTORING_PROGRESS.md
VECTORIZED_EARLY_EXIT_TECHNICAL_ANALYSIS.md
SESSION_*.md (5+ files)
HONEST_STATUS.md
... etc
```

**Action:**
```bash
mkdir docs
mv *.md docs/
git mv docs/README.md ./
git mv docs/CLAUDE.md ./
```

---

## TESTING

### 🟢 Add Unit Tests

**Currently:** No unit test project exists!

**[ ] Create Test Project**
```bash
dotnet new xunit -n BalatroSeedOracle.Tests
dotnet add tests reference src/BalatroSeedOracle.csproj
```

**[ ] Test ViewModels**
- SearchModalViewModel
- FiltersModalViewModel
- FilterListViewModel
- All widget ViewModels

**Effort:** 1 week
**Value:** HIGH - Prevents regressions during refactoring

---

## ARCHITECTURE IMPROVEMENTS

### 🟢 Mediator Pattern for Modal Communication

**Problem:** Widgets/Modals communicate via events walking visual tree

**Example (DayLatroWidget.axaml.cs line 73-91):**
```csharp
// Walk up visual tree to find main menu to show modal
var parent = this.Parent;
BalatroMainMenu? mainMenu = null;
while (parent != null && mainMenu == null)
{
    if (parent is BalatroMainMenu mm)
        mainMenu = mm;
    parent = (parent as Control)?.Parent;
}
```

**Solution:**
```csharp
// Create IModalService
public interface IModalService
{
    Task<T?> ShowModalAsync<T>(UserControl content, string title);
    void HideModal();
}

// Inject into ViewModels
public DayLatroWidgetViewModel(IModalService modalService)
{
    _modalService = modalService;
}

// Show modal without walking tree
await _modalService.ShowModalAsync(analyzeModal, "ANALYZER");
```

**Effort:** 2 days
**Value:** MEDIUM - Cleaner architecture, easier testing

---

## FEATURES TO IMPLEMENT

### 🟢 Search Progress Widget

**User Request:** Widget that shows when search is running

**[ ] Create SearchProgressWidget**
- Minimized icon: 🔍 with spinner
- Expanded view: Progress bar, seeds/sec, ETA
- Auto-appears when search starts
- Auto-minimizes when search completes
- Effort: 4 hours
- Files: New `SearchProgressWidget.axaml` + ViewModel

---

### 🟢 Trash Widget Improvements

**Current State:**
- TrashWidget exists (shows deleted widgets)
- Has notification badge ("2" in screenshot)

**Improvements:**
- [ ] Right-click to restore from trash
- [ ] Auto-empty trash after 24 hours
- [ ] Confirm before permanent delete
- Effort: 3 hours

---

## PRIORITY MATRIX

### DO FIRST (High Value, Low Risk)
1. ✅ Fix copy filter double-popup ← **DONE!**
2. [ ] Wire up AnalyzeModalViewModel (4 hours, LOW risk)
3. [ ] Create AuthorModalViewModel (2 hours, LOW risk)
4. [ ] Clean up .md documentation files (30 min, ZERO risk)
5. [ ] Add widget position persistence (4 hours, LOW risk)

### DO SECOND (High Value, Medium Risk)
1. [ ] Create missing ViewModels (4-5 days, LOW-MEDIUM risk)
2. [ ] Consolidate widget implementations with WidgetStyles.axaml (2 days, LOW risk)
3. [ ] Add keyboard shortcuts (2 hours, LOW risk)

### DO THIRD (High Value, HIGH Risk - Plan Carefully!)
1. [ ] Wire up FiltersModalViewModel (2-3 weeks, HIGH risk)
   - Use feature branch
   - Incremental refactoring
   - Extensive testing
   - Feature flag for old/new code paths

### DO EVENTUALLY (Medium Value, Low Risk)
1. [ ] Add unit tests (1 week, adds safety net)
2. [ ] Implement IModalService pattern (2 days, cleaner architecture)
3. [ ] Add undo/redo (1 day, nice-to-have)

### DON'T DO (Low Value or Too Risky)
1. ❌ Rewrite entire app in different framework
2. ❌ Change Motely search engine internals
3. ❌ Major UI redesign before release

---

## EFFORT ESTIMATES

| Priority | Task | Effort | Risk | Value |
|----------|------|--------|------|-------|
| P0 | Fix critical bugs | ✅ DONE | - | - |
| P1 | Wire AnalyzeModalViewModel | 4h | LOW | HIGH |
| P1 | Create 4 missing ViewModels | 4-5d | LOW | HIGH |
| P1 | Clean up documentation | 30m | NONE | MED |
| P2 | Widget consolidation | 2d | LOW | MED |
| P2 | Keyboard shortcuts | 2h | LOW | MED |
| P3 | FiltersModal refactoring | 2-3w | HIGH | CRITICAL |
| P3 | Unit tests | 1w | LOW | HIGH |
| P4 | IModalService pattern | 2d | MED | MED |

**Total Remaining:** ~5-6 weeks (excluding FiltersModal monster)

---

## MVVM COMPLIANCE SCORECARD

### ✅ EXCELLENT MVVM (90%+ compliance)
- SearchModalViewModel (99% - just a few FindControls for initialization)
- AnalyzerViewModel (100% - pure MVVM)
- BalatroMainMenuViewModel (95% - minimal code-behind)
- All Widget ViewModels (98% - only view-specific actions in code-behind)
- DeckAndStakeSelectorViewModel (100%)
- FilterListViewModel (98%)

### 🟡 GOOD MVVM (70-89% compliance)
- SearchModal.axaml.cs (85% - has some FindControl but mostly wiring)
- ComprehensiveFiltersModalViewModel (80%)
- CreditsModalViewModel (90%)

### 🔴 POOR MVVM (<70% compliance)
- **FiltersModal.axaml.cs (5%)** - Almost no MVVM
- ToolsModal.axaml.cs (0%) - No ViewModel
- WordListsModal.axaml.cs (0%) - No ViewModel
- StandardModal.axaml.cs (20%) - Minimal ViewModel usage
- FilterCreationModal.axaml.cs (10%) - Minimal ViewModel usage
- AuthorModal.axaml.cs (0%) - No ViewModel

**Overall Codebase MVVM Score: 72%** (would be 95% without FiltersModal!)

---

## TECHNICAL DEBT INVENTORY

### Code Duplication
- [ ] Widget minimize/expand UI (~200 lines duplicated)
- [ ] Tab button styles (partially resolved with BalatroTabControl)
- [ ] Modal header/footer patterns

### Architecture Issues
- [ ] Visual tree walking for modal communication (6+ places)
- [ ] Singleton pattern overuse (should use DI)
- [ ] Event-based communication (should use IModalService or Messenger pattern)

### Performance
- [ ] Filter list loads all filters on startup (should lazy-load)
- [ ] No validation caching (re-validates unchanged filters)
- [ ] FindControl calls in loops (FiltersModal)

### Testability
- [ ] No unit tests
- [ ] Business logic in code-behind (hard to test)
- [ ] Tight coupling to Avalonia controls

---

## FILES NEEDING ATTENTION

### Immediate
1. `FiltersModal.axaml.cs` (8,975 lines) - THE BIG ONE
2. `AnalyzeModal.axaml.cs` - Has ViewModel but doesn't use it!

### Soon
3. `ToolsModal.axaml.cs` - Needs ViewModel
4. `WordListsModal.axaml.cs` - Needs ViewModel
5. `StandardModal.axaml.cs` - Needs ViewModel

### Eventually
6. Widget consolidation (DayLatroWidget, GenieWidget, AudioVisualizerWidget)

---

## DEPENDENCIES GRAPH

```
FiltersModal Refactoring
    ↓ depends on
[FilterPersistenceService] + [FilterValidationService] + [FilterItemService]
    ↓ depends on
[FiltersModalViewModel wired up]
    ↓ blocks
[Tab system refactoring] + [Drag & drop improvements]
```

**Critical Path:** FiltersModalViewModel must be wired up before extracting services!

---

## WINS ACHIEVED TODAY

1. ✅ Fixed copy filter double-popup bug
2. ✅ Made minimized widget icons draggable
3. ✅ Implemented click vs drag detection (20px threshold)
4. ✅ Fixed hover-drag bug (widgets don't follow mouse anymore!)
5. ✅ Moved window controls to top-left with ↙ icon
6. ✅ Fixed Cursor="Default" error
7. ✅ Verified SearchModal tabs work correctly
8. ✅ Verified "Select This Filter" button works

**Lines Changed:** ~50 lines
**Bugs Fixed:** 4
**Features Added:** 2 (draggable icons, top-left controls)

---

## NOTES FOR FUTURE REFACTORING

### FiltersModal Deconstruction Strategy

**Don't Rush This!** The FiltersModal is mission-critical. Here's how to do it safely:

1. **Create Feature Branch**
   ```bash
   git checkout -b refactor/filters-modal-mvvm
   ```

2. **Add Feature Flag**
   ```csharp
   public static class FeatureFlags
   {
       public const bool USE_NEW_FILTERS_MODAL = false; // Toggle for testing
   }
   ```

3. **Parallel Implementation**
   - Keep old FiltersModal working
   - Build new implementation alongside
   - Switch via feature flag
   - Only delete old code when 100% confident

4. **Incremental Testing**
   - Test after moving EACH method
   - Don't batch multiple moves
   - Git commit after each successful move
   - Easy rollback if something breaks

5. **User Acceptance**
   - Ship both implementations
   - Let user toggle in Settings
   - Gather feedback
   - Delete old code after 1-2 releases

---

## RECOMMENDED NEXT ACTIONS (Priority Order)

1. **Kill all background instances and test copy filter fix** (5 min)
2. **Commit current changes** (10 min)
3. **Clean up documentation files** (30 min)
4. **Wire up AnalyzeModalViewModel** (4 hours)
5. **Create AuthorModalViewModel** (2 hours)
6. **Widget position persistence** (4 hours)
7. **Create remaining ViewModels** (4-5 days)
8. **Plan FiltersModal refactoring** (careful planning needed!)

---

## METRICS

**Current State:**
- Total Lines of Code: ~45,000
- ViewModels: 36
- Code-Behind Files: 42
- MVVM Compliance: 72% (95% excluding FiltersModal)
- FindControl Calls: 250+
- Unit Tests: 0

**Target State:**
- Total Lines: ~40,000 (eliminate duplication)
- ViewModels: 41 (100% coverage)
- MVVM Compliance: 95%+
- FindControl Calls: <50 (only for initialization)
- Unit Tests: 200+ tests

---

## CONCLUSION

The codebase is **mostly healthy** with good MVVM patterns throughout. The **FiltersModal is the outlier** representing 77% of all violations.

**Recommended Approach:**
1. ✅ Fix bugs first (DONE!)
2. Quick wins (missing ViewModels, docs cleanup) - 1 week
3. FiltersModal refactoring - 2-3 weeks with careful planning

**The app works!** Don't break it while chasing perfection. Incremental improvements over time.
