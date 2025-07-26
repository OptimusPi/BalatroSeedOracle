# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BalatroSeedOracle is a Balatro seed searching tool with a beautiful, Balatro-inspired Avalonia UI. It integrates Motely (a fork from tacodiva, included as a git submodule) which provides high-performance SIMD-based seed searching that processes 8 seeds simultaneously per thread.

### Key Features
- **Visual Filter Builder**: Drag-and-drop Balatro game items to create search filters
- **Ouija JSON Filters**: Enhanced JSON-based filter system for easy seed searching
- **Beautiful UI**: Balatro-themed interface built with Avalonia
- **High Performance**: Leverages Motely's vectorized search capabilities

### Current TODOs
1. **Maximize button for search widget**: Transform from standard modal to full search modal
2. **Sortable data table**: Implement a nice sortable table for search results
3. **DuckDB Integration**: Capture CSV results from Motely/OuijaJS and store in a DuckDB database (backed by .duckdb file)
4. **Visual Builder - Complete JSON Support**: Add missing config options:
   - **Ante selector**: Allow users to set the ante level
   - **Sources**: Add UI for tags, booster packs, and shop item queue
   - **Edition selector**: Add tab switcher for 5 editions:
     - Normal
     - Foil
     - Holographic
     - Polychrome
     - Negative

## Common Development Commands

### Build
```bash
dotnet build
```

### Run the UI application
```bash
dotnet run --project src/Oracle.csproj
```

### Run Motely CLI (seed searcher)
```bash
dotnet run --project external/Motely/Motely.csproj
```

### Clean build outputs
```bash
dotnet clean
```

### Restore dependencies
```bash
dotnet restore
```

## Architecture Overview

### Core Components

1. **Oracle UI Application** (`src/`)
   - Avalonia-based desktop application
   - Main entry: `Program.cs` → `MainWindow.axaml`
   - Uses MVVM pattern with services in `Services/`
   - Custom Balatro-themed UI components in `Controls/` and `Components/`

2. **Motely Search Engine** (`external/Motely/`)
   - High-performance seed searching using SIMD/AVX-512
   - Vector operations process 8 seeds simultaneously
   - Search contexts handle different game aspects (Jokers, Packs, Planets, etc.)
   - Supports custom filters via Ouija JSON format

### Key Services Integration

- **MotelySearchService**: Bridges UI to Motely engine for seed searches
- **SpriteService**: Manages game asset sprites (Jokers, Tags, Tarots, etc.)
- **FavoritesService**: Handles user's favorite seeds
- **DaylatroSeeds**: Manages daily challenge seeds

### Search System Architecture

The search system uses a context-based approach:
- `MotelySingleSearchContext.*` files handle individual seed searches
- `MotelyVectorSearchContext.*` files handle vectorized batch searches
- Filters are defined in `filters/` with JSON configuration support

### Asset Organization

Game assets in `src/Assets/`:
- Sprite sheets for Jokers, Tags, Tarots, Vouchers
- JSON metadata files for display names and properties
- UI assets for Balatro-themed interface

## Important Technical Details

- **Target Framework**: .NET 9.0
- **UI Framework**: Avalonia 11.3.2
- **Performance**: Uses unsafe code and SIMD instructions
- **Naming Conventions**: 
  - Oracle project uses underscores for joker names (e.g., `Wee_Joker`)
  - Motely internally may use different conventions
  - Be aware of naming differences when working across projects