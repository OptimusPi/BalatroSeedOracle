# Product Requirements Document: Analyzer Modal Redesign

## 1. Executive Summary

### 1.1 Purpose
Redesign the Analyzer Modal to display seed analysis results using horizontal scrolling card carousels with full visual fidelity, matching the style of professional Balatro seed analyzers like SpectralPack/TheSoul.

### 1.2 Current State
The Analyzer Modal currently displays seed analysis results in a vertical layout with basic sprites and text labels. Shop items, booster packs, and tags are shown but lack proper visual presentation with editions, stickers, and enhancements.

### 1.3 Goal
Transform the Analyzer Modal into a visually rich, horizontally-scrolling interface that displays all cards with their complete visual properties including editions, stickers, enhancements, and proper sprite overlays.

## 2. User Stories

### 2.1 As a Player
- I want to see shop contents as a horizontal scrolling list of actual card visuals
- I want to see card editions (foil, holographic, polychrome, negative) visually represented
- I want to see card stickers (eternal, perishable, rental) as overlays
- I want to see booster pack contents with actual cards inside
- I want to quickly scan through all items without excessive vertical scrolling

### 2.2 As a Speedrunner
- I want to quickly identify high-value items in shops
- I want to see skip tags and their associated blinds clearly
- I want to understand boss blind effects at a glance
- I want efficient horizontal navigation through long item lists

## 3. Functional Requirements

### 3.1 Shop Queue Display

#### 3.1.1 Layout
- **Container**: Horizontal ScrollViewer with momentum scrolling
- **Direction**: Left-to-right card arrangement
- **Overflow**: Infinite horizontal scroll for long queues
- **Height**: Fixed height to accommodate largest card sprites

#### 3.1.2 Card Rendering
- **Base Sprite**: Full-resolution card image for each item type
  - Jokers: 71x95 pixels
  - Tarots: 71x95 pixels
  - Planets: 71x95 pixels
  - Spectrals: 71x95 pixels
  - Vouchers: 71x95 pixels
  - Playing Cards: 71x95 pixels
- **Slot Indicator**: Small badge showing shop slot number (0-3)
- **Price Display**: Optional price tag overlay

#### 3.1.3 Edition Overlays
- **Foil**: Blue shimmer overlay effect
- **Holographic**: Rainbow/prismatic overlay effect
- **Polychrome**: Gold/multicolor overlay effect
- **Negative**: Inverted color effect with "NEG" badge

#### 3.1.4 Sticker Overlays
- **Eternal**: Green eternal sticker in top-right
- **Perishable**: Gray perishable sticker with countdown
- **Rental**: Blue rental sticker with price modifier

### 3.2 Booster Pack Display

#### 3.2.1 Pack Types
- **Arcana Pack**: Shows tarot cards
- **Celestial Pack**: Shows planet cards
- **Spectral Pack**: Shows spectral cards
- **Buffoon Pack**: Shows jokers
- **Standard Pack**: Shows playing cards
- **Mega Variants**: Larger packs with more cards

#### 3.2.2 Pack Visualization
- **Container**: Pack sprite as background
- **Contents**: Fanned-out cards showing actual items
- **Hover State**: Expand to show all cards clearly
- **Edition Indicators**: Show if cards have editions

### 3.3 Skip Tags Display

#### 3.3.1 Layout
- **Container**: Horizontal row of tag badges
- **Blind Association**: Label showing "Small Blind" or "Big Blind"
- **Tag Sprite**: Full tag image with effects

#### 3.3.2 Tag Types
- Standard Tags (Economy, Double, etc.)
- Boss Tags (Boss Blind skip)
- Special Tags (Negative, Holographic, etc.)

### 3.4 Boss Blind Display

#### 3.4.1 Boss Visualization
- **Boss Sprite**: Large boss blind image
- **Boss Name**: "The Soul", "The Eye", etc.
- **Effect Description**: What the boss disables/modifies
- **Ante Number**: Which ante this boss appears in

## 4. Technical Requirements

### 4.1 Component Architecture

```
AnalyzeModal
├── HeaderSection
│   ├── SeedDisplay
│   ├── DeckIndicator
│   └── StakeIndicator
├── AnteSection (repeating)
│   ├── AnteHeader
│   ├── ShopCarousel
│   │   └── ShopItemCard (repeating)
│   │       ├── CardSprite
│   │       ├── EditionOverlay
│   │       ├── StickerOverlay
│   │       └── SlotBadge
│   ├── PackCarousel
│   │   └── BoosterPack (repeating)
│   │       ├── PackSprite
│   │       └── PackContents
│   ├── TagRow
│   │   └── TagBadge (repeating)
│   └── BossSection
│       ├── BossSprite
│       └── BossEffect
```

### 4.2 Scrolling Implementation

#### 4.2.1 Horizontal ScrollViewer
```csharp
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled"
              PanningMode="HorizontalOnly">
    <StackPanel Orientation="Horizontal">
        <!-- Card items -->
    </StackPanel>
</ScrollViewer>
```

#### 4.2.2 Smooth Scrolling
- Implement momentum/inertia scrolling
- Add keyboard navigation (arrow keys)
- Support mouse wheel horizontal scroll
- Touch/trackpad gesture support

### 4.3 Sprite Management

#### 4.3.1 Sprite Loading
- Use existing SpriteService for all images
- Implement lazy loading for performance
- Cache frequently used sprites

#### 4.3.2 Overlay Compositing
- Layer sprites using Grid/Canvas
- Z-order: Base → Edition → Sticker → Badge
- Maintain proper transparency channels

## 5. Visual Design Specifications

### 5.1 Card Display Standards

#### 5.1.1 Dimensions
- **Card Size**: 71x95 pixels (base)
- **Hover Scale**: 1.1x zoom
- **Spacing**: 8px between cards
- **Container Height**: 110px minimum

#### 5.1.2 Visual Effects
- **Drop Shadow**: 2px blur, 25% opacity
- **Hover Glow**: Soft white outline
- **Selection State**: Blue border highlight
- **Disabled State**: 50% opacity overlay

### 5.2 Color Palette

```
Edition Colors:
- Foil: #8FC5FF (Bright Blue)
- Holographic: #FF8FFF (Light Purple)
- Polychrome: #FFD700 (Gold)
- Negative: #FF5555 (Red)

UI Colors:
- Background: #2a2a2a (Dark Grey)
- Card Slot: #1a1a1a (Very Dark Grey)
- Text: #ffffff (White)
- Subtext: #cccccc (Light Grey)
```

### 5.3 Typography
- **Headers**: BalatroFont, 24px
- **Subheaders**: BalatroFont, 16px, Regular
- **Labels**: BalatroFont, 12px, Regular
- **Badges**: BalatroFont, 10px

## 6. Implementation Phases

### Phase 1: Foundation (Week 1)
1. Create horizontal ScrollViewer component
2. Implement basic card rendering with sprites
3. Set up shop queue carousel structure

### Phase 2: Visual Enhancements (Week 2)
1. Add edition overlay system
2. Implement sticker overlays
3. Add slot number badges

### Phase 3: Booster Packs (Week 3)
1. Create pack display components
2. Implement pack content rendering
3. Add pack type variations

### Phase 4: Tags & Bosses (Week 4)
1. Implement tag carousel
2. Add boss blind display
3. Create effect descriptions

### Phase 5: Polish & Optimization (Week 5)
1. Add smooth scrolling animations
2. Implement hover effects
3. Performance optimization
4. Accessibility features

## 7. Success Criteria

### 7.1 Functional Success
- [ ] All shop items display with correct sprites
- [ ] Editions are visually distinguishable
- [ ] Stickers appear in correct positions
- [ ] Horizontal scrolling works smoothly
- [ ] All pack types show contents correctly
- [ ] Tags and bosses display properly

### 7.2 Performance Metrics
- [ ] Initial render < 200ms
- [ ] Smooth 60fps scrolling
- [ ] Memory usage < 100MB for typical seed
- [ ] No lag with 50+ items in view

### 7.3 User Experience
- [ ] Intuitive navigation
- [ ] Clear visual hierarchy
- [ ] Consistent with Balatro's aesthetic
- [ ] Accessible via keyboard only

## 8. Dependencies

### 8.1 Existing Services
- **SpriteService**: For loading all card/item sprites
- **MotelyAnalyzer**: For seed analysis data
- **UserProfileService**: For deck/stake preferences

### 8.2 Required Assets
- All card sprites (jokers, tarots, planets, spectrals)
- Edition overlay effects
- Sticker sprites
- Pack backgrounds
- Tag images
- Boss blind sprites

### 8.3 External References
- SpectralPack/TheSoul GitHub implementation
- Balatro's native UI patterns
- Material Design horizontal scroll guidelines

## 9. Risks & Mitigations

### 9.1 Performance Risk
**Risk**: Large sprite counts may cause lag
**Mitigation**: Implement virtualization and lazy loading

### 9.2 Visual Complexity
**Risk**: Too many overlays may clutter display
**Mitigation**: Careful z-ordering and transparency management

### 9.3 Scroll UX
**Risk**: Horizontal scroll may be unfamiliar
**Mitigation**: Add visual indicators and keyboard shortcuts

## 10. Future Enhancements

### 10.1 Version 2.0
- Animated edition effects
- Card comparison mode
- Probability calculations
- Seed manipulation preview

### 10.2 Version 3.0
- Export to image/PDF
- Share via URL
- Community seed ratings
- Speedrun route optimization

## Appendix A: Reference Implementations

### A.1 SpectralPack/TheSoul
- GitHub: [URL to implementation]
- Key features: Horizontal card display, full visual fidelity
- Technologies: Similar Avalonia/WPF approach

### A.2 Balatro Native UI
- Shop display: Horizontal with slot indicators
- Pack opening: Fanned card reveal
- Tag display: Inline with blind markers

## Appendix B: Mockups

[Space for visual mockups and wireframes]

---

**Document Version**: 1.0
**Last Updated**: 2024
**Author**: BalatroSeedOracle Team
**Status**: Ready for Implementation