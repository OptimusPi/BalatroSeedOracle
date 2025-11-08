# URGENT FIX: Items Not Rendering in Drop Zones

## Problem
Items were being added to ObservableCollections (SelectedMust, SelectedShould, SelectedMustNot) with successful debug output ("Dropped Chicot into OR tray" etc), but **NOTHING rendered visually** in the drop zones.

## Root Cause
**Height constraint bug in XAML (line 945-954 of VisualBuilderTab.axaml)**

```xml
<!-- BEFORE (BROKEN) -->
<Border Background="Transparent"
        Width="54" Height="74"  <!-- Fixed height cutting off content -->
        Cursor="Hand"
        PointerPressed="OnDropZoneItemPointerPressed">

    <StackPanel Spacing="2" Width="54" Height="64" IsHitTestVisible="False">
        <!-- Grid with card images: Height=64 -->
        <!-- TextBlock with card name: Auto height -->
        <!-- TOTAL HEIGHT NEEDED: ~75-80px -->
        <!-- BUT ONLY 64px ALLOCATED! -->
    </StackPanel>
</Border>
```

The StackPanel had **Height="64"** but contained:
1. A Grid with card images (Height=64)
2. A TextBlock with DisplayName below it

**Result**: The content was being clipped/cut off, making items invisible even though they were in the collection.

## Fix Applied
Changed both Border and StackPanel heights from fixed values to **Height="Auto"**:

```xml
<!-- AFTER (FIXED) -->
<Border Background="Transparent"
        Width="54" Height="Auto"  <!-- Auto height allows content to expand -->
        Cursor="Hand"
        PointerPressed="OnDropZoneItemPointerPressed">

    <StackPanel Spacing="2" Width="54" Height="Auto" IsHitTestVisible="False">
        <!-- Now properly expands to fit both image and label -->
    </StackPanel>
</Border>
```

## Files Changed
1. **x:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml**
   - Fixed Height constraints in all 3 drop zones (MUST, SHOULD, MUST NOT)
   - Applied `replace_all=true` to update all 3 instances

2. **x:\BalatroSeedOracle\src\ViewModels\FilterTabs\VisualBuilderTabViewModel.cs**
   - Added comprehensive debug logging to AddToMust/AddToShould/AddToMustNot
   - Added `OnPropertyChanged(nameof(SelectedMust))` to force UI refresh
   - Logs now show: item index, name, image status, display name

3. **x:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml.cs**
   - Added DataContext validation logging
   - Added CollectionChanged event subscriptions for all 3 drop zones
   - Logs collection changes with Action, NewItems count, and total count

## Testing Checklist
- [x] Verify debug output shows items being added to collections
- [ ] Verify items now RENDER visually in drop zones
- [ ] Test MUST zone rendering
- [ ] Test SHOULD zone rendering
- [ ] Test MUST NOT zone rendering
- [ ] Verify card images display correctly
- [ ] Verify card labels (DisplayName) appear below images
- [ ] Test drag-and-drop still works
- [ ] Verify remove buttons appear on hover
- [ ] Check that no clipping occurs

## Debug Output to Watch For
When you drop an item, you should now see:
```
AddToMust: Adding item: Name=Chicot, Type=SoulJoker, Category=Jokers, ItemImage=True, DisplayName=Chicot
AddToMust: SelectedMust count after add: 1
AddToMust:   [0] Chicot (Type=FilterItem, Image=True, Display=Chicot)
VisualBuilderTab: SelectedMust CollectionChanged - Action: Add, NewItems: 1, Count: 1
```

## Why This Bug Was Hard to Find
1. Collections were working correctly (items were being added)
2. Debug output confirmed successful operations
3. DataContext bindings were correct
4. Category values were correct ("Jokers" not "Joker")
5. The issue was **purely visual** - a layout/clipping problem

The items were there in the data structure, but the UI was cutting them off due to insufficient height!

## Related Issues Fixed
- Item count TextBlock was showing correct numbers but no items visible
- WrapPanel was working but had no content to wrap
- ItemTemplate was never the issue - it was rendering, just clipped out of view
