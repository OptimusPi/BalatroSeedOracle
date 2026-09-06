# HANDOFF

Session handoff for the next agent (Claude Code). All work below is committed and
pushed to `main` on `OptimusPi/BalatroSeedOracle`. Every commit builds clean
(`dotnet build src/BalatroSeedOracle/BalatroSeedOracle.csproj -c Debug` → 0 warnings, 0 errors).

## Commits this session (oldest → newest)

| Commit | Summary |
|--------|---------|
| `c563e347` | Search DB-backed results (fixes "Minimize & Continue" freeze) + #15 rename, #12 create-new-in-details, #16 z-order + edition toggle, #13 OR color, #14 newest-first sort |
| `3e7992a2` | #14 right-click delete/rename context menu + Exit button; #16 favorites add/remove (all item types, observable) |
| `3f763ab3` | #16 favorites persistence → `favorites.json` |
| `2eba9747` | #13 operator-tray card fixes (per-card remove button, scale-to-fit, edition/sticker overlays) |
| `c97ea3c3` | #12 card hover tooltip on the picker |

## The freeze fix (most important)

`SearchModalViewModel` now writes matches to a per-filter DuckDB lake
(`Motely.DataLake.SeedLakeSink`, file `Seeds/<filter>.duckdb`) and only feeds the
in-memory grid when the modal is on screen. When minimized (`IsMinimized == true`)
the result callbacks skip all `Dispatcher.UIThread.Post` calls, so the search no
longer floods the UI thread → no freeze. Grid is capped (`MaxLiveResults = 5000`);
`ReconnectFromLake()` backfills the grid from DuckDB on reopen.
Added project ref: `Motely.DataLake` in `BalatroSeedOracle.csproj`.

## Issue status

### #12 — New Filter and Card Info
- [x] "+ Create New Filter" button in the details panel (`FilterSelectionModal.axaml`), not just the placeholder page.
- [x] Card hover tooltip **mechanism**: `SelectableItem.TooltipText` (name/type/edition/stickers) bound via `ToolTip.Tip` on the picker card hitbox.
- [ ] **Effect-text DATA does not exist.** `BalatroData.Descriptions` (keyed by lowercased item name) is an empty dictionary ready to fill; `BalatroData.GetDescription(name)` reads it. No fabricated descriptions were shipped. Populate this to light up the effect text in the tooltip.

### #13 — "or, and, bannedItems" box (operator tray)
- [x] bug 1: card scale-to-fit (Viewbox in tray template).
- [x] bug 2: per-card remove button + `RemoveFromTrayCommand`.
- [x] bug 6: edition/sticker/soul overlays now render (tray uses `FilterItemCard`).
- [x] bug 7: OR shows green (converter made case-insensitive in `ClauseTrayConverters.cs`).
- [ ] bug 3: clicking the box repeatedly re-adds the box to itself. Source: `VisualBuilderTab.axaml.cs` `OnUnifiedTrayPointerPressed` (~line 2112) sets `_draggedItem = vm.UnifiedOperator` and a click-drag-release on the tray self-adds. Needs drag-system work.
- [ ] bug 4: card dragged from tray into a zone gets stuck (in-zone operators are read-only: `VisualBuilderTab.axaml.cs` ~line 976–1004; nested drag uses `FilterOperatorControl.OnCardPointerPressed`, a different drag path with no accepting remove target).
- [ ] bug 5: fan-out overlap / lost hover animation for the **committed** operator control (`FilterOperatorControl.axaml(.cs)` `UpdateFannedLayout`). The **tray** fan-out is fine.

### #14 — Search Interface
- [x] saved filters list sorted newest-first (`PaginatedFilterBrowserViewModel.LoadFilters`).
- [x] right-click a saved filter → Delete / Rename (`FilterSelectionModal.axaml`, `FilterSelectionModalViewModel.DeleteFromContext` / `RenameFromContext`).
- [x] Exit button next to Back (`ExitCommand` closes outright, bypasses `TryGoBack`).
- [ ] Save & Exit vs. auto-save-on-back: designer (FiltersModal) auto-saves on every edit (`VisualBuilderTabViewModel.TriggerAutoSave` ~2550, `ConfigureFilterTabViewModel` ~1113). Reworking to explicit Save&Exit is unstarted.
- [ ] Clear-filter button (whole search / per box). "Start Over" logic exists (`VisualBuilderTab.axaml.cs` ~1428) but has no XAML `Click` wiring found; per-box clear would call `SelectedMust`/`SelectedShould` `.Clear()`.

### #15 — Rename a saved search
- [x] Done. Rename button + dialog + persists `JamlConfig.Name`.

### #16 — Filter Bugs
- [x] bug 1: Scoring Config card z-order (`ZIndex="10"` on the header grid in `FilterSelectionModal.axaml`).
- [x] bug 2: editions toggle off on re-click (`SetEdition` in `VisualBuilderTabViewModel`).
- [x] bug 3: favorites now work for **all** item types (was Joker-only); `IsFavorite` made observable in `SelectableItem`.
- [x] bug 4: remove-from-favorites works (context menu Add/Remove).
- [x] favorites **persist** across restarts (`FavoritesService`, `favorites.json`, applied on load via `ApplyPersistedFavorites`).
- [~] bug 5 (edit edition/sticker mutates favorite): mitigated — `IsFavorite` is now a real observable and the mutators already `continue` on favorited items. NOT fully decoupled: favorites still reference the same shelf instance (no clone-on-favorite). Optional `FilterItem.Clone()` would fully isolate them; copy shape at `VisualBuilderTab.axaml.cs` ~1074–1102.

### #6 — Installation tutorial
- [ ] Untouched. Needs written steps and/or a video (can't produce video here).

## ⚠ Uncommitted work in the tree (NOT from this session)

The working tree had pre-existing changes that are unrelated to the above and were
left untouched on purpose. Review and commit these separately:
- Font swap: deletion of `src/BalatroSeedOracle/m6x11plusplus.otf` and `src/m6x11plusplus.otf`; new `src/BalatroSeedOracle/m6x11plusplus.ttf` (untracked).
- Modified: `App.axaml`, `Constants/UIConstants.cs`, `Controls/BalatroShaderBackground.cs`, `Views/MainWindow.axaml(.cs)`, `Views/BalatroMainMenu.axaml(.cs)`.
- Deleted: `CLAUDE-CAGE.md`, `.claude/settings.json`, `.claude/settings.powershelloption.txt`, `.avalonia/projects.json`.
- Untracked: `profile.json` (runtime data).

## New files added this session
- `src/BalatroSeedOracle/Services/FavoritesService.cs`

## Verify
```
git submodule update --init --recursive   # if Motely is empty
dotnet build src/BalatroSeedOracle/BalatroSeedOracle.csproj -c Debug
```

## 2026-09-06 — build break at `a9f5fbd`, fixed, UNCOMMITTED

"Every commit builds clean" above was no longer true: `a9f5fbd` read
`MotelyProgress.CompletedBatchCount` / `TotalBatchCount`, which engine commit `44727419`
(in the `src/MotelyJAML` submodule) removed. 8× CS1061 in `SearchModalViewModel.cs`.

Fix — read the counters from the running `IMotelySearch` instead (one file, four edits,
`src/BalatroSeedOracle/ViewModels/SearchModalViewModel.cs`):
- `:738-744` pause: `if (ContinueFromLast && _search is { ResumeBatchIndex: >= 0 } pausing) SaveResumeState(pausing.ResumeBatchIndex, pausing.TotalBatchCount)` — `ResumeBatchIndex` is the engine's resume hint (re-covers at most threadCount−1 batches, skips none; −1 in provider mode → nothing saved).
- `:1460-1465` every-10-batches autosave reads `_search.CompletedBatchCount` / `ResumeBatchIndex` / `TotalBatchCount`.
- `:1471` `SaveResumeState(long resumeBatch, long totalBatchCount)` — first param renamed to what it is.
- `:1528-1529` `CurrentBatch` / `MaxBatch` read `search.CompletedBatchCount` / `TotalBatchCount`.

```
dotnet build src/BalatroSeedOracle/BalatroSeedOracle.csproj -c Debug
  → Build succeeded. 0 Warning(s) 0 Error(s)
```
Not launched yet. Submodule pin unchanged (`eee395f8`). To commit just this:
`git add src/BalatroSeedOracle/ViewModels/SearchModalViewModel.cs HANDOFF.md`.
