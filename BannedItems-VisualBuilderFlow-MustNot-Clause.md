# Banned Items Visual Builder Flow - MustNot[] Clause Implementation

## Executive Summary

Implement a new "Banned Items" clause tray operator that provides an intuitive visual representation for the MustNot[] filter criteria in MotelyJSON filters. This eliminates the confusing "debuffed" button and makes filter logic crystal clear: items in Banned Items trays serialize to MustNot[], ensuring precise seed search criteria.

## Critical Requirements

**ACCURACY IS PARAMOUNT**: Any logic errors will render the entire app useless. Seed searching requires precise, consistent, and standardized behavior at every step.

## 1. Data Model Changes

### 1.1 FilterOperatorItem Extension

**File:** `src/Models/FilterOperatorItem.cs`

Add support for "BannedItems" operator type alongside existing "OR" and "AND":

```csharp
public FilterOperatorItem(string operatorType)
{
    _operatorType = operatorType; // "OR", "AND", or "BannedItems"
    Type = "Operator";
    Name = operatorType;
    DisplayName = operatorType;
    Category = "Operator";
}
```

**Valid OperatorType values:**
- `"OR"` - Blue border, OR logic
- `"AND"` - Blue border, AND logic
- `"BannedItems"` - **Red border**, MustNot[] logic

## 2. UI/UX Implementation

### 2.1 Cycle Button Behavior

**File:** `src/Components/FilterTabs/VisualBuilderTab.axaml.cs`

**Current behavior:** OR ⟲ AND

**New behavior:** OR ⟲ AND ⟲ BannedItems ⟲ OR ⟲ AND ⟲ BannedItems

The rotating arrows button (🔄) cycles through all three operator types in sequence.

### 2.2 Visual Styling - Banned Items Tray

**File:** `src/Components/FilterOperatorControl.axaml`

Create new style for Banned Items operator:

```xml
<!-- Banned Items container - RED theme -->
<Style Selector="Border.operator-card-container.banned">
    <Setter Property="BorderBrush" Value="{StaticResource Red}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Background" Value="{StaticResource DarkBackground}"/>
    <!-- ... same sizing as OR/AND -->
</Style>

<!-- Banned Items header - RED background -->
<Style Selector="Border.operator-header.banned">
    <Setter Property="Background" Value="{StaticResource Red}"/>
    <Setter Property="Padding" Value="12,6"/>
    <Setter Property="CornerRadius" Value="6,6,0,0"/>
</Style>
```

**Visual properties:**
- Border: Red (global Red color resource)
- Header background: Red
- Header text: White, "Banned Items"
- Size/shape: Identical to OR/AND trays
- Fanned cards: Same layout as OR/AND

### 2.3 Debuffed Overlay Rendering

**File:** `src/Components/FilterOperatorControl.axaml`

For items inside BannedItems tray, layer the debuffed sprite ON TOP:

```xml
<DataTemplate>
    <Border Background="Transparent" RenderTransformOrigin="0.5,1.0">
        <!-- ... rotation transforms ... -->
        <Grid Width="50" Height="70">
            <!-- Base card image -->
            <Image Source="{Binding ItemImage}" Stretch="Uniform" IsHitTestVisible="False"/>

            <!-- Soul face overlay for legendary jokers -->
            <Image Source="{Binding SoulFaceImage}" Stretch="Uniform"
                   IsVisible="{Binding SoulFaceImage, Converter={x:Static ObjectConverters.IsNotNull}}"
                   IsHitTestVisible="False"/>

            <!-- DEBUFFED overlay (FULL SIZE) - only for BannedItems tray -->
            <Image Source="{Binding DebuffedOverlayImage}" Stretch="Uniform"
                   IsVisible="{Binding IsInBannedItemsTray}"
                   IsHitTestVisible="False"/>
        </Grid>
    </Border>
</DataTemplate>
```

**Critical notes:**
- Debuffed sprite is FULL SIZE (not a smaller overlay)
- All sprites (edition, seal, sticker, debuffed) are full size with built-in whitespace from sprite sheet
- Debuffed image only renders when `IsInBannedItemsTray` is true

### 2.4 Remove Debuffed Button

**File:** `src/Components/FilterTabs/VisualBuilderTab.axaml`

**DELETE** the debuffed/banned items button entirely (lines 258-267):

```xml
<!-- DELETE THIS ENTIRE BLOCK -->
<Button Command="{Binding ToggleDebuffedCommand}"
        Classes="icon-button"
        ToolTip.Tip="Filter Out (Banned Item)"
        Width="38"
        Padding="2"
        Margin="0,0,4,0">
    <Image Source="{Binding DebuffedIconImage}" Stretch="Uniform"/>
</Button>
```

## 3. Drag-Drop Validation

### 3.1 Valid Drop Zones

**File:** `src/Components/FilterTabs/VisualBuilderTab.axaml.cs`

**BannedItems tray can ONLY be dropped into:**
- ✅ MUST zone (Filter Criteria / Filter Items)

**BannedItems tray CANNOT be dropped into:**
- ❌ SHOULD zone (Score Criteria)

### 3.2 Invalid Drop Feedback

When dragging BannedItems tray over SHOULD zone:

1. **Do NOT** show the drop zone overlay highlight
2. **Show** "not allowed" cursor (🚫)
3. **Prevent** drop operation

```csharp
private void OnDragOver(object sender, DragEventArgs e)
{
    var data = e.Data.Get("FilterOperatorItem") as FilterOperatorItem;

    if (data?.OperatorType == "BannedItems" && targetZone == "Should")
    {
        e.DragEffects = DragDropEffects.None; // Show 🚫 cursor
        // Do NOT add "drag-over" class to overlay
        return;
    }

    // ... normal drag-over logic ...
}
```

### 3.3 Merging Behavior

When BannedItems tray is dropped onto MUST zone:

**Case 1: No existing BannedItems tray**
- Drop the tray into MUST zone normally
- Render as red clause frame with "Banned Items" header

**Case 2: Existing BannedItems tray already in MUST**
- **MERGE** all items from dragged tray into existing tray
- **Silent merge** (no toast/notification - user sees it happen)
- **Delete** the dragged tray after merge completes

```csharp
private void OnDrop_MustZone(object sender, DragEventArgs e)
{
    var droppedTray = e.Data.Get("FilterOperatorItem") as FilterOperatorItem;

    if (droppedTray?.OperatorType == "BannedItems")
    {
        // Check if BannedItems already exists in MUST
        var existingBanned = SelectedMust
            .OfType<FilterOperatorItem>()
            .FirstOrDefault(x => x.OperatorType == "BannedItems");

        if (existingBanned != null)
        {
            // MERGE: Add all children to existing tray
            foreach (var child in droppedTray.Children)
            {
                existingBanned.Children.Add(child);
            }
            // Dragged tray is discarded (not added to MUST)
        }
        else
        {
            // No existing BannedItems - add normally
            SelectedMust.Add(droppedTray);
        }
    }
}
```

### 3.4 Individual Item Drops into BannedItems

Users MUST drop items into BannedItems tray first (not directly into MUST):

1. Drag item from shelf
2. Drop onto BannedItems tray (if BannedItems exists in top shelf OR in MUST zone)
3. Item gets added to BannedItems.Children with `IsInBannedItemsTray = true`

BannedItems tray itself is NOT a drop zone for individual items when it's in the top shelf - only when it's been dropped into MUST.

## 4. Serialization - Visual State → JSON

### 4.1 BuildConfigFromCurrentState

**File:** `src/ViewModels/FilterTabs/SaveFilterTabViewModel.cs`

```csharp
private MotelyJsonConfig BuildConfigFromCurrentState()
{
    var config = new MotelyJsonConfig
    {
        // ... deck, stake, name, description, etc. ...
        Must = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
        Should = new List<MotelyJsonConfig.MotleyJsonFilterClause>(),
        MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>(), // ← Initialize
    };

    if (_parentViewModel.VisualBuilderTab is VisualBuilderTabViewModel visualVm)
    {
        // Process MUST zone
        foreach (var item in visualVm.SelectedMust)
        {
            if (item is FilterOperatorItem operatorItem)
            {
                if (operatorItem.OperatorType == "BannedItems")
                {
                    // BANNED ITEMS → MustNot[]
                    foreach (var child in operatorItem.Children)
                    {
                        var clause = ConvertFilterItemToClause(child, _parentViewModel.ItemConfigs);
                        if (clause != null)
                            config.MustNot.Add(clause);
                    }
                }
                else // "OR" or "AND"
                {
                    // Normal OR/AND logic → Must[]
                    var clause = ConvertFilterItemToClause(operatorItem, _parentViewModel.ItemConfigs);
                    if (clause != null)
                        config.Must.Add(clause);
                }
            }
            else
            {
                // Direct FilterItem → Must[]
                var clause = ConvertFilterItemToClause(item, _parentViewModel.ItemConfigs);
                if (clause != null)
                    config.Must.Add(clause);
            }
        }

        // Process SHOULD zone (NEVER contains BannedItems)
        foreach (var item in visualVm.SelectedShould)
        {
            var clause = ConvertFilterItemToClause(item, _parentViewModel.ItemConfigs);
            if (clause != null)
                config.Should.Add(clause);
        }
    }

    // If no banned items, ensure MustNot is empty array (not null)
    if (config.MustNot.Count == 0)
    {
        config.MustNot = new List<MotelyJsonConfig.MotleyJsonFilterClause>();
    }

    return config;
}
```

### 4.2 Clause Structure for MustNot[]

MustNot[] uses the **SAME structure** as Must[] - full item properties are preserved:

```json
{
  "Must": [
    {
      "Type": "Joker",
      "Value": "Joker",
      "Edition": "foil",
      "Antes": [1, 2, 3],
      "Min": 1
    }
  ],
  "Should": [],
  "MustNot": [
    {
      "Type": "Joker",
      "Value": "Chicot",
      "Antes": [1],
      "Min": 1
    }
  ]
}
```

**Example:** "Must not have Chicot in ante 1" (but ante 8 Chicot is fine)

## 5. Deserialization - JSON → Visual State

### 5.1 LoadFilterFromConfig

**File:** `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs`

```csharp
public void LoadFilterFromConfig(MotelyJsonConfig config)
{
    // Clear existing state
    SelectedMust.Clear();
    SelectedShould.Clear();

    // Load MUST items
    foreach (var clause in config.Must ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>())
    {
        var filterItem = ConvertClauseToFilterItem(clause);
        if (filterItem != null)
            SelectedMust.Add(filterItem);
    }

    // Load SHOULD items
    foreach (var clause in config.Should ?? new List<MotelyJsonConfig.MotleyJsonFilterClause>())
    {
        var filterItem = ConvertClauseToFilterItem(clause);
        if (filterItem != null)
            SelectedShould.Add(filterItem);
    }

    // Load MUSTNOT items → BannedItems tray
    if (config.MustNot != null && config.MustNot.Any())
    {
        var bannedItemsTray = new FilterOperatorItem("BannedItems");

        foreach (var clause in config.MustNot)
        {
            var filterItem = ConvertClauseToFilterItem(clause);
            if (filterItem != null)
            {
                filterItem.IsInBannedItemsTray = true; // ← Enable debuffed overlay
                bannedItemsTray.Children.Add(filterItem);
            }
        }

        // Add BannedItems tray to MUST zone
        SelectedMust.Add(bannedItemsTray);
    }
}
```

### 5.2 Empty MustNot[] Handling

**If MustNot[] is empty or null:**
- Do NOT create a BannedItems tray
- Visual state has no banned items representation

**If MustNot[] has items:**
- Create ONE BannedItems tray
- Add all MustNot[] items as children
- Set `IsInBannedItemsTray = true` for each child
- Place tray in MUST zone

## 6. FilterItem Model Extension

**File:** `src/Models/FilterItem.cs`

Add property to track if item is inside BannedItems tray:

```csharp
[ObservableProperty]
private bool _isInBannedItemsTray = false;
```

This property:
- Controls debuffed overlay visibility in UI
- Set to `true` when item is added to BannedItems.Children
- Set to `false` when item is removed or moved to different tray

## 7. Return to Shelf Behavior

When user drags BannedItems tray to "Return to Item Shelf" overlay:

1. **Remove** BannedItems tray from MUST zone
2. **Do NOT** move items back to shelf (shelf always has all items available)
3. Items are simply removed from the filter editing area
4. On next serialization, MustNot[] will be empty array

```csharp
private void OnDrop_ReturnToShelf(object sender, DragEventArgs e)
{
    var item = e.Data.Get("FilterOperatorItem") as FilterOperatorItem;

    if (item?.OperatorType == "BannedItems")
    {
        // Simply remove from MUST - don't migrate items anywhere
        SelectedMust.Remove(item);
    }
}
```

## 8. Implementation Checklist

### Phase 1: Data Model
- [ ] Extend `FilterOperatorItem` to support "BannedItems" type
- [ ] Add `IsInBannedItemsTray` property to `FilterItem`
- [ ] Add `DebuffedOverlayImage` property to `FilterItem`

### Phase 2: UI Components
- [ ] Add red styling for `.operator-card-container.banned` in `FilterOperatorControl.axaml`
- [ ] Add red header styling for `.operator-header.banned`
- [ ] Update cycle button to include BannedItems: OR → AND → BANNED
- [ ] Add debuffed overlay rendering in card template (conditional on `IsInBannedItemsTray`)
- [ ] **DELETE** debuffed button from edition toolbar

### Phase 3: Drag-Drop Logic
- [ ] Implement BannedItems validation (MUST only, SHOULD rejected)
- [ ] Add "not allowed" cursor (🚫) when dragging over SHOULD
- [ ] Prevent drop zone highlight for SHOULD when dragging BannedItems
- [ ] Implement merge logic (combine children when dropping onto existing BannedItems)
- [ ] Update "Return to Shelf" to handle BannedItems removal

### Phase 4: Serialization
- [ ] Update `BuildConfigFromCurrentState` to serialize BannedItems → MustNot[]
- [ ] Ensure MustNot[] preserves full item properties (edition, seal, antes, etc.)
- [ ] Handle empty MustNot[] (serialize as `[]`, not null)

### Phase 5: Deserialization
- [ ] Update `LoadFilterFromConfig` to deserialize MustNot[] → BannedItems tray
- [ ] Set `IsInBannedItemsTray = true` for all items in BannedItems
- [ ] Handle empty/null MustNot[] (don't create BannedItems tray)
- [ ] Place BannedItems tray in MUST zone

### Phase 6: Testing
- [ ] Test cycle button: OR → AND → BANNED → OR
- [ ] Test drag BannedItems to MUST (valid)
- [ ] Test drag BannedItems to SHOULD (🚫 rejected)
- [ ] Test merging two BannedItems trays
- [ ] Test serialization: BannedItems tray → MustNot[] with full properties
- [ ] Test deserialization: MustNot[] → BannedItems tray with debuffed overlays
- [ ] Test "Return to Shelf" removes BannedItems
- [ ] Test empty MustNot[] serializes as `[]`
- [ ] Verify debuffed overlay renders FULL SIZE on all items in BannedItems

## 9. Edge Cases & Error Handling

### 9.1 Multiple BannedItems Trays
**Should never happen** - merge logic prevents it. If somehow multiple exist:
- On serialization, combine all MustNot items from all BannedItems trays
- On deserialization, always create only ONE BannedItems tray

### 9.2 BannedItems in SHOULD Zone
**Should never happen** - validation prevents it. If somehow it exists:
- Ignore during serialization (don't add to MustNot[])
- Log error for debugging

### 9.3 Empty BannedItems Tray
**Valid state** - user created tray but hasn't added items yet:
- Render normally (empty fanned cards area)
- Serialize as empty MustNot[] (`[]`)

### 9.4 Nested BannedItems
**Invalid** - BannedItems cannot contain other BannedItems trays:
- Prevent nesting in drag-drop validation
- BannedItems tray is NOT a drop zone for other operator trays

## 10. Visual Reference

```
┌─────────────────────────────────────────┐
│ Top Shelf (Operator Trays)              │
│  ┌──────┐  ┌──────┐  ┌──────────────┐  │
│  │  OR  │  │ AND  │  │ BANNED ITEMS │  │ ← Red border
│  │ Blue │  │ Blue │  │     Red      │  │
│  └──────┘  └──────┘  └──────────────┘  │
│             ↻ Cycle button               │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ MUST Zone (Filter Criteria)             │
│  ┌──────────────────────────────────┐   │
│  │  BANNED ITEMS (Red header)       │   │
│  │  ┌───┐ ┌───┐ ┌───┐              │   │
│  │  │ 🃏│ │ 🃏│ │ 🃏│ ← Debuffed   │   │
│  │  │ X │ │ X │ │ X │   overlays    │   │
│  │  └───┘ └───┘ └───┘              │   │
│  └──────────────────────────────────┘   │
│                                          │
│  (Other OR/AND clauses...)               │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ SHOULD Zone (Score Criteria)            │
│  🚫 BannedItems cannot drop here!       │
└─────────────────────────────────────────┘
```

## 11. JSON Example

**Visual State:**
- MUST zone contains:
  - BannedItems tray with 2 items: Chicot (ante 1), Blueprint (any ante, foil edition)

**Serialized JSON:**
```json
{
  "Deck": "Red",
  "Stake": "White",
  "Name": "My Filter",
  "Must": [],
  "Should": [],
  "MustNot": [
    {
      "Type": "Joker",
      "Value": "Chicot",
      "Antes": [1],
      "Min": 1
    },
    {
      "Type": "Joker",
      "Value": "Blueprint",
      "Edition": "foil",
      "Antes": [1, 2, 3, 4, 5, 6, 7, 8],
      "Min": 1
    }
  ]
}
```

## 12. Success Criteria

✅ **Cycle button cycles through three operators:** OR → AND → BANNED
✅ **BannedItems tray has red border/header** with "Banned Items" title
✅ **Items in BannedItems show debuffed overlay** (full size sprite)
✅ **BannedItems can only drop into MUST**, SHOULD shows 🚫 and no highlight
✅ **Dropping BannedItems onto existing BannedItems merges** children silently
✅ **Serialization converts BannedItems → MustNot[]** with full item properties
✅ **Empty BannedItems serializes as MustNot: []**
✅ **Deserialization converts MustNot[] → BannedItems tray** in MUST zone
✅ **Empty MustNot[] creates no BannedItems tray** in visual state
✅ **Debuffed button removed** from edition toolbar
✅ **Return to shelf removes BannedItems** without moving items to shelf

---

**End of PRD**

This specification ensures 100% accuracy in the Banned Items implementation, maintaining the precision required for reliable seed searching. All edge cases are handled, and the visual representation perfectly matches the underlying MustNot[] filter logic.
