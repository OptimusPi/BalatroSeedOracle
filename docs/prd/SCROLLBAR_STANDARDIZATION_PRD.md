# PRD: Scrollbar Design Standardization

**Status:** 🟡 **MEDIUM PRIORITY** - UX Consistency
**Priority:** P1 - Enhancement / Polish
**Estimated Time:** 3-4 hours
**Generated:** 2025-11-14

---

## Executive Summary

A custom red thermometer-style scrollbar was recently implemented in [BalatroGlobalStyles.axaml](x:\BalatroSeedOracle\src\Styles\BalatroGlobalStyles.axaml:491-568) to match the Balatro game aesthetic. This design features:
- **12px width** (wider than default)
- **Negative margin** (`-12px`) to overlay content without stealing space
- **Red thermometer appearance** (RedHover track, Red thumb)
- **Rounded corners** and proper hover states

However, this custom scrollbar is only applied to **vertical scrollbars**. This PRD defines a plan to:
1. Standardize the design across **all scrollable areas** in the app
2. Create **horizontal scrollbar** styling to match
3. Identify all components needing scrollbar updates
4. Ensure consistent UX throughout the application

---

## Problem Statement

### Current State

**What Works:**
- Vertical scrollbar styling exists in BalatroGlobalStyles.axaml (lines 491-568)
- Looks great: red thermometer track, smooth thumb, proper sizing
- Negative margin prevents stealing content space

**What's Broken/Inconsistent:**
- Only **vertical scrollbars** are styled
- Horizontal scrollbars use default Avalonia/Fluent theme (ugly, inconsistent)
- Not all scrollable controls may be picking up the global style
- Some components might have inline ScrollViewer styles that override global
- Unclear if all modals, lists, and text areas use the custom scrollbar

### User Impact

**Visual Inconsistency:**
- Users see custom scrollbars in some places, default in others
- Breaks immersion in Balatro-themed UI
- Looks unpolished and incomplete

**UX Confusion:**
- Different scrollbar widths across components
- Some scrollbars steal space, others overlay
- Inconsistent interaction patterns

---

## Current Implementation

### BalatroGlobalStyles.axaml (Lines 491-568)

#### Vertical Scrollbar (COMPLETE)

```xml
<Style Selector="ScrollBar:vertical">
    <Setter Property="Width" Value="12" />
    <Setter Property="Margin" Value="0,0,-12,0" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ScrollBar">
                <Grid>
                    <!-- Red thermometer track background -->
                    <Border Name="PART_Border"
                            Background="{StaticResource RedHover}"
                            CornerRadius="5"
                            Width="10"
                            HorizontalAlignment="Center"/>

                    <!-- Scrollable thumb -->
                    <Track Name="PART_Track"
                           IsDirectionReversed="True"
                           Minimum="{TemplateBinding Minimum}"
                           Maximum="{TemplateBinding Maximum}"
                           Value="{TemplateBinding Value, Mode=TwoWay}"
                           ViewportSize="{TemplateBinding ViewportSize}"
                           Orientation="Vertical">
                        <Track.Thumb>
                            <Thumb>
                                <Thumb.Template>
                                    <ControlTemplate>
                                        <Border Background="{StaticResource Red}"
                                                CornerRadius="4"
                                                Width="10"
                                                MinHeight="30"/>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

#### Horizontal Scrollbar (MISSING)

Currently **does not exist**. Default Fluent theme scrollbar is used.

---

## Target Architecture

### Proposed Design System

#### Design Principles
1. **Consistency**: Same visual style for vertical and horizontal scrollbars
2. **Overlay**: Negative margins to prevent stealing content space
3. **Visibility**: Wide enough to be easily grabbable (12px)
4. **Balatro Theme**: Red (#ff4c40) thumb, darker red (#a02721) track
5. **Smoothness**: Rounded corners, minimum sizes for usability

#### Specifications

**Vertical Scrollbar** (EXISTING):
- Width: 12px
- Margin: `0,0,-12,0` (overlay on right edge)
- Track: 10px wide, RedHover (#a02721), CornerRadius 5
- Thumb: 10px wide, Red (#ff4c40), CornerRadius 4, MinHeight 30px

**Horizontal Scrollbar** (TO BE CREATED):
- Height: 12px
- Margin: `0,0,0,-12` (overlay on bottom edge)
- Track: 10px tall, RedHover (#a02721), CornerRadius 5
- Thumb: 10px tall, Red (#ff4c40), CornerRadius 4, MinWidth 30px
- Orientation: Horizontal (NOT IsDirectionReversed)

**Thumb Hover State**:
- Background: Slightly brighter Red or add glow effect
- Cursor: Hand (if possible)

**Track Interaction**:
- Clicking track should page scroll

---

## Implementation Plan

### Phase 1: Create Horizontal Scrollbar Style (1 hour)

**File:** `src/Styles/BalatroGlobalStyles.axaml`
**Location:** After line 568 (after vertical scrollbar style)

**Add:**
```xml
<!-- Horizontal Scrollbar - Matches vertical design -->
<Style Selector="ScrollBar:horizontal">
    <Setter Property="Height" Value="12" />
    <Setter Property="Margin" Value="0,0,0,-12" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ScrollBar">
                <Grid>
                    <!-- Red thermometer track background -->
                    <Border Name="PART_Border"
                            Background="{StaticResource RedHover}"
                            CornerRadius="5"
                            Height="10"
                            VerticalAlignment="Center"/>

                    <!-- Scrollable thumb -->
                    <Track Name="PART_Track"
                           IsDirectionReversed="False"
                           Minimum="{TemplateBinding Minimum}"
                           Maximum="{TemplateBinding Maximum}"
                           Value="{TemplateBinding Value, Mode=TwoWay}"
                           ViewportSize="{TemplateBinding ViewportSize}"
                           Orientation="Horizontal">
                        <Track.Thumb>
                            <Thumb>
                                <Thumb.Template>
                                    <ControlTemplate>
                                        <Border Background="{StaticResource Red}"
                                                CornerRadius="4"
                                                Height="10"
                                                MinWidth="30"/>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

**Key Differences from Vertical:**
- `Height` instead of `Width`
- `Margin="0,0,0,-12"` (bottom overlay) instead of `Margin="0,0,-12,0"` (right overlay)
- `VerticalAlignment="Center"` for track instead of `HorizontalAlignment="Center"`
- `Orientation="Horizontal"` for Track
- `IsDirectionReversed="False"` (unlike vertical which is True)
- `MinWidth="30"` for thumb instead of `MinHeight="30"`

---

### Phase 2: Add Hover States (30 minutes)

**File:** `src/Styles/BalatroGlobalStyles.axaml`
**Location:** After scrollbar templates

**Add hover feedback:**
```xml
<!-- Scrollbar thumb hover effect -->
<Style Selector="ScrollBar /template/ Thumb:pointerover /template/ Border">
    <Setter Property="Background" Value="{StaticResource Red}"/>
    <Setter Property="Opacity" Value="0.9"/>
</Style>

<Style Selector="ScrollBar /template/ Thumb:pressed /template/ Border">
    <Setter Property="Background" Value="{StaticResource RedHover}"/>
</Style>
```

**Effect:**
- Thumb slightly dims on hover
- Thumb darkens when dragging (pressed state)
- Provides visual feedback for interaction

---

### Phase 3: Audit All Scrollable Components (1 hour)

**Search Strategy:**
```bash
# Find all ScrollViewer instances
grep -r "ScrollViewer" src/Views/ src/Controls/ --include="*.axaml"

# Find inline scrollbar styles that might override global
grep -r "ScrollBar" src/Views/ src/Controls/ --include="*.axaml"

# Find potential scrollable controls
grep -r "VerticalScrollBarVisibility" src/ --include="*.axaml"
grep -r "HorizontalScrollBarVisibility" src/ --include="*.axaml"
```

**Components to Check:**
1. **FilterSelectionModal.axaml** - Right panel content areas
2. **ItemConfigPopup.axaml** - Large item lists
3. **SearchModal.axaml** - Results grid, filter lists
4. **BalatroMainMenu.axaml** - Any long menus
5. **SettingsModal.axaml** - Settings lists
6. **SortableResultsGrid.axaml** - Results DataGrid
7. **DeckSpinner** - If it has scrollable lists
8. **Any ListBox / DataGrid** components

**For Each Component:**
- [ ] Verify it uses ScrollViewer (if needed)
- [ ] Check if it has inline ScrollBar styles (remove if found)
- [ ] Test that global scrollbar style applies
- [ ] Check both vertical and horizontal scrolling (if applicable)
- [ ] Verify negative margins don't cause layout issues

---

### Phase 4: Handle Special Cases (1 hour)

#### Case 1: DataGrid Scrollbars

**Problem:** DataGrid has internal ScrollViewers that might not inherit global styles.

**Solution:**
```xml
<!-- In BalatroGlobalStyles.axaml -->
<Style Selector="DataGrid ScrollBar:vertical">
    <!-- Explicitly apply same style to DataGrid scrollbars -->
    <Setter Property="Width" Value="12" />
    <Setter Property="Margin" Value="0,0,-12,0" />
    <!-- ... same template as global ... -->
</Style>
```

#### Case 2: Modal Nested ScrollViewers

**Problem:** Nested ScrollViewers (modal inside modal) might have conflicting margins.

**Solution:** Test and adjust margins on inner ScrollViewers if needed.

#### Case 3: TextBox / TextArea Scrollbars

**Problem:** Multi-line TextBox controls have tiny scrollbars.

**Solution:**
```xml
<Style Selector="TextBox ScrollBar:vertical">
    <Setter Property="Width" Value="8" />  <!-- Smaller for text editing -->
    <Setter Property="Margin" Value="0,0,-8,0" />
    <!-- ... rest of template ... -->
</Style>
```

#### Case 4: Horizontal Scrolling in WrapPanels

**Problem:** Some item grids might need horizontal scrolling.

**Verify:** ItemConfigPopup item selection might benefit from horizontal scroll.

---

### Phase 5: Testing & Refinement (30 minutes)

**Test all scrollable areas:**
1. Open every modal in the app
2. Scroll vertically and horizontally
3. Verify custom scrollbar appearance
4. Check for:
   - Layout shifts (content shouldn't move when scrollbar appears)
   - Overlapping content (negative margins should work correctly)
   - Grabbability (12px width should be comfortable)
   - Visual consistency (same colors, corners everywhere)

**Edge Cases:**
- Window resize (scrollbars appear/disappear)
- Dynamic content (scrollbar appears mid-use)
- Nested scrolling (modal with scrollable list inside)
- Disabled scrolling (scrollbar hidden when not needed)

---

## Files Requiring Changes

### Primary File
- **`src/Styles/BalatroGlobalStyles.axaml`**
  - Line 569: Add horizontal scrollbar style
  - Line ~620: Add hover state styles
  - Line ~640: Add DataGrid specific styles (if needed)

### Files to Audit (Check for Inline Overrides)
- `src/Views/Modals/FilterSelectionModal.axaml`
- `src/Controls/ItemConfigPopup.axaml`
- `src/Views/SearchModal.axaml` (or SearchTab.axaml)
- `src/Views/BalatroMainMenu.axaml`
- `src/Controls/SortableResultsGrid.axaml`
- `src/Views/Modals/*.axaml` (all modals)

### Files That Might Need Inline Exceptions
- `src/Controls/CodeEditor.axaml` (if exists) - Code editors often need different scrollbars
- `src/Views/Terminal.axaml` (if exists) - Terminal-style controls

---

## Acceptance Criteria

### Vertical Scrollbars
- [x] Custom style exists (ALREADY DONE)
- [x] Applied globally to all ScrollBar:vertical
- [ ] Works in all modals
- [ ] Works in all lists
- [ ] Works in DataGrid
- [ ] Negative margin works (no space theft)
- [ ] Hover state provides feedback

### Horizontal Scrollbars
- [ ] Custom style created matching vertical design
- [ ] 12px height with -12px bottom margin
- [ ] Red thermometer appearance
- [ ] Applied globally to all ScrollBar:horizontal
- [ ] Works in wide content areas
- [ ] Hover state matches vertical

### Consistency
- [ ] All scrollable areas use custom scrollbar
- [ ] No default Fluent scrollbars visible anywhere
- [ ] Colors match exactly (Red #ff4c40, RedHover #a02721)
- [ ] Sizes match exactly (12px width/height, 10px bar, 30px min)
- [ ] Behavior matches (smooth, grabbable, responsive)

### Special Cases
- [ ] DataGrid scrollbars styled
- [ ] TextBox scrollbars styled (smaller if needed)
- [ ] Nested ScrollViewers work correctly
- [ ] No layout issues from negative margins

---

## Success Metrics

### Visual
- ✅ 100% of scrollbars use custom Balatro red theme
- ✅ Zero default Avalonia/Fluent scrollbars visible
- ✅ Consistent appearance across all components

### UX
- ✅ Scrollbars don't steal content space (overlay via negative margin)
- ✅ Scrollbars are wide enough to grab easily (12px)
- ✅ Hover states provide clear interaction feedback
- ✅ Scrolling feels smooth and responsive

### Code Quality
- ✅ All styles in global stylesheet (no inline duplicates)
- ✅ DRY principle (no repeated scrollbar templates)
- ✅ Easy to update theme (change colors in one place)

---

## Risk Assessment

### LOW RISK:
- Global styles apply automatically
- Negative margins are well-tested (vertical already works)
- Easy to revert if issues arise

### MEDIUM RISK - Potential Issues:

#### 1. Negative Margin Edge Cases
**Problem:** Some layouts might not handle negative margins well
**Example:** Grid with strict column definitions
**Mitigation:**
- Test in all modals before finalizing
- Provide opt-out class if needed: `<ScrollViewer Classes="no-overlay-scrollbar"/>`

#### 2. DataGrid Custom ScrollViewers
**Problem:** DataGrid might resist global styles
**Mitigation:**
- Use explicit `DataGrid ScrollBar` selector
- Test with large result sets (1000+ rows)

#### 3. Horizontal Scroll Rare Use
**Problem:** App might not use horizontal scrolling much
**Impact:** Low usage means low priority, but inconsistency still hurts polish
**Mitigation:** Implement anyway for completeness

---

## Timeline

### Immediate (2 hours)
1. Create horizontal scrollbar style (1 hour)
2. Add hover states (30 minutes)
3. Test in main scrollable areas (30 minutes)

### Follow-Up (2 hours)
1. Audit all components (1 hour)
2. Fix any inline overrides (30 minutes)
3. Handle special cases (30 minutes)

**Total Estimated Time:** 4 hours

---

## Validation Checklist

Before marking complete, verify:
- [ ] Open FilterSelectionModal → right panel scrolls with custom scrollbar
- [ ] Open ItemConfigPopup → item list scrolls with custom scrollbar
- [ ] Open Search Results → grid scrolls vertically with custom scrollbar
- [ ] Resize window to force horizontal scroll → horizontal scrollbar appears and matches vertical
- [ ] Hover over scrollbar thumb → visual feedback appears
- [ ] Drag scrollbar thumb → smooth dragging, darkens on press
- [ ] No default scrollbars visible anywhere in app
- [ ] Content doesn't shift when scrollbar appears (negative margin works)

---

## Benefits

### User Experience
- **Consistent visual language** throughout app
- **Professional polish** - no UI elements overlooked
- **Better Balatro immersion** - all UI elements themed

### Developer Experience
- **Single source of truth** for scrollbar styling
- **Easy to update** - change colors in one place
- **No inline duplication** - cleaner XAML files

### Maintenance
- **Future-proof** - new scrollable components automatically styled
- **Theme flexibility** - easy to create variants (blue, gold, etc.)
- **Documented pattern** - PRD serves as reference

---

## Related PRDs

- [FILTER_MODAL_UI_BUGS_PRD.md](./FILTER_MODAL_UI_BUGS_PRD.md) - Joker cramping bug was exposed by initial scrollbar work
- [GLOBAL_STYLES_ARCHITECTURE_PRD.md](./GLOBAL_STYLES_ARCHITECTURE_PRD.md) - Future modularization of global styles

---

## Future Enhancements

### P2 - Color Variants
- Blue thermometer scrollbar for special panels
- Gold scrollbar for premium/highlighted sections
- Dynamic theme switching (settings option)

### P2 - Animation
- Smooth fade-in when scrollbar appears
- Bounce effect when reaching scroll end
- Glow effect on thumb hover

### P3 - Accessibility
- Keyboard scrolling indicators
- Screen reader compatibility
- High contrast mode support

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
