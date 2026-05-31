# Handoff: Building & Running BSO on macOS (Apple Silicon)

> For Claude Code CLI picking this up on a Mac. Written 2026-05-31 from a Windows 11
> box (cross-publishing). The target Mac is **Apple Silicon (arm64)**.

## TL;DR

The app runs on macOS via the existing **`BalatroSeedOracle.Desktop`** head — there is
**no separate macOS project** and you don't need one. It's plain `Avalonia.Desktop`, no
`#if` platform splits, and **Native AOT is off** (`PublishAot=false`, self-contained JIT),
so it builds and runs on macOS like any other desktop .NET app.

```sh
# Dev run, straight from source on the Mac:
dotnet run -c Release --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj \
  --source https://api.nuget.org/v3/index.json
```

The `--source` flag matters — see the NuGet gotcha below. Without it, restore fails.

## What changed leading into this handoff

The **Browser (WASM) head was removed** — it was failing at the emscripten link step
(`emcc ... exited with code 1`, MSB3073) and the user asked to nuke it. Removed:

- `src/BalatroSeedOracle.Browser/` (entire project)
- `.github/workflows/{build,deploy,release}-browser.yml`
- `vercel.json`
- Browser entries scrubbed from `BalatroSeedOracle.sln`, `.vscode/launch.json`, `.vscode/tasks.json`

After removal, **`dotnet build BalatroSeedOracle.sln -c Release` → 0 warnings, 0 errors.**
Remaining heads: Desktop, Android, iOS, plus Motely (engine/tests/TUI).

Doc mentions of the browser head still exist in prose (`README.md`, `docs/ARCHITECTURE.md`,
`CLAUDE.md`, `DEPLOYMENT_SETUP.md`, etc.) — not build-breaking, left as-is.

## ⚠️ NuGet gotcha (this will bite you on a fresh restore)

`src/MotelyJAML/nuget.config` declares two **local** Bootsharp feeds:

```
D:\bootsharp\src\cs\.nuget
D:\extra\bootsharp\cs\.nuget
```

These exist only on the maintainer's WASM-build machine. They do **not** exist on the Mac.
A fresh restore tries every source and hard-fails:

- Plain restore → `NU1301: The local source '...' doesn't exist`
- `--ignore-failed-sources` → downgrades to `NU1801`, but `TreatWarningsAsErrors=true`
  promotes it right back to an error.

**Fix: override the source list for restore/build/publish/run** with just nuget.org:

```sh
--source https://api.nuget.org/v3/index.json
```

The macOS desktop build pulls only public packages (Avalonia, DuckDB, SoundFlow), none from
Bootsharp, so nuget.org alone is sufficient.

**DO NOT edit `src/MotelyJAML/nuget.config`.** Those local feeds are intentional for the
Motely WASM build on the maintainer's machine. Leave it untouched; use `--source` instead.
(The Bootsharp setup is the maintainer's WASM toolchain — not your concern for the desktop app.)

## Producing a runnable build on the Mac

### Quick dev loop
```sh
dotnet run -c Release --project src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj \
  --source https://api.nuget.org/v3/index.json
```

### Self-contained publish (no .NET install needed to run)
```sh
dotnet publish src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj \
  -c Release -r osx-arm64 --self-contained \
  --source https://api.nuget.org/v3/index.json
# Output: src/BalatroSeedOracle.Desktop/bin/Release/net10.0/osx-arm64/publish/
```

This same command was already run successfully from Windows (cross-compile) — output landed
in `bin/Release/net10.0/osx-arm64/publish/`. Native deps (DuckDB, SoundFlow) come down as
osx-arm64 binaries with the `-r osx-arm64` publish.

### Running an unsigned cross-built binary (Gatekeeper)
If you copied a publish folder over (e.g. via Parsec) rather than building on the Mac:
```sh
chmod +x BalatroSeedOracle.Desktop          # exec bit is lost in transfer
xattr -dr com.apple.quarantine .            # strip quarantine or Gatekeeper blocks it
./BalatroSeedOracle.Desktop
```
It's a bare executable, not a `.app` bundle — launch from Terminal.

### Proper `.app` + DMG (macOS only)
Packaging uses Parcel and needs macOS tooling (`hdiutil`), so it only works on a real Mac:
```sh
cd src
dotnet tool restore
dotnet parcel pack BalatroSeedOracle.parcel --runtimes osx-arm64 --packages dmg
# (the now-manual .github/workflows/release-macos.yml does this in CI on macos-latest)
```
`PARCEL_LICENSE` / `PARCEL_LICENSE_KEY` must supply the Avalonia license at pack time.
Signing/notarization is not set up here; for personal use, ad-hoc run is fine.

## House rules (still apply)

- **Never run Motely** (`Motely.CLI`/`TUI`/WASM) — searches burn huge time. Build to verify; don't "run a search to check."
- `TreatWarningsAsErrors=true`, `Nullable=enable` — keep it warning-clean.
- Logging via `DebugLogger` only.
- macOS CI (`release-macos.yml`) is currently left as manual `workflow_dispatch` — the macOS
  runner minutes were getting expensive. Re-enable later if desired.
