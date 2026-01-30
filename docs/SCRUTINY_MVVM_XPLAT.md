# Scrutiny: Recent Work & MVVM / XPLAT Organization

**Date:** 2026-01-29  
**Scope:** Avalonia UI MVVM and cross-platform (XPLAT) organization in BalatroSeedOracle.

---

## 0. Stack: AOT, WASM SIMD, DuckDB-WASM (do not reinvent)

**Required and already in use.** All guidance in this repo must preserve and build on this stack. 

| Need | Where it lives | Official / ecosystem |
|------|-----------------|------------------------|
| **AOT** | Desktop: `PublishAot` in `BalatroSeedOracle.Desktop.csproj`. Browser: `RunAOTCompilation` + `PublishTrimmed` in `BalatroSeedOracle.Browser.csproj`. | [Avalonia Web Assembly](https://docs.avaloniaui.net/docs/guides/platforms/how-to-use-web-assembly), [.NET trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options). |
| **WASM SIMD** | `WasmEnableSIMD=true` in `BalatroSeedOracle.Browser.csproj`. | .NET WASM SDK; SIMD supported in modern browsers. |
| **WASM DuckDB** | `wwwroot/js/duckdb-interop.js` + `duckdb-wasm/` (duckdb-eh.wasm, duckdb-browser-eh.worker.js, duckdb.mjs). Browser uses JS interop; Desktop/Android use `DuckDB.NET.Data.Full`. | [JSImport/JSExport](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop) for Avalonia Browser; DuckDB-WASM bundle you already ship. |

- **Do not assume** AOT, WASM SIMD, or DuckDB-WASM don’t work or need to be replaced.
- **Do not reinvent:** Use the existing Browser/Desktop csproj settings, `TrimmerRoots.xml`, and DuckDB-WASM interop. Any MVVM/DI or architecture changes must keep this stack intact and AOT/trim-safe.
- **Docs to read:** [Avalonia – Web Assembly](https://docs.avaloniaui.net/docs/guides/platforms/how-to-use-web-assembly), [Avalonia – Building cross-platform applications](https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications), [.NET WASM deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

---

## 1. Recent Work Summary

### What’s in good shape

- **Platform abstraction:** `IPlatformServices` is used for capabilities (file system, audio, analyzer, results grid). No `#if BROWSER` / `#if !BROWSER` in shared code (only one `#if BROWSER` in Browser-specific `LocalStorageTester.cs`, which is acceptable).
- **Platform head projects:** Desktop/Browser/Android/iOS are used as “folders” — platform-only code lives in platform projects; shared code doesn’t compile platform-only types. Desktop-only widgets (ApiHost, AudioMixer, MusicMixer, etc.) live under `BalatroSeedOracle.Desktop/Components/Widgets/`; shared widgets (BaseWidget, DayLatro, Genie, Search) stay in `BalatroSeedOracle/Components/Widgets/`.
- **DI registration:** Core and platform services are registered in `ServiceCollectionExtensions` and platform `Program.cs`; `PlatformServices.RegisterServices` is used for Desktop/Browser-specific implementations.
- **Main shell MVVM:** `MainWindow` and `BalatroMainMenu` get their ViewModels via **constructor injection**; the composition root (App/DI) resolves and passes them. This matches the documented pattern.
- **Docs:** `docs/ARCHITECTURE.md` and `docs/AVALONIA_BEST_PRACTICES.md` describe MVVM, no service locator in constructors, and XPLAT patterns.

### What’s inconsistent with the stated rules

- **Service locator in constructors:** The architecture says: *“Never use ServiceHelper.GetService or GetRequiredService inside a View or ViewModel constructor.”* In practice, many Views and ViewModels still resolve services in their constructors, e.g.:
  - **Views:** `SearchModal`, `FiltersModal`, `ToolsModal`, `CreditsModal`, `AnalyzeModal`, `GenieWidget`, `DayLatroWidget`, `FilterSelector`, `DeckAndStakeSelector`, etc. use `ServiceHelper.GetService` / `GetRequiredService` or `App.GetService` in the parameterless constructor.
  - **ViewModels:** `SearchWidgetViewModel`, `FiltersModalViewModel`, `FilterListViewModel`, `PaginatedFilterBrowserViewModel`, `BaseWidgetViewModel`, and others use `ServiceHelper` in methods; some modal ViewModels are created manually with `new ViewModel(...)` after pulling deps via ServiceHelper in the View constructor.
- **Modals created ad hoc:** `BalatroMainMenu` often does `new FilterSelectionModal()` and `new FilterSelectionModalViewModel(...)` (or similar) instead of resolving modal View + ViewModel from DI. That bypasses the “creator resolves from DI and passes in” rule and makes testing and lifecycle harder.
- **Two resolution paths:** Both `ServiceHelper.GetService<T>()` (which delegates to `App.GetService<T>()`) and direct `App.GetService<T>()` are used. Prefer one convention (e.g. `ServiceHelper` only in shared code) for consistency.
- **Heavy code-behind:** `BalatroMainMenu.axaml.cs` is very large and owns modal flow, widget restoration, and UI helpers. A lot of this could live in ViewModels or dedicated services (e.g. “modal host”, “widget restore”) to keep Views thin and improve testability.

---

## 2. Research: Avalonia MVVM + XPLAT Best Practices

### Official Avalonia guidance

- **Architecture:** Encapsulation, separation of responsibilities, polymorphism (program to interfaces). Layers: Data, Data Access, Business, Service Access, Application (platform-specific), UI (Views + ViewModels). With Avalonia, the **UI layer can be shared** across platforms.
- **MVVM:** View = structure/presentation; ViewModel = logic and state; separation from business services via **Dependency Injection**. ViewModels should be testable without the UI.
- **Cross-platform solution:** One **core/shared project** (business logic, ViewModels, shared Views) referenced by **platform head projects** (Desktop, Android, iOS, Browser). Platform projects contain entry points, platform service registration, and platform-only UI/features.

### Alignment with this repo

- **Core project:** `BalatroSeedOracle` acts as the shared core (ViewModels, shared Views, services, `IPlatformServices`). Good.
- **Platform projects:** Desktop/Browser/Android/iOS only add entry points and platform implementations. Good.
- **Gap:** The **composition root** (who creates Views/ViewModels and wires them) is only applied consistently for the main window and main menu. Modals and many controls still create their own ViewModels via service locator in the constructor instead of being created by a single place that uses DI.

---

## 3. Recommended MVVM + XPLAT Organization

### 3.1 Dependency injection (strict)

- **ViewModels:** All dependencies come via **constructor injection**. No `ServiceHelper` or `App.GetService` in ViewModel constructors.
- **Views:** ViewModels (and any View-specific services) are **injected by the creator** (e.g. composition root, parent ViewModel, or a factory that uses DI). Views do not call `ServiceHelper`/`App.GetService` in their constructors to build their own ViewModel.
- **Optional:** Use a single `IModalViewFactory` (or similar) that resolves modal View + ViewModel from DI so `BalatroMainMenu` (or a dedicated host ViewModel) just asks “show Search modal” and the factory returns the pair. Same idea for widget creation if needed.

### 3.2 Where to resolve

- **Composition root (App / platform Program):** Resolves `MainWindow`, `BalatroMainMenu`, and their ViewModels. Already done.
- **Modals:** Prefer resolving modal View + ViewModel from DI (e.g. `GetRequiredService<SearchModal>()` where `SearchModal` is registered as a factory: `services.AddTransient<SearchModal>(sp => new SearchModal(sp.GetRequiredService<SearchModalViewModel>()))`). Then `BalatroMainMenu` (or a service) calls the factory and sets `DataContext`/content. No `new SearchModal()` + ServiceHelper in the View.
- **Widgets / controls:** Same idea: whoever creates the widget (e.g. WidgetPicker, main menu) gets the widget View + ViewModel from DI (or a small factory that uses DI), then assigns `DataContext`. No ServiceHelper inside widget View constructors.

### 3.3 Platform boundaries (keep current approach)

- **Shared project:** All code that can run on every target. Only references platform-agnostic APIs and `IPlatformServices` (and other abstractions). No `#if` for platform.
- **Platform projects:** Entry point + `PlatformServices.RegisterServices` + platform-only implementations and **platform-only Views/ViewModels** (e.g. Desktop widgets). The “folder” rule: if it’s in `BalatroSeedOracle.Desktop`, it’s not compiled for Browser and vice versa. No need for extra “provider” interfaces when the only consumer is one platform.

### 3.4 Thin Views

- **Views:** Minimal code-behind. Bindings and commands in XAML; View forwards input to ViewModel. No business logic; no pulling multiple services in constructor.
- **Modal / navigation flow:** Prefer a ViewModel or application service that “requests” a modal (e.g. “Show Search”) and gets a View+ViewModel from DI/factory; the View only displays what it’s given. This will require refactoring the current `BalatroMainMenu` modal logic into a host ViewModel or a dedicated service that uses DI.

---

## 4. Actionable Next Steps (priority order)

1. **Stop new service locator in constructors:** For any new View or ViewModel, use constructor injection only. Document this in ARCHITECTURE and in a short “MVVM checklist” in the repo.
2. **Centralize modal creation:** Introduce a small “modal factory” or “modal host” that resolves modal View + ViewModel from DI and returns them (or shows them). Refactor `BalatroMainMenu` to use it for Search, Filters, Tools, Settings, etc., so that `new FilterSelectionModal()` + manual ViewModel construction disappear.
3. **Migrate one modal end-to-end:** Pick one modal (e.g. Search or Credits), register it and its ViewModel in DI, and have the host resolve and show it. Use that as the pattern for the rest.
4. **Gradually remove ServiceHelper from View/ViewModel constructors:** Replace with constructor-injected dependencies and creation by the composition root or factory. Use `ServiceHelper` only for one-off resolution outside constructors where injecting a factory is disproportionate (and note that in comments).
5. **Trim code-behind:** Move modal flow and “who shows what” into a ViewModel or application service; keep `BalatroMainMenu.axaml.cs` to layout, animation, and View-only event wiring.

---

## 5. References (read the docs)

- [Avalonia – Web Assembly](https://docs.avaloniaui.net/docs/guides/platforms/how-to-use-web-assembly) – deploy, JS interop, troubleshooting
- [Avalonia – Architecture](https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications/architecture)
- [Avalonia – The MVVM Pattern](https://docs.avaloniaui.net/docs/concepts/the-mvvm-pattern/)
- [Avalonia – Building Cross-Platform Applications](https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications)
- [.NET Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options) – AOT/trim-safe patterns
- In-repo: `docs/ARCHITECTURE.md`, `docs/AVALONIA_BEST_PRACTICES.md`, `src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj`, `src/BalatroSeedOracle.Browser/wwwroot/js/duckdb-interop.js`

---

## 6. Status (scrutiny progress)

**In progress.** Summary as of last pass:

| Step | Status | Notes |
|------|--------|------|
| 1. Stop new service locator in constructors; document | Partial | ARCHITECTURE has MVVM checklist. Policy in place; existing code still has violations in methods (not constructors). |
| 2. Centralize modal creation | Partial | Search, Credits, Filters: creator passes ViewModel from menu. Tools, Analyze, FilterSelection: creator now passes ViewModel (and services where needed). |
| 3. Migrate one modal end-to-end | Done for Search, Credits, Filters, Tools, Analyze, FilterSelection | Pattern: creator gets VM (and services) from DI or `new ViewModel(...)`, `new Modal(vm)` or `new Modal(vm, ...)`. FilterSelectionModal supports both DataTemplate (parameterless ctor) and code-behind (constructor injection). |
| 4. Gradually remove ServiceHelper from View/ViewModel constructors | Done for target Views | MainWindow: NotificationService injected. FilterSelectionModal: ViewModel + services injected when created by creator; parameterless ctor for XAML DataTemplate only. ToolsModal, AnalyzeModal: constructor injection. BalatroMainMenu: no ServiceHelper in constructor. |
| 5. Trim code-behind | Not done | `BalatroMainMenu.axaml.cs` still large; modal flow still in View. Optional follow-up. |

**Remaining (optional):** Trim BalatroMainMenu code-behind; reduce ServiceHelper usage in View/ViewModel methods “creator passes ViewModel”; remove ServiceHelper from remaining View constructors; optionally trim BalatroMainMenu code-behind.
