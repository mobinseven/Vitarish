---
name: platform-backend-use-case
description: Owns Domain/Application use cases, commands, queries, workflows, ports, state transitions, cancellation, limits, and stable outcomes. Skip transport-, persistence-, or adapter-only work.
---

# Platform Backend Use Case

## Rules

1. Define observable behavior and its command, query, or explicit workflow/session role.
2. Put pure invariants in Domain; keep sequencing and external-capability coordination in Application.
3. Add an Application-owned port only for a real boundary or nondeterminism; never mirror a vendor SDK.
4. Explicitly define state transitions, ordering, cancellation, timeout ownership, limits, idempotency/concurrency, partial results, and terminal outcomes where relevant.
5. Return stable application outcomes; never leak transport, database, UI, browser, or vendor types inward.
6. Separate capability/configuration reads from state-changing sessions.
7. Preserve existing use-case compatibility unless explicitly changed.
8. Test deterministically at the authoritative behavior boundary.

## Stop

- Application directly depends on outward implementation details.
- A query mutates observable state.
- Long-running/realtime cancellation, timeout, or terminal behavior is ambiguous.
