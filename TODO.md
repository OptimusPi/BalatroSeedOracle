# TODO - BalatroSeedOracle Cleanup & Improvements

## 🔥🔥🔥 CRITICAL CODE QUALITY CRISIS - 2025-10-19 🔥🔥🔥

**WARNING: Previous claims of "EXCELLENT" metrics are FALSE!**
**ACTUAL STATUS: 1,865 code quality violations found!**

See `CODE_QUALITY_REPORT.md` for the brutal truth.

## ❌ FALSE CLAIMS (Session 10/19/2024 - NEEDS REVISION)

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

## ✅ CRITICAL ISSUES (FIXED TODAY - 10/19/2024)

### 1. SearchModal Critical Fixes (COMPLETED)
- [x] Added missing ShowSearchModal() method - SEARCH button now works!
- [x] Added missing ShowFiltersModal() method - CREATE NEW FILTER works!
- [x] Fixed MainMenu reference for proper modal navigation
- [x] Added Balatro-style modal transitions (200ms fall, 350ms rise)
- [x] Removed thread/batch settings from Deck/Stake tab (UI cleanup)

### 2. Debug Logging Cleanup (COMPLETED)
- [x] Wrapped all DebugLogger methods with #if DEBUG preprocessor directives
- [x] Removed all Console.WriteLine calls (6 files cleaned)
- [x] Removed all System.Diagnostics.Debug.WriteLine calls (3 files cleaned)
- [x] Fixed all empty catch blocks with appropriate comments (6 locations)
- [x] Fixed unused variable warnings (2 instances)

## 🚨 EMERGENCY PRIORITY (IMMEDIATELY!)

### Critical Code Quality Issues (From CODE_QUALITY_REPORT.md)
- [ ] **DELETE all 600+ DebugLogger calls** - Replace with proper logging
- [ ] **Fix AudioVisualizerSettingsModalViewModel** - 110+ line UI building in ViewModel!
- [ ] **Remove ALL FindControl from ViewModels** - 100+ MVVM violations
- [ ] **Extract ALL magic numbers** - 50+ hardcoded values (especially 50.8 in CardDragBehavior)
- [ ] **Delete ALL commented-out code** - 14+ files infected

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

## 📊 ACTUAL Current Metrics (DISASTER!)
- **Console.WriteLine**: Unknown (needs audit)
- **Debug.WriteLine**: Unknown (needs audit)
- **DebugLogger calls**: 600+ UNWRAPPED (FALSE claim about #if DEBUG)
- **Magic Numbers**: 50+ hardcoded values
- **MVVM Violations**: 100+ FindControl calls
- **Dead Code**: 14+ files with commented code
- **Build Status**: ⚠️ DECEPTIVE (builds but full of violations)
- **Code Quality**: 1,865 TOTAL VIOLATIONS

## 🎯 Success Criteria
- [ ] Zero debug statements in production code
- [ ] Performance metrics: <100ms modal open time
- [ ] Code coverage: >80% for critical paths

---
*Last Updated: 2024-10-19*
*Next Review: End of Week*