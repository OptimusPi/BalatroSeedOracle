# PRD: "Finish" Tab Refactor - Filter Validation & Summary

**Status:** 🔴 **MVP BLOCKER** - Critical UX Improvement
**Priority:** P0 - Immediate Implementation Required
**Estimated Time:** 4-6 hours
**Generated:** 2025-11-15

---

## Executive Summary

The current "Save" tab in the Filter Builder Modal is cramped, confusing, and wastes space. It needs a complete refactor to:

1. **Rename** "Save" → "Finish" (reflects auto-save behavior, signifies final step)
2. **Compact filter metadata** into a single top row (name, ID, description)
3. **Display all filter criteria** in an expandable hierarchical tree (left 80%)
4. **Add seed testing** with visual feedback (right 20%)
5. **One-page modal** - no whole-modal scrolling, only internal scroll viewers
6. **Test filter functionality** with smart retry logic to find at least one seed

This transforms the tab from a "save button" to a "validation & finalization" step that gives users confidence their filter actually works.

---

## Problem Statement

### Current Issues:
1. **Misleading name**: "Save" tab implies manual saving, but filter auto-saves throughout
2. **Wasted space**: Title and description take up huge vertical space
3. **No visibility**: User can't see what criteria they actually set without switching tabs
4. **No validation**: No way to test if filter actually finds seeds before publishing/sharing
5. **Cramped layout**: Everything feels squeezed and hard to read

### User Pain Points:
- "Did I set up the filter correctly?"
- "Will this filter even find any seeds?"
- "What criteria did I add again?"
- "Why is there so much empty space?"

---

## Solution: "Finish" Tab - Validation & Summary

### High-Level Design

```
┌─────────────────────────────────────────────────────────────────────────┐
│  FINISH TAB                                                             │
├─────────────────────────────────────────────────────────────────────────┤
│  Top Row (Compact Filter Metadata)                                     │
│  ┌─────────────────────┬───────────────┬───────────────────────────┐   │
│  │ Filter Name: [____] │ ID: [locked]  │ Description: [__________] │   │
│  └─────────────────────┴───────────────┴───────────────────────────┘   │
├──────────────────────────────────────────────────┬──────────────────────┤
│  LEFT (80%): Filter Criteria Tree               │  RIGHT (20%): Test   │
│  ┌──────────────────────────────────────────┐   │  ┌───────────────┐   │
│  │ [ScrollViewer]                           │   │  │ Test Button   │   │
│  │                                          │   │  ├───────────────┤   │
│  │ ▼ MUST (3 items)                        │   │  │ Seed Results  │   │
│  │   ▼ Joker: Perkeo (Foil, Eternal)       │   │  │               │   │
│  │     • Edition: Foil                      │   │  │ [ABCDEF]      │   │
│  │     • Sticker: Eternal                   │   │  │ Copy | Open   │   │
│  │   ▼ Voucher: Telescope                   │   │  │               │   │
│  │     • No modifications                   │   │  │ Status:       │   │
│  │   ▶ AND (2 items)                        │   │  │ ✅ Verified   │   │
│  │                                          │   │  └───────────────┘   │
│  │ ▼ SHOULD (1 item)                       │   │                      │
│  │   ▶ OR (3 items)                         │   │                      │
│  │                                          │   │                      │
│  │ ▼ BANNED (1 item)                       │   │                      │
│  │   ▼ Joker: Blueprint                     │   │                      │
│  │     • No modifications                   │   │                      │
│  └──────────────────────────────────────────┘   │                      │
└──────────────────────────────────────────────────┴──────────────────────┘
```

---

## Detailed Requirements

### 1. Tab Renaming

**Change:**
- "Save" → "Finish"

**Files:**
- `src/ViewModels/FiltersModalViewModel.cs` - Tab title
- `src/Views/Modals/FilterSelectionModal.axaml` - Tab header

**Justification:**
- Filter auto-saves throughout user journey
- "Finish" signifies final validation step
- Less misleading, more accurate

---

### 2. Top Row - Compact Filter Metadata

**Layout:**

**Option A: Single Row (Preferred if fits)**
```
┌──────────────────────┬────────────────┬──────────────────────────────┐
│ Filter Name: [____]  │ ID: [readonly] │ Description: [____________] │
└──────────────────────┴────────────────┴──────────────────────────────┘
```

**Option B: Two Rows (If description needs more space)**
```
┌──────────────────────┬────────────────────────────────────────────────┐
│ Filter Name: [____]  │ Description: [____________________________]   │
├──────────────────────┤                [____________________________]   │
│ ID: [readonly]       │                                                │
└──────────────────────┴────────────────────────────────────────────────┘
```

**Fields:**

1. **Filter Name** (Editable TextBox)
   - Binding: `{Binding FilterName, Mode=TwoWay}`
   - On change: Trigger filter rename logic
   - Watermark: "My Awesome Filter"

2. **Filter ID** (Read-only TextBox)
   - Binding: `{Binding NormalizedFilterName, Mode=OneWay}`
   - Background: Slightly dimmed to indicate read-only
   - ToolTip: "This is the filename and search database ID. Auto-generated from filter name."

3. **Description** (Editable TextBox)
   - Binding: `{Binding FilterDescription, Mode=TwoWay}`
   - AcceptsReturn: true (multiline)
   - TextWrapping: Wrap
   - MaxHeight: 60px (2 lines)
   - Watermark: "Write something helpful, or explain the filter mechanics that make this filter work."
   - ScrollViewer: VerticalScrollBarVisibility="Auto"

**Filter Rename Logic:**
- When user changes `FilterName`:
  1. Normalize name using existing `NormalizeFilterName()` function
  2. Check if new filename exists
  3. If exists: Show warning "A filter with this name already exists. Saving will overwrite it."
  4. If new: Save as new filter file
  5. Update `CurrentFilterPath` property
  6. Update normalized ID display

---

### 3. Two-Column Layout - Criteria Tree + Seed Testing

**Grid Layout:**
```xml
<Grid ColumnDefinitions="0.8*,0.2*" RowDefinitions="Auto,*">
    <!-- Row 0: Top metadata row (spans both columns) -->
    <!-- Row 1, Column 0: Criteria tree (left 80%) -->
    <!-- Row 1, Column 1: Seed testing (right 20%) -->
</Grid>
```

---

### 4. LEFT COLUMN (80%): Filter Criteria Tree

**Purpose:** Show user exactly what criteria they configured, in expandable format

**Structure:**

```
ScrollViewer (VerticalScrollBarVisibility="Auto")
└── StackPanel
    ├── Expander "MUST (3 items)"
    │   ├── Expander "Joker: Perkeo"
    │   │   └── StackPanel (config details)
    │   │       • Edition: Foil
    │   │       • Sticker: Eternal
    │   ├── Expander "Voucher: Telescope"
    │   │   └── StackPanel (config details)
    │   │       • No modifications
    │   └── Expander "AND (2 items)" [Nested operator]
    │       ├── Expander "Joker: Baron"
    │       └── Expander "Consumable: Lovers"
    │
    ├── Expander "SHOULD (1 item)"
    │   └── Expander "OR (3 items)" [Nested operator]
    │       ├── Expander "Joker: Burglar"
    │       ├── Expander "Joker: Triboulet"
    │       └── Expander "Score: Ante 1 > 10000"
    │
    └── Expander "BANNED (0 items)"
        └── TextBlock "No banned items"
```

**Implementation Details:**

**Top-Level Expanders (MUST, SHOULD, BANNED):**
- Header: `{ClauseType} ({Count} items)` - e.g., "MUST (3 items)"
- Background: Light teal for MUST, light green for SHOULD, light red for BANNED
- FontSize: 16
- IsExpanded: true by default for MUST and SHOULD, false for BANNED
- ItemsSource: Bindings to `SelectedMust`, `SelectedShould`, `SelectedBanned`

**Child Item Expanders:**

**Case 1: Regular Item (Joker/Voucher/Consumable/etc.)**
- Header: `{ItemType}: {ItemName}` - e.g., "Joker: Perkeo"
- Icon: Small sprite image (32x32) next to header
- Content: StackPanel showing config details:
  ```
  • Edition: {Edition}  (only if set)
  • Sticker: {Sticker}  (only if set)
  • Seal: {Seal}        (only if set)
  • Soul: ✓             (only if IsSoulJoker)
  ```
- If no modifications: Show "• No modifications" in light gray

**Case 2: Operator Item (OR/AND/BannedItems)**
- Header: `{OperatorType} ({Children.Count} items)` - e.g., "OR (3 items)"
- Background: Color-coded (Blue for AND, Green for OR, Red for BANNED)
- Content: Nested expanders for each child item
- Recursive structure: Children can also be operators

**Case 3: Score Criteria**
- Header: `Score: {Description}` - e.g., "Score: Ante 1 > 10000"
- Content: Show score config details:
  ```
  • Ante: {Ante}
  • Score: > {MinScore}
  • Chip Type: {ChipType}
  ```

**Empty State:**
- If clause is empty (0 items), show:
  ```
  TextBlock "No {ClauseType} items"
  FontStyle: Italic
  Foreground: LightGray
  ```

**Visual Design:**
- Use indentation to show hierarchy
- Use color-coding for operator types (AND=Blue, OR=Green, BANNED=Red)
- Small sprite icons next to item names for visual recognition
- Collapse/expand animations (150ms ease-out)

---

### 5. RIGHT COLUMN (20%): Seed Testing & Results

**Structure:**

```
StackPanel (VerticalAlignment="Top", Spacing="16")
├── Button "Test Filter" (Full width)
│   • Command: TestFilterCommand
│   • Background: Blue
│   • Glow effect when no results yet
│
├── Border "Results Panel"
│   ├── [Before Testing] TextBlock (helpful message)
│   │   "It is recommended to test your filter before finishing.
│   │    Press the test button and seeds might appear here.
│   │    If a seed is found, this filter will be considered
│   │    validated and working!"
│   │
│   └── [After Testing] StackPanel
│       ├── TextBlock "Seed Found!" (if success)
│       │   • FontSize: 18
│       │   • Foreground: Green
│       │
│       ├── Grid (SeedDisplay)
│       │   ├── TextBox (seed value, read-only)
│       │   │   • Text: "ABCDEF"
│       │   │   • IsReadOnly: true
│       │   │   • SelectAllOnFocus: true
│       │   └── Button "Copy" (overlay, right side)
│       │       • Command: CopySeedCommand
│       │
│       ├── Button "Open in Blueprint"
│       │   • Command: OpenBlueprintCommand
│       │   • Generates mikal walker blueprint URL
│       │   • Opens in browser
│       │
│       └── Border "Status Badge"
│           ├── [Success] "✅ Verified & Working"
│           │   • Background: Green
│           └── [Failure] "⚠️ No seeds found"
│               • Background: Orange
│               • Additional message: "Try making filter less restrictive"
└── ProgressBar (IsIndeterminate, only visible during testing)
```

**Test Filter Button Logic:**

**Phase 1: Quick Test (1 character, 1 batch)**
```
1. Create/Open DuckDB database (if not already open)
2. Run Motely Search:
   - SearchType: Sequential
   - BatchSize: 1 character
   - StartBatch: 0
   - EndBatch: 0 (only 1 batch)
3. If seeds found:
   - Take best seed (highest score)
   - Set VerifiedSeed property
   - Show "✅ Verified & Working"
   - STOP
4. If no seeds:
   - Continue to Phase 2
```

**Phase 2: Medium Test (1 character, 1 million batches)**
```
1. Run Motely Search:
   - SearchType: Sequential
   - BatchSize: 1 character
   - StartBatch: 0
   - EndBatch: 1,000,000 (1 million batches)
   - Note: 35 seeds per batch = ~35M seeds total
2. If seeds found:
   - Take best seed
   - Set VerifiedSeed property
   - Show "✅ Verified & Working"
   - STOP
3. If no seeds:
   - Continue to Phase 3
```

**Phase 3: Deep Test (Sliding window approach)**
```
1. Start with batchSize = 1, endBatch = 10,000,000
2. Run search
3. If seeds found:
   - Take best seed
   - Set VerifiedSeed property
   - Show "✅ Verified & Working"
   - STOP
4. If no seeds:
   - Show "⚠️ No seeds found in 350M seeds"
   - Suggest: "Try making filter less restrictive"
   - Set IsVerified = false
```

**Performance Safeguards:**

**Problem:** If filter is too broad (e.g., "Any Joker"), search might return millions of seeds

**Solution 1: Result Cap**
- Cap results at 1000 seeds maximum
- If more than 1000 seeds found, stop search
- Take best seed from first 1000
- Show warning: "Filter is very broad. Consider adding more criteria."

**Solution 2: Timeout**
- If search takes > 30 seconds, abort
- Show warning: "Search timed out. Filter may be too complex or database too large."

**Solution 3: Progress Feedback**
- Show progress bar during search
- Show current batch being searched
- Allow user to cancel search

---

### 6. Data Binding & ViewModel

**New Properties in FinishTabViewModel (or FiltersModalViewModel):**

```csharp
public class FinishTabViewModel : ObservableObject
{
    // Filter metadata
    [ObservableProperty]
    private string _filterName = "New Filter";

    [ObservableProperty]
    private string _normalizedFilterName = "new_filter";

    [ObservableProperty]
    private string _filterDescription = "";

    // Seed testing
    [ObservableProperty]
    private string? _verifiedSeed;

    [ObservableProperty]
    private bool _isTestingFilter;

    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private string _testStatus = "Not tested";

    [ObservableProperty]
    private int _currentBatch;

    [ObservableProperty]
    private int _totalBatches;

    // Filter criteria (from Visual Builder)
    [ObservableProperty]
    private ObservableCollection<FilterItem> _selectedMust;

    [ObservableProperty]
    private ObservableCollection<FilterItem> _selectedShould;

    [ObservableProperty]
    private ObservableCollection<FilterItem> _selectedBanned;

    // Commands
    [RelayCommand]
    private async Task TestFilter()
    {
        IsTestingFilter = true;
        TestStatus = "Testing...";

        try
        {
            // Phase 1: Quick test
            var quickResult = await SearchService.SearchSequential(
                batchSize: 1,
                startBatch: 0,
                endBatch: 0,
                filter: CurrentFilter
            );

            if (quickResult.Seeds.Count > 0)
            {
                VerifiedSeed = quickResult.Seeds.First().SeedValue;
                IsVerified = true;
                TestStatus = "✅ Verified & Working";
                return;
            }

            // Phase 2: Medium test
            TestStatus = "Testing deeper...";
            var mediumResult = await SearchService.SearchSequential(
                batchSize: 1,
                startBatch: 0,
                endBatch: 1_000_000,
                filter: CurrentFilter,
                progress: new Progress<SearchProgress>(p => {
                    CurrentBatch = p.CurrentBatch;
                    TotalBatches = p.TotalBatches;
                })
            );

            if (mediumResult.Seeds.Count > 0)
            {
                VerifiedSeed = mediumResult.Seeds.First().SeedValue;
                IsVerified = true;
                TestStatus = "✅ Verified & Working";
                return;
            }

            // Phase 3: No seeds found
            TestStatus = "⚠️ No seeds found";
            IsVerified = false;
        }
        catch (Exception ex)
        {
            TestStatus = $"❌ Error: {ex.Message}";
            IsVerified = false;
        }
        finally
        {
            IsTestingFilter = false;
        }
    }

    [RelayCommand]
    private void CopySeed()
    {
        if (VerifiedSeed != null)
        {
            Clipboard.SetText(VerifiedSeed);
            // Show toast notification?
        }
    }

    [RelayCommand]
    private void OpenBlueprint()
    {
        if (VerifiedSeed != null)
        {
            var blueprintUrl = $"https://mikal.github.io/blueprint/?seed={VerifiedSeed}";
            Process.Start(new ProcessStartInfo
            {
                FileName = blueprintUrl,
                UseShellExecute = true
            });
        }
    }

    // Filter rename logic
    partial void OnFilterNameChanged(string value)
    {
        NormalizedFilterName = NormalizeFilterName(value);

        // Check if file exists
        var newPath = Path.Combine(FiltersDirectory, $"{NormalizedFilterName}.json");
        if (File.Exists(newPath) && newPath != CurrentFilterPath)
        {
            // Show warning
            DebugLogger.Log("FinishTab", $"⚠️ Filter '{NormalizedFilterName}' already exists");
        }
    }

    private string NormalizeFilterName(string name)
    {
        // Use existing normalization function
        // Convert to lowercase, replace spaces with underscores, remove special chars
        return name.ToLower()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .Aggregate("", (acc, c) => acc + c);
    }
}
```

---

## File Structure

### New Files to Create:

1. **`src/Components/FilterTabs/FinishTab.axaml`**
   - Main UI for Finish tab
   - Top metadata row
   - Two-column layout
   - Criteria tree on left
   - Seed testing on right

2. **`src/Components/FilterTabs/FinishTab.axaml.cs`**
   - Code-behind (minimal, just InitializeComponent)

3. **`src/ViewModels/FilterTabs/FinishTabViewModel.cs`**
   - ViewModel for Finish tab
   - Properties for filter metadata
   - Seed testing logic
   - Filter rename logic

4. **`src/Components/FilterCriteriaExpander.axaml`**
   - Reusable expander component for filter criteria
   - Handles both regular items and operator items
   - Recursive rendering for nested operators

5. **`src/Components/FilterCriteriaExpander.axaml.cs`**
   - Code-behind

6. **`src/ViewModels/FilterCriteriaItemViewModel.cs`**
   - ViewModel for individual criteria item
   - Wraps FilterItem or FilterOperatorItem
   - Provides display properties (icon, description, config details)

### Files to Modify:

1. **`src/ViewModels/FiltersModalViewModel.cs`**
   - Change tab title "Save" → "Finish"
   - Add FinishTabViewModel property
   - Wire up data flow from Visual Builder to Finish tab

2. **`src/Views/Modals/FilterSelectionModal.axaml`**
   - Update tab header text
   - Replace SaveTab with FinishTab

3. **`src/Services/SearchService.cs`** (or create if doesn't exist)
   - Add `SearchSequential` method wrapper
   - Progress reporting
   - Result capping (max 1000 seeds)
   - Timeout handling (30 seconds)

---

## Implementation Plan

### Phase 1: Setup & Rename (30 minutes)
1. Rename "Save" → "Finish" in FiltersModalViewModel
2. Update tab header in FilterSelectionModal.axaml
3. Create FinishTabViewModel stub
4. Create FinishTab.axaml stub
5. Wire up basic data binding

### Phase 2: Top Row - Filter Metadata (1 hour)
1. Design compact metadata row layout
2. Add Filter Name editable TextBox
3. Add Filter ID read-only TextBox with normalization binding
4. Add Description multiline TextBox
5. Implement filter rename logic in ViewModel
6. Test auto-save on name change

### Phase 3: Left Column - Criteria Tree (2 hours)
1. Create FilterCriteriaExpander component
2. Design expander header styles (color-coded by type)
3. Implement top-level expanders (MUST, SHOULD, BANNED)
4. Implement child item expanders
   - Regular items: Show config details
   - Operator items: Recursive nesting
5. Add empty state handling
6. Add sprite icons to headers
7. Test with complex nested filters

### Phase 4: Right Column - Seed Testing (2 hours)
1. Design seed testing panel layout
2. Add "Test Filter" button with command
3. Implement TestFilterCommand:
   - Phase 1: Quick test (1 batch)
   - Phase 2: Medium test (1M batches)
   - Phase 3: Deep test (10M batches)
4. Add progress reporting
5. Add result display (seed value, copy button, blueprint link)
6. Add status badge (verified vs not verified)
7. Implement performance safeguards (result cap, timeout)
8. Test with various filter complexities

### Phase 5: Integration & Testing (1 hour)
1. Wire up FinishTab to FiltersModalViewModel
2. Pass SelectedMust/Should/Banned from Visual Builder
3. Test filter rename → save new file
4. Test seed verification flow end-to-end
5. Test with empty filters
6. Test with very broad filters (performance check)
7. Polish animations and transitions

---

## Acceptance Criteria

### Tab Renaming
- [x] Tab is named "Finish" not "Save"
- [x] Tab title updates in modal header

### Filter Metadata Row
- [x] Filter Name is editable
- [x] Filter ID auto-updates when name changes
- [x] Filter ID uses normalized format (lowercase, underscores)
- [x] Description is multiline, wraps text
- [x] Description shows placeholder when empty
- [x] Changing filter name saves to new file

### Criteria Tree (Left Column)
- [x] Shows MUST expander with count
- [x] Shows SHOULD expander with count
- [x] Shows BANNED expander with count
- [x] Empty clauses show "No {type} items"
- [x] Regular items show config details (edition, sticker, seal)
- [x] Operator items (OR/AND) show nested children
- [x] Nested operators expand recursively
- [x] Color-coded headers (Blue=AND, Green=OR, Red=BANNED)
- [x] Small sprite icons next to item names
- [x] Smooth collapse/expand animations

### Seed Testing (Right Column)
- [x] "Test Filter" button present
- [x] Shows helpful message before testing
- [x] Phase 1 quick test (1 batch) runs first
- [x] Phase 2 medium test (1M batches) if Phase 1 fails
- [x] Phase 3 deep test (10M batches) if Phase 2 fails
- [x] Displays found seed value
- [x] Copy button copies seed to clipboard
- [x] "Open in Blueprint" opens mikal walker URL
- [x] Shows verification status (✅ or ⚠️)
- [x] Progress bar shows during testing
- [x] Timeout after 30 seconds
- [x] Result cap at 1000 seeds max

### Layout & UX
- [x] One-page modal (no whole-modal scrolling)
- [x] Left column has internal scrollbar
- [x] Right column is fixed, no scroll
- [x] Top metadata row is compact (1-2 lines max)
- [x] Everything fits in standard modal size (1200x800)

---

## Visual Design Specifications

### Colors

**Top-Level Expanders:**
- MUST: `Background="{StaticResource Blue}"` with 20% opacity
- SHOULD: `Background="{StaticResource Green}"` with 20% opacity
- BANNED: `Background="{StaticResource Red}"` with 20% opacity

**Operator Expanders:**
- AND: `Background="{StaticResource Blue}"` with 30% opacity
- OR: `Background="{StaticResource Green}"` with 30% opacity
- BannedItems: `Background="{StaticResource Red}"` with 30% opacity

**Status Badges:**
- Verified: `Background="{StaticResource Green}"`, Foreground="White"
- Not Verified: `Background="{StaticResource Orange}"`, Foreground="White"
- Error: `Background="{StaticResource Red}"`, Foreground="White"

### Typography

**Top Metadata Row:**
- Labels: FontSize="12", Foreground="{StaticResource LightGrey}"
- Filter Name: FontSize="14", FontFamily="{StaticResource BalatroFont}"
- Filter ID: FontSize="12", FontFamily="Consolas" (monospace)
- Description: FontSize="12", LineHeight="18"

**Criteria Tree:**
- Top-level headers: FontSize="16", FontFamily="{StaticResource BalatroFont}"
- Item headers: FontSize="14", FontFamily="{StaticResource BalatroFont}"
- Config details: FontSize="12", Foreground="{StaticResource LightGrey}"

**Seed Testing:**
- Status: FontSize="18", FontFamily="{StaticResource BalatroFont}"
- Seed value: FontSize="16", FontFamily="Consolas"
- Helpful text: FontSize="12", TextWrapping="Wrap"

### Spacing

**Top Row:**
- Padding: "16,12" (horizontal, vertical)
- Column spacing: "12" between fields

**Criteria Tree:**
- Top-level expander margin: "0,0,0,8"
- Child expander margin: "16,4,0,4" (indented)
- Config detail margin: "24,2,0,2" (more indented)

**Seed Testing:**
- StackPanel spacing: "16"
- Button padding: "16,8"
- Result panel padding: "12"

---

## Testing Plan

### Manual Testing

#### Test 1: Tab Rename
1. Open Filter Builder Modal
2. Navigate to last tab
3. **Expected:** Tab is named "Finish"
4. **Expected:** No references to "Save" anywhere

#### Test 2: Filter Metadata
1. Edit Filter Name to "My Test Filter"
2. **Expected:** Filter ID updates to "my_test_filter"
3. Type description "This filter finds Perkeo with Foil"
4. **Expected:** Description wraps, shows scrollbar if > 2 lines
5. Change filter name to existing filter
6. **Expected:** Warning shown (if implemented)

#### Test 3: Criteria Tree - Simple Filter
1. Create filter with:
   - MUST: 1 Joker (Perkeo, Foil edition, Eternal sticker)
   - SHOULD: 1 Voucher (Telescope)
2. Go to Finish tab
3. **Expected:** MUST expander shows "MUST (1 item)"
4. Expand MUST
5. **Expected:** Shows "Joker: Perkeo" with sprite icon
6. Expand Perkeo
7. **Expected:** Shows "• Edition: Foil" and "• Sticker: Eternal"
8. **Expected:** SHOULD expander shows "SHOULD (1 item)"
9. Expand SHOULD
10. **Expected:** Shows "Voucher: Telescope" with "• No modifications"

#### Test 4: Criteria Tree - Nested Operators
1. Create filter with:
   - MUST: OR clause containing 2 jokers
2. Go to Finish tab
3. Expand MUST
4. **Expected:** Shows "OR (2 items)" with green background
5. Expand OR
6. **Expected:** Shows 2 nested joker expanders
7. **Expected:** Nested items are indented

#### Test 5: Seed Testing - Success Path
1. Create simple filter (e.g., MUST: Any Red Deck)
2. Go to Finish tab
3. Click "Test Filter"
4. **Expected:** Progress bar shows
5. **Expected:** Within 5 seconds, seed found
6. **Expected:** Seed value displays (e.g., "ABCDEF")
7. Click "Copy"
8. **Expected:** Seed copied to clipboard
9. Click "Open in Blueprint"
10. **Expected:** Browser opens to blueprint.github.io URL

#### Test 6: Seed Testing - No Seeds Found
1. Create impossible filter (e.g., MUST: Perkeo AND Chicot)
2. Go to Finish tab
3. Click "Test Filter"
4. **Expected:** Phase 1 fails, continues to Phase 2
5. **Expected:** Phase 2 fails, continues to Phase 3
6. **Expected:** After all phases, shows "⚠️ No seeds found"
7. **Expected:** Suggests making filter less restrictive

#### Test 7: Seed Testing - Timeout
1. Create very complex filter with many criteria
2. Click "Test Filter"
3. Wait 30 seconds
4. **Expected:** Search aborts with timeout message

#### Test 8: Layout Responsiveness
1. Test at minimum window size
2. **Expected:** Left column has scrollbar
3. **Expected:** Right column stays visible
4. **Expected:** Top row wraps gracefully if needed
5. Test at maximum window size
6. **Expected:** No overflow, everything fits

---

## Success Metrics

### UX Improvements
- ✅ Users can see all filter criteria at a glance
- ✅ Users can validate filter works before sharing
- ✅ Users understand auto-save behavior (no manual save needed)
- ✅ Users can edit filter name and description easily
- ✅ Users get visual feedback on filter complexity (item counts)

### Technical Quality
- ✅ One-page modal, no whole-modal scrolling
- ✅ Performant with large filters (100+ items)
- ✅ Seed testing completes within reasonable time (<30s)
- ✅ No UI freezing during search
- ✅ Proper error handling for search failures

### Code Quality
- ✅ Reusable FilterCriteriaExpander component
- ✅ Clean separation of concerns (ViewModel, View, Services)
- ✅ Proper async/await for search operations
- ✅ Progress reporting for long searches
- ✅ Cancellation support for search

---

## Risk Assessment

### MEDIUM RISK: Search Performance
**Risk:** Very broad filters might return millions of seeds, bogging down PC

**Mitigation:**
1. Result cap at 1000 seeds
2. Timeout after 30 seconds
3. Progress reporting so user knows it's working
4. Warning message if filter is too broad

### LOW RISK: Recursive Rendering
**Risk:** Deeply nested operators (OR within AND within OR...) might cause stack overflow

**Mitigation:**
1. Limit nesting depth to 5 levels
2. Use iterative rendering instead of recursive where possible
3. Test with complex nested filters

### LOW RISK: Filter Rename Conflicts
**Risk:** User renames filter to existing filter name, overwrites accidentally

**Mitigation:**
1. Show warning if filename exists
2. Require confirmation before overwriting
3. Add "Save As Copy" option

---

## Timeline

### Immediate (4-6 hours)
1. Phase 1: Setup & Rename (30 min)
2. Phase 2: Top Row Metadata (1 hour)
3. Phase 3: Criteria Tree (2 hours)
4. Phase 4: Seed Testing (2 hours)
5. Phase 5: Integration & Testing (1 hour)

**Total Estimated Time:** 6.5 hours

---

## Dependencies

### Existing Services/Functions to Use:
- `NormalizeFilterName()` - For filter ID generation
- `SearchService.SearchSequential()` - For seed searching
- `SelectedMust/Should/Banned` - From VisualBuilderTabViewModel
- Blueprint URL generation - From existing results code

### New Dependencies Needed:
- ✅ None - all features can be built with existing libraries

---

## Notes

- This refactor transforms "Save" from a passive button to an active validation step
- Users gain confidence their filter actually works before sharing
- Visual feedback (criteria tree + verified badge) reduces user anxiety
- Auto-save throughout journey means "Finish" is about validation, not saving
- Seed testing with smart retry logic ensures we find at least one seed if possible
- Performance safeguards prevent PC from being bogged down by overly broad filters

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
