---
name: add-avalonia-view-and-viewmodel
description: Creates new Avalonia View + MVVM ViewModel pairs following project patterns. Use when adding a new screen, modal, widget, or user control.
---

# Add Avalonia View and ViewModel

## Quick Start

When creating a new View/ViewModel pair:

1. Create ViewModel in `src/BalatroSeedOracle/ViewModels/`
2. Create View in `src/BalatroSeedOracle/Views/` (or `Views/Modals/`, `Components/`)
3. Register ViewModel in `ServiceCollectionExtensions.cs`

## ViewModel Template

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels;

public partial class MyFeatureViewModel : ObservableObject
{
    // Dependencies via constructor injection
    private readonly IConfigurationService _configurationService;

    public MyFeatureViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        DebugLogger.Log("MyFeatureViewModel", "Initialized");
    }

    // Source-generated property with change notification
    [ObservableProperty]
    private string _title = string.Empty;

    // Source-generated command
    [RelayCommand]
    private void DoSomething()
    {
        DebugLogger.Log("MyFeatureViewModel", "DoSomething executed");
    }

    // Async command with proper error handling
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            // Async work here
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("MyFeatureViewModel", $"LoadData failed: {ex.Message}");
        }
    }
}
```

## View Template (.axaml)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:BalatroSeedOracle.ViewModels"
             x:Class="BalatroSeedOracle.Views.MyFeatureView"
             x:DataType="vm:MyFeatureViewModel"
             x:CompileBindings="True">
    
    <StackPanel>
        <TextBlock Text="{Binding Title}" />
        <Button Content="Do Something" Command="{Binding DoSomethingCommand}" />
    </StackPanel>
</UserControl>
```

## Code-Behind Template (.axaml.cs)

```csharp
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views;

public partial class MyFeatureView : UserControl
{
    public MyFeatureView()
    {
        InitializeComponent();
    }

    // Constructor with ViewModel injection (for DI scenarios)
    public MyFeatureView(MyFeatureViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
```

## DI Registration

Add to `src/BalatroSeedOracle/Extensions/ServiceCollectionExtensions.cs`:

```csharp
// Transient for modals/widgets (new instance each time)
services.AddTransient<MyFeatureViewModel>();

// Singleton for main views (shared instance)
services.AddSingleton<MainFeatureViewModel>();
```

## UI Thread Marshaling

For background thread updates:

```csharp
Avalonia.Threading.Dispatcher.UIThread.Post(() =>
{
    Title = newValue;
});

// Or async version
await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
{
    Title = newValue;
});
```

## Checklist

- [ ] ViewModel inherits `ObservableObject`
- [ ] View has `x:DataType` and `x:CompileBindings="True"`
- [ ] ViewModel registered in `ServiceCollectionExtensions.cs`
- [ ] Uses `DebugLogger` for logging (not `Console.WriteLine`)
- [ ] Private fields use `_camelCase`, public properties use `PascalCase`
