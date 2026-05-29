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

## Big goal: proper MVVM / kill code-behind spaghetti

- pifreak wants a thorough pass to **eradicate code-behind** and enforce **proper MVVM** across BSO.
- Relevant existing docs: `docs/SCRUTINY_MVVM_XPLAT.md`, `docs/RESEARCH_AVALONIA_2026.md`, `AI_CODING_GUIDELINES.md`.
- Watch out for the pattern that caused the 2026-05-29 modal bug: hand-written `InitializeComponent()` overrides shadowing the source-generated `InitializeComponent(bool)`, leaving `x:Name` fields null. A real MVVM refactor (event→command, content via `DataTemplate`/binding instead of code-behind `ShowModalContent`) would remove this whole class of bug.
