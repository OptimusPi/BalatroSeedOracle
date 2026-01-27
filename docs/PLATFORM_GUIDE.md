# Platform Support Guide

## Adding New Platform Support

### 1. Create Platform Project

Create a new project in `src/BalatroSeedOracle.{Platform}/`:
- `Program.cs` (or platform-specific entry point)
- `{Platform}.csproj`
- Platform-specific resources if needed

### 2. Register Platform Services

In the platform entry point, register services:

```csharp
PlatformServices.RegisterServices = services =>
{
    // Register platform-specific implementations
    services.AddSingleton<IAppDataStore, {Platform}AppDataStore>();
    services.AddSingleton<IDuckDBService, {Platform}DuckDBService>();
    services.AddSingleton<IPlatformServices, {Platform}PlatformServices>();
    services.AddSingleton<IExcelExporter, {Platform}ExcelExporter>();
    
    // Platform-specific services
    services.AddSingleton<IAudioManager, {Platform}AudioManager>();
};
```

### 3. Implement Platform Services

If the platform shares capabilities with Desktop/Android/iOS, use shared implementations from `Services/Platforms/`:

- `FileSystemAppDataStore` - For platforms with file system access
- `FileSystemDuckDBService` - For platforms with native DuckDB
- `FileSystemPlatformServices` - For platforms with file system, audio, analyzer

If the platform has unique capabilities, create platform-specific implementations in the platform project.

### 4. Override Pattern

Platform projects should contain **ONLY**:
- Entry point code
- Service registration
- Platform-specific overrides

**DO NOT** duplicate service implementations that can be shared.

## Platform Capabilities

### Desktop (Windows, Linux, macOS)
- File System: ✅ Native
- Audio: ✅ SoundFlow
- Analyzer: ✅ Native DuckDB
- Results Grid: ✅ Full support

### Browser (WebAssembly)
- File System: ❌ (use localStorage)
- Audio: ✅ Web Audio API
- Analyzer: ✅ DuckDB-WASM
- Results Grid: ✅ Full support
- Excel Export: ❌ (stub implementation)
- AOT Compilation: ✅ Enabled

### Android
- File System: ✅ Native
- Audio: ✅ SoundFlow
- Analyzer: ✅ Native DuckDB
- Results Grid: ✅ Full support

### iOS
- File System: ✅ Native
- Audio: ✅ SoundFlow
- Analyzer: ✅ Native DuckDB
- Results Grid: ✅ Full support

## Testing Cross-Platform Features

1. Test on Desktop first (primary target)
2. Test on Browser (secondary target)
3. Test on Mobile if applicable
4. Verify platform-specific features work correctly
5. Check for platform-specific bugs

## Common Pitfalls

1. **Don't use `#if` directives**: Use `IPlatformServices` pattern
2. **Don't duplicate code**: Use shared implementations
3. **Don't put business logic in platform projects**: Keep it in main project
4. **Don't assume platform capabilities**: Check `IPlatformServices` properties
