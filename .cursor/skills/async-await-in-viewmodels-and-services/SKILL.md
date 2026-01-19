---
name: async-await-in-viewmodels-and-services
description: Applies async/await patterns following project conventions. Use when adding async commands, I/O operations, or background work in ViewModels and services.
---

# Async/Await in ViewModels and Services

## Core Rules

### No Fake Async

```csharp
// ✅ Good - No async keyword when no await
public Task DoSomething()
{
    // Synchronous work
    return Task.CompletedTask;
}

// ❌ Bad - Fake async
public async Task DoSomething()
{
    // Synchronous work - unnecessary async keyword
}
```

### No Blocking Calls

```csharp
// ❌ Bad - Blocks thread, can deadlock
var result = asyncMethod.Result;
asyncMethod.Wait();
asyncMethod.GetAwaiter().GetResult();

// ✅ Good
var result = await asyncMethod;
```

### No Task.Run for Async Work

```csharp
// ❌ Bad - Don't wrap async in Task.Run
Task.Run(() => DoWorkAsync());

// ✅ Good - Use proper async
await DoWorkAsync();
```

## ConfigureAwait Usage

| Context              | ConfigureAwait          | Reason                              |
| -------------------- | ----------------------- | ----------------------------------- |
| Service/Library code | `ConfigureAwait(false)` | Avoid unnecessary context switches  |
| ViewModel/UI code    | Omit                    | Need UI thread for property updates |

```csharp
// Service code
public async Task<Data> LoadDataAsync()
{
    var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
    return JsonSerializer.Deserialize<Data>(json);
}

// ViewModel code - omit ConfigureAwait
[RelayCommand]
private async Task LoadAsync()
{
    var data = await _service.LoadDataAsync();
    Title = data.Name; // UI property update
}
```

## Fire-and-Forget Pattern

```csharp
// ✅ Good - Discard with error handling inside method
_ = DoBackgroundWorkAsync();

private async Task DoBackgroundWorkAsync()
{
    try
    {
        await Task.Delay(1000);
        // Work here
    }
    catch (Exception ex)
    {
        DebugLogger.LogError("MyClass", $"Background work failed: {ex.Message}");
    }
}
```

## ViewModel Async Commands

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            var data = await _service.GetDataAsync();
            // Update properties
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("MyViewModel", $"Load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

## Service Async Pattern

```csharp
public class MyService
{
    public async Task<Result> ProcessAsync(CancellationToken cancellationToken = default)
    {
        // Always propagate cancellation token
        var data = await _repository.GetAsync(cancellationToken).ConfigureAwait(false);
        
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();
        
        return await TransformAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
```

## UI Thread Marshaling

```csharp
// From background thread to UI
await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
{
    MyProperty = newValue;
});

// Or synchronous post (fire-and-forget)
Avalonia.Threading.Dispatcher.UIThread.Post(() =>
{
    MyProperty = newValue;
});
```

## Checklist

- [ ] No `async` keyword without `await`
- [ ] No `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- [ ] `ConfigureAwait(false)` in service/library code
- [ ] Error handling in async methods
- [ ] CancellationToken propagated where appropriate
- [ ] UI updates marshaled to UI thread
