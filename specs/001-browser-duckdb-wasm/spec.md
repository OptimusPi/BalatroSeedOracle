# Feature Specification: Browser DuckDB-WASM Integration via JS Interop

**Feature Branch**: 001-browser-duckdb-wasm
**Created**: 2025-12-11
**Status**: Draft
**Input**: User description: Browser DuckDB WASM Integration via JS Interop - Implement proper JS interop to DuckDB-WASM for browser builds instead of stubbing out features

## Problem Statement

The current browser build strategy disables DuckDB-dependent features (FertilizerService, FertilizerWidget, search result storage) by using stub components that display not available in browser messages. This is a **lazy approach** that removes core functionality from the browser version.

**Reality**: DuckDB-WASM is a mature, production-ready JavaScript library that brings full DuckDB functionality to browsers. The proper solution is to use **JavaScript Interop** to call DuckDB-WASM from .NET browser code, enabling full feature parity between desktop and browser builds.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seed Search Results Storage in Browser (Priority: P1)

As a browser user, I want my seed search results to be stored persistently in DuckDB so I can query, sort, and filter them efficiently - the same way desktop users can.

**Why this priority**: This is the core value proposition. Without persistent storage, browser users can only see live results during a search - they lose all data on refresh. This makes the browser version essentially useless for serious seed hunting.

**Independent Test**: Run a seed search in browser, close the tab, reopen - results should persist. Export to CSV should work identically to desktop.

**Acceptance Scenarios**:

1. **Given** a browser session with completed search results, **When** I refresh the page, **Then** my results are still available from IndexedDB-backed DuckDB
2. **Given** search results in browser DuckDB, **When** I apply a sort/filter, **Then** DuckDB executes the query and returns sorted/filtered results
3. **Given** multiple search sessions over time, **When** I query aggregate data, **Then** DuckDB returns results across all stored searches

---

### User Story 2 - Fertilizer Seed Pile in Browser (Priority: P2)

As a browser user, I want access to the Fertilizer feature that accumulates top seeds across all my searches into a global pile for instant re-searching.

**Why this priority**: Fertilizer is a unique feature that makes seed hunting much more efficient. Disabling it in browser removes a key differentiator.

**Independent Test**: Run multiple searches in browser, verify seeds accumulate in Fertilizer pile, verify pile persists across sessions.

**Acceptance Scenarios**:

1. **Given** a completed search with 1000+ results, **When** the search completes, **Then** top 1000 seeds are automatically added to the Fertilizer pile via browser DuckDB
2. **Given** a Fertilizer pile with seeds, **When** I start a new search with Use Fertilizer enabled, **Then** the search uses the cached seeds for instant results
3. **Given** a Fertilizer pile, **When** I click Export, **Then** seeds are exported to a downloadable file

---

### User Story 3 - Transparent Platform Abstraction (Priority: P3)

As a developer, I want a clean abstraction layer so that view models and services do not know or care whether they are running on desktop (DuckDB.NET) or browser (DuckDB-WASM via JS interop).

**Why this priority**: Clean architecture ensures maintainability and prevents browser special case code from polluting the codebase.

**Independent Test**: Write a new feature using the abstraction - it should work on both platforms without conditional compilation.

**Acceptance Scenarios**:

1. **Given** an IDuckDBService interface, **When** running on desktop, **Then** DesktopDuckDBService using DuckDB.NET.Data handles all calls
2. **Given** an IDuckDBService interface, **When** running in browser, **Then** BrowserDuckDBService using JS interop to DuckDB-WASM handles all calls
3. **Given** consuming code using IDuckDBService, **When** building for either platform, **Then** no #if BROWSER conditionals are needed in consuming code

---

### Edge Cases

- What happens when IndexedDB storage quota is exceeded in browser?
- How does system handle DuckDB-WASM initialization failure (WASM not supported, blocked by CSP)?
- What happens when browser is offline and DuckDB-WASM cannot load from CDN?
- How does system handle concurrent writes from multiple browser tabs?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an IDuckDBService abstraction with implementations for both desktop (DuckDB.NET) and browser (DuckDB-WASM)
- **FR-002**: Browser implementation MUST use JavaScript interop to call DuckDB-WASM library
- **FR-003**: Browser DuckDB MUST persist data to IndexedDB for cross-session persistence
- **FR-004**: FertilizerService MUST work in browser using the abstraction layer
- **FR-005**: FertilizerWidget MUST display full functionality in browser (not a stub)
- **FR-006**: SearchManager MUST use the abstraction for result storage in browser
- **FR-007**: System MUST load DuckDB-WASM from bundled assets (not CDN) for offline support
- **FR-008**: System MUST handle DuckDB-WASM initialization gracefully with appropriate error messages

### Key Entities

- **IDuckDBService**: Interface abstracting DuckDB operations (Execute, Query, Appender pattern, etc.)
- **DesktopDuckDBService**: Implementation using DuckDB.NET.Data.Full (existing code, refactored)
- **BrowserDuckDBService**: New implementation using JSRuntime interop to DuckDB-WASM
- **DuckDBJsInterop**: JavaScript module wrapping DuckDB-WASM with methods callable from .NET

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Browser users can run searches and results persist across page refreshes (0 data loss)
- **SC-002**: Fertilizer feature works identically in browser and desktop (feature parity)
- **SC-003**: No not available in browser stub messages for DuckDB-dependent features
- **SC-004**: Browser build size increase is reasonable (less than 5MB for DuckDB-WASM bundle)
- **SC-005**: DuckDB operations in browser complete within 2x desktop performance (acceptable overhead)
- **SC-006**: Zero #if BROWSER conditionals in ViewModel or Service layer code (clean architecture)

## Out of Scope

- Server-side seed sharing / DuckLake integration (future feature)
- Seed search execution in browser (Motely uses SIMD - remains desktop only, browser shows results only)
- Progressive Web App (PWA) functionality