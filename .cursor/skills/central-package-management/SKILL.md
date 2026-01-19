---
name: central-package-management
description: Manages NuGet dependencies using Central Package Management. Use when adding, upgrading, or troubleshooting NuGet packages.
---

# Central Package Management

## Overview

This project uses **Central Package Management (CPM)** via `Directory.Packages.props` at the repository root.

## Core Rules

### Don't Add Version in .csproj

```xml
<!-- ❌ Bad - Version inline -->
<PackageReference Include="Avalonia" Version="11.3.10" />

<!-- ✅ Good - Version managed centrally -->
<PackageReference Include="Avalonia" />
```

### Add Versions to Directory.Packages.props

```xml
<!-- In Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="NewPackage" Version="1.0.0" />
</ItemGroup>
```

## Adding a New Package

### Step 1: Add to Directory.Packages.props

```xml
<ItemGroup>
  <!-- Existing packages... -->
  <PackageVersion Include="MyNewPackage" Version="2.0.0" />
</ItemGroup>
```

### Step 2: Reference in .csproj (without version)

```xml
<ItemGroup>
  <PackageReference Include="MyNewPackage" />
</ItemGroup>
```

### Step 3: Restore

```bash
dotnet restore
```

## Upgrading a Package

1. Edit version in `Directory.Packages.props`
2. Run `dotnet restore`
3. Test build: `dotnet build`

## Directory.Packages.props Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Avalonia UI -->
    <PackageVersion Include="Avalonia" Version="11.3.10" />
    <PackageVersion Include="Avalonia.Desktop" Version="11.3.10" />
    <PackageVersion Include="Avalonia.Browser" Version="11.3.10" />
    
    <!-- MVVM -->
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    
    <!-- Database -->
    <PackageVersion Include="DuckDB.NET.Data.Full" Version="1.4.1" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="Moq" Version="4.20.72" />
  </ItemGroup>
</Project>
```

## Checking Outdated Packages

```bash
dotnet list package --outdated
```

## Global Tools

Tools are defined in `.config/dotnet-tools.json`:

```bash
# Restore tools
dotnet tool restore

# List installed tools
dotnet tool list
```

## Common Issues

### Build Error: "Version already defined"

**Cause**: Version specified in both `.csproj` and `Directory.Packages.props`

**Fix**: Remove `Version` attribute from `.csproj`

### Package Not Found

**Cause**: Package not added to `Directory.Packages.props`

**Fix**: Add `<PackageVersion Include="..." Version="..." />` entry

### Restore Fails

```bash
# Clean and restore
dotnet nuget locals all --clear
dotnet restore
```

## Target Framework

All projects target **.NET 10**:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

## Checklist

- [ ] Version ONLY in `Directory.Packages.props`
- [ ] No version attribute in `.csproj` files
- [ ] `dotnet restore` succeeds
- [ ] `dotnet build` succeeds
- [ ] Tested after package changes
