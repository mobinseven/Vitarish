---
name: platform-auto-loop-reviewer
description: Review implementations against architecture rules and determine if changes are needed or approved.
---

# Platform Auto Loop Reviewer

## Purpose

Review PRs created by the implementation loop and determine if they meet architecture rules.

## Use When

- PR created by auto-implementation workflow
- Review requested via `/auto-review` comment
- Loop controller detects PR ready for review

## Review Process

1. **Gather Context**: PR, diff, AGENTS.md, relevant skills
2. **Analyze Changes**: Check each changed file
3. **Check Architecture**: Validate against AGENTS.md rules
4. **Check Skills**: Verify skill compliance
5. **Check Tests**: Verify test coverage
6. **Generate Findings**: List issues with severity
7. **Determine Verdict**: approve or changes_needed
8. **Post Comment**: Post review with findings
9. **Update Loop**: Handle next action

## Verdict Rules

### Approved
- No architecture violations
- All required tests present
- Skill compliance verified
- Build and tests pass

### Changes Needed
- Architecture violations found
- Missing required tests
- Skill non-compliance
- Build or test failures

## Review Findings Format

```markdown
## Finding: {title}

**Severity**: critical | major | minor | suggestion
**Rule**: {AGENTS.md rule or skill rule}
**Location**: {file:line}
**Description**: {what's wrong}
**Fix**: {how to fix}
```

## Architecture Checks

- SRP: One responsibility per artifact
- Single state model: No duplicated domain concepts
- Low coupling: Minimal inter-component knowledge
- Abstraction: Depend on abstractions
- CQRS: Commands mutate, queries return data
- Failure safety: Handle all failures explicitly
- Open/Closed: Extend through new code
- Core protection: Protect from infrastructure
- KISS/YAGNI: No speculative complexity
- Single source of truth: One truth for every value

## Skill Compliance Checks

- Correct skill loaded for work type
- Skill output format followed
- Skill guardrails respected
- Skill validation commands run

## Re-plan Generation

When changes needed:

1. Analyze findings
2. Determine which phases need rework
3. Generate new plan with fixes
4. Write to `.mimocode/plans/`
5. Update loop state
6. Commit and push

## Guardrails

- Do not approve with critical violations
- Do not generate vague feedback
- Do not skip architecture checks
- Do not invent issues
- Do not ignore skill compliance