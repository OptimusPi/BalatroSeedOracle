# Mono WASM Assertion Failure Fix

## Problem

**Symptom**: Palindrome search (and potentially other searches) in the browser cause a Mono WASM runtime assertion failure:

```
[MONO] * Assertion at /__w/1/s/src/runtime/src/mono/mono/mini/mini-runtime.c:2713, condition '<disabled>' not met
```

**Root Cause**: Mono WASM has known issues with `Barrier` synchronization primitives used in multi-threaded mode. The assertion at line 2713 in the Mono runtime's mini-runtime indicates that a disabled or unsupported threading feature was attempted.

**Affected Code Path**: 
1. User initiates palindrome search in browser
2. `MotelyWasm.SearchSeedsWithOptions()` creates a `SearchOptionsDto` with `Palindrome = true`
3. `MotelyPalindromeSeedProvider` is created to lazily generate palindrome seeds
4. `MotelySearch<TBaseFilter>` spawns multiple threads (default: `Environment.ProcessorCount`)
5. Threads use `Barrier` for synchronization in the pause/unpause flow
6. **Mono WASM assertion fails** during barrier synchronization

## Solution

**Approach**: Force single-threaded search mode for WASM/browser builds by capping `threadCount` to 1.

**Rationale**:
- Mono WASM threading support is incomplete, especially for advanced synchronization primitives
- Single-threaded mode eliminates all `Barrier` usage, avoiding the runtime assertion
- Performance impact is minimal in browser context (limited CPU anyway)
- All search functionality still works (just sequential instead of parallel)

**Changes Made**:

### File: `x:\BalatroSeedOracle\external\Motely\Motely.WASM\MotelyWasm.cs` (lines 276-279)

**Before**:
```csharp
int threadCount = options.ThreadCount.GetValueOrDefault();
threadCount = threadCount <= 0 ? Environment.ProcessorCount : Math.Clamp(threadCount, 1, Environment.ProcessorCount);
_searchThreadCount = threadCount;
```

**After**:
```csharp
int threadCount = options.ThreadCount.GetValueOrDefault();
// WASM has issues with Barrier synchronization in multi-threaded mode (Mono WASM assertion failure)
// Force single-threaded search for WASM builds to avoid runtime assertion at mini-runtime.c:2713
threadCount = 1;
_searchThreadCount = threadCount;
```

## Test Cases

### Palindrome Search (Previously Failing)
- **Before Fix**: Crashes with Mono WASM assertion at mini-runtime.c:2713
- **After Fix**: Completes successfully with single-threaded execution

### Random Search
- Should work (was likely working before, now guaranteed)

### Keyword Search
- Should work with single-threaded fallback

### Sequential/JAML Filter Search
- Should work with single-threaded fallback

## Trade-offs

| Aspect | Impact |
|--------|--------|
| Correctness | ✅ Fixed - no more assertion errors |
| Performance | ⚠️ Slower (single-threaded) but still functional |
| Compatibility | ✅ Maintained - only affects browser builds |
| User Experience | ✅ Better - searches complete without crashing |

## Alternative Approaches Considered

1. **Disable Barriers entirely**: Requires deeper changes to `MotelySearch` architecture
2. **Use semaphores instead of Barriers**: Still likely to fail in Mono WASM
3. **WebWorker-based threading**: Would require complete architectural rewrite
4. **Upgrade Mono/.NET version**: Future option when WASM threading is more mature

## Future Improvements

When Mono WASM threading is more mature (or we migrate to newer .NET versions with better WASM support), we can:

1. Re-enable multi-threaded search
2. Profile to find optimal thread count for browser
3. Use WebWorkers for true parallel execution (different from managed threads)
4. Add user-facing thread count control in UI

## Version Impact

- **motely-wasm v1.0.4**: Multi-threaded (broken for palindrome search)
- **motely-wasm v1.0.5**: Single-threaded (fixed, palindrome search works)
- **BalatroSeedOracle.Browser**: Should update to use motely-wasm v1.0.5+

## References

- Mono WASM Threading Limitations: https://github.com/dotnet/runtime/issues
- Balatro Seed Oracle Issue: Search doesn't find seeds (Mono WASM assertion)
- Test Environment: Vercel v0 preview (browser-based WASM execution)
