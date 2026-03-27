# PRD-09: Widget System

## Summary

A desktop widget framework allowing draggable, minimizable, closable overlay panels that float above the main content. Widgets persist their positions, can pop out to standalone windows, and are managed through a dock/picker UI. The system supports both cross-platform widgets and desktop-only widgets.

---

## Current Implementation (Legacy Reference)

### Framework

| File | Role |
|------|------|
| `Components/Widgets/BaseWidget.axaml` | Base widget template |
| `Components/Widgets/BaseWidgetControl.cs` | Pure C# base class |
| `Components/Widgets/WidgetHeader.axaml` | Title bar (title + minimize + close) |
| `Components/WidgetDock.axaml` | Widget picker dock popup |
| `ViewModels/BaseWidgetViewModel.cs` | Base VM (position, visibility, docking) |
| `Behaviors/DraggableWidgetBehavior.cs` | Drag with inertia physics |
| `Services/WidgetPositionService.cs` | Position persistence |
| `Services/WidgetWindowManager.cs` | Pop-out window management |
| `Windows/WidgetWindow.axaml` | Pop-out widget window |

### Widget Implementations (Cross-Platform)

| Widget | File | Purpose |
|--------|------|---------|
| Search | `SearchWidget.axaml` | Minimized search progress monitor |
| Genie | `GenieWidget.axaml` | AI-powered filter generation |
| EventFX | `EventFXWidget.axaml` | Event animation configuration |
| DayLatro | `DayLatroWidget.axaml` | Daily challenge seed |
| DailyRitual | `DailyRitualWidget.axaml` | Daily ritual tracker |

### Widget Implementations (Desktop-Only)

| Widget | File | Purpose |
|--------|------|---------|
| AudioMixer | `AudioMixerWidget.axaml` | Audio stem mixer |
| MusicMixer | `MusicMixerWidget.axaml` | Music mix presets |
| AudioVisualizer | `AudioVisualizerSettingsWidget.axaml` | Visualizer config |
| FrequencyDebug | `FrequencyDebugWidget.axaml` | FFT frequency display |
| TransitionDesigner | `TransitionDesignerWidget.axaml` | Shader transition designer |
| ApiHost | `ApiHostWidget.axaml` | API host config |

---

## Requirements

### R1 — BaseWidget Framework

Every widget inherits from `BaseWidget` and gets:

**Visual Structure:**
```
┌─────────────────────────────┐
│ WidgetHeader                │
│ [Title]    [_] [□] [×]      │
├─────────────────────────────┤
│                             │
│     Widget Content          │
│     (custom per widget)     │
│                             │
└─────────────────────────────┘
```

**WidgetHeader buttons:**
- Minimize (collapse to header only)
- Pop-out (open in standalone window)
- Close (hide widget)

**Base Properties (BaseWidgetViewModel):**
- `IsVisible` — show/hide
- `IsMinimized` — collapsed state
- `PositionX`, `PositionY` — canvas position
- `Width`, `Height` — widget size
- `ZIndex` — layering order
- `Title` — header text
- `WidgetId` — unique identifier for persistence

### R2 — Draggable Behavior

`DraggableWidgetBehavior` provides:
- Click-and-drag on header to move
- Physics-based inertia (widget slides after release)
- Velocity decay (exponential)
- Boundary clamping (stay within window bounds)
- Snapping to edges (optional)
- Double-click header to minimize/restore

### R3 — Position Persistence

`WidgetPositionService`:
- Save widget positions to JSON on move
- Restore positions on app startup
- Per-widget position keyed by `WidgetId`
- Default positions for first launch
- Reset positions to defaults command

### R4 — Widget Dock (Picker)

- Grid of widget icons/thumbnails
- Toggle widget visibility by clicking
- Active widgets highlighted
- Popup anchored to dock button in right dock bar
- Light-dismiss

### R5 — Widget Canvas Layer

- `Grid` with `ZIndex="10"` above main content
- `IsHitTestVisible="False"` on container (clicks pass through)
- Individual widgets have `IsHitTestVisible="True"` (clickable)
- `ClipToBounds="False"` to allow widgets near edges
- Widgets added dynamically (search widgets) or statically (known widgets)

### R6 — Pop-Out Windows

`WidgetWindowManager`:
- Any widget can pop out to a standalone `WidgetWindow`
- Window contains just the widget content
- Closing the window returns widget to canvas
- Window size matches widget size
- Multiple widget windows supported simultaneously

### R7 — Toggle All Widgets

- `ToggleAllWidgetsCommand` shows/hides all widgets at once
- Icon changes based on state (show all / hide all)
- Individual widget visibility remembered when toggling back on

### R8 — Desktop-Only Widgets

- Audio/music widgets only available on desktop platform
- Added programmatically by `DesktopAppExtensions` on Desktop startup
- Hidden on Browser/iOS/Android builds
- Use `IPlatformServices` to check platform capabilities

### R9 — Search Widget Specifics

- Dynamically created per active search
- Shows: progress bar, result count, seed range, cancel button
- Click to re-open search modal
- Auto-removed when search completes/cancels
- Multiple search widgets can coexist
- `ToggleSearchWidgetsCommand` shows/hides all search widgets

### R10 — Widget Implementations Summary

| Widget | Key Features |
|--------|-------------|
| **SearchWidget** | Progress bar, result count, cancel, re-open modal |
| **GenieWidget** | Text input, AI generate, preview, save filter |
| **EventFXWidget** | Effect toggles, sensitivity sliders, preview |
| **DayLatroWidget** | Today's seed, analyze button, copy seed, high scores |
| **DailyRitualWidget** | Daily tasks, streak tracking, completion status |
| **AudioMixerWidget** | 4 stem sliders, master volume, presets |
| **MusicMixerWidget** | Mix preset browser, apply, save custom |
| **AudioVisualizerWidget** | Reactivity settings, trigger config |
| **FrequencyDebugWidget** | Real-time FFT bar graph |
| **TransitionDesignerWidget** | Start/end preset picker, duration, scrub, save |
| **ApiHostWidget** | API URL, status indicator, connect/disconnect |

---

## Acceptance Criteria

- [ ] Widgets are draggable with inertia
- [ ] Widget positions persist across app restarts
- [ ] Widget dock shows all available widgets with toggle
- [ ] Minimize collapses to header only
- [ ] Pop-out creates standalone window
- [ ] Toggle all widgets shows/hides everything
- [ ] Search widgets are created/destroyed dynamically
- [ ] Desktop-only widgets hidden on non-desktop platforms
- [ ] Widgets stay within window bounds
- [ ] Multiple widgets can overlap with correct Z-ordering
