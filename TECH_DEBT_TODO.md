# Tech Debt TODO - Small Fixes for AI Agents

**Total Items**: 135+  
**Status**: Ready for AI agents to grab and complete  
**Priority**: Small, quick fixes that improve code quality

## ✅ Recently Completed (January 2026)

### AOT Compilation Refactoring
- [x] **AOT_001**: Removed all reflection from `JamlTypeAsKeyConverter` - replaced with static property mappings
- [x] **AOT_002**: Implemented `System.Text.Json` source generation via `MotelyJsonSerializerContext`
- [x] **AOT_003**: Implemented YamlDotNet static context via `MotelyYamlStaticContext`
- [x] **AOT_004**: Enabled AOT for Desktop builds (`PublishAot=true`)
- [x] **AOT_005**: Enabled AOT for Browser builds (`RunAOTCompilation=true`, `PublishTrimmed=true`)
- [x] **AOT_006**: Created `IExcelExporter` interface for platform abstraction
- [x] **AOT_007**: Implemented `ClosedXmlExcelExporter` for Desktop
- [x] **AOT_008**: Implemented `BrowserExcelExporter` stub for Browser
- [x] **AOT_009**: Removed DuckDB references from shared `BalatroSeedOracle` library
- [x] **AOT_010**: Updated all `Enum.GetValues()` calls to use generic `Enum.GetValues<T>()`

---

## 🔴 Critical: Logging Violations

- [ ] **TECH_DEBT_001**: Replace `Console.WriteLine(response)` in `MCP/McpServerHost.cs:54` with `DebugLogger.Log()`
- [ ] **TECH_DEBT_002**: Replace `Console.WriteLine(JsonSerializer.Serialize(errorResponse))` in `MCP/McpServerHost.cs:72` with `DebugLogger.LogError()`
- [ ] **TECH_DEBT_003**: Remove `Console.WriteLine` in `App.axaml.cs:55` - replace with `DebugLogger.Log()`
- [ ] **TECH_DEBT_004**: Remove `Debug.WriteLine` calls in `Services/Storage/BrowserLocalStorageAppDataStore.cs` (lines 19, 29, 47, 61) - use `DebugLogger` instead
- [ ] **TECH_DEBT_005**: Check `Helpers/DebugLogger.cs` for any `Console.WriteLine` that should be conditional on debug mode

---

## 🟠 Code Style: Null Checks

- [ ] **TECH_DEBT_006**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:315` (`_originalCriteriaHash == null`)
- [ ] **TECH_DEBT_007**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:399` (`searchManager != null`)
- [ ] **TECH_DEBT_008**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:493` (`dbFiles == null`)
- [ ] **TECH_DEBT_009**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:679` (`LoadedConfig != null`)
- [ ] **TECH_DEBT_010**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:718` (`LoadedConfig != null`)
- [ ] **TECH_DEBT_011**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:739` (`config.Must != null`)
- [ ] **TECH_DEBT_012**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:753` (`config.Should != null`)
- [ ] **TECH_DEBT_013**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:767` (`config.MustNot != null`)
- [ ] **TECH_DEBT_014**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:805` (`config != null`)
- [ ] **TECH_DEBT_015**: Replace `== null` with `is null` in `ViewModels/FiltersModalViewModel.cs:1013` (`config == null`)
- [ ] **TECH_DEBT_016**: Replace all `== null` / `!= null` checks in `ViewModels/FiltersModalViewModel.cs` lines 1123-1801 (scan entire file)
- [ ] **TECH_DEBT_017**: Scan all ViewModels for `== null` / `!= null` and replace with `is null` / `is not null`

---

## 🟡 Async/Await Issues

- [ ] **TECH_DEBT_018**: Check `ViewModels/FiltersModalViewModel.cs:289` `SaveCurrentFilter()` - verify it properly awaits all async calls
- [ ] **TECH_DEBT_019**: Check `ViewModels/FiltersModalViewModel.cs:381` `CleanupFilterDatabases()` - verify async pattern
- [ ] **TECH_DEBT_020**: Check `ViewModels/FiltersModalViewModel.cs:486` `DumpDatabasesToFertilizerAsync()` - verify async pattern
- [ ] **TECH_DEBT_021**: Check `ViewModels/FiltersModalViewModel.cs:573` `LoadFilter()` - verify async pattern
- [ ] **TECH_DEBT_022**: Check `ViewModels/FiltersModalViewModel.cs:687` `DeleteFilter()` - verify async pattern
- [ ] **TECH_DEBT_023**: Check `ViewModels/FiltersModalViewModel.cs:783` `ReloadVisualFromSavedFile()` - verify async pattern
- [ ] **TECH_DEBT_024**: Check `ViewModels/FiltersModalViewModel.cs:892` `RefreshSaveTabData()` - verify async pattern
- [ ] **TECH_DEBT_025**: Check `ViewModels/FiltersModalViewModel.cs:2108` `UpdateVisualBuilderFromItemConfigs()` - verify async pattern
- [ ] **TECH_DEBT_026**: Check `ViewModels/AnalyzerViewModel.cs:244` `AnalyzeCurrentSeedAsync()` - verify async pattern
- [ ] **TECH_DEBT_027**: Check all async methods in `ViewModels/FilterTabs/ValidateFilterTabViewModel.cs` - verify they properly await
- [ ] **TECH_DEBT_028**: Check all async methods in `ViewModels/FilterTabs/SaveFilterTabViewModel.cs` - verify they properly await

---

## 🟢 MVVM Violations: Code-Behind Logic

- [ ] **TECH_DEBT_029**: Move business logic from `Views/MainWindow.axaml.cs:57-64` `OnWindowClosing()` to `MainWindowViewModel`
- [ ] **TECH_DEBT_030**: Move business logic from `Views/MainWindow.axaml.cs:54` `OnWindowSizeChanged()` to `MainWindowViewModel`
- [ ] **TECH_DEBT_031**: Review `Views/BalatroMainMenu.axaml.cs` for any business logic that should be in ViewModel
- [ ] **TECH_DEBT_032**: Review all `.axaml.cs` files in `Views/` for business logic that belongs in ViewModels
- [ ] **TECH_DEBT_033**: Review all `.axaml.cs` files in `Components/` for business logic that belongs in ViewModels
- [ ] **TECH_DEBT_034**: Check `Components/FilterTabs/JamlEditorTab.axaml.cs` - ensure all logic is in `JamlEditorTabViewModel`
- [ ] **TECH_DEBT_035**: Check `Controls/SortableResultsGrid.axaml.cs` - ensure all logic is in `SortableResultsGridViewModel`

---

## 🔵 Bad Comments / AI Slop

- [ ] **TECH_DEBT_036**: Remove or improve generic XML comments like `/// <summary>` with no actual description
- [ ] **TECH_DEBT_037**: Remove redundant comments that just repeat the code (e.g., `// Set the value` above `value = 5`)
- [ ] **TECH_DEBT_038**: Remove "AI-generated" style comments that are obvious (e.g., `// Initialize the variable`)
- [ ] **TECH_DEBT_039**: Review `ViewModels/FiltersModalViewModel.cs` for bad XML comments - improve or remove
- [ ] **TECH_DEBT_040**: Review `ViewModels/FilterTabs/SaveFilterTabViewModel.cs` for bad XML comments - improve or remove
- [ ] **TECH_DEBT_041**: Remove comments like `// BUG FIX:` - if it's fixed, the comment is unnecessary
- [ ] **TECH_DEBT_042**: Remove comments like `// TODO:` that are outdated or already implemented
- [ ] **TECH_DEBT_043**: Clean up comments in `Services/Storage/BrowserLocalStorageAppDataStore.cs` - remove obvious ones

---

## 🟣 Error Handling

- [ ] **TECH_DEBT_044**: Add proper error handling to `ViewModels/FiltersModalViewModel.cs:289` `SaveCurrentFilter()` if missing
- [ ] **TECH_DEBT_045**: Add proper error handling to `ViewModels/FiltersModalViewModel.cs:381` `CleanupFilterDatabases()` if missing
- [ ] **TECH_DEBT_046**: Check all async methods for missing try-catch blocks
- [ ] **TECH_DEBT_047**: Ensure all file I/O operations have proper error handling
- [ ] **TECH_DEBT_048**: Ensure all service calls have proper error handling
- [ ] **TECH_DEBT_049**: Check for empty catch blocks that should log errors

---

## 🟤 Naming Conventions

- [ ] **TECH_DEBT_050**: Check for methods that don't follow naming conventions (e.g., `DoSomething()` should be `DoSomethingAsync()` if async)
- [ ] **TECH_DEBT_051**: Check for private fields that don't use `_camelCase` convention
- [ ] **TECH_DEBT_052**: Check for properties that don't use `PascalCase` convention
- [ ] **TECH_DEBT_053**: Check for local variables that use inconsistent naming

---

## ⚪ Code Duplication

- [ ] **TECH_DEBT_054**: Find duplicate error handling patterns and extract to helper method
- [ ] **TECH_DEBT_055**: Find duplicate null check patterns and extract to helper method
- [ ] **TECH_DEBT_056**: Find duplicate logging patterns and standardize
- [ ] **TECH_DEBT_057**: Check `ViewModels/FiltersModalViewModel.cs` for duplicate code blocks
- [ ] **TECH_DEBT_058**: Check `ViewModels/FilterTabs/` for duplicate code blocks

---

## 🔴 Unused Code

- [ ] **TECH_DEBT_059**: Remove unused `using` statements across all files
- [ ] **TECH_DEBT_060**: Remove unused private fields
- [ ] **TECH_DEBT_061**: Remove unused private methods
- [ ] **TECH_DEBT_062**: Remove commented-out code blocks
- [ ] **TECH_DEBT_063**: Remove dead code paths

---

## 🟠 Magic Numbers / Strings

- [ ] **TECH_DEBT_064**: Replace magic numbers with named constants (e.g., `500` → `VALIDATION_DELAY_MS`)
- [ ] **TECH_DEBT_065**: Replace magic strings with constants (e.g., `"Filters"` → `MODAL_TYPE_FILTERS`)
- [ ] **TECH_DEBT_066**: Check `ViewModels/FiltersModalViewModel.cs` for magic numbers/strings
- [ ] **TECH_DEBT_067**: Check `ViewModels/FilterTabs/` for magic numbers/strings

---

## 🟡 Inconsistent Patterns

- [ ] **TECH_DEBT_068**: Standardize error logging format across all ViewModels
- [ ] **TECH_DEBT_069**: Standardize success logging format across all ViewModels
- [ ] **TECH_DEBT_070**: Standardize async method patterns (all should use `ConfigureAwait(false)` for library code)
- [ ] **TECH_DEBT_071**: Standardize command patterns (all should use `[RelayCommand]` consistently)
- [ ] **TECH_DEBT_072**: Standardize property change notification patterns

---

## 🟢 Missing Documentation

- [ ] **TECH_DEBT_073**: Add XML comments to public methods in `Services/FilterService.cs`
- [ ] **TECH_DEBT_074**: Add XML comments to public methods in `Services/SearchInstance.cs`
- [ ] **TECH_DEBT_075**: Add XML comments to public properties in ViewModels
- [ ] **TECH_DEBT_076**: Add XML comments to complex business logic methods

---

## 🔵 Performance Issues

- [ ] **TECH_DEBT_077**: Check for LINQ queries that could be optimized (e.g., `.ToList()` when not needed)
- [ ] **TECH_DEBT_078**: Check for string concatenation in loops (use `StringBuilder`)
- [ ] **TECH_DEBT_079**: Check for unnecessary object allocations in hot paths
- [ ] **TECH_DEBT_080**: Check for missing `ConfigureAwait(false)` in library code

---

## 🟣 Type Safety

- [ ] **TECH_DEBT_081**: Replace `var` with explicit types where it improves readability
- [ ] **TECH_DEBT_082**: Check for unnecessary type casts
- [ ] **TECH_DEBT_083**: Check for `as` casts that should use pattern matching
- [ ] **TECH_DEBT_084**: Check for nullable reference type warnings

---

## 🟤 Platform-Specific Code

- [ ] **TECH_DEBT_085**: Ensure all `#if BROWSER` blocks have corresponding `#else` blocks
- [ ] **TECH_DEBT_086**: Check for platform-specific code that's not properly guarded
- [ ] **TECH_DEBT_087**: Verify browser-specific code doesn't break desktop builds

---

## ⚪ Resource Management

- [ ] **TECH_DEBT_088**: Check for missing `IDisposable` implementations
- [ ] **TECH_DEBT_089**: Check for missing `using` statements for `IDisposable` objects
- [ ] **TECH_DEBT_090**: Check for event handlers that aren't unsubscribed
- [ ] **TECH_DEBT_091**: Check for memory leaks in long-running operations

---

## 🔴 Validation

- [ ] **TECH_DEBT_092**: Add input validation to all public methods
- [ ] **TECH_DEBT_093**: Add null checks for all constructor parameters
- [ ] **TECH_DEBT_094**: Add validation for file paths before I/O operations
- [ ] **TECH_DEBT_095**: Add validation for user input in ViewModels

---

## 🟠 Testing

- [ ] **TECH_DEBT_096**: Add unit tests for `Helpers/DebugLogger.cs`
- [ ] **TECH_DEBT_097**: Add unit tests for `Services/FilterService.cs`
- [ ] **TECH_DEBT_098**: Add unit tests for ViewModel commands
- [ ] **TECH_DEBT_099**: Add integration tests for filter loading/saving

---

## 🟡 Miscellaneous

- [ ] **TECH_DEBT_100**: Remove all `// TODO:` comments that are outdated
- [ ] **TECH_DEBT_101**: Remove all `// FIXME:` comments that are fixed
- [ ] **TECH_DEBT_102**: Remove all `// HACK:` comments - either fix or document why it's needed
- [ ] **TECH_DEBT_103**: Standardize file headers (copyright, license, etc.)
- [ ] **TECH_DEBT_104**: Ensure all files have consistent line endings (LF)
- [ ] **TECH_DEBT_105**: Remove trailing whitespace from all files
- [ ] **TECH_DEBT_106**: Ensure consistent indentation (spaces, not tabs)
- [ ] **TECH_DEBT_107**: Remove unused project references
- [ ] **TECH_DEBT_108**: Update outdated package versions
- [ ] **TECH_DEBT_109**: Remove duplicate resource definitions
- [ ] **TECH_DEBT_110**: Ensure all XAML files use consistent naming conventions

---

## 🟢 Code Quality

- [ ] **TECH_DEBT_111**: Reduce cyclomatic complexity in methods over 20 lines
- [ ] **TECH_DEBT_112**: Break down methods over 50 lines into smaller methods
- [ ] **TECH_DEBT_113**: Extract magic numbers to constants
- [ ] **TECH_DEBT_114**: Extract repeated patterns to helper methods
- [ ] **TECH_DEBT_115**: Improve variable names for clarity

---

## 🔵 Accessibility

- [ ] **TECH_DEBT_116**: Add proper ARIA labels to interactive elements
- [ ] **TECH_DEBT_117**: Ensure keyboard navigation works for all controls
- [ ] **TECH_DEBT_118**: Add tooltips to icon-only buttons
- [ ] **TECH_DEBT_119**: Ensure color contrast meets WCAG standards

---

## 🟣 Localization

- [ ] **TECH_DEBT_120**: Extract hardcoded strings to resource files
- [ ] **TECH_DEBT_121**: Add localization support for error messages
- [ ] **TECH_DEBT_122**: Add localization support for UI text

---

## 🟤 Documentation

- [ ] **TECH_DEBT_123**: Update README.md with latest features
- [ ] **TECH_DEBT_124**: Add code examples to complex methods
- [ ] **TECH_DEBT_125**: Document complex algorithms
- [ ] **TECH_DEBT_126**: Add architecture diagrams

---

## ⚪ Security

- [ ] **TECH_DEBT_127**: Review file path handling for path traversal vulnerabilities
- [ ] **TECH_DEBT_128**: Review user input validation for injection attacks
- [ ] **TECH_DEBT_129**: Ensure sensitive data isn't logged
- [ ] **TECH_DEBT_130**: Review authentication/authorization if applicable

---

## 🔴 Final Polish

- [ ] **TECH_DEBT_131**: Run code formatter on entire solution
- [ ] **TECH_DEBT_132**: Fix all compiler warnings
- [ ] **TECH_DEBT_133**: Fix all linter errors
- [ ] **TECH_DEBT_134**: Run static analysis tools
- [ ] **TECH_DEBT_135**: Review code coverage report

---

## 📝 How to Use This List

1. **Pick an item** - Any AI agent can grab any item
2. **Complete it** - Make the fix
3. **Mark it done** - Check the box `[x]`
4. **Commit** - Small, focused commits are best
5. **Move on** - Grab the next item

### Priority Order (Suggested)
1. Critical logging violations (001-005)
2. Code style null checks (006-017)
3. Async/await issues (018-028)
4. MVVM violations (029-035)
5. Bad comments (036-043)
6. Then work through the rest

### Notes
- Each item is **small and actionable**
- Most items take **5-15 minutes** to complete
- Items are **independent** - can be done in any order
- **Test after each fix** - don't break existing functionality

---

**Last Updated**: 2025-01-XX  
**Total Items**: 135+  
**Status**: Ready for AI agents 🚀
