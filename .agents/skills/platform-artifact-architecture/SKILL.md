---
name: platform-artifact-architecture
description: Classifies changed artifacts, assigns authoritative ownership and placement, verifies dependency direction, and decides whether an ADR is required. Use when creating, moving, splitting, reviewing, or planning architecture-sensitive artifacts. Do not use for trivial edits with obvious ownership.
---

# Platform Artifact Architecture

## Purpose

Keep every artifact in the narrowest correct owner with one primary responsibility and inward dependency direction. This skill owns placement and architecture impact, not behavior implementation.

## Use when

- New or structurally changed source, contract, adapter, UI, persistence, configuration, test, generated, or documentation artifacts are proposed.
- A module, host, package, public boundary, or stable architecture rule may change.

## Do not use

- A typo, local style correction, or expectation-only test edit does not change ownership or dependencies.

## Procedure

1. Classify each material artifact: Domain, Application command/query, Application port, Contract/DTO, Infrastructure adapter, Persistence, Transport/API, React presentation, Configuration/composition, Generated output, Test, or Documentation.
2. Assign the narrowest owner: module, capability, app, host, package, or documentation scope.
3. State one primary responsibility per new or structurally changed artifact.
4. Verify dependency direction against `docs/architecture.md` and accepted ADRs. Core/Application must not import transport, UI, persistence, vendor SDKs, or other infrastructure details.
5. Name the source of truth and every derived representation or generation path.
6. Prefer an existing owner and cohesive edit over a new layer, project, wrapper, or abstraction.
7. Require an ADR only for a new or changed durable architecture rule, transport model, dependency policy, or cross-module boundary—not for routine implementation.
8. Record the architecture validators required by the touched conventions.

## Hard stops

- Two artifacts can independently decide the same rule or state.
- An inward layer depends on an outward implementation detail.
- A public or persisted boundary changes without a compatibility decision.
- The correct owner cannot be determined from repository evidence and choosing incorrectly would create a durable boundary.

## Handoff

Route behavior to `platform-backend-use-case`, persistence to `platform-data-evolution`, transport to `platform-transport-contract`, provider work to `platform-external-integration`, and UI to `platform-react-feature`.
