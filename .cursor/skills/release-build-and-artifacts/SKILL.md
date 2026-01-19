---
name: release-build-and-artifacts
description: Prepares releases using GitHub Actions workflows. Use when cutting releases, troubleshooting CI artifacts, or understanding the release process.
---

# Release Build and Artifacts

## Release Trigger

Releases are triggered by Git tags matching `v*`:

```bash
git tag v2.0.1
git push origin v2.0.1
```

## Workflow Structure

```
.github/workflows/
├── release.yml           # Main orchestrator
├── release-windows.yml   # Windows build
├── release-linux.yml     # Linux build
├── release-browser.yml   # Browser/WASM build
└── release-macos.yml     # macOS build (commented out)
```

## Main Workflow (release.yml)

```yaml
name: Complete Release Build

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  build-windows:
    uses: ./.github/workflows/release-windows.yml
    secrets: inherit

  build-linux:
    uses: ./.github/workflows/release-linux.yml
    secrets: inherit

  build-browser:
    uses: ./.github/workflows/release-browser.yml
    secrets: inherit

  create-release:
    needs: [build-windows, build-linux, build-browser]
    runs-on: ubuntu-latest
    steps:
    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        files: artifacts/**/*
        generate_release_notes: true
```

## Browser Release Workflow

```yaml
- name: Build Browser Version
  run: dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish/browser --no-restore

- name: Create Browser Archive
  run: |
    cd publish/browser
    7z a -tzip ../../BalatroSeedOracle-Browser.zip *

- name: Upload Browser Artifact
  uses: actions/upload-artifact@v4
  with:
    name: BalatroSeedOracle-Browser
    path: BalatroSeedOracle-Browser.zip
```

## Artifacts Produced

| Platform | Artifact Name                      | Contents           |
| -------- | ---------------------------------- | ------------------ |
| Windows  | `BalatroSeedOracle-Windows`        | Windows executable |
| Linux    | `BalatroSeedOracle-Linux`          | Linux executable   |
| Browser  | `BalatroSeedOracle-Browser`        | WASM build zip     |
| Browser  | `BalatroSeedOracle-Browser-Deploy` | Deployment package |

## Local Build Commands

### Desktop (Debug)

```bash
dotnet run -c Debug --project ./src/BalatroSeedOracle.csproj
```

### Desktop (Release)

```bash
dotnet run -c Release --project ./src/BalatroSeedOracle.csproj
```

### Desktop Publish

```bash
dotnet publish src/BalatroSeedOracle.Desktop/BalatroSeedOracle.Desktop.csproj -c Release -o publish/desktop
```

### Browser Publish

```bash
dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish/browser
```

## Pre-Release Tags

Tags containing `-rc` are marked as pre-releases:

```yaml
prerelease: ${{ contains(github.ref, '-rc') }}
```

```bash
git tag v2.0.1-rc1  # Pre-release
git tag v2.0.1      # Full release
```

## Troubleshooting CI

### Check Workflow Status

1. Go to Actions tab in GitHub
2. Find the release workflow run
3. Check individual job logs

### Common Issues

| Issue                 | Cause                | Fix                                           |
| --------------------- | -------------------- | --------------------------------------------- |
| Build fails           | Missing dependencies | Run `dotnet restore` locally                  |
| Submodule missing     | Not initialized      | Add `git submodule update --init --recursive` |
| Artifact upload fails | Path mismatch        | Verify output directory in publish command    |

### Manual Workflow Trigger

Use `workflow_dispatch` to trigger without a tag:

1. Go to Actions → Complete Release Build
2. Click "Run workflow"
3. Select branch

## Checklist

- [ ] All tests pass locally
- [ ] Version tag follows `v*.*.*` format
- [ ] Submodules are up to date
- [ ] Build succeeds for all platforms
- [ ] Artifacts uploaded correctly
- [ ] Release notes are accurate
