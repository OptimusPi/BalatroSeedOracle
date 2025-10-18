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
- ui: Deck/Stake selector is too large globally.
  - Description: Deck and stake selectors feel oversized in Analyzer and modal.
  - Screenshot: user will send; logging context first.
  - Status: todo

## Working / Next Up
- Empty — I’ll move items here as we start them.

## Completed Today
- Initialized live chat TODO tracker.
- Added template and usage notes.

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