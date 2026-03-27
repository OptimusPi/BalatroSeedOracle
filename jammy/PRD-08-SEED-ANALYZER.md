# PRD-08: Seed Analyzer

## Summary

The seed analyzer takes a specific seed and performs deep analysis, showing exactly what the player will encounter at each ante: shop items, pack contents, boss blinds, vouchers, tags, and more. Displays results in an ante-by-ante tree view with detailed run breakdowns.

---

## Current Implementation (Legacy Reference)

| File | Role |
|------|------|
| `Features/Analyzer/AnalyzerView.axaml` | Main analyzer view |
| `Features/Analyzer/SeedAnalysisDisplay.axaml` | Analysis results display |
| `Features/Analyzer/AnteTreeView.axaml` | Ante-by-ante tree view |
| `Features/Analyzer/RunDetailView.axaml` | Run detail display |
| `Views/Modals/AnalyzeModal.axaml` | Analyzer modal wrapper |
| `Windows/AnalyzerWindow.axaml` | Standalone analyzer window |
| `ViewModels/AnalyzeModalViewModel.cs` | Modal state |
| `ViewModels/AnalyzerViewModel.cs` | Analyzer logic/state |
| `Models/AnalysisModels.cs` | Analysis data models |

---

## Requirements

### R1 — Analyzer Input

- Seed input (text field, accept 8-character alphanumeric seeds)
- Deck selector (which deck to analyze with)
- Stake selector (which stake level)
- Ante range (which antes to analyze, default 1-8)
- "Analyze" button to start

### R2 — Analysis Output — Ante Tree View

Hierarchical tree showing each ante's contents:

```
Ante 1
  ├── Shop
  │   ├── Joker: Blueprint (Foil)
  │   ├── Joker: Hack
  │   ├── Tarot: The Fool
  │   └── Planet: Mercury
  ├── Arcana Pack
  │   ├── Tarot: The Magician
  │   └── Tarot: The High Priestess
  ├── Celestial Pack
  │   └── Planet: Jupiter
  ├── Boss Blind: The Wall
  ├── Voucher: Hone
  └── Tag: Uncommon Tag
Ante 2
  ├── ...
```

Each item shows:
- Item name
- Item sprite/icon (via `SpriteService`)
- Edition (if applicable: foil, holo, polychrome, negative)
- Source (shop, pack, boss reward)
- Position in shop/pack

### R3 — Run Detail View

Expanded view for a single ante showing:
- All available items in full detail
- Card sprites at proper scale
- Edition overlays
- Item descriptions/effects
- Scoring potential notes

### R4 — Seed Analysis Display

Summary view showing:
- Seed value
- Deck and stake
- Key highlights (best jokers found, rare editions, etc.)
- Total item counts by category
- Notable combos or synergies detected

### R5 — Analysis Execution

- Analysis runs on background thread
- Progress indicator while computing
- Results cached per seed+deck+stake combo
- Cancel support for long analyses

### R6 — Standalone Window

- Analyzer can pop out to its own window (`AnalyzerWindow`)
- Independent from main modal
- Can be opened from:
  - Main menu ANALYZER button
  - Right-click on search result → "Analyze"
  - `DayLatroWidget` → analyze daily seed
  - Keyboard shortcut (future)

### R7 — Integration Points

- Search results: click a result to analyze that seed
- DayLatro widget: analyze today's daily seed
- Filter builder: preview what a filter would match against a specific seed
- Copy seed to clipboard

---

## Acceptance Criteria

- [ ] Seed input accepts valid Balatro seeds
- [ ] Ante tree view displays all 8 antes with correct items
- [ ] Item sprites render correctly with edition overlays
- [ ] Shop, pack, boss, voucher, tag items all display
- [ ] Analysis runs in background with progress indicator
- [ ] Results are cached for repeated lookups
- [ ] Pop-out window works independently
- [ ] "Analyze" from search results works
- [ ] Copy seed to clipboard works
