# Standard Playing Card UX - Product Requirements Document

**Date**: 2025-11-02
**Feature**: StandardCard Category in Visual Builder
**Status**: NOT IMPLEMENTED (Stub exists at line 433-436 in VisualBuilderTabViewModel.cs)
**Priority**: HIGH - Missing core filter functionality

---

## 🎯 Executive Summary

The Visual Builder currently has a **stub** for StandardCard category showing "PLAYING CARDS (empty)". This PRD defines how users should interact with all 52+ standard playing cards, organized by suit and enhancement type, with proper sprite compositing.

**Current State**: Line 433-436 in `VisualBuilderTabViewModel.cs`:
```csharp
case "StandardCard":
    // Playing cards logic (to be implemented later)
    AddGroup("PLAYING CARDS", new List<FilterItem>());
    break;
```

---

## 📋 Requirements Overview

### Card Organization Structure

Users scrolling through StandardCard category should see:

```
[Hearts]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Spades]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Diamonds]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Clubs]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Mult]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Bonus]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Glass]
Ace 2 3 4 5
6 7 8 9 10
Jack Queen King

[Stone]
Stone Card (no rank/suit)
```

---

## 🃏 Card Type Classifications

### Type A: Normal Standard Cards
**Examples**: 5 of Spades, King of Hearts, Ace of Diamonds

**Visual Composition**:
1. **Base Layer**: BlankCard sprite (white background)
2. **Overlay Layer**: Rank + Suit pattern (transparent PNG)

**Implementation**: `GetPlayingCardImage(suit, rank, enhancement: null)`

**Current Code** (Lines 1183-1230 in `SpriteService.cs`):
```csharp
// Start with base card or enhancement
IImage? baseCard = null;
if (!string.IsNullOrEmpty(enhancement))
{
    baseCard = GetEnhancementImage(enhancement);
}
else
{
    // Use blank card as base
    baseCard = GetSpecialImage("BlankCard");  // ✅ This works!
}
```

---

### Type B1: Enhanced Standard Cards (Glass, Gold, Stone)
**Examples**: Glass King of Clubs, Gold Ace of Hearts, Steel 5 of Spades

**Visual Composition**:
1. **Base Layer**: Enhancement sprite (Glass/Gold/Stone/Steel background)
2. **Overlay Layer**: Rank + Suit pattern (transparent PNG)

**Implementation**: `GetPlayingCardImage(suit, rank, enhancement: "Glass")`

**Enhancement Sprites Available** (From `enhancers_metadata.json`):
- Glass: { x: 5, y: 1 }
- Gold: { x: 7, y: 0 }
- Steel: { x: 6, y: 1 }
- Stone: { x: 6, y: 0 } ⚠️ **SPECIAL CASE - No rank/suit!**

---

### Type B2: Enhanced Standard Cards (Mult, Bonus)
**Examples**: Mult King of Clubs, Bonus 7 of Diamonds

**Visual Composition**:
1. **Base Layer**: BlankCard sprite (white background)
2. **Middle Layer**: Rank + Suit pattern (transparent PNG)
3. **Top Layer**: Mult/Bonus glyph overlay (additional transparent PNG)

**Implementation**: Requires **three-layer composite**

**Enhancement Sprites Available**:
- Mult: { x: 2, y: 1 }
- Bonus: { x: 1, y: 1 }

**⚠️ NOTE**: These are rendered differently! Mult/Bonus have a "glyph" that overlays on top, not replaces the background.

---

### Type C: Enchanted Cards (with Editions)
**Examples**: Foil 5 of Spades, Holographic Glass King, Polychrome Mult Ace

**Visual Composition**:
1. Base + Overlay (from Type A/B1/B2)
2. **Edition Shader**: Foil/Holographic/Polychrome/Negative effect

**Implementation Options**:
- **MVP**: Use PNG overlays if available
- **Future**: Implement shader effects (requires Avalonia SkiaSharp research)

**Editions Available** (Handled in ItemConfigPopup):
- Normal (no effect)
- Foil
- Holographic
- Polychrome
- Negative

---

## 🎨 UI Layout Requirements

### Grouping Pattern (Like Jokers)

**Reference Implementation** (Lines 395-410 in `VisualBuilderTabViewModel.cs`):
```csharp
case "Joker":
    // Add groups: Legendary, Rare, Uncommon, Common
    AddGroup("LEGENDARY JOKERS", FilteredJokers.Where(j => j.Type == "SoulJoker"));
    AddGroup("RARE JOKERS", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Rare"));
    AddGroup("UNCOMMON JOKERS", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Uncommon"));
    AddGroup("COMMON JOKERS", FilteredJokers.Where(j => j.Type == "Joker" && j.Category == "Common"));
    break;
```

**StandardCard Implementation Should Be**:
```csharp
case "StandardCard":
    AddGroup("HEARTS", GetStandardCards("Hearts"));
    AddGroup("SPADES", GetStandardCards("Spades"));
    AddGroup("DIAMONDS", GetStandardCards("Diamonds"));
    AddGroup("CLUBS", GetStandardCards("Clubs"));
    AddGroup("MULT CARDS", GetEnhancedCards("Mult"));
    AddGroup("BONUS CARDS", GetEnhancedCards("Bonus"));
    AddGroup("GLASS CARDS", GetEnhancedCards("Glass"));
    AddGroup("STONE CARD", GetStoneCard());
    break;
```

---

## 🗂️ Data Sources

### Playing Cards Metadata
**File**: `src/Assets/Decks/playing_cards_metadata.json`

**Structure**:
```json
{
  "spriteSheet": "8BitDeck.png",
  "spriteWidth": 142,
  "spriteHeight": 190,
  "columns": 13,
  "rows": 4,
  "sprites": {
    "Hearts": { "2": { "x": 0, "y": 0 }, ... },
    "Clubs": { "2": { "x": 0, "y": 1 }, ... },
    "Diamonds": { "x": 0, "y": 2 }, ... },
    "Spades": { "x": 0, "y": 3 }, ... }
  }
}
```

**Ranks Order**: 2, 3, 4, 5, 6, 7, 8, 9, 10, Jack, Queen, King, Ace

---

### Enhancement Metadata
**File**: `src/Assets/Decks/enhancers_metadata.json`

**Enhancements Section** (Lines 26-35):
```json
"enhancements": {
  "Bonus": { "x": 1, "y": 1 },
  "Mult": { "x": 2, "y": 1 },
  "Wild": { "x": 3, "y": 1 },
  "Lucky": { "x": 4, "y": 1 },
  "Glass": { "x": 5, "y": 1 },
  "Steel": { "x": 6, "y": 1 },
  "Stone": { "x": 6, "y": 0 },
  "Gold": { "x": 7, "y": 0 }
}
```

**Special Cards Section** (Lines 42-50):
```json
"special": {
  "BlankCard": { "x": 1, "y": 0, "description": "Base card template" },
  "TheSoulGem": { "x": 0, "y": 1 },
  "LockedDeckPlaceholder": { "x": 5, "y": 0 }
}
```

---

## 🛠️ Implementation Tasks

### Phase 1: Data Layer

- [ ] **Create StandardCard Data Model**
  - FilterItem subclass or configuration
  - Properties: Suit, Rank, Enhancement, Seal, Edition
  - DisplayName generation (e.g., "5 of Spades", "Glass King")

- [ ] **Generate Card List**
  - Parse `playing_cards_metadata.json`
  - Create 52 base cards (4 suits × 13 ranks)
  - Add enhanced variants (Mult/Bonus/Glass × 13 ranks = 39 more)
  - Add Stone card (special case)
  - **Total**: ~92 card variants

- [ ] **Add to VisualBuilderTabViewModel**
  - `AllStandardCards` ObservableCollection
  - `FilteredStandardCards` for search
  - Hook into `RebuildGroupedItems()` switch case

---

### Phase 2: Sprite Compositing (SpriteService)

#### Task 2A: Fix GetPlayingCardImage() Compositing

**Current Bug** (Line 1220 in SpriteService.cs):
```csharp
// For now, return the pattern overlay as the primary visual
return cardPattern;  // ❌ WRONG - Returns overlay only!
```

**Required Fix**: Implement actual compositing using `CompositeImages()`

**Reference Pattern** (Lines 1123-1180 in SpriteService.cs):
```csharp
private IImage? CompositeImages(IImage baseImage, IImage overlayImage)
{
    // Create RenderTargetBitmap
    // Draw base, then overlay
    // Return composited image
}
```

**New Implementation Needed**:
```csharp
public IImage? GetPlayingCardImage(string suit, string rank, string? enhancement = null)
{
    // 1. Get base (BlankCard or Enhancement sprite)
    IImage? baseCard = !string.IsNullOrEmpty(enhancement)
        ? GetEnhancementImage(enhancement)
        : GetSpecialImage("BlankCard");

    if (baseCard == null) return null;

    // 2. Get card pattern overlay
    var cardPattern = GetPlayingCardPattern(suit, rank);
    if (cardPattern == null) return baseCard;

    // 3. Composite them together
    return CompositeImages(baseCard, cardPattern);  // ✅ CORRECT
}
```

#### Task 2B: Handle Type B2 (Mult/Bonus) Three-Layer Composite

**New Method Needed**:
```csharp
public IImage? GetMultBonusCard(string suit, string rank, string enhancementType)
{
    // Layer 1: BlankCard
    var baseCard = GetSpecialImage("BlankCard");

    // Layer 2: Rank + Suit pattern
    var cardPattern = GetPlayingCardPattern(suit, rank);
    var intermediate = CompositeImages(baseCard, cardPattern);

    // Layer 3: Mult/Bonus glyph
    var glyph = GetEnhancementGlyph(enhancementType);  // NEW METHOD
    return CompositeImages(intermediate, glyph);
}
```

**⚠️ QUESTION FOR BALATRO SOURCE**: Where are the Mult/Bonus glyphs stored? Are they separate sprites or part of enhancement sheet?

#### Task 2C: Stone Card Special Case

**Stone Card Rules**:
- No rank
- No suit
- Just shows Stone enhancement sprite
- Filter config: `{ "ItemKey": "Stone", "ItemType": "StandardCard", "Rank": null, "Suit": null }`

**Implementation**:
```csharp
if (enhancement == "Stone")
{
    return GetEnhancementImage("Stone");  // Just return enhancement, no overlay
}
```

---

### Phase 3: Item Configuration Popup

**Current Popup Sections** (Lines 45-164 in `ItemConfigPopup.axaml`):
- ✅ Ante Selection (IsVisible="{Binding AntesVisible}")
- ✅ Sources Selection (IsVisible="{Binding SourcesVisible}")
- ✅ Edition Selection (IsVisible="{Binding EditionVisible}") - **FIXED NOW!**
- ✅ Rank and Suit Selection (IsVisible="{Binding RankVisible}")
- ✅ Enhancement Selection (IsVisible="{Binding EnhancementVisible}")

**Required Changes**:
- [ ] When StandardCard is right-clicked, show:
  - Rank dropdown (Ace, 2-10, Jack, Queen, King)
  - Suit dropdown (Hearts, Spades, Diamonds, Clubs)
  - Enhancement dropdown (None, Mult, Bonus, Glass, Gold, Steel, Stone)
  - Seal dropdown (None, Gold, Red, Blue, Purple)
  - Edition radio buttons (Normal, Foil, Holo, Poly, Negative)

**Visibility Logic** (Lines 121-207 in `ItemConfigPopupViewModel.cs`):
```csharp
// Set visibility based on item type
if (config.ItemType == "StandardCard" || config.ItemType == "PlayingCard")
{
    RankVisible = true;
    SuitVisible = true;
    EnhancementVisible = true;
    SealVisible = true;
    EditionVisible = true;
    AntesVisible = true;  // Can appear in specific antes
    SourcesVisible = true;  // Can come from specific packs
}
```

---

### Phase 4: Filter Integration

**Motely Filter Format**:
```json
{
  "Name": "Find Glass King",
  "Must": [
    {
      "ItemKey": "King_Hearts",
      "ItemType": "StandardCard",
      "Rank": "King",
      "Suit": "Hearts",
      "Enhancement": "Glass",
      "Edition": "none",
      "Seal": "None"
    }
  ]
}
```

**FilterConfigurationService** must handle StandardCard configs properly.

---

## 📊 Testing Checklist

### Visual Display Tests
- [ ] All 52 base cards render correctly
- [ ] Hearts group shows 13 cards (Ace through King)
- [ ] Spades group shows 13 cards
- [ ] Diamonds group shows 13 cards
- [ ] Clubs group shows 13 cards

### Enhancement Tests
- [ ] Mult cards show BlankCard + Pattern + Mult glyph
- [ ] Bonus cards show BlankCard + Pattern + Bonus glyph
- [ ] Glass cards show Glass background + Pattern
- [ ] Stone card shows only Stone sprite (no rank/suit)

### Configuration Tests
- [ ] Right-click any card → popup opens
- [ ] Change rank → preview updates
- [ ] Change suit → preview updates
- [ ] Change enhancement → preview updates
- [ ] Click APPLY → card added to zone with full config

### Filter Tests
- [ ] Create filter with "5 of Spades"
- [ ] Test filter finds seeds
- [ ] Create filter with "Glass King"
- [ ] Verify Motely receives correct config

---

## 🚨 Open Questions (Need Answers!)

### Question 1: Mult/Bonus Glyph Location
**Where are the Mult/Bonus overlay glyphs stored?**
- [ ] Part of enhancement sprite sheet?
- [ ] Separate sprite sheet?
- [ ] Check Balatro Lua: `card.lua`, `card_character.lua`

### Question 2: Wild Card Enhancement
**What is "Wild" enhancement?** (Listed in enhancers_metadata.json line 29)
- [ ] Is this used for standard cards?
- [ ] Should it be in the UI?
- [ ] Check Balatro source

### Question 3: Lucky Card Enhancement
**What is "Lucky" enhancement?** (Listed in enhancers_metadata.json line 30)
- [ ] How does it differ visually?
- [ ] Should users be able to filter by it?

### Question 4: Edition Shader Implementation
**Should we implement edition shaders or use PNG overlays?**
- [ ] MVP: Use static PNG overlays (if available)
- [ ] Future: Implement SkiaSharp shaders
- [ ] Check if edition PNGs exist in Assets

---

## 📁 Files to Modify

| File | Change Type | Lines |
|------|-------------|-------|
| `ViewModels/FilterTabs/VisualBuilderTabViewModel.cs` | IMPLEMENT | 433-436 (replace stub) |
| `Services/SpriteService.cs` | FIX | 1220 (compositing bug) |
| `Services/SpriteService.cs` | ADD | New methods for Type B2 |
| `ViewModels/ItemConfigPopupViewModel.cs` | UPDATE | Visibility logic for StandardCard |
| `Models/FilterItem.cs` | VERIFY | Supports Rank/Suit/Enhancement properties |
| `Services/FilterConfigurationService.cs` | UPDATE | Handle StandardCard serialization |

---

## 🎯 Success Criteria

1. ✅ User sees all 52+ cards organized by suit and enhancement
2. ✅ Cards render with correct sprite compositing (base + overlay)
3. ✅ Right-click opens popup with rank/suit/enhancement options
4. ✅ Configured cards can be added to MUST/SHOULD/MUST NOT zones
5. ✅ Filter test finds seeds matching StandardCard criteria
6. ✅ No visual glitches or missing sprites

---

## 💡 Implementation Strategy

### Recommended Approach:
1. **Start with Type A cards ONLY** (base 52 cards, no enhancements)
   - Get data loading working
   - Get sprite compositing working
   - Get UI groups working
2. **Add Type B1** (Glass/Gold/Steel)
   - Reuse same compositing, just different base
3. **Add Type B2** (Mult/Bonus)
   - Implement three-layer composite
   - Research glyph location in Balatro
4. **Add Stone Card** (special case)
5. **Add Edition support** (Type C)
   - Start with PNG overlays if available
   - Shaders are stretch goal

---

## 🔗 Reference Materials

### Balatro Lua Files to Study:
- `external/Balatro/card.lua` - Card rendering logic
- `external/Balatro/card_character.lua` - Card character/rank/suit handling
- `external/Balatro/engine/sprite.lua` - Sprite rendering system

### Existing Code Patterns:
- **Joker Category**: Lines 395-410 in `VisualBuilderTabViewModel.cs`
- **Sprite Compositing**: Lines 1123-1180 in `SpriteService.cs`
- **Item Config Popup**: `Controls/ItemConfigPopup.axaml` (working RadioButtons!)

---

**END OF PRD**

**Ready for Implementation**: ⚠️ BLOCKED - Need answers to open questions first!
**Estimated Effort**: 8-16 hours (depends on Mult/Bonus glyph complexity)
**Complexity**: MEDIUM-HIGH (sprite compositing, three-layer composite for Type B2)
