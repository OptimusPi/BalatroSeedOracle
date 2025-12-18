# Implementation Plan: Restore Missing Web UI

**Branch**: `001-restore-web-ui` | **Date**: 2025-12-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-restore-web-ui/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Restore the missing Motely API test UI - a minimal SPA with side-by-side layout (JAML editor + results table) that was working with SignalR WebSockets for real-time search updates. The current API server serves documentation instead of the functional test interface.

## Technical Context

**Language/Version**: HTML/CSS/JavaScript (client), C# .NET 10.0 (server SignalR integration)  
**Primary Dependencies**: SignalR (WebSocket), existing MotelyAPI endpoints, Monaco Editor (JAML syntax highlighting)  
**Storage**: Browser localStorage for settings, API server handles DuckDB persistence  
**Testing**: Manual browser testing, SignalR connection validation  
**Target Platform**: Web browsers (modern JavaScript/CSS support)
**Project Type**: Minimal SPA test client for API server  
**Performance Goals**: <3 second load time, real-time WebSocket updates <100ms  
**Constraints**: Must work with existing MotelyAPI server, minimal dependencies, responsive design  
**Scale/Scope**: Single test UI page, ~5 key components (editor, results, settings, status, format button)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ✅ PASS - Constitution is template-only, no specific violations

The restoration approach aligns with software engineering principles:
- **Restoration over Rebuild**: Use existing working files instead of recreating from scratch
- **Minimal Scope**: Focus on test UI only, not expanding functionality
- **Preserve Working Code**: Keep functional API server, only restore missing client

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
external/Motely/
├── Motely.API/                    # API server project (existing)
│   ├── wwwroot/                   # Web UI files (restore target)
│   │   ├── index.html             # Main UI page (restore from indexOLD.html)
│   │   ├── app.js                 # SignalR client logic (already recovered)
│   │   ├── app.css                # Responsive styling (needs restoration)
│   │   └── favicon.ico           # Icon file
│   ├── SearchHub.cs               # SignalR hub (create new)
│   ├── MotelyApiServer.cs         # Existing HttpListener server
│   ├── MotelySearchDatabase.cs    # Clean database abstraction (already recovered)
│   └── Motely.API.csproj         # Project file (may need SignalR packages)
└── Motely.TUI/                   # TUI that hosts API server
    └── ApiServerWindow.cs         # Embeds MotelyApiServer
```

**Structure Decision**: Focused restoration approach targeting the Motely.API/wwwroot directory. The existing API server architecture is preserved while adding missing SignalR components. Web UI files are restored to their original location where the API server can serve them alongside the API endpoints.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
