# Fix Widget Clickability Regression

## Problem
Widgets (minimized icons on left side) are NOT clickable after commit f5883a6.

## What Happened
**Lola's commit 4045840:** Set MainContent Grid `IsHitTestVisible="False"` → menu buttons unclickable
**My "fix" f5883a6:** Changed to `IsHitTestVisible="True"` → menu buttons work BUT widgets broken

## Root Cause
MainContent Grid has `Background="Transparent"` + `IsHitTestVisible="True"`. This makes the ENTIRE grid capture all clicks, even in transparent areas, blocking widgets that are rendered OUTSIDE or BEHIND it.

## Evidence
BalatroMainMenu.axaml line 166:
```xml
<Grid x:Name="MainContent" ZIndex="5" IsVisible="{Binding !IsVibeOutMode}"
      Background="Transparent" IsHitTestVisible="True">
```

Widgets are probably in a different container with different ZIndex.

## Solution Options

**Option 1:** Remove Background="Transparent"
- Let only actual elements (buttons, etc.) capture hits
- Transparent background with IsHitTestVisible="True" captures EVERYTHING

**Option 2:** Keep MainContent IsHitTestVisible="False", add IsHitTestVisible="True" to individual buttons
- More targeted fix
- Only buttons capture hits, not entire grid

**Option 3:** Check widget container ZIndex and ensure it's HIGHER than MainContent
- Widgets should be on top
- If ZIndex is correct, investigate layout hierarchy

## Investigation Steps
1. Find widget container in BalatroMainMenu.axaml
2. Check ZIndex values (widgets should be > 5)
3. Check if widgets are INSIDE or OUTSIDE MainContent Grid
4. Test: Remove Background="Transparent" from MainContent - does it fix both?

## Files
- X:\BalatroSeedOracle\src\Views\BalatroMainMenu.axaml (line 166)

## Solution Applied
Removed `Background="Transparent"` from MainContent Grid (line 166).

### Why This Works
In AvaloniaUI (and WPF), when a control has:
- `IsHitTestVisible="True"` (enabled)
- `Background` set to ANY value (including "Transparent")

The control captures ALL mouse/pointer events within its bounds, even in visually transparent areas.

When Background is NOT set (null):
- The control only captures hits on its actual child elements
- Empty space allows events to pass through to controls behind it

### Technical Details
**Before:** MainContent Grid with Background="Transparent" + IsHitTestVisible="True"
- Result: Entire grid area captures all clicks (like an invisible overlay)
- Widgets behind it (ZIndex 0-1) cannot receive clicks
- Menu buttons inside MainContent work (they're children of MainContent)

**After:** MainContent Grid with NO Background + IsHitTestVisible="True"
- Result: Only actual child elements (buttons, text) capture clicks
- Empty space passes clicks through to widgets below
- Both menu buttons AND widgets are clickable

### ZIndex Layout
```
DesktopCanvas (ZIndex: default 0)
  └─ Widgets (ZIndex: 1 minimized, 100+ expanded)
MainContent (ZIndex: 5, no background)
  └─ Menu buttons (inherit parent hit testing)
ModalContainer (ZIndex: 100)
```

## Acceptance
- [x] Menu buttons clickable
- [x] Widgets clickable
- [x] No Z-index fighting
- [x] Build succeeds
