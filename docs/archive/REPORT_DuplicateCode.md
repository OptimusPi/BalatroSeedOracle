# DUPLICATE CODE AND CODE SMELLS REPORT

**Generated:** 2025-10-26
**Project:** BalatroSeedOracle C# Avalonia Application

---

## Duplicate Methods (3 patterns)

### 1. DebugLogger.Log/LogError/LogImportant
**Files:** Used in 50+ files
**Similarity:** Same logging pattern repeated everywhere
```csharp
DebugLogger.Log("ComponentName", "Message");
DebugLogger.LogError("ComponentName", $"Error: {ex.Message}");
DebugLogger.LogImportant("ComponentName", "Important message");
```
**Recommendation:** This is acceptable - centralized logging utility
**Priority:** NONE

### 2. Modal Show/Hide Pattern
**Files:** BalatroMainMenu.cs, multiple modal ViewModels
**Pattern:**
```csharp
private void ShowXModal()
{
    var modal = new XModal();
    var standardModal = new StandardModal("TITLE");
    standardModal.SetContent(modal);
    standardModal.BackClicked += (s, e) => HideModalContent();
    ShowModalContent(standardModal, "TITLE");
}
```
**Occurrences:** ShowSearchModal, ShowFiltersModal, ShowToolsModal, ShowSettingsModal, ShowAnalyzeModal
**Lines:** ~15-20 lines each, 5 instances = ~90 lines
**Recommendation:** Extract to `ShowModal<T>(string title) where T : UserControl, new()`
**Priority:** MEDIUM

### 3. Path Resolution (Filter Paths)
**Pattern:**
```csharp
var baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    ?? System.AppDomain.CurrentDomain.BaseDirectory;
var filtersDir = System.IO.Path.Combine(baseDir, "JsonItemFilters");
```
**Occurrences:** BalatroMainMenu.cs (lines 1145-1147, 1191-1193), multiple ViewModels
**Recommendation:** Extract to PathHelper.GetFiltersDirectory()
**Priority:** MEDIUM

---

## Copy-Pasted Code Blocks (5)

### 1. Deck/Stake Selection Arrays
**Files:** SearchModalViewModel.cs, DeckAndStakeViewModel.cs, FilterCreationModalViewModel.cs
**Code:**
```csharp
DeckDisplayValues = new[]
{
    "Red Deck", "Blue Deck", "Yellow Deck", "Green Deck", "Black Deck",
    "Magic Deck", "Nebula Deck", "Ghost Deck", "Abandoned Deck",
    "Checkered Deck", "Zodiac Deck", "Painted Deck", "Anaglyph Deck",
    "Plasma Deck", "Erratic Deck"
};

StakeDisplayValues = new[]
{
    "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold"
};
```
**Lines:** 24 lines duplicated in 3+ files
**Recommendation:** Move to BalatroData.cs as `public static readonly string[] Decks` and `Stakes`
**Priority:** HIGH

### 2. Filter File Loading Pattern
**Files:** FilterService.cs, SearchModalViewModel.cs, FiltersModalViewModel.cs
**Pattern:**
```csharp
var json = System.IO.File.ReadAllText(configPath);
var config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(json);
if (config != null)
{
    // Process config
}
```
**Lines:** ~8 lines, 5+ instances
**Recommendation:** Extract to FilterService.LoadConfig(string path)
**Priority:** MEDIUM

### 3. Clipboard Copy Operations
**Files:** SortableResultsGrid.axaml.cs, SearchModalViewModel.cs, SearchResultViewModel.cs
**Pattern:**
```csharp
var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
if (clipboard != null)
{
    await clipboard.SetTextAsync(text);
    DebugLogger.Log("Component", "Copied to clipboard");
}
```
**Lines:** ~6 lines, 3+ instances
**Recommendation:** Extract to ClipboardService.CopyTextAsync(string text, Control context)
**Priority:** LOW (ClipboardService already exists but not fully utilized)

### 4. SearchResults Directory Creation
**Files:** SearchManager.cs, SearchInstance.cs
**Code:**
```csharp
var searchResultsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SearchResults");
System.IO.Directory.CreateDirectory(searchResultsDir);
var dbPath = System.IO.Path.Combine(searchResultsDir, $"{searchId}.db");
```
**Lines:** 3 lines, 2+ files
**Recommendation:** Extract to PathHelper.EnsureSearchResultsPath(string filename)
**Priority:** MEDIUM

### 5. Exception Logging Pattern
**Files:** 51 files
**Pattern:**
```csharp
catch (Exception ex)
{
    DebugLogger.LogError("ComponentName", $"Error message: {ex.Message}");
}
```
**Assessment:** This is acceptable - standard error handling pattern
**Priority:** NONE

---

## God Classes (4) - Already detailed in Technical Debt Report

1. **SpriteService.cs** - 1361 lines
   - Responsibilities: Sprite loading, caching, conversion, asset management
   - Recommendation: Split into SpriteLoader, SpriteCache, SpriteConverter

2. **SearchInstance.cs** - 1312 lines
   - Responsibilities: Search execution, database ops, state persistence, events
   - Recommendation: Extract DatabaseManager, StateManager

3. **BalatroMainMenu.axaml.cs** - 1250 lines
   - Responsibilities: Modal management, navigation, shader control, widgets
   - Recommendation: Extract ModalManager, ShaderController, WidgetManager

4. **SearchModalViewModel.cs** - 1081 lines
   - Responsibilities: Search state, tabs, results, console, filtering
   - Recommendation: Extract TabManager, ResultsManager (lower priority for ViewModels)

**Priority:** MEDIUM to HIGH

---

## Long Methods (>50 lines) - 12 instances

### Animation Methods (Acceptable - verbose by nature)
1. **BalatroMainMenu.cs:439** - `TransitionToNewModal` - 74 lines (animation keyframes)
2. **BalatroMainMenu.cs:519** - `ShowModalWithAnimation` - 63 lines (animation keyframes)

### Database Methods (Could be split)
3. **SearchInstance.cs:128-167** - `SetupDatabase` - ~40 lines
4. **SearchInstance.cs:169-230** - `FormatColumnName` - ~60 lines

### UI Construction (MVVM anti-pattern - now fixed with XAML tabs)
5-10. **SearchModalViewModel.cs** - Multiple `CreateXTabContent` methods (DELETED in recent refactor) ✓

### Filter Processing
11. **FilterService.cs** - `LoadFiltersFromDirectory` - ~80 lines
12. **VisualBuilderTabViewModel.cs** - `BuildFilterFromVisual` - ~120 lines

**Priority:** LOW - Most are inherently verbose (animations, UI) or already refactored

---

## Deep Nesting (>4 levels) - 8 instances

### 1. BalatroMainMenu.cs - Modal container drag handling
**Lines:** 333-387
**Nesting:** 5 levels (if > if > if > while > if)
```csharp
if (_volumePopup?.IsOpen == true)
{
    if (source == musicButton) { return; }
    while (parent != null)
    {
        if (parent == musicButton) { return; }
    }
    if (_volumePopup.Child is Control popupContent)
    {
        if (popupPosition.HasValue)
        {
            if (!absolutePopupBounds.Contains(clickPosition))
            {
                // Close popup
            }
        }
    }
}
```
**Recommendation:** Extract to `IsClickInsidePopup(Point clickPosition)` helper
**Priority:** MEDIUM

### 2-5. VisualBuilderTab.axaml.cs - Drag and drop logic
**Lines:** Multiple instances in drag/drop event handlers
**Nesting:** 4-5 levels
**Recommendation:** Extract sub-methods for drag validation, drop handling
**Priority:** MEDIUM

### 6-8. SearchInstance.cs - Result processing loops
**Lines:** Various locations in result collection
**Nesting:** 4 levels (try > foreach > if > if)
**Recommendation:** Extract result mapping to separate methods
**Priority:** LOW

---

## Too Many Parameters (>5 params) - 0 instances

No methods found with more than 5 parameters. Good parameter discipline! ✓

---

## Repeated String Literals (10+ patterns)

### File/Directory Names
1. **"SearchResults"** - Used 10+ times
2. **"JsonItemFilters"** - Used 15+ times
3. **"WordLists"** - Used 8+ times
4. **"Assets"** - Used 12+ times
**Recommendation:** Move to `PathConstants` class
**Priority:** HIGH

### Deck/Stake Names (already noted in Copy-Paste section)
5. **"Red Deck", "Blue Deck", etc.** - Duplicated in 3+ files
6. **"White", "Red", "Green", etc.** - Duplicated in 3+ files
**Recommendation:** Move to BalatroData.cs
**Priority:** HIGH

### UI Strings
7. **"FILTER DESIGNER"** - Used 5 times
8. **"SEED SEARCH"** - Used 8 times
9. **"Unknown Filter"** - Used 6 times
**Recommendation:** Move to UIStrings.cs resource file
**Priority:** MEDIUM

### Config Keys
10. **"Data Source="** - Database connection strings - 3+ uses
**Recommendation:** Move to DatabaseConstants
**Priority:** LOW

---

## Feature Envy (3 instances)

### 1. SearchModalViewModel accessing SearchInstance internals
**Location:** SearchModalViewModel.cs:820-832
```csharp
LastKnownResultCount = _searchInstance.ResultCount;
IsSearching = _searchInstance.IsRunning;
TimeElapsed = _searchInstance.SearchDuration.ToString(@"hh\:mm\:ss");
```
**Issue:** ViewModel directly accessing multiple SearchInstance properties
**Recommendation:** SearchInstance should expose `GetStatSnapshot()` method
**Priority:** LOW (acceptable in MVVM)

### 2. BalatroMainMenu accessing ViewModel shader properties
**Location:** BalatroMainMenu.cs:683-875 (shader application methods)
```csharp
internal void ApplyVisualizerTheme(int themeIndex)
{
    if (_background is BalatroShaderBackground shader)
    {
        ViewModel.ApplyVisualizerTheme(shader, themeIndex);
    }
}
```
**Issue:** View calling ViewModel to manipulate another view component
**Recommendation:** Direct shader binding or mediator pattern
**Priority:** LOW

### 3. FilterService accessing FileSystem directly
**Location:** FilterService.cs throughout
**Assessment:** This is acceptable - service layer's job
**Priority:** NONE

---

## Large If/Else Chains (>5 branches) - 2 instances

### 1. ModalRequestedEventArgs handler
**Location:** BalatroMainMenu.cs:142-168
```csharp
switch (e.ModalType)
{
    case ModalType.Search: ShowSearchModal(); break;
    case ModalType.Filters: ShowFiltersModal(); break;
    case ModalType.Analyze: ShowAnalyzeModal(); break;
    case ModalType.Tools: ShowToolsModal(); break;
    case ModalType.Settings: ShowSettingsModal(); break;
    case ModalType.Custom: /* ... */ break;
}
```
**Assessment:** GOOD - Using switch instead of if/else chain
**Priority:** NONE

### 2. Sprite type resolution
**Location:** SpriteService.cs:multiple locations
**Assessment:** Handled with dictionaries and switch statements appropriately
**Priority:** NONE

---

## Circular Dependencies - 0 instances

No circular dependencies detected. Clean dependency graph! ✓

---

## Summary

| Code Smell | Count | Priority | Estimated Effort |
|-------------|-------|----------|-----------------|
| **Duplicate Methods** | 3 patterns | MEDIUM | 4-6 hours |
| **Copy-Pasted Code** | 5 blocks | HIGH | 6-8 hours |
| **God Classes** | 4 | MEDIUM-HIGH | 24-40 hours |
| **Long Methods** | 12 | LOW | 4-8 hours |
| **Deep Nesting** | 8 | MEDIUM | 8-12 hours |
| **Too Many Parameters** | 0 | - | - |
| **Repeated String Literals** | 10+ | HIGH | 4-6 hours |
| **Feature Envy** | 3 | LOW | 2-4 hours |
| **Large If/Else Chains** | 0 (using switch) | - | - |
| **Circular Dependencies** | 0 | - | - |

---

## Critical Refactoring Priorities

### 🔴 HIGH Priority (Do First)
1. **Extract Deck/Stake arrays to BalatroData.cs** - Remove 70+ lines of duplication
   - Effort: 2 hours

2. **Create PathConstants class** - Centralize "SearchResults", "JsonItemFilters", "WordLists"
   - Effort: 3 hours

3. **Extract Modal Show pattern** - Generic `ShowModal<T>()` method
   - Effort: 2 hours

### 🟠 MEDIUM Priority (Schedule for next sprint)
1. **Refactor God Classes** - Split SpriteService, SearchInstance, BalatroMainMenu
   - Effort: 24-40 hours

2. **Extract clipboard operations** - Fully utilize ClipboardService
   - Effort: 2 hours

3. **Simplify deep nesting** - Extract validation helpers
   - Effort: 6-8 hours

### 🟢 LOW Priority (Nice to have)
1. **Extract long animation methods** - Separate AnimationHelper class
   - Effort: 4 hours

2. **Create UIStrings resource file** - Localization preparation
   - Effort: 3 hours

---

## Refactoring Effort Summary

- **High priority items:** 7 hours
- **Medium priority items:** 32-50 hours
- **Low priority items:** 7 hours
- **Total estimated effort:** 46-64 hours

---

## Overall Code Quality Assessment

**Duplication Level:** MODERATE

**Strengths:**
- No circular dependencies
- Good use of switch statements over if/else chains
- No excessive method parameters
- Recent refactoring removed 235 lines of UI-in-code (tab creation)

**Weaknesses:**
- Significant string literal duplication (paths, deck/stake names)
- Several God classes (1000+ lines)
- Modal show pattern repeated 5+ times
- Path resolution logic duplicated

**Recommendation:**
1. Start with HIGH priority items (10-15 hours of work)
2. This will eliminate 60-70% of duplication
3. Schedule God class refactoring for separate sprint
4. Overall code quality is GOOD with room for improvement
