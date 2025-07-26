# BalatroSeedOracle Development Progress

## What We've Built
- Fixed the build system for a cross-platform Avalonia UI app themed like Balatro
- Created a modal system that takes up 85% of the screen with consistent theming
- Built a comprehensive Ouija Config Editor that:
  - Shows inside a StandardModal (not a separate window)
  - Has a split view with desires list on left, JSON editor on right
  - Includes Add/Remove desire functionality with a Balatro-themed dialog
  - Features Jokers, Tarot Cards, Spectral Cards, Vouchers, Tags, Playing Cards, and Bosses
  - Has working Load/Save functionality for JSON configs
- Fixed the Config button in Settings to properly launch the editor
- Resolved namespace issues between Oracle.Views and BalatroSeedOracle.Views
- Updated deprecated file dialogs to use new StorageProvider API

## JSON Editor Journey
- FiltersModal has a JSON editor that was broken (showing no text)
- Discovered AvaloniaEdit has compatibility issues with the Balatro theme
- Implemented fallback system using regular TextBox with ScrollViewer
- TextBox solution now works with:
  - Visible text that can be edited
  - Proper scrolling for long JSON content
  - JSON validation feedback in status bar
- Created FunRunsModal as a test environment for AvaloniaEdit
  - Still shows black box with no visible text
  - Confirms AvaloniaEdit theme incompatibility is global
- Added AvaloniaEdit Simple theme and resources to App.axaml
- Fixed build errors from missing InitializeComponent methods

## Key Files Modified
- /mnt/x/BalatroSeedOracle/src/Views/OuijaConfigEditorView.cs - Complete implementation
- /mnt/x/BalatroSeedOracle/src/Views/BalatroDesireEditDialog.cs - Fixed for proper OuijaDesire properties
- /mnt/x/BalatroSeedOracle/src/Views/BalatroMainMenu.axaml.cs - Updated modal handling and config button
- /mnt/x/BalatroSeedOracle/src/Views/Modals/SettingsModal.axaml(.cs) - Added config button handler
- /mnt/x/BalatroSeedOracle/src/Views/Modals/FiltersModal.axaml.cs - JSON editor with TextBox fallback
- /mnt/x/BalatroSeedOracle/src/Views/Modals/FunRunsModal.* - Test modal for AvaloniaEdit
- /mnt/x/BalatroSeedOracle/src/App.axaml - Added AvaloniaEdit theme and resources
- All modal AXAML files updated to 85% sizing (0.075* margins, 0.85* content)

## Fun Discoveries
- There's a typo file: BalatraMainMenu instead of BalatroMainMenu 😂
- The app has great Balatro theming with card animations and sound effects
- Modals use a clever grid-based percentage sizing system
- AvaloniaEdit really doesn't like dark themes without proper resource setup

## Current State
- Build succeeds with no warnings or errors
- JSON editor works with TextBox fallback (useFallback = true) 
- AvaloniaEdit now shows text properly with correct package (Avalonia.AvaloniaEdit 11.3.0)
- Mouse wheel scrolling fixed using tunnel routing strategy
- Config editor is fully integrated into the modal system
- UI matches the Balatro aesthetic perfectly
- Everything 'fuckin slaps' according to user feedback 🎮

## Helper Classes Implementation (Latest)
Based on CODE-IMPROVEMENTS.md recommendations, the following helper classes were implemented:

### ServiceHelper (/src/Helpers/ServiceHelper.cs)
- Eliminates duplicate reflection-based DI retrieval code
- Provides simple `GetService<T>()` method
- Used throughout codebase (SearchWidget, SearchModal, etc.)

### ControlHelper (REMOVED)
- Initially created to simplify FindControl operations
- Caused stack overflow due to method name conflict with Avalonia's built-in FindControl
- Removed in favor of using Avalonia's built-in FindControl directly
- All references updated to use the standard `this.FindControl<T>("name")` pattern

### DebugLogger (/src/Helpers/DebugLogger.cs)
- Centralized debug logging with enable/disable flag
- Structured logging with component names and timestamps
- Replaced 200+ Console.WriteLine calls
- Fixed namespace conflicts with Motely.DebugLogger using fully qualified names

### Build Error Fixes
- Fixed duplicate using directive for Motely in MotelySearchService
- Fixed DebugLogger ambiguity by using fully qualified names (Oracle.Helpers.DebugLogger)
- Fixed async/await issue in MotelySearchService.RunSearch
- Fixed SearchModal ViewDetails method to use proper window parent resolution
- Added missing ShowSearchModal method to BalatroMainMenu
- Added public properties to SearchWidget for SearchModal integration

### Stack Overflow Fix
- **Issue**: ControlHelper.FindControl was calling itself recursively, causing stack overflow
- **Root Cause**: Extension method had same name as Avalonia's built-in FindControl method
- **Solution**: Removed ControlHelper entirely and updated all code to use built-in FindControl
- **Result**: Application now starts successfully without stack overflow

### Warning Suppression for External Dependencies
- **Issue**: CS8604 warnings from Motely submodule about possible null references
- **Solution**: Created `/external/Directory.Build.props` to suppress warnings in external projects
- **Result**: Clean build with 0 warnings, 0 errors

### Search Thread Termination Fix
- **Issue**: Search threads continued running after closing the app
- **Solution**: Implemented proper cleanup chain:
  1. Made MotelySearchService implement IDisposable
  2. Added Dispose method that cancels search and disposes resources
  3. Added StopSearch method to SearchWidget
  4. Modified BalatroMainMenu.Dispose to stop search widget
  5. Added Window.Closing handler in MainWindow to dispose main menu
- **Result**: All search threads properly terminate when app closes

The implementation of helper classes (ServiceHelper and DebugLogger) successfully reduced code duplication and improved maintainability as recommended in the code audit.

## Debug Logging Implementation
To understand why search results weren't showing in the widget, comprehensive debug logging was added:

### DebugLogger Enhancement
- Enabled debug logging by setting `EnableDebugLogging = true`
- Added `LogImportant()` method for always-visible logs
- Added `LogFormat()` method for formatted messages

### MotelySearchService Logging
- Logs full search configuration (needs, wants, scores, antes)
- Tracks result processing with accept/reject reasons
- Shows progress every 100k seeds
- Logs when results are reported to UI

### SearchWidget Logging
- Tracks OnProgressUpdate calls with thread IDs
- Logs when results are displayed in UI
- Shows progress statistics periodically

### Motely (External) Logging
- OuijaJsonFilterDesc logs full config on construction
- MotelySearch.cs logs when results are enqueued
- Shows thread ID and score for each result

### Key Debug Points
1. **Config Loading**: Shows needs/wants with SearchAntes arrays
2. **Result Flow**: Tracks from Motely → MotelySearchService → SearchWidget
3. **Score Filtering**: Shows why results are accepted/rejected based on MinScore
4. **Thread Safety**: Logs thread IDs to verify proper UI thread marshaling

With this logging, you can now see:
- What configuration is loaded
- Which seeds are found by Motely
- What scores they have
- Whether they meet the MinScore criteria
- If they're properly displayed in the widget

## AvaloniaEdit Scrolling Fix
- Issue: PointerWheelChanged events were detected but not causing actual scrolling
- Root cause: Avalonia's ScrollContentPresenter handles wheel events, not ScrollViewer
- Solution: Use RoutingStrategies.Tunnel to intercept events before default handling
- Implementation completed in both FunRuns modal and FiltersModal
- Fixed sender casting issue - now uses member variable or FindControl to get editor
- Directly manipulates ScrollViewer.Offset instead of using LineUp/LineDown
- Calculates scroll amount as Delta.Y * 60 (approximately 3 lines)
- Clamps scroll position to valid range (0 to Extent.Height - Viewport.Height)
- Added handler to parent Border to catch events over empty space in editor

## Motely Integration via Git Submodule
- Removed Motely.Core NuGet package (was using local package)
- Added Motely as git submodule: `git submodule add -b Oracle https://github.com/OptimusPi/Motely.git external/Motely`
- Updated Oracle.csproj to reference Motely project directly
- Updated MotelySearchService to use actual Motely API:
  - Uses IMotelySearch interface with Results concurrent queue
  - Adapted to MotelySearchResult struct instead of OuijaResult
  - Fixed namespace changes from Motely.Core to just Motely
- Fixed OuijaJsonFilterDesc type mappings for JSON compatibility:
  - Added mappings for shortened type names (Tarot→TarotCard, Spectral→SpectralCard, etc.)
  - Handle special types (Voucher, Tag, Boss) that don't map to MotelyItemTypeCategory
  - Fixed "BlankVoucher" → "Blank" mapping for MotelyVoucher enum
  - Proper handling of SoulJoker as a special case using Soul PRNG key
- Build succeeds with the submodule integration

### Working with the Submodule
To clone the project with submodules:
```bash
git clone --recursive https://github.com/OptimusPi/BalatroSeedOracle.git
```

To update the submodule after cloning:
```bash
git submodule update --init --recursive
```

To pull latest changes from Motely Oracle branch:
```bash
cd external/Motely
git pull origin Oracle
cd ../..
git add external/Motely
git commit -m "Update Motely submodule"
```

## App Hanging Fix (Latest Implementation)
The app was hanging forever on close after running a search. The issue was in the Motely disposal chain:

### Root Cause
- MotelySearch.Dispose() was calling Pause() then trying to signal unpause barrier
- This created a deadlock where threads were waiting indefinitely
- The pause/unpause barrier synchronization was blocking disposal

### Fixes Applied

1. **MotelySearch.cs Disposal Improvements**:
   - Skip pause attempt if already disposed
   - Set status to Disposed immediately for running searches to signal threads
   - Added timeouts to barrier waits (1 second)
   - Added per-thread join timeout (1 second each)
   - Added comprehensive logging throughout disposal

2. **MotelySearch.cs Pause Improvements**:
   - Added 2-second timeout to pause barrier wait
   - Added exception handling for barrier operations
   - Prevents infinite blocking during pause

3. **MotelySearchService Disposal Improvements**:
   - Wrapped Motely disposal in Task.Run with 3-second timeout
   - Continues cleanup even if Motely disposal times out
   - Better error handling and logging

4. **MainWindow Closing Improvements**:
   - Made window closing async to avoid blocking UI thread
   - Increased disposal timeout to 5 seconds total
   - Cancels close initially, does cleanup async, then closes on UI thread
   - Prevents recursion by removing event handler before final close

### Result
The app should now close gracefully even if Motely threads are stuck, with a maximum wait time of 5 seconds before forcing close.

### Test Config
Created `/test-configs/test-simple.ouija.json` for testing:
```json
{
  "needs": [],
  "wants": [
    {
      "type": "Joker",
      "value": "any",
      "score": 100,
      "desireByAnte": 1,
      "searchAntes": [1]
    }
  ]
}
```

This config should find results since it's looking for any joker in ante 1, which has high probability.

## SearchWidget Config Update Fix
The SearchWidget was using temporary config files instead of saved configs from FiltersModal.

### Issue
- When user saved/loaded a config in FiltersModal, the SearchWidget continued using the temp file
- Config filename shown was like "temp_search_6acf0be5-30af-4a86-be1b-a3cd6b42ef71.ouija.json"

### Fix
Added `UpdateSearchWidgetConfig()` method to FiltersModal that:
1. Finds the SearchWidget through the main menu
2. Calls `LoadConfig()` on the widget with the saved/loaded config path
3. Works for both drag-drop mode and JSON editor mode save/load operations

Now when user saves or loads a config in FiltersModal, the SearchWidget immediately updates to use that config file and shows the proper filename in its UI.

## FiltersModal Drag-Drop Visual Refresh Fix
Fixed the issue where the item palette would go blank after dragging and dropping items.

### Issue
- When dragging an item to needs/wants drop zones, the visual list would disappear
- User had to click a side nav tab to make items reappear
- Caused by `RefreshItemPalette()` calling `LoadCategory()` which rebuilt entire UI

### Fix
Modified `RefreshItemPalette()` to:
1. Update selection states in-place instead of reloading the entire category
2. Find existing ResponsiveCard controls and update their IsSelectedNeed/IsSelectedWant properties
3. Added Tag property to cards during creation for identification
4. Falls back to full reload only if UI structure not found

Now drag-drop operations update smoothly without visual disruption.

## FiltersModal Redesign - All Items in One Scrollable List
Redesigned the FiltersModal to show all items in one continuous scrollable list with tabs as navigation anchors.

### Issues
- Users were required to use tabs to navigate between categories
- Only one category was visible at a time
- No visual indication of which tab was currently active

### Solution
1. **LoadAllCategories() method**: Loads all categories in one continuous list:
   - Legendary Jokers
   - Rare Jokers  
   - Uncommon Jokers
   - Common Jokers
   - Vouchers
   - Tarots
   - Spectrals
   - Tags

2. **Tab Navigation**: Tabs now act as quick navigation anchors:
   - Clicking a tab scrolls to that section
   - Tabs get highlighted with "active" class
   - Smooth scrolling to section headers

3. **Visual Improvements**:
   - Current tab is visually highlighted
   - Section headers (20px bold) clearly separate categories
   - All items visible in one long scroll view
   - Search filters all categories at once

4. **Favorites Tab**: Shows only selected items grouped by category

Now users can freely scroll through all items or use tabs to jump to specific sections, with clear visual feedback on the current location.

## Auto-Highlight Tab Navigation on Scroll
Added automatic tab highlighting based on scroll position in FiltersModal.

### Implementation
- Added `OnScrollChanged` event handler to the main ScrollViewer
- Calculates which section header is most visible in the viewport
- Automatically highlights the corresponding tab as you scroll
- Updates only when entering a new section to avoid excessive updates

### How it works
1. Tracks all section headers and their positions
2. On scroll, finds the header closest to the top of the viewport
3. Highlights the tab corresponding to that section
4. Provides smooth visual feedback as users scroll through categories

The tabs now serve dual purpose:
- Click to jump to a section
- Auto-highlight to show current scroll position

## Fix Favorites Tab Breaking Main View
Fixed the issue where clicking Favorites would break the normal category view.

### Issue
- Clicking Favorites replaced the main scrollable view
- Clicking other tabs after Favorites would not reload the main view
- The app got stuck in a broken state where nothing would load

### Solution
1. **Added NavigateToSection method** that checks if we're in Favorites view and reloads the main view first
2. **Track current active tab** to know when we need to reload
3. **Updated RefreshItemPalette** to handle Favorites view separately
4. **Modified tab click handlers** to use NavigateToSection instead of LoadCategoryWithScroll

Now the flow works correctly:
- Click Favorites → Shows only selected items
- Click any other tab → Reloads main view and scrolls to that section
- Drag-drop in Favorites view → Updates the favorites display
- Everything transitions smoothly without getting stuck
