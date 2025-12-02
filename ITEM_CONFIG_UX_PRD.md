# Item Configuration UI Redesign - PRD

## Problem Statement
The current ItemConfigPopup (388 lines) that appears when right-clicking dropped items is:
- ❌ Way too big - can't see all elements
- ❌ Not UX friendly
- ❌ Not user-friendly
- ❌ Takes up too much screen space

## Current Implementation
**File**: `src/Controls/ItemConfigPopup.axaml` (388 lines)
**Triggered**: Right-click on item in MUST/SHOULD drop zones
**Purpose**: Configure item properties (edition, stickers, seals, antes, score, label, min count)

## Requirements

### Must Configure:
1. **Edition** (Jokers/Standard Cards): None, Foil, Holographic, Polychrome, Negative
2. **Stickers** (Jokers): Eternal, Perishable, Rental
3. **Seals** (Standard Cards): None, Purple, Gold, Red, Blue
4. **Antes**: Checkboxes for antes 1-8
5. **Score**: Numeric input
6. **Label**: Text input (optional friendly name)
7. **Min Count**: For "at least N" requirements

### Design Goals:
- ✅ **Compact**: Fit everything in a small, scannable area
- ✅ **Clear**: User can see all options without scrolling
- ✅ **Fast**: Quick to configure and close
- ✅ **Contextual**: Only show relevant options (e.g., seals only for playing cards)
- ✅ **Balatro themed**: Match game visual style

### Proposed Improvements:
1. **Two-column layout** instead of vertical stack
2. **Smaller controls** (compact checkboxes, smaller text)
3. **Grouped sections** with clear visual separation
4. **Smart visibility** (hide irrelevant options based on item type)
5. **Quick actions** row at bottom (Apply, Cancel, Delete)

### Alternative: Use ItemConfigPanel?
There's a shorter version at `src/Components/ItemConfigPanel.axaml` (226 lines).
- Check if it's better designed
- If yes, switch to using Panel instead of Popup
- If no, redesign Popup to be more compact

## Success Criteria
- [ ] All config options visible without scrolling
- [ ] Popup size reduced by at least 30%
- [ ] User can configure item in < 5 seconds
- [ ] Matches Balatro visual theme
- [ ] Works for all item types (Jokers, Playing Cards, Vouchers, etc.)

## Files to Modify
- `src/Controls/ItemConfigPopup.axaml` (redesign)
- `src/ViewModels/ItemConfigPopupViewModel.cs` (verify logic)
- OR switch to `src/Components/ItemConfigPanel.axaml` if it's better

## Context
This is the "hard part" of finishing the MVP - item configuration is critical for creating filters but the current UI is frustrating to use. User wants a complete redesign that makes sense.
