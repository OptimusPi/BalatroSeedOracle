# Submodule Init Check

Verify the Motely git submodule is properly initialized.

## Input

None required.

## Steps

1. **Check Submodule Status**
   ```bash
   git submodule status
   ```

   Expected output shows commit hash without `-` prefix:
   ```
   abc1234 external/Motely (v1.0.0)
   ```

   If you see `-` prefix, submodule is NOT initialized:
   ```
   -abc1234 external/Motely
   ```

2. **Fix Uninitialized Submodule**

   If submodule is missing or uninitialized:
   ```bash
   git submodule update --init --recursive
   ```

3. **Verify Fix**
   ```bash
   ls external/Motely/
   ```
   Should show Motely project files (not empty).

## Output

Confirmation that `external/Motely/` is populated and submodule is tracking correctly.

## Notes

- **Build failures**: Missing submodule is a common cause of build errors referencing Motely types.
- **Search exclusion**: The `external/` directory is intentionally excluded from code search in `.cursorignore`. If you need to inspect Motely internals, open it as a separate workspace.
- **Updating Motely**: To pull latest submodule changes: `git submodule update --remote external/Motely`
- **Fresh clones**: Always run `git clone --recurse-submodules` or run the init command after cloning.
