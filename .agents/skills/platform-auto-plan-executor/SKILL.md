---
name: platform-auto-plan-executor
description: Execute schema-valid implementation plans by applying only the planned slice, loading required skills, running validation, and preparing a reviewable PR.
---

# Platform Auto Plan Executor

## Purpose

Execute one generated implementation plan safely and reviewably.

This skill owns plan execution. It does not generate plans, approve implementations, or own loop state transitions beyond reporting execution results.

## Use When

Use this skill when:

- a plan file exists in `.mimocode/plans/`;
- loop state indicates implementation is needed;
- a GitHub Actions or agent workflow is executing an approved/generated plan;
- a re-plan has been generated and must be applied.

## Do Not Use For

Do not use this skill for:

- creating or rewriting the plan; use `platform-auto-plan-generator`;
- reviewing the implementation; use `platform-auto-loop-reviewer`;
- managing status, iteration, or termination; use `platform-loop-state-manager`;
- normal human-directed one-PR work; use `platform-pr-delivery-loop`;
- adding work that is not present in the plan.

## Relationship to Other Skills

The plan declares required skills. Load only those skills and any directly implied guardrail skill.

Common execution handoffs:

```txt
plan -> executor -> reviewer -> state manager
```

Use `.agents/skills/README.md` for shared routing and decision ownership.

## Sources of Truth

- The current plan file in `.mimocode/plans/`.
- `AGENTS.md` architecture rules.
- `.agents/skills/README.md` shared skill contract.
- Skills listed in the plan.
- Accepted ADRs and scoped docs referenced by the plan.
- Existing repository conventions in the owning scope.

## Operating Procedure

1. Read the plan and validate that it has objective, scope, artifacts, phases, validation, and acceptance criteria.
2. Confirm the plan is still executable against the current repository state.
3. Load the skills listed for the current phase.
4. Execute phases in order and only within declared scope.
5. For each artifact, verify responsibility, location, dependency direction, and source of truth before editing.
6. Preserve generated-code boundaries; do not manually edit generated files unless the plan explicitly owns generator input/config.
7. Run the validation commands named by the plan.
8. Record pass/fail/warning evidence for every validation step.
9. Prepare a focused commit/PR with the plan reference.
10. Report execution results for loop-state update.

## Required Output

Execution reports must include:

```md
## Auto Plan Execution Report

Plan: <path>
Issue: #<number>
Iteration: <n>/<max>

### Executed Scope
- <summary>

### Changed Artifacts
| Artifact | Responsibility | Phase | Notes |
| --- | --- | --- | --- |

### Skills Applied
- <skill>: <why>

### Validation
| Command / check | Status | Evidence |
| --- | --- | --- |

### Deviations
- <none or explicit deviation with reason>

### Handoff
Next: `platform-auto-loop-reviewer` / `platform-loop-state-manager`
```

## Hard Stops

Stop execution when:

- the plan file is missing, malformed, or schema-invalid;
- the plan objective conflicts with the issue or accepted decisions;
- artifact ownership is unclear enough to risk wrong placement;
- implementation requires unplanned architecture, migration, security, data-loss, or external-service decisions;
- required validation cannot be run and no honest substitute exists;
- the implementation would exceed the planned scope;
- a generated file must be manually edited to make the plan work.

Escalate instead of silently changing the plan.

## Guardrails

- Never modify the plan file during execution.
- Do not add opportunistic cleanup or unrelated fixes.
- Do not skip required skills.
- Do not commit without validation evidence or an explicit warning.
- Do not amend previous automation commits unless the workflow explicitly requires it.
- Do not create parallel state outside `platform-loop-state-manager`.
- Keep one implementation branch/PR focused on one plan.

## Handoff

After execution, hand off to `platform-auto-loop-reviewer` for review and to `platform-loop-state-manager` for status update.

## Style

Be mechanical, explicit, and evidence-driven. Prefer small diffs over clever broad rewrites.
