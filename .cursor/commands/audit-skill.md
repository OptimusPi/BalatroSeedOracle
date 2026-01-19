# Audit Single Cursor Skill

Analyze a specific skill for relevance, quality, and alignment with best practices.

## Input

- Skill folder path (e.g., `.cursor/skills/react-island/`)

## Steps

1. **Read Skill File**
   - Load `SKILL.md` from specified folder
   - Parse frontmatter metadata (name, description)
   - Extract skill content and structure
   - List any supporting files in skill folder

2. **Assess Codebase Relevance**
   - Check if skill technologies are used in project
   - Verify referenced paths and files exist
   - Identify any obsolete or unused guidance
   - Flag skills for technologies not present in workspace

3. **Load Best Practices**
   - Check `.local/research/cursor/` for research files
   - Read most recent file (by date prefix): `2026-01-15-agent-configuration-best-practices.md` or newer
   - Extract applicable criteria for skill evaluation
   - If no research exists, use default checklist below

4. **Analyze Skill Quality**

   Evaluate against criteria:
   - [ ] Has frontmatter with name and description
   - [ ] Description matches available_skills entry
   - [ ] Clear "When to Use" section
   - [ ] Step-by-step instructions provided
   - [ ] Code examples are current and tested
   - [ ] File paths match actual project structure
   - [ ] Commands work in project context
   - [ ] No duplicate content from rules
   - [ ] Referenced files exist in codebase
   - [ ] Line count reasonable (not excessive)

5. **Generate Recommendations**

   Categorize findings:

   ### Must Fix
   - Broken file references
   - Incorrect paths or commands
   - Missing required sections
   - Security or approval gaps

   ### Should Improve
   - Missing or incomplete frontmatter
   - Outdated code examples
   - Vague instructions needing specificity
   - Missing supporting files

   ### Consider
   - Documentation enhancements
   - Example additions
   - Reorganization opportunities
   - Splitting large skills

6. **Prompt for Confirmation**
   - Present recommendations summary to user
   - List specific changes to be made
   - Wait for explicit approval before proceeding

7. **Apply Changes (if confirmed)**
   - Update frontmatter metadata
   - Fix broken references
   - Update code examples
   - Add missing sections
   - Update supporting files if needed

## Default Quality Checklist

Use when no research available:

- Has name and description in frontmatter
- "When to Use" section present
- Instructions are step-by-step and actionable
- Code examples match project conventions
- File paths reference actual project structure
- Commands tested and working
- No overlap with always-apply rules
- Follows template structure from `.local/templates/cursor-skill.md`

## Template Reference

When creating new skills or restructuring existing ones:

- Template: `.local/templates/cursor-skill.md`
- Standards: `.cursor/rules/tool-cursor-config.mdc`

## Output

Display inline:

- Current state summary (sections, file count, path references)
- Relevance score (High/Medium/Low/None)
- Numbered list of recommended changes
- Prompt: "Apply these changes? (y/n)"

## Notes

- Run `/cursor-research` first if research is older than 3 months
- Check for conflicts with other skills before applying changes
- Verify all code examples compile/run before finalizing
- Use the `cursor-authoring` skill for creating new skills
