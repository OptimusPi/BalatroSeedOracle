# NEW FEATURES TODO

## High Priority Features

### 🎯 PRNG Sources to Implement
1. **The Wheel of Fortune tarot** - Adds edition to held jokers (high priority)
2. **Cartomancer** - Creates 1 tarot at blind start (high priority)
3. **Seance** - Creates spectrals except Soul/Black Hole (high priority)
4. **Riff-Raff** - Creates common jokers at blind selection (high priority)
5. **Judgement tarot** - Creates random joker (medium priority)

### 🎨 UI/UX Improvements
1. **Auto-fill author field when creating new filters** (high priority)
2. **Fix drag ghost card sizing and legendary joker face scaling** (medium priority)
3. **Update FiltersModal to use mystery legendary icon for 'any' soul joker** (medium priority)
4. **Add SmallBlind and BigBlind icons to tag configuration popover** (medium priority)

### 🎮 Advanced Filter System
1. **Implement Erratic Deck mechanics** - Randomize card ranks/suits each round
2. **Add advanced filter dropdown** with options for:
   - Immolate Analyzer mode
   - Anaglyph-optimized tricks
   - Erratic Deck filters
   - GPU-accelerated search modes
3. **Create more specialized FilterDesc classes** like BuggySeedFilterDesc

## Medium Priority Features

### 🔍 Search Improvements
1. **Investigate website discrepancies for seed results** - Compare with balatro.calculatedseed.com
2. **Add support for additional item sources**:
   - Boss rewards
   - Starting jokers (deck-specific)
   - Tag-based sources

### 🎨 Visual Enhancements
1. **Implement animated Soul card overlay** like legendary jokers (low priority)

## Low Priority Features

### 🃏 Additional PRNG Sources
1. **Ankh Spectral** - Copies random joker
2. **Wraith Spectral** - Creates rare joker, sets money to $0
3. **8 Ball Tarot** - Creates tarot if hand contains 8
4. **Sixth Sense Joker** - Creates spectral for single 6
5. **Superposition Joker** - Creates tarot for Ace + Straight
6. **Vagabond Joker** - Creates tarot if hand played with $3 or less
7. **Emperor Tarot** - Creates up to 2 tarots
8. **High Priestess Tarot** - Creates up to 2 planets
9. **Blue/Purple Seals** - Planet/Tarot generation

## 🛑 CODE FREEZE NOTES

As of July 30, 2025, we're pausing new feature development! The codebase is in a good state with:

✅ Completed:
- Boss image rendering fixed
- Author name persistence working
- Drag-drop duplicates instead of moves
- Performance optimizations for pack checking
- BuggySeedFilterDesc for finding corrupted seeds
- --filter parameter for testing different filter types
- Comprehensive PRNG sources documentation
- Fixed hardcoded "Jimbo" issue

🎉 The app is stable and functional! Time to take a break from new features.