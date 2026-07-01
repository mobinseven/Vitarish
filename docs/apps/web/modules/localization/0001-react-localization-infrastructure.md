# React Localization Infrastructure

## Status

Accepted

## Scope

App: Web host  
Module: Localization  
Capability: React UI localization  
Related apps: Web  
Related modules: Account, Authentication, Home, Knowledge, Design system

## Context

The former Blazor UI architecture localized user-facing strings through `IStringLocalizer` before rendering them in components. The React web host had no equivalent global localization contract, so pages and components could only use hard-coded strings or page-local constants.

That creates three problems:

1. React UI copy becomes scattered across components.
2. Persian/RTL readiness cannot be enforced centrally.
3. Agents and reviewers have no concrete rule or source of truth for React localization.

## Decision

The React web host owns a central localization infrastructure under:

```txt
src/Hosts/Web/src/i18n/
```

The infrastructure is intentionally small and dependency-free for the first slice:

- `cultures.ts` owns supported cultures, the default culture, the culture cookie name, culture normalization, and text direction helpers.
- `messages.ts` owns the typed message catalog. English keys define the `MessageKey` contract; every supported culture must provide every key.
- `localizer.ts` owns translation lookup, fallback behavior, placeholder formatting, and culture-aware number/date formatting.
- `server.ts` resolves request culture from the culture cookie and `Accept-Language`.
- `client.tsx` owns `LocaleProvider`, `useLocalizer()`, and browser-side culture persistence.

`RootLayout` resolves culture server-side, sets `<html lang>` and `dir`, and passes the culture into the existing provider stack. Client components localize user-facing text through `useLocalizer().t(...)`.

## Rules

- User-facing React strings must be added to or reused from `src/Hosts/Web/src/i18n/messages.ts`.
- React components must not introduce page-local dictionaries, feature-local translation stores, or ad hoc `{ en, fa }` maps.
- Reusable UI arrays must store `labelKey: MessageKey` instead of rendered labels.
- Formatted text must use placeholders, not string concatenation.
- Culture-sensitive dates and numbers must use the active localizer or `Intl` with the active culture.
- Route paths, CSS classes, icon names, generated API names, query keys, and test selectors are technical literals and are not localized.
- Domain, Application, OpenAPI contracts, and generated API clients must remain free of UI localization infrastructure.

## Validation

The web host provides localization checks:

```bash
cd src/Hosts/Web
npm run validate:localization
npm run validate:localization:strict
npm run typecheck
npm run test
```

`validate:localization` reports candidate hard-coded React UI strings without failing. `validate:localization:strict` fails on candidates and should be used for new or fully migrated scopes.

## Consequences

Positive:

- React has a Blazor-equivalent localization boundary.
- Supported cultures, direction, and message keys are centralized.
- Components get a simple `t(...)` API without adding a third-party dependency.
- Agents and reviewers can enforce one React localization pattern.

Trade-offs:

- The first implementation supports simple placeholder interpolation, not ICU pluralization or grammatical message selection.
- Existing hard-coded React pages still need staged migration.
- If advanced pluralization, extraction, lazy-loading, or translation-management integration becomes required, this decision should be superseded by a dedicated ADR.

## Non-goals

- Do not replace Blazor `IStringLocalizer` infrastructure.
- Do not localize backend/domain/application messages directly.
- Do not add route-based locale prefixes in this slice.
- Do not introduce a translation-management service in this slice.

## Current Adoption

The initial adoption covers the root layout metadata/lang/dir, provider wiring, app bar brand text, account settings page labels, and account culture page labels/status text. Remaining React pages should migrate incrementally by adding keys to `messages.ts` and replacing hard-coded user-facing text with `useLocalizer().t(...)`.
