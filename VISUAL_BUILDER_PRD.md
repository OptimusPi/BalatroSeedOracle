# Visual Filter Builder - Product Requirements Document

## Executive Summary

The Visual Filter Builder is a drag-and-drop interface for creating complex Balatro seed search filters using a visual, card-based paradigm. Users can drag joker/consumable/etc. cards from a browsable shelf into three drop zones (MUST, SHOULD, CAN'T) and combine them with logical operators (AND/OR) to build sophisticated filter expressions.

---

## 1. Product Overview

### 1.1 Vision
Create an intuitive, game-authentic visual interface that makes complex filter creation feel like playing Balatro itself - tactile, responsive, and immediately understandable.

### 1.2 Target Users
- **Casual Players**: Want simple filters (e.g., "seeds with Chicot") without learning JSON
- **Power Users**: Need complex multi-condition filters with logical operators
- **Streamers**: Need quick filter setup with visual appeal for audience

### 1.3 Success Criteria
- Users can create basic filters (1-3 items) in under 30 seconds
- Complex filters (5+ items with operators) completable in under 2 minutes
- 80%+ of users prefer Visual Builder over JSON Editor
- Zero crashes or data loss during drag operations

---

## 2. Core Features

### 2.1 Item Shelf (Left Panel)

**Purpose**: Browse and select items to add to filter

**Components**:
- Category tabs (Joker, Consumable, Voucher, etc.)
- Search box with real-time filtering
- Scrollable card grid with Balatro-style cards
- Card hover effects (magnetic levitation + sway animation)

**Behavior**:
- Cards display with authentic Balatro artwork
- Legendary jokers show animated soul face overlay
- Hover triggers 3D tilt and vertical lift (6px up + 2px shadow)
- Cards maintain gentle breathing sway animation (±0.2 radians)
- Search filters by name, case-insensitive, instant results
- Category switch preserves search text

**Performance Requirements**:
- Shelf loads <100ms for any category
- Search results appear <50ms after typing
- Smooth 60fps animation on all card interactions
- Supports 200+ cards per category without lag

---

### 2.2 Operator Palette (Middle Panel)

**Purpose**: Provide logical operators for building complex filters

**Available Operators**:
- **AND**: All contained items must be present
- **OR**: At least one contained item must be present

**Visual Design**:
- Small draggable chips/badges with distinct colors
  - AND: Blue (#4FA8F4)
  - OR: Gold (#F4B841)
- Icon + text label format
- Slightly smaller than regular cards (60px wide vs 70px)

**Behavior**:
- Click to "pick up" operator
- Drag into drop zones to create nested logic
- Can contain 1-10 child items
- Children display as mini-cards inside operator border
- Cannot nest operators inside operators (flat hierarchy only)

**Technical Implementation**:
```csharp
public class FilterOperatorItem : FilterItem
{
    public OperatorType OperatorType { get; set; } // AND, OR
    public ObservableCollection<FilterItem> Children { get; set; }
}
```

---

### 2.3 Drop Zones (Right Panel)

**Purpose**: Define filter criteria by dropping items into categorized zones

**Three Zones**:
1. **MUST** (Blue)
   - Items that MUST be present in the seed
   - Default expanded state
   - Top third of panel

2. **SHOULD** (Gold)
   - Items that would be nice to have (optional)
   - Middle third of panel
   - Lower priority than MUST

3. **CAN'T** (Red)
   - Items that must NOT be present (blacklist)
   - Bottom third of panel
   - Excludes seeds containing these items

**Visual Design**:
- Each zone has horizontal label bar at top
- Label shows zone name + item count badge (when collapsed)
- Label is clickable to expand/collapse zone
- Only one zone expanded at a time (accordion behavior)
- Collapsed zones: 40px height (label only)
- Expanded zones: Fills remaining space (1*)

**Drop Overlay System**:
- Three overlay borders positioned in fixed thirds
- Overlays independent of zone expansion state
- Top third always maps to MUST (blue overlay)
- Middle third always maps to SHOULD (gold overlay)
- Bottom third always maps to CAN'T (red overlay)
- Drop detection uses Y-position math (thirds of container height)

**Behavior**:
- **Drag-over feedback**: Appropriate third overlay appears with pulsing glow
- **Drop**: Item added to correct zone based on Y-position
- **Auto-expand**: Target zone expands on successful drop
- **Auto-collapse**: Other zones collapse on drop
- **Item removal**: Click × button or drag back to shelf
- **Magnetic layout**: Items auto-arrange with category grouping
- **Right-click context**: Future feature for item configuration

**Performance Requirements**:
- Drop detection latency <16ms (60fps)
- Zone expansion animation 250ms cubic ease-out
- Layout recalculation <50ms for up to 50 items
- Smooth drag at 60fps regardless of zone expansion state

---

### 2.4 Drag-and-Drop System

**Interaction Flow**:

1. **Pick Up** (PointerPressed)
   - Left-click on card in shelf or drop zone
   - Card image "detaches" and follows cursor
   - Ghost card rendered in AdornerLayer (top-most Z-index)
   - Original card remains in place (hidden/dimmed)
   - Cursor becomes "grabbing" hand

2. **Drag** (PointerMoved)
   - Ghost card follows cursor with offset (-35px X, -47px Y for centering)
   - Ghost maintains sway animation (BalatroCardSwayBehavior)
   - Drop zone overlays appear/disappear based on cursor position
   - Overlay determination:
     ```csharp
     var localY = e.GetPosition(dropZoneContainer).Y;
     var containerHeight = dropZoneContainer.Bounds.Height;
     var thirdHeight = containerHeight / 3.0;

     if (localY < thirdHeight) => MUST (top third)
     else if (localY < thirdHeight * 2) => SHOULD (middle third)
     else => CAN'T (bottom third)
     ```
   - Special case: Over shelf shows "return" overlay (only if dragging from zone)

3. **Drop** (PointerReleased)
   - Determine target zone from cursor Y-position (thirds calculation)
   - Add item to target zone's ObservableCollection
   - Remove from source zone (if applicable)
   - Expand target zone, collapse others
   - Play drop sound effect (future)
   - Animate ghost card to final position (rubber-band effect)
   - Remove ghost from AdornerLayer

**Edge Cases**:
- **Drop outside all zones**: Rubber-band animation back to source
- **Drop on same zone**: Item stays, zone remains expanded, no-op
- **Drag from shelf → shelf**: Cancel drag, no changes
- **Drag operator**: Operator becomes container, awaits children
- **Right-click during drag**: Cancel drag, rubber-band back

**Visual Feedback**:
- Ghost card opacity: 0.95 (slightly transparent)
- Drop zone overlay: Pulsing animation (opacity 0.85 ↔ 1.0, 1.5s cycle)
- Overlay border: 6px thick, colored glow (30px blur radius)
- Overlay text: Large centered emoji + text (e.g., "✨ DROP IN MUST HAVE ✨")
- Return overlay: Red glow, skull emoji "⚰️ RETURN TO SHELF ⚰️"

---

### 2.5 Category Grouping Layout

**Purpose**: Auto-organize items within drop zones by category

**Behavior**:
- Items auto-arrange into category groups (Joker, Consumable, etc.)
- Groups appear in fixed order (Joker → Consumable → Voucher → ...)
- Within group, items maintain chronological add order
- 8px gap between groups for visual separation
- Items within group have 4px gap

**Visual Design**:
- Items laid out on Canvas for absolute positioning
- CategoryGroupedLayoutBehavior calculates positions
- Smooth animated transitions when items added/removed (250ms)
- Magnetic "snap" feel to grid positions

**Layout Algorithm**:
```csharp
// Pseudocode
foreach (var group in itemsByCategory)
{
    var xOffset = currentColumn * (cardWidth + gap);
    var yOffset = currentRow * (cardHeight + gap);

    if (currentColumn >= maxColumns)
    {
        currentColumn = 0;
        currentRow++;
    }

    PlaceCard(item, xOffset, yOffset);
    currentColumn++;
}
currentRow++; // Gap between categories
```

**Performance**:
- Relayout triggered on ObservableCollection changes
- Debounced to prevent thrashing (100ms delay)
- Supports 100+ items per zone without jank

---

## 3. Technical Architecture

### 3.1 MVVM Structure

**ViewModel** (`VisualBuilderTabViewModel.cs`):
```csharp
public class VisualBuilderTabViewModel : ObservableObject
{
    // Drop zone collections
    [ObservableProperty]
    ObservableCollection<FilterItem> selectedMust;

    [ObservableProperty]
    ObservableCollection<FilterItem> selectedShould;

    [ObservableProperty]
    ObservableCollection<FilterItem> selectedMustNot;

    // Expansion state (accordion behavior)
    [ObservableProperty]
    bool isMustExpanded = true;

    [ObservableProperty]
    bool isShouldExpanded = false;

    [ObservableProperty]
    bool isCantExpanded = false;

    // Commands
    [RelayCommand]
    void ExpandMust() => { IsMustExpanded = true; IsShouldExpanded = false; IsCantExpanded = false; }

    [RelayCommand]
    void ExpandShould() => { IsMustExpanded = false; IsShouldExpanded = true; IsCantExpanded = false; }

    [RelayCommand]
    void ExpandCant() => { IsMustExpanded = false; IsShouldExpanded = false; IsCantExpanded = true; }

    // Filter conversion
    public FilterConfiguration ToFilterConfiguration()
    {
        // Convert SelectedMust/Should/MustNot to Motely filter format
    }
}
```

**View** (`VisualBuilderTab.axaml.cs`):
- Zero business logic
- Handles only UI concerns:
  - Drag adorner management (AdornerLayer)
  - Drop zone hit testing (thirds calculation)
  - Animation coordination
  - Sound effect triggers
- Delegates all data operations to ViewModel

### 3.2 Data Model

```csharp
// Base class for all draggable items
public class FilterItem : SelectableItem
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Category { get; set; } // "Joker", "Consumable", etc.
    public string ItemImage { get; set; } // Path to card image
    public string? SoulFaceImage { get; set; } // Legendary overlay
    public FilterItemStatus Status { get; set; } // Future: Must/Should/MustNot
}

// Operator container
public class FilterOperatorItem : FilterItem
{
    public OperatorType OperatorType { get; set; } // AND, OR
    public ObservableCollection<FilterItem> Children { get; set; }
}

public enum OperatorType
{
    And,
    Or
}
```

### 3.3 Behaviors (Avalonia Behaviors)

**BalatroCardSwayBehavior**:
- Attaches to any Control
- Creates gentle breathing rotation (±0.2 radians)
- Uses Balatro's exact math:
  ```csharp
  var tilt_angle = elapsedSeconds * (1.56 + (_cardId / 1.14212) % 1) + _cardId / 1.35122;
  var tilt_amt = AmbientTilt * (0.5 + Math.Cos(tilt_angle)) * TiltFactor;
  rotateTransform.Angle = tilt_amt * RadiansToDegrees;
  ```
- Runs at 60fps (16.67ms timer)

**CategoryGroupedLayoutBehavior**:
- Attaches to ItemsControl
- Monitors ObservableCollection changes
- Calculates Canvas.Left/Top for each item
- Groups by Category property
- Animates position changes (250ms smooth transition)

**ResponsiveCardBehavior** (future):
- Scales cards based on viewport size
- Maintains aspect ratio
- Prevents overlap on small screens

---

## 4. User Flows

### 4.1 Simple Filter Creation
**Goal**: Create filter for "seeds with Chicot joker"

1. User clicks "Visual Builder" tab
2. User sees shelf with Jokers loaded by default
3. User spots Chicot card (or types "chic" in search)
4. User drags Chicot card to top third (MUST zone)
5. Blue overlay appears during drag
6. User releases mouse
7. MUST zone expands, shows Chicot card
8. User clicks "Save" → filter saved

**Time**: ~15 seconds
**Clicks**: 3 (tab, drag, save)

### 4.2 Complex Filter with Operators
**Goal**: "Seeds with (Chicot OR Canio) AND (DNA OR Supernova)"

1. User drags AND operator from palette to MUST zone
2. AND operator expands to show empty container
3. User drags Chicot into AND operator
4. User drags OR operator into AND operator
5. User drags Canio into OR operator
6. User drags DNA into AND operator
7. User drags OR operator into AND operator
8. User drags Supernova into nested OR
9. Visual tree structure shows:
   ```
   MUST:
   └─ AND
      ├─ Chicot
      ├─ OR
      │  └─ Canio
      ├─ DNA
      └─ OR
         └─ Supernova
   ```
10. User clicks "Save"

**Time**: ~90 seconds
**Clicks**: 10 (8 drags + save)

### 4.3 Filter Editing
**Goal**: Remove item from existing filter

1. User opens Visual Builder with existing filter loaded
2. User sees drop zones populated with items
3. User hovers over item → × button appears
4. User clicks × button
5. Item removed with fade-out animation (250ms)
6. Layout recalculates, remaining items shift smoothly
7. User clicks "Save"

**Time**: ~5 seconds
**Clicks**: 2 (×, save)

---

## 5. Visual Design Specifications

### 5.1 Colors (Balatro Theme)

```css
/* Zone Colors */
--must-blue: #4FA8F4;
--should-gold: #F4B841;
--cant-red: #F44336;

/* Backgrounds */
--dark-background: #1a1a2e;
--dark-teal-grey: #2d3142;
--black: #0f0f1e;

/* Overlays */
--semi-transparent-blue: rgba(79, 168, 244, 0.25);
--semi-transparent-gold: rgba(244, 184, 65, 0.25);
--semi-transparent-red: rgba(244, 67, 54, 0.25);
```

### 5.2 Typography

```css
--balatro-font: "m6x11plus", monospace;
--font-size-label: 18px;
--font-size-badge: 12px;
--font-size-overlay: 24px;
--font-weight-bold: 700;
--font-weight-extra-bold: 800;
```

### 5.3 Card Dimensions

```
Shelf Card: 70px × 93px (aspect ratio 0.752688)
Drop Zone Card: 57px × 76px (scaled down)
Operator Chip: 60px × 80px (custom size)
Mini Card (in operator): 50px × 66px (smallest)
```

### 5.4 Spacing

```
Card gap (within group): 4px
Group gap: 8px
Zone padding: 12px
Label padding: 8px (horizontal) × 4px (vertical)
Overlay border thickness: 6px
```

### 5.5 Animations

**Card Hover**:
- Lift: translateY(-6px)
- Shadow: 2px drop shadow
- Duration: 150ms
- Easing: ease-out

**Drop Zone Expansion**:
- Duration: 250ms
- Easing: cubic-bezier(0.4, 0.0, 0.2, 1) (cubic-ease-out)
- Property: Height (Auto ↔ 40px)

**Overlay Pulse**:
- Opacity: 0.85 ↔ 1.0
- Duration: 1.5s
- Iteration: Infinite
- Easing: sine wave

**Card Sway**:
- Rotation: ±11.46° (±0.2 radians)
- Duration: Variable per card (1.56s base + randomness)
- Easing: cosine wave
- Frame rate: 60fps (16.67ms updates)

---

## 6. Performance Requirements

### 6.1 Benchmarks

| Operation | Target | Maximum |
|-----------|--------|---------|
| Shelf category load | <50ms | <100ms |
| Search filter | <30ms | <50ms |
| Drag frame rate | 60fps | 50fps |
| Drop detection | <10ms | <16ms |
| Zone expansion | 250ms | 300ms |
| Layout recalculation | <30ms | <50ms |
| Save filter | <100ms | <200ms |

### 6.2 Scalability

- **Max items per zone**: 100 (with smooth performance)
- **Max nested operators**: 5 levels deep
- **Max operator children**: 20 items
- **Max shelf items**: 500+ (virtualized scrolling)

### 6.3 Memory

- **Card image cache**: 50MB max (evict LRU)
- **ViewModel footprint**: <5MB for typical filter (20 items)
- **Animation overhead**: <2% CPU on modern hardware

---

## 7. Future Enhancements

### 7.1 Phase 2 Features

1. **Undo/Redo System**
   - Command pattern for all filter edits
   - Ctrl+Z / Ctrl+Y keyboard shortcuts
   - History stack (max 50 operations)

2. **Item Configuration**
   - Right-click → context menu
   - Set item properties (e.g., "Joker must be Negative")
   - Visual indicator badges on cards

3. **Filter Templates**
   - Save filter as template
   - Template library (user + community)
   - One-click apply template

4. **Advanced Operators**
   - NOT operator (negate condition)
   - XOR operator (exclusive or)
   - COUNT operator (e.g., "at least 3 legendary jokers")

5. **Drag-and-Drop Reordering**
   - Drag items within zone to reorder
   - Drag operator to re-parent items
   - Smooth animated transitions

### 7.2 Phase 3 Features

1. **Multi-Select**
   - Shift+click to select range
   - Ctrl+click to add to selection
   - Drag multiple items at once

2. **Keyboard Navigation**
   - Arrow keys to navigate shelf
   - Enter to add focused item to zone
   - Delete to remove from zone
   - Tab to cycle between zones

3. **Filter Preview**
   - Live preview of matching seeds
   - "Test Filter" button → shows sample results
   - Estimated match count

4. **Filter Sharing**
   - Export filter as shareable URL
   - QR code generation
   - Import from clipboard

---

## 8. Acceptance Criteria

### 8.1 Must-Have (MVP)

- [ ] Shelf loads all item categories
- [ ] Search filters items by name
- [ ] Drag card from shelf to drop zone works
- [ ] Drop zone overlays show in correct thirds
- [ ] Drop detection uses Y-position math (thirds)
- [ ] Zone expansion/collapse on drop
- [ ] Items grouped by category in zones
- [ ] Remove item via × button
- [ ] Save filter to FilterConfiguration
- [ ] Load existing filter into zones
- [ ] No crashes during drag operations
- [ ] 60fps performance on average hardware

### 8.2 Should-Have

- [ ] AND/OR operators functional
- [ ] Nested operators support (1 level deep)
- [ ] Drag item back to shelf to remove
- [ ] Return overlay shows when dragging from zone
- [ ] Rubber-band animation on invalid drop
- [ ] Card sway animation on shelf
- [ ] Magnetic hover effect on shelf cards

### 8.3 Nice-to-Have

- [ ] Sound effects (pickup, drop, trash)
- [ ] Particle effects on drop
- [ ] Filter validation warnings
- [ ] Tooltips on operators
- [ ] Accessibility support (screen readers)

---

## 9. Technical Constraints

### 9.1 Platform
- **Framework**: Avalonia UI 11.x (cross-platform .NET)
- **Target**: Windows 10/11, macOS 12+, Linux (Debian-based)
- **.NET Version**: .NET 9.0

### 9.2 Dependencies
- **CommunityToolkit.Mvvm**: MVVM infrastructure
- **Avalonia.Xaml.Behaviors**: Behavior system
- **Newtonsoft.Json**: Filter serialization (legacy)
- **SkiaSharp** (via Avalonia): 2D graphics rendering

### 9.3 Browser Support
N/A (desktop application)

### 9.4 Accessibility
- Must support keyboard navigation (Phase 3)
- Must support screen readers (Phase 3)
- Color-blind friendly palettes (use shapes + colors)

---

## 10. Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Performance degradation with 50+ items** | Medium | High | Implement virtualization, Canvas recycling |
| **Complex nested operators confuse users** | High | Medium | Limit nesting to 2 levels, add visual guides |
| **Drag-and-drop feels janky** | Low | High | Strict 60fps requirement, profiling tools |
| **Filter conversion bugs (visual → JSON)** | Medium | High | Extensive unit tests, validation layer |
| **User loses work (no autosave)** | Low | Medium | Auto-save to temp storage every 30s |
| **Overlay detection fails on different DPI** | Medium | Medium | Use DPI-aware positioning, test on 4K displays |

---

## 11. Testing Strategy

### 11.1 Unit Tests
- FilterItem serialization/deserialization
- Operator tree traversal
- FilterConfiguration conversion
- ViewModel command logic

### 11.2 Integration Tests
- Drag-and-drop flow (shelf → zone)
- Zone expansion/collapse behavior
- Item removal from zones
- Filter save/load round-trip

### 11.3 UI Tests
- Visual regression testing (screenshot comparison)
- Animation performance profiling
- Memory leak detection (long sessions)

### 11.4 Manual Testing Checklist
- [ ] Drag 50 items into MUST zone → no lag
- [ ] Drop item on zone boundary → correct zone detection
- [ ] Rapidly click between zones → smooth expansion
- [ ] Search with 1000+ results → instant filtering
- [ ] Drag while zone animating → no crash
- [ ] Resize window → layout adapts correctly

---

## 12. Success Metrics

### 12.1 Adoption
- **Goal**: 70% of users try Visual Builder within first session
- **Measurement**: Track tab clicks in analytics

### 12.2 Engagement
- **Goal**: 50% of filters created via Visual Builder (vs JSON)
- **Measurement**: Track filter creation method

### 12.3 Efficiency
- **Goal**: Average filter creation time <60 seconds
- **Measurement**: Track time from tab open → save click

### 12.4 Quality
- **Goal**: <5 drag-and-drop bugs reported per 1000 users
- **Measurement**: GitHub issues tagged "visual-builder" + "drag"

### 12.5 Performance
- **Goal**: 95th percentile drag latency <16ms (60fps)
- **Measurement**: Embedded performance telemetry

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| **Adorner Layer** | Avalonia UI layer for rendering overlays/popups above all controls |
| **FilterConfiguration** | Data structure representing a complete filter (Motely search format) |
| **Hit Testing** | Detecting which UI element is under the cursor |
| **ObservableCollection** | .NET collection that fires events on add/remove (enables data binding) |
| **RelayCommand** | CommunityToolkit.Mvvm command wrapper for MVVM |
| **Rubber-band Animation** | Visual feedback where dragged item snaps back to origin on invalid drop |
| **Thirds Detection** | Dividing drop zone container into 3 equal vertical sections for zone detection |
| **TranslateTransform** | Avalonia transform for moving UI elements without layout recalculation |

---

## Appendix B: Code Snippets

### Drop Zone Detection Algorithm

```csharp
private string? DetectDropZone(PointerReleasedEventArgs e, Grid dropZoneContainer)
{
    var localPos = e.GetPosition(dropZoneContainer);
    var containerHeight = dropZoneContainer.Bounds.Height;
    var thirdHeight = containerHeight / 3.0;

    if (localPos.Y < thirdHeight)
    {
        return "MustDropZone"; // Top third
    }
    else if (localPos.Y < thirdHeight * 2)
    {
        return "ShouldDropZone"; // Middle third
    }
    else
    {
        return "MustNotDropZone"; // Bottom third
    }
}
```

### Filter Configuration Export

```csharp
public FilterConfiguration ToFilterConfiguration()
{
    var config = new FilterConfiguration();

    // Build joker filter (MUST + SHOULD)
    var jokerMust = SelectedMust
        .Where(i => i.Category == "Joker")
        .Select(i => i.Name)
        .ToList();

    var jokerShould = SelectedShould
        .Where(i => i.Category == "Joker")
        .Select(i => i.Name)
        .ToList();

    if (jokerMust.Any())
    {
        config.JokerFilter = new JokerFilter
        {
            Must = jokerMust,
            Should = jokerShould
        };
    }

    // Build blacklist (CAN'T)
    var blacklist = SelectedMustNot.Select(i => i.Name).ToList();
    if (blacklist.Any())
    {
        config.Blacklist = blacklist;
    }

    return config;
}
```

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-31 | Claude | Initial PRD creation |

---

**End of Document**
