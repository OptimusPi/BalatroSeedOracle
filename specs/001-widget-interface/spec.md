# Feature Specification: Widget Interface System

**Feature Branch**: `001-widget-interface`  
**Created**: 2025-12-02  
**Status**: Draft  
**Input**: User description: "Create a new Interface to handle the concept of 'Widgets' in my app. A widget functions in two modes: minimized (square app launcher button) and open (full widget window)."

## Clarifications

### Session 2025-12-02

- Q: What should be the grid cell size for widget positioning? → A: Fixed size (e.g., 100x100px) for consistent layouts
- Q: Should widget positions and states persist between application sessions? → A: No persistence for layout/positions, but widget content state persists (e.g., search instances)
- Q: Which control buttons should be visible in the open widget title bar? → A: Show only minimize, others per widget configuration
- Q: How should widgets be positioned when first created? → A: Default positions (top-left, then fill row-wise)
- Q: How should the system handle widgets dragged outside window bounds? → A: Snap back to nearest grid position
- Q: How should the docking system be implemented for widgets? → A: Use Dock.Avalonia library APIs for proper docking behavior

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Widget State Toggle (Priority: P1)

As a user, I want to click a minimized widget to open it and click minimize to close it, so I can access widget functionality when needed.

**Why this priority**: This is the core widget behavior - everything else depends on this working.

**Independent Test**: Place a widget, click to open, verify it opens with title bar containing minimize button and widget content area using Balatro UI colors/fonts, click minimize, verify it returns to square minimized state.

**Acceptance Scenarios**:

1. **Given** a widget is minimized, **When** I click the square app icon, **Then** it opens showing the widget's content with title bar
2. **Given** a widget is open, **When** I click the minimize button, **Then** it returns to minimized square state
3. **Given** a widget is open, **When** I click the X button, **Then** it closes completely (if close button is enabled for that widget)

---

### User Story 2 - Minimized Widget Visual Features (Priority: P1)

As a user, I want minimized widgets to show their icon, title, notification badge, and progress bar, so I can see their status at a glance.

**Why this priority**: Essential visual features that make the minimized state useful.

**Independent Test**: Create widgets with notification counts (0, 5, 15) and progress values (0.0, 0.5, 1.0), verify badges show hidden/circle/pill shapes and progress bars render as red thermometer style at widget bottom.

**Acceptance Scenarios**:

1. **Given** a widget has a notification count, **When** minimized, **Then** it shows a red badge (hidden if 0, circle if 1-9, pill shape for 10+)
2. **Given** a widget has progress to display, **When** minimized, **Then** it shows a red thermometer progress bar at the bottom
3. **Given** a widget has an icon and title, **When** minimized, **Then** it shows the real icon (not emoji) and floating label below

---

### User Story 3 - Widget Drag and Drop (Priority: P2)

As a user, I want to drag minimized widgets around the screen with grid snapping, so I can organize my workspace.

**Why this priority**: Key to creating a customizable workspace experience.

**Independent Test**: Drag a widget around, verify it snaps to grid positions and handles occupied spaces properly.

**Acceptance Scenarios**:

1. **Given** a widget is minimized, **When** I click and hold to drag, **Then** I can move it around the window
2. **Given** I'm dragging a widget, **When** I release it, **Then** it snaps to the nearest grid position with padding from window edges
3. **Given** I drag to an occupied grid position, **When** I release, **Then** it finds the next available grid space

---

### User Story 4 - Widget Docking System (Priority: P3)

As a user, I want to drag open widgets to screen edges to dock them in specific positions, so I can create an efficient layout.

**Why this priority**: Advanced feature for power users who want sophisticated layouts.

**Independent Test**: Drag an open widget near screen edges, verify drop zones appear and docking works correctly.

**Acceptance Scenarios**:

1. **Given** a widget is open, **When** I drag it toward screen edges, **Then** drop zones appear showing available dock positions
2. **Given** drop zones are visible, **When** I hover over one, **Then** it glows to indicate where the widget will dock
3. **Given** I release in a drop zone, **When** docking completes, **Then** the widget takes the correct position (left/right full height, or corner quarters)

---

### User Story 5 - Multiple Open Widgets (Priority: P2)

As a user, I want to have multiple widgets open at the same time without interference, so I can use multiple tools simultaneously.

**Why this priority**: Essential for productive workflows.

**Independent Test**: Open multiple widgets, verify they all work independently.

**Acceptance Scenarios**:

1. **Given** one widget is open, **When** I open another, **Then** both remain open and functional
2. **Given** multiple widgets are open, **When** I interact with one, **Then** others are unaffected
3. **Given** multiple widgets exist, **When** I minimize one, **Then** others remain in their current state

---

### Edge Cases

- What happens when dragging a minimized widget beyond window bounds? (Snaps back to nearest grid position)
- How does window resizing affect docked widgets? (Docked widgets maintain proportional positioning and resize with container)
- What occurs when no grid positions are available? (New widgets queue for next available position or expand grid if window allows)
- How does the system handle widget content that changes size? (Open widgets resize to accommodate content, maintaining minimum size constraints)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an interface for widgets with minimized and open states
- **FR-002**: Minimized widgets MUST appear as square, rounded-corner buttons with proper icons
- **FR-003**: Minimized widgets MUST display the app title as a floating label below the icon  
- **FR-004**: Minimized widgets MUST show notification badges with proper sizing behavior
- **FR-005**: Minimized widgets MUST support optional progress bars displayed as red thermometer style
- **FR-006**: Open widgets MUST display the same app title in a title bar
- **FR-007**: Open widgets MUST have minimize button in consistent position (always visible)
- **FR-008**: Open widgets MUST support configurable close (X) button per widget
- **FR-009**: Open widgets MUST support configurable pop-out button per widget
- **FR-010**: Open widgets MUST use consistent Balatro UI styling for colors, buttons, and text
- **FR-011**: System MUST support drag and drop for minimized widgets
- **FR-012**: System MUST implement grid-based snapping for dropped widgets using fixed cell size (100x100px)
- **FR-013**: System MUST handle occupied grid positions by finding next available space
- **FR-014**: System MUST maintain appropriate padding from window edges
- **FR-015**: System MUST position new widgets in default locations (top-left, then row-wise)
- **FR-016**: System MUST support widget docking to left/right full height positions
- **FR-017**: System MUST support widget docking to corner quarter positions
- **FR-018**: System MUST display drop zone overlays during drag operations
- **FR-019**: System MUST support multiple widgets in open state simultaneously
- **FR-020**: System MUST use proper AvaloniaUI icon components instead of emoji

### Key Entities

- **IWidget Interface**: Contract defining widget behavior, state management, and display properties
- **WidgetState**: Enumeration for Minimized/Open states  
- **WidgetViewModel**: MVVM wrapper with observable properties for state, notifications, progress
- **WidgetContainer**: Manages widget layout, grid positioning, and drag-drop coordination
- **DockZone**: Screen regions where widgets can be docked with positioning rules
- **WidgetMetadata**: App title, icon resource, notification count, progress value

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can toggle widgets between states with single click
- **SC-002**: Widget drag operations provide smooth visual feedback
- **SC-003**: Grid snapping places widgets in visually consistent positions
- **SC-004**: Notification badges display correctly for all number ranges (0, 1-9, 10+)
- **SC-005**: Multiple widgets operate independently without conflicts
- **SC-006**: Widget docking provides clear visual feedback and accurate positioning
- **SC-007**: System handles edge cases without crashes
- **SC-008**: Widget layouts reset to default positions each application session