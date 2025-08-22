# 🎯 Boss PRNG Reverse Engineering Analysis

## 🔍 **DISCOVERED: Complete Balatro Boss Selection Algorithm**

After studying `external/Balatro/functions/common_events.lua`, I've reverse-engineered the **exact** boss selection algorithm that Balatro uses.

## 📋 **Balatro's `get_new_boss()` Algorithm**

### **Step 1: Handle Special Cases**
```lua
-- Check if boss is pre-prescribed for this ante
if G.GAME.perscribed_bosses[ante] then 
    return prescribed_boss -- Skip normal selection
end

-- Check if boss is forced (debugging)
if G.FORCE_BOSS then 
    return G.FORCE_BOSS
end
```

### **Step 2: Build Eligible Boss List**
```lua
local eligible_bosses = {}
for k, v in pairs(G.P_BLINDS) do
    if v.boss then -- Only consider boss blinds
        if not v.boss.showdown then
            -- Regular bosses: min <= ante AND (ante % win_ante != 0 OR ante < 2)
            if v.boss.min <= math.max(1, ante) and 
               (ante % G.GAME.win_ante ~= 0 or ante < 2) then
                eligible_bosses[k] = true
            end
        else
            -- Showdown bosses: ante % win_ante == 0 AND ante >= 2
            if ante % G.GAME.win_ante == 0 and ante >= 2 then
                eligible_bosses[k] = true
            end
        end
    end
end
```

### **Step 3: Remove Banned Bosses**
```lua
for k, v in pairs(G.GAME.banned_keys) do
    if eligible_bosses[k] then 
        eligible_bosses[k] = nil 
    end
end
```

### **Step 4: Usage-Based Fairness Selection**
```lua
-- Find minimum usage count among eligible bosses
local min_use = 100
for k, v in pairs(G.GAME.bosses_used) do
    if eligible_bosses[k] then
        eligible_bosses[k] = v -- Replace true with usage count
        if v <= min_use then 
            min_use = v
        end
    end
end

-- Only keep bosses with minimum usage (fairness system)
for k, v in pairs(eligible_bosses) do
    if v > min_use then 
        eligible_bosses[k] = nil
    end
end
```

### **Step 5: Random Selection from Least-Used**
```lua
local _, boss = pseudorandom_element(eligible_bosses, pseudoseed('boss'))
G.GAME.bosses_used[boss] = G.GAME.bosses_used[boss] + 1
return boss
```

## 🔧 **Balatro's `pseudorandom_element()` Algorithm**

Located in `functions/misc_functions.lua:253`:

```lua
function pseudorandom_element(_t, seed)
    if seed then math.randomseed(seed) end
    local keys = {}
    for k, v in pairs(_t) do
        keys[#keys+1] = {k = k, v = v}
    end
    
    -- Sort by sort_id if available, otherwise by key name
    if keys[1] and keys[1].v and type(keys[1].v) == 'table' and keys[1].v.sort_id then
        table.sort(keys, function (a, b) return a.v.sort_id < b.v.sort_id end)
    else
        table.sort(keys, function (a, b) return a.k < b.k end)
    end
    
    local key = keys[math.random(#keys)].k
    return _t[key], key 
end
```

## 🧮 **Balatro's `pseudoseed()` Algorithm**

Located in `functions/misc_functions.lua:298`:

```lua
function pseudoseed(key, predict_seed)
    if key == 'seed' then return math.random() end
    
    if not G.GAME.pseudorandom[key] then 
        G.GAME.pseudorandom[key] = pseudohash(key..(G.GAME.pseudorandom.seed or ''))
    end
    
    G.GAME.pseudorandom[key] = math.abs(tonumber(string.format("%.13f", 
        (2.134453429141 + G.GAME.pseudorandom[key] * 1.72431234) % 1)))
    
    return (G.GAME.pseudorandom[key] + (G.GAME.pseudorandom.hashed_seed or 0)) / 2
end
```

## 📊 **Boss Data from `game.lua:263` (P_BLINDS)**

### **Regular Bosses** (non-showdown):
| Boss Key | Name | Min Ante | Max Ante | Special |
|----------|------|----------|----------|---------|
| `bl_hook` | The Hook | 1 | 10 | - |
| `bl_club` | The Club | 1 | 10 | - |
| `bl_psychic` | The Psychic | 1 | 10 | - |
| `bl_goad` | The Goad | 1 | 10 | - |
| `bl_head` | The Head | 1 | 10 | - |
| `bl_pillar` | The Pillar | 1 | 10 | - |
| `bl_window` | The Window | 1 | 10 | - |
| `bl_mouth` | The Mouth | 2 | 10 | - |
| `bl_fish` | The Fish | 2 | 10 | - |
| `bl_wall` | The Wall | 2 | 10 | - |
| `bl_house` | The House | 2 | 10 | - |
| `bl_water` | The Water | 2 | 10 | - |
| `bl_flint` | The Flint | 2 | 10 | - |
| `bl_needle` | The Needle | 2 | 10 | - |
| `bl_mark` | The Mark | 2 | 10 | - |
| `bl_tooth` | The Tooth | 3 | 10 | - |
| `bl_eye` | The Eye | 3 | 10 | - |
| `bl_plant` | The Plant | 4 | 10 | - |
| `bl_serpent` | The Serpent | 5 | 10 | - |
| `bl_ox` | The Ox | 6 | 10 | - |

### **Showdown Bosses** (finale):
| Boss Key | Name | Ante Rule |
|----------|------|-----------|
| `bl_final_acorn` | Amber Acorn | ante % win_ante == 0, ante >= 2 |
| `bl_final_bell` | Cerulean Bell | ante % win_ante == 0, ante >= 2 |
| `bl_final_heart` | Crimson Heart | ante % win_ante == 0, ante >= 2 |
| `bl_final_leaf` | Verdant Leaf | ante % win_ante == 0, ante >= 2 |
| `bl_final_vessel` | Violet Vessel | ante % win_ante == 0, ante >= 2 |

## ❌ **CRITICAL ISSUES in Current Motely Implementation**

### **1. Wrong Ante Logic** 
- **Motely**: `ante % 8 == 0` (hardcoded)
- **Balatro**: `ante % G.GAME.win_ante == 0` (usually 8, but can be different)

### **2. Missing Min/Max Ante Constraints**
- **Motely**: Ignores boss min/max ante completely
- **Balatro**: Strictly enforces `boss.min <= ante` for eligibility

### **3. No Usage-Based Fairness**
- **Motely**: Simple random selection every time
- **Balatro**: Complex fairness system that tracks usage and only selects least-used bosses

### **4. Wrong Sorting Logic**
- **Motely**: `ToString()` comparison 
- **Balatro**: Sorts by **key names** (`bl_arm`, `bl_club`, etc.) alphabetically

### **5. Incomplete Eligibility Logic**
- **Motely**: Oversimplified showdown vs regular distinction
- **Balatro**: Complex ante-based rules with special cases for ante < 2

### **6. Missing Banned Keys System**
- **Motely**: No concept of banned bosses
- **Balatro**: Removes bosses that are in `G.GAME.banned_keys`

## 🎯 **REQUIRED IMPLEMENTATION FIXES**

### **Core Algorithm Changes Needed:**

1. **Add Boss Metadata**: Min/max ante, showdown flag for each boss
2. **Implement Usage Tracking**: Track `bosses_used` dictionary across antes
3. **Add Banned Keys Support**: Remove banned bosses from eligible list
4. **Fix Ante Logic**: Use proper modulo logic with win_ante (8)
5. **Implement Fairness Selection**: Only select from least-used eligible bosses
6. **Fix Sorting**: Sort by key names, not display names
7. **Match PRNG exactly**: Ensure pseudoseed and pseudorandom_element work identically

### **Testing Requirements:**
- Verify known seeds produce exact same boss sequence as Balatro game
- Test both regular antes (1-7) and showdown antes (8, 16, etc.)
- Test early antes (ante < 2) special case
- Test min ante constraints (e.g., Ox requires ante >= 6)

## 💡 **Deep Technical Insights**

### **Why Usage Tracking Matters:**
The fairness system prevents the same boss from appearing multiple times while others never appear. This is **critical** for accurate seed simulation because:
- Players expect variety in boss encounters
- Some seeds rely on specific boss sequences
- The game's difficulty progression depends on boss fairness

### **Why Min/Max Ante Matters:**
- **The Ox** can't appear until ante 6+ (represents late-game power scaling)
- **The Plant** requires ante 4+ (mid-game encounter)
- **Showdown bosses** only appear on win_ante multiples (climactic encounters)

### **Critical PRNG Sequence Dependency:**
Boss selection affects ALL subsequent RNG because:
- Boss choice consumes one `pseudoseed('boss')` call
- This advances the PRNG state for all future operations
- Wrong boss = wrong everything after that point

## 🚨 **IMPACT ASSESSMENT**

**Current Status**: ❌ **BROKEN** - Boss-based filters are unreliable
**Accuracy Impact**: Any filter checking for specific bosses may fail/false-positive
**Performance Impact**: Minimal (boss checking is fast)
**User Impact**: Medium - boss filters don't work as expected

## 🛠️ **IMPLEMENTATION PLAN**

This requires a **complete rewrite** of the boss selection logic in:
- `MotelySingleSearchContext.Boss.cs` 
- `MotelyVectorSearchContext.Boss.cs`
- Boss enum definitions to include min/max ante
- PRNG stream handling for boss selection

**Estimated Effort**: 4-6 hours
**Risk Level**: High (affects PRNG accuracy)
**Testing Required**: Extensive (multiple known seeds)

---

**BOTTOM LINE**: The boss PRNG is fundamentally broken and needs a complete overhaul to match Balatro's exact algorithm. This is blocking 100% accuracy for any boss-based searches.