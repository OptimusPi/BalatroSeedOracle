# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Balatro Seed Oracle (BSO) is an **Avalonia UI** desktop/browser app for searching Balatro game seeds. The actual seed-search engine is **Motely (MotelyJAML)**, vendored as a git submodule and consumed by BSO. BSO is the UI + orchestration; Motely is the vectorized (SIMD) search core. Filters are authored in **JAML** (Jimbo's Ante Markup Language) or JSON.

## Prerequisites (builds fail without these)

- **.NET 10 SDK**, pinned to `10.0.203` in `global.json` (`rollForward: latestPatch`). C# 14.
- **Submodule must be initialized** or the solution won't build: `git submodule update --init --recursive`. The Motely engine lives at `src/MotelyJAML/` (the `.sln` references `src/MotelyJAML/Motely/Motely.csproj`).
- **Avalonia license key.** `Directory.Build.props` reads it from the `avalonia.license` file at repo root, or the `PARCEL_LICENSE` env var. See `avalonia.license.local.example`.

## Common commands

Run the desktop app (this is the runnable head — running processes are `BalatroSeedOracle.Desktop`):

```sh
dotnet run -c Release --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj   # fast search
dotnet run -c Debug   --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj   # debug; searches MUCH slower
```
(The README also shows `dotnet run --project ./src/BalatroSeedOracle.csproj`; prefer the `.Desktop` head above.)

Build / verify compile (use this to check your changes — see "never run Motely" below):

```sh
dotnet build BalatroSeedOracle.sln
```

Tests — the only test project is Motely's:

```sh
dotnet test src/MotelyJAML/Motely.Tests/Motely.Tests.csproj
dotnet test src/MotelyJAML/Motely.Tests/Motely.Tests.csproj --filter "FullyQualifiedName~SomeTestName"   # single test
```

Browser (WASM, AOT) publish:

```sh
dotnet publish -c Release src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
```

## Hard rules (these break things or violate repo conventions)

- **NEVER run Motely** (`.cursor/rules/no-run-motely.mdc`). No `dotnet run` on `Motely.CLI`, `Motely.TUI`, Motely WASM, or any Motely process — running a search burns huge time/tokens. You may **build** the solution to check compile errors, and read/edit/analyze Motely freely. Do not "run a search to verify."
- **`TreatWarningsAsErrors=true`** (`Directory.Build.props`) with `Nullable=enable` and `EnforceCodeStyleInBuild`. A warning fails the build. Code must be warning-clean, including nullability.
- **AOT is mandatory; bindings are compiled by default** (`AvaloniaUseCompiledBindingsByDefault=true`). Desktop uses Native AOT, Browser uses `TrimMode=full`. Reflection-based bindings or reflection-based (de)serialization will crash at runtime. Use `System.Text.Json` source-gen contexts and `[YamlStaticContext]`, generic `Enum.GetValues<T>()`, and static property-setter maps — never `PropertyInfo.SetValue`/`Enum.GetValues(typeof(...))`. See `docs/ARCHITECTURE.md` "AOT Compilation".
- **Logging goes through `DebugLogger` only** (`src/BalatroSeedOracle/Helpers/DebugLogger.cs`): `DebugLogger.Log/LogError/LogImportant(component, message)`. Never `Console.WriteLine` for debug — reserve it for always-visible critical failures only.

## Architecture (the parts that span multiple files)

**Project layout** — one shared core, multiple platform heads (`src/`):
- `BalatroSeedOracle/` — the **core**: all shared Views, ViewModels, Services, Models, Converters, Controls, Components. Platform-agnostic; no `#if`.
- `BalatroSeedOracle.Desktop` / `.Browser` / `.Android` / `.iOS` — **platform heads**. Each contains only its entry point (`Program.cs` / `MainActivity.cs` / `AppDelegate.cs`), `PlatformServices.RegisterServices`, and platform-only overrides.
- `src/MotelyJAML/` — the Motely submodule: `Motely` (engine), `Motely.Tests`, `Motely.TUI`.

**The "folder thing" (cross-platform pattern)** — Do **not** use `#if BROWSER` / `#if !BROWSER`. Platform-specific behavior is achieved two ways: (1) the `IPlatformServices` abstraction + DI for capabilities (`SupportsFileSystem`, `SupportsAudio`, `SupportsAnalyzer`...), and (2) putting platform-only code physically in that platform's head project (code in a head isn't compiled for other platforms). Don't invent shared interfaces (e.g. "IDesktopWidgetProvider") when option (2) already isolates the code. Swappable impls: `FileSystemAppDataStore` vs `BrowserLocalStorageAppDataStore`; `FileSystemDuckDBService` vs `BrowserDuckDBService`; `ClosedXmlExcelExporter` vs `BrowserExcelExporter` (stub). Shared platform impls live in `src/BalatroSeedOracle/Services/Platforms/`.

**MVVM + DI** — ViewModels inherit `ObservableObject` (CommunityToolkit.Mvvm), use `[ObservableProperty]` and `[RelayCommand]`. Views are thin (minimal code-behind, `{Binding}`, `x:Name` field access not `FindControl`). Services are registered in `App.axaml.cs` via `ServiceCollectionExtensions` plus per-platform `PlatformServices.RegisterServices`. **All dependencies via constructor injection.** Do **not** call `ServiceHelper.GetService`/`GetRequiredService` inside any View or ViewModel **constructor** — the creator resolves from DI and passes in. `ServiceHelper.GetService<T>()` is only for one-off resolution *outside* constructors. Modals/dialogs follow the same rule: resolve View+ViewModel from DI, don't `new ModalView()` then service-locate inside it. (Migration status: `docs/SCRUTINY_MVVM_XPLAT.md`.)

**Search data flow** — UI (`Views/SearchModalTabs/`, search widgets) → `SearchManager` / `ActiveSearchContext` (`Services/`) → a `JamlSearchBuilder` that translates the editor's draft config into Motely's search. Filters authored as JAML/JSON (`must` / `should` / `mustNot` clauses, plus `deck`, `stake`, scoring `mode` = `sum`|`max`); see README "Creating Filters". Results are written to **DuckDB** (`DuckDB.NET.Data.Full` on desktop, DuckDB-WASM in browser) and surfaced via the results grid / `ResultsExportService`. Note the historically churny area: the port from the old `MotelyJsonConfig` editor model to `JamlSearchBuilder`/`JamlRootDocument`/`JamlClauseUnion` — when touching search wiring, check which model a call site uses.

**Other key services** — `SpriteService` (game asset sprites), `UserProfileService` (settings/prefs, `userprofile.json`), `FilterCacheService` (filter management/caching), `SoundFlowAudioManager` (audio).

## Packaging / release

Packaging uses **Parcel** (`.parcel` project files exist: `src/BalatroSeedOracle.parcel`, `src/BalatroSeedOracle/BalatroSeedOracle.parcel`) to produce installers (NSIS/zip on Windows). `PARCEL_LICENSE` supplies the Avalonia license at pack time. Version is centralized in `Directory.Build.props` (`<Version>`).

## Reference docs

- `docs/ARCHITECTURE.md` — platform abstraction, MVVM/DI rules, AOT patterns (authoritative).
- `AI_CODING_GUIDELINES.md` — logging rules, MVVM/Avalonia conventions.
- `BALATRO_UI_STYLE_GUIDE.md` — visual style.
- `docs/SCRUTINY_MVVM_XPLAT.md` — current MVVM/cross-platform gaps and migration steps.
- `docs/INDEX.md` — documentation index.
