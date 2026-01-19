# Remediate Audit Outputs

Analyze and address issues discovered in audit outputs produced by `cursor-audit-chat`, then archive the reviewed audit files.

## Input

One of:

- No input (default): process all audit files in `.local/audits/`
- A single audit file path (e.g., `.local/audits/2026-01-15-foo-audit.md`)
- A date filter (e.g., `2026-01-15`) to process matching files

## Prerequisites

- Audit files exist in `.local/audits/` (typically created by `cursor-audit-chat`)
- If remediation will touch tracked files: work on a feature branch
- If remediation is strictly local-only (e.g., `.cursor/`, `.local/`), do not create a branch or commit

## Steps

### 1. Load the Audit Rubric

- Read `.cursor/commands/audit-chat.md`
- Use its categories as the schema for interpreting findings:
  - Tool Usage, Response Quality, Task Execution, Prompting Effectiveness
  - Agent Issues, Rule Opportunities, Skill Candidates, Prompting Tips, Quick Wins

### 2. Inventory Audit Outputs

- List files in `.local/audits/`
- If an input file or date filter is provided, narrow the list to the matching file(s)
- For each selected audit file:
  - Read it fully
  - Extract actionable findings into a structured set of items:
    - **Issue** (what is wrong)
    - **Evidence** (quote/section from the audit)
    - **Impact** (why it matters)
    - **Fix** (specific change to make)
    - **Where** (exact file(s) / rule(s) / command(s) / skill(s) to edit or create)

### 3. Consolidate and Prioritize

- De-duplicate repeated findings across audits
- Prioritize with three buckets:
  - **High**: security/approval gaps, recurring agent failures, workflow blockers
  - **Medium**: efficiency improvements, missing common workflows, unclear prompts
  - **Low**: polish, naming consistency, minor organization

### 4. Create an Executable Remediation Plan

Produce a plan that is directly executable (no vague tasks). For each item include:

- The exact target file(s) to edit/create
- The expected outcome / acceptance criteria
- The verification step (lint/test/manual check), if applicable

If the plan includes creating a new Cursor rule or skill:

- Use the `create-rule` or `create-skill` skills (global) for detailed instructions
- Use templates from `.local/templates/cursor-*.md`
- Follow standards in `.cursor/rules/tool-cursor-config.mdc`
- Prefer doing it only when the audit evidence shows a repeated, durable pattern

### 5. Execute the Plan

If the plan modifies tracked repo files, create a branch first (with explicit user approval):

```bash
# Example (adjust name as needed)
git branch --show-current
git checkout -b chore/audit-remediation-$(date +%Y-%m-%d)
```

Then execute the remediation items:

- Prefer editing existing rules/commands/skills over creating new ones
- Keep changes tightly scoped to what the audits justify
- After substantive edits, run targeted verification (build/lint/tests) for the files you changed

### 6. Archive Reviewed Audit Files

After the remediation plan is executed and verified:

1. Ensure archive folder exists:

```bash
mkdir -p .local/archive/audits
```

1. Move the reviewed audit files:

```bash
mv .local/audits/{selected-files...} .local/archive/audits/
```

## Output

- A short remediation summary (what changed and where)
- A list of archived audit files now in `.local/archive/audits/`

## Notes

- Treat audit findings as inputs; verify against the current repo state before making changes.
- Don't archive audit files until fixes are completed and verified.
