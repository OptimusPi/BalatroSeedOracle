# 🔧 CLAUDE.md - Development Guidelines for Balatro Seed Oracle

## 🎯 Project Philosophy

This is a high-performance Balatro seed searching application that combines:
- **CPU SIMD optimization** for blazing-fast parallel seed searching
- **MongoDB-style composite filters** for intelligent early-exit searching
- **Professional UI/UX** with AvaloniaUI for accessibility
- **Persistent storage** with DuckDB so users never lose results

## ⚠️ CRITICAL: PRNG Accuracy is SACRED

**The #1 Rule**: The Motely engine MUST maintain 100.0000000000% accuracy with Balatro's PRNG.

### Core Engine: OuijaJsonFilterDesc.cs
**THE BRAIN** of our JSON filter system! Located in `external/Motely/filters/OuijaJsonFilterDesc.cs`
- Implements MongoDB-style composite filters (Must/MustNot/Should)
- Direct integration: UI calls this C# class directly (no subprocess needed!)
- Performance-critical: Uses vectorization where possible
- Known issues (95% working):
  - May have caching bugs
  - Might check too many things unnecessarily
  - May not fully utilize Motely's vectorized contexts

### CLI Entry Point: Program.cs
- Located in `external/Motely/Program.cs`
- Test filters: `dotnet run -- --config naninf --seed dev5B111 --debug`
- Options: `--cutoff`, `--threads`, `--debug`, `--analyze`, etc.

### Before Touching Motely Core:
1. **STOP AND THINK** - Any change to PRNG logic can break everything
2. **Test CLI First** - Always verify with known seeds: `dotnet run -- --config naninf --seed dev5B111 --debug`
3. **Compare with Game** - Results must match actual Balatro behavior exactly
4. **Extreme Caution** - When in doubt, don't change it. Ask first.

## 🏗️ Architecture Overview

### Two-Layer Architecture
```
┌─────────────────────────────────────┐
│   Balatro Seed Oracle (Frontend)    │ <- AvaloniaUI, User-Friendly
│  - Visual Filter Builder            │
│  - DuckDB Persistence               │
│  - Results Visualization            │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│      Motely Engine (Backend)        │ <- SIMD, Performance-Critical
│  - AVX-512/AVX2 Vectorization      │
│  - Composite Filter System          │
│  - 100% Accurate PRNG              │
└─────────────────────────────────────┘
```

## 🚀 Filter System Architecture

### MongoDB-Style Composite Filters

The filter system uses a sophisticated early-exit strategy for maximum performance:

```json
{
  "must": [],      // ALL must match - early exit on first failure
  "mustNot": [],   // NONE can match - early exit on any match  
  "should": []     // Optional matches - used for SCORING only
}
```

### Performance Strategy (OuijaJsonFilterDesc Implementation)

1. **Early Exit on Must/MustNot**
   - Vectorized processing for supported types (Tags, Vouchers, basic Jokers)
   - Non-vectorized fallback for complex types (SoulJokers, PlayingCards)
   - Fail fast on first unmet "must" condition
   - Fail fast on any "mustNot" match
   - Check cancellation between operations

2. **Scoring with Should**
   - "Should" criteria don't reject seeds
   - Used to calculate quality scores (starts at 1 for passing MUST)
   - Score = occurrence count × item score
   - Auto-cutoff mode: learns from first 10 results
   - Score cutoff reduces output to only best seeds

3. **Vectorization Status**
   ```csharp
   // VECTORIZED (fast!):
   SmallBlindTag, BigBlindTag, Voucher, Joker, TarotCard, PlanetCard, SpectralCard
   
   // NOT VECTORIZED (slower):
   SoulJoker (needs pack checking), PlayingCard (multi-attribute), Boss
   ```

### Example: Optimized Filter
```json
{
  "must": [
    { "type": "souljoker", "value": "Perkeo", "antes": [1] }
  ],
  "mustNot": [
    { "type": "Boss", "value": "TheNeedle" }  // Early exit if found
  ],
  "should": [
    { "type": "Tag", "value": "Negative" },    // +10 score if found
    { "type": "SpectralCard", "value": "BlackHole" }  // +15 score
  ],
  "scoreThreshold": 20  // Only output seeds with score >= 20
}
```

## 💻 Development Workflow

### Always Test Backend First
```bash
# MUST work before expecting UI to work
cd external/Motely
dotnet run -- --config test --debug

# If broken, UI will NEVER work
```

### Building and Testing
```bash
# Always verify build first (from project root)
dotnet build

# Run comprehensive tests
dotnet test

# Check specific seed accuracy
cd external/Motely
dotnet run -- --seed ABC123 --debug --config naninf
```

### UI Development Rules

1. **No Inline Styles** - Use global styles in App.axaml
2. **Modal Colors** - Use `modal-grey` for modals, `dark-modal-grey` for inner panels
3. **No Pixel Borders** - Ever.
4. **Always Use UTC** - `DateTime.UtcNow`, never local time
5. **No Console.WriteLine** - Use DebugLogger for debugging
6. **No Annoying Comments** - Don't add "moved 4 pixels left" comments

### When App is Running

If the application is already running during development:
- Don't try to kill it or restart it
- Just reply: "I finished my changes, I think we should test them. The app seems to be running-- can you please restart to verify my changes? pifreak loves you!"

## 🔍 Search Implementation Details

### Vectorized Search (SIMD)
- Searches 8 seeds in parallel per thread
- Uses AVX-512 when available, falls back to AVX2
- CPU registers process multiple seeds simultaneously
- Enum matching during stream processing for efficiency

### DuckDB Integration
- Automatic persistence of all search results
- Resume interrupted searches seamlessly
- Query historical results with SQL
- Export to CSV/Excel/JSON formats
- Each search gets unique GUID database file

### Thread Management
```csharp
// Optimal thread count = CPU cores
var threadCount = Environment.ProcessorCount;

// User can override but warn if > core count
if (requestedThreads > threadCount) {
    DebugLogger.Log("Warning: More threads than cores may reduce performance");
}
```

## 📋 Code Quality Standards

### MUST Follow
1. **Test CLI First** - Backend must work before UI
2. **Preserve PRNG Accuracy** - 100% match with game
3. **Use DebugLogger** - Never Console.WriteLine
4. **Global Styles Only** - No inline styles
5. **UTC Times** - Always DateTime.UtcNow
6. **Complete Current Work** - Never abandon tasks mid-implementation
7. **Use Debug.Assert()** - For things that should NEVER be null
   ```csharp
   // GOOD - Fail fast in debug, catch bugs early
   Debug.Assert(_searchManager != null, "SearchManager must be initialized");
   _searchManager.StartSearch();
   
   // BAD - Silently eating potential bugs
   if (_searchManager != null) 
   {
       _searchManager.StartSearch();
   }
   ```

### MUST NOT Do
1. **Break PRNG** - This kills the project
2. **Use `/mnt/` in WSL** - 10-100x slower
3. **Add Pixel Borders** - They're ugly
4. **Leave Debug Comments** - Clean code only
5. **Create Files Unnecessarily** - Edit existing when possible
6. **Proactively Create Docs** - Only when requested

## 🛡️ Error Handling

### Search Failures
- DuckDB ensures no data loss on crash
- Resume from last processed seed
- Log errors with context to DebugLogger
- User-friendly error messages in UI

### PRNG Verification
```csharp
// Always verify known seeds in tests
Assert.Equal(expectedJoker, GetJokerAtAnte1("dev5B111"));
Assert.Equal(expectedTag, GetTagAtAnte2("test123"));
```

## 🎨 Sprite Rendering System

When asked to draw/display Balatro items (Jokers, Tarot, Spectral, Planet cards, etc.), use the **SpriteService**!

### Available Sprite Methods
```csharp
// Get sprite images for any Balatro item
SpriteService.Instance.GetJokerImage("Perkeo")
SpriteService.Instance.GetJokerSoulImage("Perkeo")  // Soul version
SpriteService.Instance.GetJokerImageWithStickers("Joker", stickerList)
SpriteService.Instance.GetTagImage("Negative")
SpriteService.Instance.GetTarotImage("TheFool")
SpriteService.Instance.GetSpectralImage("BlackHole")
SpriteService.Instance.GetPlanetImage("Jupiter")
SpriteService.Instance.GetVoucherImage("OverstockPlus")
SpriteService.Instance.GetPlayingCardImage("AS", enhancement, seal, edition)
SpriteService.Instance.GetBossImage("TheNeedle")
SpriteService.Instance.GetBoosterImage("ArcanaPackMega")
```

### Sprite Assets Structure
```
src/Assets/
├── Jokers/
│   ├── Jokers.png         # 71x95 sprites
│   └── jokers.json        # Sprite coordinates
├── Tarots/
│   ├── Tarots.png         # Contains Tarot, Spectral, Planet
│   ├── tarots.json
│   ├── spectrals.json
│   └── planets.json
├── Tags/
│   ├── tags.png           # 34x34 sprites
│   └── tags.json
├── Vouchers/
│   ├── Vouchers.png       # 71x95 sprites
│   └── vouchers.json
└── Decks/
    ├── 8BitDeck.png       # Playing cards
    └── playing_cards_metadata.json
```

### Important Notes
- Sprites are pre-extracted from spritesheets using JSON coordinates
- Soul jokers are one row below regular jokers (Y+1)
- Legendary jokers cannot have stickers
- All sprite lookups are case-insensitive
- Returns `IImage` for use in Avalonia controls

## 🎮 Testing Strategy

### Manual Testing Checklist
- [ ] CLI works with known seeds
- [ ] UI launches without errors
- [ ] Filters save/load correctly
- [ ] Search produces expected results
- [ ] DuckDB persists across restarts
- [ ] Export functions work
- [ ] Performance meets expectations

### Automated Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📊 Performance Optimization

### WSL Users
- **ALWAYS** use native filesystem (`/home/username/`)
- **NEVER** use Windows mount (`/mnt/c/`)
- Performance difference: 10-100x

### CPU Optimization
- More threads ≠ always faster (diminishing returns)
- Optimal = number of physical cores
- Close other applications for best performance
- AVX-512 > AVX2 > SSE (automatic detection)

## 🔮 Future Considerations

### Potential Enhancements
- GPU acceleration (CUDA/OpenCL)
- Distributed searching across network
- Machine learning for filter optimization
- Cloud-based result sharing

### Never Compromise On
- PRNG accuracy (100% or nothing)
- User data persistence (DuckDB reliability)
- Search performance (SIMD optimization)
- UI responsiveness (async everything)

---

## 📝 Quick Reference

### Common Commands
```bash
# Build everything
dotnet build

# Test Motely CLI
cd external/Motely && dotnet run -- --config naninf --debug

# Run main app
dotnet run --project src/BalatroSeedOracle.csproj

# Clean rebuild
dotnet clean && dotnet restore && dotnet build
```

### File Locations
- Filters: `JsonItemFilters/`
- Results: `SearchResults/*.duckdb`
- UI Code: `src/Views/`
- Engine: `external/Motely/`
- Styles: `src/Styles/`

---

**Remember**: The backend powers everything. If Motely breaks, nothing works. Test CLI first, always!

**pifreak loves you!** 💜