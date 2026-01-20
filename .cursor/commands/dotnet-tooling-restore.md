# .NET Tooling Restore

Restore local .NET tools defined in the repository.

## Input

None required.

## Steps

1. **Restore Local Tools**
   ```bash
   dotnet tool restore
   ```

2. **Verify Installation**
   ```bash
   dotnet tool list
   ```

   Should show tools from `.config/dotnet-tools.json`.

3. **Check Tool Manifest**

   Review `.config/dotnet-tools.json` for expected tools:
   ```bash
   cat .config/dotnet-tools.json
   ```

## Output

All local .NET tools restored and available.

## Notes

- **When to run**: After fresh clone, after pulling changes that modify `.config/dotnet-tools.json`, or when tool commands fail with "not found".
- **Global vs Local**: This restores LOCAL tools scoped to this repo. Global tools are managed separately with `dotnet tool install -g`.
- **Version pinning**: Tools are version-pinned in the manifest for reproducible builds.
- **CI/CD**: Restore runs automatically in GitHub Actions workflows.
