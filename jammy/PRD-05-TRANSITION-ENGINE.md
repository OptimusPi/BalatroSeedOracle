# PRD-05: Transition Engine

## Summary

The transition engine smoothly interpolates between shader parameter states over time. Used for app startup animations, search progress visualization, preset switching, and any scenario where shader parameters need to LERP from state A to state B.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Services/TransitionService.cs` | Core LERP engine with timer |
| `Services/SearchTransitionManager.cs` | Search lifecycle transitions |
| `Models/TransitionPreset.cs` | Named transition presets |
| `Models/VisualizerPresetTransition.cs` | Start/end params + progress |
| `Helpers/TransitionPresetHelper.cs` | Built-in transition presets |
| `ViewModels/TransitionDesignerWidgetViewModel.cs` | Designer UI state |
| `Desktop/Components/Widgets/TransitionDesignerWidget.axaml` | Desktop-only designer widget |

---

## Requirements

### R1 — TransitionService Core

```csharp
public class TransitionService
{
    bool IsTransitionActive { get; }
    float? CurrentProgress { get; }  // 0.0 to 1.0

    // Start a transition between two shader states
    void StartTransition(
        ShaderParameters startParams,
        ShaderParameters endParams,
        Action<ShaderParameters> applyCallback,
        TimeSpan? duration = null  // null = manual progress control
    );

    // Manual progress control (for search progress, loading, etc.)
    void SetProgress(float progress);  // 0.0 to 1.0

    // Stop/cancel
    void StopTransition();
}
```

### R2 — Two Transition Modes

**Time-Based (Auto):**
- Specify a `duration` (e.g., 3 seconds)
- Uses `DispatcherTimer` to advance progress
- Progress auto-advances from 0.0 to 1.0 over duration
- Callback fires with interpolated params each tick
- Used for: app startup, preset switching

**Progress-Driven (Manual):**
- No duration specified
- External code calls `SetProgress(float)` to drive the transition
- Used for: search progress bar, sprite loading progress

### R3 — Interpolation

- Linear interpolation (LERP) between all `ShaderParameters` fields:
  - `float` params: `start + (end - start) * progress`
  - `SKColor` params: per-channel LERP (R, G, B independently)
- All parameters interpolated simultaneously

**ShaderParameters fields to interpolate:**
- TimeSpeed, SpinTimeSpeed
- MainColor (R,G,B), AccentColor (R,G,B), BackgroundColor (R,G,B)
- Contrast, SpinAmount
- ParallaxX, ParallaxY
- ZoomScale
- SaturationAmount, SaturationAmount2
- PixelSize, SpinEase, LoopCount

### R4 — Single Active Transition

- Only one transition can be active at a time
- Starting a new transition cancels/replaces any existing one
- `StopTransition()` cancels and leaves params at current interpolated state

### R5 — Search Transition Manager

Manages shader transitions tied to search lifecycle:

| Event | Transition |
|-------|-----------|
| Search starts | Transition from current state → "searching" preset (e.g., energetic colors) |
| Search progress | Drive transition progress from 0% → 100% matching search progress |
| Search completes | Transition from "searching" → "results" preset |
| Search cancelled | Transition back to default preset |
| Modal closes | Transition back to idle preset |

### R6 — Transition Presets

Named presets defining start/end shader states:

```json
{
  "name": "Startup Fade In",
  "start": { "contrast": 0.0, "spinAmount": 0.0, "mainColor": "#000000" },
  "end": { "contrast": 2.0, "spinAmount": 0.3, "mainColor": "#FF4C40" },
  "duration": 3.0,
  "easing": "ease-in-out"
}
```

- Built-in presets via `TransitionPresetHelper`
- User-created presets saved to JSON
- Future: support easing functions (linear, ease-in, ease-out, ease-in-out, bounce)

### R7 — Transition Designer Widget (Desktop Only)

- Live preview of transitions
- Scrub progress manually
- Pick start/end presets
- Set duration
- Test play/reverse
- Save as named preset

---

## Acceptance Criteria

- [ ] Time-based transitions complete over specified duration
- [ ] Progress-driven transitions respond to external progress updates
- [ ] All shader parameters interpolate smoothly (no jumps)
- [ ] Color interpolation works per-channel
- [ ] Starting a new transition cancels the previous one
- [ ] Search lifecycle triggers correct transitions
- [ ] Transition presets load/save correctly
- [ ] Transition designer allows live scrubbing and preview
- [ ] No visual glitches at transition start/end boundaries
