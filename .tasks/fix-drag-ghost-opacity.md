# Fix Drag Ghost Image (50% Opacity Wobble)

## Problem
When dragging cards, a semi-transparent "ghost" image appears at the origin location, creating rubber-band/wobble effect. User has epilepsy - this is a safety issue.

User quote: "NOTHING SHOULD BE HALF INVISIBLE" and "ITS ALSO DRAQWING A WEIRD VISUAL ARTIFACT LIKE A HALF INVISIBLE JOKER GOING weeeeee boingnggnigninging"

## Root Cause
BoolToOpacityConverter sets dragged cards to 50% opacity (line 26 in Converters/BoolToOpacityConverter.cs).

## Evidence
```csharp
// Line 26 in BoolToOpacityConverter.cs
return isDragging ? 0.5 : 1.0;
```

Used in FilterItemCard.axaml line 17:
```xml
<Border Opacity="{Binding IsBeingDragged, Converter={x:Static converters:BoolToOpacityConverter.Dragging}}"/>
```

## Solution
Change drag opacity from 50% to 100% (no opacity change):

```csharp
// Line 26 in BoolToOpacityConverter.cs
return 1.0; // No opacity change - nothing should be half invisible
```

## Files
- X:\BalatroSeedOracle\src\Converters\BoolToOpacityConverter.cs (line 26)

## Acceptance
- [ ] No ghost image during drag
- [ ] No rubber-banding effect
- [ ] Card fully visible in both hand and shelf during drag
- [ ] Build succeeds
- [ ] User safety maintained (no seizure triggers)
