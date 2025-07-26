# Balatro Complete Reference Guide

## Table of Contents
1. [Game Overview & Core Mechanics](#game-overview--core-mechanics)
2. [Jokers](#jokers)
3. [Consumable Cards](#consumable-cards)
4. [Card Modifications](#card-modifications)
5. [Vouchers](#vouchers)
6. [Decks](#decks)
7. [Boss Blinds & Stakes](#boss-blinds--stakes)
8. [Additional Systems](#additional-systems)

---

## Game Overview & Core Mechanics

### Scoring Formula
**Final Score = (Base Hand Chips + Card Chips + Bonus Chips) × (Base Hand Mult + Bonus Mult + Multiplicative Bonuses)**

### Base Hand Values (Level 1)
- **High Card**: 5 chips × 1 mult = 5 points
- **Pair**: 10 chips × 2 mult = 20 points
- **Two Pair**: 20 chips × 2 mult = 40 points
- **Three of a Kind**: 30 chips × 3 mult = 90 points
- **Straight**: 30 chips × 4 mult = 120 points
- **Flush**: 35 chips × 4 mult = 140 points
- **Full House**: 40 chips × 4 mult = 160 points
- **Four of a Kind**: 60 chips × 7 mult = 420 points
- **Straight Flush**: 100 chips × 8 mult = 800 points

### Secret Hands
- **Flush Five**: 5 cards of same suit and rank
- **Flush House**: Full house where all cards are same suit
- **Five of a Kind**: 5 cards of same rank

### Card Values
- **Numbered cards**: Chips equal to face value (2-10)
- **Face cards (J, Q, K)**: 10 chips each
- **Aces**: 11 chips each

### Ante Progression
- **8 Antes** to win (Ante 8 = victory)
- Each Ante: Small Blind → Big Blind → Boss Blind
- **Score Requirements**:
  - Small Blind: 1× base
  - Big Blind: 1.5× base
  - Boss Blind: 2× base (most cases)

### Base Ante Scores
- Ante 1: 300
- Ante 2: 800
- Ante 3: 2,000
- Ante 4: 5,000
- Ante 5: 11,000
- Ante 6: 20,000
- Ante 7: 35,000
- Ante 8: 50,000

### Core Gameplay
- **Default hand size**: 8 cards
- **Default hands per round**: 4
- **Default discards per round**: 3
- **Default Joker slots**: 5
- **Interest**: $1 per $5 held (max $5 interest at $25)
- **Shop reroll cost**: $5 base (+$1 each subsequent)

---

## Jokers

### Overview
- **Total**: 150 Jokers
- **Available from Start**: 105
- **Unlock Required**: 45
- **Rarities**: Common (61), Uncommon (64), Rare (20), Legendary (5)

### Internal Naming Convention
**Note**: Naming conventions vary by implementation:
- **Balatro Lua source**: `j_[name]` (e.g., `j_joker`, `j_wee_joker`)
- **Immolate/Ouija**: Underscores (e.g., `Wee_Joker`, `The_Soul`)
- **MotelySearch**: No spaces/underscores (e.g., `WeeJoker`, `TheSoul`)
- **Your Oracle project**: Currently uses underscores following Ouija convention

### Common Jokers (61 Total)

#### Basic Multipliers
1. **Joker** (`j_joker`) - +4 Mult | Cost: $2 | Category: Multiplier
2. **Greedy Joker** (`j_greedy_joker`) - Diamond cards +3 Mult | Cost: $5 | Category: Conditional
3. **Lusty Joker** (`j_lusty_joker`) - Heart cards +3 Mult | Cost: $5 | Category: Conditional
4. **Wrathful Joker** (`j_wrathful_joker`) - Spade cards +3 Mult | Cost: $5 | Category: Conditional
5. **Gluttonous Joker** (`j_gluttonous_joker`) - Club cards +3 Mult | Cost: $5 | Category: Conditional

#### Hand-Based Multipliers
6. **Jolly Joker** (`j_jolly_joker`) - +8 Mult if Pair | Cost: $3 | Category: Conditional
7. **Zany Joker** (`j_zany_joker`) - +12 Mult if Three of a Kind | Cost: $4 | Category: Conditional
8. **Mad Joker** (`j_mad_joker`) - +10 Mult if Two Pair | Cost: $4 | Category: Conditional
9. **Crazy Joker** (`j_crazy_joker`) - +12 Mult if Straight | Cost: $4 | Category: Conditional
10. **Droll Joker** (`j_droll_joker`) - +10 Mult if Flush | Cost: $4 | Category: Conditional

#### Chip-Based
11. **Sly Joker** (`j_sly_joker`) - +50 Chips if Pair | Cost: $3
12. **Wily Joker** (`j_wily_joker`) - +100 Chips if Three of a Kind | Cost: $4
13. **Clever Joker** (`j_clever_joker`) - +150 Chips if Two Pair | Cost: $4
14. **Devious Joker** (`j_devious_joker`) - +100 Chips if Straight | Cost: $4
15. **Crafty Joker** (`j_crafty_joker`) - +80 Chips if Flush | Cost: $4

#### Special Effects
16. **Half Joker** (`j_half_joker`) - +20 Mult if ≤3 cards played | Cost: $5
17. **Credit Card** (`j_credit_card`) - Go up to -$20 in debt | Cost: $1 | Category: Economy
18. **Banner** (`j_banner`) - +40 Chips per remaining discard | Cost: $5
19. **Mystic Summit** (`j_mystic_summit`) - +15 Mult when 0 discards | Cost: $5
20. **8 Ball** (`j_8_ball`) - Create Planet card with 2+ 8s | Cost: $5 | Category: Utility
21. **Misprint** (`j_misprint`) - +0 to +23 Mult (random) | Cost: $4
22. **Gros Michel** (`j_gros_michel`) - +15 Mult, 1/6 chance destroyed | Cost: $5
23. **Cavendish** (`j_cavendish`) - X3 Mult, 1/1000 chance destroyed | Cost: $5 | Unlock: Gros Michel destroyed
24. **Abstract Joker** (`j_abstract_joker`) - +3 Mult per Joker | Cost: $4
25. **Supernova** (`j_supernova`) - Add hand play count to Mult | Cost: $5

### Uncommon Jokers (64 Total)

#### Key Examples
1. **Joker Stencil** (`j_joker_stencil`) - X1 Mult per empty slot | Cost: $8
2. **Four Fingers** (`j_four_fingers`) - Flushes/Straights need 4 cards | Cost: $7 | Category: Utility
3. **Mime** (`j_mime`) - Retrigger held cards | Cost: $5 | Category: Utility
4. **Steel Joker** (`j_steel_joker`) - X0.25 Mult per Steel Card | Cost: $50
5. **Scary Face** (`j_scary_face`) - +30 Chips per face card | Cost: $5
6. **Pareidolia** (`j_pareidolia`) - All cards are face cards | Cost: $5 | Category: Utility
7. **Hack** (`j_hack`) - Retrigger 2,3,4,5 | Cost: $6 | Category: Utility
8. **Fibonacci** (`j_fibonacci`) - Ace,2,3,5,8 give +8 Mult | Cost: $8
9. **Loyalty Card** (`j_loyalty_card`) - X1.5 Mult every 6 hands | Cost: $5 | Category: Scaling
10. **Constellation** (`j_constellation`) - X0.1 Mult per Planet used | Cost: $6 | Category: Scaling

### Rare Jokers (20 Total)

#### Key Examples
1. **Baron** (`j_baron`) - Each King in hand X1.5 Mult | Cost: $8
2. **Seeing Double** (`j_seeing_double`) - X2 Mult if Club + other suit | Cost: $8
3. **Smeared Joker** (`j_smeared_joker`) - Hearts/Diamonds same, Spades/Clubs same | Cost: $7 | Unlock: 3+ Wild Cards
4. **Blueprint** (`j_blueprint`) - Copy Joker to the right | Cost: $10 | Category: Utility
5. **Brainstorm** (`j_brainstorm`) - Copy leftmost Joker | Cost: $10 | Category: Utility
6. **Invisible Joker** (`j_invisible_joker`) - After 2 rounds, sell to dupe random Joker | Cost: $10
7. **DNA** (`j_dna`) - If first hand has 1 card, add copy to deck | Cost: $8
8. **Sock and Buskin** (`j_sock_and_buskin`) - Retrigger face cards | Cost: $6 | Unlock: Play 300 face cards
9. **Swashbuckler** (`j_swashbuckler`) - Add sell value of left Jokers to Mult | Cost: $4
10. **Hanging Chad** (`j_hanging_chad`) - Retrigger first card | Cost: $4 | Unlock: Beat Boss with High Card
11. **Shoot the Moon** (`j_shoot_the_moon`) - +13 Mult per Queen in hand | Cost: $5
12. **Driver's License** (`j_drivers_license`) - X3 Mult if 16+ Enhanced cards | Cost: $7
13. **Cartomancer** (`j_cartomancer`) - Create Tarot when Blind selected | Cost: $6
14. **Astronomer** (`j_astronomer`) - All Planet cards/packs free | Cost: $8
15. **Burnt Joker** (`j_burnt_joker`) - Upgrade first discarded hand type | Cost: $6
16. **Bootstraps** (`j_bootstraps`) - +2 Mult per $5 owned | Cost: $7

### Legendary Jokers (5 Total - Soul Spectral only)
1. **Perkeo** (`j_perkeo`) - Create Negative consumable at shop end | Category: Utility
2. **Canio** (`j_canio`) - X1 Mult, gains X1 when face card destroyed | Category: Scaling
3. **Triboulet** (`j_triboulet`) - Kings/Queens give X2 Mult | Category: Multiplier
4. **Yorick** (`j_yorick`) - X5 Mult every 23 cards discarded | Category: Scaling
5. **Chicot** (`j_chicot`) - Disables Boss Blind effects | Category: Utility

---

## Consumable Cards

### Tarot Cards (22 Total)

1. **The Fool (0)** - Creates last Tarot/Planet used
2. **The Magician (I)** - Enhances 2 cards to Lucky
3. **The High Priestess (II)** - Creates 2 Planet cards
4. **The Empress (III)** - Enhances 2 cards to Mult
5. **The Emperor (IV)** - Creates 2 Tarot cards
6. **The Hierophant (V)** - Enhances 2 cards to Bonus
7. **The Lovers (VI)** - Enhances 1 card to Wild
8. **The Chariot (VII)** - Enhances 1 card to Steel
9. **Justice (VIII)** - Enhances 1 card to Glass
10. **The Hermit (IX)** - Doubles money (max $20)
11. **The Wheel of Fortune (X)** - 1/4 chance to add edition to Joker
12. **Strength (XI)** - Increases rank of 2 cards by 1
13. **The Hanged Man (XII)** - Destroys 2 cards
14. **Death (XIII)** - Convert left card into right card
15. **Temperance (XIV)** - Gain sell value of Jokers (max $50)
16. **The Devil (XV)** - Enhances 1 card to Gold
17. **The Tower (XVI)** - Enhances 1 card to Stone
18. **The Star (XVII)** - Converts 3 cards to Diamonds
19. **The Moon (XVIII)** - Converts 3 cards to Clubs
20. **The Sun (XIX)** - Converts 3 cards to Hearts
21. **Judgement (XX)** - Creates random Joker
22. **The World (XXI)** - Converts 3 cards to Spades

### Planet Cards (12 Total)

#### Standard (9)
1. **Pluto** (High Card) - +1 Mult, +10 Chips
2. **Mercury** (Pair) - +1 Mult, +15 Chips
3. **Uranus** (Two Pair) - +1 Mult, +20 Chips
4. **Venus** (Three of a Kind) - +2 Mult, +20 Chips
5. **Saturn** (Straight) - +3 Mult, +30 Chips
6. **Jupiter** (Flush) - +2 Mult, +15 Chips
7. **Earth** (Full House) - +2 Mult, +25 Chips
8. **Mars** (Four of a Kind) - +3 Mult, +30 Chips
9. **Neptune** (Straight Flush) - +4 Mult, +40 Chips

#### Secret (3)
10. **Planet X** (Five of a Kind) - +3 Mult, +35 Chips
11. **Ceres** (Flush House) - +4 Mult, +40 Chips
12. **Eris** (Flush Five) - +3 Mult, +50 Chips

### Spectral Cards (18 Total)

1. **Familiar** - Destroy 1 card, create 3 Enhanced face cards
2. **Grim** - Destroy 1 card, create 2 Enhanced Aces
3. **Incantation** - Destroy 1 card, create 4 Enhanced number cards
4. **Talisman** - Add Gold Seal to 1 card
5. **Aura** - Add random edition to 1 card
6. **Wraith** - Create Rare Joker, set money to $0
7. **Sigil** - Convert hand to one suit, -1 hand size
8. **Ouija** - Convert hand to one rank, -1 hand size
9. **Ectoplasm** - Add Negative to Joker, -1 hand size
10. **Immolate** - Destroy 5 cards, gain $20
11. **Ankh** - Copy random Joker, destroy others
12. **Deja Vu** - Add Red Seal to 1 card
13. **Hex** - Add Polychrome to Joker, destroy others
14. **Trance** - Add Blue Seal to 1 card
15. **Medium** - Add Purple Seal to 1 card
16. **Cryptid** - Create 2 copies of 1 card
17. **The Soul** - Creates random Legendary Joker (0.3% chance)
18. **Black Hole** - Upgrade all poker hands by 1 level (0.3% chance)

---

## Card Modifications

### Enhancements (8 Types)
1. **Bonus Card** - +30 Chips when scored
2. **Mult Card** - +4 Mult when scored
3. **Wild Card** - Counts as all suits
4. **Glass Card** - X2 Mult, 1/4 chance to destroy
5. **Steel Card** - X1.5 Mult while held in hand
6. **Stone Card** - +50 Chips, no rank/suit, always scores
7. **Gold Card** - +$3 when scored
8. **Lucky Card** - 1/5 chance +20 Mult, 1/15 chance +$20

### Editions (5 Types)
1. **Base** - No effect
2. **Foil** - +50 Chips
3. **Holographic** - +10 Mult
4. **Polychrome** - X1.5 Mult
5. **Negative** - +1 Joker slot (Jokers), +1 Consumable slot (Consumables)

### Seals (4 Types)
1. **Red Seal** - Retriggers card
2. **Blue Seal** - Creates Planet card if held at round end
3. **Purple Seal** - Creates Tarot when discarded
4. **Gold Seal** - +$3 when scored

---

## Vouchers

### 32 Total (16 Base + 16 Upgraded)
All vouchers cost $10

#### Shop Enhancement
- **Overstock** → **Overstock Plus** - +1 card slot each
- **Clearance Sale** → **Liquidation** - 25% → 50% discount
- **Reroll Surplus** → **Reroll Glut** - +1 reroll each

#### Consumables
- **Crystal Ball** → **Omen Globe** - +1 slot, Spectrals in Arcana packs
- **Telescope** → **Observatory** - Guaranteed planet, held planets X1.5 Mult
- **Tarot Merchant** → **Tarot Tycoon** - 2X → 4X frequency
- **Planet Merchant** → **Planet Tycoon** - 2X → 4X frequency

#### Gameplay
- **Grabber** → **Nacho Tong** - +1 hand size each
- **Wasteful** → **Recyclomancy** - +1 discard if ≤4 cards played
- **Paint Brush** → **Palette** - +1 hand size each

#### Economy
- **Seed Money** → **Money Tree** - $5 → $10 interest cap
- **Hone** → **Glow Up** - 2X → 4X edition frequency

#### Special
- **Blank** → **Antimatter** - Nothing → +1 Joker slot
- **Magic Trick** → **Illusion** - Playing cards in shop → Enhanced cards
- **Hieroglyph** → **Petroglyph** - -1 Ante/-1 hand → also -1 discard
- **Director's Cut** → **Retcon** - 1 Boss reroll → Unlimited rerolls

---

## Decks

### 15 Starting Decks

#### Basic (5)
1. **Red Deck** - +1 discard every round
2. **Blue Deck** - +1 hand every round (Unlock: 20 items)
3. **Yellow Deck** - Start with +$10 (Unlock: 50 items)
4. **Green Deck** - $2/hand, $1/discard, no interest (Unlock: 75 items)
5. **Black Deck** - +1 Joker slot, -1 hand (Unlock: 100 items)

#### Advanced (5)
6. **Magic Deck** - Crystal Ball + 2x The Fool (Win with Red)
7. **Nebula Deck** - Telescope, -1 consumable slot (Win with Blue)
8. **Ghost Deck** - Spectral in shop, start with Hex (Win with Yellow)
9. **Abandoned Deck** - No face cards (Win with Green)
10. **Checkered Deck** - 26 Spades, 26 Hearts only (Win with Black)

#### Stakes Unlocks (5)
11. **Zodiac Deck** - Tarot/Planet Merchant + Overstock (Red Stake)
12. **Painted Deck** - +2 hand size, -1 Joker slot (Green Stake)
13. **Anaglyph Deck** - Double Tag after Boss (Black Stake)
14. **Plasma Deck** - Balance Chips/Mult, X2 Blind size (Blue Stake)
15. **Erratic Deck** - All ranks/suits randomized (Orange Stake)

---

## Boss Blinds & Stakes

### Regular Boss Blinds (23)
1. **The Hook** - Discards 2 random cards per hand
2. **The Ox** - Sets money to $0 per hand
3. **The House** - Face cards drawn face-down
4. **The Wall** - Requires 2.5x chips
5. **The Wheel** - 1/4 chance to destroy Joker
6. **The Arm** - Reduces hand level by 1
7. **The Club** - Club cards debuffed
8. **The Fish** - All cards face-down after play
9. **The Psychic** - Must play exactly 5 cards
10. **The Goad** - Spade cards debuffed
11. **The Water** - One hand type only
12. **The Window** - Diamond cards debuffed
13. **The Manacle** - -1 hand size
14. **The Eye** - No repeat hand types
15. **The Mouth** - One hand type only
16. **The Plant** - Face cards debuffed
17. **The Serpent** - Always draw 3 cards
18. **The Pillar** - Previous cards debuffed
19. **The Needle** - Only 1 hand allowed
20. **The Head** - Heart cards debuffed
21. **The Tooth** - Lose $1 per card played
22. **The Flint** - Base chips/mult halved
23. **The Mark** - Face cards face-down

### Finisher Blinds (5 - Ante 8 only)
1. **Amber Acorn** - Flips and shuffles Jokers
2. **Verdant Leaf** - All cards debuffed until Joker sold
3. **Violet Vessel** - Requires 6x chips
4. **Crimson Heart** - Random Joker debuffed per hand
5. **Cerulean Bell** - Random card permanently selected

### Stakes (8 Cumulative Difficulties)
1. **White** - Base difficulty
2. **Red** - Small Blinds give no money
3. **Green** - Faster score scaling
4. **Black** - 30% Eternal Jokers
5. **Blue** - -1 Discard
6. **Purple** - Even faster scaling
7. **Orange** - 30% Perishable Jokers
8. **Gold** - 30% Rental Jokers

---

## Additional Systems

### Skip Reward Tags (24 Total)

#### Joker Tags
- **Uncommon Tag** - Free Uncommon Joker in shop
- **Rare Tag** - Free Rare Joker in shop
- **Negative Tag** - Next base Joker becomes Negative
- **Foil Tag** - Next base Joker becomes Foil
- **Holographic Tag** - Next base Joker becomes Holographic
- **Polychrome Tag** - Next base Joker becomes Polychrome

#### Economy Tags
- **Investment Tag** - $25 after defeating Boss
- **Economy Tag** - Double money (max $40)
- **Speed Tag** - $2 per remaining Blind when Boss defeated
- **Garbage Tag** - $1 per unused discard
- **Handy Tag** - $1 per hand played this run

#### Pack Tags
- **Standard Tag** - Free Mega Standard Pack
- **Charm Tag** - Free Mega Arcana Pack
- **Meteor Tag** - Free Mega Celestial Pack
- **Buffoon Tag** - Free Mega Buffoon Pack
- **Ethereal Tag** - Free Spectral Pack

#### Utility Tags
- **Voucher Tag** - Extra voucher in next shop
- **Boss Tag** - Reroll Boss Blind
- **Coupon Tag** - Free items in next shop
- **Double Tag** - Copy next tag selected
- **Juggle Tag** - +3 hand size next round
- **D6 Tag** - Free rerolls in next shop
- **Top-up Tag** - Create Common Jokers to fill slots
- **Orbital Tag** - Upgrade random poker hand 3 levels

### Booster Packs
5 types × 3 sizes (Normal/Jumbo/Mega):
- **Arcana Pack** - Tarot cards
- **Celestial Pack** - Planet cards
- **Spectral Pack** - Spectral cards
- **Buffoon Pack** - Jokers
- **Standard Pack** - Playing cards (often enhanced)

### Stickers (Joker Modifiers)
- **Eternal** - Cannot be sold/destroyed
- **Perishable** - Destroyed after 5 rounds
- **Rental** - Costs $3 per round

### Total Collection: 340 Items
- 150 Jokers
- 15 Decks (+20 Challenge)
- 52 Consumables
- 32 Vouchers
- 32 Booster Packs
- 24 Tags
- 30 Blinds
- Various Modifiers

---

*This reference guide represents the complete collection of Balatro items and mechanics as of update 1.0.1o-FULL*
