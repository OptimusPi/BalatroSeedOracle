---
name: performance-profiling-seed-search
description: Profiles seed search performance to identify bottlenecks in filter parsing, search loops, and DB writes. Use when search is slow, investigating regressions, or optimizing CPU vs memory usage.
---

# Performance Profiling Seed Search

## When to Use

- Search is slow or has regressed
- Investigating performance improvements
- Need to identify CPU vs memory bottlenecks
- Comparing different filter configurations

## Preconditions

- **Always use Release builds** for performance validation
- Debug builds have significant overhead that skews results
- Desktop platform only for file-based profiling

## Key Performance Areas

| Phase          | What to Profile                    | Typical Bottleneck     |
| -------------- | ---------------------------------- | ---------------------- |
| Filter parsing | JAML/JSON deserialization          | Complex nested filters |
| Search loop    | Seed iteration + filter evaluation | Filter complexity      |
| DB write       | DuckDB inserts                     | Large result sets      |
| UI update      | Result binding                     | Many results displayed |

## Methodology

### 1. Isolate the Phase

Profile each phase separately to identify the bottleneck:

```
Total Time = Parse + Search + Write + Display
```

Don't optimize the wrong phase.

### 2. Add Timing Logs

Use `DebugLogger` (never `Console.WriteLine`):

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();

// ... operation ...

sw.Stop();
DebugLogger.Log("Perf", $"FilterParse: {sw.ElapsedMilliseconds}ms");
```

### 3. Measure at Boundaries

Good instrumentation points:

```csharp
// Filter parsing
DebugLogger.Log("Perf", $"ParseStart: {filterPath}");
var filter = await _filterService.LoadAsync(path);
DebugLogger.Log("Perf", $"ParseEnd: {sw.ElapsedMilliseconds}ms, clauses={filter.Clauses.Count}");

// Search loop
DebugLogger.Log("Perf", $"SearchStart: seeds={seedCount}");
// ... search ...
DebugLogger.Log("Perf", $"SearchEnd: {sw.ElapsedMilliseconds}ms, matches={matchCount}");

// DB write
DebugLogger.Log("Perf", $"WriteStart: rows={results.Count}");
await _duckDBService.InsertResultsAsync(results);
DebugLogger.Log("Perf", $"WriteEnd: {sw.ElapsedMilliseconds}ms");
```

## Platform Guardrails

**Browser vs Desktop perf is NOT comparable:**

| Factor  | Desktop     | Browser           |
| ------- | ----------- | ----------------- |
| DuckDB  | Native      | WASM (slower)     |
| Threads | Native      | SharedArrayBuffer |
| I/O     | File system | localStorage      |
| JIT     | Full        | Limited           |

Profile on the target platform. Don't draw conclusions from cross-platform comparisons.

## Common Performance Issues

### Slow Filter Parsing

- Complex nested JAML structures
- Large filter files
- Repeated re-parsing (cache filters)

### Slow Search Loop

- Too many filter clauses
- Inefficient filter evaluation order
- No early termination on impossible filters

### Slow DB Writes

- Large batch inserts without transactions
- Missing indexes
- Frequent small writes vs. batched writes

### Slow UI Updates

- Too many results bound at once
- Missing virtualization
- Frequent property change notifications

## Optimization Patterns

### Batch Operations

```csharp
// ❌ Slow: Individual inserts
foreach (var result in results)
    await db.InsertAsync(result);

// ✅ Fast: Batch insert
await db.InsertBatchAsync(results);
```

### Early Termination

```csharp
// Stop when we have enough results
if (matches.Count >= maxResults)
    break;
```

### Deferred Work

```csharp
// Don't update UI on every match
// Batch updates every N matches or every N ms
if (matches.Count % 100 == 0)
    await UpdateUIAsync(matches);
```

## Build Configuration

For profiling, use Release:

```bash
dotnet build -c Release src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj
```

For detailed profiling with symbols:

```xml
<!-- In .csproj -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DebugSymbols>true</DebugSymbols>
  <DebugType>pdbonly</DebugType>
</PropertyGroup>
```

## External Profilers

| Tool                   | Platform | Use For           |
| ---------------------- | -------- | ----------------- |
| dotnet-trace           | All      | CPU sampling      |
| dotnet-counters        | All      | Runtime metrics   |
| JetBrains dotTrace     | Desktop  | Deep CPU analysis |
| Visual Studio Profiler | Windows  | Memory + CPU      |
| Chrome DevTools        | Browser  | WASM profiling    |

### Quick dotnet-trace

```bash
# Collect trace
dotnet-trace collect --process-id <PID> --duration 00:00:30

# Analyze with speedscope.app or VS
```

## Checklist

- [ ] Using Release build (not Debug)
- [ ] Isolated the slow phase (parse/search/write/display)
- [ ] Added `DebugLogger` timing at phase boundaries
- [ ] Profiling on target platform (not cross-platform)
- [ ] Measured baseline before optimization
- [ ] Measured after optimization to confirm improvement
- [ ] Removed profiling code before commit (or guarded with `#if DEBUG`)
