# Music Visualizer Architecture

## Two Input Systems

### 1. **TRIGGERS** (Event-Based)
Discrete events that fire when conditions are met.

**Examples:**
- Beat detected (bass kick)
- High score achieved
- Seed found
- Drop/breakdown in music

**Implementation:**
```csharp
// Event fired when trigger condition met
public event EventHandler<TriggerEventArgs>? BeatDetected;

// Shader can subscribe and react
BeatDetected += (s, e) => {
    // Trigger flash effect
    // Pulse animation
    // Color change
};
```

**Use Cases:**
- Flash on beat
- Particle burst on drop
- Screen shake on heavy bass
- Color shift on transition

---

### 2. **LIVE VALUES** (Continuous Streams)
Real-time audio data plugged directly into shader uniforms.

**Examples:**
- Bass frequency magnitude (0-1)
- Treble level
- Overall volume
- Specific frequency band (e.g., 250Hz-2000Hz)

**Implementation:**
```csharp
// Continuously updated values
public float BassLevel { get; private set; } // 0.0 - 1.0
public float MidLevel { get; private set; }
public float HighLevel { get; private set; }

// Plugged into shader every frame
shader.SetUniform("uBassLevel", BassLevel);
shader.SetUniform("uMidLevel", MidLevel);
shader.SetUniform("uHighLevel", HighLevel);
```

**Use Cases:**
- Background wobble intensity = bass level
- Color saturation = treble level
- Bloom amount = overall volume
- Distortion = mid frequencies

---

## Current State

### Existing Code
- `SoundFlowAudioManager` - Handles 8-track audio playback
- `FrequencyDebugWidget` - Shows real-time frequency bands (Bass/Mid/High)
- Audio event triggers exist but may not be wired up properly

### What Needs Work
1. **Clean trigger system** - Simple events for beat/drop/etc
2. **Live value pipeline** - Continuously feed audio data to shader
3. **Configuration UI** - Let user map triggers and values to effects

---

## Proposed Design

### Trigger System
```csharp
public class AudioTriggerManager
{
    // Events
    public event EventHandler? BassKick;
    public event EventHandler? SnareHit;
    public event EventHandler? Drop;

    // Configuration
    public float BassKickThreshold { get; set; } = 0.7f;
    public float SnareThreshold { get; set; } = 0.6f;

    // Detection logic runs every frame
    public void Update(float bassLevel, float midLevel, float highLevel)
    {
        if (bassLevel > BassKickThreshold)
            BassKick?.Invoke(this, EventArgs.Empty);

        // etc...
    }
}
```

### Live Value System
```csharp
public class AudioValueStreamer
{
    // Current values (updated every frame)
    public float Bass { get; private set; }
    public float Mid { get; private set; }
    public float High { get; private set; }
    public float Volume { get; private set; }

    // Smoothing/filtering
    public float SmoothingFactor { get; set; } = 0.1f;

    public void Update(float[] fftData)
    {
        // Process FFT data into useful values
        Bass = CalculateBass(fftData);
        Mid = CalculateMid(fftData);
        High = CalculateHigh(fftData);

        // Apply smoothing to prevent jitter
        Bass = Lerp(previousBass, Bass, SmoothingFactor);
    }
}
```

### Integration with Shader
```csharp
public class VisualizerShader
{
    private AudioValueStreamer _audioValues;
    private AudioTriggerManager _triggers;

    public void Initialize()
    {
        // Subscribe to triggers
        _triggers.BassKick += OnBassKick;
    }

    public void Update()
    {
        // Continuously feed live values
        SetUniform("uBassLevel", _audioValues.Bass);
        SetUniform("uMidLevel", _audioValues.Mid);
        SetUniform("uHighLevel", _audioValues.High);

        // Shader uses these in calculations
    }

    private void OnBassKick(object? sender, EventArgs e)
    {
        // Trigger one-shot effect
        TriggerFlash();
    }
}
```

---

## Widget Configuration

### Visualizer Settings Widget
```
┌─────────────────────────────────────┐
│ 🎨 VISUALIZER                       │
├─────────────────────────────────────┤
│ LIVE VALUES (Continuous)            │
│ ☑ Bass → Background Wobble          │
│ ☑ Mid → Color Saturation            │
│ ☑ High → Particle Speed             │
│                                     │
│ TRIGGERS (Events)                   │
│ ☑ Beat Detection → Flash           │
│   Threshold: ▓▓▓▓▓▓▓░░░ [0.7]      │
│ ☑ Drop Detection → Screen Shake     │
│   Threshold: ▓▓▓▓▓▓░░░░ [0.6]      │
└─────────────────────────────────────┘
```

---

## Implementation Priority

1. **Phase 1:** Get live audio values streaming properly
   - Fix FFT data extraction
   - Smooth values
   - Expose Bass/Mid/High properties

2. **Phase 2:** Wire up shader uniforms
   - Pass live values to shader every frame
   - Test with simple effects (wobble, color shift)

3. **Phase 3:** Implement trigger system
   - Beat detection algorithm
   - Event firing
   - Connect to shader effects

4. **Phase 4:** Build configuration UI
   - Map live values to effects
   - Configure trigger thresholds
   - Save/load presets

---

## Notes

- **Separation of concerns:** Triggers and live values are independent systems
- **Performance:** Live values update every frame (60+ FPS), triggers fire occasionally
- **Flexibility:** User can enable/disable and configure both systems independently
- **Simplicity:** Start simple, add complexity only as needed

---

*This document will guide the visualizer implementation.*
