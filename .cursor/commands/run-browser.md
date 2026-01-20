# Run Browser

Run the BalatroSeedOracle Browser/WASM application locally.

## Input

None required.

## Steps

1. **Run the Browser Application**
   ```bash
   dotnet run -c Debug --project ./src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
   ```

2. **Open in Browser**
   - Terminal will show the local URL (typically `http://localhost:5000` or similar)
   - Open the URL in a modern browser (Chrome/Edge recommended for WASM)

3. **Monitor Output**
   - Watch terminal for build/runtime errors
   - Check browser DevTools console for WASM-specific issues

## Output

Browser application accessible at local development URL.

## Notes

- **Debug configuration**: Browser uses Debug by default for faster iteration; Release requires full AOT compilation.
- **First load**: Initial WASM download can be slow; subsequent loads use browser cache.
- **Browser compatibility**: Works best in Chromium-based browsers. Firefox supported but may have performance differences.
- **Threading**: Default non-threaded build. For threaded builds, see `publish-browser` command.
