# Music Visualizer - Quick Start Guide

## The 3 Components

```
📊 FFT Window           →  Create audio triggers
🎚 Audio Mixer          →  Mix track volumes & pan
🎨 Visualizer Settings  →  Map triggers to effects
```

---

## Workflow: Create a Custom Visualizer

### Step 1: Create Triggers (FFT Window 📊)

1. Click FFT Window icon to expand
2. Select track (e.g., "Bass1")
3. Play music and watch frequency bars
4. Adjust "Bass" threshold slider until LED lights up on desired beats
5. Click "+" button to save trigger
   - Creates file: `visualizer/audio_triggers/Bass1Low50.json`
6. Repeat for Mid and High bands
7. Repeat for other tracks (Drums, Melody, etc.)

**Tip**: Higher threshold = only triggers on strong beats

---

### Step 2: Mix Audio (Audio Mixer 🎚)

1. Click Audio Mixer icon to expand
2. For each track:
   - Adjust **Volume** slider (0-100%)
   - Adjust **Pan** slider (L ← → R)
   - Click **M** to mute
   - Click **S** to solo
3. Click **Save** button
4. Name your mix (e.g., "MainMenu", "Intense", "Chill")
   - Creates file: `visualizer/audio_mixes/MainMenu.json`

**Tip**: Use Solo to focus on one track while designing

---

### Step 3: Map Effects (Visualizer Settings 🎨)

1. Click Visualizer Settings icon to expand
2. Expand a shader parameter (e.g., "Zoom Scale")
3. Select trigger from dropdown (e.g., "Bass1Low50")
4. Choose effect mode:
   - **Set Value**: Instant, snappy
   - **Add Inertia**: Smooth, alive feel
5. Adjust multiplier (effect strength)
6. If using Inertia, adjust decay (0.9 = smooth, 0.5 = fast)
7. Repeat for other shader params
8. Click **Save As...** to save preset
   - Creates file: `visualizer/visualizer_presets/MyPreset.json`

---

## Effect Modes Explained

### Set Value (Instant)
- Best for: Quick punches, flashes
- Example: Zoom punch on bass drum
- Behavior: Direct mapping, no momentum

### Add Inertia (Smooth)
- Best for: Continuous motion, organic feel
- Example: Spin that builds up and slows down
- Behavior: Adds force, decays over time

**Decay Values**:
- 0.95 = Very smooth, slow decay (floaty)
- 0.90 = Smooth, moderate decay (bouncy)
- 0.75 = Fast decay (snappy but still smooth)

---

## Example Configurations

### Bass Punch Zoom
```
Trigger: Bass1Low60
Shader Param: Zoom Scale
Mode: Set Value
Multiplier: 20.0
```
→ Screen punches in on strong bass

### Continuous Spin
```
Trigger: Drums1High70
Shader Param: Spin Amount
Mode: Add Inertia
Multiplier: 2.0
Decay: 0.95
```
→ Scene spins smoothly, building momentum

### Contrast Flash
```
Trigger: Melody1Mid55
Shader Param: Contrast
Mode: Set Value
Multiplier: 3.0
```
→ Colors flash on melody hits

---

## File Locations

```
visualizer/
├── audio_triggers/       ← FFT Window saves here
├── audio_mixes/          ← Audio Mixer saves here
└── visualizer_presets/   ← Visualizer Settings saves here
```

---

## Troubleshooting

### "No triggers in dropdown"
→ Create triggers in FFT Window first

### "Effect not responding"
→ Check trigger threshold isn't too high
→ Verify music is playing
→ Try increasing multiplier

### "Too sensitive"
→ Increase trigger threshold
→ Decrease multiplier
→ Use higher decay value for smoother response

### "Builds need to be saved independently"
→ Each widget saves separately:
  - FFT Window: Click "+" button per trigger
  - Audio Mixer: Click "Save" button
  - Visualizer Settings: Click "Save As..." button

---

## Pro Tips

1. **Start with Bass**: Bass frequencies are easiest to trigger consistently
2. **Use Mid for Melody**: Mid-range captures melodic elements well
3. **High for Percussion**: High frequencies great for hi-hats and cymbals
4. **Combine Modes**: Use Set Value for punch, Add Inertia for flow
5. **Save Often**: Create presets for different moods/scenes
6. **Test Threshold**: Watch LED - should light ~2-4 times per second
7. **Decay = Feel**: 0.95 feels alive, 0.75 feels reactive

---

## Quick Reference

### Keyboard Shortcuts
- Drag widget header to reposition
- Click minimize button (↙) to hide

### Widget Positions (Default)
- FFT Window: Lower left
- Audio Mixer: Middle left
- Visualizer Settings: Upper left

### All Widgets Independent
- Each can be on/off separately
- Each saves its own config
- Changes don't affect other widgets

---

**Need More Help?**
See `MUSIC_VISUALIZER_REFACTOR_COMPLETE.md` for full documentation.
