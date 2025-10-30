# TECHNICAL DEBT REPORT

**Generated:** 2025-10-26
**Project:** BalatroSeedOracle C# Avalonia Application

---

## Blocking Async Calls (CRITICAL) - 2 instances

### X:\BalatroSeedOracle\src\Services\UserProfileService.cs:146
```csharp
SaveProfileToDiskAsync().GetAwaiter().GetResult();
```
**Location:** Property setter
**Impact:** UI freeze risk, potential deadlock
**Context:** Called from property setter, blocking the setter
**Fix:** Make setter async or use fire-and-forget with proper error handling
**Priority:** CRITICAL

### X:\BalatroSeedOracle\src\Services\UserProfileService.cs:354
```csharp
SaveProfileToDiskAsync().GetAwaiter().GetResult();
```
**Location:** Inside another method
**Impact:** UI freeze risk, potential deadlock
**Context:** Synchronous save in async context
**Fix:** Await properly or use background task
**Priority:** CRITICAL

**Total Blocking Async:** 2 instances
**Risk Level:** HIGH - Can cause UI freezes and deadlocks

---

## Async Void Methods (HIGH) - 17 instances

**Note:** Many are event handlers which is acceptable. Flagging only non-event-handler async void:

### Potentially Problematic

1. **X:\BalatroSeedOracle\src\ViewModels\MainWindowViewModel.cs:88**
   ```csharp
   private async void InitializeAsync()
   ```
   **Issue:** Initialization method - exceptions can't be caught
   **Fix:** Change to `async Task` and await in constructor wrapper
   **Priority:** MEDIUM

### Acceptable (Event Handlers)

2. **X:\BalatroSeedOracle\src\Controls\SortableResultsGrid.axaml.cs:379** - `CopySeed(string seed)` - clipboard operation
3. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:173** - `ShowSearchModal()` - UI event
4. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:207** - `ShowFiltersModal()` - UI event
5. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:439** - `TransitionToNewModal()` - animation trigger
6. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:519** - `ShowModalWithAnimation()` - animation trigger
7. **X:\BalatroSeedOracle\src\ViewModels\DayLatroWidgetViewModel.cs:380** - `OnExpanded()` - event handler
8. **X:\BalatroSeedOracle\src\ViewModels\DayLatroWidgetViewModel.cs:413** - `OnSubmitScore()` - button click
9. **X:\BalatroSeedOracle\src\ViewModels\SearchResultViewModel.cs:89** - `CopySeed(string? seed)` - clipboard
10. **X:\BalatroSeedOracle\src\Components\ResponsiveCard.axaml.cs:287** - `OnPointerMoved()` - pointer event
11. **X:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml.cs:317** - `OnPointerReleasedManualDrag()` - drag event
12. **X:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml.cs:675** - `OnStartOverClick()` - button click
13. **X:\BalatroSeedOracle\src\Views\Modals\FilterCreationModal.axaml.cs:66** - `OnFilterDeleteRequested()` - delete event
14. **X:\BalatroSeedOracle\src\Components\FilterSelector.axaml.cs:127** - `OnLoaded()` - lifecycle event
15. **X:\BalatroSeedOracle\src\Components\FilterSelector.axaml.cs:144** - `RefreshFilters()` - public method
16. **X:\BalatroSeedOracle\src\Views\Modals\ToolsModal.axaml.cs:31** - `OnImportFilesClick()` - file operation
17. **X:\BalatroSeedOracle\src\Views\Modals\WordListsModal.axaml.cs:132** - `OnFileSelectionChanged()` - selection change
18. **X:\BalatroSeedOracle\src\Views\Modals\WordListsModal.axaml.cs:206** - `OnPasteClick()` - paste event

**Assessment:** Most async void methods are event handlers which is standard practice. Only 1-2 need fixing.

---

## Fire-and-Forget Async (MEDIUM) - 0 instances

No `_ = SomeAsync()` patterns found. Good practice!

---

## Hardcoded Paths/Strings (MEDIUM) - Multiple instances

### Database Paths
```csharp
// X:\BalatroSeedOracle\src\Services\SearchManager.cs:39
var searchResultsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SearchResults");
```
**Occurrences:** SearchManager.cs, SearchInstance.cs, multiple ViewModels
**Issue:** "SearchResults", "JsonItemFilters", "WordLists" hardcoded
**Fix:** Move to AppSettings or Constants class
**Priority:** MEDIUM

### Magic Strings in Code
```csharp
// Deck names, stake names, filter names throughout ViewModels
DeckDisplayValues = new[] { "Red Deck", "Blue Deck", ... }
StakeDisplayValues = new[] { "White", "Red", "Green", ... }
```
**Issue:** Should be in BalatroData.cs constants
**Priority:** LOW - Already centralized in some places

---

## Catch-All Exceptions (MEDIUM) - 51 files

**Files with catch(Exception):** 51 total

Most are **acceptable** with proper logging:
- X:\BalatroSeedOracle\src\Services\SearchManager.cs - Logs and returns error object
- X:\BalatroSeedOracle\src\ViewModels\SearchModalViewModel.cs - Logs with DebugLogger
- X:\BalatroSeedOracle\src\Services\SearchInstance.cs - Critical section, logs errors

**Pattern observed:**
```csharp
catch (Exception ex)
{
    DebugLogger.LogError("Component", $"Error: {ex.Message}");
    // Often re-throws or sets error state
}
```

**Assessment:** Generally good - most catch blocks LOG the error. A few swallow errors without logging.

**Recommendation:** Audit the 51 files for:
1. Silent catch blocks (no logging)
2. Catches that should be specific (ArgumentException, FileNotFoundException, etc.)

**Priority:** MEDIUM

---

## Null Reference Suppressions (LOW)

Avalonia uses nullable reference types. Some `!` operators found but appear appropriate for framework integration.

**Priority:** LOW

---

## Reflection Usage (LOW) - 1 instance

### X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:1018-1028
```csharp
var searchIdProperty = icon.GetType().GetField("_searchId",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (searchIdProperty != null)
{
    var iconSearchId = searchIdProperty.GetValue(icon) as string;
    // ...
}
```
**Issue:** Accessing private field via reflection to match SearchDesktopIcon
**Fix:** Add public `SearchId` property to SearchDesktopIcon
**Impact:** Fragile, breaks if field renamed
**Priority:** MEDIUM

---

## P/Invoke Calls (LOW) - 0 instances

No P/Invoke calls detected in src directory.

---

## Unsafe Code Blocks (LOW) - 0 instances

No unsafe code blocks found.

---

## Legacy Patterns (LOW)

### Manual INotifyPropertyChanged
The codebase uses **CommunityToolkit.Mvvm** with `[ObservableProperty]` attributes - modern approach.

**No legacy patterns detected.** ✓

---

## Additional Debt Findings

### God Classes (>500 lines)
1. **SpriteService.cs** - 1361 lines
   - **Responsibilities:** Loading sprites, managing sprite cache, conversions
   - **Recommendation:** Split into SpriteLoader, SpriteCache, SpriteConverter
   - **Priority:** MEDIUM

2. **SearchInstance.cs** - 1312 lines
   - **Responsibilities:** Search execution, database management, state persistence, event handling
   - **Recommendation:** Extract DatabaseManager, StateManager, EventCoordinator
   - **Priority:** MEDIUM

3. **BalatroMainMenu.axaml.cs** - 1250 lines
   - **Responsibilities:** Modal management, navigation, shader control, widget management
   - **Recommendation:** Extract ModalManager, ShaderController, WidgetManager
   - **Priority:** MEDIUM

4. **SearchModalViewModel.cs** - 1081 lines
   - **Responsibilities:** Search UI state, tab management, results filtering, console output
   - **Recommendation:** Extract TabManager, ResultsManager, ConsoleManager
   - **Priority:** LOW (MVVM ViewModels can be larger)

### Long Methods (>50 lines)
Multiple methods exceed 50 lines, particularly:
- Animation methods in BalatroMainMenu.cs (TransitionToNewModal, ShowModalWithAnimation)
- Database setup methods in SearchInstance.cs
- UI construction methods in ViewModels

**Priority:** LOW - Many are animation keyframes which are verbose by nature

---

## Debt Summary

| Category | Count | Risk Level | Priority |
|----------|-------|-----------|----------|
| **Blocking Async Calls** | 2 | CRITICAL | CRITICAL |
| **Async Void Methods** | 1 problematic | HIGH | MEDIUM |
| **Fire-and-Forget Async** | 0 | - | - |
| **Hardcoded Paths** | ~10+ | MEDIUM | MEDIUM |
| **Catch-All Exceptions** | 51 files | MEDIUM | MEDIUM |
| **Reflection Usage** | 1 | LOW | MEDIUM |
| **P/Invoke** | 0 | - | - |
| **Unsafe Code** | 0 | - | - |
| **Legacy Patterns** | 0 | - | - |
| **God Classes** | 4 | MEDIUM | MEDIUM |

---

## Critical Priorities

### 🔴 CRITICAL (Fix Immediately)
1. **UserProfileService.cs** - Replace `.GetAwaiter().GetResult()` with proper async/await
   - Lines 146, 354
   - **Estimated effort:** 2-4 hours

### 🟠 HIGH (Fix Soon)
1. **SearchInstance.cs** - Refactor into smaller classes (1312 lines)
2. **SpriteService.cs** - Split into SpriteLoader + SpriteCache + Converter (1361 lines)

### 🟡 MEDIUM (Technical Cleanup)
1. Centralize hardcoded paths ("SearchResults", "JsonItemFilters", "WordLists") into AppSettings
2. Fix reflection usage in BalatroMainMenu.cs:1018 (add public property)
3. Audit all 51 catch(Exception) blocks for silent failures
4. Refactor BalatroMainMenu.cs (1250 lines) - extract modal/shader/widget managers

### 🟢 LOW (Nice to Have)
1. Extract long animation methods into separate helper class
2. Review nullable reference warnings

---

## Estimated Total Effort

- **Critical items:** 4-8 hours
- **High priority:** 16-24 hours
- **Medium priority:** 24-40 hours
- **Low priority:** 8-16 hours

**Total:** 52-88 hours of refactoring work

---

## Overall Debt Assessment

**Debt Level:** MODERATE

**Strengths:**
- No P/Invoke or unsafe code
- Modern MVVM with source generators
- No legacy INotifyPropertyChanged by hand
- Good logging practices in exception handlers
- No fire-and-forget async

**Weaknesses:**
- 2 critical blocking async calls
- Several God classes (>1000 lines)
- Hardcoded paths scattered throughout
- Reflection for private field access

**Recommendation:** Address CRITICAL blocking async calls immediately. Schedule refactoring of God classes over next sprint. The codebase is maintainable but could benefit from better separation of concerns.
