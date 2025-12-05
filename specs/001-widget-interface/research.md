# Research: Widget Interface System

**Date**: 2025-12-02  
**Branch**: `001-widget-interface`

## Research Areas

### AvaloniaUI Drag and Drop Patterns

**Decision**: Use AvaloniaUI's built-in drag-and-drop support with custom DragDrop.DoDragDrop() implementation  
**Rationale**: AvaloniaUI provides native cross-platform drag-and-drop capabilities that integrate well with MVVM patterns. The DragDrop class offers events for DragEnter, DragOver, and Drop that can be bound to ViewModels.  
**Alternatives considered**: 
- Custom pointer event handling - rejected due to complexity and platform differences
- Third-party libraries - rejected to maintain consistency with existing codebase

### Grid Layout Management

**Decision**: Use Canvas with calculated positioning for grid layout  
**Rationale**: Canvas provides absolute positioning needed for precise grid snapping while maintaining performance. Grid size of 100x100px allows for predictable calculations and visual consistency.  
**Alternatives considered**:
- UniformGrid - rejected due to inflexibility for dynamic positioning
- Custom Panel - rejected as unnecessarily complex for fixed grid requirements

### Widget Docking Implementation

**Decision**: Use AvaloniaUI's DockPanel with custom dock zone detection  
**Rationale**: DockPanel provides built-in docking behavior that can be extended with visual drop zones. Custom overlays can indicate valid drop areas during drag operations.  
**Alternatives considered**:
- Custom docking library - rejected to avoid external dependencies
- Manual positioning calculations - rejected due to complexity and maintenance burden

### State Management Patterns

**Decision**: Use ObservableProperty attributes from CommunityToolkit.Mvvm for widget state  
**Rationale**: Consistent with existing codebase patterns. Provides automatic INotifyPropertyChanged implementation and integrates well with AvaloniaUI binding system.  
**Alternatives considered**:
- Manual INotifyPropertyChanged implementation - rejected for maintainability
- ReactiveUI - rejected to maintain consistency with existing MVVM approach

### Icon Management

**Decision**: Use AvaloniaUI's built-in icon system with resource-based icons  
**Rationale**: Consistent with project requirement to replace emoji with proper icon components. AvaloniaUI supports vector icons through XAML resources.  
**Alternatives considered**:
- Font-based icons - rejected for complexity in AvaloniaUI context
- Bitmap icons - rejected for scaling issues across different DPI settings

### Notification Badge Rendering

**Decision**: Custom UserControl with dynamic shape rendering based on count  
**Rationale**: Allows precise control over badge appearance (hidden/circle/pill) based on notification count. Can be styled consistently with Balatro UI theme.  
**Alternatives considered**:
- Template selectors - rejected for complexity
- Multiple badge controls - rejected for resource inefficiency

### Progress Bar Implementation

**Decision**: Custom thermometer-style ProgressBar template  
**Rationale**: AvaloniaUI allows complete template customization for ProgressBar controls. Thermometer style can be achieved through custom control template with appropriate styling.  
**Alternatives considered**:
- Canvas-based custom drawing - rejected for performance concerns
- Image-based progress - rejected for theme consistency issues

## Technology Integration Points

### Existing Balatro UI Styling

**Research findings**: Current application uses consistent color schemes and component styling. Widget system must inherit these styles through AvaloniaUI's styling system using ResourceDictionary references.

### MVVM Integration

**Research findings**: Application uses CommunityToolkit.Mvvm extensively with `[ObservableProperty]` and `[RelayCommand]` attributes. Widget ViewModels should follow this pattern for consistency.

### Service Integration

**Decision**: Use constructor dependency injection for ViewModels instead of service locator pattern  
**Rationale**: Service locator is an anti-pattern in MVVM. Constructor injection provides better testability, explicit dependencies, and follows SOLID principles.  
**Research findings**: Widget services should be registered in DI container and injected into ViewModels via constructor parameters. This maintains proper separation of concerns and testability.  
**Alternatives considered**: 
- App.GetService<T>() service locator - rejected as anti-pattern
- Property injection - rejected for lack of required dependency enforcement

## Implementation Risk Assessment

### Low Risk
- Basic MVVM implementation (well-established patterns)
- Grid positioning calculations (simple math)
- State transitions (straightforward enum-based state machine)

### Medium Risk
- Drag and drop implementation (platform-specific behaviors)
- Docking visual feedback (custom UI elements)
- Integration with existing components (potential conflicts)

### High Risk
- Performance with many widgets (memory and rendering concerns)
- Complex drag operations across docked areas (interaction edge cases)

## Next Steps

1. Implement base IWidget interface and WidgetState enumeration
2. Create WidgetViewModel base class with observable properties
3. Develop minimized widget visual components
4. Implement grid layout and positioning system
5. Add drag-and-drop functionality
6. Create docking system with visual feedback
7. Integrate with existing application architecture