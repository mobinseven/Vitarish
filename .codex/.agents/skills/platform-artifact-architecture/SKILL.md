---
name: platform-artifact-architecture
description: Owns artifact classification, placement, responsibility, dependency direction, sources of truth, and ADR need. Use for structural changes; skip obvious local edits.
---

# Platform Artifact Architecture

## Rules

1. Classify each material artifact: Domain, Application command/query/port, Contract, Infrastructure adapter, Persistence, Transport, React, composition/configuration, Generated, Test, or Documentation.
2. Assign the narrowest module, capability, app, host, package, or documentation owner and one primary responsibility.
3. Verify dependencies against `docs/architecture.md` and ADRs; Domain/Application cannot import outward implementations.
4. Name the authoritative source and every derived or generated representation.
5. Prefer an existing owner and cohesive edit over a new layer, project, wrapper, or abstraction.
6. Require an ADR only for a durable architecture rule, transport model, dependency policy, or cross-module boundary change.
7. Identify required architecture validators.

## Stop

- Two artifacts independently decide one rule/state.
- An inward layer depends outward.
- A public/persisted boundary changes without compatibility intent.
- Ownership cannot be established from repository evidence and a wrong choice would create a durable boundary.
