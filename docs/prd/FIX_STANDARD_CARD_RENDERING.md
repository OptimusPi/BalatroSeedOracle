# Fix StandardCard Sprite Rendering

**Status:** 🟡 IN PROGRESS
**Priority:** P0 - BLOCKING FEATURE
**Date:** 2025-11-04

---

## Problem

StandardCards are rendering as **white rectangles** with no rank/suit patterns visible.

### Root Cause (CONFIRMED)

JSON deserialization is **case-sensitive** and failing to parse coordinates.

**File:** `src/Services/SpriteService.cs` lines 1542-1546

```csharp
private sealed class CardPosition
{
    public int X { get; set; }  // ❌ Expects uppercase "X"
    public int Y { get; set; }  // ❌ Expects uppercase "Y"
}
```

**But the JSON has lowercase:**

```json
"Hearts": {
  "2": { "x": 0, "y": 0 },  // ❌ lowercase "x" and "y"
  "3": { "x": 1, "y": 0 }
}
```

**Result:** All cards default to `{ X: 0, Y: 0 }` because deserialization fails silently.

### Evidence from Logs

```
[SpriteService] Loading pattern 2 of Hearts from position (0, 0) -> pixel (0, 0)
[SpriteService] Loading pattern 3 of Hearts from position (0, 0) -> pixel (0, 0)
[SpriteService] Loading pattern 4 of Hearts from position (0, 0) -> pixel (0, 0)
```

**Every card loads from (0, 0) instead of their correct coordinates!**

---

## The Fix

Add `[JsonPropertyName]` attributes to handle lowercase JSON properties:

```csharp
private sealed class CardPosition
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
```

---

## Implementation Steps

### Step 1: Add JsonPropertyName attributes

**File:** `src/Services/SpriteService.cs` line 1542

```csharp
private sealed class CardPosition
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
```

### Step 2: Build and test

Run the app and click StandardCard category.

**Expected result:** Cards show rank/suit patterns!

---

## Test Plan

1. ✅ Build succeeds
2. ✅ Run app
3. ✅ Click StandardCard category
4. ✅ Verify Hearts section shows 13 different cards (2-Ace)
5. ✅ Verify each card shows correct rank/suit pattern
6. ✅ Verify Mult/Bonus/Glass/Gold/Steel cards show enhancements
7. ✅ Verify Stone card shows (no rank/suit)

---

## Success Criteria

- ✅ All 52 base cards render with correct rank/suit patterns
- ✅ Enhanced cards (Mult/Bonus/Glass/Gold/Steel) show correct base + pattern
- ✅ Stone card shows Stone enhancement only (no pattern)
- ✅ Log shows different positions: `(0, 0)`, `(1, 0)`, `(2, 0)`, etc.

---

## Files to Modify

| File | Change | Lines |
|------|--------|-------|
| `src/Services/SpriteService.cs` | Add `[JsonPropertyName]` attributes | 1542-1546 |

---

## Estimated Effort

**2 minutes** - One-line fix

---

## Notes

This is why the debug logging was CRITICAL. Without it, we would have spent hours guessing why compositing wasn't working when the real issue was metadata parsing!

**The metadata JSON is correct.** The C# model just needs the proper attributes.

