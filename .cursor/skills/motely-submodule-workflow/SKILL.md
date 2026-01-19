---
name: motely-submodule-workflow
description: Manages the Motely git submodule correctly. Use when builds fail due to missing submodule, updating Motely integration, or troubleshooting submodule issues.
---

# Motely Submodule Workflow

## Overview

The Motely search engine is included as a git submodule in `external/Motely/`.

## Initial Setup

After cloning the repository:

```bash
git submodule update --init --recursive
```

Or clone with submodules:

```bash
git clone --recurse-submodules https://github.com/OptimusPi/BalatroSeedOracle.git
```

## Common Operations

### Update Submodule to Tracked Commit

```bash
# Safe: Update to the commit tracked by parent repo
git submodule update --init --recursive
```

### ⚠️ DANGEROUS: Update to Latest Remote

```bash
# DANGEROUS - Changes the tracked commit pointer!
# Only use with explicit user approval
git submodule update --init --recursive --remote
```

**Why `--remote` is dangerous:**
- Changes the submodule to the latest commit on its remote branch
- This changes the commit pointer tracked by the parent repo
- May introduce breaking API changes unexpectedly
- Requires explicit user approval before using

### After Pulling Changes

```bash
git pull --recurse-submodules
# Or separately:
git pull
git submodule update --init --recursive
```

### Check Submodule Status

```bash
git submodule status
```

Output example:

```
 abc1234 external/Motely (v1.0.0)  # Clean
+def5678 external/Motely (v1.0.0)  # Local changes (+ prefix)
-ghi9012 external/Motely           # Not initialized (- prefix)
```

## Making Changes to Motely

1. **Navigate to submodule**:
   ```bash
   cd external/Motely
   ```

2. **Create branch** (avoid detached HEAD):
   ```bash
   git checkout -b my-feature
   ```

3. **Make changes and commit**:
   ```bash
   git add .
   git commit -m "feat: add feature"
   ```

4. **Push submodule changes**:
   ```bash
   git push origin my-feature
   ```

5. **Update parent repo**:
   ```bash
   cd ../..  # Back to root
   git add external/Motely
   git commit -m "chore: update Motely submodule"
   git push
   ```

## Troubleshooting

### Build Fails: Missing Motely

**Symptom**: Build errors referencing `Motely` namespace

**Fix**:

```bash
git submodule update --init --recursive
dotnet restore
```

### Submodule Shows as Modified

**Symptom**: `git status` shows `external/Motely` modified but no changes intended

**Check**:

```bash
cd external/Motely
git status
git diff
```

**Fix** (discard local changes):

```bash
git submodule update --init --recursive --force
```

### Detached HEAD in Submodule

**Symptom**: Working in submodule shows "HEAD detached"

**Fix**: Create a branch before making changes:

```bash
cd external/Motely
git checkout -b my-feature
# or checkout existing branch
git checkout main
```

### Submodule URL Changed

```bash
git submodule sync
git submodule update --init --recursive
```

## CI/CD Integration

Ensure CI includes submodule initialization:

```yaml
- name: Checkout Repository
  uses: actions/checkout@v4
  with:
    submodules: recursive
    # or
    fetch-depth: 0

- name: Initialize Submodules
  run: git submodule update --init --recursive
```

## Security Note

Always verify submodule URLs use secure protocols (HTTPS or SSH):

```bash
# Check configured URLs
git config --get-regexp submodule\\..*\\.url

# View .gitmodules
cat .gitmodules
```

## Submodule Configuration

`.gitmodules` content:

```ini
[submodule "external/Motely"]
    path = external/Motely
    url = https://github.com/OptimusPi/Motely.git
```

## Checklist

- [ ] Submodule initialized (`git submodule update --init --recursive`)
- [ ] Build succeeds after submodule update
- [ ] Working on branch (not detached HEAD) when making changes
- [ ] Submodule changes pushed before parent commit
- [ ] CI configured to checkout submodules
