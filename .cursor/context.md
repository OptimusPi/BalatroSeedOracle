# Cursor AI Context - Balatro Seed Oracle

## Quick Reference

### Project Type
- **Framework**: Avalonia UI (cross-platform .NET)
- **Language**: C# (.NET 10)
- **Pattern**: MVVM (Model-View-ViewModel)
- **Platforms**: Desktop (primary), Browser (secondary), Android/iOS (future)

### Key Technologies
- **Avalonia UI**: Cross-platform XAML framework
- **CommunityToolkit.Mvvm**: MVVM helpers (`ObservableObject`, `RelayCommand`)
- **YamlDotNet**: YAML parsing for JAML filters
- **Motely**: High-performance seed search engine (submodule)
- **SoundFlow**: Audio engine (Desktop only)

### Critical Files to Know

#### Entry Points
```
src/Program.cs                          # Main entry (Desktop)
src/BalatroSeedOracle.Desktop/Program.cs  # Desktop-specific
src/BalatroSeedOracle/App.axaml.cs      # App setup, DI registration
```

#### Core Services
```
src/BalatroSeedOracle/Services/
  ├── SearchInstance.cs                 # Search execution engine
  ├── FilterService.cs                  # Filter CRUD operations
  ├── SpriteService.cs                  # Game asset management
  └── UserProfileService.cs             # User settings
```

#### ViewModels (MVVM)
```
src/BalatroSeedOracle/ViewModels/
  ├── FiltersModalViewModel.cs          # Filter creation/editing
  ├── SearchModalViewModel.cs           # Search interface
  └── AnalyzerViewModel.cs              # Seed analysis
```

#### Helpers
```
src/BalatroSeedOracle/Helpers/
  ├── DebugLogger.cs                    # ⚠️ USE THIS FOR ALL LOGGING
  ├── ServiceHelper.cs                  # Service locator
  └── JamlAutocompletionHelper.cs      # JAML editor autocomplete
```

### Common Patterns

#### ViewModel Pattern
```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _myProperty = "";
    
    [RelayCommand]
    private void DoSomething()
    {
        DebugLogger.Log("MyViewModel", "DoSomething called");
    }
}
```

#### Service Registration
```csharp
// In App.axaml.cs
services.AddSingleton<IMyService, MyService>();
services.AddTransient<MyViewModel>();
```

#### Service Usage
```csharp
// Constructor injection (preferred)
public MyViewModel(IMyService myService) { }

// Or service locator
var service = ServiceHelper.GetService<IMyService>();
```

### Domain Concepts

#### JAML Filter Structure
```yaml
Should:
  - And:
    Antes: [2,3,4]
    clauses:
      - joker: Blueprint
        score: 10
      - smallblindtag: NegativeTag
```

#### YAML Anchors (Templates)
```yaml
# Define once
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]

# Reuse
Should:
  - And:
    Antes: [2,3,4]
    clauses: *oops_cluster  # Inherits Antes automatically!
```

### Common Commands

#### Build & Run
```bash
# Desktop Release (fast)
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj

# Desktop Debug (slow, for development)
dotnet run -c Debug --project ./src/BalatroSeedOracle.csproj

# Browser
dotnet run -c Release --project ./src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
```

#### VS Code
- `Ctrl+Shift+B` - Run Desktop (Release) - **Default**
- `F5` - Debug with selected configuration
- `Ctrl+Shift+P` → "Tasks: Run Task" - All tasks

### Important Notes

1. **Logging**: Always use `DebugLogger.Log()`, never `Console.WriteLine()`
2. **Async**: Return `Task.CompletedTask` if no await needed
3. **Nullability**: Use `is null` not `== null`
4. **Platform**: Use `#if BROWSER` for platform-specific code
5. **MVVM**: Keep UI logic in ViewModels, not code-behind

### File Locations

- **Filters**: `JamlFilters/*.jaml`
- **Presets**: `Presets/`, `MixerPresets/`, `VisualizerPresets/`
- **User Data**: `User/userprofile.json`
- **External**: `external/Motely/` (git submodule - Motely search engine)
- **Documentation**: Root directory `.md` files

### Release Priorities

1. **Desktop Release build** - Primary target
2. **No compilation errors** - Critical
3. **Proper logging** - Use DebugLogger
4. **Performance** - Release builds are optimized
5. **User experience** - Test all major features

---

**For detailed guidelines**: See `AI_CODING_GUIDELINES.md`
**For JAML syntax**: See `YAML_BEST_PRACTICES.md`
**For architecture**: See `.cursorrules`
