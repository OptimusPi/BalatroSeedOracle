# Motely Backend Issues

These are issues with the Motely library that need to be fixed upstream.

## 1. `min` field only works for SOME filter types in MUST clauses

**Status**: Mostly implemented! Only Tag filters are missing min support

**Location**:
- ✅ WORKS in MUST (has SearchIndividualSeeds): Joker, SoulJoker, Tarot, Planet, Spectral, Voucher, Boss, PlayingCard
- ❌ DOESN'T WORK in MUST: **Tag filters only** (MotelyJsonTagFilterDesc.cs)
- ✅ WORKS in SHOULD: All types (uses MotelyJsonScoring.cs)

**Description**:
Almost all filter types properly support `min` in MUST clauses via `ctx.SearchIndividualSeeds()` scalar verification. **Only Tag filters are missing this implementation.**

**How it works (SHOULD clauses)**:
```json
"should": [
  {
    "min": 2,
    "type": "Joker",
    "value": "Blueprint",
    "score": 100,
    "antes": [1,2,3,4,5,6,7,8]
  }
]
```
This WORKS - if you have less than 2 Blueprints, the count returns 0 and you get 0 score. See `MotelyJsonScoring.CountOccurrences()` at line 1185.

**How it works (MUST clauses - Joker example)**:
```json
"must": [
  {
    "min": 2,
    "type": "Joker",
    "value": "Blueprint",
    "antes": [1,2,3,4,5,6,7,8]
  }
]
```
This WORKS! See `MotelyJsonJokerFilterDesc.cs` line 241-267 - uses `ctx.SearchIndividualSeeds()` with scalar verification at line 261-263.

**How it DOESN'T work (Tag filters)**:
```json
"must": [
  {
    "min": 60,
    "type": "Tag",
    "values": ["DoubleTag", "NegativeTag"],
    "antes": [1,2,3,4,5,6,7,8]
  }
]
```
This DOESN'T WORK - Tag filter has NO `SearchIndividualSeeds()` scalar verification pass.

**Root Cause**:
- **Most filter types** (Joker, Tarot, etc.) properly use `ctx.SearchIndividualSeeds()` for scalar verification with min checks
- **Tag filter** is the ONLY one missing this implementation - it returns the vectorized mask directly
- Tag filter needs to be updated to match the pattern used by other filters

**Fix Required for Tag Filters**:
Update `MotelyJsonTagFilterDesc.Filter()` to use the same pattern as JokerFilterDesc:

```csharp
// In MotelyJsonTagFilterDesc.Filter():
public VectorMask Filter(ref MotelyVectorSearchContext ctx)
{
    // Step 1: Vectorized filtering (fast, existing code)
    VectorMask candidateMask = /* existing vectorized logic */;

    // Step 2: Scalar verification (ADD THIS - same pattern as Joker filter)
    return ctx.SearchIndividualSeeds(candidateMask, (ref MotelySingleSearchContext singleCtx) => {
        var runState = new MotelyRunState();
        foreach (var clause in _clauses)
        {
            int totalCount = 0;
            foreach (var ante in clause.EffectiveAntes)
            {
                int anteCount = MotelyJsonScoring.CountTagOccurrences(ref singleCtx, clause, ante);
                totalCount += anteCount;
            }
            int minThreshold = clause.Min ?? 1;
            if (totalCount < minThreshold)
                return false;
        }
        return true;
    });
}
```

**Benefits of this fix**:
- Matches existing pattern used by 8 other filter types
- Reuses existing scoring functions
- Minimal code change
- Maintains vectorized performance for 99.9% rejection

---

## 2. Affected item types - CORRECTED

**Item types where `min` FULLY works (MUST, MUSTNOT, and SHOULD)**:
- ✅ Joker (has SearchIndividualSeeds at line 241)
- ✅ SoulJoker (has SearchIndividualSeeds)
- ✅ Tarot (has SearchIndividualSeeds)
- ✅ Planet (has SearchIndividualSeeds)
- ✅ Spectral (has SearchIndividualSeeds)
- ✅ Voucher (has SearchIndividualSeeds)
- ✅ Boss (has SearchIndividualSeeds)
- ✅ PlayingCard (has SearchIndividualSeeds)
- ✅ AND/OR operators (scoring only - lines 1105, 1138)

**Item types where `min` works in SHOULD but NOT in MUST**:
- ❌ **Tag** - Missing SearchIndividualSeeds scalar verification

---

**Notes**:
- User successfully uses `min: 2` for jokers in both SHOULD and MUST clauses
- Tag is the ONLY filter type that doesn't support min in MUST clauses
- All other types properly implement the SearchIndividualSeeds pattern
