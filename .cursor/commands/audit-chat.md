# Audit Chat

Analyze a chat session to discover agent problems, optimizations, and improvement opportunities.

## Input

One of:

- Current chat session (default)
- Path to existing chat summary file in `.local/chats/`

## Steps

1. **Gather Chat Data**
   - If current chat: analyze full conversation
   - If file provided: read and parse the summary file

2. **Analyze Agent Behavior**

   Evaluate the following dimensions:

   ### Tool Usage
   - Unnecessary tool calls (redundant reads, searches)
   - Missing parallel tool calls that could have been batched
   - Inefficient search patterns (broad when narrow would work)
   - Wrong tool selection (e.g., grep vs semantic search)

   ### Response Quality
   - Over-explanation or verbosity
   - Under-explanation (missing context)
   - Incorrect assumptions made
   - Hallucinated paths, APIs, or features
   - Unnecessary code generation

   ### Task Execution
   - Deviation from user intent
   - Missed requirements
   - Incomplete implementations
   - Over-engineering beyond request
   - Proper error handling and recovery

   ### Prompting Effectiveness
   - User prompts that caused confusion
   - Prompts that could be clearer
   - Missing context that would have helped
   - Effective patterns worth reusing

3. **Identify Patterns**
   - Recurring issues across the session
   - Successful strategies to replicate
   - Context gaps that rules/skills could fill

4. **Generate Recommendations**

   Categorize findings:
   - **Agent Issues**: Problems with agent behavior to address
   - **Rule Opportunities**: Patterns that should become rules
   - **Skill Candidates**: Complex workflows that warrant skills
   - **Prompting Tips**: User-side improvements
   - **Quick Wins**: Easy optimizations

5. **Load Template**
   - Read template from `.local/templates/chat-audit.md`
   - Fill all sections with findings

6. **Create Output File**
   - Filename format: `{YYYY-MM-DD}-{brief-topic}-audit.md`
   - Path: `.local/audits/{filename}`
   - Create `.local/audits/` directory if it doesn't exist

7. **Report Findings**
   - Show created file path
   - Highlight top 3 actionable improvements

## Output

File created at `.local/audits/{date}-{topic}-audit.md`

## Notes

- Be specific with examples from the chat
- Prioritize actionable recommendations
- Link findings to specific tool calls or messages when possible
- Consider both user and agent improvements
- Focus on patterns, not one-off issues
