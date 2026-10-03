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
- [x] **Effect text** (2026-09-27, branch `claude/trusting-galileo-ex3005`): all 234 jokers/tarots/spectrals/planets/vouchers resolve to corpus text. See the 2026-09-27 section below.

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
- [x] Clear-filter buttons (2026-10-03): "Clear" in the Must and Should headers (`ClearMustCommand` / `ClearShouldCommand`) and a "Start Over" button in the Must header wired to `OnStartOverClick`, which now runs `ClearAllCommand` (Must, Should, Must Not, tray) so the parent filter's collections and `ItemConfigs` stay in sync. Built 0/0, `dotnet test` 949 passed. Not launched, so the header layout is unchecked visually.

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

## 2026-09-27 — #12 effect text, engine currency check (`claude/trusting-galileo-ex3005`)

Submodule: `git submodule update --init --recursive` → `SUBMODULE_OK`, pinned `eee395f8`.
The 2026-09-06 `SearchModalViewModel` fix above is already on `main` (landed in `7da02aa`);
build at the pin was clean before any change: 0 warnings, 0 errors.

### #12 — what was actually wrong
`ed37605` had already filled `InitializeDescriptions()` with 352 en-us strings, but
(a) `SelectableItem.TooltipText` passes `Name` = Motely enum member (`EightBall`,
`OopsAll6s`, `TheWheelOfFortune`) and the keys were display names (`"8 ball"`), so on `main`
only **106 of 234** in-scope items resolved; (b) 141 of those strings carry flattened
placeholders (`"X in X chance"`, `"+X Mult"`, `"$X"`).

| Commit | What |
|--------|------|
| `1bfcc3c` | `ItemNameComparer` (letters+digits, case/accent-insensitive) on `Descriptions`; enum name and display name hit the same entry. `BalatroData` → `partial`. |
| `8d66e3e` | `scripts/gen-descriptions.mjs` → `Models/BalatroData.Descriptions.g.cs`, called after the en-us table so corpus text wins. Joker 150/150, Tarot 22/22, Spectral 18/18 (incl. The Soul, Black Hole), Planet 12/12, Voucher 32/32. No item left empty, nothing hand-written. |
| `c5992eb` | First tests in `BalatroSeedOracle.Tests` (it had zero): every non-wildcard item in `BalatroData.{Jokers,TarotCards,SpectralCards,PlanetCards,Vouchers}` has non-empty, placeholder-free text; roster sizes; aliases; spot numbers. |

Source: seedfinder.app `corpus/knowledge/{jokers,consumables}.md` @ `ea74410` (v1.0.1o-FULL).
Jokers = `Effect:` line minus the `(Currently: …)` run-state readout; tarot/spectral/planet =
first effect sentence after the price; vouchers = the `Base`/`Upgraded` halves, the upgrade
carrying its base line (`"4x more often (…) Upgrades Hone: Foil/Holo/Polychrome appear 2x more often."`).
Extra keys: `8 Ball`, `caino`, `ring_master`, `selzer`, `gluttenous_joker`.

Regenerate (writes CRLF to match `*.cs eol=crlf`, so a rerun is byte-identical to a fresh checkout and `git status` stays clean):
```
node scripts/gen-descriptions.mjs [corpusDir]   # default $BALATRO_CORPUS_DIR, then ../seedfinder.app/corpus/knowledge
```

Verified:
```
dotnet build src/BalatroSeedOracle/BalatroSeedOracle.csproj -c Debug --no-incremental  → 0 Warning(s) 0 Error(s)
dotnet test src/BalatroSeedOracle.Tests                                                 → Passed 490, Failed 0
  (negative control, corpus init commented out                                          → Failed 155 / 490)
```
Not verified: the app was not launched; tooltip wrapping relies on the Fluent `ToolTip`
template (corpus text is single-line, no `\n`).

Side effect of normalized lookup (out of #12 scope, not changed): legacy en-us text now also
shows for tags (24/24; 8 carry an `X` placeholder: Investment, Handy, Garbage, Juggle (`+X`),
Top-up, Speed, Orbital, Economy) and boss blinds (28/28; 1 carries a placeholder: The Ox,
`Playing a X`). A corpus-backed fill from mechanics.md tags/bosses would close these, as #12 did.
Pre-existing: the tarot shelf skips `any`/`*` but not the `anytarot` wildcard key.

### Engine currency: MotelyJAML master `09a0f378` — NOT bumped
Scratch worktree, submodule at `09a0f378` (91 commits past the pin). One break:
master deleted `Motely.DataLake` (`5aa6ad48` "datalake removal"), which BSO references
(`BalatroSeedOracle.csproj:49`) for the freeze fix's `SeedLakeSink` and, transitively,
`DuckDB.NET.Data` in `ReconnectFromLake`:
```
warning MSB9008: The referenced project ../MotelyJAML/Motely.DataLake/Motely.DataLake.csproj does not exist.
SearchModalViewModel.cs(52,24): error CS0234: The type or namespace name 'DataLake' does not exist in the namespace 'Motely'
```
With a throwaway `SeedLakeSink` stub + direct `DuckDB.NET.Data.Full` reference the rest of BSO
compiled 0/0 against master, so this is the only API break. Not mechanical: master ships no
disk sink (only `IMotelyResultSink` + `CompositeMotelyResultSink`, now with `Flush()`), so the
minimized-search persistence needs a BSO-owned `IMotelyResultSink` (e.g. per-filter `.txt`,
the shape `14be9856` gave the engine's sink before deleting it) and `ReconnectFromLake` rewritten
to read it. Ticket it before bumping.

Also: `dotnet build BalatroSeedOracle.slnx` fails here only in `src/MotelyJAML/Motely.AppHost`
(ASPIRE009, Aspire CLI bundle setup, via `Motely.Tests`) — environment, engine side, untouched.

