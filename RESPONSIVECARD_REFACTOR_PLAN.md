# ResponsiveCard Refactor Plan

## Problem Statement

The Visual Builder item shelf uses inline DataTemplates with duplicated XAML (4 copies for shelf/MUST/SHOULD/MUSTNOT). This causes:
- Maintenance hell (changes need to be made in 4 places)
- Inconsistent behavior between zones
- No encapsulation or reusability
- Messy code with layered Images and Borders

## The Abandoned Solution: ResponsiveCard

`src/Components/ResponsiveCard.axaml` exists but is NOT used anywhere. It was likely built to solve this exact problem but got abandoned.

## Proposed Solution

Replace inline DataTemplates with ResponsiveCard UserControl.

### Architecture

**ResponsiveCard.axaml** (already exists, needs updates):
- Clean UserControl with proper MVVM
- Handles ItemImage, EditionImage, Stickers, SoulFaceImage layering
- Includes MagneticTiltBehavior and BalatroCardSwayBehavior
- Proper hit testing separation (visual vs hitbox borders)

**Usage in VisualBuilderTab.axaml**:
```xml
<ItemsControl.ItemTemplate>
    <DataTemplate DataType="models:FilterItem">
        <components:ResponsiveCard
            DataContext="{Binding}"
            Width="71" Height="95"/>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

### Files to Modify

1. **ResponsiveCard.axaml** - Already has proper structure, just needs:
   - Update to 71x95 sizing
   - Ensure all behaviors attached correctly
   - Add data binding for FilterItem properties

2. **ResponsiveCard.axaml.cs** - Add:
   - Properties for ItemKey, Category (for drag/drop)
   - Event handlers for drag/drop if needed

3. **VisualBuilderTab.axaml** - Replace 4 DataTemplates:
   - Shelf template (line ~390)
   - MUST zone template (line ~884)
   - SHOULD zone template (line ~1051)
   - MUSTNOT zone template (line ~1218)

   All become: `<components:ResponsiveCard DataContext="{Binding}"/>`

### Benefits

✅ **DRY** - Single source of truth for card rendering
✅ **Encapsulation** - All card logic in one component
✅ **Maintainability** - Changes in one place affect all zones
✅ **Consistency** - Shelf and drop zone cards identical
✅ **Proper MVVM** - UserControl with clear responsibilities

### Risks

⚠️ **Drag/drop** - Need to verify existing OnItemPointerPressed handlers still work
⚠️ **Data binding** - Ensure FilterItem properties bind correctly
⚠️ **Performance** - Creating UserControl instances vs inline templates (negligible)

### Testing Checklist

- [ ] Shelf cards render correctly
- [ ] Hover effect works (MagneticTiltBehavior)
- [ ] Drag and drop works
- [ ] Edition/sticker overlays display correctly
- [ ] Soul joker faces render
- [ ] Drop zone cards render same as shelf
- [ ] CardFlipOnTriggerBehavior works for edition changes
- [ ] No performance regression

### Implementation Steps

1. Update ResponsiveCard.axaml to 71x95 sizing
2. Test ResponsiveCard in isolation (create test XAML)
3. Replace shelf template with ResponsiveCard
4. Test shelf functionality thoroughly
5. Replace drop zone templates one at a time
6. Commit after each successful replacement

### Estimated Time

- 2-3 hours for careful implementation
- Worth it to eliminate technical debt

### Alternative: Keep Current Approach

If this refactor is too risky for Sunday shipping:
- Extract DataTemplate to shared resource
- Reference it 4 times
- Still reduces duplication, less invasive

## Recommendation

**For Sunday shipping:** Extract to shared DataTemplate (30 min, low risk)
**For next week:** Full ResponsiveCard refactor (cleaner, proper solution)
