---
name: platform-architecture-governance
description: Governs artifact classification, file placement, dependency direction, ADR impact, frontend/backend isolation, and architecture rule audits. Use before creating, moving, or reviewing artifacts, module boundaries, contracts, clients, API adapters, infrastructure adapters, UI boundaries, or ADRs.
---

# Platform Architecture Governance

## Purpose

Protect the architecture pillars in `AGENTS.md`. This skill owns placement, boundary review, ADR routing, and per-artifact rule audits; it does not write product roadmaps, test plans, or UI copy.

## Use

Use this skill for any source, test, config, API, contract, client, UI, infrastructure, or documentation artifact whose owner or dependency direction matters. Use it for ADR creation or review when a stable rule, boundary, or cross-module dependency changes.

Skip it only for tiny local edits whose artifact owner, layer, dependency direction, and tests are obvious.

## Read

- `AGENTS.md`
- `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md` when Interface, Contracts, Client, Api, React Web, or frontend/backend boundaries are involved.
- `docs/architecture/conventions/adr-0003-implementation-conventions.md` and `docs/architecture/validation/adr-0003-interface-boundary-validation.md` when applying ADR-0003.
- Existing accepted ADRs and conventions in the owning scope.

## Procedure

1. Classify every artifact as exactly one of: Domain, Application, Command, Query, Port/contract, Adapter/infrastructure, Presentation/API/UI, Data transfer shape, Persistence model, Test, Configuration/composition, or Documentation.
2. Check AGENTS.md → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Identify the narrowest owner: app, module, capability, host, React Web feature, interface package, platform module, library, identity context, docs scope, or test scope.
4. Apply the layer boundary: Domain -> Application -> Infrastructure/Api/Client -> Presentation/Host, with Contracts as serialization-only boundary shapes.
5. Name the single source of truth for business rules, state transitions, validation rules, routes, DTOs, generated clients, UI copy, and docs.
6. Separate commands from queries. Redesign any artifact that mutates and queries observable state in the same API.
7. Check ADR impact. Recommend an ADR only for a new or changed stable architecture rule.
8. Keep the design minimal. Reuse existing conventions and do not create folders, projects, or abstractions for symmetry.

## Output

Use this shape for reviews or proposals:

```md
## Architecture Governance
Artifacts:
Dependency direction:
State/source of truth:
Command/query split:
Failure handling:
ADR impact:
Validation:
Hard stops:
```

For code submissions, include the per-artifact audit required by `AGENTS.md`.

## Hard Stops

- Domain or Application imports infrastructure, UI, transport, framework, filesystem, clock, network, persistence, or vendor SDK details.
- Contracts contain business behavior, persistence models, UI state, or backend implementation details.
- Api endpoints contain business rules or return domain entities directly.
- Client, Interface, or React Web artifacts import backend Application, Domain, Infrastructure, Api, server projects, persistence libraries, or server-only modules.
- A business rule or state source appears in more than one place without an explicit boundary and mapping.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Validation

When ADR-0003 boundaries are touched, run:

```bash
python3 tools/architecture/validate_module_api_boundaries.py
```

Also run any narrower architecture validator named by the touched convention.
