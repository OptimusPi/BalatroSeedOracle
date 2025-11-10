# Fix Button Font Size Inheritance

## Problem
Button FontSize properties are being overridden by global `TextBlock` style (line 83 in BalatroGlobalStyles.axaml sets FontSize="14").

Main menu buttons have `FontSize="36"` but text renders at 14px.

## Evidence
- Main menu SEARCH button: `Content="SEARCH" FontSize="36"` → renders tiny
- TabItem buttons work correctly (have similar pattern)

## Root Cause
Global TextBlock style (lines 80-85) applies to ALL TextBlocks, including auto-generated ones inside buttons. Button FontSize property doesn't override it.

## Solution
Add style that makes TextBlocks inside buttons inherit from parent Button:

**Location:** BalatroGlobalStyles.axaml, BEFORE the global TextBlock style

```xml
<!-- TextBlocks inside buttons inherit Button properties -->
<Style Selector="Button TextBlock">
    <Setter Property="FontSize" Value="{Binding $parent[Button].FontSize}" />
    <Setter Property="FontFamily" Value="{Binding $parent[Button].FontFamily}" />
    <Setter Property="Foreground" Value="{Binding $parent[Button].Foreground}" />
</Style>
```

## Files
- X:\BalatroSeedOracle\src\Styles\BalatroGlobalStyles.axaml (insert at line 72, before line 80)

## Acceptance
- [ ] Main menu SEARCH button displays at 36px
- [ ] DESIGNER/ANALYZER buttons display at 28px
- [ ] No regression on other buttons
- [ ] Build succeeds
