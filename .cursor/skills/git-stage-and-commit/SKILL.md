---
name: git-stage-and-commit
description: Guides correct git staging and committing workflow with sequential operations and clean handling of "nothing to commit" scenarios.
---

# Git Stage and Commit Workflow

## When to Use

Use this skill when:

- User requests staging and committing changes
- You need to commit after completing a task
- Handling git operations where sequencing matters

## Workflow Steps

### 1. Gather Repository State (Parallel)

Run these commands in parallel to understand current state:

```bash
git status --short              # See staged/unstaged/untracked files
git diff --name-only            # See unstaged changes
git log -3 --oneline            # Match commit message style
```

### 2. Stage Files Intentionally

Only stage files relevant to the current change:

```bash
git add <specific-files>        # Prefer explicit file paths
# OR
git add -p                      # Interactive staging (not recommended for agent use)
```

**Do NOT blindly `git add .`** unless explicitly requested.

### 3. Commit Sequentially

**Critical**: Run git commit **sequentially**, not in parallel with status/diff.

```bash
git commit -m "$(cat <<'EOF'
Commit message here.

Optional body with more details.
EOF
)"
```

Then immediately verify:

```bash
git status --short              # Confirm clean state
```

### 4. Handle "Nothing to Commit"

If `git commit` reports "nothing to commit, working tree clean":

1. **Stop immediately** - do not loop or retry
2. Summarize concisely: "Working tree is clean; nothing to commit."
3. If user expected changes, explain possible reasons:
   - Changes were already committed
   - Files were unstaged between operations
   - Pre-commit hooks may have modified staging

## Anti-Patterns to Avoid

| Anti-Pattern | Correct Approach |
| --- | --- |
| Running `git commit` and `git status` in parallel | Run sequentially: commit → status |
| Multiple `git status` variants after clean signal | One `git status --short` is sufficient |
| Looping after "nothing to commit" | Stop and summarize |
| Claiming "committed" without verifying | Always run `git status` after commit |

## Example: Clean Commit Flow

```bash
# Step 1: Parallel state gathering
git status --short
git diff --name-only
git log -3 --oneline

# Step 2: Stage (based on what step 1 revealed)
git add src/foo.cs src/bar.cs

# Step 3: Commit THEN verify (sequential)
git commit -m "$(cat <<'EOF'
Fix null reference in FooService

Handle edge case where config is not initialized.
EOF
)"
git status --short
# Output: (empty = success)
```

## Notes

- The system prompt already has git commit guidance; this skill reinforces sequencing and "nothing to commit" handling discovered via audit.
- When in doubt, fewer git commands are better than more.
