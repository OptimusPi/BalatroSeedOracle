# PRD-04: Shader Reactivity & EventFX

## Summary

The reactivity layer connects audio analysis (PRD-03) to shader parameters (PRD-02), making the background visually respond to music in real-time. Includes beat detection, drop detection, frequency breakpoints, and physics-based inertia for smooth parameter decay.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Services/VisualizerEventManager.cs` | Beat/drop/frequency event detection and dispatch |
| `Services/EventFXService.cs` | Maps audio events to shader effects |
| `Services/ShaderInertiaManager.cs` | Physics-based inertia/decay for shader params |
| `Services/TriggerService.cs` | Audio trigger point management |
| `Models/AudioTriggerPoint.cs` | Trigger point definition |
| `Models/ShaderParamMapping.cs` | Maps triggers to shader parameters |
| `Models/VisualizerPreset.cs` | Preset with reactivity mappings |
| `Models/VisualizerPresetNew.cs` | New preset format with trigger mappings |
| `ViewModels/EventFXWidgetViewModel.cs` | EventFX configuration UI state |
| `ViewModels/AudioVisualizerSettingsWidgetViewModel.cs` | Visualizer settings UI |

---

## Requirements

### R1 — VisualizerEventManager

Processes raw audio data and emits high-level events:

**Events:**

```csharp
event EventHandler<BeatDetectedEventArgs>? BeatDetected;
event EventHandler<DropDetectedEventArgs>? DropDetected;
event EventHandler<FrequencyBreakpointEventArgs>? FrequencyBreakpointHit;
```

**BeatDetectedEventArgs:**
- `float Intensity` — beat strength (0.0-1.0)
- `int AudioSource` — which stem triggered (1=Drums, 2=Bass, 3=Chords, 4=Melody)
- `DateTime Timestamp`

**DropDetectedEventArgs:**
- `float Intensity` — drop impact strength (0.0-1.0)
- `float FrequencyHz` — frequency band that triggered the drop
- `DateTime Timestamp`

**FrequencyBreakpointEventArgs:**
- `string BreakpointId` / `string BreakpointName`
- `float Amplitude` — current amplitude
- `string EffectName` — which shader effect to trigger
- `float EffectIntensity` — how strong
- `float EffectDurationMs` — how long

### R2 — Beat Detection Algorithm

- Analyze amplitude envelope per stem
- Compare current amplitude to running average
- Beat = amplitude exceeds threshold above average
- Configurable sensitivity per stem
- Cooldown period to prevent double-triggers
- Support different beat patterns (4/4, 3/4, etc.)

### R3 — EventFX Service

Maps detected events to shader parameter changes:

**Effect Types:**

| Effect | Shader Param(s) | Description |
|--------|-----------------|-------------|
| Shadow Flicker | `contrast` | Brief contrast pulse on beat |
| Spin Burst | `spin_amount` | Spin increases momentarily |
| Twirl | `spin_time` speed | Spin time accelerates |
| Zoom Thump | `zoom_scale` | Zoom in/out on bass hit |
| Color Saturation | `saturation_amount` | Color intensity pulse |
| Parallax Shake | `parallax_x/y` | Subtle position shake |
| Psy Blend | `psy_blend` | Psychedelic overlay flash |

**Source Tracking:**
- Each effect tracks which system is controlling it (audio vs. manual vs. transition)
- Prevents conflicts when multiple systems try to set the same parameter
- Priority: manual > transition > audio reactivity

### R4 — ShaderInertiaManager

Physics-based smooth decay for shader parameters:

```csharp
public class ShaderInertiaManager
{
    // Two modes:
    // SetValue — directly set a parameter (used by transitions, manual control)
    // AddInertia — add impulse that decays over time (used by audio events)

    void SetValue(string param, float value);
    void AddInertia(string param, float impulse, float decayRate);
    float GetCurrentValue(string param);
    void Update(float deltaTime);  // Called per frame
}
```

**Physics:**
- Velocity-based decay (exponential falloff)
- Configurable decay rate per parameter
- Clamping to min/max ranges
- Drift prevention (snap to zero below threshold)
- Smooth interpolation between impulses

### R5 — Audio Trigger Points

User-configurable triggers that fire effects at specific frequency/amplitude thresholds:

```csharp
public class AudioTriggerPoint
{
    string Id;
    string Name;
    FrequencyBand Band;        // Which frequency band to monitor
    float Threshold;            // Amplitude threshold (0.0-1.0)
    string EffectName;          // Which effect to trigger
    float EffectIntensity;      // How strong (0.0-1.0)
    float EffectDurationMs;     // Duration
    float CooldownMs;           // Min time between triggers
}
```

### R6 — Visualizer Presets

Bundle of trigger points + shader parameter mappings:

- Each preset defines which audio events map to which shader effects
- Includes default shader parameter values (base state)
- Includes reactivity sensitivity settings
- Save/load to JSON in `VisualizerPresets/` directory

### R7 — EventFX Widget

Configuration UI for real-time tuning:

- Toggle individual effects on/off
- Adjust sensitivity per effect
- Adjust decay rate per effect
- Preview effects without audio
- Save current config as preset

---

## Acceptance Criteria

- [ ] Beat detection fires reliably on drum hits
- [ ] Drop detection catches bass drops
- [ ] Shader parameters respond visually to music in real-time
- [ ] Inertia manager provides smooth decay (no jerky snapping)
- [ ] Multiple simultaneous effects don't conflict
- [ ] Source tracking prevents audio from overriding manual control
- [ ] Trigger points are user-configurable
- [ ] Visualizer presets save/load correctly
- [ ] Effects can be individually toggled on/off
- [ ] System is no-op when audio is muted/unavailable
