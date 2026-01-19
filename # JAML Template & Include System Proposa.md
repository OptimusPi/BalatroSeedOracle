# JAML Template & Include System Proposal

## Problem

Users need to reuse complex clause patterns (AND/OR structures) across multiple filters without:
- Retyping the same patterns repeatedly
- Risk of typos and inconsistencies
- Maintenance headaches when patterns need updates

## Solution: Template Library + Include System

### File Structure

```
JamlFilters/
  ├── luck3.jaml          # Main filter (uses templates)
  └── ...

JamlTemplates/            # NEW: Template library directory
  ├── mechanics/          # Game mechanic patterns
  │   ├── oops_cluster.jaml-template
  │   ├── negative_tag_skip.jaml-template
  │   └── ...
  └── scenarios/          # Common scenario patterns
      ├── early_economy.jaml-template
      └── ...
```

### File Extension Options

**Option 1: `.jaml-template`** (Recommended)
- Clear that it's a template, not a filter
- Easy to distinguish in file explorer
- Example: `oops_cluster.jaml-template`

**Option 2: `.jaml-lib`** (Library)
- Indicates it's a library/reusable component
- Example: `oops_cluster.jaml-lib`

**Option 3: `.jaml` in separate directory**
- Keep `.jaml` extension but in `JamlTemplates/` folder
- Simpler, but harder to distinguish

**Recommendation: `.jaml-template`** - Most explicit and clear.

### Template File Format

Templates are partial JAML - just the clause structure, not a full filter:

```yaml
# JamlTemplates/mechanics/oops_cluster.jaml-template
# Template: OopsAll6s joker cluster pattern
# Parameters: antes (array of integers)
type: mechanic
description: OopsAll6s jokers in shop slot clusters [2,3,4], [4,5,6], [6,7,8]

# Template content (clause structure)
- Or:
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [6,7,8]
    score: 100
```

### Include Syntax

Use YAML tag syntax for includes:

```yaml
# luck3.jaml
name: luck2
author: pifreak
deck: Anaglyph
stake: White

Must:
  - soulJoker: Perkeo
    antes: [1,2,3,4,5,6,7,8]

Should:
  - SoulJoker: Perkeo
    score: 10
  
  # Include template and customize
  - And:
    Mode: Sum
    Score: 100
    clauses:
    - smallblindtag: NegativeTag
      Antes: [2]
    - !include mechanics/oops_cluster.jaml-template
      # Parameters can be passed here
      # (requires preprocessor support)
  
  - And:
    Mode: Sum
    Score: 100
    clauses:
    - smallblindtag: NegativeTag
      Antes: [3]
    - !include mechanics/oops_cluster.jaml-template
```

### Alternative: Simpler Include Syntax

If YAML tags are too complex, use a preprocessor directive:

```yaml
# luck3.jaml
Should:
  - And:
    clauses:
    - smallblindtag: NegativeTag
      Antes: [2]
    - !include templates/mechanics/oops_cluster.jaml-template
```

Or even simpler comment-based:

```yaml
# luck3.jaml
Should:
  - And:
    clauses:
    - smallblindtag: NegativeTag
      Antes: [2]
    # @include templates/mechanics/oops_cluster.jaml-template
```

## Implementation Approach

### Phase 1: Preprocessor (Simplest)

Add a preprocessor step in `JamlConfigLoader.cs`:

```csharp
private static string PreProcessIncludes(string jamlContent, string basePath)
{
    var lines = jamlContent.Split('\n');
    var result = new StringBuilder();
    
    foreach (var line in lines)
    {
        // Match: !include path/to/template.jaml-template
        var includeMatch = Regex.Match(line, @"!include\s+(.+\.jaml-template)");
        if (includeMatch.Success)
        {
            var templatePath = includeMatch.Groups[1].Value;
            var fullPath = ResolveTemplatePath(templatePath, basePath);
            
            if (File.Exists(fullPath))
            {
                var templateContent = File.ReadAllText(fullPath);
                // Remove template metadata (type, description) if present
                var templateClauses = ExtractClauses(templateContent);
                result.AppendLine(templateClauses);
            }
            else
            {
                throw new FileNotFoundException($"Template not found: {templatePath}");
            }
        }
        else
        {
            result.AppendLine(line);
        }
    }
    
    return result.ToString();
}
```

### Phase 2: Parameter Support

Allow templates to accept parameters:

```yaml
# Template with parameter placeholder
- Or:
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
    Antes: ${ANTES}  # Parameter placeholder

# Usage
- !include mechanics/oops_cluster.jaml-template
  parameters:
    ANTES: [2]
```

### Phase 3: Template Metadata

Templates can have metadata at the top:

```yaml
---
# Template metadata (YAML front matter)
type: mechanic
name: oops_cluster
description: OopsAll6s joker cluster pattern
parameters:
  - name: antes
    type: array
    required: true
    description: Ante numbers to check
---

# Template content
- Or:
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
    Antes: ${antes}
  # ... etc
```

## Example Template Library

### `JamlTemplates/mechanics/oops_cluster.jaml-template`
```yaml
type: mechanic
description: OopsAll6s jokers in shop slot clusters

- Or:
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [6,7,8]
    score: 100
```

### `JamlTemplates/scenarios/negative_tag_with_jokers.jaml-template`
```yaml
type: scenario
description: NegativeTag skip reward + joker cluster pattern

- And:
  clauses:
  - smallblindtag: NegativeTag
    Antes: ${antes}
  - Or:
    - joker: OopsAll6s
      ShopSlots: [2,3,4]
      score: 100
      Antes: ${antes}
    - joker: OopsAll6s
      ShopSlots: [4,5,6]
      score: 100
      Antes: ${antes}
    - joker: OopsAll6s
      ShopSlots: [6,7,8]
      score: 100
      Antes: ${antes}
```

## Usage in luck3.jaml

```yaml
name: luck2
author: pifreak
deck: Anaglyph
stake: White

Must:
  - soulJoker: Perkeo
    antes: [1,2,3,4,5,6,7,8]

Should:
  - SoulJoker: Perkeo
    score: 10
  
  # Ante 2 - Use template
  - !include scenarios/negative_tag_with_jokers.jaml-template
    parameters:
      antes: [2]
    Mode: Sum
    Score: 100
  
  # Ante 3 - Same template, different antes
  - !include scenarios/negative_tag_with_jokers.jaml-template
    parameters:
      antes: [3]
    Mode: Sum
    Score: 100
```

## Implementation Steps

1. **Create `JamlTemplates/` directory structure**
2. **Add preprocessor to `JamlConfigLoader.cs`**
   - Parse `!include` directives
   - Resolve template paths
   - Inline template content
3. **Support parameter substitution** (optional Phase 2)
4. **Update documentation** with template examples
5. **Create initial template library** with common patterns

## Benefits

- ✅ **DRY (Don't Repeat Yourself)**: Define once, use everywhere
- ✅ **Consistency**: Same pattern = same structure
- ✅ **Maintainability**: Update template, all filters benefit
- ✅ **Discoverability**: Template library shows available patterns
- ✅ **Type Safety**: Templates can be validated independently

## Notes

- **"foo"** = Generic placeholder name (like "bar", "baz") - doesn't mean anything specific
- Templates are **partial JAML** - just clause structures, not full filters
- Include happens **before** YAML parsing - it's a text substitution
- Templates can include other templates (nested includes)
- Circular includes should be detected and prevented

## File Naming Convention

**Recommended**: `.jaml-template`
- Clear and explicit
- Easy to filter in file explorer
- Indicates it's a template, not a filter

**Alternative**: `.jaml-lib` (library)
- Shorter
- Indicates reusable component

**Keep it simple**: Just use `.jaml` in `JamlTemplates/` folder
- No special extension needed
- Folder name makes it clear

---

**Recommendation**: Start with `.jaml-template` extension for clarity, can simplify later if needed.
