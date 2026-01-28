# Avalonia UI Best Practices (2026)

## Compiled Bindings

Always enable compiled bindings for better performance:

```xml
<UserControl x:CompileBindings="True" ...>
```

**Benefits**:
- Compile-time binding validation
- Better performance (no reflection at runtime)
- Catches binding errors early

## MVVM Patterns

### ViewModels

- Use `CommunityToolkit.Mvvm` with source generators
- Use `[ObservableProperty]` for properties
- Use `[RelayCommand]` for commands
- Keep business logic in ViewModels

### Views

- Thin Views: Minimal code-behind
- Use data binding instead of event handlers
- Use commands, not click events
- Direct field access via `x:Name` instead of `FindControl`

### Example

```csharp
// ViewModel
[ObservableProperty]
private string _name = "";

[RelayCommand]
private void Save() { ... }
```

```xml
<!-- View -->
<TextBox Text="{Binding Name}" />
<Button Command="{Binding SaveCommand}" />
```

## Performance Tips

### TreeDataGrid

- Use `TreeDataGrid` for large lists (10,000+ rows)
- Virtualization is built-in
- Use compiled bindings
- Minimize cell template complexity

### Data Loading

- Load data on background threads
- Dispatch UI updates via `Dispatcher.UIThread.Post`
- Use progressive loading for large datasets
- Implement lazy loading for hierarchical data

### Visual Tree

- Flatten visual tree hierarchy
- Minimize nesting in templates
- Use `StreamGeometry` instead of `PathGeometry`
- Minimize `Run` elements in `TextBlock`

## Common Pitfalls

### 1. Binding Errors

- Use compiled bindings to catch errors early
- Check logs for binding errors
- Avoid `RelativeSource.FindAncestor` in data templates

### 2. UI Thread

- Always update UI on UI thread
- Use `Dispatcher.UIThread.Post` for background updates
- Don't block UI thread with heavy operations

### 3. Memory Leaks

- Unsubscribe from events
- Dispose resources properly
- Clear large collections when done

### 4. Platform-Specific Code

- **NEVER** use `#if BROWSER` / `#if !BROWSER` conditional compilation
- Use **dependency injection** with platform abstractions instead
- Use Avalonia's `IStorageProvider` for cross-platform file access
- Create interfaces in shared code, register platform implementations via DI
- Check platform capabilities at runtime where needed
- Keep platform-specific code in platform-specific projects (Desktop/Browser)

## Code Quality

### Logging

- Use `DebugLogger.Log()`, `DebugLogger.LogError()`, `DebugLogger.LogImportant()`
- **NEVER** use `Console.WriteLine()` for debug messages
- Log important operations and errors

### Async/Await

- Methods returning `Task` without `await` should return `Task.CompletedTask`
- Remove `async` keyword if no `await` is used
- Use `ConfigureAwait(false)` for library code

### Nullable Reference Types

- Use `is null` / `is not null` instead of `== null` / `!= null`
- Use null-forgiving operator `!` sparingly
- Prefer null-conditional `?.` operator

## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia Accelerate](https://docs.avaloniaui.net/accelerate/)
- [Performance Guide](https://docs.avaloniaui.net/docs/guides/development-guides/improving-performance)
- [Cross-Platform Guide](https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications)
