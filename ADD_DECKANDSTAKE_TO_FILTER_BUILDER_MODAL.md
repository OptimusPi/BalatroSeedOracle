# Product Requirements Document: Add Deck/Stake Tab to Filter Builder Modal

## Overview
Add a dedicated "Deck/Stake" tab to the Filter Builder Modal to improve UX by separating deck and stake selection from the Save/Test tab, reducing the need for scrolling and creating a more organized interface.

## Problem Statement
Currently, the deck and stake selectors are embedded in the Save/Test (ValidateFilterTab) along with filter name, description, test controls, and clause displays. This creates:
- **Poor UX**: Users must scroll down too far to see filter results after testing
- **Cluttered Interface**: Too many controls in a single tab
- **Inconsistent Design**: The Analyzer Modal has deck/stake prominently displayed, but the Filter Builder buries them

## Proposed Solution
Create a new "Deck/Stake" tab that appears between "Build Filter" and "JSON Editor" tabs, providing a dedicated space for deck and stake selection similar to the Analyzer Modal's design.

## Requirements

### 1. New Tab Addition
- **Tab Name**: "Deck/Stake"
- **Tab Position**: Between "Build Filter" and "JSON Editor"
- **Tab Order**:
  1. Build Filter
  2. **Deck/Stake** (NEW)
  3. JSON Editor
  4. Save

### 2. Deck/Stake Tab Content
The new tab should contain:

#### Visual Layout
- **Header**: "SELECT DECK & STAKE" with Balatro font styling
- **Content Area**: Two SpinnerControl components arranged vertically with proper spacing
- **Background**: Consistent with other tabs (dark theme with card-based styling)

#### Components
1. **Deck Selector**
   - Use `controls:SpinnerControl`
   - Properties:
     - Label: "Deck"
     - SpinnerType: "deck"
     - Value binding: `{Binding SelectedDeckIndex}`
     - Range: 0-14
     - DisplayValues: List of deck names
     - ReadOnly: True

2. **Stake Selector**
   - Use `controls:SpinnerControl`
   - Properties:
     - Label: "Stake"
     - SpinnerType: "stake"
     - Value binding: `{Binding SelectedStakeIndex}`
     - Range: 0-7
     - DisplayValues: List of stake names
     - ReadOnly: True

#### Visual Design
- Center the controls horizontally
- Add descriptive text explaining impact on filter results
- Consider adding visual previews of selected deck/stake
- Maintain consistent spacing and padding with other tabs

### 3. Update Save/Test Tab
Remove the following from ValidateFilterTab:
- The "Test Configuration" section containing deck and stake spinners
- Keep only:
  - Filter name and description inputs
  - Test controls (Live Search button, etc.)
  - Results display
  - Filter requirements (Must/MustNot/Should clauses)

### 4. Data Flow & State Management
- Deck and stake selections should persist across tab switches
- Values should be shared between:
  - New Deck/Stake tab
  - JSON Editor (for filter configuration)
  - Save/Test tab (for testing with selected deck/stake)
- Update `FiltersModalViewModel` to maintain these properties

## Implementation Details

### Files to Modify
1. **X:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml**
   - Add new TabItem for "Deck/Stake"

2. **X:\BalatroSeedOracle\src\Components\FilterTabs\ValidateFilterTab.axaml**
   - Remove deck/stake spinner controls
   - Adjust layout to be more compact

3. **New File: X:\BalatroSeedOracle\src\Components\FilterTabs\DeckStakeTab.axaml**
   - Create new UserControl for the tab content

4. **New File: X:\BalatroSeedOracle\src\ViewModels\FilterTabs\DeckStakeTabViewModel.cs**
   - Create ViewModel for the new tab

5. **X:\BalatroSeedOracle\src\ViewModels\FiltersModalViewModel.cs**
   - Add property for DeckStakeTabViewModel
   - Ensure deck/stake values are properly shared

### Code Reference
Use the Analyzer Modal implementation as reference:
```xml
<!-- From AnalyzerView.axaml -->
<controls:SpinnerControl Label="Deck"
                        SpinnerType="deck"
                        Value="{Binding SelectedDeckIndex}"
                        Minimum="0"
                        Maximum="14"
                        ReadOnly="True"
                        DisplayValues="{Binding DeckDisplayValues}"/>
```

## Benefits
1. **Improved UX**: Less scrolling required to see test results
2. **Better Organization**: Logical separation of configuration vs testing
3. **Consistency**: Aligns with Analyzer Modal's prominent deck/stake display
4. **Cleaner Interface**: Each tab has a focused purpose
5. **Scalability**: Room for future deck/stake-related features

## Success Criteria
- [ ] New "Deck/Stake" tab appears in correct position
- [ ] Deck and stake spinners function correctly
- [ ] Values persist across tab switches
- [ ] Save/Test tab no longer contains deck/stake controls
- [ ] Test results are immediately visible without scrolling
- [ ] Visual consistency with existing tabs maintained

## Future Enhancements
- Add visual previews of selected deck artwork
- Show stake modifiers/effects
- Quick preset buttons for common deck/stake combinations
- Deck/stake statistics or recommendations based on filter

## Timeline
- **Phase 1**: Create new tab structure and basic controls
- **Phase 2**: Remove controls from Save/Test tab
- **Phase 3**: Polish visual design and test thoroughly