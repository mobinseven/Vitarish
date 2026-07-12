---
name: platform-test-strategy
description: Chooses the smallest required test set, test placement, regression checks, contract checks, architecture checks, and environment-dependent hot scenarios. Use when adding or reviewing behavior, planning tests, changing risky code, or deciding validation for runtime dependencies.
---

# Platform Test Strategy

## Purpose

Define enough verification to catch real failure without turning testing into ceremony. This skill owns test selection, placement, and hot-scenario routing; it does not own product scope or architecture decisions.

## Use

Use this skill for new behavior, bug fixes with regression risk, architecture changes, API/client changes, React UI behavior, migrations, runtime dependencies, or any request asking what to test.

Skip it for pure documentation edits and trivial mechanical edits with no behavior or validation impact.

## Read

- `AGENTS.md`
- Existing test projects or colocated tests near the touched scope.
- Architecture, OpenAPI, Web, or localization validation docs when those boundaries are touched.

## Procedure

1. Identify the behavior or rule that can break.
2. Check `.agents/skills/README.md` → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Choose the narrowest deterministic check first: unit, component, contract, architecture validator, lint, typecheck, or self-check.
4. Place tests near the production owner using existing conventions. Do not invent a new test layout.
5. Add regression coverage for bug fixes at the root-cause boundary, not only the reported symptom path.
6. Use contract or architecture validation for boundary rules instead of duplicating those rules in many tests.
7. Mark a hot scenario when confidence requires DB, browser, network, files, queues, realtime, clocks, external services, permissions, latency, or deployment wiring.
8. Defer tests only with a reason, risk, owner, and trigger to revisit.

## Output

```md
## Test Strategy
Required tests:
Suggested checks:
Hot scenarios:
Test placement:
Validation commands:
Deferred tests:
Risk left:
```

## Hard Stops

- No runnable check exists for non-trivial new logic, risky bug fixes, security, money, auth, persistence, API contract, or migration behavior.
- Tests verify implementation details while missing the observable rule.
- A runtime-dependent behavior is claimed safe using only mocks.
- Test data creates a second source of truth for business rules.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Handoff

Hand off uncovered architecture issues to `platform-architecture-governance`, API/client issues to `platform-api-client-boundary`, React UI issues to `platform-react-ui-boundary`, and delivery readiness to `platform-delivery-loop`.
