# Performance Optimization Guide

## TreeDataGrid Optimization

### Compiled Bindings

Always use `x:CompileBindings="True"` in XAML root elements for better performance:

```xml
<UserControl x:CompileBindings="True" ...>
```

### Virtualization

TreeDataGrid has built-in virtualization. Ensure:
- Large datasets use `FlatTreeDataGridSource<T>` or `HierarchicalTreeDataGridSource<T>`
- Don't disable virtualization
- Use `MaxDisplayResults` appropriately (10,000+ rows supported)

### Cell Templates

- Minimize nesting in cell templates
- Remove unnecessary converters
- Use `StreamGeometry` instead of `PathGeometry` for icons
- Minimize `Run` elements in `TextBlock`
- Flatten visual tree hierarchy

### Data Loading

- Load data on background threads
- Dispatch minimal updates to UI thread
- Use progressive loading for large result sets
- Implement lazy loading for hierarchical data

## Database Performance

### Query Optimization

- Use indexes on frequently queried columns
- Limit result sets appropriately
- Motely handles seed length inconsistencies automatically
- SIMD optimizations handle performance for large datasets

## Background Threading

### Pattern

```csharp
await Task.Run(() => {
    // Heavy computation
}).ContinueWith(task => {
    // Update UI on UI thread
    Dispatcher.UIThread.Post(() => {
        // UI updates
    });
});
```

### Best Practices

- Keep UI thread responsive
- Use `ConfigureAwait(false)` in library code
- Dispatch UI updates via `Dispatcher.UIThread.Post`
- Use cancellation tokens for long-running operations

## Memory Management

- Dispose resources properly (`IDisposable`)
- Use `using` statements for database connections
- Clear large collections when no longer needed
- Monitor memory usage with profilers

## Profiling

- Use Avalonia Developer Tools (F12)
- Use .NET profilers (PerfView, dotTrace)
- Monitor binding errors in logs
- Check for memory leaks
