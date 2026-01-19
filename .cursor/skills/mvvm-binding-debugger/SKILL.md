---
name: mvvm-binding-debugger
description: Debugs Avalonia MVVM binding and command issues systematically. Use when UI isn't updating, commands don't fire, or bindings show errors.
---

# MVVM Binding Debugger

## Common Binding Issues

| Symptom                 | Likely Cause                   | Fix                                             |
| ----------------------- | ------------------------------ | ----------------------------------------------- |
| UI not updating         | Missing `[ObservableProperty]` | Add attribute to private field                  |
| Command not firing      | Wrong command name in XAML     | Use generated name (e.g., `DoSomethingCommand`) |
| Binding error in output | Missing `x:DataType`           | Add `x:DataType="vm:MyViewModel"`               |
| Designer shows nothing  | `DataContext` not set          | Set in constructor or XAML                      |

## Diagnostic Checklist

### 1. ViewModel Setup

```csharp
// ✅ Must inherit ObservableObject
public partial class MyViewModel : ObservableObject

// ✅ Must use [ObservableProperty] on private fields
[ObservableProperty]
private string _myProperty = string.Empty;

// ✅ Commands use [RelayCommand] on methods
[RelayCommand]
private void DoSomething() { }
```

### 2. XAML Setup

```xml
<!-- ✅ Must have x:DataType for compiled bindings -->
<UserControl x:DataType="vm:MyViewModel"
             x:CompileBindings="True">

<!-- ✅ Binding path matches property name -->
<TextBlock Text="{Binding MyProperty}" />

<!-- ✅ Command binding uses generated name -->
<Button Command="{Binding DoSomethingCommand}" />
```

### 3. DataContext Setup

```csharp
// Option 1: Constructor injection
public MyView(MyViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
}

// Option 2: Service lookup
public MyView()
{
    InitializeComponent();
    DataContext = ServiceHelper.GetService<MyViewModel>();
}
```

## Debug Techniques

### Add Logging to Property Changes

```csharp
[ObservableProperty]
private string _title = string.Empty;

partial void OnTitleChanged(string value)
{
    DebugLogger.Log("MyViewModel", $"Title changed to: {value}");
}
```

### Check DataContext at Runtime

```csharp
// In code-behind
public MyView()
{
    InitializeComponent();
    DataContextChanged += (s, e) =>
    {
        DebugLogger.Log("MyView", $"DataContext: {DataContext?.GetType().Name ?? "null"}");
    };
}
```

### Temporarily Disable Compiled Bindings

```xml
<!-- Isolate binding issues -->
<TextBlock Text="{Binding MyProperty}" x:CompileBindings="False" />
```

## Common Fixes

### Property Not Updating

```csharp
// ❌ Wrong - public field
public string Title;

// ❌ Wrong - property without notification
public string Title { get; set; }

// ✅ Correct - ObservableProperty
[ObservableProperty]
private string _title = string.Empty;
```

### Command Not Working

```csharp
// Generated command name is method name + "Command"
[RelayCommand]
private void Save() { }  // → SaveCommand

[RelayCommand]
private async Task LoadAsync() { }  // → LoadCommand (Async suffix is typically dropped)
```

```xml
<!-- Match the generated name -->
<Button Command="{Binding SaveCommand}" />
```

### Parent Binding Issues

```xml
<!-- Explicit cast for parent context under compiled bindings -->
<Button Command="{Binding $parent[ItemsControl].((vm:MainViewModel)DataContext).DeleteCommand}" />
```

## DI Registration Check

Verify ViewModel is registered:

```csharp
// In ServiceCollectionExtensions.cs
services.AddTransient<MyViewModel>();  // or AddSingleton
```

## Checklist

- [ ] ViewModel inherits `ObservableObject`
- [ ] Properties use `[ObservableProperty]` attribute
- [ ] Commands use `[RelayCommand]` attribute
- [ ] XAML has `x:DataType` and `x:CompileBindings="True"`
- [ ] `DataContext` is set correctly
- [ ] ViewModel registered in DI container
- [ ] Binding paths match property names exactly (case-sensitive)
