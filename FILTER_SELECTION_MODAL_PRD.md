# Filter Selection Modal - Product Requirements Document

## Overview
Create a **FilterSelectionModal** that acts as a gateway before opening Search, Designer, or Analyzer modals. This replaces the LOAD tab and provides a Balatro Challenges-style UI for selecting filters.

## User Flow

### Current (Before)
1. Click SEARCH → Opens Search modal with LOAD tab
2. Click VISUAL BUILDER → Opens Filter Designer modal with LOAD tab
3. Click ANALYZER → Opens Analyzer modal directly (no filter selection)

### New (After)
1. Click SEARCH → Opens FilterSelectionModal (enableSearch=true) → Select filter → Opens Search modal with selected filter
2. Click VISUAL BUILDER → Opens FilterSelectionModal (enableEdit/Copy/Delete=true) → Select filter or CREATE NEW → Opens Designer modal
3. Click ANALYZER → Opens FilterSelectionModal (enableAnalyze=true) → Select filter → Opens Analyzer modal with selected filter

## Design Requirements

### Layout (Balatro Challenges Style)
```
┌─────────────────────────────────────────────────────────┐
│  [Filter List - 200px]  │  [Details Panel - flex]       │
│  ┌──────────────────┐   │  ┌─────────────────────────┐  │
│  │ ▶ CREATE NEW     │   │  │ PerkeoBlackHoleFinder   │  │
│  │   Filter 1       │   │  │ by Logg2317             │  │
│  │   Filter 2       │   │  │ Created: Jan 2025       │  │
│  │   ...            │   │  │                         │  │
│  │                  │   │  │ Description: Requires   │  │
│  │                  │   │  │ Perkeo and black holes  │  │
│  │  [◀ Page 1/2 ▶]  │   │  │ to score...             │  │
│  └──────────────────┘   │  │                         │  │
│                          │  │ Must Have: 2 items      │  │
│                          │  │ [Perkeo] [Black Hole]   │  │
│                          │  │                         │  │
│                          │  │ Should Have: 1 item     │  │
│                          │  │ [Observatory]           │  │
│                          │  │                         │  │
│                          │  │ Must Not: 0 items       │  │
│                          │  └─────────────────────────┘  │
│                          │  [SEARCH] [EDIT] [COPY]      │
│                          │  [DELETE] [ANALYZE]          │
└──────────────────────────┴──────────────────────────────┘
                    [BACK]
```

### Key Features

#### 1. Left Panel - Filter List
- **Reuse existing `PaginatedFilterBrowser.axaml` component**
- Shows paginated list of filters (120 per page)
- Red bouncing triangle indicator (▶) for selected item
- "CREATE NEW" special item at top of page 1 (when enableEdit or enableCopy is true)
- Pagination buttons at bottom (◀ Page 1/2 ▶)
- No numbered list, no green checkmarks

#### 2. Right Panel - Filter Details
**When nothing selected:**
- Show placeholder text: "Please select a filter or CREATE NEW" (if edit enabled) or "Please select a filter"

**When filter selected:**
- **Header Section:**
  - Filter name (large, gold text, top right corner)
  - "by {Author}" (below name, smaller)
  - "Created: {Month Year}" format

- **Tab Navigation (CRITICAL - matches Balatro Challenges):**
  - Three horizontal tab buttons at TOP:
    1. **RULES** - Shows Must Have / Should Have / Must Not criteria
    2. **JOKERS** - Shows joker requirements in 5-card horizontal display
    3. **RESTRICTIONS** - Shows deck/voucher/consumable/other restrictions

  - **Triangle Indicator:** Red ▼ triangle points DOWN from active tab (NOT below content!)
  - Tab buttons: Red when active, dark gray when inactive
  - Position: Below filter name, above content area

- **Tab Content Areas:**

  **RULES Tab:**
  - Must Have: {count} items (show item sprites in horizontal row)
  - Should Have: {count} items (show item sprites in horizontal row)
  - Must Not: {count} items (show item sprites in horizontal row)

  **JOKERS Tab:**
  - Display 5 joker slots horizontally (like Balatro's joker display)
  - Show required jokers with sprites
  - Empty slots show placeholder

  **RESTRICTIONS Tab:**
  - Custom Rules section (blue box with white text)
  - Game Modifiers section showing:
    - Start with $X
    - X hands per round
    - X discards per round
    - X hand size
    - X Joker Slots
    - X Consumable Slots
  - Deck restrictions
  - Banned Cards/Tags/Other

- **Action Buttons (bottom right, BELOW tabs):**
  - **PLAY** button (large blue, primary action - for Search)
  - EDIT (blue button, visible when enableEdit=true)
  - COPY (orange button, visible when enableCopy=true)
  - DELETE (red button, visible when enableDelete=true)

#### 3. Bottom - Back Button
- Orange Balatro-style button
- Full width
- Closes modal with Cancelled result

### Button Visibility Matrix

| Main Menu Button | enableSearch | enableEdit | enableCopy | enableDelete | enableAnalyze |
|------------------|--------------|------------|------------|--------------|---------------|
| SEARCH           | ✓            | ✗          | ✗          | ✗            | ✗             |
| VISUAL BUILDER   | ✗            | ✓          | ✓          | ✓            | ✗             |
| ANALYZER         | ✗            | ✗          | ✗          | ✗            | ✓             |

## Technical Implementation

### Files Created (2 files)
1. **`src/Models/FilterSelectionResult.cs`** ✅ DONE
   - Result class with Cancelled, Action, FilterId properties
   - FilterAction enum (Cancelled, CreateNew, Search, Edit, Copy, Delete, Analyze)

2. **`src/ViewModels/FilterSelectionModalViewModel.cs`** ✅ DONE
   - Boolean properties for button visibility
   - Child ViewModel: `PaginatedFilterBrowserViewModel FilterList`
   - ObservableProperty: `SelectedFilter`
   - Computed properties: ShowDetailsPanel, ShowPlaceholder, FilterName, FilterAuthor, etc.
   - Commands: SearchCommand, EditCommand, CopyCommand, DeleteCommand, AnalyzeCommand, BackCommand
   - Event: ModalCloseRequested
   - Property: FilterSelectionResult Result

### Files to Create (2 files)
3. **`src/Views/Modals/FilterSelectionModal.axaml`**
   - UserControl with 2-column Grid layout
   - Left column (200px): Embed `<components:PaginatedFilterBrowser>`
   - Right column (flex): Details panel with metadata, preview, action buttons
   - Bottom: BACK button (full width orange)

4. **`src/Views/Modals/FilterSelectionModal.axaml.cs`**
   - Code-behind
   - Initialize ViewModel
   - Subscribe to ViewModel.ModalCloseRequested event
   - Call Close() when event fires

### Files to Modify (4 files)
5. **`src/ViewModels/PaginatedFilterBrowserViewModel.cs`**
   - Add `FilterId` property to `FilterBrowserItem` class (line ~308)
     ```csharp
     public string FilterId => System.IO.Path.GetFileNameWithoutExtension(FilePath);
     ```

6. **`src/Helpers/ModalHelper.cs`**
   - Add `ShowFilterSelectionModal()` method:
     ```csharp
     public static async Task<FilterSelectionResult> ShowFilterSelectionModal(
         this BalatroMainMenu menu,
         bool enableSearch = false,
         bool enableEdit = false,
         bool enableCopy = false,
         bool enableDelete = false,
         bool enableAnalyze = false)
     {
         var modal = new FilterSelectionModal();
         var vm = new FilterSelectionModalViewModel(
             enableSearch, enableEdit, enableCopy, enableDelete, enableAnalyze);
         modal.DataContext = vm;

         var result = await modal.ShowDialog(menu.GetWindow());
         return vm.Result;
     }
     ```

7. **`src/ViewModels/BalatroMainMenuViewModel.cs`**
   - Update SearchCommand:
     ```csharp
     var result = await _mainMenu.ShowFilterSelectionModal(enableSearch: true);
     if (!result.Cancelled && result.Action == FilterAction.Search)
         _mainMenu.ShowSearchModal(result.FilterId);
     ```
   - Update FiltersCommand (VISUAL BUILDER button):
     ```csharp
     var result = await _mainMenu.ShowFilterSelectionModal(
         enableEdit: true, enableCopy: true, enableDelete: true);
     if (result.Cancelled) return;

     switch (result.Action)
     {
         case FilterAction.CreateNew:
             _mainMenu.ShowFiltersModal(); // blank filter
             break;
         case FilterAction.Edit:
             _mainMenu.ShowFiltersModal(result.FilterId);
             break;
         case FilterAction.Copy:
             var clonedId = CloneFilter(result.FilterId);
             _mainMenu.ShowFiltersModal(clonedId);
             break;
         case FilterAction.Delete:
             DeleteFilter(result.FilterId);
             break;
     }
     ```
   - Update AnalyzerCommand:
     ```csharp
     var result = await _mainMenu.ShowFilterSelectionModal(enableAnalyze: true);
     if (!result.Cancelled && result.Action == FilterAction.Analyze)
         OpenAnalyzer(result.FilterId);
     ```

8. **`src/Views/Modals/FiltersModal.axaml`**
   - Remove entire LOAD tab from the tab control
   - Remove FiltersTab reference
   - Keep only: VISUAL BUILDER, JSON EDITOR, SAVE tabs
   - Update tab indices accordingly

### Additional Implementation Notes

#### CloneFilter Logic
Add to `FiltersModalViewModel.cs` or create a new `FilterService.cs`:
```csharp
public static string CloneFilter(string filterId)
{
    var filterPath = Path.Combine(GetFiltersDirectory(), $"{filterId}.json");
    var json = File.ReadAllText(filterPath);
    var config = JsonSerializer.Deserialize<MotelyJsonConfig>(json);

    config.Name = $"{config.Name} (Copy)";
    config.DateCreated = DateTime.UtcNow;
    config.Author = UserProfileService.GetAuthorName();

    var newId = $"{filterId}_copy";
    var newPath = Path.Combine(GetFiltersDirectory(), $"{newId}.json");
    File.WriteAllText(newPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

    return newId;
}
```

#### SpritesService Integration for Item Previews
In FilterSelectionModal.axaml, for each Must/Should/MustNot section:
```xaml
<ItemsControl ItemsSource="{Binding MustHaveItems}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Image Source="{Binding Sprite}" Width="50" Height="67" Margin="4,0"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

You'll need to parse the filter JSON and load sprites for each item using SpritesService.

## Styling Notes

- All buttons use existing Balatro button classes (btn-red, btn-blue, btn-orange)
- No emojis in buttons
- No bold font (causes readability issues with Balatro font)
- Pagination should match LOAD tab style: square buttons (44x44), CornerRadius=8, red background
- Triangle indicator uses existing `balatro-triangle balatro-bounce-horizontal` classes

## Testing Checklist

- [ ] Click SEARCH button → FilterSelectionModal opens with only SEARCH button visible
- [ ] Click VISUAL BUILDER button → FilterSelectionModal opens with EDIT, COPY, DELETE buttons visible
- [ ] Click ANALYZER button → FilterSelectionModal opens with only ANALYZE button visible
- [ ] Select "CREATE NEW" → Click EDIT → Designer opens with blank filter
- [ ] Select existing filter → Click EDIT → Designer opens with that filter loaded
- [ ] Select existing filter → Click COPY → Designer opens with cloned filter
- [ ] Select existing filter → Click DELETE → Confirmation dialog → Filter deleted
- [ ] Select existing filter → Click SEARCH → Search modal opens with that filter
- [ ] Select existing filter → Click ANALYZE → Analyzer opens with that filter
- [ ] Click BACK → Modal closes with Cancelled result
- [ ] Pagination works (◀ ▶ buttons, page indicator updates)
- [ ] Triangle indicator bounces next to selected filter
- [ ] Item sprites display correctly in preview section
- [ ] LOAD tab no longer exists in FiltersModal

## Current Status

### ✅ Completed
- FilterSelectionResult.cs created
- FilterSelectionModalViewModel.cs created with full logic
- Todo list tracking in place

### ⏳ In Progress
- FilterSelectionModal.axaml creation

### 🔴 TODO
- FilterSelectionModal.axaml.cs code-behind
- Add FilterId property to FilterBrowserItem
- Add ShowFilterSelectionModal to ModalHelper
- Update main menu button commands
- Remove LOAD tab from FiltersModal
- Add CloneFilter method
- Test all user flows
