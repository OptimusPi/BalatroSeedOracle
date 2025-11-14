# PRD: Filter Modal Layout Optimization

**Status:** 🟡 **MEDIUM PRIORITY** - UX Enhancement
**Priority:** P2 - Performance & Responsive Design
**Estimated Time:** 4-5 hours
**Generated:** 2025-11-14

---

## Executive Summary

The [FilterSelectionModal.axaml](x:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml) features a unique **three-strip header layout** that displays filter criteria visually using card sprites (Jokers, Vouchers, Consumables). While functional, this design has opportunities for optimization in:

1. **Performance** - Rendering many 71x95px images simultaneously
2. **Responsive layout** - Handling variable content amounts gracefully
3. **Visual density** - Balancing information display with breathing room
4. **Accessibility** - Ensuring sprites are meaningful to all users

---

## Current Architecture

### Three-Strip Header Design (Lines 162-414)

**Layout Structure:**
```
Grid (3 columns + 2 spacers = 5 columns)
├── Column 0: JOKERS Strip
│   ├── Vertical "JOKERS" label (rotated -90°)
│   └── ItemsControl (WrapPanel with 71x95px cards)
├── Column 1: 10px spacer
├── Column 2: VOUCHERS Strip
│   ├── Vertical "VOUCHERS" label
│   ├── Must Vouchers (71x95px)
│   └── MustNot Vouchers (36x48px with red X)
├── Column 3: 10px spacer
└── Column 4: CONSUMABLES Strip
    ├── Vertical "CONSUMABLES" label
    ├── Must Consumables (71x95px)
    └── MustNot Consumables (36x48px with red X)
```

**Current Constraints:**
- Fixed height: `Height="80"`
- Fixed padding: `Padding="8"`
- WrapPanel with `ItemWidth="40" ItemHeight="52"` (causes cramping - see bug PRD)
- ClipToBounds="False" allows overflow
- Three separate views (Filter Info, Score Setup, Deck Info) with same structure

---

## Problems Identified

### Problem 1: Performance with Many Sprites

**Issue:** Rendering 10+ high-res (71x95px) card images simultaneously can impact frame rate.

**Current Behavior:**
- Each filter can have 5-10 jokers, 2-4 vouchers, 2-3 consumables
- 15+ Image controls rendered simultaneously
- Each with BalatroCardSwayBehavior (animated sway)
- Soul face overlay images (double render for some cards)

**Performance Impact:**
- Initial render: ~50-100ms
- Animation frame time: ~2-5ms per frame (acceptable)
- Memory: ~500KB per modal open (image caching)

**Potential Optimizations:**
- Lazy load images outside viewport
- Use lower-res thumbnails (35x47px) for header
- Virtualize if >20 items displayed
- Disable sway animation in compact view

---

### Problem 2: Fixed Height Limitation

**Issue:** `Height="80"` clips content when too many items exist.

**Scenarios:**
- Filter with 10 jokers → wraps to 3+ rows, gets clipped
- Filter with 5 vouchers → wraps, only shows 2 rows
- Inconsistent clipping across three strips

**Current Workaround:** `ClipToBounds="False"` allows overflow, but breaks visual containment.

**Proposed Solutions:**

**Option A: Dynamic Height**
```xml
<Border Height="Auto" MinHeight="80" MaxHeight="160">
```
- Pros: Shows all content
- Cons: Layout shifts as content loads

**Option B: Scrollable Strip**
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="80">
```
- Pros: Fixed height, no layout shift
- Cons: Adds scrollbar (may be confusing)

**Option C: "Show More" Expander**
```xml
<Expander IsExpanded="False" Header="+5 more">
```
- Pros: Clean default, expandable on demand
- Cons: Extra interaction required

**Recommendation:** **Option A** with `MaxHeight="120"` for balance.

---

### Problem 3: Responsive Wrapping

**Issue:** WrapPanel wraps unpredictably based on available width.

**Current Behavior:**
- Modal width: 1080px
- Three strips: ~340px each (after labels and padding)
- Cards: 71px + margin = ~77px each
- Fits 4 cards per row comfortably
- 5th card wraps to second row

**Better Approach:**
- Use `UniformGrid` for predictable layout (always 4 columns, N rows)
- Or make strip width responsive (narrower on small windows)

---

### Problem 4: Visual Density

**Issue:** Cards feel cramped (see FILTER_MODAL_UI_BUGS_PRD for immediate fix).

**Long-term considerations:**
- Should cards be smaller in header (48x64px thumbnails)?
- Should header show "count" badges instead of all sprites?
- Should header be collapsible?

**Design Options:**

**Current:** Show all cards at full size (71x95px)
- Pros: Visual clarity, recognizable
- Cons: Takes space, can overwhelm

**Alternative 1:** Show thumbnails (48x64px)
- Pros: More cards fit, cleaner
- Cons: Harder to identify specific jokers

**Alternative 2:** Show count badges
```
┌──────────────┐
│ JOKERS       │
│  ┌─┐ ┌─┐ ┌─┐ │
│  │5│ │2│ │1│ │  (5 Must, 2 Should, 1 MustNot)
│  └─┘ └─┘ └─┘ │
└──────────────┘
```
- Pros: Extremely compact
- Cons: Lose visual recognition

**Recommendation:** Keep full-size cards, fix spacing (per bug PRD).

---

## Proposed Enhancements

### Enhancement 1: Performance Optimization (HIGH PRIORITY)

#### Lazy Image Loading

```xml
<!-- Add lazy loading behavior -->
<Image Source="{Binding ., Converter={StaticResource ItemNameToSpriteConverter}}"
       Width="71" Height="95"
       Stretch="Uniform">
    <i:Interaction.Behaviors>
        <behaviors:LazyImageLoadBehavior LoadWhenVisible="True"/>
        <behaviors:BalatroCardSwayBehavior />
    </i:Interaction.Behaviors>
</Image>
```

**Behavior Implementation:**
```csharp
public class LazyImageLoadBehavior : Behavior<Image>
{
    public bool LoadWhenVisible { get; set; } = true;

    protected override void OnAttached()
    {
        if (LoadWhenVisible && !IsInViewport(AssociatedObject))
        {
            // Defer loading until control is visible
            AssociatedObject.Loaded += OnLoaded;
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        // Load image now
    }
}
```

**Expected Impact:** 20-30% faster initial render.

---

#### Disable Sway in Compact View

```csharp
// In ItemTemplate, conditionally add sway behavior
<i:Interaction.Behaviors>
    <behaviors:BalatroCardSwayBehavior IsEnabled="{Binding $parent[UserControl].DataContext.EnableCardSway}"/>
</i:Interaction.Behaviors>
```

**User Setting:**
```xml
<CheckBox Content="Enable card sway animations" IsChecked="{Binding EnableCardSway}"/>
```

**Expected Impact:** 50% reduction in animation frame time.

---

### Enhancement 2: Dynamic Height with Max (MEDIUM PRIORITY)

```xml
<!-- BEFORE -->
<Border Background="{StaticResource DarkTealGrey}"
        CornerRadius="6"
        Padding="8"
        Height="80"
        ClipToBounds="False">

<!-- AFTER -->
<Border Background="{StaticResource DarkTealGrey}"
        CornerRadius="6"
        Padding="8"
        MinHeight="80"
        MaxHeight="140"
        ClipToBounds="True">
    <ScrollViewer VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled">
        <!-- Content -->
    </ScrollViewer>
</Border>
```

**Benefits:**
- Shows more content when needed (up to 140px)
- Scrollbar appears if content exceeds 140px
- No overflow breaking layout
- Predictable maximum height

**Apply to all three strips.**

---

### Enhancement 3: Thumbnail Mode (OPTIONAL)

Add user setting to show thumbnails instead of full-size cards.

```csharp
// In ViewModel
public bool UseCompactCardDisplay { get; set; } = false;
```

```xml
<!-- In ItemTemplate -->
<Grid Width="{Binding $parent[UserControl].DataContext.UseCompactCardDisplay, Converter={StaticResource BoolToSizeConverter}, ConverterParameter='48,71'}"
      Height="{Binding $parent[UserControl].DataContext.UseCompactCardDisplay, Converter={StaticResource BoolToSizeConverter}, ConverterParameter='64,95'}">
    <Image Source="..." Stretch="Uniform"/>
</Grid>
```

**Converter:**
```csharp
public class BoolToSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool useCompact && parameter is string sizes)
        {
            var parts = sizes.Split(',');
            return useCompact ? double.Parse(parts[0]) : double.Parse(parts[1]);
        }
        return null;
    }
}
```

---

### Enhancement 4: Count Badges (FUTURE)

For filters with 20+ items, show count badges instead of all sprites.

```xml
<Border Classes="count-badge">
    <StackPanel Orientation="Horizontal" Spacing="5">
        <TextBlock Text="5" Classes="must-count"/>
        <TextBlock Text="2" Classes="should-count"/>
        <TextBlock Text="1" Classes="mustnot-count"/>
    </StackPanel>
</Border>
```

**Threshold:** Show sprites if <=10 items, else show count badges.

---

## Implementation Plan

### Phase 1: Fix Immediate Bugs (30 minutes)
- See [FILTER_MODAL_UI_BUGS_PRD.md](./FILTER_MODAL_UI_BUGS_PRD.md)
- Fix ItemWidth/ItemHeight mismatch
- Adjust margins for proper spacing

### Phase 2: Performance Optimization (2 hours)
- [ ] Implement LazyImageLoadBehavior
- [ ] Add EnableCardSway setting
- [ ] Test with 20+ card filter
- [ ] Measure frame time improvement

### Phase 3: Dynamic Height (1 hour)
- [ ] Change `Height="80"` to `MinHeight="80" MaxHeight="140"`
- [ ] Add ScrollViewer to each strip
- [ ] Test wrapping with many cards
- [ ] Ensure scrollbar styling matches global theme

### Phase 4: Thumbnail Mode (Optional, 2 hours)
- [ ] Add UseCompactCardDisplay setting
- [ ] Create BoolToSizeConverter
- [ ] Update ItemTemplates
- [ ] Test visual clarity at 48x64px

---

## Acceptance Criteria

### Performance
- [ ] Initial render < 100ms (currently ~150ms with 20 cards)
- [ ] Animation frame time < 2ms (currently ~5ms)
- [ ] Memory usage < 300KB per modal (currently ~500KB)
- [ ] Smooth scrolling when using ScrollViewer

### Layout
- [ ] All cards visible (no unexpected clipping)
- [ ] Wrapping predictable and consistent
- [ ] 6-8px spacing between cards
- [ ] Max height prevents excessive vertical growth

### Responsiveness
- [ ] Works at minimum window width (1080px)
- [ ] Adapts to window resize
- [ ] No horizontal scrolling needed
- [ ] Vertical scrolling smooth with custom scrollbar

---

## Success Metrics

- ✅ 30% faster initial render
- ✅ 50% reduction in animation overhead (with sway disabled)
- ✅ Zero clipped content scenarios
- ✅ User-reported layout feels "spacious and organized"

---

## Testing Plan

### Manual Tests

1. **Performance Test**
   - Create filter with 25 items (10 jokers, 10 vouchers, 5 consumables)
   - Measure frame time during initial render
   - Verify smooth animations

2. **Layout Test**
   - Test with 1, 5, 10, 20 items per strip
   - Verify no clipping at any count
   - Check scrollbar appearance at >MaxHeight

3. **Responsive Test**
   - Resize window from 1080px to 1920px
   - Verify cards reflow correctly
   - Check spacing remains consistent

---

## Timeline

- **Phase 1 (Bug fixes):** 30 minutes
- **Phase 2 (Performance):** 2 hours
- **Phase 3 (Dynamic height):** 1 hour
- **Phase 4 (Thumbnail mode):** 2 hours (optional)

**Total:** 3.5 - 5.5 hours

---

## Related PRDs

- [FILTER_MODAL_UI_BUGS_PRD.md](./FILTER_MODAL_UI_BUGS_PRD.md) - Immediate spacing fixes
- [SCROLLBAR_STANDARDIZATION_PRD.md](./SCROLLBAR_STANDARDIZATION_PRD.md) - Scrollbar in strips

---

**END OF PRD**
