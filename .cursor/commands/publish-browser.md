# Publish Browser

Publish the BalatroSeedOracle Browser/WASM application for deployment.

## Input

Optional:

- `threaded` - Enable WASM threading support

## Steps

1. **Publish (Non-Threaded - Default)**
   ```bash
   dotnet publish -c Release ./src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
   ```

   Output location: `./src/BalatroSeedOracle.Browser/bin/Release/net10.0-browser/browser-wasm/AppBundle/`

2. **Publish (Threaded)**
   ```bash
   dotnet publish -c Release -p:EnableWasmThreads=true ./src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
   ```

3. **Deployment Requirements**

   For **non-threaded** builds:
   - Standard static file hosting works

   For **threaded** builds, server MUST set these headers:
   ```
   Cross-Origin-Opener-Policy: same-origin
   Cross-Origin-Embedder-Policy: require-corp
   ```

4. **Verify Output**
   - Check `AppBundle/` directory contains:
     - `index.html`
     - `dotnet.js`
     - `*.wasm` files
     - `_framework/` directory

## Output

Publishable static files in `AppBundle/` directory.

## Notes

- **Threaded vs Non-Threaded**: Threading enables `SharedArrayBuffer` for parallel seed searching but requires COOP/COEP headers. Non-threaded is more compatible but slower.
- **AOT Compilation**: Release builds use AOT which significantly increases build time but improves runtime performance.
- **File size**: WASM bundles can be large (10-50MB+). Ensure hosting supports appropriate transfer limits.
- **GitHub Pages**: See `.github/workflows/deploy-browser.yml` for automated deployment configuration.
