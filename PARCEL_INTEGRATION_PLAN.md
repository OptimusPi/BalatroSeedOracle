# Parcel Integration Plan for BalatroSeedOracle

**Last Updated:** 2025-01-28
**Status:** Ready to implement
**License:** Paid license available (use `PARCEL_LICENSE_KEY` env var)

---

## What is Parcel?

Parcel is AvaloniaUI's official packaging tool that creates installer packages for:
- **Windows:** NSIS installers (.exe)
- **macOS:** DMG installers (.dmg)
- **Linux:** DEB packages (.deb)
- **Cross-platform:** ZIP archives

---

## Step 1: Create Parcel Project File

Create a new file: `BalatroSeedOracle.parcel.json`

```json
{
  "displayName": "Balatro Seed Oracle",
  "publisher": "pifreak",
  "version": "1.0.0",
  "identifier": "com.pifreak.balatroseedoracle",
  "description": "Balatro seed searching and analysis tool powered by Motely",
  "icon": "src/Assets/icon.png",

  "input": {
    "project": "src/BalatroSeedOracle.csproj",
    "configuration": "Release"
  },

  "output": {
    "directory": "bin/packages"
  },

  "runtimes": [
    "win-x64",
    "osx-x64",
    "osx-arm64",
    "linux-x64"
  ],

  "packages": {
    "win-x64": ["nsis", "zip"],
    "osx-x64": ["dmg"],
    "osx-arm64": ["dmg"],
    "linux-x64": ["deb", "zip"]
  },

  "publish": {
    "selfContained": true,
    "publishTrimmed": false,
    "publishSingleFile": false,
    "includeNativeLibrariesForSelfExtract": true
  },

  "nsis": {
    "installScope": "currentUser",
    "createDesktopShortcut": true,
    "createStartMenuShortcut": true,
    "compressionLevel": "maximum",
    "licensePath": "LICENSE.txt"
  },

  "dmg": {
    "signIdentity": null,
    "backgroundColor": "#1a1a1a",
    "iconSize": 100,
    "windowSize": {
      "width": 600,
      "height": 400
    }
  },

  "deb": {
    "maintainer": "pifreak",
    "section": "utils",
    "priority": "optional",
    "dependencies": []
  }
}
```

---

## Step 2: Prepare Assets

### Icon Requirements
- **Windows NSIS:** 256x256 PNG or ICO
- **macOS DMG:** 1024x1024 PNG (for Retina displays)
- **Linux DEB:** 256x256 PNG

**TODO:** Create/verify icon file at `src/Assets/icon.png`

### License File
**TODO:** Create `LICENSE.txt` in project root if not exists

---

## Step 3: Update .csproj for Packaging

Add to `src/BalatroSeedOracle.csproj`:

```xml
<PropertyGroup>
  <!-- App metadata for installers -->
  <ApplicationVersion>1.0.0</ApplicationVersion>
  <Company>pifreak</Company>
  <Product>Balatro Seed Oracle</Product>
  <Copyright>Copyright © 2025 pifreak</Copyright>

  <!-- Icon for Windows executable -->
  <ApplicationIcon>Assets\icon.ico</ApplicationIcon>

  <!-- macOS bundle info -->
  <CFBundleName>Balatro Seed Oracle</CFBundleName>
  <CFBundleDisplayName>Balatro Seed Oracle</CFBundleDisplayName>
  <CFBundleIdentifier>com.pifreak.balatroseedoracle</CFBundleIdentifier>
  <CFBundleVersion>1.0.0</CFBundleVersion>
  <CFBundleShortVersionString>1.0.0</CFBundleShortVersionString>
</PropertyGroup>
```

---

## Step 4: Setup License Key

### Option 1: Environment Variable (Recommended)
```powershell
# PowerShell
$env:PARCEL_LICENSE_KEY = "your-license-key-here"

# Or add to profile permanently
[System.Environment]::SetEnvironmentVariable('PARCEL_LICENSE_KEY', 'your-key', 'User')
```

### Option 2: Pass via CLI
```bash
parcel pack BalatroSeedOracle.parcel.json --license-key "your-key"
```

---

## Step 5: Install Required Tools

Parcel needs platform-specific tools to create installers.

### Windows (NSIS)
```powershell
parcel install-tools
```
This downloads NSIS automatically.

### macOS (DMG)
```bash
# Requires macOS SDK tools (xcode-select)
xcode-select --install
parcel install-tools
```

### Linux (DEB)
```bash
# Requires dpkg-deb (usually pre-installed)
sudo apt-get install dpkg-deb
parcel install-tools
```

---

## Step 6: Build Packages

### Build All Platforms
```bash
parcel pack BalatroSeedOracle.parcel.json
```

### Build Specific Platform
```bash
# Windows only
parcel pack BalatroSeedOracle.parcel.json -r win-x64 -p nsis

# macOS only
parcel pack BalatroSeedOracle.parcel.json -r osx-arm64 -p dmg

# Linux only
parcel pack BalatroSeedOracle.parcel.json -r linux-x64 -p deb
```

### Build Without Recompiling
```bash
# If you already built with dotnet publish
parcel pack BalatroSeedOracle.parcel.json --no-build
```

---

## Step 7: Output Structure

After running `parcel pack`, you'll get:

```
bin/packages/
├── win-x64/
│   ├── BalatroSeedOracle-1.0.0-win-x64.exe     (NSIS installer)
│   └── BalatroSeedOracle-1.0.0-win-x64.zip     (Portable)
├── osx-x64/
│   └── BalatroSeedOracle-1.0.0-osx-x64.dmg
├── osx-arm64/
│   └── BalatroSeedOracle-1.0.0-osx-arm64.dmg
└── linux-x64/
    ├── balatroseedoracle_1.0.0_amd64.deb
    └── BalatroSeedOracle-1.0.0-linux-x64.zip
```

---

## Step 8: CI/CD Integration (GitHub Actions)

Create `.github/workflows/release.yml`:

```yaml
name: Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    strategy:
      matrix:
        os: [windows-latest, macos-latest, ubuntu-latest]

    runs-on: ${{ matrix.os }}

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Install Parcel
      run: dotnet tool install --global AvaloniaUI.Parcel.Windows

    - name: Install Tools
      run: parcel install-tools

    - name: Build Packages
      run: parcel pack BalatroSeedOracle.parcel.json
      env:
        PARCEL_LICENSE_KEY: ${{ secrets.PARCEL_LICENSE_KEY }}

    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: packages-${{ matrix.os }}
        path: bin/packages/**
```

---

## Step 9: Signing (Optional but Recommended)

### Windows Code Signing
```json
// In BalatroSeedOracle.parcel.json
"nsis": {
  "signTool": {
    "certificatePath": "path/to/certificate.pfx",
    "certificatePassword": "${CERT_PASSWORD}",
    "timestampServer": "http://timestamp.digicert.com"
  }
}
```

### macOS Code Signing
```json
// In BalatroSeedOracle.parcel.json
"dmg": {
  "signIdentity": "Developer ID Application: Your Name (TEAMID)",
  "notarize": true,
  "appleId": "${APPLE_ID}",
  "appleIdPassword": "${APPLE_PASSWORD}",
  "teamId": "${APPLE_TEAM_ID}"
}
```

---

## Step 10: Testing Packages

### Windows
1. Run the NSIS installer `.exe`
2. Verify installation to `%LOCALAPPDATA%\Programs\BalatroSeedOracle`
3. Test start menu shortcut
4. Test desktop shortcut
5. Test uninstaller

### macOS
1. Open the DMG
2. Drag app to Applications folder
3. Right-click → Open (first time to bypass Gatekeeper)
4. Verify app runs

### Linux
1. Install DEB: `sudo dpkg -i balatroseedoracle_1.0.0_amd64.deb`
2. Run from terminal: `balatroseedoracle`
3. Verify desktop launcher appears

---

## Troubleshooting

### "License key not found"
**Solution:** Set `PARCEL_LICENSE_KEY` environment variable

### "NSIS not found" (Windows)
**Solution:** Run `parcel install-tools`

### "Invalid icon format"
**Solution:** Convert icon to PNG with correct dimensions (256x256 for Windows, 1024x1024 for macOS)

### "Build failed: missing dependencies"
**Solution:** Ensure Motely native library is included in publish output

### Large Package Size
**Solution:**
- Enable trimming: `"publishTrimmed": true`
- Enable single-file: `"publishSingleFile": true`
- Trade-off: May break reflection-based code

---

## Implementation Checklist

- [ ] Create `BalatroSeedOracle.parcel.json` config file
- [ ] Prepare icon assets (256x256 PNG, 1024x1024 PNG, ICO)
- [ ] Create/verify LICENSE.txt file
- [ ] Update .csproj with app metadata
- [ ] Set `PARCEL_LICENSE_KEY` environment variable
- [ ] Run `parcel install-tools`
- [ ] Test build: `parcel pack BalatroSeedOracle.parcel.json -r win-x64 -p zip`
- [ ] Verify ZIP package works
- [ ] Build NSIS installer: `parcel pack BalatroSeedOracle.parcel.json -r win-x64 -p nsis`
- [ ] Test NSIS installer on clean Windows VM
- [ ] Build macOS packages (if you have a Mac)
- [ ] Build Linux packages
- [ ] Setup CI/CD (optional)
- [ ] Setup code signing (optional but recommended)

---

## Estimated Time

- **Initial setup:** 30 minutes
- **First successful build:** 1 hour (including troubleshooting)
- **CI/CD setup:** 2 hours
- **Code signing setup:** 3-4 hours (certificate acquisition + config)

---

## Next Steps

1. **START HERE:** Create `BalatroSeedOracle.parcel.json` in project root
2. Prepare icon assets
3. Run first test build with ZIP output
4. Iterate until installer works
5. Add code signing for production releases

---

**Ready to package your app for distribution! 🚀**
