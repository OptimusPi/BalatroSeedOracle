# Feature Specification: Orbital Widget Launcher with Balatro-Style Intro Animation

**Feature Branch**: `002-orbital-launcher`  
**Created**: 2025-12-03  
**Status**: Draft  
**Input**: User description: "Orbital widget launcher and splash screen intro"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Logo Intro Animation (Priority: P1)

User sees the seed logo (brown seed with green sprout + tiny joker, drawn by pifreak's daughter) start at 1 pixel, grow with elastic animation, spin during growth, and come into focus in the center of the screen using the existing TransitionService.

**Why this priority**: Foundation for the entire system - logo becomes the central hub for all widget interaction. Uses existing transition infrastructure.

**Independent Test**: Can be tested by launching the app and observing the intro animation sequence.

**Acceptance Scenarios**:
1. **Given** app is starting, **When** intro begins, **Then** logo starts at 1px scale in center
2. **Given** logo is tiny, **When** growth animation plays, **Then** logo elastically scales to normal size while spinning
3. **Given** logo animation completes, **When** user sees final state, **Then** logo is centered and ready for interaction

---

### User Story 2 - Orbital Widget Launcher (Priority: P1)

User clicks the centered seed logo and widget cards orbit out in a circular arc around the logo. User can select a widget to launch it, or click the logo again to retract all widgets.

**Why this priority**: Core functionality that replaces the boring modal buttons with the "A LOT CUTER" card-based selection system.

**Independent Test**: Can be tested by clicking the logo and verifying widgets appear in orbital pattern.

**Acceptance Scenarios**:
1. **Given** logo is displayed, **When** user clicks logo, **Then** widget cards orbit out in circular arc with juice animations
2. **Given** widgets are orbiting, **When** user selects a widget card, **Then** selected widget launches and others retract
3. **Given** widgets are orbiting, **When** user clicks logo again, **Then** all widgets get "sucked in" and disappear

---

### User Story 3 - Menu Slide-In System (Priority: P2)

All main UI elements (main menu buttons, floating button dock) slide into view with Balatro-style offset animations instead of just appearing.

**Why this priority**: Completes the polished intro experience and makes the entire UI feel cohesive with the game-like aesthetic.

**Independent Test**: Can be tested by observing main menu buttons sliding into position after intro.

**Acceptance Scenarios**:
1. **Given** intro animation completes, **When** main menu appears, **Then** buttons slide in from offscreen positions
2. **Given** floating dock exists, **When** UI initializes, **Then** dock slides into position from screen edge
3. **Given** any modal opens, **When** animation plays, **Then** modal slides in rather than just appearing

---

### User Story 4 - Auto-Hide Integration (Priority: P2)

When user interacts with any other UI element (main menu buttons, floating dock, modals), the orbital launcher automatically hides to prevent conflicts.

**Why this priority**: Ensures the orbital launcher doesn't interfere with existing UI workflows.

**Independent Test**: Can be tested by opening orbital launcher, then clicking a main menu button.

**Acceptance Scenarios**:
1. **Given** orbital launcher is open, **When** user clicks main menu button, **Then** orbital launcher retracts automatically
2. **Given** orbital launcher is open, **When** user interacts with floating dock, **Then** orbital launcher hides
3. **Given** any modal opens, **When** modal appears, **Then** orbital launcher is hidden if visible

---

### Edge Cases

- What happens when user rapidly clicks logo during animation?
- How does system handle widget creation failure during orbital launch?
- What if user resizes window during orbital animation?
- How does orbital launcher handle empty widget list?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST reuse existing TransitionService for all logo and menu animations
- **FR-002**: System MUST display seed logo (brown seed, green sprout, tiny joker) as central element
- **FR-003**: Logo MUST start at 1px scale and grow to normal size with spin animation
- **FR-004**: Users MUST be able to click logo to reveal orbital widget launcher
- **FR-005**: Widget cards MUST orbit in circular arc around logo with proper spacing
- **FR-006**: System MUST integrate with existing widget docking system (already implemented)
- **FR-007**: System MUST auto-hide orbital launcher when other UI elements are used
- **FR-008**: All animations MUST use juice/elastic easing for game-like feel
- **FR-009**: System MUST maintain 720p baseline design with pixelated scaling
- **FR-010**: Menu buttons MUST slide in from offscreen using offset-based animation

### Key Entities

- **Seed Logo**: Central UI element serving dual purpose as brand identity and launcher button
- **Widget Cards**: Card-based representations of available widgets in orbital layout
- **Orbital Container**: Animation container managing circular arc positioning
- **Animation States**: Intro, Orbital Open, Orbital Closed, Menu Slide states

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Logo intro animation completes in under 4 seconds (matching Balatro timing)
- **SC-002**: Widget orbital animation feels responsive (under 0.5s to fully deploy)
- **SC-003**: All animations maintain 60fps during execution
- **SC-004**: System integrates seamlessly with existing transition and docking systems
- **SC-005**: UI remains fully functional on different screen resolutions through proper scaling

## Technical Integration Notes

### Existing Systems to Leverage
- **TransitionService**: Already handles parameter interpolation for animations
- **Widget Docking System**: Already implemented with snap-to-grid and visual feedback
- **Balatro Shader Background**: Can remain HD while UI scales pixelated
- **Event System**: Can coordinate timing between intro and orbital animations

### Balatro Research Findings
- **Logo materialization**: Uses dissolve effects and particle systems
- **Menu positioning**: Offset-based sliding with Y-axis animation
- **Timing system**: Context-dependent delays (4s for splash, 2-3s for transitions)
- **Card effects**: Elastic easing with swirl patterns toward center

### Design Philosophy
- **Non-responsive design**: Fixed aspect ratio like Balatro
- **Game-like scaling**: Pixelated scaling for performance and aesthetic
- **Central hub concept**: Logo as the primary interaction point
- **Auxiliary positioning**: Widgets dock to screen edges for organized layout