# PRD-07: Filter Builder

## Summary

The filter builder is the most complex UI subsystem. It provides three editing modes (visual builder, JAML editor, JSON editor) for constructing seed search filters. Filters define what constitutes a "good" seed (e.g., "has Joker X in ante 1 shop with stake Y"). Includes validation, serialization, import/export, and a custom DSL called JAML.

---

## Current Implementation (Legacy Reference)

### Views & Components (20+ files)

| File | Role |
|------|------|
| `Views/Modals/FiltersModal.axaml` | Main filter management modal |
| `Views/Modals/FilterSelectionModal.axaml` | Filter picker/browser |
| `Views/FilterTabs/FilterTabView.axaml` | Tab container |
| **Filter Builder Tabs:** | |
| `Components/FilterTabs/VisualBuilderTab.axaml` | Drag-and-drop visual builder |
| `Components/FilterTabs/JamlEditorTab.axaml` | JAML code editor |
| `Components/FilterTabs/JsonEditorTab.axaml` | Raw JSON editor |
| `Components/FilterTabs/DeckStakeTab.axaml` | Deck/stake selector |
| `Components/FilterTabs/SaveFilterTab.axaml` | Save/name filter |
| `Components/FilterTabs/ValidateFilterTab.axaml` | Validate filter |
| `Components/FilterTabs/ClauseRowControl.axaml` | Individual clause row |
| `Components/FilterTabs/JamlErrorPanel.axaml` | Syntax errors |
| **Filter Components:** | |
| `Components/FilterSelector.axaml` | Filter selector UI |
| `Components/FilterSelectorControl.axaml` | Selector control |
| `Components/FilterItemCard.axaml` | Card display |
| `Components/FilterItemCarousel.axaml` | Carousel browser |
| `Components/FilterItemConfigRow.axaml` | Per-item config |
| `Components/FilterOperatorControl.axaml` | Operator picker |
| `Components/ItemConfigPanel.axaml` | Item config panel |
| `Components/PaginatedFilterBrowser.axaml` | Paginated browser |
| `Components/Help/JamlHelpView.axaml` | JAML help docs |

### Services (6 files)

| File | Role |
|------|------|
| `Services/FilterService.cs` | CRUD operations |
| `Services/FilterConfigurationService.cs` | Config management |
| `Services/FilterSerializationService.cs` | JSON/JAML serialization |
| `Services/FilterCacheService.cs` | Caching |
| `Services/ClauseConversionService.cs` | Visual ↔ JAML conversion |
| `Services/FavoritesService.cs` | Favorite filters |

### JAML Editor Helpers (5 files)

| File | Role |
|------|------|
| `Helpers/JamlAutocompletionHelper.cs` | Autocomplete suggestions |
| `Helpers/JamlErrorMarkerService.cs` | Syntax error highlighting |
| `Helpers/JamlHoverTooltipService.cs` | Hover tooltips |
| `Helpers/JamlCodeSnippetService.cs` | Code snippets |
| `Helpers/JsonAutocompletionHelper.cs` | JSON autocomplete |

### ViewModels (12 files)

All the FilterTab VMs, FilterSelector VMs, FilterList VMs, etc.

---

## Requirements

### R1 — Filter Data Model

```csharp
public class FilterItem
{
    string Id;
    string Name;
    string Description;
    string Author;
    DateTime CreatedAt;
    DateTime ModifiedAt;
    List<string> Tags;

    // The actual filter definition
    DeckType? Deck;
    StakeType? Stake;
    List<FilterClause> Clauses;  // The filter rules
}

public class FilterClause
{
    string Category;          // "joker", "tarot", "planet", "spectral", "voucher", "tag", "boss"
    string ItemName;          // e.g., "Blueprint", "The Fool"
    FilterOperator Operator;  // >=, <=, ==, !=, contains, etc.
    object Value;             // threshold, count, ante number, etc.
    int? Ante;                // which ante (null = any)
    string? Source;           // "shop", "pack", "boss_blind", etc.
    string? Edition;          // "foil", "holo", "polychrome", "negative"
}
```

### R2 — Three Editor Modes

#### Visual Builder Tab
- Drag-and-drop interface for constructing filters
- Add clauses via category strip (Jokers, Tarots, Planets, etc.)
- Each clause is a `ClauseRowControl` with:
  - Item selector (carousel or dropdown)
  - Operator selector (>=, <=, ==, etc.)
  - Value input (number, text, dropdown)
  - Ante selector (optional)
  - Source selector (optional)
  - Edition selector (optional)
  - Delete button
- Reorder clauses via drag-and-drop
- Real-time validation feedback
- Clause groups (AND/OR logic)

#### JAML Editor Tab
- Full code editor with syntax highlighting (TextMate grammar)
- JAML = custom DSL for filter definitions
- Features:
  - Autocomplete suggestions (`JamlAutocompletionHelper`)
  - Syntax error markers with squiggly underlines (`JamlErrorMarkerService`)
  - Hover tooltips for keywords/items (`JamlHoverTooltipService`)
  - Code snippets (`JamlCodeSnippetService`)
  - Brace folding (`BraceFoldingStrategy`)
  - Error panel showing errors with line numbers
  - "Jump to error" functionality
  - Help view (`JamlHelpView`) with syntax reference
- Bidirectional sync: JAML ↔ visual builder ↔ JSON

#### JSON Editor Tab
- Raw JSON editor with syntax highlighting
- JSON autocomplete (`JsonAutocompletionHelper`)
- Compact formatting (`CompactJsonFormatter`)
- Schema validation against `jaml.schema.json`
- Bidirectional sync with other editors

### R3 — Filter Tabs Workflow

```
[Deck/Stake] → [Visual Builder OR JAML OR JSON] → [Validate] → [Save]
```

| Tab | Purpose |
|-----|---------|
| Deck & Stake | Select deck type and stake level |
| Visual Builder | Drag-and-drop clause construction |
| JAML Editor | Code-based filter editing |
| JSON Editor | Raw JSON editing |
| Validate | Run validation, show errors/warnings |
| Save | Name, tag, and save the filter |

### R4 — Filter Selection Modal

- Browse all saved filters
- Paginated list (`PaginatedFilterBrowser`)
- Search/filter by name, tag, author
- Preview filter details
- Select filter for use in search
- Delete filters
- Import/export filters
- Favorite filters (star toggle)

### R5 — Filter Item Display Components

- `FilterItemCard` — compact card showing filter item with icon/sprite
- `FilterItemCarousel` — horizontal scrolling carousel of items in a category
- `FilterItemDisplay` — detailed item display with all properties
- `FilterCategoryStrip` — horizontal strip of category buttons for navigation
- `ItemConfigPanel` — expanded configuration for a selected item
- `FilterOperatorControl` — dropdown for comparison operators

### R6 — Serialization

- `FilterSerializationService`:
  - Serialize to/from JSON
  - Serialize to/from JAML
  - Import filters from file
  - Export filters to file
- `ClauseConversionService`:
  - Convert visual builder clauses → JAML
  - Convert JAML → visual builder clauses
  - Convert between all three representations
- Saved filters stored in `JamlFilters/` directory

### R7 — Validation

- Validate filter structure (required fields, valid operators)
- Validate item names (exist in game data)
- Validate ante ranges (1-8)
- Validate edition compatibility
- Show errors and warnings separately
- Block save if critical errors exist

### R8 — Caching

- `FilterCacheService` caches parsed filters
- Invalidate on edit
- Shared across search and editor

### R9 — Genie Widget (AI Filter Generation)

- `GenieWidget` — AI-powered filter generation
- User describes desired seed in natural language
- AI generates JAML/JSON filter
- Review and edit before saving

---

## Acceptance Criteria

- [ ] Visual builder allows adding/removing/reordering clauses
- [ ] All filter operators work (>=, <=, ==, !=, contains)
- [ ] JAML editor has autocomplete, error marking, tooltips, snippets
- [ ] JSON editor has autocomplete and formatting
- [ ] All three editors stay in sync (edit in one, reflected in others)
- [ ] Deck/stake selection persists with filter
- [ ] Validation catches errors and blocks invalid saves
- [ ] Filters save/load to disk correctly
- [ ] Filter browser supports search, pagination, favorites
- [ ] Import/export works for sharing filters
- [ ] JAML help view documents the DSL syntax
