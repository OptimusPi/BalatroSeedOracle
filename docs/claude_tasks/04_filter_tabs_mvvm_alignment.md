# Task: Align Filter Tab ViewModels with Toolkit Patterns

## Current state
`SaveFilterTabViewModel` and `JsonEditorTabViewModel` (under `src/ViewModels/FilterTabs/`) both inherit from the legacy `BaseViewModel` and rely on manual `SetProperty` plus explicitly constructed commands. They’re the last holdouts from the pre-refactor era.

## Scope
- Files: `SaveFilterTabViewModel.cs`, `JsonEditorTabViewModel.cs`
- Change each class to `partial` and derive from `ObservableObject`.
- Replace property backing fields with `[ObservableProperty]` attributes.
- Use `[RelayCommand]` / `[AsyncRelayCommand]` to expose the same command names currently bound in XAML (`SaveCommand`, `SaveAsCommand`, `ExportCommand`, `TestFilterCommand`, etc.). Preserve async semantics.
- Ensure any `CanExecute` logic still works (Toolkit supports `CanExecute` via method naming or `[NotifyCanExecuteChangedFor]`).

## Out of scope
- Reworking business logic (export paths, JSON generation, etc.).
- Changing constructor injection patterns.

## Acceptance tests
1. `dotnet build src/BalatroSeedOracle.csproj` passes without new warnings.
2. Filter modal save, save-as, export, validation commands continue to operate in the UI.
3. Toolkit analyzers show no complaints about missing partial modifiers or invalid attributes.
