---
name: browser-wasm-build-and-threads
description: Builds and validates Browser/WASM target including threaded builds. Use when working on browser support, JS interop, WASM performance, or deployment.
---

# Browser WASM Build and Threads

## Project Location

`src/BalatroSeedOracle.Browser/`

## Standard Build Commands

### Basic Build

```bash
dotnet build src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release
```

### Publish (Production)

```bash
dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish/browser
```

### Threaded Build

```bash
dotnet publish -c Release -p:EnableWasmThreads=true src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
```

## Threading Requirements

Threaded WASM requires specific HTTP headers:

```
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

### Compatible Hosts

- nginx (with header configuration)
- Caddy
- Apache (reverse proxy with headers)
- Any static server with custom headers

### Incompatible Hosts

- GitHub Pages (no COOP/COEP support)
- Basic static hosting without header control

## Browser-Specific Services

Location: `src/BalatroSeedOracle.Browser/Services/`

| Service | Purpose |
|---------|---------|
| `BrowserPlatformServices.cs` | Platform capabilities (limited) |
| `BrowserDuckDBService.cs` | DuckDB via WASM (limited) |
| `BrowserLocalStorageAppDataStore.cs` | localStorage for persistence |
| `BrowserApiHostService.cs` | API hosting |

## JavaScript Interop

Location: `src/BalatroSeedOracle.Browser/wwwroot/js/`

| File | Purpose |
|------|---------|
| `bso-helpers.js` | BSO utility functions |
| `duckdb-interop.js` | DuckDB WASM bridge |
| `webaudio-interop.js` | Web Audio API bridge |

## Performance Optimization

### Enable in .csproj

```xml
<PropertyGroup>
  <!-- Enable SIMD for better performance -->
  <WasmEnableSIMD>true</WasmEnableSIMD>
  
  <!-- Enable AOT for faster runtime -->
  <RunAOTCompilation>true</RunAOTCompilation>
  
  <!-- Enable trimming for smaller size -->
  <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

### Best Practices

- Keep visual tree shallow
- Use compiled bindings (`x:CompileBindings="True"`)
- Use virtualization for large collections
- Defer non-critical initialization
- Compress WASM files (Brotli/gzip)

## Browser Constraints

| Feature | Desktop | Browser |
|---------|---------|---------|
| File system | ✅ | ❌ Use localStorage |
| Native dialogs | ✅ | Limited |
| Audio | Full | Limited |
| stdin | ✅ | ❌ |
| DuckDB | Full | WASM version |

## Testing Browser Build

1. Build and publish
2. Serve with headers (e.g., `npx serve` with config)
3. Test in multiple browsers (Chrome, Firefox, Safari)
4. Check console for errors
5. Verify localStorage persistence

## CI/CD Integration

From `.github/workflows/release-browser.yml`:

```yaml
- name: Build Browser Version
  run: dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish/browser --no-restore

- name: Create Browser Archive
  run: |
    cd publish/browser
    7z a -tzip ../../BalatroSeedOracle-Browser.zip *
```

## Checklist

- [ ] Build succeeds without errors
- [ ] Bundle size is reasonable
- [ ] Threading headers configured (if using threads)
- [ ] localStorage persistence works
- [ ] No console errors in browser
- [ ] Performance acceptable across browsers
