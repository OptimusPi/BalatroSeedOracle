# Run Desktop

Run the main BalatroSeedOracle desktop application.

## Input

Optional:

- `debug` - Run in Debug configuration instead of Release

## Steps

1. **Run the Application**

   Default (Release - recommended for normal use/perf validation):
   ```bash
   dotnet run -c Release --project ./src/BalatroSeedOracle/BalatroSeedOracle.csproj
   ```

   Debug (choose this when you need breakpoints, stepping, extra diagnostics, or to reproduce a bug under a debugger):
   ```bash
   dotnet run -c Debug --project ./src/BalatroSeedOracle/BalatroSeedOracle.csproj
   ```

2. **Verify Launch**
   - Watch for startup errors in terminal
   - **The process should keep running** (a GUI app stays open until the user closes the window)
   - If the process exits quickly (exit code 0 or otherwise), the app did **not** launch successfully—check logs
   - Only claim "launched" after observing:
     - The application window is visible, OR
     - The process is still running (`ps` or terminal shows no exit)

## Output

Desktop application launches with main window visible and process remains running.

## Notes

- **Which config to use**: Use **Debug** when you're debugging (breakpoints/stepping/diagnostics). Use **Release** when validating "normal user experience" or performance-sensitive behavior. Debug is significantly slower due to disabled optimizations and additional runtime checks.
- **First run**: May take longer due to JIT compilation and asset loading.
- **Platform**: This targets the native desktop runtime (Windows/macOS/Linux).
- **Launch verification**: Exit code 0 alone does **not** prove the app launched—for GUI apps, the process should remain running. A fast exit typically indicates a startup failure.
