# TECHNICAL DEBT ANALYSIS
**Project:** Balatro Seed Oracle
**Date:** 2025-10-12
**Analyzer:** Deep Codebase Analysis

---

## SUMMARY

**Total Technical Debt Estimate:** 6-8 weeks of focused work
**Critical Items:** 1 (FiltersModal god class)
**High Priority Items:** 5
**Medium Priority Items:** 12
**Low Priority Items:** 8

---

## DEBT CATEGORIES

### 1. ARCHITECTURAL DEBT

#### 🔴 CRITICAL: FiltersModal God Class
**File:** `src/Views/Modals/FiltersModal.axaml.cs`
**Size:** 8,975 lines
**Responsibilities:** 10+ (should be 1)
**FindControl Calls:** 210+

**Cost of Delay:**
- Makes testing impossible (no unit tests for 80% of filter logic)
- Every bug fix touches this file (merge conflicts)
- New developers can't understand it (onboarding pain)
- Refactoring other modals blocked (this sets bad example)

**Repayment Plan:**
```
Week 1: Wire up existing FiltersModalViewModel
Week 2: Move business logic to ViewModel
Week 3: Extract services (Persistence, Validation, Items)
Week 4: Clean up code-behind, add tests
```

**Estimated Effort:** 2-3 weeks
**Interest Rate:** 2 hours/week (ongoing bug fixes in this file)

---

#### 🟡 MEDIUM: Missing ViewModels
**Files:** 5 modals without ViewModels
**Total Lines:** 1,005 lines of business logic in code-behind

**Cost:**
- Can't unit test modal logic
- Hard to refactor UI without breaking logic
- Inconsistent patterns across codebase

**Repayment:**
- 1 day per modal × 5 modals = 5 days
- AuthorModal (2h), StandardModal (4h), FilterCreationModal (1d), WordListsModal (1d), ToolsModal (1d)

---

#### 🟡 MEDIUM: Visual Tree Walking for Communication
**Occurrences:** 6+ places
**Pattern:** Widgets walk up visual tree to find parent modal

**Example:**
```csharp
// DayLatroWidget.axaml.cs line 78-85
var parent = this.Parent;
BalatroMainMenu? mainMenu = null;
while (parent != null && mainMenu == null)
{
    if (parent is BalatroMainMenu mm)
        mainMenu = mm;
    parent = (parent as Control)?.Parent;
}
```

**Cost:**
- Tight coupling to UI hierarchy
- Hard to test (requires full visual tree)
- Brittle (breaks if UI structure changes)

**Solution:** Implement IModalService
**Effort:** 2 days
**Benefit:** Testable, decoupled, cleaner

---

### 2. CODE DUPLICATION DEBT

#### 🟡 Widget Implementation Duplication
**Files:** DayLatroWidget.axaml, GenieWidget.axaml, AudioVisualizerSettingsWidget.axaml
**Duplicated Lines:** ~200 lines (minimize/expand UI)

**Cost:**
- Bug fixes must be applied 3 times
- Style changes need 3 updates
- New widgets copy/paste same code

**Solution:** WidgetStyles.axaml (user already started this!)
**Effort:** 2 days
**Lines Saved:** 200

---

#### 🟢 Tab Button Styles
**Status:** PARTIALLY RESOLVED
**Solution:** BalatroTabControl component exists and works
**Remaining:** FiltersModal has custom tab styles (could use BalatroTabControl)

---

### 3. MAINTAINABILITY DEBT

#### 🟡 FindControl Overuse
**Total Calls:** 250+
**Worst Offender:** FiltersModal (210 calls)

**Pattern:**
```csharp
// Called in loops, event handlers, everywhere
var control = this.FindControl<SomeControl>("ControlName");
if (control != null) {
    control.Property = value; // Should be data binding!
}
```

**Cost:**
- Performance impact (FindControl is slow)
- Null reference exceptions
- Hard to refactor XAML (breaks code)

**Solution:** Data binding!
```csharp
// ViewModel
[ObservableProperty] private bool _isControlVisible;

// XAML
IsVisible="{Binding IsControlVisible}"
```

**Effort:** Part of FiltersModal refactoring
**Benefit:** Faster, safer, more maintainable

---

#### 🟡 Documentation Sprawl
**Files:** 20+ .md files in project root
**Types:** Session notes, analysis docs, refactoring plans, status files

**Cost:**
- Hard to find relevant documentation
- Outdated information (5 files named "FINAL" or "COMPLETE")
- Cluttered git history

**Solution:**
```bash
mkdir docs
mv *ANALYSIS*.md SESSION*.md REFACTORING*.md STATUS*.md docs/
# Keep: README.md, CLAUDE.md, COMPREHENSIVE_TODO_LIST.md
```

**Effort:** 30 minutes
**Benefit:** Clean repo, easier navigation

---

### 4. TESTING DEBT

#### 🔴 CRITICAL: Zero Unit Tests
**Test Project:** Doesn't exist
**Coverage:** 0%

**Cost:**
- Every refactoring is risky
- Regressions are common
- Manual testing takes hours
- Confidence in changes: LOW

**Solution:** Create test project
```bash
dotnet new xunit -n BalatroSeedOracle.Tests
# Focus on ViewModels first (easiest to test)
```

**Effort:** 1 week to get to 50% ViewModel coverage
**Benefit:** Huge safety net for refactoring

---

### 5. PERFORMANCE DEBT

#### 🟢 Filter List Loading
**Current:** Loads ALL filters on startup
**Cost:** Slow startup if 100+ filters

**Solution:** Lazy-load
```csharp
// Load metadata only
var filters = Directory.GetFiles(dir).Select(f => new FilterMetadata(f));

// Load full filter on selection
var fullFilter = await LoadFilterAsync(selectedPath);
```

**Effort:** 4 hours
**Benefit:** Faster startup

---

#### 🟢 No Validation Caching
**Current:** Re-validates filter on every change
**Cost:** CPU cycles on unchanged filters

**Solution:**
```csharp
class FilterValidationCache
{
    private Dictionary<string, (string hash, ValidationResult result)> _cache;

    public ValidationResult GetOrValidate(MotelyJsonConfig config)
    {
        var hash = ComputeHash(config);
        if (_cache.TryGetValue(hash, out var cached))
            return cached.result;

        var result = Validate(config);
        _cache[hash] = (hash, result);
        return result;
    }
}
```

**Effort:** 2 hours
**Benefit:** Faster validation

---

## DEBT ACCUMULATION RATE

### How Fast is Debt Growing?

**Low Accumulation Areas (Good!):**
- Widget system - stable, not adding new widgets often
- SearchModal - well-architected, not changing much
- Services - properly extracted, well-tested by usage

**High Accumulation Areas (Watch Out!):**
- FiltersModal - every feature added here makes it worse
- Modals without ViewModels - each new modal might copy bad patterns
- Visual tree walking - new widgets might copy this pattern

**Recommendation:**
- Stop adding features to FiltersModal until refactored
- Enforce "every new modal needs a ViewModel" rule
- Code review to catch MVVM violations

---

## DEBT REPAYMENT STRATEGY

### Phase 1: Stop the Bleeding (Week 1)
**Goal:** Stop accumulating debt

Actions:
1. ✅ Fix critical bugs (DONE!)
2. [ ] Create ViewModel template for new modals
3. [ ] Add MVVM checklist to CLAUDE.md
4. [ ] Establish code review process

---

### Phase 2: Quick Wins (Week 2-3)
**Goal:** Build momentum with easy victories

Actions:
1. [ ] Clean up documentation files
2. [ ] Wire up AnalyzeModalViewModel
3. [ ] Create 5 missing ViewModels
4. [ ] Widget position persistence
5. [ ] Consolidate widget styles

**Effort:** 2 weeks
**Benefit:** Morale boost, visible progress, 80→90% MVVM compliance

---

### Phase 3: The Big One (Week 4-7)
**Goal:** Tackle FiltersModal

**Milestone-Based Approach:**
- Milestone 1: ViewModel wired up, old code still works (Week 4)
- Milestone 2: 50% of methods moved to ViewModel (Week 5)
- Milestone 3: Services extracted, 90% moved (Week 6)
- Milestone 4: Code-behind < 200 lines, tests passing (Week 7)

**Exit Criteria:**
- All business logic in ViewModel
- Code-behind < 200 lines
- Unit tests for core functionality
- No regressions in manual testing

---

### Phase 4: Prevent Recurrence (Week 8)
**Goal:** Make sure debt doesn't come back

Actions:
1. [ ] Add unit tests (prevent regressions)
2. [ ] Create architecture guidelines
3. [ ] Set up pre-commit hooks (warn on FindControl usage)
4. [ ] Document MVVM patterns in CLAUDE.md

---

## COST-BENEFIT ANALYSIS

### FiltersModal Refactoring

**Costs:**
- 2-3 weeks of development time
- High risk of breaking existing functionality
- Need extensive testing
- May introduce new bugs short-term

**Benefits:**
- Can add unit tests (prevent regressions)
- Faster feature development (clear separation)
- Easier onboarding (newcomers can understand code)
- Better performance (eliminate 210 FindControl calls)
- Can implement undo/redo (requires clean state management)

**ROI:** High, but delayed (payoff comes over 6+ months)

---

### Creating Missing ViewModels

**Costs:**
- 4-5 days of development

**Benefits:**
- 100% ViewModel coverage
- Can unit test all modals
- Consistent patterns across codebase
- Easier to refactor UI

**ROI:** Immediate (payoff starts day 1)

---

## RISK ASSESSMENT

### What Could Go Wrong?

1. **FiltersModal Refactoring Breaks Everything**
   - Likelihood: MEDIUM-HIGH
   - Impact: CRITICAL
   - Mitigation: Feature flag, parallel implementation, extensive testing

2. **Widget Consolidation Breaks Drag/Drop**
   - Likelihood: LOW
   - Impact: HIGH
   - Mitigation: Start with one widget, test thoroughly

3. **Creating ViewModels Introduces Bugs**
   - Likelihood: LOW
   - Impact: MEDIUM
   - Mitigation: Each modal independent, easy rollback

4. **Time Overruns**
   - Likelihood: MEDIUM
   - Impact: MEDIUM
   - Mitigation: Prioritize, deliver incrementally, can pause

---

## TECHNICAL DEBT vs NEW FEATURES

**Trade-off:**
- Paying down debt = fewer bugs, faster future development
- New features = user value, revenue

**Recommended Balance:**
- 70% features, 30% debt repayment
- Never let debt block critical features
- Address debt that slows feature development

**Current Situation:**
- FiltersModal debt IS blocking features (can't add undo/redo, can't test)
- Other debt isn't blocking anything

**Recommendation:**
- Fix FiltersModal (unblocks features)
- Other debt can wait

---

## MONITORING METRICS

### Track These Over Time:

1. **MVVM Compliance %**
   - Current: 72%
   - Target: 95%
   - Measure: (Lines in ViewModels) / (Lines in ViewModels + Lines in Code-Behind business logic)

2. **FindControl Density**
   - Current: 250 calls / 45,000 lines = 0.56%
   - Target: < 0.1% (< 50 calls)

3. **Average File Size**
   - Current: FiltersModal skews average to 1,200 lines
   - Target: < 300 lines average

4. **Test Coverage**
   - Current: 0%
   - Target: 70% of ViewModel code

5. **Build Time**
   - Current: ~5 seconds
   - Watch for increases (indicates complexity growth)

---

## CONCLUSION

**The debt is manageable** if addressed systematically. The FiltersModal is the main problem, representing 77% of all violations.

**Priority:**
1. ✅ Fix bugs (DONE!)
2. Quick wins (ViewModels, docs) - 2 weeks
3. FiltersModal (careful refactoring) - 3 weeks
4. Ongoing improvements

**The app works!** Don't sacrifice stability for perfect architecture. Incremental improvements over time.
