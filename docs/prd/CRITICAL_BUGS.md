# CRITICAL BUGS - Immediate Action Required

**Status:** 🔴 CRITICAL
**Priority:** P0 - Fix Immediately
**Estimated Time:** 1-2 hours
**Generated:** 2025-11-03

---

## Overview

This PRD addresses 2 critical bugs that cause silent failures and could corrupt user experience with no error feedback.

---

## Bug #1: Silent Exception Swallowing in SpriteService

### Problem
**File:** `src/Services/SpriteService.cs:365`
**Severity:** CRITICAL

Empty catch block silently swallows ALL exceptions during asset loading:

```csharp
catch { }  // Line 365 - NO error logging, NO user feedback
```

### Impact
- Sprite sheets fail to load → Users see blank/broken UI
- No error message shown to user
- No debug logs to help diagnose
- Completely silent failure mode

### User Experience Impact
User launches app, sees empty boxes instead of joker/card sprites, has NO idea what's wrong. Looks like broken/corrupted installation.

### Acceptance Criteria
- [ ] Add proper exception logging with `DebugLogger.LogError`
- [ ] Log WHICH sprite failed to load (file path, sprite name)
- [ ] Consider showing toast notification to user if critical sprites fail
- [ ] Add fallback placeholder sprite for failed loads
- [ ] Test with missing sprite file to verify error handling

### Implementation Steps
1. Replace empty catch block with proper exception handling
2. Log full exception details including stack trace
3. Add sprite name/path to error message
4. Consider adding fallback sprite constant (`MissingSprite.png`)
5. Verify logs appear in debug.log

### Code Location
```
File: src/Services/SpriteService.cs
Line: 365
Method: (Find method containing empty catch block)
```

### Test Plan
1. Delete a sprite file (e.g., joker sprite sheet)
2. Launch app
3. Verify error is logged to debug.log
4. Verify user sees placeholder OR error message
5. Verify app doesn't crash
6. Restore sprite file and verify normal operation

---

## Bug #2: Async Void Methods - Unhandled Exceptions

### Problem
**Files:** 22 instances across multiple files
**Severity:** CRITICAL

Async void methods cannot be awaited and exceptions aren't properly propagated, causing silent failures.

### Affected Files
- `src/Behaviors/CardFlipRevealBehavior.cs:141` - `public async void TriggerFlip()`
- `src/Views/BalatroMainMenu.axaml.cs` - Multiple methods:
  - `ShowSearchModalWithFilter`
  - `ShowFiltersModalDirect`
  - `ShowSettingsModal`
  - `ShowCollectionModal`
  - `ShowWidgetsModal`
- `src/Components/FilterSelector.axaml.cs` - `OnLoaded`, `RefreshFilters`
- Additional instances in widget code-behinds

### Impact
- Exceptions thrown in async void methods crash the app OR disappear completely
- No way to await completion
- No way to catch exceptions from callers
- Race conditions in UI updates

### User Experience Impact
App crashes with no error message, OR features silently fail with no indication to user.

### Acceptance Criteria
- [ ] Convert ALL async void methods to async Task
- [ ] Update callers to await the async Task methods
- [ ] Add try-catch blocks where needed
- [ ] Verify exceptions are properly logged
- [ ] Test each converted method for proper error handling

### Implementation Strategy

**Pattern to follow:**

```csharp
// BEFORE (WRONG):
public async void ShowModal()
{
    await Task.Delay(100);
    // Exception here crashes app or disappears
}

// AFTER (CORRECT):
public async Task ShowModalAsync()
{
    try
    {
        await Task.Delay(100);
    }
    catch (Exception ex)
    {
        DebugLogger.LogError("ClassName", $"ShowModal failed: {ex.Message}");
        // Optionally re-throw or show user error
    }
}

// Update callers:
await ShowModalAsync();
```

**Exception: Event Handlers**
Event handlers (like button clicks) MUST remain `async void`:
```csharp
private async void OnButtonClick(object? sender, RoutedEventArgs e)
{
    // This is OK - event handlers must be async void
}
```

### Implementation Steps

1. **Find all async void methods** (excluding event handlers):
   ```bash
   grep -rn "async void" src/ --include="*.cs" | grep -v "private async void On"
   ```

2. **For each method:**
   - Change signature: `async void Foo()` → `async Task FooAsync()`
   - Add try-catch with logging
   - Update all callers to `await FooAsync()`
   - Add `ConfigureAwait(false)` for non-UI async calls

3. **Priority order:**
   - CardFlipRevealBehavior.TriggerFlip (animation - crashes if fails)
   - BalatroMainMenu modal methods (user-facing)
   - FilterSelector refresh methods (affects filter loading)
   - Widget initialization methods (affects widget functionality)

### Test Plan

1. For each converted method:
   - Add a test that throws an exception
   - Verify exception is caught and logged
   - Verify UI shows error OR gracefully degrades
   - Verify app doesn't crash

2. Regression testing:
   - Launch app and open each modal
   - Refresh filter selector
   - Trigger card flip animations
   - Open/close widgets
   - Verify all features still work

---

## Success Metrics

- ✅ Zero empty catch blocks in codebase
- ✅ Zero async void methods (except event handlers)
- ✅ All exceptions logged to debug.log
- ✅ No silent failures during normal operation
- ✅ App handles missing assets gracefully

---

## Rollback Plan

If fixes cause issues:
1. Revert specific file changes via git
2. Monitor debug.log for new errors
3. Test in clean environment

---

## Dependencies

- DebugLogger class (already exists)
- ClipboardService (if showing error messages to user)
- No external dependencies needed

---

## Estimated Effort

- Bug #1 (Empty catch): 30 minutes
- Bug #2 (Async void): 2-3 hours (22 methods to convert + testing)
- Total: 3-4 hours

---

## Assignee

coding-agent (automated via Claude Code)

---

## Notes

These bugs are classified as CRITICAL because they cause **silent failures** - the worst kind of bug. Users have no idea something is wrong until features break mysteriously.

Priority: Fix these BEFORE any feature work.
