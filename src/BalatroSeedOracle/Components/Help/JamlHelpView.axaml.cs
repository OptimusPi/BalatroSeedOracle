using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components.Help
{
    /// <summary>
    /// JAML help view displaying markdown content.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class JamlHelpView : UserControl
    {
        public JamlHelpView()
        {
            InitializeComponent();
            LoadJamlHelp();
        }

        private void LoadJamlHelp()
        {
            try
            {
                // Load JAML help markdown
                var helpContent = GetJamlHelpMarkdown();
                // Direct x:Name field access - MarkdownViewer is a TextBlock in AXAML
                if (MarkdownViewer != null && !string.IsNullOrEmpty(helpContent))
                {
                    MarkdownViewer.Text = helpContent;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JamlHelpView", $"Failed to load JAML help: {ex.Message}");
            }
        }

        private string GetJamlHelpMarkdown()
        {
            return @"# JAML (Joker Ante Markup Language) Help

## Overview

JAML is a YAML-based filter format for searching Balatro seeds. It allows you to define complex search criteria using a simple, readable syntax.

## Basic Structure

```yaml
name: My Filter
antes: [1, 2, 3, 4, 5, 6, 7, 8]
must:
  - type: joker
    name: Blueprint
    min: 1
```

## Filter Types

### Joker Filters
```yaml
must:
  - type: joker
    name: Blueprint
    min: 1
```

### Tag Filters
```yaml
must:
  - type: tag
    name: NegativeTag
    min: 1
```

### Voucher Filters
```yaml
must:
  - type: voucher
    name: Overstock
    min: 1
```

## Antes

Specify which antes to search:
```yaml
antes: [1, 2, 3, 4, 5, 6, 7, 8]  # All antes
antes: [1, 2, 3]                  # First 3 antes only
```

## Logical Operators

### AND (All conditions must match)
```yaml
must:
  - type: joker
    name: Blueprint
  - type: joker
    name: Brainstorm
```

### OR (Any condition can match)
```yaml
should:
  - type: joker
    name: Blueprint
  - type: joker
    name: Brainstorm
```

### NOT (Exclude these)
```yaml
mustNot:
  - type: joker
    name: OopsAll6s
```

## YAML Anchors & Aliases

Reuse filter definitions:
```yaml
base_joker: &base
  type: joker
  min: 1

must:
  - <<: *base
    name: Blueprint
  - <<: *base
    name: Brainstorm
```

## Scoring

Define how results are scored:
```yaml
mode: sum  # Options: sum, max, min, avg
```

## Examples

See `JamlFilters/*.jaml` for complete examples.

## Tips

- Use YAML anchors to avoid repetition
- Combine `must`, `should`, and `mustNot` for complex filters
- Specify `antes` to limit search scope
- Use `min` to require multiple occurrences
";
        }
    }
}
