# YAML Anchors & Aliases - Detailed Implementation Plan for Avalonia UI Visual Builder

## Overview

This document provides a detailed, step-by-step implementation plan for adding YAML anchors and aliases support to the Avalonia UI Visual Builder. The parser already supports anchors, so this is purely a front-end UI implementation.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              Avalonia UI Visual Builder                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────┐ │
│  │   JAML       │───▶│   Anchor     │───▶│ Template │ │
│  │   Parser     │    │   Detector   │    │  Panel   │ │
│  └──────────────┘    └──────────────┘    └──────────┘ │
│         │                   │                  │        │
│         ▼                   ▼                  ▼        │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────┐ │
│  │   Config     │    │   Anchor     │    │  Clause  │ │
│  │   Object     │    │   Metadata   │    │   Tree   │ │
│  └──────────────┘    └──────────────┘    └──────────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## Phase 1: Display Support (Read-Only)

### Goal
Allow users to **view** anchors and aliases in the Visual Builder, but not edit them yet.

### Tasks

#### 1.1 Create Anchor Detection Service

**File**: `src/BalatroSeedOracle/Services/YamlAnchorService.cs`

```csharp
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service for detecting and managing YAML anchors and aliases
    /// </summary>
    public class YamlAnchorService
    {
        /// <summary>
        /// Represents an anchor definition in YAML
        /// </summary>
        public class AnchorDefinition
        {
            public string Name { get; set; } = "";
            public YamlNode Node { get; set; } = null!;
            public string Preview { get; set; } = "";
            public int UsageCount { get; set; } = 0;
        }

        /// <summary>
        /// Represents an alias reference
        /// </summary>
        public class AliasReference
        {
            public string AnchorName { get; set; } = "";
            public YamlNode Node { get; set; } = null!;
            public string Path { get; set; } = ""; // YAML path like "Should[0].clauses[1]"
        }

        /// <summary>
        /// Parse YAML and extract all anchor definitions
        /// </summary>
        public static Dictionary<string, AnchorDefinition> ExtractAnchors(string jamlContent)
        {
            var anchors = new Dictionary<string, AnchorDefinition>();
            
            try
            {
                var yamlStream = new YamlStream();
                using (var reader = new StringReader(jamlContent))
                {
                    yamlStream.Load(reader);
                }

                TraverseForAnchors(yamlStream.Documents[0].RootNode, anchors, "");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("YamlAnchorService", $"Error extracting anchors: {ex.Message}");
            }

            return anchors;
        }

        /// <summary>
        /// Find all alias references to a specific anchor
        /// </summary>
        public static List<AliasReference> FindAliasReferences(string jamlContent, string anchorName)
        {
            var references = new List<AliasReference>();
            
            try
            {
                var yamlStream = new YamlStream();
                using (var reader = new StringReader(jamlContent))
                {
                    yamlStream.Load(reader);
                }

                TraverseForAliases(yamlStream.Documents[0].RootNode, references, anchorName, "");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("YamlAnchorService", $"Error finding alias references: {ex.Message}");
            }

            return references;
        }

        private static void TraverseForAnchors(
            YamlNode node, 
            Dictionary<string, AnchorDefinition> anchors, 
            string path)
        {
            if (node is YamlScalarNode scalar && scalar.Anchor != null)
            {
                anchors[scalar.Anchor] = new AnchorDefinition
                {
                    Name = scalar.Anchor,
                    Node = scalar,
                    Preview = scalar.Value ?? "",
                    UsageCount = 0 // Will be calculated separately
                };
            }
            else if (node is YamlMappingNode mapping)
            {
                foreach (var pair in mapping.Children)
                {
                    var childPath = string.IsNullOrEmpty(path) 
                        ? pair.Key.ToString() 
                        : $"{path}.{pair.Key}";
                    TraverseForAnchors(pair.Value, anchors, childPath);
                }
            }
            else if (node is YamlSequenceNode sequence)
            {
                for (int i = 0; i < sequence.Children.Count; i++)
                {
                    TraverseForAnchors(sequence.Children[i], anchors, $"{path}[{i}]");
                }
            }
        }

        private static void TraverseForAliases(
            YamlNode node,
            List<AliasReference> references,
            string targetAnchorName,
            string path)
        {
            if (node is YamlAliasNode alias && alias.Value.Anchor == targetAnchorName)
            {
                references.Add(new AliasReference
                {
                    AnchorName = targetAnchorName,
                    Node = alias,
                    Path = path
                });
            }
            else if (node is YamlMappingNode mapping)
            {
                foreach (var pair in mapping.Children)
                {
                    var childPath = string.IsNullOrEmpty(path) 
                        ? pair.Key.ToString() 
                        : $"{path}.{pair.Key}";
                    TraverseForAliases(pair.Value, references, targetAnchorName, childPath);
                }
            }
            else if (node is YamlSequenceNode sequence)
            {
                for (int i = 0; i < sequence.Children.Count; i++)
                {
                    TraverseForAliases(sequence.Children[i], references, targetAnchorName, $"{path}[{i}]");
                }
            }
        }
    }
}
```

#### 1.2 Add Template Panel to Visual Builder

**File**: `src/BalatroSeedOracle/Components/FilterTabs/TemplatesPanel.axaml`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="BalatroSeedOracle.Components.FilterTabs.TemplatesPanel">
    <Border Background="{StaticResource EditorDarkBg}"
            BorderThickness="0,0,1,0"
            BorderBrush="{StaticResource EditorBorderBg}"
            Padding="12">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Header -->
            <StackPanel Grid.Row="0" Spacing="8" Margin="0,0,0,12">
                <TextBlock Text="Templates" 
                          FontFamily="{StaticResource BalatroFont}"
                          FontSize="16"
                          Foreground="{StaticResource Gold}"/>
                <TextBlock Text="Reusable clause patterns"
                          FontSize="11"
                          Foreground="{StaticResource TextSecondary}"/>
            </StackPanel>

            <!-- Template List -->
            <ListBox Grid.Row="1"
                     ItemsSource="{Binding Templates}"
                     SelectedItem="{Binding SelectedTemplate}">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Background="{StaticResource EditorHeaderBg}"
                                CornerRadius="6"
                                Padding="8"
                                Margin="0,0,0,6">
                            <StackPanel Spacing="4">
                                <StackPanel Orientation="Horizontal" Spacing="6">
                                    <TextBlock Text="&amp;"
                                              FontFamily="{StaticResource BalatroFont}"
                                              FontSize="12"
                                              Foreground="{StaticResource Gold}"/>
                                    <TextBlock Text="{Binding Name}"
                                              FontSize="12"
                                              FontWeight="Bold"/>
                                    <TextBlock Text="{Binding UsageCount, StringFormat='({0} uses)'}"
                                              FontSize="10"
                                              Foreground="{StaticResource TextSecondary}"/>
                                </StackPanel>
                                <TextBlock Text="{Binding Preview}"
                                          FontSize="10"
                                          Foreground="{StaticResource TextSecondary}"
                                          TextWrapping="Wrap"
                                          MaxHeight="40"
                                          TextTrimming="CharacterEllipsis"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Grid>
    </Border>
</UserControl>
```

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/TemplatesPanelViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public partial class TemplatesPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TemplateViewModel> _templates = new();

        [ObservableProperty]
        private TemplateViewModel? _selectedTemplate;

        public void LoadTemplates(string jamlContent)
        {
            Templates.Clear();

            var anchors = YamlAnchorService.ExtractAnchors(jamlContent);
            
            foreach (var anchor in anchors.Values)
            {
                // Count usage
                var references = YamlAnchorService.FindAliasReferences(jamlContent, anchor.Name);
                anchor.UsageCount = references.Count;

                Templates.Add(new TemplateViewModel
                {
                    Name = anchor.Name,
                    Preview = anchor.Preview,
                    UsageCount = anchor.UsageCount
                });
            }
        }
    }

    public partial class TemplateViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _preview = "";

        [ObservableProperty]
        private int _usageCount = 0;
    }
}
```

#### 1.3 Integrate Template Panel into Visual Builder

**File**: `src/BalatroSeedOracle/Components/FilterTabs/VisualBuilderTab.axaml`

Add to the Grid layout (left sidebar):

```xml
<!-- Add after existing left panel -->
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="200"/>  <!-- NEW: Templates Panel -->
    <ColumnDefinition Width="*"/>   <!-- Main content -->
    <ColumnDefinition Width="320"/> <!-- Properties panel -->
</Grid.ColumnDefinitions>

<!-- Templates Panel -->
<components:TemplatesPanel Grid.Column="0"
                           DataContext="{Binding TemplatesPanel}"/>
```

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs`

Add property:

```csharp
[ObservableProperty]
private TemplatesPanelViewModel _templatesPanel = new();

// In constructor or LoadFromParentCollections:
private void LoadAnchors()
{
    if (_parentViewModel?.JamlEditorTab is JamlEditorTabViewModel jamlVm)
    {
        TemplatesPanel.LoadTemplates(jamlVm.JamlContent);
    }
}
```

#### 1.4 Show Anchor/Alias Indicators in Clause Tree

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/ClauseRowViewModel.cs`

Add properties:

```csharp
[ObservableProperty]
private bool _isAnchorDefinition = false;

[ObservableProperty]
private string _anchorName = "";

[ObservableProperty]
private bool _isAliasReference = false;

[ObservableProperty]
private string _referencedAnchorName = "";
```

Update clause tree rendering to show icons for anchors/aliases.

### Testing Checklist for Phase 1

- [ ] Load JAML with anchors - templates panel shows all anchors
- [ ] Load JAML with aliases - clause tree shows alias indicators
- [ ] Click anchor in template panel - highlights references in clause tree
- [ ] Click alias in clause tree - jumps to anchor definition
- [ ] Usage count shows correct number of references
- [ ] Preview shows truncated template content

## Phase 2: Basic Editing Support

### Goal
Allow users to **create** anchors from selections and **edit** anchor templates.

### Tasks

#### 2.1 Create Anchor from Selection

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs`

```csharp
[RelayCommand]
private async Task CreateAnchorFromSelection()
{
    // Get selected clause(s) from visual builder
    var selectedItems = GetSelectedClauses();
    if (selectedItems.Count == 0)
    {
        // Show error: "Please select clauses to create a template"
        return;
    }

    // Show dialog to name the anchor
    var anchorName = await ShowAnchorNameDialog();
    if (string.IsNullOrWhiteSpace(anchorName))
        return;

    // Serialize selected clauses to YAML
    var yaml = SerializeClausesToYaml(selectedItems);
    
    // Add anchor marker
    var anchoredYaml = AddAnchorToYaml(yaml, anchorName);
    
    // Replace original with alias reference
    var aliasYaml = $"*{anchorName}";
    
    // Update JAML content
    await UpdateJamlWithAnchor(anchoredYaml, aliasYaml, selectedItems);
    
    // Refresh template panel
    LoadAnchors();
}
```

#### 2.2 Edit Anchor Template

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/TemplatesPanelViewModel.cs`

```csharp
[RelayCommand]
private async Task EditTemplate(TemplateViewModel template)
{
    // Load anchor definition from JAML
    var anchorYaml = ExtractAnchorYaml(template.Name);
    
    // Open editor (reuse clause editor)
    var editedYaml = await ShowTemplateEditor(anchorYaml);
    
    // Update all references
    await UpdateAnchorDefinition(template.Name, editedYaml);
    
    // Refresh template panel and clause tree
    LoadTemplates(_jamlContent);
}
```

#### 2.3 Expand Alias to Inline

**File**: `src/BalatroSeedOracle/ViewModels/FilterTabs/VisualBuilderTabViewModel.cs`

```csharp
[RelayCommand]
private async Task ExpandAlias(ClauseRowViewModel aliasRow)
{
    // Get anchor definition
    var anchorYaml = GetAnchorDefinition(aliasRow.ReferencedAnchorName);
    
    // Replace alias with full clause structure
    await ReplaceAliasWithInline(aliasRow, anchorYaml);
    
    // Refresh display
    LoadAnchors();
}
```

### Testing Checklist for Phase 2

- [ ] Create anchor from selection - works correctly
- [ ] Edit anchor - all references update
- [ ] Expand alias - replaces with inline structure
- [ ] Delete anchor - shows confirmation if in use
- [ ] Invalid anchor name - shows error
- [ ] Round-trip: Save and reload preserves anchors

## Phase 3: Advanced Features

### Goal
Support parameterized templates with merge keys and template library.

### Tasks

#### 3.1 Parameterized Templates (Merge Keys)

**File**: `src/BalatroSeedOracle/Services/YamlAnchorService.cs`

Add method to detect merge keys:

```csharp
public static bool IsMergeKey(YamlNode node)
{
    if (node is YamlMappingNode mapping)
    {
        return mapping.Children.Any(pair => 
            pair.Key is YamlScalarNode key && 
            key.Value == "<<" &&
            pair.Value is YamlAliasNode);
    }
    return false;
}
```

#### 3.2 Template Library

Create template library system:
- Pre-defined templates users can insert
- Common joker combinations
- Standard ante patterns

#### 3.3 Template Validation

- Validate anchor structure
- Check for circular references
- Warn about invalid templates

### Testing Checklist for Phase 3

- [ ] Parameter override works with merge keys
- [ ] Template library shows available templates
- [ ] Insert template from library works
- [ ] Circular reference detected
- [ ] Template structure validation works

## Integration Points

### Files to Modify

1. **VisualBuilderTabViewModel.cs**
   - Add `TemplatesPanel` property
   - Add `LoadAnchors()` method
   - Add anchor creation/editing commands

2. **VisualBuilderTab.axaml**
   - Add Templates Panel to layout
   - Add anchor/alias indicators to clause tree

3. **JamlEditorTabViewModel.cs**
   - Add anchor detection when loading JAML
   - Preserve anchors when saving

4. **ClauseRowViewModel.cs**
   - Add anchor/alias properties
   - Add visual indicators

### New Files to Create

1. `Services/YamlAnchorService.cs` - Anchor detection service
2. `ViewModels/FilterTabs/TemplatesPanelViewModel.cs` - Template panel ViewModel
3. `Components/FilterTabs/TemplatesPanel.axaml` - Template panel UI

## Timeline Estimate

- **Phase 1**: 3-5 days (read-only display)
- **Phase 2**: 5-7 days (basic editing)
- **Phase 3**: 7-10 days (advanced features)

**Total**: 15-22 days for full implementation

## Dependencies

- ✅ YamlDotNet (already in use)
- ✅ Parser support (already working)
- ⚠️ JamlFormatter anchor preservation (may need parser team help)

## Success Criteria

1. Users can view all anchors in template panel
2. Users can see alias references in clause tree
3. Users can create anchors from selections
4. Users can edit anchor templates
5. Users can expand aliases to inline
6. Round-trip preservation works (save/reload)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-XX  
**For**: Avalonia UI Visual Builder Implementation
