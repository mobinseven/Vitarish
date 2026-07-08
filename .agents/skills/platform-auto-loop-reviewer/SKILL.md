---
name: platform-auto-loop-reviewer
description: Review automated implementation PRs against the source plan, AGENTS.md, accepted decisions, required skills, and validation evidence, then return an approve/changes-needed/escalate verdict.
---

# Platform Auto Loop Reviewer

## Purpose

Review an automated implementation and decide whether it is acceptable, needs changes, or must be escalated.

This skill owns review verdicts for automated loop output. It does not execute fixes, generate new implementation plans directly, or manage loop state.

## Use When

Use this skill when:

- a PR was created by the auto-implementation workflow;
- review is requested via automation or an `/auto-review` style command;
- the loop controller marks an implementation ready for review;
- a generated implementation must be compared against its plan.

## Do Not Use For

Do not use this skill for:

- normal human-directed PR delivery; use `platform-pr-delivery-loop`;
- generating initial plans; use `platform-auto-plan-generator`;
- executing a plan; use `platform-auto-plan-executor`;
- changing loop state directly; use `platform-loop-state-manager`;
- broad architecture review unrelated to an implementation; use `platform-adr-steward` or `platform-artifact-placement-steward`.

## Relationship to Other Skills

Review against the skills named by the plan and any skills implied by changed files.

Common review chain:

```txt
executor output -> auto loop reviewer -> state manager -> generator only if re-plan is required
```

Use `.agents/skills/README.md` for shared routing and ownership.

## Sources of Truth

- Source issue and generated plan.
- PR diff and changed files.
- Execution report and validation evidence.
- `AGENTS.md` architecture rules.
- `.agents/skills/README.md` shared skill contract.
- Skills required by the plan or implied by changed artifacts.
- Accepted ADRs and scoped docs referenced by the change.

## Operating Procedure

1. Gather the source issue, plan, PR diff, changed files, execution report, and validation output.
2. Confirm the implementation matches the plan scope and non-goals.
3. Check each changed artifact for responsibility, placement, dependency direction, source of truth, CQRS, and failure handling.
4. Verify the required skills were applied and their hard stops were respected.
5. Verify required tests/checks were run or honestly marked unavailable/deferred.
6. Classify findings by severity.
7. Return one verdict: `approved`, `changes_needed`, or `escalate`.
8. If changes are needed, identify whether the existing plan remains valid or a re-plan is required.
9. Hand off the verdict to `platform-loop-state-manager`.

## Verdict Rules

### Approved

Use `approved` only when:

- implementation matches the plan;
- no blocking architecture or skill violations remain;
- required validation passed or warnings are explicitly acceptable;
- no unplanned risky work was introduced.

### Changes Needed

Use `changes_needed` when:

- the plan is still valid;
- defects are concrete and fixable within the current scope;
- missing tests or validation can be added without changing the plan;
- architecture/placement issues are local and correctable.

### Escalate

Use `escalate` when:

- the plan is wrong, stale, or unsafe;
- scope no longer fits the automation loop;
- a required product or architecture decision is missing;
- repeated iterations fail;
- validation cannot prove the required behavior;
- human judgment is required.

## Required Output

```md
## Auto Loop Review

Plan: <path>
PR: #<number>
Verdict: approved | changes_needed | escalate

### Scope Match
- <pass/fail summary>

### Findings
| Severity | File | Rule/skill | Issue | Required fix |
| --- | --- | --- | --- | --- |

### Validation Review
| Check | Status | Evidence |
| --- | --- | --- |

### Skill Compliance
| Skill | Status | Notes |
| --- | --- | --- |

### Re-plan Required
Yes / No

### Handoff
Next: `platform-loop-state-manager`
```

## Severity Definitions

- `critical`: unsafe, architecture-breaking, data-breaking, or impossible to approve.
- `major`: must fix before approval.
- `minor`: should fix if low-cost; may be deferred with reason.
- `suggestion`: optional improvement, not blocking.

## Hard Stops

Do not approve when:

- critical or major findings remain;
- required validation failed;
- the implementation differs materially from the plan;
- a generated file was manually edited in violation of the owning skill;
- frontend/backend or architecture boundaries are broken;
- business rules or state sources were duplicated;
- required skill hard stops were ignored.

## Guardrails

- Do not invent issues.
- Do not produce vague feedback.
- Do not ignore missing validation.
- Do not re-plan inside the review result; state that a re-plan is required and why.
- Do not approve based on intent when the diff violates the plan or rules.
- Do not block on cosmetic suggestions.

## Handoff

Hand the verdict to `platform-loop-state-manager`. If re-planning is required, the state manager routes back to `platform-auto-plan-generator`.

## Style

Be precise, evidence-based, and fix-oriented. Keep findings actionable and scoped to changed files.
