# Enhanced Multi-Track Audio System - Product Requirements Document

**Date**: 2025-11-16
**Feature**: Powerliminals-Style Multi-Audio Player with Variable Speed Control
**Status**: ENHANCEMENT - BUILD ON EXISTING 8-TRACK SYSTEM
**Priority**: MEDIUM - Audio Experience Polish

---

## Executive Summary

Enhance the existing **SoundFlowAudioManager** (8-track system) with advanced playback features inspired by **Powerliminals Player**:
- ✅ **Variable Playback Speed** per track (0.25x - 4.0x)
- ✅ **Time-Stretch Algorithm** (maintain pitch while changing speed)
- ✅ **Independent Track Timing** (offset/delay per track)
- ✅ **Advanced Mixing Controls** (EQ, filters, effects per track)
- ✅ **Preset Management** (save/load complex audio setups)

### Use Cases:
1. **Ambient Soundscapes**: Layer tracks at different speeds for evolving audio
2. **Binaural Beats**: Precise frequency control for focus/relaxation
3. **Creative Audio Design**: Experiment with unconventional mixes
4. **Accessibility**: Speed up/slow down audio for different listening preferences

---

## Current State

### Existing Features (SoundFlowAudioManager):
```csharp
✅ 8 independent tracks (Bass1, Bass2, Drums1, Drums2, Chords1, Chords2, Melody1, Melody2)
✅ Volume control per track (0-100%)
✅ Pan control per track (L-R)
✅ Mute/Solo per track
✅ Synchronized playback
✅ Audio reactive visualizer triggers
```

### What's Missing:
```csharp
❌ Variable playback speed per track
❌ Time-stretching (pitch-independent speed)
❌ Track offset/delay
❌ Per-track effects (reverb, EQ, filters)
❌ Advanced preset system
```

---

## Proposed Enhancements

### 1. Variable Playback Speed

**What**: Control playback speed of individual tracks (0.25x to 4.0x)

**UI**:
```xml
<Grid ColumnDefinitions="120,80,*,60">
    <TextBlock Grid.Column="0" Text="Bass1"/>
    <TextBlock Grid.Column="1" Text="Speed:" Foreground="Gray"/>
    <Slider Grid.Column="2"
            Minimum="0.25"
            Maximum="4.0"
            Value="{Binding Bass1PlaybackSpeed}"
            StepFrequency="0.05"/>
    <TextBlock Grid.Column="3"
               Text="{Binding Bass1PlaybackSpeed, StringFormat='{}{0:F2}x'}"
               Foreground="{StaticResource Gold}"/>
</Grid>
```

**Implementation**:
```csharp
public class TrackControl
{
    public float Volume { get; set; } = 1.0f;
    public float Pan { get; set; } = 0.0f;
    public float PlaybackSpeed { get; set; } = 1.0f; // NEW!
    public float TimeOffset { get; set; } = 0.0f;    // NEW!
    public bool Muted { get; set; } = false;
    public bool Solo { get; set; } = false;
}
```

**Audio Engine Integration**:
- **Option A**: Use NAudio's `VarispeedSampleProvider` (simple, but pitch changes)
- **Option B**: Use SoundTouch library (pitch-preserving time-stretch)
- **Option C**: Use BASS.NET's tempo/pitch features

**RECOMMENDED**: SoundTouch (open-source, pitch-independent)

```csharp
using SoundTouch;

public class TimeStretchProvider : ISampleProvider
{
    private readonly SoundTouch _soundTouch;
    private readonly ISampleProvider _source;

    public void SetPlaybackSpeed(float speed)
    {
        // Preserve pitch while changing speed
        _soundTouch.SetTempo(speed);
        _soundTouch.SetPitch(1.0f); // Keep original pitch
    }
}
```

---

### 2. Track Offset/Delay

**What**: Start tracks at different times (create evolving compositions)

**Example Use Case**:
```
Bass1:   [------START-----]
Drums1:      [--START--] (delayed 2 seconds)
Melody1:         [--START--] (delayed 4 seconds)
```

Creates a progressive build-up effect!

**UI**:
```xml
<Grid ColumnDefinitions="120,80,*,60">
    <TextBlock Grid.Column="0" Text="Offset:" Foreground="Gray"/>
    <Slider Grid.Column="2"
            Minimum="-10"
            Maximum="10"
            Value="{Binding Bass1TimeOffset}"
            StepFrequency="0.1"/>
    <TextBlock Grid.Column="3"
               Text="{Binding Bass1TimeOffset, StringFormat='{}{0:F1}s'}"
               Foreground="{StaticResource Gold}"/>
</Grid>
```

**Implementation**:
```csharp
public void ApplyTimeOffset(float offsetSeconds)
{
    // Start playback with delay
    if (offsetSeconds > 0)
    {
        Task.Delay((int)(offsetSeconds * 1000))
            .ContinueWith(_ => track.Play());
    }
    else if (offsetSeconds < 0)
    {
        // Start playback earlier (seek forward)
        track.Seek(TimeSpan.FromSeconds(-offsetSeconds));
        track.Play();
    }
    else
    {
        track.Play();
    }
}
```

---

### 3. Per-Track Audio Effects

**What**: Apply reverb, EQ, filters to individual tracks

**Effects**:
- **Reverb**: Add space/depth
- **EQ**: Boost/cut frequencies
- **Low-Pass Filter**: Muffle sound
- **High-Pass Filter**: Remove bass
- **Distortion**: Add grit

**UI** (Collapsible FX Section):
```xml
<Expander Header="🎛️ Effects" IsExpanded="False">
    <StackPanel Spacing="8">
        <!-- Reverb -->
        <Grid ColumnDefinitions="80,*,60">
            <TextBlock Text="Reverb"/>
            <Slider Grid.Column="1" Value="{Binding Bass1Reverb}" Minimum="0" Maximum="100"/>
            <TextBlock Grid.Column="2" Text="{Binding Bass1Reverb, StringFormat='{}{0:F0}%'}"/>
        </Grid>

        <!-- Low-Pass Filter -->
        <Grid ColumnDefinitions="80,*,60">
            <TextBlock Text="LowPass"/>
            <Slider Grid.Column="1" Value="{Binding Bass1LowPassFreq}" Minimum="20" Maximum="20000"/>
            <TextBlock Grid.Column="2" Text="{Binding Bass1LowPassFreq, StringFormat='{}{0:F0} Hz'}"/>
        </Grid>
    </StackPanel>
</Expander>
```

**Implementation** (NAudio or BASS.NET):
```csharp
// NAudio BiQuad Filter for EQ
var eq = new BiQuadFilter(44100);
eq.SetLowPassFilter(cutoffFreq, q);

// Apply to track
var effectProvider = new EffectSampleProvider(trackProvider);
effectProvider.AddEffect(eq);
```

---

### 4. Advanced Preset System

**What**: Save/Load complete audio configurations (all tracks + effects)

**Preset File Format** (JSON):
```json
{
  "name": "Ambient Evolving Soundscape",
  "description": "Slow build with layered tracks",
  "tracks": {
    "Bass1": {
      "volume": 0.7,
      "pan": -0.3,
      "speed": 0.75,
      "offset": 0.0,
      "reverb": 40,
      "lowPass": 500
    },
    "Drums1": {
      "volume": 0.5,
      "pan": 0.0,
      "speed": 1.0,
      "offset": 2.0,
      "reverb": 20,
      "lowPass": 2000
    },
    "Melody1": {
      "volume": 0.8,
      "pan": 0.4,
      "speed": 1.25,
      "offset": 4.0,
      "reverb": 60,
      "lowPass": 8000
    }
  }
}
```

**UI**:
```xml
<StackPanel Orientation="Horizontal" Spacing="8">
    <ComboBox ItemsSource="{Binding AvailablePresets}"
              SelectedItem="{Binding CurrentPreset}"
              MinWidth="200"/>
    <Button Content="Load" Command="{Binding LoadPresetCommand}"/>
    <Button Content="Save As..." Command="{Binding SavePresetCommand}"/>
    <Button Content="Delete" Command="{Binding DeletePresetCommand}"/>
</StackPanel>
```

---

### 5. Waveform Visualization (Optional)

**What**: Show audio waveform with playback position

**Example**:
```
Waveform:  ╱╲╱╲╱╲╱╲╱╲╱╲
           ▼ (playhead)
Time:      [0:00] ━━●━━━━━━ [4:32]
```

**Implementation** (NAudio.WaveFormRenderer):
```csharp
var renderer = new WaveFormRenderer();
var image = renderer.Render(audioFile, new WaveFormRendererSettings
{
    Width = 800,
    Height = 100,
    TopHeight = 50,
    BottomHeight = 50,
    BackgroundColor = Color.FromArgb(255, 30, 43, 45),
    TopPeakPen = new Pen(Color.Red, 1),
    BottomPeakPen = new Pen(Color.Blue, 1)
});

WaveformImage = ConvertToAvaloniaImage(image);
```

---

## Implementation Plan

### Phase 1: Variable Speed (2 hours)
- [ ] Integrate SoundTouch library
- [ ] Add `PlaybackSpeed` property to `TrackControl`
- [ ] Update UI with speed sliders
- [ ] Test pitch-independent time-stretch

### Phase 2: Track Offset (1 hour)
- [ ] Add `TimeOffset` property
- [ ] Implement delayed playback logic
- [ ] Add offset sliders to UI
- [ ] Test synchronized playback with offsets

### Phase 3: Audio Effects (3 hours)
- [ ] Research NAudio/BASS.NET effect APIs
- [ ] Implement reverb effect
- [ ] Implement EQ/filter effects
- [ ] Add effects UI (expandable panels)
- [ ] Test effect performance (CPU usage)

### Phase 4: Preset System (2 hours)
- [ ] Create `AudioPreset` model
- [ ] Implement save/load preset logic
- [ ] Add preset dropdown + buttons
- [ ] Create default presets (ambient, energetic, chill)

### Phase 5: Waveform Viz (2 hours - Optional)
- [ ] Integrate WaveFormRenderer
- [ ] Render waveforms for all tracks
- [ ] Add playhead indicator
- [ ] Make it interactive (click to seek)

**Total Time**: 10 hours (8 hours without waveform)

---

## Libraries Required

### SoundTouch (Pitch-Independent Time-Stretch)
```xml
<PackageReference Include="SoundTouch.Net" Version="3.3.2" />
```

**Why**: Best open-source time-stretch algorithm
**License**: LGPL (compatible with commercial use)

### NAudio Extensions (for effects)
```xml
<PackageReference Include="NAudio.Lame" Version="2.1.0" />
<PackageReference Include="NAudio.Vorbis" Version="1.5.0" />
```

---

## UI Mockup (Compact Layout)

```
┌─────────────────────────────────────────────────────────┐
│  ENHANCED AUDIO MIXER                                   │
├────────┬────────┬──────────┬──────────┬──────┬─────────┤
│ Track  │ Speed  │ Offset   │ Volume   │ Pan  │ Effects │
├────────┼────────┼──────────┼──────────┼──────┼─────────┤
│ Bass1  │ 1.00x  │ +0.0s    │ ▓▓▓▓░░   │ L─●─R│ [FX]    │
│        │ [▓▓●▓▓]│ [░●░░░░] │          │      │         │
├────────┼────────┼──────────┼──────────┼──────┼─────────┤
│ Drums1 │ 0.75x  │ +2.0s    │ ▓▓▓░░░   │ L──●R│ [FX]    │
│        │ [▓●▓▓▓]│ [░░●░░░] │          │      │         │
└────────┴────────┴──────────┴──────────┴──────┴─────────┘

Preset: [Ambient Evolving ▼] [Load] [Save] [Delete]

[▶ Play All] [⏸ Pause All] [⏹ Stop All] [🔄 Reset All]
```

---

## Files to Modify/Create

| File | Change Type | Description |
|------|-------------|-------------|
| `Services/SoundFlowAudioManager.cs` | MODIFY | Add speed, offset, effects support |
| `Models/AudioPreset.cs` | CREATE | Preset model with all track settings |
| `Models/TrackControl.cs` | MODIFY | Add speed, offset, effect properties |
| `ViewModels/MusicMixerWidgetViewModel.cs` | MODIFY | Add new properties + commands |
| `Components/Widgets/MusicMixerWidget.axaml` | MODIFY | Enhanced UI layout |
| `Audio/TimeStretchProvider.cs` | CREATE | SoundTouch integration |
| `Audio/EffectChain.cs` | CREATE | Chain multiple audio effects |

---

## Success Criteria

1. ✅ Playback speed adjustable 0.25x - 4.0x per track
2. ✅ Pitch remains constant when changing speed
3. ✅ Track offset works correctly (delayed start)
4. ✅ Audio effects apply without crackling/artifacts
5. ✅ Presets save/load all settings accurately
6. ✅ CPU usage stays under 30% with all effects enabled
7. ✅ UI is intuitive and responsive

---

## Performance Considerations

- **SoundTouch CPU Usage**: ~5-10% per track at 2x speed
- **Effects Processing**: ~2-5% per effect per track
- **Max Simultaneous Tracks**: 8 tracks * 3 effects = ~40% CPU (acceptable)

**Optimization**:
- Use SIMD in SoundTouch (already optimized)
- Cache processed audio chunks
- Only process active (non-muted) tracks

---

## Future Enhancements

- **Looping Regions**: Set loop points per track
- **MIDI Control**: Map physical MIDI faders to track controls
- **VST Plugin Support**: Load VST effects (ambitious!)
- **Audio Recording**: Record mixed output to file
- **Spectrogram View**: Real-time frequency analysis

---

**Status**: Ready to Enhance! 🎵
**Estimated Time**: 1-2 weeks
**Impact**: Pro-level audio control for creative users
