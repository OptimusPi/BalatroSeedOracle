# Research Topic

Conduct web research on a topic and save a timestamped summary.

## Input

- Topic to research (optional - if omitted, research all default topics)
- Time window (optional, defaults to "last 3 months")

## Default Topics (when no input provided)

Research all of these and combine into a single comprehensive summary:

1. **Rules** - configuration, formats, best practices
2. **Commands** - custom slash commands, workflows
3. **Skills** - SKILL.md structure, dynamic capabilities
4. **Prompting** - effective prompts, context management
5. **Model Selection** - when to use which model, Auto-Select
6. **Plan Mode** - planning workflows, saving plans
7. **Hooks** - hooks.json, automation patterns
8. **MCP** - Model Context Protocol integration
9. **Security** - credential handling, approval patterns
10. **Agent Workflows** - TDD, parallel agents, cloud agents

Output: `.local/research/cursor/{YYYY-MM-DD}-agent-configuration-best-practices.md`

## Steps

### 1. Determine Scope and Date Range

- If topic provided: research that single topic
- If no topic provided: research all default topics (comprehensive Cursor best practices)
- Get today's date
- Calculate start date based on time window (default: 3 months prior)
- Include date range in search queries for recency

### 2. Conduct Web Research

**Source Priority Tiers:**

Tier 1 - Official Cursor (always check first):

- `cursor.com/blog` - announcements, best practices
- `docs.cursor.com` - official documentation
- `cursor.com/changelog` - release notes, new features
- `forum.cursor.com` - community discussions, workarounds

Tier 2 - Related Official (check when topic-relevant):

- `modelcontextprotocol.io` - MCP specification and docs
- `github.com/modelcontextprotocol` - MCP spec repo
- `docs.anthropic.com` - Claude model behavior/capabilities
- `platform.openai.com/docs` - GPT model docs
- `ai.google.dev` - Gemini model docs

Tier 3 - Community/Third-party:

- GitHub repos with examples
- Medium/blog posts (cross-reference with official)

**Search Strategy:**

1. For Cursor topics, search `site:cursor.com {topic}` first
2. Check changelog for recent feature updates
3. Search forum for real-world usage patterns
4. Then run broader searches:
   - Search for topic + best practices + date range
   - Search for topic + configuration guide + date range
   - Search for topic + recent updates + date range

For each promising result:

- Fetch full page content when search snippets are insufficient
- Note source URL and publication date
- Extract key findings
- Weight Tier 1 > Tier 2 > Tier 3 in synthesis

### 3. Synthesize Findings

Organize research into categories:

- Core concepts/definitions
- Best practices
- Configuration patterns
- Common pitfalls
- Recent changes/updates
- Security considerations (if applicable)

### 4. Create Summary Document

**Single topic:**

- Filename: `{YYYY-MM-DD}-{topic-slug}.md`
- Path: `.local/research/{category}/`

**Comprehensive (no topic):**

- Filename: `{YYYY-MM-DD}-agent-configuration-best-practices.md`
- Path: `.local/research/cursor/`

Structure:

```markdown
# {Topic} Research Summary

Research Date: {YYYY-MM-DD}
Coverage Period: {start_date} - {end_date}

## Sources

- {source_url} ({date if available})
- ...

---

## 1. {Category 1}

{Key findings}

## 2. {Category 2}

{Key findings}

...

## Key Takeaways

1. {Most important finding}
2. {Second most important}
   ...
```

### 5. Ensure Directory Exists

```bash
mkdir -p .local/research/{category}
```

### 6. Write Summary

Save the compiled research to the timestamped file.

### 7. Report Completion

```
Research Complete

Topic: {topic} (or "Comprehensive Cursor Best Practices" if no input)
Coverage: {start_date} to {end_date}
Sources: {count} sources analyzed
Saved: {output_path}

Top 3 Findings:
1. {finding}
2. {finding}
3. {finding}
```

## Notes

- Follow source priority tiers: Tier 1 (official) > Tier 2 (related official) > Tier 3 (community)
- Check cursor.com/changelog for recent feature changes
- Cross-reference third-party claims with official sources
- Include publication dates when available
- Focus on actionable information over theory
- Keep summaries under 500 lines unless topic requires more depth
