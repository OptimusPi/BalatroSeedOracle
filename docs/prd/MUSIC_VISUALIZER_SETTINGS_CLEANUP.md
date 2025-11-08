# Music Visualizer Settings Cleanup - Product Requirements Document

**Date**: 2025-11-02
**Feature**: Clean up and consolidate Music Visualizer Widget settings
**Status**: PARTIAL IMPLEMENTATION - Some settings disconnected, widget functional but cluttered
**Priority**: HIGH - UX improvement for state-of-the-art visualizer

---

## 🎯 Executive Summary

The Music Visualizer Widget was created to avoid covering the beautiful paint-swirling shader in the middle of the screen. The widget currently works great but has accumulated **old, disconnected settings** that need removal, and lacks proper **test buttons** and **trigger point management**.

**Key Quote from User**:
> "Some of the settings are truly a state of the art invention we got going on. Looks great. works great. Other settings, are old, and no longer even hooked up to anything -- so they should be removed, okay?"

---

## 🧹 Part 1: Cleanup - Remove Old/Disconnected Settings

### Settings to REMOVE

#### ❌ "ADVANCED SETTINGS" Expander (Line 660)
**Reason**: ALL settings are advanced! This creates unnecessary nesting.

**Action**:
- Remove `<Expander Header="ADVANCED SETTINGS">` wrapper
- Move all child content up one level
- Keep "GAME EVENTS", "FREQUENCY BREAKPOINTS", "MELODIC BREAKPOINTS" sections

#### ❌ Disconnected Audio Source Dropdowns
**Location**: Lines 584-656 ("AUDIO TRIGGERS" section)

**Current Implementation** (6 mappings):
1. Shadow Flicker Source
2. Spin Source
3. Twirl Source
4. Zoom Thump Source
5. Color Saturation Source
6. Beat Pulse Source

**⚠️ VERIFY**: Are these actually connected? Check `MusicToVisualizerHandler.cs` for:
- `SetShadowFlickerSource()`
- `SetSpinSource()`
- `SetTwirlSource()`
- `SetZoomThumpSource()`
- `SetColorSaturationSource()`
- `SetBeatPulseSource()`

**If disconnected**: Remove entire "AUDIO TRIGGERS" section (lines 580-657)

**If connected**: Keep them, but rename section to "EFFECT → TRACK MAPPING"

---

## 🧪 Part 2: Add Test Buttons for Effects

### Requirement

**User Quote**:
> "Create a section of the widget that has a TEST button for every EFFECT that is made. Since the effects we made in C# take in data, then process it in a special way (for crank twist boom thump shit like that) we need a button for each of these functions, and a small input box to type a number"

### Available Effects (from MusicToVisualizerHandler.cs)

**Confirmed Effects** (Lines 35, 36, 67, 113):
1. **Zoom Punch** (`_eventZoomPunch`, `_zoomPunch`)
   - Range: `_zoomPunchRangeMin` to `_zoomPunchRangeMax`
   - Default trigger: Line 538-539
   - Event trigger: Lines 600, 611, 622, 639, 670

2. **Contrast Boost** (`_eventContrastBoost`)
   - Default: Line 0.5f to 8.0f
   - Event trigger: Lines 601, 612, 643, 671

3. **Spin** (`_rotationVelocity`)
   - Range: `_spinRangeMin` to `_spinRangeMax`
   - Event trigger: Line 646

4. **Twirl Speed** (`audioSpeedBoost`)
   - Range: `_twirlRangeMin` to `_twirlRangeMax`
   - Calculation: Lines 524-532

### UI Design for Test Section

```xml
<!-- EFFECT TESTING -->
<Border Background="{StaticResource DarkGrey}"
        BorderBrush="{StaticResource ModalBorder}"
        BorderThickness="1"
        CornerRadius="6"
        Padding="12"
        Margin="0,8,0,0">
    <StackPanel Spacing="8">
        <TextBlock Text="EFFECT TESTING"
                   Foreground="{StaticResource White}"
                   FontSize="15"
                   />
        <TextBlock Text="Test shader effects with custom intensity values"
                   Foreground="{StaticResource LightTextGrey}"
                   FontSize="12"
                   Margin="0,0,0,4"/>

        <!-- Zoom Punch Test -->
        <Grid ColumnDefinitions="120,*,80,50" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Zoom Punch"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <Slider Grid.Column="1"
                    Minimum="{Binding ZoomPunchMin}"
                    Maximum="{Binding ZoomPunchMax}"
                    Value="{Binding TestZoomPunchValue}"
                    VerticalAlignment="Center"/>
            <NumericUpDown Grid.Column="2"
                           Value="{Binding TestZoomPunchValue}"
                           Minimum="{Binding ZoomPunchMin}"
                           Maximum="{Binding ZoomPunchMax}"
                           FormatString="F1"
                           FontSize="12"/>
            <Button Grid.Column="3"
                    Content="TEST"
                    Classes="btn-blue"
                    Command="{Binding TestZoomPunchCommand}"
                    Height="28"
                    Padding="8,4"/>
        </Grid>

        <!-- Contrast Boost Test -->
        <Grid ColumnDefinitions="120,*,80,50" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Contrast Boost"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <Slider Grid.Column="1"
                    Minimum="0"
                    Maximum="10"
                    Value="{Binding TestContrastValue}"
                    VerticalAlignment="Center"/>
            <NumericUpDown Grid.Column="2"
                           Value="{Binding TestContrastValue}"
                           Minimum="0"
                           Maximum="10"
                           FormatString="F1"
                           FontSize="12"/>
            <Button Grid.Column="3"
                    Content="TEST"
                    Classes="btn-blue"
                    Command="{Binding TestContrastCommand}"
                    Height="28"
                    Padding="8,4"/>
        </Grid>

        <!-- Spin Test -->
        <Grid ColumnDefinitions="120,*,80,50" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Spin"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <Slider Grid.Column="1"
                    Minimum="{Binding SpinMin}"
                    Maximum="{Binding SpinMax}"
                    Value="{Binding TestSpinValue}"
                    VerticalAlignment="Center"/>
            <NumericUpDown Grid.Column="2"
                           Value="{Binding TestSpinValue}"
                           Minimum="{Binding SpinMin}"
                           Maximum="{Binding SpinMax}"
                           FormatString="F1"
                           FontSize="12"/>
            <Button Grid.Column="3"
                    Content="TEST"
                    Classes="btn-blue"
                    Command="{Binding TestSpinCommand}"
                    Height="28"
                    Padding="8,4"/>
        </Grid>

        <!-- Twirl Test -->
        <Grid ColumnDefinitions="120,*,80,50" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Twirl Speed"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <Slider Grid.Column="1"
                    Minimum="{Binding TwirlMin}"
                    Maximum="{Binding TwirlMax}"
                    Value="{Binding TestTwirlValue}"
                    VerticalAlignment="Center"/>
            <NumericUpDown Grid.Column="2"
                           Value="{Binding TestTwirlValue}"
                           Minimum="{Binding TwirlMin}"
                           Maximum="{Binding TwirlMax}"
                           FormatString="F1"
                           FontSize="12"/>
            <Button Grid.Column="3"
                    Content="TEST"
                    Classes="btn-blue"
                    Command="{Binding TestTwirlCommand}"
                    Height="28"
                    Padding="8,4"/>
        </Grid>
    </StackPanel>
</Border>
```

### ViewModel Commands

**Add to `AudioVisualizerSettingsWidgetViewModel.cs`**:

```csharp
// Test effect properties
[ObservableProperty]
private double _testZoomPunchValue = 15.0;

[ObservableProperty]
private double _testContrastValue = 2.0;

[ObservableProperty]
private double _testSpinValue = 5.0;

[ObservableProperty]
private double _testTwirlValue = 1.0;

// Test commands
[RelayCommand]
private void TestZoomPunch()
{
    // Trigger zoom punch effect via VisualizerEventManager
    VisualizerEventManager.Instance.TriggerManualEffect("ZoomPunch", (float)TestZoomPunchValue);
}

[RelayCommand]
private void TestContrast()
{
    VisualizerEventManager.Instance.TriggerManualEffect("Contrast", (float)TestContrastValue);
}

[RelayCommand]
private void TestSpin()
{
    VisualizerEventManager.Instance.TriggerManualEffect("Spin", (float)TestSpinValue);
}

[RelayCommand]
private void TestTwirl()
{
    VisualizerEventManager.Instance.TriggerManualEffect("Twirl", (float)TestTwirlValue);
}
```

**Add to `VisualizerEventManager.cs`**:

```csharp
public void TriggerManualEffect(string effectName, float intensity)
{
    // Route to appropriate event
    switch (effectName.ToLowerInvariant())
    {
        case "zoompunch":
        case "zoom":
            OnFrequencyBreakpointHit(new FrequencyBreakpointEventArgs
            {
                BreakpointName = "Manual Test",
                EffectName = "Zoom",
                EffectIntensity = intensity
            });
            break;
        case "contrast":
            OnFrequencyBreakpointHit(new FrequencyBreakpointEventArgs
            {
                BreakpointName = "Manual Test",
                EffectName = "Contrast",
                EffectIntensity = intensity
            });
            break;
        case "spin":
            OnFrequencyBreakpointHit(new FrequencyBreakpointEventArgs
            {
                BreakpointName = "Manual Test",
                EffectName = "Spin",
                EffectIntensity = intensity
            });
            break;
        // etc.
    }
}
```

---

## 📍 Part 3: Trigger Point System

### Current Problem

**User Quote**:
> "Now, another thing is setting the trigger point. Trigger point needs to have basically only TWO things, super simple:
> 1) Frequency band that is the trigger. There should only be 1
> 2) Value that the track hits the trigger point (the red thumb knob on the existing slider)
> 3) right now there is a fake led red circle that "lights up" and decays to visually align to see if the thumb point on the slider is a great value to use for that track."

### Data Model

**Create**: `Models/TriggerPoint.cs`

```csharp
public class TriggerPoint
{
    public string Name { get; set; } = "";  // Auto-generated: "Bass1Mid63"
    public string TrackName { get; set; } = "";  // e.g., "Bass1"
    public string TrackId { get; set; } = "";  // Internal ID
    public string FrequencyBand { get; set; } = "";  // "Low", "Mid", "High"
    public double ThresholdValue { get; set; }  // 63.47
    public string EffectName { get; set; } = "";  // "ZoomPunch", "Contrast", etc.
    public double EffectIntensity { get; set; } = 1.0;
}
```

### UI Design for Trigger Point Creation

```xml
<!-- TRIGGER POINT CREATOR -->
<Border Background="{StaticResource DarkGrey}"
        BorderBrush="{StaticResource ModalBorder}"
        BorderThickness="1"
        CornerRadius="6"
        Padding="12"
        Margin="0,8,0,0">
    <StackPanel Spacing="8">
        <TextBlock Text="TRIGGER POINT CREATOR"
                   Foreground="{StaticResource White}"
                   FontSize="15"
                   />
        <TextBlock Text="Define custom audio thresholds that trigger effects"
                   Foreground="{StaticResource LightTextGrey}"
                   FontSize="12"
                   Margin="0,0,0,4"/>

        <!-- Track Selection -->
        <Grid ColumnDefinitions="100,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Audio Track"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      SelectedIndex="{Binding SelectedTriggerTrackIndex}">
                <ComboBoxItem>Bass1</ComboBoxItem>
                <ComboBoxItem>Bass2</ComboBoxItem>
                <ComboBoxItem>Drums1</ComboBoxItem>
                <ComboBoxItem>Drums2</ComboBoxItem>
                <ComboBoxItem>Chords1</ComboBoxItem>
                <ComboBoxItem>Chords2</ComboBoxItem>
                <ComboBoxItem>Melody1</ComboBoxItem>
                <ComboBoxItem>Melody2</ComboBoxItem>
            </ComboBox>
        </Grid>

        <!-- Frequency Band Selection -->
        <Grid ColumnDefinitions="100,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Frequency Band"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      SelectedIndex="{Binding SelectedFrequencyBandIndex}">
                <ComboBoxItem>Low (Bass)</ComboBoxItem>
                <ComboBoxItem>Mid</ComboBoxItem>
                <ComboBoxItem>High (Treble)</ComboBoxItem>
            </ComboBox>
        </Grid>

        <!-- Threshold Slider with LED Indicator -->
        <StackPanel Spacing="4">
            <Grid ColumnDefinitions="100,*,60,40" ColumnSpacing="8">
                <TextBlock Grid.Column="0"
                           Text="Threshold"
                           Foreground="{StaticResource White}"
                           VerticalAlignment="Center"
                           FontSize="13"/>
                <Slider Grid.Column="1"
                        Minimum="0"
                        Maximum="100"
                        Value="{Binding TriggerThresholdValue}"
                        VerticalAlignment="Center"/>
                <TextBlock Grid.Column="2"
                           Text="{Binding TriggerThresholdValue, StringFormat='{}{0:F1}'}"
                           Foreground="{StaticResource Gold}"
                           VerticalAlignment="Center"
                           TextAlignment="Right"
                           FontSize="13"/>

                <!-- LED Indicator -->
                <Ellipse Grid.Column="3"
                         Width="20"
                         Height="20"
                         Fill="{Binding TriggerLedColor}"
                         HorizontalAlignment="Center"
                         VerticalAlignment="Center"/>
            </Grid>
            <TextBlock Text="{Binding CurrentBandValue, StringFormat='Current value: {0:F1}'}"
                       Foreground="{StaticResource LightTextGrey}"
                       FontSize="11"
                       Margin="100,0,0,0"/>
        </StackPanel>

        <!-- Save Trigger Point Button -->
        <Button Content="➕ Save Trigger Point"
                Classes="btn-green"
                Command="{Binding SaveTriggerPointCommand}"
                HorizontalAlignment="Right"
                Height="32"
                Padding="16,4"/>
    </StackPanel>
</Border>
```

### Auto-Naming Logic

```csharp
private string GenerateTriggerPointName(string trackName, string freqBand, double value)
{
    // Format: TrackName + FreqBand + ValueWithoutDecimals
    // Example: "Bass1Mid63" for Bass1 track, Mid band, value 63.47
    string valueStr = ((int)Math.Round(value)).ToString();
    return $"{trackName}{freqBand}{valueStr}";
}
```

### Persistence

**Save to**: `publish/visualizer/trigger_points.json`

```json
{
  "TriggerPoints": [
    {
      "Name": "Bass1Mid63",
      "TrackName": "Bass1",
      "TrackId": "bass1",
      "FrequencyBand": "Mid",
      "ThresholdValue": 63.47,
      "EffectName": "ZoomPunch",
      "EffectIntensity": 1.0
    }
  ]
}
```

---

## 🔗 Part 4: Effect → Trigger Point Mapping

### Requirement

**User Quote**:
> "Then, I need a section of one of these widgets to basically have ONLY the PRE-DEFINED effects that actually can plug values in-- such as zoom punch, or djcrank, or whirl, or twist, or whatever, or shadows from contrast blahdy blahdy blah
>
> left side: ONE instance of each: "zOOM pUNCH:" ETC. for each hard-coded c# effect for the BalatroShaderBackground
>
> right side: [dropdown box with all of my custom TriggerPoints]
>
> THIS SAVES TO THE SHADER THEME"

### UI Design

```xml
<!-- EFFECT → TRIGGER MAPPING -->
<Border Background="{StaticResource DarkGrey}"
        BorderBrush="{StaticResource ModalBorder}"
        BorderThickness="1"
        CornerRadius="6"
        Padding="12"
        Margin="0,8,0,0">
    <StackPanel Spacing="8">
        <TextBlock Text="EFFECT → TRIGGER MAPPING"
                   Foreground="{StaticResource White}"
                   FontSize="15"
                   />
        <TextBlock Text="Map shader effects to your custom trigger points"
                   Foreground="{StaticResource LightTextGrey}"
                   FontSize="12"
                   Margin="0,0,0,4"/>

        <!-- Zoom Punch -->
        <Grid ColumnDefinitions="140,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Zoom Punch"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      ItemsSource="{Binding TriggerPoints}"
                      SelectedItem="{Binding ZoomPunchTrigger}"
                      HorizontalAlignment="Stretch">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Name}"/>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </Grid>

        <!-- Contrast Boost -->
        <Grid ColumnDefinitions="140,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Contrast Boost"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      ItemsSource="{Binding TriggerPoints}"
                      SelectedItem="{Binding ContrastBoostTrigger}"
                      HorizontalAlignment="Stretch">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Name}"/>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </Grid>

        <!-- Spin -->
        <Grid ColumnDefinitions="140,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Spin"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      ItemsSource="{Binding TriggerPoints}"
                      SelectedItem="{Binding SpinTrigger}"
                      HorizontalAlignment="Stretch">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Name}"/>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </Grid>

        <!-- Twirl -->
        <Grid ColumnDefinitions="140,*" ColumnSpacing="8">
            <TextBlock Grid.Column="0"
                       Text="Twirl"
                       Foreground="{StaticResource White}"
                       VerticalAlignment="Center"
                       FontSize="13"/>
            <ComboBox Grid.Column="1"
                      ItemsSource="{Binding TriggerPoints}"
                      SelectedItem="{Binding TwirlTrigger}"
                      HorizontalAlignment="Stretch">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Name}"/>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </Grid>

        <TextBlock Text="✅ Mappings auto-save to current shader theme"
                   Foreground="{StaticResource Green}"
                   FontSize="11"
                   FontStyle="Italic"
                   Margin="0,4,0,0"/>
    </StackPanel>
</Border>
```

### Shader Theme Save Format

**File**: `publish/visualizer/themes/{theme_name}.json`

```json
{
  "ThemeName": "MyCustomTheme",
  "Colors": {
    "MainColor": "#FF5733",
    "AccentColor": "#3366FF",
    "BackgroundColor": "#000000"
  },
  "EffectMappings": {
    "ZoomPunch": "Bass1Mid63",
    "ContrastBoost": "Drums1High75",
    "Spin": "Chords1Low40",
    "Twirl": "Melody1Mid55"
  },
  "EffectRanges": {
    "ZoomPunchMin": -10,
    "ZoomPunchMax": 50,
    "ContrastMin": 0.5,
    "ContrastMax": 8.0,
    "SpinMin": 0,
    "SpinMax": 10,
    "TwirlMin": 0,
    "TwirlMax": 5
  }
}
```

---

## 🗂️ Files to Modify

| File | Change Type | Description |
|------|-------------|-------------|
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | CLEANUP | Remove ADVANCED expander wrapper |
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | VERIFY | Check if AUDIO TRIGGERS connected |
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | ADD | Test buttons section |
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | ADD | Trigger point creator section |
| `Components/Widgets/AudioVisualizerSettingsWidget.axaml` | ADD | Effect→Trigger mapping section |
| `ViewModels/AudioVisualizerSettingsWidgetViewModel.cs` | ADD | Test command methods |
| `ViewModels/AudioVisualizerSettingsWidgetViewModel.cs` | ADD | Trigger point properties/commands |
| `ViewModels/AudioVisualizerSettingsWidgetViewModel.cs` | ADD | Effect mapping properties |
| `Models/TriggerPoint.cs` | CREATE | New model class |
| `Services/VisualizerEventManager.cs` | ADD | TriggerManualEffect() method |
| `Services/MusicToVisualizerHandler.cs` | VERIFY | Event handlers working correctly |

---

## ✅ Success Criteria

1. ✅ ADVANCED expander removed, all settings at top level
2. ✅ Disconnected settings removed (or verified working)
3. ✅ Test buttons for all 4 main effects (Zoom, Contrast, Spin, Twirl)
4. ✅ Each test button has input slider and manual trigger
5. ✅ Trigger point creator with Track + FreqBand + Threshold
6. ✅ LED indicator shows real-time frequency values
7. ✅ Save trigger point button auto-names and persists to JSON
8. ✅ Effect→Trigger mapping dropdowns populated with saved triggers
9. ✅ Mappings auto-save to shader theme JSON
10. ✅ Widget remains compact and doesn't cover shader

---

## 🚨 Open Questions

### Question 1: Audio Source Mappings
**Are the 6 audio source dropdowns (lines 584-656) actually connected?**
- [ ] Check if `SetShadowFlickerSource()` etc. are called
- [ ] If yes: Keep and rename section
- [ ] If no: Remove entire section

### Question 2: LED Update Frequency
**How often should the LED indicator update?**
- [ ] Every frame (60fps)?
- [ ] Every 100ms?
- [ ] Check performance impact

### Question 3: Effect Name Standardization
**What are ALL the available effects?**
- [ ] Zoom Punch ✅
- [ ] Contrast Boost ✅
- [ ] Spin ✅
- [ ] Twirl ✅
- [ ] DJ Crank ❓
- [ ] Whirl ❓
- [ ] Shadow Flicker ❓

**Need to audit `MusicToVisualizerHandler.cs` and `BalatroShaderBackground.cs` for complete list**

### Question 4: Min/Max Range Storage
**Where are effect min/max ranges currently stored?**
- [ ] In ViewModel properties?
- [ ] In UserProfile.json?
- [ ] In shader theme JSON?

---

## 📝 Implementation Plan

### Phase 1: Cleanup (30 min)
1. Remove ADVANCED expander wrapper
2. Audit AUDIO TRIGGERS section (verify connected or remove)
3. Test that widget still works after cleanup

### Phase 2: Test Buttons (2 hours)
1. Add test section UI (4 effects)
2. Add ViewModel properties and commands
3. Add `TriggerManualEffect()` to VisualizerEventManager
4. Test each effect button

### Phase 3: Trigger Points (3 hours)
1. Create `TriggerPoint` model
2. Add trigger point creator UI
3. Implement LED indicator with real-time updates
4. Add save/load logic for trigger_points.json
5. Test auto-naming (Bass1Mid63 format)

### Phase 4: Effect Mapping (2 hours)
1. Add effect→trigger mapping UI
2. Bind dropdowns to saved trigger points
3. Update shader theme JSON format
4. Test save/load of mappings

### Total Estimated Time: **7-8 hours**

---

**END OF PRD**

**Status**: Ready for Implementation
**Complexity**: MEDIUM (mostly UI work, some event wiring)
**Risk**: LOW (isolated to visualizer widget, doesn't affect core functionality)
