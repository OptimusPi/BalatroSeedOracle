# Card Tilt & Sway Animation PRD

## Status: RESEARCH COMPLETE - READY FOR REVIEW

## Goal
Implement **AUTHENTIC** Balatro-style card tilt and sway animations based on the actual Balatro Lua source code.

## Current State Analysis

### ✅ What's Already Working
1. **`BalatroCardSwayBehavior.cs`** - Ambient breathing animation
   - Uses cos wave for tilt amount: `tilt_amt = AmbientTilt * (0.5 + Math.Cos(tilt_angle)) * tilt_factor`
   - Timing formula: `tilt_angle = elapsedSeconds * (1.56 + (cardId / 1.14212) % 1) + cardId / 1.35122`
   - **This matches Balatro's card.lua:4380-4383 EXACTLY** ✅
   
2. **`ResponsiveCard.axaml.cs`** - Hover detection
   - `IsHovering` property prevents seizure flicker
   - Stops ambient sway during hover
   
3. **Juice-up animation** - Currently working and beloved ❤️
   - **DO NOT TOUCH THIS** - user wants to keep it

### ❌ What's Missing - The REAL Balatro Magic

From `card.lua:4370-4384`, Balatro has **THREE tilt modes**:

1. **FOCUS Mode** (`states.focus.is`) - Card being dragged
   ```lua
   self.tilt_var.mx = cursor_x + dx*card_width
   self.tilt_var.my = cursor_y + dy*card_height
   self.tilt_var.amt = abs(hover_offset.y + hover_offset.x - 1 + dx + dy - 1) * 0.3
   ```

2. **HOVER Mode** (`states.hover.is`) - Card being hovered (MAGNETIC TILT)
   ```lua
   self.tilt_var.mx = cursor_x
   self.tilt_var.my = cursor_y  
   self.tilt_var.amt = abs(hover_offset.y + hover_offset.x - 1) * 0.3
   ```
   - **This is what creates the "magnetic" effect** - card tilts toward mouse!
   
3. **AMBIENT Mode** (`ambient_tilt`) - Breathing motion
   ```lua
   local tilt_angle = REAL_TIME*(1.56 + (ID/1.14212)%1) + ID/1.35122
   self.tilt_var.mx = ((0.5 + 0.5*ambient_tilt*cos(tilt_angle))*card_width + x) * scale
   self.tilt_var.my = ((0.5 + 0.5*ambient_tilt*sin(tilt_angle))*card_height + y) * scale
   self.tilt_var.amt = ambient_tilt*(0.5+cos(tilt_angle))*tilt_factor
   ```
   - **We already have this!** ✅

### 🎯 What Needs Implementation

**MAGNETIC TILT ON HOVER** - The missing magic that makes Balatro cards feel ALIVE!

The card should **tilt toward the mouse cursor** when hovered:
- Calculate `hover_offset.x` and `hover_offset.y` (mouse position relative to card center, normalized -1 to 1)
- Tilt amount = `abs(hover_offset.y + hover_offset.x - 1) * 0.3`
- This creates the "card is watching you" effect

## Implementation Plan

### Phase 1: Add Hover Offset Calculation to ResponsiveCard
```csharp
public double HoverOffsetX { get; private set; }
public double HoverOffsetY { get; private set; }

private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    var pos = e.GetPosition(this);
    var centerX = Bounds.Width / 2;
    var centerY = Bounds.Height / 2;
    
    // Normalize to -1 to 1 range (Balatro style)
    HoverOffsetX = (pos.X - centerX) / centerX;
    HoverOffsetY = (pos.Y - centerY) / centerY;
    
    // Notify behavior of hover offset change
    OnPropertyChanged(nameof(HoverOffsetX));
    OnPropertyChanged(nameof(HoverOffsetY));
}
```

### Phase 2: Update BalatroCardSwayBehavior with Magnetic Tilt Mode
```csharp
private void OnAnimationTick(object? sender, EventArgs e)
{
    // Find ResponsiveCard parent
    if (parent is Components.ResponsiveCard card)
    {
        if (card.IsHovering)
        {
            // MAGNETIC TILT MODE (Balatro's hover.is state)
            var hover_offset_total = Math.Abs(card.HoverOffsetY + card.HoverOffsetX - 1);
            var tilt_amt = hover_offset_total * UIConstants.CardTiltFactorRadians;
            
            // Tilt toward mouse position
            var tilt_direction = Math.Atan2(card.HoverOffsetY, card.HoverOffsetX);
            rotateTransform.Angle = tilt_amt * tilt_direction * UIConstants.CardRotationToDegrees;
            return;
        }
        else
        {
            // AMBIENT MODE - existing breathing animation
            // (keep current code)
        }
    }
}
```

### Phase 3: Test & Polish
1. Test on joker shelf items
2. Test on drop zone items
3. Test on playing cards
4. Ensure smooth transition between ambient → hover → ambient
5. **DO NOT BREAK THE JUICE-UP ANIMATION**

## Success Criteria
✅ Cards have subtle breathing motion when idle (ALREADY WORKS)  
✅ Cards tilt toward mouse when hovered (NEW - magnetic effect)  
✅ Smooth transition between animation modes  
✅ No seizure flicker (already fixed)  
✅ Juice-up animation still works  
✅ Feels EXACTLY like Balatro

## Risks & Mitigation
- **Risk**: Breaking existing juice-up animation
  - **Mitigation**: Don't touch scale transforms, only rotation
  
- **Risk**: Hover jitter/flicker
  - **Mitigation**: Use existing `IsHovering` flag and reset logic
  
- **Risk**: Performance impact
  - **Mitigation**: Already running at 60 FPS, just changing rotation calculation

## References
- Balatro source: `external/Balatro/card.lua:4370-4384`
- Current implementation: `src/Behaviors/BalatroCardSwayBehavior.cs`
- Card component: `src/Components/ResponsiveCard.axaml.cs`
- Constants: `src/Constants/UIConstants.cs`

## Decision Required
**Should we proceed with Phase 1-3 implementation?**

The research is done, the plan is solid, and we know exactly what Balatro does. Ready to execute when you give the go-ahead! 🎯
