---
name: platform-test-validation
description: Selects and reports the smallest sufficient deterministic tests, validators, generators, builds, typechecks, and runtime hot scenarios. Skip pure prose or trivial mechanical edits.
---

# Platform Test Validation

## Rules

1. List changed observable rules and failure modes.
2. Choose the narrowest deterministic evidence: unit, application/session, adapter, serialization/contract, endpoint/protocol, security-negative, React, migration, or architecture test.
3. Test at the authoritative owner; do not duplicate business rules in fixtures or use mocks to claim runtime behavior.
4. Add regression coverage at the root-cause boundary.
5. Run required generators, format/lint, typecheck, focused tests/builds, host builds, and repository validators.
6. Use hot scenarios only for behavior dependent on browsers/codecs/permissions, real providers/networks, database/index engines, deployment wiring, clocks, or concurrency.
7. Record exact commands as passed, failed, not run, or environment-blocked.
8. Never weaken assertions, tests, analyzers, or validators for a pass.

For cross-stack realtime changes, cover application lifecycle/limits, adapter failures, protocol/auth/origin/payload handling, React permissions/state/edit preservation/cleanup/localization, and unavoidable real runtime scenarios.

## Stop

- High-impact auth, data, contract, migration, money, security, or realtime behavior lacks runnable deterministic evidence.
- Mocks are presented as proof of runtime-only assumptions.
