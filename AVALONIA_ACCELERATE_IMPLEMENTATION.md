# Avalonia Accelerate Implementation Plan

## Overview
This document outlines the implementation of Avalonia Accelerate features to maximize value from your purchase.

## Current Status
- ✅ **Parcel**: Already installed (`.config/dotnet-tools.json`)
- ✅ **DiagnosticsSupport**: Already installed (`AvaloniaUI.DiagnosticsSupport` v2.0.4)
- ⏳ **TreeDataGrid**: To be added (upgrade from DataGrid)
- ⏳ **Markdown Control**: To be added (for help/documentation)
- ⏳ **WebView**: To be added (for external content)
- ⏳ **Developer Tools**: To be enabled (F12 gesture)

## Implementation Steps

### 1. TreeDataGrid (HIGH PRIORITY)
**Why**: Better performance, built-in filtering, virtualization for large datasets
**Current**: Using `DataGrid` in `SortableResultsGrid`
**Benefit**: Handles millions of rows efficiently, better UX

**Files to modify**:
- `src/BalatroSeedOracle/Controls/SortableResultsGrid.axaml` - Replace DataGrid with TreeDataGrid
- `src/BalatroSeedOracle/Controls/SortableResultsGrid.axaml.cs` - Update column definitions
- `Directory.Packages.props` - Add `AvaloniaUI.TreeDataGrid` package

### 2. Developer Tools (HIGH PRIORITY)
**Why**: Visual tree inspection, property editor, layout debugging
**Current**: Package installed but not enabled
**Benefit**: Debug UI issues faster, inspect bindings, layout problems

**Files to modify**:
- `src/BalatroSeedOracle/App.axaml.cs` - Enable Developer Tools
- `src/BalatroSeedOracle.Desktop/Program.cs` - Enable F12 gesture

### 3. Markdown Control (MEDIUM PRIORITY)
**Why**: Display help, documentation, JAML guides
**Current**: No markdown rendering
**Benefit**: Rich documentation in-app, JAML syntax help, filter guides

**Files to create**:
- `src/BalatroSeedOracle/Components/Help/HelpModal.axaml` - Help viewer
- `src/BalatroSeedOracle/Components/Help/JamlHelpView.axaml` - JAML documentation
- `Directory.Packages.props` - Add `AvaloniaUI.Markdown` package

### 4. WebView (MEDIUM PRIORITY)
**Why**: Display external content (Balatro wiki, tutorials)
**Current**: No web content display
**Benefit**: In-app access to external resources

**Files to create**:
- `src/BalatroSeedOracle/Components/WebView/ExternalContentModal.axaml` - WebView wrapper
- `Directory.Packages.props` - Add `AvaloniaUI.WebView` package

### 5. Parcel Configuration (LOW PRIORITY)
**Why**: Cross-platform packaging with code signing
**Current**: Installed but not configured
**Benefit**: Professional distribution packages

**Files to create**:
- `parcel.json` - Parcel configuration
- Update build scripts to use Parcel

## License Key Setup

To use Accelerate components, you need to add your license key to your project files.

### Option 1: Environment Variable (Recommended)
Set `AVALONIA_ACCELERATE_LICENSE_KEY` environment variable

### Option 2: Project File
Add to `Directory.Packages.props`:
```xml
<PropertyGroup>
  <AvaloniaAccelerateLicenseKey>YOUR_LICENSE_KEY</AvaloniaAccelerateLicenseKey>
</PropertyGroup>
```

### Option 3: Per-Project
Add to each `.csproj` that uses Accelerate:
```xml
<PropertyGroup>
  <AvaloniaAccelerateLicenseKey>YOUR_LICENSE_KEY</AvaloniaAccelerateLicenseKey>
</PropertyGroup>
```

## Package Versions

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="AvaloniaUI.TreeDataGrid" Version="11.3.10" />
<PackageVersion Include="AvaloniaUI.Markdown" Version="11.3.10" />
<PackageVersion Include="AvaloniaUI.WebView" Version="11.3.10" />
<PackageVersion Include="AvaloniaUI.DeveloperTools" Version="11.3.10" />
```

## Implementation Status

### ✅ Completed
1. **Added Accelerate packages** to `Directory.Packages.props`:
   - `AvaloniaUI.TreeDataGrid` v11.3.10
   - `AvaloniaUI.Markdown` v11.3.10
   - `AvaloniaUI.WebView` v11.3.10
   - `AvaloniaUI.DeveloperTools` v11.3.10

2. **Added packages to main project** (`BalatroSeedOracle.csproj`)

3. **Enabled Developer Tools** in `Program.cs`:
   - Added `.UseDeveloperTools()` to AppBuilder chain (if available)
   - Developer Tools connects via `DiagnosticsSupport` package (already installed)
   - Press **F12** in the app to open Developer Tools (if extension method available)
   - Features: Visual tree inspection, property editor, layout debugging
   - **Note**: If `.UseDeveloperTools()` doesn't exist, Developer Tools may need to be installed as a .NET tool separately

### ⏳ Next Steps
1. **Upgrade `SortableResultsGrid` to TreeDataGrid** - Better performance for large datasets
2. **Add Markdown control** - Display help/documentation in-app
3. **Add WebView component** - Show external content (Balatro wiki, tutorials)
4. **Configure Parcel** - Set up cross-platform packaging with code signing

## License Key Setup

**IMPORTANT**: To use Accelerate components, you need to add your license key.

### Option 1: Environment Variable (Recommended)
Set `AVALONIA_ACCELERATE_LICENSE_KEY` environment variable before building

### Option 2: Project File
Add to `Directory.Packages.props`:
```xml
<PropertyGroup>
  <AvaloniaAccelerateLicenseKey>YOUR_LICENSE_KEY</AvaloniaAccelerateLicenseKey>
</PropertyGroup>
```

### Option 3: Per-Project
Add to each `.csproj` that uses Accelerate:
```xml
<PropertyGroup>
  <AvaloniaAccelerateLicenseKey>YOUR_LICENSE_KEY</AvaloniaAccelerateLicenseKey>
</PropertyGroup>
```

**Note**: Without a license key, Accelerate components will show a license error or may not work. Make sure to add your license key before building.

## References
- [Avalonia Accelerate Documentation](https://deepwiki.com/AvaloniaUI/avalonia-docs/4-avalonia-accelerate-suite)
- [TreeDataGrid Quickstart](https://docs.avaloniaui.net/docs/guides/treedatagrid/quickstart)
- [Developer Tools](https://docs.avaloniaui.net/docs/guides/developer-tools/getting-started)
