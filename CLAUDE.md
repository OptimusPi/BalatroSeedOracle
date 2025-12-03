# CLAUDE.md - Balatro Seed Oracle

## Project Overview

Balatro Seed Oracle is a cross-platform desktop application for searching millions of Balatro seeds to find specific game configurations (joker combinations, voucher chains, boss combinations, etc.).

**Tech Stack**: C# 14, .NET 10, Avalonia 11.3.8, CommunityToolkit.Mvvm, DuckDB, Motely (vectorized search engine)

## Build & Run Commands

```bash
# Initial setup (clone with submodules)
git submodule update --init --recursive
dotnet restore src/BalatroSeedOracle.csproj

# Development build
dotnet build src/BalatroSeedOracle.csproj
dotnet run --project ./src/BalatroSeedOracle.csproj

# Release build (optimized, 10-50M seeds/sec)
dotnet build -c Release src/BalatroSeedOracle.csproj
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj

# Motely CLI (standalone search engine)
cd external/Motely
dotnet run --project Motely/Motely.csproj -- --help
dotnet run --project Motely/Motely.csproj -- -f JsonItemFilters/MyFilter.json -t 8 -b 4
```

## Project Structure

```
src/                    # Main Avalonia application
├── Models/             # Data models (FilterResult, SearchConfig, etc.)
├── ViewModels/         # MVVM ViewModels (MainWindowViewModel, SearchModalViewModel)
├── Views/              # AXAML views
├── Windows/            # Window definitions (MainWindow, DataGridResultsWindow)
├── Services/           # Business logic (SearchInstance, AppPaths, etc.)
└── Converters/         # Avalonia value converters

external/
├── Motely/             # Vectorized seed search engine (git submodule)
│   ├── Motely/
│   │   ├── API/        # MotelyApiServer.cs - HTTP API with embedded Balatro-style UI
│   │   ├── Executors/  # JsonSearchExecutor.cs - main search executor
│   │   ├── Filters/    # Filter implementations and JAML loader
│   │   ├── JamlFilters/# JAML schema and examples
│   │   └── TUI/        # Terminal UI (SearchWindow.cs)
│   └── JsonItemFilters/# User filter JSON files
└── Genie/              # AI-powered filter generation (separate project)
```

## Key Patterns

### MVVM with CommunityToolkit.Mvvm
- ViewModels use `[ObservableProperty]` for bindable properties
- Commands use `[RelayCommand]` attribute
- Services accessed via `App.GetService<T>()`

### Model vs ViewModel Separation
- `FilterResult` (Model) = raw data from filter files
- `FilterResultViewModel` (ViewModel) = UI-specific wrapper with selection state

### Logging
- Use `App.Log(message)` for debug logging (writes to `AppPaths.LogFilePath`)
- **IMPORTANT**: Only use `App.LogError(message)` for actual errors, not debug messages

## Critical Files

| File | Purpose |
|------|---------|
| `src/ViewModels/SearchModalViewModel.cs` | Search configuration, filter selection, search lifecycle |
| `src/Services/SearchInstance.cs` | Manages individual search execution, database operations |
| `src/Services/AppPaths.cs` | All file/directory paths (databases, filters, logs, exports) |
| `external/Motely/Motely/API/MotelyApiServer.cs` | HTTP API server with embedded web UI |
| `external/Motely/Motely/Executors/JsonSearchExecutor.cs` | Core search executor with `JsonSearchParams` |

## Important Gotchas

### Motely Batch Size
- `BatchSize` = **character count** of seeds (1-7), NOT a batch count!
- Valid values: 1, 2, 3, 4, 5, 6, 7
- Example: BatchSize=4 searches all 4-character seeds (AAAA to ZZZZ)

### Motely Thread Count
- Use `Environment.ProcessorCount` or user-configured value
- Don't hardcode arbitrary values

### SeedList Parameter
- `JsonSearchParams.SeedList` = direct list of seeds to search (added for API)
- `JsonSearchParams.Wordlist` = file path to wordlist file
- `SeedList` takes priority over `Wordlist` in `LoadSeeds()`

### DuckDB Buffering
- Results batch in memory before flushing to DB
- Call `SearchInstance.ForceFlush()` when pausing/stopping
- `CHECKPOINT` command ensures WAL is flushed

### Filter Cache
- `FilterCache.Instance.Invalidate()` when filter files change
- Use after copying/editing filter files

### Search ID Uniqueness
- Search ID = `{filterId}_{deck}_{stake}` (same filter with different deck/stake = different search)
- Use `GetSearchId()` and `GetDatabasePath()` helpers in SearchModalViewModel

### CurrentFilterPath Lifecycle
- Must restore `CurrentFilterPath` when reconnecting to existing search
- Set it in `ConnectToExistingSearch()` before displaying results

## Motely API Server

### Routes
- `GET /` - Embedded Balatro-style web UI
- `POST /search` - Start search (body: `{ filterJaml, seedCount }`)
- `GET /search?id=xxx` - Poll search status/results
- `POST /analyze` - Analyze single seed (body: `{ seed, deck, stake }`)
- `POST /genie` - Generate JAML from natural language (body: `{ prompt }`)
- `GET /filters` - List available filter files

### Fertilizer Pile Design
- **CRITICAL**: The "fertilizer pile" stores ONLY seeds (strings), NOT results!
- `HashSet<string> _fertilizerPile` - accumulates seeds across searches
- When searching: pass pile to Motely via `SeedList` parameter
- Motely re-searches the pile with each new filter (it's fast!)
- NO results caching - always get fresh results per filter

## Filter JSON Structure

```json
{
  "name": "Filter Name",
  "deck": "Red",
  "stake": "White",
  "must": [
    { "type": "Joker", "value": "Blueprint", "antes": [1,2,3] }
  ],
  "should": [
    { "type": "Voucher", "value": "Observatory", "antes": [2,3], "score": 100 }
  ],
  "mustNot": [
    { "type": "Boss", "value": "TheNeedle", "antes": [1,2,3,4,5,6,7,8] }
  ]
}
```

## JAML Schema Reference

### Top-Level Properties
```yaml
name: "Filter Name"
description: "What it does"
author: "Creator"
dateCreated: "2024-01-01"
deck: Red          # Red, Blue, Yellow, Green, Black, Magic, Nebula, Ghost, etc.
stake: White       # White, Red, Green, Black, Blue, Purple, Orange, Gold
defaults:
  antes: [1, 2, 3, 4, 5, 6, 7, 8]
  packSlots: [0, 1, 2, 3, 4, 5]
  shopSlots: [0, 1, 2, 3, 4, 5]
  score: 1
must: []           # REQUIRED items (all must match)
should: []         # OPTIONAL items (adds to score)
mustNot: []        # BANNED items (seed fails if found)
```

### Clause Properties
```yaml
joker: Blueprint         # Type-as-key shortcut syntax
antes: [1, 2, 3]         # Which antes to check (1-8)
score: 100               # Points for 'should' clauses
label: "Early Blueprint" # Custom label for results
edition: Negative        # Foil, Holo, Polychrome, Negative
seal: Red                # Red, Blue, Gold, Purple
enhancement: Steel       # Bonus, Mult, Wild, Glass, Steel, Stone, Lucky, Gold
rank: A                  # 2-10, J, Q, K, A
suit: Hearts             # Hearts, Diamonds, Clubs, Spades
sources: [shop, pack]    # Where item can be found
shopSlots: [0, 1, 2]     # Shop slot positions (0-5)
packSlots: [0, 1, 2, 3]  # Pack slot positions (0-5)
and: []                  # Nested AND conditions
or: []                   # Nested OR conditions
```

### Valid Types
- `joker` - Regular jokers
- `soulJoker` - Legendary jokers (Canio, Triboulet, Yorick, Chicot, Perkeo)
- `voucher` - Vouchers
- `tarot` / `tarotCard` - Tarot cards
- `planet` / `planetCard` - Planet cards
- `spectral` / `spectralCard` - Spectral cards
- `playingCard` / `standardCard` - Playing cards
- `boss` - Boss blinds
- `tag` - Either smallBlindTag OR bigBlindTag
- `smallBlindTag` - Tags from small blind
- `bigBlindTag` - Tags from big blind
- `and` - All nested clauses must match
- `or` - Any nested clause can match

### Valid Item Names

**Legendary Jokers (soulJoker)**: Canio, Triboulet, Yorick, Chicot, Perkeo

**Rare Jokers**: DNA, Vagabond, Baron, Obelisk, BaseballCard, AncientJoker, Campfire, Blueprint, WeeJoker, HitTheRoad, TheDuo, TheTrio, TheFamily, TheOrder, TheTribe, Stuntman, InvisibleJoker, Brainstorm, DriversLicense, BurntJoker

**Uncommon Jokers**: JokerStencil, FourFingers, Mime, CeremonialDagger, MarbleJoker, LoyaltyCard, Dusk, Fibonacci, SteelJoker, Hack, Pareidolia, SpaceJoker, Burglar, Blackboard, SixthSense, Constellation, Hiker, CardSharp, Madness, Seance, Vampire, Shortcut, Hologram, Cloud9, Rocket, MidasMask, Luchador, GiftCard, TurtleBean, Erosion, ToTheMoon, StoneJoker, LuckyCat, Bull, DietCola, TradingCard, FlashCard, SpareTrousers, Ramen, Seltzer, Castle, MrBones, Acrobat, SockAndBuskin, Troubadour, Certificate, SmearedJoker, Throwback, RoughGem, Bloodstone, Arrowhead, OnyxAgate, GlassJoker, Showman, FlowerPot, MerryAndy, OopsAll6s, TheIdol, SeeingDouble, Matador, Satellite, Cartomancer, Astronomer, Bootstraps

**Common Jokers**: Joker, GreedyJoker, LustyJoker, WrathfulJoker, GluttonousJoker, JollyJoker, ZanyJoker, MadJoker, CrazyJoker, DrollJoker, SlyJoker, WilyJoker, CleverJoker, DeviousJoker, CraftyJoker, HalfJoker, CreditCard, Banner, MysticSummit, EightBall, Misprint, RaisedFist, ChaostheClown, ScaryFace, AbstractJoker, DelayedGratification, GrosMichel, EvenSteven, OddTodd, Scholar, BusinessCard, Supernova, RideTheBus, Egg, Runner, IceCream, Splash, BlueJoker, FacelessJoker, GreenJoker, Superposition, ToDoList, Cavendish, RedCard, SquareJoker, RiffRaff, Photograph, ReservedParking, MailInRebate, Hallucination, FortuneTeller, Juggler, Drunkard, GoldenJoker, Popcorn, WalkieTalkie, SmileyFace, GoldenTicket, Swashbuckler, HangingChad, ShootTheMoon

**Vouchers**: Overstock, OverstockPlus, ClearanceSale, Liquidation, Hone, GlowUp, RerollSurplus, RerollGlut, CrystalBall, OmenGlobe, Telescope, Observatory, Grabber, NachoTong, Wasteful, Recyclomancy, TarotMerchant, TarotTycoon, PlanetMerchant, PlanetTycoon, SeedMoney, MoneyTree, Blank, Antimatter, MagicTrick, Illusion, Hieroglyph, Petroglyph, DirectorsCut, Retcon, PaintBrush, Palette

**Tags**: UncommonTag, RareTag, NegativeTag, FoilTag, HolographicTag, PolychromeTag, InvestmentTag, VoucherTag, BossTag, StandardTag, CharmTag, MeteorTag, BuffoonTag, HandyTag, GarbageTag, EtherealTag, CouponTag, DoubleTag, JuggleTag, D6Tag, TopupTag, SpeedTag, OrbitalTag, EconomyTag

**Tarot Cards**: TheFool, TheMagician, TheHighPriestess, TheEmpress, TheEmperor, TheHierophant, TheLovers, TheChariot, Justice, TheHermit, TheWheelOfFortune, Strength, TheHangedMan, Death, Temperance, TheDevil, TheTower, TheStar, TheMoon, TheSun, Judgement, TheWorld

**Spectral Cards**: Familiar, Grim, Incantation, Talisman, Aura, Wraith, Sigil, Ouija, Ectoplasm, Immolate, Ankh, DejaVu, Hex, Trance, Medium, Cryptid, Soul, BlackHole

**Planet Cards**: Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune, Pluto, PlanetX, Ceres, Eris

**Boss Blinds**: AmberAcorn, CeruleanBell, CrimsonHeart, VerdantLeaf, VioletVessel, TheArm, TheClub, TheEye, TheFish, TheFlint, TheGoad, TheHead, TheHook, TheHouse, TheManacle, TheMark, TheMouth, TheNeedle, TheOx, ThePillar, ThePlant, ThePsychic, TheSerpent, TheTooth, TheWall, TheWater, TheWheel, TheWindow

**Decks**: Red, Blue, Yellow, Green, Black, Magic, Nebula, Ghost, Abandoned, Checkered, Zodiac, Painted, Anaglyph, Plasma, Erratic, Challenge

**Stakes**: White, Red, Green, Black, Blue, Purple, Orange, Gold

**Editions**: Foil, Holo, Polychrome, Negative

**Seals**: Red, Blue, Gold, Purple

**Enhancements**: Bonus, Mult, Wild, Glass, Steel, Stone, Lucky, Gold

## Example JAML Filters

### Simple "must have" filter
```yaml
deck: Red
stake: White
must:
  - soulJoker: Perkeo
    edition: Negative
    antes: [1, 2, 3]
  - voucher: Telescope
    antes: [1, 2]
```

### Filter with scoring
```yaml
deck: Red
stake: White
must:
  - voucher: Observatory
    antes: [2, 3]
should:
  - joker: Blueprint
    antes: [1, 2, 3]
    score: 100
  - tag: NegativeTag
    antes: [1, 2]
    score: 50
```

### Filter with nested OR
```yaml
deck: Red
stake: White
must:
  - or:
    - soulJoker: Perkeo
      antes: [1, 2]
    - soulJoker: Yorick
      antes: [1, 2]
```

### Filter with specific slots
```yaml
deck: Red
stake: White
must:
  - joker: Blueprint
    antes: [1]
    shopSlots: [0, 1]
  - tarot: TheWorld
    antes: [1, 2]
    packSlots: [0, 1, 2]
```

## Development Notes

- Motely is a git submodule - always `git submodule update --init --recursive` after clone
- BSO user filters go in `AppPaths.FiltersDir` (JAML) and `JsonItemFilters/` (JSON legacy)
- Motely example filters are in `external/Motely/JsonItemFilters/`
- DuckDB databases stored in `AppPaths.SearchDatabaseDir`
- Log file at `AppPaths.LogFilePath`
