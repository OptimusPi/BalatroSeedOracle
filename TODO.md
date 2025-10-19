# TODO - BalatroSeedOracle Cleanup & Improvements

## 🚨 CRITICAL ISSUES (Fix TODAY)

### 1. Debug Logging Pollution
- [ ] Remove or wrap 674 DebugLogger calls across 62 files
- [ ] Use #if DEBUG preprocessor directives for development-only logging
- [ ] Remove fresh debug statements from:
  - `DeckAndStakeSelectorViewModel.cs`
  - `SearchModal.axaml.cs`
  - `VisualBuilderTab.axaml.cs`

### 2. Working Directory Cleanup
- [ ] Commit pending changes or revert them
- [ ] Delete mysterious `nul` file
- [ ] Stage and commit `CardDragBehavior.cs`
- [ ] Fix CRLF/LF line ending issues in .gitattributes

### 3. Async Loading Fix
- [ ] Remove Task.Delay(50) hack in VisualBuilderTabViewModel
- [ ] Implement proper async initialization pattern
- [ ] Consider preloading sprites at app startup

## 🔥 HIGH PRIORITY (This Week)

### 4. Resolve TODO Comments
- [ ] `GenieWidgetViewModel.cs:216` - Implement search modal/widget progress tracking
- [ ] `FiltersModalViewModel.cs:484` - Load favorites from FavoritesService
- [ ] `ComprehensiveFiltersModalViewModel.cs:373` - Load from BalatroData when available
- [ ] `AudioVisualizerSettingsModalViewModel.cs:693` - Add error message UI

### 5. Performance Optimizations
- [ ] Fix CardDragBehavior 60 FPS timer - only run when dragging
- [ ] Add proper disposal pattern for timers
- [ ] Review and fix event handler memory leaks

### 6. Documentation Cleanup
- [ ] Move CLICK_AWAY_FIX_TEST.md to /docs or delete
- [ ] Delete CLICK_AWAY_FIX_SUMMARY.md
- [ ] Create proper /docs folder structure

## 📝 MEDIUM PRIORITY (This Sprint)

### 7. Code Quality
- [ ] Remove all commented-out code
- [ ] Remove unused imports
- [ ] Delete dead methods
- [ ] Implement proper logging framework (Serilog?)

### 8. Or/And Clause Antes Fix
- [ ] Test the Motely changes thoroughly
- [ ] Ensure Or clauses respect their antes field
- [ ] Add unit tests for complex filter scenarios

### 9. UI/UX Polish
- [ ] SELECT THIS DECK button should reliably advance tabs
- [ ] CREATE NEW FILTER button shouldn't freeze UI
- [ ] Drag ghost physics should be smooth

## ✅ COMPLETED TODAY
- [x] Fixed click-away handler (main modals vs popups)
- [x] Added Balatro-style sway physics to drag ghost
- [x] Started async loading for FiltersModal sprites
- [x] Fixed Or/And clauses antes support in Motely
- [x] Added debug logging for SELECT THIS DECK (needs removal!)

## 📊 Technical Debt Metrics
- **Debug Statements**: 674 across 62 files
- **TODO Comments**: 4 unresolved
- **Uncommitted Files**: 5
- **Test Docs in Root**: 2 files, 200+ lines

## 🎯 Success Criteria
- Zero debug statements in production code
- Clean git status
- All TODOs resolved or ticketed
- Performance metrics: <100ms modal open time
- Code coverage: >80% for critical paths

---
*Last Updated: 2024-10-19*
*Next Review: End of Week*