---
name: platform-react-ui-boundary
description: Guides React Web UI placement, HeroUI usage, feature hooks, forms, tables, dialogs, localization, shared Web UI boundaries, accessibility, and generated-client consumption. Use for React pages, components, feature folders, user-facing copy, formatting, Web UI package promotion, or HeroUI integration.
---

# Platform React UI Boundary

## Purpose

Keep React UI workflow-shaped, localized, accessible, and separated from transport and backend behavior. This skill owns Web presentation boundaries; it does not own OpenAPI contracts or business rules.

## Use

Use this skill for files under `src/Hosts/Web/src/**`, React Web pages, feature hooks/components, HeroUI/HeroUI Pro components, user-facing text, locale formatting, API error display mapping, shared Web UI package decisions, and route-level UI composition.

Skip it for backend-only, contract-only, or generated-client-only changes with no React presentation impact.

## Read

- `AGENTS.md`
- `docs/architecture/decisions/0007-standardize-react-localization.md`
- `docs/architecture/decisions/0008-share-react-ui-components.md` when shared Web UI is involved.
- Existing files in the owning `pages`, `features`, `components`, `localization`, or `packages/web-ui` scope.
- Official HeroUI/HeroUI Pro guidance when component behavior or theming is uncertain.

## Procedure

1. Classify the UI artifact as route/page, module feature, capability feature, host component, localization artifact, generated API artifact, or shared Web UI package artifact.
2. Check `.agents/skills/README.md` → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Keep route files as composition surfaces. Reusable workflow UI belongs in feature/component/shared owners.
4. Keep feature hooks authoritative for generated-client wrapping, React Query invalidation, loading/error mapping, and use-case names.
5. Keep generated clients out of pages unless the existing feature convention explicitly permits a narrow direct read.
6. Put user-facing text, metadata, validation copy, empty states, and API error display mapping in `src/Hosts/Web/src/localization/**`.
7. Use central formatting helpers for dates, times, numbers, currencies, percentages, lists, direction, and plural-sensitive messages.
8. Prefer raw HeroUI/HeroUI Pro components and documented composition before creating local wrappers.
9. Promote to shared Web UI only when the artifact is presentation-only, generic, reusable across real Web consumers, and free of backend/generated-client/feature workflow dependencies.

## Output

```md
## React UI Boundary
UI owner:
Placement:
Data flow:
Localization:
Accessibility:
Shared Web UI impact:
Validation:
Handoff:
```

## Hard Stops

- React UI imports backend Application, Domain, Infrastructure, Api, server projects, persistence libraries, or server-only modules.
- User-facing React text is introduced outside the central localization boundary.
- Generated clients, generated DTOs, `apiFetch`, or transport infrastructure localize display copy.
- Shared Web UI receives backend DTOs, generated client types, feature workflow names, routes, or React Query hooks.
- A custom UI wrapper hides HeroUI behavior without a current reuse or accessibility reason.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Validation

Run the relevant Web host package scripts for lint, typecheck, tests, and build. Run React localization validation when available. Use browser or screenshot verification for visual work that cannot be trusted from static checks alone.
