---
name: wasm-js-interop-debugger
description: Debugs Browser/WASM interop issues including DuckDB WASM, audio interop, and threading problems. Use when browser build loads but features fail, WASM threads don't work, or JS interop calls error.
---

# WASM JavaScript Interop Debugger

## When to Use

- Browser build loads but features fail
- DuckDB WASM operations error
- Audio interop not working
- Threading features disabled or broken

## Key Locations

| Location                                    | Description               |
| ------------------------------------------- | ------------------------- |
| `src/BalatroSeedOracle.Browser/wwwroot/js/` | JS interop files          |
| `duckdb-interop.js`                         | DuckDB WASM bridge        |
| `webaudio-interop.js`                       | Web Audio API bridge      |
| `bso-helpers.js`                            | BSO utility functions     |
| `src/BalatroSeedOracle.Browser/Services/`   | Browser-specific services |

## Threading Requirements

### Required HTTP Headers

For WASM threads and SharedArrayBuffer to work:

```
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

### Quick Runtime Check

```javascript
// In browser console
console.log('Cross-origin isolated:', window.crossOriginIsolated);
console.log('SharedArrayBuffer:', typeof SharedArrayBuffer !== 'undefined');
```

If `crossOriginIsolated` is `false`, threading is disabled.

## Diagnostic Checklist

### 1. Verify Cross-Origin Isolation

| Check                        | Expected       | Problem If           |
| ---------------------------- | -------------- | -------------------- |
| `window.crossOriginIsolated` | `true`         | `false` = no threads |
| COOP header                  | `same-origin`  | Missing/wrong        |
| COEP header                  | `require-corp` | Missing/wrong        |

### 2. Check Browser Console

Common error patterns:

| Error                              | Cause                        | Fix                                      |
| ---------------------------------- | ---------------------------- | ---------------------------------------- |
| `SharedArrayBuffer is not defined` | Missing COOP/COEP headers    | Configure server headers                 |
| `Blocked by COEP`                  | Resource missing CORP header | Use `credentialless` mode or add headers |
| `wasm-function[...]` in stack      | No debug symbols             | Build with debug info                    |

### 3. Feature Detection

```javascript
// Check SIMD support
const simdSupported = WebAssembly.validate(new Uint8Array([
  0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,10,1,8,0,65,0,253,15,253,98,11
]));
console.log('SIMD:', simdSupported);

// Check threads
console.log('Threads:', typeof SharedArrayBuffer !== 'undefined' && crossOriginIsolated);
```

## Common Issues

### DuckDB WASM Failures

| Symptom          | Likely Cause            | Fix                       |
| ---------------- | ----------------------- | ------------------------- |
| Queries hang     | Missing threads support | Check COOP/COEP           |
| Silent failures  | Exception in WASM       | Check browser console     |
| Slow performance | No SIMD                 | Verify SIMD-enabled build |

### Audio Interop Issues

| Symptom        | Likely Cause           | Fix                                |
| -------------- | ---------------------- | ---------------------------------- |
| No sound       | AudioContext suspended | User interaction required          |
| Errors on init | Missing JS file        | Check `webaudio-interop.js` loaded |
| Crackling      | Buffer underruns       | Increase buffer size               |

### Asset Loading Failures

Under `require-corp` mode, all resources need CORP headers:

```
Cross-Origin-Resource-Policy: same-origin
```

Alternative: Use `credentialless` mode:

```
Cross-Origin-Embedder-Policy: credentialless
```

## Server Configuration

### Nginx

```nginx
location / {
    add_header Cross-Origin-Opener-Policy same-origin;
    add_header Cross-Origin-Embedder-Policy require-corp;
}
```

### Development (npx serve)

Create `serve.json`:

```json
{
  "headers": [
    {
      "source": "**/*",
      "headers": [
        { "key": "Cross-Origin-Opener-Policy", "value": "same-origin" },
        { "key": "Cross-Origin-Embedder-Policy", "value": "require-corp" }
      ]
    }
  ]
}
```

## Debug Build Settings

For better stack traces, build with debug info:

```xml
<!-- In Browser.csproj for debugging -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <WasmNativeStrip>false</WasmNativeStrip>
</PropertyGroup>
```

## Incompatible Hosts

These do **not** support COOP/COEP headers:

- GitHub Pages
- Basic static hosting without header control
- Some CDNs without custom header configuration

## Debugging Workflow

1. **Open browser DevTools** (F12)
2. **Check Console** for errors
3. **Check Network tab** for blocked resources
4. **Run isolation check** in Console
5. **Verify JS files loaded** in Sources tab
6. **Test interop calls** manually in Console

## Checklist

- [ ] Browser DevTools open, Console visible
- [ ] `crossOriginIsolated` returns `true`
- [ ] No `Blocked by COEP` errors in Console
- [ ] JS interop files loaded (`duckdb-interop.js`, etc.)
- [ ] Server configured with COOP/COEP headers
- [ ] Using compatible hosting (not GitHub Pages for threads)
