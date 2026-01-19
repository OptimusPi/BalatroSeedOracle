# Audit Rules

Audit the `.cursor/rules/` directory for quality, consistency, and coverage.

## Input

None required (audits all rules in `.cursor/rules/`).

## Steps

1. **Inventory Current Rules**

   List all rule files:
   - `.cursor/rules/*.mdc`
   - `.cursor/rules/*.md`

2. **Evaluate Each Rule**

   For each rule, check:

   ### Structure
   - Has clear title and description
   - Specifies applicable file glob patterns
   - Contains actionable guidance (not just theory)
   - Includes examples where helpful

   ### Content Quality
   - Accurate to current codebase
   - No outdated paths, APIs, or patterns
   - No contradictions with other rules
   - Appropriate scope (not too broad or narrow)

   ### Coverage Gaps
   - Are there common patterns lacking rules?
   - Are there error-prone areas without guidance?
   - Do skills reference rules that don't exist?

3. **Check Rule References**

   Verify rules referenced by:
   - `001-core-project-context.mdc` (main rule)
   - Skills in `.cursor/skills/`
   - Commands in `.cursor/commands/`

4. **Identify Issues**

   Categorize findings:
   - **Missing rules**: Patterns that need codification
   - **Stale rules**: Content that needs updating
   - **Redundant rules**: Overlap that should be consolidated
   - **Broken references**: Rules referenced but don't exist

5. **Generate Recommendations**

   Prioritize:
   - High-impact fixes (frequently triggered rules)
   - Quick wins (simple updates)
   - New rules (based on common agent errors)

6. **Create Output File**
   - Filename format: `{YYYY-MM-DD}-rules-audit.md`
   - Path: `.local/audits/{filename}`
   - Create `.local/audits/` directory if it doesn't exist

## Output

File created at `.local/audits/{date}-rules-audit.md`

## Notes

- Run periodically (monthly) or after major refactors.
- Focus on actionable improvements, not perfection.
- Consider agent behavior patterns when identifying gaps.
- Link findings to specific rule files with `@` paths.
