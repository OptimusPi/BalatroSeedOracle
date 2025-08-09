# BalatroSeedOracle

Find the perfect Balatro seeds with powerful filtering and search capabilities.

[TODO: SCREENSHOT HERE - Main application window]

## Quick Start

```powershell
./start.ps1
```

That's it! The script handles everything.

## Manual Setup

1. **Prerequisites**
   - [Git](https://git-scm.com/downloads)
   - [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

2. **Clone & Build**
   ```bash
   git clone https://github.com/yourusername/BalatroSeedOracle.git
   cd BalatroSeedOracle
   git submodule update --init --recursive
   dotnet restore
   dotnet build
   ```

3. **Run**
   ```bash
   dotnet run --project src/Oracle.csproj
   ```

## Features

- **Visual Filter Builder** - Drag & drop items to create complex filters
- **Multi-threaded Search** - Fast seed searching with configurable threads
- **Smart Scoring** - Automatically scores seeds based on filter criteria  
- **Export Results** - Export to Excel/CSV for further analysis
- **Save/Load Filters** - Share your filters with the community

## Usage

### Creating a Filter

1. Click "Analyze" to open the search modal
2. Drag items from the library to the drop zones
3. Configure ante ranges and sources for each item
4. Save your filter with a memorable name

[TODO: SCREENSHOT HERE - Filter builder interface]

### Searching Seeds

1. Select your filter
2. Choose deck and stake
3. Click "Let Jimbo COOK!" to start searching
4. View results with detailed scoring breakdowns

[TODO: SCREENSHOT HERE - Search results]

### Keyboard Shortcuts

- `Ctrl+C` - Copy selected seed
- `Esc` - Close modal windows
- `Tab` - Navigate between fields

## Filter Examples

Filters are stored as JSON in the `JsonItemFilters` directory:
- `ExplosiveBoi.json` - Focus on explosive jokers
- `PirateEgg.json` - Egg synergy builds  
- `BurgleMeBaby.json` - Burglar-focused runs

## Dependencies

- [Motely](https://github.com/piefox/Motely) - Core Balatro simulation engine (fork of tacodiva's work)
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [DuckDB](https://duckdb.org/) - Analytics database for results

## License

MIT License - See LICENSE file for details

`pifreak loves you!`
