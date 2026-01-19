# Triage Browser vs Desktop

Checklist for debugging platform-specific behavior differences between Browser/WASM and Desktop targets.

## Input

- Description of the issue
- Which platform exhibits the problem (Browser, Desktop, or both)
- Relevant code files

## Steps

1. **Check IPlatformServices Capabilities**

   Use capability checks, not platform detection:
   ```csharp
   var platform = App.GetService<IPlatformServices>();
   
   if (platform.SupportsFileSystem)
   {
       // File operations
   }
   
   if (platform.SupportsAudio)
   {
       // Audio playback
   }
   ```

2. **Review Platform Service Implementation**

   Check which implementation is registered:
   - Desktop: `src/BalatroSeedOracle.Desktop/Services/`
   - Browser: `src/BalatroSeedOracle.Browser/Services/`

3. **Verify Conditional Compilation (Last Resort)**

   Only use when truly necessary:
   ```csharp
   #if BROWSER
       // Browser-only code
   #else
       // Desktop code
   #endif
   ```

   Prefer `IPlatformServices` over `#if` when possible.

4. **Check Unsupported Operations**

   Common Browser limitations:
   - No native file system access (use file picker APIs)
   - No native dialogs (use in-app modals)
   - Audio requires user interaction to start
   - Threading requires COOP/COEP headers
   - No process spawning

5. **Verify JavaScript Interop (Browser)**

   If using JS interop:
   ```csharp
   [JSImport("functionName", "moduleName")]
   internal static partial void CallJsFunction();
   ```

   Check browser DevTools console for JS errors.

6. **Test Both Platforms**

   ```bash
   # Desktop
   dotnet run -c Release --project ./src/BalatroSeedOracle/BalatroSeedOracle.csproj
   
   # Browser
   dotnet run -c Debug --project ./src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj
   ```

7. **Add Platform-Aware Logging**

   ```csharp
   DebugLogger.Log("Feature", $"Platform: {platform.PlatformName}, SupportsX: {platform.SupportsX}");
   ```

8. **Check WASM-Specific Issues**

   - Memory limits (browser heap is constrained)
   - Startup time (AOT helps but initial load is slow)
   - No synchronous I/O (everything must be async)

## Output

Identified platform-specific issue and appropriate fix strategy.

## Notes

- **Design principle**: Features should degrade gracefully on Browser, not crash.
- **Capability over platform**: Check what's supported, not which platform you're on.
- **Testing**: Always test both platforms after changes to shared code.
- **Related rule**: See `@.cursor/rules/030-platform-guards.mdc` for platform guard conventions.
- **Related skill**: See `@.cursor/skills/apply-platform-guards-browser-vs-desktop/SKILL.md` for implementation patterns.
