# PRD-01: App Shell & Main Window

## Summary

The application shell is the root container that hosts the entire UI. It provides the main window chrome, the Balatro-styled main menu with its bottom dock bar, the layered Z-index architecture (background → content → widgets → modals), and the top-level navigation commands.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Views/MainWindow.axaml` | Root window (1582x830), drag-drop enabled, attribution bar |
| `Views/BalatroMainMenu.axaml` | Main UI container with all layers |
| `ViewModels/MainWindowViewModel.cs` | Window state, modal hosting, vibe-out mode |
| `ViewModels/BalatroMainMenuViewModel.cs` | Menu state, commands, audio, widget toggles |
| `Controls/ErrorBoundary.cs` | Error boundary wrapper |
| `Controls/MaximizeButton.axaml` | Custom maximize button |

---

## Requirements

### R1 — Window Setup

- Default size: 1582 x 830 (16:9-ish, fits Balatro aesthetic)
- Accept file drag-and-drop at the window level (`DragDrop.AllowDrop`)
- Custom title bar / chrome optional (current uses default + custom maximize)
- South-edge highlight border (2px `BrightSilver`)
- Dark theme only (Avalonia `RequestedThemeVariant="Dark"`)

### R2 — Layer Architecture

The main menu uses a `Grid` with overlapping layers controlled by `ZIndex`:

| Layer | ZIndex | Content |
|-------|--------|---------|
| Background | 0 | `BalatroShaderBackground` (hit-test disabled) |
| Main Content | 5 | Title + bottom dock bar (hidden in Vibe Out Mode) |
| Widget Canvas | 10 | Desktop widgets (hit-test disabled on container, enabled on widgets) |
| Modal Overlay | 100 | Dimmer + active modal content |

### R3 — Bottom Dock Bar (3-Column Layout)

```
[Left Dock]          [Center Dock]              [Right Dock]
Author Name     SEARCH  DESIGNER  ANALYZER  SETTINGS    [icons...]
```

**Left Dock:**
- Author name label + inline edit button
- Click to toggle display/edit mode
- Enter key or lost-focus commits edit
- Persisted via `UserProfileService`

**Center Dock (Main Buttons):**

| Button | Style Class | Command | Target |
|--------|-------------|---------|--------|
| SEARCH | `btn-green` | `SeedSearchCommand` | Opens SearchModal |
| DESIGNER | `btn-blue` | `EditorCommand` | Opens FiltersModal |
| ANALYZER | `btn-purple` | `AnalyzeCommand` | Opens AnalyzeModal |
| SETTINGS | `btn-orange` | `ToolCommand` | Opens ToolsModal/SettingsModal |

- Font: BalatroFont, sizes 28-36
- MinHeight: 46-60px
- Drop shadow beneath dock panel

**Right Dock (Icon Buttons, all `btn-purple` 48x48):**

| Button | Icon | Command | Behavior |
|--------|------|---------|----------|
| Widget Dock | Grid | `ToggleWidgetDockCommand` | Opens widget picker popup |
| Toggle All Widgets | Widgets | `ToggleAllWidgetsCommand` | Show/hide all widgets |
| Animation Toggle | Play/Pause | `AnimationToggleCommand` | Pause/resume shader |
| Music Toggle | Volume | `MusicToggleCommand` | Opens volume popup |
| Search Widgets | Magnify | `ToggleSearchWidgetsCommand` | Show/hide search widgets |

### R4 — Volume Popup

- Triggered by music toggle button
- `Popup` with `Placement="Top"`, light-dismiss
- Contains: "VOLUME" label, vertical slider (0-100), percentage text, mute button
- Bound to `Volume`, `VolumePercentText`, `MuteCommand`, `MuteButtonText`

### R5 — Attribution Bar

- Bottom of window, hidden in Vibe Out Mode
- Text: "Not affiliated with LocalThunk or Playstack" + "Buy Balatro" link + heart icon + "Created with love for the Balatro community"
- Dark background, centered horizontal stack

### R6 — Vibe Out Mode

- Toggle that hides all main content UI, showing only shader background + widgets
- `IsVibeOutMode` property on ViewModel
- MainContent `IsVisible` bound to `!IsVibeOutMode`
- Attribution bar hidden

### R7 — Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Open seed search |

### R8 — Error Boundary

- `ErrorBoundary` control wraps main content
- Catches unhandled exceptions in the visual tree
- Displays error state instead of crashing

---

## Acceptance Criteria

- [ ] Window opens at correct default size with dark theme
- [ ] All 4 main menu buttons open their respective modals
- [ ] Right dock icon buttons toggle their respective states
- [ ] Author name is editable inline with persistence
- [ ] Volume popup appears/dismisses correctly
- [ ] Vibe Out Mode hides all content except background
- [ ] Ctrl+F opens search
- [ ] File drag-and-drop accepted at window level
- [ ] Error boundary catches and displays errors gracefully
