# PRD-06: Search System

## Summary

The core feature of the application. Users configure search criteria (seed range, deck, stake, filters), execute searches against local or remote engines, monitor progress in real-time, view results in a sortable grid, and can minimize searches to desktop widgets for background execution.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Views/Modals/SearchModal.axaml` | Tabbed modal (Search + Results) |
| `Views/SearchModalTabs/SearchTab.axaml` | Search config UI |
| `Views/SearchModalTabs/ResultsTab.axaml` | Results display |
| `Views/SearchModalTabs/SettingsTab.axaml` | Search settings |
| `ViewModels/SearchModalViewModel.cs` | Search orchestration |
| `ViewModels/SearchResultViewModel.cs` | Result item state |
| `Services/SearchManager.cs` | Core search execution |
| `Services/ActiveSearchContext.cs` | Active search tracking |
| `Services/SearchTransitionManager.cs` | Shader transitions during search |
| `Models/SearchConfiguration.cs` | Search config model |
| `Models/SearchCriteria.cs` | Criteria model |
| `Models/SearchState.cs` | State model |
| `Models/SearchResult.cs` | Result model |
| `Models/SearchProgress.cs` | Progress model |
| `Models/SearchResultEventArgs.cs` | Event args |
| `Components/Widgets/SearchWidget.axaml` | Minimized search widget |
| `ViewModels/SearchWidgetViewModel.cs` | Widget state |
| `Controls/SortableResultsGrid.axaml` | Results data grid |
| `ViewModels/Controls/SortableResultsGridViewModel.cs` | Grid state |
| `Windows/DataGridResultsWindow.axaml` | Pop-out results window |

---

## Requirements

### R1 — Search Modal (Tabbed Interface)

**Tabs:**

| Tab | Content |
|-----|---------|
| Search | Configuration form + progress display |
| Results | Sortable results grid |

- `BalatroTabControl` with triangle indicator
- "Minimize & Continue" button visible during active search (orange, top-right)

### R2 — Search Configuration (Search Tab)

**Inputs:**
- Seed range (start seed, end seed, or "random")
- Deck selector (via DeckSpinner component)
- Stake selector (via StakeSpinner component)
- Filter selector (opens FilterSelectionModal to pick a saved filter)
- Source selector (local vs remote search engine)
- Thread count / parallelism settings
- Max results limit

**Actions:**
- Start Search button
- Cancel Search button (visible during search)
- Clear / Reset button

### R3 — Search Execution

```csharp
public class SearchManager
{
    Task StartSearchAsync(SearchConfiguration config, CancellationToken ct);
    void CancelSearch(string searchId);

    // Events
    event Action<SearchProgress>? OnProgress;
    event Action<SearchResult>? OnResultFound;
    event Action<string>? OnSearchCompleted;
    event Action<string, Exception>? OnSearchError;
}
```

**Search Engines:**

| Engine | Description |
|--------|-------------|
| `LocalSearchEngine` | Searches local database/computed seeds |
| `RemoteSearchEngine` | Calls remote API for search |

- Searches run on background threads
- Progress reported as percentage (seeds checked / total)
- Results streamed as found (not batched at end)
- Multiple concurrent searches supported via `ActiveSearchContext`

### R4 — Search Progress Display

- Progress bar with percentage
- Seeds checked / total seeds count
- Estimated time remaining
- Results found count
- Elapsed time
- Current seed being checked
- Cancel button

### R5 — Results Display (Results Tab)

**SortableResultsGrid:**
- Column-sortable data grid
- Columns: Seed, Score/Match %, relevant filter match details
- Click row to view seed details
- Right-click context menu (copy seed, analyze seed, etc.)
- Pagination for large result sets
- Pop-out to standalone window (`DataGridResultsWindow`)

### R6 — Search Widget (Minimized Desktop Monitor)

When "Minimize & Continue" is clicked:
- Search modal closes
- A `SearchWidget` appears on the desktop canvas
- Shows: search progress bar, results count, cancel button
- Click widget to re-open search modal
- Each active search gets its own widget
- Widget fires `SearchModalOpenRequested` event

### R7 — Multiple Concurrent Searches

- `ActiveSearchContext` tracks all running searches
- Each search has a unique ID
- Multiple search widgets can exist simultaneously
- Re-opening a search modal reconnects to the active search
- Searches persist across modal open/close cycles

### R8 — Search Persistence

- `IRestoreActiveSearchesProvider` restores searches on app restart (desktop)
- Search configuration saved so searches can be re-run
- Results can be exported (CSV, database)

### R9 — Search Settings (Settings Tab)

- Default thread count
- Default search engine (local/remote)
- API endpoint configuration
- Results display preferences
- Auto-analyze on result found

---

## Acceptance Criteria

- [ ] Search modal opens with Search and Results tabs
- [ ] All search configuration inputs work (seed range, deck, stake, filter, source)
- [ ] Search starts and progress updates in real-time
- [ ] Results appear in sortable grid as they're found
- [ ] "Minimize & Continue" creates a desktop search widget
- [ ] Search widget shows progress and can re-open modal
- [ ] Multiple concurrent searches work independently
- [ ] Search can be cancelled mid-execution
- [ ] Results grid supports sorting, pagination, and pop-out
- [ ] Search transitions trigger shader color changes (PRD-05)
