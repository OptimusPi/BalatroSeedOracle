# TODO List for Balatro Seed Oracle

## 🔴 CRITICAL - PRNG Accuracy Issues

### Boss PRNG Algorithm is COMPLETELY BROKEN ⚠️
- **Issue**: The boss PRNG algorithm is fundamentally wrong compared to Balatro
- **Status**: ✅ **FULLY REVERSE ENGINEERED** - Exact algorithm discovered from Lua source
- **Analysis**: Complete breakdown documented in `BOSS_PRNG_ANALYSIS.md`
- **Files affected**:
  - `/external/Motely/Motely/MotelySingleSearchContext.Boss.cs` - GetNextBoss() algorithm
  - `/external/Motely/Motely/MotelyVectorSearchContext.Boss.cs` - Vector version needs same fix

### **Critical Differences Discovered**:
1. **Wrong Ante Logic**: Uses `ante % 8` instead of `ante % win_ante` (usually 8)
2. **Missing Min/Max Constraints**: Ignores boss.min/max ante from P_BLINDS data
3. **No Usage Tracking**: Missing fairness system that ensures equal boss usage
4. **Wrong Sorting**: Sorts by ToString() instead of key names like Balatro
5. **Missing Banned Keys**: No support for G.GAME.banned_keys exclusion system
6. **Incomplete Eligibility**: Oversimplified showdown vs regular boss distinction

### **Required Implementation**:
- Complete rewrite of boss selection algorithm to match Balatro exactly
- Add boss metadata (min/max ante, showdown flags) 
- Implement usage tracking across antes for fairness
- Fix pseudorandom_element and pseudoseed implementations
- Add proper sorting by key names
- Support banned keys system

### **Impact**: 
- ❌ Any boss-based filter is currently unreliable
- ❌ PRNG sequence diverges after first boss selection
- ❌ Seeds that depend on specific boss sequences will fail

### Vector PRNG Implementations Need Verification
- **Issue**: Vector implementations may not match single context PRNG exactly
- **Status**: Boss key fixed, other keys appear to match but need testing
- **Action needed**: Run comprehensive tests comparing vector vs single outputs

## 🟡 Code Quality Issues

### Unread Constructor Parameters
- `MotelyJsonFilterDesc` constructor has unread parameters:
  - `PrefilterEnabled` parameter is never used
  - `OnResultFound` parameter is never used
- Should either use these parameters or remove them

### Null Reference Warnings
- Multiple nullable reference warnings in MotelyJsonFilterDesc.cs
- Program.cs has nullable Action parameter issues

## 🟢 Completed Tasks
- ✅ Fixed compilation errors after vectorization attempt
- ✅ Updated SearchInstance.cs for new MotelyJsonFilterDesc constructor
- ✅ Fixed vector boss PRNG key (changed "Boss" to "boss")
- ✅ Removed hardcoded test values that were cheating
- ✅ Verified all CreatePrngStream keys match between single/vector contexts

## 📝 Notes
- Build succeeds but PRNG accuracy is not guaranteed
- Unit tests will fail until boss algorithm is fixed
- This is blocking accurate seed searching