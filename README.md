# 🃏 Balatro Seed Oracle

**The Ultimate Balatro Seed Search Tool** - Find perfect seeds with lightning-fast CPU SIMD technology and powerful JSON filters!

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![Avalonia UI](https://img.shields.io/badge/Avalonia-11.0-purple)
![DuckDB](https://img.shields.io/badge/DuckDB-Powered-yellow)
![SIMD](https://img.shields.io/badge/SIMD-AVX512-red)

## ⚡ What Makes This Special?

- **Blazing Fast**: Powered by Motely's CPU SIMD engine - searches 8 seeds simultaneously per thread using AVX-512 instructions
- **100% Accurate PRNG**: Exact Balatro game logic reproduction - if we say it's there, it's there!
- **Never Lose Results**: DuckDB persistence means your searches survive crashes, power outages, and ragequits
- **Visual Filter Builder**: Drag-and-drop interface for creating complex JSON filters without writing code
- **Professional UI**: Beautiful AvaloniaUI interface that makes seed searching actually fun

## 🚀 Quick Start

### Windows Users
```powershell
# Clone the repo
git clone https://github.com/yourusername/BalatroSeedOracle.git
cd BalatroSeedOracle

# Initialize submodules (gets Motely engine)
git submodule update --init --recursive

# Build and run
dotnet build
dotnet run --project src/BalatroSeedOracle.csproj
```

### WSL/Linux Users  
**IMPORTANT**: Run from native WSL filesystem (`/home/username/`), NOT from `/mnt/` drives for 10-100x better performance!

```bash
# Clone to WSL native filesystem
cd ~
git clone https://github.com/yourusername/BalatroSeedOracle.git
cd BalatroSeedOracle

# Initialize submodules
git submodule update --init --recursive

# Build and run
dotnet build
dotnet run --project src/BalatroSeedOracle.csproj
```

## 🛠️ Architecture

### Frontend: Balatro Seed Oracle (AvaloniaUI)
- Cross-platform desktop application
- Visual filter builder with drag-and-drop
- Real-time search progress visualization
- DuckDB integration for persistent storage
- Export to CSV/Excel for analysis

### Backend: Motely Engine (C# SIMD)
- Fork of tacodiva's legendary Motely engine
- CPU-based SIMD optimization (AVX-512/AVX2)
- Searches 8 seeds in parallel per thread
- 100% accurate Balatro PRNG simulation
- JSON-powered filter system

### Testing the Backend First
**CRITICAL**: Always test Motely CLI before expecting UI to work!

```bash
cd external/Motely
dotnet run -- --config naninf --seed dev5B111 --debug
```

If the CLI doesn't work, the UI won't work. The PRNG accuracy is paramount!

## 📋 Features

### 🎯 JSON Filter System
Create complex search filters with the visual builder or write JSON directly:

```json
{
  "name": "Perkeo Black Hole Dream",
  "must": [
    {
      "type": "souljoker",
      "value": "Perkeo",
      "antes": [1],
      "sources": { "packSlots": [1] }
    },
    {
      "type": "SpectralCard",
      "value": "BlackHole",
      "antes": [1],
      "sources": { "packSlots": [1] }
    }
  ],
  "deck": "Ghost",
  "stake": "White"
}
```

### 💾 DuckDB Persistence
- Never lose search results
- Resume interrupted searches
- Query historical results
- Export to any format

### 🎨 Visual Filter Builder
- Drag items from library
- Configure antes and sources
- Real-time filter validation
- Save/load filter presets

## 🔧 Development

### Prerequisites
- .NET 9 SDK
- Git
- 64-bit CPU with AVX2 support (AVX-512 preferred)

### Project Structure
```
BalatroSeedOracle/
├── src/                    # AvaloniaUI frontend application
│   ├── Views/             # UI views and modals
│   ├── Services/          # Search manager, DuckDB integration
│   └── Models/            # Data models and DTOs
├── external/
│   ├── Motely/            # SIMD search engine (submodule)
│   │   ├── filters/       # Filter implementations
│   │   ├── enums/         # Balatro game enums
│   │   └── JsonItemFilters/ # JSON filter examples
│   └── Balatro/           # Original game Lua files (reference)
├── JsonItemFilters/       # User filter library
└── SearchResults/         # DuckDB storage files
```

### Building from Source
```bash
# Full rebuild
dotnet clean
dotnet restore
dotnet build -c Release

# Run tests (if you break PRNG accuracy, we cry)
cd external/Motely
dotnet run -- --config test --debug
```

## 🎮 Usage Examples

### Finding Legendary Jokers Early
1. Open Balatro Seed Oracle
2. Click "Analyze" 
3. Drag desired jokers to ante 1-2 slots
4. Name your filter
5. Click "Let Jimbo COOK!"
6. Watch as thousands of seeds are analyzed per second

### Exporting Results
1. Complete a search
2. Click "Export Results"
3. Choose format (CSV/Excel/JSON)
4. Open in your favorite spreadsheet app

### CLI Power User Mode
```bash
cd external/Motely
# Search with custom filter
dotnet run -- --config legendary-map-2 --threads 16

# Debug specific seed
dotnet run -- --seed ABC123 --debug --config naninf
```

## ⚠️ Important Notes

### PRNG Accuracy is Sacred
The Motely engine must maintain 100.0000000000% accuracy with Balatro's PRNG. Any changes to core PRNG logic require:
1. Extensive testing against known seeds
2. Verification with game behavior
3. Extreme caution and rethinking

### Performance Tips
- WSL users: ALWAYS use native filesystem (`/home/`), never `/mnt/`
- More threads = more speed (up to your CPU core count)
- AVX-512 CPUs get best performance
- Close other applications for maximum speed

## 📜 License

MIT License - Use it, fork it, love it!

## 🙏 Credits

- **OptimusPi** - Commissioned Motely development, made this possible
- **tacodiva** - Original Motely engine creator
- **LocalThunk** - For making Balatro, the best roguelike deckbuilder ever
- **pifreak** - For the vision and UI magic

---

**pifreak loves you!** 💜

Remember: If the backend (Motely) is broken, the frontend will never work. Test CLI first, always!