# CRITICAL: Seizure-Inducing Card Tilt Feedback Loop

**Status:** 🔴 CRITICAL UX BUG
**Priority:** P0 - Fix Immediately
**Attempt:** #5 (Previous 4 attempts FAILED)
**Generated:** 2025-11-04

---

## The Problem (ROOT CAUSE)

### Feedback Loop Explained

When mouse pointer is positioned between two cards in the item shelf:

```
Step 1: Mouse pointer touches card edge
Step 2: Card tilts AWAY from mouse (visual transform applied)
Step 3: ❌ BUG: Visual transform MOVES THE HITBOX with it
Step 4: Card is no longer "under mouse" (hitbox moved away)
Step 5: Card detects "pointer left" and animates back to normal
Step 6: Hitbox moves back under mouse pointer
Step 7: GOTO Step 1 → INFINITE SEIZURE LOOP
```

**Result:** Card rapidly toggles between tilted/untilted states, creating a seizure-inducing flicker effect.

---

## Why Previous Fixes Failed

**Previous attempts** likely tried to:
- Adjust animation timing (doesn't fix root cause)
- Add debouncing (doesn't fix root cause)
- Tweak hit detection zones (doesn't fix root cause)
- Adjust hover thresholds (doesn't fix root cause)

**None of these address the ACTUAL problem:** Visual transforms should NOT affect hit testing!

---

## The REAL Fix

### Core Principle

**Visual transforms MUST be separated from hit testing.**

The card's **logical bounds** (used for hit testing) should remain FIXED in their original position, even when the card **visual appearance** tilts/rotates.

### Implementation Strategy

In Avalonia/WPF, this is achieved using `RenderTransform` instead of `LayoutTransform`:

```csharp
// ❌ WRONG - Moves hitbox with visual
card.LayoutTransform = new RotateTransform(angle);

// ✅ CORRECT - Visual only, hitbox stays fixed
card.RenderTransform = new RotateTransform(angle);
card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
```

### Key Differences

| Transform Type | Affects Layout | Affects Hit Testing | Use For Hover Effects |
|----------------|----------------|---------------------|----------------------|
| **LayoutTransform** | ✅ Yes | ✅ Yes | ❌ NO - Causes feedback loop |
| **RenderTransform** | ❌ No | ❌ No | ✅ YES - Safe for hover |

---

## Files to Fix

### Primary Suspect: Card Hover Behavior

**Likely locations:**
- `src/Behaviors/*.cs` - Any behavior handling card tilt/hover
- `src/Controls/ResponsiveCard.axaml.cs` - Card hover handlers
- `src/Controls/ResponsiveCard.axaml` - Card XAML with transforms
- Any file with `PointerEntered`/`PointerExited` + tilt animation

### Search Strategy

```bash
# Find all files with tilt/rotate animations
grep -rn "RotateTransform\|ScaleTransform\|SkewTransform" src/ --include="*.cs" --include="*.axaml"

# Find hover handlers
grep -rn "PointerEntered\|PointerExited\|PointerMoved" src/Controls/ --include="*.cs"

# Find card-related behaviors
find src/Behaviors/ -name "*Card*" -o -name "*Hover*" -o -name "*Tilt*"
```

---

## Acceptance Criteria

### Must Have
- [ ] Card tilt animation uses `RenderTransform` (NOT `LayoutTransform`)
- [ ] Hitbox remains in original position during tilt
- [ ] No flickering/seizure effect when mouse is between cards
- [ ] Tilt animation still works smoothly
- [ ] Hover state changes cleanly (no rapid toggling)

### Test Cases

**Test 1: Edge Hover**
1. Position mouse pointer EXACTLY on the edge between two cards
2. Hold mouse steady for 3 seconds
3. ✅ PASS: Cards should NOT flicker or toggle rapidly
4. ✅ PASS: One card (or both) should show stable hover state

**Test 2: Slow Movement**
1. Move mouse slowly across card boundaries
2. ✅ PASS: Smooth transition from card A → card B
3. ✅ PASS: No seizure effect or rapid toggling

**Test 3: Fast Movement**
1. Move mouse quickly across multiple cards
2. ✅ PASS: No glitching or stuck animations
3. ✅ PASS: Cards should animate smoothly

**Test 4: Diagonal Movement**
1. Move mouse diagonally across card grid
2. ✅ PASS: No unexpected behavior at corners
3. ✅ PASS: Hover states update correctly

---

## Implementation Steps

### Step 1: Find the Card Tilt Implementation (5 min)

```bash
# Search for card tilt/hover behavior
grep -rn "PointerEntered.*tilt\|RotateTransform" src/ --include="*.cs" -B 5 -A 10
```

### Step 2: Identify Transform Type (2 min)

Look for:
```csharp
// If you see LayoutTransform → THIS IS THE BUG
card.LayoutTransform = ...

// If you see RenderTransform → Check if it's configured correctly
card.RenderTransform = ...
```

### Step 3: Replace with RenderTransform (10 min)

```csharp
// BEFORE (WRONG):
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    var card = sender as Control;
    var angle = CalculateTiltAngle(e.GetPosition(card));
    card.LayoutTransform = new RotateTransform(angle);  // ❌ MOVES HITBOX
}

// AFTER (CORRECT):
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    var card = sender as Control;
    var angle = CalculateTiltAngle(e.GetPosition(card));

    // Use RenderTransform instead - hitbox stays fixed
    card.RenderTransform = new RotateTransform(angle);
    card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
}
```

### Step 4: Test Edge Cases (10 min)

- Test with mouse between cards
- Test with overlapping card hitboxes
- Test rapid mouse movement
- Test slow mouse movement

### Step 5: Add Safeguards (5 min)

```csharp
// Add pointer capture to prevent glitching
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    var card = sender as Control;

    // Apply tilt using RenderTransform
    card.RenderTransform = new RotateTransform(angle);
    card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

    // Optional: Capture pointer to stabilize hover state
    // card.CapturePointer(e.Pointer);
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    var card = sender as Control;

    // Reset transform
    card.RenderTransform = null;

    // Optional: Release pointer capture
    // card.ReleasePointerCapture(e.Pointer);
}
```

---

## Expected Outcome

### Before Fix
- 🔴 Mouse between cards → Seizure-inducing flicker
- 🔴 Hitbox moves with tilt animation
- 🔴 Pointer events trigger incorrectly

### After Fix
- ✅ Mouse between cards → Stable, no flicker
- ✅ Hitbox remains in original position
- ✅ Smooth hover transitions
- ✅ Tilt animation still looks great

---

## Technical Deep Dive

### Why RenderTransform Doesn't Affect Hit Testing

In Avalonia (and WPF):

1. **LayoutTransform** is applied BEFORE layout pass
   - Affects measure/arrange
   - Changes logical bounds
   - **Hitbox follows the transform**

2. **RenderTransform** is applied AFTER layout pass
   - Only affects visual rendering
   - Does NOT change logical bounds
   - **Hitbox stays in original position**

This is EXACTLY what we need for hover effects!

---

## Debugging Tips

### Add Debug Logging

```csharp
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    var card = sender as Control;
    DebugLogger.Log("CardHover", $"ENTER: {card.Name} at {e.GetPosition(card)}");

    // Apply tilt...
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    var card = sender as Control;
    DebugLogger.Log("CardHover", $"EXIT: {card.Name}");

    // Reset tilt...
}
```

If you see rapid ENTER/EXIT logs → Feedback loop confirmed!

### Visual Debugging

```csharp
// Add border to visualize actual hitbox
card.BorderBrush = Brushes.Red;
card.BorderThickness = new Thickness(2);
```

The red border should NOT move with the tilt (if using RenderTransform correctly).

---

## Rollback Plan

If fix causes issues:
1. Revert changes via git
2. Disable tilt animation temporarily:
   ```csharp
   // Quick disable while debugging
   if (true) return; // Skip tilt animation
   ```

---

## Estimated Effort

- **Finding the bug:** 5 minutes
- **Implementing fix:** 10 minutes
- **Testing:** 10 minutes
- **Total:** 25 minutes

(Should be quick if we fix the ROOT CAUSE instead of band-aiding symptoms)

---

## Success Metrics

- ✅ Zero reports of seizure/flicker effect
- ✅ Smooth hover transitions
- ✅ Tilt animation still looks great
- ✅ No performance degradation

---

## Notes for Coding Agent

**DO NOT:**
- Add animation delays (doesn't fix root cause)
- Add pointer capture everywhere (band-aid)
- Adjust hover thresholds (band-aid)
- Add debouncing (band-aid)

**DO:**
- Find the LayoutTransform usage
- Replace with RenderTransform
- Test edge hover case
- Verify hitbox doesn't move

**This is attempt #5. GET IT RIGHT THIS TIME.**

---

## Root Cause Summary

```
┌─────────────────────────────────────┐
│  LayoutTransform (WRONG)            │
│  • Moves hitbox with visual         │
│  • Creates feedback loop            │
│  • Causes seizure effect            │
└─────────────────────────────────────┘
                 ↓
          REPLACE WITH
                 ↓
┌─────────────────────────────────────┐
│  RenderTransform (CORRECT)          │
│  • Hitbox stays fixed               │
│  • No feedback loop                 │
│  • Smooth hover effect              │
└─────────────────────────────────────┘
```

---

**Assignee:** csharp-avalonia-expert agent

**References:**
- [Avalonia RenderTransform Docs](https://docs.avaloniaui.net/docs/concepts/transforms)
- [WPF LayoutTransform vs RenderTransform](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/transforms-overview)
