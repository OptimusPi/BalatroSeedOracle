# 🎯 Boss PRNG Implementation TODO

## 🚨 **CRITICAL: Implement Real Boss PRNG in Motely**

Based on complete reverse engineering analysis in `BOSS_PRNG_ANALYSIS.md`, implement the exact Balatro boss selection algorithm.

### **Files to Fix:**
1. `external/Motely/Motely/MotelySingleSearchContext.Boss.cs`
2. `external/Motely/Motely/MotelyVectorSearchContext.Boss.cs`

### **Required Implementation Steps:**

#### **1. Add Boss Metadata**
```csharp
public enum MotelyBossBlind
{
    // Add min/max ante and showdown flags to each boss
    TheHook, // min: 1, max: 10, showdown: false
    TheOx,   // min: 6, max: 10, showdown: false  
    AmberAcorn, // min: 10, max: 10, showdown: true
    // ... etc for all bosses
}

public static class BossMetadata 
{
    public static readonly Dictionary<MotelyBossBlind, (int min, int max, bool showdown)> Data = new()
    {
        { MotelyBossBlind.TheHook, (1, 10, false) },
        { MotelyBossBlind.TheOx, (6, 10, false) },
        { MotelyBossBlind.AmberAcorn, (10, 10, true) },
        // ... complete boss data from BOSS_PRNG_ANALYSIS.md
    };
}
```

#### **2. Implement Usage Tracking**
```csharp
public class BossUsageTracker
{
    private Dictionary<MotelyBossBlind, int> _bossesUsed = new();
    private HashSet<string> _bannedKeys = new();
    
    public void IncrementUsage(MotelyBossBlind boss) => _bossesUsed[boss]++;
    public int GetUsage(MotelyBossBlind boss) => _bossesUsed.GetValueOrDefault(boss, 0);
    public bool IsBanned(MotelyBossBlind boss) => _bannedKeys.Contains(boss.ToString());
}
```

#### **3. Fix GetNextBoss() Algorithm**
```csharp
public MotelyBossBlind GetNextBoss(ref MotelySingleBossStream bossStream)
{
    var ante = bossStream.CurrentAnte;
    var isShowdownAnte = (ante % 8 == 0); // TODO: Use dynamic win_ante
    
    // Step 1: Build eligible bosses with min/max ante constraints
    var eligibleBosses = new List<MotelyBossBlind>();
    foreach (var boss in BossMetadata.Data.Keys)
    {
        var (min, max, showdown) = BossMetadata.Data[boss];
        
        // Apply ante constraints and showdown logic
        if (showdown == isShowdownAnte && 
            min <= ante && ante <= max && 
            (isShowdownAnte || ante % 8 != 0 || ante < 2))
        {
            if (!bossStream.IsBlindBanned(boss))
                eligibleBosses.Add(boss);
        }
    }
    
    // Step 2: Apply usage-based fairness
    var minUsage = eligibleBosses.Min(boss => bossStream.GetBossUsage(boss));
    eligibleBosses = eligibleBosses.Where(boss => bossStream.GetBossUsage(boss) == minUsage).ToList();
    
    // Step 3: Sort by key names (like Balatro)
    eligibleBosses.Sort((a, b) => string.Compare(GetBossKey(a), GetBossKey(b)));
    
    // Step 4: Select using pseudorandom_element logic
    int selectedIndex = GetNextRandomInt(ref bossStream.BossPrngStream, 0, eligibleBosses.Count - 1);
    var selectedBoss = eligibleBosses[selectedIndex];
    
    // Step 5: Update usage tracking
    bossStream.IncrementBossUsage(selectedBoss);
    
    return selectedBoss;
}
```

#### **4. Implement Balatro's pseudorandom_element()**
```csharp
private static T PseudorandomElement<T>(List<T> items, ref MotelySinglePrngStream prngStream)
{
    // Sort by key names like Balatro
    items.Sort((a, b) => string.Compare(a.ToString(), b.ToString()));
    
    // Select random index
    int index = GetNextRandomInt(ref prngStream, 0, items.Count - 1);
    return items[index];
}
```

#### **5. Implement Balatro's pseudoseed()**
```csharp
private static double Pseudoseed(string key, ref MotelySinglePrngStream prngStream)
{
    // Implement exact Balatro pseudoseed algorithm from misc_functions.lua:298
    // Uses pseudohash and specific constants: 2.134453429141, 1.72431234
    // Returns (pseudorandom[key] + hashed_seed) / 2
    throw new NotImplementedException("TODO: Implement exact Balatro pseudoseed algorithm");
}
```

### **Testing Requirements:**
- [ ] Verify boss sequence matches Balatro game exactly for known seeds
- [ ] Test regular antes (1-7) vs showdown antes (8, 16, etc.)
- [ ] Test min ante constraints (Ox >= ante 6, Plant >= ante 4)
- [ ] Test usage fairness (no boss repeats while others unused)
- [ ] Compare single vs vector context results

### **Priority: CRITICAL** 🚨
This is blocking 100% accuracy for any boss-based searches. All boss filters are currently unreliable.

### **Estimated Effort:** 
- Implementation: 6-8 hours
- Testing: 2-4 hours  
- **Total: 8-12 hours**

---

**Status**: Ready to implement - complete algorithm documented in BOSS_PRNG_ANALYSIS.md