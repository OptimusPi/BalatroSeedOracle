# Ideas Inbox

Quick-capture of links/ideas dropped during sessions. Triage later into ARCHITECTURE / RESEARCH docs. Nothing here is committed work — it's a parking lot.

## Avalonia features to evaluate (links from pifreak, 2026-05-29)

- **TreeDataGrid** — https://avaloniaui.net/tree-data-grid
  - Candidate for the **seed results grid** and/or a **JAML/filter editor** tree view.
  - NOTE: `Avalonia.Controls.TreeDataGrid` is **already referenced** in `BalatroSeedOracle.csproj`. The csproj also has a standing plan to migrate `DataGridResultsWindow` → TreeDataGrid (see Desktop csproj comments). Good alignment.
- **Charts** — https://avaloniaui.net/blog/charts
  - Possible seed-result visualization / score distribution charts.
- **Rich Text Editor** — https://avaloniaui.net/blog/rich-text-editor
  - Possible use: filter notes / descriptions field, or a nicer JAML editor surface.
- **NativeWebView** — https://docs.avaloniaui.net/controls/web/nativewebview
  - Idea (pifreak): make a **reusable WebView widget**; e.g. **Daylatro** widget could embed the real Daylatro website directly.
  - NOTE: `Avalonia.Controls.WebView` is **already referenced** in the csproj.
- **Avalonia 12** — https://avaloniaui.net/blog/avalonia-12
  - App currently runs on Avalonia **12.0.3** (confirmed via devtools). Review 12.x release notes for relevant changes.
- **MediaPlayer** — `dotnet add package Avalonia.Controls.MediaPlayer`
  - Idea (pifreak): replace **SoundFlow** (Desktop-only, not cross-platform) with a cross-platform audio option. Needs evaluation — SoundFlow currently drives the 8-track audio mixer (Desktop head only).
- Reference docs: https://docs.avaloniaui.net/controls/ · https://docs.avaloniaui.net/controls/primitives/window · https://docs.avaloniaui.net/docs/fundamentals/the-mvvm-pattern

## Tooling / MCP

- **Avalonia Build MCP** — https://docs.avaloniaui.net/tools/ai-tools/
  - Free remote MCP: live Avalonia docs, coding rules, guided prompts, migration tools (DevTools upgrade, WPF→Avalonia). Not yet connected to the session (only `avalonia-devtools` is enabled). Worth adding to MCP config.
- **avalonia-devtools MCP** — connected; used to click the live app + inspect the visual tree (invaluable for the modal bug diagnosis).
- **"Jimbo And MCP Love" MCP** — Balatro/Motely tools now available: `analyze_seed`, `search_seeds`, `get_jaml_schema`, `validate_jaml`, `get_version`.

## DuckLake — ALREADY INTEGRATED, finish doing it "properly" (2026-05-29)

**Not a new idea — it's already in the submodule.** `src/MotelyJAML/Motely.DataLake/MotelyLakeSeedSink.cs` does `INSTALL/LOAD ducklake` + `ATTACH 'ducklake:catalog.ducklake' AS lake (DATA_PATH 'data/')` and streams scored seeds into the catalog (`DuckLakeSeedSink`). The CLI autosaves to it (`--save-seeds`); the TUI has a "Results Browser — DuckLake". `*.ducklake` is in `.gitattributes`.

**The real open work (verbatim from `src/MotelyJAML/TODO.md`):**
- Use the catalog model properly: today it's per-filter `.duckdb` files masquerading as a lake → should be ONE catalog DB with filters as tables; schema evolution = catalog op.
- Pre-populated catalog with curated decks: ship Erratic Deck (etc.) known-dank seeds as Parquet, bundled or first-run download → new install has a starter pack, no search needed.
- Concurrent-reader-while-writer: browse matches in the app while a 72-hour CLI search still runs (no exclusive lock).

**BSO desktop gap:** CLI/TUI read the DuckLake; the Avalonia desktop app should too (DataGridResultsWindow / SQL editor over the single catalog). Keep it LOCAL — no cloud/object-store (avoid the SAB/COEP misery).

- DuckLake background refs: https://ducklake.select/ · https://github.com/duckdb/ducklake · https://ducklake.select/2026/04/13/ducklake-10/

## Balatro 3D Analyzer (pifreak, 2026-05-29) 🗼✨

A 3D way to explore a seed's analysis instead of the flat AnalyzeModal list. Sparks:
- **Ante tower:** each ante = a floor stacked vertically; shop jokers / packs / vouchers / tags arranged in 3D space per floor; fly up through the seed's future.
- **Real card shimmer:** render the actual Balatro holo/foil/polychrome/negative tilt-shine on the jokers a seed produces (the eye candy is the pitch).
- **Score surface:** 3D heightmap of score across antes, or across a batch — dank seeds spike as peaks.
- **Feasibility / synergy:** Avalonia is 2D-native, but the csproj already references `Avalonia.Controls.WebView`. Do the 3D as a **Three.js / WebGL scene hosted in a WebView** → doubles as the "reusable WebView widget" idea (see NativeWebView note above; same pattern could host the real Daylatro site). Motely/`Motely.DataLake` feeds data; WebGL draws.
- Pairs with: AnalyzeModal/AnalyzeModalViewModel (existing seed-analysis surface), the WebView widget idea, charts idea.

## Ads as whimsical pun joker cards (pifreak, 2026-05-29)

- Don't do banner/intrusive ads. Render sponsors/links as **Balatro-style joker cards with puns** — collectible-feeling, on-theme, screenshot-worthy. An ad people don't hate.
- Ecosystem context: **weejoker.app = "The Daily Wee"** — pifreak's Balatro fan community site (same "NOT AFFILIATED WITH LOCALTHUNK OR PLAYSTACK ❤" disclaimer as BSO). Pairs with the in-app **Daylatro widget** (daily seed). One ecosystem: Motely (engine) → Daylatro / Daily Wee (daily/community) → BSO (power tool) → pun-card ads as the tie-in.
- Surfaces: The Daily Wee site, and inside BSO via the **reusable WebView widget** (could embed the real Daily Wee site in-app — see NativeWebView + 3D Analyzer notes; same WebView pattern).

## Big goal: proper MVVM / kill code-behind spaghetti

- pifreak wants a thorough pass to **eradicate code-behind** and enforce **proper MVVM** across BSO.
- Relevant existing docs: `docs/SCRUTINY_MVVM_XPLAT.md`, `docs/RESEARCH_AVALONIA_2026.md`, `AI_CODING_GUIDELINES.md`.
- Watch out for the pattern that caused the 2026-05-29 modal bug: hand-written `InitializeComponent()` overrides shadowing the source-generated `InitializeComponent(bool)`, leaving `x:Name` fields null. A real MVVM refactor (event→command, content via `DataTemplate`/binding instead of code-behind `ShowModalContent`) would remove this whole class of bug.
