---
name: platform-pr-delivery-loop
description: Coordinate a concise single-PR delivery cycle for Vitarish work from objectives to plan, implementation, review, feedback triage, repeat decisions, and final PR handoff. Use when a task needs one pull request delivered through objective clarification, planning, implementation, review, feedback triage, and repeat decisions while orchestrating existing project skills instead of duplicating their instructions.
---

# Platform PR Delivery Loop

Use this skill to coordinate one focused pull request from objective clarification through final PR summary. Keep the loop small, evidence-driven, and aligned with existing project skills.

## Loop

```txt
objective clarification, planning, implementation, review, feedback triage, and repeat decisions
```

## 1. Restate Objectives and Constraints

Start by briefly restating:

- The requested outcome and non-goals.
- User constraints, repository instructions, architecture rules, and delivery requirements.
- The intended PR scope in one sentence.

If the objective is ambiguous or the constraints conflict, stop and ask for a human decision before planning.

## 2. Select Required Project Skills

Decide which existing skills are required before implementation. Load only the needed skill files.

- User journey: use `platform-user-journey-curator` for user workflow, persona, design objective, pain points, or journey-stage work.
- Roadmap: use `platform-feature-roadmap-curator` for phased scope, MVP/short-term/long-term sequencing, tickets, or task derivation.
- ADR: use `platform-adr-steward` for architecture decisions, supersession, conflicts, or durable design records.
- Placement: use `platform-artifact-placement-steward` before creating, moving, or reclassifying production artifacts.
- Tests: use `platform-test-curator` for required unit tests, hot scenarios, risk-based test planning, or new test-file placement.

Skip irrelevant skills for small local bug fixes, styling-only changes, simple refactors, documentation-only updates, and test-only corrections. State why skipped checks are not applicable.

## 3. Run Pre-Implementation Checks When Applicable

Before creating or moving production code, answer concisely:

1. Is a journey needed?
2. Is roadmap/task derivation needed?
3. Is an ADR needed?
4. Where should each artifact live?
5. What unit tests are required?
6. What hot test scenarios are required?

Do not proceed when placement, architecture impact, or test risk is unresolved.

## 4. Define Implementation Slice Boundaries

Plan the smallest coherent slice that can be reviewed as one PR:

- List artifacts to add/change and each artifact's single responsibility.
- Identify dependency direction and state ownership/source of truth.
- Separate command/write paths from query/read paths.
- Define tests/checks that prove the slice.
- Explicitly leave out speculative or unrelated work.

If the slice grows beyond one focused PR, stop and ask whether to split scope.

## 5. Implement

Implement only the approved slice. Preserve architecture boundaries and avoid duplicating business rules, validation, state, or infrastructure concerns. Prefer new focused artifacts over broad edits to stable code when an extension point already exists.

## 6. Review Gates

Before feedback triage or finalization, run these gates:

- Scope gate: changes match the stated PR scope and non-goals.
- Architecture gate: artifacts have one responsibility, correct placement, protected core logic, and one source of truth.
- CQRS gate: commands mutate state only; queries return data only.
- Failure gate: inputs, dependency failures, cancellation, and partial failure are handled intentionally.
- Test gate: required unit tests and applicable hot scenarios are covered or explicitly deferred with rationale.
- Regression gate: relevant automated checks have been run and results recorded.
- Delivery gate: final response and PR message can clearly summarize changes, tests/checks, risks, and follow-ups.

Fix gate failures before continuing unless a human explicitly accepts the risk.

## 7. Triage Feedback

Classify each feedback item as:

- Must-fix in this PR: required for correctness, safety, architecture compliance, tests, or requested scope.
- Should-fix in this PR: low-risk improvement that strengthens the current slice without expanding scope.
- Follow-up: valid but outside the approved slice.
- Reject/defer: conflicts with constraints, duplicates existing work, or adds speculative complexity.

Record the decision and rationale. Do not silently ignore feedback.

## 8. Repeat Criteria

Repeat `plan -> implement -> review -> feedback` when:

- A review gate fails.
- Must-fix feedback remains unresolved.
- Tests/checks expose a defect in the current slice.
- Implementation reveals a changed placement, architecture, or test-risk decision.

Each repeat must keep the PR focused. Re-run only the skills and checks affected by the new information.

## 9. Stop and Ask for Human Decision

Stop instead of continuing when:

- Objectives, constraints, or acceptance criteria conflict.
- Scope no longer fits a single focused PR.
- A required architecture or product decision is missing.
- A risky migration, data loss, security trade-off, or external dependency choice is required.
- Required validation cannot be performed and the risk is material.
- Feedback requires changing agreed non-goals.

Ask a specific question with the smallest set of viable options.

## 10. Final PR Handoff

Before finishing, ensure the PR handoff includes:

- Summary of changed behavior and touched artifacts.
- Tests/checks run, with pass/fail/warning status.
- Known risks and mitigations.
- Remaining follow-ups or explicitly state none.
- Any required per-artifact audit or architecture notes from repository instructions.
