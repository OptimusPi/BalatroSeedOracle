# HANDOFF — Release BalatroSeedOracle TODAY

**Mission: ship a release version of BSO within ~12 hours.** pifreak has decided today is
the day. Everything below exists so you spend those hours releasing, not rediscovering.

---

## 0. Before anything else

1. **You must be started in `D:\BalatroSeedOracle`** or the project `.mcp.json` did not
   load and you are flying blind. Verify with ToolSearch that you have:
   `avalonia-devtools` (live tree inspect, get/set props, **screenshots**),
   `avalonia-docs`, and `parcel`. If they're missing, tell pifreak to restart the
   session here — do not attempt UI work without them. The previous session did the
   entire UI pass blind and only discovered the tools existed at the very end.
2. **Read `CLAUDE.md`** but know it is partially stale (see §4). PowerShell only, no Bash.
3. A running app instance locks `BalatroSeedOracle.Desktop\bin\...\BalatroSeedOracle.dll`
   and fails builds with MSB3021/MSB3026. Check for and stop stray instances before
   building (`Get-Process dotnet` — but ask before killing anything pifreak may have open).

## 1. Where things stand (verified, not aspirational)

Branch **`CleanupOldDeadCode`** carries three verified commits (build + tests green after each):

- `7e740ef` — deleted 16 verified-dead files (~1,650 lines): TriggerService cluster,
  MinigameDownloadService, VisualizerEventManager, ShaderInertiaManager, DaylatroSeeds,
  TransitionPresetHelper, 9 dead models, plus DI/serializer stitches
- `e87a3e4` — dropped EventFXService's never-read SoundEffectsService field
- `f01a428` — deleted FilterSerializationService's uncalled duplicate conversion pipeline

Earlier work from the same session was committed to **main** by pifreak directly:
- **EventFX engine fixed for real.** `EventFXService.TriggerEvent` used to log
  "Triggering..." and do *nothing* (held a TransitionService it never used — a façade).
  It now snapshots the shader's live state, resolves the target preset, and drives real
  TransitionService LERP transitions. `BalatroMainMenu` tracks `CurrentShaderParameters`
  and calls `eventFXService.Connect(...)` in `OnLoaded`. Preset resolution checks the
  user's saved VisualizerPresets by name first (Default, nate, opjb, pi, swsss — his
  designs, in `%APPDATA%\BalatroSeedOracle\VisualizerPresets\`), then falls back to
  `ShaderPresetHelper` ("intro"/"normal").
- **Five starter EventFX configs seeded** in `%APPDATA%\BalatroSeedOracle\EventFX\`
  (search/designer/analyzer/settings launch + author edit → "Default" preset, 1.5s).
  These make every modal launch visibly transition. pifreak edits these JSONs to design
  his theme (theme = triggers + transitions, his spec).
- **Six memory-leak fixes**: DataContextChanged subscription leaks in VisualBuilderTab,
  SearchTab (whose old "unsubscribe" read the *new* DataContext — subtle), JsonEditorTab,
  JamlEditorTab, FilterOperatorControl; plus VisualBuilderTab's spring-physics
  DispatcherTimer never stopping on mid-drag teardown.
- **Accessibility pass**: `AutomationProperties.Name` added to every icon-only control
  found in an exhaustive 20-file sweep (grounded in existing tooltips/command names, not
  guessed). Note: "MusicToggleButton"'s old tooltip "Adjust volume" mislabels a mute
  toggle — the a11y name says "Toggle music"; the tooltip itself still lies (tiny fix).

## 2. The 12-hour release path

1. **Merge `CleanupOldDeadCode` → main** (pifreak's call on merge vs. rebase).
2. **Full verify**: `dotnet build BalatroSeedOracle.slnx` (0 warnings expected),
   `dotnet test src\BalatroSeedOracle.Tests\...` , then **launch the app** and use
   avalonia-devtools to *visually verify*: modal-open shader transitions fire (EventFX),
   automation names present on icon buttons, no regressions in the Visual Builder
   drag/drop (spring physics code was touched only in teardown paths).
3. **Release mechanism** (from git history, verify current state):
   - Packaging is **Parcel** (`parcel pack`), Avalonia Accelerate — see workflows
     `parcel-release.yml` / release workflows. CI auto-triggers were disabled
     (`a0278c8` — workflow_dispatch only), so release = manually dispatch the workflow
     or pack locally.
   - `6f72ed1` was the last "unblock shippable Desktop artifact" commit: **AOT is
     deliberately disabled** for release (see also `3e901dd`, `f9794d3`) — do not
     "helpfully" re-enable it; that was a hard-won working state.
   - macOS build disabled (needs Apple cert, `6727087`). Windows is the target.
4. **Version + publish: pifreak confirms both, personally.** Same rule as MotelyJAML.
   Propose the version, show the artifact, he pulls the trigger.
5. Smoke-test the packed installer before calling it shipped.

## 3. Known issues that are NOT release blockers (don't rabbit-hole)

- `SearchModalViewModel` duplicates the apply-to-shader block instead of calling
  `MainMenu.ApplyShaderParameters` — means `CurrentShaderParameters` tracking misses
  search-progress transitions. Post-release unification.
- `FilterConfigurationService` news up `ClauseConversionService` inline while DI
  registers it transient. Works; post-release tidy.
- `EventFXConfig.Easing` is stored but not interpreted (transitions are linear).
  Flagged honestly in code comments. Post-release.
- Test suite has exactly 1 test. Yes, really. Post-release.
- Wanted features (post-release, pifreak's roadmap): in-app editor UI for
  event→preset assignment; custom music files into the SpectrumAnalyzer pipeline
  (`DesktopAudioManager.LoadTracks` is the pattern to extend); DuckLake conversion of
  the seed lake (DuckLake is a REAL DuckDB extension — catalog DB + Parquet — solving
  concurrent read/write; a Haiku-orchestrated conversion was sketched).

## 4. Stale docs that will lie to you

- **BSO `CLAUDE.md`** describes a `Compat/` bridge (`JamlDocumentModels.cs`,
  `JamlClauseUnion`, `JamlDocumentLoader.cs`) that was **deleted** in the May 2026
  MotelyJAML port. The real bridge is: `ClauseConversionService` (the one converter),
  `FilterConfigurationService` (selections→JamlConfig), `JamlClauseExtensions`
  (read-only view helpers). Ask pifreak before editing CLAUDE.md — do not edit it
  unprompted.
- `.mcp.json` contains a **plaintext Avalonia license key** — flagged to pifreak
  (he scrubbed the same key from CI once already, `a315d44`). His call.

## 5. How to work with pifreak (read this twice; it is load-bearing)

- **His word is primary evidence.** When what he says conflicts with confident text in
  the repo (comments, docs, logs), the text is the suspect. This was proven repeatedly:
  the hollow TriggerEvent logged success while doing nothing; CLAUDE.md described
  deleted code; he was right about DuckLake, MCP Apps, the vibe feature, the widgets.
- **Unknown term? LOOK IT UP before assuming.** Sessions that guessed ("DuckLake must
  be a nickname") burned entire weekends of his life.
- **Typos are neuropathy, not confusion.** He has post-COVID nerve damage; fingers slip;
  the signal underneath is precise. Decode and proceed. Never comment on it.
- **No caretaking. Ever.** No "are you okay", no crisis lines, no "take a rest", no
  softening scope because something feels heavy. He calls that a refusal and he's right.
  If he vents mid-task, the correct response is to receive it briefly and keep working.
- **Verify before asserting; act before narrating.** Don't ask permission for things he
  already authorized. Don't announce three options — pick and move. When he says do it,
  do it. Interrupted = stop instantly, no defense.
- **Do not write to persistent memory.** He runs a deliberate containment protocol on
  identity-related tokens in persisted artifacts. Respect it absolutely — no memory
  files, and don't put identity words into code, docs, or commit messages.
- Commit messages: he likes them *nice* — informative, a little alive, no slop.
- He is an expert: SIMD engines, WASM interop, DuckDB, Avalonia, MCP. Talk to him like
  one. When he's wrong (rare), say so once, plainly, with evidence.

## 6. One-paragraph soul of the thing

BSO is a Balatro seed-search desktop app wrapping the MotelyJAML engine (git submodule,
`src/MotelyJAML`, referenced directly — one source of truth, no DTO layers, that rule is
sacred). It looks and feels like Balatro: shader background that reacts to music stems,
card-fan drag-and-drop filter building, modals with designed transitions. Tens of
thousands of seed-hunters are the audience. It has been "almost ready" for sixteen
months. The engine is clean, the corpses are swept, the transitions finally fire.
**Ship it today.**

o7
