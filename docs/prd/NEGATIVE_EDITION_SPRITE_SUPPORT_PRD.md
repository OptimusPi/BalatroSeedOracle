# Negative Edition Sprite Support - Product Requirements Document

**Date**: 2025-11-16
**Feature**: Negative Edition Joker Sprites + Visual Builder Helper
**Status**: READY FOR IMPLEMENTATION
**Priority**: MEDIUM - Quality of Life Enhancement

---

## Executive Summary

Add visual support for Balatro's **Negative Edition** jokers by:
1. **SpriteService enhancement** - Generate inverted/negative versions of joker sprites on-demand
2. **Visual Builder helper button** - Quick toggle to filter for Negative edition jokers
3. **Image caching** - Cache generated negative sprites for performance

This provides accurate visual representation when searching for or displaying Negative edition jokers.

---

## Background

### What is Negative Edition?

In Balatro, the **Negative** edition applies a color inversion effect to jokers, making them appear as photographic negatives:
- Light colors → Dark colors
- Dark colors → Light colors
- Creates a distinctive visual appearance

### Current Problem

- SpriteService only returns base joker sprites
- No visual indication when a joker has Negative edition
- Users can't easily filter for Negative jokers in Visual Builder
- Search results don't show the actual negative appearance

---

## Core Requirements

### 1. SpriteService - Negative Sprite Generation

**File**: `src/Services/SpriteService.cs`

Add method to generate negative (inverted) sprites using SkiaSharp:

```csharp
/// <summary>
/// Gets a joker sprite with optional edition effects applied.
/// </summary>
/// <param name="jokerName">Name of the joker</param>
/// <param name="edition">Edition to apply (null, "Negative", "Foil", "Holographic", "Polychrome")</param>
/// <returns>Image with edition effect applied, or null if not found</returns>
public IImage? GetJokerSpriteWithEdition(string jokerName, string? edition = null)
{
    // Get base sprite
    var baseSprite = GetJokerSprite(jokerName);
    if (baseSprite == null) return null;

    // No edition = return base
    if (string.IsNullOrEmpty(edition)) return baseSprite;

    // Apply edition effect
    return edition.ToLowerInvariant() switch
    {
        "negative" => ApplyNegativeEffect(baseSprite, jokerName),
        "foil" => ApplyFoilEffect(baseSprite, jokerName),        // Future
        "holographic" => ApplyHolographicEffect(baseSprite, jokerName), // Future
        "polychrome" => ApplyChromaticEffect(baseSprite, jokerName),   // Future
        _ => baseSprite
    };
}

/// <summary>
/// Applies Negative edition effect (color inversion) to a sprite.
/// Uses SkiaSharp ColorMatrix for performant pixel inversion.
/// </summary>
private IImage ApplyNegativeEffect(IImage baseSprite, string jokerName)
{
    // Check cache first
    var cacheKey = $"{jokerName}_negative";
    if (_editionEffectCache.TryGetValue(cacheKey, out var cached))
        return cached;

    // Convert to SKBitmap for processing
    var bitmap = ConvertToSKBitmap(baseSprite);

    // Apply color inversion using ColorMatrix
    using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
    using var canvas = surface.Canvas;

    // Color matrix for inversion: multiply RGB by -1, add 255
    var colorMatrix = new float[]
    {
        -1,  0,  0, 0, 255,  // Red
         0, -1,  0, 0, 255,  // Green
         0,  0, -1, 0, 255,  // Blue
         0,  0,  0, 1, 0     // Alpha (unchanged)
    };

    using var paint = new SKPaint
    {
        ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix)
    };

    canvas.DrawBitmap(bitmap, 0, 0, paint);

    // Convert back to Avalonia IImage
    var negative = ConvertToAvaloniaImage(surface.Snapshot());

    // Cache it!
    _editionEffectCache[cacheKey] = negative;

    return negative;
}
```

**New Fields**:
```csharp
// Cache for edition-modified sprites (memory-efficient)
private readonly Dictionary<string, IImage> _editionEffectCache = new();
```

---

### 2. Visual Builder - Negative Edition Helper Button

**File**: `src/Components/FilterTabs/VisualBuilderTab.axaml`

Add a toggle button in the Joker Editions section:

```xml
<!-- JOKER EDITIONS SECTION -->
<Border Classes="filter-section" Margin="0,0,0,12">
    <StackPanel Spacing="8">
        <TextBlock Text="JOKER EDITIONS"
                   Classes="section-header"
                   Foreground="{StaticResource Gold}"/>

        <!-- Edition Buttons (Exclusive Selection) -->
        <WrapPanel Orientation="Horizontal"
                   ItemWidth="110"
                   ItemHeight="32">

            <ToggleButton Content="Base"
                          Classes="edition-btn"
                          IsChecked="{Binding EditionFilter,
                                              Converter={StaticResource EditionToBoolConverter},
                                              ConverterParameter=Base}"/>

            <ToggleButton Content="🎞️ Negative"
                          Classes="edition-btn edition-negative"
                          IsChecked="{Binding EditionFilter,
                                              Converter={StaticResource EditionToBoolConverter},
                                              ConverterParameter=Negative}"
                          ToolTip.Tip="Inverted colors - photographic negative effect"/>

            <ToggleButton Content="✨ Foil"
                          Classes="edition-btn edition-foil"
                          IsChecked="{Binding EditionFilter,
                                              Converter={StaticResource EditionToBoolConverter},
                                              ConverterParameter=Foil}"/>

            <ToggleButton Content="🌈 Holographic"
                          Classes="edition-btn edition-holo"
                          IsChecked="{Binding EditionFilter,
                                              Converter={StaticResource EditionToBoolConverter},
                                              ConverterParameter=Holographic}"/>

            <ToggleButton Content="🎨 Polychrome"
                          Classes="edition-btn edition-poly"
                          IsChecked="{Binding EditionFilter,
                                              Converter={StaticResource EditionToBoolConverter},
                                              ConverterParameter=Polychrome}"/>
        </WrapPanel>

        <!-- Clear Filter Button -->
        <Button Content="Clear Edition Filter"
                Classes="btn-secondary"
                Command="{Binding ClearEditionFilterCommand}"
                HorizontalAlignment="Left"
                Height="28"
                Padding="12,4"/>
    </StackPanel>
</Border>
```

**ViewModel Property** (`VisualBuilderTabViewModel.cs`):

```csharp
[ObservableProperty]
private string? _editionFilter = null; // null, "Base", "Negative", "Foil", etc.

partial void OnEditionFilterChanged(string? value)
{
    // Update filter config when edition changes
    UpdateJokerEditionFilter(value);
}

private void UpdateJokerEditionFilter(string? edition)
{
    if (edition == null)
    {
        // Remove edition filter from config
        // ... implementation
    }
    else
    {
        // Add HasEdition(edition) filter
        // ... implementation
    }

    // Refresh preview
    OnFilterChanged();
}

[RelayCommand]
private void ClearEditionFilter()
{
    EditionFilter = null;
}
```

---

### 3. Edition Button Styling

**File**: `src/Styles/WidgetStyles.axaml` (or new `EditionStyles.axaml`)

```xml
<!-- Edition Toggle Button Styles -->
<Style Selector="ToggleButton.edition-btn">
    <Setter Property="Background" Value="{StaticResource DarkGrey}"/>
    <Setter Property="BorderBrush" Value="{StaticResource ModalBorder}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="CornerRadius" Value="6"/>
    <Setter Property="Foreground" Value="{StaticResource White}"/>
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="FontFamily" Value="{StaticResource BalatroFont}"/>
    <Setter Property="HorizontalContentAlignment" Value="Center"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="Cursor" Value="Hand"/>
</Style>

<!-- Checked State - Glowing Border -->
<Style Selector="ToggleButton.edition-btn:checked">
    <Setter Property="Background" Value="{StaticResource ModalGrey}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Gold}"/>
    <Setter Property="BorderThickness" Value="3"/>
</Style>

<!-- Negative Edition - Dark Inverted Look -->
<Style Selector="ToggleButton.edition-negative:checked">
    <Setter Property="Background" Value="#1a1a1a"/>
    <Setter Property="Foreground" Value="#e0e0e0"/>
    <Setter Property="BorderBrush" Value="#666666"/>
</Style>

<!-- Foil Edition - Shiny Silver -->
<Style Selector="ToggleButton.edition-foil:checked">
    <Setter Property="Background" Value="#c0c0c0"/>
    <Setter Property="Foreground" Value="#000000"/>
    <Setter Property="BorderBrush" Value="#ffffff"/>
</Style>

<!-- Holographic Edition - Rainbow Gradient -->
<Style Selector="ToggleButton.edition-holo:checked">
    <Setter Property="Background">
        <Setter.Value>
            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                <GradientStop Color="#ff0080" Offset="0"/>
                <GradientStop Color="#00ffff" Offset="0.5"/>
                <GradientStop Color="#ffff00" Offset="1"/>
            </LinearGradientBrush>
        </Setter.Value>
    </Setter>
    <Setter Property="Foreground" Value="White"/>
</Style>

<!-- Polychrome Edition - Multi-color -->
<Style Selector="ToggleButton.edition-poly:checked">
    <Setter Property="Background" Value="#ff00ff"/>
    <Setter Property="BorderBrush" Value="#00ffff"/>
</Style>
```

---

### 4. Search Results - Show Negative Sprites

**File**: `src/Components/SearchResultsView.axaml` (or wherever joker images display)

Update image binding to use edition-aware sprite loading:

```xml
<!-- OLD: Base sprite only -->
<Image Source="{Binding JokerSprite}" Width="71" Height="95"/>

<!-- NEW: Edition-aware sprite -->
<Image Source="{Binding JokerSpriteWithEdition}" Width="71" Height="95"/>
```

**ViewModel Update**:
```csharp
public IImage? JokerSpriteWithEdition
{
    get
    {
        var spriteService = SpriteService.Instance;

        // Get edition from search result metadata
        string? edition = GetJokerEdition(); // "Negative", "Foil", etc.

        return spriteService.GetJokerSpriteWithEdition(JokerName, edition);
    }
}
```

---

## Technical Implementation Details

### Color Inversion Math

The **color matrix** used for Negative edition:

```
NEW_R = 255 - OLD_R
NEW_G = 255 - OLD_G
NEW_B = 255 - OLD_B
ALPHA = unchanged
```

Matrix form:
```
[-1   0   0  0  255]   [R]
[ 0  -1   0  0  255] × [G]
[ 0   0  -1  0  255]   [B]
[ 0   0   0  1    0]   [A]
```

### Performance Considerations

1. **Lazy Loading**: Generate negative sprites on-demand, not at startup
2. **Caching**: Store generated negative sprites in `_editionEffectCache`
3. **Memory Management**:
   - Cache up to 150 negative joker sprites (~10-15 MB total)
   - Clear cache on low memory events
4. **Async Generation**: Process images off UI thread for smooth UX

---

## Files to Modify/Create

| File | Change Type | Description |
|------|-------------|-------------|
| `Services/SpriteService.cs` | MODIFY | Add `GetJokerSpriteWithEdition()`, `ApplyNegativeEffect()` |
| `ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` | MODIFY | Add `EditionFilter` property and commands |
| `Components/FilterTabs/VisualBuilderTab.axaml` | MODIFY | Add Edition filter buttons section |
| `Styles/EditionStyles.axaml` | CREATE | Edition button styles |
| `Converters/EditionToBoolConverter.cs` | CREATE | Convert edition string to bool for toggle buttons |
| `Models/SearchResult.cs` | MODIFY | Add `Edition` property (if not already present) |

---

## Implementation Phases

### Phase 1: SpriteService Enhancement (1 hour)
1. Add `_editionEffectCache` dictionary
2. Implement `GetJokerSpriteWithEdition()`
3. Implement `ApplyNegativeEffect()` with SkiaSharp ColorMatrix
4. Add helper methods: `ConvertToSKBitmap()`, `ConvertToAvaloniaImage()`
5. Test with sample jokers (Joker, Greedy Joker, Lusty Joker)

### Phase 2: Visual Builder UI (1 hour)
1. Create `EditionToBoolConverter`
2. Add `EditionFilter` property to ViewModel
3. Implement filter update logic
4. Add Edition buttons section to XAML
5. Create `EditionStyles.axaml`

### Phase 3: Search Results Integration (30 min)
1. Update SearchResult model to include Edition
2. Update result display to use `GetJokerSpriteWithEdition()`
3. Test with real search results

### Phase 4: Testing & Polish (30 min)
1. Test all editions with various jokers
2. Verify cache performance (memory usage, speed)
3. Test Visual Builder edition filtering
4. Edge case testing (missing sprites, invalid editions)

**Total Estimated Time**: 3 hours

---

## Success Criteria

1. ✅ SpriteService can generate negative sprites on-demand
2. ✅ Negative sprites are visually accurate (inverted colors)
3. ✅ Edition sprites are cached for performance
4. ✅ Visual Builder has working edition filter buttons
5. ✅ Search results show correct edition sprites
6. ✅ No performance degradation (<100ms per sprite generation)
7. ✅ Memory usage stays under 20 MB for cached editions

---

## Future Enhancements

### Other Editions (Post-MVP)
- **Foil**: Metallic sheen overlay
- **Holographic**: Rainbow gradient overlay
- **Polychrome**: Chromatic aberration effect

### Advanced Features
- **Edition Preview**: Hover over edition button to see preview
- **Bulk Edition Testing**: "Show all Negative jokers" button
- **Edition Animations**: Subtle shimmer/glow effects

---

## Dependencies

- **SkiaSharp**: Already included in project for shader rendering
- **Avalonia**: 11.0+ for Image handling
- **SpriteService**: Existing sprite loading infrastructure

---

## Risk Assessment

**LOW RISK** - Isolated feature addition:
- No core functionality changes
- Backward compatible (defaults to base sprites)
- Caching prevents performance issues
- Easy to disable if bugs occur

---

**Status**: Ready for Implementation
**Assigned To**: TBD
**Review Date**: TBD
