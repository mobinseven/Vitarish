---
name: platform-delivery-loop
description: Executes one approved repository change by revalidating the plan, loading only touched boundary skills, implementing coherent slices, running evidence-based validation, reviewing the diff, and producing a precise handoff. Use for implementation or final delivery review. Do not use for planning-only requests.
---

# Platform Delivery Loop

## Purpose

Turn an approved plan or well-scoped task into a focused, validated change. This skill orchestrates execution; boundary skills retain ownership of their technical decisions.

## Use when

- The user authorizes implementation, asks to execute a plan, or requests final delivery review.
- A local task is sufficiently explicit to implement without a separate planning artifact.

## Do not use

- The requested outcome is planning, critique, or analysis only.

## Procedure

1. Re-read current files and revalidate plan assumptions; repository state outranks stale plan detail.
2. State the focused objective, non-goals, expected contract/data/security/generated-output impact, and success evidence.
3. Select only the boundary skills touched by the implementation.
4. Implement in coherent slices, keeping each slice buildable/testable when practical. Change authoritative inputs before generated outputs.
5. Prefer deletion, reuse, standard/native behavior, existing dependencies, and cohesive edits over new abstractions.
6. Run `platform-test-validation` checks as soon as a slice can prove something, not only at the end.
7. Review the final diff for scope creep, duplicate sources of truth, wrong placement, inward dependency leaks, command/query mixing, unsafe failures, manual generated edits, missing cleanup, and stale references.
8. Classify unresolved feedback as Must-fix, Follow-up, or Rejected with reason. Do not silently defer.
9. Handoff with exact changes, evidence, unrun checks, compatibility/data/security/operational impact, plan deviations, and residual risk.

## Hard stops

- Implementation requires a missing product, security, data-loss, compatibility, or provider decision.
- Scope no longer forms one focused delivery unit.
- The only path forward weakens repository safeguards or edits generated output manually.

## Handoff

Use the completion criteria in `AGENTS.md`. Route newly discovered design defects back to the owning boundary skill rather than expanding this skill.
