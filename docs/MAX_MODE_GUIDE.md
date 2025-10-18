# Max Mode and Or Clauses - User Guide

## Overview

BalatroSeedOracle now supports **Max mode** scoring and **Or clause** grouping for advanced filter configurations. These features were already implemented in the Motely search engine and are now properly integrated into the application's filter serialization.

## Max Mode

### What is Max Mode?

Max mode changes how SHOULD clauses are scored:

- **Default (Sum mode)**: Total score = Sum of (count × score) for each SHOULD clause
- **Max mode**: Total score = Maximum occurrence count across all SHOULD clauses (ignores per-clause scores)

### When to Use Max Mode

Use Max mode when you want to:
- Rank seeds by the **maximum number of occurrences** of any single condition
- Ignore weighted scoring and focus on raw counts
- Find seeds with the highest concentration of any particular item

### Example

```json
{
  "name": "Max Mode Example",
  "mode": "Max",
  "should": [
    {
      "type": "Joker",
      "value": "Blueprint",
      "score": 100,
      "antes": [1,2,3,4,5,6,7,8]
    },
    {
      "type": "Joker",
      "value": "Brainstorm",
      "score": 50,
      "antes": [1,2,3,4,5,6,7,8]
    }
  ]
}
```

**Sum mode (default)**: If Blueprint appears 2 times and Brainstorm appears 3 times:
- Score = (2 × 100) + (3 × 50) = 350

**Max mode**:
- Score = max(2, 3) = 3 (ignores the score weights)

### Supported Mode Values

The `mode` field accepts (case-insensitive):
- `"Sum"` or omitted - Default sum-of-scores behavior
- `"Max"`, `"MaxCount"`, or `"max_count"` - Maximum count behavior

## Or Clauses

### What are Or Clauses?

Or clauses allow you to group multiple conditions where **any one** of them can match. This is useful for:
- Finding items in different slot ranges
- Matching multiple alternative conditions
- Complex filtering logic

### Structure

```json
{
  "type": "Or",
  "score": 100,
  "clauses": [
    { /* condition 1 */ },
    { /* condition 2 */ },
    { /* condition 3 */ }
  ]
}
```

### Example: Find OopsAll6s in Any Slot Range

This example finds OopsAll6s joker appearing in ANY of the specified shop slot ranges:

```json
{
  "name": "OopsAll6s Slot Finder",
  "mode": "Max",
  "should": [
    {
      "score": 100,
      "type": "Or",
      "clauses": [
        {
          "type": "Joker",
          "value": "OopsAll6s",
          "sources": { "shopSlots": [2,3,4,5] },
          "antes": [4]
        },
        {
          "type": "Joker",
          "value": "OopsAll6s",
          "sources": { "shopSlots": [6,7,8,9] },
          "antes": [4]
        },
        {
          "type": "Joker",
          "value": "OopsAll6s",
          "sources": { "shopSlots": [10,11,12,13] },
          "antes": [4]
        }
      ]
    }
  ]
}
```

**What this does**:
- Searches for OopsAll6s in ante 4
- Matches if it appears in slots 2-5 **OR** 6-9 **OR** 10-13
- With Max mode, ranks by the highest count found in any slot range
- Each nested clause inherits the parent score (100)

## And Clauses

And clauses work similarly but require **all** nested conditions to match:

```json
{
  "type": "And",
  "clauses": [
    { /* all must match */ }
  ]
}
```

## Combining Features

You can combine Max mode with complex Or/And clause structures:

```json
{
  "mode": "Max",
  "should": [
    {
      "score": 100,
      "type": "Or",
      "clauses": [
        { "type": "Joker", "value": "Blueprint", "antes": [1,2,3] },
        { "type": "Joker", "value": "Brainstorm", "antes": [1,2,3] }
      ]
    },
    {
      "score": 50,
      "type": "And",
      "clauses": [
        { "type": "Joker", "value": "Baron", "antes": [4,5,6] },
        { "type": "TarotCard", "value": "TheDevil", "antes": [4,5,6] }
      ]
    }
  ]
}
```

This finds seeds ranked by:
- The maximum of: (Blueprint OR Brainstorm count in antes 1-3, Baron AND Devil count in antes 4-6)

## Technical Details

### Motely Engine Support

These features are implemented in the Motely search engine:
- **File**: `external/Motely/Motely/filters/MotelyJson/MotelyJsonConfig.cs`
- **Enum**: `MotelyScoreAggregationMode` (Sum, MaxCount)
- **Processing**: `PostProcess()` method parses mode string
- **Nested clauses**: Recursively processed via `ProcessClause()`

### Serialization Support

FilterSerializationService now supports:
- Reading `mode` field from JSON configs
- Writing `mode` field when serializing
- Recursive serialization of nested `clauses` arrays

### File Location

The serialization service is at:
- `src/Services/FilterSerializationService.cs`

## Testing

A test filter is available at:
- `JsonItemFilters/test_max_mode.json`

Load this filter in the application to verify Max mode and Or clause functionality.
