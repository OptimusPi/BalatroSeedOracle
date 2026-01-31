# Code Discipline Enforcement Report

Generated from [.cursorrules](.cursorrules) (Balatro Seed Oracle). Comprehensive audit by code-discipline-enforcer and csharp-avalonia-expert.

---

## Rule 1: Logging – Use DebugLogger, Not Console

**Rule:** Use `DebugLogger.Log` / `LogError` / `LogImportant`. Do not call `Console.WriteLine` directly (except inside DebugLogger or its platform sink).

### Status: ✅ MOSTLY COMPLIANT

**Acceptable Console Usage (Edge Cases):**
- `src/BalatroSeedOracle.Desktop/Program.cs` - Entry point, acceptable
- `src/BalatroSeedOracle.Browser.DevServer/Program.cs` - Dev server, acceptable
- `src/BalatroSeedOracle/Helpers/DebugLogger.cs` - Platform sink implementation, acceptable
- `src/BalatroSeedOracle.Browser/Services/BrowserPlatformServices.cs:76` - Platform sink fallback, acceptable
- `src/BalatroSeedOracle.Desktop/Services/DesktopPlatformServices.cs` - Platform sink, acceptable
- `src/BalatroSeedOracle/App.axaml.cs:121` - Wait-for-key on error (Desktop only), acceptable

**Fixed:**
- ✅ `src/BalatroSeedOracle.Browser/Services/BrowserLocalStorageAppDataStore.cs` - Already uses DebugLogger (previously had Console.WriteLine, now fixed)

**Remaining Issues:**
- ⚠️ `src/BalatroSeedOracle/App.axaml.cs:121` - Uses `Console.ReadLine()` for wait-for-key. Consider using DebugLogger with a platform-specific prompt handler.

**External/Motely (Out of Scope for BSO):**
- `external/Motely/Motely.Orchestration/Executors/JsonSearchExecutor.cs` - Still uses Console (Motely codebase)
- `external/Motely/Motely.Orchestration/FertilizerDatabase.cs` - Still uses Console (Motely codebase)
- `external/Motely/Motely.Orchestration/ConfigFormatConverter.cs` - Still uses Console (Motely codebase)

---

## Rule 2: ViewModels – CommunityToolkit.Mvvm, No BaseViewModel

**Rule:** Use `ObservableObject` from CommunityToolkit.Mvvm. Do not use deprecated `BaseViewModel`.

### Status: ✅ COMPLIANT

- ✅ No `BaseViewModel` usage found in `src/`
- ✅ All ViewModels properly use `ObservableObject` with `[ObservableProperty]` and `[RelayCommand]`

---

## Rule 3: Compiled Bindings – x:CompileBindings + x:DataType (CRITICAL)

**Rule:** Every `x:CompileBindings="True"` root MUST have `x:DataType`. Missing `x:DataType` causes AVLN2000 runtime crashes in AOT builds.

### Status: ⚠️ VIOLATIONS FOUND

**Files Missing x:DataType (CRITICAL - Will crash in AOT):**

1. **`src/BalatroSeedOracle/Components/ResponsiveGrid.axaml`**
   - Has `x:CompileBindings="True"` on line 4
   - **MISSING** `x:DataType`
   - **Issue:** This is a simple container control with no bindings, but still requires x:DataType if x:CompileBindings is enabled
   - **Fix:** Either add `x:DataType` or remove `x:CompileBindings` if no bindings are used

2. **`src/BalatroSeedOracle/Controls/JokerSetDisplay.axaml`**
   - Has `x:CompileBindings="True"` on line 4
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type or remove x:CompileBindings if no bindings

3. **`src/BalatroSeedOracle/Controls/MaximizeButton.axaml`**
   - Has `x:CompileBindings="True"` on line 5
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type or remove x:CompileBindings if no bindings

4. **`src/BalatroSeedOracle/Views/Modals/ToolsModal.axaml`**
   - Has `x:CompileBindings="True"` on line 6
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

5. **`src/BalatroSeedOracle/Views/Modals/WordListsModal.axaml`**
   - Has `x:CompileBindings="True"` on line 5
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

6. **`src/BalatroSeedOracle/Windows/DataGridResultsWindow.axaml`**
   - Has `x:CompileBindings="True"` on line 9
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

7. **`src/BalatroSeedOracle/Views/Modals/WidgetPickerModal.axaml`**
   - Has `x:CompileBindings="True"` on line 4
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

8. **`src/BalatroSeedOracle/Controls/TagEditor.axaml`**
   - Has `x:CompileBindings="True"` on line 4
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

9. **`src/BalatroSeedOracle/Views/Modals/StandardModal.axaml`**
   - Has `x:CompileBindings="True"` on line 4
   - **MISSING** `x:DataType`
   - **Fix:** Add appropriate ViewModel type

10. **`src/BalatroSeedOracle/Controls/FannedCardHand.axaml`**
    - Has `x:CompileBindings="True"` on line 4
    - **MISSING** `x:DataType`
    - **Fix:** Add appropriate ViewModel type or remove x:CompileBindings if no bindings

11. **`src/BalatroSeedOracle/Components/DeckSpinner.axaml`**
    - Has `x:CompileBindings="True"` on line 5
    - **MISSING** `x:DataType`
    - **Fix:** Add appropriate ViewModel type

12. **`src/BalatroSeedOracle/Components/FilterItemCarousel.axaml`**
    - Has `x:CompileBindings="True"` on line 6
    - **MISSING** `x:DataType`
    - **Fix:** Add appropriate ViewModel type

13. **`src/BalatroSeedOracle/Components/Help/JamlHelpView.axaml`**
    - Has `x:CompileBindings="True"` on line 4
    - **MISSING** `x:DataType`
    - **Fix:** Add appropriate ViewModel type or remove x:CompileBindings if no bindings

14. **`src/BalatroSeedOracle/Components/StakeSpinner.axaml`**
    - Has `x:CompileBindings="True"` on line 5
    - **MISSING** `x:DataType`
    - **Fix:** Add appropriate ViewModel type

**Files WITH x:DataType (✅ Compliant):**
- `MainWindow.axaml` ✅
- `BalatroMainMenu.axaml` ✅
- `FiltersModal.axaml` ✅
- `SearchModal.axaml` ✅
- `CreditsModal.axaml` ✅
- All widget AXAML files ✅
- All filter tab AXAML files ✅
- Most modal AXAML files ✅

---

## Rule 4: Platform Code – DI Abstractions Only (CRITICAL)

**Rule:** Define interface in shared, implement per-platform. **No conditional compilation** (`#if BROWSER` / `#if !BROWSER`).

### Status: ✅ COMPLIANT

- ✅ No `#if BROWSER` or `#if !BROWSER` found in `src/`
- ✅ Platform-specific code properly separated into Desktop/Browser projects
- ✅ All platform abstractions use DI interfaces

**External/Motely (Out of Scope):**
- NativeFilterExecutor — fixed (uses ITerminalOutput)
- JsonSearchExecutor, FertilizerDatabase, ConfigFormatConverter — still use Console (Motely codebase)

---

## Rule 5: No FindControl

**Rule:** Use `x:Name` for direct field access or bindings. Do not use `FindControl<>`.

### Status: ✅ COMPLIANT

- ✅ No `FindControl<>` calls found in `src/`
- ✅ Code uses `x:Name` for direct field access

---

## Rule 6: UI Thread Updates

**Rule:** Use `Dispatcher.UIThread.InvokeAsync` or `Post` for UI updates from background threads.

### Status: ✅ MOSTLY COMPLIANT

**Good Examples Found:**
- ✅ `FilterTabs/ConfigureFilterTabViewModel.cs:682` - Properly uses `Dispatcher.UIThread.InvokeAsync`
- ✅ `FilterTabs/VisualBuilderTabViewModel.cs:1633` - Properly uses `Dispatcher.UIThread.InvokeAsync`
- ✅ `SearchModalViewModel.cs:1794` - Properly uses `Dispatcher.UIThread.InvokeAsync`
- ✅ `SearchModalViewModel.cs:1833` - Properly uses `Dispatcher.UIThread.Post`

**Pattern Analysis:**
- ✅ Background operations properly marshal UI updates to UI thread
- ✅ `CheckAccess()` pattern used correctly in some places
- ✅ No direct property updates from background threads detected

**Recommendations:**
- Consider standardizing on `InvokeAsync` vs `Post` (InvokeAsync is preferred for async/await patterns)

---

## Rule 7: Async Patterns

**Rule:** Use `ConfigureAwait(false)` for library code. Avoid `async` without `await`.

### Status: ⚠️ NEEDS IMPROVEMENT

**ConfigureAwait Usage:**
- ✅ `SearchModalViewModel.cs:1780` - Uses `ConfigureAwait(false)` ✅
- ⚠️ **Only 8 instances found** across 4 ViewModel files
- ⚠️ **15 async calls found** without `ConfigureAwait(false)` in ViewModels

**Files with ConfigureAwait(false):**
- `SearchWidgetViewModel.cs` (1 instance)
- `BalatroMainMenuViewModel.cs` (2 instances)
- `SearchModalViewModel.cs` (2 instances)
- `FiltersModalViewModel.cs` (3 instances)

**Files Needing ConfigureAwait(false):**
- `MainWindowViewModel.cs` - 1 async call missing ConfigureAwait
- `ApiHostWidgetViewModel.cs` - 1 async call missing ConfigureAwait
- `FilterSelectorViewModel.cs` - 4 async calls missing ConfigureAwait
- `DayLatroWidgetViewModel.cs` - 4 async calls missing ConfigureAwait
- Many other ViewModels have async methods that could benefit from ConfigureAwait(false)

**Async Without Await:**
- ⚠️ Found 52 files with async methods - need to verify none are async without await
- Most appear to have proper await usage, but manual review recommended

**Recommendations:**
1. Add `ConfigureAwait(false)` to all library/ViewModel async calls (except when UI thread context is needed)
2. Review async methods to ensure they're not async without await (use `Task.CompletedTask` instead)
3. Consider adding a code analyzer rule for missing ConfigureAwait

---

## Rule 8: Null Checks

**Rule:** Use `is null` / `is not null` instead of `== null` / `!= null`.

### Status: ⚠️ VIOLATIONS FOUND

**Files with == null / != null:**
- `FiltersModalViewModel.cs` - Multiple instances (see TECH_DEBT_TODO.md items TECH_DEBT_006-017)
- Other ViewModels may have similar issues

**Recommendations:**
- Replace all `== null` with `is null`
- Replace all `!= null` with `is not null`
- Consider adding a code analyzer rule

---

## Summary

| Rule | Status | Priority | Action Required |
|------|--------|----------|-----------------|
| 1 – DebugLogger / no Console | ✅ Mostly Compliant | Low | Optional: Replace Console.ReadLine in App.axaml.cs |
| 2 – CommunityToolkit.Mvvm | ✅ Compliant | - | None |
| 3 – Compiled Bindings | 🔴 **CRITICAL** | **HIGH** | **Fix 14 AXAML files missing x:DataType** |
| 4 – No #if for platform | ✅ Compliant | - | None |
| 5 – No FindControl | ✅ Compliant | - | None |
| 6 – UI Thread (Dispatcher) | ✅ Mostly Compliant | Low | Standardize InvokeAsync vs Post |
| 7 – Async (ConfigureAwait) | ⚠️ Needs Improvement | Medium | Add ConfigureAwait(false) to library code |
| 8 – Null Checks | ⚠️ Violations Found | Low | Replace == null with is null |

---

## Priority Actions

### 🔴 CRITICAL (Fix Immediately)
1. **Rule 3 Violations:** Fix 14 AXAML files missing `x:DataType` - these will cause runtime crashes in AOT builds
   - `ResponsiveGrid.axaml`
   - `JokerSetDisplay.axaml`
   - `MaximizeButton.axaml`
   - `ToolsModal.axaml`
   - `WordListsModal.axaml`
   - `DataGridResultsWindow.axaml`
   - `WidgetPickerModal.axaml`
   - `TagEditor.axaml`
   - `StandardModal.axaml`
   - `FannedCardHand.axaml`
   - `DeckSpinner.axaml`
   - `FilterItemCarousel.axaml`
   - `JamlHelpView.axaml`
   - `StakeSpinner.axaml`

### 🟡 MEDIUM (Fix Soon)
2. **Rule 7:** Add `ConfigureAwait(false)` to async calls in ViewModels (except when UI context needed)
3. **Rule 8:** Replace `== null` / `!= null` with `is null` / `is not null` in ViewModels

### 🟢 LOW (Nice to Have)
4. **Rule 1:** Replace `Console.ReadLine()` in App.axaml.cs with DebugLogger-based prompt
5. **Rule 6:** Standardize on `InvokeAsync` vs `Post` for UI thread marshaling

---

*Enforcement: code-discipline-enforcer + csharp-avalonia-expert. Last scan: 2026-01-31. Comprehensive audit completed.*
