---
name: platform-scope-planning
description: Classifies product intent, workflow impact, roadmap need, owner scope, non-goals, and the smallest useful delivery slice. Use for new features, ambiguous scope, journey or roadmap questions, implementation task derivation, PR slicing, or any request that may exceed one focused change.
---

# Platform Scope Planning

## Purpose

Turn product and delivery uncertainty into a scoped, minimal plan. This skill owns journey, roadmap, task-derivation, and PR-slice triage; it does not make architecture decisions or implement code.

## Use

Use this skill when the request mentions user workflows, personas, design objectives, MVP, roadmap, phases, release slicing, implementation tickets, one PR, unclear scope, or a feature that may need sequencing.

Skip it for small local bug fixes, straightforward refactors, test-only corrections, or implementation tasks whose owner, scope, and validation are already explicit.

## Read

- `AGENTS.md`
- Existing scoped docs under `docs/apps/...` or `docs/platform/modules/...` for the owner being changed.
- Accepted ADRs only when the scope may change architecture.

## Procedure

1. Identify the intended user, workflow, owner scope, and first useful outcome.
2. Check AGENTS.md → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Apply the minimum-change ladder from `AGENTS.md`: reuse existing docs, flows, routes, components, contracts, and tests before creating new artifacts.
4. Decide whether a journey map is needed. Require one only when user workflow, persona, pain point, or experience sequencing is unclear or changing.
5. Decide whether a roadmap is needed. Require one only for multi-phase scope, sequencing trade-offs, or implementation-ticket derivation.
6. Keep implementation tickets provisional unless the roadmap is approved or the user explicitly asks for provisional tickets.
7. Name non-goals, deferred scope, and the handoff skill that owns the next decision.

## Output

Use this shape when producing planning output:

```md
## Scope Brief
Outcome:
Owner:
User/workflow impact:
Smallest delivery slice:
Required artifacts:
Non-goals:
State/source of truth:
Handoff:
Validation:
```

## Hard Stops

- Stop for a human decision when the requested scope cannot fit one focused change.
- Stop when a product decision, architecture decision, security choice, data-loss behavior, or external dependency choice is missing.
- Do not invent product goals, acceptance criteria, personas, or phases as facts. Mark assumptions.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Handoff

Hand off to `platform-architecture-governance` for boundaries or ADR impact, `platform-test-strategy` for coverage, `platform-react-ui-boundary` for React UI, `platform-api-client-boundary` for OpenAPI or generated clients, and `platform-delivery-loop` for implementation.
