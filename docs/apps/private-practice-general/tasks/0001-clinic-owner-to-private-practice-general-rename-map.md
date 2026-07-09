# Rename Map: clinic-owner to private-practice-general

Status: Active
Owner: Platform Architecture
Scope: App identity rename tracking for ADR-0008
Last reviewed: 2026-07-09

## Rename Scope

| Aspect | Old | New |
|---|---|---|
| Doc root | `docs/apps/clinic-owner` | `docs/apps/private-practice-general` |
| App label | `clinic-owner` | `private-practice-general` |
| User-facing name | Clinic Owner | Private Practice General |
| npm package name | `@vitarish/web` | `@vitarish/private-practice-general` |
| App child folder | `src/Hosts/Web` (single-app root) | `src/Hosts/Web/apps/private-practice-general` |

## Backend Rename Boundary

Do not rename backend modules as part of this app-host split. The following backend domain terms remain unchanged:

- `Clinic` (backend domain module)
- `Appointments` (backend domain module)
- `Medical` (backend domain module)
- `Knowledge` (backend domain module)
- `SEA` (backend domain module)
- `Parties` (backend domain module)

Backend renames require a separate backend/module decision.

## Route Compatibility Policy

TBD â€” Decide during implementation whether old route paths must redirect or may break.

## Docs-Link Compatibility Policy

Global indexes and journey-graph visualizer links must be updated to point to `docs/apps/private-practice-general`. Old `docs/apps/clinic-owner` paths become invalid once the doc root moves. No redirect mechanism exists for docs links.

## Search Strings to Replace

| Pattern | Context | Action |
|---|---|---|
| `clinic-owner` | App identity label, docs folder, config references | Replace with `private-practice-general` |
| `Clinic Owner` | User-facing app name in headings, navigation, UI copy | Replace with `Private Practice General` |
| `apps/clinic-owner` | Docs paths in links and indexes | Replace with `apps/private-practice-general` |
| `@vitarish/web` | npm package name reference | Replace with `@vitarish/private-practice-general` |

## Strings NOT Replaced

Domain terms that describe workflows, not app identity:

- `clinic`, `patient`, `appointment`, `reservation`, `prescription`, `provider`, `service`, `shift`, `reminder`
- Backend module names (`Clinic`, `Medical`, `Knowledge`, `SEA`, `Appointments`)
