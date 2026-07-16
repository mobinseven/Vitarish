---
name: platform-backend-use-case
description: Designs and implements module Domain/Application behavior, commands, queries, orchestration, ports, session state, cancellation, limits, and stable outcomes. Use when backend business or application behavior changes. Do not use for transport-only, persistence-only, or vendor-adapter-only work.
---

# Platform Backend Use Case

## Purpose

Keep backend behavior in its authoritative Domain/Application owner while infrastructure remains replaceable. This skill owns use-case behavior and application ports, not transport framing or vendor SDK code.

## Use when

- A command, query, workflow, session, state transition, application policy, or port is added or changed.
- Cancellation, ordering, timeout, idempotency, concurrency, limits, or terminal outcomes are part of application behavior.

## Do not use

- Only endpoint metadata, schema migration mechanics, vendor calls, or React UI changes.

## Procedure

1. Identify observable behavior and decide command, query, or explicit session/workflow role.
2. Put pure invariants in Domain only when they are truly domain behavior; keep use-case sequencing and external capability coordination in Application.
3. Define an Application-owned port only when it protects a real boundary or controls nondeterminism. Do not mirror a vendor SDK.
4. Make state transitions, ordering, cancellation, timeout ownership, limits, and terminal outcomes explicit.
5. Return stable application outcomes/errors; do not leak HTTP, WebSocket, database, UI, or vendor-specific types inward.
6. Keep capability/configuration reads separate from state-changing sessions.
7. Preserve compatibility of existing use cases unless the plan explicitly changes them.
8. Add deterministic tests at the authoritative behavior boundary.

## Review questions

- Is there one owner for each rule and state transition?
- Can the use case run without transport or vendor types?
- Are command/query roles honest?
- Are cancellation and partial results defined rather than accidental?
- Is the new abstraction required now?

## Hard stops

- Application behavior depends directly on a provider SDK, database, HTTP/WebSocket object, browser concept, or UI model.
- A query changes observable state.
- Cancellation, timeout, or terminal behavior is ambiguous for a long-running or realtime operation.

## Handoff

Route provider implementation to `platform-external-integration`, transport exposure to `platform-transport-contract`, trust decisions to `platform-trust-boundary`, and tests to `platform-test-validation`.
