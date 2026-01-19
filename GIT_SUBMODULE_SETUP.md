# Git Submodule Setup Guide

## ⚠️ CRITICAL: Motely Submodule Required

**Balatro Seed Oracle** depends on the **Motely** git submodule for:
- DuckDB schema definitions (`Motely.DuckDB.DuckDBSchema`)
- Search engine implementation
- Filter execution logic
- API services

## Quick Setup

### First Time Setup
```bash
# Initialize and update the submodule
git submodule update --init --recursive
```

### If Submodule Has Uncommitted Changes
The submodule may have uncommitted changes from other agents. This is **OK for building** - the files exist and can be compiled.

If you need to reset the submodule to the committed state:
```bash
cd external/Motely
git stash  # Save changes
# OR
git commit -am "Save work"  # Commit changes
cd ../..
git submodule update --init --recursive
```

## Submodule Details

- **Path**: `external/Motely/`
- **Fork**: `MotelyJAML` on GitHub (OptimusPi/MotelyJAML)
- **Commit**: Checked out at commit referenced in parent repo
- **Status**: May have uncommitted changes (this is normal during development)

## Build Verification

After initializing the submodule, verify it works:
```bash
# Build BSO library
dotnet build src/BalatroSeedOracle/BalatroSeedOracle.csproj -c Release

# Build full solution (may fail if Motely projects have issues, but BSO should work)
dotnet build BalatroSeedOracle.sln -c Release
```

## Integration Points

BSO uses Motely in these ways:
1. **FertilizerService**: Uses `Motely.DuckDB.DuckDBSchema.FertilizerTableSchema()`
2. **SearchStateManager**: Uses `Motely.DuckDB.DuckDBSchema.SearchStateTableSchema()` and extends it
3. **SearchInstance**: Uses Motely's search logic and column name helpers
4. **Filter Services**: Uses `Motely.Filters.MotelyJsonConfig` types

## Troubleshooting

### Error: "The project file ...Motely.csproj was not found"
- **Solution**: Run `git submodule update --init --recursive`

### Error: "Unable to checkout in submodule"
- **Cause**: Uncommitted changes in submodule
- **Solution**: Either commit or stash changes in `external/Motely/`

### Error: "The type or namespace name 'Motely' could not be found"
- **Cause**: Submodule not initialized OR build hasn't restored packages
- **Solution**: 
  1. `git submodule update --init --recursive`
  2. `dotnet restore`
  3. `dotnet build`

## For AI Agents

**ALWAYS** check if the submodule is initialized before attempting to build:
```bash
Test-Path "external\Motely\Motely\Motely.csproj"
```

If this returns `False`, initialize the submodule first!
