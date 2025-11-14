# ItemConfigPopup MVVM Refactor Plan

## Executive Summary

The ItemConfigPopup is **already well-architected** following proper Avalonia MVVM best practices. This refactor focuses on:
1. **Bug fixes** (critical missing color resources)
2. **Feature completion** (add missing UI for existing model properties)
3. **Code simplification** (KISS principles, reduce duplication)
4. **Maintainability improvements** (extract services, consolidate styles)

**DO NOT**: Apply BaseWidget pattern (different use case), over-engineer, or rebuild from scratch.

---

## Current Architecture (Already Good!)

### Files
- **View**: `src/Controls/ItemConfigPopup.axaml`
- **Code-behind**: `src/Controls/ItemConfigPopup.axaml.cs` (minimal - proper MVVM ✓)
- **ViewModel**: `src/ViewModels/ItemConfigPopupViewModel.cs`
- **Model**: `src/Models/ItemConfig.cs`

### Integration
- Opens on right-click of items in drop zones (VisualBuilderTab)
- Events: `ConfigApplied`, `Cancelled`, `DeleteRequested`
- Proper event-based communication ✓

### Currently Configurable
- **Item Name** - Header display
- **Antes** (0-8) - Individual boolean checkboxes
- **Edition** (Jokers/Cards) - Radio buttons with images (Foil, Holo, Polychrome, Negative)
- **Playing Card Properties** - Rank, Suit, Enhancement, Preview
- **Sources** - Shop slots, pack slots, skip blind tags, mega arcana flag
- **Apply/Delete/Cancel** buttons

---

## Problems Identified

### 🔴 CRITICAL: Missing Color Resources
**Issue**: Runtime errors when popup opens
```
ERROR: Static resource 'NeonGreen' not found.
ERROR: Static resource 'TransparentDark1' not found.
ERROR: Static resource 'TransparentDark2' not found.
```

**Files affected**:
- `src/Controls/EditionSelector.axaml` (lines 9, 22, 35)
- `src/Controls/SourceSelector.axaml` (lines 9, 22, 35)

**Fix**: Add to `src/App.axaml` resources:
```xml
<!-- Neon/Glow Colors -->
<Color x:Key="ColorNeonGreen">#4ade80</Color>
<SolidColorBrush x:Key="NeonGreen" Color="{StaticResource ColorNeonGreen}"/>

<!-- Transparent Overlays -->
<SolidColorBrush x:Key="TransparentDark1" Color="#55000000"/>
<SolidColorBrush x:Key="TransparentDark2" Color="#33000000"/>
```

### 🟡 MEDIUM: Missing UI for Existing Model Properties
**From `ItemConfig.cs`**:
- ✅ Antes - HAS UI
- ✅ Edition - HAS UI
- ✅ Rank/Suit/Enhancement - HAS UI (for cards)
- ❌ **Seal** - In model, NO UI (Red, Blue, Gold, Purple seals for playing cards)
- ❌ **Stickers** - In model, NO UI (eternal, perishable, rental)
- ❌ **Score** - In model, NO UI (scoring weight for SHOULD clauses)
- ❌ **Min** - In model, NO UI (minimum count for MUST clauses)
- ❌ **Label** - In model, NO UI (custom label text)

### 🟡 MEDIUM: Code Duplication
**Antes Implementation**:
- 9 individual `AnteX` boolean properties in ViewModel
- Manual switch statement for ante mapping (lines 174-203)
- Should use collection + converter

**Selector Controls**:
- EditionSelector and SourceSelector have identical styling patterns
- Both use same missing color resources
- Could share base style

### 🟢 LOW: Over-Engineering (Minor)
- Ranks/Suits/Enhancements lists regenerated per instance (could be static)
- Edition images dictionary regenerated on every Configure() call
- Preview image rendering in ViewModel (could extract to service)

---

## BaseWidget Pattern Comparison

**USER ASKED**: Should we use BaseWidget pattern like widgets use?

**ANSWER**: **NO** - BaseWidget is for persistent desktop widgets with drag/minimize/maximize. ItemConfigPopup is a transient modal dialog - completely different use case.

| BaseWidget | ItemConfigPopup |
|------------|-----------------|
| Long-lived desktop widget | Temporary modal popup |
| Persistent position/size state | Transient configuration state |
| Drag, minimize, maximize | Open, configure, close |
| Managed by WidgetPositionService | Event-driven lifecycle |

**Conclusion**: Current event-based architecture is CORRECT for a modal configuration popup.

---

## Refactor Plan

### Phase 1: Fix Critical Bugs (Immediate)

**1.1 Add Missing Color Resources**
- **File**: `src/App.axaml`
- **Action**: Add NeonGreen, TransparentDark1, TransparentDark2 to `<Application.Resources>`
- **Priority**: CRITICAL - app crashes without these

**1.2 Test Popup Renders Without Errors**
- Open popup, verify no resource errors
- Test all item types (Joker, Tarot, PlayingCard, etc.)

---

### Phase 2: Add Missing Features (High Priority)

**2.1 Add Seal Selector** (Already in model!)
- **File**: `src/Controls/ItemConfigPopup.axaml`
- **Location**: After Enhancement selector
- **UI**: Radio buttons with images (similar to Edition selector)
- **Assets**: Red seal, Blue seal, Gold seal, Purple seal (already exist in Balatro sprites)
- **Visibility**: Only for PlayingCard and StandardCard types

**2.2 Add Stickers Checkboxes**
- **File**: `src/Controls/ItemConfigPopup.axaml`
- **Location**: After Edition selector
- **UI**: 3 checkboxes with icons
  - ☐ Eternal (cannot be sold)
  - ☐ Perishable (debuffs after rounds)
  - ☐ Rental (costs money each round)
- **Assets**: Use existing sticker images from sprites
- **Visibility**: For Jokers only

**2.3 Add Score Input** (for SHOULD clauses)
- **File**: `src/Controls/ItemConfigPopup.axaml`
- **Location**: New section after Antes
- **UI**: NumericUpDown (default=1, min=0, max=100)
- **Label**: "Score Weight" or "Priority"
- **Visibility**: Always visible (defaults to 1)

**2.4 Add Min Count Input** (for MUST clauses)
- **File**: `src/Controls/ItemConfigPopup.axaml`
- **Location**: Near Score input
- **UI**: NumericUpDown (default=1, min=1, max=10)
- **Label**: "Minimum Count"
- **Visibility**: Always visible (defaults to 1)

---

### Phase 3: Simplify Code (KISS Principles)

**3.1 Refactor Antes Collection**
- **Current**: 9 individual `bool Ante0...Ante8` properties + switch statement
- **Better**:
```csharp
[ObservableProperty]
private ObservableCollection<AnteItem> _antes = new()
{
    new(0, "Ante 0", true),
    new(1, "Ante 1", true),
    // ... etc
};

// In XAML: ItemsControl with checkboxes bound to collection
```

**3.2 Extract Static Data to Service**
- **Create**: `src/Services/SelectorDataService.cs`
```csharp
public static class SelectorDataService
{
    public static readonly List<string> Ranks = new() { "2", "3", ... "Ace" };
    public static readonly List<string> Suits = new() { "Hearts", "Clubs", "Diamonds", "Spades" };
    public static readonly List<string> Enhancements = new() { "None", "Bonus", "Mult", ... };
    public static readonly List<string> Seals = new() { "None", "Red", "Blue", "Gold", "Purple" };
}
```

**3.3 Extract Preview Rendering to Service**
- **Create**: `src/Services/ItemConfigPreviewService.cs`
```csharp
public class ItemConfigPreviewService
{
    public IImage GeneratePreviewImage(ItemConfig config)
    {
        // Move rendering logic from ViewModel
    }
}
```

---

### Phase 4: Reduce Duplication

**4.1 Consolidate Selector Styles**
- **Create**: `src/Styles/SelectorStyles.axaml`
- **Extract**: Common styling from EditionSelector and SourceSelector
- **Include**: Via StyleInclude in ItemConfigPopup

**4.2 Decide on Edition Selection Approach**
- **Option A**: Use existing EditionSelector control (remove custom RadioButtons)
- **Option B**: Keep custom RadioButtons (remove/deprecate EditionSelector)
- **Recommendation**: Keep custom (it's simpler and already works)

---

### Phase 5: Maintainability

**5.1 Extract Inline Styles**
- **Create**: `src/Styles/ItemConfigPopupStyles.axaml`
- **Move**: All `<Style>` definitions from ItemConfigPopup.axaml
- **Include**: Via StyleInclude

**5.2 Add XML Documentation**
- Document all ViewModel properties
- Document configurable options in ItemConfig model
- Add usage examples in code comments

**5.3 Add Unit Tests** (Optional)
- Test ItemConfigPopupViewModel configuration logic
- Test ante selection logic
- Test edition/seal/sticker combinations

---

## File Structure (After Refactor)

```
src/
├── Controls/
│   ├── ItemConfigPopup.axaml              (existing - enhanced UI)
│   ├── ItemConfigPopup.axaml.cs           (existing - minimal)
│   ├── EditionSelector.axaml              (deprecate or keep for reuse)
│   └── SourceSelector.axaml               (existing - simplify)
├── ViewModels/
│   ├── ItemConfigPopupViewModel.cs        (existing - refactored)
│   ├── EditionSelectorViewModel.cs        (existing)
│   └── SourceSelectorViewModel.cs         (existing)
├── Models/
│   ├── ItemConfig.cs                      (existing - complete)
│   └── AnteItem.cs                        (NEW - for antes collection)
├── Services/
│   ├── SelectorDataService.cs             (NEW - static data)
│   └── ItemConfigPreviewService.cs        (NEW - preview rendering)
├── Styles/
│   ├── ItemConfigPopupStyles.axaml        (NEW - extracted styles)
│   └── SelectorStyles.axaml               (NEW - shared selector styles)
└── App.axaml                              (FIX - add missing colors)
```

---

## Implementation Priority

### Sprint 1: Stabilize (Bug Fixes)
- [ ] Add NeonGreen, TransparentDark1, TransparentDark2 to App.axaml
- [ ] Test popup opens without errors
- [ ] Verify all item types work

### Sprint 2: Complete (Missing Features)
- [ ] Add Seal selector UI (radio buttons with images)
- [ ] Add Stickers checkboxes (eternal, perishable, rental)
- [ ] Add Score input (NumericUpDown)
- [ ] Add Min count input (NumericUpDown)
- [ ] Test all new features bind correctly to model

### Sprint 3: Simplify (KISS)
- [ ] Refactor Antes to ObservableCollection
- [ ] Extract static data to SelectorDataService
- [ ] Extract preview rendering to service
- [ ] Remove duplication between selectors

### Sprint 4: Polish (Maintainability)
- [ ] Extract styles to ResourceDictionary
- [ ] Add XML documentation
- [ ] Add unit tests (optional)

---

## UI Mockup (Proposed Enhancements)

```
┌─────────────────────────────────────────┐
│  [X] Item Configuration: Joker Name     │
├─────────────────────────────────────────┤
│  Antes:                                 │
│  ☑ Ante 0  ☑ Ante 1  ☑ Ante 2  ...    │
│                                         │
│  Edition:                               │
│  ◉ Normal  ○ Foil  ○ Holo  ○ Poly ...  │
│                                         │
│  Stickers:                        (NEW) │
│  ☐ Eternal  ☐ Perishable  ☐ Rental     │
│                                         │
│  Score Weight: [    1    ]        (NEW) │
│  Minimum Count: [    1    ]       (NEW) │
│                                         │
│  ▼ Advanced Source Options              │
│     Shop Slots: [1,2,3]                 │
│     Pack Slots: [1]                     │
│     ☐ Skip Blind Tags                   │
│     ☐ Mega Arcana Only                  │
│                                         │
│  [Apply]  [Delete]  [Cancel]            │
└─────────────────────────────────────────┘
```

**For Playing Cards:**
```
┌─────────────────────────────────────────┐
│  [X] Item Configuration: 5 of Hearts    │
├─────────────────────────────────────────┤
│  Antes: (same)                          │
│                                         │
│  Card Preview:                          │
│  [     5♥     ]                         │
│                                         │
│  Rank: [  5  ▼]  Suit: [Hearts ▼]      │
│  Enhancement: [None ▼]                  │
│  Seal: ○ None ◉ Red ○ Blue ○ Gold (NEW)│
│                                         │
│  Edition: (same)                        │
│  Score Weight: (same)              (NEW)│
│  Minimum Count: (same)             (NEW)│
│  (Advanced options same)                │
│                                         │
│  [Apply]  [Delete]  [Cancel]            │
└─────────────────────────────────────────┘
```

---

## Design Principles

### KISS (Keep It Simple, Stupid)
- ✅ Use built-in controls (CheckBox, RadioButton, NumericUpDown)
- ✅ Leverage existing Avalonia data binding (no manual property forwarding)
- ❌ Don't create complex abstractions or base classes unless needed
- ❌ Don't over-engineer selector controls

### MVVM Best Practices
- ✅ Zero business logic in code-behind (already done!)
- ✅ All state in ViewModel with INotifyPropertyChanged
- ✅ Event-based communication for cross-component interaction
- ✅ Data binding for all UI updates

### Avalonia Patterns
- ✅ Use StyledProperty for bindable properties
- ✅ Use ICommand for user actions
- ✅ Use Converters for complex binding logic
- ✅ Use ResourceDictionary for reusable styles

---

## Questions for User

1. **Seal Assets**: Do the seal sprites already exist? (Red/Blue/Gold/Purple seals)
2. **Stickers Assets**: Do eternal/perishable/rental sticker overlays exist?
3. **Score Tally Tab**: User mentioned maybe adding a tab - is this still needed or should Score just be an input in the popup? (RECOMMENDATION: Just add input to popup, no separate tab)
4. **Label/TagType**: Should these be visible in UI or remain model-only?

---

## Success Criteria

✅ **Phase 1**: Popup opens without color resource errors
✅ **Phase 2**: All model properties have corresponding UI
✅ **Phase 3**: Code is simpler (collection-based antes, extracted services)
✅ **Phase 4**: No code duplication, reusable styles
✅ **Bonus**: Unit tests pass, XML documentation complete

---

## Notes

- **DO NOT** apply BaseWidget pattern (wrong use case)
- **DO NOT** create separate score tally tab (add to popup)
- **DO NOT** over-engineer with complex inheritance hierarchies
- **DO** follow existing MVVM patterns (already correct!)
- **DO** keep it simple and maintainable (KISS!)

---

**Generated**: 2025-11-13
**Status**: READY FOR REVIEW & APPROVAL
