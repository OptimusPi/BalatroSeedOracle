# Implement FilterItemCarousel for Drop Zones

## Problem
Visual Builder tab has duplicated card templates in 3 drop zones (MUST/SHOULD/AVOID). FilterItemCarousel component exists but isn't used for drop zones.

## Evidence
FilterItemCarousel already created:
- X:\BalatroSeedOracle\src\Components\FilterItemCarousel.axaml
- X:\BalatroSeedOracle\src\Components\FilterItemCarousel.axaml.cs
- Has ItemsSource property
- Uses horizontal scroll with Spacing="-15" for overlap
- Reuses FilterItemCard component

Currently NOT used in VisualBuilderTab.axaml drop zones (lines need to be found).

## Solution
Replace duplicated ItemsControl templates in each drop zone with FilterItemCarousel.

**Before:**
```xml
<ItemsControl ItemsSource="{Binding MustHaveItems}">
    <ItemsControl.ItemsPanel>
        <StackPanel Orientation="Horizontal" Spacing="-15"/>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <components:FilterItemCard DataContext="{Binding}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**After:**
```xml
<components:FilterItemCarousel ItemsSource="{Binding MustHaveItems}"/>
```

## Files
- X:\BalatroSeedOracle\src\Components\FilterTabs\VisualBuilderTab.axaml

## Locations
Find all 3 ItemsControl instances bound to:
- MustHaveItems
- ShouldHaveItems
- AvoidItems

Replace each with FilterItemCarousel.

## Acceptance
- [ ] All 3 drop zones use FilterItemCarousel
- [ ] Cards display correctly (71x95, overlapping)
- [ ] Drag and drop still works
- [ ] Build succeeds
- [ ] Code consolidation: ~30 lines → 3 lines
