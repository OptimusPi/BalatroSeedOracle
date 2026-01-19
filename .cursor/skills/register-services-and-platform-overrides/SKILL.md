---
name: register-services-and-platform-overrides
description: Registers services in DI container across core and platform projects. Use when adding new services, platform abstractions, or modifying dependency injection.
---

# Register Services and Platform Overrides

## Service Registration Locations

| Location                                                          | Purpose               |
| ----------------------------------------------------------------- | --------------------- |
| `src/BalatroSeedOracle/Extensions/ServiceCollectionExtensions.cs` | Core shared services  |
| `src/BalatroSeedOracle.Desktop/Program.cs`                        | Desktop-only services |
| `src/BalatroSeedOracle.Browser/Program.cs`                        | Browser-only services |

## Core Service Registration

Add to `ServiceCollectionExtensions.AddBalatroSeedOracleServices()`:

```csharp
// Singleton - shared instance, stateless/expensive resources
services.AddSingleton<IMyService, MyService>();
services.AddSingleton<MyService>();

// Transient - new instance per request, stateful/lightweight
services.AddTransient<MyViewModel>();

// With factory for complex initialization
services.AddSingleton<IMyService>(sp => new MyService(
    sp.GetRequiredService<IDependency1>(),
    sp.GetService<IOptionalDependency>()
));
```

## Platform-Specific Services

These are registered by platform projects, NOT in core:

```csharp
// Note in ServiceCollectionExtensions.cs:
// IAppDataStore, IDuckDBService, IPlatformServices registered by platform projects
// IApiHostService also registered by platform projects
```

### Desktop Registration (`Desktop/Program.cs`)

```csharp
services.AddSingleton<IAppDataStore, DesktopAppDataStore>();
services.AddSingleton<IDuckDBService, DesktopDuckDBService>();
services.AddSingleton<IPlatformServices, DesktopPlatformServices>();
services.AddSingleton<IApiHostService, DesktopApiHostService>();

// Desktop-only services
services.AddSingleton<SoundFlowAudioManager>();
services.AddSingleton<AnalyzeModalViewModel>();
```

### Browser Registration (`Browser/Program.cs`)

```csharp
services.AddSingleton<IAppDataStore, BrowserLocalStorageAppDataStore>();
services.AddSingleton<IDuckDBService, BrowserDuckDBService>();
services.AddSingleton<IPlatformServices, BrowserPlatformServices>();
services.AddSingleton<IApiHostService, BrowserApiHostService>();
```

## Lifetime Guidelines

| Lifetime      | Use For                                                | Examples                                                 |
| ------------- | ------------------------------------------------------ | -------------------------------------------------------- |
| **Singleton** | Stateless services, expensive resources, configuration | `SearchManager`, `SpriteService`, `ConfigurationService` |
| **Transient** | Stateful per-use, lightweight, ViewModels              | `MainWindowViewModel`, `CreditsModalViewModel`           |

## Creating a New Service

### 1. Define Interface (optional but recommended)

```csharp
// src/BalatroSeedOracle/Services/IMyService.cs
public interface IMyService
{
    Task<Result> DoWorkAsync();
}
```

### 2. Implement Service

```csharp
// src/BalatroSeedOracle/Services/MyService.cs
public class MyService : IMyService
{
    private readonly IPlatformServices _platformServices;

    public MyService(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    public async Task<Result> DoWorkAsync()
    {
        // Implementation
    }
}
```

### 3. Register in Core

```csharp
// ServiceCollectionExtensions.cs
services.AddSingleton<IMyService>(sp => new MyService(
    sp.GetRequiredService<IPlatformServices>()
));
```

## Platform-Specific Service Pattern

For services with different implementations per platform:

```csharp
// Core interface
public interface IPlatformFeature
{
    bool IsSupported { get; }
    Task ExecuteAsync();
}

// Desktop implementation
public class DesktopPlatformFeature : IPlatformFeature
{
    public bool IsSupported => true;
    public Task ExecuteAsync() => /* full implementation */;
}

// Browser implementation  
public class BrowserPlatformFeature : IPlatformFeature
{
    public bool IsSupported => false;
    public Task ExecuteAsync() => Task.CompletedTask; // no-op
}
```

## Checklist

- [ ] Service registered in correct location (core vs platform)
- [ ] Appropriate lifetime chosen (Singleton vs Transient)
- [ ] Dependencies resolved via constructor injection
- [ ] Platform-specific services have both Desktop and Browser implementations
- [ ] Optional dependencies use `GetService<T>()` not `GetRequiredService<T>()`
