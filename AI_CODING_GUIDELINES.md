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
- **NEVER** use `#if BROWSER` / `#if !BROWSER` conditional compilation
- Use **dependency injection** and **platform abstractions** instead
- Create interfaces in shared code, implementations in platform-specific projects
- Use Avalonia's `IStorageProvider` for cross-platform file access
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
// ❌ WRONG - Never use conditional compilation
#if BROWSER
    await _store.WriteTextAsync(key, value);
#else
    await File.WriteAllTextAsync(path, content);
#endif

// ✅ CORRECT - Use dependency injection with platform abstractions
public interface IFileService
{
    Task WriteAsync(string path, string content);
}

// Desktop implementation (in Desktop project)
public class DesktopFileService : IFileService
{
    public Task WriteAsync(string path, string content) 
        => File.WriteAllTextAsync(path, content);
}

// Browser implementation (in Browser project)  
public class BrowserFileService : IFileService
{
    public Task WriteAsync(string path, string content)
        => _store.WriteTextAsync(path, content);
}

// Usage - inject IFileService, platform registers correct implementation
public class MyViewModel(IFileService fileService)
{
    await fileService.WriteAsync(path, content); // Works on all platforms
}
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
5. **Platform awareness** - Use DI abstractions, NOT conditional compilation

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

---
Last Updated: 2026-01-27
