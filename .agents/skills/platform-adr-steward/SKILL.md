---
name: platform-adr-steward
description: Maintain scoped platform Architecture Decision Records. Use when creating ADRs, reviewing architecture impact, detecting conflicts with accepted decisions, superseding ADRs, or updating ADR indexes. ADRs are grouped by owning app/module or platform module.
---

# Platform ADR Steward

## Purpose

Maintain Architecture Decision Records as the source of truth for significant architecture decisions.

Help draft, review, normalize, link, supersede, and summarize ADRs. Do not accept architecture decisions automatically. Accepted decisions require explicit human approval.

## Use When

Use this skill when the user asks to:

- create or update an ADR
- review whether a change needs an ADR
- detect conflicts with existing ADRs
- summarize architecture decisions
- supersede, deprecate, or reject an ADR
- maintain ADR indexes
- decide where an ADR belongs by app/module scope

## Scope-First Repository Contract

Before creating or updating an ADR, classify its owning scope:

```txt
App:
Module:
Capability:
Related apps:
Related modules:
```

Put the ADR in the workspace of the app/module it primarily belongs to.

App-owned ADRs:

```txt
docs/apps/{app-slug}/modules/{module-slug}/decisions/
```

Shared platform ADRs:

```txt
docs/platform/modules/{module-slug}/decisions/
```

Examples:

```txt
docs/apps/clinic-owner/modules/patient-procedures/decisions/ADR-0007-use-explicit-procedure-status.md
docs/platform/modules/search/decisions/ADR-0004-use-elasticsearch-for-search-retrieval.md
```

Each ADR has exactly one home folder. If an ADR affects multiple apps or modules, place it under the primary owner and list affected scopes in `Related apps` and `Related modules`.

Use `references/scope-location-rules.md` for placement rules.

## Authority Rules

You may:

- draft ADRs
- propose status changes
- update ADR indexes
- identify related ADRs
- detect possible conflicts
- recommend whether an ADR is needed
- recommend the owning app/module folder

You must not:

- mark an ADR as Accepted unless explicitly instructed
- delete ADR history
- silently rewrite accepted decisions
- invent decisions not present in the provided context
- duplicate one ADR across multiple folders
- create a second source of truth

## Architecture Principles

Apply the project principles in `references/platform-architecture-principles.md`.

Prefer the simplest process that preserves architectural knowledge.

## ADR Statuses

Use only:

- Proposed
- Accepted
- Rejected
- Deprecated
- Superseded

Prefer supersession over deletion.

## ADR Template

Use `assets/adr-template.md` for new ADRs.

Required metadata:

- Date
- Status
- App
- Module
- Capability
- Owner
- Related apps
- Related modules
- Supersedes
- Superseded by
- Related

Required sections:

- Context
- Decision
- Consequences
- Alternatives Considered
- Validation / Enforcement
- Notes

One ADR should capture one architectural decision.

## When ADR Is Required

An ADR is usually required for changes affecting:

- platform boundaries
- module ownership
- public APIs
- persistence strategy
- search or retrieval strategy
- identity, authorization, or tenant model
- AI provider/model strategy
- background processing
- deployment topology
- security posture
- shared architectural rules

An ADR is usually not required for:

- local refactoring
- bug fixes
- styling changes
- private implementation details
- tests
- changes already covered by an accepted ADR

When unsure, classify the change as `Needs Human Judgment`.

## Review Workflow

When reviewing a change:

1. Identify the architectural surface area.
2. Classify the owning app/module scope.
3. Read the local module ADR index.
4. Read related app/platform ADR indexes.
5. Read relevant accepted ADRs.
6. Compare the proposed change with accepted decisions.
7. Decide whether an ADR is required.
8. Report conflicts or supersession needs.
9. Draft an ADR only when appropriate.

Use this output:

```md
## ADR Impact

Classification: ADR Required / ADR Recommended / ADR Not Required / Needs Human Judgment

Owning scope:

Proposed location:

Reason:

Related ADRs:

Potential conflicts:

Recommended action:
```

## Creation Workflow

When creating an ADR:

1. Classify the owning app/module scope.
2. Find or create the local `decisions/` folder.
3. Find the next ADR number by scanning all repository ADRs, unless the project explicitly uses per-module numbering.
4. Create a short kebab-case title.
5. Use `assets/adr-template.md`.
6. Set status to `Proposed` unless explicitly instructed otherwise.
7. Link related and superseded ADRs.
8. Propose updates to the local module index and any global navigation index.

Use this output:

```md
## Proposed File

<scoped path>/decisions/ADR-NNNN-title.md

## ADR Draft

<ADR content>

## Index Updates

<local and global index rows>

## Notes

<assumptions or follow-up items>
```

## Conflict Rules

Report a possible conflict when a change:

- violates an accepted ADR
- introduces a second source of truth
- bypasses module boundaries
- duplicates a platform service
- reverses a decision without supersession
- adds complexity without clear architectural value

Use “possible conflict” when evidence is incomplete.

## Supersession Rules

When a decision replaces an older ADR:

- create a new ADR in the owner scope of the new decision
- add `Supersedes: ADR-NNNN` to the new ADR
- propose changing the old ADR status to `Superseded`
- add `Superseded by: ADR-NNNN` to the old ADR
- update local and global indexes

Do not delete the old ADR.

## Optional Validation

Use `scripts/validate_adrs.py` when deterministic checks are useful.

Example:

```bash
python .agents/skills/platform-adr-steward/scripts/validate_adrs.py docs
```

## Style

Be concise.

Use direct architecture language.

Do not create ceremony unless it protects architectural clarity.
