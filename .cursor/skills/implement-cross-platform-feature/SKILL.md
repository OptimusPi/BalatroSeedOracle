---
name: implement-cross-platform-feature
description: Implements features with platform-specific behavior using the head/core architecture pattern. Use when adding features that work differently on Desktop vs Browser vs Mobile.
---

# Implement Cross-Platform Feature

## When to Use

Use this skill when:

- Adding a feature that works differently on Desktop vs Browser vs Mobile
- A feature needs file system access (unavailable on Browser)
- A feature uses audio, native dialogs, or other platform-specific APIs
- You need to add a new capability flag to `IPlatformServices`
- You need to create a new platform abstraction interface

## Prerequisites

- Understanding of the head/core architecture (see `@035-cross-platform-architecture.mdc`)
- Knowledge of which platforms support the feature you're implementing
- Familiarity with dependency injection in this project

## Decision Tree

Before starting, determine your approach:

**Option A: Simple capability flag**

- Feature can be enabled/disabled with a boolean flag
- No different implementation needed, just on/off
- Example: "Does this platform support audio?"
- → Follow **Steps 1-3** (Add capability flag)

**Option B: Different implementations**

- Feature needs completely different code per platform
- Example: Desktop uses file system, Browser uses localStorage
- → Follow **Steps 4-7** (Create abstraction interface)

**Option C: Compile-time exclusion**

- Types don't exist on some platforms (rare)
- Example: Desktop-only library types
- → Follow **Steps 8-9** (Use compile guards)

---

## Steps

### Option A: Add Capability Flag to IPlatformServices

#### 1. Add Property to IPlatformServices Interface

Edit `@src/BalatroSeedOracle/Services/IPlatformServices.cs`:

```csharp
/// <summary>
/// Whether the platform supports [feature name].
/// </summary>
bool SupportsMyFeature { get; }
```

**Naming convention**: `Supports[FeatureName]` (e.g., `SupportsFileSystem`, `SupportsAudio`)

#### 2. Implement in All Platform Services

**Desktop** (`@src/BalatroSeedOracle.Desktop/Services/DesktopPlatformServices.cs`):

```csharp
public bool SupportsMyFeature => true;
```

**Browser** (`@src/BalatroSeedOracle.Browser/Services/BrowserPlatformServices.cs`):

```csharp
public bool SupportsMyFeature => false; // or true if supported
```

**Android** (`@src/BalatroSeedOracle.Android/Services/AndroidPlatformServices.cs`):

```csharp
public bool SupportsMyFeature => true; // or false
```

**iOS** (if applicable): Same pattern

#### 3. Use in Core Code

In your ViewModel or Service:

```csharp
public class MyViewModel : ObservableObject
{
    private readonly IPlatformServices _platformServices;

    public MyViewModel(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    [RelayCommand]
    private async Task DoFeatureAsync()
    {
        if (!_platformServices.SupportsMyFeature)
        {
            DebugLogger.Log("MyViewModel", "Feature not supported on this platform");
            // Optionally show user message
            return;
        }

        // Feature implementation
        await PerformFeatureWorkAsync();
    }
}
```

**✅ Done!** Skip to Verification section.

---

### Option B: Create Platform Abstraction Interface

Use this when you need different implementations per platform (not just on/off).

#### 4. Define Interface in Core

Create `@src/BalatroSeedOracle/Services/IMyService.cs`:

```csharp
namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Platform abstraction for [feature description].
    /// Implementations provided by head projects.
    /// </summary>
    public interface IMyService
    {
        /// <summary>
        /// [Method description]
        /// </summary>
        Task<Result> DoWorkAsync(string parameter);

        /// <summary>
        /// Whether this service is available on the current platform.
        /// </summary>
        bool IsAvailable { get; }
    }
}
```

**Best practices**:

- Include `IsAvailable` property for runtime checks
- Use async methods (`Task`/`Task<T>`) for I/O operations
- Document platform differences in XML comments

#### 5. Implement in Desktop Head

Create `@src/BalatroSeedOracle.Desktop/Services/DesktopMyService.cs`:

```csharp
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Desktop.Services
{
    /// <summary>
    /// Desktop implementation of IMyService.
    /// Uses [platform-specific API/library].
    /// </summary>
    public sealed class DesktopMyService : IMyService
    {
        public bool IsAvailable => true;

        public async Task<Result> DoWorkAsync(string parameter)
        {
            DebugLogger.Log("DesktopMyService", $"Processing: {parameter}");
            
            // Desktop-specific implementation
            // Example: File system access, native APIs, etc.
            
            return new Result { Success = true };
        }
    }
}
```

#### 6. Implement in Browser Head

Create `@src/BalatroSeedOracle.Browser/Services/BrowserMyService.cs`:

```csharp
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Browser.Services
{
    /// <summary>
    /// Browser implementation of IMyService.
    /// Uses [browser API/workaround] or no-op if unsupported.
    /// </summary>
    public sealed class BrowserMyService : IMyService
    {
        private readonly IAppDataStore _store;

        public BrowserMyService(IAppDataStore store)
        {
            _store = store;
        }

        public bool IsAvailable => false; // or true if you have a workaround

        public Task<Result> DoWorkAsync(string parameter)
        {
            if (!IsAvailable)
            {
                DebugLogger.Log("BrowserMyService", "Feature not supported on Browser");
                return Task.FromResult(new Result { Success = false, Message = "Not supported" });
            }

            // Browser-specific implementation (if any)
            // Example: localStorage, IndexedDB, JS interop
            
            return Task.FromResult(new Result { Success = true });
        }
    }
}
```

**Android/iOS**: Repeat similar pattern for other platforms.

#### 7. Register in Dependency Injection

**Desktop** (`@src/BalatroSeedOracle.Desktop/Program.cs`):

```csharp
services.AddSingleton<IMyService, DesktopMyService>();
```

**Browser** (`@src/BalatroSeedOracle.Browser/Program.cs`):

```csharp
services.AddSingleton<IMyService, BrowserMyService>();
```

**Core** (`@src/BalatroSeedOracle/Extensions/ServiceCollectionExtensions.cs`):

```csharp
// Add comment noting platform registration:
// Note: IMyService registered by platform projects (Desktop/Browser/Android)
```

#### 7b. Consume in Core Code

Inject the service in your ViewModel/Service:

```csharp
public class MyViewModel : ObservableObject
{
    private readonly IMyService _myService;

    public MyViewModel(IMyService myService)
    {
        _myService = myService;
    }

    [RelayCommand]
    private async Task DoWorkAsync()
    {
        if (!_myService.IsAvailable)
        {
            DebugLogger.Log("MyViewModel", "Service not available");
            return;
        }

        var result = await _myService.DoWorkAsync("parameter");
        if (result.Success)
        {
            // Handle success
        }
    }
}
```

**✅ Done!** Skip to Verification section.

---

### Option C: Compile-Time Guards (Rare)

Use ONLY when types don't exist on all platforms. Prefer Options A or B when possible.

#### 8. Add Compile Guards to Core

```csharp
#if !BROWSER
using DesktopOnlyLibrary;
#endif

public class MyViewModel : ObservableObject
{
#if !BROWSER
    private DesktopOnlyType? _desktopFeature;
#endif

    public bool CanUseFeature =>
#if !BROWSER
        _desktopFeature != null;
#else
        false;
#endif

    [RelayCommand]
    private void UseFeature()
    {
#if !BROWSER
        _desktopFeature?.DoWork();
#else
        DebugLogger.Log("MyViewModel", "Feature not available on Browser");
#endif
    }
}
```

#### 9. Document Why Compile Guards Are Needed

Add XML comment explaining why runtime abstraction wasn't possible:

```csharp
#if !BROWSER
/// <summary>
/// Desktop-only feature using [LibraryName] which is not available in WASM.
/// Compile guard required because types don't exist in Browser build.
/// </summary>
private DesktopOnlyType? _desktopFeature;
#endif
```

---

## Verification

After implementation, verify all platforms:

### Desktop Build

- [ ] Feature works as expected on Desktop
- [ ] No compilation errors
- [ ] Logging shows correct behavior

```bash
dotnet build src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj
dotnet run --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj
```

### Browser Build

- [ ] Browser build compiles successfully
- [ ] Feature gracefully degrades (no crashes)
- [ ] User sees appropriate message if feature unavailable

```bash
dotnet build src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
```

### Runtime Checks

- [ ] `IPlatformServices.SupportsX` returns correct value per platform
- [ ] Service `IsAvailable` property returns correct value
- [ ] Fallback behavior works (no exceptions)
- [ ] Debug logs show platform-specific behavior

### Code Review Checklist

- [ ] Interface defined in core (if Option B)
- [ ] All head projects have implementations (even if no-op)
- [ ] DI registration in all head `Program.cs` files
- [ ] No platform-specific code in core (except rare compile guards)
- [ ] Graceful degradation with logging
- [ ] XML documentation on new interfaces/methods

---

## Common Issues

### Issue: Build fails with "type or namespace not found"

**Cause**: Forgot to implement interface in one of the head projects.

**Solution**:

1. Check all head projects: Desktop, Browser, Android, iOS
2. Ensure each has an implementation class
3. Verify DI registration in each `Program.cs`

### Issue: NullReferenceException at runtime

**Cause**: Service not registered in DI container for current platform.

**Solution**:

1. Check head project's `Program.cs` for registration
2. Verify service is registered as Singleton or Transient
3. Ensure constructor dependencies are also registered

### Issue: Feature silently doesn't work on Browser

**Cause**: Missing capability check or `IsAvailable` check.

**Solution**:

1. Add `if (!_platformServices.SupportsX)` check
2. Add logging: `DebugLogger.Log("Component", "Feature not supported")`
3. Optionally show user-facing message

### Issue: Compile guards everywhere in core project

**Cause**: Using Option C when Option A or B would work.

**Solution**:

1. Refactor to use `IPlatformServices` capability flag (Option A)
2. Or create abstraction interface with platform implementations (Option B)
3. Reserve `#if BROWSER` for truly unavoidable cases (types don't exist)

### Issue: Desktop feature breaks Browser build

**Cause**: Desktop-specific code in core without guards.

**Solution**:

1. Move platform-specific code to head project
2. Create abstraction interface
3. Use `IPlatformServices` runtime checks

---

## Related

- `@035-cross-platform-architecture.mdc` - Architecture overview and principles
- `@030-platform-guards.mdc` - Runtime vs compile-time guards
- `@090-browser-wasm-constraints.mdc` - Browser limitations
- `@register-services-and-platform-overrides/SKILL.md` - DI registration details
- `@apply-platform-guards-browser-vs-desktop/SKILL.md` - Quick platform guard patterns
