# Deck/Stake UX Flow Enhancement

## Problem
Current UX is confusing - unclear distinction between:
- **Preferred Deck** (saved in filter config)
- **Search Instance Deck** (runtime override for current search)

## Solution Overview
Separate the concerns:
- **Filter Designer Modal**: Edit and save Preferred Deck/Stake to filter config
- **Search Modal**: View Preferred Deck (read-only info) + Override for current search instance

## Changes Required

### 1. Filter Designer Modal (FiltersModal)
**No Changes Required** - Already has "Deck/Stake" tab for editing
- Rename tab: "Deck/Stake" → "Preferred Deck"
- This is where users SET the preferred deck that gets saved to the filter JSON
- Keeps existing deck/stake selector UI

### 2. Search Modal (SearchModal)

#### Tab: "Preferred Deck" (Informational - Read-Only)
- Shows the filter's saved Preferred Deck/Stake
- Shows deck sprite + stake overlay
- Shows the warning message: "This filter's criteria includes deck-specific items..."
- **Read-only** - user cannot change the filter's preferred deck here
- **NOT the default tab** - just for reference

#### Tab: "Search" (Functional - Editable)
Add runtime deck/stake override spinners:

```
Deck / Stake
[<] [ Ghost Deck ]  [>]
[<] [ Gold Stake ]  [>]
```

- Use spinner control (like CPU threads selector)
- Allows user to override deck/stake for **current search instance only**
- Does NOT modify the filter file
- Defaults to filter's Preferred Deck/Stake (or Red Deck + White Stake if not set)

## Implementation Tasks

### Task 1: Rename Tab in Filter Designer
**File:** `src/Views/Modals/FiltersModal.axaml`
- Change tab name: "Deck/Stake" → "Preferred Deck"

### Task 2: Create Deck/Stake Spinner Component (if not exists)
**Check if exists first:** Look for reusable spinner component
**If not exists, create:** `src/Components/DeckStakeSpinner.axaml`
- Two spinners (Deck + Stake)
- Same style as CPU threads spinner
- Properties: `SelectedDeck`, `SelectedStake`
- Arrow buttons [<] [>] to cycle through enums

### Task 3: Add Spinners to Search Tab
**File:** `src/Views/Modals/SearchModal.axaml` (Search tab)
- Add Deck/Stake spinners to Search tab UI
- Bind to SearchViewModel properties
- Label: "Deck / Stake"

### Task 4: Update SearchViewModel
**File:** `src/ViewModels/SearchModalViewModel.cs`
- Ensure `SelectedDeck` and `SelectedStake` properties exist
- Default to filter's Preferred Deck/Stake (or Red/White if null)
- These override values are used for the search instance only

### Task 5: Update Preferred Deck Tab (Read-Only Info)
**File:** `src/Views/Modals/SearchModal.axaml` (Preferred Deck tab)
- Make sure it's read-only display
- Shows filter's saved Preferred Deck
- Shows warning message about deck-specific items
- **Set default tab to Search or Filter Setup** (not this info tab)

## Expected Behavior

### Filter Designer Flow
1. User opens Filter Designer
2. User edits filter on "Visual Builder" tab
3. User switches to "Preferred Deck" tab
4. User selects deck/stake that filter is designed for
5. User saves filter → Preferred Deck saved to JSON

### Search Flow
1. User opens Search Modal
2. Default tab: "Filter Setup" or "Search" (NOT "Preferred Deck")
3. User can view "Preferred Deck" tab to see filter's saved deck/stake (read-only)
4. User can override deck/stake on "Search" tab using spinners
5. Override affects ONLY current search instance (not saved to filter)

## Benefits
- **Clear separation**: Editing filter config vs. running search instance
- **Flexibility**: Can search with different deck/stake without modifying filter
- **Transparency**: Read-only info tab shows what the filter was designed for
- **Smart defaults**: Search instance defaults to filter's Preferred Deck

## Notes
- Red Deck + White Stake is the default fallback
- SearchViewModel likely already has initialization logic for this
- Filter's Preferred Deck is stored in `MotelyJsonConfig.Deck` and `MotelyJsonConfig.Stake`
