# TODO List for Balatro Seed Oracle

## 🔴 CRITICAL - PRNG Accuracy Issues

### Boss PRNG Algorithm is BROKEN
- **Issue**: The boss PRNG algorithm doesn't match Balatro's actual implementation
- **Evidence**: Unit tests were cheating with hardcoded values for UNITTEST and ALEEB seeds
- **Status**: Hardcoded cheats removed, tests will now fail revealing the real issue
- **Files affected**:
  - `/external/Motely/Motely/MotelySingleSearchContext.Boss.cs` - GetNextBoss() algorithm
  - `/external/Motely/Motely/MotelyVectorSearchContext.Boss.cs` - Vector version needs same fix
- **What was fixed so far**:
  - Changed boss PRNG key from "Boss" to "boss" (lowercase)
  - Removed hardcoded test values from GetBossForAnte()
- **What still needs fixing**:
  - The actual boss selection algorithm in GetNextBoss() doesn't match Balatro
  - Need to reverse engineer the correct boss selection logic from game
  - Tests will fail until this is fixed

### Vector PRNG Implementations Need Verification
- **Issue**: Vector implementations may not match single context PRNG exactly
- **Status**: Boss key fixed, other keys appear to match but need testing
- **Action needed**: Run comprehensive tests comparing vector vs single outputs

## 🟡 Code Quality Issues

### Unread Constructor Parameters
- `OuijaJsonFilterDesc` constructor has unread parameters:
  - `PrefilterEnabled` parameter is never used
  - `OnResultFound` parameter is never used
- Should either use these parameters or remove them

### Null Reference Warnings
- Multiple nullable reference warnings in OuijaJsonFilterDesc.cs
- Program.cs has nullable Action parameter issues

## 🟢 Completed Tasks
- ✅ Fixed compilation errors after vectorization attempt
- ✅ Updated SearchInstance.cs for new OuijaJsonFilterDesc constructor
- ✅ Fixed vector boss PRNG key (changed "Boss" to "boss")
- ✅ Removed hardcoded test values that were cheating
- ✅ Verified all CreatePrngStream keys match between single/vector contexts

## 📝 Notes
- Build succeeds but PRNG accuracy is not guaranteed
- Unit tests will fail until boss algorithm is fixed
- This is blocking accurate seed searching