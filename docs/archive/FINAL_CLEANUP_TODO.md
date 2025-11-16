# FINAL CLEANUP TODO - Issues Found by AI Agents

**Date:** 2025-11-05
**Status:** Ready for cleanup
**Goal:** Polish the codebase before MVP release

---

## 🔥 CRITICAL: Complete Architecture Refactor

### Issue: Dual Hierarchy Still Exists
**Current State:** FilterBuilderItemViewModel created but not used yet
**What Needs To Happen:**
1. Update VisualBuilderTabViewModel to use FilterBuilderItemViewModel
2. Delete FilterItem.cs, FilterOperatorItem.cs, SelectableItem.cs
3. Remove conversion functions in FiltersModalViewModel
4. Update XAML bindings in ConfigureScoreTab.axaml and ConfigureFilterTab.axaml
5. Test save/load workflow thoroughly

**Priority:** HIGH - This affects maintainability significantly

---

## 💎 Code Quality Issues

### 1. Magic Colors Everywhere

**Problem:** Colors are hardcoded as strings throughout XAML files

**Examples:**
```xml
<SolidColorBrush Color="#2C3E50"/>  <!-- What is this color? -->
<SolidColorBrush Color="#3498DB"/>  <!-- Another magic color -->
```

**Solution:**
- Define ALL colors in BalatroGlobalStyles.axaml as named StaticResources
- Replace all hardcoded colors with StaticResource references
- Document color palette in a COLORS.md file

**Files Affected:**
- src/Styles/BalatroGlobalStyles.axaml
- src/Components/**/*.axaml (all XAML files)

---

### 2. Embarrassing AI-Generated Comments

**Problem:** Comments that state the obvious or are patronizing

**Examples:**
```csharp
// Loop through items (WE KNOW IT'S A LOOP!)
foreach (var item in items)

// This is a method that does X (THE METHOD NAME ALREADY SAYS THAT!)
public void DoX()

// Initialize the variable to empty string (NO SHIT!)
var foo = "";
```

**Solution:**
- Remove all obvious comments
- Keep ONLY comments that explain WHY, not WHAT
- Remove patronizing explanations

**Search Patterns:**
- `// Loop through`
- `// Initialize`
- `// Set the`
- `// Get the`
- `// Check if`
- `// This is a`

---

### 3. MVVM Violations

**Issue:** Code-behind logic in views instead of ViewModels

**Examples to Find:**
- Event handlers in `.axaml.cs` files that manipulate UI state
- Business logic in code-behind
- Direct manipulation of ViewModel from View

**Solution:**
- Move all logic to ViewModels
- Use Commands and data binding
- Code-behind should ONLY contain:
  - Avalonia control initialization
  - Drag-drop event handlers (minimal)
  - Attached property handlers

---

### 4. Anti-Patterns Found

#### A. God Objects
**Files:**
- `FiltersModalViewModel.cs` (2000+ lines)
- `VisualBuilderTabViewModel.cs` (2200+ lines)

**Solution:**
- Extract helper services (FilterValidationService, FilterConversionService)
- Split into smaller, focused ViewModels
- Use composition over giant classes

#### B. String-Based References
**Problem:** Using string keys for ItemConfig lookup

```csharp
_parentViewModel.ItemConfigs["Joker_Blueprint#1234"]  // Fragile!
_parentViewModel.SelectedShould.Add("Joker_Blueprint#1234")  // Error-prone!
```

**Solution (Long-term):**
- Consider direct object references instead of string keys
- OR: Use strongly-typed keys (record ItemConfigKey { string Value })

#### C. Nullable Violations
**Problem:** Nullable warnings suppressed with `!` operator

```csharp
var config = _parentViewModel.ItemConfigs[key]!;  // What if key doesn't exist?
```

**Solution:**
- Use TryGetValue pattern
- Check for null properly
- Remove `!` operators

---

## 🧹 Code Cleanup Tasks

### Remove Dead Code

**Search for:**
- Unused using statements
- Commented-out code blocks
- Unreferenced methods
- Obsolete TODOs

### Consolidate Converters

**Problem:** Multiple copies of same converters

**Files:**
- src/Converters/*.cs
- Check for duplicate functionality

**Solution:**
- Merge duplicate converters
- Create a single, well-documented Converters folder

### Inconsistent Naming

**Issues:**
- Some methods use `On*` prefix for event handlers, some don't
- Some use `Handle*`, some use `*Handler`
- Inconsistent private field naming (`_field` vs `m_field`)

**Solution:**
- Pick ONE convention and stick to it
- Suggested: `_camelCase` for private fields, `On*` for event handlers

---

## 🎨 UI/UX Polish

### Missing Feedback

**Issues:**
- No loading indicators when search is running
- No feedback when save succeeds/fails
- No validation errors shown to user

**Solution:**
- Add loading spinners
- Add toast notifications for save/error
- Show validation errors inline

### Accessibility

**Missing:**
- Keyboard navigation
- Screen reader support
- High contrast mode support

**Solution:**
- Add tab indices
- Add AutomationProperties.Name to controls
- Test with keyboard-only navigation

---

## 🔧 Technical Debt

### 1. Hardcoded Paths

**Problem:** File paths hardcoded in code

```csharp
var path = "x:\\BalatroSeedOracle\\filters\\MyFilter.json";  // BAD!
```

**Solution:**
- Use Path.Combine with proper base paths
- Use app data directory
- Support relative paths

### 2. No Error Handling

**Problem:** Many try-catch blocks just swallow exceptions

```csharp
try
{
    // Do risky thing
}
catch
{
    // SILENCE! (This is BAD!)
}
```

**Solution:**
- Log all exceptions
- Show user-friendly error messages
- Add retry logic where appropriate

### 3. Memory Leaks

**Potential Issues:**
- Event handlers not unsubscribed
- IDisposable not implemented on ViewModels with event subscriptions
- Circular references in parent/child ViewModels

**Solution:**
- Implement IDisposable
- Use WeakEventPattern for long-lived subscriptions
- Audit parent/child relationships

---

## 📝 Documentation Gaps

### Missing Docs

**Need:**
- README.md with setup instructions
- ARCHITECTURE.md explaining overall structure
- CONTRIBUTING.md for future contributors
- API documentation for public methods

### Outdated Docs

**Check:**
- Old markdown files from previous sessions
- Completed TODO files
- Out-of-date screenshots

---

## 🧪 Testing Gaps

### Manual Testing Checklist

**Critical User Flows:**
- [ ] Create new filter
- [ ] Add jokers to MUST/SHOULD/BANNED
- [ ] Create OR clause with 3+ items
- [ ] Create AND clause with 3+ items
- [ ] Save filter to JSON
- [ ] Load filter from JSON
- [ ] Run search with filter
- [ ] Export results to Excel
- [ ] Apply edition/stickers/seals to items

### Edge Cases to Test

**Stress Tests:**
- [ ] Filter with 100+ items
- [ ] Deeply nested OR/AND clauses (5+ levels)
- [ ] Filter with all possible item types
- [ ] Unicode characters in filter names
- [ ] Very long filter names (100+ chars)

---

## 🚀 Performance Optimizations

### 🔥 CRITICAL: Pre-Load ALL Sprites at Startup

**Current Problem:** Lazy loading causes UI lag and disk hits during use

**The SOLUTION (User's Brilliant Idea):**
1. **Pre-load EVERY sprite** during app startup
2. **Use Balatro intro animation** as loading screen
3. **It's only a few MB!** We have RAM in 2025!
4. **Result:** ZERO disk hits during SIMD search, NO UI lag!

**Implementation Plan:**
```csharp
public class SpriteService
{
    // Pre-load ALL sprites at startup
    public async Task PreloadAllSpritesAsync(IProgress<string> progress)
    {
        progress.Report("Loading Jokers...");
        await PreloadJokers();

        progress.Report("Loading Tarots...");
        await PreloadTarots();

        progress.Report("Loading Planets...");
        await PreloadPlanets();

        // ... etc for all sprite types
    }

    private async Task PreloadJokers()
    {
        // Load ALL joker sprites into memory
        foreach (var joker in JokerList)
        {
            var image = LoadJokerSprite(joker);
            _cachedJokers[joker] = image;
        }
    }
}
```

**Balatro Intro Animation:**
- Look in `external/Balatro/**/*.lua` for intro code
- Recreate the animation in AvaloniaUI (SkiaSharp canvas)
- Show progress bar: "Loading Jokers... 32/150"
- Show Balatro logo animating while sprites load
- **Time estimate:** 2-3 seconds for ~2-3MB of sprites

**Benefits:**
- NO lag when dragging items
- NO disk hits during search
- Smoother UX overall
- Cool loading animation! 🎮

---

### Other Performance Optimizations

**1. Collection Updates:**
- Batch ObservableCollection changes
- Use BeginUpdate/EndUpdate pattern
- Reduce property change notifications

**2. Search Optimization:**
- Cache compiled filters
- Debounce UI filter updates
- Use background threads for heavy operations

---

## 🎯 Polish for MVP Release

### Must-Have Before Release

**Critical:**
- [ ] No compiler warnings
- [ ] No runtime exceptions in happy path
- [ ] All magic colors moved to StaticResources
- [ ] All embarrassing comments removed
- [ ] FilterBuilderItemViewModel refactor complete

**Nice-to-Have:**
- [ ] Loading indicators
- [ ] Save success feedback
- [ ] Keyboard shortcuts documented
- [ ] Tooltips on complex controls
- [ ] About dialog with version info

### Spit Shine Checklist

**UI Polish:**
- [ ] Consistent spacing (all Margins/Paddings use multiples of 4)
- [ ] Consistent font sizes
- [ ] Consistent corner radii (4, 6, 8, 12 only)
- [ ] All buttons have hover states
- [ ] All interactive elements have visual feedback

**Code Polish:**
- [ ] All public methods have XML docs
- [ ] No `// TODO` comments in main code
- [ ] No `#warning` or `#error` directives
- [ ] Consistent code formatting (run formatter)

---

## 📊 Metrics

### Current State
- **Files to refactor:** ~15
- **Lines of code to cleanup:** ~1000
- **Magic colors to extract:** ~50+
- **Obvious comments to remove:** ~100+
- **MVVM violations:** ~10

### Time Estimates
- **Architecture refactor:** 2-3 hours
- **Magic colors cleanup:** 1 hour
- **Comment cleanup:** 30 minutes
- **MVVM fixes:** 1-2 hours
- **Testing:** 2-3 hours
- **Total:** **~8-10 hours to MVP-ready**

---

## 🎉 WHEN THIS IS ALL DONE

You'll have:
- ✅ Clean, maintainable architecture
- ✅ No embarrassing AI comments
- ✅ Proper MVVM separation
- ✅ Named colors instead of magic strings
- ✅ Working OR/AND clause workflow
- ✅ Robust save/load with actual config objects
- ✅ A codebase you can be PROUD to show people!

**THEN WE RELEASE THIS HOE!** 🚀🎊

