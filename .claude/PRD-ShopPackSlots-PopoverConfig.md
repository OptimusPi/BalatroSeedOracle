# Product Requirements Document: Shop & Pack Slots Popover Configuration

## Executive Summary
Create an intuitive popover configuration interface that allows users to specify exact slot positions for items in the shop and various pack types. This feature enables precise filtering for "Item X in slot Y of shop/pack Z" scenarios, which Motely already supports in the backend.

## Overview
The Shop & Pack Slots Popover will provide a visual slot selection interface that appears when users need to specify WHERE an item should appear (shop slot 1-5, pack slot 1-5, etc.). This complements existing item filters by adding positional requirements.

## Core Requirements

### 1. Popover Trigger Points

#### 1.1 Trigger Contexts
- **Shop Items Filter**: "Configure slot positions" button
- **Pack Items Filter**: "Configure slot positions" button  
- **Joker Filter**: "Specify shop/pack position" option
- **Tarot Filter**: "Specify shop/pack position" option
- **Planet Filter**: "Specify shop/pack position" option
- **Spectral Filter**: "Specify shop/pack position" option
- **Playing Card Filter**: "Specify source position" option
- **Voucher Filter**: "Specify shop position" option

#### 1.2 Trigger UI
- Small gear/position icon (📍) next to item selectors
- Tooltip: "Configure exact slot position"
- Opens popover on click
- Shows active indicator when positions are configured

### 2. Popover Interface Design

#### 2.1 Layout Structure
```
┌────────────────────────────────────────┐
│   Configure Slot Positions              │
│                                         │
│ ┌─────────────────────────────────────┐│
│ │ SHOP SLOTS                          ││
│ │ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐     ││
│ │ │ 1 │ │ 2 │ │ 3 │ │ 4 │ │ 5 │     ││
│ │ └───┘ └───┘ └───┘ └───┘ └───┘     ││
│ └─────────────────────────────────────┘│
│                                         │
│ ┌─────────────────────────────────────┐│
│ │ PACK SLOTS                          ││
│ │ ┌─────────────────┐                 ││
│ │ │ Pack Type ▼     │                 ││
│ │ └─────────────────┘                 ││
│ │ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐     ││
│ │ │ 1 │ │ 2 │ │ 3 │ │ 4 │ │ 5 │     ││
│ │ └───┘ └───┘ └───┘ └───┘ └───┘     ││
│ └─────────────────────────────────────┘│
│                                         │
│ [✓ Apply] [Clear] [Cancel]             │
└────────────────────────────────────────┘
```

#### 2.2 Visual Slot Representation
- **Slot Size**: 80×112 pixels (Balatro card proportions)
- **States**:
  - Empty: Dashed border, gray background
  - Hovered: Blue glow effect
  - Selected: Solid blue border, slight scale
  - Configured: Shows mini preview of item type
- **Numbering**: Clear slot numbers (1-5)
- **Multi-select**: Ctrl+click for multiple slots

### 3. Shop Slots Configuration

#### 3.1 Shop Layout
- 5 horizontal slots representing shop positions
- Slots numbered 1-5 from left to right
- Visual matches in-game shop layout

#### 3.2 Shop-Specific Options
- **Any Shop Slot**: No specific position required (default)
- **Specific Slots**: Select one or more exact positions
- **Slot Ranges**: "First 2 slots", "Last 2 slots", "Middle slot"
- **Exclusions**: "Not in slot X" option

#### 3.3 Reroll Consideration
- Checkbox: "Must appear in initial shop (no rerolls)"
- Checkbox: "Can appear after rerolls"
- Info tooltip explaining reroll mechanics

### 4. Pack Slots Configuration

#### 4.1 Pack Type Selector
**Available Pack Types**:
- **Arcana Pack** - 4 cards
- **Celestial Pack** - 4 cards
- **Spectral Pack** - 4 cards
- **Standard Pack** - 4 cards
- **Buffoon Pack** - 2 cards
- **Jumbo Pack** - 5 cards
- **Mega Pack** - 5 cards

#### 4.2 Dynamic Slot Count
- Slot count adjusts based on selected pack type
- Buffoon Pack: 2 slots
- Standard/Arcana/Celestial/Spectral: 4 slots
- Jumbo/Mega: 5 slots
- Disabled slots gray out when pack changes

#### 4.3 Pack Opening Options
- **Choice Selection**: "Player must choose this slot"
- **Skip Selection**: "Player must skip this slot"
- **Any Selection**: "Doesn't matter if chosen" (default)

### 5. Advanced Slot Logic

#### 5.1 Combination Rules
- **AND Logic**: Item must be in ALL selected slots (rare)
- **OR Logic**: Item can be in ANY selected slot (default)
- **Sequential**: "Slots 1 then 2 then 3" for multi-pack scenarios

#### 5.2 Conditional Slots
- **If-Then Rules**: "If Joker in slot 1, then Tarot in slot 2"
- **Mutual Exclusion**: "Either slot 1 OR slot 5, not both"
- **Dependencies**: "Only if slot 3 is empty"

#### 5.3 Special Positions
- **Voucher Slot**: Special handling for single voucher slot
- **Boss Blind Reward**: Position in reward selection
- **Skip Tag Rewards**: Position in tag reward options

### 6. Visual Feedback & Preview

#### 6.1 Live Preview Panel
```
Current Configuration:
• Shop: Slots 1, 3
• Arcana Pack: Slot 2
• Condition: Must appear before ante 4
```

#### 6.2 Slot Highlighting
- Selected slots pulse with soft animation
- Configured slots show item type icon
- Invalid combinations show red warning

#### 6.3 Tooltips
- Hover over slot: "Click to require item in slot 1"
- Configured slot: "Item must appear in this position"
- Pack type: "4 cards, choose 1"

### 7. Data Integration

#### 7.1 JSON Output Format
```json
{
  "SlotRequirements": {
    "Shop": {
      "slots": [1, 3],
      "logic": "OR",
      "rerollAllowed": true
    },
    "Packs": {
      "Arcana": {
        "slots": [2],
        "mustChoose": true
      }
    }
  }
}
```

#### 7.2 MotelyJsonFilterDesc Mapping
- Maps directly to Motely's position filters
- Supports all existing slot-based criteria
- Validates against Motely's constraints

### 8. User Experience Flow

#### 8.1 Basic Workflow
1. User configuring Joker filter
2. Clicks position icon next to joker selection
3. Popover appears with slot options
4. User clicks shop slots 1 and 2
5. Clicks Apply
6. Main filter shows "Joker (Shop slots 1-2)"

#### 8.2 Quick Actions
- **Quick Templates**:
  - "First shop slot only"
  - "Any pack position"
  - "Middle three slots"
- **Recent Configurations**: Remember last 5 slot configs
- **Copy/Paste**: Copy slot config between filters

### 9. Popover Behavior

#### 9.1 Opening/Closing
- **Open**: Click trigger icon, focus trap enabled
- **Close**: Click outside, Escape key, Cancel button
- **Position**: Anchored to trigger, auto-adjust if near edge
- **Animation**: Smooth fade/scale in (200ms)

#### 9.2 State Management
- Maintains configuration while open
- Can preview without applying
- Reverts on cancel
- Saves on apply

#### 9.3 Keyboard Navigation
- Tab: Navigate between slots
- Space/Enter: Toggle slot selection
- Number keys 1-5: Quick select slot
- Escape: Close without saving
- Ctrl+Enter: Apply and close

### 10. Visual Design Specifications

#### 10.1 Popover Styling
- Background: Modal gray with subtle transparency
- Border: 2px Balatro blue border
- Shadow: Soft drop shadow for depth
- Max width: 400px
- Padding: 16px

#### 10.2 Slot Styling
- Background: Dark modal gray
- Border: 2px dashed gray (empty), solid blue (selected)
- Corner radius: 8px (card-like)
- Transition: All animations 150ms ease

#### 10.3 Typography
- Headers: Balatro font, 14px, gold color
- Labels: Balatro font, 12px, white
- Slot numbers: Bold, 16px, centered

### 11. Technical Implementation

#### 11.1 Component Structure
```
SlotPositionPopover.axaml
├── ShopSlotsSection.axaml
│   └── SlotButton.axaml (×5)
├── PackSlotsSection.axaml
│   ├── PackTypeSelector.axaml
│   └── SlotButton.axaml (×5)
├── PreviewSection.axaml
└── ActionButtons.axaml
```

#### 11.2 State Management
- Local state for temporary selection
- Commits to parent filter on apply
- Two-way binding for live updates
- Validation before applying

### 12. Acceptance Criteria

1. **Slot Selection**
   - [ ] Can select individual shop slots
   - [ ] Can select pack slots with type
   - [ ] Multi-select works correctly
   - [ ] Visual feedback is clear

2. **Pack Types**
   - [ ] All pack types available
   - [ ] Slot count adjusts correctly
   - [ ] Pack-specific rules work

3. **Configuration**
   - [ ] Generates valid JSON
   - [ ] Integrates with filters
   - [ ] Preview shows config
   - [ ] Can clear configuration

4. **UX Polish**
   - [ ] Smooth animations
   - [ ] Keyboard accessible
   - [ ] Touch-friendly
   - [ ] Responsive layout

### 13. Future Enhancements

- Visual slot preview with actual items
- Probability calculator for slot combinations
- Auto-suggest optimal slot positions
- Slot history tracking
- Import slot patterns from seeds
- Animated slot machine effect

### 14. Success Metrics

- Configuration time <10 seconds
- Zero invalid slot configurations
- 95% of users understand slot numbering
- Reduced filter complexity for position-based searches

## Appendix A: Slot Position Reference

| Location | Slot Count | Numbering | Notes |
|----------|------------|-----------|-------|
| Shop | 5 | Left to right (1-5) | Voucher separate |
| Arcana Pack | 4 | Top to bottom | Choose 1 |
| Celestial Pack | 4 | Top to bottom | Choose 1 |
| Spectral Pack | 4 | Top to bottom | Choose 1 |
| Standard Pack | 4 | Grid 2×2 | Choose 1 |
| Buffoon Pack | 2 | Side by side | Choose 1 |
| Jumbo Pack | 5 | Grid layout | Choose 1 |
| Mega Pack | 5 | Grid layout | Choose 2 |

## Appendix B: Motely Integration Points

The slot configuration maps directly to Motely's existing position filters:
- `shop_slot`: 1-5 for shop positions
- `pack_slot`: Position within specific pack type
- `source_position`: Combined source and position filter

---

*This PRD ensures users can visually configure exact slot positions for any item type, leveraging Motely's existing positional filtering capabilities through an intuitive popover interface.*