# CRITICAL MVP BLOCKING BUGS

**Status**: ACTIVE - BLOCKING MVP RELEASE
**Priority**: HIGHEST
**Last Updated**: 2025-11-20

---

## BUG 1: Search Modal "Filter File Not Found" Error

### Severity: CRITICAL - BLOCKS MOST IMPORTANT FEATURE
### Reported By: User (2025-11-20)

### User Quote:
> "when navigating through the SEARCH button, and trying to DO A SEARCH in the SEARCH MODAL.... you know the absolute MOST IMPORTANT PART OF THE FUCKING APP, CLAUDE!!!!!!
> it says like filter file cannot be found yet i MUST SELECT ONE before GETTING HERE so WHAT GIVES!"

### Symptoms:
1. User selects a filter from Filter Selection Modal
2. Search Modal opens successfully
3. User tries to start search (clicks Start Search button)
4. ERROR: "No filter path available - filter must be loaded first!"

### Root Cause:
Located in [SearchModalViewModel.cs:888-893](src/ViewModels/SearchModalViewModel.cs#L888-L893):

```csharp
private SearchCriteria BuildSearchCriteria()
{
    if (string.IsNullOrEmpty(CurrentFilterPath))
    {
        throw new InvalidOperationException(
            "No filter path available - filter must be loaded first!"
        );
    }
    // ...
}
```

The `CurrentFilterPath` property is empty/null when starting search, even though the filter was loaded.

### Investigation Findings:

1. **Filter Loading Process** ([SearchModalViewModel.cs:1335-1354](src/ViewModels/SearchModalViewModel.cs#L1335-L1354)):
   - `LoadConfigFromPath()` checks if file exists
   - If exists, sets `CurrentFilterPath = configPath` (line 1354)
   - If NOT exists, logs error and returns silently (line 1341)

2. **Recent Change Impact**:
   - Recent fix for deck/stake selection JSON refresh (2025-11-20)
   - Modified `LoadTabData()` in FiltersModalViewModel
   - Tab switching now saves to disk and reloads from disk
   - **HYPOTHESIS**: Filter path might be relative instead of absolute

3. **Path Construction** ([BalatroMainMenu.axaml.cs:298-302](src/Views/BalatroMainMenu.axaml.cs#L298-L302)):
   ```csharp
   var filtersDir = AppPaths.FiltersDir;
   var configPath = System.IO.Path.Combine(
       filtersDir,
       result.FilterId + ".json"
   );
   ```
   This constructs an absolute path, which should be correct.

### Possible Causes:
1. **Filter file doesn't exist at expected path**
   - Save operation failed silently?
   - File was deleted between selection and search?

2. **CurrentFilterPath is cleared/lost during modal lifecycle**
   - Tab switching clears it?
   - Modal reinitialization clears it?

3. **Timing issue**
   - LoadFilterAsync() hasn't completed before user clicks search?
   - Async/await issue?

### Fix Strategy:
1. Add debug logging to track `CurrentFilterPath` value through entire lifecycle
2. Verify file actually exists when opening Search Modal
3. Add validation that LoadFilterAsync() completes before enabling Start Search button
4. Ensure CurrentFilterPath persists across tab switches
5. Add better error message showing WHAT path it tried to load

### Files To Investigate:
- [x] `src/ViewModels/SearchModalViewModel.cs` - LoadConfigFromPath, BuildSearchCriteria
- [x] `src/Views/BalatroMainMenu.axaml.cs` - ShowSearchModalWithFilterAsync
- [ ] `src/Components/FilterSelectorControl.axaml.cs` - Filter selection flow
- [ ] `src/ViewModels/FiltersModalViewModel.cs` - Save filter flow

---

## BUG 2: Seal Buttons Not Working for Standard Cards

### Severity: CRITICAL - BROKEN UI FEATURE
### Reported By: User (2025-11-20)

### User Quote:
> "in the filter builder modal, the STANDARD CARDS
> EDITION HELPER BUTTONS: they all look great, they all work great.
> The SEAL BUTTONS? don't do anything? or don't work the same way?? why not..
> they are SO SIMILAR to editions and even SIMILAR TO THE STICKERS FOR JOKERS, so STUDY A WORKING IMPLEMENTATION AND FIX IT!"

### Symptoms:
1. Edition helper buttons work correctly for Standard Cards
2. Seal helper buttons don't do anything when clicked
3. Sticker buttons work correctly for Jokers
4. Seal buttons should work the same way as Edition/Sticker buttons

### Expected Behavior:
- Clicking a seal button (Gold, Red, Blue, Purple) should set that seal on the selected playing card
- Similar UX to Edition buttons (none, foil, holographic, polychrome)
- Similar UX to Sticker buttons (eternal, perishable, rental)

### Investigation TODO:
1. Find seal button implementation in VisualBuilderTab
2. Compare with working edition button implementation
3. Compare with working sticker button implementation
4. Identify missing event handler or binding
5. Fix seal button Click event

### Files To Investigate:
- [ ] `src/Components/FilterTabs/VisualBuilderTab.axaml` - Seal button definitions
- [ ] `src/Components/FilterTabs/VisualBuilderTab.axaml.cs` - Seal button event handlers
- [ ] `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` - Seal button logic

---

## BUG 3: Deck/Stake Selection JSON Refresh (FIXED)

### Status: ✅ FIXED (2025-11-20)
### Fix: Modified `LoadTabData()` to regenerate JSON for tab index 2

See [MVP_PRD.md](MVP_PRD.md#17-deckstake-selection-fix-) for details.

---

## Testing Checklist

Before marking MVP complete, verify:

### Search Flow
- [ ] Select filter from Filter Selection Modal
- [ ] Search Modal opens with correct filter name shown
- [ ] Start Search button is enabled
- [ ] Click Start Search
- [ ] Search starts without "filter file not found" error
- [ ] Progress updates appear
- [ ] Results are found and displayed

### Seal Buttons
- [ ] Select a Playing Card in Visual Builder
- [ ] Click each seal button (Gold, Red, Blue, Purple, None)
- [ ] Verify seal is applied to selected card
- [ ] Verify seal shows in JSON output
- [ ] Verify seal persists when saving/loading filter

---

## Debugging Commands

### Check if filter file exists:
```bash
ls "x:\BalatroSeedOracle\Filters" | grep -i "filtername"
```

### Check CurrentFilterPath in logs:
```bash
grep "CurrentFilterPath" logs.txt
grep "Filter file not found" logs.txt
```

### Verify JSON contains seal:
```json
{
  "type": "PlayingCard",
  "value": "A♠",
  "seal": "Gold"  // ← Should appear when seal button clicked
}
```
