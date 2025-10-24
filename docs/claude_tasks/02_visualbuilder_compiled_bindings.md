# Task: Add Compiled Binding Support to `VisualBuilderTab`

## Background
`src/Components/FilterTabs/VisualBuilderTab.axaml` does not declare an `x:DataType` or enable compiled bindings. The heavy drag/drop UI depends on `VisualBuilderTabViewModel`, and any property rename will silently break the tab. We need compile-time safety without touching the business logic already stuck in code-behind.

## Scope
- File: `src/Components/FilterTabs/VisualBuilderTab.axaml`
- Declare `x:DataType="vm:VisualBuilderTabViewModel"` and set `x:CompileBindings="True"`.
- Adjust binding expressions to match the actual viewmodel property names (e.g., command properties, collections, booleans).
- Ensure necessary XML namespace imports exist after the change.

## Out of scope
- Moving drag/drop logic out of the code-behind.
- Styling or layout changes not required by the binding fixes.

## Acceptance tests
1. `dotnet build src/BalatroSeedOracle.csproj` succeeds with no binding compile errors for `VisualBuilderTab`.
2. Drag/drop, category switching, and the search box still function the same in the running app.
3. No new toolkit warnings are emitted at runtime when opening the Filters modal.
