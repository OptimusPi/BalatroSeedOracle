# PRD: Filter Modal UI Bug Fixes

**Status:** 🔴 **CRITICAL** - Visual bugs affecting UX
**Priority:** P0 - Immediate Fix Required
**Estimated Time:** 1-2 hours
**Generated:** 2025-11-14

---

## Executive Summary

Two visual bugs in the [FilterSelectionModal.axaml](x:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml) are degrading user experience:

1. **Red arrow misalignment** - The bouncing red triangle indicator on the left-side filter list is not properly centered vertically with the selected button
2. **Joker sprite cramping** - Joker cards in the three-strip header appear cramped together with no breathing room, likely caused by recent scrollbar width changes

These issues were identified after implementing the new 12px wide scrollbar with negative margins.

---

## Problem Statement

### Bug #1: Red Arrow Misalignment

**File:** `src/Views/Modals/FilterSelectionModal.axaml`
**Lines:** 129-140
**Severity:** MEDIUM - Visual polish issue

#### Current Implementation
```xml
<!-- Triangle attached to left edge of button -->
<Polygon Points="0,0 14,8.5 0,17"
         Width="14"
         Height="17"
         Fill="{StaticResource Red}"
         Classes="balatro-bounce-horizontal"
         HorizontalAlignment="Left"
         VerticalAlignment="Center"
         Margin="-23,2,0,0"
         ZIndex="10"
         IsVisible="{Binding IsSelected}"
         IsHitTestVisible="False"/>
```

#### Issues Identified
- `Margin="-23,2,0,0"` includes a vertical offset of `2` pixels
- This was likely added to manually adjust positioning
- The `VerticalAlignment="Center"` should handle centering automatically
- The button height is `20` (line 1356), triangle height is `17`
- Mathematical centering: `(20 - 17) / 2 = 1.5` pixels offset needed
- Current `2` pixels is close but slightly off

#### Visual Impact
- Arrow appears 0.5-1 pixel too low
- Not noticeable with small buttons but visible on tall buttons
- Inconsistent with top tab triangle positioning (which is perfect)

---

### Bug #2: Joker Sprite Cramping

**File:** `src/Views/Modals/FilterSelectionModal.axaml`
**Lines:** 172-229 (Jokers strip), 231-321 (Vouchers strip), 323-413 (Consumables strip)
**Severity:** MEDIUM - Affects readability

#### Current Implementation
```xml
<!-- JOKERS Strip (Left) - ALWAYS VISIBLE -->
<Border Grid.Column="0"
        Background="{StaticResource DarkTealGrey}"
        CornerRadius="6"
        Padding="8"
        Height="80"
        ClipToBounds="False">
    <Grid>
        <!-- ... -->
        <ItemsControl Grid.Column="1" ItemsSource="{Binding SelectedFilter.Must.Jokers}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel Orientation="Horizontal" ItemWidth="40" ItemHeight="52"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Width="71" Height="95" Margin="2">
                        <Image Source="{Binding ., Converter={StaticResource ItemNameToSpriteConverter}}"
                               Width="71" Height="95"
                               Stretch="Uniform">
                ```

#### Issues Identified
- **ItemsPanel WrapPanel**: `ItemWidth="40"` and `ItemHeight="52"` define cell sizes
- **ItemTemplate Grid**: `Width="71"` and `Height="95"` define actual card sizes
- **MISMATCH**: Card is 71px wide but cell is only 40px wide
- **Result**: Cards overflow and overlap horizontally
- **Margin**: `Margin="2"` provides minimal spacing (only 4px total between cards)
- **Root Cause**: Recent scrollbar changes may have reduced available width, causing tighter wrapping

#### Expected Behavior
- Cards should have 6-8px spacing between them
- No overlapping
- Proper breathing room for hover effects

---

## Current Architecture

### Filter List (Left Side)
```
FilterSelectionModal
└── Border (Left Column, Grid.Column="0")
    └── DockPanel
        ├── StackPanel (Pagination Controls) - Docked Bottom
        └── ItemsControl (Filter List)
            └── ItemTemplate
                └── Grid (wrapper)
                    ├── Button (filter name)
                    └── Polygon (red arrow indicator) ← BUG #1
```

### Three-Strip Header (Top of Right Panel)
```
FilterSelectionModal
└── Grid (Row 0, Three-Strip Layout)
    ├── Border (Jokers Strip, Grid.Column="0")  ← BUG #2
    │   └── ItemsControl
    │       └── WrapPanel (ItemWidth=40, ItemHeight=52)
    │           └── Grid (Width=71, Height=95, Margin=2)
    │               └── Image (71x95)
    ├── Border (Vouchers Strip, Grid.Column="2")  ← SAME BUG
    └── Border (Consumables Strip, Grid.Column="4")  ← SAME BUG
```

---

## Root Cause Analysis

### Bug #1: Red Arrow Misalignment
**Cause:** Hardcoded vertical margin `Margin="-23,2,0,0"`
**Why it exists:** Quick fix to adjust positioning, never cleaned up
**Correct solution:** Remove vertical offset, let `VerticalAlignment="Center"` handle it

### Bug #2: Joker Cramping
**Cause:** Mismatch between `WrapPanel.ItemWidth` (40) and `Grid.Width` (71)
**Why it exists:**
1. Original design had different card sizes or spacing
2. Recent scrollbar changes reduced available width
3. ItemWidth never updated to match actual card width

**Correct solution:** Set `ItemWidth="75"` (71px card + 4px margin) or remove ItemWidth entirely

---

## Implementation Plan

### Phase 1: Fix Red Arrow Alignment (15 minutes)

**File:** `src/Views/Modals/FilterSelectionModal.axaml:130-140`

**Change:**
```xml
<!-- BEFORE (lines 129-140) -->
<Polygon Points="0,0 14,8.5 0,17"
         Width="14"
         Height="17"
         Fill="{StaticResource Red}"
         Classes="balatro-bounce-horizontal"
         HorizontalAlignment="Left"
         VerticalAlignment="Center"
         Margin="-23,2,0,0"
         ZIndex="10"
         IsVisible="{Binding IsSelected}"
         IsHitTestVisible="False"/>

<!-- AFTER -->
<Polygon Points="0,0 14,8.5 0,17"
         Width="14"
         Height="17"
         Fill="{StaticResource Red}"
         Classes="balatro-bounce-horizontal"
         HorizontalAlignment="Left"
         VerticalAlignment="Center"
         Margin="-23,0,0,0"
         ZIndex="10"
         IsVisible="{Binding IsSelected}"
         IsHitTestVisible="False"/>
```

**What changed:** `Margin="-23,2,0,0"` → `Margin="-23,0,0,0"` (removed vertical offset)

**Why this works:**
- `VerticalAlignment="Center"` will now properly center the 17px tall arrow in the 20px tall button
- Automatic centering is more robust than hardcoded offsets
- Matches the approach used in top tab triangles (which work perfectly)

---

### Phase 2: Fix Joker Sprite Spacing (30 minutes)

**Files:** `src/Views/Modals/FilterSelectionModal.axaml:200-227` (Jokers), `259-288` (Vouchers), `351-380` (Consumables)

**Option A: Increase ItemWidth (Recommended)**

```xml
<!-- BEFORE (lines 200-205) -->
<ItemsControl Grid.Column="1" ItemsSource="{Binding SelectedFilter.Must.Jokers}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal" ItemWidth="40" ItemHeight="52"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>

<!-- AFTER -->
<ItemsControl Grid.Column="1" ItemsSource="{Binding SelectedFilter.Must.Jokers}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal" ItemWidth="77" ItemHeight="99"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
```

**Calculations:**
- Card width: 71px
- Margin: 2px per side = 4px total horizontal
- Total cell width needed: 71 + 6 = **77px** (with 6px spacing)
- Card height: 95px
- Margin: 2px per side = 4px total vertical
- Total cell height needed: 95 + 4 = **99px**

**Apply to all three strips:**
1. Jokers strip (line 202): `ItemWidth="77" ItemHeight="99"`
2. Vouchers strip (Must section, line 262): `ItemWidth="77" ItemHeight="99"`
3. Vouchers strip (MustNot section, line 293): `ItemWidth="42" ItemHeight="52"` (smaller cards)
4. Consumables strip (Must section, line 354): `ItemWidth="77" ItemHeight="99"`
5. Consumables strip (MustNot section, line 385): `ItemWidth="42" ItemHeight="52"` (smaller cards)

**Option B: Remove ItemWidth/ItemHeight (Alternative)**

Let WrapPanel auto-size based on Grid size + Margin. Less predictable but more flexible.

```xml
<WrapPanel Orientation="Horizontal"/>
```

**Recommendation:** Use Option A for consistent, predictable layout.

---

### Phase 3: Increase Margin for Better Spacing (15 minutes)

**Optional enhancement:** Increase margin from `Margin="2"` to `Margin="3"` for 6px spacing between cards.

```xml
<!-- BEFORE (line 208) -->
<Grid Width="71" Height="95" Margin="2">

<!-- AFTER -->
<Grid Width="71" Height="95" Margin="3">
```

**Update ItemWidth/ItemHeight:**
- New ItemWidth: 71 + 6 = **77px** (already correct from Phase 2)
- New ItemHeight: 95 + 6 = **101px**

**Apply to:**
- All 71x95 cards (Jokers, Vouchers Must, Consumables Must)
- Keep 36x48 cards (MustNot sections) at `Margin="2"` with `ItemWidth="42" ItemHeight="52"`

---

## Acceptance Criteria

### Bug #1: Red Arrow Alignment
- [ ] Remove hardcoded vertical offset from Polygon Margin
- [ ] Arrow is perfectly centered vertically with button
- [ ] Arrow bounces horizontally without vertical jitter
- [ ] Consistent alignment across all filter list items

### Bug #2: Joker Spacing
- [ ] Update WrapPanel ItemWidth to 77px for 71px cards
- [ ] Update WrapPanel ItemHeight to 99px for 95px cards
- [ ] Apply to all three strips (Jokers, Vouchers, Consumables)
- [ ] Cards no longer overlap
- [ ] Minimum 6px spacing between cards
- [ ] Cards wrap properly when container width changes

---

## Testing Plan

### Manual Testing

#### Test 1: Red Arrow Alignment
1. Open Filter Selection Modal
2. Select different filters from list
3. Observe red arrow positioning
4. **Expected:** Arrow centered vertically with button text
5. **Expected:** No visible offset up or down
6. **Expected:** Smooth horizontal bounce animation

#### Test 2: Joker Spacing
1. Open Filter Selection Modal
2. Select a filter with multiple jokers
3. Observe joker card layout in top header
4. **Expected:** Cards have visible spacing (6px+)
5. **Expected:** No overlapping
6. **Expected:** Cards wrap to second row if needed
7. Resize window to test wrapping behavior
8. **Expected:** Consistent spacing at all window sizes

#### Test 3: Responsive Layout
1. Test at minimum window width (1080px)
2. Test at maximum window width
3. Verify spacing remains consistent
4. Verify no layout breaks or clipping

#### Test 4: All Three Strips
1. Test filter with jokers, vouchers, AND consumables
2. Verify all three strips have proper spacing
3. Verify MustNot items (smaller 36x48 cards) still layout correctly
4. Verify vertical "JOKERS" / "VOUCHERS" / "CONSUMABLES" labels not affected

---

## Files Requiring Changes

### Primary File
- `src/Views/Modals/FilterSelectionModal.axaml`

### Specific Line Changes

**Bug #1 Fix:**
- Line 137: `Margin="-23,2,0,0"` → `Margin="-23,0,0,0"`

**Bug #2 Fix (Jokers Strip):**
- Line 202: `ItemWidth="40" ItemHeight="52"` → `ItemWidth="77" ItemHeight="99"`
- Line 208: `Margin="2"` → `Margin="3"` (optional)

**Bug #2 Fix (Vouchers Strip - Must):**
- Line 262: `ItemWidth="40" ItemHeight="52"` → `ItemWidth="77" ItemHeight="99"`
- Line 269: `Margin="2"` → `Margin="3"` (optional)

**Bug #2 Fix (Vouchers Strip - MustNot):**
- Line 293: Already correct at `ItemWidth="40" ItemHeight="52"` for 36x48 cards
- Line 299: Keep `Margin="2"` for smaller cards

**Bug #2 Fix (Consumables Strip - Must):**
- Line 354: `ItemWidth="40" ItemHeight="52"` → `ItemWidth="77" ItemHeight="99"`
- Line 361: `Margin="2"` → `Margin="3"` (optional)

**Bug #2 Fix (Consumables Strip - MustNot):**
- Line 385: Already correct at `ItemWidth="40" ItemHeight="52"` for 36x48 cards
- Line 391: Keep `Margin="2"` for smaller cards

---

## Success Metrics

### Visual Quality
- ✅ Red arrow perfectly aligned (0px vertical offset)
- ✅ Joker cards have minimum 6px spacing
- ✅ No card overlapping at any window size
- ✅ Consistent spacing across all three strips

### User Experience
- ✅ Filter selection feels polished
- ✅ Cards are readable and hoverable
- ✅ Animations remain smooth
- ✅ Layout responsive to window resize

---

## Risk Assessment

### LOW RISK:
- Both fixes are simple margin/size adjustments
- No logic changes
- No data binding changes
- No animation timing changes

### Potential Issues:
1. **Window too narrow**: Cards might wrap too aggressively if ItemWidth is too large
   - **Mitigation**: Test at minimum window width (1080px)
   - **Fallback**: Use smaller ItemWidth (75px instead of 77px)

2. **Too much spacing**: Cards might feel too spread out
   - **Mitigation**: Start with 6px spacing (Margin="3"), adjust if needed
   - **User feedback**: Can reduce to Margin="2" if user prefers tighter layout

---

## Timeline

### Immediate (30 minutes)
1. Fix red arrow margin (5 minutes)
2. Update WrapPanel ItemWidth/ItemHeight for all strips (15 minutes)
3. Test both fixes (10 minutes)

### Optional Polish (15 minutes)
1. Increase margins for better spacing
2. Test responsive behavior
3. Get user feedback

**Total Estimated Time:** 45 minutes - 1 hour

---

## Notes

- These bugs were introduced/exposed by recent scrollbar width changes
- Both are cosmetic polish issues, not functional breaks
- Fixes are low-risk and straightforward
- Should be completed before any new feature work
- User explicitly flagged these in screenshot checklist

---

## Related PRDs

- [SCROLLBAR_STANDARDIZATION_PRD.md](./SCROLLBAR_STANDARDIZATION_PRD.md) - The scrollbar changes that exposed bug #2
- [TAB_CONTROL_ANIMATION_SYSTEM_PRD.md](./TAB_CONTROL_ANIMATION_SYSTEM_PRD.md) - Similar arrow alignment patterns

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
