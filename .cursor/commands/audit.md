# Audit Cursor Configuration

Analyze workspace cursor rules, commands, and skills to identify improvements.

## Steps

1. **Load Latest Research**
   - Check `.local/research/cursor/` for research summaries
   - Read the most recent file (by date prefix in filename)
   - Extract best practices to use as audit criteria
   - If no research exists, run `/cursor-research` first or use built-in checklist

2. **Inventory Current Configuration**
   - List all files in `.cursor/rules/`
   - List all files in `.cursor/commands/`
   - List all files in `.cursor/skills/`
   - Check for `AGENTS.md` in project root and subdirectories

3. **Analyze Rules**
   For each rule file:
   - Check frontmatter completeness (description, alwaysApply, category, priority, lastReviewed)
   - Measure line count (flag if >500 lines)
   - Check for file references vs inline content
   - Identify overlapping or redundant rules
   - Check if using legacy `.mdc` format vs new folder-based `RULE.md` format
   - Verify alwaysApply rules are truly necessary for every session

4. **Analyze Commands**
   For each command file:
   - Check if command has clear step-by-step instructions
   - Verify command is actionable and focused
   - Identify missing common workflows (pr, review, fix-issue, etc.)

5. **Analyze Skills**
   - Check if any skills exist
   - Verify SKILL.md has required frontmatter (name, description)
   - Check for bundled scripts that could be skills

6. **Check Best Practices**

   Compare current configuration against research findings and templates:
   - Templates: `.local/templates/cursor-*.md`
   - Standards: `.cursor/rules/tool-cursor-config.mdc`

   Default checklist if no research:
   - [ ] Rules under 500 lines each
   - [ ] Rules reference files instead of copying content
   - [ ] No vague guidance in rules (specific, actionable instructions)
   - [ ] Commands exist for repeated workflows
   - [ ] Skills created for domain-specific knowledge
   - [ ] No duplicate/overlapping rules
   - [ ] Metadata complete on all rules
   - [ ] Legacy formats flagged for migration
   - [ ] Model selection guidance present (if applicable)
   - [ ] Plan Mode usage documented for complex tasks
   - [ ] Security considerations addressed (credentials, approvals)
   - [ ] New rules/commands/skills follow templates

7. **Generate Improvement Plan**

   Output a structured plan with:

   ### High Priority
   - Rules exceeding 500 lines that need splitting
   - Missing critical metadata
   - Overlapping/conflicting rules
   - Security/approval gaps

   ### Medium Priority
   - Legacy format migration (.mdc -> RULE.md folders)
   - Missing commands for common workflows
   - Rules that could be agent-decided instead of always-apply
   - Incomplete frontmatter

   ### Low Priority
   - Documentation improvements
   - Organization/naming consistency
   - Skills creation opportunities
   - lastReviewed dates needing update

   ### Metrics Summary
   - Total rules: X
   - Always-apply rules: X
   - Agent-decided rules: X
   - File-scoped rules: X
   - Total commands: X
   - Total skills: X
   - Rules needing attention: X

## Output Format

Create improvement plan as `.local/plans/cursor-config-audit-YYYY-MM-DD.md` with:

- Executive summary (3-5 sentences)
- Research reference (file used, if any)
- Prioritized action items
- Specific file changes needed
- Estimated effort per item

## Notes

- Run `/cursor-research` periodically to keep best practices current
- Research older than 3 months should be refreshed before major audits
- Latest research file: `.local/research/cursor/` (most recent by date prefix)
- For creating new configuration, use the `cursor-authoring` skill
