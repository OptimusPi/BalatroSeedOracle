# Implementation Plan: Browser DuckDB-WASM Integration

**Branch**: 001-browser-duckdb-wasm | **Date**: 2025-12-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from /specs/001-browser-duckdb-wasm/spec.md

## Summary

Replace stub-based browser DuckDB handling with proper DuckDB-WASM integration via JavaScript interop. This enables full feature parity between desktop and browser builds for FertilizerService, SearchManager result storage, and all DuckDB-dependent functionality.

**Approach**: Create an IDuckDBService abstraction with platform-specific implementations:
- Desktop: DesktopDuckDBService wrapping existing DuckDB.NET.Data.Full usage
- Browser: BrowserDuckDBService using JSRuntime interop to DuckDB-WASM

## Technical Context

**Language/Version**: C# 14 / .NET 10  
**Primary Dependencies**: Avalonia 11.x, DuckDB.NET.Data.Full (desktop), @duckdb/duckdb-wasm (browser)  
**Storage**: DuckDB (desktop files, browser IndexedDB via OPFS)  
**Testing**: Manual integration testing (no unit test framework specified)  
**Target Platform**: Windows/Linux/macOS desktop + WebAssembly browser  
**Project Type**: Multi-target library with platform-specific entry points  
**Performance Goals**: Browser DuckDB within 2x of desktop performance  
**Constraints**: Bundle size <5MB for DuckDB-WASM, offline-capable  
**Scale/Scope**: Single user, local storage, thousands of seed results

## Constitution Check

*GATE: No constitution defined - proceeding with best practices*

- [x] Clean architecture (service abstraction pattern)
- [x] No magic strings in consuming code
- [x] Platform-specific code isolated to implementations only

## Project Structure

### Documentation (this feature)



### Source Code (repository root)



**Structure Decision**: Extend existing multi-target structure with a Services/DuckDB/ folder containing the abstraction layer. Platform-specific implementations live in the same folder with conditional compilation for registration.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Service abstraction | Different DuckDB APIs per platform | Direct usage requires #if BROWSER everywhere |
| JS interop layer | DuckDB-WASM is JavaScript-only | No .NET bindings exist for DuckDB-WASM |

## Implementation Phases

### Phase 1: Abstraction Layer (P3 - Foundation)

Create the IDuckDBService interface that abstracts all DuckDB operations:



### Phase 2: Desktop Implementation (P3 - Foundation)

Wrap existing DuckDB.NET.Data usage in DesktopDuckDBService:



### Phase 3: JavaScript Interop Module

Create wwwroot/js/duckdb-interop.js:



### Phase 4: Browser Implementation (P3 - Foundation)

Create BrowserDuckDBService with JSRuntime interop:



### Phase 5: Service Refactoring (P1, P2)

Refactor consuming services to use IDuckDBService:

1. **FertilizerService**: Replace DuckDBConnection with injected IDuckDBService
2. **SearchInstance**: Replace appender usage with IDuckDBAppender
3. **SearchStateManager**: Replace direct DuckDB usage
4. **DataGridResultsWindow**: Replace DuckDB queries

### Phase 6: Cleanup (Final)

1. Remove FertilizerWidget.Browser.axaml stub files
2. Remove conditional exclusions from csproj
3. Update DI registration for platform-specific implementations

## DuckDB-WASM Bundle Strategy

Use bundled DuckDB-WASM (not CDN) for offline support:



**Bundle source**: https://github.com/duckdb/duckdb-wasm/releases (latest v1.4.x)

## DI Registration



## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| DuckDB-WASM API differences | Create thin abstraction, test both platforms |
| IndexedDB persistence issues | Use OPFS for storage, fallback to memory |
| Bundle size | Use EH build (~3MB), enable gzip |
| Multi-tab conflicts | Single writer pattern with lock detection |

## Success Validation

1. Run dotnet build for both targets - no errors
2. Browser: FertilizerWidget shows full UI (not stub)
3. Browser: Run search, refresh page, results persist
4. Browser: Fertilizer pile accumulates seeds across searches
5. Desktop: All existing functionality unchanged