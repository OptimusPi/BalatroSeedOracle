# Music Visualizer System Refactor - Product Requirements Document

**Date**: 2025-11-03
**Feature**: Refactor Music Visualizer into Three Independent Modular Components
**Status**: READY FOR IMPLEMENTATION
**Priority**: HIGH - Required for release

---

## Executive Summary

Refactor the Music Visualizer system from a monolithic widget into three independent, modular components with proper separation of concerns. Each component has a single responsibility and saves its own configuration independently.

---

## The Three Independent Systems

### 1. FFT Window (Audio Analysis)
**Purpose**: Analyze audio frequency bands per track and define trigger points
**Responsibility**: ONLY audio threshold detection - completely agnostic of shaders/visuals
**Saves To**: `visualizer/audio_triggers/{name}.json`

**Current State**:
- FrequencyDebugWidget exists but doesn't have trigger point creation UI
- Trigger Point Creator is currently in AudioVisualizerSettingsWidget (WRONG PLACE)

**Needs**:
- Move Trigger Point Creator UI from AudioVisualizerSettingsWidget to FrequencyDebugWidget
- UI Elements:
  - Track selector dropdown (Bass1, Bass2, Drums1, Drums2, Chords1, Chords2, Melody1, Melody2)
  - Frequency band selector (Low, Mid, High)
  - Threshold slider with current value display
  - LED indicator (already works!)
  - "Save Trigger Point" button
- Save AudioTriggerPoint to JSON
- Load existing trigger points

### 2. Audio Mixer Widget
**Purpose**: Mix volume, pan, and track selection for current scene
**Responsibility**: ONLY audio mixing configuration
**Saves To**: `visualizer/audio_mixes/{name}.json`

**Current State**:
- Exists but has poor styling - too much vertical space
- Only has volume controls
- No pan controls
- No save/load functionality

**Needs**:
- Redesign with compact line-by-line layout:
  ```
  [Bass1    ] [M][S] [========|====] [====|====]
                ^  ^   Volume Slider   Pan Slider
              Mute Solo
  ```
- Each track gets ONE line with:
  - Label (track name, left-aligned, fixed width)
  - Mute button [M]
  - Solo button [S]
  - Volume slider (horizontal, takes most space)
  - Pan slider (horizontal, L/R balance)
- Add "Save Mix" button
- Add "Load Mix" dropdown
- Save/Load MusicMixPreset JSON

### 3. Visualizer Settings Widget
**Purpose**: Configure shader parameters and map triggers to shader effects
**Responsibility**: ONLY shader state and trigger→effect mappings
**Saves To**: `visualizer/visualizer_presets/{name}.json`

**Current State**:
- Has Trigger Point Creator UI (NEEDS TO BE REMOVED - moved to FFT Window)
- Has shader parameter controls (GOOD - keep these)
- Has effect testing buttons (GOOD - keep these)
- Has "Effect to Trigger Mapping" section (GOOD - keep and enhance)

**Needs**:
- REMOVE Trigger Point Creator section (lines 276-406 in AXAML)
- KEEP shader parameter sliders
- KEEP effect test buttons
- ENHANCE "Effect to Trigger Mapping":
  - Dropdown should list ALL saved AudioTriggerPoints
  - Add EffectMode selector: "Set Value" vs "Add Inertia"
  - Add Inertia Decay slider (for "alive" feel)
- Save/Load VisualizerPreset JSON

---

## Core Models

### ITrigger Interface
```csharp
public interface ITrigger
{
    string Name { get; set; }
    string TriggerType { get; } // "Audio", "Mouse", "GameEvent"
    bool IsActive();
    float GetIntensity(); // 0-1 or custom range
}
```

### AudioTriggerPoint Model
```csharp
public class AudioTriggerPoint : ITrigger
{
    public string Name { get; set; } // Auto-generated: "Bass1Mid63"
    public string TriggerType => "Audio";
    public string TrackName { get; set; } // "Bass1"
    public string TrackId { get; set; } // "bass1"
    public string FrequencyBand { get; set; } // "Low", "Mid", "High"
    public double ThresholdValue { get; set; } // 0-100

    // Runtime evaluation
    public bool IsActive() { /* Check if current band value > threshold */ }
    public float GetIntensity() { /* Return current band value */ }
}
```

### MusicMixPreset Model
```csharp
public class MusicMixPreset
{
    public string Name { get; set; }
    public Dictionary<string, TrackMixSettings> Tracks { get; set; }
}

public class TrackMixSettings
{
    public string TrackName { get; set; }
    public float Volume { get; set; } // 0-1
    public float Pan { get; set; } // -1 (left) to 1 (right)
    public bool Muted { get; set; }
    public bool Solo { get; set; }
}
```

### VisualizerPreset Model
```csharp
public class VisualizerPreset
{
    public string Name { get; set; }
    public Dictionary<string, float> DefaultShaderParams { get; set; }
    public List<ShaderParamMapping> TriggerMappings { get; set; }
}

public class ShaderParamMapping
{
    public string ShaderParam { get; set; } // "ZoomScale", "Contrast", "SpinAmount", etc.
    public string TriggerName { get; set; } // References AudioTriggerPoint.Name
    public EffectMode Mode { get; set; } // SetValue or AddInertia
    public float InertiaDecay { get; set; } // 0-1, for "alive" feel (friction)
    public float Multiplier { get; set; } // Scale trigger intensity
}

public enum EffectMode
{
    SetValue,    // Directly set shader param to trigger intensity
    AddInertia   // Add "force" to param with decay (feels alive!)
}
```

### Future Extensibility Examples
```csharp
public class MouseParallaxTrigger : ITrigger
{
    public string Name { get; set; }
    public string TriggerType => "Mouse";
    public bool IsActive() => true; // Always active
    public float GetIntensity() => MouseX / ScreenWidth;
}

public class GameEventTrigger : ITrigger
{
    public string Name { get; set; }
    public string TriggerType => "GameEvent";
    public string EventName { get; set; } // "SeedFound", "SearchComplete"
    public bool IsActive() { /* Check event state */ }
    public float GetIntensity() => 1.0f;
}
```

---

## File Structure

```
visualizer/
├── audio_triggers/
│   ├── Bass1Mid63.json
│   ├── Drums1High75.json
│   └── Melody1Low40.json
├── audio_mixes/
│   ├── MainMenu.json
│   ├── SearchScreen.json
│   └── ResultsView.json
└── visualizer_presets/
    ├── Default.json
    ├── WaveRider.json
    └── Inferno.json
```

---

## Files to Modify

| File | Change Type | Description |
|------|-------------|-------------|
| `Models/ITrigger.cs` | CREATE | New interface for trigger system |
| `Models/AudioTriggerPoint.cs` | CREATE | Audio trigger model implementing ITrigger |
| `Models/MusicMixPreset.cs` | CREATE | Audio mix configuration model |
| `Models/TrackMixSettings.cs` | CREATE | Per-track mix settings |
| `Models/VisualizerPreset.cs` | CREATE | Visualizer preset model |
| `Models/ShaderParamMapping.cs` | CREATE | Trigger-to-shader mapping model |
| `Components/Widgets/FrequencyDebugWidget.axaml` | MODIFY | Add Trigger Point Creator UI |
| `ViewModels/FrequencyDebugWidgetViewModel.cs` | MODIFY | Add trigger point save/load logic |
| `Components/Widgets/AudioMixerWidget.axaml` | CREATE/MODIFY | Redesign with compact layout |
| `ViewModels/AudioMixerWidgetViewModel.cs` | CREATE/MODIFY | Add pan, mute, solo, save/load |
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | MODIFY | REMOVE Trigger Point Creator section |
| `ViewModels/AudioVisualizerSettingsWidgetViewModel.cs` | MODIFY | Remove trigger point logic, enhance mappings |
| `Services/TriggerService.cs` | CREATE | Centralized trigger evaluation and management |

---

## Implementation Order

### Phase 1: Core Models (30 min)
1. Create ITrigger interface
2. Create AudioTriggerPoint model
3. Create MusicMixPreset and TrackMixSettings models
4. Create VisualizerPreset and ShaderParamMapping models
5. Create TriggerService for centralized trigger management

### Phase 2: FFT Window (1 hour)
1. Move Trigger Point Creator UI from AudioVisualizerSettingsWidget to FrequencyDebugWidget
2. Update FrequencyDebugWidgetViewModel with save/load logic
3. Test trigger point creation and persistence

### Phase 3: Audio Mixer Widget (1.5 hours)
1. Redesign AudioMixerWidget AXAML with compact line layout
2. Add pan sliders
3. Add mute/solo buttons
4. Implement MusicMixPreset save/load
5. Test mixing functionality

### Phase 4: Visualizer Settings Widget (1 hour)
1. Remove Trigger Point Creator section from AudioVisualizerSettingsWidget
2. Update Effect-to-Trigger mapping to use saved AudioTriggerPoints
3. Add EffectMode selector (SetValue vs AddInertia)
4. Add Inertia Decay sliders
5. Implement VisualizerPreset save/load

### Phase 5: Inertia System (1 hour)
1. Implement inertia/decay math in shader param updates
2. Wire up AddInertia mode
3. Test "alive" feel with friction/decay

### Phase 6: Integration & Testing (1 hour)
1. Test complete workflow: FFT → Mixer → Visualizer
2. Verify all three systems work independently
3. Verify VisualizerPreset dependency on AudioTriggerPoints
4. Test save/load for all three preset types

**Total Estimated Time**: 6 hours

---

## Success Criteria

1. ✅ FFT Window can create and save AudioTriggerPoints independently
2. ✅ Audio Mixer Widget has compact layout with volume, pan, mute, solo
3. ✅ Audio Mixer can save/load MusicMixPresets
4. ✅ Visualizer Settings Widget no longer has Trigger Point Creator
5. ✅ Visualizer can map triggers to shader params with Set/Inertia modes
6. ✅ Visualizer can save/load VisualizerPresets
7. ✅ Inertia mode creates "alive" feel with decay
8. ✅ All three systems work independently
9. ✅ Complete workflow: Create triggers → Mix audio → Design visualizer
10. ✅ User can sit down and design presets in real-time

---

## Dependencies

- VisualizerPreset depends on AudioTriggerPoints existing
- If AudioTriggerPoint is deleted, VisualizerPreset will have broken reference
- This is acceptable for release (user responsibility to manage files)

---

## Architecture Benefits

### Separation of Concerns
- **FFT**: Audio analysis only - doesn't know about shaders
- **Mixer**: Audio mixing only - doesn't know about triggers or shaders
- **Visualizer**: Shader control only - references triggers but doesn't create them

### Modularity
- Each system saves its own JSON independently
- Easy to add new trigger types (Mouse, GameEvent) via ITrigger
- Easy to add new shader params
- Easy to create new presets for different scenes

### User Experience
- User can design each piece independently
- Creative workflow: Analyze audio → Mix tracks → Design visuals
- Real-time experimentation and saving
- Clean, focused UI for each task

---

**Status**: Ready for Implementation
**Complexity**: MEDIUM (refactoring existing code + new models)
**Risk**: LOW (isolated changes, no core functionality impact)
