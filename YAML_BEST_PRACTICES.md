# YAML Best Practices for JAML Filters

## Table of Contents
1. [Indentation](#indentation)
2. [Anchors & Aliases](#anchors--aliases)
3. [Quoting Strings](#quoting-strings)
4. [Arrays](#arrays)
5. [Common Pitfalls](#common-pitfalls)
6. [JAML-Specific Tips](#jaml-specific-tips)

---

## Indentation

### ✅ DO: Use Consistent Spaces
```yaml
# Good: 2 spaces per level (JAML standard)
should:
  - joker: Blueprint
    antes: [1, 2]
    score: 10
```

### ❌ DON'T: Mix Tabs and Spaces
```yaml
# Bad: Tabs will cause parsing errors
should:
	- joker: Blueprint  # Tab instead of spaces
    antes: [1, 2]      # Spaces - inconsistent!
```

**Rule**: Always use **2 spaces** per indentation level in JAML files.

---

## Anchors & Aliases

### ✅ DO: Use Anchors for Reusable Values
```yaml
# Define parameter at top
desired_joker: &desired_joker OopsAll6s
score_per_joker: &score_per_joker 100

# Use in template
should:
  - joker: *desired_joker
    score: *score_per_joker
```

### ✅ DO: Use Anchors for Complex Patterns
```yaml
# Define cluster pattern once
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100

# Reuse it
should:
  - And:
    Antes: [2,3,4,5,6,7,8,9,10,11,12]
    clauses: *oops_cluster
```

### ✅ DO: Leverage Antes Inheritance
```yaml
# Parent And clause has antes array - children inherit automatically!
should:
  - And:
    Antes: [2,3,4,5,6,7,8,9,10,11,12]  # Define once
    clauses:
      - smallblindtag: NegativeTag  # Inherits Antes automatically
      - Or: *oops_cluster  # Each joker inherits Antes automatically
```

**Benefits**:
- No repetition needed
- Change once, updates everywhere
- Cleaner, more maintainable filters

---

## Quoting Strings

### ✅ DO: Quote Strings with Special Characters
```yaml
# Good: Quote strings with colons, dashes, or special chars
name: "My Filter: Perkeo Edition"
description: "Filter for - wait, what?"
```

### ✅ DO: Quote Numbers That Should Be Strings
```yaml
# Good: Leading zeros should be quoted
version: "01.02.03"  # String, not octal number
```

### ❌ DON'T: Quote Unnecessarily
```yaml
# Bad: Unnecessary quotes
joker: "Blueprint"  # Blueprint doesn't need quotes
antes: "[1, 2, 3]"   # Arrays don't need quotes (this breaks parsing!)

# Good: No quotes needed
joker: Blueprint
antes: [1, 2, 3]
```

---

## Arrays

### ✅ DO: Use Inline Arrays for Short Lists
```yaml
# Good: Inline for short arrays
antes: [1, 2, 3, 4]
ShopSlots: [0, 1, 2]
```

### ✅ DO: Use Multi-line Arrays for Long Lists
```yaml
# Good: Multi-line for readability
antes:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
```

### ❌ DON'T: Mix Styles Inconsistently
```yaml
# Bad: Inconsistent array style
should:
  - joker: Blueprint
    antes: [1, 2]  # Inline
  - joker: Brainstorm
    antes:         # Multi-line - inconsistent!
      - 3
      - 4
```

---

## Common Pitfalls

### 1. **Leading Zeros in Numbers**
```yaml
# ❌ Bad: Leading zero = octal number (parses as 8, not 10!)
ante: 010

# ✅ Good: Quote it or remove leading zero
ante: 10
# or
ante: "010"  # If you really need the leading zero
```

### 2. **Boolean Values**
```yaml
# ❌ Bad: These are strings, not booleans
enabled: "true"
disabled: "false"

# ✅ Good: Use actual booleans (if schema supports)
enabled: true
disabled: false
```

### 3. **Null Values**
```yaml
# ✅ Good: Use null (not "null" string)
optional_field: null

# ❌ Bad: This is a string, not null
optional_field: "null"
```

### 4. **Trailing Commas**
```yaml
# ❌ Bad: YAML doesn't support trailing commas
antes: [1, 2, 3,]  # Syntax error!

# ✅ Good: No trailing comma
antes: [1, 2, 3]
```

### 5. **Inconsistent Indentation**
```yaml
# ❌ Bad: Mixed indentation levels
should:
  - joker: Blueprint
     antes: [1, 2]  # Wrong indentation!

# ✅ Good: Consistent 2-space indentation
should:
  - joker: Blueprint
    antes: [1, 2]
```

---

## JAML-Specific Tips

### 1. **Use Antes Inheritance**
Instead of repeating `Antes: [2]` on every joker:
```yaml
# ❌ Bad: Repetitive
should:
  - And:
    clauses:
      - joker: OopsAll6s
        Antes: [2]  # Repeated
      - joker: OopsAll6s
        Antes: [2]  # Repeated again
```

```yaml
# ✅ Good: Parent antes inherited
should:
  - And:
    Antes: [2]  # Define once
    clauses:
      - joker: OopsAll6s  # Inherits Antes: [2]
      - joker: OopsAll6s   # Inherits Antes: [2]
```

### 2. **Use Anchors for Parameters**
```yaml
# ✅ Good: Define parameters at top
desired_joker: &desired_joker OopsAll6s
score_per_joker: &score_per_joker 100

# Use throughout
should:
  - joker: *desired_joker
    score: *score_per_joker
```

### 3. **Use Anchors for Complex Patterns**
```yaml
# ✅ Good: Define pattern once, reuse everywhere
oops_cluster: &oops_cluster
  - joker: OopsAll6s
    ShopSlots: [2,3,4]
    score: 100
  - joker: OopsAll6s
    ShopSlots: [4,5,6]
    score: 100

should:
  - And:
    Antes: [2]
    clauses:
      - smallblindtag: NegativeTag
      - Or: *oops_cluster  # Reuse the pattern
```

### 4. **Comments for Clarity**
```yaml
# ✅ Good: Use comments to explain complex logic
# Ante 2 - Score based on NegativeTag Skip Reward + Desired jokers in shop cluster
should:
  - And:
    Antes: [2]
    Mode: Max
    clauses:
      - smallblindtag: NegativeTag  # Inherits Antes from parent
      - Or: *oops_cluster  # Each joker inherits Antes from parent
```

---

## Editor Features

### Autocomplete (Ctrl+Space)
- **Property names**: Type `j` → suggests `joker:`
- **Joker values**: Type `joker: ` → suggests all jokers
- **Anchor references**: Type `*` → suggests defined anchors
- **Anchor definitions**: Type `: &` → suggests anchor patterns

### Schema Validation
- Real-time validation against `jaml.schema.json`
- Error highlighting for invalid properties
- Type checking for enum values (deck, stake, edition, etc.)

### Code Folding
- Fold/unfold arrays and objects
- Collapse long filter definitions
- Navigate large files easily

---

## Quick Reference

| Feature | Syntax | Example |
|---------|--------|---------|
| **Anchor Definition** | `key: &anchor_name value` | `joker: &j OopsAll6s` |
| **Anchor Reference** | `*anchor_name` | `joker: *j` |
| **Merge Key** | `<<: *anchor_name` | `<<: *base_clause` |
| **Array (inline)** | `[item1, item2]` | `antes: [1, 2, 3]` |
| **Array (multi-line)** | `- item1\n- item2` | `antes:\n  - 1\n  - 2` |
| **Comment** | `# comment` | `# This is a comment` |
| **Null** | `null` | `optional: null` |
| **Boolean** | `true` / `false` | `enabled: true` |

---

## Resources

- **JAML Schema**: `jaml.schema.json` - Full schema definition
- **YAML Spec**: https://yaml.org/spec/1.2.2/
- **YAML Anchors**: https://yaml.org/spec/1.2.2/#anchors-and-aliases
- **YAML Merge Keys**: https://yaml.org/type/merge.html

---

**Last Updated**: 2025-01-XX  
**Version**: 1.0
