# PRD: Filter Test & Validation System

**Priority**: 🔥 HIGH - User Experience Critical
**Status**: BROKEN/UGLY - Needs Complete Redesign
**Impact**: Users can't verify their filters work before running full searches

---

## Executive Summary

**THE PROBLEM**: The Filter Test modal is ugly, confusing, and doesn't actually test anything useful. It just shows "Ready to test... ?" with no clear action.

**USER'S EXACT WORDS**:
> "yikes i forgot about this modal tab contents hyahahgahahahah look how ugly hahahah its ok no worries make a PRD to fix this modal."

**WHAT IT SHOULD DO**:
1. When user clicks "TEST FILTER" button
2. Start a quick seed search using the current filter
3. Stop as soon as 1 matching seed is found
4. Show the found seed as proof the filter works
5. Mark filter as "VERIFIED ✓" with a green checkmark
6. If no seed found after reasonable time, show "NO MATCHES FOUND" warning

---

## Current State (UGLY)

### What's Wrong:
- ❌ Just says "Ready to test... ?" - no actual testing happens
- ❌ Ugly placeholder UI that doesn't match Balatro's design
- ❌ Deck/Stake selectors showing "0" - unclear what they do
- ❌ No visual feedback or results
- ❌ No verification status indicator
- ❌ Doesn't follow Balatro's clean, card-game aesthetic
- ❌ **Filter Name field is empty** - should be PRE-FILLED with current filter name!
- ❌ **Description field is empty** - should load existing description if filter was loaded

### Current Location:
- File: `x:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml`
- Modal: SaveFilterModal → "Filter Test" tab
- Test button binding: `{Binding TestFilterCommand}`

---

## Balatro Design Principles (from Screenshots)

### Visual Language:
1. **Color Palette**:
   - Primary action buttons: Bright RED (#FF4444)
   - Secondary/Back buttons: ORANGE/GOLD (#FF9933)
   - Success indicators: GREEN checkmarks
   - Background: Dark teal/grey (#2D3642)
   - Text: WHITE with Balatro font

2. **Layout**:
   - Clean, centered content
   - Generous padding and spacing
   - Clear visual hierarchy
   - Simple, uncluttered design

3. **Feedback**:
   - Immediate visual response
   - Clear success/failure states
   - Checkboxes with ✓ marks
   - Status text in appropriate colors

4. **Typography**:
   - Balatro font for headers
   - Clean, readable body text
   - ALL CAPS for emphasis
   - Clear labels with context

---

## New Design Specification

### Layout Structure

```
┌─────────────────────────────────────────────────────┐
│                   🔬 FILTER TEST                    │
├─────────────────────────────────────────────────────┤
│                                                     │
│   ┌─────────────────────────────────────────────┐  │
│   │         TEST RESULT DISPLAY AREA            │  │
│   │                                             │  │
│   │  [Status: Idle / Testing / Success / Fail] │  │
│   │                                             │  │
│   │  [Seed display if found]                   │  │
│   │  [Stats: Time taken, seeds checked]        │  │
│   │                                             │  │
│   └─────────────────────────────────────────────┘  │
│                                                     │
│            ┌───────────────────┐                    │
│            │   TEST FILTER     │ (RED button)       │
│            └───────────────────┘                    │
│                                                     │
│   Filter Status: [✓ VERIFIED] or [⚠ UNVERIFIED]   │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### State Machine

#### State 1: IDLE (Initial State)
```
┌─────────────────────────────────────────────────────┐
│              Ready to test your filter              │
│                                                     │
│         Click TEST FILTER to verify this            │
│           filter can find matching seeds            │
│                                                     │
│              [Large red TEST FILTER button]         │
│                                                     │
│         Status: ⚠ Filter not yet tested            │
└─────────────────────────────────────────────────────┘
```

#### State 2: TESTING (Active Search)
```
┌─────────────────────────────────────────────────────┐
│                🔍 TESTING FILTER...                 │
│                                                     │
│         Searching for matching seeds...             │
│                                                     │
│         Seeds checked: 1,234,567                    │
│         Time elapsed: 2.5s                          │
│         Speed: 492K seeds/sec                       │
│                                                     │
│              [STOP TEST button - orange]            │
└─────────────────────────────────────────────────────┘
```

#### State 3: SUCCESS (Seed Found!)
```
┌─────────────────────────────────────────────────────┐
│              ✅ FILTER VERIFIED!                    │
│                                                     │
│         Found matching seed:                        │
│                                                     │
│         ┌─────────────────────────────┐             │
│         │    SEED: A1B2C3D4          │             │
│         │    Score: 1,234            │             │
│         │    [📋 Copy]               │             │
│         └─────────────────────────────┘             │
│                                                     │
│    Checked 567,890 seeds in 1.2 seconds            │
│                                                     │
│              [TEST AGAIN button]                    │
│                                                     │
│         Status: ✓ Filter works correctly!          │
└─────────────────────────────────────────────────────┘
```

#### State 4: FAILURE (No Match Found)
```
┌─────────────────────────────────────────────────────┐
│           ⚠ NO MATCHING SEEDS FOUND                 │
│                                                     │
│    Searched 10,000,000 seeds without finding        │
│           a match for this filter.                  │
│                                                     │
│         This could mean:                            │
│         • Filter is too restrictive                 │
│         • Need to search more seeds                 │
│         • Filter may have an error                  │
│                                                     │
│              [TRY AGAIN button]                     │
│              [EDIT FILTER button]                   │
│                                                     │
│         Status: ⚠ No verification yet              │
└─────────────────────────────────────────────────────┘
```

---

## Technical Implementation

### ViewModel: SaveFilterViewModel

#### New Properties:
```csharp
// Test state
[ObservableProperty]
private FilterTestState _testState = FilterTestState.Idle;

[ObservableProperty]
private string _testStatusMessage = "Filter not yet tested";

[ObservableProperty]
private bool _isTestRunning = false;

[ObservableProperty]
private bool _filterVerified = false;

// Test results
[ObservableProperty]
private SearchResult? _foundSeed = null;

[ObservableProperty]
private long _seedsChecked = 0;

[ObservableProperty]
private TimeSpan _testDuration = TimeSpan.Zero;

[ObservableProperty]
private string _testSpeed = "0 seeds/sec";
```

#### New Commands:
```csharp
[RelayCommand]
private async Task TestFilter()
{
    TestState = FilterTestState.Testing;
    IsTestRunning = true;

    var startTime = DateTime.Now;
    var searchCancellation = new CancellationTokenSource();

    try
    {
        // Create temporary search instance
        var tempSearch = new SearchInstance(CurrentFilter, maxResults: 1);

        tempSearch.ProgressUpdated += (s, progress) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                SeedsChecked = progress.SeedsSearched;
                TestDuration = DateTime.Now - startTime;
                TestSpeed = FormatSpeed(progress.Speed);
            });
        };

        tempSearch.ResultFound += (s, result) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                FoundSeed = result;
                FilterVerified = true;
                TestState = FilterTestState.Success;
                TestStatusMessage = "✓ Filter works correctly!";
            });

            // Stop immediately after finding first result
            tempSearch.Stop();
        };

        // Start search
        await tempSearch.StartAsync(searchCancellation.Token);

        // If we get here without finding a seed, it failed
        if (!FilterVerified)
        {
            TestState = FilterTestState.NoMatch;
            TestStatusMessage = "⚠ No matching seeds found";
        }
    }
    catch (Exception ex)
    {
        TestState = FilterTestState.Error;
        TestStatusMessage = $"Error: {ex.Message}";
    }
    finally
    {
        IsTestRunning = false;
    }
}

[RelayCommand]
private void StopTest()
{
    // Cancel ongoing test
    searchCancellation?.Cancel();
}

[RelayCommand]
private async Task CopyTestSeed()
{
    if (FoundSeed != null)
    {
        await ClipboardService.CopyToClipboardAsync(FoundSeed.Seed);
        // Show toast notification
    }
}
```

### Enum: FilterTestState
```csharp
public enum FilterTestState
{
    Idle,       // Not tested yet
    Testing,    // Currently searching
    Success,    // Found a matching seed
    NoMatch,    // Searched but found nothing
    Error       // Error occurred during test
}
```

---

## XAML Design (Balatro-Styled)

### Test Tab Content:
```xml
<Grid Padding="20" RowDefinitions="Auto,*,Auto,Auto">

    <!-- Header -->
    <TextBlock Grid.Row="0"
               Text="🔬 FILTER TEST"
               FontFamily="{StaticResource BalatroFont}"
               FontSize="24"
               Foreground="{StaticResource Gold}"
               HorizontalAlignment="Center"
               Margin="0,0,0,20"/>

    <!-- Test Result Display Area -->
    <Border Grid.Row="1"
            Background="{StaticResource DarkBackground}"
            BorderBrush="{StaticResource ModalBorder}"
            BorderThickness="2"
            CornerRadius="8"
            Padding="30"
            MinHeight="250">

        <!-- IDLE STATE -->
        <StackPanel IsVisible="{Binding TestState, Converter={StaticResource EnumEquals}, ConverterParameter=Idle}"
                    Spacing="20"
                    VerticalAlignment="Center">
            <TextBlock Text="Ready to test your filter"
                       FontSize="18"
                       Foreground="{StaticResource White}"
                       HorizontalAlignment="Center"/>
            <TextBlock Text="Click TEST FILTER to verify this filter can find matching seeds"
                       FontSize="14"
                       Foreground="{StaticResource LightGrey}"
                       TextWrapping="Wrap"
                       TextAlignment="Center"
                       MaxWidth="400"/>
        </StackPanel>

        <!-- TESTING STATE -->
        <StackPanel IsVisible="{Binding TestState, Converter={StaticResource EnumEquals}, ConverterParameter=Testing}"
                    Spacing="15"
                    VerticalAlignment="Center">
            <TextBlock Text="🔍 TESTING FILTER..."
                       FontFamily="{StaticResource BalatroFont}"
                       FontSize="20"
                       Foreground="{StaticResource Gold}"
                       HorizontalAlignment="Center"/>
            <TextBlock Text="Searching for matching seeds..."
                       FontSize="14"
                       Foreground="{StaticResource White}"
                       HorizontalAlignment="Center"/>

            <StackPanel Spacing="8" Margin="0,20,0,0">
                <TextBlock Text="{Binding SeedsChecked, StringFormat='Seeds checked: {0:N0}'}"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"/>
                <TextBlock Text="{Binding TestDuration, StringFormat='Time elapsed: {0:hh\\:mm\\:ss}'}"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"/>
                <TextBlock Text="{Binding TestSpeed}"
                           Foreground="{StaticResource Gold}"
                           HorizontalAlignment="Center"/>
            </StackPanel>
        </StackPanel>

        <!-- SUCCESS STATE -->
        <StackPanel IsVisible="{Binding TestState, Converter={StaticResource EnumEquals}, ConverterParameter=Success}"
                    Spacing="20"
                    VerticalAlignment="Center">
            <TextBlock Text="✅ FILTER VERIFIED!"
                       FontFamily="{StaticResource BalatroFont}"
                       FontSize="22"
                       Foreground="{StaticResource Green}"
                       HorizontalAlignment="Center"/>
            <TextBlock Text="Found matching seed:"
                       FontSize="14"
                       Foreground="{StaticResource White}"
                       HorizontalAlignment="Center"/>

            <!-- Seed Display Card -->
            <Border Background="{StaticResource ModalGrey}"
                    BorderBrush="{StaticResource Green}"
                    BorderThickness="2"
                    CornerRadius="8"
                    Padding="20"
                    MaxWidth="300">
                <StackPanel Spacing="10">
                    <TextBlock Text="{Binding FoundSeed.Seed, StringFormat='SEED: {0}'}"
                               FontFamily="Consolas"
                               FontSize="16"
                               
                               Foreground="{StaticResource Gold}"
                               HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding FoundSeed.TotalScore, StringFormat='Score: {0:N0}'}"
                               Foreground="{StaticResource White}"
                               HorizontalAlignment="Center"/>
                    <Button Content="📋 Copy Seed"
                            Command="{Binding CopyTestSeedCommand}"
                            Classes="btn-red"
                            HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>

            <TextBlock Text="{Binding SeedsChecked, StringFormat='Checked {0:N0} seeds in {1:0.0} seconds'}"
                       Foreground="{StaticResource LightGrey}"
                       HorizontalAlignment="Center"
                       FontSize="12"/>
        </StackPanel>

        <!-- NO MATCH STATE -->
        <StackPanel IsVisible="{Binding TestState, Converter={StaticResource EnumEquals}, ConverterParameter=NoMatch}"
                    Spacing="20"
                    VerticalAlignment="Center">
            <TextBlock Text="⚠ NO MATCHING SEEDS FOUND"
                       FontFamily="{StaticResource BalatroFont}"
                       FontSize="20"
                       Foreground="{StaticResource Red}"
                       HorizontalAlignment="Center"/>
            <TextBlock TextWrapping="Wrap"
                       TextAlignment="Center"
                       MaxWidth="400"
                       Foreground="{StaticResource White}"
                       FontSize="14">
                <Run Text="Searched "/>
                <Run Text="{Binding SeedsChecked, StringFormat='{0:N0}'}" />
                <Run Text=" seeds without finding a match for this filter."/>
            </TextBlock>

            <StackPanel Spacing="8">
                <TextBlock Text="This could mean:"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"
                           FontSize="12"/>
                <TextBlock Text="• Filter is too restrictive"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"
                           FontSize="12"/>
                <TextBlock Text="• Need to search more seeds"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"
                           FontSize="12"/>
                <TextBlock Text="• Filter may have an error"
                           Foreground="{StaticResource LightGrey}"
                           HorizontalAlignment="Center"
                           FontSize="12"/>
            </StackPanel>
        </StackPanel>

    </Border>

    <!-- Action Buttons -->
    <StackPanel Grid.Row="2"
                Orientation="Horizontal"
                HorizontalAlignment="Center"
                Spacing="15"
                Margin="0,20,0,0">

        <!-- TEST FILTER button (shown when Idle or NoMatch) -->
        <Button Content="TEST FILTER"
                Command="{Binding TestFilterCommand}"
                Classes="btn-red"
                FontSize="16"
                
                Padding="40,12"
                IsVisible="{Binding !IsTestRunning}"/>

        <!-- STOP TEST button (shown when Testing) -->
        <Button Content="STOP TEST"
                Command="{Binding StopTestCommand}"
                Classes="btn-orange"
                FontSize="16"
                Padding="40,12"
                IsVisible="{Binding IsTestRunning}"/>

        <!-- TEST AGAIN button (shown when Success) -->
        <Button Content="TEST AGAIN"
                Command="{Binding TestFilterCommand}"
                Classes="btn-red"
                FontSize="14"
                Padding="30,10"
                IsVisible="{Binding TestState, Converter={StaticResource EnumEquals}, ConverterParameter=Success}"/>
    </StackPanel>

    <!-- Status Footer -->
    <Border Grid.Row="3"
            Background="{StaticResource DarkBackground}"
            BorderBrush="{StaticResource ModalBorder}"
            BorderThickness="0,2,0,0"
            Padding="15"
            Margin="0,20,0,0">
        <TextBlock Text="{Binding TestStatusMessage}"
                   FontSize="14"
                   HorizontalAlignment="Center">
            <TextBlock.Foreground>
                <MultiBinding Converter="{StaticResource TestStateToColorConverter}">
                    <Binding Path="FilterVerified"/>
                    <Binding Path="TestState"/>
                </MultiBinding>
            </TextBlock.Foreground>
        </TextBlock>
    </Border>

</Grid>
```

---

## Test Behavior

### Test Parameters:
- **Max seeds to check**: 10,000,000 (10M)
- **Stop condition**: First matching seed found OR max seeds reached
- **Timeout**: 30 seconds max
- **Thread**: Background thread with UI updates via Dispatcher

### Search Instance Configuration:
```csharp
var testSearch = new SearchInstance(filter)
{
    MaxResults = 1,              // Stop after 1 result
    MaxSeedsToSearch = 10_000_000, // Don't search forever
    ProgressUpdateInterval = 100,  // Update UI every 100ms
};
```

### Performance Targets:
- Should find common seeds within 1-2 seconds
- Should timeout gracefully if filter is too restrictive
- Should not block UI thread
- Should clean up resources properly

---

## Validation States

### Filter Badge (Show in Save tab):
```xml
<!-- Show in the Save Filter tab header or near filter name -->
<Border Background="{StaticResource Green}"
        CornerRadius="12"
        Padding="8,4"
        IsVisible="{Binding FilterVerified}">
    <TextBlock Text="✓ VERIFIED"
               FontSize="10"
               
               Foreground="White"/>
</Border>

<Border Background="{StaticResource Red}"
        CornerRadius="12"
        Padding="8,4"
        IsVisible="{Binding !FilterVerified}">
    <TextBlock Text="⚠ UNVERIFIED"
               FontSize="10"
               
               Foreground="White"/>
</Border>
```

---

## Edge Cases

### 1. Filter Changes After Test
- If user modifies filter after successful test, reset `FilterVerified = false`
- Show warning: "Filter modified - test again to verify"

### 2. Very Restrictive Filters
- Stop after 10M seeds checked
- Show helpful message about filter being too restrictive
- Suggest relaxing some constraints

### 3. Invalid Filters
- Catch exceptions during test
- Show clear error message
- Don't mark as verified

### 4. Rapid Test Spam
- Debounce test button (min 500ms between tests)
- Cancel previous test if new one starts

---

## Success Criteria

### Must Have (P0):
- ✅ Test actually runs a real seed search
- ✅ Stops after finding 1 matching seed
- ✅ Shows found seed with copy button
- ✅ Updates in real-time during test
- ✅ Marks filter as VERIFIED on success
- ✅ Handles "no match found" gracefully
- ✅ Matches Balatro visual design

### Nice to Have (P1):
- ✅ Show stats (seeds checked, time, speed)
- ✅ Animated loading state
- ✅ Toast notification on success
- ✅ Export test result as proof
- ✅ Remember verification status in saved filter

### Polish (P2):
- ✅ Sound effect on success
- ✅ Confetti animation for verified filter
- ✅ Share test result to Discord
- ✅ Test history log

---

## Additional Fix: Pre-fill Filter Name & Description

### Problem:
When user opens Save Filter modal, the Filter Name and Description fields are EMPTY. But the user already HAS a filter loaded (either from Visual Builder or JSON Editor or loaded from file).

### Solution:
```csharp
// In SaveFilterViewModel constructor or OnLoad:
if (!string.IsNullOrEmpty(CurrentFilter.Name))
{
    FilterName = CurrentFilter.Name;
}

if (!string.IsNullOrEmpty(CurrentFilter.Description))
{
    FilterDescription = CurrentFilter.Description;
}

// If filter was loaded from file, use that filename as default:
if (!string.IsNullOrEmpty(LoadedFilterFileName))
{
    FilterName = Path.GetFileNameWithoutExtension(LoadedFilterFileName);
}
```

This way, the user doesn't have to re-type the filter name every time they want to save!

---

## Files to Modify

### 1. ViewModel
- `x:\BalatroSeedOracle\src\ViewModels\SaveFilterViewModel.cs`
  - Add test state properties
  - Implement `TestFilterCommand`
  - Add `StopTestCommand`
  - Handle search lifecycle
  - **Fix: Pre-fill FilterName and FilterDescription from current filter**

### 2. View (XAML)
- `x:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml`
  - Find SaveFilterModal "Filter Test" tab content
  - Replace entire content with new design
  - Add state-based visibility

### 3. Models (if needed)
- Add `FilterTestState` enum
- Possibly extend `FilterMetadata` to store verification status

### 4. Converters (if needed)
- `EnumEqualsConverter` - check enum state
- `TestStateToColorConverter` - map state to color

---

## Testing Plan

### Manual Tests:
1. **Simple Filter Test**:
   - Create filter: MUST have "Joker"
   - Click TEST FILTER
   - Should find seed within 1 second
   - Verify: Shows seed, score, copy button works

2. **Restrictive Filter Test**:
   - Create filter: MUST have 10 rare jokers + 5 vouchers + specific conditions
   - Click TEST FILTER
   - Should run for several seconds
   - Eventually show "NO MATCH FOUND" (or find seed if lucky)

3. **Stop Test**:
   - Start test for restrictive filter
   - Click STOP TEST mid-search
   - Verify: Search stops, no crash

4. **Re-test**:
   - Test filter successfully
   - Modify filter slightly
   - Verify: Verification badge disappears
   - Re-test and verify again

5. **UI States**:
   - Verify all 4 states render correctly
   - Check colors, fonts, spacing match Balatro style
   - Ensure smooth transitions

---

## Design Mockup Summary

### Color Scheme:
- **Background**: Dark grey-teal (#2D3642)
- **Success**: Green (#44FF44)
- **Warning**: Red (#FF4444)
- **Info**: Gold (#FFAA33)
- **Text**: White, light grey

### Typography:
- **Headers**: Balatro font, 20-24px
- **Body**: System font, 14-16px
- **Labels**: 12px, grey
- **Emphasis**: Bold, gold color

### Spacing:
- **Padding**: 20-30px around content
- **Margins**: 15-20px between sections
- **Button gaps**: 15px horizontal spacing

---

## Implementation Priority

### Phase 1: Core Functionality (2-3 hours)
1. Add test state properties to ViewModel
2. Implement TestFilterCommand with real search
3. Wire up basic UI with states
4. Test that it works end-to-end

### Phase 2: UI Polish (1-2 hours)
1. Apply Balatro styling
2. Add all 4 state layouts
3. Implement proper colors and fonts
4. Add verification badge

### Phase 3: Refinements (1 hour)
1. Add copy seed button
2. Improve status messages
3. Add stats display
4. Edge case handling

---

## Notes

- This is a **user-facing feature** - polish matters!
- Follow Balatro's clean, game-like aesthetic
- Real-time feedback is critical for good UX
- Filter verification builds user confidence
- Testing should be FAST for simple filters (< 2 seconds)

---

**END OF PRD**
