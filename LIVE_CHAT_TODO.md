# Live Chat TODO Tracker

Created: 2025-10-17

Purpose: Lightweight, running list I update as you chat. Send quick notes and screenshots; I’ll capture and categorize them here.

## How We Use This
- You drop short messages or bullets; I log them immediately.
- I tag items by feature (e.g., `analyzer`, `filters`, `ui`, `json`, `pagination`).
- Statuses: `todo` → `in-progress` → `done`.
- I’ll keep this file tidy and summarize progress at milestones.

## Quick Add Template
- Tag: <feature>
- Title: <short name>
- Description: <one-liner>
- Screenshot: <reference/description>
- Status: todo

---

## Inbox (New Items)
- Empty — I'll log new items here as you send them.

## Working / Next Up
- Empty — I'll move items here as we start them.

## Completed Today (2025-10-18)

### Session 1: MD File Cleanup & UI Fixes
- ui: Fixed Deck/Stake selector sizing globally.
  - Reduced DeckSpinner: Height 196→148, Width 56→44, FontSize 32→28, Panel Width 360→300
  - Reduced StakeSpinner: Height 120→90, Width 56→44, FontSize 28→24, Panel Width 360→300
  - Reduced SpinnerControl: Button Height 36→32, Badge Height 40→36
  - Status: done

### Session 2: Bug Fixes & Features
- ui: Fixed CREATE NEW FILTER button in SearchModal filter paginator
  - Added event subscription in SearchModal.axaml.cs
  - Added OpenFiltersModal() method to navigate up visual tree
  - Status: done

- json: Fixed default score logic for filter items
  - Items without explicit scores now default to antes count (min 1) instead of 0
  - Example: {"Type": "TarotCard", "value": "Any"} with 6 antes = 6 points
  - Status: done

- json: Implemented Max mode and Or clause support
  - Added nested clauses serialization in FilterSerializationService
  - Max mode uses MaxCount aggregation (highest occurrence vs sum of scores)
  - Or clauses allow grouping multiple conditions where any one can match
  - Created comprehensive MAX_MODE_GUIDE.md documentation
  - Status: done

- docs: Cleaned up completed MD files
  - Deleted SHADER_ANIMATION_FIX.md (completed)
  - Deleted PAGINATION_FIX.md (completed)
  - Deleted FILTER_MODAL_REFACTORING_PLAN.md (duplicate)
  - Deleted dead AuthorModal.axaml.cs code
  - Status: done

### UI: Deck/Stake Selector – Context & Pointers
- Components: `src/Components/DeckAndStakeSelector.axaml`, `src/Components/DeckSpinner.axaml`.
- Styles driving size:
  - `DeckSpinner.axaml`: `Button#PrevButton`/`NextButton` Height `196`, Width `56`, FontSize `32`.
  - `controls|PanelSpinner Viewbox` MaxHeight `196`.
  - `controls|PanelSpinner Border.panel-content` Width `360`.
  - `SpinnerControl.axaml`: value badge Height `40`, button Height `36`.
- Likely fix direction:
  - Reduce arrow button Height to ~`148` and Width to ~`44`.
  - Shrink `Viewbox` MaxHeight to ~`148`; panel content Width to ~`300`.
  - Decrease stake spinner button Height to ~`30–32`; value badge Height ~`32`.
  - Centralize these in a style resource (e.g., `WidgetStyles.axaml`) so changes apply everywhere.
  - Optional: expose a `SizeVariant` on `DeckSpinner`/`SpinnerControl` (`compact`, `normal`).


## Analyzer Observations (Seed Analyzer)
- Placeholder: “Analyzer works partially” — details to be filled from your notes.

## Filters Designer Observations
- Placeholder for any filter-builder issues or wishes.

## General Notes
- I’ll keep sections minimal and focused so we can move fast.
- If a topic grows, I’ll split it into its own file and link it here.