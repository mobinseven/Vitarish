---
name: platform-auto-plan-generator
description: Generate schema-valid implementation plans from GitHub issues by extracting objective, scope, constraints, acceptance criteria, validation, and explicit skill routing.
---

# Platform Auto Plan Generator

## Purpose

Convert a GitHub issue into one executable implementation plan that the automated implementation loop can follow.

This skill owns issue-to-plan translation. It does not execute the plan, review the resulting PR, or manage loop state.

## Use When

Use this skill when:

- a GitHub issue is labeled `auto-implementation`;
- a user requests automated implementation from an issue;
- requirements must be converted into a plan file under `.mimocode/plans/`;
- an automated loop needs a re-plan after review feedback.

## Do Not Use For

Do not use this skill for:

- normal human-directed one-PR work; use `platform-pr-delivery-loop`;
- free-form roadmap creation; use `platform-feature-roadmap-curator`;
- architecture decision creation; use `platform-adr-steward`;
- implementation after a plan exists; use `platform-auto-plan-executor`;
- reviewing a completed implementation; use `platform-auto-loop-reviewer`;
- changing loop state except to name the expected next state; use `platform-loop-state-manager`.

## Relationship to Other Skills

Load only the skills required by the issue.

| Issue content | Route to |
| --- | --- |
| User workflow, persona, design objective, journey gap | `platform-user-journey-curator` |
| MVP/phases, roadmap, task derivation | `platform-feature-roadmap-curator` |
| Architecture decision or accepted ADR conflict | `platform-adr-steward` |
| File/project/folder ownership | `platform-artifact-placement-steward` |
| Frontend/backend boundary | `platform-frontend-backend-isolation` |
| OpenAPI contract | `platform-openapi-contract-steward` |
| Generated React client | `platform-generated-react-client-steward` |
| React/HeroUI feature integration | `platform-feature-ui-integration-steward` |
| Localization | `platform-localization-maintenance` |
| Tests or hot scenarios | `platform-test-curator` |

Follow `.agents/skills/README.md` for the shared skill operating model.

## Sources of Truth

- GitHub issue body, title, labels, comments, and linked PRs.
- `AGENTS.md` architecture rules.
- `.agents/skills/README.md` shared skill contract.
- `.mimocode/schemas/plan-schema.json` when present.
- Accepted ADRs and existing scoped docs referenced by the issue.
- Existing repository conventions in the owning app/module/capability.

## Operating Procedure

1. Parse the issue objective, requested behavior, constraints, labels, acceptance criteria, and explicit non-goals.
2. Classify the owning scope: `App`, `Module`, `Capability`, `Related apps`, `Related modules`.
3. Identify missing decisions, assumptions, dependencies, and risks.
4. Route only the skills needed for the plan.
5. Break the work into the smallest executable phases.
6. For each phase, name artifacts, responsibilities, expected locations, validation, and acceptance criteria.
7. Mark unresolved decisions as blockers or assumptions; do not hide them inside implementation steps.
8. Validate the plan shape against the schema when available.
9. Write one plan file under `.mimocode/plans/`.
10. Hand off to `platform-auto-plan-executor` and `platform-loop-state-manager`.

## Required Output

Every plan must include:

```markdown
# Plan: {title}

## Metadata
- Issue: #{number}
- Generated: {timestamp}
- Iteration: {n}/{max}
- Owning scope: {app/module/capability}
- Skills required: {list}
- Source of truth: {issue/docs/ADR}

## Objective
{one paragraph}

## Scope
### In Scope
- {item}
### Out of Scope
- {item}

## Assumptions and Blockers
- {assumption or blocker}

## Phases

### Phase 1: {name}
#### Objective
{what this phase achieves}

#### Artifacts
| Artifact | Type | Responsibility | Location | Owning skill |
| --- | --- | --- | --- | --- |

#### Implementation Steps
1. {step}

#### Validation
```bash
{command}
```

#### Acceptance Criteria
- [ ] {criterion}

## Verification
1. {check}
```

## Hard Stops

Do not generate an executable plan when:

- the issue has no clear objective;
- acceptance criteria are absent and cannot be safely inferred;
- the scope cannot fit one focused implementation loop;
- an architecture decision is required but missing;
- the plan would violate `AGENTS.md`;
- artifact ownership or source of truth is unresolved;
- validation cannot be named honestly.

Escalate to human review instead of producing vague work.

## Guardrails

- Do not create plans that rewrite stable core logic without justification.
- Do not exceed a reasonable scope; prefer splitting over broad plans.
- Do not invent product goals, deadlines, or architecture decisions.
- Do not load every skill by default.
- Do not create duplicate source-of-truth docs.
- Always include validation commands or explain why validation is manual/not available.
- Always include acceptance criteria.

## Handoff

After plan generation, update loop state through `platform-loop-state-manager` and hand off to `platform-auto-plan-executor`.

## Style

Be concise, deterministic, and executable. Prefer tables for artifacts and checklists for acceptance criteria.
