# AI Coding Guidelines - Balatro Seed Oracle

## CRITICAL RULES (AI Agent Compatibility)

### 1. LOGGING VIOLATIONS - NEVER BREAK THESE RULES
- **NEVER** use `Console.WriteLine()` for debug messages in production code
- **ONLY** use `DebugLogger.Log()` for debug messages  
- **ONLY** print debug messages when --debug flag is set
- Console.WriteLine should ONLY be used for critical errors/failures that must always be visible
- Debug messages must be conditional on debug mode, not printed in Release builds

### 2. DEBUG LOGGER USAGE
```csharp
// ✅ CORRECT - Use DebugLogger
DebugLogger.Log("ComponentName", "Message");
DebugLogger.LogError("ComponentName", "Error message");
DebugLogger.LogImportant("ComponentName", "Important info");

// ❌ WRONG - Never use Console for debug
Console.WriteLine("Debug message"); // BANNED
```

### 3. CODE PATTERNS
- Keep methods simple and focused
- Add clear comments for complex business logic
- Use consistent naming conventions
- Follow existing MVVM patterns

## Project Architecture

### Main Components
- **App.axaml.cs** - Application entry point and service setup
- **Program.cs** - Main method with exception handling
- **DebugLogger.cs** - CENTRALIZED logging (use this ONLY)
- **Services/** - Core business logic
- **ViewModels/** - MVVM view models
- **Views/** - Avalonia UI components
- **Components/** - Reusable UI controls

### Key Services
- **SearchManager** - Handles seed searching operations
- **SpriteService** - Manages game assets
- **UserProfileService** - User settings and preferences
- **FilterCacheService** - Filter management and caching

### Critical Files
- `src/BalatroSeedOracle/Helpers/DebugLogger.cs` - LOGGING SYSTEM
- `src/Program.cs` - Application startup
- `src/BalatroSeedOracle/App.axaml.cs` - Service configuration

## AI Agent Best Practices

### Before Making Changes
1. **Read existing files** to understand patterns
2. **Search for similar implementations** using grep
3. **Check for existing DebugLogger usage** in the area
4. **Follow existing naming conventions**

### Making Changes
1. **Use DebugLogger.Log()** for all debug output
2. **Add meaningful comments** for complex logic
3. **Keep changes minimal** and focused
4. **Test compilation** after changes

### Common Tasks
- **Adding new features**: Follow existing MVVM patterns
- **Fixing bugs**: Use DebugLogger for error reporting
- **UI changes**: Follow Avalonia patterns in existing components
- **Service changes**: Use dependency injection patterns

## Platform Compatibility
- Code supports Desktop, Browser, Android, iOS
- Use `#if BROWSER` and `#if !BROWSER` for platform-specific code
- Desktop is the primary target for releases

## Avalonia UI Framework - AI Agent Reference

### Core Concepts
- **Cross-platform XAML framework** for .NET
- **MVVM pattern** - Model-View-ViewModel architecture
- **Reactive UI** - Data binding and property change notification
- **Platform abstraction** - Single codebase, multiple platforms

### Key Documentation Resources
- **Official Site**: https://avaloniaui.net/
- **GitHub Repository**: https://github.com/AvaloniaUI/Avalonia
- **Documentation**: https://docs.avaloniaui.net/ (when available)
- **Samples**: https://github.com/AvaloniaUI/Avalonia/tree/master/samples

### Avalonia Patterns in This Project

#### 1. MVVM Architecture
```csharp
// View Model - Inherits from ViewModelBase
public class SearchWidgetViewModel : ViewModelBase
{
    private string _searchText;
    public string SearchText 
    { 
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
}

// View - XAML with DataContext binding
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBox Text="{Binding SearchText}" />
</UserControl>
```

#### 2. Dependency Injection
```csharp
// In App.axaml.cs - Service registration
services.AddSingleton<SearchManager>();
services.AddTransient<SearchWidgetViewModel>();

// In ViewModels/Services - Constructor injection
public SearchWidgetViewModel(SearchManager searchManager)
{
    _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
}
```

#### 3. Commands and Events
```csharp
// Command in ViewModel
public ICommand SearchCommand { get; }

// Constructor
SearchCommand = new RelayCommand(ExecuteSearch);

// Method
private void ExecuteSearch()
{
    // Search logic here
    DebugLogger.Log("SearchWidget", "Search executed");
}
```

#### 4. Platform-Specific Code
```csharp
#if BROWSER
    // Browser-specific implementation
    await _store.WriteTextAsync(key, value);
#else
    // Desktop implementation
    await File.WriteAllTextAsync(path, content);
#endif
```

#### 5. Styling and Theming
```csharp
// Dynamic resource binding
Background = DynamicResource.Parse("SystemControlBackgroundAltHighBrush");

// Custom styling in XAML
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="#FF007ACC" />
</Style>
```

### Common Avalonia Controls Used
- **UserControl** - Custom components
- **Window** - Main application windows
- **Grid/StackPanel** - Layout containers
- **TextBox/TextBlock** - Text display/input
- **Button** - Click interactions
- **ListBox/ItemsControl** - Data lists
- **TabControl** - Tabbed interfaces

### AI Agent Guidelines for Avalonia

#### When Creating UI Components:
1. **Follow MVVM pattern** - Separate UI from logic
2. **Use data binding** - Avoid direct UI manipulation
3. **Implement INotifyPropertyChanged** - Use ViewModelBase
4. **Use commands** - Not event handlers
5. **Platform awareness** - Use conditional compilation

#### When Modifying Existing UI:
1. **Study similar components** - Find existing patterns
2. **Check ViewModelBase usage** - Follow property patterns
3. **Use DebugLogger** - For UI debugging
4. **Test on multiple platforms** - Browser/Desktop differences

#### Styling Guidelines:
1. **Use dynamic resources** - For theme support
2. **Follow existing styles** - In App.axaml or separate files
3. **Responsive design** - Handle different screen sizes
4. **Accessibility** - Use proper ARIA labels where applicable

### Performance Considerations
- **Virtualization** for large lists (VirtualizingStackPanel)
- **Data binding optimization** - Avoid expensive operations in getters
- **UI thread awareness** - Use Dispatcher for UI updates
- **Memory management** - Unsubscribe from events appropriately

### Debugging Avalonia
- **Live preview** - Use Visual Studio/XAML preview
- **DebugLogger integration** - Add logging to ViewModels
- **Platform debugging** - Test Browser vs Desktop separately
- **Binding errors** - Check Output window for binding issues

## Emergency Release Notes
This project was made AI-compatible on 2026-01-01 to fix critical logging violations that were preventing AI agents from working correctly.

### Fixed Issues:
- DebugLogger hard-coded enabled flags set to false
- Removed forced debug enable in Program.cs
- Replaced all Console.WriteLine calls with DebugLogger
- Added these AI coding guidelines

### For Future AI Agents:
1. **Always use DebugLogger** - never Console.WriteLine for debug
2. **Follow these patterns** - they're proven to work
3. **Keep it simple** - complexity breaks AI agents
4. **Test changes** - compilation errors block progress

---
Created: 2026-01-01
Purpose: Make Balatro Seed Oracle compatible with AI coding agents
Status: AI READY ✅
