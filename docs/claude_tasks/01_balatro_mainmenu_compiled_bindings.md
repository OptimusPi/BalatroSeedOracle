# Task: Restore Compiled Bindings for `BalatroMainMenu`

## Why this matters
`BalatroMainMenu.axaml` explicitly disables compiled bindings (`x:CompileBindings="False"`). That breaks the repo guideline of using compiled bindings to catch typos at build time. Losing compile-time checks is already hiding binding mistakes in this view.

## Scope
- File: `src/Views/BalatroMainMenu.axaml`
- Re-enable `x:CompileBindings="True"` (or delete the override).
- Fix every binding error that surfaces after re-enabling (names, nullability, etc.).
- Do **not** refactor unrelated UI logic.

## Constraints
- Keep the `BalatroMainMenuViewModel` API unchanged (no renaming public properties unless the view already uses the wrong name).
- Zero code-behind changes unless a binding truly cannot be expressed in XAML.
- No new features—just bring the view back into compliance.

## Acceptance tests
1. `dotnet build src/BalatroSeedOracle.csproj` passes with no binding compilation errors or warnings for `BalatroMainMenu`.
2. Launching the app shows `BalatroMainMenu` behaving exactly as before (volume slider, modals, widgets, etc.).
3. Spot-check the build output to confirm Avalonia compiled bindings are enabled for this view.
