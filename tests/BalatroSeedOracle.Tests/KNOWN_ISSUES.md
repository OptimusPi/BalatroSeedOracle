# Known Issues

## Motely Submodule Build Error

**Status**: Pre-existing issue (affects all builds, not just tests)

**Symptom**: Build fails with error:

```
error CS0234: The type or namespace name 'DuckDB' does not exist in the namespace 'Motely'
```

**Root Cause**: The Motely submodule has a dependency issue with DuckDB.NET.Data. The parent repo uses DuckDB.NET.Data.Full v1.4.1, but Motely uses v1.1.3.

**Impact**:

- Local builds fail for test project, core library, and desktop project
- CI builds will fail until this is resolved
- This blocks the test gate from being fully functional

**Workaround**: None currently. This needs to be fixed in the Motely submodule or the dependency versions need to be aligned.

**Next Steps**:

1. Investigate if Motely can be updated to use DuckDB.NET.Data.Full v1.4.1
2. Or downgrade parent repo to use v1.1.3 (may break other functionality)
3. Or fix the Motely submodule's package references
4. Update the submodule commit once fixed

**Related Files**:

- `Directory.Packages.props` (parent repo) - defines DuckDB.NET.Data.Full v1.4.1
- `external/Motely/Directory.Packages.props` - defines DuckDB.NET.Data.Full v1.1.3
- `external/Motely/Motely/Motely.csproj` - references Motely package

**Tracking**: This issue should be tracked separately from the test infrastructure setup.
