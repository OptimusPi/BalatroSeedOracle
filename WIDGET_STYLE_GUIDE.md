# Balatro Seed Oracle - Widget Style Guide

## Standard Widget Appearance

All desktop widgets follow these **standard dimensions and styling** for consistency:

### 📏 Dimensions

**Minimized State (Icon):**
- Size: `128x128` pixels
- Border: `3px` solid
- Border Color: `{StaticResource Border}` (default) → `{StaticResource Gold}` (hover)
- Corner Radius: `12px`
- Background: `{StaticResource MainBackground}`

**Expanded State (Window):**
- Default Size: `400x500` pixels (customizable via ViewModel)
- Border: `3px` solid gold
- Border Color: `{StaticResource Gold}`
- Corner Radius: `12px`
- Background: `{StaticResource MainBackground}`
- Box Shadow: `0 8 32 0 #80000000` (large shadow)

### 🎨 Visual Style

**Icon Appearance:**
- Large emoji/icon (48-64px)
- Title text below (12-14px, BalatroFont)
- Hover effect: Background darkens, border turns gold, subtle glow

**Expanded Window:**
- Header bar with gold accent
- Draggable by clicking header
- Minimize button (▼) in top-right
- Content area with 12px padding

### 🔔 Notification Badge

- Position: Top-right corner (Canvas.Right="-8", Canvas.Top="-8")
- Style: `notification-badge` class (already global)
- Text: Count or "!" for boolean notifications
- Shows when: `ShowNotificationBadge` = true

### 🎯 Behavior Standards

1. **Click to Expand:** Icon click opens floating window
2. **Draggable:** Click and drag header to move window
3. **Minimize:** ▼ button collapses to icon
4. **Position Memory:** Widget remembers X/Y position (stored in ViewModel)

## Implementation

### Using BaseWidgetViewModel

```csharp
public class MyWidgetViewModel : BaseWidgetViewModel
{
    public MyWidgetViewModel()
    {
        // Set widget identity
        WidgetTitle = "My Widget";
        WidgetIcon = "🎲";  // Emoji or text

        // Optional: Custom dimensions
        Width = 500;
        Height = 600;

        // Optional: Custom position
        PositionX = 100;
        PositionY = 100;
    }

    protected override void OnExpanded()
    {
        // Called when widget opens
        // Load data, start timers, etc.
    }

    protected override void OnMinimized()
    {
        // Called when widget closes
        // Stop timers, save state, etc.
    }

    protected override void OnClosed()
    {
        // Called when widget is permanently closed
        // Cleanup resources
    }
}
```

### Notification Badges

```csharp
// Show count
SetNotification(5);  // Shows "5"

// Show alert
SetNotification(1);  // Shows "1"

// Clear
ClearNotification();
```

## Widget Examples

### DayLatroWidget
- Icon: 📅
- Title: "Daylatro"
- Size: 350x450
- Notification: Shows "!" when new day available

### Search Widget
- Icon: 🔍 (dynamic based on state)
- Title: Filter name
- Size: 128x128 (minimized only, no expand yet)
- Notification: Shows result count

## Color Palette

- **Gold:** `{StaticResource Gold}` - #ffd700 (borders, accents)
- **Background:** `{StaticResource MainBackground}` - Dark base
- **Dark BG:** `{StaticResource DarkBackground}` - Header bars
- **Border:** `{StaticResource Border}` - Default borders
