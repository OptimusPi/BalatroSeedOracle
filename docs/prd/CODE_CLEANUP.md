# CODE CLEANUP - Remove Dead Code, TODOs, and Clutter

**Status:** 🟢 LOW PRIORITY
**Priority:** P3 - Nice to Have
**Estimated Time:** 2-3 hours
**Generated:** 2025-11-03

---

## Overview

Clean up codebase by removing:
- Commented-out code (leftovers from refactoring)
- Dead code and unused files
- TODO comments indicating unfinished work
- Removed feature references (VibeOut mode)
- Debugging artifacts left in production

---

## Issue #1: Commented-Out Event Handlers in Widgets

### Problem
**Files:** Multiple widget code-behinds
**Severity:** LOW

Commented-out event handler registrations clutter code:

```csharp
// ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead
// this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead
```

**Affected Files:**
- `src/Components/Widgets/AudioVisualizerSettingsWidget.axaml.cs:37, 40, 42`
- `src/Components/Widgets/DayLatroWidget.axaml.cs:40, 43`
- `src/Components/Widgets/GenieWidget.axaml.cs:27, 30`
- `src/Components/Widgets/MusicMixerWidget.axaml.cs:33, 36`
- Other widget files

### Impact
- Code clutter makes file harder to read
- Confusing for new developers
- No functional impact (already commented out)

### Acceptance Criteria
- [ ] Remove ALL commented-out code blocks from widget files
- [ ] Keep ONLY explanatory comments (not commented code)
- [ ] If context is important, convert to documentation comment

### Implementation

```csharp
// BEFORE:
public AudioVisualizerSettingsWidget()
{
    InitializeComponent();
    // ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead
    // this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead
}

// AFTER:
public AudioVisualizerSettingsWidget()
{
    InitializeComponent();
    // Widget state management is handled via XAML bindings
}
```

**Search command:**
```bash
grep -rn "// REMOVED" src/Components/Widgets/ --include="*.cs"
```

---

## Issue #2: VibeOut Mode Removal Leftovers

### Problem
**Files:** Multiple
**Severity:** LOW

VibeOut feature was removed but comments scattered throughout codebase:

**Affected Files:**
- `src/Views/BalatroMainMenu.axaml.cs:36, 80, 1158`
- `src/Controls/BalatroShaderBackground.cs:29`
- `src/Helpers/ModalHelper.cs:417`

```csharp
// Comments like:
// REMOVED: VibeOut mode - was causing performance issues
// VibeOut widget removed in favor of Music Mixer
```

### Impact
- Confuses developers who don't know what VibeOut was
- Code clutter
- No functional impact

### Acceptance Criteria
- [ ] Remove ALL references to VibeOut mode
- [ ] If context is important, add to CHANGELOG.md
- [ ] Clean up related commented-out code

### Implementation

**Search for all VibeOut references:**
```bash
grep -rni "vibeout\|vibe out" src/ --include="*.cs"
```

**Remove or update comments:**
```csharp
// BEFORE:
// REMOVED: VibeOut mode - was causing performance issues

// AFTER:
(Delete the comment entirely)
```

**If context is important, add to CHANGELOG.md:**
```markdown
## [Removed] - 2025-10
- VibeOut mode - Removed in favor of Music Mixer widget (better performance)
```

---

## Issue #3: REMOVED Method Reference Comments

### Problem
**File:** `src/Views/BalatroMainMenu.axaml.cs:1733-1738`
**Severity:** LOW

Method removal notices clutter code:

```csharp
// REMOVED: GetFilterName, CloneFilterWithName, DeleteFilter
// These methods have been moved to FilterService for proper MVVM separation
// Use IFilterService.GetFilterNameAsync(), IFilterService.CloneFilterAsync(), IFilterService.DeleteFilterAsync()
```

### Impact
- Helpful for migration but adds clutter
- Should be in migration guide instead
- No functional impact

### Acceptance Criteria
- [ ] Remove method removal notices
- [ ] Create MIGRATION_GUIDE.md if needed
- [ ] Keep only essential migration info in code comments

### Implementation

```csharp
// BEFORE:
// REMOVED: GetFilterName, CloneFilterWithName, DeleteFilter
// These methods have been moved to FilterService for proper MVVM separation
// Use IFilterService.GetFilterNameAsync(), IFilterService.CloneFilterAsync(), IFilterService.DeleteFilterAsync()

// AFTER:
(Delete - add to MIGRATION_GUIDE.md if needed)
```

**Create docs/MIGRATION_GUIDE.md:**
```markdown
# Migration Guide

## Filter Methods Moved to FilterService

**When:** 2025-10
**Why:** MVVM separation of concerns

**Old code:**
```csharp
var name = await GetFilterName(filterPath);
```

**New code:**
```csharp
var filterService = ServiceHelper.GetService<IFilterService>();
var name = await filterService.GetFilterNameAsync(filterPath);
```

---

## Issue #4: TODO Comments - Unfinished Work

### Problem
**Multiple Files**
**Severity:** MEDIUM

TODO comments indicate unfinished features:

```csharp
// TODO: Implement track volume control when audio manager supports it
// src/Views/BalatroMainMenu.axaml.cs:1374

// TODO: Move to FiltersModalViewModel.SaveFilter - only invalidate when MUST/SHOULD/MUSTNOT changes during SAVE
// src/Services/SearchInstance.cs:705

// TODO: Type B2 (Mult/Bonus) needs third layer with glyph overlay
// src/Services/SpriteService.cs:1370

// TODO: Implement JSON export for all shader parameters
// src/ViewModels/AudioVisualizerSettingsWidgetViewModel.cs:1427
```

### Impact
- Features users might expect are incomplete
- Indicates tech debt accumulation
- Should be tracked in issue tracker, not code comments

### Acceptance Criteria
- [ ] Find all TODO comments
- [ ] For each TODO:
  - Create GitHub issue if work is needed
  - Remove TODO if no longer relevant
  - Implement if trivial (<15 min)
- [ ] Update code comments to reference issues

### Implementation

**Find all TODOs:**
```bash
grep -rn "TODO\|FIXME\|HACK\|XXX" src/ --include="*.cs" > todos.txt
```

**For each TODO:**

**Option 1 - Create issue:**
```csharp
// BEFORE:
// TODO: Implement track volume control when audio manager supports it

// AFTER:
// Track volume control pending - see issue #123
```

**Option 2 - Remove if obsolete:**
```csharp
// BEFORE:
// TODO: Add null check (done in previous refactor)

// AFTER:
(Delete comment)
```

**Option 3 - Implement if trivial:**
```csharp
// BEFORE:
// TODO: Add validation

// AFTER:
if (string.IsNullOrEmpty(input))
{
    throw new ArgumentException("Input cannot be empty");
}
```

---

## Issue #5: Development/Debug Artifacts

### Problem
**File:** `src/ViewModels/SearchModalViewModel.cs:909`
**Severity:** LOW

Development comments left in production:

```csharp
// INTENTIONALLY REMOVED: Batch size conversion logic
```

### Impact
- Confusing for maintainers
- Code clutter
- No functional impact

### Acceptance Criteria
- [ ] Remove development artifacts
- [ ] Remove "INTENTIONALLY REMOVED" comments (code is already removed!)
- [ ] Keep only production-relevant comments

### Implementation

```csharp
// BEFORE:
// INTENTIONALLY REMOVED: Batch size conversion logic

// AFTER:
(Delete - code is already removed, comment adds no value)
```

---

## Issue #6: Empty Regions and Spacing Inconsistencies

### Problem
**Multiple Files**
**Severity:** LOW

Inconsistent use of #region blocks, some empty:

```csharp
#region Helper Methods
// ... only 1 method
#endregion

#region Private Fields
// ... mixed with public properties
#endregion
```

### Impact
- Makes code harder to navigate
- Regions used inconsistently

### Acceptance Criteria
- [ ] Remove #region blocks with <3 members
- [ ] Remove empty #region blocks
- [ ] Use regions consistently OR remove entirely
- [ ] Standardize on one approach per file type

### Implementation

**Option 1 - Keep regions for large classes:**
```csharp
// Keep regions in files >500 lines
// Remove regions in files <500 lines
```

**Option 2 - Remove all regions:**
```csharp
// Rely on IDE outlining instead of #region
```

**Recommended: Option 1**

---

## Implementation Plan

### Phase 1: Quick Wins (30 minutes)
1. Remove commented-out event handlers from widgets
2. Remove VibeOut references
3. Remove "REMOVED" method notices
4. Remove "INTENTIONALLY REMOVED" comments

### Phase 2: TODO Audit (1 hour)
1. Find all TODO/FIXME/HACK comments
2. Create GitHub issues for real work
3. Remove obsolete TODOs
4. Implement trivial TODOs

### Phase 3: Region Cleanup (30 minutes)
1. Remove regions from small files (<500 lines)
2. Standardize region usage in large files
3. Remove empty regions

### Phase 4: Documentation (30 minutes)
1. Create MIGRATION_GUIDE.md if needed
2. Update CHANGELOG.md with removed features
3. Add cleanup notes to README

---

## Automated Cleanup Script

Create `scripts/cleanup.sh`:

```bash
#!/bin/bash

echo "=== Code Cleanup Script ==="

# Find commented-out event handlers
echo "Finding commented-out code..."
grep -rn "// .*\..*+=.*// REMOVED" src/ --include="*.cs"

# Find VibeOut references
echo "Finding VibeOut references..."
grep -rni "vibeout\|vibe out" src/ --include="*.cs"

# Find TODOs
echo "Finding TODOs..."
grep -rn "TODO\|FIXME\|HACK\|XXX" src/ --include="*.cs"

# Find empty regions
echo "Finding regions with <3 members..."
# (Would need more complex parsing)

echo "=== Review output and remove manually ==="
```

---

## Files Requiring Changes

### Widgets (remove commented code)
- `src/Components/Widgets/AudioVisualizerSettingsWidget.axaml.cs`
- `src/Components/Widgets/DayLatroWidget.axaml.cs`
- `src/Components/Widgets/GenieWidget.axaml.cs`
- `src/Components/Widgets/MusicMixerWidget.axaml.cs`
- `src/Components/Widgets/FrequencyDebugWidget.axaml.cs`

### VibeOut cleanup
- `src/Views/BalatroMainMenu.axaml.cs`
- `src/Controls/BalatroShaderBackground.cs`
- `src/Helpers/ModalHelper.cs`

### TODO cleanup
- `src/Views/BalatroMainMenu.axaml.cs`
- `src/Services/SearchInstance.cs`
- `src/Services/SpriteService.cs`
- `src/ViewModels/AudioVisualizerSettingsWidgetViewModel.cs`

### New files to create
- `docs/MIGRATION_GUIDE.md` (if needed)
- `scripts/cleanup.sh` (automation)

---

## Test Plan

### Verification Tests
1. Build project → verify 0 errors, 0 warnings
2. Run all existing tests → verify 0 failures
3. Manual smoke test → verify all features work
4. Code review → verify no important context lost

### Cleanup Verification
1. Search for "REMOVED" → should find 0 results
2. Search for "TODO" → should find <5 results (tracked issues)
3. Search for "VibeOut" → should find 0 results
4. Search for commented-out code → should find minimal results

---

## Success Metrics

- ✅ Zero commented-out event handlers
- ✅ Zero VibeOut references
- ✅ All TODOs either implemented or tracked as issues
- ✅ <5 untracked TODOs in codebase
- ✅ Code comment count reduced by 20%
- ✅ All code comments add value (no clutter)

---

## Risks

### Risk: Removing Important Context
**Mitigation:** Before deleting any comment, verify it doesn't contain:
- Important business logic explanation
- Reason for a workaround
- Reference to external issue/bug

**Action:** If context is important:
- Convert to XML documentation comment (///)
- Move to MIGRATION_GUIDE.md
- Create GitHub issue

### Risk: Breaking Features
**Mitigation:**
- Only remove COMMENTED code (not active code)
- Test after each file cleanup
- Commit frequently for easy rollback

---

## Dependencies

None - this is pure cleanup work.

---

## Estimated Effort

- Phase 1 (Quick wins): 30 minutes
- Phase 2 (TODO audit): 1 hour
- Phase 3 (Region cleanup): 30 minutes
- Phase 4 (Documentation): 30 minutes
- Testing: 30 minutes
- **Total: 3 hours**

---

## Assignee

coding-agent (automated via Claude Code)

---

## Notes

**Philosophy:** Code should be self-documenting. Comments should explain WHY, not WHAT.

**Good comment:**
```csharp
// Use WeakReference to prevent memory leak when modal is closed
var weakRef = new WeakReference<Modal>(modal);
```

**Bad comment:**
```csharp
// Set variable to true
isLoading = true;
```

**Really bad comment:**
```csharp
// REMOVED: Old code that doesn't work anymore
```

---

## Benefits

### Readability
- Less clutter = easier to read
- No confusing commented-out code
- Clear what code is actually used

### Maintainability
- No confusion about what's active vs. removed
- TODOs tracked properly in issue tracker
- Easier to understand codebase

### Professionalism
- Clean code shows attention to detail
- No development artifacts in production
- Code looks production-ready

---

## Checklist

Before marking this PRD complete:

- [ ] All commented-out code removed
- [ ] All VibeOut references removed
- [ ] All TODOs either implemented or tracked
- [ ] MIGRATION_GUIDE.md created if needed
- [ ] CHANGELOG.md updated
- [ ] Build succeeds with 0 warnings
- [ ] All features tested and working
- [ ] Code review completed
- [ ] Git history shows logical commits (one file/concept per commit)
