# Avalonia License Key Setup

This document explains how to properly configure your Avalonia Accelerate license key for BalatroSeedOracle.

## Overview

The license key is used to enable Avalonia Accelerate components:
- **TreeDataGrid** - High-performance hierarchical data display
- **Markdown** - Markdown content rendering
- **WebView** - Web content embedding
- **Parcel** - Cross-platform packaging tool

## Configuration Methods

### Method 1: Environment Variable (Recommended for Development)

Set the `AVALONIA_LICENSE_KEY` environment variable before building:

**PowerShell:**
```powershell
$env:AVALONIA_LICENSE_KEY = "your_license_key_here"
dotnet build
```

**Command Prompt (cmd):**
```cmd
set AVALONIA_LICENSE_KEY=your_license_key_here
dotnet build
```

**Bash (Linux/macOS):**
```bash
export AVALONIA_LICENSE_KEY="your_license_key_here"
dotnet build
```

### Method 2: avalonia.license.local File (For CI/CD or Persistent Setup)

1. Copy the example file:
   ```bash
   cp avalonia.license.local.example avalonia.license.local
   ```

2. Add your license key to `avalonia.license.local`:
   ```
   avln_on_key:v1:YOUR_LICENSE_KEY_HERE
   ```

3. The file is already excluded from git in `.gitignore`

### Method 3: For Parcel (Packaging)

When using Parcel CLI to create installers:

```powershell
parcel pack ./BalatroSeedOracle.parcel --license-key YOUR_LICENSE_KEY
```

Or via environment variable:
```powershell
$env:PARCEL_LICENSE_KEY = "YOUR_LICENSE_KEY"
parcel pack ./BalatroSeedOracle.parcel
```

## Architecture

The license key is configured in `Directory.Build.props` (centralized) and applied to all projects:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <AvaloniaUILicenseKey Condition="'$(AVALONIA_LICENSE_KEY)' != ''">$(AVALONIA_LICENSE_KEY)</AvaloniaUILicenseKey>
</PropertyGroup>
```

Each executable project then applies the license:

```xml
<!-- src/BalatroSeedOracle/BalatroSeedOracle.csproj -->
<ItemGroup>
  <AvaloniaUILicenseKey Include="$(AvaloniaUILicenseKey)" Condition="'$(AvaloniaUILicenseKey)' != ''" />
</ItemGroup>
```

## Security Notes

- **NEVER** commit `avalonia.license` or `avalonia.license.local` to git
- Both files are excluded in `.gitignore`
- For CI/CD pipelines, use environment variables or secrets management
- The license key is sensitive - treat it like a password

## Getting Your License Key

If you don't have a license key:

1. Visit [Avalonia Accelerate](https://avaloniaui.net/accelerate/)
2. Sign up or log in
3. Download your license key
4. Keep it safe and secure

## Troubleshooting

**License Not Being Recognized:**
- Verify `AVALONIA_LICENSE_KEY` environment variable is set: `echo $env:AVALONIA_LICENSE_KEY`
- Check that `Directory.Build.props` exists in the root directory
- Rebuild the solution: `dotnet clean && dotnet build`

**Build Errors with Accelerate Components:**
- Ensure all executable projects have the `<AvaloniaUILicenseKey>` ItemGroup
- The error typically appears when TreeDataGrid, WebView, or Markdown packages are used without a valid key

**For Browser/WASM Builds:**
- The license key is applied the same way for `BalatroSeedOracle.Browser`
- No special configuration needed beyond the environment variable setup

## References

- [Avalonia Accelerate Documentation](https://docs.avaloniaui.net/accelerate/)
- [Avalonia License Key Installation](https://docs.avaloniaui.net/accelerate/installation)
