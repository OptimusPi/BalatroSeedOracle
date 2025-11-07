# PRD: Audio Visualizer Settings Widget Simplification

## Current Problem

**Current file:** `AudioVisualizerSettingsWidget.axaml` - **1,233 lines** of complexity hell

### Current Features (TOO MANY):
1. Preset Load/Save system
2. Theme selection (8 presets + custom colors)
3. Custom color pickers (Main + Accent)
4. Effect testing section (4 test sliders + buttons)
5. Shader param to trigger mapping (3 complex expanders)
6. Direct shader parameters (11 sliders with min/max textboxes!)
7. Effect to track mapping (6 dropdowns)
8. Game events (3 checkboxes)
9. Frequency breakpoints (dynamic add/remove lists)
10. Melodic breakpoints (dynamic add/remove lists)
11. Track volumes (8 individual sliders)
12. Search transitions (2 preset dropdowns)

### Why This Violates KISS:
- **Feature creep** - trying to control everything from one widget
- **Overwhelming UI** - 1233 lines of XAML for ONE widget
- **Poor UX** - users don't need 90% of this
- **Maintenance nightmare** - hard to debug, hard to extend
- **Configuration hell** - too many knobs to tweak

## Proposed KISS Solution

### Core Principle:
**"Most users just want cool visuals that react to music - make it EASY"**

### Simplified Widget (Target: ~200 lines):

```
┌─────────────────────────────────┐
│  🎨 Visualizer                 │
│                                 │
│  Preset: [Electric Storm   ▼]  │
│                                 │
│  ┌───────────────────────────┐ │
│  │ Intensity    ███████░░░ 70%│ │
│  │ Speed        █████░░░░░ 50%│ │
│  │ Color Shift  ██████████100%│ │
│  └───────────────────────────┘ │
│                                 │
│  [Load...] [Save As...]         │
└─────────────────────────────────┘
```

### Essential Features ONLY:

1. **Preset Dropdown** (1 line)
   - Load predefined presets (Electric Storm, Inferno, etc.)
   - Simple dropdown, no complexity

2. **Intensity Slider** (1 line)
   - Controls overall effect strength (0-100%)
   - Affects zoom, contrast, spin all at once

3. **Speed Slider** (1 line)
   - Controls animation speed (0-100%)
   - Affects time multiplier

4. **Color Shift Slider** (1 line)
   - Controls color intensity/saturation (0-100%)
   - Simple color adjustment

5. **Load/Save Buttons** (1 line)
   - Load preset from JSON file
   - Save current settings to JSON file

### What Gets REMOVED:

❌ Theme selection (redundant with presets)
❌ Custom color pickers (use presets or JSON)
❌ Effect testing section (use app, not widget)
❌ Shader param mapping (advanced users edit JSON)
❌ Direct shader parameters (11 sliders! use JSON!)
❌ Effect to track mapping (use JSON presets)
❌ Game events (keep these in app settings, not visualizer)
❌ Frequency breakpoints (advanced - JSON only)
❌ Melodic breakpoints (advanced - JSON only)
❌ Track volumes (use audio mixer widget, not visualizer)
❌ Search transitions (questionable feature, remove for now)

### Advanced Users:
- Edit JSON preset files directly for fine control
- Widget is for QUICK adjustments, not full config

## Implementation Plan

### Step 1: Create New Simplified Widget
**File:** `AudioVisualizerWidget.axaml` (new, clean start)

**XAML Structure:**
```xml
<UserControl>
    <Grid>
        <!-- Minimized icon -->
        <StackPanel IsVisible="{Binding IsMinimized}">
            <Border Width="64" Height="64">
                <TextBlock Text="🎨" FontSize="28"/>
            </Border>
            <TextBlock Text="Visual" FontSize="10"/>
        </StackPanel>

        <!-- Expanded settings -->
        <Border IsVisible="{Binding !IsMinimized}" MinWidth="320" MaxWidth="420">
            <StackPanel Spacing="12" Padding="15">
                <!-- Header -->
                <Grid ColumnDefinitions="Auto,*">
                    <Button Content="↙" Command="{Binding MinimizeCommand}"/>
                    <TextBlock Text="🎨 Visualizer" FontSize="13"/>
                </Grid>

                <!-- Preset dropdown -->
                <ComboBox SelectedItem="{Binding SelectedPreset}"
                          ItemsSource="{Binding AvailablePresets}"/>

                <!-- Simple sliders -->
                <Grid ColumnDefinitions="80,*,40">
                    <TextBlock Text="Intensity"/>
                    <Slider Grid.Column="1" Value="{Binding Intensity}" Maximum="100"/>
                    <TextBlock Grid.Column="2" Text="{Binding Intensity, StringFormat={}{0:0}%}"/>
                </Grid>

                <Grid ColumnDefinitions="80,*,40">
                    <TextBlock Text="Speed"/>
                    <Slider Grid.Column="1" Value="{Binding Speed}" Maximum="100"/>
                    <TextBlock Grid.Column="2" Text="{Binding Speed, StringFormat={}{0:0}%}"/>
                </Grid>

                <Grid ColumnDefinitions="80,*,40">
                    <TextBlock Text="Color Shift"/>
                    <Slider Grid.Column="1" Value="{Binding ColorShift}" Maximum="100"/>
                    <TextBlock Grid.Column="2" Text="{Binding ColorShift, StringFormat={}{0:0}%}"/>
                </Grid>

                <!-- Load/Save buttons -->
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Button Content="Load..." Command="{Binding LoadPresetCommand}"/>
                    <Button Content="Save As..." Command="{Binding SavePresetCommand}"/>
                </StackPanel>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

### Step 2: Update ViewModel
**File:** `AudioVisualizerWidgetViewModel.cs`

**Properties needed:**
- `IsMinimized` (bool)
- `SelectedPreset` (string)
- `AvailablePresets` (ObservableCollection<string>)
- `Intensity` (double 0-100)
- `Speed` (double 0-100)
- `ColorShift` (double 0-100)
- `MinimizeCommand`
- `LoadPresetCommand`
- `SavePresetCommand`

**Slider Logic:**
```csharp
// When any slider changes, update shader params
private void OnIntensityChanged(double value)
{
    // Intensity affects multiple params at once
    ShaderParams.ZoomScale = 1.0 + (value / 100.0) * 0.5;
    ShaderParams.Contrast = 1.0 + (value / 100.0) * 2.0;
    ShaderParams.SpinAmount = (value / 100.0) * 5.0;
}

private void OnSpeedChanged(double value)
{
    // Speed affects time multiplier
    ShaderParams.TimeMultiplier = 0.5 + (value / 100.0) * 1.5;
}

private void OnColorShiftChanged(double value)
{
    // Color affects saturation
    ShaderParams.Saturation = 0.5 + (value / 100.0) * 1.5;
}
```

### Step 3: Replace Old Widget
1. Rename old widget: `AudioVisualizerSettingsWidget.axaml.OLD`
2. Create new simplified widget
3. Update `BalatroMainMenu.axaml` to use new widget
4. Test thoroughly
5. Delete old widget after confirmation

## Migration Path

### For Users:
- Existing JSON presets still work
- Can still edit JSON for advanced control
- Widget is just simpler UI on top

### For Developers:
- Much easier to maintain
- Clear separation: Widget = simple UI, JSON = advanced config
- ~200 lines vs 1233 lines (83% reduction!)

## Success Criteria

✅ Widget under 300 lines of XAML
✅ 3 simple sliders (Intensity, Speed, Color)
✅ Preset dropdown works
✅ Load/Save JSON presets works
✅ Visual effects respond to slider changes
✅ Minimized icon works
✅ No feature regressions for JSON users

## Questions for User

1. **Do you want these 3 sliders (Intensity, Speed, Color)?** Or different ones?
2. **Should preset dropdown auto-load from `presets/` folder?** Or hardcoded list?
3. **Keep the minimized icon (🎨)?** Or different icon?
4. **Any other simple controls needed?** (On/Off toggle? Reset button?)

## Alternative: Even Simpler

If you want MAXIMUM KISS:

```
┌─────────────────────────────────┐
│  🎨 Visualizer                 │
│                                 │
│  [ON / OFF]                     │
│                                 │
│  Preset: [Electric Storm   ▼]  │
│                                 │
│  Intensity: ███████░░░ 70%     │
└─────────────────────────────────┘
```

Just:
- On/Off toggle
- Preset dropdown
- Single intensity slider

Your call!
