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

### Platform Projects: Overrides Only (the "folder thing")

Platform head projects (`.Desktop`, `.Browser`, `.Android`, `.iOS`) are **folders**: code in a platform project is **not included** when building another platform. Use that instead of shared interfaces for platform-only behavior.

- **Desktop/** contains desktop-only code (e.g. desktop widget setup). Browser build does not compile or include it. No need for a shared "provider" interface — just put the code in the Desktop folder.
- Platform projects should contain **ONLY**: entry point, service registration via `PlatformServices.RegisterServices`, and platform-specific overrides.
- **DO NOT** duplicate service implementations; **DO NOT** add shared interfaces (e.g. "IDesktopWidgetProvider") when the same result is achieved by having the code only in the compatible platform folder.

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
- **ViewModels** receive all dependencies via **constructor injection**
- **Views** receive their ViewModel via **constructor injection** (the creator resolves the ViewModel from DI and passes it). Never use `ServiceHelper.GetService` or `GetRequiredService` inside a View or ViewModel **constructor**
- Use `ServiceHelper.GetService<T>()` only for one-off resolution outside constructors (e.g. in a method when no injected dependency exists). Prefer constructor injection everywhere

### MVVM & XPLAT organization (proper structure)

- **Core project** (`BalatroSeedOracle`): Shared business logic, ViewModels, shared Views, services, and `IPlatformServices`. Referenced by all platform heads. No `#if` for platform; use interfaces + DI.
- **Platform projects** (`.Desktop`, `.Browser`, `.Android`, `.iOS`): Entry point, `PlatformServices.RegisterServices`, and **platform-only** Views/ViewModels/services. Code in a platform project is not compiled for other platforms (the "folder thing").
- **Who creates Views/ViewModels:** The composition root (App + DI) creates the main window and main menu and injects their ViewModels. Modals and dialogs should be created the same way: resolve View + ViewModel from DI (or a small factory that uses DI), then assign `DataContext`/content. Do not use `new ModalView()` and then `ServiceHelper` in the View constructor to build the ViewModel.
- **Thin Views:** Views only display and forward input; they do not resolve services in constructors. Prefer a modal host or factory that resolves modal View + ViewModel from DI so the host (e.g. main menu) stays agnostic of construction details.

**MVVM checklist (new / refactored code):**

- [ ] ViewModels: all dependencies via **constructor injection**; no `ServiceHelper`/`App.GetService` in constructors.
- [ ] Views: ViewModel (or content) **injected by creator** or resolved from DI by the caller; no `ServiceHelper` in View constructors.
- [ ] Modals: resolve modal View + ViewModel from DI (e.g. `GetRequiredService<CreditsModal>()`); no `new ModalView()` + ServiceHelper inside the View.
- [ ] Prefer one resolution path: use `ServiceHelper` in shared code; avoid mixing `App.GetService` and `ServiceHelper` for the same purpose.

For a detailed scrutiny of current gaps and migration steps, see **docs/SCRUTINY_MVVM_XPLAT.md**.

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

### Excel Export

- Desktop: Use `ClosedXmlExcelExporter` (ClosedXML library)
- Browser: Use `BrowserExcelExporter` (stub - not yet implemented)

**Interface**: `IExcelExporter` in `src/BalatroSeedOracle/Services/Export/`

```csharp
public interface IExcelExporter
{
    bool IsAvailable { get; }
    Task ExportAsync(string filePath, string sheetName, 
                     IReadOnlyList<string> headers, 
                     IReadOnlyList<IReadOnlyList<object?>> rows);
}
```

## AOT Compilation

**Required stack (already configured):** AOT, WASM SIMD, and DuckDB-WASM are in use. Do not assume they don't work; do not reinvent. See `docs/SCRUTINY_MVVM_XPLAT.md` §0 and [Avalonia Web Assembly](https://docs.avaloniaui.net/docs/guides/platforms/how-to-use-web-assembly).

All platforms support **Ahead-of-Time (AOT) compilation** for optimal performance:

### BalatroSeedOracle Desktop AOT
- Enabled via `<PublishAot>true</PublishAot>` in `BalatroSeedOracle.Desktop.csproj`
- Native binary output with no JIT overhead
- Faster startup and reduced memory usage

### BalatroSeedOracle Browser AOT + WASM SIMD + DuckDB-WASM
- AOT: `<RunAOTCompilation>true</RunAOTCompilation>` in `BalatroSeedOracle.Browser.csproj`; requires `<PublishTrimmed>true</PublishTrimmed>` for WASM
- WASM SIMD: `<WasmEnableSIMD>true</WasmEnableSIMD>` (and optionally `<WasmEnableThreads>true</WasmEnableThreads>`)
- DuckDB in browser: `wwwroot/js/duckdb-interop.js` + `duckdb-wasm/` (DuckDB-WASM bundle); Desktop/Android use `DuckDB.NET.Data.Full`
- Pre-compiled WebAssembly modules; do not remove or replace this stack

### Motely AOT Configuration

**Motely Core Library** (`external/Motely/Motely/Motely.csproj`):
- **Desktop**: `<PublishAot>true</PublishAot>` enabled for `net10.0` target framework
- **Browser**: `<RunAOTCompilation>true</RunAOTCompilation>` and `<PublishTrimmed>true</PublishTrimmed>` enabled for `net10.0-browser` target framework
- AOT infrastructure includes:
  - `MotelyJsonSerializerContext` - AOT-compatible JSON serialization
  - `MotelyYamlStaticContext` - AOT-compatible YAML serialization
  - AOT-safe property access patterns in `JamlTypeAsKeyConverter`

**Motely CLI** (`external/Motely/Motely.CLI/Motely.CLI.csproj`):
- `<PublishAot>true</PublishAot>` enabled for native compilation
- Use `dotnet publish -c Release -p:SelfContained=true` to build AOT binary
- Run published executable: `.\Motely.CLI\bin\Release\net10.0\publish\MotelyCLI.exe`

### AOT-Compatible Patterns

**JSON Serialization**: Use `System.Text.Json` source generation
```csharp
[JsonSerializable(typeof(MotelyJsonConfig))]
internal partial class MotelyJsonSerializerContext : JsonSerializerContext { }

// Usage
JsonSerializer.Deserialize(json, MotelyJsonSerializerContext.Default.MotelyJsonConfig);
```

**YAML Serialization**: Use YamlDotNet static context
```csharp
[YamlStaticContext]
[YamlSerializable(typeof(MotelyJsonConfig))]
public partial class MotelyYamlStaticContext : StaticContext { }

// Usage
var deserializer = new StaticDeserializerBuilder(MotelyYamlStaticContext.Instance).Build();
```

**Enum Operations**: Use generic overloads
```csharp
// ✅ AOT-compatible
var values = Enum.GetValues<MotelyJoker>();

// ❌ Not AOT-compatible
var values = Enum.GetValues(typeof(MotelyJoker));
```

**Property Access**: Use static mappings instead of reflection
```csharp
// ✅ AOT-compatible
private static readonly Dictionary<string, Action<object, object?>> PropertySetters = new()
{
    ["PropertyName"] = (obj, val) => ((MyType)obj).PropertyName = (string?)val
};

// ❌ Not AOT-compatible
propertyInfo.SetValue(obj, value);
```

## Service Lifecycle

1. **Registration**: Services registered in platform entry points or `ServiceCollectionExtensions`
2. **Initialization**: Platform services initialized in `App.axaml.cs`
3. **Usage**: Services injected into ViewModels via constructor
4. **Disposal**: Services implementing `IDisposable` are disposed on app shutdown

## Best Practices

1. **No `#if` Directives**: Use `IPlatformServices` pattern instead
2. **No Service Locator in Constructors**: Views and ViewModels receive dependencies via constructor injection only. The creator (DI or composition root) resolves from the container and passes in. Never call `ServiceHelper.GetService` or `GetRequiredService` inside a constructor
3. **No Legacy Code**: Remove all backward compatibility code
4. **No Migration Logic**: Rebuild data rather than migrating
5. **Shared Code**: Keep common implementations in main project
6. **Platform Overrides**: Only in platform head projects when absolutely necessary
