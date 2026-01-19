# Summarize Chat

Summarize the current chat session and save to `.local/chats/`.

## Steps

1. **Analyze Chat Content**
   - Identify the primary goal/task discussed
   - Extract key decisions made
   - Note important code changes or files affected
   - Capture any blockers or unresolved issues
   - List tools/commands used

2. **Generate Brief Title**
   - Create 2-4 word slug from main topic (lowercase, hyphenated)
   - Examples: `vite-config-fix`, `auth-setup`, `react-island-impl`

3. **Load Template**
   - Read template from `.cursor/templates/chat-summary.md`
   - Fill all sections based on chat analysis

4. **Create Output File**
   - Filename format: `{YYYY-MM-DD}-{brief-title}.md`
   - Path: `.local/chats/{filename}`
   - Create `.local/chats/` directory if it doesn't exist

5. **Write Summary**
   - Populate template with:
     - Date and duration estimate
     - Primary objective
     - Key outcomes and decisions
     - Files modified (with brief descriptions)
     - Commands/tools used
     - Unresolved items or follow-ups
     - Lessons learned (if any)

6. **Report Completion**
   - Show the created file path
   - Provide 1-sentence summary of what was captured

## Output

File created at `.local/chats/{date}-{brief-title}.md`

## Notes

- Keep summaries concise (aim for <200 lines)
- Focus on actionable information, not conversation transcript
- Include links to relevant docs/issues if discussed
- Tag with relevant areas: `[Auth]`, `[CMS]`, `[Frontend]`, etc.
