# Visual Builder Card Display Improvements - Product Requirements Document

**Date**: 2025-01-03
**Feature**: Improve Visual Builder Drop-Zone and OR/AND Clause Card Rendering
**Status**: READY FOR IMPLEMENTATION
**Priority**: HIGH - UI/UX Polish

---

## Executive Summary

Fix the visual builder drop-zone indicator and redesign OR/AND clause rendering to use elegant fanned-out joker card displays instead of the current messy tray layout.

---

## Current Problems

1. **Drop-Zone Indicator Issues**:
   - Has ugly emoji arrows (🟩) that need to be removed
   - Size doesn't match expanded drop-zone (currently 1/3 size)
   - Shows even when not hovering over collapsed drop-zones

2. **OR/AND Clause Rendering Issues**:
   - Cards are misaligned, different sizes, squished together
   - Labels overlap or cut into card images
   - Nested borders look terrible
   - No proper spacing or visual hierarchy

---

## Scope Clarification

### IN SCOPE (Visual Builder Area Only)
These improvements apply to the **main visual builder area** where you construct filter logic with MUST/SHOULD/OR/AND clauses.

### OUT OF SCOPE (Keep Current Behavior)
The **top shelf editing mode** (2 trays where you drop jokers) keeps current behavior - DO NOT modify.

---

## Requirements

### 1. Drop-Zone Indicator Improvements

#### Remove Emojis
- Remove all emoji characters from drop-zone indicator text
- Use plain text: "DROP IN MUST HAVE" instead of "🟩 DROP IN MUST HAVE 🟩"

#### Match Expanded Drop-Zone Size
- Drop-zone indicator should be same width/height as expanded drop-zone
- NOT 1/3 size - full size

#### Hover-Only Display
- Only show drop-zone indicator when hovering over a **collapsed** drop-zone
- When expanded, no need for indicator (already visible)
- When not hovering, no indicator shown

### 2. OR/AND Clause Card Rendering Redesign

#### New Container Design
Replace current "tray" rendering with card-style container:

**Structure**:
```
┌─────────────────────────────┐
│  OR                         │  ← Blue header (thin, just enough for text)
├─────────────────────────────┤
│                             │
│   [Fanned Joker Cards]      │  ← Cards displayed poker-hand style
│                             │
└─────────────────────────────┘
```

**Visual Specs**:
- Thin blue border around entire container
- Solid blue section at top (height: just enough for "OR" or "AND" text)
- White text in header ("OR" or "AND")
- Transparent/dark background inside container
- Cards rendered with fanned-out effect (like holding poker hand)

#### Fanned-Out Card Display

**Layout**:
- Cards overlap slightly (10-15px offset)
- Each card rotated slightly (5-10 degrees alternating)
- Creates poker hand "fan" effect
- Cards maintain aspect ratio (no squishing)
- Proper spacing and z-index ordering

**Example** (3 cards):
```
    ┌──┐
   ┌┼──┼┐
  ┌┼┼──┼┼┐
  │││  │││
  │││  │││
  └┴┴──┴┴┘
```

#### Drag-and-Drop Support
- Entire OR/AND container can be dragged back out
- Individual cards inside can also be dragged out
- Drop zones work same as before

---

## Implementation Details

### Files to Modify

| File | Change |
|------|--------|
| `Components/FilterTabs/VisualBuilderTab.axaml` | Update drop-zone indicator XAML |
| `ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` | Add hover state properties |
| `Components/FilterOperatorControl.axaml` | Redesign OR/AND rendering |
| `Components/FilterOperatorControl.axaml.cs` | Add fanned card layout logic |
| `Styles/BalatroGlobalStyles.axaml` | Add card-container styles |

### Drop-Zone Indicator XAML

**Before**:
```xml
<TextBlock Text="🟩 DROP IN MUST HAVE 🟩" .../>
```

**After**:
```xml
<TextBlock Text="DROP IN MUST HAVE"
           IsVisible="{Binding IsHoveringMustDropZone}"
           .../>
```

### OR/AND Container XAML

```xml
<Border Classes="operator-card-container">
    <!-- Header -->
    <Border Classes="operator-header">
        <TextBlock Text="{Binding OperatorType}"
                   Classes="operator-label"/>
    </Border>

    <!-- Fanned Cards -->
    <Canvas Classes="fanned-cards-canvas">
        <ItemsControl ItemsSource="{Binding Items}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <Canvas/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <!-- Individual card with rotation/offset -->
                    <Border RenderTransformOrigin="0.5,0.5">
                        <Border.RenderTransform>
                            <TransformGroup>
                                <RotateTransform Angle="{Binding FanAngle}"/>
                                <TranslateTransform X="{Binding FanOffsetX}"/>
                            </TransformGroup>
                        </Border.RenderTransform>
                        <!-- Card content here -->
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Canvas>
</Border>
```

### Fanned Card Layout Algorithm

```csharp
private void CalculateFannedPositions(ObservableCollection<CardItem> items)
{
    int count = items.Count;
    double baseAngle = -10.0; // Start angle
    double angleDelta = 5.0;  // Angle between cards
    double xOffset = 15.0;    // Horizontal offset

    for (int i = 0; i < count; i++)
    {
        items[i].FanAngle = baseAngle + (i * angleDelta);
        items[i].FanOffsetX = i * xOffset;
        items[i].ZIndex = i; // Cards on right appear in front
    }
}
```

---

## Styles

### Operator Card Container
```xml
<Style Selector="Border.operator-card-container">
    <Setter Property="BorderBrush" Value="{StaticResource Blue}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Background" Value="{StaticResource DarkGrey}"/>
    <Setter Property="Padding" Value="0"/>
</Style>

<Style Selector="Border.operator-header">
    <Setter Property="Background" Value="{StaticResource Blue}"/>
    <Setter Property="Padding" Value="12,6"/>
    <Setter Property="CornerRadius" Value="6,6,0,0"/>
</Style>

<Style Selector="TextBlock.operator-label">
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="HorizontalAlignment" Value="Center"/>
</Style>
```

---

## Success Criteria

1. ✅ Drop-zone indicator has no emojis
2. ✅ Drop-zone indicator matches expanded drop-zone size
3. ✅ Drop-zone indicator only shows on hover over collapsed zone
4. ✅ OR/AND clauses use card-style container with blue border
5. ✅ OR/AND header shows "OR" or "AND" text in white on blue background
6. ✅ Cards inside OR/AND are fanned out like poker hand
7. ✅ Cards maintain aspect ratio (no squishing)
8. ✅ Fanned cards have proper rotation and offset
9. ✅ OR/AND containers can be dragged back out
10. ✅ Individual cards can be dragged from OR/AND containers

---

## Visual Mockup

### Before (Current - Messy):
```
╔═══════════════════════════════╗
║ MUST  🟩 (small)              ║
╠═══════════════════════════════╣
║ ┌──┐┌──┐┌──┐┌──┐             ║  ← Squished, misaligned
║ │  ││  ││  ││  │             ║
║ └──┘└──┘└──┘└──┘             ║
╚═══════════════════════════════╝
```

### After (Improved - Clean):
```
╔═══════════════════════════════╗
║ DROP IN MUST HAVE             ║  ← No emojis, full width
╠═══════════════════════════════╣
║ ┌────────────────┐            ║
║ │ OR             │            ║  ← Card container
║ ├────────────────┤            ║
║ │    ┌──┐        │            ║
║ │   ┌┼──┼┐       │            ║  ← Fanned cards
║ │  ┌┼┼──┼┼┐      │            ║
║ │  └┴┴──┴┴┘      │            ║
║ └────────────────┘            ║
╚═══════════════════════════════╝
```

---

## Testing

1. **Drop-Zone Indicator**:
   - Hover over collapsed MUST zone → indicator appears, no emojis
   - Hover away → indicator disappears
   - Expand zone → no indicator needed
   - Verify size matches expanded zone

2. **OR/AND Container**:
   - Create OR clause with 3 jokers → cards fan out nicely
   - Verify blue border and header
   - Verify "OR" text in white on blue background
   - Drag entire container back out → works
   - Drag individual card out → works

3. **Card Rendering**:
   - Verify no squishing (cards maintain aspect ratio)
   - Verify rotation angles look natural (not too extreme)
   - Verify z-index ordering (right cards in front)
   - Verify spacing is consistent

---

**Status**: Ready for Implementation with Avalonia Expert Agent
**Complexity**: MEDIUM (XAML styling + layout logic)
**Risk**: LOW (isolated to visual builder, doesn't affect filter logic)
