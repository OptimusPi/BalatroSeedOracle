# PRD-10: Balatro UI Kit (Custom Controls)

## Summary

A library of custom Avalonia controls styled to match the Balatro game aesthetic. These controls are shared across all features and provide the visual identity of the application. Includes themed buttons, tab controls, card displays, spinners, selectors, grids, and layout components.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `App.axaml` | 82KB global styles, colors, control templates |
| `Controls/BalatroTabControl.axaml` | Themed tab control |
| `Controls/FannedCardHand.axaml` | Fanned card display |
| `Controls/PlayingCardSelector.axaml` | Card picker |
| `Controls/EditionSelector.axaml` | Edition picker |
| `Controls/JokerSetDisplay.axaml` | Joker set visualization |
| `Controls/FilterCategoryStrip.axaml` | Category strip |
| `Controls/FilterItemDisplay.axaml` | Filter item renderer |
| `Controls/SortableResultsGrid.axaml` | Sortable data grid |
| `Controls/SpinnerControl.axaml` | Loading spinner |
| `Controls/PanelSpinner.axaml` | Panel loading spinner |
| `Controls/InfoPanel.axaml` | Info display panel |
| `Controls/TagEditor.axaml` | Tag editing control |
| `Controls/SourceSelector.axaml` | Data source picker |
| `Controls/MaximizeButton.axaml` | Custom maximize |
| `Controls/ErrorBoundary.cs` | Error boundary |
| `Controls/ErrorBoundaryStyles.axaml` | Error boundary styles |
| `Controls/Navigation/SelectableCategoryButton.axaml` | Category button |
| `Components/DeckSpinner.axaml` | Deck selector spinner |
| `Components/StakeSpinner.axaml` | Stake selector spinner |
| `Components/ResponsiveGrid.axaml` | Responsive layout grid |

---

## Requirements

### R1 — Color System

**Base Palette (from Balatro):**

| Name | Hex | Usage |
|------|-----|-------|
| Red | `#FF4C40` | Primary/danger |
| Blue | `#0093FF` | Links/accent |
| Green | `#4BC292` | Success/primary actions |
| Purple | `#8A63D2` | Secondary actions |
| Orange | `#FFA500` | Warnings/tertiary |
| Gold | `#FFD700` | Highlights/labels |
| White | `#FFFFFF` | Primary text |
| DarkBackground | `#1E2B2D` | Main background |
| ModalGrey | `#2A3A3C` | Panel backgrounds |
| DarkBorder | `#1A2426` | Dark borders |
| ModalBorder | `#4A5A5C` | Panel borders |
| BrightSilver | `#C0C0C0` | Subtle borders |
| HoverShadow | `#0D1415` | Drop shadows |
| SemiTransparentBlack | `#80000000` | Modal overlay |

**Semantic Resources:**
- `Border`, `HoverBorder`, `ActiveBorder`
- `DarkBackground`, `LightBackground`
- `PrimaryText`, `SecondaryText`, `DisabledText`

### R2 — Button Styles

Balatro-styled buttons with drop shadow:

| Class | Background | Usage |
|-------|-----------|-------|
| `btn-green` | Green gradient | Primary actions (SEARCH) |
| `btn-blue` | Blue gradient | Secondary actions (DESIGNER) |
| `btn-purple` | Purple gradient | Tertiary actions, icon buttons |
| `btn-orange` | Orange gradient | Settings/tools |
| `btn-red` | Red gradient | Danger/cancel |
| `author-button` | Grey/muted | Author name display |

**Button Features:**
- 3D drop shadow effect (darker border below)
- Hover state (brighten)
- Pressed state (shadow collapses, appears to press down)
- Disabled state (desaturated)
- Custom `CornerRadius` per button
- `FontFamily="BalatroFont"` by default

### R3 — BalatroTabControl

Custom tab control mimicking Balatro's in-game tabs:
- Horizontal tab strip
- Active tab highlighted with accent color
- Triangle/arrow indicator pointing to active tab content
- Smooth selection transitions
- `SelectedIndex` bindable property
- Support for dynamic tab items

### R4 — Card Display Controls

#### FannedCardHand
- Displays playing cards in a fanned layout (like a hand of cards)
- Configurable fan angle and card overlap
- `FannedHandBehavior` for interactive fanning
- Card hover effects (lift, scale)

#### PlayingCardSelector
- Grid of all 52 playing cards + jokers
- Click to select/deselect
- Multi-select mode
- Selected cards highlighted
- Sprite rendering via `SpriteService`

#### EditionSelector
- Pick card edition: None, Foil, Holo, Polychrome, Negative
- Visual preview of each edition
- Radio-button behavior (single select)

#### JokerSetDisplay
- Display a set of joker cards in a row
- Each joker shows sprite + name
- Edition overlay support
- Hover for tooltip with description

### R5 — Spinner Controls

#### DeckSpinner
- Cycle through Balatro deck types
- Left/right arrows to change
- Shows deck sprite + name
- Wraps around at ends

#### StakeSpinner
- Same pattern as DeckSpinner but for stake levels
- Shows stake icon + name
- Color-coded per stake level

#### SpinnerControl (Generic)
- Generic left/right spinner for any list of items
- Label display
- Wrap-around or clamp options

#### PanelSpinner
- Full-panel loading indicator
- Spinning animation
- Optional message text

### R6 — FilterCategoryStrip

- Horizontal strip of category buttons
- Categories: Jokers, Tarots, Planets, Spectrals, Vouchers, Tags, Bosses
- `SelectableCategoryButton` with icon + label
- Click to filter displayed items by category
- Selected category highlighted
- Scrollable if too many categories

### R7 — SortableResultsGrid

- `DataGrid`-based control with Balatro styling
- Column headers clickable to sort (ascending/descending/none)
- Sort indicator arrows
- Alternating row colors
- Row selection highlighting
- Right-click context menu support
- Pagination controls
- Column auto-sizing
- ViewModel: `SortableResultsGridViewModel` handles sort state

### R8 — InfoPanel

- Expandable/collapsible information panel
- Header with expand/collapse icon
- Content area with formatted text
- Used for displaying item descriptions, filter details, etc.

### R9 — TagEditor

- Add/remove tags from a list
- Tags displayed as pills/chips
- Text input to add new tags
- Click X to remove tag
- Autocomplete suggestions (optional)

### R10 — ResponsiveGrid

- Responsive layout grid that adjusts columns based on available width
- Auto-wrapping items
- Configurable min/max column widths
- Consistent spacing

### R11 — Font System

- Custom `BalatroFont` loaded as app resource
- Fallback to system fonts
- Consistent sizing across controls

### R12 — Converters

17 value converters for data binding:

| Converter | Purpose |
|-----------|---------|
| `BoolToOpacityConverter` | Show/fade based on bool |
| `BoolToGridLengthConverter` | Show/hide grid rows |
| `BoolToBrushConverter` | Color based on state |
| `BoolToExpandIconConverter` | Expand/collapse icon |
| `BoolToLedColorConverter` | LED indicator color |
| `CountToBoolConverter` | Has items check |
| `EnumToIntConverter` | Enum to integer |
| `InverseBoolConverter` | Negate bool |
| `TruncateConverter` | Truncate long strings |
| `StringEqualityConverter` | Compare strings |
| `ShadowDirectionToMarginConverter` | Shadow positioning |
| `SpriteConverters` | Load Balatro sprites (20+ types) |
| `PlayingCardConverters` | Card sprite loading |
| `AnteCheckboxConverter` | Ante checkbox state |
| `FilterItemConverters` | Filter item display |
| `ClauseTrayConverters` | Clause formatting |
| `FilterCriteriaConverter` | Criteria display |
| `CategorySizeConverters` | Category sizing |

### R13 — Behaviors

| Behavior | Purpose |
|----------|---------|
| `DraggableWidgetBehavior` | Widget dragging with inertia |
| `CardDragBehavior` | Card dragging with skew |
| `FloatingBehavior` | Sine/cosine floating animation |
| `BalatroCardSwayBehavior` | Card idle sway effect |
| `MagneticTiltBehavior` | Tilt toward cursor |
| `FannedHandBehavior` | Fan card layout |
| `CategoryGroupedLayoutBehavior` | Group items by category |
| `PlaySfxOnValueChangeBehavior` | SFX on slider/value change |

---

## Acceptance Criteria

- [ ] All color resources defined and accessible via `StaticResource`
- [ ] Button styles match Balatro aesthetic with shadows and states
- [ ] BalatroTabControl shows triangle indicator on active tab
- [ ] FannedCardHand renders cards in fan layout
- [ ] DeckSpinner and StakeSpinner cycle through options with sprites
- [ ] SortableResultsGrid sorts by column click
- [ ] FilterCategoryStrip navigates between categories
- [ ] TagEditor supports add/remove tags
- [ ] All converters work correctly in bindings
- [ ] All behaviors attach and function properly
- [ ] BalatroFont renders consistently across platforms
