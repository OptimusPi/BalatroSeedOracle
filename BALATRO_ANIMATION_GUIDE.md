# 🎮 Balatro Animation & Polish Guide

## Extracted from REAL Balatro Source Code (external/Balatro/*.lua)

This guide documents the **exact animation formulas** used in Balatro to create that cozy, immersive, juicy feel.

---

## 📊 **CORE ANIMATION FORMULAS**

### 1. **Floating Animation** (The signature "breathing" effect)

**Source:** `external/Balatro/engine/animatedsprite.lua` lines 88-92

```lua
if self.float then
    self.T.r = 0.02*math.sin(2*G.TIMERS.REAL+self.T.x)
    self.offset.y = -(1+0.3*math.sin(0.666*G.TIMERS.REAL+self.T.y))*self.shadow_parrallax.y
    self.offset.x = -(0.7+0.2*math.sin(0.666*G.TIMERS.REAL+self.T.x))*self.shadow_parrallax.x
end
```

**Translation to C#/Avalonia:**
```csharp
if (IsFloating)
{
    // Gentle rotation wobble
    double rotation = 0.02 * Math.Sin(2 * totalSeconds + initialX);

    // Vertical breathing (slower frequency)
    double offsetY = -(1 + 0.3 * Math.Sin(0.666 * totalSeconds + initialY)) * shadowParallaxY;

    // Horizontal sway (even slower, smaller amplitude)
    double offsetX = -(0.7 + 0.2 * Math.Sin(0.666 * totalSeconds + initialX)) * shadowParallaxX;
}
```

**Key Parameters:**
- **Rotation amplitude:** 0.02 radians (~1.15 degrees) - VERY subtle!
- **Rotation frequency:** 2.0 cycles/second
- **Vertical amplitude:** 1.0 ± 0.3 (30% variation)
- **Vertical frequency:** 0.666 cycles/second (slow breathing)
- **Horizontal amplitude:** 0.7 ± 0.2 (28% variation)
- **Horizontal frequency:** 0.666 cycles/second
- **Phase offset:** Uses object position (T.x, T.y) for variation between instances

---

### 2. **Text Floating Animation** (DynaText wobble)

**Source:** `external/Balatro/engine/text.lua` line 234

```lua
if self.config.float then
    letter.offset.y = (G.SETTINGS.reduced_motion and 0 or 1) *
                     math.sqrt(self.scale) *
                     (2 + (self.font.FONTSCALE/G.TILESIZE)*2000*math.sin(2.666*G.TIMERS.REAL+200*k)) +
                     60*(letter.scale-1)
end
```

**Translation to C#/Avalonia:**
```csharp
if (IsFloating && !ReducedMotionEnabled)
{
    double baseOffset = Math.Sqrt(scale);
    double waveAmplitude = (fontSize / tileSize) * 2000;
    double waveOffset = Math.Sin(2.666 * totalSeconds + 200 * letterIndex);
    double scaleOffset = 60 * (letterScale - 1);

    letterOffsetY = baseOffset * (2 + waveAmplitude * waveOffset) + scaleOffset;
}
```

**Key Parameters:**
- **Wave frequency:** 2.666 cycles/second (faster than sprites)
- **Phase per letter:** 200 * letterIndex (creates wave effect across text)
- **Amplitude:** Scales with font size
- **Base offset:** 2 units + wave

---

### 3. **Card Tilt Animation** (Mouse hover effect)

**Source:** `external/Balatro/card.lua` line 15-16

```lua
self.tilt_var = {mx = 0, my = 0, dx = 0, dy = 0, amt = 0}
self.ambient_tilt = 0.2
```

**Key Parameters:**
- **Ambient tilt:** 0.2 (always-on subtle tilt even without mouse)
- **Mouse delta:** Tracks `dx`, `dy` for responsive tilting
- **Tilt amount:** Variable based on mouse proximity

---

### 4. **Shader Effects** (Dissolve, Hologram)

**Source:** `external/Balatro/card.lua` lines 4518-4522 (Soul cards)

```lua
-- Hologram effect
self.children.floating_sprite:draw_shader('hologram', nil, self.ARGS.send_to_shader, nil,
    self.children.center, 2*scale_mod, 2*rotate_mod)

-- Dissolve effect (animated)
self.children.floating_sprite:draw_shader('dissolve', 0, nil, nil, self.children.center,
    scale_mod, rotate_mod, nil,
    0.1 + 0.03*math.sin(1.8*G.TIMERS.REAL), -- Animated threshold
    nil, 0.6)
```

**Key Parameters:**
- **Hologram scale:** 2x normal
- **Dissolve threshold:** 0.1 ± 0.03 (oscillates at 1.8 Hz)
- **Dissolve opacity:** 0.6

---

## 🎨 **ANIMATION TIMING**

### Animation FPS
**Source:** `external/Balatro/engine/animatedsprite.lua` line 78

```lua
local new_frame = math.floor(G.ANIMATION_FPS*(G.TIMERS.REAL - self.offset_seconds))%self.current_animation.frames
```

- **Frame rate:** Controlled by `G.ANIMATION_FPS`
- **Timing:** Uses `G.TIMERS.REAL` (real elapsed time, not game time)
- **Offset:** Each sprite has `offset_seconds` for phase variation

---

## 🎯 **PRACTICAL IMPLEMENTATION FOR AVALONIA**

### Example: Floating Widget

```csharp
public partial class FloatingWidget : UserControl
{
    private DispatcherTimer _animationTimer;
    private DateTime _startTime;
    private Point _initialPosition;
    private double _shadowParallaxX = 1.0;
    private double _shadowParallaxY = 1.0;

    public FloatingWidget()
    {
        InitializeComponent();
        _startTime = DateTime.Now;
        _initialPosition = new Point(RenderTransform.Value.M31, RenderTransform.Value.M32);

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _animationTimer.Tick += AnimationTick;
        _animationTimer.Start();
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        double t = (DateTime.Now - _startTime).TotalSeconds;

        // Balatro's exact floating formula
        double rotation = 0.02 * Math.Sin(2 * t + _initialPosition.X);
        double offsetY = -(1 + 0.3 * Math.Sin(0.666 * t + _initialPosition.Y)) * _shadowParallaxY;
        double offsetX = -(0.7 + 0.2 * Math.Sin(0.666 * t + _initialPosition.X)) * _shadowParallaxX;

        // Apply transform
        var transform = new TransformGroup();
        transform.Children.Add(new RotateTransform(rotation * (180 / Math.PI))); // Convert to degrees
        transform.Children.Add(new TranslateTransform(offsetX, offsetY));

        RenderTransform = transform;
    }
}
```

---

## 🌊 **USING AVALONIA.XAML.BEHAVIORS**

Now that we have `Avalonia.Xaml.Behaviors` installed, we can create reusable animation behaviors!

### FloatingBehavior.cs

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System;
using System.Threading;

public class FloatingBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> RotationAmplitudeProperty =
        AvaloniaProperty.Register<FloatingBehavior, double>(nameof(RotationAmplitude), 0.02);

    public static readonly StyledProperty<double> VerticalAmplitudeProperty =
        AvaloniaProperty.Register<FloatingBehavior, double>(nameof(VerticalAmplitude), 0.3);

    public static readonly StyledProperty<double> HorizontalAmplitudeProperty =
        AvaloniaProperty.Register<FloatingBehavior, double>(nameof(HorizontalAmplitude), 0.2);

    public static readonly StyledProperty<double> FrequencyProperty =
        AvaloniaProperty.Register<FloatingBehavior, double>(nameof(Frequency), 0.666);

    public double RotationAmplitude
    {
        get => GetValue(RotationAmplitudeProperty);
        set => SetValue(RotationAmplitudeProperty, value);
    }

    public double VerticalAmplitude
    {
        get => GetValue(VerticalAmplitudeProperty);
        set => SetValue(VerticalAmplitudeProperty, value);
    }

    public double HorizontalAmplitude
    {
        get => GetValue(HorizontalAmplitudeProperty);
        set => SetValue(HorizontalAmplitudeProperty, value);
    }

    public double Frequency
    {
        get => GetValue(FrequencyProperty);
        set => SetValue(FrequencyProperty, value);
    }

    private CompositionDisposable? _disposables;
    private DateTime _startTime;
    private Point _initialPosition;

    protected override void OnAttached()
    {
        base.OnAttached();
        _disposables = new CompositionDisposable();
        _startTime = DateTime.Now;

        if (AssociatedObject != null)
        {
            _initialPosition = new Point(
                AssociatedObject.RenderTransform?.Value.M31 ?? 0,
                AssociatedObject.RenderTransform?.Value.M32 ?? 0
            );

            // Start animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += AnimationTick;
            timer.Start();

            _disposables.Add(Disposable.Create(() => timer.Stop()));
        }
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        if (AssociatedObject == null) return;

        double t = (DateTime.Now - _startTime).TotalSeconds;

        // Balatro's exact floating formula
        double rotation = RotationAmplitude * Math.Sin(2 * t + _initialPosition.X);
        double offsetY = -(1 + VerticalAmplitude * Math.Sin(Frequency * t + _initialPosition.Y));
        double offsetX = -(0.7 + HorizontalAmplitude * Math.Sin(Frequency * t + _initialPosition.X));

        var transform = new TransformGroup();
        transform.Children.Add(new RotateTransform(rotation * (180 / Math.PI)));
        transform.Children.Add(new TranslateTransform(offsetX, offsetY));

        AssociatedObject.RenderTransform = transform;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _disposables?.Dispose();
    }
}
```

### Usage in XAML:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:i="using:Avalonia.Xaml.Interactivity"
             xmlns:local="using:BalatroSeedOracle.Behaviors">

    <!-- Apply floating effect to any control! -->
    <Border Width="200" Height="300" Background="Red">
        <i:Interaction.Behaviors>
            <local:FloatingBehavior RotationAmplitude="0.02"
                                   VerticalAmplitude="0.3"
                                   HorizontalAmplitude="0.2"
                                   Frequency="0.666"/>
        </i:Interaction.Behaviors>
    </Border>
</UserControl>
```

---

## 🎪 **BALATRO'S COZY VIBE PRINCIPLES**

Based on analyzing the source code:

### 1. **Constant Motion, But SUBTLE**
- Nothing is static - even idle sprites have `float = true`
- Rotation amplitude is only **1.15 degrees** max
- Movement is **slow** (0.666 Hz = one cycle every 1.5 seconds)

### 2. **Phase Variation**
- Each object uses its position as phase offset
- Prevents synchronized "marching" - everything breathes independently
- Creates organic, living feel

### 3. **Layered Effects**
- Cards have rotation + Y offset + X offset
- Text has per-letter phase shifts
- Multiple frequencies (2.0, 0.666, 2.666 Hz) create complexity

### 4. **Accessibility First**
- `G.SETTINGS.reduced_motion` checks everywhere
- Screenshake can be disabled
- Float effects can be turned off

### 5. **Shadow Parallax**
- Shadows move **opposite** to the sprite
- Creates depth illusion
- Amplitude controlled by `shadow_parrallax.x/y`

---

## 🚀 **RECOMMENDED IMPLEMENTATION PLAN**

### Phase 1: Add Floating to Widgets
1. Create `FloatingBehavior` (above)
2. Apply to `DayLatroWidget` and `AudioVisualizerWidget`
3. Test parameters (start subtle, can increase later)

### Phase 2: Add to Buttons
1. Create `HoverBounce behavior` (scale 1.0 → 1.05 on hover)
2. Create `ClickPulse` behavior (quick scale pulse on click)
3. Use easing functions: `CubicEaseOut` for bounces

### Phase 3: Text Effects
1. Create `FloatingTextBehavior` with per-character offsets
2. Apply to titles, labels
3. Add `pop_in` effect (scale 0 → 1 with overshoot)

### Phase 4: Advanced Effects
1. Mouse parallax for background
2. Chromatic aberration shader (if Skia supports it)
3. Glow/dissolve effects for special items

---

## 📦 **PACKAGES WE JUST INSTALLED**

### ✅ Egorozh.ColorPicker.Avalonia 11.0.3
- RGB/HSB color picker
- **Use for:** Custom shader theme colors (replace ComboBox)

### ✅ Avalonia.Xaml.Behaviors 11.3.0.6
- Attach animations/interactions via XAML
- **Use for:** FloatingBehavior, HoverBehavior, etc.

### ✅ LiveChartsCore.SkiaSharpView.Avalonia 2.0.0-rc6.1
- Beautiful animated charts
- **Use for:** Seed score distribution graphs, joker probability curves

### ✅ AvaloniaEdit.TextMate
- Syntax highlighting for text editors
- **Use for:** JSON filter editor with highlighting

---

## 🎯 **QUICK WINS**

Want immediate "WOW" factor? Do these:

1. **Add FloatingBehavior to DayLatroWidget** (5 minutes)
   - Instant Balatro-style breathing effect!

2. **Add hover scale to all buttons** (10 minutes)
   ```xml
   <i:Interaction.Behaviors>
       <ia:PointerOverBehavior TargetScale="1.05" Duration="0.15"/>
   </i:Interaction.Behaviors>
   ```

3. **Add ColorPicker to Audio Visualizer widget** (15 minutes)
   - Replace "Main Color" and "Accent Color" ComboBoxes
   - Let users pick EXACT RGB values!

4. **Add particle effects to search complete** (20 minutes)
   - When search finishes with results > 100
   - Gold confetti burst using Particles

---

## 📚 **ADDITIONAL RESOURCES**

- **Balatro Source:** `X:\BalatroSeedOracle\external\Balatro\*.lua`
- **Animation Engine:** `external/Balatro/engine/animatedsprite.lua`
- **Card Logic:** `external/Balatro/card.lua` (tilt, hover, shaders)
- **Text Effects:** `external/Balatro/engine/text.lua` (floating text)
- **UI Definitions:** `external/Balatro/functions/UI_definitions.lua` (DynaText examples)

---

**REMEMBER:** Balatro's magic is in the **subtlety**. Start with small amplitudes and slow frequencies. You can always increase, but if you start too aggressive it feels janky instead of cozy!

Happy animating! 🎮✨
