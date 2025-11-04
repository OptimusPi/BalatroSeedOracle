# Card Animation System - Match Real Balatro Behavior

**Status:** 🔴 CRITICAL - Animations Don't Match Real Game
**Priority:** P0 - Fix Immediately
**Generated:** 2025-11-04

---

## The Problem

We've been implementing the WRONG animation system! After reviewing the actual Balatro Lua code, we have:

**What We Implemented:**
- ✅ Ambient sway (breathing) runs continuously
- ❌ NO magnetic tilt toward mouse
- ❌ Sway doesn't stop on hover
- ❌ Effects conflict → seizure bug

**What Real Balatro Does:**
- ✅ Ambient sway ONLY when idle (not hovering)
- ✅ Magnetic 3D tilt toward mouse on hover
- ✅ Drag physics sway when dragging
- ✅ Clean state transitions between modes

---

## Real Balatro Animation States

From `external/Balatro/card.lua:4371-4383`:

### State 1: Dragging (states.focus.is)
```lua
self.tilt_var.mx = cursor_x + drag_offset_x
self.tilt_var.my = cursor_y + drag_offset_y
self.tilt_var.amt = hover_offset_based_tilt
```
**Effect:** Card tilts with drag physics

### State 2: Hovering (states.hover.is)
```lua
self.tilt_var.mx = G.CONTROLLER.cursor_position.x
self.tilt_var.my = G.CONTROLLER.cursor_position.y
self.tilt_var.amt = math.abs(hover_offset) * tilt_factor
```
**Effect:** Magnetic 3D tilt toward cursor (NOT sway!)

### State 3: Idle (ambient_tilt)
```lua
tilt_angle = G.TIMERS.REAL*(1.56 + (ID/1.14212)%1) + ID/1.35122
tilt_var.mx = (0.5 + 0.5*ambient_tilt*cos(tilt_angle)) * width + x
tilt_var.my = (0.5 + 0.5*ambient_tilt*sin(tilt_angle)) * height + y
tilt_var.amt = ambient_tilt*(0.5+cos(tilt_angle))*tilt_factor
```
**Effect:** Gentle breathing sway animation

---

## Current Implementation Issues

### Issue #1: BalatroCardSwayBehavior Runs Continuously
**File:** `src/Behaviors/BalatroCardSwayBehavior.cs`
**Problem:** Ambient sway runs in timer tick every frame, even during hover
**Result:** Conflicts with hover scale effect → seizure

### Issue #2: No Magnetic Tilt on Hover
**File:** `src/Components/ResponsiveCard.axaml.cs`
**Problem:** OnPointerEntered only does scale effect, no tilt toward mouse
**Result:** Cards don't feel responsive to cursor position

### Issue #3: No State Management
**Problem:** No way to switch between idle/hover/drag states cleanly
**Result:** Effects overlap and conflict

---

## The Correct Implementation

### Architecture

```
ResponsiveCard (Control)
├─ State: Idle / Hovering / Dragging
├─ When Idle:
│  └─ BalatroCardSwayBehavior (ambient tilt)
├─ When Hovering:
│  └─ MagneticTiltBehavior (tilt toward mouse)
└─ When Dragging:
   └─ DragPhysicsBehavior (drag sway)
```

### Step 1: Add State Property to ResponsiveCard

```csharp
public enum CardAnimationState
{
    Idle,       // Ambient breathing sway
    Hovering,   // Magnetic tilt toward mouse
    Dragging    // Drag physics sway
}

private CardAnimationState _animationState = CardAnimationState.Idle;
```

### Step 2: Modify BalatroCardSwayBehavior to Respect State

```csharp
private void OnAnimationTick(object? sender, EventArgs e)
{
    // Check if card is in hover state
    if (AssociatedObject is ResponsiveCard card && card.IsHovering)
    {
        // STOP ambient sway during hover!
        return;
    }

    // Only run ambient sway when idle
    // ... existing ambient tilt code
}
```

### Step 3: Create MagneticTiltBehavior

```csharp
public class MagneticTiltBehavior : Behavior<Control>
{
    private DispatcherTimer? _tiltTimer;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null) return;

        // Track pointer position
        AssociatedObject.PointerMoved += OnPointerMoved;

        // Start tilt update timer
        _tiltTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _tiltTimer.Tick += UpdateMagneticTilt;
        _tiltTimer.Start();
    }

    private Point? _lastPointerPosition;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _lastPointerPosition = e.GetPosition(AssociatedObject);
    }

    private void UpdateMagneticTilt(object? sender, EventArgs e)
    {
        if (AssociatedObject == null || _lastPointerPosition == null) return;

        // Get pointer position relative to card center
        var cardCenter = new Point(
            AssociatedObject.Bounds.Width / 2,
            AssociatedObject.Bounds.Height / 2
        );

        var offsetX = (_lastPointerPosition.Value.X - cardCenter.X) / cardCenter.X;
        var offsetY = (_lastPointerPosition.Value.Y - cardCenter.Y) / cardCenter.Y;

        // Calculate tilt angle (Balatro uses ~0.3 tilt factor)
        var tiltFactor = 0.3;
        var tiltAmount = Math.Abs(offsetY + offsetX) * tiltFactor;

        // Calculate rotation angle toward mouse
        var angle = Math.Atan2(offsetY, offsetX) * (180 / Math.PI);

        // Find RotateTransform and apply
        if (AssociatedObject.RenderTransform is TransformGroup group)
        {
            var rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
            if (rotateTransform != null)
            {
                rotateTransform.Angle = angle * tiltAmount;
            }
        }
    }
}
```

### Step 4: Update ResponsiveCard to Switch States

```csharp
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    _animationState = CardAnimationState.Hovering;
    IsHovering = true; // For BalatroCardSwayBehavior to check

    PlayHoverThudAnimation();

    // Enable magnetic tilt, disable ambient sway
    EnableMagneticTilt();
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    _animationState = CardAnimationState.Idle;
    IsHovering = false;

    AnimateScale(1.0, TimeSpan.FromMilliseconds(150));

    // Disable magnetic tilt, enable ambient sway
    DisableMagneticTilt();
}
```

---

## Implementation Plan

### Phase 1: Fix State Management (30 min)
1. Add `IsHovering` property to ResponsiveCard
2. Set `IsHovering = true` in OnPointerEntered
3. Set `IsHovering = false` in OnPointerExited
4. Modify BalatroCardSwayBehavior to check `IsHovering` and return early if true

### Phase 2: Implement Magnetic Tilt (1 hour)
1. Create `MagneticTiltBehavior.cs`
2. Implement pointer tracking and tilt calculation
3. Apply tilt to RotateTransform in TransformGroup
4. Test hover tilt effect

### Phase 3: Attach Behaviors Conditionally (30 min)
1. Add MagneticTiltBehavior to ResponsiveCard.axaml
2. Set `IsEnabled` binding to `IsHovering` property
3. Test state transitions

### Phase 4: Test & Polish (30 min)
1. Test idle → hover → idle transitions
2. Verify no seizure effect
3. Verify smooth tilt toward mouse
4. Match Balatro feel

---

## Files to Modify

### 1. ResponsiveCard.axaml.cs
- Add `IsHovering` property
- Set state in OnPointerEntered/Exited
- Expose property for behaviors to check

### 2. BalatroCardSwayBehavior.cs
- Check `IsHovering` before running ambient sway
- Return early if hovering

### 3. MagneticTiltBehavior.cs (NEW FILE)
- Create new behavior for hover tilt
- Track pointer position
- Calculate tilt toward mouse
- Apply to RotateTransform

### 4. ResponsiveCard.axaml
- Keep BalatroCardSwayBehavior (for idle sway)
- Add MagneticTiltBehavior (for hover tilt)
- Both attached, state-managed by IsHovering

---

## Expected Behavior

### Before Fix
- 🔴 Cards sway continuously (even when hovering)
- 🔴 No magnetic tilt toward mouse
- 🔴 Seizure effect when mouse between cards
- 🔴 Doesn't match real Balatro

### After Fix
- ✅ Cards sway gently when idle
- ✅ Cards tilt toward mouse on hover (magnetic 3D effect)
- ✅ Clean state transitions
- ✅ No seizure effect
- ✅ Matches real Balatro behavior

---

## Acceptance Criteria

- [ ] Ambient sway ONLY runs when NOT hovering
- [ ] Magnetic tilt activates on hover
- [ ] Tilt direction follows mouse position
- [ ] No seizure effect when mouse between cards
- [ ] Smooth transitions between states
- [ ] Feels like real Balatro

---

## Test Plan

### Test 1: Idle State
1. Don't hover over any cards
2. **Expected:** Cards gently sway with breathing animation
3. **Expected:** Sway uses cos/sin waves (organic motion)

### Test 2: Hover State
1. Move mouse over a card
2. **Expected:** Sway stops immediately
3. **Expected:** Card tilts toward mouse cursor (3D effect)
4. **Expected:** Tilt angle follows mouse movement

### Test 3: Hover Exit
1. Move mouse off card
2. **Expected:** Magnetic tilt stops
3. **Expected:** Ambient sway resumes smoothly

### Test 4: Edge Case (Mouse Between Cards)
1. Position mouse between two cards
2. **Expected:** NO seizure/flicker effect
3. **Expected:** Both cards in correct state (one or both hovering)

---

## Estimated Effort

- Phase 1 (State management): 30 minutes
- Phase 2 (Magnetic tilt): 1 hour
- Phase 3 (Integration): 30 minutes
- Phase 4 (Testing): 30 minutes
- **Total: 2.5 hours**

---

## Notes

The seizure bug is a SYMPTOM of the wrong animation system. Fixing the root cause (matching real Balatro's state-based animations) will eliminate it permanently.

**Key Insight from Balatro Code:**
The animations are MUTUALLY EXCLUSIVE based on state:
- `if dragging → drag physics`
- `else if hovering → magnetic tilt`
- `else if ambient_tilt → breathing sway`

We were running sway continuously, which is wrong!

---

## References

- Balatro source: `external/Balatro/card.lua:4371-4383`
- Current implementation: `src/Behaviors/BalatroCardSwayBehavior.cs`
- Card component: `src/Components/ResponsiveCard.axaml.cs`
