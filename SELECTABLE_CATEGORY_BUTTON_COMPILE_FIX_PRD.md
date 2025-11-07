# FIX-IT PRD: SelectableCategoryButton Compile Error

## Summary
- **Issue**: `Avalonia error AVLN3000` thrown when building after refactoring `SelectableCategoryButton` template.
- **Impact**: Build blocks `dotnet run` and `dotnet build` workflows, preventing QA and release validation.
- **Owner**: UI toolkit squad (Selectable Navigation Buttons).

## Current Behavior
- Error originates from `src/Controls/Navigation/SelectableCategoryButton.axaml` line 68.
- XAML setter attempts to assign to `Classes`, which is not an `AvaloniaProperty` and therefore unsupported in styles.
- Build halts immediately; UI cannot launch for manual testing.

## Root Cause
- Refactor tried to reuse Balatro triangle bounce style by dynamically adding `balatro-bounce-horizontal` through a Setter.
- `Classes` collection cannot be mutated via `Setter` because it is not dependency property-backed.
- Resulting markup violates Avalonia parse rules, producing AVLN3000.

## Proposed Fix
1. Apply reusable styling through static class assignment in the polygon markup.
2. Keep bounce animation by including `balatro-bounce-horizontal` in the element's `Classes` attribute.
3. Limit style responsibilities to visibility toggling when the button becomes selected.

## Validation Plan
- `dotnet build` from repository root.
- Manual smoke test: navigate to filter modal and confirm selection arrow animates only for active button.
- Regression check: verify special `Favorites` button still overrides fill color on selection.

## Rollout
- Change resides entirely in XAML; no migration steps required.
- Communicate fix in changelog entry for UI refactor batch once merged.
