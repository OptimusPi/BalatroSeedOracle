# Filter Configuration Guide

This guide covers how to create and configure filters for Balatro Seed Oracle.

## Visual Editor

Use the in-app visual filter designer to create complex filters by dragging and dropping criteria.

## JSON Configuration

Create custom filters manually using JSON format.

### Basic Example

```json
{
  "name": "Negative Perkeo Hunt",
  "description": "Find seeds with Negative Perkeo and Telescope",
  "mode": "sum",
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

## Filter Types

- **Vouchers**: `Telescope`, `Observatory`, `Hieroglyph`, etc.
- **Soul Jokers**: `Perkeo`, `Triboulet`, `Canio`, `Chicot`, etc.
- **Regular Jokers**: `Blueprint`, `Brainstorm`, `Showman`, etc.
- **Tarot Cards**: `TheFool`, `TheWorld`, `Death`, etc.
- **Spectral Cards**: `Ankh`, `Soul`, `Wraith`, etc.
- **Planet Cards**: `Jupiter`, `Mars`, `Venus`, etc.
- **Playing Cards**: Specific ranks/suits with seals/editions
- **Boss Blinds**: `TheGoad`, `CeruleanBell`, `TheOx`, etc.
- **Tags**: `NegativeTag`, `SpeedTag`, `RareTag`, etc.

## Advanced Features

- **Pack slot targeting**: Find items in specific booster pack positions
- **Shop slot filtering**: Target specific shop positions
- **Edition requirements**: Negative, Polychrome, Foil editions
- **Multiple clause logic**: Complex AND/OR requirements

## Scoring Modes

The `mode` field controls how scores from `should` clauses are aggregated.

### Supported Values

- `sum` (default): Adds `count * score` for each `should` clause
- `max`: Uses the maximum raw occurrence `count` across all `should` clauses (per-clause `score` is ignored)
- `max_count`, `maxcount`: Aliases for `max`

### Notes

- `minScore` in the Search modal is compared against the aggregated value from the selected mode
- Negative `score` values are allowed; they only affect `sum` mode

### Example: Max Aggregation

```json
{
  "name": "Tarot or Planet Rush",
  "mode": "max",
  "should": [
    { "type": "TarotCard", "value": "TheFool", "antes": [1, 2, 3], "score": 5 },
    { "type": "PlanetCard", "value": "Jupiter", "antes": [1, 2, 3], "score": 50 }
  ]
}
```

In this example, even though `PlanetCard` has higher `score`, `mode: max` ignores `score` and takes the higher raw occurrence count between the two clauses.

## Filter Storage

- **JSON Filters**: Store in `JsonFilters/` directory
- **JAML Filters**: Store in `JamlFilters/` directory (YAML-based format)

For more details on JAML format, see the [MotelyJAML README](../external/Motely/README.md).
