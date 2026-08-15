# Vitarish Documentation

Status: Active  
Owner: Platform Architecture  
Scope: Documentation navigation  
Last reviewed: 2026-07-09

This file is a map, not a source of truth. Scoped documents remain authoritative in their owning folders.

## Primary scopes

- [Architecture](architecture/decisions/_index.md): global ADRs, conventions, and validation rules.
- [Platform](platform/README.md): app-agnostic module design, roadmaps, tasks, tests, runbooks, providers, and shared design-system docs.
- [Apps](apps/): app-specific decision trees, journeys, tests, decisions, roadmaps, tasks, and runbooks in flat app-owned collections.
- [Hosts](hosts/): host-runtime runbooks, tasks, and tests.
- [Indexes](indexes/): navigation-only journey and test indexes.
- [Development](development/): contributor and development workflow notes.
- [Runbooks](runbooks/): cross-cutting operational runbooks only.
- [ISO/IEC 15408 scope](security/iso15408/security-target.md): Security Target, TOE/threat model, SFR/SAR matrix, evaluated configuration, inventory, and single-machine topology.

## IDE discoverability

`docs/docs.csproj` exists only so Visual Studio can browse the documentation folder as a project. It is not an authoritative documentation source.
