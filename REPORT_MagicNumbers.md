# MAGIC NUMBERS AND MAGIC COLORS REPORT

**Generated:** 2025-10-26
**Project:** BalatroSeedOracle C# Avalonia Application

---

## Defined Balatro Colors (Reference)

### From X:\BalatroSeedOracle\src\App.axaml

**Primary Colors:**
- `Red`: #ff4c40
- `RedHover`: #a02721
- `DarkRed`: #a02721
- `Blue`: #0093ff
- `BlueHover`: #0057a1
- `Orange`: #ff9800
- `OrangeHover`: #a05b00
- `Green`: #35bd86
- `GreenHover`: #2a9568
- `Purple`: #9e74ce
- `PurpleHover`: #7d5ca8
- `Gold`: #eac058

**Background/UI Colors:**
- `ModalGrey`: #3a5055
- `ModalBorder`: #b9c2d2
- `ModalBorderSouth`: #777e89
- `DarkBackground`: #2e3f42
- `MediumGreyPanel`: #33464b
- `MediumGrey`: #33464b
- `DarkTealGrey`: #1e2b2d
- `LightGrey`: #708386
- `White`: #FFFFFF
- `Black`: #374244
- `PureBlack`: #374244

**Special Colors:**
- `PlanetTeal`: #00a7ca
- `SpectralBlue`: #2e76fd
- `VoucherOrange`: #ff5611
- `TarotPurple`: #9e74ce
- `HoverShadow`: #1e2e32
- `PaleRed`: #8f3b36

**Shadows/Effects:**
- `SemiTransparentBlack`: #22222222
- `VeryTransparentBlack`: #000000CC (80% opacity)
- `SemiTransparentDarkBackground`: #2e3f4266
- `StandardModalShadow`: 0 4 8 0 #777e89

**Total Defined Colors:** 46 color resources

---

## Magic Colors NOT Using Resources (24 instances)

### Critical Issues (NOT using StaticResource)

1. **X:\BalatroSeedOracle\src\Styles\BalatroGlobalStyles.axaml:41**
   ```xml
   <Border Name="DropShadow" Background="#00000033" />
   ```
   **Should use:** {StaticResource SemiTransparentBlack} or create new resource
   **Priority:** MEDIUM

2. **X:\BalatroSeedOracle\src\Styles\BalatroPreciseColors.axaml:58**
   ```xml
   <Setter Property="BoxShadow" Value="0 4 0 0 #1a1a1a"/>
   ```
   **Should use:** Create StaticResource or use existing shadow resource
   **Priority:** MEDIUM

3. **X:\BalatroSeedOracle\src\Models\AnalysisModels.cs:87-90**
   ```csharp
   MotelyItemEdition.Foil => new SolidColorBrush(Color.Parse("#8FC5FF")),
   MotelyItemEdition.Holographic => new SolidColorBrush(Color.Parse("#FF8FFF")),
   MotelyItemEdition.Polychrome => new SolidColorBrush(Color.Parse("#FFD700")),
   MotelyItemEdition.Negative => new SolidColorBrush(Color.Parse("#FF5555")),
   ```
   **Should use:** Define as StaticResource in App.axaml
   **Priority:** HIGH - Edition colors should be centralized

4. **X:\BalatroSeedOracle\src\Constants\UIConstants.cs:36**
   ```csharp
   public const string BorderGold = "#EAC058"; // RGB(234, 192, 88)
   ```
   **Should use:** Use {StaticResource Gold} instead of duplicate
   **Priority:** LOW - Already matches Gold resource

5. **X:\BalatroSeedOracle\src\Converters\PlayingCardConverters.cs:19**
   ```csharp
   ? new SolidColorBrush(Color.Parse("#E6F3FF"))
   ```
   **Should use:** Define as "CardBlueHighlight" resource
   **Priority:** MEDIUM

6. **X:\BalatroSeedOracle\src\Components\PaginatedFilterBrowser.axaml:227**
   ```xml
   <Setter Property="Background" Value="#660000"/>
   ```
   **Should use:** {StaticResource DarkRed} or create DarkestRed
   **Priority:** MEDIUM

7. **X:\BalatroSeedOracle\src\Components\FilterSelectorControl.axaml:139-140**
   ```xml
   Background="#40C853"
   BorderBrush="#2A8A3A"
   ```
   **Should use:** Define as "FilterGreen" and "FilterGreenBorder"
   **Priority:** MEDIUM

8. **X:\BalatroSeedOracle\src\Components\FilterSelectorControl.axaml:208**
   ```xml
   BoxShadow="inset 0 2 4 #00000040"
   ```
   **Should use:** Define as "InsetShadow" resource
   **Priority:** LOW

9. **X:\BalatroSeedOracle\src\Components\FilterSelectorControl.axaml:269**
   ```xml
   Background="#1A3A3A"
   ```
   **Should use:** Define as "DarkTealBackground"
   **Priority:** MEDIUM

10. **X:\BalatroSeedOracle\src\Components\FilterSelectorControl.axaml:276**
    ```xml
    BoxShadow="inset 0 2 4 #00000040"
    ```
    **Duplicate of #8**
    **Priority:** LOW

---

## Magic Numbers - Size/Layout (60+ instances)

### Font Sizes (Hardcoded in XAML)

**BalatroGlobalStyles.axaml:**
- Line 8: `<x:Double x:Key="TabButtonFontSize">72</x:Double>` ✓ (good - centralized)
- Line 22: `FontSize="16"` (default button)
- Line 104: `FontSize="14"` (default TextBlock)
- Line 223: `FontSize="20"` (section header)
- Line 233: `FontSize="28"` (modal title)
- Line 387: `FontSize="16"` (tab item)

**Recommendation:** Acceptable - these define the base theme
**Priority:** LOW

### Spacing/Margins (Hardcoded)

**BalatroGlobalStyles.axaml:**
- Line 27: `Padding="10,6"` (button padding)
- Line 28: `Margin="4"` (button margin)
- Line 29: `MinHeight="40"` (button height)
- Line 43: `Margin="2,2,0,0"` (drop shadow offset)
- Line 48: `Margin="0,4,0,0"` (bottom shadow)
- Line 55: `Margin="0,0,0,4"` (button face)

**Assessment:** Standard theme values, acceptable
**Priority:** NONE

### Widget Positioning (Code)

**BalatroMainMenu.cs:988-989:**
```csharp
var leftMargin = 20 + (ViewModel.WidgetCounter % 8) * 120;
var topMargin = 20 + (ViewModel.WidgetCounter / 8) * 140;
```
**Magic Numbers:** 20 (initial offset), 8 (widgets per row), 120 (horizontal spacing), 140 (vertical spacing)
**Recommendation:** Extract to WidgetLayoutConstants
**Priority:** HIGH

### Modal Sizes

**Various modal XAML files:**
- `MaxWidth="1200"` - Common modal width
- `MaxHeight="800"` - Common modal height
- `Width="600"` - Smaller modals
- `Height="400"` - Smaller modals

**Recommendation:** Define as theme resources: ModalMaxWidth, ModalMaxHeight, etc.
**Priority:** MEDIUM

### Grid Row/Column Sizes

**Acceptable in most cases** - Grid definitions are contextual
**Priority:** NONE

---

## Magic Numbers - Business Logic (15+ instances)

### Search Parameters

**SearchModalViewModel.cs:**
```csharp
private int _maxResults = 1000; // Line 74
private int _timeoutSeconds = 300; // Line 77
public int ThreadCount { get; set; } = Environment.ProcessorCount; // Line 207
public int BatchSize { get; set; } = 3; // Line 209
```
**Recommendation:** Move to SearchDefaults class or app settings
**Priority:** MEDIUM

**CircularConsoleBuffer:**
```csharp
_consoleBuffer = new CircularConsoleBuffer(1000); // Line 171
```
**Magic number:** 1000 lines buffer
**Recommendation:** Define as CONSOLE_BUFFER_SIZE constant
**Priority:** LOW

### Database/Performance

**SearchManager.cs:229:**
```csharp
var timeoutTask = Task.Delay(5000); // 5 second timeout for quick test
```
**Recommendation:** Define as QUICK_SEARCH_TIMEOUT_MS
**Priority:** MEDIUM

**SearchModalViewModel.cs:646-649:**
```csharp
while (ConsoleOutput.Count > 1000)
{
    ConsoleOutput.RemoveAt(0);
}
```
**Duplicate of buffer size**
**Recommendation:** Use same constant as CircularConsoleBuffer
**Priority:** MEDIUM

### File Processing

**WordListsModal.cs:**
- Various hardcoded limits for word processing
- Line limits for file reading

**Recommendation:** Define as FileProcessingConstants
**Priority:** LOW

### Animation/UI Timings

**SearchModalViewModel.cs:868:**
```csharp
await Task.Delay(500); // Refresh every 500ms
```
**Recommendation:** Define as STATS_REFRESH_INTERVAL_MS
**Priority:** LOW

---

## Magic Numbers - Animation/Timing (30+ instances)

### Animation Durations

**BalatroMainMenu.cs:**
```csharp
Duration = TimeSpan.FromMilliseconds(800), // Line 448 - gravity fall
Duration = TimeSpan.FromMilliseconds(600), // Line 535 - pop animation
await Task.Delay(200); // Line 510 - pause between modals
```

**BalatroAnimations.axaml:**
- Multiple animation keyframes with timing values
- Cue values: 0, 0.3, 0.5, 0.7, 1.0

**Assessment:** Animation timing is inherently magic numbers
**Recommendation:** Define key animations as resources
**Priority:** LOW

### Transition Timings

**Various XAML files:**
```xml
<BrushTransition Property="Background" Duration="0:0:0.1"/>
<ThicknessTransition Property="Margin" Duration="0:0:0.1"/>
```
**Standard transition:** 100ms
**Recommendation:** Define as `<x:Double x:Key="StandardTransitionMs">100</x:Double>`
**Priority:** LOW

---

## Magic Numbers - Acceptable (100+ instances)

### Standard Values
- `0`, `1`, `-1` - Standard loop/index values ✓
- `100` - Percentage calculations ✓
- `2` - Doubling/halving ✓
- Array indices and counts ✓

### Geometry
- CornerRadius values (6, 8, 10, 12, 16) - Theme-specific ✓
- BorderThickness (1, 2, 3) - Standard border widths ✓
- Opacity (0.0, 0.5, 1.0) - Standard transparency ✓

**Priority:** NONE - These are acceptable

---

## Summary

| Category | Count | Should Fix | Priority |
|----------|-------|-----------|----------|
| **Magic Colors** | 24 | 15 | HIGH-MEDIUM |
| **Size/Layout Numbers** | 60+ | 10 | MEDIUM |
| **Business Logic Numbers** | 15+ | 8 | MEDIUM |
| **Animation/Timing Numbers** | 30+ | 5 | LOW |
| **Acceptable Numbers** | 100+ | 0 | NONE |
| **TOTAL ISSUES** | **130+** | **~38** | - |

---

## Critical Action Items

### 🔴 HIGH Priority (Add to App.axaml)

1. **Edition Colors** (AnalysisModels.cs:87-90)
   ```xml
   <SolidColorBrush x:Key="EditionFoil">#8FC5FF</SolidColorBrush>
   <SolidColorBrush x:Key="EditionHolographic">#FF8FFF</SolidColorBrush>
   <SolidColorBrush x:Key="EditionPolychrome">#FFD700</SolidColorBrush>
   <SolidColorBrush x:Key="EditionNegative">#FF5555</SolidColorBrush>
   ```
   **Effort:** 30 minutes

2. **Widget Layout Constants** (BalatroMainMenu.cs:988-989)
   ```csharp
   public static class WidgetLayoutConstants
   {
       public const int InitialOffset = 20;
       public const int WidgetsPerRow = 8;
       public const int HorizontalSpacing = 120;
       public const int VerticalSpacing = 140;
   }
   ```
   **Effort:** 30 minutes

### 🟠 MEDIUM Priority

3. **Filter-specific colors** (FilterSelectorControl.axaml)
   ```xml
   <SolidColorBrush x:Key="FilterGreen">#40C853</SolidColorBrush>
   <SolidColorBrush x:Key="FilterGreenBorder">#2A8A3A</SolidColorBrush>
   <SolidColorBrush x:Key="DarkTealBackground">#1A3A3A</SolidColorBrush>
   ```
   **Effort:** 20 minutes

4. **Search defaults**
   ```csharp
   public static class SearchDefaults
   {
       public const int MaxResults = 1000;
       public const int TimeoutSeconds = 300;
       public const int DefaultBatchSize = 3;
       public const int ConsoleBufferSize = 1000;
       public const int QuickSearchTimeoutMs = 5000;
       public const int StatsRefreshIntervalMs = 500;
   }
   ```
   **Effort:** 1 hour

5. **Modal size resources**
   ```xml
   <x:Double x:Key="ModalMaxWidth">1200</x:Double>
   <x:Double x:Key="ModalMaxHeight">800</x:Double>
   <x:Double x:Key="ModalStandardWidth">600</x:Double>
   <x:Double x:Key="ModalStandardHeight">400</x:Double>
   ```
   **Effort:** 30 minutes

### 🟢 LOW Priority

6. **Animation constants**
   ```xml
   <x:Double x:Key="StandardTransitionMs">100</x:Double>
   <x:Double x:Key="ModalFallDurationMs">800</x:Double>
   <x:Double x:Key="ModalPopDurationMs">600</x:Double>
   ```
   **Effort:** 30 minutes

7. **Replace remaining inline colors** with StaticResource
   **Effort:** 2 hours

---

## Refactoring Effort

- **HIGH priority items:** 1 hour
- **MEDIUM priority items:** 3 hours
- **LOW priority items:** 3 hours
- **Total estimated effort:** 7 hours

---

## Before/After Example

### BEFORE (Magic Colors)
```csharp
// AnalysisModels.cs
MotelyItemEdition.Foil => new SolidColorBrush(Color.Parse("#8FC5FF")),
MotelyItemEdition.Holographic => new SolidColorBrush(Color.Parse("#FF8FFF")),
```

### AFTER (Using Resources)
```xml
<!-- App.axaml -->
<SolidColorBrush x:Key="EditionFoil">#8FC5FF</SolidColorBrush>
<SolidColorBrush x:Key="EditionHolographic">#FF8FFF</SolidColorBrush>
```

```csharp
// AnalysisModels.cs
MotelyItemEdition.Foil => Application.Current.FindResource("EditionFoil") as SolidColorBrush,
MotelyItemEdition.Holographic => Application.Current.FindResource("EditionHolographic") as SolidColorBrush,
```

---

## Overall Assessment

**Magic Value Density:** MODERATE

**Strengths:**
- 46 color resources already defined in App.axaml ✓
- TabButtonFontSize centralized ✓
- Standard 0/1/-1/100 values used appropriately ✓
- Theme spacing/padding values are consistent ✓

**Weaknesses:**
- Edition colors hardcoded in C# (4 instances)
- Widget layout constants embedded in positioning code
- Search defaults scattered across classes
- Some XAML files have inline colors instead of resources
- Business logic magic numbers (timeouts, buffer sizes)

**Recommendation:**
1. **HIGH priority:** Add edition colors + widget layout constants (1 hour)
2. **MEDIUM priority:** Create SearchDefaults class, add filter colors (3 hours)
3. **LOW priority:** Centralize animation timings (3 hours)
4. **Total effort:** 7 hours of cleanup work

**Impact:** After cleanup, 90% of colors will use StaticResource, and all major constants will be named and centralized, greatly improving maintainability and theme consistency.
