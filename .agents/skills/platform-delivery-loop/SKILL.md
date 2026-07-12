---
name: platform-delivery-loop
description: Coordinates one focused repository delivery loop from objective, scope, implementation, validation, review, feedback triage, automation state, and handoff. Use for one-PR work, auto-implementation issue loops, plan execution, review verdicts, and delivery status decisions.
---

# Platform Delivery Loop

## Purpose

Keep implementation work reviewable, scoped, validated, and finishable. This skill owns the delivery loop; it does not own product scope, architecture decisions, API contracts, UI boundaries, or test strategy.

## Use

Use this skill when work should land as one focused change, when executing an approved plan, when reviewing completed changes before handoff, when triaging PR feedback, or when managing an automated issue-to-plan-to-implementation loop.

Skip it for pure analysis where the user explicitly does not want implementation or delivery.

## Read

- `AGENTS.md`
- `.agents/skills/README.md`
- Any approved implementation plan, issue, PR comments, review feedback, and validation logs that define the loop.

## Procedure

1. State the objective, one-sentence scope, non-goals, constraints, and success evidence.
2. Check `.agents/skills/README.md` → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Load only the task-specific skills required by the touched surface.
4. Implement only the approved slice. Prefer deletion, reuse, native platform features, standard library behavior, and existing dependencies before new code.
5. Run the smallest validation set that proves the change, plus required architecture/API/UI checks.
6. Review the diff for scope creep, architecture violations, command/query mixing, failure handling, missing tests, and stale references.
7. Classify feedback as `Must-fix`, `Should-fix`, `Follow-up`, or `Reject/defer`.
8. Repeat only while the same focused scope remains valid.

## Output

```md
## Delivery Loop
Scope:
Changes:
Validation:
Review result:
Feedback status:
Open risks:
Handoff:
```

For automated loops, also include iteration count, legal next state, termination condition, and escalation reason when applicable.

## Hard Stops

- Scope no longer fits one focused change.
- Required product, architecture, security, migration, data-loss, validation, or external-dependency decision is missing.
- The plan requires editing stable core logic when an extension point already exists.
- Review feedback is silently ignored or reclassified without reason.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Handoff

Hand off unresolved scope to `platform-scope-planning`, architecture to `platform-architecture-governance`, validation gaps to `platform-test-strategy`, simplification concerns to `platform-simplification-audit`, and skill/process defects to `platform-skill-governance`.
