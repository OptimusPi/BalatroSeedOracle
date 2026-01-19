# Audit Single Cursor Rule

Analyze a specific rule file for relevance, quality, and alignment with best practices.

## Input

- Rule file path (e.g., `.cursor/rules/tool-dotnet.mdc`)

## Steps

1. **Read Rule File**
   - Load the specified rule file
   - Parse frontmatter metadata (category, priority, lastReviewed, globs)
   - Extract rule content and structure

2. **Assess Codebase Relevance**
   - Check if file patterns in globs match files in codebase
   - Verify referenced tools/technologies are used in project
   - Identify any obsolete or unused guidance
   - Flag rules for technologies not present in workspace

3. **Check Skill Overlap**
   - List skills in `.cursor/skills/` directories
   - Compare rule topic/technology against skill names and descriptions
   - Read potentially overlapping skills to identify:
     - Duplicate guidance (same instructions in both)
     - Conflicting guidance (contradictory instructions)
     - Misplaced content (belongs in skill vs rule or vice versa)
   - Note: Rules = automatic context injection; Skills = on-demand workflows

4. **Load Best Practices**
   - Check `.local/research/cursor/` for research files
   - Read most recent file (by date prefix): `2026-01-15-agent-configuration-best-practices.md` or newer
   - Extract applicable criteria for rule evaluation
   - If no research exists, use default checklist below

5. **Analyze Rule Quality**

   Evaluate against criteria:
   - [ ] Line count under 500 (check with `wc -l`)
   - [ ] Clear, actionable instructions (not vague guidance)
   - [ ] Frontmatter complete (category, priority, lastReviewed)
   - [ ] Glob patterns accurate for intended files
   - [ ] No duplicate content from other rules or skills
   - [ ] References files instead of copying large content blocks
   - [ ] Examples provided where helpful
   - [ ] Format: legacy `.mdc` vs folder-based `RULE.md`

6. **Generate Recommendations**

   Categorize findings:

   ### Must Fix
   - Critical issues affecting rule function
   - Security or approval gaps
   - Incorrect glob patterns
   - Conflicting guidance with skills

   ### Should Improve
   - Missing metadata fields
   - Overly long content needing split
   - Vague instructions needing specificity
   - Legacy format migration
   - Duplicate content with skills (consolidate)

   ### Consider
   - Documentation enhancements
   - Example additions
   - Reorganization opportunities
   - Move workflow content to skill (if procedural/multi-step)
   - Move constraint content from skill to rule (if always-apply)

7. **Prompt for Confirmation**
   - Present recommendations summary to user
   - List specific changes to be made
   - Wait for explicit "yes" approval before proceeding

8. **Apply Changes (if confirmed)**
   - Update frontmatter metadata
   - Refactor content as recommended
   - Update lastReviewed date to today (YYYY-MM-DD format)
   - Split into multiple rules if needed
   - Migrate format if applicable

## Default Quality Checklist

Use when no research available:

- Under 500 lines
- Has category, priority, lastReviewed in frontmatter
- Glob patterns match actual project files
- Instructions are specific and actionable
- No overlap with always-apply rules
- No duplicate/conflicting content with `.cursor/skills/`
- Uses file references for large code examples
- Follows template structure from `.local/templates/cursor-rule.md`

## Template Reference

When creating new rules or restructuring existing ones:

- Template: `.local/templates/cursor-rule.md`
- Standards: `.cursor/rules/tool-cursor-config.mdc`

## Output

Display inline:

- Current state summary (lines, metadata, glob coverage)
- Relevance score (High/Medium/Low/None)
- Numbered list of recommended changes
- Prompt: "Apply these changes? (y/n)"

## Notes

- Run `/cursor-research` first if research is older than 3 months
- Check for conflicts with other rules before applying changes
- Backup original if making significant structural changes
- For creating new rules, use the `create-rule` skill (global) or template at `.local/templates/cursor-rule.md`
- Rule vs Skill distinction:
  - Rules: constraints, standards, auto-injected context (passive)
  - Skills: workflows, procedures, on-demand guidance (active)
