# Product Requirements Document: PlayingCard Visual Filter Designer

## Executive Summary
Create a visual designer within FiltersModal that allows users to build PlayingCard filters through an intuitive UI, generating the appropriate MotelyJsonFilterDesc configuration that Motely already supports.

## Overview
The PlayingCard filter designer will provide a comprehensive visual interface for users to specify playing card criteria including rank, suit, enhancement, seal, and source location. All these properties are already supported by Motely's MotelyJsonFilterDesc backend - this PRD focuses on creating the visual UI layer.

## Core Requirements

### 1. PlayingCard FilterItem Component

#### 1.1 Visual Representation
- Display playing cards as they appear in Balatro
- Show card rank and suit clearly
- Display enhancement effects (visual overlays/borders)
- Show seal stamps on cards
- Use existing sprite assets from SpriteService

#### 1.2 Card Selection Grid
- Grid layout showing all 52 standard playing cards
- Visual states: Normal, Hover, Selected
- Multi-select capability with Ctrl/Shift click patterns
- "Select All" / "Clear Selection" buttons for quick actions
- Group selection by suit or rank

### 2. Card Properties Panel

#### 2.1 Enhancement Selector
**Available Enhancements:**
- **Glass** - Glass card effect overlay
- **Steel** - Metallic/steel appearance
- **Gold** - Golden card effect
- **Lucky** - Lucky green glow effect
- **Mult** - Multi multiplier indicator
- **Bonus** - Bonus chip indicator
- **Wild** - Wild card effect
- **Stone** - Stone card effect
- **None** - No enhancement (default)

**UI Design:**
- Radio button group or dropdown for single enhancement selection
- Visual preview of enhancement effect on selected cards
- Enhancement icon next to each option

#### 2.2 Seal Selector
**Available Seals:**
- **Gold Seal** - Gold wax seal stamp
- **Red Seal** - Red wax seal stamp
- **Purple Seal** - Purple wax seal stamp
- **Blue Seal** - Blue wax seal stamp
- **No Seal** - No seal (default)

**UI Design:**
- Radio button group with seal icons
- Visual preview showing seal placement on card
- Seal appears in top-right corner of card

#### 2.3 Source Location Selector
**Available Sources:**
- **Shop** - Cards appearing in shop
- **Packs** - Cards from booster packs
  - Standard Pack
  - Jumbo Pack
  - Mega Pack
  - Buffoon Pack
  - Spectral Pack
  - Celestial Pack
  - Arcana Pack
- **Tags** - Cards from skip tags
  - Uncommon Tag
  - Rare Tag
  - Negative Tag
  - Foil Tag
  - Holographic Tag
  - Polychrome Tag
  - Double Tag
  - Economy Tag
- **Starting Deck** - Initial deck cards
- **Rewards** - Boss blind rewards

**UI Design:**
- Hierarchical checkbox tree
- Expand/collapse for pack types and tag types
- "Any Source" option for no source filtering

### 3. Visual Card Builder Interface

#### 3.1 Layout Structure
```
┌─────────────────────────────────────────┐
│         PlayingCard Filter Builder       │
├─────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────────────┐ │
│ │             │ │                     │ │
│ │  Card Grid  │ │  Properties Panel   │ │
│ │   (52)      │ │  - Enhancement      │ │
│ │             │ │  - Seal             │ │
│ │             │ │  - Source           │ │
│ └─────────────┘ └─────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │        Preview & Summary             │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

#### 3.2 Card Grid Component
- 4 rows × 13 columns (one suit per row)
- Card size: 60×84 pixels (maintains aspect ratio)
- Hover effect: Slight scale and glow
- Selected state: Blue border highlight
- Enhancement overlay rendered on card
- Seal stamp in corner when applicable

#### 3.3 Preview Section
- Shows currently configured filter visually
- Displays selected cards with all properties
- Text summary: "Ace of Spades with Gold enhancement and Red Seal from Shop"
- Live updates as user makes selections

### 4. Integration with MotelyJsonFilterDesc

#### 4.1 Data Structure Mapping
```json
{
  "PlayingCards": [
    {
      "rank": "Ace",
      "suit": "Spades",
      "enhancement": "gold",
      "seal": "red",
      "source": ["shop"]
    }
  ]
}
```

#### 4.2 Filter Generation
- Convert visual selections to JSON structure
- Support AND/OR logic for multiple cards
- Handle "any" wildcards for unspecified properties
- Validate combinations before saving

### 5. User Interactions

#### 5.1 Workflow
1. User selects one or more cards from grid
2. Applies enhancement/seal/source properties
3. Previews the filter configuration
4. Adds to filter criteria
5. Can create multiple PlayingCard filters with different properties

#### 5.2 Bulk Operations
- Apply properties to all selected cards at once
- Copy properties from one card to others
- Create card ranges (e.g., "All Hearts with Gold enhancement")
- Template presets for common configurations

### 6. Visual Design Specifications

#### 6.1 Color Scheme
- Follow existing Balatro/app theme
- Card backgrounds: Standard playing card colors
- Enhancement effects: Match in-game appearances
- Selection highlight: Balatro blue (#4A90E2)
- Hover state: Subtle glow effect

#### 6.2 Animations
- Smooth transitions for hover states
- Card flip animation when selecting
- Enhancement effect animations (subtle shimmer/glow)
- Seal stamp "press" animation when applied

### 7. Technical Implementation

#### 7.1 Components Structure
```
PlayingCardFilterItem.axaml
├── CardSelectionGrid.axaml
│   └── PlayingCardTile.axaml
├── CardPropertiesPanel.axaml
│   ├── EnhancementSelector.axaml
│   ├── SealSelector.axaml
│   └── SourceSelector.axaml
└── FilterPreview.axaml
```

#### 7.2 Data Binding
- Two-way binding for all selections
- Observable collections for selected cards
- Property change notifications for live preview
- Validation on filter generation

### 8. Acceptance Criteria

1. **Card Selection**
   - [ ] User can select individual cards
   - [ ] User can multi-select cards
   - [ ] User can select by suit/rank groups
   - [ ] Selected cards are visually distinct

2. **Property Application**
   - [ ] Enhancement can be applied to selected cards
   - [ ] Seal can be applied to selected cards
   - [ ] Source location can be specified
   - [ ] Properties show visual preview on cards

3. **Filter Generation**
   - [ ] Generates valid MotelyJsonFilterDesc JSON
   - [ ] Supports all Motely-supported properties
   - [ ] Validates filter before saving
   - [ ] Can be tested immediately in search

4. **Visual Fidelity**
   - [ ] Cards look like Balatro cards
   - [ ] Enhancements match game appearance
   - [ ] Seals appear correctly positioned
   - [ ] Responsive and smooth interactions

5. **User Experience**
   - [ ] Intuitive without documentation
   - [ ] Fast and responsive
   - [ ] Clear visual feedback
   - [ ] Undo/redo capability

### 9. Future Enhancements

- Drag-and-drop card arrangement
- Advanced filter logic builder (AND/OR/NOT)
- Save filter templates
- Import filters from game saves
- Preview filter results before running
- Card probability calculator based on filter

### 10. Success Metrics

- User can create complex PlayingCard filters in <30 seconds
- 90% of filters created generate valid Motely queries
- Zero crashes or hangs during filter creation
- Visual preview matches actual search results

## Appendix A: Enhancement Visual Reference

| Enhancement | Visual Effect | Color/Style |
|------------|--------------|-------------|
| Glass | Translucent overlay | Light blue tint |
| Steel | Metallic sheen | Silver gradient |
| Gold | Golden shine | Gold gradient |
| Lucky | Clover pattern | Green glow |
| Mult | ×2 overlay | Red highlight |
| Bonus | +chips overlay | Blue highlight |
| Wild | Wild text | Purple effect |
| Stone | Rock texture | Gray overlay |

## Appendix B: Seal Placement Specification

Seals appear in the top-right corner of cards, sized at 20×20 pixels, with a slight overlap on the card edge to simulate a wax seal effect.

## Appendix C: Source Icon Mapping

| Source | Icon | Description |
|--------|------|-------------|
| Shop | 🛒 | Shopping cart |
| Pack | 📦 | Package box |
| Tag | 🏷️ | Price tag |
| Reward | 🏆 | Trophy |
| Starting | ▶️ | Play button |

---

*This PRD ensures that the PlayingCard visual filter designer provides an intuitive, visually rich interface for creating filters that leverage all of Motely's existing PlayingCard filtering capabilities.*