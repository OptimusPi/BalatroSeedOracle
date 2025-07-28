# Buffoon Pack Search Guide

## Overview
You can now search for jokers that appear specifically in buffoon packs! This guide explains how to configure your search filters to find jokers in different sources.

## How to Specify Sources in JSON Config

### Basic Structure
In your `.ouija.json` filter files, you can control where jokers are searched for using these properties in the Needs/Wants array:

```json
{
  "Type": "Joker",
  "Value": "JollyJoker",           // Use PascalCase for joker names
  "SearchAntes": [1, 2, 3],        // Which antes to search
  "Score": 1,                      // Points for this joker
  "IncludeShopStream": false,      // Search in shop? (default: true)
  "IncludeBoosterPacks": true,      // Search in buffoon packs? (default: false)
  "IncludeSkipTags": false          // Search in skip tags? (default: false)
}
```

### Examples

#### 1. Find Any Joker in Buffoon Packs Only
```json
{
  "Deck": "RedDeck",
  "Stake": "WhiteStake",
  "MaxSearchAnte": 2,
  "Needs": [
    {
      "Type": "Joker",
      "Value": "any",
      "SearchAntes": [1, 2],
      "Score": 1,
      "IncludeShopStream": false,
      "IncludeBoosterPacks": true,
      "IncludeSkipTags": false
    }
  ]
}
```

#### 2. Find Specific Joker in Buffoon Packs Only
```json
{
  "Deck": "RedDeck",
  "Stake": "WhiteStake",
  "MaxSearchAnte": 3,
  "Needs": [
    {
      "Type": "Joker",
      "Value": "JollyJoker",
      "SearchAntes": [1, 2, 3],
      "Score": 1,
      "IncludeShopStream": false,
      "IncludeBoosterPacks": true,
      "IncludeSkipTags": false
    }
  ]
}
```

#### 3. Find Foil Joker in Buffoon Packs
```json
{
  "Deck": "RedDeck",
  "Stake": "WhiteStake",
  "MaxSearchAnte": 3,
  "Needs": [
    {
      "Type": "Joker",
      "Value": "JollyJoker",
      "Edition": "Foil",
      "SearchAntes": [1, 2, 3],
      "Score": 1,
      "IncludeShopStream": false,
      "IncludeBoosterPacks": true,
      "IncludeSkipTags": false
    }
  ]
}
```

#### 4. Find Joker in EITHER Shop OR Buffoon Packs
```json
{
  "Deck": "RedDeck",
  "Stake": "WhiteStake",
  "MaxSearchAnte": 2,
  "Needs": [
    {
      "Type": "Joker",
      "Value": "JollyJoker",
      "SearchAntes": [1, 2],
      "Score": 1,
      "IncludeShopStream": true,       // Check shop
      "IncludeBoosterPacks": true,      // AND buffoon packs
      "IncludeSkipTags": false
    }
  ]
}
```

## Important Notes

1. **Joker Names**: Use PascalCase without spaces for joker names:
   - ✅ `"JollyJoker"` (correct)
   - ❌ `"Jolly Joker"` (incorrect)
   - ❌ `"jolly joker"` (incorrect)
   - Special: Use `"any"` to find any joker

2. **Default Behavior**: If you don't specify these properties, the default is:
   - `IncludeShopStream`: true (searches shop)
   - `IncludeBoosterPacks`: true (DOES search buffoon packs by default!)
   - `IncludeSkipTags`: true (DOES search skip tags by default!)

3. **Buffoon Pack Availability**: 
   - Ante 1 gets the guaranteed first buffoon pack
   - Additional buffoon packs can appear randomly in the pack selection

4. **Pack Sizes**: Buffoon packs can contain 2 or 3 jokers depending on the pack size roll

5. **Editions**: You can search for specific editions (Foil, Holographic, Polychrome, Negative) in buffoon packs

6. **Performance**: Searching buffoon packs requires individual seed processing (non-vectorized), so it may be slower than shop-only searches

## Testing Your Filters

1. Save your filter as a `.ouija.json` file in the `ouija_configs` folder
2. Load it in the Oracle UI or run it with Motely CLI:
   ```bash
   dotnet run --project external/Motely/Motely.csproj -- --config ouija_configs/your_filter.ouija.json
   ```

## Troubleshooting

- If you're not finding results, make sure `IncludeBoosterPacks` is set to `true`
- Check that `IncludeShopStream` is set to `false` if you ONLY want buffoon pack results
- Remember that not all antes will have buffoon packs - they're random after the first one