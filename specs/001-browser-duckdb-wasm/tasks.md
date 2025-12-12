# Tasks: Browser DuckDB-WASM Integration

**Input**: Design documents from /specs/001-browser-duckdb-wasm/
**Prerequisites**: plan.md, spec.md

**Tests**: Not requested - manual integration testing only

**Organization**: Tasks grouped by user story for independent implementation

## Format: [ID] [P?] [Story] Description

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: Download DuckDB-WASM bundle and prepare structure

- [ ] T001 Create Services/DuckDB/ folder in src/BalatroSeedOracle/Services/DuckDB/
- [ ] T002 Download DuckDB-WASM release (v1.4.x EH build) to src/BalatroSeedOracle.Browser/wwwroot/js/duckdb-wasm/
- [ ] T003 [P] Create placeholder duckdb-interop.js in src/BalatroSeedOracle.Browser/wwwroot/js/duckdb-interop.js

---

## Phase 2: Foundational - Platform Abstraction (US3, blocking)

**Purpose**: Core abstraction layer - MUST complete before US1/US2

### Interfaces

- [ ] T004 [P] Create IDuckDBService interface in src/BalatroSeedOracle/Services/DuckDB/IDuckDBService.cs
- [ ] T005 [P] Create IDuckDBConnection interface in src/BalatroSeedOracle/Services/DuckDB/IDuckDBConnection.cs
- [ ] T006 [P] Create IDuckDBAppender interface in src/BalatroSeedOracle/Services/DuckDB/IDuckDBAppender.cs

### Desktop Implementation

- [ ] T007 Create DesktopDuckDBService in src/BalatroSeedOracle/Services/DuckDB/DesktopDuckDBService.cs
- [ ] T008 Create DesktopDuckDBConnection in src/BalatroSeedOracle/Services/DuckDB/DesktopDuckDBConnection.cs
- [ ] T009 Create DesktopDuckDBAppender in src/BalatroSeedOracle/Services/DuckDB/DesktopDuckDBAppender.cs

### JavaScript Interop

- [ ] T010 Implement DuckDB-WASM wrapper in src/BalatroSeedOracle.Browser/wwwroot/js/duckdb-interop.js
- [ ] T011 Update index.html to load DuckDB-WASM in src/BalatroSeedOracle.Browser/wwwroot/index.html

### Browser Implementation

- [ ] T012 Create BrowserDuckDBService in src/BalatroSeedOracle/Services/DuckDB/BrowserDuckDBService.cs
- [ ] T013 Create BrowserDuckDBConnection in src/BalatroSeedOracle/Services/DuckDB/BrowserDuckDBConnection.cs
- [ ] T014 Create BrowserDuckDBAppender in src/BalatroSeedOracle/Services/DuckDB/BrowserDuckDBAppender.cs

### DI Registration

- [ ] T015 Add IDuckDBService registration to App.axaml.cs with platform-specific implementations

**Checkpoint**: Foundation ready

---

## Phase 3: User Story 1 - Search Results Storage (P1)

**Goal**: Browser users can persist search results in DuckDB-WASM

**Test**: Run search in browser, close tab, reopen - results persist

- [ ] T016 [US1] Refactor SearchInstance to use IDuckDBService in src/BalatroSeedOracle/Services/SearchInstance.cs
- [ ] T017 [US1] Refactor SearchStateManager to use IDuckDBService in src/BalatroSeedOracle/Services/SearchStateManager.cs
- [ ] T018 [US1] Refactor DataGridResultsWindow to use IDuckDBService in src/BalatroSeedOracle/Windows/DataGridResultsWindow.axaml.cs
- [ ] T019 [US1] Verify desktop build: dotnet build -f net10.0
- [ ] T020 [US1] Verify browser build: dotnet build -f net10.0-browser

**Checkpoint**: Search results work on both platforms

---

## Phase 4: User Story 2 - Fertilizer (P2)

**Goal**: Browser users can use Fertilizer feature

**Test**: Run multiple searches, verify seeds accumulate

- [ ] T021 [US2] Refactor FertilizerService to use IDuckDBService in src/BalatroSeedOracle/Services/FertilizerService.cs
- [ ] T022 [US2] Remove FertilizerWidget stub exclusion from src/BalatroSeedOracle/BalatroSeedOracle.csproj
- [ ] T023 [US2] Update FertilizerWidgetViewModel in src/BalatroSeedOracle/ViewModels/FertilizerWidgetViewModel.cs
- [ ] T024 [US2] Test Fertilizer in browser

**Checkpoint**: Fertilizer works on both platforms

---

## Phase 5: Polish and Cleanup

- [ ] T025 Delete FertilizerWidget.Browser.axaml from src/BalatroSeedOracle/Components/Widgets/
- [ ] T026 Delete FertilizerWidget.Browser.axaml.cs from src/BalatroSeedOracle/Components/Widgets/
- [ ] T027 Remove browser stub exclusions from src/BalatroSeedOracle/BalatroSeedOracle.csproj
- [ ] T028 Remove DuckDB conditionals from src/BalatroSeedOracle/Services/SearchManager.cs
- [ ] T029 Remove TempDir browser conditional from src/BalatroSeedOracle/Helpers/AppPaths.cs
- [ ] T030 [P] Verify both builds compile: dotnet build
- [ ] T031 [P] Test desktop functionality
- [ ] T032 [P] Test browser functionality
- [ ] T033 Discard git changes to deleted stub files

---

## Dependencies

- **Phase 1**: No dependencies
- **Phase 2**: Depends on Phase 1 - BLOCKS US1/US2
- **Phase 3 (US1)**: After Phase 2
- **Phase 4 (US2)**: After Phase 2 (parallel with US1)
- **Phase 5**: After US1 and US2

## Parallel Opportunities

- T004, T005, T006 (interfaces) in parallel
- T007, T008, T009 (desktop) in parallel
- T012, T013, T014 (browser) in parallel
- Phase 3 and Phase 4 can run in parallel
- T030, T031, T032 in parallel

## MVP Strategy

1. Phase 1 + 2 -> Foundation
2. Phase 3 (US1) -> Search results persist
3. **STOP**: This is usable MVP!
4. Continue with Phase 4, 5 for full feature