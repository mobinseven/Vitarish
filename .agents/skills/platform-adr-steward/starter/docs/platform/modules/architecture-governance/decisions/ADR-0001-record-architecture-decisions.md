# ADR-0001: Record Architecture Decisions

Date: YYYY-MM-DD
Status: Accepted
App: Platform
Module: Architecture Governance
Capability: Architecture decision records
Owner: Architecture
Related apps:
Related modules:
Supersedes:
Superseded by:
Related:

## Context

Significant architecture decisions need to be discoverable and preserved with their context and consequences.

## Decision

Use scoped Markdown Architecture Decision Records under the owning app/module or platform module.

## Consequences

Decisions remain close to the product or platform area they affect while global indexes can still provide navigation.

## Alternatives Considered

- One global ADR folder: simpler, but becomes hard to navigate as the platform grows.
- External wiki: easier editing, but more likely to drift from the repository.

## Validation / Enforcement

Review architecture-impacting changes for ADR needs and validate ADR metadata and placement.

## Notes

Indexes are navigation only. ADR files are the source of truth.
