# Scrollbar Positioning Fix - PRD

## Problem Statement
The red thermometer-style scrollbar in the Filter Builder modal's item shelf is currently overlapping/hanging outside the border and covering cards underneath it. This wastes screen real estate and creates a poor UX.

## Visual Analysis (from screenshot)
- **Location**: Filter Builder modal → middle column (item shelf with joker cards)
- **Current behavior**: Scrollbar appears to overlap the border edge and cards hide underneath
- **Issue**: Scrollbar doesn't take up layout space, causing cards to be hidden

## Requirements

### 1. Scrollbar Positioning
- [ ] Scrollbar must be INSIDE the border, not overlapping or hanging outside
- [ ] Scrollbar should be positioned at the RIGHT EDGE of the item shelf area
- [ ] Scrollbar must be within the bounds of the containing Border/ScrollViewer

### 2. Layout Space Allocation
- [ ] Scrollbar MUST take up actual layout space
- [ ] Cards must be "shoved over" by the scrollbar's width
- [ ] NO cards should be hidden underneath the scrollbar
- [ ] Grid of cards should resize to accommodate scrollbar width

### 3. Visual Consistency
- [ ] Maintain the red thermometer style (Balatro visual theme)
- [ ] Scrollbar should be visible and functional
- [ ] No visual overlap with border or cards

## Technical Implementation

### File to Modify
`x:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml`

### Key Areas
1. **ScrollViewer** (line ~491): Contains the item shelf grid
   - Current: Scrollbar likely overlays content
   - Fix: Ensure scrollbar is part of layout (not overlay)

2. **Possible Solutions**:
   - Check `ScrollViewer.VerticalScrollBarVisibility` property
   - Verify scrollbar is not using `Overlay` mode (use `Auto` or `Visible` instead)
   - Ensure `Padding` or `Margin` accommodates scrollbar width
   - Might need to adjust `UniformGrid` width or column calculations

### Expected Properties
```xaml
<ScrollViewer VerticalScrollBarVisibility="Auto"  <!-- NOT "Overlay" -->
              HorizontalScrollBarVisibility="Disabled"
              Padding="0,0,0,0">  <!-- May need right padding -->
```

### Grid Adjustments
The UniformGrid inside the ScrollViewer may need:
- Explicit width calculation to account for scrollbar
- Padding on the right side
- Margin adjustments

## Success Criteria
- [ ] Scrollbar appears INSIDE the border (not overlapping edge)
- [ ] Scrollbar is at the RIGHT EDGE of the item shelf area
- [ ] Cards are NOT hidden under scrollbar
- [ ] Grid resizes properly when scrollbar appears/disappears
- [ ] Visual appearance matches Balatro red thermometer style
- [ ] No wasted screen space

## Context
This is part of MVP polish - we're fighting for every pixel of screen real estate. The current overlapping scrollbar wastes space and creates confusion.

## Notes
- User is very particular about layout precision
- This is a visual/UX issue that requires careful attention to Avalonia layout system
- ScrollViewer in Avalonia can use "Overlay" mode (scrollbar overlays content) or standard mode (scrollbar takes layout space)
- We want standard mode so cards don't hide
