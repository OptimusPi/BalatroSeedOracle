# Task: Replace Hard-Coded Colors with Shared Resources

## Why
Numerous XAML files still embed literal hex colors (`#2a2a2a`, `#FFD700`, etc.) instead of referencing brushes from `App.axaml` or a shared palette. This violates the project’s styling convention and makes theme adjustments painful.

## Scope
- Identify all hard-coded colors in `src/**/*.axaml` (start with the offenders in `Views/Modals/VisualizerWorkspace.axaml`, `Views/Modals/AudioVisualizerSettingsModal.axaml`, `Views/BalatroMainMenu.axaml`, and `Components/FilterTabs/*.axaml`).
- For each unique color, map it to an existing resource key in `App.axaml` or add a new key if it represents a legitimate palette value.
- Update the XAML to reference the resource via `{StaticResource ...}`.

## Constraints
- Stay within existing color semantics; do not alter the visual design unless the resource name makes that explicit.
- If you must add new brushes, document them in `App.axaml` with clear comments.

## Acceptance tests
1. No raw hex colors remain in application XAML other than resource dictionary definitions.
2. `dotnet build src/BalatroSeedOracle.csproj` still passes.
3. UI looks unchanged during manual spot-checks of the updated views.
