# PRD-02: GPU Shader Background

## Summary

A full-screen animated GPU shader background that renders the signature Balatro paint-mixing swirl effect. Uses SkiaSharp runtime shaders via Avalonia's Composition API. Supports 14+ configurable uniforms, 3-color theming, mouse parallax, and a secondary psychedelic overlay shader.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Controls/BalatroShaderBackground.cs` | Avalonia `Control` hosting a `CompositionCustomVisual` |
| `Constants/ShaderConstants.cs` | SKSL shader source code (2 shaders) |
| `Models/ShaderParameters.cs` | Shader parameter model |
| `Models/ShaderParametersConfig.cs` | Serializable config |
| `Models/ShaderParamMapping.cs` | Maps names to shader uniforms |
| `Helpers/ShaderPresetHelper.cs` | Built-in shader presets |

---

## Requirements

### R1 — Shader Renderer Control

- Custom Avalonia `Control` subclass
- Uses `CompositionCustomVisual` + `CompositionCustomVisualHandler`
- Accesses SkiaSharp canvas via `ISkiaSharpApiLeaseFeature`
- Compiles SKSL shaders at runtime via `SKRuntimeEffect.CreateShader`
- Handles attach/detach lifecycle (dispose shader builders on detach)
- Auto-resizes with `ArrangeOverride`
- Set `IsHitTestVisible="False"` so UI clicks pass through

### R2 — Main Shader (Balatro Paint-Mixing)

**Uniforms:**

| Uniform | Type | Default | Description |
|---------|------|---------|-------------|
| `resolution` | `float2` | window size | Viewport dimensions |
| `time` | `float` | elapsed * speed | Animation time |
| `spin_time` | `float` | elapsed * spin_speed | Spin animation time |
| `colour_1` | `float4` | (255,76,64) Red | Main/primary color |
| `colour_2` | `float4` | (0,147,255) Blue | Accent color |
| `colour_3` | `float4` | (30,43,45) Teal | Background color |
| `contrast` | `float` | 2.0 | Color contrast |
| `spin_amount` | `float` | 0.3 | Swirl intensity |
| `parallax_x` | `float` | 0.0 | Horizontal parallax offset |
| `parallax_y` | `float` | 0.0 | Vertical parallax offset |
| `zoom_scale` | `float` | 0.0 | Zoom level |
| `saturation_amount` | `float` | 0.0 | Primary saturation boost |
| `saturation_amount_2` | `float` | 0.0 | Secondary saturation boost |
| `pixel_size` | `float` | 1440.0 | Pixelation/posterization level |
| `spin_ease` | `float` | 0.5 | Spin easing factor |
| `loop_count` | `float` | 5.0 | Paint effect loop iterations (1-64) |

**Shader Features:**
- RGB-to-HSV and HSV-to-RGB color conversion
- Paint-like color blending with configurable loop count
- Center swirl animation with spin easing
- Pixelated/posterized effect
- Parallax offset (driven by mouse position)
- Saturation boost for color intensity
- Zoom effects

### R3 — Psychedelic Overlay Shader

A secondary shader that blends on top of the main shader when `psy_blend > 0`.

**Uniforms:**

| Uniform | Type | Default | Description |
|---------|------|---------|-------------|
| `resolution` | `float2` | window size | Viewport dimensions |
| `time` | `float` | shared with main | Animation time |
| `speed` | `float` | 1.0 | Animation speed |
| `fractal_complexity` | `float` | 1.0 | Fractal detail level |
| `color_cycle` | `float` | 1.0 | Color cycling speed |
| `kaleidoscope` | `float` | 0.0 | Kaleidoscope intensity |
| `fluid_flow` | `float` | 0.0 | Fluid motion intensity |
| `mouse` | `float2` | (0.5, 0.5) | Mouse position (normalized) |
| `melody` | `float` | 0.0 | Melody audio band |
| `chords` | `float` | 0.0 | Chords audio band |
| `bass` | `float` | 0.0 | Bass audio band |

**Blending:** Alpha-blended over the main shader using `psy_blend` (0.0-1.0).

### R4 — Animation Loop

- `Stopwatch`-based timing (elapsed seconds)
- `time = elapsedSeconds * _timeSpeed`
- `spin_time = elapsedSeconds * _spinTimeSpeed`
- Uses `RegisterForNextAnimationFrameUpdate()` for vsync-aligned rendering
- `OnAnimationFrameUpdate` → `Invalidate()` → `OnRender`
- Pause/resume via `AnimationEnabled` property (stops registering for frames)

### R5 — Public API

```csharp
// Animation
bool AnimationEnabled { get; set; }

// Time speeds (multipliers, not absolute)
void SetTime(float speedMultiplier);
void SetSpinTime(float speedMultiplier);

// Colors (SKColor)
void SetMainColor(SKColor color);
void SetAccentColor(SKColor color);
void SetBackgroundColor(SKColor color);

// Effect parameters
void SetContrast(float value);
void SetSpinAmount(float value);
void SetParallax(float x, float y);
void SetZoomScale(float value);
void SetSaturationAmount(float value);
void SetSaturationAmount2(float value);
void SetPixelSize(float value);
void SetSpinEase(float value);
void SetLoopCount(float value);

// Psychedelic overlay
void SetPsychedelicBlend(float blend);  // 0-1
void SetPsychedelicSpeed(float speed);
void SetPsychedelicComplexity(float value);
void SetPsychedelicColorCycle(float value);
void SetPsychedelicKaleidoscope(float value);
void SetPsychedelicFluidFlow(float value);

// Getters for current values (for transition snapshots)
float GetTimeSpeed();
float GetSpinTimeSpeed();
float GetContrast();
// ... etc for all params
```

### R6 — Preset System

- `ShaderPresetHelper` provides named presets (e.g., "Classic Red", "Ocean Blue", "Neon Purple")
- Each preset is a `ShaderParameters` bundle (all uniform values + colors)
- Presets are loadable by name
- Support saving/loading custom presets to JSON

### R7 — Performance

- Shader compilation happens once (lazy init on first render)
- Log shader compilation errors via `DebugLogger`
- Shader builder is reused across frames (only uniforms update)
- Dispose shader builders on detach to prevent GPU leaks
- Skip render if bounds are zero

---

## Acceptance Criteria

- [ ] Shader renders the Balatro paint-swirl effect at 60fps
- [ ] All 14 uniforms are individually controllable
- [ ] 3-color theming works (main, accent, background)
- [ ] Parallax responds to mouse position
- [ ] Psychedelic overlay blends correctly when enabled
- [ ] Animation pauses/resumes cleanly
- [ ] Preset system loads/saves shader parameter bundles
- [ ] No GPU memory leaks on window close or view detach
- [ ] Graceful fallback if shader compilation fails
