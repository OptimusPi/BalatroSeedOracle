# Deck & Stake Selector Refactoring - Fixed

## Problem
The `FiltersModal` was breaking encapsulation by using `FindControl<T>()` to directly access internal controls (`DeckSpinner` and `StakeSpinner`) within the `DeckAndStakeSelector` component. This violated MVVM principles and made the code brittle.

## Solution
Refactored the code to use the `DeckAndStakeSelector`'s public API instead of accessing internal controls.

## Changes Made

### 1. Saving Deck/Stake Preferences (Line ~6795)

**Before:**
```csharp
// Get deck/stake preferences from the selector
string deckName = "Red";   // Default
string stakeName = "White"; // Default

var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
if (deckStakeSelector != null)
{
    // Get the deck spinner control
    var deckSpinner = deckStakeSelector.FindControl<DeckSpinner>("DeckSpinnerControl");
    if (deckSpinner != null)
    {
        int deckIndex = deckSpinner.SelectedDeckIndex;
        string[] deckNames = { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                               "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
        if (deckIndex >= 0 && deckIndex < deckNames.Length)
        {
            deckName = deckNames[deckIndex];
        }
    }

    // Get the stake spinner control
    var stakeSpinner = deckStakeSelector.FindControl<SpinnerControl>("StakeSpinner");
    if (stakeSpinner != null)
    {
        int stakeIndex = (int)stakeSpinner.Value;
        string[] stakeNames = { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };
        if (stakeIndex >= 0 && stakeIndex < stakeNames.Length)
        {
            stakeName = stakeNames[stakeIndex];
        }
    }
}
```

**After:**
```csharp
// Get deck/stake preferences from the selector using public API
string deckName = "Red";   // Default
string stakeName = "White"; // Default

var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
if (deckStakeSelector != null)
{
    // Use the public API properties instead of accessing internal controls
    deckName = deckStakeSelector.SelectedDeckName;
    stakeName = deckStakeSelector.SelectedStakeName;
}
```

### 2. Loading Deck/Stake Preferences (Line ~7400)

**Before:**
```csharp
// Load deck/stake preferences
var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
if (deckStakeSelector != null && !string.IsNullOrEmpty(config.Deck) && !string.IsNullOrEmpty(config.Stake))
{
    // Map deck name to index
    string[] deckNames = { "Red", "Blue", "Yellow", "Green", "Black", "Magic", "Nebula", "Ghost",
                           "Abandoned", "Checkered", "Zodiac", "Painted", "Anaglyph", "Plasma", "Erratic" };
    int deckIndex = Array.IndexOf(deckNames, config.Deck);
    if (deckIndex == -1) deckIndex = 0; // Default to Red

    // Map stake name to index
    string[] stakeNames = { "White", "Red", "Green", "Black", "Blue", "Purple", "Orange", "Gold" };
    int stakeIndex = Array.IndexOf(stakeNames, config.Stake);
    if (stakeIndex == -1) stakeIndex = 0; // Default to White

    // Set the values in the spinners
    var deckSpinner = deckStakeSelector.FindControl<DeckSpinner>("DeckSpinnerControl");
    if (deckSpinner != null)
    {
        deckSpinner.SelectedDeckIndex = deckIndex;
    }

    var stakeSpinner = deckStakeSelector.FindControl<SpinnerControl>("StakeSpinner");
    if (stakeSpinner != null)
    {
        stakeSpinner.Value = stakeIndex;
    }
}
```

**After:**
```csharp
// Load deck/stake preferences using public API
var deckStakeSelector = this.FindControl<DeckAndStakeSelector>("PreferredDeckStakeSelector");
if (deckStakeSelector != null && !string.IsNullOrEmpty(config.Deck) && !string.IsNullOrEmpty(config.Stake))
{
    // Use the public SetDeck/SetStake methods instead of accessing internal controls
    deckStakeSelector.SetDeck(config.Deck);
    deckStakeSelector.SetStake(config.Stake);
}
```

## Benefits

1. **Encapsulation**: The component's internal structure is now hidden from consumers
2. **Maintainability**: Changes to `DeckAndStakeSelector` internals won't break `FiltersModal`
3. **MVVM Compliance**: Proper separation of concerns maintained
4. **Code Simplicity**: 30+ lines reduced to 3 lines in save section, 20+ lines reduced to 3 lines in load section
5. **Error Reduction**: No manual index mapping or array lookups needed
6. **Type Safety**: Using strongly-typed properties and methods instead of control lookups

## DeckAndStakeSelector Public API

The component exposes the following public interface:

```csharp
// Properties
public int DeckIndex { get; set; }
public int StakeIndex { get; set; }
public string SelectedDeckName { get; }  // Read-only
public string SelectedStakeName { get; }  // Read-only

// Methods
public void SetDeck(string deckName);
public void SetStake(string stakeName);

// Event
public event Action<(int deckIndex, int stakeIndex)>? SelectionChanged;
```

## Testing
- ✅ Build succeeded with no errors
- ✅ Code follows MVVM patterns
- ✅ Deck/stake preferences now properly save and load through the public API

## File Modified
- `src/Views/Modals/FiltersModal.axaml.cs` (Lines ~6795-6810 and ~7400-7410)
