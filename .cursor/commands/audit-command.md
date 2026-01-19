# Audit Single Cursor Command

Analyze a specific command file for relevance, quality, and alignment with best practices.

## Input

- Command file path (e.g., `.cursor/commands/pr.md`)

## Steps

1. **Read Command File**
   - Load the specified command file
   - Extract title, description, input requirements
   - Parse steps and structure
   - Identify referenced files (templates, rules, skills, scripts)

2. **Assess Codebase Relevance**
   - Verify referenced files exist (templates, scripts, rules)
   - Check if command tools/technologies are available
   - Identify any obsolete or unused steps
   - Flag commands for workflows not applicable to project

3. **Check Related Artifacts**
   - Verify templates in `.local/templates/` exist if referenced
   - Check rules in `.cursor/rules/` referenced by command
   - Check skills in `.cursor/skills/` referenced by command
   - Verify scripts/tools referenced are available

4. **Load Best Practices**
   - Check `.local/research/cursor/` for research files
   - Read most recent file (by date prefix): `2026-01-15-agent-configuration-best-practices.md` or newer
   - Extract applicable criteria for command evaluation
   - If no research exists, use default checklist below

5. **Analyze Command Quality**

   Evaluate against criteria:
   - [ ] Clear title describing purpose
   - [ ] Input section defines all expected inputs
   - [ ] Steps are numbered and sequential
   - [ ] Steps are actionable (not vague)
   - [ ] Approval gates present for external operations
   - [ ] Error handling described for failure cases
   - [ ] Referenced files/templates exist
   - [ ] Shell commands are correct and tested
   - [ ] No duplicate steps from other commands
   - [ ] Line count reasonable (under 300 lines preferred)

6. **Generate Recommendations**

   Categorize findings:

   ### Must Fix
   - Broken file/template references
   - Missing approval gates for external operations
   - Incorrect shell commands or paths
   - Security gaps (missing auth, exposed secrets)

   ### Should Improve
   - Missing input documentation
   - Vague or unclear steps
   - Missing error handling
   - Overly long command needing split
   - Outdated references

   ### Consider
   - Documentation enhancements
   - Example additions
   - Step consolidation opportunities
   - Extract reusable steps to skill

7. **Prompt for Confirmation**
   - Present recommendations summary to user
   - List specific changes to be made
   - Wait for explicit approval before proceeding

8. **Apply Changes (if confirmed)**
   - Update command content
   - Fix broken references
   - Add missing sections
   - Update shell commands if needed

## Default Quality Checklist

Use when no research available:

- Clear title and description
- Input section present with all parameters
- Steps numbered and actionable
- Approval gates for git push, PR creation, external API calls
- Error handling for common failures
- Referenced templates/files exist
- Shell commands tested and working
- No overlap with other commands
- Follows template structure from `.local/templates/cursor-command.md`

## Template Reference

When creating new commands or restructuring existing ones:

- Template: `.local/templates/cursor-command.md`
- Standards: `.cursor/rules/tool-cursor-config.mdc`

## Output

Display inline:

- Current state summary (lines, sections, file references)
- Relevance score (High/Medium/Low/None)
- Numbered list of recommended changes
- Prompt: "Apply these changes? (y/n)"

## Notes

- Run `/cursor-research` first if research is older than 3 months
- Check for conflicts with other commands before applying changes
- Verify all shell commands work before finalizing
- Use the `cursor-authoring` skill for creating new commands
- Command vs Skill distinction:
  - Commands: user-invoked workflows via `/command-name` (explicit trigger)
  - Skills: agent-loaded context for task types (implicit loading)
