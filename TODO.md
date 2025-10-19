# TODO - BalatroSeedOracle Cleanup & Improvements

## ✅ COMPLETED (Session 10/19/2024)

### Performance Optimizations
- [x] CardDragBehavior: Timer only runs when hovering/dragging (not constant 60 FPS)
- [x] Fixed async loading in VisualBuilderTabViewModel (removed Task.Delay hack)
- [x] Added proper loading state tracking with IsLoading property

### Code Quality Fixes
- [x] Removed debug logging from DeckAndStakeSelectorViewModel
- [x] Removed debug logging from SearchModal.axaml.cs
- [x] Fixed line endings in .gitattributes (CRLF for Windows)
- [x] Fixed unused variable warning in catch block

### TODO Comments Resolved
- [x] FiltersModalViewModel:484 - Now loads from FavoritesService
- [x] AudioVisualizerSettingsModalViewModel:693 - Added ErrorMessage property
- [x] ComprehensiveFiltersModalViewModel:373 - Loads from BalatroData
- [x] GenieWidgetViewModel:216 - Reworded as future enhancement

### Music Visualizer Verified Working
- [x] AudioIntensity defaults to 1.0 (reactive)
- [x] TimeSpeed defaults to 1.0 (animating)
- [x] BalatroShaderBackground connected to VibeAudioManager

### Bug Fixes
- [x] Fixed click-away handler (main modals vs popups)
- [x] Fixed Or/And clauses antes support in Motely
- [x] Added Balatro-style sway physics to drag ghost

## 🚨 CRITICAL ISSUES (Fix TODAY)

### 1. Debug Logging Pollution (REMAINING)
- [ ] Remove or wrap remaining ~670 DebugLogger calls across 60+ files
- [ ] Use #if DEBUG preprocessor directives for development-only logging
- [ ] Consider implementing proper logging framework (Serilog?)

## 🔥 HIGH PRIORITY (This Week)

### Remaining Documentation Tasks
- [ ] Create proper /docs folder structure
- [ ] Move technical documentation to /docs
- [ ] Update README with current features

## 📝 MEDIUM PRIORITY (This Sprint)

### Code Quality
- [ ] Remove all commented-out code
- [ ] Remove unused imports
- [ ] Implement proper logging framework (Serilog?)

### Testing
- [ ] Add unit tests for complex filter scenarios
- [ ] Test Or/And clause antes thoroughly
- [ ] Performance testing for modal open times

## 📊 Current Metrics
- **Debug Statements**: ~670 remaining (down from 674)
- **TODO Comments**: 0 in critical code (all resolved!)
- **Build Status**: Clean (0 warnings, 0 errors)
- **Performance**: CardDragBehavior optimized (timer on-demand only)

## 🎯 Success Criteria
- [ ] Zero debug statements in production code
- [ ] Performance metrics: <100ms modal open time
- [ ] Code coverage: >80% for critical paths

---
*Last Updated: 2024-10-19*
*Next Review: End of Week*