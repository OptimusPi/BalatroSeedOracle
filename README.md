# Balatro Seed Oracle

Search millions of Balatro seeds to find the perfect runs for your strategies.

## Docs

- See `docs/INDEX.md` for the current documentation index and cleanup plan.

## What is this?

Balatro Seed Oracle helps you find specific Balatro seeds based on detailed criteria:

- **Find exact joker combinations** - Blueprint + Brainstorm in specific antes
- **Locate rare voucher chains** - Telescope → Observatory progressions  
- **Hunt for soul jokers** - Negative Perkeo with specific pack requirements
- **Search for perfect setups** - Specific bosses, tags, spectral cards, etc.

Built for the Balatro community to discover optimal seeds for challenge runs, high scores, and specific strategies.

## Installation

### Requirements

- Windows, Linux, or macOS*
- Intel, AMD, or Apple Silicon CPU**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [git](https://git-scm.com/downloads)]

### NOTE on other compatibilities

- Not sure if other OS will work such as BSD, but it probably will.
- I think this might work on other CPUs like ARM, but I have not tested it.
- I think I might need some changes to Motely's Vector helpers to make it work, but it's possible the fallbacks (if AVX512 and/or AVX2 not supported) tacodiva added already will be compatible.
- Mobile would be really cool in the future.
- Web page version would be really cool in the future.

## Clone this repository (or download zip, or use Github Desktop)

Either click the green button in the upper-right of this page and download in your favorite way...or:

```sh
git clone https://github.com/OptimusPi/BalatroSeedOracle.git
cd BalatroSeedOracle
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj
```

## Initialize the git Submodule

This repo uses git submodules to include the Motely search engine.
The git submodule is my fork of tacodiva/Motely that includes the MotelyJson support!
You need to initialize and update the submodule after cloning:

```bash
git submodule update --init --recursive
```

## Run the Balatro Seed Oracle GUI Application

If you are making code changes on your own fork for example, you can specify Debug build configuration.
NOTE: This searches a *lot* slower than release version!

```sh
dotnet run -c Debug --project ./src/BalatroSeedOracle.csproj
```

To run the optimized release build:

```sh
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj
```

## Using the Command Line Interface

For advanced users who prefer CLI usage, the bundled **MotelyJAML** search engine supports command-line operation with JSON/JAML filters, native C# filters, and seed analysis.

**See the [MotelyJAML README](./external/Motely/README.md) for complete CLI documentation.**

Quick CLI example:
```bash
cd ./external/Motely
dotnet run -- --help
```

## Creating Filters

### Visual Editor
Use the in-app visual filter designer to create complex filters by dragging and dropping criteria.

### JSON Configuration
Create custom filters manually:

```json
{
  "name": "Negative Perkeo Hunt",
  "description": "Find seeds with Negative Perkeo and Telescope",
  "mode": "sum", // optional: "sum" (default) or "max"
  "deck": "Red", 
  "stake": "White",
  "must": [
    {
      "type": "Voucher",
      "value": "Telescope", 
      "antes": [1, 2]
    },
    {
      "type": "SoulJoker",
      "value": "Perkeo",
      "edition": "Negative",
      "antes": [1, 2, 3]
    }
  ],
  "should": [
    {
      "type": "Joker", 
      "value": "Blueprint",
      "antes": [2, 3, 4],
      "score": 10
    }
  ]
}
```

### Filter Types

- **Vouchers**: `Telescope`, `Observatory`, `Hieroglyph`, etc.
- **Soul Jokers**: `Perkeo`, `Triboulet`, `Canio`, `Chicot`, etc.
- **Regular Jokers**: `Blueprint`, `Brainstorm`, `Showman`, etc.
- **Tarot Cards**: `TheFool`, `TheWorld`, `Death`, etc.
- **Spectral Cards**: `Ankh`, `Soul`, `Wraith`, etc.
- **Planet Cards**: `Jupiter`, `Mars`, `Venus`, etc.  
- **Playing Cards**: Specific ranks/suits with seals/editions
- **Boss Blinds**: `TheGoad`, `CeruleanBell`, `TheOx`, etc.
- **Tags**: `NegativeTag`, `SpeedTag`, `RareTag`, etc.

### Advanced Features

- **Pack slot targeting**: Find items in specific booster pack positions
- **Shop slot filtering**: Target specific shop positions
- **Edition requirements**: Negative, Polychrome, Foil editions
- **Multiple clause logic**: Complex AND/OR requirements

## Performance

- **Multi-threaded search** - Utilizes all CPU cores
- **SIMD vectorization** - Hardware-accelerated filtering
- **Smart caching** - Optimized for repeated searches
- **Batch processing** - Configurable search chunk sizes

Typical speeds: 10-50 million seeds per second depending on filter complexity.

## Scoring Modes

- `mode`: Controls how scores from `should` clauses are aggregated.
- Supported values: `sum` (default), `max`, `max_count`, `maxcount`.
- Behavior:
  - `sum`: Adds `count * score` for each `should` clause.
  - `max`: Uses the maximum raw occurrence `count` across all `should` clauses (per-clause `score` is ignored).
- Notes:
  - `minScore` in the Search modal is compared against the aggregated value from the selected mode.
  - Negative `score` values are allowed; they only affect `sum` mode.

Example using max aggregation:

```json
{
  "name": "Tarot or Planet Rush",
  "mode": "max",
  "should": [
    { "type": "TarotCard", "value": "TheFool", "antes": [1,2,3], "score": 5 },
    { "type": "PlanetCard", "value": "Jupiter", "antes": [1,2,3], "score": 50 }
  ]
}
```

In this example, even though `PlanetCard` has higher `score`, `mode: max` ignores `score` and takes the higher raw occurrence count between the two clauses.

## File Structure

- `src/` - Main application code
- `external/Motely/` - High-performance search engine
- `JsonFilters/` & `JamlFilters/` - Pre-made filter configurations
- `SearchResults/` - Database files with search results

## Contributing

This is a community project. Contributions welcome:

- **New filter configurations** - Share effective seed hunt strategies
- **Performance improvements** - Optimization suggestions
- **Bug reports** - Issues with specific filters or seeds
- **Feature requests** - Additional filtering capabilities

## Technical Details

Built on:

- **.NET 10 / C# 14** - Modern C# with high performance features
- **Avalonia UI** - Cross-platform desktop framework  
- **DuckDB** - Fast analytical database for results
- **Motely** - Custom vectorized Balatro seed analysis engine

The search engine uses advanced vectorized operations to achieve high throughput when analyzing millions of seed combinations.

## Support

- **Discord**: Balatro community server (#tools channel)
- **Issues**: Open GitHub issues for bugs/requests
- **Wiki**: Check project wiki for advanced usage

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

---

*Built by pifreak for the Balatro community*
