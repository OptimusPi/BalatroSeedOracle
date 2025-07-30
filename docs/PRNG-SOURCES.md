# Balatro PRNG Sources Documentation

This document details all PRNG sources in Balatro and how they generate items.

## Current Ouija Implementation vs Missing Sources

### Currently Implemented in Ouija:
- ✅ Shop (jokers, vouchers, playing cards)
- ✅ Booster Packs (Arcana, Celestial, Spectral, Standard, Buffoon)
- ✅ Tags (skip tags from blinds)
- ✅ The Soul (from Arcana/Spectral packs)

### Missing Sources to Implement:

## 1. **The Wheel of Fortune (Tarot Card)**
- **PRNG Key**: `wheel_of_fortune`
- **What it does**: 1 in 4 chance to add edition to a random non-edition joker
- **Editions**: Foil, Holographic, or Polychrome (NOT Negative)
- **When**: When used/consumed
- **Implementation**: `poll_edition('wheel_of_fortune', nil, true, true)`

## 2. **Riff-Raff Joker**
- **PRNG Key**: `rif`
- **What it does**: Creates 2 Common jokers when blind is selected
- **Restriction**: Common rarity only, must have room
- **When**: At blind selection
- **Implementation**: `create_card('Joker', G.jokers, nil, 0, nil, nil, nil, 'rif')`

## 3. **Cartomancer Joker**
- **PRNG Key**: `car`
- **What it does**: Creates 1 Tarot card at the start of each blind
- **When**: Beginning of blind
- **Implementation**: `create_card('Tarot', G.consumeables, nil, nil, nil, nil, nil, 'car')`

## 4. **Seance Joker**
- **PRNG Key**: `sea`
- **What it does**: Creates 1 Spectral card if poker hand contains the joker's favorite hand
- **Restriction**: Cannot create The Soul or Black Hole
- **Implementation**: `create_card('Spectral', G.consumeables, nil, nil, nil, nil, nil, 'sea')`

## 5. **Judgement Tarot**
- **PRNG Key**: `jud`
- **What it does**: Creates a random Joker
- **Implementation**: `create_card('Joker', G.jokers, false, nil, nil, nil, nil, 'jud')`

## 6. **The Soul (Special Card)**
- **PRNG Key**: `sou`
- **What it does**: Creates a Legendary Joker
- **Where found**: Can replace cards in Arcana or Spectral packs
- **Implementation**: `create_card('Joker', G.jokers, true, nil, nil, nil, nil, 'sou')`

## 7. **Wraith Spectral**
- **PRNG Key**: `wra`
- **What it does**: Creates a random Rare Joker, sets money to $0
- **Implementation**: `create_card('Joker', G.jokers, nil, 0.99, nil, nil, nil, 'wra')`

## 8. **Ankh Spectral**
- **PRNG Key**: `ankh`
- **What it does**: Creates a copy of a random Joker
- **Implementation**: Uses `pseudorandom_element(G.jokers.cards, pseudoseed('ankh_choice'))` then copies

## 9. **Cryptid Spectral**
- **PRNG Key**: Not directly used - duplicates selected card
- **What it does**: Creates a copy of 1 selected card in hand

## 10. **8 Ball Tarot**
- **PRNG Key**: `8ba`
- **What it does**: Creates a random Tarot if hand contains 8
- **Implementation**: `create_card('Tarot', G.consumeables, nil, nil, nil, nil, nil, '8ba')`

## 11. **Sixth Sense Joker**
- **PRNG Key**: `sixth`
- **What it does**: Creates a Spectral card if played hand is single 6
- **When**: After playing hand

## 12. **Superposition Joker**
- **PRNG Key**: `sup`
- **What it does**: Creates Tarot card if hand contains Ace and Straight
- **Implementation**: `create_card(card_type, G.consumeables, nil, nil, nil, nil, nil, 'sup')`

## 13. **Vagabond Joker**
- **PRNG Key**: `vag`
- **What it does**: Creates a Tarot if hand played with $3 or less

## 14. **Emperor Tarot**
- **PRNG Key**: `emp`
- **What it does**: Creates up to 2 random Tarots
- **Implementation**: `create_card('Tarot', G.consumeables, nil, nil, nil, nil, nil, 'emp')`

## 15. **High Priestess Tarot**
- **PRNG Key**: `pri`
- **What it does**: Creates up to 2 random Planets
- **Implementation**: `create_card('Planet', G.consumeables, nil, nil, nil, nil, nil, 'pri')`

## 16. **Blue Seal**
- **PRNG Key**: `blusl`
- **What it does**: Creates a Planet card for last hand played
- **Implementation**: `create_card(card_type, G.consumeables, nil, nil, nil, nil, _planet, 'blusl')`

## 17. **Purple Seal**
- **PRNG Key**: `8ba` (same as 8 Ball)
- **What it does**: Creates a random Tarot when discarded

## 18. **Standard Pack Cards**
- **PRNG Keys**: 
  - `stdset` - Has enhancement (60% chance)
  - `Enhanced` - Enhancement type
  - `standard_edition` - Edition
  - `stdseal` - Has seal
  - `stdsealtype` - Seal type

## 19. **Boss Blind Rewards**
- **PRNG Key**: `boss`
- **What it does**: Various rewards after defeating boss

## 20. **Starting Jokers (Decks)**
- Various deck-specific starting jokers

## 21. **Tags as Sources**
- **Rare Tag**: `rta` - Creates Rare joker in shop
- **Uncommon Tag**: `uta` - Creates Uncommon joker in shop
- **Buffoon Tag**: Adds free Buffoon pack
- **Charm Tag**: Adds free Arcana pack
- **Meteor Tag**: Adds free Celestial pack
- **Ethereal Tag**: Adds free Spectral pack
- **Coupon Tag**: Free items in next shop
- **Double Tag**: Doubles next tag
- **Polychrome/Foil/Holographic/Negative Tags**: Give edition to next joker

## Other PRNG Keys Found:
- `misprint` - Misprint Joker values
- `lucky_mult` - Lucky Cat multiplier proc
- `lucky_money` - Lucky Cat money proc
- `sigil` - Sigil suit selection
- `ouija` - Ouija rank selection
- `erratic` - Erratic Joker behavior
- `gros_michel` / `cavendish` - Joker extinction
- `orbital` - Orbital tag
- `eternal` / `perishable` / `rental` - Joker properties

## Implementation Priority:
1. **The Wheel of Fortune tarot** - High impact, common edition creator
2. **Judgement tarot** - Core joker creation
3. **Riff-Raff** - Simple common joker creation
4. **Cartomancer** - Simple tarot creation
5. **Seance** - Conditional spectral creation
6. **Blue/Purple seals** - Common card sources
7. **Other jokers** - Lower priority sources