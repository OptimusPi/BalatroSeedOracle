# PRD-03: Audio & Music System

## Summary

A platform-abstracted audio system for background music playback with multi-stem mixing, volume control, and real-time FFT frequency analysis. Desktop uses native audio; browser/mobile use platform-specific implementations. The audio data feeds into the shader reactivity system (PRD-04).

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Services/IAudioManager.cs` | Audio playback interface |
| `Desktop/Services/DesktopAudioManager.cs` | Desktop audio implementation |
| `Services/SoundEffectsService.cs` | UI sound effects |
| `Models/MixerSettings.cs` | Mixer configuration |
| `Models/MusicMixPreset.cs` | Mix presets |
| `Models/TrackMetadata.cs` | Track info |
| `Models/MusicReactivity.cs` | Reactivity settings |
| `Helpers/MixerHelper.cs` | Mixer utilities |
| `ViewModels/AudioMixerWidgetViewModel.cs` | Mixer UI state |
| `ViewModels/MusicMixerWidgetViewModel.cs` | Music mixer UI state |

---

## Requirements

### R1 — IAudioManager Interface

```csharp
public interface IAudioManager
{
    // Playback
    void Play();
    void Pause();
    void Stop();
    bool IsPlaying { get; }

    // Volume (0-100)
    float Volume { get; set; }
    bool IsMuted { get; set; }

    // Track management
    void LoadTrack(string trackPath);
    TrackMetadata? CurrentTrack { get; }

    // Multi-stem mixing
    void SetStemVolume(AudioStem stem, float volume);
    float GetStemVolume(AudioStem stem);

    // FFT / frequency analysis
    event Action<float, float, float, float>? OnAudioAnalysis; // bass, chords, melody, drums
    float[] GetFrequencyData();  // Raw FFT bins
    float GetBandAmplitude(FrequencyBand band);
}
```

### R2 — Audio Stems

Support 4 independently mixable stems:

| Stem | ID | Description |
|------|----|-------------|
| Drums | 1 | Percussion/rhythm |
| Bass | 2 | Bass frequencies |
| Chords | 3 | Harmonic content |
| Melody | 4 | Lead/melody |

Each stem has independent volume (0.0-1.0). Stems are mixed in real-time.

### R3 — FFT Analysis

- Real-time frequency analysis of the mixed audio output
- Provide amplitude values for frequency bands:
  - Sub-bass (20-60 Hz)
  - Bass (60-250 Hz)
  - Low-mid (250-500 Hz)
  - Mid (500-2000 Hz)
  - High-mid (2000-4000 Hz)
  - Presence (4000-6000 Hz)
  - Brilliance (6000-20000 Hz)
- Fire `OnAudioAnalysis` event at ~60Hz with aggregated band values
- Raw FFT bins available via `GetFrequencyData()` for visualizer widgets

### R4 — Mix Presets

- Named presets (e.g., "Full Mix", "Drums Only", "Chill", "Ambient")
- Each preset stores per-stem volumes
- Save/load custom presets to JSON in `MixerPresets/` directory
- Apply preset smoothly (optional crossfade)

### R5 — Sound Effects

- `SoundEffectsService` for UI interaction sounds
- Short, one-shot audio clips (button clicks, card flips, etc.)
- Independent volume from music
- Fire-and-forget playback

### R6 — Volume Controls

- Master volume (0-100, displayed as percentage)
- Per-stem volume (0.0-1.0)
- Mute toggle (remembers pre-mute volume)
- Volume popup UI (vertical slider + mute button) — see PRD-01

### R7 — Platform Abstraction

| Platform | Implementation |
|----------|---------------|
| Desktop (Win/Mac/Linux) | Native audio (NAudio/BASS/etc.) |
| Browser (WASM) | Web Audio API via JS interop |
| iOS/Android | Platform audio APIs |

- All platforms implement `IAudioManager`
- FFT may not be available on all platforms (graceful degradation)
- Registered via DI in platform-specific startup

---

## Acceptance Criteria

- [ ] Music plays on app startup with default mix
- [ ] Volume slider controls master volume smoothly
- [ ] Mute toggle works and remembers pre-mute level
- [ ] Individual stem volumes are adjustable
- [ ] Mix presets load and apply correctly
- [ ] FFT data fires at ~60Hz for shader reactivity
- [ ] UI sound effects play independently of music
- [ ] Audio system degrades gracefully on platforms without FFT
- [ ] No audio artifacts on pause/resume
