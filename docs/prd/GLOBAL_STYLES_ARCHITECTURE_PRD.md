# PRD: Global Styles Architecture & Modularization

**Status:** 🟡 **MEDIUM PRIORITY** - Technical Debt & Maintainability
**Priority:** P2 - Architecture Improvement
**Estimated Time:** 4-6 hours
**Generated:** 2025-11-14

---

## Executive Summary

[BalatroGlobalStyles.axaml](x:\BalatroSeedOracle\src\Styles\BalatroGlobalStyles.axaml) is currently a **1,600+ line monolithic file** containing all global UI styles. While functional, this creates maintainability challenges:

- **Hard to navigate** - Styles for buttons, inputs, scrollbars, cards, and modals all mixed together
- **Merge conflicts** - Multiple devs touching same massive file
- **Slow to load** - All styles parsed on app startup
- **Unclear organization** - No clear sections or grouping
- **Difficult to reuse** - Can't easily extract styles for other projects

This PRD proposes **modularizing** global styles into logical files while maintaining the current StyleInclude architecture.

---

## Current Architecture

### App.axaml Style Loading (Lines 182-188)

```xml
<Application.Styles>
    <StyleInclude Source="avares://Avalonia.Themes.Fluent/FluentTheme.xaml" />
    <StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/BalatroGlobalStyles.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/BalatroAnimations.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/WidgetStyles.axaml" />
</Application.Styles>
```

**Current Structure:**
- BalatroGlobalStyles.axaml (~1600 lines) - Everything
- BalatroAnimations.axaml (~76 lines) - Animation classes
- WidgetStyles.axaml (~150 lines) - Widget-specific styles

---

### BalatroGlobalStyles.axaml Breakdown

**Current sections (estimated line counts):**
1. **Button Styles** (~300 lines)
   - `.btn-red`, `.btn-blue`, `.btn-orange`, `.btn-green`, `.btn-purple`
   - `.back-button`, `.close-button`, `.settings-button`
   - Hover, pressed, disabled states

2. **Input Styles** (~200 lines)
   - TextBox, ComboBox, CheckBox, ToggleSwitch
   - Focus states, validation

3. **Scrollbar Styles** (~150 lines)
   - Vertical scrollbar (custom red thermometer)
   - Track, thumb, hover states

4. **Modal & Panel Styles** (~200 lines)
   - Modal containers, borders, headers
   - Panel backgrounds and shadows

5. **Card & Sprite Styles** (~150 lines)
   - Card containers, hover effects
   - Sprite display, badges

6. **List & Grid Styles** (~200 lines)
   - ListBox, DataGrid styles
   - Row hover, selection

7. **Navigation Styles** (~100 lines)
   - Tab controls, breadcrumbs
   - Menu items

8. **Utility Classes** (~100 lines)
   - `.text-gold`, `.text-red`, `.text-center`
   - Margin helpers, spacing

9. **Special Controls** (~200 lines)
   - Expander, ProgressBar, Slider
   - Context menus, tooltips

**Problem:** All 1,600 lines in one file, hard to find specific styles.

---

## Proposed Modular Architecture

### New File Structure

```
src/Styles/
├── App.axaml (loads all modules)
├── Colors.axaml (Color resources - extracted from App.axaml)
├── Fonts.axaml (Font families, sizes)
├── Shadows.axaml (BoxShadows definitions)
├── Animations/
│   ├── BalatroAnimations.axaml (EXISTING - bounces)
│   ├── Transitions.axaml (fade, slide transitions)
│   └── Effects.axaml (glow, pulse)
├── Controls/
│   ├── Buttons.axaml (~300 lines from global)
│   ├── Inputs.axaml (~200 lines)
│   ├── Scrollbars.axaml (~150 lines)
│   ├── Lists.axaml (~200 lines)
│   └── Navigation.axaml (~100 lines)
├── Components/
│   ├── Modals.axaml (~200 lines)
│   ├── Cards.axaml (~150 lines)
│   ├── Badges.axaml (~50 lines)
│   └── Widgets.axaml (EXISTING WidgetStyles.axaml)
├── Utilities/
│   ├── TextStyles.axaml (~50 lines)
│   ├── Spacing.axaml (~50 lines)
│   └── Layout.axaml (~50 lines)
└── Legacy/
    └── BalatroGlobalStyles.axaml (kept for reference, not loaded)
```

**Total files:** ~20 modular style files vs 1 monolithic file

---

### Loading Order in App.axaml

```xml
<Application.Styles>
    <!-- External themes -->
    <StyleInclude Source="avares://Avalonia.Themes.Fluent/FluentTheme.xaml" />
    <StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />

    <!-- Core resources (must load first) -->
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Colors.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Fonts.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Shadows.axaml" />

    <!-- Animations -->
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Animations/BalatroAnimations.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Animations/Transitions.axaml" />

    <!-- Base controls -->
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Controls/Buttons.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Controls/Inputs.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Controls/Scrollbars.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Controls/Lists.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Controls/Navigation.axaml" />

    <!-- Components -->
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Components/Modals.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Components/Cards.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Components/Widgets.axaml" />

    <!-- Utilities (load last) -->
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Utilities/TextStyles.axaml" />
    <StyleInclude Source="avares://BalatroSeedOracle/Styles/Utilities/Spacing.axaml" />
</Application.Styles>
```

**Benefits:**
- Clear dependency order
- Easy to disable specific modules for testing
- Self-documenting architecture

---

## Implementation Plan

### Phase 1: Extract Colors & Resources (1 hour)

**Create:** `src/Styles/Colors.axaml`

**Move from App.axaml:**
- All `<Color x:Key="Color...">` definitions (Layer 1)
- All `<SolidColorBrush x:Key="...">` definitions (Layer 2)
- Color aliases (Layer 3)

**Create:** `src/Styles/Fonts.axaml`
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <Style>
        <Style.Resources>
            <FontFamily x:Key="BalatroFont">avares://BalatroSeedOracle/m6x11plusplus.otf#m6x11plusplus</FontFamily>
            <x:Double x:Key="FontSizeNormal">14</x:Double>
            <x:Double x:Key="FontSizeSmall">12</x:Double>
            <x:Double x:Key="FontSizeLarge">16</x:Double>
        </Style.Resources>
    </Style>
</Styles>
```

**Create:** `src/Styles/Shadows.axaml`
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <Style>
        <Style.Resources>
            <BoxShadows x:Key="StandardModalShadow">0 4 8 0 #777e89</BoxShadows>
            <BoxShadows x:Key="WidgetWindowShadow">0 6 16 0 #55000000</BoxShadows>
            <!-- ... all shadow definitions ... -->
        </Style.Resources>
    </Style>
</Styles>
```

---

### Phase 2: Extract Button Styles (1 hour)

**Create:** `src/Styles/Controls/Buttons.axaml`

**Move from BalatroGlobalStyles.axaml:**
- All `Button.btn-red`, `Button.btn-blue`, etc. styles
- Back button, close button styles
- Disabled states, hover states

**Structure:**
```xml
<Styles xmlns="https://github.com/avaloniaui">
    <!-- Red buttons (primary actions) -->
    <Style Selector="Button.btn-red">
        <!-- ... -->
    </Style>

    <!-- Blue buttons (secondary actions) -->
    <Style Selector="Button.btn-blue">
        <!-- ... -->
    </Style>

    <!-- Special buttons -->
    <Style Selector="Button.back-button">
        <!-- ... -->
    </Style>
</Styles>
```

**Test:** Build app, verify all buttons still styled correctly.

---

### Phase 3: Extract Input Styles (45 minutes)

**Create:** `src/Styles/Controls/Inputs.axaml`

**Contents:**
- TextBox styles
- ComboBox styles
- CheckBox styles
- ToggleSwitch styles (move from ToggleSwitch.axaml?)

---

### Phase 4: Extract Scrollbar Styles (30 minutes)

**Create:** `src/Styles/Controls/Scrollbars.axaml`

**Move:**
- Vertical scrollbar template (~80 lines)
- Horizontal scrollbar template (when created, per SCROLLBAR_STANDARDIZATION_PRD)
- Hover states

---

### Phase 5: Extract Remaining Control Styles (1 hour)

**Create:**
- `Lists.axaml` (ListBox, DataGrid)
- `Navigation.axaml` (TabControl, menus)

---

### Phase 6: Extract Component Styles (1 hour)

**Create:**
- `Modals.axaml` (modal containers, headers, footers)
- `Cards.axaml` (card displays, sprite containers)
- `Badges.axaml` (notification badges, count badges)

---

### Phase 7: Utility Styles (30 minutes)

**Create:**
- `TextStyles.axaml` (`.text-gold`, `.text-center`, etc.)
- `Spacing.axaml` (margin helpers)
- `Layout.axaml` (alignment helpers)

---

### Phase 8: Update App.axaml (30 minutes)

**Replace:**
```xml
<StyleInclude Source="avares://BalatroSeedOracle/Styles/BalatroGlobalStyles.axaml" />
```

**With:**
```xml
<!-- Core -->
<StyleInclude Source="avares://BalatroSeedOracle/Styles/Colors.axaml" />
<StyleInclude Source="avares://BalatroSeedOracle/Styles/Fonts.axaml" />
<!-- ... all new modules ... -->
```

---

### Phase 9: Archive Old File (5 minutes)

**Rename:**
- `BalatroGlobalStyles.axaml` → `BalatroGlobalStyles.axaml.OLD`
- Or move to `src/Styles/Legacy/`

**Do not delete** - keep for reference during testing.

---

## Acceptance Criteria

### Functionality
- [ ] App builds without errors
- [ ] All styles load correctly
- [ ] No visual regressions (buttons, inputs, modals all match)
- [ ] Animation classes still work
- [ ] Performance unchanged (load time < 100ms)

### Organization
- [ ] Styles grouped logically by type
- [ ] Each file < 300 lines
- [ ] Clear file naming (Buttons.axaml, not ButtonStyles.axaml)
- [ ] No duplicate styles across files

### Maintainability
- [ ] Easy to find specific style (search by control type)
- [ ] Can disable module without breaking app
- [ ] New styles have obvious location
- [ ] Consistent structure across files

---

## Success Metrics

- ✅ **20 modular files** instead of 1 monolithic file
- ✅ **Average file size: <150 lines** (was 1600)
- ✅ **Time to find specific style: <10 seconds** (was ~1 minute)
- ✅ **Zero visual regressions**
- ✅ **Zero performance degradation**

---

## Risk Assessment

### LOW RISK:
- StyleInclude is standard Avalonia pattern
- Can test incrementally (move one module at a time)
- Easy to revert (keep old file as backup)

### MEDIUM RISK - Potential Issues:

#### 1. Style Override Order
**Problem:** Styles loaded later override earlier styles.
**Example:** If Buttons.axaml loads before Colors.axaml, color references fail.
**Mitigation:** Load resources first (Colors, Fonts), then controls, then utilities.

#### 2. Duplicate Selectors
**Problem:** Same selector in multiple files causes conflicts.
**Example:** `Button.btn-red` defined in both Buttons.axaml and Modals.axaml.
**Mitigation:** Clear ownership (buttons = Buttons.axaml, no exceptions).

#### 3. Increased File Count
**Problem:** 20 files to manage instead of 1.
**Mitigation:** Clear folder structure, naming convention, this PRD as guide.

---

## Migration Strategy

### Incremental Approach (Recommended)

1. **Week 1:** Extract Colors, Fonts, Shadows
   - Lowest risk, pure data
   - Test thoroughly

2. **Week 2:** Extract Buttons, Inputs
   - Most used controls
   - High visibility, careful testing

3. **Week 3:** Extract Scrollbars, Lists, Navigation
   - Medium complexity

4. **Week 4:** Extract Components, Utilities
   - Low risk, least used

5. **Week 5:** Polish, documentation, archive old file

**Total:** 5 weeks @ 1 hour per week = 5 hours spread out.

### Big Bang Approach (Alternative)

1. Extract all files in one day (6 hours)
2. Test comprehensively
3. Fix any issues
4. Deploy

**Risk:** Higher chance of breaking changes.
**Benefit:** Done faster.

---

## Testing Plan

### Automated Testing
- [ ] Build succeeds in Release mode
- [ ] No XAML parsing errors
- [ ] Resource references resolve correctly

### Manual Testing
- [ ] Open every modal
- [ ] Click every button style
- [ ] Test all input controls
- [ ] Verify scrollbars appear correctly
- [ ] Check card displays
- [ ] Verify animations work

### Visual Regression Testing
- [ ] Take screenshots before modularization
- [ ] Take screenshots after
- [ ] Pixel-by-pixel comparison
- [ ] Zero visual differences

---

## Timeline

### Full Implementation (6 hours)
- Phase 1 (Colors/Fonts/Shadows): 1 hour
- Phase 2 (Buttons): 1 hour
- Phase 3 (Inputs): 45 minutes
- Phase 4 (Scrollbars): 30 minutes
- Phase 5 (Lists/Nav): 1 hour
- Phase 6 (Components): 1 hour
- Phase 7 (Utilities): 30 minutes
- Phase 8 (App.axaml): 30 minutes
- Phase 9 (Archive): 5 minutes
- Testing: 45 minutes

**Total:** ~6.5 hours

### Incremental (Recommended)
- Week 1: 1 hour
- Week 2: 1 hour
- Week 3: 1 hour
- Week 4: 1 hour
- Week 5: 1 hour

**Total:** 5 hours spread over 5 weeks

---

## Benefits

### Developer Experience
- **Faster navigation:** Find styles in seconds, not minutes
- **Easier collaboration:** Fewer merge conflicts
- **Clear ownership:** Each file has clear purpose
- **Onboarding:** New devs understand structure faster

### Maintainability
- **Easier refactoring:** Change one module without affecting others
- **Easier testing:** Can disable modules to isolate issues
- **Easier reuse:** Extract individual modules for other projects

### Performance
- **Lazy loading potential:** Could load modules on-demand in future
- **Parallel parsing:** Avalonia might parse StyleIncludes in parallel
- **Smaller diffs:** Git diffs show only changed module

---

## Future Enhancements

### P3 - Theme Variants
With modular structure, easy to create theme variants:

```
src/Styles/Themes/
├── Red/
│   └── Colors.axaml (Red theme)
├── Blue/
│   └── Colors.axaml (Blue theme)
└── Gold/
    └── Colors.axaml (Gold theme)
```

Load theme dynamically in App.xaml.cs:
```csharp
var themeColor = Settings.ThemeColor; // "Red", "Blue", "Gold"
var colorPath = $"avares://BalatroSeedOracle/Styles/Themes/{themeColor}/Colors.axaml";
```

---

## Related PRDs

- [COLOR_RESOURCE_CONSOLIDATION_COMPLETE_PRD.md](./COLOR_RESOURCE_CONSOLIDATION_COMPLETE_PRD.md) - Colors.axaml extracted from App.axaml
- [SCROLLBAR_STANDARDIZATION_PRD.md](./SCROLLBAR_STANDARDIZATION_PRD.md) - Goes into Scrollbars.axaml

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
