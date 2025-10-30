# MISSING FEATURES AND INCOMPLETE WORK REPORT

**Generated:** 2025-10-26
**Project:** BalatroSeedOracle C# Avalonia Application

---

## TODO Comments (9)

1. **X:\BalatroSeedOracle\src\Views\Modals\VisualizerWorkspace.axaml.cs:242**
   - TODO: Save current settings as a preset
   - **Priority:** LOW - Nice-to-have feature

2. **X:\BalatroSeedOracle\src\Services\VibeAudioManager.cs:227**
   - TODO: settings = profile?.VibeOutSettings; - property doesn't exist yet
   - **Priority:** MEDIUM - VibeOut settings integration pending

3. **X:\BalatroSeedOracle\src\Services\VibeAudioManager.cs:244**
   - TODO: Use user's saved volumes when VibeOutSettings exists
   - **Priority:** MEDIUM - User preference persistence missing

4. **X:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml.bak:211**
   - TODO: Add horizontal item sprites using SpritesService
   - **Priority:** LOW - Backup file, may not be active code

5. **X:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml.bak:234**
   - TODO: Add horizontal item sprites using SpritesService
   - **Priority:** LOW - Backup file

6. **X:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml.bak:257**
   - TODO: Add horizontal item sprites using SpritesService
   - **Priority:** LOW - Backup file

7. **X:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml.bak:283**
   - TODO: 5-slot horizontal joker display
   - **Priority:** LOW - Backup file

8. **X:\BalatroSeedOracle\src\Views\Modals\FilterSelectionModal.axaml.bak:308**
   - TODO: Custom Rules, Game Modifiers, Deck, Banned Cards sections
   - **Priority:** MEDIUM - Backup file, but represents planned features

9. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:825**
   - TODO: Implement track volume control when audio manager supports it
   - **Priority:** MEDIUM - Per-track volume control not implemented

10. **X:\BalatroSeedOracle\src\ViewModels\AudioVisualizerSettingsWidgetViewModel.cs:741**
    - TODO: Implement JSON export for all shader parameters
    - **Priority:** LOW - Export feature for visualizer settings

11. **X:\BalatroSeedOracle\src\ViewModels\BalatroMainMenuViewModel.cs:555**
    - TODO: Implement effect bindings that map tracks to shader parameters
    - **Priority:** MEDIUM - Audio-to-visual mapping feature

12. **X:\BalatroSeedOracle\src\ViewModels\FilterSelectionModalViewModel.cs:201**
    - TODO: Show confirmation dialog
    - **Priority:** HIGH - Missing user confirmation for potentially destructive action

13. **X:\BalatroSeedOracle\src\ViewModels\SearchModalViewModel.cs:937**
    - TODO AFTER pifreak configures the visualizer THEN we can make the search mode audio!
    - **Priority:** LOW - Future enhancement waiting on design

---

## HACK/FIXME Comments (0)

No HACK or FIXME comments found. This is good - no acknowledged technical shortcuts.

---

## NotImplementedException (21)

All in **Converter** classes - these are **ConvertBack** methods which are typically one-way converters:

### X:\BalatroSeedOracle\src\Converters\SpriteConverters.cs
1. Line 41 - `ConvertBack` in converter
2. Line 68 - `ConvertBack` in converter
3. Line 95 - `ConvertBack` in converter
4. Line 122 - `ConvertBack` in converter
5. Line 162 - `ConvertBack` in converter
6. Line 193 - `ConvertBack` in converter
7. Line 255 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\Converters\PlayingCardConverters.cs
8. Line 28 - `ConvertBack` in converter
9. Line 62 - `ConvertBack` in converter
10. Line 83 - `ConvertBack` in converter
11. Line 111 - `ConvertBack` in converter
12. Line 133 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\Converters\BoolToOpacityConverter.cs
13. Line 28 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\Converters\FilterCriteriaConverter.cs
14. Line 51 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\Converters\BoolToLedColorConverter.cs
15. Line 36 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\Converters\FilterItemConverters.cs
16. Line 92 - `ConvertBack` in converter
17. Line 129 - `ConvertBack` in converter
18. Line 195 - `ConvertBack` in converter

### X:\BalatroSeedOracle\src\ViewModels\Converters.cs
19. Line 35 - `ConvertBack` in converter
20. Line 51 - `ConvertBack` in converter
21. Line 67 - `ConvertBack` in converter

**Assessment:** These are **acceptable** - ConvertBack in one-way bindings should throw NotImplementedException. Not a bug.

**Priority:** NONE - Standard pattern for one-way converters

---

## Empty/Placeholder Methods (0)

No empty method bodies found with placeholder logic.

---

## Commented-Out Code (2)

1. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:609-667**
   - Large commented-out section: VibeOut Mode feature (removed)
   - **Lines:** ~58 lines of dead code
   - **Recommendation:** DELETE - Feature officially removed

2. **X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml.cs:878-920**
   - Commented-out shader range application methods
   - **Lines:** ~42 lines
   - **Recommendation:** DELETE - Methods removed from BalatroShaderBackground

**Total commented-out code:** ~100 lines

**Priority:** LOW - Cleanup recommended but not blocking

---

## Incomplete Switch Statements (0)

No incomplete switch/case statements with missing cases or default throws detected.

---

## Dead Code (0)

No unreachable code or obviously unused methods detected beyond commented-out sections.

---

## Summary

| Category | Count | Priority Distribution |
|----------|-------|----------------------|
| TODO Comments | 13 | High: 1, Medium: 5, Low: 7 |
| HACK/FIXME Comments | 0 | - |
| NotImplementedException | 21 | NONE (all acceptable converter patterns) |
| Empty/Placeholder Methods | 0 | - |
| Commented-Out Code Blocks | 2 | LOW (cleanup) |
| Incomplete Switch Statements | 0 | - |
| Dead Code | 0 | - |
| **TOTAL ISSUES** | **15** | **High: 1, Medium: 5, Low: 9** |

---

## Critical Action Items

### HIGH Priority
1. **FilterSelectionModalViewModel.cs:201** - Add confirmation dialog for delete operations

### MEDIUM Priority
1. Implement track volume control (BalatroMainMenu.cs:825)
2. Implement effect bindings for audio-to-visual mapping (BalatroMainMenuViewModel.cs:555)
3. Complete VibeOut settings integration (VibeAudioManager.cs:227, 244)
4. Implement custom rules/modifiers sections (FilterSelectionModal planning)

### LOW Priority
1. Add preset save functionality for visualizer (VisualizerWorkspace.cs:242)
2. Implement JSON export for shader parameters (AudioVisualizerSettingsWidgetViewModel.cs:741)
3. Clean up commented-out VibeOut code (~100 lines)
4. Remove backup .axaml.bak files with TODOs

---

## Overall Assessment

The codebase is in **good shape** regarding incomplete work:
- Only **13 active TODOs**, most are low priority
- **No HACK or FIXME** comments indicating rushed code
- **NotImplementedExceptions are all legitimate** (converter ConvertBack methods)
- **Minimal commented-out code** (just removed VibeOut feature)
- **1 critical missing feature:** Confirmation dialog for destructive actions

**Estimated effort to complete all items:** 16-24 hours
