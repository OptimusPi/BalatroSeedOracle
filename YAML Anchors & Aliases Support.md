# YAML Anchors & Aliases Support for Avalonia UI Visual Designer

## Overview

This document describes how to implement YAML anchors (`&`) and aliases (`*`) support in the Avalonia UI Visual Designer Modal for JAML filters. This feature allows users to create reusable clause templates, reducing repetition and improving maintainability of complex filters.

## Background

### What are YAML Anchors & Aliases?

YAML anchors and aliases are a built-in YAML feature for template reuse:

- **Anchors** (`&name`): Define a reusable template/node
- **Aliases** (`*name`): Reference an anchor, creating a copy
- **Merge Keys** (`<<: *name`): Merge properties from an anchor into the current node

### Why Support This?

Many JAML filters have repetitive patterns:
- Same clause structures across multiple antes
- Repeated joker combinations with only ante numbers changing
- Complex nested structures that appear multiple times

YAML anchors eliminate this repetition, making filters:
- **More maintainable**: Change once, update everywhere
- **More readable**: Less visual clutter
- **More powerful**: Template-based filter building

## Parser Support

The JAML parser (`JamlConfigLoader.cs`) **already supports** YAML anchors and aliases. This is verified by the test `Test5_JamlAnchorsExpandToJsonCorrectly` in `Motely.Tests/FormatConversionTests.cs`.

The parser uses `YamlDotNet`, which handles anchors/aliases natively. No parser changes are needed.

## Implementation Guide for Avalonia UI

### Phase 1: Display Support

#### 1.1 Parse Anchors During Load

When loading JAML in the Visual Designer:

```csharp
// In your JAML loader
var config = JamlConfigLoader.TryLoadFromJamlString(jamlContent, out var config, out var error);

// YamlDotNet automatically expands anchors during deserialization
// The config object contains the fully expanded structure
```

**Important**: The parser expands anchors automatically. The `MotelyJsonConfig` object you receive has all anchors resolved to their actual values.

#### 1.2 Detect Anchor Definitions

To show anchors in the UI, you need to parse the raw YAML separately:

```csharp
using YamlDotNet.RepresentationModel;

var yamlStream = new YamlStream();
using (var reader = new StringReader(jamlContent))
{
    yamlStream.Load(reader);
}

// Traverse the YAML document to find anchors
var anchors = new Dictionary<string, YamlNode>();
TraverseForAnchors(yamlStream.Documents[0].RootNode, anchors);

void TraverseForAnchors(YamlNode node, Dictionary<string, YamlNode> anchors)
{
    if (node is YamlScalarNode scalar && scalar.Anchor != null)
    {
        anchors[scalar.Anchor] = scalar;
    }
    else if (node is YamlMappingNode mapping)
    {
        foreach (var child in mapping.Children.Values)
        {
            TraverseForAnchors(child, anchors);
        }
    }
    else if (node is YamlSequenceNode sequence)
    {
        foreach (var child in sequence.Children)
        {
            TraverseForAnchors(child, anchors);
        }
    }
}
```

#### 1.3 Visual Indicators

In the Visual Designer UI:

- **Anchor definitions**: Show a special icon/badge (e.g., `&template_name`)
- **Alias references**: Show a link icon or different styling (e.g., `*template_name`)
- **Template panel**: Sidebar showing all defined anchors with preview

### Phase 2: Editing Support

#### 2.1 Create Anchor from Selection

Allow users to:
1. Select a clause/clause group in the visual designer
2. Right-click → "Create Template" or "Extract as Anchor"
3. Name the anchor (e.g., `oops_pattern`)
4. System creates anchor definition and replaces selection with alias

```csharp
// Pseudo-code for anchor creation
public void CreateAnchorFromSelection(ClauseViewModel selectedClause, string anchorName)
{
    // 1. Serialize selected clause to YAML
    var yaml = SerializeClauseToYaml(selectedClause);
    
    // 2. Add anchor marker
    var anchoredYaml = AddAnchorToYaml(yaml, anchorName);
    
    // 3. Replace original with alias reference
    var aliasYaml = $"*{anchorName}";
    
    // 4. Update JAML content
    UpdateJamlContent(anchoredYaml, aliasYaml);
}
```

#### 2.2 Edit Anchor Templates

When user clicks on an anchor definition:
- Show the template editor (same as clause editor)
- Changes to template affect all alias references
- Visual indicator showing "Template: affects X references"

#### 2.3 Convert Alias to Inline

Allow users to "expand" an alias:
- Right-click alias → "Expand Template"
- Replace alias with full clause structure
- Useful for one-off modifications

### Phase 3: Advanced Features

#### 3.1 Parameterized Templates

For templates that differ only by ante numbers:

```yaml
# Template with parameter
ante_pattern: &ante_pattern
  smallblindtag: NegativeTag
  Antes: [2]  # This is the parameter

# Use with override
Should:
  - And:
      clauses:
        - <<: *ante_pattern
          Antes: [3]  # Override the antes value
```

**Implementation**: Use YAML merge keys (`<<:`) to allow property overrides.

#### 3.2 Template Library

Pre-defined templates users can insert:
- Common joker combinations
- Standard ante patterns
- Reusable scoring structures

#### 3.3 Template Validation

When editing templates:
- Validate that template structure is valid JAML
- Check that all referenced properties exist
- Warn if template might create invalid filter logic

## UI/UX Design Recommendations

### Visual Designer Layout

```
┌─────────────────────────────────────────────────┐
│  JAML Visual Designer                           │
├──────────┬──────────────────────┬───────────────┤
│          │                      │               │
│ Templates│   Main Editor        │   Properties  │
│ Panel    │   (Clause Tree)      │   Panel       │
│          │                      │               │
│ &pattern1│   Should:           │   [Selected   │
│ &pattern2│     - And:           │    Clause     │
│          │       clauses:       │    Editor]    │
│ [+ New]  │         - *pattern1  │               │
│          │         - *pattern2  │               │
│          │                      │               │
└──────────┴──────────────────────┴───────────────┘
```

### Template Panel Features

1. **List of Templates**
   - Show all anchors defined in current filter
   - Preview of template structure
   - Usage count (how many aliases reference it)

2. **Template Actions**
   - Edit template
   - Duplicate template
   - Delete template (with confirmation if in use)
   - Export template (for template library)

3. **Template Preview**
   - Collapsible tree view of template structure
   - Highlight which properties can be overridden

### Clause Tree Features

1. **Anchor Indicators**
   - Icon next to anchor definitions
   - Different icon for alias references
   - Click alias → jump to anchor definition

2. **Context Menu**
   - "Create Template" (on selection)
   - "Expand Template" (on alias)
   - "Edit Template" (on anchor)
   - "Go to Definition" (on alias)

3. **Visual Styling**
   - Anchor definitions: Subtle background color
   - Alias references: Dotted border or link styling
   - Hover: Show template preview tooltip

## Code Examples

### Example 1: Simple Anchor

```yaml
# Define template
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    Antes: [2]
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    Antes: [2]
    ShopSlots: [4,5,6]
    score: 100

# Use template
Should:
  - And:
      clauses:
        - smallblindtag: NegativeTag
          Antes: [2]
        - Or: *oops_cluster
```

**Visual Designer Representation**:
- `oops_cluster` appears in Templates Panel
- `Or: *oops_cluster` shows as alias reference in clause tree
- Clicking alias shows template preview

### Example 2: Parameterized Template (Merge Keys)

```yaml
# Template with parameter
negative_tag_base: &negative_tag_base
  smallblindtag: NegativeTag
  Antes: [2]  # Default value

# Use with override
Should:
  - And:
      clauses:
        - <<: *negative_tag_base
          Antes: [3]  # Override parameter
```

**Visual Designer Representation**:
- Template shows "Antes: [2]" as default
- Alias reference shows merge icon
- Properties panel allows overriding "Antes" value

### Example 3: Nested Templates

```yaml
# Nested structure
joker_pattern: &joker_pattern
  joker: OopsAll6s
  ShopSlots: [2,3,4]
  score: 100

or_cluster: &or_cluster
  Or:
    - *joker_pattern
    - *joker_pattern  # Can reference same template multiple times

Should:
  - And:
      clauses:
        - smallblindtag: NegativeTag
        - *or_cluster
```

**Visual Designer Representation**:
- Templates Panel shows hierarchy: `or_cluster` → `joker_pattern`
- Nested references are expandable
- Usage count shows `joker_pattern` used 2 times

## Testing Checklist

### Parser Compatibility
- [ ] Load JAML with anchors - parses correctly
- [ ] Load JAML with aliases - expands correctly
- [ ] Load JAML with merge keys - merges correctly
- [ ] Round-trip: Save and reload preserves anchors

### Visual Designer
- [ ] Display anchor definitions with icon
- [ ] Display alias references with different styling
- [ ] Templates Panel shows all anchors
- [ ] Click alias jumps to anchor definition
- [ ] Template preview shows correct structure

### Editing
- [ ] Create anchor from selection works
- [ ] Edit anchor updates all references
- [ ] Expand alias to inline works
- [ ] Delete anchor with confirmation if in use
- [ ] Parameter override works with merge keys

### Validation
- [ ] Invalid anchor name shows error
- [ ] Reference to non-existent anchor shows error
- [ ] Circular anchor reference detected
- [ ] Template structure validation works

## Integration Points

### Existing Code to Reference

1. **Parser**: `Motely/JamlConfigLoader.cs`
   - Already handles anchors/aliases
   - No changes needed

2. **Formatter**: `Motely/filters/MotelyJson/JamlFormatter.cs`
   - Formats JAML output
   - May need updates to preserve anchors in output

3. **Schema**: `jaml.schema.json`
   - JSON Schema for validation
   - Anchors are YAML-level, not schema-level
   - No schema changes needed

### Avalonia-Specific Considerations

1. **YAML Parsing**: Use `YamlDotNet` (same as Motely)
   - Already in use or easy to add
   - Full anchor/alias support

2. **UI Framework**: Avalonia UI
   - TreeView for clause structure
   - ContextMenu for actions
   - PropertyGrid for editing

3. **Data Binding**: 
   - Bind clause tree to YAML structure
   - Two-way binding for edits
   - Update JAML on template changes

## Migration Path

### Phase 1: Read-Only Support (Week 1)
- Display anchors/aliases in visual designer
- Show templates panel
- Allow viewing but not editing

### Phase 2: Basic Editing (Week 2)
- Create anchor from selection
- Edit anchor definitions
- Expand aliases to inline

### Phase 3: Advanced Features (Week 3)
- Parameterized templates (merge keys)
- Template library
- Template validation

## Example: luck3.jaml Refactored

### Before (Repetitive)
```yaml
Should:
  - And:
      Mode: Sum
      Score: 100
      clauses:
        - smallblindtag: NegativeTag
          Antes: [2]
        - Or:
            - joker: OopsAll6s
              Antes: [2]
              ShopSlots: [2,3,4]
            - joker: OopsAll6s
              Antes: [2]
              ShopSlots: [4,5,6]
            - joker: OopsAll6s
              Antes: [2]
              ShopSlots: [6,7,8]
  - And:  # Same structure, different antes
      Mode: Sum
      Score: 100
      clauses:
        - smallblindtag: NegativeTag
          Antes: [3]
        - Or:
            - joker: OopsAll6s
              Antes: [3]
              ShopSlots: [2,3,4]
            # ... etc
```

### After (With Anchors)
```yaml
# Define reusable patterns
oops_or_pattern: &oops_or_pattern
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [6,7,8]
    score: 100

ante_and_pattern: &ante_and_pattern
  Mode: Sum
  Score: 100
  clauses:
    - smallblindtag: NegativeTag
      Antes: [2]  # Parameter
    - Or: *oops_or_pattern

Should:
  - <<: *ante_and_pattern
    clauses:
      - smallblindtag: NegativeTag
        Antes: [2]  # Override
      - Or:
          - <<: *oops_or_pattern
            Antes: [2]  # Add antes to each
          - <<: *oops_or_pattern
            Antes: [2]
          - <<: *oops_or_pattern
            Antes: [2]
  - <<: *ante_and_pattern
    clauses:
      - smallblindtag: NegativeTag
        Antes: [3]  # Different ante
      - Or:
          - <<: *oops_or_pattern
            Antes: [3]
          # ... etc
```

**Note**: The exact anchor structure may vary based on what properties need to be parameterized. The key is allowing users to define templates and reuse them.

## Questions for Implementation

1. **Anchor Naming**: 
   - Allow user-defined names or auto-generate?
   - Validation rules for anchor names?

2. **Template Scope**:
   - Anchors scoped to current filter only?
   - Or allow importing from template library?

3. **Editing Model**:
   - Edit templates in-place or separate modal?
   - How to handle template updates affecting multiple references?

4. **Performance**:
   - How many anchors before performance degrades?
   - Lazy-load template previews?

## Resources

- **YAML Spec**: https://yaml.org/spec/1.2.2/#anchors-and-aliases
- **YamlDotNet Docs**: https://github.com/aaubry/YamlDotNet
- **Motely Parser**: `Motely/JamlConfigLoader.cs`
- **Test Example**: `Motely.Tests/FormatConversionTests.cs` - `Test5_JamlAnchorsExpandToJsonCorrectly`

## Summary

YAML anchors and aliases are a powerful feature for reducing repetition in JAML filters. The parser already supports them, so the Avalonia UI Visual Designer just needs to:

1. **Display** anchors and aliases visually
2. **Allow creating** anchors from selections
3. **Enable editing** anchor templates
4. **Support** parameterized templates via merge keys

This will make the Visual Designer more powerful and user-friendly for complex filter creation.

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-XX  
**Author**: For Avalonia UI Visual Designer Team
