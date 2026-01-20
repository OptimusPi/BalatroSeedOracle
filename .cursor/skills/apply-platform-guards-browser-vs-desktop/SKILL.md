---
name: apply-platform-guards-browser-vs-desktop
description: Adds platform-specific behavior guards for Browser vs Desktop. Use when implementing features involving file I/O, audio, dialogs, or other platform-dependent functionality.
---

# Apply Platform Guards (Browser vs Desktop)

## Preferred: Runtime Checks via IPlatformServices

Always prefer runtime checks over compile-time guards:

```csharp
// ✅ Good - Runtime check
if (_platformServices.SupportsFileSystem)
{
    await File.WriteAllTextAsync(path, content);
}

// ❌ Avoid - Compile-time guard (use only when necessary)
#if !BROWSER
    await File.WriteAllTextAsync(path, content);
#endif
```

## Available Platform Capabilities

| Property              | Desktop | Browser | Use For             |
| --------------------- | ------- | ------- | ------------------- |
| `SupportsFileSystem`  | ✅      | ❌      | File I/O operations |
| `SupportsAudio`       | ✅      | ❌      | Audio playback      |
| `SupportsAnalyzer`    | ✅      | ❌      | Seed analysis tools |
| `SupportsResultsGrid` | ✅      | ❌      | Results grid/tab UI |

## Common Patterns

### File Operations

```csharp
public async Task SaveDataAsync(string data)
{
    if (!_platformServices.SupportsFileSystem)
    {
        // Browser: use localStorage via IAppDataStore
        await _appDataStore.WriteTextAsync("key", data);
        return;
    }

    // Desktop: direct file access
    var path = Path.Combine(AppPaths.DataDir, "data.json");
    await File.WriteAllTextAsync(path, data);
}
```

### Audio Features

```csharp
public void PlaySound(string soundName)
{
    if (!_platformServices.SupportsAudio)
    {
        DebugLogger.Log("Audio", "Audio not supported on this platform");
        return;
    }

    _audioManager?.Play(soundName);
}
```

### Optional Service Pattern

```csharp
// Null-safe for services that may not exist on all platforms
_audioManager?.Play(soundName);
_analyzerService?.Analyze(seed);
```

## Browser Constraints

The browser platform has these limitations:

- **No file system** - Use `IAppDataStore` (localStorage)
- **No stdin** - Cannot use `Console.ReadLine()`
- **No native dialogs** - File picker limited
- **No audio** - `IPlatformServices.SupportsAudio` is false
- **DuckDB is available via WASM**, but some native features/extensions are not (e.g., DuckLake)

## Compile-Time Guards (When Required)

Use only when types/members don't exist on other platforms:

```csharp
#if !BROWSER
private SearchInstance? _searchInstance;
#endif

public bool CanMinimizeToDesktop =>
#if !BROWSER
    _searchInstance != null && !string.IsNullOrEmpty(_currentSearchId);
#else
    false;
#endif
```

## Service Implementation Pattern

```csharp
// Desktop implementation
public class DesktopDuckDBService : IDuckDBService
{
    public async Task<IEnumerable<Result>> QueryAsync(string sql)
    {
        using var connection = new DuckDBConnection(_connectionString);
        // Full implementation
    }
}

// Browser implementation (no-op or alternative)
public class BrowserDuckDBService : IDuckDBService
{
    public Task<IEnumerable<Result>> QueryAsync(string sql)
    {
        // In this repo, browser DuckDB uses WASM + JS interop (see BrowserDuckDBService).
        // For unsupported features, either no-op or throw NotSupportedException with a clear message.
    }
}
```

## Checklist

- [ ] Used `IPlatformServices` runtime check (not `#if` guard)
- [ ] Provided fallback behavior for unsupported platforms
- [ ] Used `IAppDataStore` for browser storage (not file system)
- [ ] Optional services accessed with null-conditional (`?.`)
- [ ] Logged platform limitations with `DebugLogger`

## Related

- `@035-cross-platform-architecture.mdc` - Architecture overview and principles
- `@implement-cross-platform-feature/SKILL.md` - Complete workflow for new features
- `@030-platform-guards.mdc` - Platform-specific code rules
