# Tasks: Restore Missing Web UI

**Input**: Design documents from `/specs/001-restore-web-ui/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and SignalR setup

- [ ] T001 Add SignalR package to external/Motely/Motely.API/Motely.API.csproj
- [ ] T002 Verify MotelySearchDatabase.cs is in external/Motely/Motely.API/MotelySearchDatabase.cs
- [ ] T003 Ensure SearchHub.cs exists in external/Motely/Motely.API/SearchHub.cs
- [ ] T004 Verify app.js is in external/Motely/Motely.API/wwwroot/app.js

## Phase 2: Foundational (Core Dependencies)

**Purpose**: Essential components needed for all user stories

- [ ] T005 Create SignalR hub registration in existing MotelyApiServer.cs
- [ ] T006 Add SignalR endpoint routing to MotelyApiServer.cs
- [ ] T007 Integrate SignalR broadcasting in search progress callbacks

## Phase 3: User Story 1 - Locate Missing Web UI Files (Priority: P1)

**Story Goal**: Find and restore original web UI files
**Independent Test**: indexOLD.html is copied to index.html and loads in browser

- [x] T008 [US1] Copy indexOLD.html to index.html in external/Motely/Motely.API/wwwroot/index.html
- [x] T009 [US1] Verify app.js SignalR client code in external/Motely/Motely.API/wwwroot/app.js
- [x] T010 [US1] Test basic HTML loading at http://localhost:3141

## Phase 4: User Story 2 - Restore Interactive Web Interface (Priority: P2)

**Story Goal**: Get functional web interface with JAML editor and search capability
**Independent Test**: Complete search workflow works (create filter → search → view results)

- [ ] T011 [P] [US2] Add Format button functionality to JAML editor in wwwroot/index.html
- [ ] T012 [P] [US2] Implement side-by-side CSS layout in wwwroot/app.css
- [ ] T013 [US2] Connect Start search button to SignalR search initiation
- [ ] T014 [US2] Wire SignalR events to update results table in real-time
- [ ] T015 [US2] Test complete search workflow: filter creation → execution → results

## Phase 5: User Story 3 - Responsive Design and Status Indicators (Priority: P3)

**Story Goal**: Mobile-responsive layout and visual search status feedback
**Independent Test**: Interface works on mobile screens with proper status indicators

- [ ] T016 [P] [US3] Add responsive CSS media queries for mobile layout in wwwroot/app.css
- [ ] T017 [P] [US3] Implement filter dropdown with status indicators (🔴/🟢) in wwwroot/index.html
- [ ] T018 [P] [US3] Add settings gear icon and modal functionality in wwwroot/index.html
- [ ] T019 [US3] Test mobile responsive behavior and status indicator accuracy

## Phase 6: Polish & Integration

**Purpose**: Final integration and testing

- [ ] T020 [P] Verify SignalR connection stability under extended use
- [ ] T021 [P] Test API error handling and graceful degradation
- [ ] T022 Validate complete restoration against original functionality checklist

## Dependencies

**Story Dependencies**:
- **US1** → **US2**: Must locate files before restoring functionality
- **US2** → **US3**: Need basic UI working before adding responsive features
- **US1, US2, US3** are otherwise independent

**Parallel Execution Opportunities**:
- T011, T012 (Format button + CSS) can run in parallel
- T016, T017, T018 (responsive features) can run in parallel
- T020, T021 (testing tasks) can run in parallel

## Implementation Strategy

**MVP Scope**: Complete Phase 3 (User Story 1) delivers minimal viable restoration
- Basic web UI loads and displays
- Proves file recovery was successful
- Establishes foundation for further work

**Incremental Delivery**:
- **Phase 3**: Basic file restoration and loading
- **Phase 4**: Functional search interface with real-time updates
- **Phase 5**: Enhanced UX with responsive design and status indicators

**Testing Approach**: Manual browser testing focused on user workflows
- Each phase has independent test criteria
- Emphasis on functional validation over automated testing
- Browser developer tools for SignalR connection verification