# Changelog Enforcement Guide

This repository uses a multi-layered approach to ensure `CHANGELOG.md` stays up-to-date.

## How It Works

### 1. Agent Rule (`.cursor/rules/005-changelog-policy.mdc`)

**Purpose**: Guides AI agents to update the changelog proactively

**What it does**:

- Instructs agents when changelog updates are required
- Provides formatting guidelines and examples
- Ensures agents check changelog before completing tasks

**For agents**: Read this rule to understand when and how to update `CHANGELOG.md`

### 2. CI Workflow (`.github/workflows/changelog-check.yml`)

**Purpose**: Enforces changelog updates at PR time (hard gate)

**What it does**:

- Runs on every pull request to `main`, `develop`, or version branches
- Checks if user-facing files were modified (`src/`, `docs/`, `Presets/`, etc.)
- Fails the PR if `CHANGELOG.md` wasn't updated
- Posts a helpful comment explaining what's needed
- Allows bypass with `skip-changelog` label for non-user-facing changes

**Bypass scenarios**:

- Add `skip-changelog` label to the PR
- Or only modify files in skip paths (`.cursor/`, `.github/workflows/`, `.config/`, etc.)

### 3. Pre-commit Hook (`lefthook.yml`)

**Purpose**: Friendly reminder before committing (soft gate)

**What it does**:

- Warns if you're committing user-facing changes without staging `CHANGELOG.md`
- Does NOT block the commit (just a reminder)
- Runs automatically if you have `lefthook` installed

**Setup**:

```bash
brew install lefthook  # or your package manager
lefthook install
```

### 4. PR Template (`.github/pull_request_template.md`)

**Purpose**: Checklist reminder for contributors

**What it does**:

- Includes a checkbox: "I have updated CHANGELOG.md under ## [Unreleased]"
- Reminds contributors before they submit the PR

## Quick Reference

### When to Update Changelog

✅ **YES** - Update for:

- Bug fixes
- New features
- Behavior changes
- Performance improvements
- Breaking changes
- Filter/format changes
- User-facing documentation

❌ **NO** - Skip for:

- Refactoring (no behavior change)
- Code comments
- Formatting/whitespace
- `.cursor/` tooling
- CI/build config (unless affects contributors)
- Test-only changes

### How to Update

1. Open `CHANGELOG.md`
2. Find `## [Unreleased]` section
3. Add your entry under the appropriate category:
   - `### Added` - New features
   - `### Changed` - Modifications
   - `### Fixed` - Bug fixes
4. Use imperative mood: "Add X", "Fix Y", "Change Z"
5. Keep it concise (1-2 lines)

### Example Entry

```markdown
## [Unreleased]

### Added

- Keyboard shortcut (Ctrl+R) to restart search

### Fixed

- Browser version no longer crashes when exporting empty results
```

## Bypassing the Check

If your PR truly doesn't need a changelog entry:

1. **Option A**: Add the `skip-changelog` label to your PR
2. **Option B**: Only modify files in skip paths (CI config, Cursor rules, etc.)

The CI check will automatically pass in these cases.

## For Maintainers

### Creating a Release

When cutting a release:

1. Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`
2. Add a new empty `## [Unreleased]` section at the top:
   ```markdown
   ## [Unreleased]

   ### Added

   ### Changed

   ### Fixed
   ```
3. Commit and tag the release

### Managing the skip-changelog Label

Create the label in GitHub if it doesn't exist:

```bash
gh label create skip-changelog --description "Skip changelog requirement for this PR" --color "d4c5f9"
```

Or via GitHub UI: Settings → Labels → New label

## Troubleshooting

### CI check fails even though I updated CHANGELOG.md

**Cause**: The check verifies that the `## [Unreleased]` section has new content

**Fix**: Ensure you added entries under a category heading (`### Added`, `### Changed`, etc.)

### Pre-commit hook doesn't run

**Cause**: `lefthook` not installed or hooks not initialized

**Fix**:

```bash
brew install lefthook
lefthook install
```

### I need to bypass for a legitimate reason

**Fix**: Add the `skip-changelog` label to your PR and explain why in the PR description

## Questions?

See `.cursor/rules/005-changelog-policy.mdc` for detailed guidelines.
