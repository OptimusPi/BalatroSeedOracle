# Technical Debt Report
**Generated:** 2025-01-XX
**Codebase Size:** ~35,000 lines (C# + XAML)
**Status:** Pre-Release Audit

## Executive Summary
This document catalogs technical debt found during pre-release audit. Items are prioritized by impact and organized by category for systematic remediation.

---

## Priority 1: Release Blockers (CRITICAL)

### None Identified
All critical bugs have been resolved. The application is ready for initial release.

---

## Priority 2: Post-Release Cleanup (HIGH)

### 1. Async/Await Anti-Patterns
**Impact:** Potential deadlocks, poor error handling, fire-and-forget operations
**Files Affected:** 19 files

#### Issues Found:
- **async void methods** that should be `async Task` (except UI event handlers)
- Missing `await` operators causing fire-and-forget behavior
- Potential `.Wait()` / `.Result` blocking calls

#### Affected Files:
- `Views/BalatroMainMenu.axaml.cs` - Multiple async void methods
- `ViewModels/SearchModalViewModel.cs`
- `ViewModels/DayLatroWidgetViewModel.cs`
- `Services/SearchInstance.cs`
- `Services/SoundFlowAudioManager.cs`
- And 14 more...

#### Recommendation:
- Convert `async void` → `async Task` for all non-event-handler methods
- Add proper error handling to all async operations
- Search for `.Result` and `.Wait()` - replace with `await`

---

### 2. Hardcoded Colors Not Using Balatro Resources
**Impact:** Inconsistent styling, hard to maintain theme
**Files Affected:** 33+ files

#### Issues Found:
Inline hex colors instead of using StaticResource references:
- `#8b0000` (dark red hover) - should use `{StaticResource DarkRed}`
- `#80FFFFFF` (semi-transparent white) - no resource exists
- `#660000` (pressed red) - no resource exists
- Color literals in ViewModels (code-behind)

#### Examples:
```xaml
<!-- BAD -->
<Border Background="#8b0000"/>

<!-- GOOD -->
<Border Background="{StaticResource DarkRed}"/>
```

#### Affected Files:
- `Styles/BalatroSliderStyles.axaml` - `#8b0000` hover
- `Views/Modals/FilterSelectionModal.axaml` - Multiple hardcoded colors
- `Components/Widgets/AudioVisualizerSettingsWidget.axaml`
- `Components/Widgets/FrequencyDebugWidget.axaml`
- And 29 more...

#### Recommendation:
1. Add missing color resources to `App.axaml`:
   ```xaml
   <SolidColorBrush x:Key="DarkRedHover">#8b0000</SolidColorBrush>
   <SolidColorBrush x:Key="DarkRedPressed">#660000</SolidColorBrush>
   <SolidColorBrush x:Key="SemiTransparentWhite">#80FFFFFF</SolidColorBrush>
   ```

2. Replace all hardcoded hex colors with resource references

3. Document the Balatro color palette in `COLORS.md`

---

### 3. Dead Code & Orphaned Files
**Impact:** Confusing codebase, wasted space
**Files Affected:** 3 files

#### Files to Remove:
- `src/Services/SoundFlowAudioManager.NAudio.old` - Old backup (474 lines)
- `src/obj/214fdf8d-b14f-4f20-ad06-0659d077d1d2.tmp` - Build artifact
- `src/obj/5266b9ed-7125-45fe-bf09-218bfc09a338.tmp` - Build artifact

#### Recommendation:
Delete these files immediately. Add `*.old` and `*.bak` to `.gitignore`.

---

## Priority 3: Future Improvements (MEDIUM)

### 4. Error Handling Patterns
**Impact:** Difficult debugging, inconsistent error messages
**Files Affected:** 47 files

#### Issues Found:
- Broad `catch (Exception ex)` blocks everywhere
- Should use specific exception types where possible
- Inconsistent error logging (some use DebugLogger, some don't)

#### Example:
```csharp
// CURRENT
try { ... }
catch (Exception ex)
{
    DebugLogger.LogError("Component", $"Error: {ex.Message}");
}

// BETTER
try { ... }
catch (FileNotFoundException ex)
{
    DebugLogger.LogError("Component", $"Filter file not found: {ex.FileName}", ex);
    throw; // Re-throw for caller to handle
}
catch (JsonException ex)
{
    DebugLogger.LogError("Component", $"Invalid JSON: {ex.Message}", ex);
    // Handle gracefully
}
```

#### Recommendation:
- Use specific exception types where meaningful
- Always include exception object in logs (not just `.Message`)
- Consider creating custom exception types for domain errors

---

### 5. Code Duplication

#### A. Debug Logging Boilerplate (100+ instances)
```csharp
// Repeated pattern:
DebugLogger.Log("ComponentName", "message");
DebugLogger.LogError("ComponentName", $"Error: {ex.Message}");
```

**Recommendation:** Create ILogger wrapper with component context:
```csharp
public class ComponentLogger
{
    private readonly string _componentName;
    public ComponentLogger(string componentName) => _componentName = componentName;
    public void Info(string message) => DebugLogger.Log(_componentName, message);
    public void Error(string message, Exception ex = null) => DebugLogger.LogError(_componentName, message, ex);
}
```

#### B. Filter Validation Logic
Duplicated across multiple ViewModels. Should extract to shared validator service.

#### C. ObservableCollection UI Thread Marshalling
27 files use ObservableCollection - many have duplicate Dispatcher.Invoke patterns.

---

### 6. MVVM Architecture Concerns

#### A. Memory Leaks from Event Subscriptions
**Files:** Multiple ViewModels

**Issue:** Event handlers not unsubscribed in Dispose
```csharp
public MyViewModel()
{
    SomeService.SomeEvent += OnSomeEvent;
    // No corresponding -= in Dispose!
}
```

**Recommendation:**
- Implement IDisposable on all ViewModels
- Unsubscribe from events in Dispose
- Use WeakEventManager where appropriate

#### B. Missing Property Change Notifications
Some properties modify state without raising PropertyChanged.

#### C. Business Logic in ViewModels
Some ViewModels have complex business logic that should be in services.

---

### 7. Performance Considerations

#### A. Large ObservableCollections Without Virtualization
- Search results can have thousands of items
- Currently loads everything into memory
- No UI virtualization

**Recommendation:**
- Use VirtualizingStackPanel for large lists
- Implement paging/lazy loading
- Consider SourceCache from DynamicData

#### B. LINQ in Hot Paths
Multiple `.Where().Select().ToList()` chains in frequently-called methods.

**Recommendation:**
- Profile with BenchmarkDotNet
- Consider pre-computing/caching expensive queries
- Use `for` loops in critical paths

---

### 8. Documentation Gaps

#### Missing Documentation:
- No XML docs on public APIs
- No architectural overview (MV VM, services, data flow)
- No contributor guide
- No color palette documentation
- Component interaction diagrams

#### Inconsistent Commenting:
- Some files have extensive comments
- Others have none
- Mix of styles (// vs ///)

#### Recommendation:
Create documentation structure:
```
docs/
  ├── ARCHITECTURE.md
  ├── COLORS.md
  ├── CONTRIBUTING.md
  ├── SERVICES.md
  └── VIEWMODELS.md
```

---

## By Category Breakdown

### Async/Await Issues (19 files)
| File | Issue | Severity |
|------|-------|----------|
| BalatroMainMenu.axaml.cs | async void methods | Medium |
| SearchModalViewModel.cs | Missing awaits | Medium |
| DayLatroWidgetViewModel.cs | Fire-and-forget | Low |
| ... | ... | ... |

### Hardcoded Colors (33 files)
| File | Count | Examples |
|------|-------|----------|
| FilterSelectionModal.axaml | 3 | #8b0000, #660000 |
| BalatroSliderStyles.axaml | 1 | #8b0000 |
| FrequencyDebugWidget.axaml | 5+ | Various LEDs |
| AudioVisualizerSettingsWidget.axaml | 2 | #80FFFFFF |
| ... | ... | ... |

### Error Handling (47 files)
All files use `catch (Exception ex)` - candidates for specific exception types.

### ObservableCollection Usage (27 files)
Monitor for:
- UI thread violations
- Memory leaks from event handlers
- Missing virtualization

---

## Recommended Action Plan

### Phase 1: Quick Wins (1-2 hours)
1. Delete dead files (.old, .tmp)
2. Add missing color resources to App.axaml
3. Update .gitignore

### Phase 2: Safety Improvements (1 day)
1. Fix async/await patterns
2. Implement IDisposable on ViewModels
3. Unsubscribe event handlers

### Phase 3: Code Quality (2-3 days)
1. Replace hardcoded colors with resources
2. Improve error handling specificity
3. Extract duplicate code to shared utilities

### Phase 4: Documentation (1-2 days)
1. Write architecture docs
2. Document color palette
3. Add XML docs to public APIs

### Phase 5: Performance (ongoing)
1. Profile with diagnostics tools
2. Add virtualization to large lists
3. Optimize hot paths

---

## Notes

### Won't Fix (By Design)
- **DebugLogger usage in production**: Acceptable for initial release, consider proper logging framework later
- **Some async void event handlers**: Required by UI framework
- **Consolas font fallbacks**: System fonts vary, this is expected

### Future Considerations
- Dependency injection container (currently manual construction)
- Unit tests (none exist currently)
- Integration tests
- CI/CD pipeline
- Telemetry/analytics
- Crash reporting

---

## Metrics

| Category | Count | Priority |
|----------|-------|----------|
| Async issues | 19 files | High |
| Hardcoded colors | 33+ files | High |
| Dead files | 3 files | High |
| Broad exceptions | 47 files | Medium |
| ObservableCollections | 27 files | Medium |
| Missing docs | All files | Low |

**Total Technical Debt Estimate:** 5-7 days of focused work

---

*This is a living document. Update as debt is addressed or new debt is identified.*