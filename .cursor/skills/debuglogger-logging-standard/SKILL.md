---
name: debuglogger-logging-standard
description: Implements logging using DebugLogger following project conventions. Use when adding diagnostics, error handling, or debug output in C# files.
---

# DebugLogger Logging Standard

## Required: Use DebugLogger

**ONLY** use these methods for logging:

```csharp
DebugLogger.Log("Category", "Message");           // Debug messages
DebugLogger.LogError("Category", "Error message"); // Error messages
DebugLogger.LogImportant("Category", "Important"); // Important info (verbose)
```

Location: `src/BalatroSeedOracle/Helpers/DebugLogger.cs`

## Forbidden

**NEVER** use `Console.WriteLine()` for debug messages except:
- Inside `BrowserPlatformServices` or `DesktopPlatformServices`
- As fallback in `DebugLogger.LogInternal()` itself

```csharp
// ❌ Bad - Don't use Console directly
Console.WriteLine("Search started");
System.Diagnostics.Debug.WriteLine("Debug info");

// ✅ Good - Use DebugLogger
DebugLogger.Log("SearchModal", "Search started");
```

## Usage Patterns

### Standard Logging

```csharp
DebugLogger.Log("MyViewModel", "Operation started");
DebugLogger.Log("MyService", $"Processing {count} items");
```

### Error Logging

```csharp
try
{
    await DoWorkAsync();
}
catch (Exception ex)
{
    DebugLogger.LogError("MyService", $"Operation failed: {ex.Message}");
    throw; // or handle appropriately
}
```

### Important/Verbose Logging

```csharp
// Only shown when verbose logging is enabled
DebugLogger.LogImportant("FilterService", $"Loaded {filters.Count} filters");
```

### Format Logging

```csharp
DebugLogger.LogFormat("MyService", "Processed {0} of {1} items", current, total);
```

## Category Naming

Use the class name as category:

```csharp
public class SearchModalViewModel
{
    private void StartSearch()
    {
        DebugLogger.Log("SearchModalViewModel", "Search initiated");
    }
}
```

## How It Works

DebugLogger routes output through `IPlatformServices.WriteLog()`:
- **Desktop**: `Console.WriteLine()`
- **Browser**: `System.Console.WriteLine()` with `Debug.WriteLine()` fallback

Logging is only active in DEBUG builds when enabled:

```csharp
// In App.axaml.cs or startup
DebugLogger.SetDebugEnabled(true);    // Enable debug logging
DebugLogger.SetVerboseEnabled(true);  // Enable verbose/important logging
```

## Checklist

- [ ] Used `DebugLogger.Log/LogError/LogImportant` (not Console)
- [ ] Category matches class name
- [ ] Error messages include exception details (`ex.Message`)
- [ ] No sensitive data in log messages
