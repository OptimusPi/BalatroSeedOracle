# Balatro Seed Oracle - MVP Product Requirements Document

**Status**: Active Development
**Priority**: BLOCKING MVP RELEASE
**Last Updated**: 2025-11-20

---

## Executive Summary

This document tracks all MVP-blocking features and fixes for Balatro Seed Oracle. These items MUST be completed before release. Non-MVP features should be deferred to post-release.

---

## 1. COMPLETED MVP Fixes

### 1.1 Wild Cards Category ✅
- **Status**: COMPLETED
- **Description**: Added purple "Wild Cards" category button
- **Changes**:
  - Created purple category button in [VisualBuilderTab.axaml:86-102](src/Components/FilterTabs/VisualBuilderTab.axaml#L86-L102)
  - Moved wildcards from Favorites to Wild Cards category
  - Added category handling in [VisualBuilderTabViewModel.cs:559-571](src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs#L559-L571)

### 1.2 Grid Column Fix ✅
- **Status**: COMPLETED
- **Description**: Changed item grid from 6 columns to 5 columns for better visual fit
- **Changes**:
  - Updated UniformGrid Columns property to 5 in [VisualBuilderTab.axaml:543](src/Components/FilterTabs/VisualBuilderTab.axaml#L543)
  - Increased left column width from 115px to 135px for better spacing

### 1.3 Edition Buttons Off-by-One Fix ✅
- **Status**: COMPLETED
- **Description**: Fixed edition button sprite mapping (foil button was showing none, etc.)
- **Root Cause**: Edition sprites were incorrectly mapped starting at position 0 instead of position 1
- **Changes**:
  - Corrected sprite position mapping in [SpriteService.cs:1529-1537](src/Services/SpriteService.cs#L1529-L1537):
    - none/normal: position 0 (was unmapped)
    - foil: position 1 (was 0) ← FIXED
    - holographic: position 2 (was 1) ← FIXED
    - polychrome: position 3 (was 2) ← FIXED
    - debuffed: position 4 (was 3) ← FIXED

### 1.4 Card Drag Opacity Fix ✅
- **Status**: COMPLETED
- **Description**: Entire card (image + label) now hides during drag operation
- **Root Cause**: Opacity binding was on Border element instead of root StackPanel
- **Changes**:
  - Moved opacity binding to root StackPanel in [FilterItemCard.axaml:15](src/Components/FilterItemCard.axaml#L15)
  - Ensures both image and label fade during drag/respawn cycle

### 1.5 Card Size Standardization ✅
- **Status**: COMPLETED
- **Description**: Standardized all cards to 71px × 95px everywhere
- **Changes**:
  - [FilterItemCard.axaml:14](src/Components/FilterItemCard.axaml#L14) - Width 80→71px
  - [FilterOperatorControl.axaml:131-194](src/Components/FilterOperatorControl.axaml#L131-L194) - 50×70→71×95px (4 locations)
  - [CategorySizeConverters.cs](src/Converters/CategorySizeConverters.cs) - Simplified to return 71.0 and 95.0

### 1.6 Section Labels ✅
- **Status**: COMPLETED
- **Description**: Added "EDITION", "STICKERS", "SEALS" labels for clarity
- **Changes**:
  - Added section labels in [VisualBuilderTab.axaml:292-297,376-381,420-425](src/Components/FilterTabs/VisualBuilderTab.axaml)

### 1.7 Deck/Stake Selection Fix ✅
- **Status**: COMPLETED (2025-11-20)
- **Description**: Deck/stake changes now properly save and reflect in JSON tab
- **Root Cause**: JSON tab was reloading from disk instead of regenerating from current state
- **Changes**:
  - Modified [FiltersModalViewModel.cs:900-925](src/ViewModels/FiltersModalViewModel.cs#L900-L925)
  - JSON Editor tab (index 2) now regenerates from current state instead of reloading from disk
  - Ensures deck/stake changes from Deck/Stake tab are immediately reflected

---

## 2. IN-PROGRESS MVP Features

### 2.1 Transition Designer Widget 🎬
- **Status**: IN-PROGRESS (BLOCKING MVP)
- **Priority**: HIGH
- **Description**: New widget for designing and testing audio/visual transitions

#### Requirements:
1. **Widget Icon**: 🎬 (movie slapper board emoji)
2. **Layout**:
   ```
   🎬 Transition Designer

   Audio:
   [Music Mix A ▼] → [Music Mix B ▼]

   Visual:
   [Visual Preset A ▼] → [Visual Preset B ▼]

   Test Transition:
   |----O------------------------]  (Balatro red thermometer slider)

   Easing: [Dropdown ▼] [Test Transition]

   [SAVE TRANSITION]
   ```

3. **Slider Behavior**:
   - 0.0f = Pure A side
   - 0.5f = Interpolated halfway (lerp)
   - 1.0f = Pure B side
   - Manual scrubbing for real-time preview
   - "Test Transition" button animates from 0.0 to 1.0
   - Disable user input during test animation

4. **Dropdown Options**:
   - Audio dropdowns: List of saved music mixes + "Default"
   - Visual dropdowns: List of saved visual presets + "Default"
   - "Default" option allows testing audio-only or visual-only transitions

5. **Easing Options**:
   - Use Avalonia easing types (Linear, CubicEaseOut, etc.)

6. **Save Transition**:
   - Creates transition object with: A side, B side, easing, type (audio/visual)

#### Implementation Files:
- [ ] Create `src/Components/Widgets/TransitionDesignerWidget.axaml`
- [ ] Create `src/ViewModels/TransitionDesignerWidgetViewModel.cs`
- [ ] Register widget in app initialization
- [ ] Use Balatro red thermometer slider style (reference existing slider styles)

#### Design Principles:
- **AGNOSTIC**: Transitions are type-agnostic (can apply to audio, visuals, or anything)
- **MODULAR**: Separate audio transitions from visual transitions
- **CLEAN**: Remove transition code from AudioVisualizerSettingsWidget (see 2.2)

---

## 3. PENDING MVP Tasks

### 3.1 Clean Up AudioVisualizerSettingsWidget
- **Status**: PENDING (depends on 2.1)
- **Description**: Remove transition-related buttons after Transition Designer is complete
- **Changes Needed**:
  - Remove lines 91-94 from [AudioVisualizerSettingsWidget.axaml](src/Components/Widgets/AudioVisualizerSettingsWidget.axaml):
    - "Save Transition..." button
    - "Load Transition..." button
    - "Animate Visual + Mix → B" button
  - Remove corresponding ViewModel commands
  - Simplify the widget to focus on visualizer settings only

### 3.2 Music Mixer Mute Icon Clarification
- **Status**: PENDING
- **Description**: Mute/unmute icon is confusing - needs clear visual state
- **Changes Needed**:
  - Review current mute icon in Music Mixer widget
  - Ensure icon clearly shows muted vs unmuted state
  - Consider using standard mute/unmute icons (speaker with X vs speaker with waves)

### 3.3 Music Mix Save/Load Verification
- **Status**: PENDING (VERIFY)
- **Description**: Verify "SAVE A MIX" and "LOAD A MIX" functionality works correctly
- **Test Cases**:
  - [ ] Save a custom mix with specific slider values
  - [ ] Load the saved mix and verify slider values are restored
  - [ ] Verify mix list updates after save
  - [ ] Verify mix can be selected from dropdown in Transition Designer (after 2.1)

---

## 4. Known Issues (Non-Blocking)

### 4.1 Background Builds
- Two background bash processes running:
  - `8d4ebd`: Parcel pack installer (building release package)
  - `2c789b`: dotnet run (app was running, now closed)
- **Action**: User can check build status if needed

---

## 5. Testing Checklist

Before marking MVP complete, verify ALL of the following:

### Visual Builder Tab
- [ ] Wild Cards category shows all "Any" type wildcards
- [ ] Grid displays 5 columns for Jokers and Vouchers
- [ ] Edition buttons (none, foil, holographic, polychrome) show correct sprites
- [ ] Edition buttons set correct edition values
- [ ] Dragging a card hides both image AND label from shelf
- [ ] All cards are 71px × 95px (item shelf, drop zones, clause trays)
- [ ] Section labels "EDITION", "STICKERS", "SEALS" are visible

### Filter Builder Modal - Tab Switching
- [ ] Switching from Visual Builder tab saves current state
- [ ] Switching from Deck/Stake tab saves deck/stake selection
- [ ] Switching TO JSON Editor tab regenerates JSON from current state
- [ ] JSON shows updated deck/stake after changing in Deck/Stake tab
- [ ] Switching TO Validate Filter tab refreshes validation data

### Deck/Stake Selection
- [ ] Deck dropdown shows all 15 decks (Red through Erratic)
- [ ] Stake dropdown shows all 8 stakes (White through Gold)
- [ ] Selecting deck updates JSON when switching to JSON tab
- [ ] Selecting stake updates JSON when switching to JSON tab
- [ ] Deck/stake selections persist when switching tabs

### Transition Designer (After Implementation)
- [ ] Widget icon is 🎬 (movie slapper board)
- [ ] Audio dropdowns list all saved music mixes + "Default"
- [ ] Visual dropdowns list all saved visual presets + "Default"
- [ ] Slider is Balatro red thermometer style
- [ ] Manual scrubbing works (0.0 to 1.0)
- [ ] "Test Transition" button animates smoothly
- [ ] User input disabled during test animation
- [ ] Easing dropdown shows Avalonia easing types
- [ ] "SAVE TRANSITION" creates transition object
- [ ] AudioVisualizerSettingsWidget no longer has transition buttons

---

## 6. Post-MVP Features (DEFERRED)

These features are nice-to-have but NOT blocking MVP release:

- Additional visualizer effects
- More transition easing options
- Transition presets library
- Keyboard shortcuts for widgets
- Widget snap-to-grid
- Widget groups/tabs
- Export visualizer settings
- Import community presets

---

## 7. Development Notes

### Recent Session Context
- Fixed deck/stake selection JSON refresh issue (2025-11-20)
- Working on Transition Designer widget implementation
- User requested PRD to track MVP progress and avoid getting lost
- User is frustrated with scope creep and wants to focus on MVP only

### Key User Quotes
- "NEED THE MVP APP! NEED THE MVP!!!"
- "I DONT WANNA GET LOST!"
- "PLEASE FOR THE LOVE OF COMPACTING YOU BASTARD!!!!!"

### Development Principles
- Focus ONLY on MVP blocking items
- Defer all non-essential features
- Maintain code quality and consistency
- Follow Balatro visual style
- Keep implementation simple and functional
