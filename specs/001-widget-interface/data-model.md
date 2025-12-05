# Data Model: Widget Interface System

**Date**: 2025-12-02  
**Branch**: `001-widget-interface`

## Core Entities

### IWidget Interface

**Purpose**: Contract defining widget behavior and properties  
**Location**: `src/Models/IWidget.cs`

**Properties**:
- `string Id` - Unique widget identifier
- `string Title` - Display title for widget
- `string IconResource` - Path to icon resource
- `WidgetState State` - Current widget state (Minimized/Open) - **not persisted**
- `int NotificationCount` - Notification badge count
- `double ProgressValue` - Progress bar value (0.0-1.0)
- `bool ShowCloseButton` - Whether close button is visible
- `bool ShowPopOutButton` - Whether pop-out button is visible
- `Point Position` - Widget position in grid coordinates - **not persisted**
- `Size Size` - Widget size when open
- `bool IsDocked` - Whether widget is currently docked - **not persisted**
- `DockPosition DockPosition` - Current dock position if docked - **not persisted**
- `object? PersistedState` - Widget-specific state that survives sessions (e.g., search instance data)

**Methods**:
- `Task OpenAsync()` - Transition to open state
- `Task MinimizeAsync()` - Transition to minimized state  
- `Task CloseAsync()` - Close widget completely
- `UserControl GetContentView()` - Get widget's content view
- `void UpdateNotifications(int count)` - Update notification count
- `void UpdateProgress(double value)` - Update progress value
- `Task<object?> SaveStateAsync()` - Save widget-specific state for persistence
- `Task LoadStateAsync(object? state)` - Load widget-specific state from persistence

### WidgetState Enumeration

**Purpose**: Define possible widget states  
**Location**: `src/Models/WidgetState.cs`

**Values**:
- `Minimized` - Widget in minimized state (square button)
- `Open` - Widget in open state (full functionality)
- `Transitioning` - Widget transitioning between states

### WidgetMetadata Model

**Purpose**: Store widget metadata and configuration  
**Location**: `src/Models/WidgetMetadata.cs`

**Properties**:
- `string Id` - Unique identifier
- `string Title` - Widget display title
- `string IconResource` - Icon resource path
- `Type WidgetType` - Type of widget implementation
- `Type ViewModelType` - Type of associated ViewModel
- `bool AllowClose` - Whether widget can be closed
- `bool AllowPopOut` - Whether widget can be popped out
- `Size DefaultSize` - Default size when opened
- `string Description` - Widget description
- `string Category` - Widget category for grouping

### WidgetPosition Model

**Purpose**: Manage widget positioning and grid coordinates  
**Location**: `src/Models/WidgetPosition.cs`

**Properties**:
- `int GridX` - X coordinate in grid
- `int GridY` - Y coordinate in grid
- `double PixelX` - Actual pixel X position
- `double PixelY` - Actual pixel Y position
- `bool IsValidPosition` - Whether position is valid
- `bool IsOccupied` - Whether grid position is occupied

**Methods**:
- `static WidgetPosition FromGrid(int x, int y)` - Create from grid coordinates
- `static WidgetPosition FromPixels(double x, double y)` - Create from pixel coordinates
- `Point ToGrid()` - Convert to grid coordinates
- `Point ToPixels()` - Convert to pixel coordinates

### DockPosition Enumeration

**Purpose**: Define available docking positions  
**Location**: `src/Models/DockPosition.cs`

**Values**:
- `None` - Not docked
- `LeftFull` - Left side, full height
- `RightFull` - Right side, full height
- `TopLeft` - Top-left quarter
- `TopRight` - Top-right quarter
- `BottomLeft` - Bottom-left quarter
- `BottomRight` - Bottom-right quarter

### DockZone Model

**Purpose**: Define docking zones and their properties  
**Location**: `src/Models/DockZone.cs`

**Properties**:
- `DockPosition Position` - Zone position
- `Rect Bounds` - Zone boundaries
- `bool IsActive` - Whether zone is currently active
- `bool IsHighlighted` - Whether zone is highlighted during drag
- `string DisplayText` - Text to show in drop zone

**Methods**:
- `bool ContainsPoint(Point point)` - Check if point is within zone
- `void Activate()` - Activate zone during drag operation
- `void Deactivate()` - Deactivate zone
- `void Highlight()` - Highlight zone during hover
- `void ClearHighlight()` - Clear highlight

## ViewModels

### WidgetViewModel Base Class

**Purpose**: Base MVVM ViewModel for all widgets  
**Location**: `src/ViewModels/Widgets/WidgetViewModel.cs`

**Observable Properties** (CommunityToolkit.Mvvm):
- `[ObservableProperty] string title`
- `[ObservableProperty] string iconResource`
- `[ObservableProperty] WidgetState state`
- `[ObservableProperty] int notificationCount`
- `[ObservableProperty] double progressValue`
- `[ObservableProperty] bool showCloseButton`
- `[ObservableProperty] bool showPopOutButton`
- `[ObservableProperty] WidgetPosition position`
- `[ObservableProperty] bool isDocked`
- `[ObservableProperty] DockPosition dockPosition`

**Relay Commands**:
- `[RelayCommand] async Task Open()`
- `[RelayCommand] async Task Minimize()`
- `[RelayCommand] async Task Close()`
- `[RelayCommand] async Task PopOut()`

### WidgetContainerViewModel

**Purpose**: Manage collection of widgets and layout  
**Location**: `src/ViewModels/Widgets/WidgetContainerViewModel.cs`

**Observable Properties**:
- `[ObservableProperty] ObservableCollection<WidgetViewModel> widgets`
- `[ObservableProperty] WidgetViewModel selectedWidget`
- `[ObservableProperty] bool isDragging`
- `[ObservableProperty] ObservableCollection<DockZone> dockZones`

**Relay Commands**:
- `[RelayCommand] async Task CreateWidget(string widgetType)`
- `[RelayCommand] async Task RemoveWidget(WidgetViewModel widget)`
- `[RelayCommand] void StartDrag(WidgetViewModel widget)`
- `[RelayCommand] void EndDrag(Point position)`

### DockZoneViewModel

**Purpose**: Manage docking zones and visual feedback  
**Location**: `src/ViewModels/Widgets/DockZoneViewModel.cs`

**Observable Properties**:
- `[ObservableProperty] ObservableCollection<DockZone> zones`
- `[ObservableProperty] DockZone activeZone`
- `[ObservableProperty] bool showZones`

**Relay Commands**:
- `[RelayCommand] void ShowZones()`
- `[RelayCommand] void HideZones()`
- `[RelayCommand] void ActivateZone(DockPosition position)`

## Validation Rules

### Widget Positioning
- Grid positions must be non-negative integers
- Pixel positions must be within container bounds
- No two minimized widgets can occupy same grid position
- Docked widgets cannot overlap with grid positions

### State Transitions
- Widgets can only transition between Minimized and Open states
- Transitioning state is temporary and should not persist
- Close operation removes widget entirely from collection

### Notification Display
- Count 0: Badge hidden
- Count 1-9: Circular badge
- Count 10+: Pill-shaped badge with number
- Negative counts not allowed

### Progress Values
- Must be between 0.0 and 1.0 inclusive
- Progress bar only shown when value > 0.0
- Invalid values default to 0.0

## Relationships

### Widget ↔ Container
- One-to-many: Container manages multiple widgets
- Container responsible for positioning and layout
- Widgets notify container of state changes

### Widget ↔ DockZone
- Many-to-one: Multiple widgets can target same zone type
- Only open widgets can be docked
- Docked widgets have modified positioning behavior

### Widget ↔ ViewModel
- One-to-one: Each widget instance has corresponding ViewModel
- ViewModels handle UI state and commands
- Data binding connects Models to Views

## State Transitions

### Widget Lifecycle
1. **Creation** → Minimized state at default position
2. **User Click** → Minimized → Open
3. **Minimize Command** → Open → Minimized
4. **Close Command** → Any state → Removed
5. **Drag Operation** → Position changes, state preserved

### Docking Lifecycle
1. **Start Drag** (Open widget) → Show dock zones
2. **Hover Zone** → Highlight zone
3. **Release in Zone** → Dock widget, hide zones
4. **Release outside** → Return to original position

## Data Volume Assumptions

- **Maximum Widgets**: 20 widgets per session
- **Grid Size**: Approximately 50x50 grid cells (based on typical window sizes)
- **Memory per Widget**: <1KB for metadata and state
- **Performance Target**: <16ms for state transitions (60 FPS)
- **Notification Range**: 0-999 notifications per widget