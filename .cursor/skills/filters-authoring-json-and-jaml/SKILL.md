---
name: filters-authoring-json-and-jaml
description: Adds or modifies filter serialization and editor behavior (JSON/JAML). Use when changing filter formats, adding clause fields, or debugging filter round-trips.
---

# Filter Authoring (JSON and JAML)

## Key Files

| File                                     | Purpose                             |
| ---------------------------------------- | ----------------------------------- |
| `ViewModels/FiltersModalViewModel.cs`    | Main filter editing ViewModel       |
| `Services/FilterSerializationService.cs` | JSON serialization/deserialization  |
| `Services/FilterConfigurationService.cs` | Build config from visual selections |
| `Motely.Filters.MotelyJsonConfig`        | Filter data model                   |

## Filter JSON Structure

```json
{
  "name": "My Filter",
  "description": "Filter description",
  "author": "Username",
  "dateCreated": "2026-01-19T00:00:00Z",
  "mode": "sum",
  "deck": "Red",
  "stake": "white",
  "must": [
    { "type": "Joker", "value": "Blueprint", "antes": [1, 2, 3] }
  ],
  "should": [
    { "type": "Voucher", "value": "Telescope", "antes": [1, 2], "score": 10 }
  ],
  "mustNot": [
    { "type": "Boss", "value": "TheNeedle" }
  ]
}
```

## Clause Types

| Type                            | Value Examples               |
| ------------------------------- | ---------------------------- |
| `Joker`                         | `Blueprint`, `Brainstorm`    |
| `SoulJoker`                     | `Perkeo`, `Triboulet`, `Any` |
| `Voucher`                       | `Telescope`, `Observatory`   |
| `TarotCard`                     | `TheFool`, `TheWorld`        |
| `SpectralCard`                  | `Ankh`, `Soul`               |
| `PlanetCard`                    | `Jupiter`, `Mars`            |
| `Boss`                          | `TheNeedle`, `ThePlant`      |
| `SmallBlindTag` / `BigBlindTag` | `NegativeTag`, `RareTag`     |
| `Or` / `And`                    | Nested clauses               |

## Clause Properties

```json
{
  "type": "Joker",
  "value": "Blueprint",
  "edition": "Negative",
  "antes": [1, 2, 3, 4],
  "score": 10,
  "min": 1,
  "sources": {
    "shopSlots": [0, 1],
    "packSlots": [0, 1, 2],
    "tags": true,
    "requireMega": true
  }
}
```

## Operator Clauses (AND/OR)

```json
{
  "type": "Or",
  "mode": "Max",
  "clauses": [
    { "type": "Joker", "value": "Blueprint" },
    { "type": "Joker", "value": "Brainstorm" }
  ]
}
```

## Scoring Modes

| Mode            | Behavior                                          |
| --------------- | ------------------------------------------------- |
| `sum` (default) | Adds `count * score` for each should clause       |
| `max`           | Uses maximum raw occurrence count (ignores score) |

## Round-Trip Considerations

### Metadata Preservation

Filter metadata (name, description, author, dateCreated) should be preserved on re-save:

```csharp
// Store original metadata when loading
_originalDateCreated = config.DateCreated;
_originalAuthor = config.Author;

// Use originals when saving
config.DateCreated = _originalDateCreated ?? DateTime.Now;
config.Author = _originalAuthor ?? userProfileService.GetAuthorName();
```

### Criteria Hash

Used to detect meaningful changes vs metadata-only edits:

```csharp
// Compute hash of MUST/SHOULD/MUSTNOT criteria
var currentHash = ComputeCriteriaHash();

// Only invalidate databases if criteria changed
if (currentHash != _originalCriteriaHash)
{
    await CleanupFilterDatabases();
}
```

## JAML (YAML-based) Format

JAML uses YamlDotNet for serialization with camelCase naming:

```yaml
name: My Filter
deck: Red
stake: white
must:
  - type: Joker
    value: Blueprint
    antes: [1, 2, 3]
should:
  - type: Voucher
    value: Telescope
    score: 10
```

## Adding New Clause Fields

1. Add property to `MotelyJsonConfig.MotelyJsonFilterClause`
2. Update `WriteFilterItem()` in `FilterSerializationService`
3. Update `ConvertClauseToItemConfig()` in `FiltersModalViewModel`
4. Update `ConvertItemConfigToClause()` for reverse conversion
5. Test round-trip serialization

## Checklist

- [ ] JSON structure matches `MotelyJsonConfig` schema
- [ ] Metadata preserved on re-save
- [ ] Criteria changes trigger database cleanup
- [ ] Both JSON and JAML serialization updated
- [ ] Round-trip tested (save → load → save produces same output)
