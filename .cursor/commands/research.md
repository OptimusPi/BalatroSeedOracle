# Research Best Practices

Research current best practices and patterns for a given technology, tool, language, or framework.

## Purpose

Produce actionable, agent-consumable research summaries by searching official documentation and trusted sources.

## Inputs required

- Topic: technology | tool | language | framework (e.g., "Umbraco 15", "Entity Framework Core", "React Server Components")

## Context to add before running

- None required

## Constraints / guardrails

- Do not cite paywalled or login-gated sources without noting access limitations
- Do not fabricate sources; only include URLs actually retrieved
- Search sources in priority order (official docs first, then community sources)
- Limit scope to best practices and patterns; skip beginner tutorials and getting-started content (advanced guides and official best-practice docs are acceptable)
- Do not include deprecated or EOL version guidance unless explicitly requested

## Stop conditions

- If the topic is ambiguous (e.g., "React" could mean React Native, React DOM, React Server Components), ask one clarifying question and wait.
- If no relevant sources are found after initial search, report findings and ask whether to broaden scope.

## Steps

1. Preflight check
   - Ensure `.local/research/` directory exists; create if missing
   - Verify write permissions

2. Clarify topic scope
   - Confirm technology name and version (if applicable)
   - Identify specific areas of interest (architecture, security, performance, testing, etc.)

3. Search for authoritative sources (in priority order)
   - Official documentation site (primary source)
   - Official release notes and changelogs (last 6 months)
   - Official blog posts and announcements
   - GitHub repository docs/wiki (if official)
   - Standards/specifications (e.g., W3C, ECMA, IETF)
   - Well-regarded community sources (e.g., Martin Fowler, Microsoft DevBlogs, Thoughtworks Radar)
   - Conference talks from official events

4. Extract best practices
   - Current recommended patterns
   - Anti-patterns to avoid
   - Breaking changes or migrations from recent versions
   - Security considerations
   - Performance guidelines
   - Testing approaches

5. Synthesize findings
   - Consolidate overlapping guidance
   - Note conflicting recommendations with source attribution
   - Prioritize official sources over community opinions

6. Write research summary
   - Use the Output format below
   - Save to `.local/research/{kebab-topic}-{YYYY-MM-DD}.md`
   - Use kebab-case for topic in filename (e.g., `entity-framework-core-2026-01-18.md`)

## Output format

```markdown
# {Topic} Best Practices Research Summary

Research Date: {YYYY-MM-DD}
Coverage Period: {YYYY-MM-DD of 6 months prior} - {YYYY-MM-DD today}

## Sources

- {url_1} ({year})
- {url_2} ({year})
  ...

---

## 1. {Category 1}

### {Subcategory}

- Bullet point guidance
- Specific recommendations with rationale

### {Subcategory}

- Additional guidance

## 2. {Category 2}

...

## Key Takeaways

1. **{Takeaway 1}**: One-sentence summary
2. **{Takeaway 2}**: One-sentence summary
   ...
```

## Test plan

- [ ] Topic clarification question asked when input is ambiguous
- [ ] Output file created at correct path with correct naming
- [ ] All listed sources are real and accessible
- [ ] Content is actionable (specific recommendations, not vague guidance)
- [ ] Key takeaways summarize the most important points

## Notes

- Target audience is Cursor agents; write for machine consumption (clear structure, explicit recommendations)
- Research older than 3 months should be refreshed before use in audits
- Related commands: `/audit` uses research output as audit criteria
