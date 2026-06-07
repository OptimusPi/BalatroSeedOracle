# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Balatro Seed Oracle (BSO) is an **Avalonia UI** desktop app for searching Balatro game seeds. The seed-search engine is **Motely (MotelyJAML)**, vendored as a git submodule and consumed by BSO. BSO is the UI + orchestration; Motely is the vectorized (SIMD) search core. Filters are authored in **JAML** (Jimbo's Ante Markup Language).

## Prerequisites

- **.NET 10 SDK** (C# 14); pinned to `10.0.204` (`rollForward: latestPatch`) in `global.json`.
- **The submodule must be initialized** or the solution won't build: `git submodule update --init --recursive`. The Motely engine lives at `src/MotelyJAML/`.
- **Avalonia license** — supply your own key in an `avalonia.license` file at repo root. It's gitignored; never commit it. `Directory.Build.props` also accepts the key via the `PARCEL_LICENSE` env var (overrides the file).

## Common commands

Run the desktop app (the runnable head; running processes are named `BalatroSeedOracle.Desktop`):

```powershell
dotnet run -c Release --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj   # fast search
dotnet run -c Debug   --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj   # debug; searches MUCH slower
```

Build / verify compile (use this to check your changes):

```powershell
dotnet build BalatroSeedOracle.sln
```

Tests (the only test project is Motely's):

```powershell
dotnet test src/MotelyJAML/Motely.Tests/Motely.Tests.csproj
dotnet test src/MotelyJAML/Motely.Tests/Motely.Tests.csproj --filter "FullyQualifiedName~SomeTestName"   # single test
```

On a fresh clone, restore may need the public feed passed explicitly (`src/MotelyJAML/nuget.config` leaves room for optional local Bootsharp feeds): append `--source https://api.nuget.org/v3/index.json` to the `run`/`restore` command if restore fails.

## Hard rules

- **NEVER run Motely.** No `dotnet run` on `Motely.CLI`, `Motely.TUI`, or any Motely process — running a search burns huge time. You may **build** the solution to check compile errors, and read/edit/analyze Motely freely. Do not "run a search to verify."
- **`TreatWarningsAsErrors=true`** (`Directory.Build.props`) with `Nullable=enable` and `EnforceCodeStyleInBuild`. A warning fails the build. Code must be warning-clean, including nullability.
- **Compiled bindings are on by default** (`AvaloniaUseCompiledBindingsByDefault=true`). The Desktop head ships self-contained single-file (JIT). Keep code reflection-free where it matters: use `System.Text.Json` source-gen contexts and `[YamlStaticContext]`, generic `Enum.GetValues<T>()`, and static property-setter maps — not `PropertyInfo.SetValue`/`Enum.GetValues(typeof(...))`.
- **Logging goes through `DebugLogger`** (`src/BalatroSeedOracle/Helpers/DebugLogger.cs`): `DebugLogger.Log/LogError/LogImportant(component, message)`. Reserve `Console.WriteLine` for always-visible critical failures only.

## Architecture (the parts that span multiple files)

**Project layout** — one shared core, multiple platform heads (`src/`):
- `BalatroSeedOracle/` — the **core**: all shared Views, ViewModels, Services, Models, Converters, Controls, Components. Platform-agnostic.
- `BalatroSeedOracle.Desktop` / `.Android` / `.iOS` — **platform heads**. Each holds only its entry point, `PlatformServices.RegisterServices`, and platform-only overrides. Desktop is the head you run.
- `src/MotelyJAML/` — the Motely submodule: `Motely` (engine), `Motely.Tests`, `Motely.TUI`, `Motely.CLI`, `Motely.Wasm`.
- `JamlFilters/` (repo root) — pre-made JAML filter configs (`.jaml`). `SearchResults/` (repo root) — DuckDB result databases.

**Cross-platform pattern** — Do **not** use `#if BROWSER` / platform `#if`. Platform-specific behavior comes from two places: (1) the `IPlatformServices` abstraction + DI for capabilities (`SupportsFileSystem`, `SupportsAudio`, ...), and (2) putting platform-only code physically in that platform's head project (code in a head isn't compiled for other platforms). Swappable service impls live in `src/BalatroSeedOracle/Services/Platforms/` (e.g. `FileSystemAppDataStore`, `FileSystemDuckDBService`).

**MVVM + DI** — ViewModels inherit `ObservableObject` (CommunityToolkit.Mvvm), using `[ObservableProperty]` and `[RelayCommand]`. Views are thin (minimal code-behind, `{Binding}`, `x:Name` field access not `FindControl`). Services are registered in `App.axaml.cs` via `ServiceCollectionExtensions` plus per-platform `PlatformServices.RegisterServices`. **All dependencies via constructor injection.** Do **not** call `ServiceHelper.GetService`/`GetRequiredService` inside any View or ViewModel **constructor** — the creator resolves from DI and passes dependencies in. `ServiceHelper.GetService<T>()` is only for one-off resolution *outside* constructors. Modals follow the same rule: resolve View+ViewModel from DI, don't `new ModalView()` then service-locate inside it.

**Search data flow** — UI (search modal tabs, search widgets) → `SearchManager` / `ActiveSearchContext` (`Services/`) → the draft config is serialized to JAML via `JamlFormatter.Format` and loaded through `Motely.Filters.Jaml.JamlConfigLoader.TryLoad`, which parses **JAML only** (`YamlDotNet`) — there is no JSON load path. Filters have `must` / `should` / `mustNot` clauses, plus `deck`, `stake`, and scoring `mode`: `sum` (default — adds `count * score` per `should` clause), `max` (max raw occurrence `count` across clauses; per-clause `score` ignored), or `max_count`/`maxcount`. The Search modal's `minScore` is compared against the aggregated value from the selected mode; negative `score` values only affect `sum` mode. Results are written to **DuckDB** (`DuckDB.NET.Data.Full`) and surfaced via the results grid / export services. The current search model is `JamlRootDocument` / `JamlClauseUnion` via `JamlSearchBuilder` — confirm which model a call site uses before wiring into it.

**Other key services** — `SpriteService` (game asset sprites), `UserProfileService` (settings/prefs, `userprofile.json`), `FilterCacheService` (filter management/caching), `SoundFlowAudioManager` (audio).

## Avalonia conventions

Avalonia 12. Follow Avalonia idioms (not WPF): `.axaml` files with `xmlns="https://github.com/avaloniaui"` and `using:` namespaces; always set `x:DataType`; style with **selectors + pseudo-classes** (`:pointerover`, `:pressed`), never `Style.Triggers` or `Style x:Key`; `StyledProperty`/`DirectProperty`, never `DependencyProperty`; `bool IsVisible`, not `Visibility`; `TreeDataTemplate`, not `HierarchicalDataTemplate`; `avares://` asset URIs; `Dispatcher.UIThread` for UI-thread work; `GridSplitter` for resizable Grid panes.

The repo has the Avalonia MCP servers wired up (`.mcp.json`): **`avalonia-docs`** (Build MCP — `search_avalonia_docs`, `lookup_avalonia_api`, `get_avalonia_expert_rules`, WPF→Avalonia migration) for looking up APIs instead of guessing, and **`avalonia-devtools`** (`attach-to-app` / `attach-to-file`, visual tree, screenshots, live props) for inspecting the running UI. Use them.

## Packaging

Packaging uses **Parcel** (`.parcel` project files: `src/BalatroSeedOracle.parcel`, `src/BalatroSeedOracle/BalatroSeedOracle.parcel`) to produce installers. In CI, Parcel pack supplies the Avalonia license through the `PARCEL_LICENSE` env var (it overrides the committed file via `Directory.Build.props`). The **BSO app version** is centralized in `Directory.Build.props` (`<Version>`, currently `2.0.0`) — this is the desktop product version, independent of the Motely (MotelyJAML) submodule's own version (v19).
