# PRD: Implement Negative Edition with Color Inversion Shader

## Executive Summary
Properly implement Negative Edition by applying a color inversion effect to card sprites. Currently, the Negative button incorrectly uses the Debuffed sprite (red X). Negative is an Edition in Balatro that inverts colors like a photo negative. The button icon should show an inverted Joker card, and all items with Negative edition should render with inverted colors.

## Problem Statement

### Current (Incorrect) Implementation:
- **Negative button** shows Debuffed sprite (red X) - WRONG
- **Negative edition** should invert colors, not show red X
- **Debuffed sprite** is being used for the wrong purpose

### Correct Behavior (from Balatro):
- **Negative Edition** = Color inversion effect (like photo negative)
- **Button icon** = Inverted Joker card sprite
- **Item rendering** = Full color inversion on base card + overlays
- **Visual effect** = Dark becomes light, light becomes dark, colors flip

## Solution Architecture

### Part 1: Create Color Inversion Effect for Avalonia

Avalonia supports custom effects. We can implement color inversion using:

**Option A: SkiaSharp Color Filter (Recommended)**
Since the app already uses SkiaSharp for sprite rendering, use `SKColorFilter.CreateColorMatrix()`:

```csharp
// src/Effects/ColorInversionEffect.cs
using SkiaSharp;
using Avalonia.Skia;

namespace BalatroSeedOracle.Effects
{
    /// <summary>
    /// Color inversion effect for Negative Edition cards (Balatro-style photo negative)
    /// </summary>
    public static class ColorInversion
    {
        private static readonly SKColorFilter InvertFilter = SKColorFilter.CreateColorMatrix(
            new float[]
            {
                -1,  0,  0, 0, 255,  // Red inverted
                 0, -1,  0, 0, 255,  // Green inverted
                 0,  0, -1, 0, 255,  // Blue inverted
                 0,  0,  0, 1,   0   // Alpha unchanged
            }
        );

        public static SKPaint GetInvertedPaint()
        {
            return new SKPaint { ColorFilter = InvertFilter };
        }

        public static SKImage InvertImage(SKImage source)
        {
            var imageInfo = new SKImageInfo(source.Width, source.Height);
            var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;

            using (var paint = GetInvertedPaint())
            {
                canvas.DrawImage(source, 0, 0, paint);
            }

            return surface.Snapshot();
        }
    }
}
```

**Option B: Avalonia Effect (If supported)**
```csharp
// Custom Avalonia effect
public class InvertColorsEffect : IEffect
{
    // Implementation using Avalonia's effect system
}
```

### Part 2: Apply Inversion to NegativeImage Property

Update `SelectableItem.cs` to return inverted sprite for Negative edition:

```csharp
public IImage? EditionImage
{
    get
    {
        if (string.IsNullOrEmpty(Edition) || Edition == "None")
            return null;

        if (Edition.Equals("negative", StringComparison.OrdinalIgnoreCase))
        {
            // For Negative, we don't use an overlay sprite
            // Instead, we invert the entire card in the view
            return null;
        }

        // For other editions (Foil, Holo, Polychrome), use overlay sprites
        return Services.SpriteService.Instance.GetEditionImage(Edition);
    }
}

// New property for color inversion flag
public bool HasNegativeEdition =>
    Edition?.Equals("negative", StringComparison.OrdinalIgnoreCase) == true;
```

### Part 3: Update FilterItemCard to Apply Inversion

Add color inversion effect to the base card image when `HasNegativeEdition` is true:

**Option A: Using SkiaSharp in Image Source (Preferred)**

Modify SpriteService to return pre-inverted images:

```csharp
// SpriteService.cs
public IImage? GetCardImageWithEdition(string itemKey, string? edition)
{
    var baseImage = GetCardImage(itemKey);

    if (edition?.Equals("negative", StringComparison.OrdinalIgnoreCase) == true)
    {
        // Return inverted version
        return InvertImage(baseImage);
    }

    return baseImage;
}

private IImage? InvertImage(IImage? source)
{
    if (source == null) return null;

    // Convert to SKImage, apply inversion, convert back to Avalonia IImage
    // Implementation depends on how sprites are stored
}
```

**Option B: Apply Effect in XAML (If Avalonia supports it)**

```xml
<Image Name="BaseCardImage"
       Source="{Binding ItemImage}"
       Width="71" Height="95">
    <Image.Effect>
        <effects:InvertColorsEffect
            IsEnabled="{Binding HasNegativeEdition}"/>
    </Image.Effect>
</Image>
```

**Option C: Computed Property (Simplest)**

Add a computed property that returns the correct image:

```csharp
// SelectableItem.cs
public IImage? DisplayImage
{
    get
    {
        var baseImage = ItemImage;

        if (HasNegativeEdition && baseImage != null)
        {
            return Services.SpriteService.Instance.InvertImage(baseImage);
        }

        return baseImage;
    }
}
```

Then bind to `DisplayImage` instead of `ItemImage`:

```xml
<Image Name="BaseCardImage"
       Source="{Binding DisplayImage}"
       Width="71" Height="95"/>
```

### Part 4: Update Negative Button Icon

The button should show an inverted Joker card, not the Debuffed sprite:

**VisualBuilderTab.axaml** (update Negative button):

```xml
<!-- CURRENT (wrong - shows debuffed sprite) -->
<Button Command="{Binding SetEditionCommand}"
        CommandParameter="Negative">
    <Image Source="{Binding DebuffedSprite}"/>  <!-- WRONG -->
</Button>

<!-- NEW (correct - shows inverted joker) -->
<Button Classes="action-button"
        ToolTip.Tip="Negative Edition (Inverted Colors)"
        Command="{Binding SetEditionCommand}"
        CommandParameter="Negative">
    <Image Source="{Binding NegativeEditionIcon}"
           Width="32" Height="32"/>
</Button>
```

**ViewModel** (generate inverted icon):

```csharp
public IImage? NegativeEditionIcon
{
    get
    {
        // Get a sample joker card (e.g., "Joker" base card)
        var jokerImage = Services.SpriteService.Instance.GetCardImage("Joker");

        // Return inverted version for button icon
        return Services.SpriteService.Instance.InvertImage(jokerImage);
    }
}
```

### Part 5: Ensure Immediate Visual Update

When Negative edition is applied, the color inversion should appear immediately (same issue as other editions):

```csharp
[RelayCommand]
public void SetEdition(string edition)
{
    if (SelectedEdition == edition)
        return;

    SelectedEdition = edition;
    string? editionValue = edition == "None" ? null : edition.ToLower();

    foreach (var group in GroupedItems)
    {
        foreach (var item in group.Items)
        {
            if (item.Category != "Joker" && item.Category != "StandardCard")
                continue;

            item.Edition = editionValue;

            // Force refresh of display image
            item.OnPropertyChanged(nameof(item.DisplayImage));
            item.OnPropertyChanged(nameof(item.HasNegativeEdition));
        }
    }
}
```

## Implementation Approaches (Ranked)

### Approach 1: Pre-computed Inverted Images (Simplest)
**Pros:**
- No real-time shader processing
- Fast rendering
- Simple binding

**Cons:**
- Doubles memory usage for Negative cards
- Need to cache inverted versions

### Approach 2: SkiaSharp Runtime Inversion (Balanced)
**Pros:**
- Leverages existing SkiaSharp infrastructure
- Efficient color matrix transformation
- No memory overhead

**Cons:**
- Slight CPU cost per render
- Need to integrate with Avalonia's image pipeline

### Approach 3: Custom Avalonia Effect (Most Elegant)
**Pros:**
- Native Avalonia approach
- Reusable effect system
- GPU-accelerated if supported

**Cons:**
- More complex implementation
- May not be fully supported in Avalonia
- Platform-specific behavior

## Recommended Implementation

**Use Approach 1 (Pre-computed) for MVP:**

1. Add `InvertImage()` method to SpriteService
2. Create `DisplayImage` computed property on SelectableItem
3. Bind FilterItemCard to `DisplayImage` instead of `ItemImage`
4. Generate inverted Joker icon for Negative button
5. Update Edition property to trigger `DisplayImage` refresh

## SkiaSharp Color Matrix Reference

**Color inversion matrix:**
```csharp
new float[]
{
    -1,  0,  0, 0, 255,  // R' = 255 - R
     0, -1,  0, 0, 255,  // G' = 255 - G
     0,  0, -1, 0, 255,  // B' = 255 - B
     0,  0,  0, 1,   0   // A' = A (unchanged)
}
```

This inverts RGB channels while preserving alpha transparency.

## Testing Requirements
1. Click Negative button → All cards invert colors immediately
2. Negative button icon shows inverted Joker (not red X)
3. Inverted cards are readable and look correct
4. Drag inverted card → Colors stay inverted
5. Drop inverted card → Colors remain inverted in drop zone
6. Switch to another edition → Inversion removed, new edition applied
7. Click "None" → Inversion removed, original colors restored

## Success Criteria
- Negative edition inverts colors like Balatro
- Button icon shows inverted card (not Debuffed sprite)
- Visual update happens immediately when clicked
- No performance degradation
- Inverted colors persist through drag-and-drop
- Works on all card types (Jokers, Standard Cards)

## File Locations
- **Effect/Helper**: `src/Effects/ColorInversion.cs` (new file)
- **Sprite Service**: `src/Services/SpriteService.cs` (add InvertImage method)
- **Model**: `src/Models/SelectableItem.cs` (add DisplayImage, HasNegativeEdition)
- **View**: `src/Components/FilterItemCard.axaml` (bind to DisplayImage)
- **ViewModel**: `src/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` (NegativeEditionIcon)
- **Button**: `src/Components/FilterTabs/VisualBuilderTab.axaml` (update Negative button)

## Debuffed Sprite (Red X) - Save for Later

The Debuffed sprite (red X) is currently being used incorrectly for Negative. Once Negative is fixed with color inversion, repurpose Debuffed for visual "MUST-NOT" feedback as described in the other PRD.

## Time Estimate
2-3 hours to implement color inversion properly with pre-computed approach. Longer if using runtime shaders.

## Example Visual

**Before (Wrong):**
```
Negative Button: [🚫 Red X]
Cards: Normal colors with red X overlay
```

**After (Correct):**
```
Negative Button: [🃏 Inverted Joker]
Cards: Fully inverted colors (dark↔light, colors flipped)
```

## Future Enhancement

If performance is good with pre-computed images, consider caching inverted sprites on app startup to eliminate any runtime cost.
