# Product Requirements Document: Sticker Compositing System

## Overview
Implement a visual compositing system for Balatro card overlays that renders multiple transparent PNG layers in proper Z-order. The system must follow KISS principles - the visual layer draws what it's told without caring about business logic conflicts.

## Visual Layer Architecture

### Image Stack Order (bottom to top)
1. **ItemImage** - Base card sprite (joker/voucher/tarot/etc.)
2. **EditionImage** - Edition overlay (foil/holo/polychrome/negative) - ✅ ALREADY IMPLEMENTED
3. **Sticker Layers** - Multiple sticker overlays (eternal/perishable/rental) - ⚠️ TO IMPLEMENT
4. **SoulFaceImage** - Legendary joker face overlay - ✅ ALREADY IMPLEMENTED

### Design Principles
- **KISS (Keep It Simple, Stupid)** - Visual layer renders separate transparent PNGs stacked in Grid
- **No business logic in view** - Don't validate conflicts (e.g., eternal + perishable)
- **Separate Image elements** - Each overlay is a distinct `<Image>` in XAML
- **Proper Z-order** - Grid children stack in document order (first child = bottom layer)
- **Transparent PNGs** - All overlays use transparency, stacking creates final composite

## Data Model

### Current Implementation (KEEP THIS)
```csharp
// SelectableItem.cs - existing working code
private List<string>? _stickers;
public List<string>? Stickers
{
    get => _stickers;
    set
    {
        if (_stickers != value)
        {
            _stickers = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StickersImage)); // This needs to change
        }
    }
}
```

### Current Problem
```csharp
// CURRENT: Only shows FIRST sticker
public IImage? StickersImage
{
    get
    {
        if (Stickers == null || Stickers.Count == 0)
            return null;
        return Services.SpriteService.Instance.GetStickerImage(Stickers[0]); // ❌ Stickers[0] only!
    }
}
```

### Required Solution
Replace single `StickersImage` property with **separate computed properties for each sticker type**:

```csharp
// NEW: Separate image property for each sticker type
public IImage? EternalStickerImage
{
    get
    {
        if (Stickers == null || !Stickers.Contains("eternal"))
            return null;
        return Services.SpriteService.Instance.GetStickerImage("eternal");
    }
}

public IImage? PerishableStickerImage
{
    get
    {
        if (Stickers == null || !Stickers.Contains("perishable"))
            return null;
        return Services.SpriteService.Instance.GetStickerImage("perishable");
    }
}

public IImage? RentalStickerImage
{
    get
    {
        if (Stickers == null || !Stickers.Contains("rental"))
            return null;
        return Services.SpriteService.Instance.GetStickerImage("rental");
    }
}
```

### Property Change Notifications
When `Stickers` property changes, must notify ALL sticker image properties:
```csharp
set
{
    if (_stickers != value)
    {
        _stickers = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(EternalStickerImage));
        OnPropertyChanged(nameof(PerishableStickerImage));
        OnPropertyChanged(nameof(RentalStickerImage));
    }
}
```

## XAML Implementation

### Current XAML (ConfigureFilterTab.axaml)
```xml
<!-- CURRENT: Lines ~393-407 -->
<Grid Width="64" Height="85">
    <Image Source="{Binding ItemImage}" Width="64" Height="85"/>
    <Image Source="{Binding EditionImage}" Width="64" Height="85"/>
    <Image Source="{Binding StickersImage}" Width="64" Height="85"/>  <!-- ❌ Single sticker only -->
    <Image Source="{Binding SoulFaceImage}" Width="64" Height="85"/>
</Grid>
```

### Required XAML
```xml
<!-- NEW: Separate Image element for each sticker type -->
<Grid Width="64" Height="85">
    <!-- Layer 1: Base card sprite -->
    <Image Source="{Binding ItemImage}" Width="64" Height="85"/>

    <!-- Layer 2: Edition overlay (foil/holo/poly/negative) -->
    <Image Source="{Binding EditionImage}" Width="64" Height="85"/>

    <!-- Layer 3a: Eternal sticker (cannot destroy) -->
    <Image Source="{Binding EternalStickerImage}" Width="64" Height="85"/>

    <!-- Layer 3b: Perishable sticker (expires after X rounds) -->
    <Image Source="{Binding PerishableStickerImage}" Width="64" Height="85"/>

    <!-- Layer 3c: Rental sticker (costs money each round) -->
    <Image Source="{Binding RentalStickerImage}" Width="64" Height="85"/>

    <!-- Layer 4: Soul face overlay (legendary jokers) -->
    <Image Source="{Binding SoulFaceImage}" Width="64" Height="85"/>
</Grid>
```

### Why This Works
- Grid stacks children in document order (first child = bottom)
- All images same size (64x85) and fully overlapping
- Transparent PNGs allow layers to show through
- Null/empty sources render nothing (no visual impact)
- No code changes needed for layout - pure data binding

## Implementation Tasks

### Task 1: Update SelectableItem.cs
**File:** `x:\BalatroSeedOracle\src\Models\SelectableItem.cs`

**Changes:**
1. **REMOVE** the `StickersImage` property entirely (lines 128-136)
2. **ADD** three new computed properties:
   - `EternalStickerImage` - checks if "eternal" in Stickers list
   - `PerishableStickerImage` - checks if "perishable" in Stickers list
   - `RentalStickerImage` - checks if "rental" in Stickers list
3. **UPDATE** the Stickers setter to notify all three new properties

### Task 2: Update XAML Templates
**Files to update:**
- `x:\BalatroSeedOracle\src\Components\FilterTabs\ConfigureFilterTab.axaml` (line ~398)
- `x:\BalatroSeedOracle\src\Components\ResponsiveCard.axaml` (line ~118-124) - if it has stickers
- Any other XAML files that render SelectableItem/FilterItem cards

**Changes:**
1. **REPLACE** single `<Image Source="{Binding StickersImage}"/>`
2. **WITH** three separate Image elements (see Required XAML above)

### Task 3: Verify SpriteService
**File:** Check that `Services.SpriteService.Instance.GetStickerImage()` works correctly

**Expected behavior:**
- `GetStickerImage("eternal")` returns eternal sticker PNG
- `GetStickerImage("perishable")` returns perishable sticker PNG
- `GetStickerImage("rental")` returns rental sticker PNG
- Returns null if sticker sprite not found (graceful degradation)

## Testing Requirements

### Visual Testing
1. **Single sticker** - Card with only "eternal" should show eternal sticker
2. **Multiple stickers** - Card with ["eternal", "perishable"] should show BOTH overlays
3. **All stickers** - Card with ["eternal", "perishable", "rental"] should show ALL THREE
4. **No stickers** - Card with null/empty Stickers should show no stickers
5. **Sticker + Edition** - Card with stickers AND edition should show both layers
6. **Legendary with stickers** - Soul joker with stickers should show face on top

### Z-Order Testing
Verify stacking order by checking overlap:
- Edition behind stickers (stickers should be more prominent)
- All stickers behind soul face (face should be topmost)
- Base card behind everything

### Performance Testing
- No performance regression with additional Image elements
- Null bindings don't cause layout thrashing

## Success Criteria

✅ Multiple stickers render simultaneously on single card
✅ Z-order is correct (base → edition → stickers → face)
✅ KISS principle maintained (visual layer doesn't validate business logic)
✅ No code duplication (computed properties are DRY)
✅ Data binding works correctly (property changes trigger UI updates)
✅ Build succeeds without errors
✅ Existing SelectableItem.Stickers property unchanged (List<string>?)

## Non-Requirements

❌ Don't validate sticker conflicts (e.g., eternal + perishable shouldn't coexist)
❌ Don't add business logic to SelectableItem (keep it as dumb data model)
❌ Don't change the Stickers property type (keep List<string>?)
❌ Don't add animation logic in this PR (future enhancement)

## Future Enhancements (Out of Scope)

- Ambient wobble animation for stickers (like soul face)
- Sticker glow effects
- Sticker positioning variants (top-left, top-right, center)
- Sticker size variants (small, medium, large)
- Business logic validation (warn if conflicting stickers assigned)

## References

- Balatro source: `external/Balatro/card.lua` - card rendering logic
- Current implementation: `src/Models/SelectableItem.cs` lines 113-136
- Current XAML: `src/Components/FilterTabs/ConfigureFilterTab.axaml` line ~398
- Sprite service: `src/Services/SpriteService.cs` (verify GetStickerImage method)
