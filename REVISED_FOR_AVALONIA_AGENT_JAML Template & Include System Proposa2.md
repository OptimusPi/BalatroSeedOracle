# Revised Proposal: YAML Anchors & Aliases for Template System

## Executive Summary

After analysis by the Avalonia UI front-end team, we've determined that **YAML Anchors & Aliases** (native YAML feature) is the better choice over the custom `!include` preprocessor system for template reuse in JAML filters.

**Decision**: Use **YAML Anchors & Aliases** instead of `!include` preprocessor.

## Why YAML Anchors Instead of Include System?

### 1. **Standard YAML Feature**
- ✅ Native YAML 1.2 specification feature
- ✅ No custom syntax needed
- ✅ Works with any YAML parser (YamlDotNet already supports it)
- ✅ No preprocessor step required

### 2. **Parser Already Supports It**
- ✅ `JamlConfigLoader.cs` already handles anchors/aliases via YamlDotNet
- ✅ Test `Test5_JamlAnchorsExpandToJsonCorrectly` verifies it works
- ✅ **No parser changes needed** - it just works!

### 3. **Browser Compatibility**
- ✅ Works in WebAssembly (YamlDotNet supports it)
- ❌ Include System requires file system access (not available in browser)
- ✅ Anchors are part of YAML structure, not file system

### 4. **Visual Builder Compatibility**
- ✅ Can detect anchors in YAML structure (semantic)
- ✅ Can show template panel with all anchors
- ✅ Can show alias references with visual indicators
- ✅ Can edit template and see all references update
- ✅ Round-trip preservation possible
- ❌ Include System: Text substitution loses semantic structure
- ❌ Include System: Can't show "this came from template" in UI
- ❌ Include System: Can't edit template and see updates

### 5. **Round-Trip Preservation**
- ✅ YAML Anchors: Can detect and preserve anchors when saving
- ❌ Include System: Once expanded, can't get back to `!include` statements

## What This Means for Motely Parser

### Good News: No Changes Needed! 🎉

The parser (`JamlConfigLoader.cs`) **already supports** YAML anchors and aliases. YamlDotNet handles it natively.

### What We Need from Parser Team

1. **Verify anchor support is working** (test already exists)
2. **Ensure anchor preservation** when formatting JAML output
   - `JamlFormatter.cs` should preserve anchors when writing YAML
   - Currently may expand anchors - need to preserve them

3. **Documentation**
   - Add examples of anchor usage in JAML filters
   - Document merge keys (`<<:`) for parameterized templates

## YAML Anchors Syntax (For Reference)

### Basic Anchor Definition
```yaml
# Define template
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100

# Use template
Should:
  - And:
      clauses:
        - smallblindtag: NegativeTag
          Antes: [2]
        - Or: *oops_cluster  # Reference the anchor
```

### Parameterized Templates (Merge Keys)
```yaml
# Template with default values
negative_tag_base: &negative_tag_base
  smallblindtag: NegativeTag
  Antes: [2]  # Default parameter

# Use with override
Should:
  - And:
      clauses:
        - <<: *negative_tag_base  # Merge template
          Antes: [3]  # Override parameter
```

## Implementation Status

### Parser (Motely) - ✅ DONE
- YamlDotNet handles anchors natively
- Test exists: `Test5_JamlAnchorsExpandToJsonCorrectly`
- **Action needed**: Ensure `JamlFormatter.cs` preserves anchors when writing

### Front-End (Avalonia UI) - 🚧 IN PROGRESS
- Phase 1: Display support (read-only visualization)
- Phase 2: Editing support (create/edit anchors)
- Phase 3: Advanced features (parameterized templates)

## Benefits Over Include System

| Feature | YAML Anchors | Include System |
|---------|--------------|----------------|
| Standard YAML | ✅ Yes | ❌ Custom syntax |
| Browser support | ✅ Yes | ❌ Needs file system |
| Visual builder | ✅ Yes | ❌ Text substitution |
| Round-trip | ✅ Yes | ❌ No |
| Parser changes | ✅ None needed | ❌ Preprocessor needed |
| Edit templates | ✅ Yes | ❌ No |

## Migration Path

### For Existing Filters
- No migration needed - anchors work immediately
- Can gradually refactor repetitive patterns to use anchors

### For Template Library Concept
- Instead of `JamlTemplates/` folder with `.jaml-template` files
- Use anchor definitions within filters or separate "template filter" files
- Users can copy anchor definitions between filters

## Questions for Parser Team

1. **Anchor Preservation in JamlFormatter**
   - Does `JamlFormatter.cs` currently preserve anchors when writing YAML?
   - If not, can we add this feature?
   - Priority: Medium (needed for round-trip editing)

2. **Merge Keys Support**
   - Are merge keys (`<<:`) fully supported?
   - Any known limitations?

3. **Performance**
   - Any performance concerns with many anchors?
   - Circular reference detection?

## Conclusion

**Recommendation**: Continue using YAML Anchors & Aliases (native YAML feature) instead of implementing the `!include` preprocessor system.

**Parser Action Items**:
1. Verify anchor preservation in `JamlFormatter.cs`
2. Add documentation/examples for anchor usage
3. Test merge keys support

**Front-End Action Items**:
1. Implement Phase 1: Display support (read-only)
2. Implement Phase 2: Editing support
3. Implement Phase 3: Advanced features

---

**Document Version**: 1.0  
**Date**: 2025-01-XX  
**From**: Avalonia UI Front-End Team  
**To**: Motely Parser Team (OptimusPi/MotelyJAML submodule)
