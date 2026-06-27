---
name: platform-pr-delivery-loop
description: Coordinate one focused PR from objective clarification through plan, implementation, review, feedback triage, repeat decisions, and final handoff. Use when delivery must stay within one pull request while routing to existing project skills only when needed.
---

# Platform PR Delivery Loop

## Purpose

Coordinate one small, reviewable pull request. Keep scope explicit, evidence-driven, architecture-safe, and limited to the approved slice.

```txt
objective -> plan -> implement -> review -> triage feedback -> repeat if needed -> handoff
```

## Use When

Use for work that should be delivered as one PR and needs PR-level scope control, skill routing, review gates, feedback handling, and final summary.

Skip for tiny edits that need no PR-level planning unless requested.

## Flow

### 1. Clarify Intent

State the requested outcome, non-goals, relevant constraints, repo/architecture rules, delivery requirements, and one-sentence PR scope.

Ask for a human decision only when ambiguity, conflict, or missing acceptance criteria would make planning unsafe.

### 2. Load Only Needed Skills

Use existing skills as operating manuals, not duplicated instructions:

- workflow/persona/journey: `platform-user-journey-curator`
- roadmap/MVP/phases/tickets: `platform-feature-roadmap-curator`
- architecture decision/conflict: `platform-adr-steward`
- artifact creation/movement/classification: `platform-artifact-placement-steward`
- unit tests/hot scenarios/test placement: `platform-test-curator`

Skip irrelevant skills for small bug fixes, styling-only work, simple refactors, docs-only changes, and test-only corrections. State skipped checks only when the omission may be questioned.

### 3. Run the Pre-Implementation Gate

Before creating or moving production code, answer: journey needed? roadmap/task derivation needed? ADR needed? artifact locations? required unit tests? required hot scenarios?

Do not implement while placement, architecture impact, or material test risk is unresolved.

### 4. Define the PR Slice

Plan the smallest coherent reviewable slice: artifacts and one responsibility each; dependency direction; state/rule source of truth; command/write versus query/read split; proof tests/checks; explicit out-of-scope items.

If the slice no longer fits one focused PR, stop and ask whether to split it.

### 5. Implement

Implement only the approved slice. Preserve boundaries, protect core logic, avoid duplicated rules/state/validation, and prefer focused extension points over broad edits to stable code.

### 6. Review Gates

Verify before feedback triage or handoff:

- Scope: changes match scope and non-goals.
- Architecture: responsibility, placement, dependency direction, protected core, and source of truth are correct.
- CQRS: commands mutate; queries return data; no mixed APIs.
- Failure: invalid input, dependency failure, cancellation, and partial failure are intentional.
- Tests: required tests/hot scenarios are covered or deferred with rationale.
- Regression: relevant checks were run and recorded.
- Delivery: summary can explain changes, checks, risks, and follow-ups.

Fix failures before continuing unless the user explicitly accepts the risk.

### 7. Triage Feedback

Classify every item as `Must-fix`, `Should-fix`, `Follow-up`, or `Reject/defer`. Record the reason. Do not silently ignore feedback.

### 8. Repeat Only When Needed

Repeat `plan -> implement -> review -> feedback` only when a gate fails, must-fix feedback remains, tests expose a slice defect, or implementation changes placement/architecture/test-risk assumptions.

Re-run only affected skills and checks. Keep the PR focused.

### 9. Stop Rules

Stop for a human decision when objectives conflict, scope exceeds one PR, a required product/architecture decision is missing, material security/migration/data-loss/external-dependency risk appears, required validation cannot be performed, or feedback changes agreed non-goals.

Ask one specific question with the smallest viable options.

### 10. Final Handoff

Finish with changed behavior and touched artifacts, tests/checks with pass/fail/warning status, risks and mitigations, follow-ups or `None`, and required architecture notes or per-artifact audits.
