# TAB SWITCHING BUG FIX - Complete Implementation

## Problem Summary
When switching tabs in the Filter Builder modal, changes made in the current tab were NOT being saved before switching, and the new tab was NOT loading fresh data from the shared state.

**Critical Bug Example:**
1. User goes to Deck/Stake tab
2. User selects "Yellow" deck
3. User clicks JSON Editor tab
4. JSON still shows `"deck": "red"` (stale data)

## Root Cause
The `OnSelectedTabIndexChanged` handler in `FiltersModalViewModel.cs` was trying to use temp file save/load but:
- Did NOT call save/load methods on individual tab ViewModels
- Did NOT regenerate JSON from current state when switching TO JSON Editor tab
- Did NOT parse JSON and update state when switching FROM JSON Editor tab

## Solution Implemented

### File Modified: `src/ViewModels/FiltersModalViewModel.cs`

#### 1. Added Using Statement
```csharp
using Avalonia.Media;  // For Brushes in JSON Editor status colors
```

#### 2. Refactored `OnSelectedTabIndexChanged` Method
```csharp
partial void OnSelectedTabIndexChanged(int value)
{
    DebugLogger.Log("FiltersModalViewModel", $"🔄 Tab switching: FROM tab {_selectedTabIndex - 1} TO tab {value}");

    // CRITICAL: Save current tab's data to the shared filter state BEFORE switching
    SaveCurrentTabData();

    UpdateTabVisibility(value);
    OnPropertyChanged(nameof(CurrentTabContent));

    // CRITICAL: Load fresh data into the new tab AFTER switching
    LoadTabData(value);
}
```

#### 3. Added `SaveCurrentTabData()` Method
Saves current tab's state to parent ViewModel BEFORE switching:

- **Tab 0 (Build Filter)**: Auto-syncs via collection bindings (no action needed)
- **Tab 1 (Deck/Stake)**: Auto-saves via two-way binding (logs current values)
- **Tab 2 (JSON Editor)**: Parses JSON and calls `LoadConfigIntoState()` to update parent
- **Tab 3 (Validate)**: Read-only (no save needed)

#### 4. Added `LoadTabData()` Method
Loads fresh data into new tab AFTER switching:

- **Tab 0 (Build Filter)**: Auto-loads from parent collections (no action needed)
- **Tab 1 (Deck/Stake)**: Auto-loads via two-way binding (logs current values)
- **Tab 2 (JSON Editor)**: Calls `RegenerateJsonFromState()` to rebuild JSON
- **Tab 3 (Validate)**: Calls `RefreshSaveTabData()` to update display

#### 5. Added `SaveJsonEditorToState()` Helper
Parses JSON from JSON Editor and updates parent ViewModel state:
- Validates JSON syntax
- Deserializes to `MotelyJsonConfig`
- Calls `LoadConfigIntoState()` to update all parent properties (deck, stake, must, should, etc.)

#### 6. Added `RegenerateJsonFromState()` Helper
Regenerates JSON from current parent ViewModel state:
- Calls `BuildConfigFromCurrentState()` to get current filter config
- Includes deck/stake from parent ViewModel
- Serializes to JSON with proper formatting
- Updates JSON Editor content and status

#### 7. Removed Obsolete Temp File Methods
Deleted unused methods:
- `SaveFilterStateToTempFile()`
- `LoadFilterStateFromTempFile()`

## Data Flow

### Tab Switching Flow (Deck/Stake → JSON Editor)
```
1. User selects "Yellow" deck in Deck/Stake tab
   ↓ (two-way binding)
2. SelectedDeckIndex = 2 in FiltersModalViewModel
   ↓
3. User clicks JSON Editor tab
   ↓
4. OnSelectedTabIndexChanged(2) fires
   ↓
5. SaveCurrentTabData() → logs deck/stake (already saved via binding)
   ↓
6. LoadTabData(2) → RegenerateJsonFromState()
   ↓
7. BuildConfigFromCurrentState() → reads SelectedDeckIndex = 2 → "Yellow"
   ↓
8. JSON Editor now shows: "deck": "Yellow" ✅
```

### Tab Switching Flow (JSON Editor → Deck/Stake)
```
1. User edits JSON: "deck": "Blue", "stake": "purple"
   ↓
2. User clicks Deck/Stake tab
   ↓
3. OnSelectedTabIndexChanged(1) fires
   ↓
4. SaveCurrentTabData() → SaveJsonEditorToState()
   ↓
5. Parse JSON → LoadConfigIntoState()
   ↓
6. SelectedDeckIndex = 1 ("Blue"), SelectedStakeIndex = 5 ("purple")
   ↓
7. LoadTabData(1) → logs current values
   ↓
8. Deck/Stake tab shows Blue deck, Purple stake ✅
```

## Testing Checklist

### Test Case 1: Deck/Stake → JSON Editor
- [ ] Select Yellow deck in Deck/Stake tab
- [ ] Switch to JSON Editor tab
- [ ] Verify JSON shows `"deck": "Yellow"`
- [ ] Verify JSON shows correct stake

### Test Case 2: JSON Editor → Deck/Stake
- [ ] Edit JSON to set `"deck": "Blue"`
- [ ] Switch to Deck/Stake tab
- [ ] Verify Blue deck is selected in spinner

### Test Case 3: Build Filter → JSON Editor
- [ ] Add 3 jokers to MUST zone in Build Filter
- [ ] Switch to JSON Editor
- [ ] Verify JSON shows 3 items in `"must": []`

### Test Case 4: JSON Editor → Build Filter
- [ ] Edit JSON to add a new joker to `"must"`
- [ ] Switch to Build Filter tab
- [ ] Verify new joker appears in MUST zone

### Test Case 5: All Tabs Round-Trip
- [ ] Build Filter: Add items
- [ ] Deck/Stake: Select Yellow deck, Gold stake
- [ ] JSON Editor: Verify all changes are present
- [ ] Validate: Verify all changes are present
- [ ] Go back to Deck/Stake: Verify Yellow/Gold still selected

## Impact Assessment

### Changes Made
- ✅ Modified 1 file: `src/ViewModels/FiltersModalViewModel.cs`
- ✅ Added 1 using statement
- ✅ Refactored 1 method: `OnSelectedTabIndexChanged`
- ✅ Added 4 new methods: `SaveCurrentTabData`, `LoadTabData`, `SaveJsonEditorToState`, `RegenerateJsonFromState`
- ✅ Removed 2 obsolete methods: `SaveFilterStateToTempFile`, `LoadFilterStateFromTempFile`

### Unchanged Components
- ❌ No changes to `DeckStakeTabViewModel.cs` (already works via two-way binding)
- ❌ No changes to `JsonEditorTabViewModel.cs` (save/load handled by parent)
- ❌ No changes to `VisualBuilderTabViewModel.cs` (already auto-syncs)
- ❌ No changes to XAML files

### Risk Level: LOW
- Uses existing methods (`BuildConfigFromCurrentState`, `LoadConfigIntoState`)
- Follows existing MVVM patterns
- No breaking changes to public APIs
- No changes to persistence layer

## Debug Logging

All tab switches now produce detailed logs:
```
FiltersModalViewModel: 🔄 Tab switching: FROM tab 1 TO tab 2
FiltersModalViewModel: 💾 Saving data from tab index 1
FiltersModalViewModel: ✅ Deck/Stake saved: Deck=Yellow (2), Stake=0
FiltersModalViewModel: 📂 Loading data into tab index 2
FiltersModalViewModel: ✅ JSON regenerated: 3 must, 2 should, Deck=Yellow, Stake=white
```

## Pre-existing Build Issues (NOT related to this fix)
The following XAML errors existed BEFORE this fix:
- `SaveFilterTab.axaml` lines 79, 117, 155: Multiple Child assignments
- These are unrelated to the tab switching bug fix
