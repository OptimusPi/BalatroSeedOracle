# All Fixes Ready for Testing

## Critical Fixes

### 1. Deck/Stake JSON Regeneration ✅
**File**: `src/ViewModels/FiltersModalViewModel.cs:881`
**Fix**: Changed from magic number `if (value == 2)` to proper check `if (IsJsonTabVisible)`
**Result**: When switching to JSON Editor tab, regenerates JSON with current SelectedDeck and SelectedStake values

### 2. Volume Slider Direction ✅
**File**: `src/Views/BalatroMainMenu.axaml:56-57`
**Fix**: Swapped `Minimum="100" Maximum="0"` so visual matches interaction
**Result**: Drag up = louder, drag down = quieter (no more runaway thumb)

### 3. Standard Cards - All Suits for Enhancements ✅
**File**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs:1743-1773`
**Fix**: Loop through all 4 suits for each enhancement type
**Cards Added**: 260 new enhanced cards
- Mult: Hearts, Spades, Diamonds, Clubs (52 cards)
- Bonus: All 4 suits (52 cards)
- Glass: All 4 suits (52 cards)
- Gold: All 4 suits (52 cards)
- Steel: All 4 suits (52 cards)

**Groups**: 20 new groups with suit names
- "Mult Cards - Hearts", "Mult Cards - Spades", etc.

### 4. Scrollbar Positioning ✅
**File**: `src/Components/FilterTabs/VisualBuilderTab.axaml:507`
**Fix**: Added `Padding="0,0,4,0"` to ScrollViewer
**Result**: Scrollbar inside border, cards pushed left (nothing hidden)

### 5. SetSeal Skips Favorites ✅
**File**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs:2531-2532`
**Fix**: Added Favorites skip check (matches SetEdition behavior)
**Result**: Clicking seal buttons won't modify favorited items

### 6. Removed Unnecessary Seal "None" Button ✅
**File**: `src/Components/FilterTabs/VisualBuilderTab.axaml:427`
**Fix**: Removed None button - seals work as toggles
**Result**: Saves screen space, consistent with toggle UX

## Build Status
**Waiting for app to close** - then auto-rebuild will apply all fixes.

## Test Checklist
- [ ] Deck/stake selection → switch to JSON tab → verify deck/stake in JSON
- [ ] Volume slider → drag up (louder), drag down (quieter), thumb follows mouse
- [ ] Standard Card category → see all enhancement × suit combinations
- [ ] Item shelf scrollbar → inside border, cards not hidden
- [ ] Seal buttons → don't modify Favorites
