# PRD: Add "Preferred Deck" Tab to Filters Modal

## Overview
Move the deck/stake selector from the Save Filter tab to its own dedicated left-nav tab called "Preferred Deck". This uses the existing shared `DeckAndStakeSelector` component (already used in Analyzer modal).

## Current State
- **Save Filter Tab** has inline deck/stake spinners (lines 67-103 in SaveFilterTab.axaml)
- **Analyzer Modal** already uses the shared `DeckAndStakeSelector` component successfully
- Deck/stake selection is buried in the Save tab, making it hard to find

## Goal
Create a new "Preferred Deck" tab in the Filters modal left navigation that:
1. Uses the shared `DeckAndStakeSelector` component (NOT inline spinners)
2. Allows users to set their preferred deck/stake for filter design
3. Removes deck/stake selectors from Save Filter tab (simplifies that tab)
4. Matches the UX pattern from Analyzer modal's DECK/STAKE tab

## Shared Component Reference
**File:** `x:\BalatroSeedOracle\src\Components\DeckAndStakeSelector.axaml`

**What it provides:**
- DeckSpinner (Balatro-style panel with deck art, title, description, dots)
- Stake SpinnerControl (stake selector with display values)
- CONTINUE button
- Proper ViewModel binding via DataContext

**Usage example from Analyzer modal (line 174):**
```xml
<components:DeckAndStakeSelector Name="DeckAndStakeSelector"
                                HorizontalAlignment="Center"
                                VerticalAlignment="Center"/>
```

## Implementation Tasks

### 1. Create PreferredDeckTab.axaml + .cs
**Location:** `x:\BalatroSeedOracle\src\Components\FilterTabs\PreferredDeckTab.axaml`

**Content structure:**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:components="using:BalatroSeedOracle.Components"
             xmlns:vm="using:BalatroSeedOracle.ViewModels.FilterTabs"
             x:Class="BalatroSeedOracle.Components.FilterTabs.PreferredDeckTab"
             x:DataType="vm:VisualBuilderTabViewModel">

    <StackPanel Spacing="24" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,40,0,0">
        <!-- Explanatory Text -->
        <TextBlock Text="Select the deck and stake you're designing this filter for"
                  FontFamily="{StaticResource BalatroFont}"
                  FontSize="13"
                  Foreground="{StaticResource VeryLightGrey}"
                  TextWrapping="Wrap"
                  HorizontalAlignment="Center"
                  TextAlignment="Center"
                  Opacity="0.8"
                  MaxWidth="400"/>

        <!-- Shared Deck and Stake Selector Component -->
        <components:DeckAndStakeSelector Name="DeckAndStakeSelector"
                                        HorizontalAlignment="Center"
                                        VerticalAlignment="Center"/>
    </StackPanel>
</UserControl>
```

**Code-behind (.cs):**
```csharp
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class PreferredDeckTab : UserControl
    {
        public PreferredDeckTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
```

### 2. Update FiltersModal.axaml - Add Left Nav Tab
**File:** `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml`

**Find the left nav category buttons (around line 90-130)**

**Add new tab button AFTER "Configure" but BEFORE "Save":**
```xml
<Button Content="Preferred Deck"
        Classes="nav-button"
        Click="OnTabClick"
        Tag="PreferredDeck"
        HorizontalAlignment="Stretch"/>
```

### 3. Update FiltersModal.axaml - Add Tab Content Panel
**Add new Grid row for PreferredDeckTab content:**
```xml
<!-- Preferred Deck Tab -->
<Grid Grid.Column="2" IsVisible="{Binding PreferredDeckTabVisible}">
    <filtertabs:PreferredDeckTab DataContext="{Binding}"/>
</Grid>
```

### 4. Update FiltersModal.axaml.cs - Wire Up Navigation
**File:** `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml.cs`

**In `OnTabClick` method, add case:**
```csharp
case "PreferredDeck":
    _viewModel.ActiveTab = FilterTabType.PreferredDeck;
    break;
```

### 5. Update FiltersModalViewModel.cs - Add Tab Enum + Visibility
**File:** `x:\BalatroSeedOracle\src\ViewModels\FiltersModalViewModel.cs`

**Add to FilterTabType enum:**
```csharp
public enum FilterTabType
{
    Configure,
    VisualBuilder,
    JsonEditor,
    PreferredDeck,  // NEW
    Save
}
```

**Add visibility property:**
```csharp
public bool PreferredDeckTabVisible => ActiveTab == FilterTabType.PreferredDeck;
```

**Update OnActiveTabChanged to notify visibility:**
```csharp
partial void OnActiveTabChanged(FilterTabType value)
{
    OnPropertyChanged(nameof(ConfigureTabVisible));
    OnPropertyChanged(nameof(VisualBuilderTabVisible));
    OnPropertyChanged(nameof(JsonEditorTabVisible));
    OnPropertyChanged(nameof(PreferredDeckTabVisible));  // NEW
    OnPropertyChanged(nameof(SaveTabVisible));
}
```

### 6. Update SaveFilterTab.axaml - REMOVE Deck/Stake Section
**File:** `x:\BalatroSeedOracle\src\Components\FilterTabs\SaveFilterTab.axaml`

**DELETE lines 67-103** (entire "Preferred Deck & Stake" section)

**Update Grid.RowDefinitions to remove row 6:**
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>  <!-- Header -->
    <RowDefinition Height="20"/>    <!-- Spacer -->
    <RowDefinition Height="Auto"/>  <!-- Filter Name -->
    <RowDefinition Height="10"/>    <!-- Spacer -->
    <RowDefinition Height="Auto"/>  <!-- Description -->
    <RowDefinition Height="20"/>    <!-- Spacer -->
    <!-- REMOVED: Preferred Deck & Stake row -->
    <RowDefinition Height="Auto"/>  <!-- Current Filter Info -->
    <RowDefinition Height="20"/>    <!-- Spacer -->
    <RowDefinition Height="Auto"/>  <!-- Action Buttons -->
    <RowDefinition Height="*"/>     <!-- Status -->
</Grid.RowDefinitions>
```

**Update Grid.Row indices for elements after deleted section:**
- Current Filter Info: Grid.Row="6" (was 8)
- Spacer: Grid.Row="7" (was 9)
- Action Buttons: Grid.Row="8" (was 10)
- Status: Grid.Row="9" (was 11)

### 7. Wire Up DeckAndStakeSelector DataContext
**The component needs access to VisualBuilderTabViewModel's deck/stake properties**

**Option A (Recommended):** DeckAndStakeSelector already has its own ViewModel
- Check `DeckAndStakeSelectorViewModel.cs` for properties
- If it exists, wire it up in PreferredDeckTab code-behind

**Option B:** Share VisualBuilderTabViewModel
- PreferredDeckTab already has `x:DataType="vm:VisualBuilderTabViewModel"`
- DeckAndStakeSelector will inherit DataContext from parent

## Success Criteria
✅ New "Preferred Deck" tab appears in left nav (between Configure and Save)
✅ Tab shows DeckAndStakeSelector component (deck spinner + stake selector + continue button)
✅ Deck/stake selection persists in VisualBuilderTabViewModel.SelectedDeck/SelectedStake
✅ Save Filter tab simplified (deck/stake section removed)
✅ Build succeeds without errors
✅ Navigation between tabs works correctly
✅ Bouncing arrow indicator points to active tab

## Files to Modify
1. `x:\BalatroSeedOracle\src\Components\FilterTabs\PreferredDeckTab.axaml` (CREATE)
2. `x:\BalatroSeedOracle\src\Components\FilterTabs\PreferredDeckTab.axaml.cs` (CREATE)
3. `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml` (MODIFY - add nav button + content panel)
4. `x:\BalatroSeedOracle\src\Views\Modals\FiltersModal.axaml.cs` (MODIFY - add nav case)
5. `x:\BalatroSeedOracle\src\ViewModels\FiltersModalViewModel.cs` (MODIFY - add enum + visibility)
6. `x:\BalatroSeedOracle\src\Components\FilterTabs\SaveFilterTab.axaml` (MODIFY - remove deck/stake section)

## Notes
- The shared DeckAndStakeSelector component already exists and is battle-tested in Analyzer modal
- No new ViewModel needed - reuse existing ViewModels
- This simplifies the Save tab and makes deck selection more discoverable
- Follow the same pattern as Analyzer modal's DECK/STAKE tab
