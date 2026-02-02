# BalatroSeedOracle.Browser Deployment

## Build

```bash
cd src/BalatroSeedOracle.Browser
dotnet publish -c Release
```

Output: `bin/Release/net10.0-browser/publish/wwwroot/`

## Required HTTP Headers

**Multi-threading requires these headers on EVERY response:**

```
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

Without these, `SharedArrayBuffer` is disabled and threading fails.

## Local Development

Use the DevServer (auto-adds headers):

```bash
cd src/BalatroSeedOracle.Browser.DevServer
dotnet run
```

Or use `serve` with the included `serve.json`:

```bash
npx serve bin/Release/net10.0-browser/publish/wwwroot -c ../serve.json
```

## Production Hosting

### Cloudflare Pages

Add `_headers` file to `wwwroot/`:

```
/*
  Cross-Origin-Opener-Policy: same-origin
  Cross-Origin-Embedder-Policy: require-corp
```

### Nginx

```nginx
location / {
    add_header Cross-Origin-Opener-Policy "same-origin" always;
    add_header Cross-Origin-Embedder-Policy "require-corp" always;
    try_files $uri $uri/ /index.html;
}
```

### Azure Static Web Apps

Add `staticwebapp.config.json`:

```json
{
  "globalHeaders": {
    "Cross-Origin-Opener-Policy": "same-origin",
    "Cross-Origin-Embedder-Policy": "require-corp"
  }
}
```

## WASM Features Enabled

- **AOT Compilation**: Faster execution, larger bundle
- **SIMD**: Vector math acceleration
- **Multi-threading**: Parallel search execution
- **Trimming**: Smaller bundle size

## Troubleshooting

- **SharedArrayBuffer error**: Headers missing or accessing via IP instead of localhost
- **libSkiaSharp DllNotFoundException**: Run `dotnet workload install wasm-tools` and rebuild
- **Slow first load**: AOT bundle is large (~30MB), but executes fast after load
