# BalatroShaderBackground Animation Fix

## Problem
The `BalatroShaderBackground` animated shader background was not animating/rendering on startup. The background appeared static with no movement.

## Root Cause
The animation loop was never being initiated. While the `OnAnimationFrameUpdate()` method was properly implemented and would call `RegisterForNextAnimationFrameUpdate()`, nothing was triggering the **first** animation frame when the control was attached to the visual tree.

The animation loop flow:
1. `OnAnimationFrameUpdate()` → calls `Invalidate()` and `RegisterForNextAnimationFrameUpdate()`
2. `OnRender()` → renders the frame and calls `RegisterForNextAnimationFrameUpdate()`
3. Next frame → back to step 1

**Problem**: The loop never started because step 1 was never initially triggered.

## Solution
Added `_customVisual.SendHandlerMessage(CompositionCustomVisualHandler.StartAnimations)` in the `OnAttachedToVisualTree()` method to kick off the animation loop when the control is first attached.

## Code Changes

### File: `src/Controls/BalatroShaderBackground.cs`

**Location**: `OnAttachedToVisualTree()` method (lines ~175-186)

**Before**:
```csharp
var compositionTarget = ElementComposition.GetElementVisual(this);
if (compositionTarget?.Compositor != null)
{
    _handler = new BalatroShaderHandler();
    _customVisual = compositionTarget.Compositor.CreateCustomVisual(_handler);
    ElementComposition.SetElementChildVisual(this, _customVisual);
    _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
}

// Hook up mouse move for parallax effect
this.PointerMoved += OnPointerMoved;
```

**After**:
```csharp
var compositionTarget = ElementComposition.GetElementVisual(this);
if (compositionTarget?.Compositor != null)
{
    _handler = new BalatroShaderHandler();
    _customVisual = compositionTarget.Compositor.CreateCustomVisual(_handler);
    ElementComposition.SetElementChildVisual(this, _customVisual);
    _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
    
    // Start the animation loop
    _customVisual.SendHandlerMessage(CompositionCustomVisualHandler.StartAnimations);
}

// Hook up mouse move for parallax effect
this.PointerMoved += OnPointerMoved;
```

## How It Works

1. **Control Creation**: When `BalatroShaderBackground` is instantiated in XAML
2. **Attached to Visual Tree**: `OnAttachedToVisualTree()` is called
3. **Custom Visual Setup**: Creates the composition custom visual with the shader handler
4. **Animation Start**: `SendHandlerMessage(StartAnimations)` triggers the first animation frame
5. **Animation Loop**: 
   - `OnAnimationFrameUpdate()` is called by the composition system
   - Calls `Invalidate()` to mark for redraw
   - Calls `RegisterForNextAnimationFrameUpdate()` to schedule next frame
   - `OnRender()` draws the shader with updated time
   - Loop continues indefinitely while `_isAnimating` is true

## Visual Result
- ✅ Background now animates smoothly on startup
- ✅ Shader effects (rotation, color cycling, parallax) all work
- ✅ Can be paused/resumed with `IsAnimating` property
- ✅ Responds to theme changes and audio reactivity
- ✅ Mouse parallax effect functional

## Testing
- ✅ Build successful with no errors or warnings
- ✅ Animation loop properly initiated on control load
- ✅ No performance issues or memory leaks

## Technical Details

### Avalonia Composition API
- `CompositionCustomVisual` - Avalonia's system for custom rendering with Skia
- `CompositionCustomVisualHandler` - Handler for custom render logic
- `StartAnimations` message - Built-in message to begin animation frame callbacks
- `RegisterForNextAnimationFrameUpdate()` - Schedules the next frame callback

### Shader Implementation
The background uses a real SKSL shader (SkiaSharp Shading Language) converted from Balatro's original GLSL shader, featuring:
- Perlin noise-based distortion
- Color cycling with multiple palette colors
- Rotation and zoom effects
- Audio-reactive parameters
- Mouse parallax offset
- Theme system with 8 preset palettes
