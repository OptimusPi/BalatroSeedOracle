# Triage Avalonia Binding

Systematic checklist for debugging Avalonia MVVM binding and command issues.

## Input

- Description of the binding/command issue
- Relevant View (`.axaml`) and ViewModel (`.cs`) files

## Steps

1. **Check DataContext Setup**

   In the View (`.axaml`):
   ```xml
   <UserControl x:DataType="vm:YourViewModel">
   ```

   In code-behind (`.axaml.cs`):
   ```csharp
   DataContext = App.GetService<YourViewModel>();
   ```

   Or via ViewLocator if using that pattern.

2. **Verify x:DataType Declaration**

   - Must be at root element level
   - Must match actual ViewModel type
   - Namespace must be declared: `xmlns:vm="using:BalatroSeedOracle.ViewModels"`

3. **Check Compiled Bindings**

   For compiled bindings (recommended):
   ```xml
   <TextBlock Text="{Binding PropertyName}" />
   ```

   If binding fails silently, try adding `x:CompileBindings="True"` to get compile-time errors.

4. **Verify Property Implementation**

   ViewModel property must be observable:
   ```csharp
   [ObservableProperty]
   private string _propertyName = "";
   ```

   Or manual:
   ```csharp
   public string PropertyName
   {
       get => _propertyName;
       set => SetProperty(ref _propertyName, value);
   }
   ```

5. **Check Command Binding**

   Command must be:
   ```csharp
   [RelayCommand]
   private void DoSomething() { }
   // Generates: DoSomethingCommand
   ```

   XAML binding:
   ```xml
   <Button Command="{Binding DoSomethingCommand}" />
   ```

6. **Verify DI Registration**

   In `App.axaml.cs`:
   ```csharp
   services.AddTransient<YourViewModel>();
   ```

7. **Add Debug Logging**

   In ViewModel constructor:
   ```csharp
   DebugLogger.Log("YourViewModel", "Constructor called");
   ```

   In property setters:
   ```csharp
   DebugLogger.Log("YourViewModel", $"PropertyName changed to: {value}");
   ```

8. **Check Avalonia Output**

   Look for binding errors in:
   - IDE Output window
   - Console/terminal output
   - Avalonia DevTools (if enabled)

## Output

Identified root cause and fix for binding issue.

## Notes

- **Common issues**: Wrong `x:DataType`, missing `[ObservableProperty]`, ViewModel not registered in DI, DataContext not set.
- **Async commands**: Use `[RelayCommand]` on `async Task` methods - generates `DoSomethingCommand` with built-in busy state.
- **Collection bindings**: Use `ObservableCollection<T>` for lists that update.
- **Related skill**: See `@.cursor/skills/mvvm-binding-debugger/SKILL.md` for detailed debugging workflow.
