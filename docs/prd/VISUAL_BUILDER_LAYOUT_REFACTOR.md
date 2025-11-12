# PRD: Visual Builder Layout Refactor - Search Bar Relocation & Card Sizing

## Executive Summary
Fix critical layout issues in Visual Builder:
1. **Move search bar** from top center to LEFT COLUMN (between filter name badge and category tabs)
2. **Fix card sizing** - Cards are too large, causing 5-column layout to overflow
3. **Add space for tab selection indicator** - Red Balatro triangle indicator needs room on far left
4. **Study existing tab selection patterns** - Use the same pattern as other category switchers in the codebase

## Problem Statement

### Current Issues:
1. **Search bar location wastes space** - Top center position inefficient
2. **Cards too large** - Can't fit 5 columns properly in item shelf
3. **No space for selection indicator** - Red triangle tab indicator has nowhere to render
4. **Inconsistent patterns** - Not using existing SelectableCategoryButton pattern

### Visual Problems:
```
[Current Layout - BROKEN]
┌─────────────────────────────────────────────────┐
│         [Search Bar Here - Wasted Space]        │
├──────────┬──────────────────────────────────────┤
│ Category │  Item Shelf (cards too big!)         │
│  Tabs    │  [Card] [Card] [Card] [Card] [???]   │
│ (no ▶)  │                                       │
└──────────┴──────────────────────────────────────┘

[Target Layout - FIXED]
┌──────────┬──────────────────────────────────────┐
│ Filter   │  Item Shelf (properly sized cards)   │
│  Badge   │  [Card] [Card] [Card] [Card] [Card]  │
├──────────┤                                       │
│ [Search] │  Drop Zones (MUST/SHOULD/BANNED)     │
├──────────┤                                       │
│▶Category │                                       │
│ Category │                                       │
│ Category │                                       │
└──────────┴──────────────────────────────────────┘
```

## Solution Architecture

### Part 1: Move Search Bar to Left Column

**Current location** (VisualBuilderTab.axaml, approximate line 150):
```xml
<!-- Top row with search bar (REMOVE THIS) -->
<Grid Grid.Row="0" ColumnDefinitions="*,Auto,*">
    <TextBox Grid.Column="1"
             Watermark="Search items..."
             Text="{Binding SearchFilter, Mode=TwoWay}"/>
</Grid>
```

**New location** (insert after filter name badge, before category tabs):
```xml
<Grid RowDefinitions="Auto,8,Auto,8,*">
    <!-- Row 0: Filter Name Badge (existing) -->
    <Border Grid.Row="0"
            Background="{StaticResource DarkBackground}"
            BorderThickness="0"
            CornerRadius="12"
            Padding="8,6"
            MaxWidth="160">
        <TextBlock Text="{Binding FilterName, FallbackValue='New Filter'}"
                   FontFamily="{StaticResource BalatroFont}"
                   FontSize="13"
                   Foreground="{StaticResource Gold}"
                   HorizontalAlignment="Center"
                   TextTrimming="CharacterEllipsis"/>
    </Border>

    <!-- Row 2: Search Box (NEW LOCATION) -->
    <TextBox Grid.Row="2"
             Name="SearchBox"
             Watermark="Search items..."
             Text="{Binding SearchFilter, Mode=TwoWay}"
             Background="{StaticResource DarkBackground}"
             BorderThickness="0"
             CornerRadius="8"
             Foreground="{StaticResource White}"
             Padding="8,6"
             VerticalContentAlignment="Center"
             HorizontalAlignment="Stretch"
             MaxWidth="160"
             FontSize="12"/>

    <!-- Row 4: Category Tabs (pushed down to make room) -->
    <StackPanel Grid.Row="4" Spacing="4">
        <!-- Category buttons here -->
    </StackPanel>
</Grid>
```

### Part 2: Fix Card Sizing in Item Shelf

**Current problem:** Cards at native 71x95 size + margins make 5 columns too wide.

**Analysis needed:**
- Item shelf container width
- Card width (71px) + margins + spacing
- Target: 5 columns should fit comfortably

**Potential solutions:**

**Option A: Reduce card scale**
```xml
<components:FilterItemCard
    RenderTransform="scale(0.9)"  <!-- 10% smaller -->
    RenderTransformOrigin="0.5,0.5"/>
```

**Option B: Tighter spacing**
Reduce UniformGrid spacing from current value to smaller gap:
```xml
<UniformGrid Columns="5"
             HorizontalSpacing="4"  <!-- Reduce from 8 -->
             VerticalSpacing="4"/>
```

**Option C: Container width adjustment**
Increase item shelf container width to accommodate 5 full-size cards:
```xml
<ScrollViewer MinWidth="400"  <!-- Increase minimum width -->
```

### Part 3: Add Space for Tab Selection Indicator

**Study existing implementation:**
The project already has `SelectableCategoryButton` at `src/Controls/Navigation/SelectableCategoryButton.cs` with red triangle indicator.

**Key features:**
- `IsSelected` property triggers `:selected` pseudoclass
- Red triangle arrow is part of button template
- Bouncing animation in XAML
- Arrow positioned on left side of button

**Current category tabs** (need to migrate to SelectableCategoryButton):
```xml
<!-- CURRENT (broken) -->
<Button Content="Joker"
        Command="{Binding SelectCategoryCommand}"
        CommandParameter="Joker"/>

<!-- TARGET (fixed) -->
<nav:SelectableCategoryButton
    Content="Joker"
    Category="Joker"
    IsSelected="{Binding IsJokerSelected}"
    Command="{Binding SelectCategoryCommand}"
    CommandParameter="Joker"/>
```

**ViewModel updates needed:**
```csharp
// Add selection properties
public bool IsFavoritesSelected => SelectedCategory == "Favorites";
public bool IsJokerSelected => SelectedCategory == "Joker";
public bool IsConsumableSelected => SelectedCategory == "Consumable";
public bool IsSkipTagSelected => SelectedCategory == "SkipTag";
public bool IsBossSelected => SelectedCategory == "Boss";
public bool IsVoucherSelected => SelectedCategory == "Voucher";
public bool IsStandardCardSelected => SelectedCategory == "StandardCard";

public string SelectedCategory
{
    get => _selectedCategory;
    set
    {
        if (SetProperty(ref _selectedCategory, value))
        {
            // Notify all selection properties
            OnPropertyChanged(nameof(IsFavoritesSelected));
            OnPropertyChanged(nameof(IsJokerSelected));
            // ... etc

            UpdateFilteredItems();
        }
    }
}
```

### Part 4: Reference Existing Patterns

**Files to study:**
- `src/Controls/Navigation/SelectableCategoryButton.cs` - Button with selection indicator
- `src/Controls/Navigation/SelectableCategoryButton.axaml` - Button template and arrow styling
- `src/Components/Shared/FilterCategoryNav.axaml` - May already use SelectableCategoryButton pattern

**Key pattern to follow:**
1. Use SelectableCategoryButton control (DON'T recreate it)
2. Bind `IsSelected` to ViewModel property
3. Selection indicator (red triangle) is intrinsic to button template
4. No external positioning logic needed

## Layout Measurements

### Calculate Required Widths:

**Card dimensions:**
- Native: 71px width × 95px height
- With label: 95px total width (71px card + margins)

**5-column layout:**
- 5 cards × 95px = 475px minimum width
- Add spacing: 4 gaps × 8px = 32px
- Add container padding: 16px
- **Total minimum: 523px**

**Left column:**
- Filter name badge: 160px max
- Search box: 160px max
- Category buttons: 160px typical
- Tab indicator: 20px (red triangle)
- **Total: ~180px with indicator space**

## Testing Requirements
1. Search bar appears in left column below filter name badge
2. Category tabs appear below search bar
3. Red triangle indicator shows on selected category
4. 5 cards fit horizontally in item shelf without overflow
5. Cards are properly sized (not too large)
6. Tab indicator animates (bounces) on selection
7. All existing drag-and-drop functionality works
8. Layout responsive to window resizing

## Success Criteria
- Search bar in left column (intuitive location)
- 5-column card layout fits properly
- Red triangle tab indicator visible and functional
- Uses existing SelectableCategoryButton pattern
- No custom positioning logic for indicator
- Clean MVVM architecture maintained

## File Locations
- **Main view**: `src/Components/FilterTabs/VisualBuilderTab.axaml`
- **ViewModel**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs`
- **Reference button**: `src/Controls/Navigation/SelectableCategoryButton.cs` and `.axaml`
- **Reference nav**: `src/Components/Shared/FilterCategoryNav.axaml`

## Implementation Steps
1. Study SelectableCategoryButton implementation
2. Move search bar to left column Grid
3. Update category tabs to use SelectableCategoryButton
4. Add selection properties to ViewModel
5. Adjust card sizing/spacing for 5-column layout
6. Test all interactions and animations
7. Verify no regressions in drag-and-drop

## Implementation Notes
- **DON'T** recreate SelectableCategoryButton - use existing control
- **DON'T** add custom positioning logic - use intrinsic template features
- **DO** study existing implementations before coding
- **DO** measure actual widths to ensure 5 columns fit
- **DO** maintain MVVM separation (no view logic in code-behind)

## Time Estimate
2-3 hours if existing patterns are followed correctly. Could take 23 hours if anti-patterns are used (external arrow positioning, etc).
