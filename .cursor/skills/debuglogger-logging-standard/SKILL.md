---
name: debuglogger-logging-standard
description: Implements logging using DebugLogger following project conventions. Use when adding diagnostics, error handling, or debug output in C# files.
---

# DebugLogger Logging Standard

## Required: Use DebugLogger

**ONLY** use these methods for logging:

```csharp
DebugLogger.Log("Category", "Message");              // Debug-level messages
DebugLogger.LogWarning("Category", "Warning");       // Warning messages
DebugLogger.LogError("Category", "Error message");   // Error messages
DebugLogger.LogImportant("Category", "Important");   // Important info
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

// ✅ Good - Use DebugLogger with appropriate level
DebugLogger.Log("SearchModal", "Search started");
DebugLogger.LogWarning("SearchModal", "No results found");
DebugLogger.LogError("SearchModal", "Search failed");
```

## Usage Patterns

### Standard Debug Logging

```csharp
DebugLogger.Log("MyViewModel", "Operation started");
DebugLogger.Log("MyService", $"Processing {count} items");
```

### Warning Logging (NEW - Production-Visible)

```csharp
// Warnings are logged by default in Release builds
DebugLogger.LogWarning("MyService", "Configuration missing, using defaults");
DebugLogger.LogWarning("MyViewModel", $"Unexpected state: {state}");
```

### Error Logging (Production-Visible)

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

### Important/Operational Logging

```csharp
// Important operational information
DebugLogger.LogImportant("FilterService", $"Loaded {filters.Count} filters");
DebugLogger.LogImportant("App", "Initialization complete");
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

## Log Levels (Production-Capable)

BSO uses runtime-configurable log levels that work in **both Debug and Release builds**:

- **Error** (0) - Critical errors, always logged
- **Warning** (1) - Warnings about potential issues (default in Release)
- **Important** (2) - Important operational information
- **Debug** (3) - Debug-level diagnostics (default in Debug builds)
- **Verbose** (4) - Verbose/trace-level information

### Configuration

Set log level via environment variable or CLI:

```bash
# Environment variable
export BSO_LOG_LEVEL=debug
./BalatroSeedOracle

# CLI argument
./BalatroSeedOracle --log-level=warning
```

Valid values: `error`, `warning`, `important`, `debug`, `verbose`

**Default behavior:**
- Debug builds: `Debug` level (shows all logs)
- Release builds: `Warning` level (errors + warnings only)

### Programmatic Configuration

```csharp
// In Program.cs or startup code
DebugLogger.SetMinimumLevel(BsoLogLevel.Warning);  // Set minimum level

// Legacy methods (deprecated but still work)
DebugLogger.SetDebugEnabled(true);    // Maps to Debug level
DebugLogger.SetVerboseEnabled(true);  // Maps to Verbose level
```

## How It Works

DebugLogger routes output through `IPlatformServices`:

- **Desktop**: `Console.WriteLine()` for Error/Warning/Important, `Debug.WriteLine()` for Debug/Verbose
- **Browser**: `System.Console.WriteLine()` with `Debug.WriteLine()` fallback

**Production-capable:** Logging works in Release builds with runtime filtering (not compiled out).

## Checklist

- [ ] Used `DebugLogger.Log/LogWarning/LogError/LogImportant` (not Console)
- [ ] Category matches class name
- [ ] Error messages include exception details (`ex.Message`)
- [ ] No sensitive data in log messages
- [ ] Used appropriate log level (Error for errors, Warning for warnings, etc.)
