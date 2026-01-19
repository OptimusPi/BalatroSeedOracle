# JAML Template System - Final Decision (Confirmed)

## Executive Summary

**Decision**: Use **YAML Anchors & Aliases** for template reuse in JAML filters.

**Status**: ✅ Parser already supports it | ✅ Antes inheritance confirmed | 🚧 Front-end display: simple implementation

---

## Why YAML Anchors?

### 1. **Antes Inheritance EXISTS** ✅

**Confirmed**: Motely parser has `CloneClauseWithAnte()` that propagates antes from parent `And`/`Or` clauses to all children recursively.

**How it works**:
- If parent `And` clause has `Antes: [2,3,4,5,6,7,8,9,10,11,12]` and `AntesWasExplicitlySet = true`
- Motely clones each child clause for EACH ante in the array
- Children inherit antes automatically - no need to specify on each joker!

**File**: `external/Motely/Motely/filters/MotelyJson/MotelyCompositeFilterDesc.cs` lines 192-209

### 2. **Real-World Use Case: luck4.jaml**

**Current** (57 lines, repetitive):
```yaml
Should:
  - And:
    Mode: Max
    Score: 100
    clauses:
      - smallblindtag: NegativeTag
        Antes: [2]
      - Or:
          - joker: OopsAll6s
            Antes: [2]
            ShopSlots: [2,3,4]
          # ... repeated for [4,5,6] and [6,7,8]
  
  - And:  # Same structure for ante 3
    # ... 19 more lines of repetition
```

**With Anchors + Antes Inheritance** (clean):
```yaml
# Define cluster pattern ONCE (no antes needed!)
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [6,7,8]
    score: 100

Should:
  # ONE And clause for ALL antes 2-12!
  - And:
    Antes: [2,3,4,5,6,7,8,9,10,11,12]  # Parent antes array
    Mode: Max
    Score: 100
    clauses:
      - smallblindtag: NegativeTag  # Inherits Antes automatically!
      - Or: *oops_cluster  # Each joker inherits Antes automatically!
```

**Result**: 57 lines → ~20 lines. Change cluster pattern once, updates everywhere.

### 3. **Standard YAML Feature**
- ✅ Native YAML 1.2 specification
- ✅ No custom syntax needed
- ✅ Parser already supports it (YamlDotNet)
- ✅ Browser/WebAssembly compatible

---

## What This Means for Motely Parser

### Good News: Everything Already Works! 🎉

- ✅ YAML anchors parse correctly
- ✅ Antes inheritance works (`CloneClauseWithAnte()`)
- ✅ Test exists: `AnchorAntesInheritanceTest.cs`

### Action Items for Parser Team

1. **Verify anchor preservation in JamlFormatter** (Priority: Medium)
   - When writing JAML, preserve anchors (don't expand them)
   - Needed for round-trip editing

2. **Documentation** (Priority: Low)
   - Add example showing anchors + antes inheritance
   - Document the `AntesWasExplicitlySet` flag behavior

---

## Implementation for Avalonia UI

### Simple Version (15 minutes - 1 hour)

**Just show anchors exist** - no editing needed yet:

```csharp
// In JamlEditorTabViewModel or wherever you load JAML
using YamlDotNet.RepresentationModel;

public void DetectAnchors(string jamlContent)
{
    var yamlStream = new YamlStream();
    using (var reader = new StringReader(jamlContent))
    {
        yamlStream.Load(reader);
    }
    
    var anchors = new List<string>();
    TraverseForAnchors(yamlStream.Documents[0].RootNode, anchors);
    
    // Show in UI: "This filter uses {anchors.Count} template(s)"
}

private void TraverseForAnchors(YamlNode node, List<string> anchors)
{
    if (node is YamlScalarNode scalar && scalar.Anchor != null)
    {
        anchors.Add(scalar.Anchor);
    }
    else if (node is YamlMappingNode mapping)
    {
        foreach (var child in mapping.Children.Values)
            TraverseForAnchors(child, anchors);
    }
    else if (node is YamlSequenceNode sequence)
    {
        foreach (var child in sequence.Children)
            TraverseForAnchors(child, anchors);
    }
}
```

**That's it.** Show a badge/icon if anchors exist. Done.

### Full Version (Later, if needed)

- Template panel showing all anchors
- Click alias → jump to definition
- Create anchor from selection
- Edit anchor templates

**But honestly**: The simple version is probably enough. Users can write anchors manually in JAML editor, and the parser handles it.

---

## Example: luck4.jaml Refactored

### Before (Repetitive - 57 lines)

```yaml
Should:
  - And:
    Mode: Max
    Score: 100
    clauses:
      - smallblindtag: NegativeTag
        Antes: [2]
      - Or:
          - joker: OopsAll6s
            Antes: [2]
            ShopSlots: [2,3,4]
            score: 100
          - joker: OopsAll6s
            Antes: [2]
            ShopSlots: [4,5,6]
            score: 100
          - joker: OopsAll6s
            Antes: [2]
            ShopSlots: [6,7,8]
            score: 100
  
  - And:  # Same for ante 3
    Mode: Sum
    Score: 100
    clauses:
      - smallblindtag: NegativeTag
        Antes: [3]
      - Or:
          - joker: OopsAll6s
            Antes: [3]
            ShopSlots: [2,3,4]
            score: 100
          # ... etc
```

### After (With Anchors + Antes Inheritance - ~20 lines)

```yaml
# Define cluster pattern ONCE
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [6,7,8]
    score: 100

Should:
  # Ante 2 - Max mode
  - And:
    Antes: [2]  # Parent antes - children inherit!
    Mode: Max
    Score: 100
    clauses:
      - smallblindtag: NegativeTag  # Inherits Antes: [2]
      - Or: *oops_cluster  # Each joker inherits Antes: [2]
  
  # Ante 3 - Sum mode
  - And:
    Antes: [3]  # Parent antes - children inherit!
    Mode: Sum
    Score: 100
    clauses:
      - smallblindtag: NegativeTag  # Inherits Antes: [3]
      - Or: *oops_cluster  # Each joker inherits Antes: [3]
```

**Or even better - if you want antes 2-12 in ONE clause**:

```yaml
Should:
  - And:
    Antes: [2,3,4,5,6,7,8,9,10,11,12]  # ALL antes at once!
    Mode: Max
    Score: 100
    clauses:
      - smallblindtag: NegativeTag  # Inherits all antes
      - Or: *oops_cluster  # Each joker inherits all antes
```

**Motely automatically creates separate AND groups for each ante**, so this works perfectly!

---

## Benefits

| Feature | Without Anchors | With Anchors |
|---------|----------------|--------------|
| **Lines of code** | 57 lines | ~20 lines |
| **Repetition** | 11x same pattern | Define once |
| **Maintenance** | Change 11 places | Change once |
| **Antes 2-12** | 11 separate And clauses | 1 And clause with array |
| **Readability** | Lots of repetition | Clear pattern definition |

---

## Implementation Priority

### Phase 1: Simple Display (15 min - 1 hour)
- Detect anchors in JAML
- Show badge/icon: "Uses templates"
- **That's it.** Users can write anchors manually.

### Phase 2: Template Panel (1-2 days)
- Show all anchors in sidebar
- Click alias → jump to definition
- Preview template structure

### Phase 3: Editing (Later, if needed)
- Create anchor from selection
- Edit anchor templates
- Expand alias to inline

**Recommendation**: Start with Phase 1. It's useful and takes 15 minutes. Phase 2/3 can wait.

---

## Conclusion

**YAML Anchors + Antes Inheritance = Powerful combination**

- ✅ Define cluster pattern once with anchor
- ✅ Parent `And` clause has `Antes: [2,3,4,5,6,7,8,9,10,11,12]`
- ✅ Children inherit antes automatically
- ✅ No repetition needed
- ✅ Clean, maintainable filters

**For Avalonia UI**: Simple anchor detection is enough. Full editing can come later if users want it.

---

**Document Version**: 3.0 (Final - Confirmed)  
**Date**: 2025-01-XX  
**Status**: ✅ Antes inheritance confirmed | ✅ Anchors work | 🚧 Simple UI display needed
