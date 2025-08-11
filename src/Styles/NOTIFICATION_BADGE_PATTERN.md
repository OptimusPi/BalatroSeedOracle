# Notification Badge Pattern

## Overview
This document explains the proper way to implement notification badges in BalatroSeedOracle to avoid clipping issues.

## The Problem
Using negative margins to position notification badges outside their parent container causes clipping when the parent has `ClipToBounds="True"` (which is the default in many cases). This results in circular badges appearing "squared off" where they extend beyond the parent bounds.

## The Solution: Canvas Overlay
Instead of using negative margins, use a Canvas overlay with absolute positioning. This ensures badges are never clipped and always appear perfectly circular.

## Implementation Pattern

### Basic Structure
```xml
<Grid Margin="10,10,0,0"> <!-- Add margin to hold badge space -->
    <!-- Main content (button, icon, etc.) -->
    <Button>
        <Border>
            <!-- Icon content -->
        </Border>
    </Button>
    
    <!-- Notification badge overlay -->
    <Canvas ZIndex="100">
        <Border Classes="notification-badge" 
                Canvas.Right="-8" 
                Canvas.Top="-8"
                IsVisible="{Binding HasNotification}">
            <TextBlock Text="{Binding NotificationCount}" />
        </Border>
    </Canvas>
</Grid>
```

### Key Concept: Invisible Padding
Instead of positioning badges inside the parent bounds, we:
1. **Add margin/padding** to the parent container to reserve space for the badge
2. **Use negative Canvas positioning** to place the badge in that reserved space
3. **Badge hangs off the main content** but stays within the container bounds

### Global Styles
The following styles are available globally in `BalatroGlobalStyles.axaml`:

```xml
<!-- Standard notification badge -->
<Style Selector="Border.notification-badge">
    <Setter Property="Background" Value="{StaticResource Red}"/>
    <Setter Property="CornerRadius" Value="12"/>
    <Setter Property="MinWidth" Value="24"/>
    <Setter Property="Height" Value="24"/>
    <Setter Property="BoxShadow" Value="0 2 8 rgba(0,0,0,0.3)"/>
    <Setter Property="BorderBrush" Value="{StaticResource White}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>

<!-- Small notification badge variant -->
<Style Selector="Border.notification-badge.small">
    <Setter Property="MinWidth" Value="16"/>
    <Setter Property="Height" Value="16"/>
    <Setter Property="CornerRadius" Value="8"/>
</Style>
```

### Usage Examples

#### Standard Badge (Hanging)
```xml
<Grid Margin="12,12,0,0">
    <!-- Content -->
    <Canvas ZIndex="100">
        <Border Classes="notification-badge"
                Canvas.Right="-8"
                Canvas.Top="-8">
            <TextBlock Text="3" />
        </Border>
    </Canvas>
</Grid>
```

#### Small Badge (Hanging)
```xml
<Grid Margin="10,10,0,0">
    <!-- Content -->
    <Canvas ZIndex="100">
        <Border Classes="notification-badge small"
                Canvas.Right="-5"
                Canvas.Top="-5">
            <TextBlock Text="!" />
        </Border>
    </Canvas>
</Grid>
```

## Positioning Guidelines

### Badge Positioning
- Use **negative Canvas values** to hang badges off the corner: `Canvas.Right="-8"` / `Canvas.Top="-8"`
- Use **positive Canvas values** for inset badges: `Canvas.Right="5"` / `Canvas.Top="5"`

### Container Padding
- **Add margin/padding** to parent containers to reserve space for hanging badges
- **Top-right badge**: Add `Margin="10,10,0,0"` or `Padding="8,8,15,8"`
- **Adjust values** based on badge size (standard=24px, small=16px)

### Positioning Options
- `Canvas.Right` and `Canvas.Top` for top-right positioning
- `Canvas.Left` and `Canvas.Top` for top-left positioning  
- `Canvas.Right` and `Canvas.Bottom` for bottom-right positioning
- `Canvas.Left` and `Canvas.Bottom` for bottom-left positioning

Typical values:
- **Hanging badges**: `Canvas.Right="-8"` / `Canvas.Top="-8"` 
- **Inset badges**: `Canvas.Right="5"` / `Canvas.Top="5"`

## Key Benefits

1. **No Clipping** - Canvas doesn't clip its children by default
2. **Precise Positioning** - Absolute pixel control
3. **Proper Z-Order** - Always renders on top with ZIndex
4. **Responsive** - Works with any parent size/layout
5. **Performance** - No layout fighting or negative margin issues

## Examples in Codebase

- **DayLatroWidget**: Small "!" badge for new daily challenges
- **SearchDesktopIcon**: Result count badge on search icons

## Migration from Old Pattern

### Before (Problematic)
```xml
<Border>
    <!-- Content -->
    <Border Classes="notification-badge"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            Margin="-5,-5,0,0">
        <TextBlock Text="!" />
    </Border>
</Border>
```

### After (Fixed with Hanging Badge)
```xml
<Grid Margin="10,10,0,0">
    <Border>
        <!-- Content -->
    </Border>
    <Canvas ZIndex="100">
        <Border Classes="notification-badge small"
                Canvas.Right="-5"
                Canvas.Top="-5">
            <TextBlock Text="!" />
        </Border>
    </Canvas>
</Grid>
```

This pattern ensures consistent, professional-looking notification badges throughout the application.
