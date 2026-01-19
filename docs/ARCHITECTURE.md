# Architecture Documentation

## Platform Abstraction Pattern

### IPlatformServices Interface

The `IPlatformServices` interface provides a clean abstraction for platform-specific capabilities:

- `SupportsFileSystem`: Whether the platform has native file system access
- `SupportsAudio`: Whether the platform supports audio playback
- `SupportsAnalyzer`: Whether the analyzer feature is available
- `SupportsResultsGrid`: Whether the results grid feature is available

**Key Principle**: Use `IPlatformServices` checks instead of `#if BROWSER` / `#if !BROWSER` directives.

### Service Registration

Platform-specific services are registered in each platform's entry point:

- **Desktop**: `src/BalatroSeedOracle.Desktop/Program.cs`
- **Browser**: `src/BalatroSeedOracle.Browser/Program.cs`
- **Android**: `src/BalatroSeedOracle.Android/MainActivity.cs`
- **iOS**: `src/BalatroSeedOracle.iOS/AppDelegate.cs`

**Shared Services**: Common implementations live in `src/BalatroSeedOracle/Services/Platforms/`:
- `FileSystemAppDataStore`
- `FileSystemDuckDBService`
- `FileSystemPlatformServices`

### Platform Projects: Overrides Only

Platform head projects (`.Browser`, `.Android`, `.iOS`) should contain **ONLY**:
- Entry point code (`Program.cs`, `MainActivity.cs`, `AppDelegate.cs`)
- Service registration via `PlatformServices.RegisterServices`
- Platform-specific overrides when necessary

**DO NOT** duplicate service implementations in platform projects.

## MVVM Architecture

### ViewModels

- Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` for automatic property change notifications
- Use `[RelayCommand]` for commands
- Keep business logic in ViewModels, not Views

### Views

- Thin Views: Minimal code-behind
- Use data binding (`{Binding PropertyName}`)
- Use commands, not event handlers
- Direct field access via `x:Name` instead of `FindControl`

### Dependency Injection

- Services registered in `App.axaml.cs` via `ServiceCollectionExtensions`
- ViewModels receive services via constructor injection
- Access services via `App.GetService<T>()` or `ServiceHelper.GetService<T>()`

## Cross-Platform Considerations

### File System Access

- Desktop/Android/iOS: Use `FileSystemAppDataStore` (native file system)
- Browser: Use `BrowserLocalStorageAppDataStore` (localStorage)

### Database Access

- Desktop/Android/iOS: Use `FileSystemDuckDBService` (native DuckDB)
- Browser: Use `BrowserDuckDBService` (DuckDB-WASM)

### Audio

- Desktop: Use `SoundFlowAudioManager` (SoundFlow library)
- Browser: Use `SoundFlowAudioManager` with Web Audio API (same class, different implementation path)

## Service Lifecycle

1. **Registration**: Services registered in platform entry points or `ServiceCollectionExtensions`
2. **Initialization**: Platform services initialized in `App.axaml.cs`
3. **Usage**: Services injected into ViewModels via constructor
4. **Disposal**: Services implementing `IDisposable` are disposed on app shutdown

## Best Practices

1. **No `#if` Directives**: Use `IPlatformServices` pattern instead
2. **No Legacy Code**: Remove all backward compatibility code
3. **No Migration Logic**: Rebuild data rather than migrating
4. **Shared Code**: Keep common implementations in main project
5. **Platform Overrides**: Only in platform head projects when absolutely necessary
