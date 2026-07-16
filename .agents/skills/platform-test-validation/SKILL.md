---
name: platform-test-validation
description: Chooses and runs the smallest sufficient deterministic tests, architecture and contract validators, generators, builds, typechecks, and runtime hot scenarios for a change. Use for any non-trivial behavior or boundary change and for validation planning. Do not use for pure prose edits.
---

# Platform Test Validation

## Purpose

Prove observable behavior and boundary integrity with the smallest sufficient evidence. This skill owns validation selection and reporting, not production design.

## Use when

- Behavior, security, public contracts, persistence, integrations, generated code, React lifecycle, or architecture boundaries change.
- A plan or implementation needs test placement, commands, or hot scenarios.

## Do not use

- A documentation-only or trivial mechanical edit has no executable or structural effect.

## Procedure

1. List the observable rules and failure modes introduced or materially changed.
2. Choose the narrowest deterministic test for each: unit, application/session, adapter mapping, serialization/contract, endpoint/protocol, security negative, React hook/component, migration, or architecture validator.
3. Test at the authoritative owner; avoid duplicating business rules in fixtures or testing only mocks for runtime-dependent behavior.
4. Add regression coverage at the root-cause boundary for bug fixes.
5. Include generators, format/lint, typecheck, focused builds/tests, host builds, and repository validators required by touched boundaries.
6. Define runtime hot scenarios only where confidence depends on browser permissions/codecs, real providers, network behavior, database/index engines, deployment wiring, clocks, or concurrency.
7. Record exact commands and results. Distinguish passed, failed, not run, and environment-blocked checks.
8. Do not weaken tests, analyzers, validators, or assertions to obtain a pass.

## Required coverage for cross-stack realtime work

- Application ordering, cancellation, timeout, limits, and terminal outcomes
- Adapter mapping and provider failure behavior
- Protocol serialization, malformed frames, route stability, auth, origin, and payload limits
- React support/permission gating, state transitions, edit preservation, cleanup, localization, and read-only behavior
- Real browser/provider/network hot scenarios when those behaviors cannot be proven deterministically

## Hard stops

- Non-trivial auth, data, contract, migration, money, security, or realtime behavior has no runnable deterministic check.
- A runtime-only assumption is presented as proven solely by mocks.

## Handoff

Return validation evidence to `platform-delivery-loop`, or validation requirements to `platform-change-planning` for a planning-only request.
