# Code Review

Perform a thorough technical code review of provided files, directories, or code.

## Purpose

Analyze code for architecture, design patterns, naming, code smells, security issues, and developer usability. Output prioritized suggestions with reasoning.

## Inputs required

- Files, directories, or code snippets added to context

## Context to add before running

- Files to review (use @file or @folder)
- Relevant configuration files if reviewing infrastructure code
- Test files if reviewing code with associated tests

## Constraints / guardrails

- Do not make changes to files; this is a review-only command
- Do not assume missing context; ask if unclear
- Prioritize critical issues over style nits
- Reference specific line numbers when possible
- Keep reasoning concise but actionable

## Stop conditions

- If no files or code are provided in context, ask: "Please add the files or directories you want reviewed using @file or @folder."
- If the scope is too large (>20 files), ask: "This is a large scope. Would you like me to review a subset first, or proceed with all files?"

## Steps

1. Identify inputs
   - List all files/directories/code provided in context
   - Categorize by type (backend, frontend, infra, tests, config)

2. Determine applicable standards
   - Check file extensions and content to identify language/framework
   - Load relevant workspace rules and skills if applicable:
     - `.NET/C#`: Use `dotnet-code-review` skill criteria
     - `TypeScript/React`: Check for React island patterns, component structure
     - `Terraform`: Check for module patterns, security defaults
     - `PowerShell`: Check for cross-platform patterns
   - Apply general best practices for the identified stack

3. Perform review
   - Architecture: Module boundaries, separation of concerns, coupling
   - Naming: Clarity, consistency, domain alignment
   - Design patterns: Appropriate use, anti-patterns
   - Code smells: Duplication, complexity, dead code
   - Security: Input validation, secrets, injection risks, auth/authz
   - Performance: N+1 queries, memory leaks, expensive operations
   - Error handling: Exception management, logging, user feedback
   - Testability: DI usage, mockability, test coverage
   - Developer usability: Readability, documentation, onboarding friction

4. Categorize findings by priority
   - Critical: Security vulnerabilities, data integrity risks, breaking changes
   - High: Performance issues, memory leaks, incorrect async patterns
   - Medium: Code quality, maintainability, missing tests
   - Low: Style, naming preferences (if not automated)

5. Generate output
   - Use the output format below

## Output format

### Files Reviewed

- `path/to/file.ext` (lines X-Y if partial)

### Summary

One paragraph overview of code quality and main concerns.

### Suggested Changes

#### Critical

1. **[File:Line]** Short description
   - Reasoning: Why this matters
   - Suggestion: How to fix

#### High

1. **[File:Line]** Short description
   - Reasoning: Why this matters
   - Suggestion: How to fix

#### Medium

1. **[File:Line]** Short description
   - Reasoning: Why this matters
   - Suggestion: How to fix

#### Low

1. **[File:Line]** Short description
   - Reasoning: Why this matters
   - Suggestion: How to fix

### Strengths

- What's done well (list 2-3 positives if applicable)

### Questions

- Any clarifying questions about intent or requirements

## Test plan

- [ ] Correctly identifies language/framework from file extensions
- [ ] Produces at least one finding per reviewed file (or explicitly notes "no issues found")
- [ ] Findings include file path and line number where applicable
- [ ] Each finding has reasoning and suggestion
- [ ] Findings are correctly categorized by priority
- [ ] Large scope triggers confirmation prompt

## Notes

- For PR-specific reviews with Azure DevOps integration, use `/review-pr` instead
- This command does not post comments or modify files
- Findings can be used to create follow-up tasks or PR comments manually
