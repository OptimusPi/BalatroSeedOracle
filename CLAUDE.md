# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Initialize git submodule (Motely search engine) - REQUIRED on first clone
cd external
git submodule update --init --recursive
cd ..

# Run the GUI application (Debug)
dotnet run -c Debug --project ./src/BalatroSeedOracle.csproj

# Run the GUI application (Release - MUCH faster search)
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj

# Build only
dotnet build -c Release --project ./src/BalatroSeedOracle.csproj

# Run Motely CLI (for testing filters)
cd ./external/Motely/Motely
dotnet run -c Release -- --json PerkeoObservatory --threads 16

# Analyze a specific seed
dotnet run -c Release -- --analyze XTTO2111

# Search with wordlist
dotnet run -c Release -- --json MyFilter --wordlist 2NegativeEggs
```

## Project Architecture

### Core Components

**Avalonia MVVM Architecture**
- `ViewModels/` - Business logic and state management (CommunityToolkit.MVVM)
- `Views/` - XAML UI definitions
- `Models/` - Data models and domain entities
- `Services/` - Singleton services registered via DI (see `ServiceCollectionExtensions.cs`)

**Motely Search Engine** (git submodule in `external/Motely/`)
- High-performance vectorized Balatro seed analyzer
- Uses SIMD (AVX2/AVX512) for 10-50M seeds/second throughput
- MotelyJson filter system for complex seed criteria
- Thread-local counters with `Interlocked` operations for stats

**Key Services**
- `SearchManager` - Coordinates Motely searches, manages batching
- `VibeAudioManager` - Music player with 8-track mixer (Drums1/2, Bass1/2, Chords1/2, Melody1/2)
  - Uses NAudio for desktop audio playback
  - FFT analysis for beat detection and visualizer integration
  - All 8 tracks play simultaneously in MixingSampleProvider for perfect sync
- `UserProfileService` - Persists settings (volumes, filter preferences)
- `FavoritesService` - Manages saved search results

### Audio System Details

**Track Synchronization**
- All 8 OGG tracks MUST have identical sample counts (export from DAW with same loop markers/settings)
- Tracks added to NAudio MixingSampleProvider at startup - play simultaneously from sample 0
- Volume 0.0f keeps tracks in sync but silent (volume controls audibility, not playback position)
- LoopStream wraps each track for seamless looping

**Visualizer Integration**
- `BalatroShaderBackground` - Custom SkiaSharp shader with Balatro theme
- `PsychedelicShaderBackground` - WinAmp MilkDrop-inspired fractals
- FFT analysis feeds shader parameters (bass, mid, treble, beat detection)

### Filter System

**MotelyJson Configuration**
- JSON-based filter definitions in `external/Motely/Motely/JsonItemFilters/`
- Visual filter designer in `FiltersModal` (XAML + ViewModel)
- Fuzzy-match validator catches typos (e.g., "SoulJoker" → "Did you mean 'souljoker'?")
- Filter types: Vouchers, Soul Jokers, Regular Jokers, Tarots, Spectrals, Planets, Playing Cards, Boss Blinds, Tags

**Filter Scoring**
- `MotelyJsonScoring.cs` - Handles tally counting and score calculation
- Recent bug fix: `CountTagOccurrences()` returns 0/1/2 for tags in small/big blind
- Tag tally columns correctly use `CountTagOccurrences` instead of binary checks

### Database Layer

**DuckDB Integration**
- Results stored in `SearchResults/*.duckdb`
- Schema: seed (string), score (int), ante tallies (int columns)
- Auto-deletion of filter databases when filter is deleted
- `FilterDatabaseManager` handles database operations

### Dependency Injection

All services registered in `Extensions/ServiceCollectionExtensions.cs`:
```csharp
services.AddSingleton<SearchManager>();
services.AddSingleton<VibeAudioManager>();
services.AddSingleton<UserProfileService>();
// ... etc
```

Retrieve via `ServiceHelper.GetRequiredService<T>()` or `App.GetService<T>()`

### MVVM Patterns

**Proper MVVM** (what we aim for):
- ViewModels handle ALL business logic
- Code-behind only wires up events and finds controls
- DataContext set via DI
- Example: `SearchModalViewModel` - zero business logic in `SearchModal.axaml.cs`

**Common Pitfalls**
- Don't create new service instances - use DI singletons!
- Don't put business logic in code-behind
- Don't manually find controls unless absolutely necessary (databinding preferred)

## Known Issues & Gotchas

**Audio Desync**
- If tracks drift out of sync, OGG files have different sample counts
- Solution: Re-export all 8 tracks from DAW with identical settings and verify byte sizes are similar

**NAudio Limitations**
- Desktop only (Windows/Mac/Linux)
- Mobile/Web requires LibVLCSharp or platform-specific audio APIs

**Motely Stats Bug**
- Threads must call `FlushLocalCounters()` before exiting or stats show 0
- Fixed in `MotelySearch.cs` line 687-690

**Filter Validation**
- Always run validator on user JSON to catch typos
- Levenshtein distance < 3 triggers "Did you mean?" suggestions

## File Exports

**Audio Requirements**
- Format: WAV or OGG Vorbis (NAudio supported)
- Sample rate: 44100 Hz or 48000 Hz (consistent across all tracks)
- Bit depth: 16-bit or 24-bit
- **CRITICAL:** Export all 8 tracks with identical length/loop markers

**Filter Exports**
- JSON files saved to `JsonItemFilters/`
- Filter name automatically normalized (spaces → underscores)
- Associated DuckDB files stored in `SearchResults/`

## Performance Notes

**Search Optimization**
- Release builds are 10x+ faster than Debug (SIMD optimizations)
- Thread count: Typically CPU core count (adjustable in UI)
- Batch size: 10M seeds per batch (configurable)
- Typical throughput: 10-50M seeds/second depending on filter complexity

**Motely Vectorization**
- Uses `System.Runtime.Intrinsics` for SIMD operations
- AVX2/AVX512 detection with fallbacks for older CPUs
- Vector helpers in `Motely/MotelyVector*.cs`

## Project Structure Notes

- `src/Assets/Audio/` - 8-track music files (Drums1.ogg, Bass1.ogg, etc.) + SFX
- `src/Controls/` - Custom Avalonia controls (shaders, widgets)
- `src/Behaviors/` - Avalonia behaviors for UI interactions
- `src/Styles/` - XAML resource dictionaries (BalatroTabs.axaml, etc.)
- `external/Motely/` - Git submodule (DO NOT EDIT directly, fork repo if changes needed)

## Velopack Auto-Updates

- Version defined in `BalatroSeedOracle.csproj` (AssemblyVersion, FileVersion)
- `VelopackApp.Build().Run()` in `Program.cs` handles update checks
- Build release packages with Velopack CLI

## Critical Code Patterns

**Thread-Safe Operations**
```csharp
// Motely stats - use Interlocked for thread-local counters
Interlocked.Add(ref _localSeedsProcessed, batch);
```

**Audio Track Control**
```csharp
// Direct volume control (bypasses state machine)
vibeAudioManager.SetTrackVolume("Drums1", 0.8f, pan: 0.0f);
```

**Filter Validation**
```csharp
// Always validate before search
var errors = MotelyJsonConfigValidator.Validate(config);
if (errors.Any()) { /* show errors */ }
```
