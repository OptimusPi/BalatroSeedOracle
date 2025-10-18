# Documentation Cleanup Plan

This index inventories all Markdown docs and recommends actions to reduce clutter while keeping high-value references easy to find.

## Keep (Core docs)
- README.md — project overview and entry point
- AVALONIA_MVVM_2025_GUIDE.md — core architecture and MVVM conventions
- WIDGET_STYLE_GUIDE.md — UI consistency and widget patterns
- BALATRO_ANIMATION_GUIDE.md — animation guidance
- .copilot/PROJECT_INDEX.md — AI tooling index (leave in `.copilot/`)

## Active Work Plans (polish & keep)
- FILTERS_MODAL_REFACTOR_PLAN.md — active refactor plan
- FILTER_MODAL_REFACTORING_PLAN.md — related refactor notes
- DECK_STAKE_SELECTOR_FIX.md — in-progress selector fixes
- PAGINATION_FIX.md — fixes for results paging
- SHADER_ANIMATION_FIX.md — shader animation tasks
- LIVE_CHAT_TODO.md — feature todos
- COMPREHENSIVE_TODO_LIST.md — master task list (consider merging into issue tracker)

## Archive (move to `docs/archive/`)
- MVVM_REFACTORING_ANALYSIS.md — analysis concluded or superseded by guide
- TECHNICAL_DEBT_ANALYSIS.md — evergreen reference; archive to reduce root noise
- MUST_ARRAY_EARLY_EXIT_ANALYSIS.md — historical analysis
- MUST_EARLY_EXIT_BUG_REPORT.md — historical bug report
- JOKER_FILTER_EARLY_EXIT_FIX.md — historical fix notes
- VECTORIZED_EARLY_EXIT_TECHNICAL_ANALYSIS.md — technical exploration (keep as reference)
- HONEST_STATUS.md — status log, likely obsolete
- CLAUDE_INSTRUCTIONS_MVP_APP.MD — legacy instruction set

## Delete Candidates (after grace period)
- None immediate. After archiving, if a doc sees no updates or references for 30 days, consider deletion.

## Next Actions
1. Create `docs/archive/` and move the Archive items above.
2. Update `README.md` with a short Docs section linking to this index.
3. Merge “COMPREHENSIVE_TODO_LIST.md” items into your issue tracker and then archive the file.

## Notes
- Archiving first is safer than immediate deletion; it preserves history and avoids breaking links.
- If any archived doc remains actively used, promote it back to Keep.