# Browser Search Fix Plan

## Documentation References

**MUST READ before implementing:**

- **Platform Abstraction Pattern**: https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications/dealing-with-platforms
- **Dependency Injection**: https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
- **Project Architecture**: `x:\BalatroSeedOracle\docs\ARCHITECTURE.md`
- **Avalonia Best Practices**: `x:\BalatroSeedOracle\docs\AVALONIA_BEST_PRACTICES.md`

## Key Rules from Docs

1. **NO `#if BROWSER` or `#if !BROWSER`** in shared code - use interfaces + DI
2. **Interfaces** in Core/shared project (e.g., `IDuckDBService`, `IPlatformServices`)
3. **Platform implementations** in head projects (Desktop, Browser, iOS, Android)
4. **DI registration** via `PlatformServices.RegisterServices` at startup
5. **Runtime detection** via `IPlatformServices.SupportsFileSystem` (not compile-time)

## Problem
`MotelySearchOrchestrator.Launch()` uses `MotelySearchDatabase` (native DuckDB) when `OutputDbPath` is provided.
This fails in Browser because DuckDB.NET doesn't support WASM - Browser needs JS interop to DuckDB-WASM.

## Solution Architecture

```
Desktop:
  SearchManager.StartSearchAsync()
    → MotelySearchOrchestrator.Launch(config, params WITH OutputDbPath)
    → Motely writes to MotelySearchDatabase (native DuckDB)
    → ActiveSearchContext queries via IDuckDBService (DesktopDuckDBService)

Browser:
  SearchManager.StartSearchAsync()
    → MotelySearchOrchestrator.Launch(config, params WITHOUT OutputDbPath, WITH resultCallback)
    → Results come via callback
    → SearchManager stores results via IDuckDBService (BrowserDuckDBService → DuckDB-WASM)
    → ActiveSearchContext queries via IDuckDBService (BrowserDuckDBService)
```

## Files to Modify

### 1. `src/BalatroSeedOracle/Services/SearchManager.cs`

```csharp
// In StartSearchAsync():

var platformServices = ServiceHelper.GetService<IPlatformServices>();
var isBrowser = platformServices != null && !platformServices.SupportsFileSystem;

if (isBrowser)
{
    // Browser: No OutputDbPath, use callback to store results
    var duckDb = ServiceHelper.GetService<IDuckDBService>();
    // Create in-memory DB via BrowserDuckDBService
    // Use resultCallback to insert rows
    
    var searchParams = new JsonSearchParams
    {
        Threads = criteria.ThreadCount,
        BatchSize = criteria.BatchSize,
        // NO OutputDbPath!
    };
    
    var motelySearch = MotelySearchOrchestrator.Launch(config, searchParams, result =>
    {
        // Store result via IDuckDBService
        // This routes to BrowserDuckDBService → DuckDB-WASM
    });
}
else
{
    // Desktop: Use OutputDbPath (existing code)
    var searchParams = new JsonSearchParams
    {
        OutputDbPath = dbPath,
        // ...
    };
    var motelySearch = MotelySearchOrchestrator.Launch(config, searchParams);
}
```

### 2. `src/BalatroSeedOracle/Services/ActiveSearchContext.cs`

Already fixed - uses `IDuckDBService` for cross-platform DB queries.

### 3. Browser DuckDB Table Creation

`BrowserDuckDBService` needs to create the results table schema before search starts.
Use `MotelyRunConfig.Factory(config).Columns` to get column definitions.

## Key Points

1. **No `#if BROWSER`** - Use `IPlatformServices.SupportsFileSystem` at runtime
2. **One SearchManager** - Platform detection at runtime, not compile time
3. **IDuckDBService abstraction** - Already wired up correctly
4. **MotelySearchOrchestrator** - Works on all platforms, just don't pass OutputDbPath for browser

## Testing

1. Desktop: `dotnet build src/BalatroSeedOracle.Desktop`
2. Browser: `dotnet build src/BalatroSeedOracle.Browser`
3. Run Desktop and verify search works
4. Run Browser and verify search works (results stored in DuckDB-WASM)
