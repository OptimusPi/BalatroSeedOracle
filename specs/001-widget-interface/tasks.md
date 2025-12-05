# Tasks: Widget Interface System - ACTUAL IMPLEMENTATION

**Input**: Design documents from `/specs/001-widget-interface/`
**Prerequisites**: plan.md (required), data-model.md, contracts/, quickstart.md

**CRITICAL**: This task list focuses on **ACTUALLY FIXING THE EXISTING WIDGETS** instead of building parallel infrastructure that doesn't get used.

**Organization**: Tasks prioritized by immediate impact and user value

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Phase 1: Fix Immediate Broken Functionality

**Purpose**: Fix the widgets that are currently broken and frustrating to use

### Fix Broken Minimize Buttons

- [X] T001 [P] Fix EventFXWidget to inherit from BaseWidgetControl in src/Components/Widgets/EventFXWidget.axaml.cs
- [X] T002 [P] Fix TransitionDesignerWidget inheritance in src/Components/Widgets/TransitionDesignerWidget.axaml.cs  
- [X] T003 [P] Fix MusicMixerWidget inheritance in src/Components/Widgets/MusicMixerWidget.axaml.cs
- [X] T004 [P] Fix AudioVisualizerSettingsWidget inheritance in src/Components/Widgets/AudioVisualizerSettingsWidget.axaml.cs
- [X] T005 [P] Fix GenieWidget inheritance in src/Components/Widgets/GenieWidget.axaml.cs
- [X] T006 [P] Fix HostApiWidget inheritance in src/Components/Widgets/HostApiWidget.axaml.cs
- [X] T007 [P] Fix FertilizerWidget inheritance in src/Components/Widgets/FertilizerWidget.axaml.cs

### Remove Pixel Borders (NO COLORED BORDERS EVER)

- [X] T008 [P] Remove pixel borders from EventFXWidget.axaml (BorderThickness="0", remove BorderBrush)
- [X] T009 [P] Remove pixel borders from MusicMixerWidget.axaml
- [X] T010 [P] Remove pixel borders from AudioVisualizerSettingsWidget.axaml  
- [X] T011 [P] Remove pixel borders from GenieWidget.axaml
- [X] T012 [P] Remove pixel borders from HostApiWidget.axaml
- [X] T013 [P] Remove pixel borders from FertilizerWidget.axaml
- [X] T014 [P] Remove pixel borders from SearchWidget.axaml
- [X] T015 [P] Remove pixel borders from DayLatroWidget.axaml

### Fix Widget Toggle Functionality

- [X] T016 Ensure all widgets properly implement IsMinimized binding in their ViewModels
- [X] T017 Verify ExpandCommand exists and works in all widget ViewModels derived from BaseWidgetViewModel
- [X] T018 Test minimize/expand functionality on all widgets after fixes

---

## Phase 2: Replace Custom Widget Positioning with New Grid System

**Purpose**: Replace the inconsistent custom positioning with the proper grid system we designed

### Convert to Grid Layout System

- [X] T019 Replace hardcoded widget positions with grid-based positioning in WidgetLayoutService
- [X] T020 Update DayLatroWidget to use grid positioning instead of hardcoded PositionX/Y
- [X] T021 [P] Convert EventFXWidget positioning to grid system
- [X] T022 [P] Convert TransitionDesignerWidget positioning to grid system
- [X] T023 [P] Convert MusicMixerWidget positioning to grid system
- [X] T024 [P] Convert AudioVisualizerWidget positioning to grid system
- [X] T025 [P] Convert GenieWidget positioning to grid system
- [X] T026 [P] Convert HostApiWidget positioning to grid system
- [X] T027 [P] Convert FertilizerWidget positioning to grid system

### Replace Custom Drag Behavior with New System

- [X] T028 Replace DraggableWidgetBehavior with new grid-snapping drag system in all widgets
- [X] T029 Implement proper boundary detection and snap-back for out-of-bounds drags
- [X] T030 Add visual grid feedback during drag operations
- [X] T031 Test drag and drop functionality with grid snapping

---

## Phase 3: Implement Proper Widget Interface

**Purpose**: Make existing widgets implement the IWidget interface properly

### Convert Widget ViewModels

- [X] T032 Make DayLatroWidgetViewModel implement IWidget interface properly with all required methods
- [X] T033 Make EventFXWidgetViewModel implement IWidget interface  
- [X] T034 Make TransitionDesignerWidgetViewModel implement IWidget interface
- [X] T035 Make MusicMixerWidgetViewModel implement IWidget interface
- [X] T036 Make AudioVisualizerWidgetViewModel implement IWidget interface
- [X] T037 Make GenieWidgetViewModel implement IWidget interface
- [X] T038 Make HostApiWidgetViewModel implement IWidget interface
- [X] T039 Make FertilizerWidgetViewModel implement IWidget interface
- [X] T040 Make SearchWidgetViewModel implement IWidget interface

### Replace Widget Display System

- [X] T041 Replace hardcoded widget Grid in BalatroMainMenu.axaml with WidgetContainer
- [X] T042 Update BalatroMainMenuViewModel to initialize WidgetContainer and register existing widgets
- [X] T043 Implement widget registration for all existing widget types
- [X] T044 Test that all widgets display properly in new container system

---

## Phase 4: Add Advanced Features

**Purpose**: Implement the advanced features from the specification

### Notification and Progress System

- [X] T045 [P] Add notification badge support to all widgets using NotificationBadge component
- [X] T046 [P] Add progress bar support to widgets that need it using ThermometerProgressBar
- [X] T047 [P] Replace emoji icons with proper AvaloniaUI icons in all widgets
- [X] T048 Test notification and progress display across all widgets

### Docking System Implementation

- [X] T049 Research and integrate actual AvaloniaUI docking library instead of custom implementation
- [X] T050 Replace custom DockingService with proper AvaloniaUI docking library
- [X] T051 Implement dock zones for left/right full height and corner positions
- [X] T052 Add visual drop zone feedback during docking operations
- [X] T053 Test docking functionality with existing widgets

---

## Phase 5: Polish and Validation

**Purpose**: Ensure everything works properly and meets requirements

### Final Integration Testing

- [X] T054 [P] Test all widgets can be minimized and expanded properly
- [X] T055 [P] Test multiple widgets can be open simultaneously without conflicts
- [X] T056 [P] Test drag and drop with grid snapping works correctly
- [X] T057 [P] Test docking system works with all widget types
- [X] T058 [P] Test notification badges display correctly (0=hidden, 1-9=circle, 10+=pill)
- [X] T059 [P] Test progress bars display properly when widgets have progress
- [X] T060 Validate that widget layouts reset properly each session (no persistence)

### Performance and Polish

- [X] T061 [P] Optimize widget rendering for multiple open widgets
- [X] T062 [P] Add smooth state transition animations if desired
- [X] T063 [P] Add keyboard navigation support for accessibility
- [X] T064 Code cleanup and refactoring for consistency
- [X] T065 Update documentation to reflect actual implementation

---

## Critical Success Criteria

**MUST WORK AFTER IMPLEMENTATION**:
1. **ALL widgets have working minimize buttons** (click minimized → expand, click minimize button → minimize)
2. **NO pixel borders anywhere** (BorderThickness="0" for all widget borders)
3. **Consistent Balatro UI styling** across all widgets
4. **Grid-based positioning** instead of hardcoded coordinates
5. **Multiple widgets can be open** without conflicts
6. **Drag and drop works** with proper grid snapping
7. **Notification badges work** on widgets that need them
8. **Progress bars work** on widgets that show progress

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1**: Fix immediate issues - can start immediately
- **Phase 2**: Grid system - depends on Phase 1 completion
- **Phase 3**: Interface implementation - depends on Phase 2
- **Phase 4**: Advanced features - depends on Phase 3
- **Phase 5**: Polish - depends on all phases

### Parallel Opportunities
- All widget fixes within each phase can run in parallel [P]
- Different widget files can be modified simultaneously
- Testing tasks can run in parallel once implementation is complete

### Critical Path
1. **Fix broken minimize buttons FIRST** (T001-T007)
2. **Remove all pixel borders** (T008-T015)  
3. **Verify basic functionality works** (T016-T018)
4. **Then proceed with grid system and advanced features**

## Parallel Example: Phase 1 Widget Fixes

```bash
# Fix all widget inheritance issues in parallel:
Task: "Fix EventFXWidget to inherit from BaseWidgetControl"
Task: "Fix TransitionDesignerWidget inheritance" 
Task: "Fix MusicMixerWidget inheritance"
Task: "Fix AudioVisualizerSettingsWidget inheritance"
Task: "Fix GenieWidget inheritance"
Task: "Fix HostApiWidget inheritance" 
Task: "Fix FertilizerWidget inheritance"
```

## Implementation Strategy

### Immediate Value First
1. **Phase 1**: Fix broken minimize buttons → **immediate user relief**
2. **Phase 1**: Remove ugly pixel borders → **immediate visual improvement**  
3. **Phase 1**: Test basic functionality → **validate fixes work**

### Incremental Enhancement
1. **Phase 2**: Add grid positioning → **better layout**
2. **Phase 3**: Proper interface implementation → **consistency**
3. **Phase 4**: Advanced features → **enhanced functionality**

### Success Validation
- After Phase 1: **All widgets minimize/expand properly**
- After Phase 2: **Widgets position in clean grid**
- After Phase 3: **Consistent interface across all widgets**
- After Phase 4: **Full feature set working**

---

## Notes

- **FOCUS ON FIXING EXISTING WIDGETS** not building parallel systems
- **Each task should deliver immediate user value**
- **Test frequently** to ensure changes don't break existing functionality  
- **Phase 1 is CRITICAL** - user frustration relief
- **Use actual AvaloniaUI docking library** when implementing docking
- **No more reinvented wheels** - leverage existing base classes properly