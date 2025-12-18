# Feature Specification: Restore Missing Web UI

**Feature Branch**: `001-restore-web-ui`  
**Created**: 2025-12-16  
**Status**: Draft  
**Input**: User description: "Help me restore my missing Balatro Seed Oracle web UI. I had a functional web interface with: - Side-by-side layout: JAML editor on left, results table on right - Settings gear icon - Start search button - Filter dropdown with status indicators (🔴 stopped, 🟢 running) - Format button (unique identifier) - Responsive CSS that collapses to vertical on mobile - Real search functionality that connected to MotelyAPI Current situation: - MotelyAPI server works and serves fake Swagger docs - Real web UI is missing/replaced - May be in git history or wrong directory - Need to find the original HTML/CSS/JS files - Located in BalatroSeedOracle project at X:\BalatroSeedOracle Search for the missing web UI files and help restore the functional interface. The API backend is working, just need the client UI back."

## Problem Statement

The functional Balatro Seed Oracle web UI has been lost or replaced, leaving users without access to the interactive seed search interface. The current web interface only shows API documentation instead of the working application that allows users to create filters, execute searches, and view results in a user-friendly format.

The missing interface was a complete web application that provided an intuitive way to interact with the MotelyAPI backend, featuring a responsive design and real-time search capabilities.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Locate Missing Web UI Files (Priority: P1)

As a developer, I want to find the original web UI files so I can restore the functional interface that users depend on for seed searching.

**Why this priority**: Without locating the original files, no functionality can be restored. This is the prerequisite for all other work.

**Independent Test**: Successfully locate and identify the original HTML, CSS, and JavaScript files that comprised the working web interface.

**Acceptance Scenarios**:

1. **Given** git history and file system search capabilities, **When** searching for the original web UI files, **Then** the complete set of interface files is located
2. **Given** located files, **When** examining their content, **Then** the side-by-side layout structure and Format button are confirmed present
3. **Given** found files, **When** comparing timestamps and git history, **Then** the replacement/loss event is identified

---

### User Story 2 - Restore Interactive Web Interface (Priority: P2)

As a user, I want to access the functional web interface with JAML editor and results display so I can create filters and execute seed searches through a user-friendly interface.

**Why this priority**: This restores the core user-facing functionality that was lost. Users need the interactive interface to be productive.

**Independent Test**: Access the restored web interface and successfully create a filter, execute a search, and view results in the side-by-side layout.

**Acceptance Scenarios**:

1. **Given** the restored web UI, **When** accessing the interface, **Then** the side-by-side layout displays with JAML editor on left and results table on right
2. **Given** the JAML editor, **When** writing a filter, **Then** the Format button properly formats the JAML syntax
3. **Given** a completed filter, **When** clicking Start search button, **Then** the search executes and results populate in the right panel

---

### User Story 3 - Responsive Design and Status Indicators (Priority: P3)

As a mobile user, I want the interface to adapt to smaller screens and display clear status indicators so I can use the application effectively on any device.

**Why this priority**: Ensures accessibility across devices and provides clear feedback about search operations.

**Independent Test**: Access the interface on mobile/narrow screens and verify responsive layout changes and status indicator functionality.

**Acceptance Scenarios**:

1. **Given** a mobile or narrow screen, **When** accessing the web UI, **Then** the layout collapses to vertical stacking instead of side-by-side
2. **Given** multiple saved filters, **When** viewing the filter dropdown, **Then** active searches show 🟢 indicators and stopped searches show 🔴 indicators
3. **Given** the settings gear icon, **When** clicked, **Then** configuration options are accessible and functional

---

### Edge Cases

- What happens when original files are permanently lost and need recreation from specifications?
- How does system handle partial file recovery where some components are missing?
- What occurs when located files are from incompatible versions or different API contracts?
- How does interface behave when MotelyAPI server is unavailable or returns errors?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST locate the original web UI files through git history analysis and file system search
- **FR-002**: Web interface MUST display a side-by-side layout with JAML editor on left and results table on right
- **FR-003**: Interface MUST include a Format button that properly formats JAML syntax in the editor
- **FR-004**: System MUST provide a Start search button that initiates seed searches via MotelyAPI connection
- **FR-005**: Filter dropdown MUST display status indicators (🔴 for stopped, 🟢 for running searches)
- **FR-006**: Interface MUST include a settings gear icon for accessing configuration options
- **FR-007**: Layout MUST be responsive and collapse to vertical stacking on mobile/narrow screens
- **FR-008**: Web UI MUST connect to and communicate with the existing MotelyAPI backend
- **FR-009**: Search results MUST populate in real-time as they are returned from the API
- **FR-010**: Interface MUST handle API connection failures gracefully with appropriate user feedback

### Key Entities

- **Web UI Files**: HTML, CSS, and JavaScript files that comprise the user interface
- **JAML Editor**: Text editor component for writing and formatting filter syntax
- **Results Table**: Display component for showing search results in tabular format
- **Filter Dropdown**: Selection component showing saved filters with status indicators
- **Search Session**: Active connection between web UI and MotelyAPI for executing searches
- **Layout Container**: Responsive container that manages side-by-side vs vertical layout

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Original web UI files are located within 2 hours of investigation effort
- **SC-002**: Restored interface loads and displays correctly in under 3 seconds
- **SC-003**: Users can complete a full search workflow (create filter → format → search → view results) in under 5 minutes
- **SC-004**: Responsive layout transitions occur smoothly when screen width drops below tablet breakpoint
- **SC-005**: Status indicators accurately reflect search states with 100% reliability
- **SC-006**: Interface maintains functionality across major browsers (Chrome, Firefox, Safari)

### Quality Measures

- Complete restoration of original functionality without feature loss
- Seamless integration with existing MotelyAPI backend
- Responsive design works across desktop, tablet, and mobile viewports
- User workflow efficiency matches or exceeds original interface performance

## Assumptions

- Original web UI files exist somewhere in git history or alternative locations
- MotelyAPI backend contracts remain compatible with original web UI expectations
- Browser environment supports modern JavaScript and CSS features used in original interface
- Users are familiar with JAML syntax and expect formatting assistance
- Real-time search updates provide better user experience than batch result loading
