# PRD: Remove MUST-NOT Drop Zone & Use Debuffed Button with IsInvertedFilter

## Executive Summary
Remove the unused MUST-NOT drop zone from Visual Builder. Debuffed items (marked with red X) go into the MUST drop zone with `IsInvertedFilter=true` to represent inverted logic ("must NOT have this"). The Negative button currently uses the Debuffed sprite incorrectly - this will be fixed in the Negative Edition PRD. Keep the Debuffed button for marking items as inverted MUST clauses.

## Problem Statement

### Current Issues:
1. **MUST-NOT drop zone is unused** - Wastes screen space, confusing UX
2. **Negative button uses wrong sprite** - Shows Debuffed sprite (red X) instead of inverted colors
3. **Need Debuffed button functionality** - Users need to mark "don't want this" items
4. **Inverted logic needs visual feedback** - Red X overlay perfect for MUST-NOT

### Correct Architecture:
- **MUST drop zone** contains both regular AND debuffed items
- **Regular items**: Normal appearance, `IsInvertedFilter=false` → "Must have X"
- **Debuffed items**: Red X overlay, `IsInvertedFilter=true` → "Must NOT have X"
- **One drop zone** handles both positive and negative logic
- **IsInvertedFilter property** already exists, just needs to be used correctly

## Solution Architecture

### Part 1: Remove MUST-NOT Drop Zone

**Files to modify:**
- `src/Components/FilterTabs/VisualBuilderTab.axaml` - Remove MUST-NOT drop zone UI
- `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` - Remove MUST-NOT collection and logic

**XAML to remove:**
```xml
<!-- MUST-NOT drop zone section (entire section) -->
<Border Grid.Row="2" ...>
    <Grid RowDefinitions="Auto,*">
        <TextBlock Text="MUST NOT" .../>
        <ItemsRepeater Items="{Binding MustNotItems}" .../>
    </Grid>
</Border>
```

**ViewModel properties to remove:**
```csharp
public ObservableCollection<FilterItem> MustNotItems { get; }
```

**Logic to remove:**
- All drag-and-drop handling for MUST-NOT zone
- MUST-NOT item removal commands
- MUST-NOT clause generation in filter export

### Part 2: Create Separate Debuffed Button (Keep Negative for Edition)

**Current situation:**
- Negative button incorrectly shows Debuffed sprite (red X)
- Need BOTH Negative (for color inversion) AND Debuffed (for inverted filter logic)

**Add new Debuffed button** (VisualBuilderTab.axaml):
```xml
<!-- NEW: Debuffed button (separate from Negative Edition) -->
<Button Classes="action-button"
        ToolTip.Tip="Toggle Debuffed (Inverted Logic)"
        Command="{Binding ToggleDebuffedCommand}">
    <Image Source="{Binding DebuffedIconPath}"
           Width="32" Height="32"/>
</Button>

<!-- EXISTING: Negative button (will be fixed to show inverted sprite) -->
<Button Classes="action-button"
        ToolTip.Tip="Negative Edition (Inverted Colors)"
        Command="{Binding SetEditionCommand}"
        CommandParameter="Negative">
    <Image Source="{Binding NegativeEditionIcon}"
           Width="32" Height="32"/>
</Button>
```

**Icon paths:**
- **Debuffed sprite**: Currently being used incorrectly for Negative
- **Negative sprite**: Will be inverted Joker card (see Negative Edition PRD)

### Part 3: Add Debuffed Property to FilterItem

**Update SelectableItem.cs:**

```csharp
private bool _isDebuffed;
public bool IsDebuffed
{
    get => _isDebuffed;
    set
    {
        if (_isDebuffed != value)
        {
            _isDebuffed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DebuffedImage));
        }
    }
}

public IImage? DebuffedImage
{
    get
    {
        if (!IsDebuffed)
            return null;

        return Services.SpriteService.Instance.GetEnhancementImage("debuffed");
    }
}
```

### Part 4: Add Debuffed Overlay to FilterItemCard

**Update FilterItemCard.axaml** (add after other overlays):

```xml
<!-- Debuffed overlay (red X for MUST-NOT items) -->
<Image Source="{Binding DebuffedImage}"
       Width="71"
       Height="95"
       Stretch="Uniform"
       HorizontalAlignment="Center"
       VerticalAlignment="Center"
       IsVisible="{Binding DebuffedImage, Converter={x:Static ObjectConverters.IsNotNull}}"
       IsHitTestVisible="False"
       ZIndex="100"/>  <!-- Higher than other overlays -->
```

### Part 5: Update ToggleDebuffed Command to Set IsInvertedFilter

**Update VisualBuilderTabViewModel.cs:**

```csharp
[RelayCommand]
public void ToggleDebuffed()
{
    // Toggle debuffed state on all items in shelf
    foreach (var group in GroupedItems)
    {
        foreach (var item in group.Items)
        {
            item.IsDebuffed = !item.IsDebuffed;  // Toggle red X overlay
        }
    }

    // Also toggle on drop zone items
    foreach (var item in MustHaveItems.Concat(ShouldHaveItems).Concat(BannedItems))
    {
        item.IsDebuffed = !item.IsDebuffed;
    }
}
```

### Part 6: Link IsDebuffed to IsInvertedFilter on Drop

When a Debuffed item is dragged to MUST zone, set `IsInvertedFilter=true`:

**Update drag-and-drop handler:**

```csharp
private void OnMustHaveItemDropped(FilterItem item)
{
    // Set inverted filter flag based on debuffed state
    item.IsInvertedFilter = item.IsDebuffed;

    MustHaveItems.Add(item);

    // Red X overlay persists if IsDebuffed=true
}
```

### Part 7: Update Filter Export Logic

When exporting filter, respect `IsInvertedFilter` to generate MUST-NOT clauses:

```csharp
// Filter export logic
foreach (var item in MustHaveItems)
{
    if (item.IsInvertedFilter)
    {
        // Generate MUST-NOT clause
        filter.AddMustNotHave(item);
    }
    else
    {
        // Generate MUST clause
        filter.AddMustHave(item);
    }
}
```

## Visual Behavior

### Item Shelf:
- Click Debuffed button → All items show red X overlay
- Items marked with `IsDebuffed=true`

### Drop into MUST Zone:
- Regular item (no red X) → `IsInvertedFilter=false` → "Must have X"
- Debuffed item (red X) → `IsInvertedFilter=true` → "Must NOT have X"
- Both appear in same MUST drop zone
- Red X overlay shows which are inverted

### Filter Export:
- `IsInvertedFilter=false` items → Generate positive MUST clauses
- `IsInvertedFilter=true` items → Generate negative MUST-NOT clauses

## Testing Requirements
1. **MUST-NOT drop zone completely removed** from UI
2. **Debuffed button** appears in action buttons (separate from Negative Edition)
3. **Click Debuffed** → All items show red X overlay immediately
4. **Debuffed overlay** renders above all other overlays (highest ZIndex)
5. **Drag regular item to MUST** → `IsInvertedFilter=false`, no red X
6. **Drag debuffed item to MUST** → `IsInvertedFilter=true`, red X persists
7. **Both regular and debuffed items** appear in same MUST zone
8. **Filter export** generates correct clauses based on `IsInvertedFilter`
9. **Toggle Debuffed off** → Red X disappears, `IsInvertedFilter` updates

## Success Criteria
- MUST-NOT drop zone removed, screen space reclaimed
- Debuffed button toggles red X overlay on items
- `IsInvertedFilter` property correctly set on drop
- MUST zone contains both positive and negative logic items
- Red X overlay clearly distinguishes inverted items
- Filter export generates correct MUST and MUST-NOT clauses
- No confusion between Debuffed (inverted logic) and Negative (color inversion)

## File Locations
- **View**: `src/Components/FilterTabs/VisualBuilderTab.axaml` (remove MUST-NOT zone, add Debuffed button)
- **ViewModel**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` (remove MUST-NOT logic, add SetDebuffed)
- **Model**: `src/Models/SelectableItem.cs` (add IsDebuffed property)
- **Card**: `src/Components/FilterItemCard.axaml` (add Debuffed overlay)
- **Sprites**: Check `Assets/Sprites/Enhancements/` for Debuffed sprite

## Implementation Notes
- **MUST zone is unified** - handles both positive and inverted logic
- **IsInvertedFilter property already exists** - just needs to be wired up correctly
- **Red X overlay provides visual feedback** - users can see which items are inverted
- **Filter export must respect IsInvertedFilter** - generate MUST-NOT clauses accordingly
- **Debuffed ≠ Negative** - Debuffed is inverted filter logic, Negative is color inversion Edition
- **Remove MustNotItems collection entirely** - no longer needed with unified MUST zone

## Architecture Summary

```
Before (Broken):
┌─────────────┬─────────────┐
│ MUST Zone   │ MUST-NOT    │  ← Two separate zones
│  [Joker]    │  Zone       │
│             │  [Baron]    │  ← Unused, wastes space
└─────────────┴─────────────┘

After (Correct):
┌─────────────────────────────┐
│ MUST Zone (Unified)         │
│  [Joker]                    │  ← IsInvertedFilter=false (want)
│  [Baron with red X]         │  ← IsInvertedFilter=true (don't want)
└─────────────────────────────┘

Filter Export:
- Joker → MUST clause (positive)
- Baron → MUST-NOT clause (inverted)
```

## Time Estimate
2-3 hours to safely remove MUST-NOT zone, add Debuffed overlay, and wire up IsInvertedFilter.
