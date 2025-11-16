# PRD: Tab Control Animation System Enhancement

**Status:** 🟢 **WORKING** - Enhancement / Documentation
**Priority:** P2 - Polish & Extensibility
**Estimated Time:** 2-3 hours
**Generated:** 2025-11-14

---

## Executive Summary

The application uses **animated bouncing triangles** as selection indicators for tabbed interfaces and list navigation. This PRD documents the current implementation and proposes enhancements to make the animation system more reusable, customizable, and consistent across the application.

### Current Implementation Files:
- **Animations:** [BalatroAnimations.axaml](x:\BalatroSeedOracle\src\Styles\BalatroAnimations.axaml) (lines 1-76)
- **Tab Control:** [BalatroTabControl.axaml](x:\BalatroSeedOracle\src\Controls\BalatroTabControl.axaml) (lines 1-103)
- **Tab Control Logic:** [BalatroTabControl.axaml.cs](x:\BalatroSeedOracle\src\Controls\BalatroTabControl.axaml.cs)

### What Works:
- ✅ Vertical bouncing triangles on horizontal tabs (top of tab control)
- ✅ Horizontal bouncing triangles on vertical list items (left of button)
- ✅ Smooth sine-wave easing animation
- ✅ Automatic positioning via code-behind
- ✅ Configurable animation classes

### What Can Be Improved:
- Inconsistent positioning logic (hardcoded offsets in different places)
- Animation parameters hardcoded (duration, distance)
- No support for other selection indicators (arrows, underlines, glows)
- Code-behind positioning could be more flexible
- No animation pause/resume on window focus loss

---

## Current Architecture

### Animation Definitions (BalatroAnimations.axaml)

#### Vertical Bounce (for horizontal tabs)
```xml
<!-- Lines 7-21: Polygon version -->
<Style Selector="Polygon.balatro-bounce-vertical">
    <Style.Animations>
        <Animation Duration="0:0:0.8"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="SineEaseInOut">
            <KeyFrame Cue="0%">
                <Setter Property="TranslateTransform.Y" Value="0"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="6"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

<!-- Lines 23-38: TextBlock version (same animation, different selector) -->
<Style Selector="TextBlock.balatro-bounce-vertical">
    <!-- ... identical animation ... -->
</Style>
```

**Parameters:**
- **Duration:** 0.8 seconds
- **Distance:** 6 pixels downward
- **Easing:** SineEaseInOut
- **Loop:** Infinite alternate (bounces forever)

#### Horizontal Bounce (for vertical lists)
```xml
<!-- Lines 41-55: Polygon version -->
<Style Selector="Polygon.balatro-bounce-horizontal">
    <Style.Animations>
        <Animation Duration="0:0:0.8"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="SineEaseInOut">
            <KeyFrame Cue="0%">
                <Setter Property="TranslateTransform.X" Value="0"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.X" Value="8"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

**Parameters:**
- **Duration:** 0.8 seconds
- **Distance:** 8 pixels rightward (slightly more than vertical)
- **Easing:** SineEaseInOut
- **Loop:** Infinite alternate

#### Triangle Styling
```xml
<!-- Lines 58-60: Red fill -->
<Style Selector="Polygon.balatro-triangle">
    <Setter Property="Fill" Value="{StaticResource Red}"/>
</Style>

<!-- Lines 63-73: Drop shadow -->
<Style Selector="Polygon.nav-selection-arrow">
    <Setter Property="Effect">
        <Setter.Value>
            <DropShadowEffect Color="{StaticResource BlackColor}"
                              BlurRadius="4"
                              OffsetX="2"
                              OffsetY="2"
                              Opacity="0.8"/>
        </Setter.Value>
    </Setter>
</Style>
```

---

### BalatroTabControl Implementation

#### XAML Structure (BalatroTabControl.axaml)

```xml
<ControlTemplate>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="12"/>   <!-- Triangle indicator space -->
            <RowDefinition Height="*"/>    <!-- TabControl -->
        </Grid.RowDefinitions>

        <!-- Triangle indicator - positioned automatically above selected tab -->
        <Canvas Grid.Row="0" Name="PART_TriangleCanvas" ClipToBounds="False" IsHitTestVisible="False">
            <Polygon Name="PART_TriangleIndicator"
                     Points="0,0 12,0 6,8"
                     Classes="balatro-triangle balatro-bounce-vertical"
                     Canvas.Left="0"
                     Canvas.Top="0"/>
        </Canvas>

        <TabControl Grid.Row="1" Name="PART_TabControl" ...>
            <!-- Tab items styled as red buttons -->
        </TabControl>
    </Grid>
</ControlTemplate>
```

**Key Design Decisions:**
- **Canvas positioning:** Triangle uses `Canvas.Left` for pixel-perfect positioning
- **ClipToBounds="False":** Allows triangle to extend above tab control
- **IsHitTestVisible="False":** Triangle doesn't interfere with mouse clicks
- **Classes="balatro-triangle balatro-bounce-vertical":** Applies red fill + bounce animation

#### Code-Behind Positioning (BalatroTabControl.axaml.cs)

```csharp
private const double TRIANGLE_HALF_WIDTH = 6.0; // Half of the 12px wide triangle

private void UpdateTrianglePosition()
{
    if (_triangleIndicator == null || _tabControl == null) return;

    var selectedTab = _tabControl.ItemContainerGenerator
        .ContainerFromIndex(_tabControl.SelectedIndex) as TabItem;

    if (selectedTab?.Bounds.Width > 0)
    {
        var tabCenterX = selectedTab.Bounds.Left + (selectedTab.Bounds.Width / 2.0);
        var triangleLeft = tabCenterX - TRIANGLE_HALF_WIDTH;
        Canvas.SetLeft(_triangleIndicator, triangleLeft);
    }
}
```

**Positioning Logic:**
1. Find selected TabItem visual element
2. Calculate center X position: `Left + Width / 2`
3. Offset by triangle half-width: `CenterX - 6px`
4. Set `Canvas.Left` to position triangle

**Events Triggering Update:**
- `SelectionChanged` on TabControl
- `Loaded` event
- `SizeChanged` on selected TabItem (for window resize)

---

### FilterSelectionModal List Arrows

#### XAML Structure (FilterSelectionModal.axaml:129-140)

```xml
<Grid>
    <Button Classes="filter-list-item" ... />

    <!-- Triangle attached to left edge of button -->
    <Polygon Points="0,0 14,8.5 0,17"
             Width="14"
             Height="17"
             Fill="{StaticResource Red}"
             Classes="balatro-bounce-horizontal"
             HorizontalAlignment="Left"
             VerticalAlignment="Center"
             Margin="-23,0,0,0"
             ZIndex="10"
             IsVisible="{Binding IsSelected}"/>
</Grid>
```

**Positioning Method:**
- **Static positioning:** Uses negative margin instead of code-behind
- **Margin="-23,0,0,0":** Pulls triangle 23px to left (14px width + 9px spacing)
- **VerticalAlignment="Center":** Auto-centers vertically in button
- **IsVisible binding:** Shows only for selected item

**Pros of Static Approach:**
- No code-behind needed
- Simple and declarative
- Works for uniform-sized buttons

**Cons of Static Approach:**
- Hardcoded offset (breaks if button width changes)
- Doesn't adapt to dynamic button sizes
- Less flexible than Canvas approach

---

## Problem Statement

### Inconsistency Issues

**Problem 1: Two Different Positioning Methods**
- BalatroTabControl: Canvas + code-behind (dynamic, flexible)
- FilterSelectionModal: Margin (static, hardcoded)
- **Impact:** Hard to maintain, no reusable pattern

**Problem 2: Hardcoded Animation Parameters**
- Duration, distance, easing buried in XAML
- Can't easily create faster/slower variants
- No way to pause animations (accessibility concern)

**Problem 3: Limited Reusability**
- Only two directions supported (vertical, horizontal)
- No diagonal, circular, or other animation patterns
- Hard to apply to non-Polygon shapes (rectangles, icons, etc.)

**Problem 4: No Customization API**
- Can't adjust bounce distance per control
- Can't change animation speed
- Can't use different easing curves

---

## Proposed Enhancements

### Enhancement 1: Parameterized Animations (HIGH PRIORITY)

**Problem:** Animation parameters hardcoded in XAML.

**Solution:** Use Avalonia's animation properties with default values.

#### Before (Current):
```xml
<Style Selector="Polygon.balatro-bounce-vertical">
    <Style.Animations>
        <Animation Duration="0:0:0.8" ...>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="6"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

#### After (Proposed):
```xml
<!-- Define animation classes with varying speeds -->
<Style Selector="Polygon.balatro-bounce-vertical">
    <Style.Animations>
        <Animation Duration="0:0:0.8" ...>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="6"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

<!-- Fast variant -->
<Style Selector="Polygon.balatro-bounce-vertical-fast">
    <Style.Animations>
        <Animation Duration="0:0:0.5" ...>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="4"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

<!-- Slow variant -->
<Style Selector="Polygon.balatro-bounce-vertical-slow">
    <Style.Animations>
        <Animation Duration="0:0:1.2" ...>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="8"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

<!-- Large distance variant -->
<Style Selector="Polygon.balatro-bounce-vertical-large">
    <Style.Animations>
        <Animation Duration="0:0:0.8" ...>
            <KeyFrame Cue="100%">
                <Setter Property="TranslateTransform.Y" Value="10"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

**Usage:**
```xml
<!-- Use fast bounce for small tabs -->
<Polygon Classes="balatro-triangle balatro-bounce-vertical-fast"/>

<!-- Use large bounce for emphasis -->
<Polygon Classes="balatro-triangle balatro-bounce-vertical-large"/>
```

---

### Enhancement 2: Unified Positioning Behavior (MEDIUM PRIORITY)

**Problem:** Two different approaches (Canvas + code vs Margin).

**Solution:** Create reusable attached behavior.

#### Proposed: SelectionIndicatorBehavior

```csharp
// File: src/Behaviors/SelectionIndicatorBehavior.cs
public class SelectionIndicatorBehavior : Behavior<Polygon>
{
    // Positioning mode
    public static readonly StyledProperty<PositionMode> PositionModeProperty =
        AvaloniaProperty.Register<SelectionIndicatorBehavior, PositionMode>(
            nameof(PositionMode), PositionMode.Auto);

    public PositionMode PositionMode
    {
        get => GetValue(PositionModeProperty);
        set => SetValue(PositionModeProperty, value);
    }

    // Target element to track
    public static readonly StyledProperty<Control?> TargetProperty =
        AvaloniaProperty.Register<SelectionIndicatorBehavior, Control?>(
            nameof(Target));

    public Control? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    // Offset from target
    public static readonly StyledProperty<Point> OffsetProperty =
        AvaloniaProperty.Register<SelectionIndicatorBehavior, Point>(
            nameof(Offset), new Point(-20, 0));

    public Point Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (Target != null)
        {
            Target.PropertyChanged += OnTargetPropertyChanged;
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (AssociatedObject == null || Target == null) return;

        switch (PositionMode)
        {
            case PositionMode.LeftCenter:
                PositionLeftCenter();
                break;
            case PositionMode.TopCenter:
                PositionTopCenter();
                break;
            case PositionMode.Auto:
                DetectAndPosition();
                break;
        }
    }

    private void PositionLeftCenter()
    {
        var targetBounds = Target.Bounds;
        var indicatorHeight = AssociatedObject.Bounds.Height;
        var y = (targetBounds.Height - indicatorHeight) / 2.0;
        Canvas.SetLeft(AssociatedObject, Offset.X);
        Canvas.SetTop(AssociatedObject, y + Offset.Y);
    }

    // ... other positioning methods ...
}

public enum PositionMode
{
    Auto,
    LeftCenter,
    TopCenter,
    RightCenter,
    BottomCenter
}
```

**Usage:**
```xml
<Grid>
    <Button Name="TargetButton" ... />
    <Polygon Classes="balatro-triangle balatro-bounce-horizontal">
        <i:Interaction.Behaviors>
            <behaviors:SelectionIndicatorBehavior
                Target="{Binding #TargetButton}"
                PositionMode="LeftCenter"
                Offset="-20,0"/>
        </i:Interaction.Behaviors>
    </Polygon>
</Grid>
```

**Benefits:**
- Declarative positioning in XAML
- No code-behind needed
- Reusable across components
- Supports dynamic resizing

---

### Enhancement 3: Additional Animation Patterns (LOW PRIORITY)

**Expand animation library beyond bouncing triangles:**

#### Pulse Animation
```xml
<Style Selector="Control.balatro-pulse">
    <Style.Animations>
        <Animation Duration="0:0:1.0"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="SineEaseInOut">
            <KeyFrame Cue="0%">
                <Setter Property="Opacity" Value="1.0"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="Opacity" Value="0.6"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

#### Glow Animation
```xml
<Style Selector="Control.balatro-glow">
    <Style.Animations>
        <Animation Duration="0:0:0.6"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate">
            <KeyFrame Cue="0%">
                <Setter Property="Effect">
                    <DropShadowEffect BlurRadius="10" Color="#ff4c40" Opacity="0.0"/>
                </Setter>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="Effect">
                    <DropShadowEffect BlurRadius="20" Color="#ff4c40" Opacity="0.8"/>
                </Setter>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

#### Scale Bounce
```xml
<Style Selector="Control.balatro-scale-bounce">
    <Style.Animations>
        <Animation Duration="0:0:0.8"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="ElasticEaseOut">
            <KeyFrame Cue="0%">
                <Setter Property="RenderTransform">
                    <ScaleTransform ScaleX="1.0" ScaleY="1.0"/>
                </Setter>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="RenderTransform">
                    <ScaleTransform ScaleX="1.1" ScaleY="1.1"/>
                </Setter>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

**Use Cases:**
- **Pulse:** Subtle attention draw (notification badges)
- **Glow:** Highlight special items (legendary jokers)
- **Scale Bounce:** Celebrate achievements

---

### Enhancement 4: Animation Control API (MEDIUM PRIORITY)

**Problem:** No way to pause/resume animations (accessibility, performance).

**Solution:** Create animation controller service.

```csharp
// File: src/Services/AnimationControlService.cs
public interface IAnimationControlService
{
    bool AnimationsEnabled { get; set; }
    void PauseAllAnimations();
    void ResumeAllAnimations();
    void SetAnimationSpeed(double speedMultiplier);
}

public class AnimationControlService : IAnimationControlService
{
    private bool _animationsEnabled = true;

    public bool AnimationsEnabled
    {
        get => _animationsEnabled;
        set
        {
            _animationsEnabled = value;
            if (value)
                ResumeAllAnimations();
            else
                PauseAllAnimations();
        }
    }

    public void PauseAllAnimations()
    {
        // Traverse visual tree, pause all animations
    }

    public void ResumeAllAnimations()
    {
        // Resume animations
    }

    public void SetAnimationSpeed(double speedMultiplier)
    {
        // Adjust animation playback speed
    }
}
```

**Integration with Settings:**
```xml
<CheckBox Content="Enable animations" IsChecked="{Binding AnimationsEnabled}"/>
<Slider Minimum="0.5" Maximum="2.0" Value="{Binding AnimationSpeed}"/>
```

**Accessibility Benefits:**
- Users with vestibular disorders can disable animations
- Battery saving mode can reduce animation speed
- Focus mode can pause non-essential animations

---

## Implementation Plan

### Phase 1: Document Current System (30 minutes)
- [x] Document existing animations in BalatroAnimations.axaml
- [x] Document BalatroTabControl positioning logic
- [x] Document FilterSelectionModal static positioning
- [x] Create this PRD

### Phase 2: Create Animation Variants (1 hour)
- [ ] Add fast/slow/large variants to BalatroAnimations.axaml
- [ ] Add pulse/glow/scale animations
- [ ] Test performance impact
- [ ] Document usage examples

### Phase 3: Unified Positioning (Optional, 2 hours)
- [ ] Create SelectionIndicatorBehavior
- [ ] Refactor BalatroTabControl to use behavior (optional)
- [ ] Refactor FilterSelectionModal to use behavior (optional)
- [ ] Test across all modals

### Phase 4: Animation Control Service (Optional, 2 hours)
- [ ] Create IAnimationControlService interface
- [ ] Implement AnimationControlService
- [ ] Add settings UI for animation control
- [ ] Test pause/resume functionality

---

## Acceptance Criteria

### Current System (Already Working)
- [x] Vertical bounce animates triangles on horizontal tabs
- [x] Horizontal bounce animates triangles on vertical lists
- [x] Animations loop infinitely with sine easing
- [x] Triangle positioning updates on tab selection
- [x] Animations don't interfere with mouse interaction

### Enhancement 1: Animation Variants
- [ ] Fast, slow, and large bounce variants exist
- [ ] Variants are documented with usage examples
- [ ] Performance impact is negligible (<1ms frame time)
- [ ] All variants use consistent easing curves

### Enhancement 2: Unified Positioning (Optional)
- [ ] SelectionIndicatorBehavior supports all positioning modes
- [ ] Behavior handles window resize correctly
- [ ] No code-behind needed for new selection indicators
- [ ] Existing components work with behavior

### Enhancement 3: Additional Animations (Optional)
- [ ] Pulse, glow, and scale animations exist
- [ ] Animations are performant (GPU-accelerated)
- [ ] Examples show proper usage
- [ ] No visual glitches or jank

### Enhancement 4: Animation Control (Optional)
- [ ] Users can disable animations globally
- [ ] Users can adjust animation speed
- [ ] Settings persist across sessions
- [ ] Pause/resume works correctly

---

## Success Metrics

### Consistency
- ✅ All selection indicators use same animation library
- ✅ All positioning logic follows same pattern
- ✅ Easy to add new animated indicators

### Reusability
- ✅ Animation classes work on any control type
- ✅ Positioning behavior reusable across components
- ✅ No duplicate code

### Accessibility
- ✅ Users can disable/slow animations
- ✅ Animations don't cause motion sickness
- ✅ Essential functionality works without animations

---

## Timeline

### Core Documentation (This PRD)
- **Time:** 30 minutes (DONE)
- **Priority:** P0

### Animation Variants
- **Time:** 1 hour
- **Priority:** P1 (Optional)

### Unified Positioning
- **Time:** 2 hours
- **Priority:** P2 (Optional)

### Animation Control
- **Time:** 2 hours
- **Priority:** P2 (Optional)

**Total if all implemented:** 5.5 hours

---

## Related PRDs

- [FILTER_MODAL_UI_BUGS_PRD.md](./FILTER_MODAL_UI_BUGS_PRD.md) - Red arrow positioning bug (related to this system)
- [GLOBAL_STYLES_ARCHITECTURE_PRD.md](./GLOBAL_STYLES_ARCHITECTURE_PRD.md) - Where animation styles fit in global architecture

---

## Notes

- Current system works well, enhancements are **optional polish**
- Priority should be given to bug fixes over animation system expansion
- Animation control service is important for accessibility (consider prioritizing)
- Positioning behavior is nice-to-have but not critical

---

## Assignee

coding-agent (automated via Claude Code)

---

**END OF PRD**
