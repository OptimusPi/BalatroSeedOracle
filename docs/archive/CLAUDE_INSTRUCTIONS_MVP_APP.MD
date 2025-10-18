# Balatro Seed Oracle — MVP App Instructions

This document gives crisp, actionable guidance to finish and ship the MVP. It orients agents and developers to the current architecture, the next tasks, and how to validate changes quickly.

## Quick Start
- Build: `dotnet build src/BalatroSeedOracle.csproj -v m`
- Run (Debug): `dotnet run --project src/BalatroSeedOracle.csproj`
- Packaging: `pwsh Publish-Velopack.ps1` (Windows installer)

## Repo Structure
- `src/` — Avalonia app (UI, ViewModels, Components)
- `src/Components/Widgets/` — Shared widget components (BaseWidget, DayLatro, Genie, Audio Visualizer Settings)
- `src/Views/Modals/` — Modal windows (Filters, Tools, Author, Search)
- `src/ViewModels/` — View models using CommunityToolkit.Mvvm (source generators)
- `external/Motely/` — High-performance search engine

## Current State (What changed)
- Filter creation UI now uses `FilterSelectorControl` (new) instead of legacy `FilterSelector` (old).
- Widgets are organized under `src/Components/Widgets/`:
  - `BaseWidget.axaml[.cs]`
  - `DayLatroWidget.axaml[.cs]`
  - `GenieWidget.axaml[.cs]`
  - `AudioVisualizerSettingsWidget.axaml[.cs]`
- Clean build verified.

## Principles
- Keep UI logic in ViewModels; Views should bind and forward events only.
- Prefer `[ObservableProperty]` and `[RelayCommand]` (CommunityToolkit.Mvvm) to cut boilerplate.
- Use shared styles and components (WidgetStyles, BalatroTabControl) to prevent duplication.
- Strive for small, testable units; wire with events and services, not visual tree walking.

## Agent Directions
- When touching UI, patch both axaml and axaml.cs and keep namespaces consistent with `x:Class`.
- For component moves, prefer moving files without changing namespaces to minimize churn.
- After every UI change, build, run locally, and exercise the flow end-to-end.
- Remove verbose, chatty comments; keep only high-signal guidance.

## High-Value Next Steps
1) Filters Modal cleanup (focus only on what’s needed for MVP)
- Swap direct file I/O to ViewModel commands backed by small services.
- Replace `FindControl` chains with bindings and commands.
- Keep existing UX; reduce code-behind surface gradually.

2) Search Flow validation
- Confirm `SearchModalViewModel.StartSearchAsync` loads configs and streams results.
- Validate selection flow (`FilterSelectorControl` events).
- Exercise error paths; ensure UI shows clear status and recovery.

3) Widget polish
- Ensure `BaseWidgetViewModel` drives ZIndex and minimize/expand consistently.
- Use `BaseWidget` container for simple widgets; let complex ones own their layout but reuse styling.

4) Visual consistency
- Apply `WidgetStyles.axaml` consistently.
- Use `BalatroTabControl` for tabs where feasible.

## Testing & Validation
- Unit tests: none required for MVP; favor manual flows.
- Manual checks:
  - Create, edit, copy, delete filters.
  - Import JSON in Filter Creation modal.
  - Run searches and inspect streaming output.
  - Widgets minimize/expand, drag, ZIndex update.
- Logs: watch `AddConsoleMessage` and debug statements for clarity.

## Packaging & Release
- Validate `dotnet publish` then run `Publish-Velopack.ps1`.
- Smoke-test installer launch and core flows.

## Notes
- Legacy `FilterSelector` can be removed once new control is stable across all modals.
- Keep namespace `BalatroSeedOracle.Components` for moved components to avoid breaking `x:Class` references.

## Done Definition (MVP)
- Clean build, app runs, core flows verified manually.
- Filters can be created/edited/selected; search executes and displays results.
- Widgets are organized and functional.
- Packaging script produces installer; smoke test passes.