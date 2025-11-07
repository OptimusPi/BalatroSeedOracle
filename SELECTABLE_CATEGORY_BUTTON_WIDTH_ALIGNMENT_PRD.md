# FIX-IT PRD: Filter Category Button Width Alignment

## Summary
- **Issue**: Filter category buttons became narrower than the search bar after embedding the arrow indicator, breaking Balatro visual alignment.
- **Impact**: Sidebar UI looks uneven; players misinterpret hit area and feel polish regressions.
- **Owner**: UI toolkit squad (Selectable Navigation Buttons).

## Current Behavior
- `TextBox` search field constrained to 160px max width.
- `SelectableCategoryButton` template reserves an internal arrow column, causing overall width to shrink relative to the search field.
- Visual delta visible in latest screenshot (11/07/2025) where button borders tuck inside the search bar edges.

## Root Cause
- Width coordination relied on `MaxWidth` rather than explicit sizing.
- Arrow column consumed part of the total width, reducing the clickable area while search box maintained full 160px span.
- No shared constant to synchronize sizing among the three critical elements (title badge, search bar, button stack).

## Proposed Fix
1. Introduce a `FilterNavControlWidth` resource scoped to `FilterCategoryNav`.
2. Apply the resource to the header badge, search box, stack panel, and each `SelectableCategoryButton` instance.
3. Retain stretch alignment so future style tweaks can raise the shared constant without hunting individual values.

## Validation Plan
- Launch filter modal and confirm sidebar elements line up flush.
- Resize window to ensure width binding does not break layout or introduce clipping.
- Confirm selection arrow animation still positions correctly relative to button edge.

## Rollout
- Pure XAML adjustments; no migrations needed.
- Flagged for inclusion in next UI polish changelog.
