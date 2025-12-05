# Quickstart: Widget Interface Implementation

**Date**: 2025-12-02  
**Branch**: `001-widget-interface`

## Implementation Overview

This quickstart guide provides the essential steps to implement the widget interface system in Balatro Seed Oracle. The system follows MVVM patterns with proper dependency injection and integrates seamlessly with the existing AvaloniaUI architecture.

## Prerequisites

- AvaloniaUI 11.3.8 project structure
- CommunityToolkit.Mvvm package
- Existing MVVM patterns in place
- Dependency injection container configured

## Phase 1: Core Models and Contracts

### 1. Create Core Enumerations

**File**: `src/Models/WidgetState.cs`
```csharp
public enum WidgetState
{
    Minimized,
    Open,
    Transitioning
}

public enum DockPosition
{
    None,
    LeftFull,
    RightFull,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
```

### 2. Implement Data Models

Create the following model classes in `src/Models/`:
- `WidgetMetadata.cs` - Widget configuration and metadata
- `WidgetPosition.cs` - Position management with grid/pixel conversion
- `DockZone.cs` - Docking zone definitions

### 3. Define Service Contracts

Implement interfaces in `src/Services/Contracts/`:
- `IWidget.cs` - Core widget contract
- `IWidgetRegistry.cs` - Widget type registration
- `IWidgetLayoutService.cs` - Grid layout management
- `IDockingService.cs` - Docking system

See `contracts/` directory for complete interface definitions.

## Phase 2: Service Implementation

### 4. Widget Registry Service

**File**: `src/Services/WidgetRegistryService.cs`

**Purpose**: Manages widget type registration and instance creation
**Dependencies**: None (or IServiceProvider for widget creation)
**Key responsibilities**:
- Register/unregister widget types
- Create widget instances
- Provide widget metadata lookup

### 5. Widget Layout Service

**File**: `src/Services/WidgetLayoutService.cs`

**Purpose**: Handles grid positioning and layout calculations
**Dependencies**: None
**Key responsibilities**:
- Grid coordinate conversions (pixel ↔ grid)
- Default position calculation (top-left, row-wise)
- Position validation and snapping
- Available position finding

### 6. Docking Service

**File**: `src/Services/DockingService.cs`

**Purpose**: Manages widget docking and drop zones
**Dependencies**: None
**Key responsibilities**:
- Create and manage dock zones
- Handle drag-and-drop docking operations
- Calculate docked widget bounds
- Manage dock zone visual feedback

## Phase 3: MVVM Implementation

### 7. Base Widget ViewModel

**File**: `src/ViewModels/Widgets/WidgetViewModel.cs`

**Dependencies**: IWidgetLayoutService, IDockingService (via constructor injection)
**Key features**:
- Observable properties with `[ObservableProperty]`
- Relay commands with `[RelayCommand]`
- State transition handling
- Notification and progress management

Example constructor:
```csharp
public WidgetViewModel(
    IWidgetLayoutService layoutService,
    IDockingService dockingService)
{
    _layoutService = layoutService;
    _dockingService = dockingService;
}
```

### 8. Widget Container ViewModel

**File**: `src/ViewModels/Widgets/WidgetContainerViewModel.cs`

**Dependencies**: IWidgetRegistry, IWidgetLayoutService, IDockingService
**Key features**:
- Manage collection of widgets
- Handle widget creation/removal
- Coordinate drag-and-drop operations
- Manage layout and docking

### 9. Dock Zone ViewModel

**File**: `src/ViewModels/Widgets/DockZoneViewModel.cs`

**Dependencies**: IDockingService
**Key features**:
- Manage dock zone visibility
- Handle zone highlighting during drag
- Coordinate with docking service

## Phase 4: View Implementation

### 10. Minimized Widget View

**File**: `src/Views/Widgets/MinimizedWidget.axaml`

**Features**:
- Square button with rounded corners
- Icon display (AvaloniaUI icon components)
- Floating title label below
- Notification badge (hidden/circle/pill based on count)
- Optional progress bar (thermometer style)
- Drag-and-drop support

### 11. Widget Container View

**File**: `src/Views/Widgets/WidgetContainer.axaml`

**Features**:
- Canvas for absolute positioning
- Grid overlay for visual feedback
- Dock zone overlays
- Drag-and-drop handling
- Multiple widget support

### 12. Open Widget View

**File**: `src/Views/Widgets/WidgetView.axaml`

**Features**:
- Title bar with widget title
- Control buttons (minimize always, close/pop-out configurable)
- Content area for widget-specific UI
- Balatro UI styling integration
- Drag support for docking

## Phase 5: Integration

### 12.5. Widget State Persistence (Content Only)

**Purpose**: Persist widget content state (not layout/positions) between sessions

**Implementation**:
- Widget layout resets each session (positions, dock state, minimized/open)
- Widget content persists (e.g., search instances, progress, notifications)
- Use `SaveStateAsync()` and `LoadStateAsync()` methods on widgets
- Store persisted state in existing application storage (e.g., search databases)

**Search Instance Widget Example**:
```csharp
public async Task<object?> SaveStateAsync()
{
    return new SearchInstanceState
    {
        SearchId = this.SearchId,
        FilterPath = this.CurrentFilterPath,
        // ... other search-specific state
    };
}

public async Task LoadStateAsync(object? state)
{
    if (state is SearchInstanceState searchState)
    {
        await RestoreSearchInstance(searchState.SearchId);
        // ... restore search-specific state
    }
}
```

## Phase 5: Integration

### 13. Dependency Registration

Register services in your DI container:
```csharp
services.AddSingleton<IWidgetRegistry, WidgetRegistryService>();
services.AddSingleton<IWidgetLayoutService, WidgetLayoutService>();
services.AddSingleton<IDockingService, DockingService>();
services.AddTransient<WidgetContainerViewModel>();
```

### 14. Widget Registration

Register existing components as widgets:
```csharp
var registry = serviceProvider.GetService<IWidgetRegistry>();
registry.RegisterWidget(new WidgetMetadata
{
    Id = "search-widget",
    Title = "Search",
    WidgetType = typeof(SearchWidget),
    ViewModelType = typeof(SearchWidgetViewModel),
    // ... other properties
});
```

### 15. Main Window Integration

Add widget container to your main window or create a dedicated workspace view:
```xml
<views:WidgetContainer 
    DataContext="{Binding WidgetContainer}"
    Grid.Row="1" />
```

## Testing Strategy

### Unit Tests
- Service logic (positioning, docking calculations)
- ViewModel behavior (state transitions, commands)
- Model validation rules

### Integration Tests
- Widget creation and registration
- Drag-and-drop operations
- Service interaction patterns

### UI Tests
- Visual feedback during operations
- Responsive layout behavior
- Cross-platform compatibility

## Performance Considerations

### Memory Management
- Dispose widget instances properly
- Use weak event patterns for service events
- Avoid memory leaks in drag operations

### UI Performance
- Virtualize large widget collections if needed
- Use efficient data binding patterns
- Minimize layout calculations during drag

### Startup Performance
- Lazy-load widget types
- Register widgets on-demand
- Use async patterns for heavy operations

## Common Patterns

### Creating a New Widget Type
1. Implement `IWidget` interface
2. Create corresponding ViewModel inheriting from `WidgetViewModel`
3. Design AXAML view with proper data binding
4. Register widget type with metadata
5. Handle widget-specific state and behavior

### Handling Widget State Changes
- Use ObservableProperty for automatic change notification
- Validate state transitions in ViewModel
- Update visual elements through data binding
- Persist temporary state in memory only (no session persistence)

### Implementing Drag and Drop
- Use AvaloniaUI's built-in DragDrop class
- Handle DragEnter, DragOver, and Drop events
- Coordinate with layout and docking services
- Provide visual feedback during operations

## Troubleshooting

### Common Issues
- **Service injection failures**: Ensure services are registered before ViewModels
- **Drag positioning errors**: Verify grid calculations and bounds checking
- **State synchronization**: Use proper observable patterns and data binding
- **Performance issues**: Profile memory usage and layout calculations

### Debugging Tips
- Use debug visualizers for grid positions
- Log service interactions for troubleshooting
- Test edge cases (window resize, rapid state changes)
- Validate widget lifecycle management

## Next Steps

After basic implementation:
1. Add animation transitions between states
2. Implement widget persistence if requirements change
3. Add keyboard navigation support
4. Create widget templates for common patterns
5. Optimize performance based on usage patterns

## Resources

- [AvaloniaUI Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MVVM Best Practices](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/xaml/xaml-basics/data-bindings-to-mvvm)
- Project CLAUDE.md for Balatro-specific patterns and conventions